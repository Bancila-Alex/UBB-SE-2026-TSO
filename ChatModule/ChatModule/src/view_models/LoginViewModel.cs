using ChatModule.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatModule.Services;

namespace ChatModule.viewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => Set(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => Set(ref _password, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => Set(ref _errorMessage, value);
    }

    private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => Set(ref _isLoading, value);
        }

        public RelayCommand LoginCommand { get; }
        public RelayCommand GoToRegisterCommand { get; }
        public RelayCommand ForgotPasswordCommand { get; }

        public event Func<Guid, string, Task>? LoginSucceeded;
        public event Action? RegisterRequested;
        public event Action? ForgotPasswordRequested;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoginCommand = new RelayCommand(LoginAsync, () => !IsLoading);
            GoToRegisterCommand = new RelayCommand(OnGoToRegister);
            ForgotPasswordCommand = new RelayCommand(OnForgotPassword);
        }

        private async Task LoginAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            try
            {
                var user = await _authService.LoginAsync(Username, Password);
                if (user != null)
                {
                    if (LoginSucceeded != null)
                        await LoginSucceeded(user.Id, user.Username);
                }
                else
                {
                    ErrorMessage = "Invalid credentials";
                }
            }
            catch (Exception ex)
        {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task OnGoToRegister()
        {
            RegisterRequested?.Invoke();
            return Task.CompletedTask;
        }

        private Task OnForgotPassword()
        {
            ForgotPasswordRequested?.Invoke();
            return Task.CompletedTask;
    }
    
}
