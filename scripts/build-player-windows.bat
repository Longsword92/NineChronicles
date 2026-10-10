@echo off
setlocal

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
set "PROJECT_PATH=%~dp0..\nekoyume"
set "OUTPUT_ROOT=D:\Game\9c"
set "BUILD_NAME=NineChronicles"

set "BUILD_DIR=%PROJECT_PATH%\..\build\StandaloneWindows"
set "OUTPUT_DIR=%OUTPUT_ROOT%\%BUILD_NAME%"
set "LOG_FILE=%PROJECT_PATH%\..\build\unity-build-windows.log"

if not exist "%UNITY_EXE%" (
    echo [ERROR] Unity executable not found: "%UNITY_EXE%"
    exit /b 1
)

if not exist "%PROJECT_PATH%\Assets" (
    echo [ERROR] Unity project path is invalid: "%PROJECT_PATH%"
    exit /b 1
)

if not exist "%OUTPUT_ROOT%" (
    mkdir "%OUTPUT_ROOT%"
    if errorlevel 1 (
        echo [ERROR] Failed to create output root: "%OUTPUT_ROOT%"
        exit /b 1
    )
)

if not exist "%PROJECT_PATH%\..\build" (
    mkdir "%PROJECT_PATH%\..\build"
    if errorlevel 1 (
        echo [ERROR] Failed to create build folder next to project.
        exit /b 1
    )
)

echo [INFO] Building Windows player...
"%UNITY_EXE%" ^
  -quit ^
  -batchmode ^
  -nographics ^
  -projectPath "%PROJECT_PATH%" ^
  -buildTarget StandaloneWindows64 ^
  -executeMethod NekoyumeEditor.Builder.BuildStandaloneWindows ^
  -playerName "%BUILD_NAME%" ^
  -logFile "%LOG_FILE%"

if errorlevel 1 (
    echo [ERROR] Unity build failed. Check log: "%LOG_FILE%"
    exit /b 1
)

if not exist "%BUILD_DIR%\%BUILD_NAME%.exe" (
    echo [ERROR] Build output not found: "%BUILD_DIR%\%BUILD_NAME%.exe"
    echo [ERROR] Check log: "%LOG_FILE%"
    exit /b 1
)

if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
    if errorlevel 1 (
        echo [ERROR] Failed to clean output directory: "%OUTPUT_DIR%"
        exit /b 1
    )
)

mkdir "%OUTPUT_DIR%"
if errorlevel 1 (
    echo [ERROR] Failed to create output directory: "%OUTPUT_DIR%"
    exit /b 1
)

robocopy "%BUILD_DIR%" "%OUTPUT_DIR%" /MIR >nul
if errorlevel 8 (
    echo [ERROR] Failed to copy build output to: "%OUTPUT_DIR%"
    exit /b 1
)

echo [INFO] Build completed successfully.
echo [INFO] Output: "%OUTPUT_DIR%\%BUILD_NAME%.exe"
echo [INFO] Unity log: "%LOG_FILE%"
exit /b 0
