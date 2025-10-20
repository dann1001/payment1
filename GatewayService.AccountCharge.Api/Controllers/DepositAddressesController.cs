// File: Api/Controllers/DepositsController.cs
using GatewayService.AccountCharge.Application.Commands.Deposits;
using GatewayService.AccountCharge.Application.Contracts.Deposits;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace GatewayService.AccountCharge.Api.Controllers;

[ApiController]
[Route("api/v1/deposits")]
[Produces("application/json")]
public sealed class DepositsController : ControllerBase
{
    private readonly ISender _sender;

    public DepositsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public endpoint to generate a new deposit address from Nobitex.</summary>
    [HttpPost("address")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(GeneratedAddressResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<GeneratedAddressResult>> GenerateAddress(
        [FromBody] GenerateAddressRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Currency))
            return BadRequest(new { error = "Currency is required." });

        var result = await _sender.Send(
            new GenerateAddressCommand(req.Currency, req.Network),
            ct);

        return Ok(result);
    }
}
   