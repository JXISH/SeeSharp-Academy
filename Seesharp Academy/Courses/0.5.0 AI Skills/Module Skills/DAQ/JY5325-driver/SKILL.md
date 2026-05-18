---
name: jy5325-driver
description: 提供 JYTEK JY5325 系列（PXIe/PCIe-5325）多功能同步数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）、模拟输出（AO）、数字输入（DI）、数字输出（DO）、计数器输入（CI）、计数器输出（CO）的单点/有限/连续/录制模式，触发配置（数字/模拟/软件触发）、时钟配置（内部/外部/参考时钟）、多卡同步、录制模式（Finite/Infinite Streaming）、脉冲测量（边沿计数/频率/周期/脉宽/正交编码/双脉冲编码）。当用户使用 JY5325、PXIe-5325、PCIe-5325、JY5325AITask、JY5325AOTask、JY5325DITask、JY5325DOTask、JY5325CITask、JY5325COTask、AIMode.Single/Finite/Continuous/Record、AOMode.ContinuousWrapping、CIType.EdgeCounting/FrequencyMeas/QuadEncoder、RecordMode.Finite/Infinite、SyncTopology.Master/Slave 开发多功能同步采集、信号生成、波形记录、脉冲测量、自动化测试应用时自动应用。
---

# JY5325 驱动开发指引

## 适用硬件

- **PCIe-5325**：PCIe 接口多功能同步模拟输出、输入模块
- **PXIe-5325**：PXIe 接口多功能同步模拟输出、输入模块

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5325\Bin\JY5325.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5325\Bin\JY5325.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY5325;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

### 整机规格

| 功能             | 规格                                                    |
| ---------------- | ------------------------------------------------------- |
| 系统精度         | 280 ppm                                                 |
| 接口             | PCIe / PXIe                                             |

### 模拟输入（AI）

| 参数         | 规格                                                    |
| ------------ | ------------------------------------------------------- |
| 分辨率       | 16-bit                                                  |
| 通道数       | 8 通道同步差分输入                                      |
| 最大采样率   | 5 MS/s/ch                                               |
| 输入量程     | ±1 V / ±2 V / ±5 V / ±10 V                              |
| 输入耦合     | 直流耦合                                                |
| 输入阻抗     | 1 GΩ                                                    |
| 带宽         | 2.2 MHz                                                 |
| CMRR         | -105 dB                                                 |
| THD          | -80 dB                                                  |
| 过压保护     | 上电 ±25 V / 断电 ±15 V                                 |
| FIFO 缓存    | 100 M Samples                                           |

### 模拟输出（AO）

| 参数         | 规格                                                    |
| ------------ | ------------------------------------------------------- |
| 分辨率       | 16-bit                                                  |
| 通道数       | 8 通道同步输出                                          |
| 最大更新率   | 1 MS/s/ch                                               |
| 输出量程     | ±10 V                                                   |
| 精度         | 150 ppm                                                 |
| FIFO 缓存    | 100 M Samples                                           |
| 输出模式     | 单次 / 流式 / 定时 / 触发                               |

### 数字输入/输出（DIO）

| 参数         | 规格                                                    |
| ------------ | ------------------------------------------------------- |
| 通道数       | 8 路 硬件定时 DIO（每路可独立配置为 DI 或 DO）          |
| 最大时钟频率 | 10 MHz                                                  |
| 支持功能     | 静态 / 有限 / 连续（循环 / 非循环）/ 触发               |

### 计数器（Counter）

| 参数         | 规格                                                    |
| ------------ | ------------------------------------------------------- |
| 通道数       | 2 路 32 位通用定时器/计数器                             |
| 内部时基     | 100 MHz                                                 |
| 测量功能     | 边沿计数、频率测量、周期测量、脉宽测量、半周期测量、双沿分离、正交编码、双脉冲编码 |
| 输出功能     | 单脉冲、有限脉冲序列、连续脉冲序列（循环/非循环）、PWM  |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
using JY5325;

var task = new JY5325AITask(0);          // 1. 创建（按槽位号）
task.AddChannel(0, -10, 10);             // 2. 添加通道
task.Mode = AIMode.Continuous;           // 3. 配置
task.SampleRate = 1000000;
task.Start();                            // 4. 启动
// ... 读取数据 ...
task.Stop();                             // 5. 停止
task.Channels.Clear();                   // 6. 清除通道
```

**任务类列表**：

| 任务类             | 功能                     | 构造函数                                              |
| ------------------ | ------------------------ | ----------------------------------------------------- |
| `JY5325AITask`     | 模拟输入任务             | `new JY5325AITask(int slotNumber)`                    |
| `JY5325AOTask`     | 模拟输出任务             | `new JY5325AOTask(int slotNumber)`                    |
| `JY5325DITask`     | 数字输入任务             | `new JY5325DITask(int slotNumber)`                    |
| `JY5325DOTask`     | 数字输出任务             | `new JY5325DOTask(int slotNumber)`                    |
| `JY5325CITask`     | 计数器输入任务（**需指定 counterID**） | `new JY5325CITask(int slotNumber, int counterID)` |
| `JY5325COTask`     | 计数器输出任务（**需指定 counterID**） | `new JY5325COTask(int slotNumber, int counterID)` |
| `JY5325Device`     | 设备级配置（参考时钟等） | `new JY5325Device(int slotNumber)`                    |

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY5325AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Single / Finite / Continuous / Record   |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s），最大 5 MS/s       |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数             |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（非 Single 模式）  |
| `SampleClock.Source` | `AISampleClockSource`   | Internal（默认）/ External              |
| `Trigger.Type`       | `AITriggerType`         | Immediate（默认）/ Digital / Analog / Soft |
| `Trigger.Mode`       | `AITriggerMode`         | Start（默认）/ Reference                |
| `Channels.Count`     | `int`                   | 当前已添加的通道数                      |
| `Sync.Topology`      | `SyncTopology`          | Independent / Master / Slave            |
| `Record`             | `AIRecord`              | 录制配置（Mode / FilePath / FileFormat）|

#### AddChannel 重载

```csharp
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);

// 多通道（相同量程）
aiTask.AddChannel(int[] chnIds, double rangeLow, double rangeHigh);

// 多通道（独立量程）
aiTask.AddChannel(int[] chnIds, double[] rangeLows, double[] rangeHighs);

