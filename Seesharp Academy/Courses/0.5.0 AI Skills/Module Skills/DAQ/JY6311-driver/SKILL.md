---
name: jy6311-driver
description: 提供 JYTEK JY6311 系列 16 通道、24-bit、通道间隔离 RTD 温度采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）RTD 温度/电阻/电压三类测量模式（Single/Finite/Continuous）、PT100/PT1000 两线/三线/四线接法、多种 TCR 系数（TCR3851/3916/3920/3911/3928/3750）、数字输入/输出（DI/DO）单点模式、数字触发/软触发配置、内部/外部时钟配置、多卡采样时钟同步。当用户使用 USB/PCIe/PXIe-6311、JY6311AITask、JY6311DITask、JY6311DOTask、RTDType.PT100、RTDType.PT1000、RTDTerminal.TwoWire/ThreeWire/FourWire、RTDTCRType、MeasureDataType.Temperature/Resistance/Voltage、AIMode.Single、AIMode.Finite、AIMode.Continuous 开发温度采集、热电阻测量、工业测温、环境监测、自动化测试应用时自动应用。
---

# JY6311 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY6311;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                      | 规格                                                           |
| ------------------------- | -------------------------------------------------------------- |
| AI 分辨率                 | 24-bit                                                         |
| AI 通道数                 | 16 通道（通道间隔离）                                          |
| 采样方式                  | 同步采集（Simultaneous sampling）                              |
| AI 最大采样率             | 3 kS/s（每通道）                                               |
| 温度测量精度              | 0.37 ℃（PT100，4 线制）                                        |
| 支持传感器                | RTD PT100 / PT1000                                             |
| 接线方式                  | 2 线制 / 3 线制 / 4 线制                                       |
| 温度测量范围（PT100）     | -200 ℃ ~ +850 ℃                                                |
| 温度测量范围（PT1000）    | -200 ℃ ~ +150 ℃                                                |
| 电阻测量范围              | 0 Ω ~ 400 Ω（400 Ω / 1600 Ω 两档）                             |
| 电压测量量程              | ±1.25V / ±625mV / ±312.5mV / ±156.25mV / ±78.125mV / ±39.062mV |
| 激励电流                  | 1 mA（PT100）/ 750 μA（PT1000）                                |
| 通道间隔离                | 60 VDC                                                         |
| 通道对大地隔离            | 60 VDC                                                         |
| 电源工频抑制              | 50/60 Hz 软件可选                                              |
| 触发方式                  | 数字触发 / 软件触发 / 立即触发                                 |
| DI/DO                     | 3 通道（复用 PFI0~PFI2，TTL 电平）                             |
| 接口类型                  | USB / PCIe / PXIe                                              |

### 产品型号

- **USB-6311**：16-ch 24-bit USB 通道间隔离 RTD 温度输入模块
- **PCIe-6311**：16-ch 24-bit PCIe 通道间隔离 RTD 温度输入模块
- **PXIe-6311**：16-ch 24-bit PXIe 通道间隔离 RTD 温度输入模块

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY6311AITask(0);                                  // 1. 创建（按槽位号）
task.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire);         // 2. 添加通道（温度/电阻）
task.Mode = AIMode.Continuous;                                    // 3. 配置
task.SampleRate = 100;
task.Start();                                                     // 4. 启动
// ... 读取数据 ...
task.Stop();                                                      // 5. 停止
task.Channels.Clear();                                            // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY6311AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                          |
| -------------------- | ----------------------- | --------------------------------------------- |
| `Mode`               | `AIMode`                | Single / Finite / Continuous                  |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s），最大 3 kS/s             |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数                   |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（非 Single 模式）        |
| `TransferedSamples`  | `ulong`                 | 已传输点数（非 Single 模式）                  |
| `PowerLineFrequency` | `PowerLineFrequency`    | 工频抑制频率（_50Hz / _60Hz）                 |
| `SampleClock`        | `AISampleClock`         | 时钟配置对象                                  |
| `Trigger`            | `AITrigger`             | 触发配置对象                                  |
| `SignalExport`       | `AISignalExport`        | 信号导出对象                                  |
| `Channels`           | `List<AIChannel>`       | 已添加的通道列表                              |
| `Device`             | `JY6311Device`          | 设备对象                                      |

