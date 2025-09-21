using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayService.AccountCharge.Application.DTOs;

    public sealed record GeneratedAddressResult(
     string Address,
     string? Tag,
     string Network,
     int WalletId,
     string Currency,
     DateTimeOffset CreatedAt
 );


