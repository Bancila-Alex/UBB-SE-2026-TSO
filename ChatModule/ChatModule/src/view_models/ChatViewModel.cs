using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.Services;
using ChatModule.src.domain.Enums;
using ChatModule.ViewModels;

namespace ChatModule.src.view_models
{
    public class ChatViewModel : BaseViewModel
    {
        private readonly MessageService _messageService;
        private readonly MessageInteractionService _interactionService;
        private readonly ReadReceiptService _readReceiptService;
        private readonly MentionService _mentionService;
        private readonly DirectMessageService _directMessageService;
        private readonly ConversationRepository _conversationRepository;
        private readonly Guid _currentUserId;

        private string _conversationTitle = string.Empty;
        private Message? _pinnedMessage;
        private bool _isInputDisabled;
        private string? _inputDisabledReason;
        private bool _isLoading;
        private string _messageInput = string.Empty;
        private string? _selectedAttachmentPath;
        private Message? _replyingTo;
        private Message? _editingMessage;
        private int _messageSkip = 0;
        private const int PageSize = 100;
        private bool _hasMoreMessages = true;
        private bool _isUnreadInitialized;

        public Guid ConversationId { get; private set; }

        public string ConversationTitle
        {
            get => _conversationTitle;
            private set => Set(ref _conversationTitle, value);
        }

        public ObservableCollection<Message> Messages { get; } = new();

        public ObservableCollection<User> MentionSuggestions { get; } = new();

        public bool HasMentionSuggestions => MentionSuggestions.Count > 0;

        public Message? PinnedMessage
        {
            get => _pinnedMessage;
            private set => Set(ref _pinnedMessage, value);
        }

        private bool _isConversationGroup;
        public bool IsConversationGroup
        {
            get => _isConversationGroup;
            private set => Set(ref _isConversationGroup, value);
        }

        public bool IsInputDisabled
        {
            get => _isInputDisabled;
            private set => Set(ref _isInputDisabled, value);
        }

