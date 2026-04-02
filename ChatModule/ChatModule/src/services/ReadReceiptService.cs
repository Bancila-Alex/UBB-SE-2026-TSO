using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;

namespace ChatModule.Services
{
    public class ReadReceiptService
    {
        private readonly ParticipantRepository _participantRepository;
        private readonly MessageRepository _messageRepository;

        public ReadReceiptService(ParticipantRepository participantRepository, MessageRepository messageRepository)
        {
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        }

        public async Task MarkAsReadAsync(Guid conversationId, Guid userId, Guid messageId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            if (participant == null)
            {
                throw new InvalidOperationException("User is not a participant of this conversation.");
            }

            var targetMessage = await _messageRepository.GetByIdAsync(messageId);
            if (targetMessage == null || targetMessage.ConversationId != conversationId)
            {
                return;
            }

            if (targetMessage.UserId.HasValue && targetMessage.UserId.Value == userId)
            {
                return;
            }

            if (participant.LastReadMessageId.HasValue)
            {
                var currentLastRead = await _messageRepository.GetByIdAsync(participant.LastReadMessageId.Value);
                if (currentLastRead != null && currentLastRead.CreatedAt >= targetMessage.CreatedAt)
                {
                    return;
                }
            }

            await _participantRepository.UpdateLastReadAsync(conversationId, userId, messageId);
        }

        public async Task MarkLatestAsReadAsync(Guid conversationId, Guid userId)
        {
            var latestReadableMessageId = await _messageRepository.GetLatestReadableMessageIdAsync(conversationId, userId);
            if (!latestReadableMessageId.HasValue)
            {
                return;
            }

            await MarkAsReadAsync(conversationId, userId, latestReadableMessageId.Value);
        }

        public async Task<List<Participant>> GetReadReceiptsAsync(Guid conversationId, Guid messageId)
        {
            var targetMessage = await _messageRepository.GetByIdAsync(messageId);
            if (targetMessage == null)
            {
                return new List<Participant>();
            }

            var participants = await _participantRepository.GetAllForConversationAsync(conversationId);
            var readers = new List<Participant>();

            foreach (var participant in participants)
            {
                if (!participant.LastReadMessageId.HasValue)
                {
                    continue;
                }

                var lastRead = await _messageRepository.GetByIdAsync(participant.LastReadMessageId.Value);
                if (lastRead != null && lastRead.CreatedAt >= targetMessage.CreatedAt)
                {
                    readers.Add(participant);
                }
            }

            return readers;
        }

        public async Task<int> GetReadByCountAsync(Guid conversationId, Guid messageId)
        {
            var readers = await GetReadReceiptsAsync(conversationId, messageId);
            return readers.Count;
        }

        public async Task<Guid?> GetLastReadMessageAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            return participant?.LastReadMessageId;
        }

        public async Task<List<Participant>> GetParticipantsAsync(Guid conversationId)
        {
            return await _participantRepository.GetAllForConversationAsync(conversationId);
        }
    }
}
