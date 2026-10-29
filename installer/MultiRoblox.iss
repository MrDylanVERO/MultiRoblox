#define MyAppName "MultiRoblox"
#define MyAppVersion "3.2.4"
#define MyAppPublisher "MultiRoblox"
#define MyAppExeName "MultiRoblox.exe"
#define RootDir SourcePath + ".."

[Setup]
AppId={{B88B02FC-2962-47F1-B9DF-D0A93211E301}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\MultiRoblox
DefaultGroupName=MultiRoblox
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#RootDir}\dist
OutputBaseFilename=MultiRoblox-Installer-v3.2.4
SetupIconFile={#RootDir}\assets\MultiRoblox.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x86compatible x64compatible
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
italian.DesktopIcon=Crea un collegamento sul desktop
english.DesktopIcon=Create a desktop shortcut
german.DesktopIcon=Desktop-Verknüpfung erstellen
french.DesktopIcon=Créer un raccourci sur le bureau
spanish.DesktopIcon=Crear un acceso directo en el escritorio
italian.ExtraLinks=Collegamenti aggiuntivi:
english.ExtraLinks=Additional shortcuts:
german.ExtraLinks=Zusätzliche Verknüpfungen:
french.ExtraLinks=Raccourcis supplémentaires :
spanish.ExtraLinks=Accesos directos adicionales:

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; GroupDescription: "{cm:ExtraLinks}"; Flags: unchecked

[Files]
Source: "{#RootDir}\dist\MultiRoblox-v3.2.4.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion

[Icons]
Name: "{group}\MultiRoblox"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\MultiRoblox"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Avvia MultiRoblox"; Flags: nowait postinstall skipifsilent
