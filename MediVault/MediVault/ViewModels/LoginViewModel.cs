using System;
using System.Threading.Tasks;
using System.Windows.Input;
using MediVault.Services;

namespace MediVault.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private string _username = "admin";
    private string _password = string.Empty;
    private string? _error;
    private bool _isBusy;

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string? Error
    {
        get => _error;
        set => SetField(ref _error, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public ICommand LoginCommand { get; }

    public event Action? LoginSucceeded;

    public LoginViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsBusy);
    }

    private async Task LoginAsync()
    {
        Error = null;
        IsBusy = true;
        try
        {
            var user = await AuthService.LoginAsync(Username, Password);
            if (user == null)
            {
                Error = "Invalid username or password.";
                return;
            }
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
