using TicketManagement.Web.Models;

namespace TicketManagement.Web.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(LoginViewModel loginViewModel);
        Task<bool> RegisterAsync(RegisterViewModel registerViewModel);
        void Logout();
        bool IsAuthenticated();
        bool IsAdmin();
        UserViewModel GetCurrentUser();
        int GetCurrentUserId();
    }
} 