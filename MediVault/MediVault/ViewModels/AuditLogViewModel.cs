using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using MediVault.Models;
using MediVault.Services;

namespace MediVault.ViewModels;

public class AuditLogViewModel : BaseViewModel
{
    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }

    public ObservableCollection<AuditLog> Entries { get; } = new();

    public ICommand RefreshCommand { get; }

    public AuditLogViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var entries = await AuditService.GetRecentAsync(500);
            Entries.Clear();
            foreach (var e in entries) Entries.Add(e);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
