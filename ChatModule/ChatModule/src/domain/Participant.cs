using System;
using ChatModule.src.domain.Enums;

namespace ChatModule.Models
{
    public class Participant
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }
        public ParticipantRole Role { get; set; }
        public Guid? LastReadMessageId { get; set; }
        public DateTime? TimeoutUntil { get; set; }
        public bool IsFavourite { get; set; }
    }
}
