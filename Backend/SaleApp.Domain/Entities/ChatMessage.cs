namespace SaleApp.Domain.Entities;

public class ChatMessage
{
    public int ChatMessageId { get; set; }
    public int ConversationId { get; set; }
    public string SenderType { get; set; } = string.Empty; // User/Shop
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public ChatConversation ChatConversation { get; set; } = null!;
}
