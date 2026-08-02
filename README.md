# Desktop Apps

[![build](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml/badge.svg)](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20XAML-0078D4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Three Windows desktop applications written in **C#** on **.NET 8** with **WPF** and the
**MVVM** pattern. Each one is a self-contained solution exploring a different slice of
line-of-business desktop development — records management, task tracking and retail POS —
with a shared emphasis on layered architecture, testable service boundaries and
cryptographically signed licensing.

Every solution builds under CI with warnings treated as errors.

## Projects

| Project | Domain | Highlights |
| --- | --- | --- |
| [**MediVault**](MediVault/) | Clinic & medical records | EF Core + SQLite, BCrypt auth, role-based access control, audit log, FluentValidation, PDF export, HMAC-SHA256 licensing |
| [**AgileFlow**](AgileFlow/) | Kanban task manager | EF Core + SQLite, custom WPF controls, in-app message bus, ECDSA P-256 machine-bound licensing, Inno Setup installer |
| [**StoreApp**](StoreApp/) | Retail point-of-sale | Repository pattern, DTO service boundaries, dashboard metrics, cart & order workflow, BCrypt auth, HMAC-SHA256 licensing |

Each project has its own README with features, architecture and setup instructions.

## Common ground

| Area | Choice |
| --- | --- |
| Language / runtime | C# 12 on .NET 8 (`net8.0-windows`) |
| UI | WPF + XAML, MVVM, custom themes and resource dictionaries |
| Persistence | Entity Framework Core 8 (SQLite, or the InMemory provider for demo data) |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` / `Hosting` |
| Security | BCrypt.Net-Next password hashing; HMAC-SHA256 and ECDSA P-256 license signing |
| CI | GitHub Actions on `windows-latest`, Release build, `/warnaserror` |

## Building

Requires the **.NET 8 SDK** and Windows.

```bash
git clone https://github.com/nalmahmoud77-boop/Desktop-Apps.git
cd Desktop-Apps

dotnet build MediVault/MediVault.sln
dotnet build AgileFlow/AgileFlow.sln
dotnet build StoreApp/StoreApp.sln
```

Each solution also opens directly in Visual Studio 2022.

## A note on licensing modules

Each app ships a licensing layer, and the repository is deliberately explicit about the
trust boundary involved:

- **MediVault** and **StoreApp** use a *symmetric* HMAC-SHA256 scheme. The demonstration
  signing secret is committed so the app and its key generator work immediately after a
  clone — it is a sample value with nothing deployed behind it. A real deployment would
  load the secret from the environment rather than a compiled-in constant.
- **AgileFlow** uses an *asymmetric* ECDSA P-256 scheme. The app embeds only the public
  key; the private signing key is generated locally by the vendor tool and is gitignored,
  so no key in this repository can mint a valid license.

## License

[MIT](LICENSE)
