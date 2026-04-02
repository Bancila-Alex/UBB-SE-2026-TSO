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
        private readonly FriendRequestService _friendRequestService;
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
        public event Action? OpenRequestsRequested;

        public RelayCommand LoadCommand { get; }
        public RelayCommand<Guid> OpenDmCommand { get; }
        public RelayCommand<Guid> ViewProfileCommand { get; }
        public RelayCommand<Guid> RemoveFriendCommand { get; }
        public RelayCommand SendFriendRequestCommand { get; }
        public RelayCommand OpenRequestsCommand { get; }

        private string _friendUsernameInput = string.Empty;
        public string FriendUsernameInput
        {
            get => _friendUsernameInput;
            set => Set(ref _friendUsernameInput, value);
        }

        private string? _friendActionMessage;
        public string? FriendActionMessage
        {
            get => _friendActionMessage;
            set => Set(ref _friendActionMessage, value);
        }

        public FriendListViewModel(
            FriendListService friendListService,
            FriendRequestService friendRequestService,
            Guid currentUserId)
        {
            _friendListService = friendListService ?? throw new ArgumentNullException(nameof(friendListService));
            _friendRequestService = friendRequestService ?? throw new ArgumentNullException(nameof(friendRequestService));
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
            SendFriendRequestCommand = new RelayCommand(SendFriendRequestAsync);
            OpenRequestsCommand = new RelayCommand(OpenRequestsAsync);
        }

        public FriendListViewModel(
            FriendListService friendListService,
            FriendRequestService friendRequestService,
            DirectMessageService directMessageService,
            Guid currentUserId)
            : this(friendListService, friendRequestService, currentUserId)
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

        private async Task SendFriendRequestAsync()
        {
            FriendActionMessage = null;

            if (string.IsNullOrWhiteSpace(FriendUsernameInput))
            {
                FriendActionMessage = "Enter a username first.";
                return;
            }

            try
            {
                var sent = await _friendRequestService.SendRequestByUsernameAsync(_currentUserId, FriendUsernameInput);
                if (!sent)
                {
                    FriendActionMessage = "User not found.";
                    return;
                }

                FriendActionMessage = "Friend request sent.";
                FriendUsernameInput = string.Empty;
            }
            catch (Exception ex)
            {
                FriendActionMessage = ex.Message;
            }
        }

        private Task OpenRequestsAsync()
        {
            OpenRequestsRequested?.Invoke();
            return Task.CompletedTask;
        }
    }
}
