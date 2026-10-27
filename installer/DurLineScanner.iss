; Inno-Setup-Skript für den DurLine Scanner
; Aufruf (siehe .github/workflows/release.yml):
;   ISCC.exe /DAppVersion=1.0.0 /DSourceDir=<publish-Ordner> DurLineScanner.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\DurLineScanner"
#endif

#define AppName "DurLine Scanner"
#define AppExe "DurLineScanner.exe"

[Setup]
AppId={{6F1C2B8E-4D3A-4C7B-9E51-2A8D7F0B3C64}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=DurLine Scanner contributors
AppCopyright=© 2026 DurLine Scanner contributors, GPL-3.0
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Ohne Adminrechte für den aktuellen Benutzer installierbar, wahlweise für alle
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputBaseFilename=DurLineScanner-{#AppVersion}-Setup
SetupIconFile=..\src\DurLineScanner\Assets\app.ico
LicenseFile=..\LICENSE
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autoprograms}\{#AppName} – Benutzerhandbuch"; Filename: "{app}\BENUTZERHANDBUCH.md"; Languages: de
Name: "{autoprograms}\{#AppName} – User guide"; Filename: "{app}\USER_GUIDE.md"; Languages: en
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Messreihen (Dokumente\DurLineScanner) und Einstellungen (%AppData%\DurLineScanner) bleiben beim Deinstallieren erhalten.
