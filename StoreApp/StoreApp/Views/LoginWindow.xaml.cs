using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using StoreApp.ViewModels;

namespace StoreApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            _vm = vm;
            _vm.LoginSucceeded += _ =>
            {
                var app = (App)Application.Current;
                var main = app.Services.GetRequiredService<MainWindow>();
                app.MainWindow = main;
                main.Show();
                if (main.DataContext is MainViewModel mvm && mvm.CurrentView is DashboardViewModel dash)
                    dash.Refresh();
                Close();
            };
            PasswordBox.PasswordChanged += (_, _) => _vm.Password = PasswordBox.Password;
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.OriginalSource is System.Windows.Controls.TextBox or System.Windows.Controls.PasswordBox or System.Windows.Controls.Button) return;
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
            Loaded += (_, _) =>
            {
                PasswordBox.Password = _vm.Password;
                UsernameBox.Focus();
            };
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _vm.LoginCommand.CanExecute(PasswordBox.Password))
                _vm.LoginCommand.Execute(PasswordBox.Password);
        }
    }
}
