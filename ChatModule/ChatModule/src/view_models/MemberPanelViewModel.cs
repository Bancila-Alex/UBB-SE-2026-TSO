using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.ViewModels;

namespace ChatModule.src.view_models
{
    public class MemberPanelViewModel : BaseViewModel
    {
        private readonly MemberPanelService _memberPanelService;
        private readonly ModerationService _moderationService;
        private readonly SearchService _searchService;
        private readonly Guid _currentUserId;
        private Guid _conversationId;

        public ObservableCollection<Participant> Members { get; } = new();
        public ObservableCollection<User> AddMemberResults { get; } = new();

        private string _addMemberQuery = string.Empty;
        public string AddMemberQuery
        {
            get => _addMemberQuery;
            set
            {
                if (Set(ref _addMemberQuery, value))
                    _ = SearchUsersAsync();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        public RelayCommand LoadCommand { get; }
        public RelayCommand<Guid> AddMemberCommand { get; }

        public MemberPanelViewModel(
            MemberPanelService memberPanelService,
            ModerationService moderationService,
            SearchService searchService,
            Guid currentUserId)
        {
            _memberPanelService = memberPanelService ?? throw new ArgumentNullException(nameof(memberPanelService));
            _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _currentUserId = currentUserId;

            LoadCommand = new RelayCommand(LoadAsync);
            AddMemberCommand = new RelayCommand<Guid>(AddMemberAsync);
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
                Members.Clear();
                foreach (var member in members)
                {
                    Members.Add(member);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchUsersAsync()
        {
            AddMemberResults.Clear();

            if (string.IsNullOrWhiteSpace(_addMemberQuery))
            {
                return;
            }

            var results = await _searchService.SearchUsersForAddMemberAsync(_conversationId, _addMemberQuery);
            foreach (var user in results)
            {
                AddMemberResults.Add(user);
            }
        }

        private async Task AddMemberAsync(Guid userId)
        {
            await _moderationService.AddMemberAsync(_conversationId, _currentUserId, userId);
            AddMemberQuery = string.Empty;
            AddMemberResults.Clear();
            await LoadAsync();
        }
    }
}