// 说明：
// chnId 范围 0..7（共 8 通道）
// rangeLow / rangeHigh 组合：±1V / ±2V / ±5V / ±10V
```

#### AIMode 详解

| 模式         | 说明                                             | 触发/时钟要求                         |
| ------------ | ------------------------------------------------ | ------------------------------------- |
| `Single`     | 单点采集，`ReadSinglePoint` 读取一次            | 不依赖时钟/触发                       |
| `Finite`     | 有限点采集，指定 `SamplesToAcquire` 后自动停止  | 需配置 `SampleRate`                   |
| `Continuous` | 连续采集到内部 FIFO，循环读取                    | 需配置 `SampleRate`                   |
| `Record`     | 流式录制到磁盘（bin 文件），支持 Finite/Infinite | 需配置 `Record.Mode`、`Record.FilePath` |

#### 单点采集示例（AIMode.Single）

```csharp
using JY5325;

int slotNum = 0;
int channelID = 0;
double readValue = 0;

JY5325AITask aiTask = new JY5325AITask(slotNum);
aiTask.Mode = AIMode.Single;
aiTask.AddChannel(channelID, -10, 10);  // ±10V 量程

try
{
    aiTask.ReadSinglePoint(ref readValue, channelID);
    Console.WriteLine($"Channel {channelID}: {readValue:F6} V");
}
catch (JYDriverException ex)
{
    Console.WriteLine($"Read failed: {ex.Message}");
}
finally
{
    aiTask.Channels.Clear();
}
```

#### 连续采集示例（AIMode.Continuous）

```csharp
int slotNum = 0;
int samplesPerRead = 10000;
double[] readValue = new double[samplesPerRead];

JY5325AITask aiTask = new JY5325AITask(slotNum);
aiTask.Mode = AIMode.Continuous;
aiTask.AddChannel(0, -10, 10);
aiTask.SampleClock.Source = AISampleClockSource.Internal;
aiTask.SampleRate = 1000000;  // 1 MS/s
aiTask.Start();

while (acquiring)
{
    if (aiTask.AvailableSamples >= (ulong)readValue.Length)
    {
        aiTask.ReadData(ref readValue, readValue.Length, -1);  // -1 表示阻塞等待
        // 处理 readValue
    }
}

aiTask.Stop();
aiTask.Channels.Clear();
```

#### 有限点多通道采集示例（AIMode.Finite）

```csharp
int[] channels = { 0, 1, 2, 3 };
int samplesPerChannel = 10000;
double[,] readValue = new double[samplesPerChannel, channels.Length];

JY5325AITask aiTask = new JY5325AITask(0);
aiTask.Mode = AIMode.Finite;
aiTask.AddChannel(channels, -10, 10);
aiTask.SampleRate = 1000000;
aiTask.SamplesToAcquire = samplesPerChannel;
aiTask.Start();

aiTask.ReadData(ref readValue, samplesPerChannel, -1);
aiTask.Stop();
aiTask.Channels.Clear();
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;  // PFI0..7
aiTask.Trigger.Digital.Edge = DigitalTriggerEdge.Rising;       // Rising / Falling
```

#### 模拟触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;  // 触发通道 0..7
aiTask.Trigger.Analog.Condition = AnalogTriggerCondition.RisingSlope;
aiTask.Trigger.Analog.Level = 2.5;         // 触发电平 V
aiTask.Trigger.Analog.Hysteresis = 0.1;    // 滞回 V
```

#### 软件触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
// ... 触发时 ...
aiTask.Trigger.Soft.Fire();
```

#### 参考触发（Reference Trigger）

```csharp
// 触发前保留预触发点数，触发后继续采集
aiTask.Trigger.Mode = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 1000;  // 预触发点数
```

#### 外部采样时钟

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 1000000;  // 期望频率（用于 FIFO 分配）
aiTask.SampleClock.External.Edge = ClockEdge.Rising;
```

#### AIMode.Record 录制模式

见后文《录制模式》章节。

---

## 模拟输出（AO）

### 任务类：`JY5325AOTask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`         | `double`                | 每通道更新率（Sa/s），最大 1 MS/s       |
| `SamplesToUpdate`    | `int`                   | Finite 模式下每通道输出点数             |
| `SampleClock.Source` | `AOSampleClockSource`   | Internal（默认）/ External              |
| `Trigger.Type`       | `AOTriggerType`         | Immediate / Digital / Soft              |
| `SignalExport`       | `AOSignalExport`        | 导出采样时钟/触发至 PFI 等终端          |

#### AOMode 详解

| 模式                    | 说明                                                |
| ----------------------- | --------------------------------------------------- |
| `Single`                | 单点输出，`WriteSinglePoint` 立即输出              |
| `Finite`                | 有限点输出，输出 `SamplesToUpdate` 点后自动停止    |
| `ContinuousWrapping`    | 连续循环输出缓冲区内已写入的波形（无需持续写入）   |
| `ContinuousNoWrapping`  | 连续流式输出，用户需持续写入数据                   |

#### AddChannel

```csharp
// 单通道
aoTask.AddChannel(int channelID);

// 多通道
aoTask.AddChannel(int[] channelIDs);

// channelID 范围 0..7（共 8 通道）
```

#### 单点输出示例（AOMode.Single）

```csharp
using JY5325;

JY5325AOTask aoTask = new JY5325AOTask(0);
aoTask.AddChannel(0);
aoTask.Mode = AOMode.Single;
aoTask.WriteSinglePoint(2.5, 0);  // 在通道 0 输出 2.5V
aoTask.Channels.Clear();
```

#### 连续循环输出示例（AOMode.ContinuousWrapping）

```csharp
int slotNum = 0;
int channelID = 0;
int waveformLength = 10000;
double updateRate = 1000000;  // 1 MS/s

// 生成一个周期的正弦波
double[] writeValue = new double[waveformLength];
for (int i = 0; i < waveformLength; i++)
    writeValue[i] = 5 * Math.Sin(2 * Math.PI * i / waveformLength);

JY5325AOTask aoTask = new JY5325AOTask(slotNum);
aoTask.AddChannel(channelID);
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = updateRate;
aoTask.SamplesToUpdate = waveformLength;
aoTask.SampleClock.Source = AOSampleClockSource.Internal;

// 可选：将采样时钟导出至 PFI0
aoTask.SignalExport.Add(AOSignalExportSource.SampleClock, SignalExportDestination.PFI0);

aoTask.WriteData(writeValue, -1);
aoTask.Start();

// ... 持续输出波形 ...

aoTask.Stop();
aoTask.Channels.Clear();
```

