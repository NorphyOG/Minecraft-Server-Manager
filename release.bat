@echo off
setlocal
cd /d "%~dp0"

REM ZIP des Publish-Ordners nach dist\ (Versionsnummer aus csproj / Datum im Dateinamen)

echo [MSM] Publish Release ^(win-x64, framework-dependent^) …
dotnet publish "src\MinecraftServerManager\MinecraftServerManager.csproj" ^
  -c Release ^
  -o "publish\out" ^
  -r win-x64 ^
  --self-contained false

if errorlevel 1 (
  echo [MSM] publish fehlgeschlagen.
  exit /b 1
)

for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "(Select-Xml -Path 'src\MinecraftServerManager\MinecraftServerManager.csproj' -XPath '//Version').Node.InnerText"`) do set VER=%%V
if "%VER%"=="" set VER=1.0.0

for /f "usebackq delims=" %%D in (`powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"`) do set STAMP=%%D

if not exist "dist" mkdir dist

set ZIP=dist\MinecraftServerManager-%VER%-%STAMP%.zip
echo [MSM] Erzeuge ZIP: %ZIP%

powershell -NoProfile -Command "Compress-Archive -Path 'publish\out\*' -DestinationPath '%ZIP%' -Force"

if errorlevel 1 (
  echo [MSM] ZIP fehlgeschlagen.
  exit /b 1
)

echo [MSM] Fertig. Artefakt: %ZIP%
echo [MSM] Hinweis: publish\ und dist\ stehen in .gitignore.
exit /b 0
