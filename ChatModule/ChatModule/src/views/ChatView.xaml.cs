using ChatModule.src.view_models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Windows.Storage.Pickers;

namespace ChatModule.src.views
{
    public sealed partial class ChatView : UserControl
    {
        public ChatViewModel ViewModel { get; }

        public ChatView(ChatViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            ViewModel.RequestEmojiAsync = RequestEmojiAsync;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            ViewModel.ScrollToMessageRequested += OnScrollToMessageRequested;
            ViewModel.ReadReceiptDetailsRequested += OnReadReceiptDetailsRequested;
            ViewModel.ReplyPreviewTapped += OnReplyPreviewTappedByViewModel;
            ViewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachSearchPanel();
            _ = ViewModel.MarkConversationAsReadAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ScrollToMessageRequested -= OnScrollToMessageRequested;
            ViewModel.ReadReceiptDetailsRequested -= OnReadReceiptDetailsRequested;
            ViewModel.ReplyPreviewTapped -= OnReplyPreviewTappedByViewModel;
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

        public void SetSidePanel(UserControl panel)
        {
            SidePanelHost.Content = panel;
        }

        private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel.Messages.Count > 0 && e?.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ChatModule.Models.Message newMessage && !newMessage.IsMine)
                    {
                        _ = ViewModel.MarkVisibleMessagesAsReadAsync(newMessage.Id);
                    }
                }

                ScrollToBottom();
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

        private void ScrollToBottom()
        {
            if (ViewModel.Messages.Count == 0)
            {
                return;
            }

            var last = ViewModel.Messages[^1];
            MessagesList.ScrollIntoView(last);
        }

        private async void OnLeaveGroupClicked(object sender, RoutedEventArgs e)
        {
            await ViewModel.LeaveGroupAsync();
        }

        private async void OnAttachClicked(object sender, RoutedEventArgs e)
        {
            if (App.MainAppWindow == null)
            {
                return;
            }

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var ext = Path.GetExtension(file.Name)?.ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                {
                    await ShowInfoDialogAsync("Attachment", "Only PNG and JPEG images are supported.");
                    return;
                }

                var props = await file.GetBasicPropertiesAsync();
                const ulong maxSize = 6UL * 1024UL * 1024UL;
                if (props.Size > maxSize)
                {
                    await ShowInfoDialogAsync("Attachment", "Image size must be 6MB or less.");
                    return;
                }

                await ViewModel.SetAttachmentAsync(file.Path);
            }
        }

        private async void OnClearAttachmentClicked(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearAttachmentAsync();
        }

        private async void OnSetNicknameClicked(object sender, RoutedEventArgs e)
        {
            await ViewModel.SetNicknameAsync();
        }

        private async void OnClearNicknameClicked(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearNicknameAsync();
        }

        private async void OnReadReceiptTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is TextBlock { Tag: Guid messageId })
            {
                await ViewModel.ShowReadReceiptDetailsAsync(messageId);
            }
        }

        private async void OnReplyPreviewTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Guid replyToId && replyToId != Guid.Empty)
            {
                await ViewModel.OpenReplyTargetAsync(replyToId);
            }
        }

        private void OnReplyPreviewTappedByViewModel(Guid replyToId)
        {
            OnScrollToMessageRequested(replyToId);
        }

        private async void OnReadReceiptDetailsRequested(string body)
        {
            if (XamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Seen By",
                Content = body,
                CloseButtonText = "Close",
                XamlRoot = XamlRoot
            };

            _ = await dialog.ShowAsync();
        }

        private async System.Threading.Tasks.Task<string?> RequestEmojiAsync()
        {
            if (XamlRoot == null)
            {
                return null;
            }

            var list = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single,
                IsItemClickEnabled = true,
                ItemsSource = new[] { "👍", "❤️", "😂", "🔥", "👏", "😮", "😢", "🙏", "🎉", "👀" },
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 23, 21, 59)),
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 200, 172, 214)),
                Width = 280,
                MaxHeight = 220
            };

            var selected = default(string);
            list.ItemClick += (_, args) =>
            {
                selected = args.ClickedItem as string;
            };

            var dialog = new ContentDialog
            {
                Title = "Pick a reaction",
                Content = list,
                PrimaryButtonText = "Use",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(selected) ? list.SelectedItem as string : selected;
        }

        private async System.Threading.Tasks.Task ShowInfoDialogAsync(string title, string body)
        {
            if (XamlRoot == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = body,
                CloseButtonText = "Close",
                XamlRoot = XamlRoot
            };

            _ = await dialog.ShowAsync();
        }
    }
}
