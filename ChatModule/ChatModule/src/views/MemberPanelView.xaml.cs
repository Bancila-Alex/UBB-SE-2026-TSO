using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class MemberPanelView : UserControl
    {
        public MemberPanelViewModel ViewModel { get; }

        public MemberPanelView(MemberPanelViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
