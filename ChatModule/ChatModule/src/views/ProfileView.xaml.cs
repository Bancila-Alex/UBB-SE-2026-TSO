using ChatModule.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace ChatModule.src.views
{
    public sealed partial class ProfileView : UserControl
    {
        public ProfileViewModel? ViewModel { get; private set; }

        public ProfileView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public ProfileView(ProfileViewModel viewModel)
            : this()
        {
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private void OnLoaded(object sender, global::Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ViewModel == null && DataContext is ProfileViewModel vm)
            {
                ViewModel = vm;
                Bindings.Update();
            }
        }

        private async void OnUploadAvatarClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".webp");

            if (App.MainAppWindow == null)
            {
                return;
            }

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var extension = Path.GetExtension(file.Path)?.ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                {
                    return;
                }

                var info = new FileInfo(file.Path);
                if (info.Length > 5 * 1024 * 1024)
                {
                    await ShowInfoAsync("Avatar too large", "Please choose an image smaller than 5MB.");
                    return;
                }

                ViewModel.EditAvatarUrl = file.Path;
            }
        }

        private async Task ShowInfoAsync(string title, string message)
        {
            if (App.MainAppWindow == null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = App.MainAppWindow.Content.XamlRoot
            };

            _ = await dialog.ShowAsync();
        }
    }
}
