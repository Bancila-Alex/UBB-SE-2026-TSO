using ChatModule.ViewModels;
using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class ConversationSidePanelView : UserControl
    {
        public ConversationSidePanelViewModel ViewModel { get; }

        public ConversationSidePanelView(ConversationSidePanelViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();

            if (viewModel.ContentViewModel is MemberPanelViewModel memberPanelViewModel)
            {
                PanelContentHost.Content = new MemberPanelView(memberPanelViewModel);
            }
            else if (viewModel.ContentViewModel is ProfileViewModel profileViewModel)
            {
                PanelContentHost.Content = new ProfileView(profileViewModel);
            }
        }
    }
}
