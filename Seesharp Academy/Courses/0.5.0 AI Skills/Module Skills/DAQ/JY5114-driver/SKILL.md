---
name: jy5114-driver
description: 提供 JYTEK JY5114 系列（JY5114-H7）多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、模拟输出（AO）单点/有限/连续输出、数字输入/输出（DI/DO）按 line 操作的单点/有限/连续模式、计数器输入/输出（CI/CO）边沿计数/频率/周期/脉冲/编码器测量/脉冲生成、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、多卡同步（PXI_Trig0~7）、录制模式（Finite/Infinite Streaming）。当用户使用 PCIe-5114-H7、PXIe-5114-H7、JY5114AITask、JY5114AOTask、JY5114DITask、JY5114DOTask、JY5114CITask、JY5114COTask、AIMode、AOMode.ContinuousWrapping、AIBandWidth.Narrow/Medium/Wide、AITerminal.RSE/Differential 开发高通道密度数据采集、信号发生、数字 IO 控制、计数/定时测量、多卡同步、流盘录波等自动化测试应用时自动应用。
---

# JY5114 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5114\Bin\JY5114.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5114\Bin\JY5114.xml`（IntelliSense 支持）
- **目标框架**：.NET Framework 4.x（x64 或 AnyCPU）
- **命名空间**：`using JY5114;`
- **辅助组件**：`SeeSharpTools.JY.DSP.Fundamental`（波形生成）、`SeeSharpTools.JY.GUI`（EasyChartX 波形控件）
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等
- **范例工程**：`D:\JYTEK_Work\Examples\C#\DAQ\JY5114.Examples`

## 硬件规格速查

| 功能 | 规格 |
|---|---|
| AI 分辨率 | 16-bit |
| AI 通道数 | 96 单端 / 48 差分 |
| AI 最大采样率 | 1 MS/s/n（n = 1..96，多通道共享） |
| AI 输入量程 | ±10V / ±5V / ±2.5V |
| AI 精度（时基） | 470 ppm |
| AI 输入阻抗 | >1 MΩ \|\| 330 pF |
| AI CMRR | 85 dB |
| AI 通道间串扰 | −80 dB |
| AI DNL / INL | No Missing Code / 70 ppm |
| AI 过压保护 | ±15 V |
| AI 板载内存 | 256 MB |
| AI 抗混叠带宽档位 | `AIBandWidth.Narrow`（15 kHz）/ `Medium`（39 kHz）/ `Wide`（375 kHz） |
| AO 分辨率 | 16-bit |
| AO 通道数 | 2 通道单端 |
| AO 最大更新率 | 2 MS/s（每通道） |
| AO 输出量程 | ±10V |
| AO 板载内存 | 64 MB |
| DIO 通道 | 24 线（其中 16 线硬件定时 10 MHz） |
| 计数器 | 2 个 32-bit 通用计数器 |
| 触发方式 | Immediate / Digital / Analog / Soft / MultichannelAnalog |
| 接口 | PCIe / PXIe |

### 产品型号

- **PCIe-5114-H7** / **PXIe-5114-H7**：96-ch AI (16-bit, 1 MS/s/n)，2-ch AO，24 DIO，2 CTR，PCIe/PXIe 高通道密度多功能 I/O 模块

> `AIBandWidth` 枚举值为 `Narrow / Medium / Wide`，对应 15 kHz / 39 kHz / 375 kHz 抗混叠带宽。

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY5114AITask(0);                                // 1. 按槽位号创建
task.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide); // 2. 添加通道（必须指定终端+带宽）
task.Mode = AIMode.Continuous;                                  // 3. 配置
task.SampleRate = 10000;
task.Start();                                                   // 4. 启动
// ... 读取数据 ...
task.Stop();                                                    // 5. 停止
task.Channels.Clear();                                          // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY5114AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Single / Finite / Continuous / Record   |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s）                    |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数             |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（非 Single 模式）  |
| `TransferedSamples`  | `ulong`                 | 已传输点数（非 Single 模式）            |
| `SampleClock`        | `AISampleClock`         | 时钟配置对象（Internal / External）     |
| `Trigger`            | `AITrigger`             | 触发配置对象                            |
| `Record`             | `AIRecord`              | 录制配置对象                            |
| `SignalExport`       | `AISignalExport`        | 信号导出（同步用）                      |
| `Channels`           | `List<AIChannel>`       | 已添加的通道列表                        |

#### AddChannel 重载

```csharp
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh, AITerminal terminal, AIBandWidth bandwidth);

// 多通道（相同量程）
aiTask.AddChannel(int[] chnsId, double rangeLow, double rangeHigh, AITerminal terminal, AIBandWidth bandwidth);

// 多通道（各自量程）
aiTask.AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh, AITerminal terminal, AIBandWidth bandwidth);

// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2.5    rangeHigh: 10 / 5 / 2.5
// AITerminal.RSE → 通道编号 0~95；AITerminal.Differential → 通道编号 0~47
```

### AITerminal 枚举

| 值             | 说明             |
| -------------- | ---------------- |
| `RSE`          | 参考单端模式     |
| `Differential` | 差分模式         |

> JY5114 不提供 NRSE。

### AIBandWidth 枚举

| 值       | 抗混叠带宽  | 典型场景                   |
| -------- | ----------- | -------------------------- |
| `Narrow` | 15 kHz      | 低频/工频信号降噪         |
| `Medium` | 39 kHz      | 中频信号                   |
| `Wide`   | 375 kHz     | 全带宽，用于宽带信号       |

#### 读取数据重载

