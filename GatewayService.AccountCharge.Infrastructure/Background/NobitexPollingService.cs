using GatewayService.AccountCharge.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GatewayService.AccountCharge.Infrastructure.Background;

public sealed class NobitexPollingService : BackgroundService
{
    private readonly ILogger<NobitexPollingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly IConfiguration _config;

    public NobitexPollingService(
        ILogger<NobitexPollingService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
        var seconds = _config.GetValue<int?>("Nobitex:PollingSeconds") ?? 30;
        _interval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Nobitex polling service started. Interval: {Seconds}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                // Resolve scoped services inside the scope:
                var orchestrator = scope.ServiceProvider.GetRequiredService<DepositMatchingOrchestrator>();

                // TODO: decide which wallets to poll. Example: read from config or DB.
                // var walletIds = new[] { 123, 456 }; // placeholder
                // foreach (var wid in walletIds)
                // {
                //     var res = await orchestrator.FetchAndApplyAsync(wid, limit: 30, since: null, stoppingToken);
                //     _logger.LogInformation("Wallet {Wid}: total={Total} matched={Matched} applied={Applied} already={Already} rejected={Rejected}",
                //         wid, res.Total, res.Matched, res.Applied, res.AlreadyApplied, res.Rejected);
                // }

                // If you don't yet have the polling loop logic, at least touch orchestrator once:
                _ = orchestrator; // keep compiler happy until you wire actual logic
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Nobitex polling iteration");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
        }

        _logger.LogInformation("Nobitex polling service stopped.");
    }
}