#### AddChannel 重载

JY6311 支持三类测量：**RTD 温度**、**RTD 电阻**、**电压**，分别对应不同的 AddChannel 重载。

```csharp
// ===== RTD 温度测量（需指定 TCR 系数）=====
// 单通道
aiTask.AddChannel(int chnId, RTDType rtdType, RTDTerminal terminal, RTDTCRType tcr);
// 多通道（统一类型）
aiTask.AddChannel(int[] chnsId, RTDType rtdType, RTDTerminal terminal, RTDTCRType tcr);
// 多通道（各自类型）
aiTask.AddChannel(int[] chnsId, RTDType[] rtdTypes, RTDTerminal[] terminals, RTDTCRType[] tcrs);

// ===== RTD 电阻测量（无需 TCR）=====
aiTask.AddChannel(int chnId, RTDType rtdType, RTDTerminal terminal);
aiTask.AddChannel(int[] chnsId, RTDType rtdType, RTDTerminal terminal);
aiTask.AddChannel(int[] chnsId, RTDType[] rtdTypes, RTDTerminal[] terminals);

// ===== 电压测量 =====
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);
aiTask.AddChannel(int[] chnsId, double rangeLow, double rangeHigh);
aiTask.AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh);

// 通道编号：0~15
// 电压 rangeLow/High 合法配对：
//   ±1.25 / ±0.625 / ±0.3125 / ±0.15625 / ±0.078125 / ±0.0390625
```

#### 枚举说明

**MeasureDataType 枚举**（通道测量量类型，只读属性）

| 值            | 说明       |
| ------------- | ---------- |
| `Resistance`  | 电阻测量（Ω） |
| `Temperature` | 温度测量（℃） |
| `Voltage`     | 电压测量（V） |

**RTDType 枚举**

| 值       | 说明          |
| -------- | ------------- |
| `PT100`  | Pt100 铂电阻  |
| `PT1000` | Pt1000 铂电阻 |

**RTDTerminal 枚举**

| 值          | 说明     |
| ----------- | -------- |
| `TwoWire`   | 2 线制   |
| `ThreeWire` | 3 线制   |
| `FourWire`  | 4 线制（推荐） |

**RTDTCRType 枚举**（温度电阻系数，仅温度测量有效）

| 值                  | 说明                 |
| ------------------- | -------------------- |
| `Pt100_TCR3851`     | PT100, α=0.003851（默认） |
| `Pt100_TCR3916`     | PT100, α=0.003916    |
| `Pt100_TCR3920`     | PT100, α=0.003920    |
| `Pt100_TCR3911`     | PT100, α=0.003911    |
| `Pt100_TCR3928`     | PT100, α=0.003928    |
| `Pt1000_TCR3851`    | PT1000, α=0.003851（默认） |
| `Pt1000_TCR3750`    | PT1000, α=0.003750   |

#### 读取数据重载

```csharp
// Single 模式 — 单通道
aiTask.ReadSinglePoint(ref double readValue, int channel);
// Single 模式 — 全通道
aiTask.ReadSinglePoint(ref double[] readValue);

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);
aiTask.ReadData(ref double[] buf, int samplesPerChannel);   // 默认 timeout=-1
// Finite/Continuous — 多通道（列存储，每列一个通道）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
aiTask.ReadData(ref double[,] buf, int samplesPerChannel);

// timeout = -1 → 永久等待
```

#### AI 三种模式速查

| 模式         | 典型配置                       | 读取方式                                     |
| ------------ | ------------------------------ | -------------------------------------------- |
| `Single`     | `Mode=AIMode.Single`           | 轮询 `ReadSinglePoint`                       |
| `Finite`     | `+SamplesToAcquire=N`          | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                  | Timer 轮询 `AvailableSamples` → `ReadData`   |

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
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // PFI0~PFI2 / PXI_Trig0~7
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;     // Rising / Falling
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
aiTask.SampleClock.External.ExpectedRate = 100;
```

#### 工频抑制配置

根据现场工频选择对应抑制档位，可显著改善测温稳定性：

```csharp
aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;   // 国内电网
// aiTask.PowerLineFrequency = PowerLineFrequency._60Hz;  // 部分海外电网
```

---

## 多卡同步

JY6311 支持通过 PXI 背板触发总线进行多卡采样时钟同步，适用于大规模温度监测场景。

```csharp
// ===== 主卡配置（Slot 2）=====
var masterTask = new JY6311AITask(2);
masterTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);
masterTask.Mode = AIMode.Finite;
masterTask.SamplesToAcquire = 1000;
masterTask.SampleRate = 100;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟和触发信号
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 4）=====
var slaveTask = new JY6311AITask(4);
slaveTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 1000;

slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 100;

slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();
masterTask.Start();

// ===== 读取数据 =====
double[] masterData = new double[1000];
double[] slaveData = new double[1000];

masterTask.WaitUntilDone(-1);
slaveTask.WaitUntilDone(-1);

masterTask.ReadData(ref masterData, 1000, -1);
slaveTask.ReadData(ref slaveData, 1000, -1);

masterTask.Stop();
slaveTask.Stop();
masterTask.Channels.Clear();
slaveTask.Channels.Clear();
```

---

## 数字输入（DI）

### 任务类：`JY6311DITask`

JY6311 的 DI 通道复用 PFI 管脚，共 **3 通道**（0~2），仅支持**单点读取**（无缓冲模式）。

```csharp
var diTask = new JY6311DITask(0);
diTask.AddChannel(0);      // 通道 0
diTask.AddChannel(1);      // 通道 1
diTask.AddChannel(2);      // 通道 2
diTask.Start();

bool value = false;
diTask.ReadSinglePoint(ref value, 0);   // 读单通道
// 或：读全部已添加通道
bool[] values = new bool[3];
diTask.ReadSinglePoint(ref values);

diTask.Stop();
diTask.Channels.Clear();
```

---

## 数字输出（DO）

### 任务类：`JY6311DOTask`

同样复用 PFI 管脚，3 通道（0~2），仅支持**单点写入**。

```csharp
var doTask = new JY6311DOTask(0);
doTask.AddChannel(0);
doTask.AddChannel(1);
doTask.Start();

doTask.WriteSinglePoint(true, 0);     // 通道 0 输出高
doTask.WriteSinglePoint(false, 1);    // 通道 1 输出低

// 或：写入全部已添加通道
bool[] writeValue = { true, false };
doTask.WriteSinglePoint(writeValue);

doTask.Stop();
doTask.Channels.Clear();
```

> **注意**：DI 与 DO 不能同时占用同一 PFI 管脚；PFI 同时也被 AI 触发/外部时钟复用，配置时需注意资源冲突。

---

## 常见错误处理

| 异常代码                                         | 原因                     | 处理建议              |
| ------------------------------------------------ | ------------------------ | --------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误   | 检查设备管理器槽位号  |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start | 在 Start 前添加通道   |
| `BufferDataOverflow`                             | 读取速度慢于采集速度     | 增大读取频率或减小采样率 |
| `ReadDataTimeout`                                | timeout 内未读到足够数据 | 增大 timeout 或检查采样率 |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数      | Stop 后再修改         |
| `SampleRateParameterInvalid`                     | 采样率超过 3 kS/s        | 降低采样率            |
| `ChannelNumberParameterInvalid`                  | 通道号 < 0 或 > 15       | 使用合法通道号        |
| `ChannelInputRangeParameterInvalid`              | 电压量程不合法           | 使用枚举表中的量程对  |

---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY6311;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.dll
```

---

## JY6311AITask — 模拟输入任务

### 构造函数

```csharp
new JY6311AITask(int slotNumber)     // 按槽位号创建（推荐）
new JY6311AITask(string boardType)   // 按板卡类型字符串创建
```

### 属性

