using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Web.Models;
using TicketManagement.Web.Services;

namespace TicketManagement.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;

        public UserController(IApiService apiService, IAuthService authService)
        {
            _apiService = apiService;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (_authService.IsAdmin())
                return RedirectToAction("Index", "Admin");

            // Get current user and their tickets
            var userId = _authService.GetCurrentUserId();
            var user = _authService.GetCurrentUser();
            var tickets = await _apiService.GetTicketsByUserIdAsync(userId);

            var model = new UserDashboardViewModel
            {
                UserName = user.Name,
                TotalTickets = tickets.Count(),
                OpenTickets = tickets.Count(t => t.Status == "Open"),
                ClosedTickets = tickets.Count(t => t.Status == "Closed")
            };

            // Category summary
            var categories = tickets
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .GroupBy(t => t.Category)
                .Select(g => new CategoryTicketsSummary
                {
                    Category = g.Key,
                    TicketCount = g.Count()
                })
                .OrderByDescending(c => c.TicketCount)
                .Take(5)
                .ToList();
            
            model.CategorySummary = categories;

            // Priority summary
            var priorities = tickets
                .GroupBy(t => t.Priority)
                .Select(g => new PriorityTicketsSummary
                {
                    Priority = g.Key,
                    TicketCount = g.Count()
                })
                .OrderByDescending(p => p.TicketCount)
                .ToList();
            
            model.PrioritySummary = priorities;

            // Monthly summary (last 6 months)
            var lastSixMonths = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Select(date => new { Month = date.ToString("MMM yyyy"), Date = date })
                .ToList();
            
            foreach (var month in lastSixMonths)
            {
                var firstDayOfMonth = new DateTime(month.Date.Year, month.Date.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                
                var monthTickets = tickets.Where(t => 
                    t.CreatedAt >= firstDayOfMonth && 
                    t.CreatedAt <= lastDayOfMonth);
                
                var monthlySummary = new MonthlyTicketsSummary
                {
                    Month = month.Month,
                    OpenedTickets = monthTickets.Count(),
                    ClosedTickets = monthTickets.Count(t => t.Status == "Closed")
                };
                
                model.MonthlyTickets.Add(monthlySummary);
            }
            
            // Reverse to show in chronological order
            model.MonthlyTickets = model.MonthlyTickets.OrderBy(m => DateTime.Parse(m.Month)).ToList();
            
            // Recent tickets (5 most recent)
            model.RecentTickets = tickets
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateTicket()
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (_authService.IsAdmin())
                return RedirectToAction("Index", "Admin");

            return View(new CreateTicketViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket(CreateTicketViewModel model)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            // Log model validation state
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }
            else
            {
                try 
                {
                    var userId = _authService.GetCurrentUserId();
                    Console.WriteLine($"Creating ticket for user ID: {userId}");
                    
                    var ticket = await _apiService.CreateTicketAsync(model, userId);
                    
                    if (ticket != null)
                    {
                        Console.WriteLine("Ticket created successfully");
                        return RedirectToAction("OpenTickets");
                    }
                    
                    Console.WriteLine("Failed to create ticket - API returned null");
                    ModelState.AddModelError("", "Failed to create ticket. Please try again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in CreateTicket: {ex.Message}");
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OpenTickets()
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            var userId = _authService.GetCurrentUserId();
            var tickets = await _apiService.GetOpenTicketsByUserIdAsync(userId);
            
            var model = new TicketListViewModel
            {
                Tickets = tickets,
                ListType = "Open"
            };
            
            return View("TicketList", model);
        }

        [HttpGet]
        public async Task<IActionResult> ClosedTickets()
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            var userId = _authService.GetCurrentUserId();
            var tickets = await _apiService.GetClosedTicketsByUserIdAsync(userId);
            
            var model = new TicketListViewModel
            {
                Tickets = tickets,
                ListType = "Closed"
            };
            
            return View("TicketList", model);
        }

        [HttpGet]
        public async Task<IActionResult> ViewTicket(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            var ticket = await _apiService.GetTicketByIdAsync(id);
            
            if (ticket == null)
                return NotFound();

            var userId = _authService.GetCurrentUserId();
            
            // Only allow viewing tickets that belong to the user (unless admin)
            if (ticket.UserId != userId && !_authService.IsAdmin())
                return Forbid();
            
            // Get messages for this ticket
            var messages = await _apiService.GetMessagesByTicketIdAsync(id);
            
            // Update view model to include messages
            ViewBag.Messages = messages;
            ViewBag.CurrentUserId = userId;
            ViewBag.IsAdmin = _authService.IsAdmin();
            
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> CloseTicket(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            var ticket = await _apiService.GetTicketByIdAsync(id);
            
            if (ticket == null)
                return NotFound();

            var userId = _authService.GetCurrentUserId();
            
            // Only allow closing tickets that belong to the user (unless admin)
            if (ticket.UserId != userId && !_authService.IsAdmin())
                return Forbid();
            
            var result = await _apiService.CloseTicketAsync(id);
            
            if (result != null)
            {
                TempData["SuccessMessage"] = "Ticket closed successfully.";
                return RedirectToAction("ViewTicket", new { id });
            }
            
            TempData["ErrorMessage"] = "Failed to close ticket. Please try again.";
            return RedirectToAction("ViewTicket", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ReopenTicket(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            var ticket = await _apiService.GetTicketByIdAsync(id);
            
            if (ticket == null)
                return NotFound();

            var userId = _authService.GetCurrentUserId();
            
            // Only allow reopening tickets that belong to the user (unless admin)
            if (ticket.UserId != userId && !_authService.IsAdmin())
                return Forbid();
            
            var result = await _apiService.ReopenTicketAsync(id);
            
            if (result != null)
            {
                TempData["SuccessMessage"] = "Ticket reopened successfully.";
                return RedirectToAction("ViewTicket", new { id });
            }
            
            TempData["ErrorMessage"] = "Failed to reopen ticket. Please try again.";
            return RedirectToAction("ViewTicket", new { id });
        }

        // Add a new controller action for sending messages
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageViewModel model)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");
            
            var userId = _authService.GetCurrentUserId();
            
            // Verify the ticket exists and user has access
            var ticket = await _apiService.GetTicketByIdAsync(model.TicketId);
            if (ticket == null)
                return NotFound();
                
            // Only ticket owner or admin can send messages
            if (ticket.UserId != userId && !_authService.IsAdmin())
                return Forbid();
            
            // Create the message
            var message = await _apiService.CreateMessageAsync(model, userId);
            
            if (message == null)
            {
                return BadRequest(new { success = false, message = "Failed to send message. Please try again." });
            }
            
            return Ok(new { success = true, message = "Message sent successfully" });
        }
        
        // Add endpoint for fallback polling when SignalR fails (especially for admin users)
        [HttpGet]
        public async Task<IActionResult> GetNewMessages(int ticketId, int lastMessageId = 0)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");
            
            var userId = _authService.GetCurrentUserId();
            
            // Verify the ticket exists and user has access
            var ticket = await _apiService.GetTicketByIdAsync(ticketId);
            if (ticket == null)
                return NotFound();
                
            // Only ticket owner or admin can get messages
            if (ticket.UserId != userId && !_authService.IsAdmin())
                return Forbid();
            
            // Get messages newer than lastMessageId
            var allMessages = await _apiService.GetMessagesByTicketIdAsync(ticketId);
            var newMessages = allMessages.Where(m => m.MessageId > lastMessageId).ToList();
            
            // Add the isAdmin flag to the response for client-side filtering
            bool isAdmin = _authService.IsAdmin();
            
            return Json(new { 
                success = true, 
                messages = newMessages,
                isAdmin = isAdmin,
                currentUserId = userId
            });
        }

        // Add endpoint to get all messages for a ticket
        [HttpGet]
        public async Task<IActionResult> GetTicketMessages(int id)
        {
            if (!_authService.IsAuthenticated())
                return Unauthorized();
            
            var userId = _authService.GetCurrentUserId();
            
            // Verify the ticket exists and user has access
            var ticket = await _apiService.GetTicketByIdAsync(id);
            if (ticket == null)
                return NotFound(new { success = false, message = "Ticket not found" });
                
            // Only ticket owner or admin can get messages
            if (ticket.UserId != userId && !_authService.IsAdmin())
                return Forbid();
            
            // Get all messages for the ticket
            var messages = await _apiService.GetMessagesByTicketIdAsync(id);
            
            return Ok(messages);
        }
    }
} 