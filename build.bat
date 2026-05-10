@echo off
setlocal
cd /d "%~dp0"

REM Minecraft Server Manager — schneller Build (Debug oder Release)
REM Nutzung: build.bat [Debug|Release]

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Release

echo [MSM] Baue Konfiguration: %CONFIG%
dotnet build "src\MinecraftServerManager\MinecraftServerManager.csproj" -c "%CONFIG%"
set EXITCODE=%ERRORLEVEL%
if %EXITCODE% neq 0 (
  echo [MSM] Build fehlgeschlagen ^(Exit %EXITCODE%^).
  exit /b %EXITCODE%
)

echo [MSM] Build OK — Ausgabe unter src\MinecraftServerManager\bin\%CONFIG%\
exit /b 0
