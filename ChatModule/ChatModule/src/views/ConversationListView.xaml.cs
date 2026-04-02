using ChatModule.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class ConversationListView : UserControl
    {
        public ConversationListViewModel ViewModel { get; }

        public ConversationListView(ConversationListViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
