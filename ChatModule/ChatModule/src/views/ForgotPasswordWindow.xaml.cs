using ChatModule.Services;
using ChatModule.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ChatModule.src.views
{
    public sealed partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordViewModel ViewModel { get; }

        public ForgotPasswordWindow(ForgotPasswordViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        public ForgotPasswordWindow(AuthService authService)
            : this(new ForgotPasswordViewModel(authService))
        {
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.NewPassword = NewPasswordBox.Password;
        }
    }
}
