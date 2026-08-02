using System.Windows;
using System.Windows.Input;
using StoreApp.ViewModels;

namespace StoreApp.Views
{
    public partial class LicenseWindow : Window
    {
        private readonly LicenseViewModel _vm;

        public LicenseWindow(LicenseViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            _vm = vm;
            _vm.Activated += _ =>
            {
                DialogResult = true;
                Close();
            };
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.OriginalSource is System.Windows.Controls.TextBox or System.Windows.Controls.Button) return;
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
