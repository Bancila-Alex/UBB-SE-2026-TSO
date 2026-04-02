using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;

namespace ChatModule.Services
{
    public class SearchService
    {
        private readonly MessageRepository _messageRepository;
        private readonly ParticipantRepository _participantRepository;
        private readonly UserRepository _userRepository;

        public SearchService(
            MessageRepository messageRepository,
            ParticipantRepository participantRepository,
            UserRepository userRepository)
        {
            _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<List<Message>> SearchMessagesAsync(Guid conversationId, Guid userId, string query)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            if (participant == null)
            {
                throw new InvalidOperationException("Participant not found for this conversation.");
            }

            var messages = await _messageRepository.SearchInConversationAsync(conversationId, query);
            foreach (var message in messages)
            {
                if (message.UserId.HasValue)
                {
                    var sender = await _userRepository.GetByIdAsync(message.UserId.Value);
                    message.SenderUsername = sender?.Username ?? "Unknown User";
                }
                else
                {
                    message.SenderUsername = "System";
                }
            }

            return messages;
        }

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<User>();

            return await _userRepository.SearchByUsernameAsync(query);
        }

        public async Task<List<User>> SearchUsersForAddMemberAsync(Guid conversationId, string query)
        {
            var existingParticipants = await _participantRepository.GetAllForConversationAsync(conversationId);
            var existingUserIds = existingParticipants.Select(p => p.UserId).ToHashSet();

            var users = await _userRepository.SearchByUsernameAsync(query);
            return users.Where(u => !existingUserIds.Contains(u.Id)).ToList();
        }
    }
}
