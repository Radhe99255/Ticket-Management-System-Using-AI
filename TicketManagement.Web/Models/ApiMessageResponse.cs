using TicketManagement.Web.Models;

namespace TicketManagement.Web.Models
{
    public class ApiMessageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MessageViewModel MessageData { get; set; }
    }
} 