```csharp
// Single 模式
aiTask.ReadSinglePoint(ref double readValue, int channel);
aiTask.ReadSinglePoint(ref double[] readValue);      // 读所有通道

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);

// Finite/Continuous — 多通道（列存储，每列一个通道）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);

// timeout = -1 → 永久等待
```

#### AI 四种模式速查

| 模式         | 典型配置                       | 读取方式                                     |
| ------------ | ------------------------------ | -------------------------------------------- |
| `Single`     | `Mode=AIMode.Single`           | 轮询 `ReadSinglePoint`                       |
| `Finite`     | `+SamplesToAcquire=N`          | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                  | Timer 轮询 `AvailableSamples` → `ReadData`   |
| `Record`     | `+Record.FilePath/Mode/Length` | `GetRecordPreviewData` 预览，数据写入文件    |

#### 连续采集 Timer 模式（最常用）

```csharp
timer.Enabled = false;
if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
{
    aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
    easyChartX1.Plot(readBuffer);
}
timer.Enabled = true;
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // PFI0~PFI23 / PXI_Trig0~7
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;     // Rising / Falling
```

#### 模拟触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;   // Channel_0 ~ Channel_95
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge; // Edge / Hysteresis / Window
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
aiTask.SendSoftwareTrigger();
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10000;
```

### 多卡同步

JY5114 支持多卡采样时钟 + 触发总线同步，信号通过 PXI_Trig0~7 在背板传递。

#### AI 多卡连续同步采集

```csharp
// ===== 主卡配置（Slot 2）=====
var masterTask = new JY5114AITask(2);
masterTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
masterTask.Mode = AIMode.Continuous;
masterTask.SampleRate = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟 + 启动触发
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 4）=====
var slaveTask = new JY5114AITask(4);
slaveTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
slaveTask.Mode = AIMode.Continuous;

// 使用外部时钟（接收主卡的 PXI_Trig0）
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10000;

// 接收主卡的启动触发
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();
masterTask.Start();
```

#### AI Reference 触发（预触发）多卡同步

```csharp
masterTask.Trigger.Mode = AITriggerMode.Reference;
masterTask.Trigger.PreTriggerSamples = 1000;   // 必须 ≤ SamplesToAcquire
masterTask.Trigger.Type = AITriggerType.Digital;
masterTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
masterTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 主卡通过 PXI_Trig1 把自身 ReferenceTrig 导出给从卡
masterTask.SignalExport.Add(AISignalExportSource.ReferenceTrig, SignalExportDestination.PXI_Trig1);
```

> 启动顺序：**先从卡后主卡**；关闭顺序反之（先主后从）。
> `SignalExport.Add(...)` 可重复调用以导出多个信号。

---

## 模拟输出（AO）

### 任务类：`JY5114AOTask`

#### 关键属性

| 属性                    | 类型                  | 说明                                                            |
| ----------------------- | --------------------- | --------------------------------------------------------------- |
| `Mode`                  | `AOMode`              | Single / Finite / ContinuousWrapping / ContinuousNoWrapping     |
| `UpdateRate`            | `double`              | 更新率（Sa/s），最高 2 MS/s                                     |
| `SamplesToUpdate`       | `int`                 | Finite 模式每通道输出点数                                       |
| `BufLenInSamples`       | `int`                 | 缓冲区大小（每通道样本数）                                      |
| `AvaliableLenInSamples` | `int`                 | 可写入的样本数                                                  |
| `TransferedSamples`     | `ulong`               | 已传输样本数                                                    |
| `SampleClock`           | `AOSampleClock`       | 时钟配置                                                        |
| `Trigger`               | `AOTrigger`           | 触发配置                                                        |
| `SignalExport`          | `AOSignalExport`      | 信号导出                                                        |

#### AddChannel 方法

```csharp
void AddChannel(int chnId)        // 通道 0~1，输出量程固定 ±10V
void AddChannel(int[] chnsId)
// chnId = -1 → 添加全部 2 通道
```

> 注：JY5114 AO `AddChannel` 不传量程参数（硬件固定 ±10V 单端输出）。

#### 写入数据

```csharp
// Single 模式
aoTask.WriteSinglePoint(double writeValue, int channel);
aoTask.WriteSinglePoint(double[] writeValue);     // 所有通道同时写

// Continuous/Finite — 多通道（列存储）
aoTask.WriteData(double[,] buf, int timeout);

// Continuous/Finite — 单通道
aoTask.WriteData(double[] buf, int timeout);
```

#### AO 连续环形输出标准流程

```csharp
aoTask = new JY5114AOTask(0);
aoTask.AddChannel(0);                             // ±10V 固定量程
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 10000;

// 生成波形数据
double[] writeBuf = new double[1000];
for (int i = 0; i < 1000; i++)
    writeBuf[i] = 5.0 * Math.Sin(2 * Math.PI * i / 1000);

