---
name: jy5500-driver
description: 提供 JYTEK JY5500 系列（5510/5511/5515/5516）多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、模拟输出（AO）单点/有限/连续输出、数字输入/输出（DI/DO）单点/有限/连续模式、计数器输入/输出（CI/CO）边沿计数/频率/周期/脉冲/编码器测量/脉冲生成、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、录制模式（Finite/Infinite Streaming）。当用户使用 USB/PCIe/PXIe-5510/5511/5515/5516、JY5500AITask、JY5500AOTask、JY5500DITask、JY5500DOTask、JY5500CITask、JY5500COTask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record、AOMode.ContinuousWrapping 开发数据采集、信号生成、波形输出、计数器测量、自动化测试应用时自动应用。
---

# JY5500 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5500\Bin\JY5500.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5500\Bin\JY5500.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY5500;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                  | 5510/5511 规格                          | 5515/5516 规格                          |
| --------------------- | --------------------------------------- | --------------------------------------- |
| AI 分辨率             | 18-bit                                  | 18-bit                                  |
| AI 通道数             | 16 差分 / 32 单端                       | 8 差分 / 16 单端                        |
| AI 最大采样率         | 2 MS/s（每通道）                        | 1.25 MS/s（每通道）                     |
| AI 输入量程           | ±10V / ±5V / ±2V / ±1V / ±0.5V / ±0.2V / ±0.1V | ±10V / ±5V / ±2V / ±1V / ±0.5V / ±0.2V / ±0.1V |
| AI 精度               | 140 ppm                                 | 140 ppm                                 |
| AI 输入耦合           | 直流耦合                                | 直流耦合                                |
| AO 分辨率             | 16-bit                                  | 16-bit                                  |
| AO 通道数             | 4 通道单端                              | 2 通道单端                              |
| AO 最大更新率         | 2.86 MS/s                               | 2.86 MS/s                               |
| AO 输出量程           | ±10V / ±5V                              | ±10V / ±5V                              |
| DI/DO 通道            | 48 通道（TTL 电平）                     | 24 通道（TTL 电平）                     |
| 计数器                | 4 个 32-bit 计数器                      | 2 个 32-bit 计数器                      |
| 计数器最大频率        | 200 MHz                                 | 100 MHz                                 |
| 触发方式              | 模拟 / 数字 / 软件触发                   | 模拟 / 数字 / 软件触发                   |
| 接口类型              | USB / PXIe / PCIe                       | USB / PXIe / PCIe                       |

### 产品型号

- **USB-5510**：16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, USB 多功能 I/O 模块
- **PCIe-5510**：16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PCIe 多功能 I/O 模块
- **PXIe-5510**：16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PXIe 多功能 I/O 模块

- **USB-5511**：8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, USB 多功能 I/O 模块
- **PCIe-5511**：8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PCIe 多功能 I/O 模块
- **PXIe-5511**：8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PXIe 多功能 I/O 模块

- **USB-5515**：16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, USB 多功能 I/O 模块
- **PCIe-5515**：16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PCIe 多功能 I/O 模块
- **PXIe-5515**：16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PXIe 多功能 I/O 模块

- **USB-5516**：8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, USB 多功能 I/O 模块
- **PCIe-5516**：8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PCIe 多功能 I/O 模块
- **PXIe-5516**：8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PXIe 多功能 I/O 模块

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY5500AITask(0);          // 1. 创建（按槽位号）
task.AddChannel(0, -10, 10);             // 2. 添加通道
task.Mode = AIMode.Continuous;            // 3. 配置
task.SampleRate = 10000;
task.Start();                            // 4. 启动
// ... 读取数据 ...
task.Stop();                             // 5. 停止
task.Channels.Clear();                   // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY5500AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Single / Finite / Continuous / Record   |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s）                    |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数             |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（非 Single 模式）  |
| `SampleClock.Source` | `AISampleClockSource`   | Internal（默认）/ External              |
| `Trigger.Type`       | `AITriggerType`         | Immediate / Digital / Analog / Soft     |

#### AddChannel 重载

```csharp
// 单通道（使用默认 RSE 终端模式）
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);

// 多通道（相同量程，使用默认 RSE 终端模式）
aiTask.AddChannel(int[] chnsId, double rangeLow, double rangeHigh);

// 单通道（指定终端模式）
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh, AITerminal terminalCfg);

// 多通道（相同量程，指定终端模式）
aiTask.AddChannel(int[] chnsId, double rangeLow, double rangeHigh, AITerminal terminalCfg);

// 多通道（各自量程，指定终端模式）
aiTask.AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh, AITerminal terminalCfg);

// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2 / -1 / -0.5 / -0.2 / -0.1
// rangeHigh: 10 / 5 / 2 / 1 / 0.5 / 0.2 / 0.1
// terminalCfg: AITerminal.RSE（默认）/ NRSE / Differential
// 通道编号：5510/5511 SE 0-31, Differential 0-7或16-23; 5515/5516 SE 0-16, Differential 0-7
```

### AITerminal 枚举

| 值            | 说明           |
| ------------- | -------------- |
| `RSE`         | 参考单端模式（默认） |
| `NRSE`        | 非参考单端模式     |
| `Differential`| 差分模式        |

#### 读取数据重载

```csharp
// Single 模式 — 单通道
aiTask.ReadSinglePoint(ref double readValue, int channel);

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);

// Finite/Continuous — 多通道（列存储，每列一个通道）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);

// timeout = -1 → 永久等待
```

#### AI 四种模式速查

| 模式         | 典型配置                      | 读取方式                                     |
| ------------ | ----------------------------- | -------------------------------------------- |
| `Single`     | `Mode=AIMode.Single`          | 轮询 `ReadSinglePoint`                       |
| `Finite`     | `+SamplesToAcquire=N`         | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                 | Timer 轮询 `AvailableSamples` → `ReadData`   |
| `Record`     | `+Record.FilePath/Mode/Length`| `GetRecordPreviewData` 预览，数据写入文件    |

#### 连续采集 Timer 模式（最常用）

```csharp
// Timer Tick 中：
timer.Enabled = false;
if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
{
    aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
    easyChartX1.Plot(readBuffer);   // 显示波形
}
timer.Enabled = true;
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;  // PFI0~PFI15
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;    // Rising/Falling
```

#### 模拟触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.AI0;     // AI0~AI31
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;                   // 触发电平（V）
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
aiTask.SendSoftwareTrigger();  // 手动发送软触发
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10000;
```

### 多卡同步

JY5500 支持多种同步方式：AI 多卡同步、AI 与 AO 同步、DI/DO 多卡同步等。

#### AI 多卡采样时钟同步（有限采集）

```csharp
// ===== 主卡配置（Slot 2）=====
var masterTask = new JY5500AITask(2);
masterTask.AddChannel(0);
masterTask.Mode = AIMode.Finite;
masterTask.SamplesToAcquire = 10000;
masterTask.SampleRate = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟和触发信号
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 4）=====
var slaveTask = new JY5500AITask(4);
slaveTask.AddChannel(0);
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 10000;

