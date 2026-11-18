@echo off
setlocal
rem Digital Rain screensaver - installer. Created by aning with the help of Claude.
rem Copies DigitalRain.scr to your user folder and sets it as your screensaver (no admin rights needed).
rem Run with /quiet to skip opening Screen Saver Settings at the end.

set "SRC=%~dp0DigitalRain.scr"
set "DEST=%LOCALAPPDATA%\DigitalRain"
if not exist "%SRC%" (
  echo DigitalRain.scr was not found next to Install.bat.
  echo Extract the whole zip first, then run Install.bat from the extracted folder.
  pause
  exit /b 1
)
if not exist "%DEST%" mkdir "%DEST%"
copy /y "%SRC%" "%DEST%\DigitalRain.scr" >nul
if errorlevel 1 (
  echo Could not copy the screensaver to %DEST%.
  pause
  exit /b 1
)
rem Remove the "downloaded from the internet" mark so Windows runs it without asking.
powershell -NoProfile -Command "Unblock-File -LiteralPath '%DEST%\DigitalRain.scr'" >nul 2>&1

rem Screensaver paths with spaces can fail; use the short (8.3) path when available.
for %%I in ("%DEST%\DigitalRain.scr") do set "SCR=%%~sI"

reg add "HKCU\Control Panel\Desktop" /v SCRNSAVE.EXE /t REG_SZ /d "%SCR%" /f >nul
reg add "HKCU\Control Panel\Desktop" /v ScreenSaveActive /t REG_SZ /d 1 /f >nul
rem Keep the current delay if one is set, otherwise start after 5 minutes.
set "TIMEOUT="
for /f "tokens=3" %%T in ('reg query "HKCU\Control Panel\Desktop" /v ScreenSaveTimeOut 2^>nul ^| find "ScreenSaveTimeOut"') do set "TIMEOUT=%%T"
if not defined TIMEOUT reg add "HKCU\Control Panel\Desktop" /v ScreenSaveTimeOut /t REG_SZ /d 300 /f >nul
rem Tell Windows to reload the settings.
rundll32.exe user32.dll,UpdatePerUserSystemParameters 1, True

echo.
echo Digital Rain is installed and set as your screensaver.
echo Installed to: %DEST%
if /i "%~1"=="/quiet" exit /b 0
echo Opening Screen Saver Settings so you can preview it and change the delay...
start "" control.exe desk.cpl,,@screensaver
echo.
pause
