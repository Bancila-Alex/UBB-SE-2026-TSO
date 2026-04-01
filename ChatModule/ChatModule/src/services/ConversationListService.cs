using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class ConversationListService
    {
        private readonly ConversationRepository _conversationRepository;
        private readonly ParticipantRepository _participantRepository;
        private readonly MessageRepository _messageRepository;
        private readonly UserRepository _userRepository;

        public ConversationListService(
            ConversationRepository conversationRepository,
            ParticipantRepository participantRepository,
            MessageRepository messageRepository,
            UserRepository userRepository)
        {
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
            _messageRepository = messageRepository;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<List<Conversation>> GetAllAsync(Guid userId)
        {
            return await _conversationRepository.GetAllForUserAsync(userId);
        }

        public async Task<List<Conversation>> GetDmsAsync(Guid userId)
        {
            var conversations = await GetAllAsync(userId);
            return conversations.Where(conversation => conversation.Type == ConversationType.Dm).ToList();
        }

        public async Task<List<Conversation>> GetGroupsAsync(Guid userId)
        {
            var conversations = await GetAllAsync(userId);
            return conversations.Where(conversation => conversation.Type == ConversationType.Group).ToList();
        }

        public async Task<List<Conversation>> GetFavouritesAsync(Guid userId)
        {
            var participants = await _participantRepository.GetAllForUserAsync(userId);

            var favouriteConversationIds = participants
                .Where(participant => participant.IsFavourite)
                .Select(participant => participant.ConversationId)
                .ToHashSet();

            if (favouriteConversationIds.Count == 0)
            {
                return new List<Conversation>();
            }

            var conversations = await GetAllAsync(userId);
            return conversations
                .Where(conversation => favouriteConversationIds.Contains(conversation.Id))
                .ToList();
        }

        public async Task<List<Conversation>> SearchAsync(Guid userId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllAsync(userId);
            }

            var normalizedQuery = query.Trim();
            var conversations = await GetAllAsync(userId);
            var dmUsernameByConversationId = new Dictionary<Guid, string>();

            foreach (var conversation in conversations)
            {
                if (conversation.Type != ConversationType.Dm)
                {
                    continue;
                }

                var participants = await _participantRepository.GetAllForConversationAsync(conversation.Id);
                var otherParticipant = participants.FirstOrDefault(participant => participant.UserId != userId);
                if (otherParticipant == null)
                {
                    continue;
                }

                var otherUser = await _userRepository.GetByIdAsync(otherParticipant.UserId);
                if (otherUser == null)
                {
                    continue;
                }

                dmUsernameByConversationId[conversation.Id] = otherUser.Username;
            }

            return conversations
                .Where(conversation =>
                    (conversation.Type == ConversationType.Group
                     && conversation.Title?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) == true)
                    || (conversation.Type == ConversationType.Dm
                        && dmUsernameByConversationId.TryGetValue(conversation.Id, out var username)
                        && username.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        public async Task SetFavouriteAsync(Guid conversationId, Guid userId, bool isFavourite)
        {
            await _participantRepository.UpdateFavouriteAsync(conversationId, userId, isFavourite);
        }

        public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
        {
            var participant = await _participantRepository.GetAsync(conversationId, userId);
            if (participant == null)
            {
                return 0;
            }

            return await GetUnreadCountForParticipantAsync(conversationId, participant);
        }

        public async Task<Message?> GetLastMessageAsync(Guid conversationId)
        {
            return await _messageRepository.GetLastMessageAsync(conversationId);
        }

        public async Task<List<Conversation>> GetUnreadAsync(Guid userId)
        {
            var conversations = await GetAllAsync(userId);
            var participants = await _participantRepository.GetAllForUserAsync(userId);

            var participantByConversationId = new Dictionary<Guid, Participant>();
            foreach (var participant in participants)
            {
                participantByConversationId[participant.ConversationId] = participant;
            }

            var unreadConversations = new List<Conversation>();

            foreach (var conversation in conversations)
            {
                if (!participantByConversationId.TryGetValue(conversation.Id, out var participant))
                {
                    continue;
                }

                var unreadCount = await GetUnreadCountForParticipantAsync(conversation.Id, participant);
                if (unreadCount > 0)
                {
                    unreadConversations.Add(conversation);
                }
            }

            return unreadConversations;
        }

        private async Task<int> GetUnreadCountForParticipantAsync(Guid conversationId, Participant participant)
        {
            if (participant.LastReadMessageId.HasValue)
            {
                return await _messageRepository.CountUnreadAsync(conversationId, participant.LastReadMessageId.Value);
            }

            var lastMessage = await _messageRepository.GetLastMessageAsync(conversationId);
            return lastMessage == null ? 0 : 1;
        }
    }
}
