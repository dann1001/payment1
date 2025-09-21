using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Commands.CreateInvoice;
using GatewayService.AccountCharge.Application.Services;
using GatewayService.AccountCharge.Infrastructure.Background;
using GatewayService.AccountCharge.Infrastructure.Http;
using GatewayService.AccountCharge.Infrastructure.Options;
using GatewayService.AccountCharge.Infrastructure.Persistence;
using GatewayService.AccountCharge.Infrastructure.Providers;
using GatewayService.AccountCharge.Infrastructure.Repositories;
using GatewayService.AccountCharge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GatewayService.AccountCharge.Infrastructure;

public static class DependencyInjection
{
    // ⚠️ This AddApplication() really belongs in the Application project/namespace.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateInvoiceHandler).Assembly);
        });

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        var cs = configuration.GetConnectionString("AccountChargeDb")
                 ?? throw new InvalidOperationException("Missing connection string 'AccountChargeDb'");
        services.AddDbContext<AccountChargeDb>(opt =>
       opt.UseSqlServer(cs)
          .EnableSensitiveDataLogging()      // نمایش مقادیر پارامترها
          .EnableDetailedErrors()            // جزئیات بیشتر برای خطاها
          .LogTo(Console.WriteLine,          // مقصد لاگ (می‌تونی لاگر هم بذاری)
              new[]
              {
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting,
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted,
               Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ContextDisposed,
               Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SaveChangesStarting,
               Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SaveChangesCompleted
              },
              Microsoft.Extensions.Logging.LogLevel.Information,
              DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime // آپشن‌های اضافی برای خوانایی
          )
   );


        // UoW & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<Domain.Repositories.IInvoiceRepository, EfInvoiceRepository>();

        // Options (Payment Matching only; Nobitex is bound/validated inside AddNobitexHttp)
        services.Configure<PaymentMatchingOptionsConfig>(configuration.GetSection("PaymentMatching"));

        // HttpClient (Typed) with base address, UA, and Token header
        services.AddNobitexHttp(configuration);

        // Providers / Generators
        services.AddScoped<IPaymentMatchingOptionsProvider, ConfigPaymentMatchingOptionsProvider>();
        services.AddScoped<IInvoiceNumberGenerator, DateTicksInvoiceNumberGenerator>();

        // Orchestrator (SCOPED) – used by controllers and background job
        services.AddScoped<DepositMatchingOrchestrator>(); // from Application.Services

        // Background polling (Singleton IHostedService)
        var enablePolling = configuration.GetValue<bool?>("Nobitex:EnablePolling") ?? true;
        if (enablePolling)
        {
            services.AddHostedService<NobitexPollingService>();
        }

        return services;
    }
}
