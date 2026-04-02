using ChatModule.Services;
using ChatModule.ViewModels;
using Microsoft.UI.Xaml;
using System;

namespace ChatModule.src.views
{
    public sealed partial class RegisterWindow : Window
    {
        public RegisterViewModel ViewModel { get; }

        public DateTimeOffset BirthdayDate
        {
            get
            {
                if (ViewModel.Birthday.HasValue)
                {
                    return new DateTimeOffset(ViewModel.Birthday.Value);
                }

                return DateTimeOffset.Now;
            }
            set
            {
                ViewModel.Birthday = value.DateTime;
            }
        }

        public RegisterWindow(RegisterViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        public RegisterWindow(AuthService authService)
            : this(new RegisterViewModel(authService))
        {
        }

        public bool IsNotLoading(bool isLoading) => !isLoading;

        public Visibility ErrorMessageVisibility(string? errorMessage)
            => string.IsNullOrWhiteSpace(errorMessage) ? Visibility.Collapsed : Visibility.Visible;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.Password = PasswordBox.Password;
        }
    }
}
