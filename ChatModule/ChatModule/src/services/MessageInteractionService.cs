using ChatModule.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class MessageInteractionService
    {
        private readonly MessageRepository _messageRepo;
        private readonly ParticipantRepository _participantRepo;
        private readonly UserRepository _userRepo;

        public MessageInteractionService(
            MessageRepository messageRepo,
            ParticipantRepository participantRepo,
            UserRepository userRepo)
        {
            _messageRepo = messageRepo;
            _participantRepo = participantRepo;
            _userRepo = userRepo;
        }

        public async Task ReactToMessageAsync(Guid messageId, Guid userId, string emoji)
        {
            var message = await _messageRepo.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");

            if (string.IsNullOrWhiteSpace(emoji))
            {
                throw new InvalidOperationException("Reaction cannot be empty.");
            }

            if (message.MessageType == MessageType.Reaction)
            {
                throw new InvalidOperationException("You cannot react to a reaction message.");
            }

            await RequireCanSendAsync(message.ConversationId, userId);
            var existingReactions = await _messageRepo.GetReactionsForMessageAsync(messageId);
            var existingActive = existingReactions.FirstOrDefault(r => r.UserId == userId && !r.IsDeleted);
            var existingDeleted = existingReactions.FirstOrDefault(r => r.UserId == userId && r.IsDeleted);

            if (existingActive != null)
            {
                await _messageRepo.UpdateContentAsync(existingActive.Id, emoji);
            }
            else if (existingDeleted != null)
            {
                await _messageRepo.UpdateContentAsync(existingDeleted.Id, emoji);
                await _messageRepo.UnsoftDeleteAsync(existingDeleted.Id);
            }
            else await _messageRepo.CreateAsync(new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = message.ConversationId,
                UserId = userId,
                Content = emoji,
                CreatedAt = DateTime.UtcNow,
                ReplyToId = null,
                IsEdited = false,
                IsDeleted = false,
                MessageType = MessageType.Reaction,
                ParentMessageId = messageId
            });
        }

        public async Task RemoveReactionAsync(Guid messageId, Guid userId)
        {
            var message = await _messageRepo.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");
            await RequireCanSendAsync(message.ConversationId, userId);
            var existingReactions = await _messageRepo.GetReactionsForMessageAsync(messageId);
            if (existingReactions.Any(r => r.UserId == userId && !r.IsDeleted))
            {
                var existing = existingReactions.First(r => r.UserId == userId && !r.IsDeleted);
                await _messageRepo.SoftDeleteAsync(existing.Id);
            }
            else
            {
                throw new InvalidOperationException("Reaction not found for this user.");
            }
        }

        public async Task<List<Message>> GetReactionsAsync(Guid messageId)
        {
            var message = await _messageRepo.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");
            return await _messageRepo.GetReactionsForMessageAsync(messageId);
        }

        public async Task<string?> BuildReplyPreviewAsync(Guid messageId)
        {
            var message = await _messageRepo.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");
            if (message.IsDeleted)
            {
                return "This message has been deleted.";
            }
            if (message.MessageType == MessageType.Reaction)
            {
                return "This is a reaction and cannot be previewed.";
            }
            var senderName = "Unknown User";
            if (message.UserId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(message.UserId.Value);
                if (user != null)
                {
                    senderName = user.Username;
                }
            }
            var contentPreview = message.Content != null
                ? (message.Content.Length > 100 ? message.Content.Substring(0, 100) + "..." : message.Content)
                : "[No Text]";
            return $"{senderName}: {contentPreview}";
        }

        public async Task<(string Sender, string Content)?> BuildReplyPreviewPartsAsync(Guid messageId)
        {
            var message = await _messageRepo.GetByIdAsync(messageId)
                ?? throw new InvalidOperationException("Message not found.");

            if (message.IsDeleted)
            {
                return ("Deleted", "This message has been deleted.");
            }

            if (message.MessageType == MessageType.Reaction)
            {
                return ("Reaction", "This is a reaction and cannot be previewed.");
            }

            var senderName = "Unknown User";
            if (message.UserId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(message.UserId.Value);
                if (user != null)
                {
                    senderName = user.Username;
                }
            }

            var contentPreview = message.Content != null
                ? (message.Content.Length > 100 ? message.Content.Substring(0, 100) + "..." : message.Content)
                : "[No Text]";

            return (senderName, contentPreview);
        }

        private async Task<Participant> RequireActiveParticipantAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepo.GetAsync(conversationId, userId);
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

    }   
}
