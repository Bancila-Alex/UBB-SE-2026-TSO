using ChatModule.Repositories;
using ChatModule.Services;
using ChatModule.ViewModels;
using ChatModule.src.view_models;
using ChatModule.src.views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace ChatModule
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private readonly Guid _initialUserId;
        private readonly string _initialUsername;
        private readonly UserRepository _userRepository;
        private readonly ConversationRepository _conversationRepository;
        private readonly ParticipantRepository _participantRepository;
        private readonly MessageRepository _messageRepository;
        private readonly DirectMessageService _directMessageService;
        private readonly GroupService _groupService;
        private readonly SearchService _searchService;
        private readonly MessageService _messageService;
        private readonly MessageInteractionService _messageInteractionService;
        private readonly ReadReceiptService _readReceiptService;
        private readonly MentionService _mentionService;

        public MainWindow()
            : this(Guid.Empty, "guest")
        {
        }

        public MainWindow(Guid userId, string username)
        {
            _initialUserId = userId;
            _initialUsername = username;

            var db = (Application.Current as App)?.DatabaseManager
                     ?? new DatabaseManager("Data Source=localhost;Initial Catalog=ChatModule;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;");

            var userRepository = new UserRepository(db);
            var friendRepository = new FriendRepository(db);
            var conversationRepository = new ConversationRepository(db);
            var participantRepository = new ParticipantRepository(db);
            var messageRepository = new MessageRepository(db);

            var conversationListService = new ConversationListService(conversationRepository, participantRepository, messageRepository, userRepository);
            var friendRequestService = new FriendRequestService(friendRepository, userRepository, conversationRepository, participantRepository);
            var friendListService = new FriendListService(friendRepository, userRepository);
            var blockService = new BlockService(friendRepository, userRepository);
            var profileService = new ProfileService(userRepository, friendRepository);
            var directMessageService = new DirectMessageService(conversationRepository, participantRepository, friendRepository, userRepository);
            var groupService = new GroupService(conversationRepository, participantRepository, messageRepository, userRepository);
            var searchService = new SearchService(messageRepository, participantRepository, userRepository);
            var messageService = new MessageService(messageRepository, participantRepository, userRepository);
            var messageInteractionService = new MessageInteractionService(messageRepository, participantRepository, userRepository);
            var readReceiptService = new ReadReceiptService(participantRepository);
            var mentionService = new MentionService(participantRepository, userRepository);

            _userRepository = userRepository;
            _conversationRepository = conversationRepository;
            _participantRepository = participantRepository;
            _messageRepository = messageRepository;
            _directMessageService = directMessageService;
            _groupService = groupService;
            _searchService = searchService;
            _messageService = messageService;
            _messageInteractionService = messageInteractionService;
            _readReceiptService = readReceiptService;
            _mentionService = mentionService;

            ViewModel = new MainViewModel(
                conversationListService,
                friendRequestService,
                friendListService,
                blockService,
                profileService,
                directMessageService);

            InitializeComponent();

            ViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.CurrentPage))
                {
                    RenderCurrentPage();
                }
            };
            ViewModel.NavigateToChatRequested += conversationId => _ = OpenChatAsync(conversationId);

            ViewModel.NavigateToLoginRequested += () =>
            {
                var db = (Application.Current as App)?.DatabaseManager
                         ?? new DatabaseManager("Data Source=localhost;Initial Catalog=ChatModule;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;");
                var loginWindow = new LoginWindow(new AuthService(new UserRepository(db)));
                loginWindow.LoginSucceeded += async (userId, username) =>
                {
                    var nextMain = new MainWindow(userId, username);
                    nextMain.Activate();
                    loginWindow.Close();
                    Close();
                    await System.Threading.Tasks.Task.CompletedTask;
                };
                loginWindow.Activate();
            };

            _ = InitialiseAndRenderAsync();
        }

        private async System.Threading.Tasks.Task InitialiseAndRenderAsync()
        {
            await ViewModel.InitialiseAsync(_initialUserId, _initialUsername);
            RenderCurrentPage();
        }

        private void RenderCurrentPage()
        {
            UserControl? view = ViewModel.CurrentPage switch
            {
                ConversationListViewModel vm => BuildConversationListView(vm),
                FriendListViewModel vm => new FriendListView(vm),
                FriendRequestsViewModel vm => new FriendRequestsView(vm),
                ProfileViewModel vm => BuildProfileView(vm),
                ChatViewModel vm => new ChatView(vm),
                _ => null
            };

            CurrentPageHost.Content = view;
        }

        private ConversationListView BuildConversationListView(ConversationListViewModel vm)
        {
            vm.NewGroupRequested -= OnNewGroupRequested;
            vm.NewDmRequested -= OnNewDmRequested;
            vm.ConversationOpened -= OnConversationOpened;

            vm.NewGroupRequested += OnNewGroupRequested;
            vm.NewDmRequested += OnNewDmRequested;
            vm.ConversationOpened += OnConversationOpened;

            return new ConversationListView(vm);
        }

        private ProfileView BuildProfileView(ProfileViewModel vm)
        {
            vm.NavigateToChatRequested -= OnConversationOpened;
            vm.NavigateToChatRequested += OnConversationOpened;
            return new ProfileView(vm);
        }

        private void OnConversationOpened(Guid conversationId)
        {
            _ = OpenChatAsync(conversationId);
        }

        private void OnNewGroupRequested()
        {
            _ = ShowCreateGroupDialogAsync();
        }

        private void OnNewDmRequested()
        {
            _ = ShowCreateDmDialogAsync();
        }

        private async Task ShowCreateGroupDialogAsync()
        {
            var createGroupViewModel = new CreateGroupViewModel(_groupService, _searchService, ViewModel.CurrentUserId);
            var dialog = new CreateGroupDialog(createGroupViewModel)
            {
                XamlRoot = CurrentPageHost.XamlRoot
            };

            _ = await dialog.ShowAsync();

            if (dialog.CreatedConversation != null)
            {
                await OpenChatAsync(dialog.CreatedConversation.Id);
            }
        }

        private async Task ShowCreateDmDialogAsync()
        {
            if (CurrentPageHost.XamlRoot == null)
            {
                return;
            }

            var usernameBox = new TextBox
            {
                PlaceholderText = "Enter username",
                Margin = new Thickness(0, 8, 0, 0)
            };

            var dialog = new ContentDialog
            {
                Title = "Start New DM",
                Content = usernameBox,
                PrimaryButtonText = "Start",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = CurrentPageHost.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var username = usernameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || user.Id == ViewModel.CurrentUserId)
            {
                await ShowInfoDialogAsync("User not found", "Enter another username to start a DM.");
                return;
            }

            var conversation = await _directMessageService.GetOrCreateAsync(ViewModel.CurrentUserId, user.Id);
            await OpenChatAsync(conversation.Id);
        }

        private async Task ShowInfoDialogAsync(string title, string message)
        {
            if (CurrentPageHost.XamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = CurrentPageHost.XamlRoot
            };

            _ = await dialog.ShowAsync();
        }

        private async Task OpenChatAsync(Guid conversationId)
        {
            var chatViewModel = new ChatViewModel(
                _messageService,
                _messageInteractionService,
                _readReceiptService,
                _mentionService,
                _directMessageService,
                _conversationRepository,
                ViewModel.CurrentUserId);

            await chatViewModel.LoadAsync(conversationId);
            CurrentPageHost.Content = new ChatView(chatViewModel);
        }
    }
}
