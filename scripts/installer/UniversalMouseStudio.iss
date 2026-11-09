; Estrutura de instalador opcional. Requer Inno Setup 6 instalado separadamente.
; Execute scripts/publish.ps1 primeiro. Sem drivers, elevação ou início automático.
[Setup]
AppId={{7DA9E1CB-231A-4EC7-9D37-63875B41B3A7}
AppName=Universal Mouse Studio
AppVersion=0.1.2
DefaultDirName={localappdata}\Programs\UniversalMouseStudio
DefaultGroupName=Universal Mouse Studio
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\..\artifacts
OutputBaseFilename=UniversalMouseStudio-Setup-x64
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\UniversalMouseStudio.exe
[Files]
Source: "..\..\artifacts\portable-win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Universal Mouse Studio"; Filename: "{app}\UniversalMouseStudio.exe"
[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    if MsgBox('Remover também os perfis locais do Universal Mouse Studio? Esta ação não restaura ajustes globais do Windows.', mbConfirmation, MB_YESNO) = IDYES then
      DelTree(ExpandConstant('{localappdata}\UniversalMouseStudio'), True, True, True);
end;
