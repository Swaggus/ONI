@echo off
setlocal

echo === DupeTimeline build ===
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: .NET SDK not found.
    echo.
    echo Install the .NET SDK ^(any 6.0 or newer^) from:
    echo   https://dotnet.microsoft.com/download
    echo.
    echo Then double-click this script again.
    echo.
    pause
    exit /b 1
)

cd /d "%~dp0DupeTimeline"
if errorlevel 1 (
    echo ERROR: Could not find DupeTimeline subfolder next to this script.
    pause
    exit /b 1
)

dotnet build -c Release
if errorlevel 1 (
    echo.
    echo Build failed. See errors above.
    echo.
    echo Most common cause: ONI install not in a standard Steam path.
    echo Re-run with the path set explicitly, e.g.:
    echo   dotnet build -c Release -p:GameFolder="D:\Games\OxygenNotIncluded\OxygenNotIncluded_Data\Managed"
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo  Done. Mod installed to:
echo    %%USERPROFILE%%\Documents\Klei\OxygenNotIncluded\mods\Local\DupeTimeline
echo.
echo  Next:
echo    1. Restart Oxygen Not Included if it's running
echo    2. Main Menu - Mods - enable "Dupe Activity Timeline"
echo    3. Restart again when prompted
echo ============================================================
pause
