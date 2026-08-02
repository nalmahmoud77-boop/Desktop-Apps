using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MediVault.Models;
using MediVault.ViewModels;

namespace MediVault.Views;

public partial class AppointmentsView : UserControl
{
    public AppointmentsView()
    {
        InitializeComponent();
    }

    private void Appointment_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is Appointment a && DataContext is AppointmentsViewModel vm)
        {
            vm.Selected = a;
        }
    }
}
