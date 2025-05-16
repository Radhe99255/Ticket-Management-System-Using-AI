using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Data.DataContext;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.Repositories
{
    public class TicketRepository : ITicketRepository
    {
        private readonly TicketDbContext _context;

        public TicketRepository(TicketDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Ticket>> GetAllTicketsAsync()
        {
            return await _context.Tickets
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetTicketsByUserIdAsync(int userId)
        {
            return await _context.Tickets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetOpenTicketsByUserIdAsync(int userId)
        {
            return await _context.Tickets
                .Where(t => t.UserId == userId && t.Status == TicketStatus.Open)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> GetClosedTicketsByUserIdAsync(int userId)
        {
            return await _context.Tickets
                .Where(t => t.UserId == userId && t.Status == TicketStatus.Closed)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Ticket> GetTicketByIdAsync(int ticketId)
        {
            return await _context.Tickets
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);
        }

        public async Task<Ticket> CreateTicketAsync(Ticket ticket)
        {
            await _context.Tickets.AddAsync(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<Ticket> UpdateTicketAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<bool> CloseTicketAsync(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
                return false;

            ticket.Status = TicketStatus.Closed;
            ticket.ClosedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddAdminResponseAsync(int ticketId, string response)
        {
            try
            {
                Console.WriteLine($"Repository: Finding ticket with ID {ticketId}");
                var ticket = await _context.Tickets.FindAsync(ticketId);
                
                if (ticket == null)
                {
                    Console.WriteLine($"Repository: Ticket with ID {ticketId} not found");
                    return false;
                }

                Console.WriteLine($"Repository: Current AdminResponse: '{ticket.AdminResponse}'");
                Console.WriteLine($"Repository: Setting AdminResponse to: '{response}'");
                
                ticket.AdminResponse = response;
                
                Console.WriteLine("Repository: Saving changes to database");
                var saveResult = await _context.SaveChangesAsync();
                
                Console.WriteLine($"Repository: SaveChangesAsync result: {saveResult}");
                return saveResult > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Repository error in AddAdminResponseAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<bool> ReopenTicketAsync(int ticketId)
        {
            try
            {
                Console.WriteLine($"Repository: Finding ticket with ID {ticketId} to reopen");
                var ticket = await _context.Tickets.FindAsync(ticketId);
                
                if (ticket == null)
                {
                    Console.WriteLine($"Repository: Ticket with ID {ticketId} not found");
                    return false;
                }

                if (ticket.Status == TicketStatus.Open)
                {
                    Console.WriteLine($"Repository: Ticket with ID {ticketId} is already open");
                    return true; // Already in the desired state
                }

                Console.WriteLine($"Repository: Changing ticket status from {ticket.Status} to Open");
                ticket.Status = TicketStatus.Open;
                ticket.ClosedAt = null;
                
                Console.WriteLine("Repository: Saving changes to database");
                var saveResult = await _context.SaveChangesAsync();
                
                Console.WriteLine($"Repository: SaveChangesAsync result: {saveResult}");
                return saveResult > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Repository error in ReopenTicketAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
} 