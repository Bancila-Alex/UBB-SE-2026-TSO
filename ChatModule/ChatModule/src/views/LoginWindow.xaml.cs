using System;
using System.Threading.Tasks;
using ChatModule.Services;
using ChatModule.viewModels;
using Microsoft.UI.Xaml;

namespace ChatModule.src.views
{
    public sealed partial class LoginWindow : Window
    {
        private readonly AuthService _authService;
        public LoginViewModel ViewModel { get; }

        public event Func<Guid, string, Task>? LoginSucceeded;

        public LoginWindow(AuthService authService)
        {
            _authService = authService;
            ViewModel = new LoginViewModel(_authService);
            InitializeComponent();

            ViewModel.LoginSucceeded += OnLoginSucceededAsync;
            ViewModel.RegisterRequested += OnRegisterRequested;
            ViewModel.ForgotPasswordRequested += OnForgotPasswordRequested;
        }

        private Task OnLoginSucceededAsync(Guid userId, string username)
        {
            return LoginSucceeded?.Invoke(userId, username) ?? Task.CompletedTask;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = PasswordBox.Password;
        }

        private void OnRegisterRequested()
        {
            var registerWindow = new RegisterWindow(_authService);
            registerWindow.ViewModel.RegisterSucceeded += async (userId, username) =>
            {
                if (LoginSucceeded != null)
                {
                    await LoginSucceeded(userId, username);
                }

                registerWindow.Close();
            };
            registerWindow.ViewModel.NavigateToLoginRequested += () => registerWindow.Close();
            registerWindow.Activate();
        }

        private void OnForgotPasswordRequested()
        {
            var forgotPasswordWindow = new ForgotPasswordWindow(_authService);
            forgotPasswordWindow.ViewModel.NavigateToLoginRequested += () => forgotPasswordWindow.Close();
            forgotPasswordWindow.Activate();
        }
    }
}
