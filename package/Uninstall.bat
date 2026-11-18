@echo off
setlocal
rem Digital Rain screensaver - uninstaller. Created by aning with the help of Claude.
set "DEST=%LOCALAPPDATA%\DigitalRain"

rem Clear the screensaver setting only if Digital Rain is the current screensaver.
set "CUR="
for /f "tokens=2,*" %%A in ('reg query "HKCU\Control Panel\Desktop" /v SCRNSAVE.EXE 2^>nul ^| find "SCRNSAVE.EXE"') do set "CUR=%%B"
echo %CUR% | find /i "DigitalRain" >nul
if not errorlevel 1 (
  reg delete "HKCU\Control Panel\Desktop" /v SCRNSAVE.EXE /f >nul
  rundll32.exe user32.dll,UpdatePerUserSystemParameters 1, True
  echo Screensaver setting cleared.
)
if exist "%DEST%" rmdir /s /q "%DEST%"
echo Digital Rain has been removed.
if /i "%~1"=="/quiet" exit /b 0
pause
