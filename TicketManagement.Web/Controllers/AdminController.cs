using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Web.Models;
using TicketManagement.Web.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace TicketManagement.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IApiService apiService, IAuthService authService, ILogger<AdminController> logger)
        {
            _apiService = apiService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            var users = await _apiService.GetAllUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> UserTickets(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            var user = await _apiService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            var tickets = await _apiService.GetTicketsByUserIdAsync(id);
            
            ViewBag.User = user;
            
            var model = new TicketListViewModel
            {
                Tickets = tickets,
                ListType = "All"
            };
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ViewTicket(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            var ticket = await _apiService.GetTicketByIdAsync(id);
            
            if (ticket == null)
                return NotFound();
            
            // Get messages for this ticket
            var messages = await _apiService.GetMessagesByTicketIdAsync(id);
            
            // Update view model to include messages
            ViewBag.Messages = messages;
            ViewBag.CurrentUserId = _authService.GetCurrentUserId();
            
            return View(ticket);
        }

        [HttpGet]
        public async Task<IActionResult> RespondToTicket(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            var ticket = await _apiService.GetTicketByIdAsync(id);
            
            if (ticket == null)
                return NotFound();
            
            var model = new RespondTicketViewModel
            {
                TicketId = ticket.TicketId,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                AdminResponse = ticket.AdminResponse
            };
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RespondToTicket(RespondTicketViewModel model)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            // Add logging to see what's happening
            Console.WriteLine($"Responding to ticket ID: {model.TicketId}");
            Console.WriteLine($"Admin response text: {model.AdminResponse}");

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _apiService.RespondToTicketAsync(model.TicketId, model.AdminResponse);
                    
                    if (result != null)
                    {
                        Console.WriteLine("Response added successfully");
                        return RedirectToAction("ViewTicket", new { id = model.TicketId });
                    }
                    
                    Console.WriteLine("Failed to add response - API returned null");
                    ModelState.AddModelError("", "Failed to respond to ticket. Please try again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in RespondToTicket: {ex.Message}");
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }
            
            // If we get here, there was an error, so we need to refetch the ticket data
            // to make sure the view has all the required information
            var ticket = await _apiService.GetTicketByIdAsync(model.TicketId);
            if (ticket != null)
            {
                // Preserve the user's response text while refreshing the other ticket data
                model.Subject = ticket.Subject;
                model.Description = ticket.Description;
                model.Status = ticket.Status;
                model.CreatedAt = ticket.CreatedAt;
                // Keep the AdminResponse from the model as it contains the user's input
            }
            else
            {
                // If the ticket can't be retrieved, at least ensure we're not displaying default values
                if (string.IsNullOrEmpty(model.Subject))
                    model.Subject = "[Ticket details unavailable]";
                if (string.IsNullOrEmpty(model.Description))
                    model.Description = "[Description unavailable]";
                if (string.IsNullOrEmpty(model.Status))
                    model.Status = "Unknown";
                if (model.CreatedAt == default)
                    model.CreatedAt = DateTime.Now;
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CloseTicket(int id)
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            var result = await _apiService.CloseTicketAsync(id);
            
            if (result != null)
            {
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

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            var result = await _apiService.ReopenTicketAsync(id);
            
            if (result != null)
            {
                TempData["SuccessMessage"] = "Ticket reopened successfully.";
                return RedirectToAction("ViewTicket", new { id });
            }
            
            TempData["ErrorMessage"] = "Failed to reopen ticket. Please try again.";
            return RedirectToAction("ViewTicket", new { id });
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!_authService.IsAuthenticated())
                return RedirectToAction("Login", "Account");

            if (!_authService.IsAdmin())
                return RedirectToAction("Index", "User");

            // Get all users and tickets for dashboard metrics
            var users = await _apiService.GetAllUsersAsync();
            var tickets = await _apiService.GetAllTicketsAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = users.Count(),
                TotalTickets = tickets.Count(),
                OpenTickets = tickets.Count(t => t.Status == "Open"),
                ClosedTickets = tickets.Count(t => t.Status == "Closed")
            };

            // User tickets summary
            foreach (var user in users)
            {
                var userTickets = tickets.Where(t => t.UserId == user.UserId).ToList();
                
                if (userTickets.Any())
                {
                    model.UserTicketsSummary.Add(new UserTicketsSummary
                    {
                        UserName = user.Name,
                        TotalTickets = userTickets.Count,
                        OpenTickets = userTickets.Count(t => t.Status == "Open"),
                        ClosedTickets = userTickets.Count(t => t.Status == "Closed")
                    });
                }
            }

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

            return View(model);
        }

        // Add a new controller action for sending messages
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageViewModel model)
        {
            if (!_authService.IsAuthenticated())
                return Unauthorized();
                
            if (!_authService.IsAdmin())
                return Forbid();
            
            var userId = _authService.GetCurrentUserId();
            
            try
            {
                // Create the message through the API
                var message = await _apiService.CreateMessageAsync(model, userId);
                
                if (message == null)
                {
                    return BadRequest(new { success = false, message = "Failed to send message. Please try again." });
                }
                
                // Return success - SignalR hub will handle broadcasting the message
                return Ok(new { success = true, message = "Message sent successfully", data = message });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while sending the message." });
            }
        }
        
        // Add an endpoint to get ticket messages for polling fallback
        [HttpGet]
        public async Task<IActionResult> GetTicketMessages(int id)
        {
            if (!_authService.IsAuthenticated() || !_authService.IsAdmin())
                return Unauthorized();
                
            try
            {
                var messages = await _apiService.GetMessagesByTicketIdAsync(id);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetTicketMessages: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving messages." });
            }
        }
    }
} 