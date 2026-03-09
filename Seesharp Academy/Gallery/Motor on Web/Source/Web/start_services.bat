@echo off
chcp 65001 >nul
title 锐视公学电机状态监测系统 - 服务启动器

echo ============================================
echo    锐视公学电机状态监测系统
echo    启动所有必要服务
echo ============================================
echo.

REM 检查Node.js是否已安装
where node >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 未找到Node.js！
    echo 请先安装Node.js: https://nodejs.org/
    pause
    exit /b 1
)

REM 检查Python是否已安装
where python >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [错误] 未找到Python！
    echo 请先安装Python: https://www.python.org/
    pause
    exit /b 1
)

REM 检查Node.js依赖是否已安装
if not exist "node_modules" (
    echo [注意] Node.js依赖未安装，正在安装...
    call npm install
    if %ERRORLEVEL% NEQ 0 (
        echo [错误] Node.js依赖安装失败！
        pause
        exit /b 1
    )
    echo [完成] Node.js依赖安装成功！
    echo.
)

REM 检查Python依赖是否已安装
echo [检查] Python依赖...
python -c "import redis" 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [注意] Python依赖未安装，正在安装...
    echo   - 安装 redis numpy scipy...
    pip install redis numpy scipy
    if %ERRORLEVEL% NEQ 0 (
        echo [错误] Python依赖安装失败！
        echo 请手动运行: pip install redis numpy scipy
        pause
        exit /b 1
    )
    echo [完成] Python依赖安装成功！
    echo.
) else (
    python -c "import numpy" 2>nul
    if %ERRORLEVEL% NEQ 0 (
        echo [注意] 缺少numpy模块，正在安装...
        pip install numpy scipy
        echo [完成] numpy和scipy安装成功！
        echo.
    )
)

REM 检查Redis是否已安装
where redis-server >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [警告] 未找到Redis服务！
    echo.
    echo 您可以选择：
    echo   1. 安装Redis后重新运行
    echo   2. 使用已有的Redis服务（如果已运行）
    echo   3. 跳过Redis，仅启动Web服务（将无法显示数据）
    echo.
    set /p choice="请选择 (1/2/3): "
    if "%choice%"=="1" (
        echo 请先安装Redis：
        echo   - Windows: 下载 Redis for Windows 或使用 choco install redis-64
        echo   - 或访问: https://github.com/microsoftarchive/redis/releases
        pause
        exit /b 1
    )
    if "%choice%"=="3" (
        echo 跳过Redis启动...
        goto SKIP_REDIS
    )
)

echo [1/3] 启动Redis服务...
where redis-server >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    REM 检查端口6379是否已被占用
    netstat -ano | findstr ":6379" | findstr "LISTENING" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [注意] 端口6379已被占用，Redis可能已在运行
        echo [跳过] Redis服务启动...
    ) else (
        start "Redis-Server" cmd /k "echo Redis Service Running... && redis-server && pause"
        REM 等待Redis启动
        timeout /t 3 /nobreak >nul
    )
) else (
    echo [跳过] Redis未安装，跳过Redis启动...
)

:SKIP_REDIS
echo [2/3] 启动Python数据模拟器...
cd /d "%~dp0Test"
start "Python-Simulator" cmd /k "echo Python Data Simulator Running... && python test_motor_data.py && pause"
cd /d "%~dp0"

REM 等待Python启动
timeout /t 2 /nobreak >nul

echo [3/3] 启动Node.js Web服务器...
start "NodeJS-Web-Server" cmd /k "echo Web Server Running on port 3000... && echo. && npm start && pause"

echo.
echo ============================================
echo All services started!
echo.
echo Service Windows:
echo   - Redis-Server:     Redis Database Service
echo   - Python-Simulator: Motor Data Simulator
echo   - NodeJS-Web-Server:Web Server
echo.
echo Open browser and visit:
echo   http://localhost:3000/motor_monitor.html
echo.
echo Press any key to close this window (services keep running)...
echo ============================================
pause >nul
