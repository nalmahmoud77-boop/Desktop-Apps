using System.Windows;
using StoreApp.ViewModels;

namespace StoreApp
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.LoggedOut += () =>
            {
                ((App)Application.Current).Restart();
            };
        }
    }
}
