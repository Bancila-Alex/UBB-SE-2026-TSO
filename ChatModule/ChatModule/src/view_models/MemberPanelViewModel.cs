using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.src.domain.Enums;
using ChatModule.ViewModels;
using ChatModule.src.domain.Enums;

namespace ChatModule.src.view_models
{
    public class MemberPanelViewModel : BaseViewModel
    {
        private readonly MemberPanelService _memberPanelService;
        private readonly ModerationService _moderationService;
        private readonly Guid _currentUserId;

        public Guid ConversationId { get; private set; }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            private set => Set(ref _isAdmin, value);
        }

        private bool _isPanelVisible = true;
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set
            {
                if (Set(ref _isPanelVisible, value))
                    OnPropertyChanged(nameof(TogglePanelIcon));
            }
        }

        public string TogglePanelIcon => IsPanelVisible ? "◀" : "▶";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        public ObservableCollection<MemberDisplayItem> Members { get; } = new();
        public ObservableCollection<MemberDisplayItem> BannedMembers { get; } = new();
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

        private User? _selectedAddMember;
        public User? SelectedAddMember
        {
            get => _selectedAddMember;
            set => Set(ref _selectedAddMember, value);
        }

        public RelayCommand LoadCommand { get; }
        public RelayCommand TogglePanelCommand { get; }
        public RelayCommand AddMemberCommand { get; }
        public RelayCommand<Guid> BanMemberCommand { get; }
        public RelayCommand<Guid> UnbanMemberCommand { get; }
        public RelayCommand<Guid> TimeoutMemberCommand { get; }
        public RelayCommand<Guid> RemoveTimeoutCommand { get; }
        public RelayCommand<Guid> PromoteCommand { get; }
        public RelayCommand<Guid> DemoteCommand { get; }
        public RelayCommand<Guid> ViewProfileCommand { get; }

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

            LoadCommand = new RelayCommand(LoadMembersAsync);
            TogglePanelCommand = new RelayCommand(TogglePanelAsync);
            AddMemberCommand = new RelayCommand(AddMemberAsync);
            BanMemberCommand = new RelayCommand<Guid>(BanMemberAsync);
            UnbanMemberCommand = new RelayCommand<Guid>(UnbanMemberAsync);
            TimeoutMemberCommand = new RelayCommand<Guid>(OpenTimeoutDialogAsync);
            RemoveTimeoutCommand = new RelayCommand<Guid>(RemoveTimeoutAsync);
            PromoteCommand = new RelayCommand<Guid>(PromoteMemberAsync);
            DemoteCommand = new RelayCommand<Guid>(DemoteMemberAsync);
            ViewProfileCommand = new RelayCommand<Guid>(OpenProfileAsync);
        }

        public async Task LoadAsync(Guid conversationId)
        {
            ConversationId = conversationId;
            await LoadMembersAsync();
        }

        private async Task LoadMembersAsync()
        {
            IsLoading = true;
            try
            {
                var participants = await _memberPanelService.GetMembersAsync(ConversationId);
                Members.Clear();
                BannedMembers.Clear();
                IsAdmin = false;

                foreach (var participant in participants)
                {
                    var user = await _memberPanelService.GetUserAsync(participant.UserId);
                    if (user == null) continue;

                    var item = new MemberDisplayItem
                    {
                        UserId = participant.UserId,
                        Username = user.Username,
                        AvatarUrl = user.AvatarUrl,
                        Status = user.Status,
                        Role = participant.Role,
                        HasTimeout = participant.TimeoutUntil.HasValue && participant.TimeoutUntil > DateTime.UtcNow
                    };

                    if (participant.Role == ParticipantRole.Banned)
                        BannedMembers.Add(item);
                    else
                        Members.Add(item);

                    if (participant.UserId == _currentUserId && participant.Role == ParticipantRole.Admin)
                        IsAdmin = true;
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

        private Task TogglePanelAsync()
        {
            IsPanelVisible = !IsPanelVisible;
            return Task.CompletedTask;
        }

        private async Task SearchUsersToAddAsync()
        {
            AddMemberResults.Clear();
            SelectedAddMember = null;

            if (string.IsNullOrWhiteSpace(_addMemberQuery))
                return;

            var results = await _memberPanelService.SearchUsersToAddAsync(ConversationId, _addMemberQuery);
            foreach (var user in results)
                AddMemberResults.Add(user);
        }

        private async Task AddMemberAsync()
        {
            if (SelectedAddMember == null) return;
            await _moderationService.AddMemberAsync(ConversationId, _currentUserId, SelectedAddMember.Id);
            AddMemberQuery = string.Empty;
            AddMemberResults.Clear();
            SelectedAddMember = null;
            await LoadMembersAsync();
        }

        private async Task BanMemberAsync(Guid userId)
        {
            await _moderationService.BanMemberAsync(ConversationId, _currentUserId, userId);
            await LoadMembersAsync();
        }

        private async Task UnbanMemberAsync(Guid userId)
        {
            await _moderationService.UnbanMemberAsync(ConversationId, _currentUserId, userId);
            await LoadMembersAsync();
        }

        private async Task OpenTimeoutDialogAsync(Guid userId)
        {
            await _moderationService.TimeoutMemberAsync(ConversationId, _currentUserId, userId, TimeSpan.FromHours(1));
            await LoadMembersAsync();
        }

        private async Task RemoveTimeoutAsync(Guid userId)
        {
            await _moderationService.RemoveTimeoutAsync(ConversationId, _currentUserId, userId);
            await LoadMembersAsync();
        }

        private async Task PromoteMemberAsync(Guid userId)
        {
            await _moderationService.PromoteMemberAsync(ConversationId, _currentUserId, userId);
            await LoadMembersAsync();
        }

        private async Task DemoteMemberAsync(Guid userId)
        {
            await _moderationService.DemoteMemberAsync(ConversationId, _currentUserId, userId);
            await LoadMembersAsync();
        }

        private Task OpenProfileAsync(Guid userId)
        {
            NavigateToProfileRequested?.Invoke(userId);
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
