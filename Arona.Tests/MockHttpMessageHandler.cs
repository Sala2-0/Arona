namespace Arona.Tests;

public class MockHttpMessageHandler(HttpResponseMessage responseMessage) : HttpMessageHandler
{
    private readonly HttpResponseMessage _responseMessage = responseMessage;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseMessage);
    }
}