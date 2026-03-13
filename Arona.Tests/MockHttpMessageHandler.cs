namespace Arona.Tests;

public class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage?> responseFactory) : DelegatingHandler(new HttpClientHandler())
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var mockResponse = responseFactory(request);
        
        return mockResponse != null
            ? Task.FromResult(mockResponse)
            : base.SendAsync(request, cancellationToken);
    }
}