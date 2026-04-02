using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;

namespace ChatModule.Services
{
    public class ReadReceiptService
    {
        private readonly ParticipantRepository _participantRepository;

        public ReadReceiptService(ParticipantRepository participantRepository)
        {
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
        }

        public async Task MarkAsReadAsync(Guid conversationId, Guid userId, Guid messageId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            if (participant == null)
            {
                throw new InvalidOperationException("User is not a participant of this conversation.");
            }

            await _participantRepository.UpdateLastReadAsync(conversationId, userId, messageId);
        }

        public async Task<List<Participant>> GetReadReceiptsAsync(Guid conversationId, Guid messageId)
        {
            var participants = await _participantRepository.GetAllForConversationAsync(conversationId);
            var readers = new List<Participant>();

            foreach (var participant in participants)
            {
                if (participant.LastReadMessageId == messageId)
                {
                    readers.Add(participant);
                }
            }

            return readers;
        }

        public async Task<Guid?> GetLastReadMessageAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            return participant?.LastReadMessageId;
        }
    }
}
