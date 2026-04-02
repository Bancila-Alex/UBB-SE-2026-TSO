using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.ViewModels;

namespace ChatModule.src.view_models
{
    public class FriendListViewModel : BaseViewModel
    {
        private readonly FriendListService _friendListService;
        private DirectMessageService? _directMessageService;
        private readonly Guid _currentUserId;

        public ObservableCollection<User> Friends { get; } = new();
        public ObservableCollection<FriendListItemViewModel> FriendItems { get; } = new();

        public bool HasFriends => FriendItems.Count > 0;
        public bool ShowFriendList => !IsLoading && HasFriends;
        public bool ShowEmptyState => !IsLoading && !HasFriends;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (Set(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(ShowFriendList));
                    OnPropertyChanged(nameof(ShowEmptyState));
                }
            }
        }

        public event Action<Guid>? NavigateToChatRequested;
        public event Action<Guid>? NavigateToProfileRequested;

        public RelayCommand LoadCommand { get; }
        public RelayCommand<Guid> OpenDmCommand { get; }
        public RelayCommand<Guid> ViewProfileCommand { get; }
        public RelayCommand<Guid> RemoveFriendCommand { get; }

        public FriendListViewModel(FriendListService friendListService, Guid currentUserId)
        {
            _friendListService = friendListService ?? throw new ArgumentNullException(nameof(friendListService));
            _currentUserId = currentUserId;

            FriendItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasFriends));
                OnPropertyChanged(nameof(ShowFriendList));
                OnPropertyChanged(nameof(ShowEmptyState));
            };

            LoadCommand = new RelayCommand(LoadFriendsAsync);
            OpenDmCommand = new RelayCommand<Guid>(OpenDmAsync);
            ViewProfileCommand = new RelayCommand<Guid>(ViewProfileAsync);
            RemoveFriendCommand = new RelayCommand<Guid>(RemoveFriendAsync);
        }

        public FriendListViewModel(
            FriendListService friendListService,
            DirectMessageService directMessageService,
            Guid currentUserId)
            : this(friendListService, currentUserId)
        {
            _directMessageService = directMessageService ?? throw new ArgumentNullException(nameof(directMessageService));
        }

        public void SetDirectMessageService(DirectMessageService directMessageService)
        {
            _directMessageService = directMessageService ?? throw new ArgumentNullException(nameof(directMessageService));
        }

        private async Task LoadFriendsAsync()
        {
            IsLoading = true;
            try
            {
                var friends = await _friendListService.GetFriendsAsync(_currentUserId);
                Friends.Clear();
                FriendItems.Clear();
                foreach (var friend in friends)
                {
                    Friends.Add(friend);
                    FriendItems.Add(new FriendListItemViewModel(friend));
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OpenDmAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            if (_directMessageService == null)
            {
                return;
            }

            var conversation = await _directMessageService.GetOrCreateAsync(_currentUserId, userId);
            NavigateToChatRequested?.Invoke(conversation.Id);
        }

        private Task ViewProfileAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return Task.CompletedTask;
            }

            NavigateToProfileRequested?.Invoke(userId);
            return Task.CompletedTask;
        }

        private async Task RemoveFriendAsync(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            await _friendListService.RemoveFriendAsync(_currentUserId, userId);
            await LoadFriendsAsync();
        }
    }
}
