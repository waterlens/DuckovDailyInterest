@echo off

SET "CSPROJ_FILE=DailyInterest.csproj"
SET "DUCKOV_PATH=C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov\Duckov_Data"

ECHO Setting DuckovPath to: %DUCKOV_PATH%

REM Use PowerShell to replace the DuckovPath in the .csproj file
REM The pattern looks for <DuckovPath>...</DuckovPath>

powershell -Command "(Get-Content '%CSPROJ_FILE%') -replace '<DuckovPath>.*</DuckovPath>', '<DuckovPath>%DUCKOV_PATH%</DuckovPath>' | Set-Content '%CSPROJ_FILE%'"

IF %ERRORLEVEL% EQU 0 (
    ECHO Successfully patched DuckovPath in %CSPROJ_FILE%
) ELSE (
    ECHO Failed to patch DuckovPath in %CSPROJ_FILE%
    EXIT /B 1
)

