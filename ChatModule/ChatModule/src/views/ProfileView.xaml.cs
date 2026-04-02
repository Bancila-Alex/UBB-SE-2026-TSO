using ChatModule.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
                ViewModel.EditAvatarUrl = file.Path;
            }
        }
    }
}