// 先写数据再 Start（Wrapping 模式要求）
aoTask.WriteData(writeBuf, -1);
aoTask.Start();
```

### AOMode 枚举

| 值                      | 说明                             |
| ---------------------- | -------------------------------- |
| `Single`               | 写入寄存器，立即输出             |
| `Finite`               | 输出固定点数后停止               |
| `ContinuousWrapping`   | 循环输出固定缓冲区               |
| `ContinuousNoWrapping` | 持续消耗缓冲区数据，可实时追加   |

### AOTriggerType 枚举

| 值          | 说明        |
| ----------- | ----------- |
| `Immediate` | 立即触发    |
| `Digital`   | 数字触发    |
| `Soft`      | 软件触发    |

---

## 数字输入（DI）

### 任务类：`JY5114DITask`

> JY5114 DI/DO 采用 **按 line 操作**。共 24 条数字线，其中 16 条支持硬件定时（10 MHz）。

#### 关键属性

| 属性                 | 类型                  | 说明                                    |
| -------------------- | --------------------- | --------------------------------------- |
| `Mode`               | `DIMode`              | Single / Finite / Continuous            |
| `SampleRate`         | `double`              | 采样率（Sa/s）                          |
| `SamplesToAcquire`   | `int`                 | Finite 模式每通道采集点数               |
| `AvailableSamples`   | `ulong`               | 缓冲区可读点数                          |
| `SampleClock`        | `DISampleClock`       | 时钟配置                                |
| `Trigger`            | `DITrigger`           | 触发配置                                |
| `SignalExport`       | `DISignalExport`      | 信号导出                                |

#### AddChannel 方法

```csharp
void AddChannel(int lineId)         // 添加单条数字线
void AddChannel(int[] lineIds)
// lineId = -1 → 添加全部已使能的数字线
```

#### 读取数据

```csharp
// Single — 读取指定 line
bool readValue = false;
diTask.ReadSinglePoint(ref readValue, lineId);

// Single — 读取所有已添加 line
bool[] readValues = new bool[24];
diTask.ReadSinglePoint(ref readValues);

// Finite / Continuous
diTask.ReadData(ref short[] buf, uint samplesPerChannel, int timeout);
diTask.ReadData(ref byte[]  buf, uint samplesPerChannel, int timeout);
diTask.ReadData(ref byte[,] buf, uint samplesPerChannel, int timeout);   // 多通道（列存储）
```

---

## 数字输出（DO）

### 任务类：`JY5114DOTask`

#### 关键属性

| 属性                    | 类型                  | 说明                                                            |
| ----------------------- | --------------------- | --------------------------------------------------------------- |
| `Mode`                  | `DOMode`              | Single / Finite / ContinuousWrapping / ContinuousNoWrapping     |
| `UpdateRate`            | `double`              | 更新率（Sa/s）                                                  |
| `SamplesToUpdate`       | `int`                 | Finite 模式输出点数                                             |
| `BufLenInSamples`       | `int`                 | 缓冲区大小                                                      |
| `AvaliableLenInSamples` | `int`                 | 可写入样本数                                                    |
| `SampleClock`           | `DOSampleClock`       | 时钟配置                                                        |
| `Trigger`               | `DOTrigger`           | 触发配置                                                        |
| `SignalExport`          | `DOSignalExport`      | 信号导出                                                        |

#### AddChannel 方法

```csharp
void AddChannel(int lineId)
void AddChannel(int[] lineIds)
// lineId = -1 → 添加全部已使能的数字线
```

#### 写入数据

```csharp
// Single
doTask.WriteSinglePoint(bool value, int lineId);
doTask.WriteSinglePoint(bool[] values);       // 写所有已添加 line

// Finite / Continuous
doTask.WriteData(ushort[] buf, uint samplesPerChannel, int timeout);
doTask.WriteData(byte[]  buf, uint samplesPerChannel, int timeout);
doTask.WriteData(byte[,] buf, uint samplesPerChannel, int timeout);   // 多通道
```

---

## 计数器输入（CI）

### 任务类：`JY5114CITask`

**构造**：`new JY5114CITask(int slotNumber, int counterId)` — counterId 取 `0` 或 `1`（共 2 个 32 位计数器）。

#### CI 测量类型

| CIType              | 说明                |
| ------------------- | ------------------- |
| `EdgeCounting`      | 边沿计数            |
| `Frequency`         | 频率测量（Hz）      |
| `Period`            | 周期测量（秒）      |
| `Pulse`             | 脉冲宽度测量        |
| `TwoEdgeSeparation` | 两边沿间隔测量      |
| `QuadEncoder`       | 正交编码器          |
| `TwoPulseEncoder`   | 双脉冲编码器        |
| `SemiPeriod`        | 半周期测量          |

#### 关键属性（按测量类型选择相应子对象）

```csharp
ciTask.Mode = CIMode.Single;         // Single / Finite / Continuous
ciTask.Type = CIType.EdgeCounting;

// 边沿计数专用
ciTask.EdgeCounting.Direction      = CountDirection.Up;       // Up / Down / External
ciTask.EdgeCounting.InitialCount   = 0;
ciTask.EdgeCounting.OutEvent.Threshold = 100;                 // 达到阈值输出信号
ciTask.EdgeCounting.OutEvent.IdleState = EdgeCntOunEvtIdleState.LowLevel;
ciTask.EdgeCounting.Pause.ActivePolarity = LevelPolarity.HighLevel;
ciTask.EdgeCounting.Pause.Terminal       = InputTerminal.PFI0;
```

#### 读取方法

```csharp
// EdgeCounting / QuadEncoder / TwoPulseEncoder
void ReadSinglePoint(ref uint count);
void ReadSinglePoint(ref int  count);
void ReadSinglePoint(ref uint[]   count, int samplesToRead, int timeout);
void ReadSinglePoint(ref uint[,]  count, int samplesToRead, int timeout);

// Frequency / Period / SemiPeriod
void ReadSinglePoint(ref double measurement, int timeout);
void ReadSinglePoint(ref double[]  m, int samplesToRead, int timeout);
void ReadSinglePoint(ref double[,] m, int samplesToRead, int timeout);

