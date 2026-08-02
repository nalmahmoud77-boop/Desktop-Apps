using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediVault.Models;
using MediVault.Services;

namespace MediVault.ViewModels;

public class PrescriptionItemDraft : BaseViewModel
{
    private Medication? _medication;
    private string _dosage = "1 tablet";
    private string _frequency = "Twice daily";
    private string _duration = "5 days";
    private string? _instructions;
    private int _quantity = 10;

    public Medication? Medication { get => _medication; set => SetField(ref _medication, value); }
    public string Dosage { get => _dosage; set => SetField(ref _dosage, value); }
    public string Frequency { get => _frequency; set => SetField(ref _frequency, value); }
    public string Duration { get => _duration; set => SetField(ref _duration, value); }
    public string? Instructions { get => _instructions; set => SetField(ref _instructions, value); }
    public int Quantity { get => _quantity; set => SetField(ref _quantity, value); }
}

public class PrescriptionsViewModel : BaseViewModel
{
    private Prescription? _selected;
    private bool _isCreating;
    private string? _validationErrors;
    private bool _isBusy;

    public ObservableCollection<Prescription> Prescriptions { get; } = new();
    public ObservableCollection<Patient> Patients { get; } = new();
    public ObservableCollection<Doctor> Doctors { get; } = new();
    public ObservableCollection<Medication> Medications { get; } = new();
    public ObservableCollection<PrescriptionItemDraft> Items { get; } = new();

    private Patient? _selectedPatient;
    private Doctor? _selectedDoctor;
    private string? _diagnosis;
    private string? _notes;

    public Patient? SelectedPatient { get => _selectedPatient; set => SetField(ref _selectedPatient, value); }
    public Doctor? SelectedDoctor { get => _selectedDoctor; set => SetField(ref _selectedDoctor, value); }
    public string? Diagnosis { get => _diagnosis; set => SetField(ref _diagnosis, value); }
    public string? Notes { get => _notes; set => SetField(ref _notes, value); }

    public Prescription? Selected
    {
        get => _selected;
        set
        {
            SetField(ref _selected, value);
            OnPropertyChanged(nameof(HasSelected));
        }
    }

    public bool HasSelected => Selected != null;
    public bool IsCreating { get => _isCreating; set => SetField(ref _isCreating, value); }
    public string? ValidationErrors { get => _validationErrors; set => SetField(ref _validationErrors, value); }
    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }

    public ICommand NewCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand PrintCommand { get; }

    public PrescriptionsViewModel()
    {
        NewCommand = new RelayCommand(_ => StartNew());
        AddItemCommand = new RelayCommand(_ => Items.Add(new PrescriptionItemDraft()));
        RemoveItemCommand = new RelayCommand(p => { if (p is PrescriptionItemDraft d) Items.Remove(d); });
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(_ => Cancel());
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected != null);
        ExportPdfCommand = new RelayCommand(_ => ExportPdf(), _ => Selected != null);
        PrintCommand = new RelayCommand(_ => Print(), _ => Selected != null);
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var rxs = await PrescriptionService.GetAllAsync();
            Prescriptions.Clear();
            foreach (var rx in rxs) Prescriptions.Add(rx);

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

            if (Medications.Count == 0)
            {
                var meds = await PatientService.GetAllMedicationsAsync();
                foreach (var m in meds) Medications.Add(m);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartNew()
    {
        IsCreating = true;
        SelectedPatient = null;
        SelectedDoctor = null;
        Diagnosis = null;
        Notes = null;
        Items.Clear();
        Items.Add(new PrescriptionItemDraft());
        ValidationErrors = null;
    }

    private void Cancel()
    {
        IsCreating = false;
        ValidationErrors = null;
        Items.Clear();
    }

    private async Task SaveAsync()
    {
        ValidationErrors = null;
        if (SelectedPatient == null) { ValidationErrors = "Select a patient."; return; }
        if (SelectedDoctor == null) { ValidationErrors = "Select a doctor."; return; }
        if (Items.Count == 0) { ValidationErrors = "Add at least one medication."; return; }
        if (Items.Any(i => i.Medication == null)) { ValidationErrors = "Each item must have a medication."; return; }

        var rx = new Prescription
        {
            PatientId = SelectedPatient.Id,
            DoctorId = SelectedDoctor.Id,
            Diagnosis = Diagnosis,
            Notes = Notes,
            Items = Items.Select(i => new PrescriptionItem
            {
                MedicationId = i.Medication!.Id,
                Dosage = i.Dosage,
                Frequency = i.Frequency,
                Duration = i.Duration,
                Instructions = i.Instructions,
                Quantity = i.Quantity
            }).ToList()
        };

        try
        {
            await PrescriptionService.CreateAsync(rx);
            IsCreating = false;
            await LoadAsync();
            Selected = Prescriptions.FirstOrDefault(r => r.Id == rx.Id);
        }
        catch (Exception ex)
        {
            ValidationErrors = ex.Message;
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected == null) return;
        var res = MessageBox.Show($"Delete prescription {Selected.Code}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;
        await PrescriptionService.DeleteAsync(Selected.Id);
        Selected = null;
        await LoadAsync();
    }

    private void ExportPdf()
    {
        if (Selected == null) return;
        if (PdfService.ExportPrescription(Selected))
        {
            MessageBox.Show("Prescription exported.", "MediVault", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Print()
    {
        if (Selected == null) return;
        PdfService.PrintPrescription(Selected);
    }
}
