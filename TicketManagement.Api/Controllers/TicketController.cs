using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Api.DTOs;
using TicketManagement.Data.Models;
using TicketManagement.Data.Repositories;
using System.Text.Json;
using Serilog;

namespace TicketManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;

        public TicketController(ITicketRepository ticketRepository, IUserRepository userRepository)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetAllTickets()
        {
            Log.Information("Retrieving all tickets");
            var tickets = await _ticketRepository.GetAllTicketsAsync();
            Log.Information("Retrieved {Count} tickets", tickets.Count());
            
            return Ok(tickets.Select(MapToTicketDto));
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetTicketsByUserId(int userId)
        {
            Log.Information("Retrieving tickets for user {UserId}", userId);
            
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                Log.Warning("User {UserId} not found when retrieving tickets", userId);
                return NotFound("User not found");
            }

            var tickets = await _ticketRepository.GetTicketsByUserIdAsync(userId);
            Log.Information("Retrieved {Count} tickets for user {UserId}", tickets.Count(), userId);
            
            return Ok(tickets.Select(MapToTicketDto));
        }

        [HttpGet("open/user/{userId}")]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetOpenTicketsByUserId(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var tickets = await _ticketRepository.GetOpenTicketsByUserIdAsync(userId);
            
            return Ok(tickets.Select(MapToTicketDto));
        }

        [HttpGet("closed/user/{userId}")]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetClosedTicketsByUserId(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            var tickets = await _ticketRepository.GetClosedTicketsByUserIdAsync(userId);
            
            return Ok(tickets.Select(MapToTicketDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicketById(int id)
        {
            var ticket = await _ticketRepository.GetTicketByIdAsync(id);
            
            if (ticket == null)
                return NotFound("Ticket not found");

            return Ok(MapToTicketDto(ticket));
        }

        [HttpPost("create")]
        public async Task<ActionResult<TicketResponseDto>> CreateTicket(CreateTicketDto createTicketDto, [FromQuery] int userId)
        {
            try
            {
                Log.Information("Creating new ticket for user {UserId}. Subject: {Subject}", userId, createTicketDto.Subject);
                
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    Log.Warning("Failed to create ticket: User {UserId} not found", userId);
                    return NotFound(new TicketResponseDto { Success = false, Message = "User not found" });
                }

                var ticket = new Ticket
                {
                    UserId = userId,
                    Subject = createTicketDto.Subject,
                    Description = createTicketDto.Description,
                    Priority = createTicketDto.Priority,
                    Category = createTicketDto.Category ?? string.Empty,
                    SubCategory = createTicketDto.SubCategory ?? string.Empty,
                    CreatedAt = DateTime.Now,
                    Status = TicketStatus.Open,
                    AdminResponse = string.Empty // Ensure this is not null
                };

                Console.WriteLine($"Creating ticket in database with Priority: {ticket.Priority}");
                try {
                    var createdTicket = await _ticketRepository.CreateTicketAsync(ticket);
                    Console.WriteLine($"Ticket created successfully with ID: {createdTicket.TicketId}");

                    return Ok(new TicketResponseDto
                    {
                        Success = true,
                        Message = "Ticket created successfully",
                        Ticket = MapToTicketDto(createdTicket)
                    });
                }
                catch (Exception dbEx) {
                    Console.WriteLine($"Database error: {dbEx.Message}");
                    if (dbEx.InnerException != null) {
                        Console.WriteLine($"Inner exception details: {dbEx.InnerException.Message}");
                        Console.WriteLine($"Inner exception source: {dbEx.InnerException.Source}");
                        Console.WriteLine($"Inner exception stack trace: {dbEx.InnerException.StackTrace}");
                    }
                    throw; // Rethrow to outer catch
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating ticket for user {UserId}", userId);
                return StatusCode(500, new TicketResponseDto { Success = false, Message = "An error occurred while creating the ticket" });
            }
        }

        [HttpPost("{id}/respond")]
        public async Task<ActionResult<TicketResponseDto>> AddAdminResponse(int id, AdminResponseDto adminResponseDto)
        {
            try
            {
                Console.WriteLine($"Received admin response request for ticket ID: {id}");
                Console.WriteLine($"Response text: {adminResponseDto?.Response}");
                
                if (adminResponseDto == null || string.IsNullOrEmpty(adminResponseDto.Response))
                {
                    Console.WriteLine("Admin response is null or empty");
                    return BadRequest(new TicketResponseDto { Success = false, Message = "Response cannot be empty" });
                }

                var ticket = await _ticketRepository.GetTicketByIdAsync(id);
                if (ticket == null)
                {
                    Console.WriteLine($"Ticket with ID {id} not found");
                    return NotFound(new TicketResponseDto { Success = false, Message = "Ticket not found" });
                }

                Console.WriteLine("Adding admin response to database...");
                var success = await _ticketRepository.AddAdminResponseAsync(id, adminResponseDto.Response);
                
                if (!success)
                {
                    Console.WriteLine("Failed to add admin response");
                    return BadRequest(new TicketResponseDto { Success = false, Message = "Failed to add response" });
                }

                // Get updated ticket
                Console.WriteLine("Getting updated ticket...");
                ticket = await _ticketRepository.GetTicketByIdAsync(id);
                Console.WriteLine($"Updated ticket retrieved. AdminResponse: {ticket.AdminResponse}");

                var response = new TicketResponseDto
                {
                    Success = true,
                    Message = "Response added successfully",
                    Ticket = MapToTicketDto(ticket)
                };
                
                Console.WriteLine("Returning successful response");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding admin response: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new TicketResponseDto
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                });
            }
        }

        [HttpPost("{id}/close")]
        public async Task<ActionResult<TicketResponseDto>> CloseTicket(int id)
        {
            Log.Information("Attempting to close ticket {TicketId}", id);
            
            var ticket = await _ticketRepository.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                Log.Warning("Failed to close ticket: Ticket {TicketId} not found", id);
                return NotFound(new TicketResponseDto { Success = false, Message = "Ticket not found" });
            }

            var success = await _ticketRepository.CloseTicketAsync(id);
            if (!success)
            {
                Log.Error("Failed to close ticket {TicketId}", id);
                return BadRequest(new TicketResponseDto { Success = false, Message = "Failed to close ticket" });
            }

            // Get updated ticket
            ticket = await _ticketRepository.GetTicketByIdAsync(id);
            Log.Information("Successfully closed ticket {TicketId}", id);

            return Ok(new TicketResponseDto
            {
                Success = true,
                Message = "Ticket closed successfully",
                Ticket = MapToTicketDto(ticket)
            });
        }

        [HttpPost("{id}/reopen")]
        public async Task<ActionResult<TicketResponseDto>> ReopenTicket(int id)
        {
            try
            {
                Log.Information("Attempting to reopen ticket {TicketId}", id);
                
                var ticket = await _ticketRepository.GetTicketByIdAsync(id);
                if (ticket == null)
                {
                    Log.Warning("Failed to reopen ticket: Ticket {TicketId} not found", id);
                    return NotFound(new TicketResponseDto { Success = false, Message = "Ticket not found" });
                }

                if (ticket.Status == TicketStatus.Open)
                {
                    Log.Warning("Cannot reopen ticket {TicketId}: Ticket is already open", id);
                    return BadRequest(new TicketResponseDto { Success = false, Message = "Ticket is already open" });
                }

                Log.Debug("Reopening ticket {TicketId} in the database", id);
                var success = await _ticketRepository.ReopenTicketAsync(id);
                
                if (!success)
                {
                    Log.Error("Failed to reopen ticket {TicketId} in database", id);
                    return BadRequest(new TicketResponseDto { Success = false, Message = "Failed to reopen ticket" });
                }

                // Get updated ticket
                Log.Debug("Retrieving updated ticket {TicketId}", id);
                ticket = await _ticketRepository.GetTicketByIdAsync(id);

                Log.Information("Successfully reopened ticket {TicketId}", id);
                var response = new TicketResponseDto
                {
                    Success = true,
                    Message = "Ticket reopened successfully",
                    Ticket = MapToTicketDto(ticket)
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while reopening ticket {TicketId}", id);
                return StatusCode(500, new TicketResponseDto 
                { 
                    Success = false, 
                    Message = $"An error occurred: {ex.Message}" 
                });
            }
        }

        private TicketDto MapToTicketDto(Ticket ticket)
        {
            Log.Debug("Mapping ticket {TicketId} to DTO", ticket.TicketId);
            return new TicketDto
            {
                TicketId = ticket.TicketId,
                UserId = ticket.UserId,
                UserName = ticket.User?.Name ?? "Unknown User",
                Subject = ticket.Subject,
                Description = ticket.Description ?? string.Empty,
                Status = ticket.Status.ToString(),
                CreatedAt = ticket.CreatedAt,
                ClosedAt = ticket.ClosedAt,
                AdminResponse = ticket.AdminResponse ?? string.Empty,
                Priority = ticket.Priority.ToString(),
                Category = ticket.Category,
                SubCategory = ticket.SubCategory
            };
        }
    }
}