using ChatModule.Repositories;
using ChatModule.src.domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatModule.Services
{
    public class MessageService
    {
        private readonly MessageRepository _messageRepository;
        private readonly ParticipantRepository _participantRepository;

        public MessageService(MessageRepository messageRepository, ParticipantRepository participantRepository)
        {
            _messageRepository = messageRepository;
            _participantRepository = participantRepository;
        }

        public async Task EditMessageAsync(Guid messageId, Guid requesterId, string newContent)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null)
                throw new InvalidOperationException("Message not found.");

            if (message.UserId == null || message.UserId.Value != requesterId)
                throw new UnauthorizedAccessException("You are not the author of this message.");

            await _messageRepository.UpdateContentAsync(messageId, newContent);
            await _messageRepository.SetEditedAsync(messageId);
        }

        public async Task DeleteMessageAsync(Guid messageId, Guid requesterId)
        {
            var message = await _messageRepository.GetByIdAsync(messageId);
            if (message == null)
                throw new InvalidOperationException("Message not found.");

            bool isAuthor = message.UserId != null && message.UserId.Value == requesterId;
            bool isAdmin = false;

            if (!isAuthor)
            {
                var participant = await _participantRepository.GetAsync(message.ConversationId, requesterId);
                isAdmin = participant != null && participant.Role == ParticipantRole.Admin;
            }

            if (!isAuthor && !isAdmin)
                throw new UnauthorizedAccessException("You do not have permission to delete this message.");

            await _messageRepository.SoftDeleteAsync(messageId);
        }
    }
}
