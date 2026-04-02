using System;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class DirectMessageService
    {
        private readonly ConversationRepository _conversationRepository;
        private readonly ParticipantRepository _participantRepository;
        private readonly BlockService _blockService;
        private readonly UserRepository _userRepository;

        public DirectMessageService(
            ConversationRepository conversationRepository,
            ParticipantRepository participantRepository,
            FriendRepository friendRepository,
            UserRepository userRepository)
        {
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
            _userRepository = userRepository;
            _blockService = new BlockService(friendRepository, userRepository);
        }

        public async Task<Conversation> GetOrCreateAsync(Guid userId1, Guid userId2)
        {
            var existingDm = await _conversationRepository.GetDmBetweenAsync(userId1, userId2);
            if (existingDm != null)
            {
                return existingDm;
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Dm,
                Title = null,
                IconUrl = null,
                CreatedBy = userId1,
                PinnedMessageId = null
            };

            await _conversationRepository.CreateAsync(conversation);

            var now = DateTime.UtcNow;
            await _participantRepository.CreateAsync(new Participant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = userId1,
                JoinedAt = now,
                Role = ParticipantRole.Member,
                LastReadMessageId = null,
                TimeoutUntil = null,
                IsFavourite = false
            });

            await _participantRepository.CreateAsync(new Participant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = userId2,
                JoinedAt = now,
                Role = ParticipantRole.Member,
                LastReadMessageId = null,
                TimeoutUntil = null,
                IsFavourite = false
            });

            return conversation;
        }

        public async Task<Participant?> GetOtherParticipantAsync(Guid conversationId, Guid currentUserId)
        {
            var participants = await _participantRepository.GetAllForConversationAsync(conversationId);
            return participants.FirstOrDefault(p => p.UserId != currentUserId);
        }

        public async Task<bool> IsBlockedAsync(Guid conversationId, Guid viewerUserId)
        {
            var otherParticipant = await GetOtherParticipantAsync(conversationId, viewerUserId);
            if (otherParticipant == null)
            {
                return false;
            }

            var blockedByViewer = await _blockService.IsBlockedAsync(viewerUserId, otherParticipant.UserId);
            var blockedByOther = await _blockService.IsBlockedAsync(otherParticipant.UserId, viewerUserId);

            return blockedByViewer || blockedByOther;
        }

        public async Task<User?> GetOtherUserAsync(Guid conversationId, Guid viewerUserId)
        {
            var otherParticipant = await GetOtherParticipantAsync(conversationId, viewerUserId);
            if (otherParticipant == null)
            {
                return null;
            }

            return await _userRepository.GetByIdAsync(otherParticipant.UserId);
        }
    }
}
