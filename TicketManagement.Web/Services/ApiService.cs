using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TicketManagement.Web.Models;

namespace TicketManagement.Web.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
            _baseUrl = configuration["ApiSettings:BaseUrl"];
        }

        #region Authentication

        public async Task<UserViewModel> LoginAsync(LoginViewModel loginViewModel)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                email = loginViewModel.Email,
                password = loginViewModel.Password
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/user/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonConvert.DeserializeObject<ApiResponse<UserViewModel>>(jsonResponse);
                
                if (loginResponse.Success)
                {
                    return loginResponse.User;
                }
            }
            
            return null;
        }

        public async Task<UserViewModel> RegisterAsync(RegisterViewModel registerViewModel)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                name = registerViewModel.Name,
                email = registerViewModel.Email,
                password = registerViewModel.Password
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/user/signup", content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var registerResponse = JsonConvert.DeserializeObject<ApiResponse<UserViewModel>>(jsonResponse);
                
                if (registerResponse.Success)
                {
                    return registerResponse.User;
                }
            }
            
            return null;
        }

        #endregion

        #region User

        public async Task<IEnumerable<UserViewModel>> GetAllUsersAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/user");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<UserViewModel>>(jsonResponse);
            }
            
            return Enumerable.Empty<UserViewModel>();
        }

        public async Task<UserViewModel> GetUserByIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserViewModel>(jsonResponse);
            }
            
            return null;
        }

        #endregion

        #region Tickets

        public async Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/ticket");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<TicketViewModel>>(jsonResponse);
            }
            
            return Enumerable.Empty<TicketViewModel>();
        }

        public async Task<IEnumerable<TicketViewModel>> GetTicketsByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/ticket/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<TicketViewModel>>(jsonResponse);
            }
            
            return Enumerable.Empty<TicketViewModel>();
        }

        public async Task<IEnumerable<TicketViewModel>> GetOpenTicketsByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/ticket/open/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<TicketViewModel>>(jsonResponse);
            }
            
            return Enumerable.Empty<TicketViewModel>();
        }

        public async Task<IEnumerable<TicketViewModel>> GetClosedTicketsByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/ticket/closed/user/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<TicketViewModel>>(jsonResponse);
            }
            
            return Enumerable.Empty<TicketViewModel>();
        }

        public async Task<TicketViewModel> GetTicketByIdAsync(int ticketId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/ticket/{ticketId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TicketViewModel>(jsonResponse);
            }
            
            return null;
        }

        public async Task<TicketViewModel> CreateTicketAsync(CreateTicketViewModel createTicketViewModel, int userId)
        {
            try
            {
                // Check if userId is valid
                if (userId <= 0)
                {
                    Console.WriteLine($"Invalid userId: {userId}. User may not be properly authenticated.");
                    return null;
                }

                // Log the request data
                var requestData = new
                {
                    subject = createTicketViewModel.Subject,
                    description = createTicketViewModel.Description,
                    priority = (int)createTicketViewModel.Priority,
                    category = createTicketViewModel.Category ?? string.Empty,
                    subCategory = createTicketViewModel.SubCategory ?? string.Empty
                };
                
                Console.WriteLine($"Creating ticket with data: {JsonConvert.SerializeObject(requestData)}");
                
                var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

                Console.WriteLine($"Sending request to: {_baseUrl}/api/ticket/create?userId={userId}");
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/ticket/create?userId={userId}", content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response status: {response.StatusCode}");
                Console.WriteLine($"Response content: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var ticketResponse = JsonConvert.DeserializeObject<ApiTicketResponse>(responseContent);
                        
                        if (ticketResponse?.Success == true)
                        {
                            return ticketResponse.Ticket;
                        }
                        else
                        {
                            Console.WriteLine($"API returned success=false: {ticketResponse?.Message ?? "No message"}");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"Error deserializing API response: {jsonEx.Message}");
                        Console.WriteLine($"Response content was: {responseContent}");
                    }
                }
                else
                {
                    Console.WriteLine($"API request failed with status: {response.StatusCode}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("API endpoint not found. Check if the API is running and the URL is correct.");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
                Console.WriteLine($"Stack trace: {httpEx.StackTrace}");
                if (httpEx.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {httpEx.InnerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateTicketAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            
            return null;
        }

        public async Task<TicketViewModel> RespondToTicketAsync(int ticketId, string response)
        {
            try
            {
                Console.WriteLine($"Adding admin response to ticket ID: {ticketId}");
                Console.WriteLine($"Response text: {response}");
                
                var requestData = new { response = response ?? string.Empty };
                Console.WriteLine($"Request data: {JsonConvert.SerializeObject(requestData)}");
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestData), 
                    Encoding.UTF8, 
                    "application/json");

                var endpoint = $"{_baseUrl}/api/ticket/{ticketId}/respond";
                Console.WriteLine($"Sending request to: {endpoint}");
                
                var apiResponse = await _httpClient.PostAsync(endpoint, content);
                
                var responseContent = await apiResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Response status: {apiResponse.StatusCode}");
                Console.WriteLine($"Response content: {responseContent}");
                
                if (apiResponse.IsSuccessStatusCode)
                {
                    try
                    {
                        var ticketResponse = JsonConvert.DeserializeObject<ApiTicketResponse>(responseContent);
                        
                        if (ticketResponse?.Success == true)
                        {
                            Console.WriteLine("Admin response added successfully");
                            return ticketResponse.Ticket;
                        }
                        else
                        {
                            Console.WriteLine($"API returned success=false: {ticketResponse?.Message ?? "No message"}");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"Error deserializing API response: {jsonEx.Message}");
                        Console.WriteLine($"Response content was: {responseContent}");
                    }
                }
                else
                {
                    Console.WriteLine($"API request failed with status: {apiResponse.StatusCode}");
                    if (apiResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine("API endpoint not found. Check if the API is running and the URL is correct.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in RespondToTicketAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            
            return null;
        }

        public async Task<TicketViewModel> CloseTicketAsync(int ticketId)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/ticket/{ticketId}/close", null);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var ticketResponse = JsonConvert.DeserializeObject<ApiTicketResponse>(jsonResponse);
                
                if (ticketResponse.Success)
                {
                    return ticketResponse.Ticket;
                }
            }
            
            return null;
        }

        public async Task<TicketViewModel> ReopenTicketAsync(int ticketId)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/ticket/{ticketId}/reopen", null);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiTicketResponse>(jsonResponse);
                
                if (apiResponse.Success)
                {
                    return apiResponse.Ticket;
                }
            }
            
            return null;
        }

        #endregion

        #region Messages

        public async Task<IEnumerable<MessageViewModel>> GetMessagesByTicketIdAsync(int ticketId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/message/ticket/{ticketId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IEnumerable<MessageViewModel>>(jsonResponse);
            }
            
            return Enumerable.Empty<MessageViewModel>();
        }

        public async Task<MessageViewModel> CreateMessageAsync(CreateMessageViewModel createMessageViewModel, int userId)
        {
            var content = new StringContent(JsonConvert.SerializeObject(createMessageViewModel), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/message?userId={userId}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiMessageResponse>(jsonResponse);
                
                if (apiResponse.Success)
                {
                    return apiResponse.MessageData;
                }
            }
            
            return null;
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId)
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/message/{messageId}/read", null);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<ApiMessageResponse>(jsonResponse);
                
                return apiResponse.Success;
            }
            
            return false;
        }

        public async Task<int> GetUnreadMessageCountAsync(int ticketId, int userId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/message/unread/{ticketId}/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<int>(jsonResponse);
            }
            
            return 0;
        }

        public async Task<ApiMessageResponse> SaveMessageAsync(MessageDto message)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(new
                {
                    TicketId = message.TicketId,
                    Content = message.Content,
                    IsSenderAdmin = message.IsSenderAdmin
                }), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/message?userId={message.SenderId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ApiMessageResponse>(jsonResponse);
                }
                
                return new ApiMessageResponse
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                return new ApiMessageResponse
                {
                    Success = false,
                    Message = $"Exception occurred: {ex.Message}"
                };
            }
        }

        #endregion
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T User { get; set; }
    }

    public class ApiTicketResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TicketViewModel Ticket { get; set; }
    }

    public class ApiMessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MessageViewModel MessageData { get; set; }
    }
} 