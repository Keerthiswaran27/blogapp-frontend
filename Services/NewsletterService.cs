using BlogApp1.Shared;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlogApp1.Client.Services
{
    public class NewsletterService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public NewsletterService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;

        }

        public async Task<bool> SubscribeAsync(string emailId)
        {
            var userUuid = await _js.InvokeAsync<string>("sessionStorage.getItem", "uid");

            var request = new CreateNewsletterSubscriptionRequest
            {
                EmailId = emailId,
                UserUuid = userUuid
            };

            var response = await _http.PostAsJsonAsync("api/newsletter/subscribe", request); // typical pattern[web:49][web:50]

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> IsSubscribedAsync()
        {
            var userUuid = await _js.InvokeAsync<string>("sessionStorage.getItem", "uid");

            var request = new CheckNewsletterSubscriptionRequest
            {
                UserUuid = userUuid
            };

            var response = await _http.PostAsJsonAsync("api/newsletter/check", request);

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<CheckNewsletterSubscriptionResponse>();

            return result?.Status == "present";
        }
    }
}
