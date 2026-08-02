# AgileFlow — Kanban Task Manager

[![build](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml/badge.svg)](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20XAML-0078D4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A personal **Kanban-style task manager** for Windows, built with **WPF** and **C#** on
**.NET 8** using the **MVVM** pattern — with drag-and-drop boards, SQLite persistence,
an Inno Setup installer and ECDSA-signed licensing.

## Features

- **Boards** with configurable **columns** (e.g. To Do / In Progress / Done)
- **Task items** with title, description, **priority** and **tags**
- Move tasks between columns
- Custom WPF controls and dialogs for a clean UX
- **Local-first persistence** — SQLite database in the user's app-data folder
- **Licensing** — ECDSA P-256 signed license keys, machine-bound activation
- **Distributable** — self-contained publish profile and Inno Setup installer

## Tech stack

| Area | Choice |
| --- | --- |
| Language / runtime | C# 12 on .NET 8 (`net8.0-windows`) |
| UI | WPF + XAML, MVVM, custom controls |
| Persistence | Entity Framework Core 8 + SQLite |
| Messaging | Lightweight in-app messenger for view-model decoupling |
| Licensing | ECDSA P-256 (`ImportPkcs8PrivateKey` / `ImportSubjectPublicKeyInfo`) |
| Packaging | `publish.ps1` self-contained build + Inno Setup (`installer/AgileFlow.iss`) |

## Solution structure

```
AgileFlow/
├── PersonalTaskManagement/      Main WPF application (AssemblyName: AgileFlow)
│   ├── Models/                  Board, BoardColumn, TaskItem, Tag, Priority
│   ├── Data/                    DbContext and persistence
│   ├── Licensing/               License key parsing, verification, activation state
│   ├── Messaging/               In-app message bus
│   ├── ViewModels/              MVVM view models
│   └── Views/                   XAML views, Controls, Dialogs
├── installer/                   Inno Setup script + build script
└── tools/LicenseKeyTool/        Vendor-side license key generator
```

## Getting started

Requires the **.NET 8 SDK** and Windows.

```bash
git clone https://github.com/nalmahmoud77-boop/Desktop-Apps.git
cd Desktop-Apps/AgileFlow
dotnet build AgileFlow.sln
dotnet run --project PersonalTaskManagement/PersonalTaskManagement.csproj
```

Or open `AgileFlow.sln` in Visual Studio 2022 and press **F5**.

See [DEPLOY.md](DEPLOY.md) for building the self-contained release and installer.

## Licensing keys

Licensing uses an **asymmetric** design: the app embeds only the ECDSA *public* key, while the
vendor-side private key stays out of the repository entirely.

`tools/LicenseKeyTool` generates its own keypair on first run and writes the private key to
`signing-key.pkcs8.b64` next to the binary. That file is **gitignored and intentionally not
committed** — a private signing key in a public repo would let anyone mint valid licenses.

To set up your own signing identity:

```bash
dotnet run --project tools/LicenseKeyTool          # generates signing-key.pkcs8.b64
```

Then copy the printed public key into the app's embedded public-key constant so the two sides
match. Rotating the keypair invalidates every previously issued key.

## License

[MIT](LICENSE)
