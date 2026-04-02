using System;
using System.Threading.Tasks;
using ChatModule.Services;
using ChatModule.src.view_models;

namespace ChatModule.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ConversationListService _conversationListService;
        private readonly FriendRequestService _friendRequestService;
        private readonly FriendListService? _friendListService;
        private readonly BlockService _blockService;
        private readonly DirectMessageService _directMessageService;
        private readonly ProfileService _profileService;

        private Guid _currentUserId;
        public Guid CurrentUserId
        {
            get => _currentUserId;
            private set => Set(ref _currentUserId, value);
        }

        private string _currentUsername = string.Empty;
        public string CurrentUsername
        {
            get => _currentUsername;
            private set => Set(ref _currentUsername, value);
        }

        private BaseViewModel? _currentPage;
        public BaseViewModel? CurrentPage
        {
            get => _currentPage;
            private set => Set(ref _currentPage, value);
        }

        public event Action? NavigateToLoginRequested;
        public event Action<Guid>? NavigateToChatRequested;

        public RelayCommand GoToConversationsCommand { get; }
        public RelayCommand GoToFriendsCommand { get; }
        public RelayCommand GoToProfileCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public MainViewModel(
            ConversationListService conversationListService,
            FriendRequestService friendRequestService,
            BlockService blockService,
            ProfileService profileService,
            DirectMessageService directMessageService)
            : this(
                conversationListService,
                friendRequestService,
                friendListService: null,
                blockService,
                profileService,
                directMessageService)
        {
        }

        public MainViewModel(
            ConversationListService conversationListService,
            FriendRequestService friendRequestService,
            FriendListService? friendListService,
            BlockService blockService,
            ProfileService profileService,
            DirectMessageService directMessageService)
        {
            _conversationListService = conversationListService ?? throw new ArgumentNullException(nameof(conversationListService));
            _friendRequestService = friendRequestService ?? throw new ArgumentNullException(nameof(friendRequestService));
            _friendListService = friendListService;
            _blockService = blockService ?? throw new ArgumentNullException(nameof(blockService));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _directMessageService = directMessageService ?? throw new ArgumentNullException(nameof(directMessageService));

            GoToConversationsCommand = new RelayCommand(GoToConversationsAsync);
            GoToFriendsCommand = new RelayCommand(GoToFriendsAsync);
            GoToProfileCommand = new RelayCommand(GoToProfileAsync);
            LogoutCommand = new RelayCommand(LogoutAsync);
        }

        public Task InitialiseAsync(Guid userId, string username)
        {
            CurrentUserId = userId;
            CurrentUsername = username;
            CurrentPage = new ConversationListViewModel(_conversationListService, CurrentUserId);
            return Task.CompletedTask;
        }

        private Task GoToConversationsAsync()
        {
            CurrentPage = new ConversationListViewModel(_conversationListService, CurrentUserId);
            return Task.CompletedTask;
        }

        private Task GoToFriendsAsync()
        {
            if (_friendListService != null)
            {
                var friendListViewModel = new FriendListViewModel(_friendListService, _directMessageService, CurrentUserId);
                friendListViewModel.NavigateToProfileRequested += OnNavigateToProfileFromFriends;
                friendListViewModel.NavigateToChatRequested += OnNavigateToChatFromFriends;
                CurrentPage = friendListViewModel;
                return Task.CompletedTask;
            }

            CurrentPage = new FriendRequestsViewModel(_friendRequestService, CurrentUserId);
            return Task.CompletedTask;
        }

        private Task GoToProfileAsync()
            => ShowProfileAsync(CurrentUserId);

        private void OnNavigateToProfileFromFriends(Guid userId)
        {
            _ = ShowProfileAsync(userId);
        }

        private void OnNavigateToChatFromFriends(Guid conversationId)
        {
            NavigateToChatRequested?.Invoke(conversationId);
        }

        private async Task ShowProfileAsync(Guid targetUserId)
        {
            var profileViewModel = new ProfileViewModel(_friendRequestService, _blockService, _directMessageService, _profileService, CurrentUserId);
            await profileViewModel.LoadAsync(targetUserId);
            CurrentPage = profileViewModel;
        }

        private Task LogoutAsync()
        {
            CurrentUserId = Guid.Empty;
            CurrentUsername = string.Empty;
            CurrentPage = null;
            NavigateToLoginRequested?.Invoke();
            return Task.CompletedTask;
        }
    }
}