#### 连续非循环流式输出（AOMode.ContinuousNoWrapping）

```csharp
JY5325AOTask aoTask = new JY5325AOTask(0);
aoTask.AddChannel(0);
aoTask.Mode = AOMode.ContinuousNoWrapping;
aoTask.UpdateRate = 1000000;

aoTask.WriteData(initialBuffer, -1);  // 先写入一段
aoTask.Start();

while (generating)
{
    // 持续补充数据（保持缓冲区不空）
    aoTask.WriteData(nextBuffer, -1);
}

aoTask.Stop();
aoTask.Channels.Clear();
```

#### 有限点输出示例（AOMode.Finite）

```csharp
JY5325AOTask aoTask = new JY5325AOTask(0);
aoTask.AddChannel(0);
aoTask.Mode = AOMode.Finite;
aoTask.UpdateRate = 100000;
aoTask.SamplesToUpdate = waveformData.Length;

aoTask.WriteData(waveformData, -1);
aoTask.Start();

// 等待输出完成（可以通过查询状态或固定延时）
while (!aoTask.IsDone) { System.Threading.Thread.Sleep(10); }

aoTask.Stop();
aoTask.Channels.Clear();
```

#### 多通道输出

```csharp
// 二维数组：[samples, channels]
double[,] writeValue = new double[samples, 4];
int[] channels = { 0, 1, 2, 3 };

aoTask.AddChannel(channels);
aoTask.WriteData(writeValue, -1);
```

#### AO 数字触发

```csharp
aoTask.Trigger.Type = AOTriggerType.Digital;
aoTask.Trigger.Digital.Source = AODigitalTriggerSource.PFI0;
aoTask.Trigger.Digital.Edge = DigitalTriggerEdge.Rising;
```

---

## 数字输入（DI）

### 任务类：`JY5325DITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                      |
| -------------------- | ----------------------- | ----------------------------------------- |
| `Mode`               | `DIMode`                | Single / Finite / Continuous              |
| `SampleRate`         | `double`                | 采样率（Sa/s），最大 10 MHz               |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数               |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数                      |
| `SampleClock.Source` | `DISampleClockSource`   | Internal / External                       |
| `Trigger.Type`       | `DITriggerType`         | Immediate / Digital / Soft                |
| `Channels.Count`     | `int`                   | 当前已添加的通道数                        |

#### AddChannel

```csharp
// 每个 DIO line 作为一个通道（0..7）
diTask.AddChannel(int lineID);
```

#### 单点读取

```csharp
JY5325DITask diTask = new JY5325DITask(0);
diTask.AddChannel(0);
diTask.AddChannel(1);
diTask.Mode = DIMode.Single;

byte[] value = new byte[diTask.Channels.Count];
diTask.ReadSinglePoint(ref value);
Console.WriteLine($"Line0={value[0]}, Line1={value[1]}");
diTask.Channels.Clear();
```

#### 连续读取示例

```csharp
int slotNum = 0;
int samplesPerRead = 10000;

JY5325DITask diTask = new JY5325DITask(slotNum);
for (int i = 0; i < 8; i++) diTask.AddChannel(i);

diTask.Mode = DIMode.Continuous;
diTask.SampleClock.Source = DISampleClockSource.Internal;
diTask.SampleRate = 1000000;  // 1 MHz
diTask.Start();

byte[,] dataBuf = new byte[samplesPerRead, diTask.Channels.Count];
while (acquiring)
{
    if (diTask.AvailableSamples >= (ulong)samplesPerRead)
    {
        diTask.ReadData(ref dataBuf, (uint)samplesPerRead, -1);
        // 处理 dataBuf[sample, channel]
    }
}

diTask.Stop();
diTask.Channels.Clear();
```

#### 外部时钟

```csharp
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
diTask.SampleClock.External.ExpectedRate = 1000000;
```

#### 数字触发

```csharp
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.PFI0;
diTask.Trigger.Digital.Edge = DigitalTriggerEdge.Rising;
```

---

## 数字输出（DO）

### 任务类：`JY5325DOTask`

#### 关键属性

| 属性                 | 类型                    | 说明                                              |
| -------------------- | ----------------------- | ------------------------------------------------- |
| `Mode`               | `DOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`         | `double`                | 更新率（Sa/s），最大 10 MHz                       |
| `SamplesToUpdate`    | `int`                   | Finite 模式下每通道输出点数                       |
| `SampleClock.Source` | `DOSampleClockSource`   | Internal / External                               |
| `Trigger.Type`       | `DOTriggerType`         | Immediate / Digital / Soft                        |

#### AddChannel

```csharp
doTask.AddChannel(int lineID);  // lineID: 0..7
```

#### 单点输出

```csharp
JY5325DOTask doTask = new JY5325DOTask(0);
doTask.AddChannel(0);
doTask.AddChannel(1);
doTask.Mode = DOMode.Single;

byte[] value = { 1, 0 };  // Line0=高, Line1=低
doTask.WriteSinglePoint(value);
doTask.Channels.Clear();
```

#### 连续循环输出示例（DOMode.ContinuousWrapping）

```csharp
int slotNum = 0;
int updateRate = 1000000;
int samplesPerChannel = 1000;

JY5325DOTask doTask = new JY5325DOTask(slotNum);
doTask.AddChannel(0);
doTask.AddChannel(1);

doTask.Mode = DOMode.ContinuousWrapping;
doTask.UpdateRate = updateRate;

// [samples, channels]
byte[,] writeValue = new byte[samplesPerChannel, doTask.Channels.Count];
// 生成方波（50% 占空比）
for (int i = 0; i < samplesPerChannel; i++)
{
    writeValue[i, 0] = (byte)(i < samplesPerChannel / 2 ? 1 : 0);
    writeValue[i, 1] = (byte)(i < samplesPerChannel / 4 ? 1 : 0);
}

doTask.WriteData(writeValue, (uint)writeValue.GetLength(0), -1);
doTask.Start();

// ... 循环输出 ...

doTask.Stop();
doTask.Channels.Clear();
```

