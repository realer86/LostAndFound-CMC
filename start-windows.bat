@echo off
cd /d "%~dp0"
title Foundly Full Operations - Localhost
chcp 65001 >nul
where docker >nul 2>nul
if errorlevel 1 (
 echo Docker chua co trong PATH. Cai va mo Docker Desktop truoc.
 pause
 exit /b 1
)
echo ================================================
echo        FOUNDLY FULL OPERATIONS - LOCALHOST
echo ================================================
echo Chay Website User + Admin + API + PostgreSQL...
docker compose up --build -d
if errorlevel 1 (
 echo.
 echo Khoi dong that bai. Co the port 5173/5174/8080/5433 dang bi su dung.
 echo Hay dong project Docker cu roi chay lai file nay.
 pause
 exit /b 1
)
echo.
echo Website sinh vien: http://localhost:5173
echo Website quan tri : http://localhost:5174
echo API health       : http://localhost:8080/api/health
echo.
start http://localhost:5173
pause
