using System;
using System.ComponentModel.DataAnnotations;
using TicketManagement.Data.Models;

namespace TicketManagement.Api.DTOs
{
    public class TicketDto
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

    public class CreateTicketDto
    {
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Subject { get; set; }

        [Required]
        public string Description { get; set; }

        [Range(0, 3)] // Ensure this matches the enum values (Low=0, Medium=1, High=2, Critical=3)
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        [StringLength(100)]
        public string Category { get; set; }

        [StringLength(100)]
        public string SubCategory { get; set; }
    }

    public class AdminResponseDto
    {
        [Required(ErrorMessage = "Response text is required")]
        [MinLength(2, ErrorMessage = "Response must be at least 2 characters")]
        public string Response { get; set; } = string.Empty;
    }
    
    public class TicketResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public TicketDto Ticket { get; set; }
    }
} 