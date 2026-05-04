@echo off
REM ─────────────────────────────────────────────────────────────────────
REM   Giwu HRMS Installer (double-click launcher)
REM
REM   Hand this folder (with GiwuHRMS.cer + the .msixbundle + these two
REM   scripts) to end users. They double-click this .cmd, accept the
REM   UAC prompt, and the GUI installer takes care of:
REM
REM     1. Importing the cert into LocalMachine\TrustedPeople
REM     2. Importing the cert into LocalMachine\Root (Trusted Root CAs)
REM     3. Installing the MSIX bundle
REM ─────────────────────────────────────────────────────────────────────

setlocal
set "SCRIPT_DIR=%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%install-msix.ps1" %*
exit /b %ERRORLEVEL%
