using System.Text;
using Bookkeeping.Domain.Invoices;
using Bookkeeping.Infrastructure.Auth;
using Bookkeeping.Infrastructure.DependencyInjection;
using Bookkeeping.Infrastructure.Documents;
using Bookkeeping.Infrastructure.Persistence;
using Bookkeeping.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

// QuestPDF is free under the Community license for this use.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Used to fetch invoice-template logos for PDF rendering.
builder.Services.AddHttpClient();

// JWT bearer auth. Tokens are issued by JwtTokenIssuer using the same Jwt settings.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
        };
    });
builder.Services.AddAuthorization();

// Composition root: infrastructure first, then one line per module.
builder.Services
    .AddPersistence(builder.Configuration)
    .AddIdentityModule()
    .AddTransactionsModule()
    .AddLedgerModule()
    .AddReportingModule()
    .AddCreditReadinessModule()
    .AddInvoiceModule(builder.Configuration);

var app = builder.Build();


// Dev convenience: create the schema on startup. Switch to EF migrations for
// anything beyond local development (see README).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    dbContext.Database.EnsureCreated();

    await IdentitySeeder.SeedAsync(userManager, roleManager);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
