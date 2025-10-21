// File: GatewayService.AccountCharge.Application/Commands/Prepaid/SyncPrepaidInvoiceResult.cs
using System;

namespace GatewayService.AccountCharge.Application.Commands.Prepaid
{
    /// <summary>
    /// Result returned from syncing a prepaid invoice.
    /// </summary>
    public sealed record SyncPrepaidInvoiceResult(
        bool Updated,
        bool Duplicate,
        Guid? AccountingInvoiceId
    );
}
