using System;
using FluentValidation;
using MediVault.Models;

namespace MediVault.Validators;

public class PatientValidator : AbstractValidator<Patient>
{
    public PatientValidator()
    {
        RuleFor(p => p.MedicalId)
            .NotEmpty().WithMessage("Medical ID is required.")
            .Matches(@"^MED-\d{6}$").WithMessage("Medical ID must follow format MED-XXXXXX (6 digits).");

        RuleFor(p => p.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(80);

        RuleFor(p => p.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(80);

        RuleFor(p => p.DateOfBirth)
            .Must(d => d > new DateTime(1900, 1, 1) && d <= DateTime.Today)
            .WithMessage("Date of birth must be between 1900 and today.");

        RuleFor(p => p.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(@"^\+?[0-9\-\s\(\)]{7,20}$").WithMessage("Phone number format is invalid (digits, dashes, spaces or parentheses).");

        RuleFor(p => p.Email)
            .EmailAddress().WithMessage("Invalid email address.")
            .When(p => !string.IsNullOrWhiteSpace(p.Email));

        RuleFor(p => p.BloodGroup)
            .Matches(@"^(A|B|AB|O)[+-]$").WithMessage("Blood group must be A+, A-, B+, B-, AB+, AB-, O+, or O-.")
            .When(p => !string.IsNullOrWhiteSpace(p.BloodGroup));
    }
}
