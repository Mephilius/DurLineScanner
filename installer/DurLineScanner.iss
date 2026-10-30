; Inno Setup script for the DUR-line Scanner
; Usage (see .github/workflows/release.yml):
;   ISCC.exe /DAppVersion=1.0.0 /DSourceDir=<publish folder> DurLineScanner.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\DurLineScanner"
#endif

#define AppName "DUR-line Scanner"
#define AppExe "DurLineScanner.exe"

[Setup]
AppId={{6F1C2B8E-4D3A-4C7B-9E51-2A8D7F0B3C64}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=DUR-line Scanner contributors
AppCopyright=© 2026 DUR-line Scanner contributors, GPL-3.0
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Installable for the current user without admin rights, optionally for all users
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

[InstallDelete]
; Shortcuts from versions up to 0.9.1 (spelled "DurLine Scanner" back then)
Type: files; Name: "{autoprograms}\DurLine Scanner.lnk"
Type: files; Name: "{autoprograms}\DurLine Scanner – Benutzerhandbuch.lnk"
Type: files; Name: "{autoprograms}\DurLine Scanner – User guide.lnk"
Type: files; Name: "{autodesktop}\DurLine Scanner.lnk"

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autoprograms}\{#AppName} – Benutzerhandbuch"; Filename: "{app}\BENUTZERHANDBUCH.md"; Languages: de
Name: "{autoprograms}\{#AppName} – User guide"; Filename: "{app}\USER_GUIDE.md"; Languages: en
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Measurement series (Documents\DurLineScanner) and settings (%AppData%\DurLineScanner) are kept on uninstall.
