using Microsoft.EntityFrameworkCore;
using SaleApp.Application.DTOs;
using SaleApp.Application.Interfaces;
using SaleApp.Domain.Entities;
using SaleApp.Infrastructure.Data;

namespace SaleApp.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly SaleAppDbContext _context;

    public ChatService(SaleAppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatConversationDto> CreateConversationAsync(int userId)
    {
        // Check if conversation already exists for this user
        var existingConversation = await _context.ChatConversations
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Open");

        if (existingConversation != null)
        {
            return MapToConversationDto(existingConversation);
        }

        // Create new conversation
        var conversation = new ChatConversation
        {
            UserId = userId,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatConversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Reload with user data
        var createdConversation = await _context.ChatConversations
            .Include(c => c.User)
            .FirstAsync(c => c.ConversationId == conversation.ConversationId);

        return MapToConversationDto(createdConversation);
    }

    public async Task<List<ChatConversationDto>> GetUserConversationsAsync(int userId)
    {
        var conversations = await _context.ChatConversations
            .Include(c => c.User)
            .Include(c => c.ChatMessages.OrderByDescending(m => m.SentAt).Take(1))
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var conversationDtos = new List<ChatConversationDto>();
        foreach (var conv in conversations)
        {
            var dto = MapToConversationDto(conv);
            dto.UnreadCount = await GetUnreadMessageCountAsync(conv.ConversationId, "User");
            conversationDtos.Add(dto);
        }

        return conversationDtos;
    }

    public async Task<List<ChatConversationDto>> GetAllConversationsAsync()
    {
        var conversations = await _context.ChatConversations
            .Include(c => c.User)
            .Include(c => c.ChatMessages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync();

        var conversationDtos = new List<ChatConversationDto>();
        foreach (var conv in conversations)
        {
            var dto = MapToConversationDto(conv);
            dto.UnreadCount = await GetUnreadMessageCountAsync(conv.ConversationId, "Shop");
            conversationDtos.Add(dto);
        }

        return conversationDtos;
    }

    public async Task<ChatConversationDto?> GetConversationByIdAsync(int conversationId)
    {
        var conversation = await _context.ChatConversations
            .Include(c => c.User)
            .Include(c => c.ChatMessages.OrderByDescending(m => m.SentAt).Take(1))
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
            return null;

        return MapToConversationDto(conversation);
    }

    public async Task<bool> CloseConversationAsync(int conversationId)
    {
        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation == null)
            return false;

        conversation.Status = "Closed";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ChatMessageDto> SendMessageAsync(int conversationId, string message, string senderType, int? senderId = null)
    {
        // Verify conversation exists
        var conversationExists = await _context.ChatConversations
            .AnyAsync(c => c.ConversationId == conversationId);

        if (!conversationExists)
        {
            throw new InvalidOperationException($"Conversation {conversationId} does not exist");
        }

        var chatMessage = new ChatMessage
        {
            ConversationId = conversationId,
            SenderType = senderType,
            Message = message,
            SentAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);

        // Update conversation's last message time
        var conversation = await _context.ChatConversations
            .AsTracking()
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (conversation != null)
        {
            conversation.LastMessageAt = chatMessage.SentAt;
            _context.ChatConversations.Update(conversation);
        }

        await _context.SaveChangesAsync();

        return MapToMessageDto(chatMessage);
    }

    public async Task<List<ChatMessageDto>> GetConversationMessagesAsync(int conversationId, int skip = 0, int take = 50)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        messages.Reverse(); // Return in ascending order (oldest to newest)
        return messages.Select(MapToMessageDto).ToList();
    }

    public async Task<bool> MarkMessageAsReadAsync(int messageId)
    {
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.ChatMessageId == messageId);

        if (message == null || message.ReadAt != null)
            return false;

        message.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadMessageCountAsync(int conversationId, string receiverType)
    {
        // receiverType: "User" means count messages from Shop
        // receiverType: "Shop" means count messages from User
        var senderType = receiverType == "User" ? "Shop" : "User";

        return await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId 
                && m.SenderType == senderType 
                && m.ReadAt == null)
            .CountAsync();
    }

    private ChatConversationDto MapToConversationDto(ChatConversation conversation)
    {
        var lastMessage = conversation.ChatMessages?.FirstOrDefault();

        return new ChatConversationDto
        {
            ConversationId = conversation.ConversationId,
            UserId = conversation.UserId,
            Username = conversation.User?.Username ?? "Unknown",
            Status = conversation.Status,
            LastMessageAt = conversation.LastMessageAt,
            CreatedAt = conversation.CreatedAt,
            LastMessage = lastMessage != null ? MapToMessageDto(lastMessage) : null,
            UnreadCount = 0 // Will be set separately
        };
    }

    private ChatMessageDto MapToMessageDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            ChatMessageId = message.ChatMessageId,
            ConversationId = message.ConversationId,
            SenderType = message.SenderType,
            Message = message.Message,
            SentAt = message.SentAt,
            ReadAt = message.ReadAt
        };
    }
}
