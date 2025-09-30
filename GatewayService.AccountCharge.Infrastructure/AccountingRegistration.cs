// D:\GatewayService.AccountCharge\GatewayService.AccountCharge.Infrastructure\AccountingRegistration.cs
using System.Net.Http.Headers;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Infrastructure.Http;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure;

public static class AccountingRegistration
{
    public static IServiceCollection AddAccountingHttp(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<AccountingOptionsConfig>()
                .Bind(config.GetSection(AccountingOptionsConfig.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Accounting:BaseUrl is required");

        services.AddHttpClient<IAccountingClient, AccountingClient>((sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<AccountingOptionsConfig>>().Value;
            http.BaseAddress = new Uri(opt.BaseUrl!);

            http.DefaultRequestHeaders.UserAgent.Clear();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opt.UserAgent ?? "TraderBot/GatewayService.AccountCharge");

            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            if (!string.IsNullOrWhiteSpace(opt.ApiKey))
                http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", opt.ApiKey);
        });

        return services;
    }
}
