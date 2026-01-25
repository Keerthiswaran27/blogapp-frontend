using BlogApp1.Shared;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlogApp1.Client.Services
{
    public class BlogAiService
    {
        private readonly HttpClient _http;

        public BlogAiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<OllamaBlogResponse> GenerateBlogAsync(
      OllamaBlogRequest request)
        {
            try
            {
                var res = await _http.PostAsJsonAsync(
                    "api/blog-ai/generate",
                    request);

                // 🔴 API returned error
                if (!res.IsSuccessStatusCode)
                {
                    var err = await res.Content.ReadAsStringAsync();

                    // Try to extract error message
                    try
                    {
                        var json = JsonDocument.Parse(err);
                        if (json.RootElement.TryGetProperty("error", out var e))
                            throw new Exception(e.ToString());
                    }
                    catch
                    {
                        // fallback
                        throw new Exception($"API Error: {res.StatusCode} - {err}");
                    }
                }

                var result =
                    await res.Content
                             .ReadFromJsonAsync<OllamaBlogResponse>();

                if (result == null)
                    throw new Exception("Empty response from server");

                return result;
            }
            catch (HttpRequestException)
            {
                throw new Exception("Network error. Please check connection.");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Request timeout. Try again later.");
            }
            catch (JsonException)
            {
                throw new Exception("Invalid response format from server.");
            }
        }
        public async Task<BlogGenerateResponse?> GenerateBlog(
       BlogGenerateRequest request)
        {
            var response = await _http.PostAsJsonAsync(
                "api/blog-ai/generate", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response
                .Content
                .ReadFromJsonAsync<BlogGenerateResponse>();
        }

    }
}
