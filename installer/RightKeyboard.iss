#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif
#ifndef AppVersion
  #define AppVersion "1.5.0-alpha.6"
#endif

#define AppId "{{D7EC931A-AB1D-4F36-9F95-674B202CF334}"
#define AppIdValue "{D7EC931A-AB1D-4F36-9F95-674B202CF334}"
#define AppMutex "Local\RightKeyboard.SingleInstance"
#define CloseEvent "Local\RightKeyboard.Close"
#define RunKey "Software\Microsoft\Windows\CurrentVersion\Run"
#define StartupApprovedKey "Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run"
#define UninstallKey "Software\Microsoft\Windows\CurrentVersion\Uninstall\" + AppIdValue + "_is1"

[Setup]
AppId={#AppId}
AppName=RightKeyboard
AppVersion={#AppVersion}
AppPublisher=Colaboradores de RightKeyboard
DefaultDirName={localappdata}\RightKeyboard\app
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
ArchitecturesAllowed=x64compatible
CloseApplications=yes
CloseApplicationsFilter=RightKeyboard.exe
RestartApplications=no
UninstallDisplayIcon={app}\RightKeyboard.exe
OutputDir={#OutputDir}
OutputBaseFilename=RightKeyboard-{#AppVersion}-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\RightKeyboard\ico_input0002.ico

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "startup"; Description: "Iniciar RightKeyboard con Windows"; Flags: checkedonce; Check: IsFreshInstall

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
Root: HKCU; Subkey: "{#RunKey}"; ValueType: string; ValueName: "RightKeyboard"; ValueData: """{app}\RightKeyboard.exe"""; Tasks: startup; Check: ShouldCreateStartupEntry

[Icons]
Name: "{autoprograms}\RightKeyboard"; Filename: "{app}\RightKeyboard.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\RightKeyboard.exe"; Description: "Iniciar RightKeyboard"; Flags: nowait postinstall skipifsilent

[Code]
const
  EventModifyState = $0002;

var
  IsUpgrade: Boolean;
  DeleteUserData: Boolean;

function OpenEvent(DesiredAccess: LongWord; InheritHandle: Boolean;
  Name: string): THandle;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(EventHandle: THandle): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';

function IsFreshInstall: Boolean;
begin
  Result := not IsUpgrade;
end;

function IsStartupExternallyDisabled: Boolean;
var
  Approval: AnsiString;
begin
  Result := False;
  if RegQueryBinaryValue(HKCU, '{#StartupApprovedKey}', 'RightKeyboard', Approval) then
    if Length(Approval) > 0 then
      Result := Ord(Approval[1]) = 3;
end;

function ShouldCreateStartupEntry: Boolean;
begin
  Result := IsFreshInstall and WizardIsTaskSelected('startup') and
    not IsStartupExternallyDisabled;
end;

function InitializeSetup: Boolean;
begin
  IsUpgrade := RegKeyExists(HKCU, '{#UninstallKey}');
  Result := True;
end;

procedure InitializeWizard;
begin
  if IsUpgrade then
  begin
    WizardForm.Caption := 'Actualizar RightKeyboard';
    WizardForm.NextButton.Caption := '&Actualizar';
    WizardForm.WelcomeLabel1.Caption := 'Actualizar RightKeyboard';
    WizardForm.WelcomeLabel2.Caption :=
      'El asistente actualizará la instalación existente de RightKeyboard a la versión {#AppVersion}.' + #13#10 + #13#10 +
      'Se conservarán las preferencias y la configuración de inicio automático.';
  end;
end;

procedure SignalCloseEvent;
var
  EventHandle: THandle;
begin
  EventHandle := OpenEvent(EventModifyState, False, '{#CloseEvent}');
  if EventHandle <> 0 then
  begin
    SetEvent(EventHandle);
    CloseHandle(EventHandle);
  end;
end;

function WaitForApplicationToClose: Boolean;
var
  Attempts: Integer;
begin
  if not CheckForMutexes('{#AppMutex}') then
  begin
    Result := True;
    exit;
  end;

  SignalCloseEvent;
  for Attempts := 1 to 40 do
  begin
    Sleep(250);
    if not CheckForMutexes('{#AppMutex}') then
    begin
      Result := True;
      exit;
    end;
  end;

  Result := False;
end;

function PrepareToInstall(var NeedsRestart: Boolean): string;
begin
  if WaitForApplicationToClose then
    Result := ''
  else
    Result := 'No se pudo cerrar RightKeyboard de forma segura. Ciérrelo desde el icono del área de notificación y vuelva a intentarlo.';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and IsFreshInstall and
    not WizardIsTaskSelected('startup') then
    RegDeleteValue(HKCU, '{#RunKey}', 'RightKeyboard');
end;

function InitializeUninstall: Boolean;
begin
  Result := WaitForApplicationToClose;
  if not Result then
  begin
    MsgBox('No se pudo cerrar RightKeyboard de forma segura. Ciérrelo desde el icono del área de notificación y vuelva a ejecutar la desinstalación.',
      mbError, MB_OK);
    exit;
  end;

  if UninstallSilent then
    DeleteUserData := ExpandConstant('{param:BORRARDATOS|0}') = '1'
  else
    DeleteUserData := MsgBox(
      '¿Desea eliminar también las preferencias, exportaciones y respaldos de RightKeyboard?' + #13#10 + #13#10 +
      'Seleccione No para conservarlos.', mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, '{#RunKey}', 'RightKeyboard');
    if DeleteUserData then
      DelTree(ExpandConstant('{localappdata}\RightKeyboard'), True, True, True);
  end;
end;
