
using BlogApp1.Shared;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace BlogApp1.Client.Services;

public class AiBlogService
{
    private readonly HttpClient _httpClient;

    public AiBlogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateBlogAsync(
    OllamaBlogRequest request)
    {
        var res = await _httpClient.PostAsJsonAsync(
            "api/blog-ai/generate",
            request);

        var raw = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception(
                $"API Error ({res.StatusCode}) : {raw}");
        }

        using var doc = JsonDocument.Parse(raw);

        if (!doc.RootElement.TryGetProperty("blog", out var blogProp))
        {
            throw new Exception("Invalid API response format");
        }

        return blogProp.GetString() ?? "";
    }


}
