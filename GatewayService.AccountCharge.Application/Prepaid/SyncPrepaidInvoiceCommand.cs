// File: GatewayService.AccountCharge.Application/Commands/Prepaid/SyncPrepaidInvoiceCommand.cs
using System;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid
{
    /// <summary>
    /// Triggers a sync for a prepaid invoice and returns a SyncPrepaidInvoiceResult.
    /// </summary>
    public sealed record SyncPrepaidInvoiceCommand(Guid Id) : IRequest<SyncPrepaidInvoiceResult>;
}
