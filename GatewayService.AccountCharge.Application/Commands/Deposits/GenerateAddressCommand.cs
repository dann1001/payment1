// File: Application/Commands/Deposits/GenerateAddressCommand.cs
using GatewayService.AccountCharge.Application.Contracts.Deposits;
using MediatR;

namespace GatewayService.AccountCharge.Application.Commands.Deposits;

// Request to generate a deposit address via Nobitex
public sealed record GenerateAddressCommand(
    string Currency,
    string? Network
) : IRequest<GeneratedAddressResult>;
