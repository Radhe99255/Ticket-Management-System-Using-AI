using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> AuthenticateUserAsync(string email, string password);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(User user);
        Task<bool> UserExistsAsync(string email);
    }
} 