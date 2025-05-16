using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManagement.Data.DataContext;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly TicketDbContext _context;

        public UserRepository(TicketDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> AuthenticateUserAsync(string email, string password)
        {
            return await _context.Users.FirstOrDefaultAsync(
                u => u.Email.ToLower() == email.ToLower() && u.Password == password);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> CreateUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
    }
} 