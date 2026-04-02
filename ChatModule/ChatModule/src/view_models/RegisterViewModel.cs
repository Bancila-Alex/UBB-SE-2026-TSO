using ChatModule.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatModule.ViewModels;

public class RegisterViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => Set(ref _username, value);
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => Set(ref _email, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => Set(ref _password, value);
    }

    private string _phone = string.Empty;
    public string Phone
    {
        get => _phone;
        set => Set(ref _phone, value);
    }

    private DateTime? _birthday;
    public DateTime? Birthday
    {
        get => _birthday;
        set => Set(ref _birthday, value);
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

    public RelayCommand RegisterCommand { get; }
    public RelayCommand BackToLoginCommand { get; }

    public event Action? NavigateToLoginRequested;

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        RegisterCommand = new RelayCommand(RegisterAsync, () => !IsLoading);
        BackToLoginCommand = new RelayCommand(OnBackToLogin);
    }

    private async Task RegisterAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _authService.RegisterAsync(
                Username,
                Email,
                Password,
                Phone,
                Birthday,
                null // avatarUrl
            );
            NavigateToLoginRequested?.Invoke();
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

    private Task OnBackToLogin()
    {
        NavigateToLoginRequested?.Invoke();
        return Task.CompletedTask;
    }
}
