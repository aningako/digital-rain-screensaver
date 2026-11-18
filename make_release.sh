#!/bin/sh
# Build DigitalRain.scr with the Windows C# compiler and package dist/DigitalRain-<version>.zip (run from WSL).
set -e
cd "$(dirname "$0")"
VER=$(grep -o 'AssemblyFileVersion("[0-9.]*' DigitalRain.cs | grep -o '[0-9]*\.[0-9]*\.[0-9]*' | head -1)
W=/mnt/c/Users/$(cmd.exe /c "echo %USERNAME%" 2>/dev/null | tr -d '\r')/AppData/Local/Temp/digitalrain-build
WIN=$(wslpath -w "$W")
rm -rf "$W"; mkdir -p "$W"; cp DigitalRain.cs "$W/"
cmd.exe /c "cd /d $WIN && C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /out:DigitalRain.scr DigitalRain.cs" 2>/dev/null | tr -d '\r'
test -f "$W/DigitalRain.scr"
mkdir -p dist
python3 - "$W/DigitalRain.scr" "dist/DigitalRain-$VER.zip" <<'PY'
import sys, zipfile
scr, out = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED) as z:
    z.write(scr, 'DigitalRain/DigitalRain.scr')
    for f in ('package/Install.bat', 'package/Uninstall.bat', 'package/README.txt', 'LICENSE', 'DigitalRain.cs'):
        z.write(f, 'DigitalRain/' + f.split('/')[-1] + ('.txt' if f == 'LICENSE' else ''))
print(out)
PY
rm -rf "$W"
