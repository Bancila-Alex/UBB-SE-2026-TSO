using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class ProfileService
    {
        private readonly UserRepository _userRepository;
        private readonly FriendRepository _friendRepository;

        public ProfileService(UserRepository userRepository, FriendRepository friendRepository)
        {
            _userRepository = userRepository;
            _friendRepository = friendRepository;
        }

        public async Task<User?> GetProfileAsync(Guid targetUserId)
        {
            return await _userRepository.GetByIdAsync(targetUserId);
        }

        public async Task<List<User>> GetAllUsersAsync(Guid viewerUserId, string? searchQuery)
        {
            var users = string.IsNullOrWhiteSpace(searchQuery)
                ? await _userRepository.GetAllAsync()
                : await _userRepository.SearchByUsernameAsync(searchQuery);

            return users.Where(user => user.Id != viewerUserId).ToList();
        }

        public async Task UpdateProfileAsync(Guid userId, string? bio, string? avatarUrl, DateTime? birthday)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return;
            }

            if (bio != null)
            {
                user.Bio = bio;
            }

            if (avatarUrl != null)
            {
                user.AvatarUrl = avatarUrl;
            }

            if (birthday != null)
            {
                user.Birthday = birthday;
            }

            await _userRepository.UpdateAsync(user);
        }

        public async Task UpdateStatusAsync(Guid userId, UserStatus status)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return;
            }

            user.Status = status;
            await _userRepository.UpdateAsync(user);
        }

        public async Task<List<User>> GetMutualFriendsAsync(Guid userId1, Guid userId2)
        {
            var mutualFriendIds = await _friendRepository.GetMutualFriendIdsAsync(userId1, userId2);
            var mutualFriends = new List<User>();

            foreach (var mutualFriendId in mutualFriendIds)
            {
                var user = await _userRepository.GetByIdAsync(mutualFriendId);
                if (user != null)
                {
                    mutualFriends.Add(user);
                }
            }

            return mutualFriends;
        }

        private bool IsTodayBirthday(DateTime? birthday)
        {
            if (!birthday.HasValue)
            {
                return false;
            }

            var today = DateTime.Today;
            return birthday.Value.Month == today.Month && birthday.Value.Day == today.Day;
        }
    }
}
