@echo off
title Game Mode Enable
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

echo [1/5] Saving process list and stopping third-party apps...
:: This PowerShell command safely ignores Session 0 (critical system services) and core Windows processes.
:: It saves the exact executable paths to log.txt, then stops them.
powershell -NoProfile -Command "$exclusions = @('smss','csrss','wininit','services','lsass','winlogon','svchost','fontdrvhost','dwm','spoolsv','explorer','taskmgr','cmd','conhost','powershell','searchindexer','sihost','taskhostw','RuntimeBroker','System','Registry','SearchUI','ShellExperienceHost','SecurityHealthService','GameLauncher'); $procs = Get-Process | Where-Object { $_.Name -notin $exclusions -and $_.Path -ne $null -and $_.SessionId -ne 0 }; $procs | Select-Object -ExpandProperty Path | Sort-Object -Unique | Out-File -FilePath 'log.txt' -Encoding UTF8; foreach ($p in $procs) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }"

echo [2/5] Disabling Internet connection...
powershell -NoProfile -Command "Disable-NetAdapter -Physical -Confirm:$false -ErrorAction SilentlyContinue"

echo [3/5] Stopping Windows Explorer...
taskkill /f /im explorer.exe >nul 2>&1

echo [4/5] Starting Game Launcher...
:: Replace "GameLauncher.exe" with the actual name of your executable
start "" "GameLauncher.exe"

echo [5/5] Game Mode Active!
exit