using System.Net.Mime;
using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using GatewayService.AccountCharge.Application.Commands.CreateInvoice;
using GatewayService.AccountCharge.Application.Commands.GenerateAndAttachAddress;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Application.Queries.GetInvoiceDetails;
using GatewayService.AccountCharge.Application.Queries.GetInvoiceStatus;
using GatewayService.AccountCharge.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using GatewayService.AccountCharge.Application.Commands.ConfirmTxHash;


namespace GatewayService.AccountCharge.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoices")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class InvoicesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly DepositMatchingOrchestrator _orchestrator;

    public InvoicesController(ISender sender, DepositMatchingOrchestrator orchestrator)
    {
        _sender = sender;
        _orchestrator = orchestrator;
    }

    /// <summary>Create a new invoice.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceHttpRequest req, CancellationToken ct)
    {
        // Convert optional ExpiresAtUtc to TTL
        TimeSpan? ttl = null;
        if (req.ExpiresAtUtc.HasValue)
        {
            var delta = req.ExpiresAtUtc.Value - DateTimeOffset.UtcNow;
            if (delta > TimeSpan.Zero) ttl = delta;
        }

        var cmd = new CreateInvoiceCommand(
            req.InvoiceNumber,
            req.Currency,
            req.Amount,
            req.CustomerId,
            ttl
        );

        var invoiceId = await _sender.Send(cmd, ct);
        var dto = await _sender.Send(new GetInvoiceStatusQuery(invoiceId), ct);

        return CreatedAtAction(
            nameof(GetByNumber),
            routeValues: new { version = "1.0", invoiceNumber = dto.InvoiceNumber },
            value: dto
        );
    }

    /// <summary>Get full invoice details by invoice number.</summary>
    [HttpGet("{invoiceNumber}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber([FromRoute] string invoiceNumber, CancellationToken ct)
    {
        try
        {
            var dto = await _sender.Send(new GetInvoiceDetailsQuery(invoiceNumber), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Get current invoice status by Id.</summary>
    [HttpGet("status/{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await _sender.Send(new GetInvoiceStatusQuery(id), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ===== Manual sync by invoiceNumber (ساده‌سازی؛ از SyncInvoiceAsync استفاده می‌کنیم) =====
    [HttpPost("{invoiceNumber}/sync")]
    [ProducesResponseType(typeof(ManualSyncHttpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ManualSync([FromRoute] string invoiceNumber, [FromBody] ManualSyncHttpRequest req, CancellationToken ct)
    {
        try
        {
            var invoice = await _sender.Send(new GetInvoiceDetailsQuery(invoiceNumber), ct);

            // یک‌جا کل اینوویس را سینک می‌کنیم
            var applied = await _orchestrator.SyncInvoiceAsync(invoice.Id, ct);

            // برای هم‌خوانی با Response قبلی، بقیه مقادیر را صفر می‌گذاریم
            var polledWallets = (invoice.Addresses ?? Array.Empty<InvoiceAddressDto>())
                                .Select(a => a.WalletId)
                                .Distinct()
                                .Count();

            return Ok(new ManualSyncHttpResponse
            {
                InvoiceNumber = invoiceNumber,
                PolledWallets = polledWallets,
                TotalDepositsSeen = 0,
                Matched = 0,
                Applied = applied,
                AlreadyApplied = 0,
                Rejected = 0
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // ===== New endpoints =====

    //public sealed class GenerateAddressRequest
    //{
    //    public string Currency { get; set; } = default!;
    //    public string? Network { get; set; }
    //}

    ///// <summary>Generate/attach a deposit address for an invoice by Id.</summary>
    //[HttpPost("{id:guid}/address")]
    //[Consumes("application/json")]
    //[Produces("application/json")]
    //[ProducesResponseType(typeof(GeneratedAddressResult), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //public async Task<ActionResult<GeneratedAddressResult>> GenerateAddress(
    //    [FromRoute] Guid id,
    //    [FromBody] GenerateAddressRequest req,
    //    CancellationToken ct)
    //{
    //    try
    //    {
    //        var result = await _sender.Send(
    //            new GenerateAndAttachAddressToInvoiceCommand(id, req.Currency, req.Network),
    //            ct);

    //        return Ok(result); // 200 + JSON: address, network, walletId, currency, createdAt
    //    }
    //    catch (KeyNotFoundException)
    //    {
    //        return NotFound();
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        return BadRequest(new { error = ex.Message });
    //    }
    //}

    [HttpPost("{id:guid}/sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ManualSync([FromRoute] Guid id, CancellationToken ct)
    {
        var applied = await _orchestrator.SyncInvoiceAsync(id, ct);
        return Ok(new { applied });
    }
    [HttpPost("{id:guid}/confirm-tx")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmByTxHash([FromRoute] Guid id, [FromBody] ConfirmTxRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.TxHash))
            return BadRequest(new { error = "txHash is required" });

        var res = await _sender.Send(new ConfirmTxHashCommand(id, req.TxHash.Trim()), ct);

        if (!res.FoundOnExchange)
            return NotFound(new { error = "Transaction not found on exchange" });

        return Ok(new
        {
            res.InvoiceId,
            res.FoundOnExchange,
            res.Matched,
            res.Applied,
            res.Reason
        });
    }
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceTransactionItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListTransactions(
        [FromRoute] Guid id,
        [FromQuery, Range(1, 200)] int? limit,
        [FromQuery] DateTimeOffset? sinceUtc,
        [FromQuery] bool onlyInvoiceAddresses = true,
        CancellationToken ct = default)
    {
        try
        {
            var list = await _sender.Send(
                new GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactions.GetInvoiceTransactionsQuery(
                    InvoiceId: id,
                    Limit: limit,
                    SinceUtc: sinceUtc,
                    OnlyInvoiceAddresses: onlyInvoiceAddresses
                ), ct);

            // Map to lightweight HTTP DTO
            var http = list.Select(x => new InvoiceTransactionItem
            {
                TxHash = x.TxHash!,
                Address = x.Address,
                Network = x.Network,
                Tag = x.Tag,
                Amount = x.Amount,
                Currency = x.Currency,
                Confirmed = x.Confirmed,
                Confirmations = x.Confirmations,
                RequiredConfirmations = x.RequiredConfirmations,
                CreatedAt = x.CreatedAt
            }).ToList();

            return Ok(http);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    // GET: /api/v1/invoices/{id}/transactions/all
    [HttpGet("{id:guid}/transactions/all")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceTransactionItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAllTransactions([FromRoute] Guid id, CancellationToken ct = default)
    {
        try
        {
            var list = await _sender.Send(
                new GatewayService.AccountCharge.Application.Queries.GetInvoiceTransactionsAll.GetInvoiceTransactionsAllQuery(id),
                ct);

            var http = list.Select(x => new InvoiceTransactionItem
            {
                TxHash = x.TxHash!,
                Address = x.Address,
                Network = x.Network,
                Tag = x.Tag,
                Amount = x.Amount,
                Currency = x.Currency,
                Confirmed = x.Confirmed,
                Confirmations = x.Confirmations,
                RequiredConfirmations = x.RequiredConfirmations,
                CreatedAt = x.CreatedAt
            }).ToList();

            return Ok(http);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public sealed class InvoiceTransactionItem
    {
        public string TxHash { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string? Network { get; set; }
        public string? Tag { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = default!;
        public bool Confirmed { get; set; }
        public int Confirmations { get; set; }
        public int RequiredConfirmations { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class ConfirmTxRequest
    {
        public string TxHash { get; set; } = default!;
        public string Network { get; set; } = default!;
    }
    // -------------------------
    // HTTP Request/Response DTOs scoped to API
    // -------------------------

    public sealed class CreateInvoiceHttpRequest
    {
        [Required] public string Currency { get; set; } = default!;
        [Range(0, double.MaxValue)] public decimal Amount { get; set; }
        public string? CustomerId { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }

    public sealed class ManualSyncHttpRequest
    {
        [Range(1, 200)] public int? Limit { get; set; }           // فعلاً استفاده نمی‌کنیم
        public DateTimeOffset? SinceUtc { get; set; }             // فعلاً استفاده نمی‌کنیم
    }

    public sealed class ManualSyncHttpResponse
    {
        public string InvoiceNumber { get; set; } = default!;
        public int PolledWallets { get; set; }
        public int TotalDepositsSeen { get; set; }
        public int Matched { get; set; }
        public int Applied { get; set; }
        public int AlreadyApplied { get; set; }
        public int Rejected { get; set; }
    }
}
