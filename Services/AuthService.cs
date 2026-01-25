using BlogApp1.Shared;
using Microsoft.JSInterop;
using Supabase.Gotrue;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ASService.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public AuthService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }
        public async Task<Guid?> RegisterAsync(SignupModel request)
        {
            // 1) Signup
            var signupResponse = await _http.PostAsJsonAsync("api/auth/signup", request);
            var signupText = await signupResponse.Content.ReadAsStringAsync();
            Console.WriteLine("Signup response: " + signupText);

            if (!signupResponse.IsSuccessStatusCode)
            {
                return null;
            }

            // Deserialize correctly
            var signupPayload = JsonSerializer.Deserialize<SignupResponseDto>(signupText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (signupPayload == null || signupPayload.UserId == Guid.Empty)
            {
                Console.WriteLine("Signup payload invalid or UserId empty");
                return null;
            }

            var userId = signupPayload.UserId;

            // 2) Create profile
            var profileRequest = new CreateProfileRequest
            {
                UserId = userId,
                Name = request.Name,
                Email = request.Email,
                Role = request.Role
            };

            var profileResponse = await _http.PostAsJsonAsync("api/auth/create-profile", profileRequest);
            var profileText = await profileResponse.Content.ReadAsStringAsync();
            //Console.WriteLine("Profile response: " + profileText);

            if (!profileResponse.IsSuccessStatusCode)
            {
                // Do whatever you want on profile error
                return null;
            }
            await _js.InvokeVoidAsync("sessionStorage.setItem", "uid", userId);
            return userId;
        }

        public async Task<string> LoginAsync(SignInModel request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/signin", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var user_details =  JsonSerializer.Deserialize<UserDetails>(content, options);
                    var userInfoResponse = await _http.GetAsync($"api/auth/user-info?uid={user_details.Uid}");
                    Console.WriteLine("check 1 passes");
                    if (userInfoResponse.IsSuccessStatusCode)
                    {
                        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<AuthResponse>();
                        Console.WriteLine("check 2 passes");
                        if(userInfo.Role.Contains(request.Role))
                        {
                            await _js.InvokeVoidAsync("sessionStorage.setItem", "access_token", user_details.AccessToken); // Replace with actual token
                            await _js.InvokeVoidAsync("sessionStorage.setItem", "email", userInfo.Email);
                            await _js.InvokeVoidAsync("sessionStorage.setItem", "name", userInfo.FullName);
                            await _js.InvokeVoidAsync("sessionStorage.setItem", "userRole", request.Role);
                            await _js.InvokeVoidAsync("sessionStorage.setItem", "uid", user_details.Uid);
                            return request.Role;
                        }
                        else
                        {
                            return "wrong role";
                        }
                    }
                    else
                    {
                        return "no user found";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Sign-in failed: {errorContent}");
                }
                else
                {
                    throw new Exception($"Sign-in failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return "error";
            }
        }
        public async Task UpdateUserPreferencesAsync(List<string> topics)
        {
            var userId = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            var request = new UpdateUserPreferenceRequest
            {
                UserId = userId,
                Preferences = topics
            };

            var response = await _http.PostAsJsonAsync("api/auth/User-Preference", request);
            response.EnsureSuccessStatusCode();
            // Optionally read response:
            // var updated = await response.Content.ReadFromJsonAsync<BlogUserDto>();
        }
        public async Task<UserStatsDto?> GetUserStatsAsync()
        {
            var userId = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            return await _http.GetFromJsonAsync<UserStatsDto>($"api/auth/user-stats/{userId}");
        }

        public async Task<BlogUserDto?> GetUserInfoAsync()
        {
            var userId = await _js.InvokeAsync<Guid>("sessionStorage.getItem", "uid");
            return await _http.GetFromJsonAsync<BlogUserDto>($"api/auth/user-profile-info/{userId}");
        }

        public async Task<BlogUserDto?> UpdateProfileAsync(BlogUserDto user)
        {
            // PUT api/blogusers
            var response = await _http.PutAsJsonAsync("api/auth/editprofile", user);

            if (!response.IsSuccessStatusCode)
            {
                // You can throw, return null, or handle errors more gracefully
                return null;
            }

            // Assuming the API returns the updated BlogUserDto
            return await response.Content.ReadFromJsonAsync<BlogUserDto>();
        }
    }
    public class AuthResponse
    {
        public string Email {  get; set; }
        public string FullName  { get; set; }
        public string[] Role { get; set; }
    }
    public class UserDetails
    {
        public string AccessToken { get; set; }
        public Guid Uid { get; set; }
    }
}