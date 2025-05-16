using System;

namespace TicketManagement.Web.Models
{
    /// <summary>
    /// A unified message model for chat functionality
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The ticket this message belongs to
        /// </summary>
        public int TicketId { get; set; }
        
        /// <summary>
        /// User ID of the message sender
        /// </summary>
        public int SenderId { get; set; }
        
        /// <summary>
        /// Display name of the sender
        /// </summary>
        public string SenderName { get; set; } = string.Empty;
        
        /// <summary>
        /// Actual message content
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the sender is an admin
        /// </summary>
        public bool IsAdmin { get; set; }
        
        /// <summary>
        /// When the message was sent
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the message has been read by the recipient
        /// </summary>
        public bool IsRead { get; set; }
    }
    
    /// <summary>
    /// Simplified model for sending new messages
    /// </summary>
    public class NewChatMessage
    {
        /// <summary>
        /// The ticket this message belongs to
        /// </summary>
        public int TicketId { get; set; }
        
        /// <summary>
        /// Actual message content
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
} 