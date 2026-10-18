@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  GeoArea — збірка інсталятора для передачі іншим людям
rem
rem  Результат у папці dist\:
rem   - GeoArea-Setup-<версія>.exe   — інсталятор (Inno Setup): майстер
rem     установки українською, ярлик на робочому столі та в меню «Пуск»,
rem     деінсталятор у «Програмах і компонентах». Прав адміністратора
rem     не вимагає.
rem   - GeoArea-<версія>-portable.zip — той самий GeoArea.exe без
rem     інсталятора: розпакувати в будь-яку папку і запустити.
rem
rem  Потрібно на комп'ютері, де збираємо:
rem   - .NET 8 SDK (або новіший)
rem   - Inno Setup 6.3+ (https://jrsoftware.org/isdl.php) — лише для Setup.exe;
rem     portable-архів збереться і без нього.
rem ============================================================

set "ROOT=%~dp0"
set "PUBLISH_DIR=%ROOT%publish"
set "DIST_DIR=%ROOT%dist"
set "PROJECT=%ROOT%src\GeoArea.App\GeoArea.App.csproj"
set "ISS=%ROOT%installer\GeoArea.iss"
set "VERSION=1.0.0"

echo.
echo [1/4] Перевірка .NET SDK...
where dotnet >nul 2>nul
if errorlevel 1 (
    echo    ПОМИЛКА: dotnet не знайдено. Установіть .NET 8 SDK з https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
dotnet --version

echo.
echo [2/4] Збірка GeoArea.exe ^(Release, single-file, win-x64^)...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=none ^
    -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo    ПОМИЛКА збірки. Дивіться повідомлення вище.
    pause
    exit /b 1
)

if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

echo.
echo [3/4] Portable-архів...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference='Stop';" ^
  "$tmp = Join-Path $env:TEMP 'GeoArea-portable'; if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }; New-Item $tmp -ItemType Directory | Out-Null;" ^
  "Copy-Item '%PUBLISH_DIR%\GeoArea.exe' $tmp; Copy-Item '%ROOT%README.md' (Join-Path $tmp 'Інструкція.md'); Copy-Item '%ROOT%docs\sample-coordinates.txt' (Join-Path $tmp 'приклад-координат.txt');" ^
  "$zip = '%DIST_DIR%\GeoArea-%VERSION%-portable.zip'; if (Test-Path $zip) { Remove-Item $zip -Force };" ^
  "Compress-Archive -Path (Join-Path $tmp '*') -DestinationPath $zip -CompressionLevel Optimal; Remove-Item $tmp -Recurse -Force;" ^
  "Write-Host ('   Створено: ' + $zip)"
if errorlevel 1 echo    Увага: portable-архів не створено.

echo.
echo [4/4] Інсталятор ^(Inno Setup^)...
set "ISCC="
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not defined ISCC if exist "%LocalAppData%\Programs\Inno Setup 6\ISCC.exe" set "ISCC=%LocalAppData%\Programs\Inno Setup 6\ISCC.exe"
if not defined ISCC (
    for /f "delims=" %%I in ('where ISCC.exe 2^>nul') do if not defined ISCC set "ISCC=%%I"
)
if not defined ISCC (
    echo    Inno Setup не знайдено. Інсталятор пропущено.
    echo    Установіть Inno Setup 6 з https://jrsoftware.org/isdl.php і запустіть цей скрипт ще раз.
    echo    Поки що можна передавати portable-архів із папки dist\.
    echo.
    pause
    exit /b 0
)
echo    Компілятор: %ISCC%
"%ISCC%" /Qp "%ISS%"
if errorlevel 1 (
    echo    ПОМИЛКА компіляції інсталятора. Дивіться повідомлення вище.
    pause
    exit /b 1
)

echo.
echo Готово. Файли для передачі лежать у папці:
echo    %DIST_DIR%
dir /b "%DIST_DIR%"
echo.
pause
endlocal
