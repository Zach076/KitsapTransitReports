; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{DECC7B73-2988-42F5-B409-7FEFDFCF96DB}
AppName=Kitsap Transit Reports
AppVersion=0.5
;AppVerName=Kitsap Transit Reports 0.5
AppPublisher=WWU
DefaultDirName={pf}\Kitsap Transit Reports
DisableProgramGroupPage=yes
OutputBaseFilename=SetupKitsapTransitReports
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "KTReports\bin\Release\KTReports.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\EntityFramework.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\EntityFramework.SqlServer.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\EntityFramework.SqlServer.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\EntityFramework.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\KTReports.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Data.SQLite.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Data.SQLite.dll.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Data.SQLite.EF6.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Data.SQLite.Linq.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Data.SQLite.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\KTReports.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\x64\SQLite.Interop.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
Source: "KTReports\bin\Release\x86\SQLite.Interop.dll"; DestDir: "{app}\x86"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Windows.Controls.DataVisualization.Toolkit.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KTReports\bin\Release\System.Windows.Controls.DataVisualization.Toolkit.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "routes.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "ktdatabase.sqlite3"; DestDir: "{userappdata}"; Flags: onlyifdoesntexist;
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\Kitsap Transit Reports"; Filename: "{app}\KTReports.exe"
Name: "{commondesktop}\Kitsap Transit Reports"; Filename: "{app}\KTReports.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\KTReports.exe"; Description: "{cm:LaunchProgram,Kitsap Transit Reports}"; Flags: nowait postinstall skipifsilent

