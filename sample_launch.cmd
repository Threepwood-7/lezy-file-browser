@ECHO OFF
:: ============================================================
:: sample_launch.cmd — quick-start launcher for LezyFileBrowser
::
:: Points the browser at your Downloads folder and moves
:: accepted items to a sibling "Downloads_ok" folder.
::
:: Edit the two SET lines below to suit your setup, then
:: just double-click this file (or run it from a terminal).
:: ============================================================

:: --- Source: the folder you want to browse ---
SET X_INPUT_DIR=%USERPROFILE%\Downloads

:: --- Destination: where accepted items land after you press Space ---
SET X_OK_DIR=%USERPROFILE%\Downloads_ok

:: --- Hide files smaller than this (MB). 0 = show everything ---
SET X_MIN_FILE_SIZE_MB=99

:: --- Sort order: 1=newest first  3=name A-Z  7=random ---
SET X_SORT_DIR=1

:: -------------------------------------------------------
:: Launch the exe that lives next to this script.
:: Adjust the path if you installed to a different folder.
:: -------------------------------------------------------
SET EXE=%~dp0LezyFileBrowserNet10.exe

IF NOT EXIST "%EXE%" (
    ECHO ERROR: could not find %EXE%
    ECHO Make sure this .cmd sits in the same folder as LezyFileBrowserNet10.exe
    PAUSE
    EXIT /B 1
)

START "" "%EXE%"
