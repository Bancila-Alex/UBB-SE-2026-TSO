using System;
using System.Collections.Generic;
using ChatModule.src.domain.Enums;

namespace ChatModule.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid? UserId { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? ReplyToId { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public MessageType MessageType { get; set; }
        public Guid? ParentMessageId { get; set; }
        public string? SenderUsername { get; set; }
        public Dictionary<string, int> ReactionCounts { get; set; } = new();
    }
}
