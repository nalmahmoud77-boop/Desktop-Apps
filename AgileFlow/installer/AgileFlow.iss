; AgileFlow — Inno Setup installer script
; ------------------------------------------------------------------
; Wraps the self-contained single-file AgileFlow.exe (produced by
; publish.ps1) into a Windows installer with Start-menu shortcuts,
; an optional desktop icon, and Add/Remove-Programs uninstall support.
;
; Build it with:  installer\build-installer.ps1
; (that script runs publish.ps1 first, then compiles this .iss).
;
; Compiling directly:  ISCC.exe installer\AgileFlow.iss
; ------------------------------------------------------------------

#define MyAppName        "AgileFlow"
#define MyAppPublisher   "AgileFlow"
#define MyAppExeName     "AgileFlow.exe"

; Path to the published exe, relative to this .iss file.
#define MyAppExe         "..\dist\AgileFlow\" + MyAppExeName

; Pull the version straight from the compiled exe so the installer
; always matches the build (requires the exe to exist at compile time).
#define MyAppVersion     GetVersionNumbersString(MyAppExe)

[Setup]
; AppId uniquely identifies this app for upgrades/uninstall. Keep it STABLE
; across releases — never regenerate it, or upgrades install side-by-side.
AppId={{7B3F1E4A-9C82-4D5B-A1E7-6F2D0C4B8E31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
; Per-machine install into Program Files -> needs elevation.
PrivilegesRequired=admin
; AgileFlow is a 64-bit self-contained build.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Windows 10 or later (10.0 = build 10240).
MinVersion=10.0
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
OutputDir=..\dist
OutputBaseFilename=AgileFlow-Setup-{#MyAppVersion}
DisableProgramGroupPage=yes
DisableDirPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyAppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}";           Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";     Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Offer to launch AgileFlow when the wizard finishes.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; NOTE: The user database lives at %LOCALAPPDATA%\AgileFlow\agileflow.db and is
; intentionally NOT removed on uninstall, so reinstalling keeps the user's data
; and license. Deleting it is left to the user.
