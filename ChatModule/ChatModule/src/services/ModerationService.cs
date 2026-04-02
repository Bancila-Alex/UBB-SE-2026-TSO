using System;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;
using ChatModule.src.domain;

namespace ChatModule.Services
{
    public class ModerationService
    {
        private readonly ParticipantRepository _participantRepository;
        private readonly MessageRepository _messageRepository;
        private readonly UserRepository _userRepository;

        public ModerationService(
            ParticipantRepository participantRepository,
            MessageRepository messageRepository,
            UserRepository userRepository)
        {
            _participantRepository = participantRepository;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public async Task BanMemberAsync(Guid conversationId, Guid adminId, Guid targetId)
        {
            await RequireAdminAsync(conversationId, adminId);
            await _participantRepository.UpdateRoleAsync(conversationId, targetId, ParticipantRole.Banned);
            await WriteSystemMessageAsync(conversationId, $"User {targetId} was banned.");
        }

        public async Task UnbanMemberAsync(Guid conversationId, Guid adminId, Guid targetId)
        {
            await RequireAdminAsync(conversationId, adminId);
            await _participantRepository.UpdateRoleAsync(conversationId, targetId, ParticipantRole.Member);
            await WriteSystemMessageAsync(conversationId, $"User {targetId} was unbanned.");
        }

        public async Task TimeoutMemberAsync(Guid conversationId, Guid adminId, Guid targetId, TimeSpan duration)
        {
            await RequireAdminAsync(conversationId, adminId);
            await _participantRepository.UpdateTimeoutAsync(conversationId, targetId, DateTime.UtcNow + duration);
        }

        public async Task RemoveTimeoutAsync(Guid conversationId, Guid adminId, Guid targetId)
        {
            await RequireAdminAsync(conversationId, adminId);
            await _participantRepository.UpdateTimeoutAsync(conversationId, targetId, null);
        }

        public async Task PromoteMemberAsync(Guid conversationId, Guid adminId, Guid targetId)
        {
            await RequireAdminAsync(conversationId, adminId);
            await _participantRepository.UpdateRoleAsync(conversationId, targetId, ParticipantRole.Admin);
            await WriteSystemMessageAsync(conversationId, $"User {targetId} was promoted to admin.");
        }

        public async Task DemoteMemberAsync(Guid conversationId, Guid adminId, Guid targetId)
        {
            await RequireAdminAsync(conversationId, adminId);
            await _participantRepository.UpdateRoleAsync(conversationId, targetId, ParticipantRole.Member);
            await WriteSystemMessageAsync(conversationId, $"User {targetId} was demoted to member.");
        }

        public async Task AddMemberAsync(Guid conversationId, Guid adminId, Guid newUserId)
        {
            await RequireAdminAsync(conversationId, adminId);

            var existing = await _participantRepository.GetAsync(conversationId, newUserId);
            if (existing != null)
            {
                throw new InvalidOperationException("User is already a participant of this conversation.");
            }

            await _participantRepository.CreateAsync(new Participant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = newUserId,
                JoinedAt = DateTime.UtcNow,
                Role = ParticipantRole.Member,
                LastReadMessageId = null,
                TimeoutUntil = null,
                IsFavourite = false
            });

            await WriteSystemMessageAsync(conversationId, $"User {newUserId} was added to the group.");
        }

        private async Task RequireAdminAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            if (participant == null || participant.Role != ParticipantRole.Admin)
            {
                throw new InvalidOperationException("Only admins can perform this action.");
            }
        }

        private async Task WriteSystemMessageAsync(Guid conversationId, string text)
        {
            await _messageRepository.CreateAsync(new Message
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
