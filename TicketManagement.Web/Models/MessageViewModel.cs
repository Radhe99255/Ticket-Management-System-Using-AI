using System;

namespace TicketManagement.Web.Models
{
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public int TicketId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public bool IsSenderAdmin { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
    
    public class CreateMessageViewModel
    {
        public int TicketId { get; set; }
        public string Content { get; set; }
    }
} 