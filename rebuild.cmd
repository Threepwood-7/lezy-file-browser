@echo off
CALL "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
dotnet build LezyFileBrowserNet10.sln -c Release -p:Platform="Any CPU" --nologo
