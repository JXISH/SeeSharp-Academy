# 锐视公学电机状态监测系统

## 系统概述

本系统用于大屏幕展示电机的工作状态，通过本机Redis服务接收实时数据，并以可视化的方式展示3D模型、时域信号、频谱和频率数据。

## 系统架构

- **前端**: HTML + Three.js + Chart.js
- **后端**: Node.js + Express + Redis客户端
- **数据源**: Redis数据库（通过Python测试程序模拟）

## 功能特性

### 前端显示
- ✨ **3D模型展示**: 中部显示电机3D模型（船桨模型.glb），支持鼠标交互旋转缩放，无操作时自动旋转
- 📊 **时域信号图**: 左上显示实时时域波形
- 📈 **频谱图**: 左中显示信号频谱
- 📉 **频率历史图**: 左下显示即时频率历史（Strip Chart）
- 📋 **数据表格**: 右侧显示即时频率及幅度数据
- 🌌 **星空背景**: 暗夜星空深蓝色基调，科技感装饰

### 数据更新
- 数据从Redis实时读取
- 最小更新间隔：500ms
- 自动处理数据刷新竞争

## 安装步骤

### 1. 安装Redis

**Windows:**
```bash
# 下载Redis for Windows
# 或使用Chocolatey安装
choco install redis-64
```

**Linux/Mac:**
```bash
# 使用包管理器安装
sudo apt-get install redis-server  # Ubuntu/Debian
brew install redis                  # macOS
```

启动Redis服务：
```bash
redis-server
```

### 2. 安装Node.js依赖

```bash
cd "d:\Git\Github\SeeSharp-Academy\Seesharp Academy\Gallery\Motor on Web\Source\Web"
npm install
```

### 3. 安装Python依赖

```bash
pip install redis numpy scipy
```

## 使用方法

### 🚀 推荐方式：一键启动（Windows）

**双击运行 `start_services.bat`**

该批处理文件会自动：
1. ✓ 检查Redis、Node.js和Python是否已安装
2. ✓ 启动Redis服务
3. ✓ 启动Python数据模拟器
4. ✓ 启动Node.js Web服务器
5. ✓ 在浏览器中打开监测界面

**停止所有服务：双击运行 `stop_services.bat`**

### 方式一：手动启动（完整测试模式）

1. **启动Redis服务**
   ```bash
   redis-server
   ```

2. **启动Python数据模拟器**（在新终端）
   ```bash
   cd Test
   python test_motor_data.py
   ```

3. **启动Node.js后端服务器**（在新终端）
   ```bash
   npm start
   ```

4. **打开浏览器访问**
   ```
   http://localhost:3000/motor_monitor.html
   ```

### 方式二：仅前端测试（使用真实Redis数据）

如果已有真实的Redis数据源：

1. **启动Redis服务**
   ```bash
   redis-server
   ```

2. **启动Node.js后端服务器**
   ```bash
   npm start
   ```

3. **打开浏览器**
   ```
   http://localhost:3000/motor_monitor.html
   ```

### 方式三：仅HTML预览（无数据）

直接在浏览器中打开 `motor_monitor.html` 文件，可以查看界面布局和3D模型，但不会有实时数据更新。

## Redis数据格式

系统从Redis读取以下键值：

| 键名 | 数据类型 | 说明 |
|------|---------|------|
| `SeesharpMotor:TimeWaveform` | 逗号分隔浮点数 | 时域信号数据 |
| `SeesharpMotor:Waveform_dT` | 浮点数 | 时域信号采样间隔(s) |
| `SeesharpMotor:Spectrum` | 逗号分隔浮点数 | 信号频谱数据 |
| `SeesharpMotor:Spectrum_dF` | 浮点数 | 频谱采样间隔(Hz) |
| `SeesharpMotor:Frequencies` | 逗号分隔浮点数 | 即时频率列表 |
| `SeesharpMotor:Amplitudes` | 逗号分隔浮点数 | 即时频率幅度列表 |

## Python测试程序说明

`Test/test_motor_data.py` 程序模拟电机运行状态：

- **基础频率**: 50Hz附近波动
- **谐波成分**: 包含2-5次谐波
- **噪声**: 添加高斯噪声模拟真实环境
- **更新频率**: 500ms
- **采样率**: 10kHz

## 文件结构

```
Web/
├── motor_monitor.html      # 前端监测页面
├── server.js               # Node.js后端服务器
├── package.json            # Node.js依赖配置
├── README.md              # 本说明文件
├── 船桨模型.glb           # 3D模型文件
└── Test/
    └── test_motor_data.py # Python数据模拟程序
```

## 技术栈

- **前端框架**: Three.js (3D渲染), Chart.js (图表)
- **后端框架**: Node.js + Express
- **数据存储**: Redis
- **数据处理**: Python + NumPy + SciPy

## 故障排除

### 问题1: Redis连接失败
- 检查Redis服务是否运行：`redis-cli ping`
- 确认Redis端口6379未被占用
- 如果看到错误 `bind: No such file or directory` 或 `Address already in use`：
  - 这表示端口6379已被占用，Redis可能已在运行
  - 解决方法1: 使用 `stop_services.bat` 停止现有Redis实例
  - 解决方法2: 在任务管理器中结束 `redis-server.exe` 进程
  - 解决方法3: 如果Redis已正常运行，可以直接使用（start_services.bat会自动检测并跳过启动）

### 问题2: 3D模型无法加载
- 确认 `船桨模型.glb` 文件在正确位置
- 检查浏览器控制台是否有CORS错误
- 使用本地服务器访问，不要直接打开HTML文件

### 问题3: 数据不更新
- 确认Python测试程序正在运行
- 检查Node.js服务器控制台日志
- 验证Redis数据键名是否正确

### 问题4: 图表显示异常
- 检查浏览器是否支持Canvas
- 尝试刷新页面
- 查看浏览器控制台错误信息

## 性能优化建议

- 使用Chrome或Edge浏览器获得最佳性能
- 大屏幕建议分辨率1920x1080或更高
- Redis更新频率建议不低于500ms
- 3D模型文件大小建议控制在10MB以内

## 开发者

锐视公学 SeeSharp Academy

## 许可证

MIT License
