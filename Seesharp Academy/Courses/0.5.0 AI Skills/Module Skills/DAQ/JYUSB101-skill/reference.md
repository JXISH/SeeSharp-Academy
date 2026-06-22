# JYUSB101 API 速查表

> 源文档：`C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\JYUSB101.xml`

## 通用类型

### JYDriverException

所有驱动错误统一抛出 `JYDriverException`。捕获方式：

```csharp
catch (JYDriverException ex) { /* ex.Message */ }
```

### JYLog（调试）

```csharp
JYLog.EnableLog = true;          // 使能日志
JYLog.LogLevel = JYLogLevel.DEBUG;
```

## JYUSB101AITask（模拟输入）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB101AITask(int boardNum)` | 在指定板卡号上创建 AI 任务 |

### 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `List<AIChannel>` | 通道列表 |
| `Mode` | `AIMode` | 采集模式 |
| `SampleRate` | `double` | 每通道采样率 |
| `SamplesToAcquire` | `int` | 有限点采集每通道样点数（默认 1024，`<0` 表示无限） |
| `BufLenInSamples` | `int` | 缓冲区每通道样点数（`Start()` 后分配） |
| `AvailableSamples` | `int` | 当前可读取的样点数 |
| `ClockEdge` | `AIClockEdge` | 外部时钟沿 |
| `Trigger` | `AITrigger` | 触发参数配置 |

### 主要方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int chnId)` | 添加单通道 |
| `AddChannel(int[] chnsId)` | 添加多通道 |
| `RemoveChannel(int chnId)` | 删除指定通道（`-1` 删除全部） |
| `Start()` | 启动任务 |
| `Stop()` | 停止任务 |
| `ReadData(ref double[] buf, int samples, int timeout)` | 单通道读取 |
| `ReadData(ref double[,] buf, int timeout)` | 多通道读取 |
| `ReadSinglePoint(ref double value)` | 单点读取（`AIMode.Single`） |
| `WaitUntilDone(int timeout)` | 等待任务完成 |

### AIMode 枚举

| 值 | 说明 |
|----|------|
| `Single` | 单点方式 |
| `Finite` | 有限点方式 |
| `Continuous` | 连续方式 |
| `Record` | 连续流盘模式 |

### AITerminal 枚举

| 值 | 说明 |
|----|------|
| `Differential` | 差分模式 |

### AITriggerType 枚举

| 值 | 说明 |
|----|------|
| `Immediate` | 无触发 |
| `DigitalEdge` | 数字边沿触发 |
| `AnalogEdge` | 模拟边沿触发 |

## JYUSB101AOTask（模拟输出）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB101AOTask(int boardNum)` | 在指定板卡号上创建 AO 任务 |

### 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `List<AOChannel>` | 通道列表 |
| `Mode` | `AOMode` | 输出模式 |
| `UpdateRate` | `double` | 每通道更新速率 |
| `SamplesToUpdate` | `int` | 有限点输出每通道点数 |
| `BufLenInSamples` | `int` | 缓冲区每通道样点数 |
| `AvaliableLenInSamples` | `int` | 当前缓冲区每通道可写入样点数 |
| `Trigger` | `AOTrigger` | 触发参数配置 |

### 主要方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int chnId)` | 添加单通道 |
| `AddChannel(int[] chnsId)` | 添加多通道 |
| `RemoveChannel(int chnId)` | 删除指定通道 |
| `WriteData(double[] buf, int timeout)` | 单通道写入（多通道按行交错） |
| `WriteData(double[,] buf, int timeout)` | 多通道写入（每通道按列存放） |
| `WriteSinglePoint(double value)` | 单点输出 |
| `Start()` | 启动输出 |
| `Stop()` | 停止输出 |
| `WaitUntilDone(int timeout)` | 等待输出完成 |

### AOMode 枚举

| 值 | 说明 |
|----|------|
| `Single` | 单点方式 |
| `Finite` | 有限点方式 |
| `ContinuousNoWrapping` | 连续非环绕输出 |
| `ContinuousWrapping` | 连续环绕输出 |

### AOTriggerType 枚举

| 值 | 说明 |
|----|------|
| `Immediate` | 无触发 |
| `Digital` | 数字触发 |

## JYUSB101DITask（数字输入）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB101DITask(int boardNum)` | 创建 DI 任务 |

### 主要方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int lineNum)` | 添加单线 |
| `AddChannel(int[] lineNum)` | 添加多线；`new int[0]` 表示整个端口 |
| `RemoveChannel(int lineNum)` | 删除指定线 |
| `RemoveChannel(int[] lineNum)` | 删除多线；空数组删除全部 |
| `ReadSinglePoint(ref bool[] buf)` | 每通道读取最新一个点 |

## JYUSB101DOTask（数字输出）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB101DOTask(int boardNum)` | 创建 DO 任务 |

### 主要方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int lineNum)` | 添加单线 |
| `AddChannel(int[] lineNum)` | 添加多线；`new int[0]` 表示整个端口 |
| `RemoveChannel(int lineNum)` | 删除指定线 |
| `RemoveChannel(int[] lineNum)` | 删除多线；空数组删除全部 |
| `WriteSinglePoint(bool[] buf)` | 每通道更新一个点 |

## JYUSB101CITask（计数器输入）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB101CITask(int boardNum, int chnId)` | 创建 CI 任务，`chnId` 为 0 或 1 |

### 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `CIMode` | 应用类型 |
| `Counter` | `Counter` | 简单计数参数 |
| `Measure` | `Measure` | 测量参数 |