// 使用外部时钟（接收主卡的 PXI_Trig0）
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10000;

// 接收主卡的触发信号
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.DisableCalibration = true;  // 禁用校准以加速启动
slaveTask.Start();
masterTask.Start();

// ===== 读取数据 =====
double[] masterData = new double[10000];
double[] slaveData = new double[10000];

masterTask.WaitUntilDone(-1);
slaveTask.WaitUntilDone(-1);

masterTask.ReadData(ref masterData, 10000, -1);
slaveTask.ReadData(ref slaveData, 10000, -1);

masterTask.Stop();
slaveTask.Stop();
masterTask.Channels.Clear();
slaveTask.Channels.Clear();
```

#### AI 多卡连续同步采集

```csharp
// ===== 主卡配置 =====
var masterTask = new JY5500AITask(0);
masterTask.AddChannel(0);
masterTask.Mode = AIMode.Continuous;
masterTask.SampleRate = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟和触发信号
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== 从卡配置 =====
var slaveTask = new JY5500AITask(1);
slaveTask.AddChannel(0);
slaveTask.Mode = AIMode.Continuous;

slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10000;

slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// 启动
slaveTask.Start();
masterTask.Start();

// 连续采集数据（在 Timer 中读取）
System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
timer.Interval = 100;
timer.Tick += (s, e) =>
{
    timer.Enabled = false;
    if (masterTask.AvailableSamples >= 1000 && slaveTask.AvailableSamples >= 1000)
    {
        double[] masterData = new double[1000];
        double[] slaveData = new double[1000];
        
        masterTask.ReadData(ref masterData, 1000, 1000);
        slaveTask.ReadData(ref slaveData, 1000, 1000);
        
        // 处理数据...
    }
    timer.Enabled = true;
};
timer.Start();
```

#### AI 与 AO 同步

JY5500 支持 AI 采集与 AO 输出同步，实现闭环测试。

**注意**：在 AI-AO 同步中，**AI 作为主设备**（导出时钟和触发信号），**AO 作为从设备**（接收 AI 的时钟和触发信号）。

```csharp
// ===== AI Task（主设备）=====
var aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// AI 导出采样时钟和触发信号
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PFI0);

// ===== AO Task（从设备）=====
var aoTask = new JY5500AOTask(0);
aoTask.AddChannel(0, -10, 10);
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 10000;

// AO 接收 AI 的时钟和触发
aoTask.Trigger.Type = AOTriggerType.Digital;
aoTask.Trigger.Digital.Source = AODigitalTriggerSource.AI_StartTrig;
aoTask.SampleClock.Source = AOSampleClockSource.External;
aoTask.SampleClock.External.Terminal = ClockTerminal.AI_SampleClock;
aoTask.SampleClock.External.ExpectedRate = 10000;

// 先写数据到 AO 缓冲区
double[] aoData = new double[10000];  // 填充输出数据
aoTask.WriteData(aoData);

// 启动（AO 先启动，AI 后启动）
aoTask.Start();
aiTask.Start();

// 连续采集数据（在 Timer 中读取）
System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
timer.Interval = 100;
timer.Tick += (s, e) =>
{
    timer.Enabled = false;
    if (aiTask.AvailableSamples >= 1000)
    {
        double[,] aiData = new double[1000, 1];
        aiTask.ReadData(ref aiData, 1000, -1);
        // 处理数据...
    }
    timer.Enabled = true;
};
timer.Start();
```

**关键要点：**
- **主设备是 AI**（导出时钟），**从设备是 AO**（接收时钟）
- 主设备通过 `SignalExport.Add()` 导出时钟和触发信号
- 从设备配置 `AOSampleClockSource.External` 接收外部时钟
- 启动顺序：**先从卡后主卡**（AO 先启动，AI 后启动）
- 使用 `PFI0~PFI15` 作为同步信号线，或使用内部信号 `ClockTerminal.AI_SampleClock` 和 `AODigitalTriggerSource.AI_StartTrig`
- AO 作为从设备时，可使用 `AODigitalTriggerSource.AI_StartTrig` 直接接收 AI 的触发信号

---

## 模拟输出（AO）

### 任务类：`JY5500AOTask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`         | `double`                | 更新率（Sa/s）                          |
| `Trigger.Type`       | `AOTriggerType`         | Immediate / Digital / Soft              |

#### 写入数据

```csharp
// Single 模式
aoTask.WriteSinglePoint(double writeValue, int channel);

// Continuous/Finite 模式 — 多通道（列存储）
double[,] writeBuf = new double[samples, channels];
// ... 填充数据 ...
aoTask.WriteData(writeBuf, int timeout);

// 或单通道
aoTask.WriteData(double[] buf, int timeout);
```

#### AO 连续输出标准流程

```csharp
aoTask = new JY5500AOTask(0);
aoTask.AddChannel(0, -10, 10);
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 10000;

// 生成波形数据
double[,] writeBuf = new double[1000, 1];
// ... 填充波形数据 ...

// 先写数据，再 Start（Wrapping 模式要求）
aoTask.WriteData(writeBuf, -1);
aoTask.Start();
```

---

## 数字输入（DI）

### 任务类：`JY5500DITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `DIMode`                | Single / Finite / Continuous            |
| `SampleRate`         | `double`                | 采样率（Sa/s）                          |
| `Trigger.Type`       | `DITriggerType`         | Immediate / Digital / Soft              |
| `SampleClock.Source` | `DISampleClockSource`   | Internal / External                     |

#### 读取数据

```csharp
// Single 模式
bool[] readValue = new bool[8];
diTask.ReadSinglePoint(ref readValue, int portID);

// Continuous/Finite 模式
byte[,] dataBuf = new byte[samples, channels];
diTask.ReadData(ref dataBuf, uint samplesPerChannel, int timeout);
```

---

## 数字输出（DO）

### 任务类：`JY5500DOTask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `DOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`         | `double`                | 更新率（Sa/s）                          |
| `Trigger.Type`       | `DOTriggerType`         | Immediate / Digital / Soft              |

#### 写入数据

```csharp
// Single 模式
bool[] writeValue = new bool[8] { true, false, true, false, true, false, true, false };
doTask.WriteSinglePoint(writeValue, int portID);

// Continuous/Finite 模式 — 多通道（列存储）
byte[,] writeBuf = new byte[samples, channels];
// ... 填充数据 ...
doTask.WriteData(writeBuf, uint samplesPerChannel, int timeout);
```

---

## 计数器输入（CI）

### 任务类：`JY5500CITask`

**构造**：`new JY5500CITask(int slotNumber, int channel)` — channel: 5510/5515 支持 0-3，5511/5516 支持 0-1

#### CI 测量类型