// Pulse / TwoEdgeSeparation
void ReadSinglePoint(ref double m1, ref double m2, int timeout);
void ReadSinglePoint(ref double[]  m1, ref double[]  m2, int samplesToRead, int timeout);
void ReadSinglePoint(ref double[,] m1, ref double[,] m2, int samplesToRead, int timeout);
```

---

## 计数器输出（CO）

### 任务类：`JY5114COTask`

**构造**：`new JY5114COTask(int slotNumber, int counterId)` — counterId 取 `0` 或 `1`。

```csharp
coTask = new JY5114COTask(0, 0);
coTask.Mode         = COMode.Single;
coTask.IdleState    = COIdleState.LowLevel;
coTask.InitialDelay = 0;

// 输出频率 1 kHz、占空比 50%、共 100 个脉冲
var pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, 100);
coTask.WriteSinglePoint(pulse);
coTask.Start();
coTask.WaitUntilDone(-1);
coTask.Stop();
```

### COPulseType 三种构造方式

| 类型                 | param1            | param2            |
| -------------------- | ----------------- | ----------------- |
| `DutyCycleFrequency` | 频率（Hz）        | 占空比（0~1）     |
| `HighLowTime`        | 高电平时长（秒）  | 低电平时长（秒）  |
| `HighLowTick`        | 高电平 Tick 数    | 低电平 Tick 数    |

`count = -1` 表示无限循环输出。

### COIdleState 枚举

| 值          | 说明             |
| ----------- | ---------------- |
| `LowLevel`  | 空闲为低电平     |
| `HighLevel` | 空闲为高电平     |

---

## 录制模式（Record）

将 AI 数据流式写入文件，支持边录边预览：

```csharp
aiTask = new JY5114AITask(0);
aiTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 500000;

aiTask.Record.FilePath   = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;            // 仅支持 Bin
aiTask.Record.Mode       = RecordMode.Finite;         // Finite / Infinite
aiTask.Record.Length     = 10.0;                      // 录制时长（秒），Finite 有效

aiTask.Start();

// 预览
double[,] preview = new double[1000, 1];
aiTask.GetRecordPreviewData(ref preview, 1000, -1);
easyChartX1.Plot(preview);

// 等待录制完成
double recordedLen; bool recordDone;
do { aiTask.GetRecordStatus(out recordedLen, out recordDone); } while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

### AIRecord 配置属性

| 属性         | 类型          | 说明                             |
| ------------ | ------------- | -------------------------------- |
| `FilePath`   | `string`      | 录制文件路径                     |
| `FileFormat` | `FileFormat`  | 文件格式（仅 `Bin`）             |
| `Mode`       | `RecordMode`  | `Finite` / `Infinite`            |
| `Length`     | `double`      | 录制时长（秒），`Finite` 时有效  |

---

## 常见错误处理

| 异常代码                                     | 原因                         | 处理建议                        |
| -------------------------------------------- | ---------------------------- | ------------------------------- |
| `OpenDeviceFailed`                           | 板卡未连接或槽位号错误       | 检查 JYTEK 设备管理器槽位号     |
| `NoChannelAdded`                             | 未调用 AddChannel 就 Start   | 在 Start 前添加通道             |
| `BufferDataOverflow`                         | 读取速度慢于采集速度         | 增大读取频率 / 降低采样率       |
| `ReadDataTimeout`                            | timeout 内未读到足够数据     | 增大 timeout 或检查采样率       |
| `TaskHasStartedCannotPerformTheSetOperation` | Task 运行中修改参数          | Stop 后再修改                   |
| `SampleRateParameterInvalid`                 | 采样率超过硬件上限           | 全部通道共享 1 MS/s，逐通道均分 |
| `ChannelInputRangeParameterInvalid`          | 无效输入量程                 | 仅支持 ±10/±5/±2.5 V            |
| `SignalExportDestinationInvalid`             | 导出目的地非法               | 确认 PXI_Trig0~7 或 PFI 可用性  |

---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY5114;
// DLL：C:\SeeSharp\JYTEK\Hardware\DAQ\JY5114\Bin\JY5114.dll
```

## JY5114AITask — 模拟输入任务

### 构造函数

```csharp
new JY5114AITask(int slotNumber)
new JY5114AITask(string deviceName, int slotNumber)    // 指定设备名
```

### 主要属性

| 属性                  | 类型                | 说明                                    |
| --------------------- | ------------------- | --------------------------------------- |
| `Mode`                | `AIMode`            | Single / Finite / Continuous / Record   |
| `SampleRate`          | `double`            | 每通道采样率（Sa/s）                    |
| `SamplesToAcquire`    | `int`               | Finite 模式采集点数/通道                |
| `SamplesToRead`       | `int`               | 每次读取默认点数                        |
| `BufLenInSamples`     | `int`               | 缓冲区大小                              |
| `AvailableSamples`    | `ulong`             | 可读点数                                |
| `TransferedSamples`   | `ulong`             | 已传输点数                              |
| `SampleClock`         | `AISampleClock`     | 时钟                                    |
| `Trigger`             | `AITrigger`         | 触发                                    |
| `Record`              | `AIRecord`          | 录制                                    |
| `SignalExport`        | `AISignalExport`    | 信号导出                                |
| `Channels`            | `List<AIChannel>`   | 通道列表                                |

### 方法

```csharp
// 添加通道（必须指定 AITerminal + AIBandWidth）
void AddChannel(int chnId,    double rangeLow,  double rangeHigh, AITerminal terminal, AIBandWidth bandwidth);
void AddChannel(int[] chnsId, double rangeLow,  double rangeHigh, AITerminal terminal, AIBandWidth bandwidth);
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh, AITerminal terminal, AIBandWidth bandwidth);

