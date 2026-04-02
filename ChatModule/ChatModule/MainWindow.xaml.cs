using ChatModule.Repositories;
using ChatModule.Services;
using ChatModule.ViewModels;
using ChatModule.src.view_models;
using ChatModule.src.views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace ChatModule
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private readonly Guid _initialUserId;
        private readonly string _initialUsername;

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
                ConversationListViewModel vm => new ConversationListView(vm),
                FriendListViewModel _ => new UserControl
                {
                    Content = new TextBlock
                    {
                        Text = "Friend list is temporarily unavailable.",
                        Margin = new Thickness(24),
                        FontSize = 18
                    }
                },
                FriendRequestsViewModel vm => new FriendRequestsView(vm),
                ProfileViewModel vm => new ProfileView(vm),
                _ => null
            };

            CurrentPageHost.Content = view;
        }
    }
}
