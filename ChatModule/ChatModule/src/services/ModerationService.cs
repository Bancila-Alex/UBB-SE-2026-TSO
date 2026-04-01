using System;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;

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
