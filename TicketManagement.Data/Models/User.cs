using System;
using System.Collections.Generic;

namespace TicketManagement.Data.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }

        // Navigation property for tickets
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
} 