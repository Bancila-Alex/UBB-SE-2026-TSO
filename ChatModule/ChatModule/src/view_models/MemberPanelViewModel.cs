using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.src.domain.Enums;
using ChatModule.ViewModels;

namespace ChatModule.src.view_models
{
    public class MemberPanelViewModel : BaseViewModel
    {
        private readonly MemberPanelService _memberPanelService;
        private readonly ModerationService _moderationService;
        private readonly Guid _currentUserId;
        private Guid _conversationId;

        public ObservableCollection<Participant> Members { get; } = new();
        public ObservableCollection<Participant> BannedMembers { get; } = new();
        public ObservableCollection<User> AddMemberResults { get; } = new();

        private Participant? _selectedMember;
        public Participant? SelectedMember
        {
            get => _selectedMember;
            set => Set(ref _selectedMember, value);
        }

        private User? _selectedAddMember;
        public User? SelectedAddMember
        {
            get => _selectedAddMember;
            set => Set(ref _selectedAddMember, value);
        }

        private string _addMemberQuery = string.Empty;
        public string AddMemberQuery
        {
            get => _addMemberQuery;
            set
            {
                if (Set(ref _addMemberQuery, value))
                {
                    _ = SearchUsersToAddAsync();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            private set => Set(ref _isAdmin, value);
        }

        public RelayCommand LoadCommand { get; }
        public RelayCommand AddMemberCommand { get; }
        public RelayCommand ViewProfileCommand { get; }
        public RelayCommand<Guid> BanMemberCommand { get; }
        public RelayCommand<Guid> UnbanMemberCommand { get; }
        public RelayCommand<Guid> TimeoutMemberCommand { get; }
        public RelayCommand RemoveTimeoutCommand { get; }
        public RelayCommand<Guid> PromoteCommand { get; }
        public RelayCommand<Guid> DemoteCommand { get; }

        public event Action<Guid>? NavigateToProfileRequested;

        public Func<Task<TimeSpan?>>? RequestTimeoutDurationAsync { get; set; }

        public MemberPanelViewModel(
            MemberPanelService memberPanelService,
            ModerationService moderationService,
            Guid currentUserId)
        {
            _memberPanelService = memberPanelService ?? throw new ArgumentNullException(nameof(memberPanelService));
            _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
            _currentUserId = currentUserId;

            LoadCommand = new RelayCommand(LoadAsync);
            AddMemberCommand = new RelayCommand(AddMemberAsync);
            ViewProfileCommand = new RelayCommand(ViewProfileAsync);
            BanMemberCommand = new RelayCommand<Guid>(BanMemberAsync);
            UnbanMemberCommand = new RelayCommand<Guid>(UnbanMemberAsync);
            TimeoutMemberCommand = new RelayCommand<Guid>(TimeoutMemberAsync);
            RemoveTimeoutCommand = new RelayCommand(RemoveTimeoutAsync);
            PromoteCommand = new RelayCommand<Guid>(PromoteAsync);
            DemoteCommand = new RelayCommand<Guid>(DemoteAsync);
        }

        public async Task InitializeAsync(Guid conversationId)
        {
            _conversationId = conversationId;
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var members = await _memberPanelService.GetMembersAsync(_conversationId);
                var bannedMembers = await _memberPanelService.GetBannedMembersAsync(_conversationId);

                IsAdmin = members.Any(member => member.UserId == _currentUserId && member.Role == ParticipantRole.Admin);

                Members.Clear();
                foreach (var member in members.Where(member => member.Role != ParticipantRole.Banned))
                {
                    Members.Add(member);
                }

                BannedMembers.Clear();
                foreach (var banned in bannedMembers)
                {
                    BannedMembers.Add(banned);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchUsersToAddAsync()
        {
            AddMemberResults.Clear();
            SelectedAddMember = null;

            if (string.IsNullOrWhiteSpace(_addMemberQuery))
            {
                return;
            }

            var results = await _memberPanelService.SearchUsersToAddAsync(_conversationId, _addMemberQuery);
            foreach (var user in results)
            {
                AddMemberResults.Add(user);
            }
        }

        private async Task AddMemberAsync()
        {
            if (SelectedAddMember == null)
            {
                return;
            }

            await _moderationService.AddMemberAsync(_conversationId, _currentUserId, SelectedAddMember.Id);
            AddMemberQuery = string.Empty;
            AddMemberResults.Clear();
            SelectedAddMember = null;
            await LoadAsync();
        }

        private Task ViewProfileAsync()
        {
            if (SelectedAddMember == null)
            {
                return Task.CompletedTask;
            }

            NavigateToProfileRequested?.Invoke(SelectedAddMember.Id);
            return Task.CompletedTask;
        }

        private async Task BanMemberAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            await _moderationService.BanMemberAsync(_conversationId, _currentUserId, userId);
            await LoadAsync();
        }

        private async Task UnbanMemberAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            await _moderationService.UnbanMemberAsync(_conversationId, _currentUserId, userId);
            await LoadAsync();
        }

        private async Task TimeoutMemberAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            var duration = await ChooseTimeoutDurationAsync();
            if (!duration.HasValue)
            {
                return;
            }

            await _moderationService.TimeoutMemberAsync(_conversationId, _currentUserId, userId, duration.Value);
            await LoadAsync();
        }

        private async Task RemoveTimeoutAsync()
        {
            if (SelectedMember == null)
            {
                return;
            }

            await _moderationService.RemoveTimeoutAsync(_conversationId, _currentUserId, SelectedMember.UserId);
            await LoadAsync();
        }

        private async Task PromoteAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            await _moderationService.PromoteMemberAsync(_conversationId, _currentUserId, userId);
            await LoadAsync();
        }

        private async Task DemoteAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            await _moderationService.DemoteMemberAsync(_conversationId, _currentUserId, userId);
            await LoadAsync();
        }

        private async Task<TimeSpan?> ChooseTimeoutDurationAsync()
        {
            if (RequestTimeoutDurationAsync != null)
            {
                return await RequestTimeoutDurationAsync();
            }

            return TimeSpan.FromMinutes(10);
        }
    }
}
