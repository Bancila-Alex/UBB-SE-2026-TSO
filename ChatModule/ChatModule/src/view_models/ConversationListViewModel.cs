using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChatModule.Services;
using ChatModule.src.domain;

namespace ChatModule.ViewModels
{
    public class ConversationListViewModel : BaseViewModel
    {
        public const string AllTab = "All";
        public const string DirectMessagesTab = "Direct Messages";
        public const string GroupsTab = "Groups";
        public const string FavoritesTab = "Favorites";
        public const string UnreadTab = "Unread";

        private readonly ConversationListService _conversationListService;
        private readonly Guid _currentUserId;

        public ObservableCollection<Conversation> Conversations { get; } = new();

        private Conversation? _selectedConversation;
        public Conversation? SelectedConversation
        {
            get => _selectedConversation;
            set => Set(ref _selectedConversation, value);
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (Set(ref _searchQuery, value))
                    _ = SearchAsync();
            }
        }

        private string _activeTab = AllTab;
        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                if (Set(ref _activeTab, value))
                {
                    OnActiveTabChanged();
                }
            }
        }

        public bool IsAllTabActive => string.Equals(_activeTab, AllTab, StringComparison.Ordinal);
        public bool IsDirectMessagesTabActive => string.Equals(_activeTab, DirectMessagesTab, StringComparison.Ordinal);
        public bool IsGroupsTabActive => string.Equals(_activeTab, GroupsTab, StringComparison.Ordinal);
        public bool IsFavoritesTabActive => string.Equals(_activeTab, FavoritesTab, StringComparison.Ordinal);
        public bool IsUnreadTabActive => string.Equals(_activeTab, UnreadTab, StringComparison.Ordinal);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (Set(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(HasConversations));
                    OnPropertyChanged(nameof(ShowConversationList));
                }
            }
        }

        public bool ShowEmptyState => !IsLoading && Conversations.Count == 0;
        public bool HasConversations => Conversations.Count > 0;
        public bool ShowConversationList => !IsLoading && HasConversations;

        public RelayCommand           LoadCommand             { get; }
        public RelayCommand<string>   SwitchTabCommand        { get; }
        public RelayCommand           NewGroupCommand         { get; }
        public RelayCommand           NewDmCommand            { get; }
        public RelayCommand<Guid>     ToggleFavouriteCommand  { get; }
        public RelayCommand<Guid>     OpenConversationCommand { get; }

        public event Action? NewGroupRequested;
        public event Action? NewDmRequested;
        public event Action<Guid>? ConversationOpened;

        public ConversationListViewModel(ConversationListService conversationListService, Guid currentUserId)
        {
            _conversationListService = conversationListService ?? throw new ArgumentNullException(nameof(conversationListService));
            _currentUserId = currentUserId;
            LoadCommand             = new RelayCommand(LoadTabAsync);
            SwitchTabCommand        = new RelayCommand<string>(SwitchTabAsync);
            NewGroupCommand         = new RelayCommand(RequestNewGroupAsync);
            NewDmCommand            = new RelayCommand(RequestNewDmAsync);
            ToggleFavouriteCommand  = new RelayCommand<Guid>(ToggleFavouriteAsync);
            OpenConversationCommand = new RelayCommand<Guid>(OpenConversationAsync);

            Conversations.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(HasConversations));
                OnPropertyChanged(nameof(ShowConversationList));
            };
        }

        private async Task LoadTabAsync()
        {
            IsLoading = true;
            try
            {
                var results = ActiveTab switch
                {
                    AllTab => await _conversationListService.GetAllAsync(_currentUserId),
                    DirectMessagesTab => await _conversationListService.GetDmsAsync(_currentUserId),
                    GroupsTab => await _conversationListService.GetGroupsAsync(_currentUserId),
                    FavoritesTab => await _conversationListService.GetFavouritesAsync(_currentUserId),
                    UnreadTab => await _conversationListService.GetUnreadAsync(_currentUserId),
                    _ => await _conversationListService.GetAllAsync(_currentUserId),
                };

                Conversations.Clear();
                foreach (var c in results)
                    Conversations.Add(c);
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(HasConversations));
                OnPropertyChanged(nameof(ShowConversationList));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                await LoadTabAsync();
                return;
            }

            IsLoading = true;
            try
            {
                var results = await _conversationListService.SearchAsync(_currentUserId, _searchQuery);
                Conversations.Clear();
                foreach (var c in results)
                    Conversations.Add(c);
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(HasConversations));
                OnPropertyChanged(nameof(ShowConversationList));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SwitchTabAsync(string tab)
        {
            ActiveTab = tab;
            _searchQuery = string.Empty;
            OnPropertyChanged(nameof(SearchQuery));
            await LoadTabAsync();
        }

        private Task RequestNewGroupAsync()
        {
            NewGroupRequested?.Invoke();
            return Task.CompletedTask;
        }

        private Task RequestNewDmAsync()
        {
            NewDmRequested?.Invoke();
            return Task.CompletedTask;
        }

        private async Task ToggleFavouriteAsync(Guid conversationId)
        {
            var favourites = await _conversationListService.GetFavouritesAsync(_currentUserId);
            bool isFavourite = favourites.Exists(c => c.Id == conversationId);
            await _conversationListService.SetFavouriteAsync(conversationId, _currentUserId, !isFavourite);
            await LoadTabAsync();
        }

        private Task OpenConversationAsync(Guid conversationId)
        {
            ConversationOpened?.Invoke(conversationId);
            return Task.CompletedTask;
        }

        private void OnActiveTabChanged()
        {
            OnPropertyChanged(nameof(IsAllTabActive));
            OnPropertyChanged(nameof(IsDirectMessagesTabActive));
            OnPropertyChanged(nameof(IsGroupsTabActive));
            OnPropertyChanged(nameof(IsFavoritesTabActive));
            OnPropertyChanged(nameof(IsUnreadTabActive));
        }
    }
}
