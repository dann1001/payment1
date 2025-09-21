using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GatewayService.AccountCharge.Infrastructure.Persistence
{
    // Match your actual DbContext type name from the error: AccountChargeDb
    public sealed class AccountChargeDbFactory : IDesignTimeDbContextFactory<AccountChargeDb>
    {
        public AccountChargeDb CreateDbContext(string[] args)
        {
            // Resolve configuration from multiple possible locations
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var basePath = Directory.GetCurrentDirectory(); // Infrastructure project folder
            var apiDir = Path.GetFullPath(Path.Combine(basePath, "..", "GatewayService.AccountCharge.Api"));

            var cb = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables();

            // Also try to read appsettings from the API project (common setup)
            if (Directory.Exists(apiDir))
            {
                cb.AddJsonFile(Path.Combine(apiDir, "appsettings.json"), optional: true)
                  .AddJsonFile(Path.Combine(apiDir, $"appsettings.{env}.json"), optional: true);
            }

            var config = cb.Build();

            var conn = config.GetConnectionString("AccountChargeDb")
                ?? "Server=DESKTOP-B95G2DP;Database=AccountChargeDb;Trusted_Connection=True;TrustServerCertificate=True;";

            var opts = new DbContextOptionsBuilder<AccountChargeDb>()
                .UseSqlServer(conn)
                .Options;

            return new AccountChargeDb(opts);
        }
    }
}
