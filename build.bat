@echo off
rem MonitorCover - NAPDA. Copyright (c) 2026 Hononoo (NAPDA). MIT License.
rem Builds the program with the C# compiler that ships with Windows (.NET Framework 4.x).
rem The Korean output file name lives in release.rsp (Unicode), so this file stays plain ASCII.
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
rem The maker's business card is built into the exe (assets\namecard.png).
set "FLAGS=/nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /resource:assets\namecard.png,namecard.png"

rem Pass 1 builds a temporary exe so it can draw its own icon; pass 2 embeds that icon.
"%CSC%" %FLAGS% /out:MonitorCover.tmp.exe MonitorCover.cs || exit /b 1
start "" /wait MonitorCover.tmp.exe --write-icon app.ico || exit /b 1
del MonitorCover.tmp.exe
"%CSC%" %FLAGS% /win32icon:app.ico @release.rsp MonitorCover.cs || exit /b 1
echo Build OK
