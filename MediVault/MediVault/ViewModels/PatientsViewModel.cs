using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediVault.Models;
using MediVault.Services;
using MediVault.Validators;
using Condition = MediVault.Models.Condition;

namespace MediVault.ViewModels;

public class PatientsViewModel : BaseViewModel
{
    private string _search = string.Empty;
    private Patient? _selected;
    private Patient _editing = NewPatient();
    private bool _isEditing;
    private bool _isNew;
    private string? _validationErrors;
    private bool _isBusy;

    public ObservableCollection<Patient> Patients { get; } = new();
    public ObservableCollection<Medication> AvailableMedications { get; } = new();
    public ObservableCollection<Condition> AvailableConditions { get; } = new();

    public ObservableCollection<PatientMedication> PatientMedications { get; } = new();
    public ObservableCollection<PatientCondition> PatientConditions { get; } = new();

    private Medication? _selectedMedication;
    private Condition? _selectedCondition;
    private string _newDosage = "1 tablet";
    private string _newFrequency = "Twice daily";
    private DateTime _newStart = DateTime.Today;
    private DateTime? _newEnd;
    private DateTime _newDiagnosed = DateTime.Today;
    private ConditionSeverity _newSeverity = ConditionSeverity.Mild;

    public Medication? SelectedMedication { get => _selectedMedication; set => SetField(ref _selectedMedication, value); }
    public Condition? SelectedCondition { get => _selectedCondition; set => SetField(ref _selectedCondition, value); }
    public string NewDosage { get => _newDosage; set => SetField(ref _newDosage, value); }
    public string NewFrequency { get => _newFrequency; set => SetField(ref _newFrequency, value); }
    public DateTime NewStart { get => _newStart; set => SetField(ref _newStart, value); }
    public DateTime? NewEnd { get => _newEnd; set => SetField(ref _newEnd, value); }
    public DateTime NewDiagnosed { get => _newDiagnosed; set => SetField(ref _newDiagnosed, value); }
    public ConditionSeverity NewSeverity { get => _newSeverity; set => SetField(ref _newSeverity, value); }

    public Array Genders => Enum.GetValues(typeof(Gender));
    public Array Severities => Enum.GetValues(typeof(ConditionSeverity));

    public string Search
    {
        get => _search;
        set
        {
            if (SetField(ref _search, value)) _ = LoadAsync();
        }
    }

    public Patient? Selected
    {
        get => _selected;
        set
        {
            if (SetField(ref _selected, value))
            {
                _ = LoadDetailAsync();
            }
        }
    }

    public Patient Editing { get => _editing; set => SetField(ref _editing, value); }
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
    public ICommand AddMedicationCommand { get; }
    public ICommand RemoveMedicationCommand { get; }
    public ICommand AddConditionCommand { get; }
    public ICommand RemoveConditionCommand { get; }

