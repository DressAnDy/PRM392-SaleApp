using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SaleApp.Application.Interfaces;
using System.Security.Claims;

namespace SaleApp.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly NotificationHelper _notificationHelper;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, NotificationHelper notificationHelper, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _notificationHelper = notificationHelper;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        _logger.LogInformation($"?? OnConnectedAsync: userId={userId}, role={userRole}, connectionId={Context.ConnectionId}");

        if (userId.HasValue)
        {
            // Regular users join their personal group
            // Admin/Seller join shop_admin group instead of personal group
            if (userRole == "Admin" || userRole == "Seller")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "shop_admin");
                _logger.LogInformation($"? Admin/Seller userId={userId} joined group: 'shop_admin'");
            }
            else
            {
                // Regular users only
                var userGroup = $"user_{userId.Value}";
                await Groups.AddToGroupAsync(Context.ConnectionId, userGroup);
                _logger.LogInformation($"? Regular user userId={userId} joined group: '{userGroup}'");
            }
        }
        else
        {
            _logger.LogWarning($"?? User connected without valid userId claim");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userId.HasValue)
        {
            if (userRole == "Admin" || userRole == "Seller")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "shop_admin");
                _logger.LogInformation($"Admin/Seller {userId} left group: shop_admin");
            }
            else
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
                _logger.LogInformation($"User {userId.Value} left group: user_{userId.Value}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Send message from client
    public async Task SendMessage(int conversationId, string message)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            if (!userId.HasValue)
            {
                throw new HubException("User not authenticated");
            }

            _logger.LogInformation($"?? SendMessage: userId={userId}, role={userRole}, conversationId={conversationId}, message='{message}'");

            // Verify conversation exists first
            var conversation = await _chatService.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                _logger.LogError($"? Conversation {conversationId} not found");
                throw new HubException($"Conversation {conversationId} not found. Please create conversation first.");
            }

            _logger.LogInformation($"?? Conversation info: conversationId={conversationId}, conversationUserId={conversation.UserId}, conversationUsername={conversation.Username}");

            // Determine sender type based on role
            var isShop = userRole == "Admin" || userRole == "Seller";
            var senderType = isShop ? "Shop" : "User";

            // Save message to database
            var chatMessage = await _chatService.SendMessageAsync(conversationId, message, senderType, userId.Value);

            _logger.LogInformation($"?? Message saved: messageId={chatMessage.ChatMessageId}, senderType={senderType}");

            // Send message to appropriate recipients
            if (senderType == "User")
            {
                // User sent message ? Send to ALL shop admins
                _logger.LogInformation($"?? User?Shop: Sending to group 'shop_admin'");
                await Clients.Group("shop_admin").SendAsync("ReceiveMessage", chatMessage);
            }
            else
            {
                // Shop sent message ? Send ONLY to the specific user (conversation owner)
                var targetUserId = conversation.UserId;
                var targetGroup = $"user_{targetUserId}";
                
                _logger.LogInformation($"?? Shop?User: Sending to group '{targetGroup}' (userId={targetUserId})");
                
                // Send to user group ONLY
                await Clients.Group(targetGroup).SendAsync("ReceiveMessage", chatMessage);
                
                // Try to send notification
                try
                {
                    await _notificationHelper.SendChatNotificationAsync(targetUserId, conversationId, $"Shop replied: {message}");
                    _logger.LogInformation($"?? Notification sent to user {targetUserId}");
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning($"?? Failed to send notification: {notifEx.Message}");
                }
            }

            // Send confirmation to sender
            await Clients.Caller.SendAsync("MessageSent", chatMessage);
            _logger.LogInformation($"? MessageSent confirmation sent to sender");
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"? Error in SendMessage: {ex.Message}\n{ex.StackTrace}");
            throw new HubException($"Failed to send message: {ex.Message}");
        }
    }

    // Mark message as read
    public async Task MarkAsRead(int messageId)
    {
        await _chatService.MarkMessageAsReadAsync(messageId);
        
        // Notify other parties
        await Clients.Others.SendAsync("MessageRead", messageId);
    }

    // Typing indicator
    public async Task UserTyping(int conversationId)
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (!userId.HasValue)
            return;

        var conversation = await _chatService.GetConversationByIdAsync(conversationId);
        if (conversation == null)
            return;

        var senderType = (userRole == "Admin" || userRole == "Seller") ? "Shop" : "User";

        if (senderType == "User")
        {
            // Notify shop admins
            await Clients.Group("shop_admin").SendAsync("UserTyping", conversationId, userId.Value);
        }
        else
        {
            // Notify the specific user
            await Clients.Group($"user_{conversation.UserId}").SendAsync("ShopTyping", conversationId);
        }
    }

    // Helper methods
    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }

    private string? GetUserRole()
    {
        return Context.User?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
