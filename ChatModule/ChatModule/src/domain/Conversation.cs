using System;
using ChatModule.src.domain.Enums;

namespace ChatModule.src.domain
{
    public class Conversation
    {
        public Guid Id { get; set; }

        public ConversationType Type { get; set; }

        public string? Title { get; set; }

        public string? IconUrl { get; set; }

        public Guid CreatedBy { get; set; }

        public Guid? PinnedMessageId { get; set; }
    }
}
