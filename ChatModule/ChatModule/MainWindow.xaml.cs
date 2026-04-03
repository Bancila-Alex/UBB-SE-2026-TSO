using ChatModule.Repositories;
using ChatModule.Services;
using ChatModule.ViewModels;
using ChatModule.src.view_models;
using ChatModule.src.views;
using ChatModule.src.domain.Enums;
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
        private readonly FriendRequestService _friendRequestService;
        private readonly BlockService _blockService;
        private readonly ProfileService _profileService;
        private readonly MemberPanelService _memberPanelService;
        private readonly ModerationService _moderationService;

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
            var directMessageService = new DirectMessageService(conversationRepository, participantRepository, friendRepository, userRepository, messageRepository);
            var groupService = new GroupService(conversationRepository, participantRepository, messageRepository, userRepository);
            var searchService = new SearchService(messageRepository, participantRepository, userRepository);
            var messageService = new MessageService(messageRepository, participantRepository, userRepository, conversationRepository);
            var messageInteractionService = new MessageInteractionService(messageRepository, participantRepository, userRepository);
            var readReceiptService = new ReadReceiptService(participantRepository, messageRepository, userRepository);
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
            _friendRequestService = friendRequestService;
            _blockService = blockService;
            _profileService = profileService;
            _memberPanelService = new MemberPanelService(participantRepository, userRepository, friendRepository);
            _moderationService = new ModerationService(participantRepository, messageRepository, userRepository);

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
                    SafeRenderCurrentPage();
                }
            };
            ViewModel.NavigateToChatRequested += conversationId => _ = OpenChatAsync(conversationId);

            ViewModel.NavigateToLoginRequested += () =>
            {
                var db = (Application.Current as App)?.DatabaseManager
                         ?? new DatabaseManager("Data Source=localhost;Initial Catalog=ChatModule;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;");
                var loginWindow = new LoginWindow(new AuthService(new UserRepository(db)));
                loginWindow.LoginSucceeded += (userId, username) =>
                {
                    var nextMain = new MainWindow(userId, username);
                    App.SetMainWindow(nextMain);
                    nextMain.Activate();
                    loginWindow.Close();
                    Close();
                    return Task.CompletedTask;
                };
                loginWindow.Activate();
            };

            _ = InitialiseAndRenderAsync();
        }

        private async System.Threading.Tasks.Task InitialiseAndRenderAsync()
        {
            try
            {
                await ViewModel.InitialiseAsync(_initialUserId, _initialUsername);
                SafeRenderCurrentPage();
            }
            catch (Exception ex)
            {
                if (CurrentPageHost.XamlRoot != null)
                {
                    await ShowInfoDialogAsync("Startup error", ex.Message);
                }
            }
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

        private void SafeRenderCurrentPage()
        {
            try
            {
                RenderCurrentPage();
            }
            catch (Exception ex)
            {
                CurrentPageHost.Content = new TextBlock
                {
                    Text = $"Failed to render page: {ex.Message}",
                    Margin = new Thickness(16)
                };
            }
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

        private async Task<string?> ShowInputDialogAsync(string title, string placeholder)
        {
            var inputBox = new TextBox
            {
                PlaceholderText = placeholder,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = inputBox,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = CurrentPageHost.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            return inputBox.Text;
        }

        private async Task OpenChatAsync(Guid conversationId)
        {
            try
            {
                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                if (conversation == null)
                {
                    return;
                }

                var chatViewModel = new ChatViewModel(
                    _messageService,
                    _messageInteractionService,
                    _readReceiptService,
                    _mentionService,
                    _directMessageService,
                    _conversationRepository,
                    _searchService,
                    ViewModel.CurrentUserId);

                await chatViewModel.LoadAsync(conversationId);

                var chatView = new ChatView(chatViewModel);
                chatViewModel.LeaveGroupRequested += async () =>
                {
                    try
                    {
                        await _groupService.LeaveGroupAsync(conversationId, ViewModel.CurrentUserId);
                        await ShowInfoDialogAsync("Group", "You left the group.");
                        ViewModel.GoToConversationsCommand.Execute(null);
                    }
                    catch (Exception ex)
                    {
                        await ShowInfoDialogAsync("Unable to leave group", ex.Message);
                    }
                };
                chatViewModel.SetNicknameRequested += async () =>
                {
                    var nickname = await ShowInputDialogAsync("Set group nickname", "Nickname (max 16 chars)");
                    if (nickname == null)
                    {
                        return;
                    }

                    try
                    {
                        await _messageService.SetNicknameAsync(conversationId, ViewModel.CurrentUserId, nickname);
                        await chatViewModel.LoadAsync(conversationId);
                    }
                    catch (Exception ex)
                    {
                        await ShowInfoDialogAsync("Nickname", ex.Message);
                    }
                };

                chatViewModel.ClearNicknameRequested += async () =>
                {
                    try
                    {
                        await _messageService.SetNicknameAsync(conversationId, ViewModel.CurrentUserId, null);
                        await chatViewModel.LoadAsync(conversationId);
                    }
                    catch (Exception ex)
                    {
                        await ShowInfoDialogAsync("Nickname", ex.Message);
                    }
                };

                if (conversation.Type == ConversationType.Group)
                {
                    var memberPanelViewModel = new MemberPanelViewModel(_memberPanelService, _moderationService, ViewModel.CurrentUserId);
                    memberPanelViewModel.NavigateToProfileRequested += async userId =>
                    {
                        var profileVm = new ProfileViewModel(_friendRequestService, _blockService, _directMessageService, _profileService, ViewModel.CurrentUserId);
                        await profileVm.LoadAsync(userId);
                        var profilePanelVm = new ConversationSidePanelViewModel(ConversationType.Dm, profileVm, () =>
                        {
                            var membersPanelVm = new ConversationSidePanelViewModel(ConversationType.Group, memberPanelViewModel);
                            chatView.SetSidePanel(new ConversationSidePanelView(membersPanelVm));
                        });
                        chatView.SetSidePanel(new ConversationSidePanelView(profilePanelVm));
                    };
                    await memberPanelViewModel.LoadAsync(conversationId);
                    var sideVm = new ConversationSidePanelViewModel(ConversationType.Group, memberPanelViewModel);
                    chatView.SetSidePanel(new ConversationSidePanelView(sideVm));
                }
                else
                {
                    var otherUser = await _directMessageService.GetOtherUserAsync(conversationId, ViewModel.CurrentUserId);
                    if (otherUser != null)
                    {
                        var profileVm = new ProfileViewModel(_friendRequestService, _blockService, _directMessageService, _profileService, ViewModel.CurrentUserId);
                        await profileVm.LoadAsync(otherUser.Id);
                        var sideVm = new ConversationSidePanelViewModel(ConversationType.Dm, profileVm);
                        chatView.SetSidePanel(new ConversationSidePanelView(sideVm));
                    }
                }

                CurrentPageHost.Content = chatView;
            }
            catch (InvalidOperationException ex)
            {
                await ShowInfoDialogAsync("Unable to open conversation", ex.Message);
                ViewModel.GoToConversationsCommand.Execute(null);
            }
        }
    }
}