#### 连续非循环（流式）输出（DOMode.ContinuousNoWrapping）

```csharp
doTask.Mode = DOMode.ContinuousNoWrapping;
doTask.WriteData(initialBuffer, -1);
doTask.Start();
while (generating)
{
    doTask.WriteData(nextBuffer, -1);  // 持续补充
}
doTask.Stop();
```

#### 有限点输出（DOMode.Finite）

```csharp
doTask.Mode = DOMode.Finite;
doTask.UpdateRate = 100000;
doTask.SamplesToUpdate = writeValue.GetLength(0);
doTask.WriteData(writeValue, (uint)writeValue.GetLength(0), -1);
doTask.Start();
```

#### DO 数字触发

```csharp
doTask.Trigger.Type = DOTriggerType.Digital;
doTask.Trigger.Digital.Source = DODigitalTriggerSource.PFI0;
doTask.Trigger.Digital.Edge = DigitalTriggerEdge.Rising;
```

---

## 计数器输入（CI）

### 任务类：`JY5325CITask`

> ⚠️ **特别注意**：CITask 构造函数与其他 Task **不同**，需要额外传入 `counterID`（0 或 1）。

```csharp
// 正确：必须指定计数器 ID（0 或 1）
JY5325CITask ciTask = new JY5325CITask(slotNumber, counterID);
```

#### 关键属性

| 属性                    | 类型                    | 说明                                        |
| ----------------------- | ----------------------- | ------------------------------------------- |
| `Type`                  | `CIType`                | 计数器测量类型（见下表）                    |
| `Mode`                  | `CIMode`                | Single / Finite / Continuous                |
| `SampleRate`            | `double`                | 采样率（仅 Internal/External 时钟模式使用） |
| `SamplesToAcquire`      | `int`                   | Finite 模式下采集点数                       |
| `AvailableSamples`      | `ulong`                 | 缓冲区可读点数                              |
| `SampleClock.Source`    | `CISampleClockSource`   | Internal / External / Implicit              |
| `Trigger.Type`          | `CITriggerType`         | Immediate / Digital / Soft                  |
| `EdgeCounting`          | 子对象                  | 边沿计数参数配置                            |
| `FrequencyMeas`         | 子对象                  | 频率测量参数                                |
| `PeriodMeas`            | 子对象                  | 周期测量参数                                |
| `PulseMeas`             | 子对象                  | 脉宽测量参数                                |
| `SemiPeriodMeas`        | 子对象                  | 半周期测量参数                              |
| `TwoEdgeSeparation`     | 子对象                  | 双沿分离测量参数                            |
| `QuadEncoder`           | 子对象                  | 正交编码器参数                              |
| `TwoPulseEncoder`       | 子对象                  | 双脉冲编码器参数                            |

#### CIType 测量类型

| 类型                          | 功能描述                                        |
| ----------------------------- | ----------------------------------------------- |
| `CIType.EdgeCounting`         | 边沿计数                                        |
| `CIType.FrequencyMeas`        | 频率测量                                        |
| `CIType.PeriodMeas`           | 周期测量                                        |
| `CIType.PulseMeas`            | 脉宽测量（高/低电平时间）                       |
| `CIType.SemiPeriodMeas`       | 半周期测量                                      |
| `CIType.TwoEdgeSeparation`    | 双沿分离测量                                    |
| `CIType.QuadEncoder`          | 正交编码器（A/B 两路）                          |
| `CIType.TwoPulseEncoder`      | 双脉冲编码器                                    |

#### CISampleClockSource

| 源          | 说明                                                          |
| ----------- | ------------------------------------------------------------- |
| `Internal`  | 内部 100 MHz 时基 + 采样率分频                                |
| `External`  | 外部时钟（通过 `SampleClock.External.Terminal` 指定）         |
| `Implicit`  | 由被测信号自身触发采样（频率/周期测量常用，无需额外时钟）     |

### 边沿计数示例（EdgeCounting + Continuous）

```csharp
using JY5325;

int slotNum = 0;
int counterID = 0;
uint[] readValue = new uint[1000];

JY5325CITask ciTask = new JY5325CITask(slotNum, counterID);
ciTask.Type = CIType.EdgeCounting;
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Internal;
ciTask.SampleRate = 1000;  // 每秒采样 1000 次

// 计数方向：CountUp（向上）/ CountDown（向下）
ciTask.EdgeCounting.Direction = CountDirection.CountUp;
ciTask.EdgeCounting.InitialCount = 0;
ciTask.EdgeCounting.Edge = CounterEdge.Rising;

ciTask.Start();

while (acquiring)
{
    if (ciTask.AvailableSamples >= (ulong)readValue.Length)
    {
        ciTask.ReadData(ref readValue, readValue.Length, -1);
        // readValue 为累计计数值
    }
}

ciTask.Stop();
ciTask.Channels.Clear();
```

### 频率测量示例（FrequencyMeas + Continuous）

```csharp
int slotNum = 0;
int counterID = 0;
double[] readFreq = new double[100];

JY5325CITask ciTask = new JY5325CITask(slotNum, counterID);
ciTask.Type = CIType.FrequencyMeas;
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Implicit;

// 频率范围（Hz）
ciTask.FrequencyMeas.MinFrequency = 1;
ciTask.FrequencyMeas.MaxFrequency = 1000000;

ciTask.Start();

if (ciTask.AvailableSamples >= (ulong)readFreq.Length)
{
    ciTask.ReadData(ref readFreq, readFreq.Length, -1);
    // readFreq 单位 Hz
}

ciTask.Stop();
ciTask.Channels.Clear();
```

### 周期测量示例

```csharp
JY5325CITask ciTask = new JY5325CITask(0, 0);
ciTask.Type = CIType.PeriodMeas;
ciTask.Mode = CIMode.Finite;
ciTask.SampleClock.Source = CISampleClockSource.Implicit;
ciTask.SamplesToAcquire = 100;

ciTask.PeriodMeas.MinPeriod = 1e-6;   // 最小周期 1μs
ciTask.PeriodMeas.MaxPeriod = 1.0;    // 最大周期 1s

ciTask.Start();
double[] readPeriod = new double[100];
ciTask.ReadData(ref readPeriod, readPeriod.Length, -1);  // 单位 秒
ciTask.Stop();
ciTask.Channels.Clear();
```

