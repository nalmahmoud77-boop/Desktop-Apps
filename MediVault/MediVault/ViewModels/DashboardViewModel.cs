using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MediVault.Models;
using MediVault.Services;

namespace MediVault.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private int _patientCount;
    private int _appointmentsToday;
    private int _activePrescriptions;
    private int _doctorsCount;
    private string _greeting = "Welcome back";
    private bool _isBusy;

    public int PatientCount { get => _patientCount; set => SetField(ref _patientCount, value); }
    public int AppointmentsToday { get => _appointmentsToday; set => SetField(ref _appointmentsToday, value); }
    public int ActivePrescriptions { get => _activePrescriptions; set => SetField(ref _activePrescriptions, value); }
    public int DoctorsCount { get => _doctorsCount; set => SetField(ref _doctorsCount, value); }
    public string Greeting { get => _greeting; set => SetField(ref _greeting, value); }
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }

    public ObservableCollection<Appointment> UpcomingAppointments { get; } = new();
    public ObservableCollection<AuditLog> RecentActivity { get; } = new();

    public string TodayDate => DateTime.Now.ToString("dddd, MMMM d, yyyy");

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var name = AuthService.CurrentUser?.FullName?.Split(' ').FirstOrDefault() ?? "there";
            var hour = DateTime.Now.Hour;
            var part = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
            Greeting = $"{part}, {name}";

            var patientsTask = PatientService.CountAsync();
            var apptCountTask = AppointmentService.CountTodayAsync();
            var upcomingTask = AppointmentService.GetUpcomingAsync(8);
            var prescriptionsTask = PrescriptionService.GetAllAsync();
            var doctorsTask = AppointmentService.GetActiveDoctorsAsync();
            var auditsTask = AuditService.GetRecentAsync(10);

            await Task.WhenAll(patientsTask, apptCountTask, upcomingTask, prescriptionsTask, doctorsTask, auditsTask);

            PatientCount = patientsTask.Result;
            AppointmentsToday = apptCountTask.Result;
            ActivePrescriptions = prescriptionsTask.Result.Count;
            DoctorsCount = doctorsTask.Result.Count;

            UpcomingAppointments.Clear();
            foreach (var a in upcomingTask.Result) UpcomingAppointments.Add(a);

            RecentActivity.Clear();
            foreach (var a in auditsTask.Result) RecentActivity.Add(a);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
