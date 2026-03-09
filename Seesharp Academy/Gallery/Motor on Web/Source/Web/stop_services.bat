@echo off
chcp 65001 >nul
title 锐视公学电机状态监测系统 - 停止服务

echo ============================================
echo    锐视公学电机状态监测系统
echo    停止所有服务
echo ============================================
echo.

echo 正在关闭服务...
echo.

REM 关闭Node.js Web服务器
echo [1/3] 关闭Node.js Web服务器...
taskkill /FI "WINDOWTITLE eq NodeJS-Web-Server*" /F >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo     OK - NodeJS Web Server已关闭
) else (
    echo     -   NodeJS Web Server未运行
)

REM 关闭Python数据模拟器
echo [2/3] 关闭Python数据模拟器...
taskkill /FI "WINDOWTITLE eq Python-Simulator*" /F >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo     OK - Python Simulator已关闭
) else (
    echo     -   Python Simulator未运行
)

REM 关闭Redis服务器
echo [3/3] 关闭Redis服务...
taskkill /FI "WINDOWTITLE eq Redis-Server*" /F >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo     OK - Redis Server已关闭
) else (
    echo     -   Redis Server未运行
)

REM 也尝试关闭redis-server进程
taskkill /IM redis-server.exe /F >nul 2>nul

echo.
echo ============================================
echo All services stopped!
echo ============================================
echo.
pause