        public string? InputDisabledReason
        {
            get => _inputDisabledReason;
            private set => Set(ref _inputDisabledReason, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => Set(ref _isLoading, value);
        }

        public string MessageInput
        {
            get => _messageInput;
            set
            {
                if (Set(ref _messageInput, value))
                    _ = UpdateMentionSuggestionsAsync();
            }
        }

        public string? SelectedAttachmentPath
        {
            get => _selectedAttachmentPath;
            private set => Set(ref _selectedAttachmentPath, value);
        }

        public string? SelectedAttachmentName => string.IsNullOrWhiteSpace(SelectedAttachmentPath)
            ? null
            : Path.GetFileName(SelectedAttachmentPath);

        public Message? ReplyingTo
        {
            get => _replyingTo;
            private set => Set(ref _replyingTo, value);
        }

        public Message? EditingMessage
        {
            get => _editingMessage;
            private set => Set(ref _editingMessage, value);
        }

        public Func<Task<string?>>? RequestEmojiAsync { get; set; }

        public event Action<Guid, List<Message>>? ReactionsChanged;

        public event Action<Guid>? ScrollToMessageRequested;
        public event Action<string>? ReadReceiptDetailsRequested;
        public event Action? LeaveGroupRequested;

        public RelayCommand<Guid> ReactCommand { get; }

        public RelayCommand<Guid> ScrollToMessageCommand { get; }

        public RelayCommand SendCommand { get; }

        public RelayCommand CancelReplyCommand { get; }

        public RelayCommand LoadMoreCommand { get; }

        public RelayCommand<Guid> EditMessageCommand { get; }

        public RelayCommand<Guid> DeleteMessageCommand { get; }

        public RelayCommand CancelEditCommand { get; }

        public RelayCommand<Guid> ReplyToCommand { get; }

        public RelayCommand<User> InsertMentionCommand { get; }

        public RelayCommand<Tuple<Guid, string>> ReactWithSpecificEmojiCommand { get; }

        public RelayCommand<Guid> ToggleReactionCounterCommand { get; }

        public RelayCommand OpenSearchCommand { get; }

        public ICommand CloseSearchCommand { get; }

        public RelayCommand<Guid> JumpToSearchResultCommand { get; }
        public RelayCommand<Guid> ShowReadReceiptDetailsCommand { get; }

        public MessageSearchViewModel MessageSearch { get; }

        private bool _isSearchVisible;
        public bool IsSearchVisible
        {
            get => _isSearchVisible;
            private set => Set(ref _isSearchVisible, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => Set(ref _errorMessage, value);
        }

        private Message? _firstUnreadMessage;
        public Message? FirstUnreadMessage
        {
            get => _firstUnreadMessage;
            private set => Set(ref _firstUnreadMessage, value);
        }

        private int _unreadSeparatorCount;
        public int UnreadSeparatorCount
        {
            get => _unreadSeparatorCount;
            private set => Set(ref _unreadSeparatorCount, value);
        }

        public bool HasUnreadSeparator => FirstUnreadMessage != null && UnreadSeparatorCount > 0;

        public ChatViewModel(
            MessageService messageService,
            MessageInteractionService interactionService,
            ReadReceiptService readReceiptService,
            MentionService mentionService,
            DirectMessageService directMessageService,
            ConversationRepository conversationRepository,
            SearchService searchService,
            Guid currentUserId)
        {
            _messageService = messageService;
            _interactionService = interactionService;
            _readReceiptService = readReceiptService;
            _mentionService = mentionService;
            _directMessageService = directMessageService;
            _conversationRepository = conversationRepository;
            _currentUserId = currentUserId;
            MessageSearch = new MessageSearchViewModel(searchService, currentUserId);

            MentionSuggestions.CollectionChanged += HandleMentionSuggestionsChanged;
            MessageSearch.CloseRequested += () => IsSearchVisible = false;
            MessageSearch.JumpToMessageRequested += messageId => _ = ScrollToMessageAsync(messageId);

            ReactCommand = new RelayCommand<Guid>(OpenEmojiPickerAsync);
            ScrollToMessageCommand = new RelayCommand<Guid>(ScrollToMessageAsync);
            SendCommand = new RelayCommand(SendAsync);
            CancelReplyCommand = new RelayCommand(CancelReplyAsync);
            LoadMoreCommand = new RelayCommand(LoadMoreAsync);
            EditMessageCommand = new RelayCommand<Guid>(StartEditAsync);
            DeleteMessageCommand = new RelayCommand<Guid>(DeleteAsync);
            CancelEditCommand = new RelayCommand(CancelEditAsync);
            ReplyToCommand = new RelayCommand<Guid>(ReplyToAsync);
            InsertMentionCommand = new RelayCommand<User>(InsertMentionAsync);
            ReactWithSpecificEmojiCommand = new RelayCommand<Tuple<Guid, string>>(ReactWithSpecificEmojiAsync);
            ToggleReactionCounterCommand = new RelayCommand<Guid>(ToggleReactionCounterAsync);
            OpenSearchCommand = new RelayCommand(OpenSearchAsync);
            CloseSearchCommand = new RelayCommand(CloseSearchAsync);
            JumpToSearchResultCommand = new RelayCommand<Guid>(JumpToSearchResultAsync);
            ShowReadReceiptDetailsCommand = new RelayCommand<Guid>(ShowReadReceiptDetailsAsync);
        }

        private void HandleMentionSuggestionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasMentionSuggestions));
        }

        public async Task LoadAsync(Guid conversationId)
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                ConversationId = conversationId;
                OnPropertyChanged(nameof(ConversationId));

                var messages = await _messageService.GetMessagesAsync(conversationId, _currentUserId, 0, 100);
                Messages.Clear();
                foreach (var message in messages)
                {
                    message.IsMine = message.UserId == _currentUserId;
                    Messages.Add(message);
                }

                _messageSkip = 0;
                _hasMoreMessages = messages.Count >= PageSize;

                await PopulateReactionCountersAsync();

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                IsConversationGroup = conversation?.Type == ConversationType.Group;

