using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManagement.Api.DTOs;
using TicketManagement.Data.Models;
using TicketManagement.Data.Repositories;
using Serilog;

namespace TicketManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        
        public MessageController(
            IMessageRepository messageRepository, 
            ITicketRepository ticketRepository,
            IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
        }
        
        [HttpGet("ticket/{ticketId}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesByTicketId(int ticketId)
        {
            Log.Information("Getting messages for ticket {TicketId}", ticketId);
            
            var ticket = await _ticketRepository.GetTicketByIdAsync(ticketId);
            if (ticket == null)
            {
                Log.Warning("Ticket {TicketId} not found", ticketId);
                return NotFound("Ticket not found");
            }
            
            var messages = await _messageRepository.GetMessagesByTicketIdAsync(ticketId);
            Log.Information("Found {Count} messages for ticket {TicketId}", messages.Count(), ticketId);
            
            // Map messages to DTOs with enriched sender information
            var messageDtos = new List<MessageDto>();
            foreach (var message in messages)
            {
                var dto = await EnhanceMessageWithSenderInfo(message);
                messageDtos.Add(dto);
            }
            
            return Ok(messageDtos);
        }
        
        // Helper method to enhance a message with sender information
        private async Task<MessageDto> EnhanceMessageWithSenderInfo(Message message)
        {
            var dto = MapToMessageDto(message);
            
            // If sender info is missing, fetch it directly
            if (string.IsNullOrEmpty(dto.SenderName))
            {
                var sender = await _userRepository.GetUserByIdAsync(message.SenderId);
                if (sender != null)
                {
                    dto.SenderName = sender.Name;
                    dto.IsSenderAdmin = sender.IsAdmin;
                }
                else
                {
                    // Use fallback values if user can't be found
                    dto.SenderName = "Unknown User";
                    dto.IsSenderAdmin = false;
                }
            }
            
            return dto;
        }
        
        [HttpPost]
        public async Task<ActionResult<MessageResponseDto>> CreateMessage(CreateMessageDto createMessageDto, [FromQuery] int userId)
        {
            try
            {
                Log.Information("Creating message for ticket {TicketId} from user {UserId}", createMessageDto.TicketId, userId);
                
                var ticket = await _ticketRepository.GetTicketByIdAsync(createMessageDto.TicketId);
                if (ticket == null)
                {
                    Log.Warning("Failed to create message: Ticket {TicketId} not found", createMessageDto.TicketId);
                    return NotFound(new MessageResponseDto { Success = false, Message = "Ticket not found" });
                }
                
                if (ticket.Status == TicketStatus.Closed)
                {
                    Log.Warning("Attempted to send message to closed ticket {TicketId}", createMessageDto.TicketId);
                    return BadRequest(new MessageResponseDto { Success = false, Message = "Cannot send messages to closed tickets" });
                }
                
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    Log.Warning("Failed to create message: User {UserId} not found", userId);
                    return NotFound(new MessageResponseDto { Success = false, Message = "User not found" });
                }
                
                // Check if the user is allowed to send messages for this ticket
                // Only the ticket owner or admins can send messages
                if (!user.IsAdmin && ticket.UserId != userId)
                {
                    Log.Warning("User {UserId} attempted to send message to ticket {TicketId} without permission", userId, createMessageDto.TicketId);
                    return Forbid();
                }
                
                var message = new Message
                {
                    TicketId = createMessageDto.TicketId,
                    SenderId = userId,
                    Content = createMessageDto.Content,
                    SentAt = DateTime.Now,
                    IsRead = false
                };
                
                // Save message to database
                var createdMessage = await _messageRepository.CreateMessageAsync(message);
                
                // Map to DTO for response with enhanced sender info
                var messageDto = await EnhanceMessageWithSenderInfo(createdMessage);
                
                Log.Information("Message {MessageId} created successfully for ticket {TicketId}", messageDto.MessageId, createMessageDto.TicketId);
                
                return Ok(new MessageResponseDto
                {
                    Success = true,
                    Message = "Message sent successfully",
                    MessageData = messageDto
                });
            }
            catch (Exception ex)
            {
                Log.Error("Error creating message: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    Log.Error("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                }
                
                return StatusCode(500, new MessageResponseDto
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                });
            }
        }
        
        [HttpPost("{messageId}/read")]
        public async Task<ActionResult<MessageResponseDto>> MarkAsRead(int messageId)
        {
            Log.Information("Marking message {MessageId} as read", messageId);
            
            var message = await _messageRepository.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                Log.Warning("Message {MessageId} not found", messageId);
                return NotFound(new MessageResponseDto { Success = false, Message = "Message not found" });
            }
            
            var success = await _messageRepository.MarkMessageAsReadAsync(messageId);
            if (!success)
            {
                Log.Warning("Failed to mark message {MessageId} as read", messageId);
                return BadRequest(new MessageResponseDto { Success = false, Message = "Failed to mark message as read" });
            }
            
            // Get the updated message with sender information
            var messageDto = await EnhanceMessageWithSenderInfo(message);
            messageDto.IsRead = true;
            
            Log.Information("Message {MessageId} marked as read successfully", messageId);
            
            return Ok(new MessageResponseDto
            {
                Success = true,
                Message = "Message marked as read",
                MessageData = messageDto
            });
        }
        
        [HttpGet("unread/{ticketId}/{userId}")]
        public async Task<ActionResult<int>> GetUnreadMessageCount(int ticketId, int userId)
        {
            Log.Information("Getting unread message count for ticket {TicketId} and user {UserId}", ticketId, userId);
            var count = await _messageRepository.GetUnreadMessageCountAsync(ticketId, userId);
            Log.Information("Found {Count} unread messages for ticket {TicketId} and user {UserId}", count, ticketId, userId);
            return Ok(count);
        }
        
        [HttpDelete("{messageId}")]
        public async Task<ActionResult<MessageResponseDto>> DeleteMessage(int messageId, [FromQuery] int userId)
        {
            Log.Information("Deleting message {MessageId} requested by user {UserId}", messageId, userId);
            
            var message = await _messageRepository.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                Log.Warning("Message {MessageId} not found", messageId);
                return NotFound(new MessageResponseDto { Success = false, Message = "Message not found" });
            }
            
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                Log.Warning("User {UserId} not found", userId);
                return NotFound(new MessageResponseDto { Success = false, Message = "User not found" });
            }
            
            // Only the sender or an admin can delete a message
            if (message.SenderId != userId && !user.IsAdmin)
            {
                Log.Warning("User {UserId} attempted to delete message {MessageId} without permission", userId, messageId);
                return Forbid();
            }
            
            var success = await _messageRepository.DeleteMessageAsync(messageId);
            if (!success)
            {
                Log.Warning("Failed to delete message {MessageId}", messageId);
                return BadRequest(new MessageResponseDto { Success = false, Message = "Failed to delete message" });
            }
            
            Log.Information("Message {MessageId} deleted successfully", messageId);
            
            return Ok(new MessageResponseDto
            {
                Success = true,
                Message = "Message deleted successfully"
            });
        }
        
        private MessageDto MapToMessageDto(Message message)
        {
            return new MessageDto
            {
                MessageId = message.MessageId,
                TicketId = message.TicketId,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsRead = message.IsRead
            };
        }
    }
}