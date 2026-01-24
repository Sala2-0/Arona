namespace Arona.Models.Api;
internal interface IApiQuery<TRequest, TResponse>
{
    Task<TResponse> GetAsync(TRequest request);
}
