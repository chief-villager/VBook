using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookkeeping.Infrastructure.Persistence;

// Lets "dotnet ef migrations add ..." construct the context without the full app.
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=bookkeeping;User Id=SA;Password=FortyFourMinus4!;TrustServerCertificate=True;Encrypt=True;")
            .Options;

        return new AppDbContext(options, new NoOpDomainEventDispatcher());
    }
}
