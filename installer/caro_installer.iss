; Inno Setup Script for Caro Game (Caro Arena)
; Tải Inno Setup miễn phí tại: https://jrsoftware.org/isdl.php
; Mở file này bằng Inno Setup và nhấn F9 (Build) để xuất ra file "CaroGame_Setup_v1.0.exe"

#define MyAppName "Cờ Caro Vô Hạn (Caro Arena)"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Caro Arena Team"
#define MyAppExeName "CaroGame.Wpf.exe"

[Setup]
AppId={{E5813A29-798B-4B52-87F9-5B26E2D07C0A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\CaroGame
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\publish\Installer
OutputBaseFilename=CaroGame_Setup_v1.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish\CaroClient\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
