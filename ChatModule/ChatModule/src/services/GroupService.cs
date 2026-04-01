using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class GroupService
    {
        private readonly ConversationRepository _convRepo;
        private readonly ParticipantRepository _participantRepo;
        private readonly MessageRepository _messageRepo;

        public GroupService(
            ConversationRepository convRepo,
            ParticipantRepository participantRepo,
            MessageRepository messageRepo,
            UserRepository userRepo)
        {
            _convRepo = convRepo;
            _participantRepo = participantRepo;
            _messageRepo = messageRepo;
            _ = userRepo;
        }

        public async Task<Conversation> CreateGroupAsync(Guid creatorId, string title, string? iconUrl, List<Guid> memberIds)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Group title cannot be empty.", nameof(title));

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Group,
                Title = title,
                IconUrl = iconUrl,
                CreatedBy = creatorId,
                PinnedMessageId = null
            };

            await _convRepo.CreateAsync(conversation);

            var now = DateTime.UtcNow;

            await _participantRepo.CreateAsync(new Participant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = creatorId,
                JoinedAt = now,
                Role = ParticipantRole.Admin,
                LastReadMessageId = null,
                TimeoutUntil = null,
                IsFavourite = false
            });

            foreach (var memberId in memberIds.Where(id => id != creatorId))
            {
                await _participantRepo.CreateAsync(new Participant
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    UserId = memberId,
                    JoinedAt = now,
                    Role = ParticipantRole.Member,
                    LastReadMessageId = null,
                    TimeoutUntil = null,
                    IsFavourite = false
                });
            }

            await WriteSystemMessageAsync(conversation.Id, $"Group \"{title}\" was created.");
            return conversation;
        }

        public async Task UpdateGroupInfoAsync(Guid conversationId, Guid requesterId, string? newTitle, string? newIconUrl)
        {
            await RequireAdminAsync(conversationId, requesterId);

            var conversation = await _convRepo.GetByIdAsync(conversationId)
                ?? throw new InvalidOperationException("Conversation not found.");

            if (newTitle != null)
                conversation.Title = newTitle;
            if (newIconUrl != null)
                conversation.IconUrl = newIconUrl;

            await _convRepo.UpdateAsync(conversation);
        }

        public async Task LeaveGroupAsync(Guid conversationId, Guid userId)
        {
            var leavingParticipant = await _participantRepo.GetAsync(conversationId, userId)
                ?? throw new InvalidOperationException("You are not a member of this conversation.");

            await _participantRepo.DeleteAsync(conversationId, userId);

            var remainingParticipants = await _participantRepo.GetAllForConversationAsync(conversationId);

            if (remainingParticipants.Count == 0)
            {
                await _convRepo.DeleteAsync(conversationId);
                return;
            }

            var isLeavingAdmin = leavingParticipant.Role == ParticipantRole.Admin;
            var hasRemainingAdmin = remainingParticipants.Any(participant => participant.Role == ParticipantRole.Admin);
            var shouldPromoteOldestParticipant = isLeavingAdmin && !hasRemainingAdmin;

            if (shouldPromoteOldestParticipant)
            {
                var promotedParticipant = remainingParticipants
                    .OrderBy(participant => participant.JoinedAt)
                    .ThenBy(participant => participant.UserId)
                    .First();

                await _participantRepo.UpdateRoleAsync(conversationId, promotedParticipant.UserId, ParticipantRole.Admin);
                await WriteSystemMessageAsync(conversationId, "A new admin has been appointed.");
            }

            await WriteSystemMessageAsync(conversationId, "A member has left the group.");
        }

        public async Task PinMessageAsync(Guid conversationId, Guid requesterId, Guid messageId)
        {
            await RequireAdminAsync(conversationId, requesterId);

            var message = await _messageRepo.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");

            if (message.ConversationId != conversationId)
                throw new InvalidOperationException("Message does not belong to this conversation.");

            await _convRepo.SetPinnedMessageAsync(conversationId, messageId);
        }

        public async Task UnpinMessageAsync(Guid conversationId, Guid requesterId)
        {
            await RequireAdminAsync(conversationId, requesterId);
            await _convRepo.SetPinnedMessageAsync(conversationId, null);
        }

        public async Task PostEventNoticeAsync(Guid conversationId, Guid adminId, string eventTitle, DateTime eventDate)
        {
            await RequireAdminAsync(conversationId, adminId);
            await WriteSystemMessageAsync(conversationId, $"Event: \"{eventTitle}\" on {eventDate:f}.");
        }

        private async Task RequireAdminAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepo.GetAsync(conversationId, userId);
            if (participant == null)
                throw new InvalidOperationException("You are not a member of this conversation.");
            if (participant.Role != ParticipantRole.Admin)
                throw new UnauthorizedAccessException("Only admins can perform this action.");
        }

        private async Task WriteSystemMessageAsync(Guid conversationId, string text)
        {
            await _messageRepo.CreateAsync(new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = null,
                Content = text,
                CreatedAt = DateTime.UtcNow,
                ReplyToId = null,
                IsEdited = false,
                IsDeleted = false,
                MessageType = MessageType.System,
                ParentMessageId = null
            });
        }
    }
}
