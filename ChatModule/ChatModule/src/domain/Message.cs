using System;
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
        public int HeartReactionCount { get; set; }
        public int ThumbsUpReactionCount { get; set; }
        public int LaughReactionCount { get; set; }
        public int FireReactionCount { get; set; }
    }
}
