using Bookkeeping.Application.Abstractions;
using Bookkeeping.Application.CreditReadiness;
using Bookkeeping.Application.Identity;
using Bookkeeping.Application.Invoices;
using Bookkeeping.Application.Ledger;
using Bookkeeping.Application.Reporting;
using Bookkeeping.Application.Transactions;
using Bookkeeping.Domain.Invoices.Events;
using Bookkeeping.Domain.Transactions.Events;
using Bookkeeping.Infrastructure.Auth;
using Bookkeeping.Infrastructure.Documents;
using Bookkeeping.Infrastructure.Mono;
using Bookkeeping.Infrastructure.Persistence;
using Bookkeeping.Infrastructure.Persistence.Configurations;
using Bookkeeping.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Writers;
using Polly;

namespace Bookkeeping.Infrastructure.DependencyInjection;

// Each module registers its own repository, service, and event handlers. The
// composition root simply chains these, so a module is added or removed in one line.
public static class ModuleRegistration
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IIdentityService, IdentityService>();

        // ASP.NET Core Identity backed by the shared AppDbContext (auth schema).
        services.AddIdentityCore<ApplicationUser>(options => options.Password.RequiredLength = 8)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        // Override the default store so credential creation joins the caller's unit of
        // work instead of saving itself (see BookkeepingUserStore).
        services.AddScoped<IUserStore<ApplicationUser>, BookkeepingUserStore>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();

        // Permission-based authorization, scoped per business. The policy provider turns
        // a permission name used as a policy into a PermissionRequirement, and the handler
        // decides it against the caller's role for the business on the route.
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }

    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionService, TransactionService>();

        // Bank accounts a business has connected through the feed provider.
        services.AddScoped<ILinkedBankAccountRepository, LinkedBankAccountRepository>();
        services.AddScoped<IBankAccountService, BankAccountService>();

        // Bank-import review queue: pulls a linked account's feed into a quarantine of
        // staged rows and promotes approved rows into Transactions.
        services.AddScoped<IStagedBankTransactionRepository, StagedBankTransactionRepository>();
        services.AddScoped<IBankImportService, BankImportService>();

        // Mono open-banking client (Transactions:Mono config section), validated at startup.
        services.AddOptions<MonoOptions>()
            .BindConfiguration(MonoOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<MonoApiClient>((sp, http) =>
            {
                var mono = sp.GetRequiredService<IOptions<MonoOptions>>().Value;
                http.BaseAddress = new Uri(mono.BaseUrl);
                http.DefaultRequestHeaders.Add("mono-sec-key", mono.SecretKey);
            })
            .AddStandardResilienceHandler(o =>
            {
                // Microsoft.Extensions.Http.Resilience — retry + circuit breaker + timeout
                o.Retry.MaxRetryAttempts = 3;
                o.Retry.BackoffType = DelayBackoffType.Exponential;
                o.Retry.UseJitter = true;
            });

        // The app depends on IBankFeedProvider; Mono is the concrete adapter behind it.
        services.AddScoped<IBankFeedProvider, MonoBankFeedProvider>();

        return services;
    }

    public static IServiceCollection AddLedgerModule(this IServiceCollection services)
    {
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IPostingRuleResolver, PostingRuleResolver>();

        // The core seam: a recorded transaction becomes a balanced journal entry.
        services.AddScoped<IDomainEventHandler<TransactionRecorded>, TransactionRecordedHandler>();
        return services;
    }

    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IFinancialReportingService, FinancialReportingService>();
        return services;
    }

    public static IServiceCollection AddCreditReadinessModule(this IServiceCollection services)
    {
        services.AddScoped<ICreditReadinessService, CreditReadinessService>();
        return services;
    }

    public static IServiceCollection AddInvoiceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoicePdfJobRepository, InvoicePdfJobRepository>();

        // Creating an invoice stages a PDF job in the same transaction (outbox).
        services.AddScoped<IDomainEventHandler<InvoiceCreated>, InvoiceCreatedHandler>();

        // Stateless and thread-safe: one instance serves every request.
        services.AddSingleton<IInvoicePdfGenerator, InvoicePdfGenerator>();

        // R2 storage + the background worker that drains the PDF outbox out of band.
        services.Configure<R2Options>(configuration.GetSection("R2"));
        services.AddSingleton<IInvoiceDocumentStore, R2InvoiceDocumentStore>();
        services.AddHostedService<InvoicePdfOutboxProcessor>();
        return services;
    }

}
