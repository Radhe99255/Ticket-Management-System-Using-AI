using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketManagement.Data.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        
        [ForeignKey("Ticket")]
        public int TicketId { get; set; }
        
        [ForeignKey("Sender")]
        public int SenderId { get; set; }
        
        public string Content { get; set; }
        
        public DateTime SentAt { get; set; } = DateTime.Now;
        
        public bool IsRead { get; set; } = false;
        
        // Navigation properties
        public Ticket Ticket { get; set; }
        public User Sender { get; set; }
    }
} 