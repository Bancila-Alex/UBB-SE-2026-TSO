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
        public string? SenderAvatarUrl { get; set; }
        public string SenderInitial => !string.IsNullOrWhiteSpace(SenderUsername)
            ? SenderUsername.Substring(0, 1).ToUpperInvariant()
            : "?";
        public Dictionary<string, int> ReactionCounts { get; set; } = new();
        public bool IsMine { get; set; }
        public int ReadByCount { get; set; }
        public string? ReadReceiptLabel { get; set; }
        public bool ShowUnreadSeparator { get; set; }
        public string? AttachmentImagePath { get; set; }
        public string? ReplyPreviewText { get; set; }
        public string? ReplyPreviewSender { get; set; }
        public string? ReplyPreviewContent { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
