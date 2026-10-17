@echo off
rem Build autoclicker.exe (Python launcher). Output goes to the project root.
rem The exe starts main.py in the same folder with pythonw, so Python is required.
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "ROOT=%~dp0.."

if not exist "%CSC%" (
    echo csc.exe not found: %CSC%
    exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu ^
    /win32icon:"%ROOT%\app_icon.ico" ^
    /reference:System.Windows.Forms.dll ^
    /out:"%ROOT%\autoclicker.exe" ^
    "%~dp0AutoClicker.cs"

if errorlevel 1 (
    echo Build failed.
    exit /b 1
)
echo Build succeeded: %ROOT%\autoclicker.exe
endlocal