| CIType                | 说明                    |
| --------------------- | ----------------------- |
| `EdgeCounting`        | 边沿计数                |
| `Frequency`           | 频率测量                |
| `Period`              | 周期测量                |
| `Pulse`               | 脉冲宽度测量            |
| `TwoEdgeSeparation`   | 两边沿间隔测量          |
| `Encode`              | 正交编码器              |

```csharp
// 边沿计数示例
ciTask = new JY5500CITask(0, 0);
ciTask.Mode = CIMode.Single;
ciTask.Type = CIType.EdgeCounting;
ciTask.EdgeCounting.Direction = CountDirection.Up;
ciTask.EdgeCounting.InitialCount = 0;
ciTask.Start();
uint count;
ciTask.ReadSinglePoint(ref count);
ciTask.Stop();
```

---

## 计数器输出（CO）

### 任务类：`JY5500COTask`

```csharp
coTask = new JY5500COTask(0, 0);
coTask.Mode = COMode.Single;
coTask.IdleState = COIdleState.LowLevel;
coTask.InitialDelay = 0;

// 输出脉冲（频率 1kHz，占空比 50%，100 个脉冲）
var pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, 100);
coTask.WriteSinglePoint(pulse);
coTask.Start();
```

`COPulseType` 三种构造方式：

| 类型              | param1       | param2       |
| ----------------- | ------------ | ------------ |
| `DutyCycleFrequency` | 频率（Hz）   | 占空比（0~1） |
| `HighLowTime`     | 高电平时长（s） | 低电平时长（s） |
| `HighLowTick`     | 高电平 Tick 数 | 低电平 Tick 数 |

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 50000;

// 配置录制参数
aiTask.Record.FilePath = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;        // Finite / Infinite
aiTask.Record.Length = 10.0;                   // 录制时长（秒），Finite 有效

aiTask.Start();

// 可在录制期间预览数据
double[,] preview = new double[1000, 1];
aiTask.GetRecordPreviewData(ref preview, 1000, -1);
easyChartX1.Plot(preview);

// 等待录制完成
double recordedLen;
bool recordDone;
do
{
    aiTask.GetRecordStatus(out recordedLen, out recordDone);
} while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

### AIRecord 配置属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（仅支持 Bin） |
| `Mode` | `RecordMode` | Finite（定长）/ Infinite（无限） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |

---

## 常见错误处理

| 异常代码                                         | 原因                     | 处理建议              |
| ------------------------------------------------ | ------------------------ | --------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误   | 检查设备管理器槽位号  |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start | 在 Start 前添加通道   |
| `BufferDataOverflow`                             | 读取速度慢于采集速度     | 增大读取频率或减小采样率 |
| `ReadDataTimeout`                                | timeout 内未读到足够数据 | 增大 timeout 或检查采样率 |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数      | Stop 后再修改         |
| `SampleRateParameterInvalid`                     | 采样率超过硬件上限       | 检查通道数和最大采样率 |

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)


---

# 完整 API 参考

# JY5500 驱动 API 完整参考

## 命名空间与引用

```csharp
using JY5500;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DAQ\JY5500\Bin\JY5500.dll
```

---

## JY5500AITask — 模拟输入任务

### 构造函数

```csharp
new JY5500AITask(int slotNumber)      // 按槽位号创建（推荐）
```

### 属性

| 属性                  | 类型                | 默认值      | 说明                                    |
| --------------------- | ------------------- | ----------- | --------------------------------------- |
| `Mode`                | `AIMode`            | —           | Single / Finite / Continuous / Record   |
| `SampleRate`          | `double`            | 10000       | 每通道采样率（Sa/s）                    |
| `SamplesToAcquire`    | `int`               | —           | Finite 模式采集点数/通道                |
| `AvailableSamples`    | `ulong`             | —           | 缓冲区可读点数（非 Single 模式有效）    |
| `TransferedSamples`   | `ulong`             | —           | 已传输点数（非 Single 模式有效）        |
| `SampleClock`         | `AISampleClock`     | —           | 时钟配置对象                            |
| `Trigger`             | `AITrigger`         | —           | 触发配置对象                            |
| `Record`              | `AIRecord`          | —           | 录制配置对象                            |
| `Channels`            | `List<AIChannel>`   | —           | 已添加的通道列表                        |

### 方法

#### AddChannel

```csharp
// 单通道（使用默认 RSE 终端模式）
void AddChannel(int chnId, double rangeLow, double rangeHigh)

// 多通道（相同量程，使用默认 RSE 终端模式）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh)

// 单通道（指定终端模式）
void AddChannel(int chnId, double rangeLow, double rangeHigh, AITerminal terminalCfg)

// 多通道（相同量程，指定终端模式）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh, AITerminal terminalCfg)

// 多通道（各自量程，指定终端模式）
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh, AITerminal terminalCfg)

// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2 / -1 / -0.5 / -0.2 / -0.1
// rangeHigh: 10 / 5 / 2 / 1 / 0.5 / 0.2 / 0.1
// terminalCfg: AITerminal.RSE（默认）/ NRSE / Differential
// 通道编号：5510/5511 SE 0-31, Differential 0-7或16-23; 5515/5516 SE 0-16, Differential 0-7
```

#### 控制

```csharp
void Start()
void Stop()
void WaitUntilDone(int timeout)   // timeout=-1 永久等待，仅 Finite 有效
void SendSoftwareTrigger()        // 软触发
```

#### 读取数据

```csharp
// Single 模式
void ReadSinglePoint(ref double readValue, int channel)

// 缓冲模式（Finite / Continuous）
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)      // 单通道
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)     // 多通道（列存储）
```

#### 录制模式

```csharp
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout)
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout)
void GetRecordStatus(out double recordedLength, out bool recordDone)
```

---

## AIMode 枚举

| 值           | 说明                                         |
| ------------ | -------------------------------------------- |
| `Single`     | 软件触发单点读取，可循环调用 ReadSinglePoint |
| `Finite`     | 采集固定点数后停止，通过 WaitUntilDone 等待完成 |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取     |
| `Record`     | 数据流式写入文件，支持预览                   |

---

## AISampleClock — 时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.Internal;   // 默认
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;  // PFI0~PFI15
aiTask.SampleClock.External.ExpectedRate = 10000.0;         // 外部时钟期望频率
```

### ClockTerminal 枚举

| 值                         | 说明         |
| ------------------------- | ---------- |
| `ECLK`                    | 外部时钟引脚定义 |
| `PFI0` ~ `PFI15`          | 前面板 PFI 引脚 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线   |
| `AI_SampleClock`          | AI 采样时钟    |
| `AO_SampleClock`          | AO 采样时钟    |
| `DI_SampleClock`          | DI 采样时钟    |
| `DO_SampleClock`          | DO 采样时钟    |
| `CI0_SampleClock` ~ `CI3_SampleClock` | CI 采样时钟 |

---

## AITrigger — 触发配置

### 属性

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;    // Digital / Analog / Soft / Immediate
aiTask.Trigger.Mode = AITriggerMode.Start;      // Start / Reference
```

