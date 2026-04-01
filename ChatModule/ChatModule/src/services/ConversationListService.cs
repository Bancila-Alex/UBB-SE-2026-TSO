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

        public ConversationListService(
            ConversationRepository conversationRepository,
            ParticipantRepository participantRepository,
            MessageRepository messageRepository,
            UserRepository userRepository)
        {
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
            _messageRepository = messageRepository;
            _ = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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

                if (participant.LastReadMessageId.HasValue)
                {
                    var unreadCount = await _messageRepository.CountUnreadAsync(
                        conversation.Id,
                        participant.LastReadMessageId.Value);

                    if (unreadCount > 0)
                    {
                        unreadConversations.Add(conversation);
                    }

                    continue;
                }

                var lastMessage = await _messageRepository.GetLastMessageAsync(conversation.Id);
                if (lastMessage != null)
                {
                    unreadConversations.Add(conversation);
                }
            }

            return unreadConversations;
        }
    }
}
