using ChatModule.src.view_models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace ChatModule.src.views
{
    public sealed partial class MemberPanelView : UserControl
    {
        public MemberPanelViewModel ViewModel { get; }

        public MemberPanelView(MemberPanelViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            ViewModel.RequestTimeoutDurationAsync = RequestTimeoutDurationAsync;
        }

        private async Task<TimeSpan?> RequestTimeoutDurationAsync()
        {
            var durationCombo = new ComboBox();
            durationCombo.Items.Add(new ComboBoxItem { Content = "10 minutes", Tag = "10m" });
            durationCombo.Items.Add(new ComboBoxItem { Content = "1 hour", Tag = "1h" });
            durationCombo.Items.Add(new ComboBoxItem { Content = "24 hours", Tag = "24h" });
            durationCombo.Items.Add(new ComboBoxItem { Content = "7 days", Tag = "7d" });
            durationCombo.Items.Add(new ComboBoxItem { Content = "Custom (minutes)", Tag = "custom" });
            durationCombo.SelectedIndex = 0;

            var customMinutesBox = new TextBox
            {
                PlaceholderText = "Enter custom minutes",
                IsEnabled = false
            };

            durationCombo.SelectionChanged += (_, _) =>
            {
                if (durationCombo.SelectedItem is ComboBoxItem selected)
                {
                    customMinutesBox.IsEnabled = string.Equals(selected.Tag as string, "custom", StringComparison.Ordinal);
                }
            };

            var content = new StackPanel { Spacing = 8 };
            content.Children.Add(new TextBlock { Text = "Pick timeout duration" });
            content.Children.Add(durationCombo);
            content.Children.Add(customMinutesBox);

            var dialog = new ContentDialog
            {
                Title = "Timeout member",
                Content = content,
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return null;
            }

            var tag = (durationCombo.SelectedItem as ComboBoxItem)?.Tag as string;
            return tag switch
            {
                "10m" => TimeSpan.FromMinutes(10),
                "1h" => TimeSpan.FromHours(1),
                "24h" => TimeSpan.FromHours(24),
                "7d" => TimeSpan.FromDays(7),
                "custom" when int.TryParse(customMinutesBox.Text, out var minutes) && minutes > 0 => TimeSpan.FromMinutes(minutes),
                _ => null,
            };
        }
    }
}
