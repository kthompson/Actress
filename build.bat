@echo off
if not exist ".\bin" mkdir .\bin
if not exist ".\bin\NuGet.exe" (
  powershell -Command "Invoke-WebRequest http://www.nuget.org/NuGet.exe -OutFile .\bin\NuGet.exe"
)
.\bin\NuGet.exe pack .\Actress\Actress.csproj -Build
