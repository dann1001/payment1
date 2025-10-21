// File: GatewayService.AccountCharge.Api/Controllers/PrepaidInvoicesController.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using GatewayService.AccountCharge.Application.Commands.Prepaid;
using GatewayService.AccountCharge.Application.DTOs;
using GatewayService.AccountCharge.Application.Queries.Prepaid;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.AccountCharge.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/prepaid-invoices")]
    [Produces(MediaTypeNames.Application.Json)]
    public sealed class PrepaidInvoicesController : ControllerBase
    {
        private readonly ISender _sender;
        public PrepaidInvoicesController(ISender sender) => _sender = sender;

        public sealed class CreateRequest
        {
            [Required] public string Currency { get; set; } = default!;
            public string? Network { get; set; }
            [Required] public string TxHash { get; set; } = default!;
            public string? CustomerId { get; set; }
            public DateTimeOffset? ExpiresAtUtc { get; set; }
        }

        [HttpPost]
        [ProducesResponseType(typeof(PrepaidInvoiceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct)
        {
            TimeSpan? ttl = null;
            if (req.ExpiresAtUtc.HasValue)
            {
                var delta = req.ExpiresAtUtc.Value - DateTimeOffset.UtcNow;
                if (delta > TimeSpan.Zero) ttl = delta;
            }

            var id = await _sender.Send(new CreatePrepaidInvoiceCommand(
                Currency: req.Currency,
                Network: req.Network,
                TxHash: req.TxHash,
                CustomerId: req.CustomerId,
                Ttl: ttl
            ), ct);

            // Explicit generic Send<T> to avoid falling back to object
            var sync = await _sender.Send<SyncPrepaidInvoiceResult>(new SyncPrepaidInvoiceCommand(id), ct);
            if (sync.Duplicate)
            {
                return Conflict(new
                {
                    code = "DuplicateTx",
                    prepaidInvoiceId = id,
                    accountingInvoiceId = sync.AccountingInvoiceId,
                    message = "Transaction already exists."
                });
            }

            var dto = await _sender.Send(new GetPrepaidInvoiceQuery(id), ct);
            return CreatedAtAction(nameof(GetById), new { version = "1.0", id }, dto);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PrepaidInvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            try
            {
                var dto = await _sender.Send(new GetPrepaidInvoiceQuery(id), ct);
                return Ok(dto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("{id:guid}/sync")]
        [ProducesResponseType(typeof(PrepaidInvoiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Sync([FromRoute] Guid id, CancellationToken ct)
        {
            try
            {
                var sync = await _sender.Send<SyncPrepaidInvoiceResult>(new SyncPrepaidInvoiceCommand(id), ct);
                if (sync.Duplicate)
                {
                    return Conflict(new
                    {
                        code = "DuplicateTx",
                        prepaidInvoiceId = id,
                        accountingInvoiceId = sync.AccountingInvoiceId,
                        message = "Transaction already exists."
                    });
                }

                var dto = await _sender.Send(new GetPrepaidInvoiceQuery(id), ct);
                return Ok(dto);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
