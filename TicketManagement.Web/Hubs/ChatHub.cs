using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TicketManagement.Web.Models;
using TicketManagement.Web.Services;

namespace TicketManagement.Web.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time chat functionality
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IApiService _apiService;

        public ChatHub(ILogger<ChatHub> logger, IApiService apiService)
        {
            _logger = logger;
            _apiService = apiService;
        }

        /// <summary>
        /// Handle sending a message to all clients connected to a ticket group
        /// </summary>
        public async Task SendMessage(ChatMessage message)
        {
            try
            {
                _logger.LogInformation($"Handling message for ticket #{message.TicketId} from user {message.SenderId}");
                
                // Validate message
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }
                
                if (message.TicketId <= 0)
                {
                    throw new ArgumentException("TicketId must be positive", nameof(message.TicketId));
                }
                
                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    throw new ArgumentException("Message content cannot be empty", nameof(message.Content));
                }
                
                // Format sender name - add admin label if needed
                if (message.IsAdmin && !string.IsNullOrEmpty(message.SenderName))
                {
                    // Remove any existing admin labels
                    var cleanName = message.SenderName.Replace(" (Admin)", "").Trim();
                    
                    // Add a single admin label
                    message.SenderName = $"{cleanName} (Admin)";
                }
                
                // Get the sender's connection ID for tracking
                string connectionId = Context.ConnectionId;
                _logger.LogInformation($"Sender connection ID: {connectionId}");
                
                // Save message to database
                var savedMessage = await SaveMessageToDatabase(message);
                
                if (savedMessage != null)
                {
                    // Broadcast the saved message to ALL clients in the group (including sender)
                    // This ensures everyone sees the exact same database-saved message
                    await Clients.Group($"ticket-{message.TicketId}")
                        .SendAsync("ReceiveMessage", savedMessage);
                    
                    _logger.LogInformation($"Message broadcast completed for ticket-{message.TicketId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Join a ticket's chat group to receive its messages
        /// </summary>
        public async Task JoinTicketGroup(int ticketId)
        {
            try
            {
                if (ticketId <= 0)
                {
                    throw new ArgumentException("TicketId must be positive", nameof(ticketId));
                }
                
                _logger.LogInformation($"Client {Context.ConnectionId} joining ticket-{ticketId} group");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
                await Clients.Caller.SendAsync("JoinedGroup", ticketId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in JoinTicketGroup: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Leave a ticket's chat group
        /// </summary>
        public async Task LeaveTicketGroup(int ticketId)
        {
            try
            {
                if (ticketId <= 0)
                {
                    throw new ArgumentException("TicketId must be positive", nameof(ticketId));
                }
                
                _logger.LogInformation($"Client {Context.ConnectionId} leaving ticket-{ticketId} group");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in LeaveTicketGroup: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Ping method to verify connection is active
        /// </summary>
        public async Task Ping()
        {
            try
            {
                _logger.LogDebug($"Ping received from client {Context.ConnectionId}");
                await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Ping: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Mark a message as read
        /// </summary>
        public async Task MarkMessageAsRead(int messageId)
        {
            try
            {
                if (messageId <= 0)
                {
                    throw new ArgumentException("MessageId must be positive", nameof(messageId));
                }
                
                var success = await _apiService.MarkMessageAsReadAsync(messageId);
                if (success)
                {
                    _logger.LogInformation($"Message {messageId} marked as read");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in MarkMessageAsRead: {ex.Message}");
                throw;
            }
        }
        
        // Handle connection events
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            string clientIp = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            
            _logger.LogInformation($"Client connected: {Context.ConnectionId} from {clientIp}");
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            if (exception != null)
            {
                _logger.LogError($"Disconnection error: {exception.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }
        
        // Helper method to save messages to the database
        private async Task<ChatMessage> SaveMessageToDatabase(ChatMessage message)
        {
            try
            {
                // Convert to API model
                var createMessageViewModel = new CreateMessageViewModel
                {
                    TicketId = message.TicketId,
                    Content = message.Content
                };
                
                // Save via API service
                var savedMessage = await _apiService.CreateMessageAsync(createMessageViewModel, message.SenderId);
                
                if (savedMessage == null)
                {
                    _logger.LogWarning("Failed to save message to database");
                    return null;
                }
                
                // Convert to a ChatMessage for consistency
                return new ChatMessage
                {
                    Id = savedMessage.MessageId,
                    TicketId = savedMessage.TicketId,
                    SenderId = savedMessage.SenderId,
                    SenderName = savedMessage.SenderName,
                    Content = savedMessage.Content,
                    IsAdmin = savedMessage.IsSenderAdmin,
                    Timestamp = savedMessage.SentAt,
                    IsRead = savedMessage.IsRead
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving message to database: {ex.Message}");
                throw;
            }
        }
    }
} 