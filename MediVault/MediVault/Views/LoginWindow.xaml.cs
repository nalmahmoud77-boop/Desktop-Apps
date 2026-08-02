using System.Windows;
using System.Windows.Input;
using MediVault.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediVault.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        _vm.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    private void OnLoginSucceeded()
    {
        var main = App.Services.GetRequiredService<MainWindow>();
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        _vm.Password = PasswordBox.Password;
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _vm.Password = PasswordBox.Password;
            if (_vm.LoginCommand.CanExecute(null)) _vm.LoginCommand.Execute(null);
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