### 脉宽测量示例（高电平/低电平时间）

```csharp
JY5325CITask ciTask = new JY5325CITask(0, 0);
ciTask.Type = CIType.PulseMeas;
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Implicit;

ciTask.PulseMeas.MinPulseWidth = 1e-7;
ciTask.PulseMeas.MaxPulseWidth = 1.0;

ciTask.Start();

// 读取结构：CIPulse[] { HighTime, LowTime }
CIPulse[] pulses = new CIPulse[100];
ciTask.ReadData(ref pulses, pulses.Length, -1);
foreach (var p in pulses)
    Console.WriteLine($"High={p.HighTime*1e6:F2}μs, Low={p.LowTime*1e6:F2}μs");

ciTask.Stop();
ciTask.Channels.Clear();
```

### 正交编码器示例（QuadEncoder）

```csharp
JY5325CITask ciTask = new JY5325CITask(0, 0);
ciTask.Type = CIType.QuadEncoder;
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Internal;
ciTask.SampleRate = 1000;

ciTask.QuadEncoder.EncodingType = EncoderType.X4;  // X1 / X2 / X4
ciTask.QuadEncoder.InitialCount = 0;
ciTask.QuadEncoder.PulsesPerRevolution = 1024;

ciTask.Start();
int[] readEncoder = new int[1000];
ciTask.ReadData(ref readEncoder, readEncoder.Length, -1);  // 位置计数
ciTask.Stop();
ciTask.Channels.Clear();
```

### 双沿分离测量（TwoEdgeSeparation）

```csharp
JY5325CITask ciTask = new JY5325CITask(0, 0);
ciTask.Type = CIType.TwoEdgeSeparation;
ciTask.Mode = CIMode.Finite;
ciTask.SamplesToAcquire = 100;
ciTask.SampleClock.Source = CISampleClockSource.Implicit;

ciTask.TwoEdgeSeparation.FirstEdge = CounterEdge.Rising;
ciTask.TwoEdgeSeparation.SecondEdge = CounterEdge.Falling;

ciTask.Start();
double[] readTime = new double[100];
ciTask.ReadData(ref readTime, readTime.Length, -1);  // 两沿间时间差（秒）
ciTask.Stop();
ciTask.Channels.Clear();
```

### CI 数字触发

```csharp
ciTask.Trigger.Type = CITriggerType.Digital;
ciTask.Trigger.Digital.Source = CIDigitalTriggerSource.PFI0;
ciTask.Trigger.Digital.Edge = DigitalTriggerEdge.Rising;
```

---

## 计数器输出（CO）

### 任务类：`JY5325COTask`

> ⚠️ 与 CITask 相同，构造函数需要 `counterID`：
> `new JY5325COTask(int slotNumber, int counterID)`

#### 关键属性

| 属性             | 类型              | 说明                                          |
| ---------------- | ----------------- | --------------------------------------------- |
| `Mode`           | `COMode`          | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `InitialDelay`   | `double`          | 初始延时（秒）                                |
| `IdleState`      | `COIdleState`     | Low / High（空闲电平）                        |
| `Trigger.Type`   | `COTriggerType`   | Immediate / Digital / Soft                    |

#### COPulseType 脉冲描述方式

| 类型                      | 参数 1   | 参数 2     | 说明                            |
| ------------------------- | -------- | ---------- | ------------------------------- |
| `HighLowTime`             | 高电平时间 (s) | 低电平时间 (s) | 按时间指定                  |
| `HighLowTick`             | 高 tick 数 | 低 tick 数 | 按 100MHz 时基 tick 数指定     |
| `DutyCycleFrequency`      | 频率 (Hz)  | 占空比 (0..1) | 按频率+占空比指定            |

#### `COPulse` 构造

```csharp
// 签名：COPulse(COPulseType type, double param1, double param2, int count)
var pulse = new COPulse(COPulseType.DutyCycleFrequency, 5000.0, 0.5, 20);
// 5 kHz、50% 占空比、输出 20 个周期
```

### 单脉冲输出示例（COMode.Single）

```csharp
JY5325COTask coTask = new JY5325COTask(0, 0);
coTask.Mode = COMode.Single;
coTask.IdleState = COIdleState.Low;
coTask.InitialDelay = 0;

var pulse = new COPulse(COPulseType.HighLowTime, 0.001, 0.001, 1);  // 1ms 高 + 1ms 低
coTask.WriteData(new[] { pulse }, 1);
coTask.Start();
```

### 有限脉冲序列输出（COMode.Finite）

```csharp
JY5325COTask coTask = new JY5325COTask(0, 0);
coTask.Mode = COMode.Finite;
coTask.IdleState = COIdleState.Low;

COPulse[] pulses = new COPulse[]
{
    new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, 100),
    new COPulse(COPulseType.DutyCycleFrequency, 2000.0, 0.25, 50),
};

coTask.WriteData(pulses, pulses.Length);
coTask.Start();

while (!coTask.IsDone) System.Threading.Thread.Sleep(10);
coTask.Stop();
```

### 连续循环 PWM 输出（COMode.ContinuousWrapping）

```csharp
JY5325COTask coTask = new JY5325COTask(0, 0);
coTask.Mode = COMode.ContinuousWrapping;
coTask.IdleState = COIdleState.Low;
coTask.InitialDelay = 0;

COPulse[] pulses = new COPulse[]
{
    new COPulse(COPulseType.HighLowTime,     0.5, 0.5, 10),
    new COPulse(COPulseType.DutyCycleFrequency, 5.0, 0.5, 20),
    new COPulse(COPulseType.HighLowTick, 1000000, 1000000, 30),
};

coTask.WriteData(pulses, pulses.Length);
coTask.Start();

// ... 循环输出 ...

coTask.Stop();
```

### CO 数字触发

```csharp
coTask.Trigger.Type = COTriggerType.Digital;
coTask.Trigger.Digital.Source = CODigitalTriggerSource.PFI0;
coTask.Trigger.Digital.Edge = DigitalTriggerEdge.Rising;
```

---

## 录制模式（AI Record）

