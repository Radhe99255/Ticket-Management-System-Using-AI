using System;

namespace TicketManagement.Api.DTOs
{
    public class MessageDto
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
    
    public class CreateMessageDto
    {
        public int TicketId { get; set; }
        public string Content { get; set; }
    }
    
    public class MessageResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MessageDto MessageData { get; set; }
    }
} 