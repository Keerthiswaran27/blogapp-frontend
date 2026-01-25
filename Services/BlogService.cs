using BlogApp1.Shared;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BSService.Services
{
    public class BlogService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public BlogService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public async Task<List<BlogDto>> GetAllBlogsAsync()
        {
            var blogs = await _http.GetFromJsonAsync<List<BlogDto>>("api/blogs");
            return blogs ?? new List<BlogDto>();
        }

        public async Task<BlogData?> GetBlogBySlugAsync(string slug)
        {
            return await _http.GetFromJsonAsync<BlogData>($"api/blogs/{slug}");
        }
        public async Task<bool> ToggleLikeAsync(int id, bool state)
        {
            var userid = await _js.InvokeAsync<Guid>("sessionStorage.getItem","uid");
            var requestBody = new LikeRequest { Id = id, State = state,UserId = userid }; 
            
            var response = await _http.PostAsJsonAsync("api/blogs/like", requestBody);
            Console.WriteLine(response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }
        public async Task<List<int>> GetLikedIdAsync()
        {
            var id = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            List<int> result = await _http.GetFromJsonAsync<List<int>>($"api/blogs/likedid/{id}");
            if(result !=null)
            {
                return result;
            }
            return new List<int>();

        }
        public async Task<List<int>> GetReadingHistoryAsync()
        {
            var id = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            var result = await _http.GetFromJsonAsync<int[]>($"api/Blogs/history/{id}");
            return result.ToList() ?? new List<int>();
        }
        public async Task<List<int>> GetSavedIdAsync()
        {
            var id = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            List<int> result = await _http.GetFromJsonAsync<List<int>>($"api/blogs/savedid/{id}");
            if(result !=null)
            {
                return result;
            }
            return new List<int>();

        }
        public async Task<bool> ToggleSaveAsync(int id ,bool state)
        {
            var userid = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            var requestBody = new SaveIdRequest { Uid = userid,BlogId = id,State = state };

            var response = await _http.PostAsJsonAsync("api/blogs/save", requestBody);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BlogUser>();
                Console.WriteLine($"Saved count: {result.SavedId.Length}");
            }
            else
            {
                Console.WriteLine("Error saving blog");
            }
            return response.IsSuccessStatusCode;
        }
        public async Task<BlogDto?> CreateBlogAsync(NewBlogRequest request)
        {
            var userid = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            var name = await _js.InvokeAsync<string>("sessionStorage.getItem", "name");
            if (userid == null)
                throw new InvalidOperationException("User not logged in.");

            var wordCount = CalculateWordCount(request.Content);
            var readingTime = CalculateReadingTimeMinutes(wordCount);

            var payload = new NewBlog1
            {
                Title = request.Title,
                Slug = request.Slug,
                Content = request.Content,
                CoverImageUrl = request.CoverImageUrl,
                AuthorName = name,
                AuthorUid = userid.ToString(),
                Tags = request.Tags,
                Domain = request.Domain,
                MetaDescription = request.MetaDescription,
                Summary = request.Summary,
                ReadingTime = readingTime,
                WordCount = wordCount
            };

            var response = await _http.PostAsJsonAsync("api/Blogs/newblog", payload);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BlogDto>();
        }

        public int CalculateWordCount(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            var normalized = Regex.Replace(content, @"\s+", " ").Trim();
            return normalized.Split(' ').Length;
        }

        public long CalculateReadingTimeMinutes(int wordCount, int wordsPerMinute = 200)
        {
            if (wordCount <= 0 || wordsPerMinute <= 0)
                return 0;

            var minutes = (int)Math.Ceiling(wordCount / (double)wordsPerMinute);
            return minutes;
        }
    

        public async Task<bool> FollowUserAsync(Guid authorUid)
        {
            try
            {
                var readeruid = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
                var request = new FollowRequest
                {
                    ReaderUid = readeruid,
                    AuthorUid = authorUid
                };

                var response = await _http.PostAsJsonAsync("api/blogs/follow", request);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public async Task<List<string>> GetFollowingAsync()
        {
            try
            {
                var readeruid = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
                var result = await _http.GetFromJsonAsync<List<string>>($"api/blogs/following/{readeruid}");
                return result ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        public async Task<List<string>> GetAuthorFollowingAsync(string uid)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<string>>($"api/blogs/following/{Guid.Parse(uid)}");
                return result ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        public async Task<List<string>> GetFollowersAsync(string uid)
        {
            try
            {
                var response = await _http.GetAsync($"api/blogs/followers/{Guid.Parse(uid)}");

                if (response.IsSuccessStatusCode)
                {
                    var followers = await response.Content.ReadFromJsonAsync<List<string>>();
                    return followers ?? new List<string>();
                }

                return new List<string>(); // return empty if not found/error
            }
            catch
            {
                return new List<string>(); // fallback
            }
        }
        public async Task TrackReadAsync(int blogId)
        {
            var userId = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            var request = new TrackReadRequest
            {
                BlogId = blogId,
                UserId = userId
            };

            var response = await _http.PostAsJsonAsync("api/blogs/read-history", request);
            response.EnsureSuccessStatusCode();
            // If you need the returned payload, deserialize it:
            // var result = await response.Content.ReadFromJsonAsync<TrackReadResultDto>();
        }
        public async Task<List<BlogDto>?> GetBlogsByAuthorAsync()
        {
            var authorUid = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");


            return await _http.GetFromJsonAsync<List<BlogDto>>(
                $"api/blogs/author-blogs/{authorUid}");
        }
        public class FollowRequest
        {
            public Guid ReaderUid { get; set; }
            public Guid AuthorUid { get; set; }
        }

        public class SaveIdRequest
        {
            public Guid Uid { get; set; }
            public int BlogId { get; set; }
            public bool State { get; set; }
        }
        public class LikeRequest
        {
            public int Id { get; set; }
            public bool State { get; set; }
            public Guid UserId { get; set; }
        }
        public class LikeIdRequest
        {
            public Guid Uid { get; set; }
        }
    }
}