// 控制
void Start();
void Stop();
void WaitUntilDone(int timeout);          // 仅 Finite 有效
void SendSoftwareTrigger();

// 读取数据
void ReadSinglePoint(ref double readValue, int channel);
void ReadSinglePoint(ref double[] readValues);
void ReadData(ref double[]  buf, int samplesPerChannel, int timeout);
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout);

// 录制模式
void GetRecordPreviewData(ref double[]  buf, int samplesPerChannel, int timeout);
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout);
void GetRecordStatus(out double recordedLength, out bool recordDone);
```

## AIMode 枚举

| 值           | 说明                         |
| ------------ | ---------------------------- |
| `Single`     | 寄存器单点读取               |
| `Finite`     | 有限点数采集                 |
| `Continuous` | 连续采集                     |
| `Record`     | 流式写入文件，支持预览       |

## AITriggerType 枚举

| 值                   | 说明              |
| -------------------- | ----------------- |
| `Immediate`          | 立即触发（默认） |
| `Digital`            | 数字边沿触发      |
| `Analog`             | 模拟边沿/窗口触发 |
| `Soft`               | 软件触发          |
| `MultichannelAnalog` | 多通道模拟触发    |

## AITriggerMode 枚举

| 值          | 说明          |
| ----------- | ------------- |
| `Start`     | 开始触发      |
| `Reference` | 参考（预触发） |

## AITrigger 主要属性

| 属性                 | 类型                              | 说明                                          |
| -------------------- | --------------------------------- | --------------------------------------------- |
| `Type`               | `AITriggerType`                   | 触发类型                                      |
| `Mode`               | `AITriggerMode`                   | Start / Reference                             |
| `PreTriggerSamples`  | `uint`                            | 预触发样本数（Reference 有效，≤ SamplesToAcquire） |
| `ReTriggerCount`     | `int`                             | 重复触发次数（-1 无限）                       |
| `Digital`            | `AIDigitalTrigger`                | 数字触发子对象                                |
| `Analog`             | `AIAnalogTrigger`                 | 模拟触发子对象                                |

### 数字触发

```csharp
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // PFI0~PFI23、PXI_Trig0~7
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;   // Rising / Falling
```

### 模拟触发

```csharp
aiTask.Trigger.Analog.Source     = AIAnalogTriggerSource.Channel_0;         // Channel_0 ~ Channel_95
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;          // Edge / Hysteresis / Window

// Edge 模式
aiTask.Trigger.Analog.Edge.Slope     = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;

// Hysteresis 模式
aiTask.Trigger.Analog.Hysteresis.Slope         = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 3.0;
aiTask.Trigger.Analog.Hysteresis.LowThreshold  = 1.0;

// Window 模式
aiTask.Trigger.Analog.Window.Condition     = AIAnalogWindowCondition.Entering;   // Entering / Leaving
aiTask.Trigger.Analog.Window.HighThreshold = 5.0;
aiTask.Trigger.Analog.Window.LowThreshold  = -5.0;
```

## ClockTerminal 枚举（节选）

| 值                        | 说明                    |
| ------------------------- | ----------------------- |
| `PFI0` ~ `PFI23`          | 前面板 PFI 引脚（24 条）|
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 背板触发总线        |
| `AI_SampleClock`          | AI 采样时钟       |
| `AO_SampleClock`          | AO 采样时钟       |
| `DI_SampleClock`          | DI 采样时钟       |
| `DO_SampleClock`          | DO 采样时钟       |

## SignalExportDestination 枚举（节选）

| 值                        | 说明                         |
| ------------------------- | ---------------------------- |
| `PFI0` ~ `PFI23`          | 导出到前面板 PFI 引脚（24 条）|
| `PXI_Trig0` ~ `PXI_Trig7` | 导出到 PXI 背板触发总线      |

## AISignalExportSource 枚举

| 值               | 说明               |
| ---------------- | ------------------ |
| `SampleClock`    | 采样时钟           |
| `StartTrig`      | 启动触发信号       |
| `ReferenceTrig`  | 参考触发（预触发） |

## AIRecord 属性

| 属性         | 类型         | 说明                         |
| ------------ | ------------ | ---------------------------- |
| `FilePath`   | `string`     | 录制文件路径                 |
| `FileFormat` | `FileFormat` | `Bin`                        |
| `Mode`       | `RecordMode` | `Finite` / `Infinite`        |
| `Length`     | `double`     | 录制时长（秒），Finite 有效  |

## JY5114AOTask — 模拟输出任务

### 构造与属性

```csharp
new JY5114AOTask(int slotNumber)
new JY5114AOTask(string deviceName, int slotNumber)
```

| 属性                    | 类型              | 说明                                                    |
| ----------------------- | ----------------- | ------------------------------------------------------- |
| `Mode`                  | `AOMode`          | Single/Finite/ContinuousWrapping/ContinuousNoWrapping   |
| `UpdateRate`            | `double`          | 更新率（Sa/s），最高 2 MS/s                             |
| `SamplesToUpdate`       | `int`             | Finite 每通道输出点数                                   |
| `BufLenInSamples`       | `int`             | 缓冲区大小                                              |
| `AvaliableLenInSamples` | `int`             | 可写入样本数                                            |
| `TransferedSamples`     | `ulong`           | 已传输样本数                                            |
| `CompleteState`         | `bool`            | 完成状态                                                |
| `SampleClock`           | `AOSampleClock`   | 时钟                                                    |
| `Trigger`               | `AOTrigger`       | 触发                                                    |
| `SignalExport`          | `AOSignalExport`  | 信号导出                                                |

### 方法

```csharp
void AddChannel(int chnId);            // 0~1；-1=全部
void AddChannel(int[] chnsId);

