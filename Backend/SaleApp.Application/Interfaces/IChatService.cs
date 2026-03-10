using SaleApp.Application.DTOs;

namespace SaleApp.Application.Interfaces;

public interface IChatService
{
    // Conversation Management
    Task<ChatConversationDto> CreateConversationAsync(int userId);
    Task<List<ChatConversationDto>> GetUserConversationsAsync(int userId);
    Task<List<ChatConversationDto>> GetAllConversationsAsync(); // For shop admin
    Task<ChatConversationDto?> GetConversationByIdAsync(int conversationId);
    Task<bool> CloseConversationAsync(int conversationId);
    
    // Message Management
    Task<ChatMessageDto> SendMessageAsync(int conversationId, string message, string senderType, int? senderId = null);
    Task<List<ChatMessageDto>> GetConversationMessagesAsync(int conversationId, int skip = 0, int take = 50);
    Task<bool> MarkMessageAsReadAsync(int messageId);
    Task<int> GetUnreadMessageCountAsync(int conversationId, string receiverType);
}
