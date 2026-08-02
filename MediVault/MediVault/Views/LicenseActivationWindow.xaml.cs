using System.Windows;
using System.Windows.Input;
using MediVault.ViewModels;

namespace MediVault.Views;

public partial class LicenseActivationWindow : Window
{
    private readonly LicenseActivationViewModel _vm;

    public bool WasActivated { get; private set; }

    public LicenseActivationWindow(LicenseActivationViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
        _vm.ActivationSucceeded += OnActivationSucceeded;
    }

    private void OnActivationSucceeded()
    {
        WasActivated = true;
        Close();
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
