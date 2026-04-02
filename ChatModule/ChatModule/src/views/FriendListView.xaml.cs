using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;
using System;

namespace ChatModule.src.views
{
    public sealed partial class FriendListView : UserControl
    {
        public FriendListViewModel? ViewModel { get; private set; }

        public FriendListView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public FriendListView(FriendListViewModel viewModel)
            : this()
        {
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private void OnLoaded(object sender, global::Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ViewModel == null && DataContext is FriendListViewModel vm)
            {
                ViewModel = vm;
            }

            ViewModel?.LoadCommand.Execute(null);
        }

        private void OnOpenDmClick(object sender, global::Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button { Tag: Guid id } && ViewModel?.OpenDmCommand != null)
            {
                ViewModel.OpenDmCommand.Execute(id);
            }
        }

        private void OnViewProfileClick(object sender, global::Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button { Tag: Guid id } && ViewModel?.ViewProfileCommand != null)
            {
                ViewModel.ViewProfileCommand.Execute(id);
            }
        }

        private void OnRemoveFriendClick(object sender, global::Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button { Tag: Guid id } && ViewModel?.RemoveFriendCommand != null)
            {
                ViewModel.RemoveFriendCommand.Execute(id);
            }
        }
    }
}
