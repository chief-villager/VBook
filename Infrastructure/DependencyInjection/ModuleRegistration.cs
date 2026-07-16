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
using Bookkeeping.Infrastructure.Persistence;
using Bookkeeping.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;

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
        return services;
    }

    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ITransactionService, TransactionService>();
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