void Start();  void Stop();
void WaitUntilDone(int timeout);

// Single
void WriteSinglePoint(double value, int channel);
void WriteSinglePoint(double[] values);

// Continuous / Finite
void WriteData(double[]  buf, int timeout);
void WriteData(double[,] buf, int timeout);
```

## JY5114DITask — 数字输入任务

```csharp
new JY5114DITask(int slotNumber)

void AddChannel(int lineId);           // -1 添加全部已使能线
void AddChannel(int[] lineIds);

void Start();  void Stop();

// Single
void ReadSinglePoint(ref bool readValue, int lineId);
void ReadSinglePoint(ref bool[] readValues);

// Finite / Continuous
void ReadData(ref short[] buf, uint samplesPerChannel, int timeout);
void ReadData(ref byte[]  buf, uint samplesPerChannel, int timeout);
void ReadData(ref byte[,] buf, uint samplesPerChannel, int timeout);
```

### DIMode 枚举

| 值           | 说明       |
| ------------ | ---------- |
| `Single`     | 单点       |
| `Finite`     | 有限点数   |
| `Continuous` | 连续采集   |

## JY5114DOTask — 数字输出任务

```csharp
new JY5114DOTask(int slotNumber)

void AddChannel(int lineId);
void AddChannel(int[] lineIds);

void Start();  void Stop();  void WaitUntilDone(int timeout);

// Single
void WriteSinglePoint(bool value, int lineId);
void WriteSinglePoint(bool[] values);

// Continuous / Finite
void WriteData(ushort[] buf, uint samplesPerChannel, int timeout);
void WriteData(byte[]   buf, uint samplesPerChannel, int timeout);
void WriteData(byte[,]  buf, uint samplesPerChannel, int timeout);
```

### DOMode 枚举

| 值                     | 说明             |
| ---------------------- | ---------------- |
| `Single`               | 立即输出         |
| `Finite`               | 有限点数输出     |
| `ContinuousWrapping`   | 循环输出         |
| `ContinuousNoWrapping` | 连续消耗缓冲区   |

## JY5114CITask — 计数器输入任务

```csharp
new JY5114CITask(int slotNumber, int counterId)      // counterId: 0 或 1
new JY5114CITask(string deviceName, int counterId)
```

### 主要属性

| 属性                | 类型                  | 说明                    |
| ------------------- | --------------------- | ----------------------- |
| `Mode`              | `CIMode`              | Single/Finite/Continuous|
| `Type`              | `CIType`              | 测量类型                |
| `SampleClock`       | `CISampleClock`       | 时钟（Finite/Continuous）|
| `SamplesToAcquire`  | `int`                 | Finite 采集点数         |
| `AvailableSamples`  | `ulong`               | 可读点数                |
| `EdgeCounting`      | `EdgeCounting`      | 边沿计数子对象          |
| `FrequencyMeas`     | `FrequencyMeas`     | 频率测量                |
| `PeriodMeas`        | `PeriodMeas`        | 周期测量                |
| `PulseMeas`         | `PulseMeas`         | 脉冲测量                |
| `TwoEdgeSeparation` | `TwoEdgeSeparation` | 两边沿间隔测量          |
| `QuadEncoder`       | `QuadEncoder`       | 正交编码器              |
| `TwoPulseEncoder`   | `TwoPulseEncoder`   | 双脉冲编码器            |
| `SemiPeriodMeas`    | `SemiPeriodMeas`    | 半周期测量              |

### EdgeCounting 子对象字段

```csharp
ciTask.EdgeCounting.Direction         // CountDirection.Up / Down / External
ciTask.EdgeCounting.InitialCount
ciTask.EdgeCounting.OutEvent.Threshold
ciTask.EdgeCounting.OutEvent.IdleState   // EdgeCntOunEvtIdleState.LowLevel / HighLevel
ciTask.EdgeCounting.Pause.ActivePolarity // LevelPolarity.HighLevel / LowLevel / None
ciTask.EdgeCounting.Pause.Terminal       // InputTerminal.PFI0 ~ PFI23 / PXI_Trig0~7
```

### 读取重载

```csharp
void ReadSinglePoint(ref uint count);
void ReadSinglePoint(ref int  count);
void ReadSinglePoint(ref uint[]  count, int samplesToRead, int timeout);
void ReadSinglePoint(ref uint[,] count, int samplesToRead, int timeout);

void ReadSinglePoint(ref double m, int timeout);
void ReadSinglePoint(ref double[]  m, int samplesToRead, int timeout);
void ReadSinglePoint(ref double[,] m, int samplesToRead, int timeout);

