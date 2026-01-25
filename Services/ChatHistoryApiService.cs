using BlogApp1.Shared;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BlogApp1.Client.Services
{
  

    public class ChatHistoryApiService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public ChatHistoryApiService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        // 1️⃣ INSERT CHAT MESSAGE
        public async Task SaveChatAsync(ChatInsertDto dto)
        {
            var json = JsonSerializer.Serialize(dto);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync(
                "api/chat-history/insert",
                content);

            response.EnsureSuccessStatusCode();
        }

        // 2️⃣ FETCH CHAT HISTORY BY USER UUID
        public async Task<List<ChatHistoryGroupDto>>
            GetChatHistoryAsync()
        {
            var userUuid = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");

            var response = await _http.GetAsync(
                $"api/chat-history/{userUuid}");

            var result =
                await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<
                List<ChatHistoryGroupDto>>(
                result,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }

}
