@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "PLAYER=%~dp0Builds\WindowsVisualFixProbe-c780b39\AzureSword.exe"
if not exist "%PLAYER%" set "PLAYER=%~dp0Builds\WindowsRelease\AzureSword.exe"

if not exist "%PLAYER%" (
    echo [Qinglan Demo] No local Windows Player was found.
    echo Build the project first, then run this launcher again.
    pause
    exit /b 1
)

set "LOG_DIR=%~dp0TestResults\QinglanDemo\ManualLaunch"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
set "LOG_FILE=%LOG_DIR%\player.log"
for %%I in ("%PLAYER%") do set "PLAYER_DIR=%%~dpI"

start "Qinglan Demo" /D "%PLAYER_DIR%" "%PLAYER%" ^
    -screen-fullscreen 0 ^
    -screen-width 1600 ^
    -screen-height 900 ^
    -logFile "%LOG_FILE%"

endlocal