void ReadSinglePoint(ref double m1, ref double m2, int timeout);
void ReadSinglePoint(ref double[]  m1, ref double[]  m2, int samplesToRead, int timeout);
void ReadSinglePoint(ref double[,] m1, ref double[,] m2, int samplesToRead, int timeout);
```

## JY5114COTask — 计数器输出任务

```csharp
new JY5114COTask(int slotNumber, int counterId)   // counterId: 0 或 1
```

| 属性                    | 类型            | 说明                                |
| ----------------------- | --------------- | ----------------------------------- |
| `Mode`                  | `COMode`        | Single/Finite/ContinuousWrapping/…  |
| `IdleState`             | `COIdleState`   | LowLevel / HighLevel                |
| `InitialDelay`          | `double`        | 初始延迟（秒）                      |
| `OutputTerminal`        | `COTerminal`    | 输出终端                            |
| `SamplesToUpdate`       | `int`           | Finite 输出点数                     |
| `AvaliableLenInSamples` | `int`           | 缓冲区可用                          |
| `TransferedSamples`     | `ulong`         | 已传输样本数                        |
| `TransferedPulses`      | `ulong`         | 已传输脉冲数                        |
| `Trigger`               | `COTrigger`     | 触发                                |
| `Pause`                 | `COPause`       | 暂停触发                            |

### COPulse 构造

```csharp
new COPulse(COPulseType.DutyCycleFrequency, double freq, double dutyCycle, int count);
new COPulse(COPulseType.HighLowTime,        double highTime, double lowTime,   int count);
new COPulse(COPulseType.HighLowTick,        double highTick, double lowTick,   int count);
// count = -1 → 无限循环
```

### 方法

```csharp
void WriteSinglePoint(COPulse pulse);
void Start();  void Stop();  void WaitUntilDone(int timeout);
```

## 异常类 JYDriverException

```csharp
try { /* ... */ }
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);    // ex.ErrorCode, ex.ExceptionName
}
```

常见枚举值：`OpenDeviceFailed` / `NoChannelAdded` / `BufferDataOverflow` /
`ReadDataTimeout` / `TaskHasStartedCannotPerformTheSetOperation` /
`SampleRateParameterInvalid` / `ChannelInputRangeParameterInvalid` /
`TriggerParameterInvalid` / `SignalExportDestinationInvalid`。

---

# 完整代码示例

> 均来自 `JY5114.Examples` 工程，提取核心逻辑并加中文注释。

## 示例 1：AI 单点采集（Console）

```csharp
using System;
using JY5114;

