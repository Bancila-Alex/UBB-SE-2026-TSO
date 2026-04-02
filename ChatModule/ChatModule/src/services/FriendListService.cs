using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;

namespace ChatModule.Services
{
    public class FriendListService
    {
        private readonly FriendRepository _friendRepository;
        private readonly UserRepository _userRepository;

        public FriendListService(FriendRepository friendRepository, UserRepository userRepository)
        {
            _friendRepository = friendRepository ?? throw new ArgumentNullException(nameof(friendRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<List<User>> GetFriendsAsync(Guid userId)
        {
            var acceptedFriends = await _friendRepository.GetAcceptedFriendsAsync(userId);
            var friends = new List<User>();

            foreach (var friend in acceptedFriends)
            {
                var friendUserId = friend.UserId1 == userId ? friend.UserId2 : friend.UserId1;
                var user = await _userRepository.GetByIdAsync(friendUserId);
                if (user != null)
                {
                    friends.Add(user);
                }
            }

            return friends;
        }

        public async Task RemoveFriendAsync(Guid userId, Guid friendId)
        {
            await _friendRepository.DeleteAsync(userId, friendId);
        }
    }
}
