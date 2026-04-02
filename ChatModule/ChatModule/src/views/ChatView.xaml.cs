using ChatModule.src.view_models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace ChatModule.src.views
{
    public sealed partial class ChatView : UserControl
    {
        public ChatViewModel ViewModel { get; }

        public ChatView(ChatViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ViewModel.ScrollToMessageRequested += OnScrollToMessageRequested;
            ViewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachSearchPanel();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ScrollToMessageRequested -= OnScrollToMessageRequested;
            ViewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
        }

        private void OnSearchPanelLoaded(object sender, RoutedEventArgs e)
        {
            AttachSearchPanel();
        }

        private void AttachSearchPanel()
        {
            if (SearchPanelHost.Content != null)
            {
                return;
            }

            SearchPanelHost.Content = new MessageSearchPanel(ViewModel.MessageSearch);
        }

        private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel.Messages.Count > 0)
            {
                _ = ViewModel.MarkVisibleMessagesAsReadAsync(ViewModel.Messages[0].Id);
            }
        }

        private void OnScrollToMessageRequested(Guid messageId)
        {
            var target = ViewModel.Messages.FirstOrDefault(m => m.Id == messageId);
            if (target != null)
            {
                MessagesList.ScrollIntoView(target);
            }
        }
    }
}
