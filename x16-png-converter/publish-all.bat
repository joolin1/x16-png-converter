@echo off
setlocal

set CONFIG=Release
set PROJECT=x16-png-converter.csproj

REM Windows x64
dotnet publish %PROJECT% -c %CONFIG% -r win-x64 --self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o publish\win-x64

REM macOS x64
dotnet publish %PROJECT% -c %CONFIG% -r osx-x64 --self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o publish\osx-x64

REM Linux x64
dotnet publish %PROJECT% -c %CONFIG% -r linux-x64 --self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o publish\linux-x64

echo Done!
pause
