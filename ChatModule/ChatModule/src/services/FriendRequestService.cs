using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class FriendRequestService
    {
        private readonly FriendRepository _friendRepository;
        private readonly UserRepository _userRepository;
        private readonly ConversationRepository _conversationRepository;
        private readonly ParticipantRepository _participantRepository;

        public FriendRequestService(
            FriendRepository friendRepository,
            UserRepository userRepository,
            ConversationRepository conversationRepository,
            ParticipantRepository participantRepository)
        {
            _friendRepository = friendRepository;
            _userRepository = userRepository;
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
        }

        public async Task SendRequestAsync(Guid senderId, Guid receiverId)
        {
            var alreadyFriends = await _friendRepository.IsFriendAsync(senderId, receiverId);
            if (alreadyFriends)
            {
                throw new InvalidOperationException("Users are already friends.");
            }

            var existingRelation = await _friendRepository.GetAsync(senderId, receiverId);
            if (existingRelation != null)
            {
                throw new InvalidOperationException("A friend request already exists between these users.");
            }

            _friendRepository.CreateAsync(new Friend
            {
                Id = Guid.NewGuid(),
                UserId1 = senderId,
                UserId2 = receiverId,
                Status = FriendStatus.Pending,
                IsMatch = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task AcceptRequestAsync(Guid currentUserId, Guid requesterId)
        {
            await _friendRepository.UpdateStatusAsync(requesterId, currentUserId, FriendStatus.Accepted);

            var existingDm = await _conversationRepository.GetDmBetweenAsync(currentUserId, requesterId);
            if (existingDm != null)
            {
                return;
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Dm,
                Title = null,
                IconUrl = null,
                CreatedBy = currentUserId,
                PinnedMessageId = null
            };

            await _conversationRepository.CreateAsync(conversation);

            var now = DateTime.UtcNow;
            await _participantRepository.CreateAsync(new Participant
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                UserId = currentUserId,
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
                UserId = requesterId,
                JoinedAt = now,
                Role = ParticipantRole.Member,
                LastReadMessageId = null,
                TimeoutUntil = null,
                IsFavourite = false
            });
        }

        public async Task DeclineRequestAsync(Guid currentUserId, Guid requesterId)
        {
            var relation = await _friendRepository.GetAsync(requesterId, currentUserId);
            if (relation == null || relation.Status != FriendStatus.Pending)
            {
                throw new InvalidOperationException("No pending friend request found.");
            }

            await _friendRepository.DeleteAsync(requesterId, currentUserId);
        }

        public async Task<List<User>> GetIncomingRequestsAsync(Guid currentUserId)
        {
            var pendingRequests = await _friendRepository.GetPendingRequestsForUserAsync(currentUserId);
            var senders = new List<User>();

            foreach (var request in pendingRequests)
            {
                var sender = await _userRepository.GetByIdAsync(request.UserId1);
                if (sender != null)
                {
                    senders.Add(sender);
                }
            }

            return senders;
        }

        public async Task<FriendStatus?> GetRelationshipStatusAsync(Guid userId1, Guid userId2)
        {
            var relation = await _friendRepository.GetAsync(userId1, userId2);
            return relation?.Status;
        }
    }
}
