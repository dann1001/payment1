using System.Net.Http.Headers;
using GatewayService.AccountCharge.Application.Abstractions;
using GatewayService.AccountCharge.Infrastructure.Http;
using GatewayService.AccountCharge.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GatewayService.AccountCharge.Infrastructure;

public static class NobitexRegistration
{
    public static IServiceCollection AddNobitexHttp(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<NobitexOptionsConfig>()
                .Bind(config.GetSection(NobitexOptionsConfig.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "Nobitex:BaseUrl is required");

        services.AddHttpClient<INobitexClient, NobitexClient>((sp, http) =>
        {
            var opt = sp.GetRequiredService<IOptions<NobitexOptionsConfig>>().Value;
            http.BaseAddress = new Uri(opt.BaseUrl!);
            http.DefaultRequestHeaders.UserAgent.Clear();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(opt.UserAgent ?? "TraderBot/GatewayService");

            var token = opt.Token ?? Environment.GetEnvironmentVariable("NOBITEX_API_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Nobitex API Token is missing.");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

            // Optional: Accept JSON
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        return services;
    }
}
