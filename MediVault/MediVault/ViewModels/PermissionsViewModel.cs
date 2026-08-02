using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediVault.Models;
using MediVault.Services;

namespace MediVault.ViewModels;

/// <summary>One row of the role × permission matrix (Admin is locked on).</summary>
public class PermissionRow : BaseViewModel
{
    public AppPermission Permission { get; }
    public string Label { get; }
    public string Description { get; }

    private bool _doctor;
    private bool _receptionist;

    public PermissionRow(AppPermission permission, bool doctor, bool receptionist)
    {
        Permission = permission;
        Label = permission.DisplayName();
        Description = permission.Description();
        _doctor = doctor;
        _receptionist = receptionist;
    }

    /// <summary>Admin always has full access; shown checked and disabled.</summary>
    public bool Admin => true;
    public bool Doctor { get => _doctor; set => SetField(ref _doctor, value); }
    public bool Receptionist { get => _receptionist; set => SetField(ref _receptionist, value); }
}

public class PermissionsViewModel : BaseViewModel
{
    private User? _selected;
    private User _editing = NewUser();
    private string _editingPassword = string.Empty;
    private bool _isEditing;
    private bool _isNew;
    private string? _validationErrors;
    private bool _isBusy;

    public ObservableCollection<User> Users { get; } = new();
    public ObservableCollection<PermissionRow> Permissions { get; } = new();

    public Array Roles => Enum.GetValues(typeof(UserRole));

    public User? Selected
    {
        get => _selected;
        set
        {
            if (SetField(ref _selected, value))
                OnPropertyChanged(nameof(HasSelected));
        }
    }

    public User Editing { get => _editing; set => SetField(ref _editing, value); }
    public string EditingPassword { get => _editingPassword; set => SetField(ref _editingPassword, value); }
    public bool IsEditing { get => _isEditing; set => SetField(ref _isEditing, value); }
    public bool IsNew { get => _isNew; set => SetField(ref _isNew, value); }
    public string? ValidationErrors { get => _validationErrors; set => SetField(ref _validationErrors, value); }
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }
    public bool HasSelected => Selected != null;

    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SavePermissionsCommand { get; }

    public PermissionsViewModel()
    {
        NewCommand = new RelayCommand(_ => StartNew());
        EditCommand = new RelayCommand(_ => StartEdit(), _ => Selected != null);
        SaveCommand = new AsyncRelayCommand(SaveUserAsync);
        CancelCommand = new RelayCommand(_ => CancelEdit());
        DeleteCommand = new AsyncRelayCommand(DeleteUserAsync, () => Selected != null);
        SavePermissionsCommand = new AsyncRelayCommand(SavePermissionsAsync);
    }

    private static User NewUser() => new User
    {
        Role = UserRole.Receptionist,
        IsActive = true
    };

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var users = await UserService.GetAllAsync();
            var selectedId = Selected?.Id;
            Users.Clear();
            foreach (var u in users) Users.Add(u);
            Selected = selectedId == null ? null : Users.FirstOrDefault(u => u.Id == selectedId);

            var matrix = await PermissionService.GetAllAsync();
            bool Allowed(UserRole role, AppPermission p) =>
                matrix.FirstOrDefault(m => m.Role == role && m.Permission == p)?.Allowed ?? false;

            Permissions.Clear();
            foreach (AppPermission p in Enum.GetValues(typeof(AppPermission)))
                Permissions.Add(new PermissionRow(p, Allowed(UserRole.Doctor, p), Allowed(UserRole.Receptionist, p)));

            CancelEdit();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartNew()
    {
        Editing = NewUser();
        EditingPassword = string.Empty;
        IsNew = true;
        IsEditing = true;
        ValidationErrors = null;
    }

    private void StartEdit()
    {
        if (Selected == null) return;
        Editing = new User
        {
            Id = Selected.Id,
            Username = Selected.Username,
            FullName = Selected.FullName,
            Role = Selected.Role,
            IsActive = Selected.IsActive,
            CreatedAt = Selected.CreatedAt
        };
        EditingPassword = string.Empty;
        IsNew = false;
        IsEditing = true;
        ValidationErrors = null;
    }

    private void CancelEdit()
    {
        IsEditing = false;
        IsNew = false;
        EditingPassword = string.Empty;
        ValidationErrors = null;
    }

    private async Task SaveUserAsync()
    {
        ValidationErrors = null;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Editing.Username)) errors.Add("Username is required.");
        if (string.IsNullOrWhiteSpace(Editing.FullName)) errors.Add("Full name is required.");
        if (IsNew && string.IsNullOrWhiteSpace(EditingPassword)) errors.Add("A password is required for a new user.");
        if (!string.IsNullOrEmpty(EditingPassword) && EditingPassword.Length < 6)
            errors.Add("Password must be at least 6 characters.");
        if (errors.Count > 0)
        {
            ValidationErrors = string.Join("\n", errors.Select(e => "• " + e));
            return;
        }

        try
        {
            if (IsNew)
            {
                var added = await UserService.AddAsync(Editing, EditingPassword);
                await LoadAsync();
                Selected = Users.FirstOrDefault(u => u.Id == added.Id);
            }
            else
            {
                var updated = await UserService.UpdateAsync(Editing, EditingPassword);
                await LoadAsync();
                Selected = Users.FirstOrDefault(u => u.Id == updated.Id);
            }
        }
        catch (Exception ex)
        {
            ValidationErrors = ex.Message;
        }
    }

    private async Task DeleteUserAsync()
    {
        if (Selected == null) return;

        if (Selected.Id == AuthService.CurrentUser?.Id)
        {
            MessageBox.Show("You can't delete the account you're signed in with.",
                "MediVault", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = MessageBox.Show($"Delete user {Selected.Username}? This cannot be undone.",
            "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        try
        {
            await UserService.DeleteAsync(Selected.Id);
            Selected = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "MediVault", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SavePermissionsAsync()
    {
        var entries = new List<RolePermission>();
        foreach (var row in Permissions)
        {
            entries.Add(new RolePermission { Role = UserRole.Admin, Permission = row.Permission, Allowed = true });
            entries.Add(new RolePermission { Role = UserRole.Doctor, Permission = row.Permission, Allowed = row.Doctor });
            entries.Add(new RolePermission { Role = UserRole.Receptionist, Permission = row.Permission, Allowed = row.Receptionist });
        }

        await PermissionService.SaveAsync(entries);
        MessageBox.Show("Permissions saved. Changes take effect the next time a user signs in.",
            "MediVault", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
