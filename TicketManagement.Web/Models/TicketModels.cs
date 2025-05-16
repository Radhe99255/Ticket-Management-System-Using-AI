using System;
using System.ComponentModel.DataAnnotations;
using TicketManagement.Data.Models;

namespace TicketManagement.Web.Models
{
    public class TicketViewModel
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string AdminResponse { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
    }

    public class CreateTicketViewModel
    {
        [Required]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Subject must be between 5 and 200 characters")]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        [StringLength(100)]
        public string Category { get; set; }

        [StringLength(100)]
        public string SubCategory { get; set; }
    }

    public class RespondTicketViewModel
    {
        [Required]
        public int TicketId { get; set; }
        
        [Required]
        public string Subject { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public string Status { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        [Required(ErrorMessage = "Response is required")]
        [MinLength(2, ErrorMessage = "Response must be at least 2 characters")]
        public string AdminResponse { get; set; }
    }

    public class TicketListViewModel
    {
        public IEnumerable<TicketViewModel> Tickets { get; set; }
        public string ListType { get; set; } // "Open", "Closed", "All"
    }
} 