### AITrigger 其他重要属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `PreTriggerSamples` | `uint` | 预触发样本数，Reference模式有效，必须 ≤ SamplesToAcquire |
| `ReTriggerCount` | `int` | 重复触发次数，0或1表示只触发一次，-1表示连续重复触发直到停止 |

### 数字触发

```csharp
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;     // PFI0~PFI15
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;       // Rising / Falling
```

### 模拟触发

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;  // Channel_0 ~ Channel_31
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;  // Edge / Hysteresis / Window

// Edge 模式
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;

// Hysteresis 模式
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 3.0;
aiTask.Trigger.Analog.Hysteresis.LowThreshold = 1.0;

// Window 模式
aiTask.Trigger.Analog.Window.Condition = AIAnalogWindowCondition.Entering;
aiTask.Trigger.Analog.Window.HighThreshold = 5.0;
aiTask.Trigger.Analog.Window.LowThreshold = -5.0;
```

### AITriggerType 枚举

| 值           | 说明        |
| ----------- | --------- |
| `Immediate` | 立即触发（默认）  |
| `Digital`   | 数字边沿触发    |
| `Analog`    | 模拟边沿/窗口触发 |
| `Soft`      | 软件触发      |

### AITriggerMode 枚举

| 值           | 说明       |
| ----------- | -------- |
| `Start`     | 开始触发（默认） |
| `Reference` | 参考触发     |

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;           // 仅支持 Bin 格式
aiTask.Record.Mode = RecordMode.Finite;              // Finite / Infinite
aiTask.Record.Length = 10.0;                         // 录制时长（秒），Finite 有效
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |

---

## RecordMode 枚举

| 值 | 说明 |
|----|------|
| `Finite` | 有限录制，录制指定时长后自动停止 |
| `Infinite` | 无限录制，需手动调用 Stop 停止 |

---

## FileFormat 枚举

| 值 | 说明 |
|----|------|
| `Bin` | 二进制格式（double 类型） |

---

## JY5500AOTask — 模拟输出任务

### 构造函数

```csharp
new JY5500AOTask(int slotNumber)
```

### 属性

| 属性            | 类型              | 默认值 | 说明                                                    |
| ------------- | --------------- | --- | ----------------------------------------------------- |
| `Mode`        | `AOMode`        | —   | Single/Finite/ContinuousWrapping/ContinuousNoWrapping |
| `UpdateRate`  | `double`        | —   | 更新率（Sa/s）                                             |
| `SamplesToUpdate` | `int`       | —   | Finite模式每通道输出点数                                   |
| `BufLenInSamples` | `int`       | —   | 缓冲区大小（每通道样本数）                                  |
| `AvaliableLenInSamples` | `int` | —   | 可写入的样本数                                             |
| `TransferedSamples` | `ulong`   | —   | 已传输样本数（非Single模式有效）                           |
| `CompleteState` | `bool`        | —   | 完成状态                                                   |
| `SampleClock` | `AOSampleClock` | —   | 时钟配置对象                                                |
| `Trigger`     | `AOTrigger`     | —   | 触发配置对象                                                |
| `DisableCalibration` | `bool`   | —   | 禁用校准                                                   |
| `SignalExport` | `AOSignalExport` | —   | 信号导出                                                   |

### 方法

```csharp
void AddChannel(int chnId, double rangeLow, double rangeHigh)     // 添加通道（0~3）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh)
void RemoveChannel(int chnId)

void Start()
void Stop()
void WaitUntilDone(int timeout)

// Single 模式
void WriteSinglePoint(double writeValue, int channel)

// Continuous/Finite 模式
void WriteData(double[,] buf, int timeout)     // 多通道（列存储）
void WriteData(double[] buf, int timeout)      // 单通道
```

### AOMode 枚举

| 值                      | 说明              |
| ---------------------- | --------------- |
| `Single`               | 写入寄存器，立即输出      |
| `Finite`               | 输出固定点数后停止       |
| `ContinuousWrapping`   | 循环输出固定缓冲区       |
| `ContinuousNoWrapping` | 持续消耗缓冲区数据，可实时追加 |

---

## JY5500DITask — 数字输入任务

### 构造函数

```csharp
new JY5500DITask(int slotNumber)
```

### 属性

| 属性                 | 类型              | 默认值 | 说明                           |
| ------------------ | --------------- | --- | ---------------------------- |
| `Channels`         | `List<DIChannel>` | —   | 已添加的端口列表                 |
| `Mode`             | `DIMode`        | —   | Single / Finite / Continuous |
| `SampleRate`       | `double`        | —   | 采样率（Sa/s）                    |
| `SamplesToAcquire` | `int`           | —   | Finite模式每通道采集点数          |
| `BufLenInSamples`  | `int`           | —   | 缓冲区大小（每通道样本数）         |
| `AvailableSamples` | `ulong`         | —   | 缓冲区可读点数（非Single模式）    |
| `TransferedSamples`| `ulong`         | —   | 已传输样本数（非Single模式）      |
| `SampleClock`      | `DISampleClock` | —   | 时钟配置对象                       |
| `Trigger`          | `DITrigger`     | —   | 触发配置对象                       |
| `SignalExport`     | `DISignalExport`| —   | 信号导出                           |
| `Device`           | `JY5500Device`  | —   | 设备对象                           |

### 方法

```csharp
void AddChannel(int portID)                         // 添加端口（0~3，每端口 8 通道）
void AddChannel(int[] portIDs)
void AddChannel(int portID, bool enableFilter, int minPulseWidth)           // 带滤波器配置
void AddChannel(int[] portIDs, bool enableFilter, int minPulseWidth)        // 多端口相同滤波器
void AddChannel(int[] portIDs, bool[] enableFilter, int[] minPulseWidth)    // 多端口各自滤波器
void RemoveChannel(int portID)

void Start()
void Stop()

// Single 模式
void ReadSinglePoint(ref bool[] readValue, int portID)

// Continuous/Finite 模式
void ReadData(ref byte[,] buf, uint samplesPerChannel, int timeout)
void ReadData(ref ushort[] buf, uint samplesPerChannel, int timeout)
```

### DIMode 枚举

| 值            | 说明     |
| ------------ | ------ |
| `Single`     | 单点读取   |
| `Finite`     | 有限点数采集 |
| `Continuous` | 连续采集   |

---

## JY5500DOTask — 数字输出任务

### 构造函数

```csharp
new JY5500DOTask(int slotNumber)
```

### 属性

| 属性            | 类型              | 默认值 | 说明                                                    |
| ------------- | --------------- | --- | ----------------------------------------------------- |
| `Channels`    | `List<DOChannel>` | —   | 已添加的端口列表                                          |
| `Mode`        | `DOMode`        | —   | Single/Finite/ContinuousWrapping/ContinuousNoWrapping |
| `UpdateRate`  | `double`        | —   | 更新率（Sa/s）                                             |
| `SamplesToUpdate` | `int`       | —   | Finite模式每通道输出点数                                   |
| `BufLenInSamples` | `int`       | —   | 缓冲区大小（每通道样本数）                                  |
| `AvaliableLenInSamples` | `int` | —   | 可写入的样本数                                             |
| `TransferedSamples` | `ulong`   | —   | 已传输样本数（非Single模式有效）                           |
| `CompleteState` | `bool`        | —   | 完成状态                                                   |
| `SampleClock` | `DOSampleClock` | —   | 时钟配置对象                                                |
| `Trigger`     | `DOTrigger`     | —   | 触发配置对象                                                |
| `SignalExport` | `DOSignalExport` | —   | 信号导出                                                   |
| `Device`      | `JY5500Device`  | —   | 设备对象                                                   |

### 方法

```csharp
void AddChannel(int portID)                         // 添加端口（0~3）
void AddChannel(int[] portIDs)
void RemoveChannel(int portID)

