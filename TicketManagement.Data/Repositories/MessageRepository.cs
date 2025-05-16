using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Data.DataContext;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly TicketDbContext _context;

        public MessageRepository(TicketDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Message>> GetMessagesByTicketIdAsync(int ticketId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.TicketId == ticketId)
                    .Include(m => m.Sender)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
                
                // Verify Sender is loaded
                foreach (var message in messages)
                {
                    if (message.Sender == null)
                    {
                        // Try to load sender explicitly if not loaded
                        message.Sender = await _context.Users.FindAsync(message.SenderId);
                    }
                }
                
                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting messages for ticket {ticketId}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return Enumerable.Empty<Message>();
            }
        }

        public async Task<Message> GetMessageByIdAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.MessageId == messageId);
                
                if (message != null && message.Sender == null)
                {
                    // Try to load sender explicitly if not loaded
                    message.Sender = await _context.Users.FindAsync(message.SenderId);
                }
                
                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting message {messageId}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        public async Task<Message> CreateMessageAsync(Message message)
        {
            try
            {
                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();
                
                // Reload the message with the sender included
                return await GetMessageByIdAsync(message.MessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<bool> MarkMessageAsReadAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                    return false;

                message.IsRead = true;
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking message as read: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<int> GetUnreadMessageCountAsync(int ticketId, int userId)
        {
            return await _context.Messages
                .Where(m => m.TicketId == ticketId && m.SenderId != userId && !m.IsRead)
                .CountAsync();
        }

        public async Task<bool> DeleteMessageAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                    return false;

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
} 