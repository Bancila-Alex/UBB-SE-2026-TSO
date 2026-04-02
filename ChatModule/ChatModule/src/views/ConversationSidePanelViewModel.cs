using System;
using ChatModule.src.domain.Enums;
using ChatModule.src.view_models;
using ChatModule.ViewModels;

namespace ChatModule.src.views
{
    public class ConversationSidePanelViewModel : BaseViewModel
    {
        private bool _isPanelVisible = true;

        public ConversationType ConversationType { get; }

        public string PanelTitle => ConversationType == ConversationType.Group ? "Members" : "Profile";

        public BaseViewModel ContentViewModel { get; }

        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            private set
            {
                if (Set(ref _isPanelVisible, value))
                {
                    OnPropertyChanged(nameof(TogglePanelIcon));
                    OnPropertyChanged(nameof(PanelWidth));
                }
            }
        }

        public string TogglePanelIcon => IsPanelVisible ? "◀" : "▶";

        public double PanelWidth => IsPanelVisible ? 360 : 44;

        public RelayCommand TogglePanelCommand { get; }

        public ConversationSidePanelViewModel(ConversationType conversationType, BaseViewModel contentViewModel)
        {
            ConversationType = conversationType;
            ContentViewModel = contentViewModel;
            TogglePanelCommand = new RelayCommand(TogglePanelAsync);
        }

        private System.Threading.Tasks.Task TogglePanelAsync()
        {
            IsPanelVisible = !IsPanelVisible;
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
