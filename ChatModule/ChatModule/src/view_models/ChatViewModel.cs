using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChatModule.Models;
using ChatModule.Services;

namespace ChatModule.src.view_models
{
    public class ChatViewModel : BaseViewModel
    {
        private readonly MessageInteractionService _interactionService;
        private readonly Guid _currentUserId;

        public Guid ConversationId { get; private set; }

        public ObservableCollection<Message> Messages { get; } = new();


        public Func<Task<string?>>? RequestEmojiAsync { get; set; }


        public event Action<Guid, List<Message>>? ReactionsChanged;


        public event Action<Guid>? ScrollToMessageRequested;

        public RelayCommand<Guid> ReactCommand           { get; }
        public RelayCommand<Guid> ScrollToMessageCommand { get; }

        public ChatViewModel(
            MessageService messageService,
            MessageInteractionService interactionService,
            ReadReceiptService readReceiptService,
            MentionService mentionService,
            Guid currentUserId)
        {
            _ = messageService;
            _interactionService  = interactionService;
            _ = readReceiptService;
            _ = mentionService;
            _currentUserId       = currentUserId;
            ReactCommand           = new RelayCommand<Guid>(OpenEmojiPickerAsync);
            ScrollToMessageCommand = new RelayCommand<Guid>(ScrollToMessageAsync);
        }

        private async Task OpenEmojiPickerAsync(Guid messageId)
        {
            if (RequestEmojiAsync == null)
                return;

            var emoji = await RequestEmojiAsync();
            if (emoji == null)
                return;

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
