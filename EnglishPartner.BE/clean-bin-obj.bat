@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo Cleaning all .NET bin and obj folders...
echo ========================================================

cd /d "%~dp0"

set "COUNT=0"

for /d /r . %%d in (bin obj) do (
    set "DIRPATH=%%d"
    echo !DIRPATH! | findstr /i "\\node_modules\\ \\.git\\" >nul
    if errorlevel 1 (
        if exist "%%d" (
            echo Deleting: "%%d"
            rd /s /q "%%d" 2>nul
            set /a COUNT+=1
        )
    )
)

echo ========================================================
echo Hoan tat: Da xoa !COUNT! thu muc bin va obj.
echo ========================================================
pause
