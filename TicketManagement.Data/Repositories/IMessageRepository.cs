using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManagement.Data.Models;

namespace TicketManagement.Data.Repositories
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetMessagesByTicketIdAsync(int ticketId);
        Task<Message> GetMessageByIdAsync(int messageId);
        Task<Message> CreateMessageAsync(Message message);
        Task<bool> MarkMessageAsReadAsync(int messageId);
        Task<int> GetUnreadMessageCountAsync(int ticketId, int userId);
        Task<bool> DeleteMessageAsync(int messageId);
    }
} 