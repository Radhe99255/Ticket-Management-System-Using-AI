using System.Collections.Generic;
using System;

namespace TicketManagement.Web.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
        public List<UserTicketsSummary> UserTicketsSummary { get; set; } = new List<UserTicketsSummary>();
        public List<CategoryTicketsSummary> CategorySummary { get; set; } = new List<CategoryTicketsSummary>();
        public List<MonthlyTicketsSummary> MonthlyTickets { get; set; } = new List<MonthlyTicketsSummary>();
        public List<PriorityTicketsSummary> PrioritySummary { get; set; } = new List<PriorityTicketsSummary>();
    }

    public class UserDashboardViewModel
    {
        public string UserName { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
        public double CompletionRate => TotalTickets > 0 ? Math.Round((double)ClosedTickets / TotalTickets * 100, 1) : 0;
        public List<CategoryTicketsSummary> CategorySummary { get; set; } = new List<CategoryTicketsSummary>();
        public List<MonthlyTicketsSummary> MonthlyTickets { get; set; } = new List<MonthlyTicketsSummary>();
        public List<PriorityTicketsSummary> PrioritySummary { get; set; } = new List<PriorityTicketsSummary>();
        public List<TicketViewModel> RecentTickets { get; set; } = new List<TicketViewModel>();
    }

    public class UserTicketsSummary
    {
        public string UserName { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }
    }

    public class CategoryTicketsSummary
    {
        public string Category { get; set; }
        public int TicketCount { get; set; }
    }

    public class MonthlyTicketsSummary
    {
        public string Month { get; set; }
        public int OpenedTickets { get; set; }
        public int ClosedTickets { get; set; }
    }

    public class PriorityTicketsSummary
    {
        public string Priority { get; set; }
        public int TicketCount { get; set; }
    }
} 