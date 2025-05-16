using System;
using System.Text.Json.Serialization;

namespace TicketManagement.Web.Models
{
    /// <summary>
    /// Data transfer object for messages sent between client and server
    /// </summary>
    public class MessageDto
    {
        /// <summary>
        /// Unique identifier for the message
        /// </summary>
        public int MessageId { get; set; }
        
        /// <summary>
        /// The ticket this message belongs to
        /// </summary>
        public int TicketId { get; set; }
        
        /// <summary>
        /// User ID of the message sender
        /// </summary>
        public int SenderId { get; set; }
        
        /// <summary>
        /// Name of the sender
        /// </summary>
        public string SenderName { get; set; } = string.Empty;
        
        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the sender is an admin
        /// </summary>
        public bool IsSenderAdmin { get; set; }
        
        /// <summary>
        /// When the message was sent
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the message has been read
        /// </summary>
        public bool IsRead { get; set; }
    }
} 