# MediVault — Clinic & Medical Records Desktop App

[![build](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml/badge.svg)](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20XAML-0078D4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A clinic / medical-records desktop application built with **WPF** and **C#** on **.NET 8**,
following the **MVVM** pattern with a layered service architecture, EF Core persistence,
BCrypt-hashed credentials and role-based access control.

## Features

- **Patients** — records, medical conditions and medications per patient
- **Doctors & Appointments** — scheduling and management
- **Prescriptions** — prescriptions with multiple prescription items
- **Authentication & authorization** — users, roles and role-based permissions (RBAC)
- **Password security** — BCrypt hashing with a work factor of 11
- **Audit log** — tracks sensitive actions for accountability
- **Input validation** — FluentValidation rules across entry forms
- **PDF export** — generate documents from records
- **Licensing module** — HMAC-SHA256 signed license keys with a companion key generator

## Tech stack

| Area | Choice |
| --- | --- |
| Language / runtime | C# 12 on .NET 8 (`net8.0-windows`) |
| UI | WPF + XAML, MVVM, custom themes |
| Persistence | Entity Framework Core 8 + SQLite (lazy-loading proxies) |
| DI / hosting | `Microsoft.Extensions.Hosting` + `DependencyInjection` |
| Security | BCrypt.Net-Next, HMAC-SHA256 license signing |
| Validation | FluentValidation |

## Solution structure

```
MediVault/
├── MediVault/                 Main WPF application
│   ├── Models/                Domain entities (Patient, Doctor, Appointment, Prescription, ...)
│   ├── Data/                  DbContext and seed data
│   ├── Services/              Business logic (Patient, Appointment, Auth, Permission, Audit, Pdf, ...)
│   ├── Validators/            FluentValidation rules
│   ├── Converters/            XAML value converters
│   ├── Themes/                Styles and resource dictionaries
│   ├── ViewModels/            MVVM view models
│   └── Views/                 XAML views
├── MediVault.Licensing/       Licensing library (key format + verification)
└── MediVault.KeyGen/          License key generator tool
```

## Getting started

Requires the **.NET 8 SDK** and Windows.

```bash
git clone https://github.com/nalmahmoud77-boop/Desktop-Apps.git
cd Desktop-Apps/MediVault
dotnet build MediVault.sln
dotnet run --project MediVault/MediVault.csproj
```

Or open `MediVault.sln` in Visual Studio 2022 and press **F5**.

### Demo credentials

The database is seeded on first run with a single administrator account:

| Username | Password |
| --- | --- |
| `admin` | `admin123` |

Change it immediately in any real deployment.

## A note on the licensing module

`MediVault.Licensing/EmbeddedSecret.cs` contains a **demonstration** HMAC signing secret so
that the app and the key generator work out of the box when you clone the repo. It is a sample
value with no commercial deployment behind it.

If you fork this for anything real, generate a fresh 32-byte secret and load it from an
environment variable or a gitignored local file rather than a compiled-in constant — anyone
holding the signing secret can mint valid license keys.

## License

[MIT](LICENSE)