Console.WriteLine("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());
var aiTask = new JY5114AITask(boardNum);

aiTask.Mode = AIMode.Single;
Console.WriteLine("请输入通道号：");
int channelID = Convert.ToInt32(Console.ReadLine());
aiTask.AddChannel(channelID, -10, 10, AITerminal.RSE, AIBandWidth.Wide);

aiTask.Start();

double readValue = 0;
int flag = 1;
while (flag == 1)
{
    aiTask.ReadSinglePoint(ref readValue, channelID);
    Console.WriteLine($"通道 {channelID} 电压: {readValue} V");
    Console.WriteLine("是否继续读取? (1:是, 0:否)");
    flag = Convert.ToInt16(Console.ReadLine());
}

aiTask.Stop();
aiTask.Channels.Clear();
```

## 示例 2：AI 连续采集（WinForm + Timer）

```csharp
private JY5114AITask aiTask;
private double[] readBuffer;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5114AITask(0);
        aiTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
        aiTask.Mode       = AIMode.Continuous;
        aiTask.SampleRate = 10000;
        aiTask.Start();

        readBuffer = new double[1000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    try
    {
        if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
        {
            aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
            easyChartX1.Plot(readBuffer);
        }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
    timer_FetchData.Enabled = true;
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
}
```

## 示例 3：AI 有限多通道采集

```csharp
aiTask = new JY5114AITask(0);
int[] channels = { 0, 1, 2 };
aiTask.AddChannel(channels, -10, 10, AITerminal.RSE, AIBandWidth.Wide);

aiTask.Mode             = AIMode.Finite;
aiTask.SamplesToAcquire = 1000;
aiTask.SampleRate       = 10000;
aiTask.Start();

aiTask.WaitUntilDone(-1);
double[,] data = new double[1000, 3];
aiTask.ReadData(ref data, 1000, -1);
easyChartX1.Plot(data);

aiTask.Stop();
aiTask.Channels.Clear();
```

## 示例 4：AI 数字触发连续采集

```csharp
aiTask = new JY5114AITask(0);
aiTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 50000;

aiTask.Trigger.Type            = AITriggerType.Digital;
aiTask.Trigger.Digital.Source  = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge    = AIDigitalTriggerEdge.Rising;
aiTask.Start();
```

## 示例 5：AI 模拟触发采集

```csharp
aiTask.Trigger.Type                  = AITriggerType.Analog;
aiTask.Trigger.Analog.Source         = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator     = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope     = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
aiTask.Start();
```

## 示例 6：AI 外部时钟采集

```csharp
aiTask.SampleClock.Source                = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal     = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10000;
aiTask.Start();
```

## 示例 7：AI 多卡连续同步（主 / 从）

```csharp
// 主卡 Slot 2
var masterTask = new JY5114AITask(2);
masterTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
masterTask.Mode = AIMode.Continuous;
masterTask.SampleRate = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig1);

// 从卡 Slot 4
var slaveTask = new JY5114AITask(4);
slaveTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
slaveTask.Mode = AIMode.Continuous;
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal     = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10000;
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

slaveTask.Start();   // 先从卡
masterTask.Start();  // 再主卡
```

## 示例 8：AO 单点输出

```csharp
var aoTask = new JY5114AOTask(0);
aoTask.AddChannel(0);              // ±10V 固定量程
aoTask.Mode = AOMode.Single;
aoTask.Start();

aoTask.WriteSinglePoint(5.0, 0);
System.Threading.Thread.Sleep(1000);
aoTask.WriteSinglePoint(-5.0, 0);

aoTask.Stop();
aoTask.Channels.Clear();
```

## 示例 9：AO 连续环形输出（正弦波）

```csharp
private JY5114AOTask aoTask;

private void button_start_Click(object sender, EventArgs e)
{
    aoTask = new JY5114AOTask(0);
    aoTask.AddChannel(0);
    aoTask.Mode       = AOMode.ContinuousWrapping;
    aoTask.UpdateRate = 10000;

    int samples = 10000;
    double[] buf = new double[samples];
    for (int i = 0; i < samples; i++)
        buf[i] = 5.0 * Math.Sin(2 * Math.PI * i / samples);

    aoTask.WriteData(buf, -1);    // Wrapping 模式：先写再 Start
    aoTask.Start();
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (aoTask != null) { aoTask.Stop(); aoTask.Channels.Clear(); }
}
```

## 示例 10：DI 单点采集（按 line）

```csharp
var diTask = new JY5114DITask(0);
diTask.AddChannel(-1);             // 添加全部已使能线
diTask.Mode = DIMode.Single;
diTask.Start();

bool[] readValues = new bool[24];
diTask.ReadSinglePoint(ref readValues);
for (int i = 0; i < readValues.Length; i++)
    Console.WriteLine($"Line {i}: {readValues[i]}");

diTask.Stop();
diTask.Channels.Clear();
```

## 示例 11：DO 单点输出

```csharp
var doTask = new JY5114DOTask(0);
doTask.AddChannel(-1);             // 全部已使能线
doTask.Mode = DOMode.Single;
doTask.Start();

bool[] writeValues = new bool[24];
for (int i = 0; i < writeValues.Length; i++) writeValues[i] = (i % 2 == 0);
doTask.WriteSinglePoint(writeValues);

// 单条线更新
doTask.WriteSinglePoint(true, 3);

doTask.Stop();
doTask.Channels.Clear();
```

## 示例 12：CI 边沿计数

```csharp
private JY5114CITask ciTask;

private void button_start_Click(object sender, EventArgs e)
{
    ciTask = new JY5114CITask(0, 0);    // 计数器 0
    ciTask.Mode = CIMode.Single;
    ciTask.Type = CIType.EdgeCounting;

    ciTask.EdgeCounting.Direction    = CountDirection.Up;
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

## 示例 13：CI 频率测量

```csharp
ciTask = new JY5114CITask(0, 0);
ciTask.Mode = CIMode.Single;
ciTask.Type = CIType.Frequency;
ciTask.Start();

double freq;
ciTask.ReadSinglePoint(ref freq, -1);
Console.WriteLine($"频率: {freq:F2} Hz");

ciTask.Stop();
```

## 示例 14：CO 脉冲输出

```csharp
var coTask = new JY5114COTask(0, 0);       // 计数器 0
coTask.Mode         = COMode.Single;
coTask.IdleState    = COIdleState.LowLevel;
coTask.InitialDelay = 0;

// 1 kHz / 50% 占空比 / 100 个脉冲
var pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, 100);
coTask.WriteSinglePoint(pulse);
coTask.Start();
coTask.WaitUntilDone(-1);
coTask.Stop();
```

## 示例 15：CO 连续脉冲输出

```csharp
coTask = new JY5114COTask(0, 0);
coTask.Mode      = COMode.ContinuousWrapping;
coTask.IdleState = COIdleState.LowLevel;

var pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, -1);  // 无限
coTask.WriteSinglePoint(pulse);
coTask.Start();

// 运行中更新参数
coTask.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 2000.0, 0.3, -1));
```

## 示例 16：Record 有限录制

```csharp
aiTask = new JY5114AITask(0);
aiTask.AddChannel(0, -10, 10, AITerminal.RSE, AIBandWidth.Wide);
aiTask.Mode       = AIMode.Record;
aiTask.SampleRate = 500000;

aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.Mode     = RecordMode.Finite;
aiTask.Record.Length   = 10.0;
aiTask.Start();

// 轮询录制状态 + 预览
double recordedLen; bool recordDone;
double[,] preview = new double[1000, 1];
do
{
    aiTask.GetRecordStatus(out recordedLen, out recordDone);
    if (aiTask.AvailableSamples >= 1000)
        aiTask.GetRecordPreviewData(ref preview, 1000, 1000);
} while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

## 示例 17：Record 无限录制

```csharp
aiTask = new JY5114AITask(0);
for (int i = 0; i < 4; i++)
    aiTask.AddChannel(i, -10, 10, AITerminal.RSE, AIBandWidth.Wide);

aiTask.Mode       = AIMode.Record;
aiTask.SampleRate = 500000;

aiTask.Record.FilePath = @"C:\Data\infinite.bin";
aiTask.Record.Mode     = RecordMode.Infinite;      // 手动 Stop 才会停止
aiTask.Start();

// 界面 Timer 持续预览
// button_Stop_Click 里调用 aiTask.Stop(); aiTask.Channels.Clear();
```

## 综合技巧

### 量程选择

```csharp
// 选择略大于信号峰值的量程以提高精度
aiTask.AddChannel(0, -10,  10,  AITerminal.RSE,          AIBandWidth.Wide);    // 大信号
aiTask.AddChannel(1, -2.5, 2.5, AITerminal.Differential, AIBandWidth.Narrow);  // 小信号 + 窄带宽滤波降噪
```

### 多通道数据解析

```csharp
double[,] buf = new double[1000, 3];  // 1000 点、3 通道
aiTask.ReadData(ref buf, 1000, -1);
double[] ch0 = new double[1000];
for (int i = 0; i < 1000; i++) ch0[i] = buf[i, 0];
```

### 窗体关闭资源释放

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer_FetchData != null) timer_FetchData.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
        if (aoTask != null) { aoTask.Stop(); aoTask.Channels.Clear(); }
        if (diTask != null) { diTask.Stop(); diTask.Channels.Clear(); }
        if (doTask != null) { doTask.Stop(); doTask.Channels.Clear(); }
        if (ciTask != null) ciTask.Stop();
        if (coTask != null) coTask.Stop();
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```