void Start()
void Stop()
void WaitUntilDone(int timeout)

// Single 模式
void WriteSinglePoint(bool[] writeValue, int portID)

// Continuous/Finite 模式
void WriteData(byte[,] buf, uint samplesPerChannel, int timeout)
```

### DOMode 枚举

| 值                      | 说明              |
| ---------------------- | --------------- |
| `Single`               | 写入寄存器，立即输出      |
| `Finite`               | 输出固定点数后停止       |
| `ContinuousWrapping`   | 循环输出固定缓冲区       |
| `ContinuousNoWrapping` | 持续消耗缓冲区数据，可实时追加 |

---

## JY5500CITask — 计数器输入任务

### 构造函数

```csharp
new JY5500CITask(int slotNumber, int channel)      // channel: 5510/5515 支持 0-3，5511/5516 支持 0-1
```

### 属性

| 属性                  | 类型                | 说明                                    |
| --------------------- | ------------------- | --------------------------------------- |
| `Mode`                | `CIMode`            | Single / Finite / Continuous            |
| `Type`                | `CIType`            | 测量类型                                |
| `SampleClock`         | `CISampleClock`     | 时钟配置对象                            |
| `SamplesToAcquire`    | `int`               | Finite模式每通道采集点数                |
| `AvailableSamples`    | `ulong`             | 缓冲区可读点数（非Single模式）          |
| `TransferedSamples`   | `ulong`             | 已传输样本数（非Single模式）            |
| `EdgeCounting`        | `CIEdgeCounting`    | 边沿计数配置                            |
| `FrequencyMeas`       | `CIFrequencyMeas`   | 频率测量配置                            |
| `PeriodMeas`          | `CIPeriodMeas`      | 周期测量配置                            |
| `PulseMeas`           | `CIPulseMeas`       | 脉冲测量配置                            |
| `TwoEdgeSeparation`   | `CITwoEdgeSeparation`| 两边沿间隔测量配置                     |
| `QuadEncoder`         | `CIQuadEncoder`     | 正交编码器配置                          |
| `TwoPulseEncoder`     | `CITwoPulseEncoder` | 双脉冲编码器配置                        |

### CIType 枚举

| 值                    | 说明                    |
| --------------------- | ----------------------- |
| `EdgeCounting`        | 边沿计数                |
| `Frequency`           | 频率测量（Hz）          |
| `Period`              | 周期测量（秒）          |
| `Pulse`               | 脉冲宽度测量            |
| `TwoEdgeSeparation`   | 两边沿间隔测量          |
| `QuadEncoder`         | 正交编码器              |
| `TwoPulseEncoder`     | 双脉冲编码器            |

### 读取方法

```csharp
// EdgeCounting / QuadEncoder / TwoPulseEncoder
void ReadSinglePoint(ref uint count)
void ReadSinglePoint(ref uint[] count, int samplesToRead, int timeout)
void ReadSinglePoint(ref uint[,] count, int samplesToRead, int timeout)

// Frequency / Period
void ReadSinglePoint(ref double measurement, int timeout)
void ReadSinglePoint(ref double[] measurement, int samplesToRead, int timeout)
void ReadSinglePoint(ref double[,] measurement, int samplesToRead, int timeout)

