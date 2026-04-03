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
        private readonly MessageRepository _messageRepository;
        private readonly BlockService _blockService;
        private readonly UserRepository _userRepository;

        public DirectMessageService(
            ConversationRepository conversationRepository,
            ParticipantRepository participantRepository,
            FriendRepository friendRepository,
            UserRepository userRepository,
            MessageRepository messageRepository)
        {
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
            _userRepository = userRepository;
            _messageRepository = messageRepository;
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

        public async Task<(Message Pinned, Message Notice)> PinMessageAsync(Guid conversationId, Guid requesterId, Guid messageId, DateTime expiresAt)
        {
            var participants = await _participantRepository.GetAllForConversationAsync(conversationId);
            if (!participants.Any(p => p.UserId == requesterId))
                throw new InvalidOperationException("You are not a participant in this conversation.");

            var message = await _messageRepository.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");

            if (message.ConversationId != conversationId)
                throw new InvalidOperationException("Message does not belong to this conversation.");

            // Clear PinExpiresAt on any previously pinned message
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation?.PinnedMessageId != null && conversation.PinnedMessageId != messageId)
            {
                await _messageRepository.SetPinExpiresAtAsync(conversation.PinnedMessageId.Value, null);
            }

            await _conversationRepository.SetPinnedMessageAsync(conversationId, messageId);
            await _messageRepository.SetPinExpiresAtAsync(messageId, expiresAt);
            message.PinExpiresAt = expiresAt;

            var user = await _userRepository.GetByIdAsync(requesterId);
            var username = user?.Username ?? "Someone";
            var notice = await WriteSystemMessageAsync(conversationId, $"{username} pinned a message.");

            return (message, notice);
        }

        public async Task<Message> UnpinMessageAsync(Guid conversationId, Guid requesterId)
        {
            var participants = await _participantRepository.GetAllForConversationAsync(conversationId);
            if (!participants.Any(p => p.UserId == requesterId))
                throw new InvalidOperationException("You are not a participant in this conversation.");

            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation?.PinnedMessageId != null)
            {
                await _messageRepository.SetPinExpiresAtAsync(conversation.PinnedMessageId.Value, null);
            }

            await _conversationRepository.SetPinnedMessageAsync(conversationId, null);

            var user = await _userRepository.GetByIdAsync(requesterId);
            var username = user?.Username ?? "Someone";
            return await WriteSystemMessageAsync(conversationId, $"{username} unpinned a message.");
        }

        public async Task ClearExpiredPinAsync(Guid conversationId, Guid pinnedMessageId)
        {
            await _messageRepository.SetPinExpiresAtAsync(pinnedMessageId, null);
            await _conversationRepository.SetPinnedMessageAsync(conversationId, null);
        }

        private async Task<Message> WriteSystemMessageAsync(Guid conversationId, string text)
        {
            var notice = new Message
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
            };
            await _messageRepository.CreateAsync(notice);
            return notice;
        }
    }
}