                if (conversation?.Type == ConversationType.Dm)
                {
                    var otherUser = await _directMessageService.GetOtherUserAsync(conversationId, _currentUserId);
                    if (otherUser != null)
                    {
                        ConversationTitle = otherUser.Username;
                    }
                    else
                    {
                        ConversationTitle = "Direct Message";
                    }
                }
                else
                {
                    ConversationTitle = conversation?.Title ?? "Conversation";
                }

                if (conversation?.PinnedMessageId != null)
                {
                    PinnedMessage = messages.FirstOrDefault(m => m.Id == conversation.PinnedMessageId.Value);
                }
                else
                {
                    PinnedMessage = null;
                }

                var isBlocked = await _directMessageService.IsBlockedAsync(conversationId, _currentUserId);
                if (isBlocked)
                {
                    IsInputDisabled = true;
                    InputDisabledReason = "Messaging is disabled because one of the users is blocked.";
                }
                else
                {
                    var cannotSendReason = await _messageService.GetCannotSendReasonAsync(conversationId, _currentUserId);
                    IsInputDisabled = !string.IsNullOrWhiteSpace(cannotSendReason);
                    InputDisabledReason = cannotSendReason;
                }

                await PopulateReadReceiptMetadataAsync();
                await UpdateUnreadSeparatorAsync();
                _isUnreadInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SendAsync()
        {
            ErrorMessage = null;
            try
            {
                if (ConversationId == Guid.Empty)
                {
                    return;
                }

                if (IsInputDisabled)
                {
                    var cannotSendReason = await _messageService.GetCannotSendReasonAsync(ConversationId, _currentUserId);
                    if (!string.IsNullOrWhiteSpace(cannotSendReason))
                    {
                        InputDisabledReason = cannotSendReason;
                        ErrorMessage = cannotSendReason;
                    }
                    return;
                }

                var liveCannotSendReason = await _messageService.GetCannotSendReasonAsync(ConversationId, _currentUserId);
                if (!string.IsNullOrWhiteSpace(liveCannotSendReason))
                {
                    IsInputDisabled = true;
                    InputDisabledReason = liveCannotSendReason;
                    ErrorMessage = liveCannotSendReason;
                    return;
                }

                if (string.IsNullOrWhiteSpace(MessageInput))
                {
                    if (string.IsNullOrWhiteSpace(SelectedAttachmentPath))
                    {
                        ErrorMessage = "Empty messages cannot be sent.";
                        return;
                    }
                }

                if (EditingMessage != null)
                {
                    await ConfirmEditAsync();
                    return;
                }

                var content = MessageInput;
                if (!string.IsNullOrWhiteSpace(SelectedAttachmentPath))
                {
                    content = string.IsNullOrWhiteSpace(content) ? "[Attachment]" : content;
                }
                var replyToId = ReplyingTo?.Id;

                var message = await _messageService.SendMessageAsync(ConversationId, _currentUserId, content, replyToId);
                message.IsMine = true;
                message.AttachmentImagePath = SelectedAttachmentPath;
                Messages.Add(message);
                await PopulateReadReceiptMetadataAsync();
                await UpdateUnreadSeparatorAsync();

                MessageInput = string.Empty;
                SelectedAttachmentPath = null;
                OnPropertyChanged(nameof(SelectedAttachmentName));
                ReplyingTo = null;

                await _readReceiptService.MarkLatestAsReadAsync(ConversationId, _currentUserId);
                await PopulateReadReceiptMetadataAsync();
                await UpdateUnreadSeparatorAsync();

                var postSendCannotSendReason = await _messageService.GetCannotSendReasonAsync(ConversationId, _currentUserId);
                if (!string.IsNullOrWhiteSpace(postSendCannotSendReason))
                {
                    IsInputDisabled = true;
                    InputDisabledReason = postSendCannotSendReason;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task LoadMoreAsync()
        {
            ErrorMessage = null;
            try
            {
                if (ConversationId == Guid.Empty)
                {
                    return;
                }

                if (!_hasMoreMessages)
                {
                    return;
                }

                _messageSkip += PageSize;
                var older = await _messageService.GetMessagesAsync(ConversationId, _currentUserId, _messageSkip, PageSize);
                foreach (var message in older)
                {
                    message.IsMine = message.UserId == _currentUserId;
                    Messages.Add(message);
                }

                _hasMoreMessages = older.Count >= PageSize;

                await PopulateReactionCountersAsync();
                await PopulateReadReceiptMetadataAsync();
                await UpdateUnreadSeparatorAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private Task StartEditAsync(Guid messageId)
        {
            var message = Messages.FirstOrDefault(m => m.Id == messageId);
            if (message == null)
            {
                return Task.CompletedTask;
            }

            EditingMessage = message;
            MessageInput = message.Content ?? string.Empty;
            ReplyingTo = null;
            return Task.CompletedTask;
        }

        private async Task ConfirmEditAsync()
        {
            ErrorMessage = null;
            if (EditingMessage == null)
            {
                return;
            }

            var messageId = EditingMessage.Id;
            var newContent = MessageInput;

            await _messageService.EditMessageAsync(messageId, _currentUserId, newContent);

            var index = Messages.IndexOf(EditingMessage);
            if (index >= 0)
            {
                var updated = Messages[index];
                updated.Content = newContent;
                updated.IsEdited = true;
                Messages[index] = updated;
            }

            MessageInput = string.Empty;
            EditingMessage = null;
            await PopulateReadReceiptMetadataAsync();
        }

        private Task CancelEditAsync()
        {
            EditingMessage = null;
            MessageInput = string.Empty;
            return Task.CompletedTask;
        }

        private async Task DeleteAsync(Guid messageId)
        {
            ErrorMessage = null;
            try
            {
                await _messageService.DeleteMessageAsync(messageId, _currentUserId);

                var message = Messages.FirstOrDefault(m => m.Id == messageId);
                if (message != null)
                {
                    message.IsDeleted = true;
                    var index = Messages.IndexOf(message);
                    if (index >= 0)
                    {
                        Messages[index] = message;
                    }
                }

                await PopulateReadReceiptMetadataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private Task CancelReplyAsync()
        {
            ReplyingTo = null;
            return Task.CompletedTask;
        }

        private Task ReplyToAsync(Guid messageId)
        {
            ReplyingTo = Messages.FirstOrDefault(m => m.Id == messageId);
            return Task.CompletedTask;
        }

        private async Task OpenEmojiPickerAsync(Guid messageId)
        {
            ErrorMessage = null;
            try
            {
                if (RequestEmojiAsync == null)
                {
                    return;
                }

                var emoji = await RequestEmojiAsync();
                if (emoji == null)
                {
                    return;
                }

                await _interactionService.ReactToMessageAsync(messageId, _currentUserId, emoji);

                var reactions = await _interactionService.GetReactionsAsync(messageId);
                ReactionsChanged?.Invoke(messageId, reactions);

                await PopulateReactionCountersAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private Task ScrollToMessageAsync(Guid messageId)
        {
            ScrollToMessageRequested?.Invoke(messageId);
            return Task.CompletedTask;
        }

        private async Task UpdateMentionSuggestionsAsync()
        {
            MentionSuggestions.Clear();

            if (ConversationId == Guid.Empty)
            {
                return;
            }

            var atIndex = _messageInput.LastIndexOf('@');
            if (atIndex < 0)
            {
                return;
            }

            var token = _messageInput.Substring(atIndex + 1);
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            var candidates = await _mentionService.GetCandidatesAsync(ConversationId, token);
            foreach (var user in candidates)
            {
                MentionSuggestions.Add(user);
            }
        }

        private async Task PopulateReactionCountersAsync()
        {
            for (var index = 0; index < Messages.Count; index++)
            {
                var message = Messages[index];
                if (message.MessageType == MessageType.Reaction)
                {
                    message.ReactionCounts.Clear();
                    Messages[index] = message;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(message.Content) && message.Content.StartsWith("[Image] ", StringComparison.Ordinal))
                {
                    message.AttachmentImagePath = message.Content.Substring("[Image] ".Length);
                }
                else
                {
                    message.AttachmentImagePath = null;
                }

                var reactions = await _interactionService.GetReactionsAsync(message.Id);
                message.ReactionCounts = reactions
                    .Where(r => !r.IsDeleted && !string.IsNullOrWhiteSpace(r.Content))
                    .GroupBy(r => r.Content!)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
                Messages[index] = message;
            }

            OnPropertyChanged(nameof(Messages));
        }

        private async Task ReactWithSpecificEmojiAsync(Tuple<Guid, string> payload)
        {
            ErrorMessage = null;
            var messageId = payload.Item1;
            var emoji = payload.Item2;
            if (messageId == Guid.Empty || string.IsNullOrWhiteSpace(emoji))
            {
                return;
            }

            await _interactionService.ReactToMessageAsync(messageId, _currentUserId, emoji);

            var reactions = await _interactionService.GetReactionsAsync(messageId);
            ReactionsChanged?.Invoke(messageId, reactions);

            await PopulateReactionCountersAsync();
        }

        private async Task ToggleReactionCounterAsync(Guid messageId)
        {
            ErrorMessage = null;
            try
            {
                var message = Messages.FirstOrDefault(m => m.Id == messageId);
                if (message == null)
                {
                    return;
                }

                if (message.ReactionCounts.Count == 0)
                {
                    return;
                }

                var topReaction = message.ReactionCounts
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                    .First().Key;

                var reactions = await _interactionService.GetReactionsAsync(messageId);
                var mine = reactions.FirstOrDefault(r => r.UserId == _currentUserId && !r.IsDeleted);

                if (mine != null && string.Equals(mine.Content, topReaction, StringComparison.Ordinal))
                {
                    await _interactionService.RemoveReactionAsync(messageId, _currentUserId);
                }
                else
                {
                    await _interactionService.ReactToMessageAsync(messageId, _currentUserId, topReaction);
                }

                var updated = await _interactionService.GetReactionsAsync(messageId);
                ReactionsChanged?.Invoke(messageId, updated);
                await PopulateReactionCountersAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private Task OpenSearchAsync()
        {
            if (ConversationId != Guid.Empty)
            {
                MessageSearch.Initialise(ConversationId);
            }

            IsSearchVisible = true;
            return Task.CompletedTask;
        }

        private Task CloseSearchAsync()
        {
            IsSearchVisible = false;
            return Task.CompletedTask;
        }

        private Task JumpToSearchResultAsync(Guid messageId)
        {
            IsSearchVisible = false;
            return ScrollToMessageAsync(messageId);
        }

        private Task InsertMentionAsync(User user)
        {
            var atIndex = _messageInput.LastIndexOf('@');
            if (atIndex >= 0)
            {
                MessageInput = _messageInput.Substring(0, atIndex) + $"@{user.Username} ";
            }

            MentionSuggestions.Clear();
            return Task.CompletedTask;
        }

        public async Task MarkVisibleMessagesAsReadAsync(Guid lastVisibleMessageId)
        {
            if (ConversationId == Guid.Empty)
            {
                return;
            }

            if (!_isUnreadInitialized)
            {
                return;
            }

            await _readReceiptService.MarkAsReadAsync(ConversationId, _currentUserId, lastVisibleMessageId);
            await PopulateReadReceiptMetadataAsync();
            await UpdateUnreadSeparatorAsync();
        }

        public async Task MarkConversationAsReadAsync()
        {
            if (ConversationId == Guid.Empty)
            {
                return;
            }

            await _readReceiptService.MarkLatestAsReadAsync(ConversationId, _currentUserId);
            await PopulateReadReceiptMetadataAsync();
            await UpdateUnreadSeparatorAsync();
        }

        private async Task PopulateReadReceiptMetadataAsync()
        {
            if (ConversationId == Guid.Empty)
            {
                return;
            }

            var participants = await _readReceiptService.GetParticipantsAsync(ConversationId);
            var participantCount = participants.Count;

            foreach (var message in Messages)
            {
                message.IsMine = message.UserId == _currentUserId;

                if (message.MessageType == MessageType.Reaction || message.IsDeleted)
                {
                    message.ReadByCount = 0;
                    message.ReadReceiptLabel = null;
                    continue;
                }

                var readByCount = await _readReceiptService.GetReadByCountAsync(ConversationId, message.Id);
                message.ReadByCount = readByCount;

                if (!message.IsMine)
                {
                    message.ReadReceiptLabel = null;
                    continue;
                }

                var otherReaders = await _readReceiptService.GetReadByOthersCountAsync(ConversationId, message.Id, _currentUserId);

                if (message.MessageType == MessageType.System)
                {
                    message.ReadReceiptLabel = null;
                    continue;
                }

                if (otherReaders <= 0)
                {
                    message.ReadReceiptLabel = null;
                }
                else if (!IsConversationGroup)
                {
                    message.ReadReceiptLabel = "Seen";
                }
                else
                {
                    message.ReadReceiptLabel = otherReaders <= 0
                        ? null
                        : $"Seen by {otherReaders}/{Math.Max(1, participantCount - 1)}";
                }
            }

            OnPropertyChanged(nameof(Messages));
        }

        public async Task ShowReadReceiptDetailsAsync(Guid messageId)
        {
            if (ConversationId == Guid.Empty || messageId == Guid.Empty)
            {
                return;
            }

            var readers = await _readReceiptService.GetReaderUsernamesAsync(ConversationId, messageId, _currentUserId);
            if (readers.Count == 0)
            {
                ReadReceiptDetailsRequested?.Invoke("No one else has seen this message yet.");
                return;
            }

            var body = string.Join(Environment.NewLine, readers);
            ReadReceiptDetailsRequested?.Invoke(body);
        }

        public Task LeaveGroupAsync()
        {
            if (IsConversationGroup)
            {
                LeaveGroupRequested?.Invoke();
            }

            return Task.CompletedTask;
        }

        public Task SetAttachmentAsync(string path)
        {
            SelectedAttachmentPath = string.IsNullOrWhiteSpace(path) ? null : path;
            OnPropertyChanged(nameof(SelectedAttachmentName));
            return Task.CompletedTask;
        }

        public Task ClearAttachmentAsync()
        {
            SelectedAttachmentPath = null;
            OnPropertyChanged(nameof(SelectedAttachmentName));
            return Task.CompletedTask;
        }

        private async Task UpdateUnreadSeparatorAsync()
        {
            if (Messages.Count == 0)
            {
                FirstUnreadMessage = null;
                UnreadSeparatorCount = 0;
                ApplyUnreadSeparatorFlag();
                return;
            }

            var lastReadMessageId = await _readReceiptService.GetLastReadMessageAsync(ConversationId, _currentUserId);
            var lastReadTimestamp = await _readReceiptService.GetLastReadTimestampAsync(ConversationId, _currentUserId);
            var firstUnread = default(Message);
            var unreadCount = 0;

            if (!lastReadMessageId.HasValue)
            {
                foreach (var message in Messages)
                {
                    if (message.UserId == _currentUserId || message.MessageType == MessageType.System)
                    {
                        continue;
                    }

                    firstUnread ??= message;
                    unreadCount++;
                }

                FirstUnreadMessage = firstUnread;
                UnreadSeparatorCount = unreadCount;
                ApplyUnreadSeparatorFlag();
                return;
            }

            var crossedLastRead = false;
            foreach (var message in Messages)
            {
                if (!crossedLastRead)
                {
                    if (message.Id == lastReadMessageId.Value)
                    {
                        crossedLastRead = true;
                    }

                    continue;
                }

                if (message.UserId == _currentUserId || message.MessageType == MessageType.System)
                {
                    continue;
                }

                if (lastReadTimestamp.HasValue && message.CreatedAt <= lastReadTimestamp.Value)
                {
                    continue;
                }

                firstUnread ??= message;
                unreadCount++;
            }

            FirstUnreadMessage = firstUnread;
            UnreadSeparatorCount = unreadCount;
            ApplyUnreadSeparatorFlag();
        }

        private void ApplyUnreadSeparatorFlag()
        {
            foreach (var message in Messages)
            {
                message.ShowUnreadSeparator = false;
            }

            if (FirstUnreadMessage != null)
            {
                FirstUnreadMessage.ShowUnreadSeparator = true;
            }

            OnPropertyChanged(nameof(HasUnreadSeparator));
        }
    }
}
