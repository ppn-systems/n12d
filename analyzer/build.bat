@echo off
echo [Nalix.Analyzer] Building .exe...
pip install pyinstaller -q
pyinstaller --onefile ^
            --add-data "templates;templates" ^
            --name "Nalix.Analyzer" ^
            cli.py
echo [Nalix.Analyzer] Build complete: dist\Nalix.Analyzer.exe
pause
