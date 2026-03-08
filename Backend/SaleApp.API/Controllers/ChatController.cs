using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using System.Security.Claims;

namespace SaleApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // GET: api/Chat/conversations
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (!userId.HasValue)
            return Unauthorized();

        List<ChatConversationDto> conversations;

        // If admin/shop, get all conversations
        if (userRole == "Admin" || userRole == "Seller")
        {
            conversations = await _chatService.GetAllConversationsAsync();
        }
        else
        {
            // Regular user, get only their conversations
            conversations = await _chatService.GetUserConversationsAsync(userId.Value);
        }

        return Ok(conversations);
    }

    // GET: api/Chat/conversations/{id}
    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(int id)
    {
        var conversation = await _chatService.GetConversationByIdAsync(id);
        
        if (conversation == null)
            return NotFound(new { message = "Conversation not found" });

        // Check if user has access to this conversation
        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userRole != "Admin" && userRole != "Seller" && conversation.UserId != userId)
            return Forbid();

        return Ok(conversation);
    }

    // POST: api/Chat/conversations
    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation()
    {
        var userId = GetUserId();

        if (!userId.HasValue)
            return Unauthorized();

        var conversation = await _chatService.CreateConversationAsync(userId.Value);
        return CreatedAtAction(nameof(GetConversation), new { id = conversation.ConversationId }, conversation);
    }

    // GET: api/Chat/conversations/{id}/messages
    [HttpGet("conversations/{id}/messages")]
    public async Task<IActionResult> GetMessages(int id, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        // Check if conversation exists and user has access
        var conversation = await _chatService.GetConversationByIdAsync(id);
        if (conversation == null)
            return NotFound(new { message = "Conversation not found" });

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userRole != "Admin" && userRole != "Seller" && conversation.UserId != userId)
            return Forbid();

        var messages = await _chatService.GetConversationMessagesAsync(id, skip, take);
        return Ok(messages);
    }

    // POST: api/Chat/conversations/{id}/messages
    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message cannot be empty" });

        // Check if conversation exists and user has access
        var conversation = await _chatService.GetConversationByIdAsync(id);
        if (conversation == null)
            return NotFound(new { message = "Conversation not found" });

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userRole != "Admin" && userRole != "Seller" && conversation.UserId != userId)
            return Forbid();

        var senderType = (userRole == "Admin" || userRole == "Seller") ? "Shop" : "User";
        var message = await _chatService.SendMessageAsync(id, request.Message, senderType, userId);

        return Ok(message);
    }

    // PUT: api/Chat/messages/{id}/read
    [HttpPut("messages/{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _chatService.MarkMessageAsReadAsync(id);
        
        if (!result)
            return NotFound(new { message = "Message not found or already read" });

        return NoContent();
    }

    // PUT: api/Chat/conversations/{id}/close
    [HttpPut("conversations/{id}/close")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> CloseConversation(int id)
    {
        var result = await _chatService.CloseConversationAsync(id);
        
        if (!result)
            return NotFound(new { message = "Conversation not found" });

        return NoContent();
    }

    // GET: api/Chat/conversations/{id}/unread-count
    [HttpGet("conversations/{id}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(int id)
    {
        var conversation = await _chatService.GetConversationByIdAsync(id);
        if (conversation == null)
            return NotFound(new { message = "Conversation not found" });

        var userId = GetUserId();
        var userRole = GetUserRole();

        if (userRole != "Admin" && userRole != "Seller" && conversation.UserId != userId)
            return Forbid();

        var receiverType = (userRole == "Admin" || userRole == "Seller") ? "Shop" : "User";
        var count = await _chatService.GetUnreadMessageCountAsync(id, receiverType);

        return Ok(new { unreadCount = count });
    }

    // Helper methods
    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }

    private string? GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
