using ChatModule.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class ProfileView : UserControl
    {
        public ProfileViewModel ViewModel { get; }

        public ProfileView(ProfileViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
