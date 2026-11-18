#!/bin/sh
# Rebuild DigitalRain.scr with the C# compiler that ships with Windows and install it (run from WSL).
set -e
B=/mnt/c/Users/aning/AppData/Local/DigitalRain
mkdir -p "$B"
cp "$(dirname "$0")/DigitalRain.cs" "$B/"
cmd.exe /c "cd /d C:\Users\aning\AppData\Local\DigitalRain && C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /out:DigitalRain.scr DigitalRain.cs" 2>/dev/null | tr -d '\r'
rm -f "$B/DigitalRain.cs"
ls -la "$B/DigitalRain.scr"
