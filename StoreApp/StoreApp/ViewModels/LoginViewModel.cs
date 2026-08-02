using System.Windows;
using System.Windows.Input;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _auth;
        private string _username = "admin";
        private string _password = "admin";
        private string _errorMessage = string.Empty;
        private bool _isBusy;

        public LoginViewModel(IAuthService auth)
        {
            _auth = auth;
            LoginCommand = new RelayCommand(ExecuteLogin, _ => !IsBusy);
        }

        public string Username { get => _username; set => SetProperty(ref _username, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        public ICommand LoginCommand { get; }

        public event Action<UserDto>? LoginSucceeded;

        private void ExecuteLogin(object? parameter)
        {
            ErrorMessage = string.Empty;
            IsBusy = true;
            try
            {
                var pwd = parameter as string ?? Password;
                var user = _auth.Login(new LoginDto { Username = Username, Password = pwd });
                if (user == null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }
                LoginSucceeded?.Invoke(user);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
