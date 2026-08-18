@echo off
title Restore Normal Mode
:: Request Administrative Privileges
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo Requesting Administrative Privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
    :: Set working directory to the folder where this batch file is located
    pushd "%CD%"
    CD /D "%~dp0"

echo [1/4] Starting Windows Explorer...
start explorer.exe

echo [2/4] Enabling Internet connection...
powershell -NoProfile -Command "Enable-NetAdapter -Physical -Confirm:$false -ErrorAction SilentlyContinue"

echo [3/4] Restoring applications from log...
:: Reads log.txt, starts each program minimized, then deletes the log file
powershell -NoProfile -Command "if (Test-Path 'log.txt') { $paths = Get-Content 'log.txt'; foreach ($path in $paths) { if (Test-Path $path) { Start-Process -FilePath $path -WindowStyle Minimized -ErrorAction SilentlyContinue } }; Remove-Item 'log.txt' -Force }"

echo [4/4] Normal Mode Restored!
timeout /t 3
exit