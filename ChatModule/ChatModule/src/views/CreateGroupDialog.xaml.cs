using ChatModule.Models;
using ChatModule.src.domain;
using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace ChatModule.src.views
{
    public sealed partial class CreateGroupDialog : ContentDialog
    {
        public CreateGroupViewModel ViewModel { get; }
        public Conversation? CreatedConversation { get; private set; }

        public CreateGroupDialog(CreateGroupViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            PrimaryButtonText = string.Empty;
            SecondaryButtonText = string.Empty;
            CloseButtonText = string.Empty;

            ViewModel.GroupCreated += OnGroupCreated;
            ViewModel.Cancelled += OnCancelled;

            Closed += (_, _) =>
            {
                ViewModel.GroupCreated -= OnGroupCreated;
                ViewModel.Cancelled -= OnCancelled;
            };
        }

        private void OnGroupCreated(Conversation conversation)
        {
            CreatedConversation = conversation;
            Hide();
        }

        private void OnCancelled()
        {
            Hide();
        }

        private async void OnPickIconClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".webp");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync().AsTask();
            if (file != null)
            {
                ViewModel.IconUrl = file.Path;
            }
        }
    }
}
