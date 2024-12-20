using System.Net.Http.Headers;

namespace Equinor.SubSurfAppManagementMonitoringNuGet.HttpClients;

public interface ISmdaClient
{
    Task<HttpResponseMessage> GetSmdaDataAsync(string path, string accessToken,HttpCompletionOption? option = null,
        CancellationToken cancellationToken = default);
}

public class SmdaClient : ISmdaClient
{
    private readonly HttpClient _httpClient;

    public SmdaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> GetSmdaDataAsync(string path, string accessToken, HttpCompletionOption? option = null, CancellationToken cancellationToken = default)
    {
        var completionOption = option ?? HttpCompletionOption.ResponseContentRead;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync(path, completionOption, cancellationToken);
        return response;
    }
}