| 属性                  | 类型                  | 默认值 | 说明                                      |
| --------------------- | --------------------- | ------ | ----------------------------------------- |
| `Mode`                | `AIMode`              | —      | Single / Finite / Continuous              |
| `SampleRate`          | `double`              | —      | 每通道采样率（Sa/s），最大 3 kS/s         |
| `SamplesToAcquire`    | `int`                 | —      | Finite 模式采集点数/通道                  |
| `AvailableSamples`    | `ulong`               | —      | 缓冲区可读点数（非 Single 模式有效）      |
| `TransferedSamples`   | `ulong`               | —      | 已传输点数（非 Single 模式有效）          |
| `PowerLineFrequency`  | `PowerLineFrequency`  | —      | 工频抑制：_50Hz / _60Hz                   |
| `SampleClock`         | `AISampleClock`       | —      | 时钟配置对象                              |
| `Trigger`             | `AITrigger`           | —      | 触发配置对象                              |
| `SignalExport`        | `AISignalExport`      | —      | 信号导出对象                              |
| `Channels`            | `List<AIChannel>`     | —      | 已添加的通道列表                          |
| `Device`              | `JY6311Device`        | —      | 设备对象                                  |

### 方法

#### AddChannel — 见上文"AddChannel 重载"章节

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
void ReadSinglePoint(ref double[] readValue)                     // 全部通道

// 缓冲模式（Finite / Continuous）
void ReadData(ref double[] buf, int samplesPerChannel)
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)
void ReadData(ref double[,] buf, int samplesPerChannel)
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)
```

---

## AIChannel — 通道属性（只读）

| 属性              | 类型                | 说明                              |
| ----------------- | ------------------- | --------------------------------- |
| `ChannelID`       | `int`               | 通道号                            |
| `MeasureDataType` | `MeasureDataType`   | 测量量类型（Resistance/Temperature/Voltage） |
| `RTDType`         | `RTDType`           | RTD 类型（温度/电阻模式有效）     |
| `TerminalType`    | `RTDTerminal`       | 接线方式（温度/电阻模式有效）     |
| `RTDTCRType`      | `RTDTCRType`        | TCR 系数（温度模式有效）          |
| `RangeLow`        | `double`            | 输入量程下限                      |
| `RangeHigh`       | `double`            | 输入量程上限                      |

---

## AIMode 枚举

| 值           | 说明                                            |
| ------------ | ----------------------------------------------- |
| `Single`     | 软件触发单点读取，可循环调用 ReadSinglePoint    |
| `Finite`     | 采集固定点数后停止，通过 WaitUntilDone 等待完成 |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取        |

---

## AISampleClock — 时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.Internal;    // 默认
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;   // PFI0~PFI2
aiTask.SampleClock.External.ExpectedRate = 100.0;
```

### ClockTerminal 枚举

| 值                        | 说明             |
| ------------------------- | ---------------- |
| `PFI0` ~ `PFI2`           | 前面板 PFI 引脚  |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线     |
| `AI_SampleClock`          | AI 采样时钟     |

---

## AITrigger — 触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;     // Digital / Soft / Immediate
aiTask.Trigger.Mode = AITriggerMode.Start;       // Start / Reference
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

### AITriggerType 枚举

| 值           | 说明            |
| ------------ | --------------- |
| `Immediate`  | 立即触发（默认） |
| `Digital`    | 数字边沿触发    |
| `Soft`       | 软件触发        |

### AITriggerMode 枚举

| 值          | 说明             |
| ----------- | ---------------- |
| `Start`     | 开始触发（默认） |
| `Reference` | 参考触发         |

### AITrigger 其他重要属性

| 属性                | 类型   | 说明                                                         |
| ------------------- | ------ | ------------------------------------------------------------ |
| `PreTriggerSamples` | `uint` | 预触发样本数，Reference 模式有效，必须 ≤ SamplesToAcquire    |
| `ReTriggerCount`    | `int`  | 重复触发次数，0 或 1 触发一次，-1 持续重复直到 Stop          |

### AIDigitalTriggerSource 枚举

| 值                       | 说明           |
| ------------------------ | -------------- |
| `PFI0` ~ `PFI2`          | 前面板 PFI     |
| `PXI_Trig0` ~ `PXI_Trig7`| PXI 触发总线   |

### AIDigitalTriggerEdge 枚举

| 值        | 说明   |
| --------- | ------ |
| `Rising`  | 上升沿 |
| `Falling` | 下降沿 |

---

