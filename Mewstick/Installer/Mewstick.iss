; Script Inno Setup cho Mewstick (bản framework-dependent, cần .NET 8 Desktop Runtime)
; Nếu thư mục publish của bạn khác, sửa dòng PublishDir bên dưới.

#define MyAppName "Mewstick"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "ThinhNT"
#define MyAppURL "https://quanlysv.vercel.app"
#define MyAppExe "Mewstick.exe"
#define PublishDir "D:\Mewstick\Mewstick\bin\Release\net8.0-windows10.0.19041.0\win-x64"

[Setup]
AppId={{B7A3C1D2-5E4F-4A6B-9C8D-1F2E3A4B5C6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
AppCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoProductName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=D:\Mewstick\Installer\Output
OutputBaseFilename=Mewstick-Setup-{#MyAppVersion}
SetupIconFile=D:\Mewstick\Mewstick\Resource\app.ico
UninstallDisplayIcon={app}\{#MyAppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Trùng tên Mutex trong App.xaml.cs: nếu app đang chạy, bộ cài sẽ nhắc đóng app trước
AppMutex=Mewstick_SingleInstance_Mutex

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài Desktop"; GroupDescription: "Tuỳ chọn:"; Flags: unchecked
Name: "startup"; Description: "Khởi động cùng Windows"; GroupDescription: "Tuỳ chọn:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Mewstick"; ValueData: """{app}\{#MyAppExe}"" --startup"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Mở Mewstick ngay"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet8DesktopInstalled(): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.*'), FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDotNet8DesktopInstalled() then
  begin
    if MsgBox('Mewstick cần ".NET 8 Desktop Runtime (x64)" nhưng máy này chưa có.' + #13#10 +
              'Bấm Yes để mở trang tải về, cài xong hãy chạy lại bộ cài này.',
              mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Result := False;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  { Gỡ cài đặt thì xoá luôn mục khởi động cùng Windows (kể cả khi bật từ menu tray) }
  if CurUninstallStep = usUninstall then
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Mewstick');
end;