JY5325 AI 支持硬盘流式录制，适用于长时间、高速、大数据量的波形保存。

### 录制属性

| 属性                 | 类型           | 说明                                           |
| -------------------- | -------------- | ---------------------------------------------- |
| `Record.Mode`        | `RecordMode`   | Finite（有限长度）/ Infinite（无限长度）       |
| `Record.FilePath`    | `string`       | 保存文件路径（.bin）                           |
| `Record.FileFormat`  | `FileFormat`   | Bin（二进制）                                  |
| `Record.SamplesToRecord` | `long`     | Finite 模式下录制总点数                        |

### 录制时预览方法

```csharp
// 每通道取最近 N 点用于实时显示（不影响磁盘写入）
aiTask.GetRecordPreviewData(ref previewBuf, samplesPerChannel, timeout);

// 查询录制状态
var status = aiTask.GetRecordStatus();
```

### 无限长度录制示例（Infinite Streaming）

```csharp
using JY5325;

JY5325AITask aiTask = new JY5325AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.Record.Mode = RecordMode.Infinite;
aiTask.SampleRate = 5000000;  // 5 MS/s

string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
aiTask.Record.FilePath = $@"D:\RecordData\{ts}.bin";

aiTask.Start();

// 实时预览（不影响磁盘写入）
double[,] previewBuf = new double[10000, aiTask.Channels.Count];
while (recording)
{
    if (aiTask.AvailableSamples >= (ulong)previewBuf.GetLength(0))
    {
        aiTask.GetRecordPreviewData(ref previewBuf, previewBuf.GetLength(0), 1000);
        // 绘图等
    }
}

aiTask.Stop();  // 自动 flush 到磁盘
aiTask.Channels.Clear();
```

### 有限长度录制示例（Finite Streaming）

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.Mode = RecordMode.Finite;
aiTask.Record.SamplesToRecord = 1_000_000_000L;  // 录制 10 亿点后自动停止
aiTask.Record.FilePath = @"D:\data\test.bin";
aiTask.SampleRate = 5000000;

aiTask.Start();

while (!aiTask.IsDone)
{
    // 预览 or 查询状态
    System.Threading.Thread.Sleep(100);
}

aiTask.Stop();
```

### Bin 文件回放（Playback）

录制完成后的 `.bin` 文件可通过 `Winform AI Data Playback` 示例程序读取，按通道数、数据类型解析。

---

## 多卡同步（Multi-Card Sync）

JY5325 支持 PXIe 机箱内的多卡参考时钟同步与触发路由。

### 同步拓扑（SyncTopology）

| 拓扑              | 说明                                     |
| ----------------- | ---------------------------------------- |
| `Independent`     | 独立运行（默认）                         |
| `Master`          | 主卡：输出同步脉冲/触发                  |
| `Slave`           | 从卡：接收同步脉冲/触发                  |

### 关键属性

```csharp
// 任务级同步配置
aiTask.Sync.Topology             = SyncTopology.Master;       // 或 Slave
aiTask.Sync.TriggerRouting       = SyncTriggerRouting.PXI_Trig0;
aiTask.Sync.PulseRouting         = SyncPulseRouting.PXI_Trig1;

// 设备级参考时钟配置（需在任务启动前 Commit）
aiTask.Device.ReferenceClock.Source            = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
aiTask.Device.ReferenceClock.Commit();
```

### 典型同步启动顺序（Master/Slave）

```
1) 先配置 Slave：Topology=Slave、参考时钟=PXIe_Clk100 → Commit → Start
2) 再配置 Master：Topology=Master、参考时钟=PXIe_Clk100 → Commit → Start
3) Master Start 会通过 PXI_Trig0 线路触发所有 Slave
```

### 完整示例：双卡参考时钟同步 + 相位差测量

```csharp
using JY5325;

int masterSlot = 2, slaveSlot = 4;
int chnID = 0;
double sampleRate = 5000000;
int samples = 100000;

// --- Master ---
var masterTask = new JY5325AITask(masterSlot);
masterTask.AddChannel(chnID, -10, 10);
masterTask.Mode = AIMode.Finite;
masterTask.SampleRate = sampleRate;
masterTask.SamplesToAcquire = samples;
masterTask.Trigger.Type = AITriggerType.Immediate;

masterTask.Sync.Topology       = SyncTopology.Master;
masterTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
masterTask.Device.ReferenceClock.Commit();

// --- Slave ---
var slaveTask = new JY5325AITask(slaveSlot);
slaveTask.AddChannel(chnID, -10, 10);
slaveTask.Mode = AIMode.Finite;
slaveTask.SampleRate = sampleRate;
slaveTask.SamplesToAcquire = samples;

slaveTask.Sync.Topology       = SyncTopology.Slave;
slaveTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slaveTask.Device.ReferenceClock.Commit();

// --- 启动顺序：先 Slave，后 Master ---
slaveTask.Start();
masterTask.Start();

double[] masterBuf = new double[samples];
double[] slaveBuf  = new double[samples];
masterTask.ReadData(ref masterBuf, samples, 10000);
slaveTask.ReadData(ref slaveBuf,   samples, 10000);

slaveTask.Stop();
masterTask.Stop();
slaveTask.Channels.Clear();
masterTask.Channels.Clear();
```

### DI/DO 同步（通过采样时钟共享）

```csharp
// DO 侧导出采样时钟到 PFI 线
doTask.SignalExport.Add(DOSignalExportSource.SampleClock, SignalExportDestination.PFI0);

// DI 侧使用同一 PFI 线作为外部时钟
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
```

---

## 信号路由（SignalExport）

各任务都支持将内部时钟/触发信号导出到外部终端，供其他任务/板卡使用。

### 导出源（XXSignalExportSource）

| 任务 | 导出源枚举                    | 可选值                                   |
| ---- | ----------------------------- | ---------------------------------------- |
| AI   | `AISignalExportSource`        | SampleClock / StartTrigger / ReferenceTrigger |
| AO   | `AOSignalExportSource`        | SampleClock / StartTrigger               |
| DI   | `DISignalExportSource`        | SampleClock / StartTrigger               |
| DO   | `DOSignalExportSource`        | SampleClock / StartTrigger               |
| CI   | `CISignalExportSource`        | StartTrigger                             |

### 导出目的（SignalExportDestination）

- `PFI0` ~ `PFI7`
- `PXI_Trig0` ~ `PXI_Trig7`

### 示例

```csharp
// 将 AO 采样时钟导出到 PFI0
aoTask.SignalExport.Add(AOSignalExportSource.SampleClock, SignalExportDestination.PFI0);

