#define AppName "Fingertech Biometric API"
#define AppVersion "2.4.5"
#define AppPublisher "Fingertech"
#define AppURL "https://github.com/FingertechSuporte4"
#define AppExeName "BiometricService.exe"
#define ServiceName "FingertechBiometricAPI"
#define ServiceDisplayName "Fingertech Biometric API"
#define PublishDir "..\BiometricServiceAPI\bin\Release\net8.0\win-x64\publish"

[Setup]
AppId={{A3F2C1D4-8B5E-4F7A-9C2D-1E6B3A4F5D8C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\Fingertech\BiometricAPI
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=.\installer-output
OutputBaseFilename=Fingertech-BiometricAPI-Setup-v{#AppVersion}
SetupIconFile=.\icone-finger.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
MinVersion=10.0
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startservice"; Description: "Iniciar o servi�o ap�s a instala��o"; GroupDescription: "Configura��es adicionais:"; Flags: checked

[Files]
Source: "{#PublishDir}\{#AppExeName}";         DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\aspnetcorev2_inprocess.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\appsettings.json";       DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "{#PublishDir}\platform_keyset.json";   DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\webapp\*";               DestDir: "{app}\webapp"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"

[Run]
; Registra como servico do Windows
Filename: "{sys}\sc.exe"; Parameters: "create ""{#ServiceName}"" binPath= ""{app}\{#AppExeName} --windows-service"" start= auto DisplayName= ""{#ServiceDisplayName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Registrando servico do Windows..."

; Configura descricao do servico
Filename: "{sys}\sc.exe"; Parameters: "description ""{#ServiceName}"" ""API de captura biometrica Fingertech"""; Flags: runhidden waituntilterminated

; Abre porta 5000 no firewall
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""{#AppName}"" dir=in action=allow protocol=TCP localport=5000"; Flags: runhidden waituntilterminated; StatusMsg: "Configurando firewall..."

; Inicia o servico (somente se o usuario marcou a opcao)
Filename: "{sys}\sc.exe"; Parameters: "start ""{#ServiceName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Iniciando servico..."; Tasks: startservice

[UninstallRun]
; Para o servico antes de desinstalar
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#ServiceName}"""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "delete ""{#ServiceName}"""; Flags: runhidden waituntilterminated

; Remove regra do firewall
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#AppName}"""; Flags: runhidden waituntilterminated

[Code]
// Verifica se o SDK da NITGEN esta instalado
function NitgenSDKInstalled(): Boolean;
begin
  Result := FileExists('C:\Program Files (x86)\NITGEN\eNBSP SDK Professional\SDK\dotNET\NITGEN.SDK.NBioBSP.dll');
end;

// Verifica se o servico ja existe (atualizacao)
function ServiceExists(): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'query "{#ServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;

  if not NitgenSDKInstalled() then
  begin
    if MsgBox(
      'O SDK da NITGEN (eNBioBSP) nao foi encontrado nesta maquina.' + #13#10 + #13#10 +
      'A API requer o SDK da NITGEN para funcionar com o leitor biometrico.' + #13#10 +
      'Voce pode instalar o SDK depois e a API funcionara normalmente.' + #13#10 + #13#10 +
      'Deseja continuar a instalacao assim mesmo?',
      mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  // Se o servico ja existir (atualizacao), para antes de substituir os arquivos
  if CurStep = ssInstall then
  begin
    if ServiceExists() then
    begin
      Exec(ExpandConstant('{sys}\sc.exe'), 'stop "{#ServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Sleep(2000);
    end;
  end;
end;
