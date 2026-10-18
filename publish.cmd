@echo off
chcp 65001 >nul
setlocal

rem ============================================================
rem  GeoArea — збірка одним exe-файлом і створення ярлика
rem
rem  Що робить цей скрипт:
rem   1. Збирає GeoArea.exe (self-contained, single-file, win-x64)
rem      у папку publish\ поруч із цим файлом.
rem   2. Створює ярлик "GeoArea" на робочому столі поточного
rem      користувача, який запускає цей exe.
rem
rem  Потрібен лише .NET 8 SDK (або новіший) на комп'ютері, де збираємо.
rem  На комп'ютері користувача нічого встановлювати не треба.
rem ============================================================

set "ROOT=%~dp0"
set "PUBLISH_DIR=%ROOT%publish"
set "PROJECT=%ROOT%src\GeoArea.App\GeoArea.App.csproj"

echo.
echo [1/3] Перевірка .NET SDK...
where dotnet >nul 2>nul
if errorlevel 1 (
    echo    ПОМИЛКА: dotnet не знайдено. Установіть .NET 8 SDK з https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
dotnet --version

echo.
echo [2/3] Збірка GeoArea.exe ^(це може тривати 1-2 хвилини^)...
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

echo.
echo [3/3] Створення ярлика на робочому столі...
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%scripts\create-shortcut.ps1" -ExePath "%PUBLISH_DIR%\GeoArea.exe"
if errorlevel 1 (
    echo    Ярлик не створено. Exe лежить тут: %PUBLISH_DIR%\GeoArea.exe
    pause
    exit /b 1
)

echo.
echo Готово. Програма: %PUBLISH_DIR%\GeoArea.exe
echo Ярлик "GeoArea" створено на робочому столі.
echo.
pause
endlocal
