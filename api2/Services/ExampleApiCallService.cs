using System.Text.Json;

namespace api2.Services;

public class ExampleApiCallService
{
    private readonly HttpClient _httpClient;

    // Use IHttpClientFactory to get the named client
    public ExampleApiCallService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("protection");
    }

    public async Task<string> CallApiAsync()
    {
        try
        {
            // The token will be automatically retrieved and added to the Authorization header
            var response = await _httpClient.GetAsync("http://localhost:5007/Secured");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }

            return $"Error: {response.StatusCode}";
        }
        catch (Exception ex)
        {
            return $"Exception: {ex.Message}";
        }
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json);
    }
}