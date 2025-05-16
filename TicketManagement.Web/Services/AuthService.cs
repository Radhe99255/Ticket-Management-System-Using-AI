using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Security.Claims;
using TicketManagement.Web.Models;

namespace TicketManagement.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISession _session;

        public AuthService(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _httpContextAccessor = httpContextAccessor;
            _session = httpContextAccessor.HttpContext?.Session;
        }

        public async Task<bool> LoginAsync(LoginViewModel loginViewModel)
        {
            var user = await _apiService.LoginAsync(loginViewModel);
            
            if (user != null)
            {
                // Store user in session
                _session.SetString("CurrentUser", JsonConvert.SerializeObject(user));
                return true;
            }
            
            return false;
        }

        public async Task<bool> RegisterAsync(RegisterViewModel registerViewModel)
        {
            var user = await _apiService.RegisterAsync(registerViewModel);
            
            if (user != null)
            {
                // Store user in session
                _session.SetString("CurrentUser", JsonConvert.SerializeObject(user));
                return true;
            }
            
            return false;
        }

        public void Logout()
        {
            _session.Remove("CurrentUser");
        }

        public bool IsAuthenticated()
        {
            return _session.GetString("CurrentUser") != null;
        }

        public bool IsAdmin()
        {
            var userJson = _session.GetString("CurrentUser");
            if (string.IsNullOrEmpty(userJson))
                return false;

            var user = JsonConvert.DeserializeObject<UserViewModel>(userJson);
            return user?.IsAdmin ?? false;
        }

        public UserViewModel GetCurrentUser()
        {
            var userJson = _session.GetString("CurrentUser");
            if (string.IsNullOrEmpty(userJson))
                return null;

            return JsonConvert.DeserializeObject<UserViewModel>(userJson);
        }

        public int GetCurrentUserId()
        {
            var user = GetCurrentUser();
            return user?.UserId ?? 0;
        }
    }
} 