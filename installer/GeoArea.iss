; ============================================================
;  GeoArea — сценарій інсталятора для Inno Setup 6.3+
;  https://jrsoftware.org/isinfo.php (безкоштовний)
;
;  Збирається скриптом build-installer.cmd у корені репозиторію.
;  Очікує готовий publish\GeoArea.exe (single-file, self-contained),
;  тому користувачу не потрібен встановлений .NET.
; ============================================================

#define MyAppName "GeoArea"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Dressler Consulting"
#define MyAppExeName "GeoArea.exe"
#define MyAppDescription "Обчислення площі ділянки за координатами"
#define RepoRoot "..\"

[Setup]
; Унікальний ідентифікатор програми — НЕ змінювати між версіями,
; інакше оновлення встановиться поруч замість заміни.
AppId={{7E1C2B4A-9C3D-4F0E-8A6B-2D5F1C9E7A31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppComments={#MyAppDescription}
VersionInfoVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} — {#MyAppDescription}

; Установка без прав адміністратора (у папку користувача); якщо запущено
; від адміністратора — у Program Files. Користувач може обрати в діалозі.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableWelcomePage=no
AllowNoIcons=yes

; Тільки 64-бітна Windows 10/11
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

; Вигляд і вихідний файл
WizardStyle=modern
WizardSizePercent=110
SetupIconFile={#RepoRoot}src\GeoArea.App\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
OutputDir={#RepoRoot}dist
OutputBaseFilename=GeoArea-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Якщо програма відкрита під час оновлення — попросити закрити
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=auto

[Languages]
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"
Name: "english";   MessagesFile: "compiler:Default.isl"

[CustomMessages]
ukrainian.LaunchAfterInstall=Запустити {#MyAppName} після встановлення
ukrainian.CreateDesktopIconHint=Створити ярлик на робочому столі (рекомендовано)
ukrainian.SampleFile=Приклад файлу з координатами
ukrainian.Readme=Інструкція
english.LaunchAfterInstall=Launch {#MyAppName} after installation
english.CreateDesktopIconHint=Create a desktop shortcut (recommended)
english.SampleFile=Sample coordinates file
english.Readme=Readme

[Tasks]
; Ярлик на робочому столі — увімкнено за замовчуванням
Name: "desktopicon"; Description: "{cm:CreateDesktopIconHint}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#RepoRoot}publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#RepoRoot}README.md"; DestDir: "{app}"; DestName: "Інструкція.md"; Flags: ignoreversion
Source: "{#RepoRoot}docs\sample-coordinates.txt"; DestDir: "{app}\Приклади"; DestName: "приклад-координат.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"
Name: "{group}\{cm:SampleFile}"; Filename: "{app}\Приклади\приклад-координат.txt"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchAfterInstall}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Нічого зайвого не лишаємо; налаштувань програма на диск не пише.
Type: filesandordirs; Name: "{app}\Приклади"