// 将 AI Start 触发导出到 PXI_Trig0（用于多卡同步）
aiTask.SignalExport.Add(AISignalExportSource.StartTrigger, SignalExportDestination.PXI_Trig0);
```

---

## 异常处理

所有驱动调用均应包裹 try-catch，至少捕获 `JYDriverException`：

```csharp
try
{
    aiTask.Start();
}
catch (JYDriverException ex)
{
    // ex.ErrorCode / ex.Message
    MessageBox.Show($"Driver error {ex.ErrorCode}: {ex.Message}");
}
catch (Exception ex)
{
    MessageBox.Show($"System error: {ex.Message}");
}
```

### 常见错误场景

| 场景                         | 处理建议                                                       |
| ---------------------------- | -------------------------------------------------------------- |
| 槽位号错误                   | 通过 JYTEK 设备管理器确认实际 Slot Number                      |
| 通道号越界（AI/AO 超过 7）   | 检查通道索引 0..7                                              |
| 量程不在支持列表             | AI 只支持 ±1/±2/±5/±10V；AO 只支持 ±10V                        |
| 采样率超过最大值             | AI ≤ 5 MS/s，AO ≤ 1 MS/s，DIO ≤ 10 MHz                         |
| 连续 NoWrapping 模式缓冲区空 | 提前写入足够数据或提高写入频率                                 |
| Record 磁盘写速不足          | 使用 SSD、降低采样率或通道数                                   |
| 多卡同步 Start 顺序颠倒      | 必须先 Start Slave，再 Start Master                            |

### 资源释放的标准范式

```csharp
try
{
    aiTask.Stop();
}
catch { /* ignore */ }
finally
{
    aiTask?.Channels.Clear();
    aiTask = null;
}
```

---

## 完整枚举参考

### AIMode / AOMode / DIMode / DOMode / CIMode / COMode

```csharp
enum AIMode   { Single, Finite, Continuous, Record }
enum AOMode   { Single, Finite, ContinuousWrapping, ContinuousNoWrapping }
enum DIMode   { Single, Finite, Continuous }
enum DOMode   { Single, Finite, ContinuousWrapping, ContinuousNoWrapping }
enum CIMode   { Single, Finite, Continuous }
enum COMode   { Single, Finite, ContinuousWrapping, ContinuousNoWrapping }
```

### 触发类型

```csharp
enum AITriggerType { Immediate, Digital, Analog, Soft }
enum AOTriggerType { Immediate, Digital, Soft }
enum DITriggerType { Immediate, Digital, Soft }
enum DOTriggerType { Immediate, Digital, Soft }
enum CITriggerType { Immediate, Digital, Soft }
enum COTriggerType { Immediate, Digital, Soft }

enum AITriggerMode { Start, Reference }  // Start=启动触发，Reference=参考触发（带预采样）
enum DigitalTriggerEdge { Rising, Falling }
enum AnalogTriggerCondition { RisingSlope, FallingSlope, HighLevel, LowLevel, InsideRegion, OutsideRegion }
```

### 时钟源

```csharp
enum AISampleClockSource { Internal, External }
enum AOSampleClockSource { Internal, External }
enum DISampleClockSource { Internal, External }
enum DOSampleClockSource { Internal, External }
enum CISampleClockSource { Internal, External, Implicit }

enum ClockTerminal
{
    PFI0, PFI1, PFI2, PFI3, PFI4, PFI5, PFI6, PFI7,
    PXI_Trig0, PXI_Trig1, PXI_Trig2, PXI_Trig3, PXI_Trig4, PXI_Trig5, PXI_Trig6, PXI_Trig7,
    AI_SampleClock, AO_SampleClock, DI_SampleClock, DO_SampleClock,
    CI_0_SampleClock, CI_1_SampleClock,
    CO_0_Output, CO_1_Output
}

enum ClockEdge { Rising, Falling }
```

### 数字/模拟触发源

```csharp
enum AIDigitalTriggerSource
{
    PFI0..PFI7,
    PXI_Trig0..PXI_Trig7,
    AO_StartTrig, DI_StartTrig, DI_ReferenceTrig, DO_StartTrig,
    CIO_0_StartTrig, CIO_1_StartTrig,
    CO_0_Out, CO_1_Out
}

