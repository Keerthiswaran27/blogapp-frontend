using BlogApp1.Shared;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace BlogApp1.Client.Services
{

    using System.Net.Http;
    using System.Text;
    using System.Text.Json;

    public class ChatService
    {
        private readonly HttpClient _http;

        public ChatService(HttpClient http)
        {
            _http = http;
        }

        public async Task<BotReply> AskBot(string question)
        {
            var json = JsonSerializer.Serialize(question);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response =
                await _http.PostAsync(
                    "api/chat/ask",
                    content);

            var result =
                await response.Content.ReadAsStringAsync();

            var raw = JsonSerializer.Deserialize<BotReplyRaw>(
                result,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            // map backend → frontend model
            return new BotReply
            {
                Answer = raw.answer,
                Sources = raw.chunks.Select(x =>
                    new SourceSnippet
                    {
                        Content = x.text,
                        Similarity = x.score
                    }).ToList()
            };
        }

        // internal mapping model
        class BotReplyRaw
        {
            public string answer { get; set; }
            public List<ChunkRaw> chunks { get; set; }
        }

        class ChunkRaw
        {
            public string text { get; set; }
            public float score { get; set; }
        }
    }



}
