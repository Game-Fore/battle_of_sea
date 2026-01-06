using System;

namespace battle_sea.Models.Chat
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SenderName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ChatChannel Channel { get; set; }
        public string? TargetPlayerId { get; set; }
    }
}