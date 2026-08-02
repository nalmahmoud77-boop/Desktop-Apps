using FluentValidation;
using MediVault.Models;

namespace MediVault.Validators;

public class AppointmentValidator : AbstractValidator<Appointment>
{
    public AppointmentValidator()
    {
        RuleFor(a => a.PatientId).GreaterThan(0).WithMessage("Patient is required.");
        RuleFor(a => a.DoctorId).GreaterThan(0).WithMessage("Doctor is required.");
        RuleFor(a => a.StartTime).NotEmpty();
        RuleFor(a => a.EndTime).GreaterThan(a => a.StartTime).WithMessage("End time must be after start time.");
        RuleFor(a => a.Reason).MaximumLength(200);
    }
}
