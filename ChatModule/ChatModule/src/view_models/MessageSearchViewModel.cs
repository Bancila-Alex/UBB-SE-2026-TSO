using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatModule.Models;
using ChatModule.Services;
using ChatModule.ViewModels;

namespace ChatModule.src.view_models
{
    public class MessageSearchViewModel : BaseViewModel
    {
        private readonly SearchService _searchService;
        private readonly Guid _currentUserId;

        private Guid _conversationId;
        public Guid ConversationId
        {
            get => _conversationId;
            private set => Set(ref _conversationId, value);
        }

        private string _query = string.Empty;
        public string Query
        {
            get => _query;
            set => Set(ref _query, value);
        }

        public ObservableCollection<Message> Results { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => Set(ref _isLoading, value);
        }

        private string? _noResultsMessage;
        public string? NoResultsMessage
        {
            get => _noResultsMessage;
            private set => Set(ref _noResultsMessage, value);
        }

        public event Action<Guid>? JumpToMessageRequested;
        public event Action? CloseRequested;

        public ICommand SearchCommand { get; }
        public ICommand CloseCommand { get; }
        public RelayCommand<Guid> JumpToMessageCommand { get; }

        public MessageSearchViewModel(SearchService searchService, Guid currentUserId)
        {
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _currentUserId = currentUserId;

            SearchCommand = new RelayCommand(SearchAsync);
            CloseCommand = new RelayCommand(CloseAsync);
            JumpToMessageCommand = new RelayCommand<Guid>(JumpToMessageAsync);
        }

        public void Initialise(Guid conversationId)
        {
            ConversationId = conversationId;
            Query = string.Empty;
            Results.Clear();
            NoResultsMessage = null;
        }

        private async Task SearchAsync()
        {
            Results.Clear();
            NoResultsMessage = null;

            if (ConversationId == Guid.Empty)
            {
                NoResultsMessage = "No conversation selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(_query))
            {
                NoResultsMessage = "Enter text to search.";
                return;
            }

            IsLoading = true;
            try
            {
                var messages = await _searchService.SearchMessagesAsync(ConversationId, _currentUserId, _query.Trim());
                foreach (var message in messages)
                {
                    Results.Add(message);
                }

                if (Results.Count == 0)
                {
                    NoResultsMessage = "No messages found.";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task JumpToMessageAsync(Guid messageId)
        {
            if (messageId == Guid.Empty)
            {
                return Task.CompletedTask;
            }

            JumpToMessageRequested?.Invoke(messageId);
            return Task.CompletedTask;
        }

        private Task CloseAsync()
        {
            CloseRequested?.Invoke();
            return Task.CompletedTask;
        }
    }
}
