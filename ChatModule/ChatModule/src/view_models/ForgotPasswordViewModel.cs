using ChatModule.Services;
using System;
using System.Threading.Tasks;

namespace ChatModule.ViewModels;

public class ForgotPasswordViewModel : BaseViewModel
{
    private readonly AuthService _authService;

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => Set(ref _email, value);
    }

    private string _newPassword = string.Empty;
    public string NewPassword
    {
        get => _newPassword;
        set => Set(ref _newPassword, value);
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => Set(ref _errorMessage, value);
    }

    private string? _successMessage;
    public string? SuccessMessage
    {
        get => _successMessage;
        set => Set(ref _successMessage, value);
    }

    public RelayCommand SubmitCommand { get; }
    public RelayCommand BackToLoginCommand { get; }

    public event Action? NavigateToLoginRequested;

    public ForgotPasswordViewModel(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        SubmitCommand = new RelayCommand(SubmitAsync);
        BackToLoginCommand = new RelayCommand(BackToLoginAsync);
    }

    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            await _authService.ChangePasswordAsync(Email, NewPassword);
            SuccessMessage = "Password updated";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private Task BackToLoginAsync()
    {
        NavigateToLoginRequested?.Invoke();
        return Task.CompletedTask;
    }
}
