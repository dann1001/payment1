using System;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Application.Commands.CreateInvoice;
using GatewayService.AccountCharge.Application.Services;
using GatewayService.AccountCharge.Domain.Repositories;
using GatewayService.AccountCharge.Infrastructure.Background;
using GatewayService.AccountCharge.Infrastructure.Http;
using GatewayService.AccountCharge.Infrastructure.Options;
using GatewayService.AccountCharge.Infrastructure.Persistence;
using GatewayService.AccountCharge.Infrastructure.Providers;
using GatewayService.AccountCharge.Infrastructure.Repositories;
using GatewayService.AccountCharge.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure;

public static class DependencyInjection
{
    // Application wiring (MediatR, handlers)
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateInvoiceHandler).Assembly);
        });

        return services;
    }

    // Infra wiring (EF Core, HttpClients, repos, orchestrators, bg workers)
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---------------- EF Core ----------------
        var cs = configuration.GetConnectionString("AccountChargeDb")
                 ?? throw new InvalidOperationException("Missing connection string 'AccountChargeDb'");

        services.AddDbContext<AccountChargeDb>(opt =>
            opt.UseSqlServer(cs)
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors()
               .LogTo(Console.WriteLine,
                   new[]
                   {
                       RelationalEventId.CommandExecuting,
                       RelationalEventId.CommandExecuted,
                       CoreEventId.ContextDisposed,
                       CoreEventId.SaveChangesStarting,
                       CoreEventId.SaveChangesCompleted
                   },
                   Microsoft.Extensions.Logging.LogLevel.Information,
                   DbContextLoggerOptions.SingleLine | DbContextLoggerOptions.UtcTime)
        );

        // ---------------- Options ----------------
        // Payment matching from config
        services.Configure<PaymentMatchingOptionsConfig>(configuration.GetSection("PaymentMatching"));

        // Nobitex options (Program.cs قبلاً Bind می‌کند؛ اینجا هم اگر خواستی:)
        services.Configure<NobitexOptionsConfig>(configuration.GetSection(NobitexOptionsConfig.SectionName));

        // ---------------- Http Clients ----------------
        // Accounting (has its own helper)
        services.AddAccountingHttp(configuration);

        // Nobitex (Typed client + baseUrl + token header if present)
        // Infrastructure/DependencyInjection.cs
        services.AddHttpClient<INobitexClient, NobitexClient>((sp, http) =>
        {
            var nobitex = sp.GetRequiredService<IOptions<NobitexOptionsConfig>>().Value;

            var baseUrl = string.IsNullOrWhiteSpace(nobitex.BaseUrl)
                ? "https://api.nobitex.ir"
                : nobitex.BaseUrl;

            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            if (!string.IsNullOrWhiteSpace(nobitex.UserAgent))
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", nobitex.UserAgent);

            // 👇 همین خط مشکل رو حل می‌کنه: به جای Bearer از Token استفاده کن
            if (!string.IsNullOrWhiteSpace(nobitex.Token))
                http.DefaultRequestHeaders.Remove("Authorization");
                http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {nobitex.Token}");
        });


        // ---------------- UoW & Repositories ----------------
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
        services.AddScoped<IPrepaidInvoiceRepository, EfPrepaidInvoiceRepository>(); // ← جدید

        // ---------------- Providers / Generators ----------------
        services.AddScoped<IPaymentMatchingOptionsProvider, ConfigPaymentMatchingOptionsProvider>();
        services.AddScoped<IInvoiceNumberGenerator, DateTicksInvoiceNumberGenerator>();

        // ---------------- Orchestrators ----------------
        services.AddScoped<DepositMatchingOrchestrator>();

        // ---------------- Background Services ----------------
        var enablePolling = configuration.GetValue<bool?>("Nobitex:EnablePolling") ?? true;
        if (enablePolling)
            services.AddHostedService<NobitexPollingService>();

        // اگر خواستی برای Prepaid ها هم پولر اضافه کنی:
        // var enablePrepaidPolling = configuration.GetValue<bool?>("Prepaid:EnablePolling") ?? false;
        // if (enablePrepaidPolling)
        //     services.AddHostedService<PrepaidInvoicePollingService>();

        return services;
    }
}
