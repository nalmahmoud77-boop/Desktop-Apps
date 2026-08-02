using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediVault.Models;
using MediVault.Services;

namespace MediVault.ViewModels;

public class AppointmentsViewModel : BaseViewModel
{
    private DateTime _selectedDate = DateTime.Today;
    private Appointment? _selected;
    private Appointment _editing = NewAppointment();
    private bool _isEditing;
    private bool _isNew;
    private string? _validationErrors;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    public ObservableCollection<Appointment> Appointments { get; } = new();
    public ObservableCollection<Patient> Patients { get; } = new();
    public ObservableCollection<Doctor> Doctors { get; } = new();

    public Array Statuses => Enum.GetValues(typeof(AppointmentStatus));

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetField(ref _selectedDate, value)) _ = LoadAsync();
        }
    }

    public Appointment? Selected
    {
        get => _selected;
        set
        {
            SetField(ref _selected, value);
            OnPropertyChanged(nameof(HasSelected));
        }
    }

    public bool HasSelected => Selected != null;

    public Appointment Editing { get => _editing; set => SetField(ref _editing, value); }
    public bool IsEditing { get => _isEditing; set => SetField(ref _isEditing, value); }
    public bool IsNew { get => _isNew; set => SetField(ref _isNew, value); }
    public string? ValidationErrors { get => _validationErrors; set => SetField(ref _validationErrors, value); }
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    private DateTime _startDate = DateTime.Today;
    private TimeSpan _startTime = new TimeSpan(9, 0, 0);
    private int _durationMinutes = 30;

    public DateTime StartDate { get => _startDate; set { if (SetField(ref _startDate, value)) UpdateEditingTimes(); } }
    public TimeSpan StartTime { get => _startTime; set { if (SetField(ref _startTime, value)) UpdateEditingTimes(); } }
    public int DurationMinutes { get => _durationMinutes; set { if (SetField(ref _durationMinutes, value)) UpdateEditingTimes(); } }

    public AppointmentsViewModel()
    {
        NewCommand = new RelayCommand(_ => StartNew());
        EditCommand = new RelayCommand(_ => StartEdit(), _ => Selected != null);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(_ => CancelEdit());
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected != null);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    private static Appointment NewAppointment()
    {
        var start = DateTime.Today.AddHours(9);
        return new Appointment
        {
            StartTime = start,
            EndTime = start.AddMinutes(30),
            Status = AppointmentStatus.Scheduled
        };
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var rangeStart = SelectedDate.Date;
            var rangeEnd = rangeStart.AddDays(1);
            var appts = await AppointmentService.GetForRangeAsync(rangeStart, rangeEnd);
            Appointments.Clear();
            foreach (var a in appts) Appointments.Add(a);

            if (Patients.Count == 0)
            {
                var pts = await PatientService.GetAllAsync();
                foreach (var p in pts) Patients.Add(p);
            }

            if (Doctors.Count == 0)
            {
                var docs = await AppointmentService.GetActiveDoctorsAsync();
                foreach (var d in docs) Doctors.Add(d);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateEditingTimes()
    {
        var start = StartDate.Date + StartTime;
        Editing.StartTime = start;
        Editing.EndTime = start.AddMinutes(DurationMinutes);
    }

    private void StartNew()
    {
        Editing = NewAppointment();
        StartDate = SelectedDate.Date;
        StartTime = new TimeSpan(9, 0, 0);
        DurationMinutes = 30;
        UpdateEditingTimes();
        IsNew = true;
        IsEditing = true;
        ValidationErrors = null;
    }

    private void StartEdit()
    {
        if (Selected == null) return;
        Editing = new Appointment
        {
            Id = Selected.Id,
            PatientId = Selected.PatientId,
            DoctorId = Selected.DoctorId,
            StartTime = Selected.StartTime,
            EndTime = Selected.EndTime,
            Reason = Selected.Reason,
            Notes = Selected.Notes,
            Status = Selected.Status
        };
        StartDate = Selected.StartTime.Date;
        StartTime = Selected.StartTime.TimeOfDay;
        DurationMinutes = (int)(Selected.EndTime - Selected.StartTime).TotalMinutes;
        IsNew = false;
        IsEditing = true;
        ValidationErrors = null;
    }

    private void CancelEdit()
    {
        IsEditing = false;
        IsNew = false;
        ValidationErrors = null;
    }

    private async Task SaveAsync()
    {
        ValidationErrors = null;
        UpdateEditingTimes();

        if (Editing.PatientId <= 0) { ValidationErrors = "Please select a patient."; return; }
        if (Editing.DoctorId <= 0) { ValidationErrors = "Please select a doctor."; return; }
        if (Editing.EndTime <= Editing.StartTime) { ValidationErrors = "End time must be after start time."; return; }

        try
        {
            if (IsNew) await AppointmentService.CreateAsync(Editing);
            else await AppointmentService.UpdateAsync(Editing);

            IsEditing = false;
            IsNew = false;
            StatusMessage = "Saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ValidationErrors = ex.Message;
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected == null) return;
        var res = MessageBox.Show("Delete this appointment?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        await AppointmentService.DeleteAsync(Selected.Id);
        Selected = null;
        await LoadAsync();
    }
}
