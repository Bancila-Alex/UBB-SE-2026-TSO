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
        private readonly BlockService _blockService;
        private readonly DirectMessageService _directMessageService;

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

        public RelayCommand GoToConversationsCommand { get; }
        public RelayCommand GoToFriendsCommand { get; }
        public RelayCommand GoToProfileCommand { get; }
        public RelayCommand LogoutCommand { get; }

        public MainViewModel(
            ConversationListService conversationListService,
            FriendRequestService friendRequestService,
            BlockService blockService,
            DirectMessageService directMessageService)
        {
            _conversationListService = conversationListService ?? throw new ArgumentNullException(nameof(conversationListService));
            _friendRequestService = friendRequestService ?? throw new ArgumentNullException(nameof(friendRequestService));
            _blockService = blockService ?? throw new ArgumentNullException(nameof(blockService));
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
            CurrentPage = new FriendRequestsViewModel(_friendRequestService, CurrentUserId);
            return Task.CompletedTask;
        }

        private Task GoToProfileAsync()
        {
            CurrentPage = new ProfileViewModel(_friendRequestService, _blockService, _directMessageService, CurrentUserId);
            return Task.CompletedTask;
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
