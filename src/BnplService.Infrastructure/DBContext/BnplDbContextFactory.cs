using Microsoft.EntityFrameworkCore.Design;

namespace BnplService.Infrastructure.DBContext;

/// <summary>
/// فقط برای ابزار dotnet ef (ساخت Migration و اسکریپت):
///   BNPL_CONNECTION="Server=...;Database=Bnpl;..." dotnet ef database update -p src/BnplService.Infrastructure
/// </summary>
public sealed class BnplDbContextFactory : IDesignTimeDbContextFactory<BnplDbContext>
{
    public BnplDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("BNPL_CONNECTION")
                         ?? "Server=localhost;Database=Bnpl;Trusted_Connection=True;TrustServerCertificate=True";
        return new BnplDbContext(new DbContextOptionsBuilder<BnplDbContext>().UseSqlServer(connection, BnplModel.SqlServer).Options);
    }
}
