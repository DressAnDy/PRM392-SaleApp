namespace SaleApp.Application.DTOs;

public class ChatMessageDto
{
    public int ChatMessageId { get; set; }
    public int ConversationId { get; set; }
    public string SenderType { get; set; } = string.Empty; // User/Shop
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
}

public class CreateConversationRequest
{
    public int UserId { get; set; }
}

public class ChatConversationDto
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public ChatMessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}
