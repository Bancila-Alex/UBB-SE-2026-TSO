using ChatModule.src.view_models;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class MessageSearchPanel : UserControl
    {
        public MessageSearchViewModel ViewModel { get; }

        public MessageSearchPanel(MessageSearchViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
