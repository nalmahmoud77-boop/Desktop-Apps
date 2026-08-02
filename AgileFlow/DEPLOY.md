# Deploying AgileFlow

## Build a production release

From the solution root:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish.ps1
```

This produces:

- `dist\AgileFlow\AgileFlow.exe` — a **self-contained, single-file** Windows x64 executable
- `dist\AgileFlow-win-x64.zip` — the same, zipped and ready to send

"Self-contained" means the target PC does **not** need the .NET runtime installed —
everything (WPF, EF Core, the SQLite native library) is bundled into the one `.exe`.

> You can also publish from Visual Studio: right-click the **PersonalTaskManagement**
> project → **Publish** → pick the **Production** profile.

## What to send

Send the customer just **`AgileFlow.exe`** (or the zip). No installer is required —
they double-click it to run. On first launch it:

1. Creates its database at `%LOCALAPPDATA%\AgileFlow\agileflow.db`
2. Asks for a **license key** (see below)

## Target machine requirements

- Windows 10 / 11, 64-bit
- No .NET install needed (self-contained build)

## Licensing (per customer)

Licenses are cryptographically signed and bound to a machine, so each customer needs
a key minted for **their** machine.

1. Ask the customer to run `AgileFlow.exe`; on the activation screen they click
   **Copy** to get their **Machine ID**.
2. On your (vendor) machine, mint a key with the tool:

   ```powershell
   dotnet run --project tools/LicenseKeyTool -- mint monthly --machine <THEIR_ID>
   dotnet run --project tools/LicenseKeyTool -- mint yearly  --machine <THEIR_ID>
   dotnet run --project tools/LicenseKeyTool -- mint full    --machine <THEIR_ID>
   ```

   Use `--any` instead of `--machine <ID>` to issue an unbound key that works on any PC.

3. Send them the key; they paste it into the activation window.

### ⚠ Keep these OUT of the customer build

- `tools/LicenseKeyTool/` — the **key generator**, and especially
  `tools/LicenseKeyTool/signing-key.pkcs8.b64`, your **private signing key**.
  If this leaks, anyone can mint valid licenses. Never ship it; back it up securely.

The app itself only contains the **public** key, so the shipped `AgileFlow.exe` cannot
mint keys.

## Versioning

Bump `<Version>` / `<FileVersion>` / `<AssemblyVersion>` in
`PersonalTaskManagement/PersonalTaskManagement.csproj` before each release.

## Optional improvements for production

- **App icon**: add an `.ico` and set `<ApplicationIcon>app.ico</ApplicationIcon>` in the csproj.
- **Installer**: wrap `AgileFlow.exe` in an Inno Setup or WiX/MSIX installer for
  Start-menu shortcuts and uninstall support.
- **Code signing**: sign `AgileFlow.exe` with an Authenticode certificate so Windows
  SmartScreen doesn't warn users on first run.
