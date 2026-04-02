using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatModule.Repositories;

namespace ChatModule.Services
{
    public class MentionService
    {
        private readonly ParticipantRepository _participantRepository;
        private readonly UserRepository _userRepository;

        public MentionService(ParticipantRepository participantRepository, UserRepository userRepository)
        {
            _participantRepository = participantRepository;
            _userRepository = userRepository;
        }

        public async Task<List<Guid>> ExtractMentionedUserIdsAsync(Guid conversationId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<Guid>();
            }

            var participants = await _participantRepository.GetAllForConversationAsync(conversationId);
            var memberIds = participants.Select(p => p.UserId).ToHashSet();

            var usernames = Regex.Matches(content, "@([A-Za-z0-9_.-]+)")
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            var mentionedUserIds = new HashSet<Guid>();
            foreach (var username in usernames)
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user != null && memberIds.Contains(user.Id))
                {
                    mentionedUserIds.Add(user.Id);
                }
            }

            return mentionedUserIds.ToList();
        }
    }
}
