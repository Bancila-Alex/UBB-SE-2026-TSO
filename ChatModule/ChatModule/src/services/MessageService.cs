using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatModule.Services
{
    public class MessageService
    {
        private readonly MessageRepository _messageRepository;
        private readonly ParticipantRepository _participantRepository;
        private readonly UserRepository _userRepository;

        public MessageService(
            MessageRepository messageRepository,
            ParticipantRepository participantRepository,
            UserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _participantRepository = participantRepository;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        private async Task<Participant> RequireActiveParticipantAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            if (participant == null)
            {
                throw new InvalidOperationException("Participant not found for this conversation.");
            }

            if (participant.Role == ParticipantRole.Banned)
            {
                throw new InvalidOperationException("Participant is banned in this conversation.");
            }

            return participant;
        }

        private async Task RequireCanSendAsync(Guid conversationId, Guid userId)
        {
            var participant = await RequireActiveParticipantAsync(conversationId, userId);
            if (participant.TimeoutUntil.HasValue && participant.TimeoutUntil.Value > DateTime.UtcNow)
            {
                throw new InvalidOperationException("Participant is timed out and cannot send messages.");
            }
        }

        public async Task<List<Message>> GetMessagesAsync(Guid conversationId, Guid userId, int skip, int take)
        {
            await RequireActiveParticipantAsync(conversationId, userId);

            var messages = await _messageRepository.GetByConversationAsync(conversationId, skip, take);
            await PopulateSenderMetadataAsync(messages);
            return messages;
        }

        public async Task<Message> SendMessageAsync(Guid conversationId, Guid senderId, string content, Guid? replyToId)
        {
            await RequireCanSendAsync(conversationId, senderId);

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content cannot be empty.", nameof(content));
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                ReplyToId = replyToId,
                IsEdited = false,
                IsDeleted = false,
                MessageType = MessageType.Text,
                ParentMessageId = null
            };

            await _messageRepository.CreateAsync(message);

            var sender = await _userRepository.GetByIdAsync(senderId);
            message.SenderUsername = sender?.Username;
            message.SenderAvatarUrl = sender?.AvatarUrl;

            return message;
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

        private async Task PopulateSenderMetadataAsync(List<Message> messages)
        {
            foreach (var message in messages)
            {
                if (!message.UserId.HasValue)
                {
                    message.SenderUsername = "System";
                    message.SenderAvatarUrl = null;
                    continue;
                }

                var sender = await _userRepository.GetByIdAsync(message.UserId.Value);
                message.SenderUsername = sender?.Username ?? "Unknown";
                message.SenderAvatarUrl = sender?.AvatarUrl;
            }
        }
    }
}
