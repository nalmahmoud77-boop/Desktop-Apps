using System.Windows;
using System.Windows.Input;
using StoreApp.ViewModels;

namespace StoreApp.Views
{
    public partial class LicenseInfoWindow : Window
    {
        private readonly LicenseInfoViewModel _vm;

        public LicenseInfoWindow(LicenseInfoViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            _vm = vm;
            _vm.CloseRequested += Close;

            MouseLeftButtonDown += (_, e) =>
            {
                if (e.OriginalSource is System.Windows.Controls.TextBox or System.Windows.Controls.Button) return;
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
