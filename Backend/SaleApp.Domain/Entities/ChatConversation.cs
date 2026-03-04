namespace SaleApp.Domain.Entities;

public class ChatConversation
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
