@echo off
REM Doppelklick-Starter fuer die Multikeys-Einrichtung.
REM Ruft das PowerShell-Skript ohne Aenderung der Ausfuehrungsrichtlinie auf.
title Multikeys - Einrichtung
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Multikeys.ps1"
echo.
pause
