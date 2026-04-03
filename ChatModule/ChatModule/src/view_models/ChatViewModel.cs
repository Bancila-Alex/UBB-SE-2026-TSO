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
            private set
            {
                if (Set(ref _pinnedMessage, value))
                    OnPropertyChanged(nameof(PinnedMessageExpiryLabel));
            }
        }

        public string? PinnedMessageExpiryLabel =>
            _pinnedMessage?.PinExpiresAt.HasValue == true
                ? $"Expires {_pinnedMessage.PinExpiresAt.Value.ToLocalTime():g}"
                : null;

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
        public Func<Task<DateTime?>>? RequestPinExpiryAsync { get; set; }

        public event Action<Guid, List<Message>>? ReactionsChanged;

        public event Action<Guid>? ScrollToMessageRequested;
        public event Action<string>? ReadReceiptDetailsRequested;
        public event Action<Guid>? ReplyPreviewTapped;
        public event Action? LeaveGroupRequested;
        public event Action? SetNicknameRequested;
        public event Action? ClearNicknameRequested;

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
        public RelayCommand<Guid> PinMessageCommand { get; }
        public RelayCommand UnpinMessageCommand { get; }

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
            PinMessageCommand = new RelayCommand<Guid>(PinAsync);
            UnpinMessageCommand = new RelayCommand(UnpinAsync);
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
                    PrepareMessageForDisplay(message);
                    message.IsMine = message.UserId == _currentUserId;
                    Messages.Add(message);
                }

                _messageSkip = 0;
                _hasMoreMessages = messages.Count >= PageSize;

                await PopulateReactionCountersAsync();
                await PopulateReplyPreviewsAsync();

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
                    var pinned = messages.FirstOrDefault(m => m.Id == conversation.PinnedMessageId.Value);
                    if (pinned != null && pinned.PinExpiresAt.HasValue && pinned.PinExpiresAt.Value <= DateTime.UtcNow)
                    {
                        await _directMessageService.ClearExpiredPinAsync(conversationId, pinned.Id);
                        PinnedMessage = null;
                    }
                    else
                    {
                        PinnedMessage = pinned;
                    }
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
                    var storedAttachmentPath = await _messageService.PersistImageAttachmentAsync(SelectedAttachmentPath);
                    content = string.IsNullOrWhiteSpace(content)
                        ? $"[Image] {storedAttachmentPath}"
                        : $"[Image] {storedAttachmentPath}{Environment.NewLine}{content}";
                }
                var replyToId = ReplyingTo?.Id;

                var message = await _messageService.SendMessageAsync(ConversationId, _currentUserId, content, replyToId);
                PrepareMessageForDisplay(message);
                message.IsMine = true;
                ApplyMessageActions(message);

                if (replyToId.HasValue)
                {
                    var parts = await _interactionService.BuildReplyPreviewPartsAsync(replyToId.Value);
                    if (parts.HasValue)
                    {
                        message.ReplyPreviewSender = parts.Value.Sender;
                        message.ReplyPreviewContent = parts.Value.Content;
                        message.ReplyPreviewText = $"{parts.Value.Sender}: {parts.Value.Content}";
                    }
                }

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
                    PrepareMessageForDisplay(message);
                    message.IsMine = message.UserId == _currentUserId;
                    Messages.Add(message);
                }

                _hasMoreMessages = older.Count >= PageSize;

                await PopulateReactionCountersAsync();
                await PopulateReplyPreviewsAsync();
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
                await PopulateReplyPreviewsAsync();
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

        public Task OpenReplyTargetAsync(Guid replyToId)
        {
            if (replyToId != Guid.Empty)
            {
                ReplyPreviewTapped?.Invoke(replyToId);
            }

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

                var reactions = await _interactionService.GetReactionsAsync(message.Id);
                message.ReactionCounts = reactions
                    .Where(r => !r.IsDeleted && !string.IsNullOrWhiteSpace(r.Content))
                    .GroupBy(r => r.Content!)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
                Messages[index] = message;
            }
        }

        private async Task PopulateReplyPreviewsAsync()
        {
            for (var i = 0; i < Messages.Count; i++)
            {
                var message = Messages[i];
                ApplyMessageActions(message);

                if (message.ReplyToId.HasValue)
                {
                    var parts = await _interactionService.BuildReplyPreviewPartsAsync(message.ReplyToId.Value);
                    if (parts.HasValue)
                    {
                        message.ReplyPreviewSender = parts.Value.Sender;
                        message.ReplyPreviewContent = parts.Value.Content;
                        message.ReplyPreviewText = $"{parts.Value.Sender}: {parts.Value.Content}";
                    }
                    else
                    {
                        message.ReplyPreviewSender = null;
                        message.ReplyPreviewContent = null;
                        message.ReplyPreviewText = null;
                    }
                }
                else
                {
                    message.ReplyPreviewSender = null;
                    message.ReplyPreviewContent = null;
                    message.ReplyPreviewText = null;
                }

                Messages[i] = message;
            }
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
            await PopulateReplyPreviewsAsync();
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

            for (var i = 0; i < Messages.Count; i++)
            {
                var message = Messages[i];
                message.IsMine = message.UserId == _currentUserId;

                if (message.MessageType != MessageType.Reaction && !message.IsDeleted)
                {
                    var readByCount = await _readReceiptService.GetReadByCountAsync(ConversationId, message.Id);
                    message.ReadByCount = readByCount;

                    if (message.IsMine && message.MessageType != MessageType.System)
                    {
                        var otherReaders = await _readReceiptService.GetReadByOthersCountAsync(ConversationId, message.Id, _currentUserId);
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
                            message.ReadReceiptLabel = $"Seen by {otherReaders}/{Math.Max(1, participantCount - 1)}";
                        }
                    }
                    else
                    {
                        message.ReadReceiptLabel = null;
                    }
                }
                else
                {
                    message.ReadByCount = 0;
                    message.ReadReceiptLabel = null;
                }

                Messages[i] = message;
            }
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

        public Task SetNicknameAsync()
        {
            if (IsConversationGroup)
            {
                SetNicknameRequested?.Invoke();
            }

            return Task.CompletedTask;
        }

        public Task ClearNicknameAsync()
        {
            if (IsConversationGroup)
            {
                ClearNicknameRequested?.Invoke();
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

        private async Task PinAsync(Guid messageId)
        {
            ErrorMessage = null;
            try
            {
                if (RequestPinExpiryAsync == null)
                    return;

                var expiresAt = await RequestPinExpiryAsync();
                if (!expiresAt.HasValue)
                    return;

                var (_, notice) = await _directMessageService.PinMessageAsync(ConversationId, _currentUserId, messageId, expiresAt.Value);

                var msg = Messages.FirstOrDefault(m => m.Id == messageId);
                if (msg != null)
                {
                    msg.PinExpiresAt = expiresAt.Value;
                    var idx = Messages.IndexOf(msg);
                    if (idx >= 0)
                        Messages[idx] = msg;
                }

                PinnedMessage = msg;

                PrepareMessageForDisplay(notice);
                ApplyMessageActions(notice);
                Messages.Add(notice);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task UnpinAsync()
        {
            ErrorMessage = null;
            try
            {
                var notice = await _directMessageService.UnpinMessageAsync(ConversationId, _currentUserId);
                PinnedMessage = null;

                PrepareMessageForDisplay(notice);
                ApplyMessageActions(notice);
                Messages.Add(notice);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private static void PrepareMessageForDisplay(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                message.AttachmentImagePath = null;
                return;
            }

            var content = message.Content!;
            if (!content.StartsWith("[Image] ", StringComparison.Ordinal))
            {
                message.AttachmentImagePath = null;
                return;
            }

            var body = content.Substring("[Image] ".Length);
            var split = body.Split(new[] { "\r\n", "\n" }, 2, StringSplitOptions.None);
            var imagePath = split[0].Trim();
            if (!File.Exists(imagePath))
            {
                var fileName = Path.GetFileName(imagePath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    var candidate = Path.Combine(AppContext.BaseDirectory, "attachments", fileName);
                    if (File.Exists(candidate))
                    {
                        imagePath = candidate;
                    }
                }
            }

            message.AttachmentImagePath = imagePath;
            message.Content = split.Length > 1 ? split[1] : string.Empty;
        }

        private void ApplyMessageActions(Message message)
        {
            var mine = message.UserId.HasValue && message.UserId.Value == _currentUserId;
            var editableType = message.MessageType == MessageType.Text;
            var notDeleted = !message.IsDeleted;
            var pinnable = !IsConversationGroup
                           && message.MessageType != MessageType.System
                           && message.MessageType != MessageType.Reaction
                           && notDeleted;

            message.CanDelete = mine && notDeleted;
            message.CanEdit = mine && notDeleted && editableType;
            message.CanPin = pinnable;
        }
    }
}