    public PatientsViewModel()
    {
        NewCommand = new RelayCommand(_ => StartNew());
        EditCommand = new RelayCommand(_ => StartEdit(), _ => Selected != null);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(_ => CancelEdit());
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => Selected != null);
        AddMedicationCommand = new AsyncRelayCommand(AddMedicationAsync, () => Selected != null && SelectedMedication != null);
        RemoveMedicationCommand = new AsyncRelayCommand(RemoveMedicationAsync);
        AddConditionCommand = new AsyncRelayCommand(AddConditionAsync, () => Selected != null && SelectedCondition != null);
        RemoveConditionCommand = new AsyncRelayCommand(RemoveConditionAsync);
    }

    private static Patient NewPatient() => new Patient
    {
        MedicalId = $"MED-{new Random().Next(100000, 999999)}",
        DateOfBirth = DateTime.Today.AddYears(-30),
        Gender = Gender.Male
    };

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var patients = await PatientService.GetAllAsync(Search);
            var meds = await PatientService.GetAllMedicationsAsync();
            var conds = await PatientService.GetAllConditionsAsync();

            Patients.Clear();
            foreach (var p in patients) Patients.Add(p);
            AvailableMedications.Clear();
            foreach (var m in meds) AvailableMedications.Add(m);
            AvailableConditions.Clear();
            foreach (var c in conds) AvailableConditions.Add(c);

            if (Selected != null)
            {
                var match = Patients.FirstOrDefault(x => x.Id == Selected.Id);
                Selected = match;
            }
            OnPropertyChanged(nameof(HasSelected));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDetailAsync()
    {
        OnPropertyChanged(nameof(HasSelected));
        PatientMedications.Clear();
        PatientConditions.Clear();
        IsEditing = false;
        ValidationErrors = null;

        if (Selected == null) return;

        var detail = await PatientService.GetByIdAsync(Selected.Id);
        if (detail == null) return;

        foreach (var pm in detail.PatientMedications) PatientMedications.Add(pm);
        foreach (var pc in detail.PatientConditions) PatientConditions.Add(pc);
    }

    private void StartNew()
    {
        Editing = NewPatient();
        IsNew = true;
        IsEditing = true;
        ValidationErrors = null;
    }

    private void StartEdit()
    {
        if (Selected == null) return;
        Editing = new Patient
        {
            Id = Selected.Id,
            MedicalId = Selected.MedicalId,
            FirstName = Selected.FirstName,
            LastName = Selected.LastName,
            DateOfBirth = Selected.DateOfBirth,
            Gender = Selected.Gender,
            Phone = Selected.Phone,
            Email = Selected.Email,
            Address = Selected.Address,
            BloodGroup = Selected.BloodGroup,
            MedicalHistory = Selected.MedicalHistory,
            Allergies = Selected.Allergies
        };
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
        var validator = new PatientValidator();
        var result = await validator.ValidateAsync(Editing);
        if (!result.IsValid)
        {
            ValidationErrors = string.Join("\n", result.Errors.Select(e => "• " + e.ErrorMessage));
            return;
        }

        try
        {
            if (IsNew)
            {
                var added = await PatientService.AddAsync(Editing);
                await LoadAsync();
                Selected = Patients.FirstOrDefault(p => p.Id == added.Id);
            }
            else
            {
                var updated = await PatientService.UpdateAsync(Editing);
                await LoadAsync();
                Selected = Patients.FirstOrDefault(p => p.Id == updated.Id);
            }
            IsEditing = false;
            IsNew = false;
        }
        catch (Exception ex)
        {
            ValidationErrors = ex.Message;
        }
    }

    private async Task DeleteAsync()
    {
        if (Selected == null) return;
        var res = MessageBox.Show($"Delete patient {Selected.FullName}? This cannot be undone.",
            "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        await PatientService.DeleteAsync(Selected.Id);
        Selected = null;
        await LoadAsync();
    }

    private async Task AddMedicationAsync()
    {
        if (Selected == null || SelectedMedication == null) return;
        await PatientService.AssignMedicationAsync(Selected.Id, SelectedMedication.Id, NewDosage, NewFrequency, NewStart, NewEnd, null);
        await LoadDetailAsync();
    }

    private async Task RemoveMedicationAsync(object? param)
    {
        if (param is PatientMedication pm && Selected != null)
        {
            await PatientService.RemoveMedicationAsync(pm.PatientId, pm.MedicationId, pm.StartDate);
            await LoadDetailAsync();
        }
    }

    private async Task AddConditionAsync()
    {
        if (Selected == null || SelectedCondition == null) return;
        await PatientService.AssignConditionAsync(Selected.Id, SelectedCondition.Id, NewDiagnosed, NewSeverity, null);
        await LoadDetailAsync();
    }

    private async Task RemoveConditionAsync(object? param)
    {
        if (param is PatientCondition pc && Selected != null)
        {
            await PatientService.RemoveConditionAsync(pc.PatientId, pc.ConditionId, pc.DiagnosedOn);
            await LoadDetailAsync();
        }
    }
}
