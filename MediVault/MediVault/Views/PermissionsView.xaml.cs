using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MediVault.ViewModels;

namespace MediVault.Views;

public partial class PermissionsView : UserControl
{
    private PermissionsViewModel? _vm;

    public PermissionsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm = DataContext as PermissionsViewModel;
        if (_vm != null) _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    // PasswordBox can't data-bind, so mirror its value into the view model.
    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.EditingPassword = PasswordBox.Password;
    }

    // When the view model clears the password (new/edit/cancel), clear the box too.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PermissionsViewModel.EditingPassword)
            && string.IsNullOrEmpty(_vm?.EditingPassword)
            && PasswordBox.Password.Length > 0)
        {
            PasswordBox.Password = string.Empty;
        }
    }
}
