using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Repositories;
using ChatModule.Services;
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

        public Guid ConversationId { get; private set; }

        public string ConversationTitle
        {
            get => _conversationTitle;
            private set => Set(ref _conversationTitle, value);
        }

        public ObservableCollection<Message> Messages { get; } = new();

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
            set => Set(ref _messageInput, value);
        }

        public Message? ReplyingTo
        {
            get => _replyingTo;
            private set => Set(ref _replyingTo, value);
        }

        public Func<Task<string?>>? RequestEmojiAsync { get; set; }

        public event Action<Guid, List<Message>>? ReactionsChanged;

        public event Action<Guid>? ScrollToMessageRequested;

        public RelayCommand<Guid> ReactCommand { get; }

        public RelayCommand<Guid> ScrollToMessageCommand { get; }

        public RelayCommand SendCommand { get; }

        public RelayCommand CancelReplyCommand { get; }

        public RelayCommand<Guid> ReplyToCommand { get; }

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

            var content = MessageInput;
            var replyToId = ReplyingTo?.Id;

            var message = await _messageService.SendMessageAsync(ConversationId, _currentUserId, content, replyToId);
            Messages.Insert(0, message);

            MessageInput = string.Empty;
            ReplyingTo = null;
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
        }

        private Task ScrollToMessageAsync(Guid messageId)
        {
            ScrollToMessageRequested?.Invoke(messageId);
            return Task.CompletedTask;
        }
    }
}