enum AIAnalogTriggerSource
{
    Channel_0, Channel_1, Channel_2, Channel_3,
    Channel_4, Channel_5, Channel_6, Channel_7
}
```

### 同步与参考时钟

```csharp
enum SyncTopology         { Independent, Master, Slave }
enum SyncTriggerRouting   { PXI_Trig0..PXI_Trig7 }
enum SyncPulseRouting     { PXI_Trig0..PXI_Trig7 }
enum ReferenceClockSource { Internal, External }
enum ExternalReferenceClockTerminal { PXIe_Clk100, /* ... */ }
```

### 录制

```csharp
enum RecordMode  { Finite, Infinite }
enum FileFormat  { Bin }
```

### CI 相关

```csharp
enum CIType
{
    EdgeCounting, FrequencyMeas, PeriodMeas, PulseMeas,
    SemiPeriodMeas, TwoEdgeSeparation, QuadEncoder, TwoPulseEncoder
}
enum CountDirection { CountUp, CountDown }
enum CounterEdge    { Rising, Falling }
enum EncoderType    { X1, X2, X4 }
```

### CO 相关

```csharp
enum COPulseType  { HighLowTime, HighLowTick, DutyCycleFrequency }
enum COIdleState  { Low, High }
```

---

## 完整 API 速查表

### `JY5325AITask`

| 签名                                                           | 说明                     |
| -------------------------------------------------------------- | ------------------------ |
| `JY5325AITask(int slotNumber)`                                 | 构造（按槽位号）         |
| `JY5325AITask(string boardName)`                               | 构造（按板卡名）         |
| `void AddChannel(int chnId, double low, double high)`          | 单通道                   |
| `void AddChannel(int[] chnIds, double low, double high)`       | 多通道相同量程           |
| `void AddChannel(int[] chnIds, double[] lows, double[] highs)` | 多通道独立量程           |
| `void Start()` / `void Stop()`                                 | 启动/停止                |
| `void ReadSinglePoint(ref double value, int chnId)`            | Single 模式读单点        |
| `void ReadSinglePoint(ref double[] values)`                    | Single 模式读全通道      |
| `void ReadData(ref double[], int samples, int timeout)`        | 单通道读取（一维）       |
| `void ReadData(ref double[,], int samples, int timeout)`       | 多通道读取（二维）       |
| `void GetRecordPreviewData(ref double[,], int, int)`           | Record 预览              |
| `RecordStatus GetRecordStatus()`                               | Record 状态查询          |

### `JY5325AOTask`

| 签名                                                   | 说明              |
| ------------------------------------------------------ | ----------------- |
| `JY5325AOTask(int slotNumber)`                         | 构造              |
| `void AddChannel(int chnId)` / `AddChannel(int[])`     | 添加通道          |
| `void WriteSinglePoint(double value, int chnId)`       | 单点输出          |
| `void WriteData(double[], int timeout)`                | 一维数据写入      |
| `void WriteData(double[,], int timeout)`               | 二维（多通道）写入 |
| `void Start()` / `void Stop()`                         | 启动/停止         |
| `bool IsDone`                                          | Finite 完成标志   |

### `JY5325DITask`

| 签名                                                             | 说明            |
| ---------------------------------------------------------------- | --------------- |
| `JY5325DITask(int slotNumber)`                                   | 构造            |
| `void AddChannel(int lineId)`                                    | 添加通道        |
| `void ReadSinglePoint(ref byte[] values)`                        | 单点读取        |
| `void ReadData(ref byte[,], uint samples, int timeout)`          | 连续/有限读取   |
| `void ReadData(ref ushort[], uint samples, int timeout)`         | 打包格式读取    |

### `JY5325DOTask`

| 签名                                                             | 说明            |
| ---------------------------------------------------------------- | --------------- |
| `JY5325DOTask(int slotNumber)`                                   | 构造            |
| `void AddChannel(int lineId)`                                    | 添加通道        |
| `void WriteSinglePoint(byte[] values)`                           | 单点输出        |
| `void WriteData(byte[,], uint samples, int timeout)`             | 有限/连续输出   |

### `JY5325CITask`

| 签名                                                             | 说明                      |
| ---------------------------------------------------------------- | ------------------------- |
| `JY5325CITask(int slotNumber, int counterID)`                    | **构造（需 counterID）**  |
| `void ReadData(ref uint[], int, int)`                            | 边沿计数读取              |
| `void ReadData(ref double[], int, int)`                          | 频率/周期/脉宽/时间差读取 |
| `void ReadData(ref int[], int, int)`                             | 编码器位置读取            |
| `void ReadData(ref CIPulse[], int, int)`                         | 脉冲 (High/Low) 读取      |

### `JY5325COTask`

| 签名                                                             | 说明                     |
| ---------------------------------------------------------------- | ------------------------ |
| `JY5325COTask(int slotNumber, int counterID)`                    | **构造（需 counterID）** |
| `void WriteData(COPulse[] pulses, int count)`                    | 写入脉冲描述             |

### `JY5325Device`

| 属性/方法                                                        | 说明                 |
| ---------------------------------------------------------------- | -------------------- |
| `ReferenceClock.Source`                                          | 参考时钟源           |
| `ReferenceClock.External.Terminal`                               | 外部参考时钟终端     |
| `ReferenceClock.Commit()`                                        | 提交时钟配置         |

---

## 常见使用模式速查

### 模式 1：AI 单点读取
```
new AITask → AddChannel → Mode=Single → ReadSinglePoint → Channels.Clear
```

### 模式 2：AI 连续采集 + 实时处理
```
new AITask → AddChannel → Mode=Continuous → SampleRate → Start
→ 轮询 AvailableSamples → ReadData → 处理 → ... → Stop → Channels.Clear
```

### 模式 3：AO 连续循环输出波形
```
new AOTask → AddChannel → Mode=ContinuousWrapping → UpdateRate → SamplesToUpdate
→ WriteData(一周期波形) → Start → ... → Stop → Channels.Clear
```

### 模式 4：Counter 边沿计数
```
new CITask(slot, counterID) → Type=EdgeCounting → Mode=Continuous
→ EdgeCounting.Direction/InitialCount → SampleClock.Source → Start
→ ReadData(uint[]) → Stop → Channels.Clear
```

### 模式 5：CO 连续 PWM 输出
```
new COTask(slot, counterID) → Mode=ContinuousWrapping
→ new COPulse(DutyCycleFrequency, f, duty, count)
→ WriteData(COPulse[]) → Start → ... → Stop
```

### 模式 6：长时间流式录制
```
new AITask → AddChannel → Mode=Record → Record.Mode=Infinite
→ Record.FilePath → SampleRate → Start
→ 循环 GetRecordPreviewData → ... → Stop
```

### 模式 7：多卡参考时钟同步
```
Slave:  Sync.Topology=Slave  → TriggerRouting=PXI_Trig0
        → Device.ReferenceClock.External.Terminal=PXIe_Clk100 → Commit → Start
Master: Sync.Topology=Master → TriggerRouting=PXI_Trig0
        → Device.ReferenceClock.External.Terminal=PXIe_Clk100 → Commit → Start
```

---

## 参考资料

- 驱动安装路径：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5325\`
- 范例代码：`D:\JYTEK_Work\Examples\C#\DAQ\JY5325.Examples\`
  - `Analog Input/`：17 个 AI 示例（Single / Finite / Continuous + 各种触发）
  - `Analog Output/`：12 个 AO 示例（Single / Finite / ContinuousWrapping / NoWrapping）
  - `Digital Input/`：7 个 DI 示例
  - `Digital Output/`：10 个 DO 示例
  - `Counter Input/`：28 个 CI 示例（涵盖所有 CIType）
  - `Counter Output/`：10 个 CO 示例
  - `Record/`：3 个录制/回放示例
  - `Sync/`：5 个多卡/多通道同步示例
- 测试面板：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5325\Bin\JY5325TestPanel.exe`
- 产品型录：`简仪产品型录` PCIe/PXIe-5325 章节
