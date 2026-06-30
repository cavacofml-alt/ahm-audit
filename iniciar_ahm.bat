@echo off
title AHM Audit
set DATABASE_URL=postgresql://ahm:ahm123@localhost:5432/ahmaudit
cd /d C:\AHM.Audit
echo A iniciar AHM Audit...
echo Acede em: http://localhost:5000
echo Para parar pressiona Ctrl+C
echo.
dotnet run
pause
