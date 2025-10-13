using GatewayService.AccountCharge.Application.Commands.Prepaid;
using GatewayService.AccountCharge.Domain.PrepaidInvoices;
using GatewayService.AccountCharge.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GatewayService.AccountCharge.Infrastructure.Background;

public sealed class PrepaidInvoicePollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrepaidInvoicePollingService> _log;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public PrepaidInvoicePollingService(IServiceScopeFactory scopeFactory, ILogger<PrepaidInvoicePollingService> log)
    {
        _scopeFactory = scopeFactory; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AccountChargeDb>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var pending = await db.PrepaidInvoices
                    .AsNoTracking()
                    .Where(x => x.Status == PrepaidInvoiceStatus.AwaitingConfirmations && (!x.ExpiresAt.HasValue || x.ExpiresAt > DateTimeOffset.UtcNow))
                    .OrderBy(x => x.CreatedAt)
                    .Take(50)
                    .Select(x => x.Id)
                    .ToListAsync(stoppingToken);

                foreach (var id in pending)
                    await mediator.Send(new SyncPrepaidInvoiceCommand(id), stoppingToken);
            }
            catch (Exception ex) { _log.LogError(ex, "PrepaidInvoice polling iteration failed"); }

            try { await Task.Delay(_interval, stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }
}