## AISignalExport — 信号导出

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);
```

### AISignalExportSource 枚举

| 值              | 说明            |
| --------------- | --------------- |
| `StartTrig`     | 开始触发信号    |
| `ReferenceTrig` | 参考触发信号    |
| `SampleClock`   | 采样时钟        |

### SignalExportDestination 枚举

| 值                        | 说明           |
| ------------------------- | -------------- |
| `PFI0` ~ `PFI2`           | 前面板 PFI 引脚|
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线   |

---

## PowerLineFrequency 枚举

| 值      | 说明          |
| ------- | ------------- |
| `_50Hz` | 50 Hz 工频抑制 |
| `_60Hz` | 60 Hz 工频抑制 |

---

## JY6311DITask — 数字输入任务

### 构造函数

```csharp
new JY6311DITask(int slotNumber)
new JY6311DITask(string boardType)
```

### 方法

```csharp
void AddChannel(int channelID)              // 通道号 0~2
void AddChannel(int[] channelIDs)
void RemoveChannel(int channelID)

void Start()
void Stop()

void ReadSinglePoint(ref bool[] readValue)                 // 读全部已添加通道
void ReadSinglePoint(ref bool readValue, int channelID)    // 读单通道
```

---

## JY6311DOTask — 数字输出任务

### 构造函数

```csharp
new JY6311DOTask(int slotNumber)
new JY6311DOTask(string boardType)
```

### 方法

```csharp
void AddChannel(int channelID)              // 通道号 0~2
void AddChannel(int[] channelIDs)
void RemoveChannel(int channelID)

void Start()
void Stop()

void WriteSinglePoint(bool[] writeValue)                   // 写全部已添加通道
void WriteSinglePoint(bool writeValue, int channelID)      // 写单通道
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

| 枚举值                                           | 含义                     |
| ------------------------------------------------ | ------------------------ |
| `OpenDeviceFailed`                               | 打开设备失败             |
| `CloseDeviceFailed`                              | 关闭设备失败             |
| `NoChannelAdded`                                 | 未添加通道               |
| `StartTaskFailed`                                | 启动 Task 失败           |
| `StopTaskFailed`                                 | 停止 Task 失败           |
| `TaskHasNotStarted`                              | Task 未启动即读写        |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数      |
| `BufferDataOverflow`                             | 采集缓冲区溢出           |
| `ReadDataTimeout`                                | 读取超时                 |
| `SampleRateParameterInvalid`                     | 采样率超限               |
| `ChannelNumberParameterInvalid`                  | 无效通道号               |
| `ChannelInputRangeParameterInvalid`              | 无效输入量程             |
| `TriggerParameterInvalid`                        | 触发参数无效             |

---

# 完整代码示例

> 所有示例均来自 `JY6311.Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点温度测量（Console）

```csharp
using System;
using System.Threading;
using JY6311;

