using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class FriendListView : UserControl
    {
        public FriendListViewModel ViewModel { get; }

        public FriendListView(FriendListViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            ViewModel.LoadCommand.Execute(null);
        }
    }
}
