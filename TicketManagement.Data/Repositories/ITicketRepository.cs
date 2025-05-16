using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.Repositories
{
    public interface ITicketRepository
    {
        Task<IEnumerable<Ticket>> GetAllTicketsAsync();
        Task<IEnumerable<Ticket>> GetTicketsByUserIdAsync(int userId);
        Task<IEnumerable<Ticket>> GetOpenTicketsByUserIdAsync(int userId);
        Task<IEnumerable<Ticket>> GetClosedTicketsByUserIdAsync(int userId);
        Task<Ticket> GetTicketByIdAsync(int ticketId);
        Task<Ticket> CreateTicketAsync(Ticket ticket);
        Task<Ticket> UpdateTicketAsync(Ticket ticket);
        Task<bool> CloseTicketAsync(int ticketId);
        Task<bool> ReopenTicketAsync(int ticketId);
        Task<bool> AddAdminResponseAsync(int ticketId, string response);
    }
} 