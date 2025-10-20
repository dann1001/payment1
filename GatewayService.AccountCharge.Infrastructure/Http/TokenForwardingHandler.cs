using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GatewayService.AccountCharge.Infrastructure.Http;

/// <summary>
/// Forwards the incoming Authorization header to downstream services like Accounting.
/// </summary>
public sealed class TokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContext;

    public TokenForwardingHandler(IHttpContextAccessor httpContext)
        => _httpContext = httpContext;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContext.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.TryAddWithoutValidation("Authorization", token);
        return base.SendAsync(request, cancellationToken);
    }
}
