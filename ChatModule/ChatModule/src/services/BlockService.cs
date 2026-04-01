using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class BlockService
    {
        private readonly FriendRepository _friendRepository;
        private readonly UserRepository _userRepository;

        public BlockService(FriendRepository friendRepository, UserRepository userRepository)
        {
            _friendRepository = friendRepository;
            _userRepository = userRepository;
        }

        public async Task BlockUserAsync(Guid blockerId, Guid targetId)
        {
            await _friendRepository.UpdateStatusAsync(blockerId, targetId, FriendStatus.Blocked);
        }

        public async Task UnblockUserAsync(Guid blockerId, Guid targetId)
        {
            await _friendRepository.DeleteAsync(blockerId, targetId);
        }

        public async Task<List<User>> GetBlockedUsersAsync(Guid userId)
        {
            var blockedUsers = new List<User>();
            var relations = await _friendRepository.GetAllForUserAsync(userId);

            foreach (var relation in relations)
            {
                if (relation.Status != FriendStatus.Blocked)
                {
                    continue;
                }

                var otherUserId = relation.UserId1 == userId ? relation.UserId2 : relation.UserId1;
                var user = await _userRepository.GetByIdAsync(otherUserId);
                if (user != null)
                {
                    blockedUsers.Add(user);
                }
            }

            return blockedUsers;
        }

        public async Task<bool> IsBlockedAsync(Guid blockerId, Guid targetId)
        {
            var relation = await _friendRepository.GetAsync(blockerId, targetId);
            return relation != null && relation.Status == FriendStatus.Blocked;
        }
    }
}
