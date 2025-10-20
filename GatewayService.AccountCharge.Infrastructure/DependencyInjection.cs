// File: D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\DependencyInjection.cs
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
using Microsoft.AspNetCore.Http; 

namespace GatewayService.AccountCharge.Infrastructure;

public static class DependencyInjection
{
    // ---------------- MediatR & Application Layer ----------------
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateInvoiceHandler).Assembly);
        });

        return services;
    }

    // ---------------- Infrastructure Layer ----------------
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
        services.Configure<PaymentMatchingOptionsConfig>(configuration.GetSection("PaymentMatching"));
        services.Configure<NobitexOptionsConfig>(configuration.GetSection(NobitexOptionsConfig.SectionName));

        // ---------------- Http Clients ----------------
        // Allow reading HttpContext for Authorization forwarding
        services.AddHttpContextAccessor();
        services.AddTransient<TokenForwardingHandler>();

        // Accounting HTTP client (with token forwarding + SSL bypass for dev)
        var accountingSection = configuration.GetSection("Accounting");
        var baseUrl = accountingSection["BaseUrl"] ?? throw new InvalidOperationException("Accounting:BaseUrl required.");

        services.AddHttpClient<IAccountingClient, AccountingClient>(http =>
        {
            http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        })
        .AddHttpMessageHandler<TokenForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            // ⚠️ فقط برای توسعه (self-signed certs)
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        });

        // Nobitex typed client — Single Source of Truth for Authorization is inside NobitexClient.
        services.AddHttpClient<INobitexClient, NobitexClient>((sp, http) =>
        {
            var nobitex = sp.GetRequiredService<IOptions<NobitexOptionsConfig>>().Value;

            var baseUrl = string.IsNullOrWhiteSpace(nobitex.BaseUrl)
                ? "https://apiv2.nobitex.ir"
                : nobitex.BaseUrl.TrimEnd('/');

            http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            if (!string.IsNullOrWhiteSpace(nobitex.UserAgent))
                http.DefaultRequestHeaders.UserAgent.ParseAdd(nobitex.UserAgent);
        });

        // ---------------- UoW & Repositories ----------------
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();
        services.AddScoped<IPrepaidInvoiceRepository, EfPrepaidInvoiceRepository>();

        // ---------------- Providers / Generators ----------------
        services.AddScoped<IPaymentMatchingOptionsProvider, ConfigPaymentMatchingOptionsProvider>();
        services.AddScoped<IInvoiceNumberGenerator, DateTicksInvoiceNumberGenerator>();

        // ---------------- Orchestrators ----------------
        services.AddScoped<DepositMatchingOrchestrator>();

        // ---------------- Background Services ----------------
        var enablePolling = configuration.GetValue<bool?>("Nobitex:EnablePolling") ?? true;
        if (enablePolling)
            services.AddHostedService<NobitexPollingService>();

        return services;
    }
}