Console.Write("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());

Console.Write("请输入通道号（0~15）：");
int channelID = Convert.ToInt32(Console.ReadLine());

JY6311AITask aiTask = new JY6311AITask(boardNum);

// 添加通道：PT100 四线制温度测量，TCR=0.003851
aiTask.AddChannel(channelID, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);     // 等待首次转换完成

double temperature = 0;
aiTask.ReadSinglePoint(ref temperature, channelID);
Console.WriteLine($"通道 {channelID} 温度：{temperature:F2} ℃");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 2：AI 单点电阻测量

```csharp
aiTask = new JY6311AITask(0);
// 添加通道：PT100 四线制电阻测量（不指定 TCR）
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire);
aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double resistance = 0;
aiTask.ReadSinglePoint(ref resistance, 0);
Console.WriteLine($"电阻值：{resistance:F3} Ω");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 3：AI 单点电压测量

```csharp
aiTask = new JY6311AITask(0);
// 电压模式：±1.25V 量程
aiTask.AddChannel(0, -1.25, 1.25);
aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double voltage = 0;
aiTask.ReadSinglePoint(ref voltage, 0);
Console.WriteLine($"电压值：{voltage:F6} V");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 4：AI 连续温度采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY6311;

public partial class MainForm : Form
{
    private JY6311AITask aiTask;
    private double[] readValue;

    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            readValue = new double[1000];

            // 创建 Task（槽位号 0）
            aiTask = new JY6311AITask(0);

            // 添加通道：PT100 四线制温度测量
            aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

            // 连续模式
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 100;                     // 100 Sa/s
            aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;

            aiTask.Start();

            timer1.Enabled = true;
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask.AvailableSamples >= (ulong)readValue.Length)
        {
            try
            {
                aiTask.ReadData(ref readValue, -1);
                easyChartX_AI.Plot(readValue);
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        timer1.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }
}
```

---

## 示例 5：AI 有限多通道温度采集

```csharp
JY6311AITask aiTask = new JY6311AITask(0);

// 多通道相同类型
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 500;
aiTask.SampleRate = 100;

aiTask.Start();
aiTask.WaitUntilDone(-1);

// 多通道读取：列存储 [采样点, 通道]
double[,] readValue = new double[500, 4];
aiTask.ReadData(ref readValue, 500, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 6：AI 多通道混合测量（不同 RTD 类型）

```csharp
JY6311AITask aiTask = new JY6311AITask(0);

// 各通道独立配置
int[] channels = { 0, 1, 2 };
RTDType[] types = { RTDType.PT100, RTDType.PT1000, RTDType.PT100 };
RTDTerminal[] terminals = { RTDTerminal.FourWire, RTDTerminal.ThreeWire, RTDTerminal.TwoWire };
RTDTCRType[] tcrs = { RTDTCRType.Pt100_TCR3851, RTDTCRType.Pt1000_TCR3851, RTDTCRType.Pt100_TCR3916 };

aiTask.AddChannel(channels, types, terminals, tcrs);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100;
aiTask.Start();

// ... 读取数据 ...
```

---

## 示例 7：AI 连续采集 + 数字触发

```csharp
aiTask = new JY6311AITask(0);
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100;

// 数字触发：PFI0 上升沿
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发到来后自动开始采集，后续读取同连续模式...
```

---

## 示例 8：AI 软触发采集

```csharp
aiTask = new JY6311AITask(0);
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 500;
aiTask.SampleRate = 100;

aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();

// 需要时手动发软触发
aiTask.SendSoftwareTrigger();

aiTask.WaitUntilDone(-1);
double[] data = new double[500];
aiTask.ReadData(ref data, 500, -1);
```

---

## 示例 9：DI 单点读取（3 通道）

```csharp
JY6311DITask diTask = new JY6311DITask(0);
diTask.AddChannel(0);
diTask.AddChannel(1);
diTask.AddChannel(2);
diTask.Start();

bool[] values = new bool[3];
diTask.ReadSinglePoint(ref values);
Console.WriteLine($"DI0={values[0]}, DI1={values[1]}, DI2={values[2]}");

diTask.Stop();
diTask.Channels.Clear();
```

---

## 示例 10：DO 单点写入（3 通道）

```csharp
JY6311DOTask doTask = new JY6311DOTask(0);
doTask.AddChannel(0);
doTask.AddChannel(1);
doTask.AddChannel(2);
doTask.Start();

// 逐通道写入
doTask.WriteSinglePoint(true, 0);
doTask.WriteSinglePoint(false, 1);
doTask.WriteSinglePoint(true, 2);

// 或：一次写入全部
bool[] writeValue = { true, false, true };
doTask.WriteSinglePoint(writeValue);

doTask.Stop();
doTask.Channels.Clear();
```

---

## 综合技巧

### 多通道数据解析

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[500, 4];
aiTask.ReadData(ref buf, 500, -1);

// 提取各通道数据
double[] ch0 = new double[500];
for (int i = 0; i < 500; i++) ch0[i] = buf[i, 0];
```

### 测量类型选择策略

```csharp
// 1. 高精度温度测量（推荐）：4 线制 + PT100 + 正确 TCR
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

// 2. 中距离布线：3 线制（补偿引线电阻）
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

// 3. 需原始电阻值：电阻模式（不做温度换算）
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire);

// 4. 自定义传感器或直接测量小信号：电压模式
aiTask.AddChannel(0, -0.078125, 0.078125);    // ±78.125mV 最高精度量程
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer1 != null) timer1.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
        if (diTask != null) { diTask.Stop(); diTask.Channels.Clear(); }
        if (doTask != null) { doTask.Stop(); doTask.Channels.Clear(); }
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```
