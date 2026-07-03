using System.Net;

namespace HelixCarbon.Client.Services;

public sealed class UnauthorizedResponseHandler(AuthSessionSignal sessionSignal) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            sessionSignal.NotifyUnauthorized();
        }

        return response;
    }
}
