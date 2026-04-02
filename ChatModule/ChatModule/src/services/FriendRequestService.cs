using System;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
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
    }
}
