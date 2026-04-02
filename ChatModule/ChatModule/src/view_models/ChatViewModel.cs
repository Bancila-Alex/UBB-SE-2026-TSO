using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private Message? _replyingTo;
        private Message? _editingMessage;
        private int _messageSkip = 0;
        private const int PageSize = 100;

        public Guid ConversationId { get; private set; }

        public string ConversationTitle
        {
            get => _conversationTitle;
            private set => Set(ref _conversationTitle, value);
        }

        public ObservableCollection<Message> Messages { get; } = new();

        public ObservableCollection<User> MentionSuggestions { get; } = new();

        public Message? PinnedMessage
        {
            get => _pinnedMessage;
            private set => Set(ref _pinnedMessage, value);
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

        public ChatViewModel(
            MessageService messageService,
            MessageInteractionService interactionService,
            ReadReceiptService readReceiptService,
            MentionService mentionService,
            DirectMessageService directMessageService,
            ConversationRepository conversationRepository,
            Guid currentUserId)
        {
            _messageService = messageService;
            _interactionService = interactionService;
            _readReceiptService = readReceiptService;
            _mentionService = mentionService;
            _directMessageService = directMessageService;
            _conversationRepository = conversationRepository;
            _currentUserId = currentUserId;

            ReactCommand = new RelayCommand<Guid>(OpenEmojiPickerAsync);
            ScrollToMessageCommand = new RelayCommand<Guid>(ScrollToMessageAsync);
            SendCommand = new RelayCommand(SendAsync);
            CancelReplyCommand = new RelayCommand(CancelReplyAsync);
            ReplyToCommand = new RelayCommand<Guid>(ReplyToAsync);
            InsertMentionCommand = new RelayCommand<User>(InsertMentionAsync);
            ReactWithSpecificEmojiCommand = new RelayCommand<Tuple<Guid, string>>(ReactWithSpecificEmojiAsync);
        }

        public async Task LoadAsync(Guid conversationId)
        {
            IsLoading = true;
            try
            {
                ConversationId = conversationId;
                OnPropertyChanged(nameof(ConversationId));

                var messages = await _messageService.GetMessagesAsync(conversationId, _currentUserId, 0, 100);
                Messages.Clear();
                foreach (var message in messages)
                {
                    Messages.Add(message);
                }

                await PopulateReactionCountersAsync();

                var conversation = await _conversationRepository.GetByIdAsync(conversationId);
                ConversationTitle = conversation?.Title ?? string.Empty;

                if (conversation?.PinnedMessageId != null)
                {
                    PinnedMessage = messages.FirstOrDefault(m => m.Id == conversation.PinnedMessageId.Value);
                }
                else
                {
                    PinnedMessage = null;
                }

                var isBlocked = await _directMessageService.IsBlockedAsync(conversationId, _currentUserId);
                IsInputDisabled = isBlocked;
                InputDisabledReason = isBlocked ? "Messaging is disabled because one of the users is blocked." : null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SendAsync()
        {
            if (ConversationId == Guid.Empty)
            {
                return;
            }

            if (IsInputDisabled)
            {
                return;
            }

            if (EditingMessage != null)
            {
                await ConfirmEditAsync();
                return;
            }

            var content = MessageInput;
            var replyToId = ReplyingTo?.Id;

            var message = await _messageService.SendMessageAsync(ConversationId, _currentUserId, content, replyToId);
            Messages.Insert(0, message);

            MessageInput = string.Empty;
            ReplyingTo = null;
        }

        private async Task LoadMoreAsync()
        {
            if (ConversationId == Guid.Empty)
            {
                return;
            }

            _messageSkip += PageSize;
            var older = await _messageService.GetMessagesAsync(ConversationId, _currentUserId, _messageSkip, PageSize);
            foreach (var message in older)
            {
                Messages.Add(message);
            }

            await PopulateReactionCountersAsync();
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
        }

        private Task CancelEditAsync()
        {
            EditingMessage = null;
            MessageInput = string.Empty;
            return Task.CompletedTask;
        }

        private async Task DeleteAsync(Guid messageId)
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
            foreach (var message in Messages)
            {
                if (message.MessageType == MessageType.Reaction)
                {
                    message.ReactionCounts.Clear();
                    continue;
                }

                var reactions = await _interactionService.GetReactionsAsync(message.Id);
                message.ReactionCounts = reactions
                    .Where(r => !r.IsDeleted && !string.IsNullOrWhiteSpace(r.Content))
                    .GroupBy(r => r.Content!)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            }
        }

        private async Task ReactWithSpecificEmojiAsync(Tuple<Guid, string> payload)
        {
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

            await _readReceiptService.MarkAsReadAsync(ConversationId, _currentUserId, lastVisibleMessageId);
        }
    }
}
