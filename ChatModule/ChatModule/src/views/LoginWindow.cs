using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ChatModule.Repositories;
using ChatModule.Services;
using ChatModule.ViewModels;
using ChatModule.viewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ChatModule.src.views
{
    public sealed class LoginWindow : Window
    {
        private readonly AuthService _authService;
        private readonly TextBlock _errorText;
        private readonly ProgressRing _loadingRing;
        private readonly Button _loginButton;

        public LoginViewModel ViewModel { get; }

        public event Func<Guid, string, Task>? LoginSucceeded;

        public LoginWindow(AuthService authService)
        {
            _authService = authService;
            ViewModel = new LoginViewModel(_authService);

            var root = new Grid
            {
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 23, 21, 59))
            };

            var card = new Border
            {
                Width = 440,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(24),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(ColorHelper.FromArgb(255, 46, 35, 108))
            };

            var panel = new StackPanel { Spacing = 10 };
            panel.Children.Add(new TextBlock
            {
                Text = "Welcome Back",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 200, 172, 214))
            });

            var usernameBox = new TextBox { PlaceholderText = "Username" };
            usernameBox.TextChanged += (_, _) => ViewModel.Username = usernameBox.Text;

            var passwordBox = new PasswordBox { PlaceholderText = "Password" };
            passwordBox.PasswordChanged += (_, _) => ViewModel.Password = passwordBox.Password;

            _loginButton = new Button { Content = "Login", Command = ViewModel.LoginCommand };
            var registerButton = new Button { Content = "Sign Up", Command = ViewModel.GoToRegisterCommand };
            var forgotButton = new Button { Content = "Forgot Password", Command = ViewModel.ForgotPasswordCommand };

            _errorText = new TextBlock
            {
                Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 138, 138)),
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            _loadingRing = new ProgressRing
            {
                Width = 30,
                Height = 30,
                IsActive = false,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(usernameBox);
            panel.Children.Add(passwordBox);
            panel.Children.Add(_loginButton);
            panel.Children.Add(registerButton);
            panel.Children.Add(forgotButton);
            panel.Children.Add(_errorText);
            panel.Children.Add(_loadingRing);

            card.Child = panel;
            root.Children.Add(card);
            Content = root;

            ViewModel.LoginSucceeded += OnLoginSucceededAsync;
            ViewModel.RegisterRequested += OnRegisterRequested;
            ViewModel.ForgotPasswordRequested += OnForgotPasswordRequested;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            Closed += OnClosed;
            UpdateUiState();
        }

        private void OnClosed(object sender, WindowEventArgs args)
        {
            ViewModel.LoginSucceeded -= OnLoginSucceededAsync;
            ViewModel.RegisterRequested -= OnRegisterRequested;
            ViewModel.ForgotPasswordRequested -= OnForgotPasswordRequested;
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Closed -= OnClosed;
        }

        private Task OnLoginSucceededAsync(Guid userId, string username)
        {
            return LoginSucceeded?.Invoke(userId, username) ?? Task.CompletedTask;
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

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) || e.PropertyName == nameof(LoginViewModel.ErrorMessage))
            {
                try
                {
                    UpdateUiState();
                }
                catch
                {
                    // Ignore updates while/after the window is closing.
                }
            }
        }

        private void UpdateUiState()
        {
            _loginButton.IsEnabled = !ViewModel.IsLoading;
            _loadingRing.IsActive = ViewModel.IsLoading;
            _loadingRing.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
            _errorText.Text = ViewModel.ErrorMessage ?? string.Empty;
            _errorText.Visibility = string.IsNullOrWhiteSpace(ViewModel.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
