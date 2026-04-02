using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChatModule.Services;
using ChatModule.src.domain;

namespace ChatModule.ViewModels
{
    public class ConversationListViewModel : BaseViewModel
    {
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

        private string _activeTab = "All";
        public string ActiveTab
        {
            get => _activeTab;
            set => Set(ref _activeTab, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        public RelayCommand         LoadCommand      { get; }
        public RelayCommand<string> SwitchTabCommand { get; }

        public ConversationListViewModel(ConversationListService conversationListService, Guid currentUserId)
        {
            _conversationListService = conversationListService ?? throw new ArgumentNullException(nameof(conversationListService));
            _currentUserId = currentUserId;
            LoadCommand = new RelayCommand(LoadTabAsync);
            SwitchTabCommand = new RelayCommand<string>(SwitchTabAsync);
        }

        private async Task LoadTabAsync()
        {
            IsLoading = true;
            try
            {
                var results = ActiveTab switch
                {
                    "All" => await _conversationListService.GetAllAsync(_currentUserId),
                    "Direct Messages" => await _conversationListService.GetDmsAsync(_currentUserId),
                    "Groups" => await _conversationListService.GetGroupsAsync(_currentUserId),
                    "Favorites" => await _conversationListService.GetFavouritesAsync(_currentUserId),
                    "Unread" => await _conversationListService.GetUnreadAsync(_currentUserId),
                    _ => await _conversationListService.GetAllAsync(_currentUserId),
                };

                Conversations.Clear();
                foreach (var c in results)
                    Conversations.Add(c);
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
    }
}
