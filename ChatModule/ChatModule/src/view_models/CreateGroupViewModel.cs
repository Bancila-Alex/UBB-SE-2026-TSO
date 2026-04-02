using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.ViewModels;
using ChatModule.src.domain;

namespace ChatModule.src.view_models
{
    public class CreateGroupViewModel : BaseViewModel
    {
        private readonly GroupService _groupService;
        private readonly SearchService _searchService;
        private readonly Guid _currentUserId;

        public ObservableCollection<User> SelectedMembers { get; } = new();
        public ObservableCollection<User> MemberSearchResults { get; } = new();

        private string _groupName = string.Empty;
        public string GroupName
        {
            get => _groupName;
            set => Set(ref _groupName, value);
        }

        private string? _iconUrl;
        public string? IconUrl
        {
            get => _iconUrl;
            set => Set(ref _iconUrl, value);
        }

        private string _memberSearchQuery = string.Empty;
        public string MemberSearchQuery
        {
            get => _memberSearchQuery;
            set
            {
                if (Set(ref _memberSearchQuery, value))
                    _ = SearchMembersAsync();
            }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public RelayCommand CreateCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand<User> AddMemberCommand { get; }
        public RelayCommand<User> RemoveMemberCommand { get; }

        public event Action<Conversation>? GroupCreated;
        public event Action? Cancelled;

        public CreateGroupViewModel(
            GroupService groupService,
            SearchService searchService,
            Guid currentUserId)
        {
            _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _currentUserId = currentUserId;

            CreateCommand = new RelayCommand(CreateGroupAsync);
            CancelCommand = new RelayCommand(CancelAsync);
            AddMemberCommand = new RelayCommand<User>(AddMemberAsync);
            RemoveMemberCommand = new RelayCommand<User>(RemoveMemberAsync);
        }

        private async Task SearchMembersAsync()
        {
            MemberSearchResults.Clear();

            if (string.IsNullOrWhiteSpace(_memberSearchQuery))
                return;

            var alreadyAdded = SelectedMembers.Select(u => u.Id).ToHashSet();
            var results = await _searchService.SearchUsersAsync(_memberSearchQuery);
            foreach (var user in results.Where(u => u.Id != _currentUserId && !alreadyAdded.Contains(u.Id)))
                MemberSearchResults.Add(user);
        }

        private Task AddMemberAsync(User user)
        {
            if (SelectedMembers.All(u => u.Id != user.Id))
                SelectedMembers.Add(user);

            MemberSearchResults.Remove(user);
            return Task.CompletedTask;
        }

        private Task RemoveMemberAsync(User user)
        {
            SelectedMembers.Remove(user);
            return Task.CompletedTask;
        }

        private async Task CreateGroupAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(_groupName))
            {
                ErrorMessage = "Please enter a group name.";
                return;
            }

            try
            {
                var memberIds = SelectedMembers.Select(u => u.Id).ToList();
                var conversation = await _groupService.CreateGroupAsync(_currentUserId, _groupName.Trim(), _iconUrl, memberIds);
                GroupCreated?.Invoke(conversation);
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private Task CancelAsync()
        {
            Cancelled?.Invoke();
            return Task.CompletedTask;
        }
    }
}