// Pulse / TwoEdgeSeparation
void ReadSinglePoint(ref double m1, ref double m2, int timeout)
void ReadSinglePoint(ref double[] m1, ref double[] m2, int samplesToRead, int timeout)
void ReadSinglePoint(ref double[,] m1, ref double[,] m2, int samplesToRead, int timeout)
```

---

## COIdleState 枚举

| 值           | 说明           |
| ------------ | -------------- |
| `LowLevel`   | 空闲状态为低电平 |
| `HighLevel`  | 空闲状态为高电平 |

---

## JY5500COTask — 计数器输出任务

### 构造函数

```csharp
new JY5500COTask(int slotNumber, int channel)      // channel: 5510/5515 支持 0-3，5511/5516 支持 0-1
```

### 属性

| 属性                  | 类型                | 说明                                    |
| --------------------- | ------------------- | --------------------------------------- |
| `Mode`                | `COMode`            | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `OutputTerminal`      | `COTerminal`        | 脉冲输出终端                            |
| `IdleState`           | `COIdleState`       | LowLevel / HighLevel                    |
| `InitialDelay`        | `double`            | 初始延迟（秒）                          |
| `TimeBase`            | `COTimeBase`        | 时基配置                                |
| `Pause`               | `COPause`           | 暂停触发配置                            |
| `SamplesToUpdate`     | `int`               | Finite模式输出样本数                    |
| `AvaliableLenInSamples`| `int`              | 缓冲区可用长度                          |
| `TransferedSamples`   | `ulong`             | 已传输样本数                            |
| `TransferedPulses`    | `ulong`             | 已传输脉冲数                            |
| `Trigger`             | `COTrigger`         | 触发配置对象                            |

### COPulse 构造

```csharp
// 按频率和占空比
new COPulse(COPulseType.DutyCycleFrequency, double freq, double dutyCycle, int count)
// 按高低电平时长（秒）
new COPulse(COPulseType.HighLowTime, double highTime, double lowTime, int count)
// 按高低 Tick 数
new COPulse(COPulseType.HighLowTick, double highTick, double lowTick, int count)
// count = -1 → 无限循环
```

### 方法

```csharp
void WriteSinglePoint(COPulse pulse)
void Start()
void Stop()
void WaitUntilDone(int timeout)
```

---

## 异常类 JYDriverException

```csharp
try { ... }
catch (JYDriverException ex)
{
    // ex.Message — 驱动错误描述
    // ex.ExceptionName — 异常枚举名
    // ex.ErrorCode — 错误码
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 枚举（常见值）

| 枚举值                                           | 含义                        |
| ------------------------------------------------ | --------------------------- |
| `OpenDeviceFailed`                               | 打开设备失败（未连接/槽位号错误） |
| `CloseDeviceFailed`                              | 关闭设备失败            |
| `NoChannelAdded`                                 | 未添加通道                  |
| `StartTaskFailed`                                | 启动 Task 失败              |
| `StopTaskFailed`                                 | 停止 Task 失败              |
| `TaskHasNotStarted`                              | Task 未启动即读写       |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数      |
| `BufferDataOverflow`                             | 采集缓冲区溢出              |
| `BufferDataDownflow`                             | 输出缓冲区数据不足         |
| `ReadDataTimeout`                                | 读取超时                    |
| `WriteDataTimeout`                               | 写入超时              |
| `SampleRateParameterInvalid`                     | 采样率超限                  |
| `ChannelNumberParameterInvalid`                  | 无效通道号             |
| `ChannelInputRangeParameterInvalid`              | 无效输入量程            |
| `TriggerParameterInvalid`                        | 触发参数无效            |

---

## 设备硬件规格

| 参数                  | 5510/5511                               | 5515/5516                               |
| --------------------- | --------------------------------------- | --------------------------------------- |
| AI 分辨率             | 18-bit                                  | 18-bit                                  |
| AI 通道数             | 16 差分 / 32 单端                       | 8 差分 / 16 单端                        |
| AI 最大采样率         | 2 MS/s（每通道）                        | 1.25 MS/s（每通道）                     |
| AI 输入量程           | ±10V / ±5V / ±2V / ±1V / ±0.5V / ±0.2V / ±0.1V | ±10V / ±5V / ±2V / ±1V / ±0.5V / ±0.2V / ±0.1V |
| AI 精度               | 140 ppm                                 | 140 ppm                                 |
| AI 输入耦合           | 直流耦合                                | 直流耦合                                |
| AI 输入阻抗           | 高阻抗                                  | 高阻抗                                  |
| AO 分辨率             | 16-bit                                  | 16-bit                                  |
| AO 通道数             | 4 通道单端                              | 2 通道单端                              |
| AO 最大更新率         | 2.86 MS/s                               | 2.86 MS/s                               |
| AO 输出量程           | ±10V / ±5V                              | ±10V / ±5V                              |
| DI/DO 通道            | 48 通道（TTL 电平）                     | 24 通道（TTL 电平）                     |
| 计数器                | 4 个 32-bit 计数器                      | 2 个 32-bit 计数器                      |
| 计数器最大频率        | 200 MHz                                 | 100 MHz                                 |
| 触发方式              | 模拟 / 数字 / 软件触发                   | 模拟 / 数字 / 软件触发                   |
| 接口                  | USB / PXIe / PCIe                       | USB / PXIe / PCIe                       |

### 产品型号

| 型号        | 描述                                          |
| ----------- | --------------------------------------------- |
| USB-5510    | 16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, USB 多功能 I/O |
| PCIe-5510   | 16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PCIe 多功能 I/O |
| PXIe-5510   | 16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PXIe 多功能 I/O |
| USB-5511    | 8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, USB 多功能 I/O |
| PCIe-5511   | 8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PCIe 多功能 I/O |
| PXIe-5511   | 8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PXIe 多功能 I/O |
| USB-5515    | 16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, USB 多功能 I/O |
| PCIe-5515   | 16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PCIe 多功能 I/O |
| PXIe-5515   | 16-ch AI (18-bit, 2MS/s), 4-ch AO, 48 DIO, 4 CTR, PXIe 多功能 I/O |
| USB-5516    | 8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, USB 多功能 I/O |
| PCIe-5516   | 8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PCIe 多功能 I/O |
| PXIe-5516   | 8-ch AI (18-bit, 1.25MS/s), 2-ch AO, 24 DIO, 2 CTR, PXIe 多功能 I/O |


---

# 完整代码示例

# JY5500 代码示例集

> 所有示例均来自 `JY5500_V4.4.1_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点采集（Console）

```csharp
using System;
using JY5500;

// 1. 创建 AI Task（按槽位号）
Console.WriteLine("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());
JY5500AITask aiTask = new JY5500AITask(boardNum);

// 2. 设置模式
aiTask.Mode = AIMode.Single;

// 3. 添加通道：通道 0，量程 ±10V
Console.WriteLine("请输入通道号：");
int channelID = Convert.ToInt32(Console.ReadLine());
aiTask.AddChannel(channelID, -10, 10);

// 4. 启动 Task
aiTask.Start();

// 5. 循环读取单点
double readValue = 0;
int flag = 1;
while (flag == 1)
{
    aiTask.ReadSinglePoint(ref readValue, channelID);
    Console.WriteLine($"通道 {channelID} 电压值: {readValue} V");
    Console.WriteLine("是否继续读取? (1:是, 0:否)");
    flag = Convert.ToInt16(Console.ReadLine());
}

// 6. 停止并清除通道
aiTask.Stop();
aiTask.Channels.Clear();
Console.WriteLine("采集完成，按任意键退出");
Console.ReadKey();
```

---

## 示例 2：AI 连续采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY5500;

public partial class MainForm : Form
{
    private JY5500AITask aiTask;
    private double[] readBuffer;
    private double lowRange = -10;
    private double highRange = 10;

    // 启动按钮
    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            // 1. 创建 Task（槽位号 0）
            aiTask = new JY5500AITask(0);

            // 2. 添加通道
            aiTask.AddChannel(0, lowRange, highRange);

            // 3. 配置连续模式参数
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 10000;  // 10 kSa/s

            // 4. 启动 Task
            aiTask.Start();

            // 5. 分配读取缓冲区
            readBuffer = new double[1000];

            // 6. 启动 Timer（每 10ms 刷新一次）
            timer_FetchData.Enabled = true;
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    // Timer Tick：轮询缓冲区并读取
    private void timer_FetchData_Tick(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        try
        {
            // 只有缓冲区数据足够时才读取，避免阻塞
            if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
            {
                // ReadData：timeout=-1 表示等待直到数据就绪
                aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);

                // 在 EasyChartX 上显示波形
                easyChartX1.Plot(readBuffer);
            }
        }
        catch (JYDriverException ex) 
        { 
            MessageBox.Show(ex.Message); 
            return; 
        }
        timer_FetchData.Enabled = true;
    }

    // 停止按钮
    private void button_stop_Click(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }

    // 窗体关闭时确保资源释放
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (aiTask != null) aiTask.Stop();
    }
}
```

---

## 示例 3：AI 有限采集（多通道）

```csharp
private JY5500AITask aiTask;
private double[,] readValue;   // [采样点, 通道] 列存储

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JY5500AITask(0);

    // 添加多个通道（通道 0、1、2，相同量程）
    int[] channels = { 0, 1, 2 };
    aiTask.AddChannel(channels, -10, 10);

    // 配置有限模式
    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 1000;   // 每通道采集 1000 点
    aiTask.SampleRate = 10000;

    aiTask.Start();

    // 分配多通道缓冲区 [采样点数, 通道数]
    readValue = new double[1000, 3];

    // 等待采集完成（使用 Timer 轮询或 WaitUntilDone）
    // 方式 1：WaitUntilDone 阻塞等待
    // aiTask.WaitUntilDone(-1);

    // 方式 2：Timer 轮询
    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    if (aiTask.AvailableSamples >= (ulong)aiTask.SamplesToAcquire)
    {
        aiTask.ReadData(ref readValue, 1000, -1);
        easyChartX1.Plot(readValue);

        aiTask.Stop();
        aiTask.Channels.Clear();
        timer_FetchData.Enabled = false;
    }
}
```

---

## 示例 4：AI 数字触发连续采集

```csharp
aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 50000;

// 配置数字触发：PFI0 上升沿触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发信号到来后自动开始采集
// 后续读取同连续模式...
```

---

## 示例 5：AI 模拟触发采集

```csharp
aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// 配置模拟触发：AI0 通道，上升沿触发，阈值 2.5V
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;

aiTask.Start();
// 等待触发信号到来后自动开始采集
```

---

## 示例 6：AI 软触发采集

```csharp
aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// 配置软触发
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;

aiTask.Start();

// 在需要时手动发送软触发
// （例如点击按钮后调用）
aiTask.SendSoftwareTrigger();

// 后续读取同连续模式...
```

---

## 示例 7：AI 外部时钟采集

```csharp
aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;

// 使用外部时钟（外部信号从 PFI0 引脚引入）
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10000;  // 告知驱动期望频率

aiTask.Start();
// 后续读取同连续模式...
```

---

## 示例 8：AI 录制模式（数据流写入文件）

```csharp
aiTask = new JY5500AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 50000;

// 配置录制参数
aiTask.Record.FilePath = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;
aiTask.Record.Length = 10.0;   // 录制 10 秒

aiTask.Start();

// 可在录制期间预览数据
double[,] preview = new double[1000, 1];
aiTask.GetRecordPreviewData(ref preview, 1000, -1);
easyChartX1.Plot(preview);

// 等待录制完成
double recordedLen;
bool recordDone;
do
{
    aiTask.GetRecordStatus(out recordedLen, out recordDone);
} while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 9：AO 单点输出

```csharp
JY5500AOTask aoTask = new JY5500AOTask(0);

// 添加通道 0
aoTask.AddChannel(0, -10, 10);

aoTask.Mode = AOMode.Single;
aoTask.Start();

// 输出 5V
aoTask.WriteSinglePoint(5.0, 0);
System.Threading.Thread.Sleep(1000);

// 输出 -5V
aoTask.WriteSinglePoint(-5.0, 0);

aoTask.Stop();
aoTask.Channels.Clear();
```

---

## 示例 10：AO 连续输出（生成正弦波）

```csharp
private JY5500AOTask aoTask;
private double[,] writeValue;

private void button_start_Click(object sender, EventArgs e)
{
    aoTask = new JY5500AOTask(0);
    aoTask.AddChannel(0, -10, 10);

    // 配置连续环形模式
    aoTask.Mode = AOMode.ContinuousWrapping;
    aoTask.UpdateRate = 10000;     // 10 kSa/s

    // 生成正弦波数据（1秒数据，1Hz 频率）
    int samples = 10000;
    writeValue = new double[samples, 1];
    
    for (int i = 0; i < samples; i++)
    {
        double t = (double)i / samples;
        writeValue[i, 0] = 5.0 * Math.Sin(2 * Math.PI * t);  // 5V 幅值正弦波
    }

    // 先写数据，再 Start（Wrapping 模式要求）
    aoTask.WriteData(writeValue, (uint)samples, -1);
    aoTask.Start();
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (aoTask != null)
    {
        aoTask.Stop();
        aoTask.Channels.Clear();
    }
}
```

---

## 示例 11：DI 单点采集

```csharp
// 数字输入（4 端口，每端口 8 线）
var diTask = new JY5500DITask(0);
diTask.AddChannel(0);       // 添加端口 0
diTask.Start();

bool[] readValue = new bool[8];
diTask.ReadSinglePoint(ref readValue, 0);   // 读端口 0 的 8 线状态
Console.WriteLine($"Port 0 Line 0: {readValue[0]}, Line 1: {readValue[1]}");

diTask.Stop();
```

---

## 示例 12：DI 连续采集

```csharp
private JY5500DITask diTask;
private byte[,] dataBufByte;

private void button_start_Click(object sender, EventArgs e)
{
    diTask = new JY5500DITask(0);

    // 添加多个端口
    for (int i = 0; i < 4; i++)
    {
        diTask.AddChannel(i);
    }

    // 配置连续模式
    diTask.Mode = DIMode.Continuous;
    diTask.SampleRate = 10000;
    diTask.Start();

    // 分配缓冲区 [采样点数, 通道数]
    dataBufByte = new byte[1000, 4];
    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;

    if (diTask.AvailableSamples >= (ulong)dataBufByte.GetLength(0))
    {
        diTask.ReadData(ref dataBufByte, (uint)dataBufByte.GetLength(0), -1);
        easyChartX_readData.Plot(dataBufByte, 0, 1, SeeSharpTools.JY.GUI.MajorOrder.Column);
    }

    timer_FetchData.Enabled = true;
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (diTask != null)
    {
        diTask.Stop();
        diTask.Channels.Clear();
    }
}
```

---

## 示例 13：DO 单点输出

```csharp
JY5500DOTask doTask = new JY5500DOTask(0);

// 添加端口 0 和 1
doTask.AddChannel(0);
doTask.AddChannel(1);

doTask.Mode = DOMode.Single;
doTask.Start();

// 输出指定电平（每端口 8 线）
bool[] writeValue0 = { true, false, true, false, true, false, true, false };
bool[] writeValue1 = { false, true, false, true, false, true, false, true };

doTask.WriteSinglePoint(writeValue0, 0);
doTask.WriteSinglePoint(writeValue1, 1);

// 动态更新输出值
writeValue0[0] = false;
doTask.WriteSinglePoint(writeValue0, 0);

doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 14：DO 连续环形输出（Wrapping）

```csharp
private JY5500DOTask doTask;
private byte[,] writeValue;

private void button_start_Click(object sender, EventArgs e)
{
    doTask = new JY5500DOTask(0);
    doTask.AddChannel(0);

    // 配置连续环形模式
    doTask.Mode = DOMode.ContinuousWrapping;
    doTask.UpdateRate = 100000;     // 100 kSa/s

    // 生成数字波形数据（1秒数据）
    writeValue = new byte[100000, 1];

    // 生成 50% 占空比的方波，频率 1kHz
    int oneCyclePoints = 100;  // 100000 / 1000 = 100 points/cycle
    int highLevelPoints = 50;  // 50% 占空比

    for (int i = 0; i < 1000; i++)  // 1000 个周期
    {
        for (int j = 0; j < oneCyclePoints; j++)
        {
            if (j < highLevelPoints)
                writeValue[i * oneCyclePoints + j, 0] = 0xFF;  // 8 线都高
            else
                writeValue[i * oneCyclePoints + j, 0] = 0x0;   // 8 线都低
        }
    }

    // 先写数据，再 Start（Wrapping 模式要求）
    doTask.WriteData(writeValue, (uint)writeValue.GetLength(0), -1);
    doTask.Start();
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (doTask != null)
    {
        doTask.Stop();
        doTask.Channels.Clear();
    }
}
```

---

## 示例 15：CI 边沿计数

```csharp
private JY5500CITask ciTask;

private void button_start_Click(object sender, EventArgs e)
{
    // 计数器通道 0
    ciTask = new JY5500CITask(0, 0);
    ciTask.Mode = CIMode.Single;
    ciTask.Type = CIType.EdgeCounting;
    
    // 配置边沿计数
    ciTask.EdgeCounting.Direction = CountDirection.Up;
    ciTask.EdgeCounting.InitialCount = 0;
    
    ciTask.Start();
    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    uint count = 0;
    ciTask.ReadSinglePoint(ref count);
    textBox_count.Text = count.ToString();
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (ciTask != null) ciTask.Stop();
}
```

---

## 示例 16：CI 频率测量

```csharp
ciTask = new JY5500CITask(0, 0);
ciTask.Mode = CIMode.Single;
ciTask.Type = CIType.Frequency;

ciTask.Start();

double freq;
ciTask.ReadSinglePoint(ref freq, -1);  // 阻塞等待测量完成
Console.WriteLine($"频率: {freq:F2} Hz");

ciTask.Stop();
```

---

## 示例 17：CO 脉冲输出

```csharp
private JY5500COTask coTask;

private void button_start_Click(object sender, EventArgs e)
{
    coTask = new JY5500COTask(0, 0);
    coTask.Mode = COMode.Single;
    coTask.IdleState = COIdleState.LowLevel;
    coTask.InitialDelay = 0;

    // 输出 1kHz，占空比 50%，100 个脉冲
    var pulse = new COPulse(
        COPulseType.DutyCycleFrequency,
        1000.0,    // 频率 1kHz
        0.5,       // 占空比 50%
        100        // 100 个脉冲
    );
    
    coTask.WriteSinglePoint(pulse);
    coTask.Start();
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (coTask != null) coTask.Stop();
}
```

---

## 示例 18：CO 连续脉冲输出

```csharp
coTask = new JY5500COTask(0, 0);
coTask.Mode = COMode.ContinuousWrapping;
coTask.IdleState = COIdleState.LowLevel;

// 无限脉冲（count = -1）
var pulse = new COPulse(
    COPulseType.DutyCycleFrequency,
    1000.0,    // 频率 1kHz
    0.5,       // 占空比 50%
    -1         // 无限循环
);

coTask.WriteSinglePoint(pulse);
coTask.Start();

// 运行中可更新参数
coTask.WriteSinglePoint(new COPulse(
    COPulseType.DutyCycleFrequency, 2000.0, 0.3, -1));
```

---

## 示例 19：Record 有限录制模式（Finite Streaming）

录制固定时长的数据到文件，录制完成后自动停止：

```csharp
private JY5500AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5500AITask(0);
        
        // 添加通道
        aiTask.AddChannel(0, -10, 10);
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;
        
        // 配置录制参数
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 10.0;              // 录制 10 秒
        
        aiTask.Start();
        
        previewData = new double[1000, aiTask.Channels.Count];
        timer_Preview.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    // 检查录制状态
    double recordedLength;
    bool recordDone;
    aiTask.GetRecordStatus(out recordedLength, out recordDone);
    
    // 录制完成，自动停止
    if (recordDone)
    {
        try { if (aiTask != null) aiTask.Stop(); }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
        
        aiTask.Channels.Clear();
        timer_Preview.Enabled = false;
        MessageBox.Show("录制完成！");
    }
    else
    {
        // 录制中，预览数据
        if (aiTask.AvailableSamples >= 1000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
        }
    }
    
    timer_Preview.Enabled = true;
}
```

---

## 示例 20：Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用 Stop 停止：

```csharp
private JY5500AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5500AITask(0);
        
        // 添加多通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, -10, 10);
        }
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;  // 50 kS/s
        
        // 配置无限录制
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制模式
        
        aiTask.Start();
        
        previewData = new double[1000, aiTask.Channels.Count];
        timer_Preview.Enabled = true;
        
        button_Start.Enabled = false;
        button_Stop.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    // 无限模式下只读取预览数据，不检查录制状态
    if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
    }
    
    timer_Preview.Enabled = true;
}

private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    try 
    { 
        if (aiTask != null) 
        { 
            aiTask.Stop(); 
            aiTask.Channels.Clear(); 
        } 
    }
    catch (JYDriverException ex) 
    { 
        MessageBox.Show(ex.Message); 
    }
    
    button_Start.Enabled = true;
    button_Stop.Enabled = false;
}
```

---

## 综合技巧

### 多通道数据解析

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[1000, 3];   // 1000 点，3 通道
aiTask.ReadData(ref buf, 1000, -1);

// 提取各通道数据
double[] ch0 = new double[1000];
double[] ch1 = new double[1000];
for (int i = 0; i < 1000; i++)
{
    ch0[i] = buf[i, 0];
    ch1[i] = buf[i, 1];
}
```

### 量程选择

```csharp
// 量程选择原则：选择略大于信号峰值的量程，提高精度
// JY5500 支持：±10V / ±5V / ±2V / ±1V / ±0.5V / ±0.2V / ±0.1V

aiTask.AddChannel(0, -5, 5);   // ±5V 量程
aiTask.AddChannel(1, -0.1, 0.1);   // ±0.1V 量程（小信号高精度）
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timerFetch != null) timerFetch.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
        if (aoTask != null) { aoTask.Stop(); aoTask.Channels.Clear(); }
        if (diTask != null) { diTask.Stop(); diTask.Channels.Clear(); }
        if (doTask != null) { doTask.Stop(); doTask.Channels.Clear(); }
        if (ciTask != null) ciTask.Stop();
        if (coTask != null) coTask.Stop();
    }
    catch (JYDriverException ex) 
    { 
        MessageBox.Show(ex.Message); 
    }
}
```

### 录制数据回放

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    if (!File.Exists(fileBrowser.FileName)) { MessageBox.Show("文件不存在"); return; }
    
    // JY5500 数据格式为 double
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 4;  // 录制时通道数
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    playbackData = new double[1000, channelCount];
    
    button_OpenFile.Enabled = false;
    button_Playback.Enabled = true;
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        for (int i = 0; i < playbackData.GetLength(0); i++)
            for (int ch = 0; ch < playbackData.GetLength(1); ch++)
                if (playbackStream.Position < playbackStream.Length)
                    playbackData[i, ch] = playbackReader.ReadDouble();
    }
    catch (EndOfStreamException)
    {
        timer_Playback.Enabled = false;
        playbackReader.Close();
        playbackStream.Close();
        MessageBox.Show("回放完成");
    }
}
```
