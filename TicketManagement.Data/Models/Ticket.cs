using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketManagement.Data.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        
        [ForeignKey("User")]
        public int UserId { get; set; }
        
        public string Subject { get; set; }
        public string Description { get; set; }
        
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ClosedAt { get; set; }
        
        public string AdminResponse { get; set; }
        
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public string Category { get; set; }
        public string SubCategory { get; set; }
        
        // Navigation property
        public User User { get; set; }
        
        // Chat messages
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
    
    public enum TicketStatus
    {
        Open,
        Closed
    }
    
    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
} 