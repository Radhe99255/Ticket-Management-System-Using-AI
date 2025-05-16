using TicketManagement.Web.Models;

namespace TicketManagement.Web.Services
{
    public interface IApiService
    {
        // Authentication
        Task<UserViewModel> LoginAsync(LoginViewModel loginViewModel);
        Task<UserViewModel> RegisterAsync(RegisterViewModel registerViewModel);
        
        // User
        Task<IEnumerable<UserViewModel>> GetAllUsersAsync();
        Task<UserViewModel> GetUserByIdAsync(int userId);
        
        // Tickets
        Task<IEnumerable<TicketViewModel>> GetAllTicketsAsync();
        Task<IEnumerable<TicketViewModel>> GetTicketsByUserIdAsync(int userId);
        Task<IEnumerable<TicketViewModel>> GetOpenTicketsByUserIdAsync(int userId);
        Task<IEnumerable<TicketViewModel>> GetClosedTicketsByUserIdAsync(int userId);
        Task<TicketViewModel> GetTicketByIdAsync(int ticketId);
        Task<TicketViewModel> CreateTicketAsync(CreateTicketViewModel createTicketViewModel, int userId);
        Task<TicketViewModel> RespondToTicketAsync(int ticketId, string response);
        Task<TicketViewModel> CloseTicketAsync(int ticketId);
        Task<TicketViewModel> ReopenTicketAsync(int ticketId);
        
        // Messages
        Task<IEnumerable<MessageViewModel>> GetMessagesByTicketIdAsync(int ticketId);
        Task<MessageViewModel> CreateMessageAsync(CreateMessageViewModel createMessageViewModel, int userId);
        Task<bool> MarkMessageAsReadAsync(int messageId);
        Task<int> GetUnreadMessageCountAsync(int ticketId, int userId);
        Task<ApiMessageResponse> SaveMessageAsync(MessageDto message);
    }
} 