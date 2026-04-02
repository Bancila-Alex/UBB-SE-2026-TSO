using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.src.domain.Enums;

namespace ChatModule.Services
{
    public class MemberPanelService
    {
        private readonly ParticipantRepository _participantRepo;
        private readonly UserRepository _userRepo;
        private readonly FriendRepository _friendRepo;

        public MemberPanelService(
            ParticipantRepository participantRepo,
            UserRepository userRepo,
            FriendRepository friendRepo)
        {
            _participantRepo = participantRepo;
            _userRepo = userRepo;
            _friendRepo = friendRepo;
        }


        public async Task<List<Participant>> GetMembersAsync(Guid conversationId)
        {
            return await _participantRepo.GetAllForConversationAsync(conversationId);
        }

        public async Task<List<Participant>> GetBannedMembersAsync(Guid conversationId)
        {
            var participants = await _participantRepo.GetAllForConversationAsync(conversationId);
            return participants.Where(p => p.Role == ParticipantRole.Banned).ToList();
        }

    }
}
