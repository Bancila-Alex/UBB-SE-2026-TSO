using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class FriendRequestsView : UserControl
    {
        public FriendRequestsViewModel ViewModel { get; }

        public FriendRequestsView(FriendRequestsViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
