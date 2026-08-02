# StoreApp — Retail POS Desktop Application

[![build](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml/badge.svg)](https://github.com/nalmahmoud77-boop/Desktop-Apps/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20%2F%20XAML-0078D4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A retail store / point-of-sale desktop application built with **WPF** and **C#** on **.NET 8**,
following the **MVVM** pattern with a layered architecture, a repository layer and
DTO-based service boundaries.

## Features

- **Dashboard** with key business metrics (revenue, orders, stock alerts)
- **Products** — catalog management across categories, discounts and stock status
- **Customers** — customer records and order history
- **Cart & Orders** — build a cart, apply tax/shipping rules and place orders
- **Authentication** — BCrypt-verified login with role-based session handling
  (Admin / Manager / Cashier)
- **Licensing** — HMAC-SHA256 license validation with a separate issuer tool

## Architecture

The solution is deliberately layered so that each concern is swappable and testable:

```
Views (XAML)  →  ViewModels  →  Services (interfaces)  →  Repositories  →  DbContext
                                      ↕
                                    DTOs
```

Entities never cross the service boundary — services accept and return DTOs, which keeps
the view models free of EF Core types.

## Tech stack

| Area | Choice |
| --- | --- |
| Language / runtime | C# 12 on .NET 8 (`net8.0-windows`) |
| UI | WPF + XAML, MVVM |
| Persistence | Entity Framework Core 8 (InMemory provider for the demo dataset) |
| DI | `Microsoft.Extensions.DependencyInjection` |
| Security | BCrypt.Net-Next password hashing, HMAC-SHA256 license signing |
| Service contracts | `IProductService`, `ICartService`, `IOrderService`, `ICustomerService`, `IDashboardService`, `IAuthService`, `ILicenseService` |

## Solution structure

```
StoreApp/
├── StoreApp/                        Main WPF application
│   ├── Models/Entities/             Product, Customer, Order, OrderItem, User
│   ├── Models/DTOs/                 Service-boundary DTOs
│   ├── Enums/                       ProductCategory, OrderStatus, UserRole, ...
│   ├── Data/                        DbContext and seed data
│   ├── Repositories/                Generic + per-entity repositories
│   ├── Services/                    Business logic and licensing
│   ├── Helpers/                     Shared utilities
│   ├── Styles/                      Resource dictionaries
│   ├── ViewModels/                  MVVM view models
│   └── Views/                       XAML views
├── StoreApp.Licensing/              Licensing library (key format + verification)
├── StoreApp.KeyGen/                 License key generator (WPF)
└── tools/StoreApp.LicenseIssuer/    Console license issuer
```

## Getting started

Requires the **.NET 8 SDK** and Windows.

```bash
git clone https://github.com/nalmahmoud77-boop/Desktop-Apps.git
cd Desktop-Apps/StoreApp
dotnet build StoreApp.sln
dotnet run --project StoreApp/StoreApp.csproj
```

Or open `StoreApp.sln` in Visual Studio 2022 and press **F5**.

### Demo credentials

The in-memory database is seeded on every run with a demo catalogue and two accounts:

| Username | Password | Role |
| --- | --- | --- |
| `admin` | `admin` | Admin |
| `manager` | `manager` | Manager |

Seed passwords are BCrypt-hashed at seed time, so no plaintext credential is ever persisted.

## A note on the licensing module

`StoreApp.Licensing/LicenseSecret.cs` contains a **demonstration** HMAC signing secret so that
the app, the key generator and the issuer all work out of the box when you clone the repo.
It is a sample value with no commercial deployment behind it.

If you fork this for anything real, generate a fresh 32-byte secret and load it from an
environment variable or a gitignored local file rather than a compiled-in constant — anyone
holding the signing secret can mint valid license keys.

## License

[MIT](LICENSE)
