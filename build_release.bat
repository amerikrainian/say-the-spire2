@echo off
setlocal

pushd "%~dp0" || exit /b 1

echo === Building mod (Release) ===
dotnet build -c Release
if errorlevel 1 goto :fail

echo === Building documentation ===
mdbook build docs_src
if errorlevel 1 goto :fail

echo === Adding docs to release zip ===
uv run python scripts\add_docs_to_release.py
if errorlevel 1 goto :fail

echo === Done ===
echo Release zip: SayTheSpire2.zip
echo Installer:   SayTheSpire2Installer.exe
popd
exit /b 0

:fail
set "exit_code=%errorlevel%"
popd
exit /b %exit_code%