### 主要方法

| 方法 | 说明 |
|------|------|
| `Start()` | 启动 CI 任务 |
| `Stop()` | 停止 CI 任务 |
| `ReadCounter(ref uint buf)` | 读取计数值 |
| `ReadMeasure(ref double buf)` | 读取测量值（周期/脉宽/边沿间隔） |

### CIMode 枚举

| 值 | 说明 |
|----|------|
| `Counter` | 简单计数 |
| `Measure` | 测量（周期/脉宽/边沿间隔） |

### CountDirection 枚举

| 值 | 说明 |
|----|------|
| `Up` | 增计数 |
| `Down` | 减计数 |

### CIClockSource 枚举

| 值 | 说明 |
|----|------|
| `Internal` | 内部时钟 |
| `External` | 外部时钟 |

### CIGateSource 枚举

| 值 | 说明 |
|----|------|
| `External` | 外部 Gate 引脚 |

### CIPolarity 枚举

| 值 | 说明 |
|----|------|
| `HighActive` | 高有效 |

### CIClockEdge 枚举

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿 |
| `Falling` | 下降沿 |

### MeasureType 枚举

| 值 | 说明 |
|----|------|
| `SinglePeriodMSR` | Gate 上信号的单周期测量 |
| `SinglePulseWidthMSR` | Gate 上信号的单脉冲宽度测量 |
| `EdgeSeparationMSR` | gate 与 aux 信号的差分测量 |

### Counter 类属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `ClockEdge` | `CIClockEdge` | 计数时钟边沿 |
| `ClockSource` | `CIClockSource` | 计数时钟源 |
| `Direction` | `CountDirection` | 计数方向 |
| `InitialCount` | `uint` | 初始计数值 |

### Measure 类属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `ClockEdge` | `CIClockEdge` | 计数时钟边沿 |
| `ClockSource` | `CIClockSource` | 测量时钟源 |
| `ExternalClockRate` | `double` | 外部时钟频率 |
| `Type` | `MeasureType` | 测量类型 |

## JYUSB101COTask（计数器输出）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB101COTask(int boardNum, int chnId)` | 创建 CO 任务，`chnId` 为 0 或 1 |

### 主要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `COMode` | 应用类型 |
| `Clock` | `COClock` | 时钟设置 |
| `Gate` | `COGate` | Gate 设置 |
| `Pulse` | `COPulse` | 脉冲参数设置 |
| `IdleState` | `COSignalLevel` | 闲置状态电平 |

### 主要方法

| 方法 | 说明 |
|------|------|
| `Start()` | 启动 CO 任务 |
| `Stop()` | 停止 CO 任务 |
| `ApplyParam()` | 应用参数配置（PWM 运行时动态更新） |

### COMode 枚举

| 值 | 说明 |
|----|------|
| `SingleGatedPulseGen` | Gate 使能单脉冲生成 |
| `RetrigSinglePulseGen` | 可重触发单脉冲生成 |
| `SingleTrigContPulseGen` | 触发连续脉冲生成 |
| `ContGatedPulseGen` | Gate 使能连续脉冲生成 |
| `SingleTrigContPulseGenPWM` | 触发连续 PWM 生成 |
| `ContGatedPulseGenPWM` | Gate 使能连续 PWM 生成 |

### COClockSource / COGateSource / COSignalEdge

| 枚举 | 主要值 |
|------|--------|
| `COClockSource` | `Internal`, `External` |
| `COGateSource` | `External` |
| `COSignalEdge` | `Rising`, `Falling` |

### COPolarity / COSignalLevel

| 枚举 | 主要值 |
|------|--------|
| `COPolarity` | `HighActive`, `LowActive` |
| `COSignalLevel` | 信号电平 |

### COPulseType 枚举

| 值 | 说明 |
|----|------|
| `HighLowTime` | 按高低电平时间配置 |
| `HighLowTick` | 按时钟 Tick 数配置 |

### COPulse 类属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `COPulseType` | 脉冲参数类型 |
| `Time.High` | `double` | 高电平时间（秒） |
| `Tick.High` | `double` | 高电平 Tick 数 |
| `InitialDelay` | `int` | 初始延迟 |

## JYUSB101Device（设备信息）

可通过 `JYUSB101Device.GetInstance(ushort cardNum)` 获取设备实例，查询硬件能力。

| 属性 | 说明 |
|------|------|
| `CardType` | 卡的类型 |
| `BoardClkRate` | 计数器时基 |
| `DiffChannelCount` | 差分/伪差分通道数 |
| `DiffChannelCountSEChannelCount` | 单端/伪单端通道数 |
| `MaxSampleRateSingleChannel` | 单通道最大采样率 |
| `AOChannelCount` | AO 通道数 |
| `MaxUpdateRateSingleChannel` | 单通道最大更新率 |
| `DIO_LineCount` | DIO 线数 |

## SeeSharpTools 辅助 DLL

| DLL | 常用命名空间/类 | 用途 |
|-----|-----------------|------|
| `SeeSharpTools.JY.DSP.Fundamental` | `Generation.SineWave`, `Generation.SquareWave`, `Generation.UniformWhiteNoise` | 生成正弦、方波、白噪声 |
| `SeeSharpTools.JY.ArrayUtility` | `ArrayManipulation.Transpose` | 多通道数组转置 |
| `SeeSharpTools.JY.GUI` | `EasyChartX`, `LED` 等控件 | 官方 WinForm 范例可视化 |
