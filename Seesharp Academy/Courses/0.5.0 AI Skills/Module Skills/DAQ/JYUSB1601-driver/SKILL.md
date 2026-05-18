---
name: jyusb1601-driver
description: 提供 JYTEK JYUSB-1601 多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、模拟输出（AO）单点/有限/连续输出、数字输入/输出（DI/DO）单点/有限/连续模式、计数器输入/输出（CI/CO）边沿计数/频率/周期/脉冲测量/脉冲生成、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、录制模式（Finite/Infinite Streaming）。当用户使用 JYUSB1601、USB-1601、JYUSB1601AITask、JYUSB1601AOTask、JYUSB1601CITask、JYUSB1601COTask、JYUSB1601DITask、JYUSB1601DOTask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record 开发数据采集、信号生成、计数器测量、自动化测试应用时自动应用。
---

# JYUSB-1601 驱动开发指引

## 环境要求

- **驱动 DLL**：C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB1601\Bin/JYUSB1601.dll（引用到 .csproj）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JYUSB1601;`
- **设备名称**：在 JYTEK 设备管理器中设置的板卡别名（Board Name），如 `"Dev1"`

## 硬件规格速查

| 功能       | 规格                             |
| -------- | ------------------------------ |
| AI 通道数   | 16 SE / 8 差分                   |
| AI 最大采样率 | 单通道 250 kSa/s，N 通道 250k/N Sa/s |
| AI 输入范围  | ±10V / ±5V / ±2.5V             |
| AO 通道数   | 2 通道                           |
| AO 最大更新率 | 单通道 2.86 MSa/s，双通道 2 MSa/s     |
| DIO      | 8 线输入 + 8 线输出                  |
| 计数器      | 2 个（CTR0 / CTR1）               |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JYUSB1601AITask("Dev1");   // 1. 创建
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

### 任务类：`JYUSB1601AITask`

#### 关键属性

| 属性                   | 类型                    | 说明                                    |
| -------------------- | --------------------- | ------------------------------------- |
| `Mode`               | `AIMode`              | Single / Finite / Continuous / Record |
| `SampleRate`         | `double`              | 每通道采样率（Sa/s）                          |
| `SamplesToAcquire`   | `int`                 | Finite 模式下每通道采集点数                     |
| `AvailableSamples`   | `ulong`               | 缓冲区中可读取的点数（非 Single 模式）               |
| `Bandwidth`          | `AIBandwidth`         | \_15KHz / \_39KHz / \_80KHz（默认）       |
| `SampleClock.Source` | `AISampleClockSource` | Internal（默认）/ External                |
| `Trigger.Type`       | `AITriggerType`       | Immediate（默认）/ Digital / Soft         |

#### AddChannel 重载

```csharp
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);
// 单通道 + 接线方式
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh, AITerminal terminal);
// chnId = -1 → 添加全部通道
// AITerminal: RSE（默认）/ Differential
```

#### 读取数据重载

```csharp
// Single 模式 — 单通道
aiTask.ReadSinglePoint(ref double readValue, int channel);
// Single 模式 — 所有通道
aiTask.ReadSinglePoint(ref double[] readValues);

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);
// Finite/Continuous — 多通道（列存储，每列一个通道）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
// timeout = -1 → 永久等待
```

#### AI 四种模式速查

| 模式           | 典型配置                           | 读取方式                                     |
| ------------ | ------------------------------ | ---------------------------------------- |
| `Single`     | `Mode=AIMode.Single`           | 轮询 `ReadSinglePoint`                     |
| `Finite`     | `+SamplesToAcquire=N`          | `WaitUntilDone(-1)` 后 `ReadData`         |
| `Continuous` | `+SampleRate`                  | Timer 轮询 `AvailableSamples` → `ReadData` |
| `Record`     | `+Record.FilePath/Mode/Length` | `GetRecordPreviewData` 预览，数据写入文件         |

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
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 10000; // 期望外部时钟频率
```

---

## 模拟输出（AO）

### 任务类：`JYUSB1601AOTask`

#### 关键属性

| 属性                | 类型                    | 说明                                                          |
| ----------------- | --------------------- | ----------------------------------------------------------- |
| `Mode`            | `AOMode`              | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`      | `double`              | 更新率（Sa/s）                                                   |
| `SamplesToUpdate` | `int`                 | Finite/Continuous 模式下每通道输出点数                                |
| `CompleteState`   | `OutputCompleteState` | Zero / Hold（默认，保持末值）                                        |

#### AddChannel / 写入

```csharp
aoTask.AddChannel(int chnId);   // chnId=-1 添加全部，AO 仅 0~1 通道
aoTask.AddChannel(int[] chnsId);

// Single 模式
aoTask.WriteSinglePoint(double[] writeValues);   // 所有通道
aoTask.WriteSinglePoint(double writeValue, int channel);  // 指定通道

// 缓冲模式（Finite / Continuous）
aoTask.WriteData(double[] buf, int timeout);     // 单通道
aoTask.WriteData(double[,] buf, int timeout);    // 多通道（列存储）
```

#### AO 四种模式速查

| 模式                     | 说明                       |
| ---------------------- | ------------------------ |
| `Single`               | 写寄存器，立即输出直流电压            |
| `Finite`               | 一次性输出 N 点波形，完成后保持末值或归零   |
| `ContinuousWrapping`   | 循环播放缓冲区中固定波形（不可实时更新）     |
| `ContinuousNoWrapping` | 持续从缓冲区消耗数据，可实时追加（适合流式输出） |

#### 连续环形输出标准流程

```csharp
aoTask = new JYUSB1601AOTask("Dev1");
aoTask.AddChannel(0);
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 100000;
aoTask.SamplesToUpdate = 1000;          // 缓冲区大小
aoTask.WriteData(waveBuffer, -1);       // 先写数据
aoTask.Start();                         // 再启动
```

---

## 数字 I/O（DI / DO）

### 任务类：`JYUSB1601DITask` / `JYUSB1601DOTask`

```csharp
// 数字输入（8 线，DIO_0~DIO_7）
var diTask = new JYUSB1601DITask("Dev1");
diTask.AddChannel(0);       // 添加第 0 线
diTask.Start();
bool value;
diTask.ReadSinglePoint(ref value, 0);   // 读单线
diTask.Stop();

// 数字输出
var doTask = new JYUSB1601DOTask("Dev1");
doTask.AddChannel(0);
doTask.Start();
doTask.WriteSinglePoint(true, 0);       // 写单线
doTask.Stop();
```

---

## 计数器输入（CI）

### 任务类：`JYUSB1601CITask`

**构造**：`new JYUSB1601CITask(string boardName, int channel)` — channel 0 或 1

#### CI 测量类型

| CIType              | 读取方法                                                         | 返回值            |
| ------------------- | ------------------------------------------------------------ | -------------- |
| `EdgeCounting`      | `ReadSinglePoint(ref uint count)`                            | 边沿计数值          |
| `Frequency`         | `ReadSinglePoint(ref double meas, int timeout)`              | 频率（Hz）         |
| `Period`            | `ReadSinglePoint(ref double meas, int timeout)`              | 周期（秒）          |
| `Pulse`             | `ReadSinglePoint(ref double m1, ref double m2, int timeout)` | 低电平时长、高电平时长（秒） |
| `TwoEdgeSeparation` | `ReadSinglePoint(ref double m1, ref double m2, int timeout)` | 两边沿间隔（秒）       |
| `QuadEncoder`       | `ReadSinglePoint(ref uint count)`                            | 编码器计数          |

```csharp
// 频率测量示例
ciTask = new JYUSB1601CITask("Dev1", 0);
ciTask.Type = CIType.Frequency;
ciTask.Start();
double freq;
ciTask.ReadSinglePoint(ref freq, -1);   // timeout=-1 永久等待
ciTask.Stop();
```

---

## 计数器输出（CO）

### 任务类：`JYUSB1601COTask`

```csharp
var coTask = new JYUSB1601COTask("Dev1", 0);
// 按频率+占空比配置脉冲（1kHz，50% 占空比，输出 -1=无限个）
var pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, -1);
coTask.WriteSinglePoint(pulse);
coTask.Start();
// ... 等待 ...
coTask.Stop();
```

`COPulseType` 三种构造方式：

| 类型                   | param1     | param2     |
| -------------------- | ---------- | ---------- |
| `DutyCycleFrequency` | 频率（Hz）     | 占空比（0~1）   |
| `HighLowTime`        | 高电平时长（秒）   | 低电平时长（秒）   |
| `HighLowTick`        | 高电平 Tick 数 | 低电平 Tick 数 |

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
aiTask = new JYUSB1601AITask("Dev1");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 10000;

// 配置录制参数
aiTask.Record.FilePath = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;       // 仅支持 Bin 格式
aiTask.Record.Mode = RecordMode.Finite;          // Finite / Infinite
aiTask.Record.Length = 10.0;                     // 录制时长（秒），Finite 有效

aiTask.Start();

// 可在录制期间预览数据
double[] preview = new double[1000];
aiTask.GetRecordPreviewData(ref preview, 1000, -1);
easyChartX1.Plot(preview);

// 等待录制完成
double recordedLen;
bool recordDone;
do
{
    aiTask.GetRecordStatus(ref recordedLen, ref recordDone);
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

### 录制模式方法

| 方法 | 说明 |
|------|------|
| `GetRecordPreviewData(ref double[] buf, int samples, int timeout)` | 获取单通道预览数据 |
| `GetRecordPreviewData(ref double[,] buf, int samples, int timeout)` | 获取多通道预览数据 |
| `GetRecordStatus(ref double recordedLength, ref bool recordDone)` | 获取录制状态 |

---

## 信号导出（Signal Export）

将内部信号输出到 DIO 引脚（如将 AI 采样时钟导出用于同步）：

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,
                        SignalExportDestination.DIO_0);
```

---

## 常见错误处理

| 异常代码                                         | 原因                     | 处理建议              |
| -------------------------------------------- | ---------------------- | ----------------- |
| `OpenDeviceFailed`                           | 板卡未连接或名称错误             | 检查设备管理器别名         |
| `NoChannelAdded`                             | 未调用 AddChannel 就 Start | 在 Start 前添加通道     |
| `BufferDataOverflow`                         | 读取速度慢于采集速度             | 增大读取频率或减小采样率      |
| `ReadDataTimeout`                            | timeout 内未读到足够数据       | 增大 timeout 或检查采样率 |
| `TaskHasStartedCannotPerformTheSetOperation` | Task 运行中修改参数           | Stop 后再修改         |
| `SampleRateParameterInvalid`                 | 采样率超过硬件上限              | 检查通道数和最大采样率       |

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)


---

# 完整 API 参考

# JYUSB-1601 驱动 API 完整参考

## 命名空间与引用

```csharp
using JYUSB1601;
// DLL 路径：Driver/JYUSB1601.dll
```

---

## 设备信息类 `JYUSB1601Device`

通过 `aiTask.Device` / `aoTask.Device` 访问：

| 属性                           | 类型     | 说明          |
| ---------------------------- | ------ | ----------- |
| `BoardClockRate`             | double | 板卡主时钟频率（Hz） |
| `DiffChannelCount`           | int    | 差分通道数（8）    |
| `SEChannelCount`             | int    | 单端通道数（16）   |
| `MaxSampleRateSingleChannel` | double | AI 单通道最大采样率 |
| `MaxSampleRateMutilChannel`  | double | AI 多通道最大采样率 |
| `AOChannelCount`             | int    | AO 通道数（2）   |
| `MaxUpdateRate`              | double | AO 最大更新率    |
| `SerialNumber`               | string | 设备序列号       |

---

## JYUSB1601AITask — 模拟输入任务

### 构造函数

```csharp
new JYUSB1601AITask()               // 按槽位号创建
new JYUSB1601AITask(string boardName)  // 按板卡别名创建（推荐）
```

### 属性

| 属性                  | 类型                | 默认值      | 说明                                    |
| ------------------- | ----------------- | -------- | ------------------------------------- |
| `Mode`              | `AIMode`          | —        | Single / Finite / Continuous / Record |
| `SampleRate`        | `double`          | 10000    | 每通道采样率（Sa/s）                          |
| `SamplesToAcquire`  | `int`             | —        | Finite 模式采集点数/通道                      |
| `BufLenInSamples`   | `int`             | —        | 缓冲区容量（点/通道），须 ≥ SamplesToAcquire      |
| `AvailableSamples`  | `ulong`           | —        | 缓冲区可读点数（非 Single 模式有效）                |
| `TransferedSamples` | `ulong`           | —        | 已传输点数（非 Single 模式有效）                  |
| `Bandwidth`         | `AIBandwidth`     | `_80KHz` | \_15KHz / \_39KHz / \_80KHz           |
| `SampleClock`       | `AISampleClock`   | —        | 时钟配置对象                                |
| `Trigger`           | `AITrigger`       | —        | 触发配置对象                                |
| `Record`            | `AIRecord`        | —        | 录制配置对象                                |
| `SignalExport`      | `AISignalExport`  | —        | 信号导出配置                                |
| `Channels`          | `List<AIChannel>` | —        | 已添加的通道列表                              |

### 方法

#### AddChannel

```csharp
void AddChannel(int chnId, double rangeLow, double rangeHigh)
void AddChannel(int chnId, double rangeLow, double rangeHigh, AITerminal terminal)
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh, AITerminal terminal)
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh, AITerminal terminal)
// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2.5
// rangeHigh: 10 / 5 / 2.5
// terminal: AITerminal.RSE（默认） / AITerminal.Differential
```

#### 控制

```csharp
void Start()
void Stop()
void WaitUntilDone(int timeout)   // timeout=-1 永久等待，仅 Finite 有效
void SendSoftwareTrigger()        // 软触发
```

#### 读取数据（电压值）

```csharp
// Single 模式
void ReadSinglePoint(ref double readValue, int channel)
void ReadSinglePoint(ref double[] readValues)              // 所有通道

// 缓冲模式（Finite / Continuous）
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)
void ReadData(ref double[] buf, int timeout)
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)  // 多通道
void ReadData(ref double[,] buf, int timeout)
void ReadData(IntPtr buf, int samplesPerChannel, int timeout)         // 非托管
```

#### 读取原始数据（Int16）

```csharp
void ReadRawSinglePoint(ref short readValue, int channel)
void ReadRawSinglePoint(ref short[] readValues)
void ReadRawData(ref short[] buf, int samplesPerChannel, int timeout)
void ReadRawData(ref short[,] buf, int samplesPerChannel, int timeout)
```

#### 录制模式

```csharp
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout)
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout)
void GetRecordStatus(ref double recordedLength, ref bool recordDone)
```

---

## AIMode 枚举

| 值            | 说明                              |
| ------------ | ------------------------------- |
| `Single`     | 软件触发单点读取，可循环调用 ReadSinglePoint  |
| `Finite`     | 采集固定点数后停止，通过 WaitUntilDone 等待完成 |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取   |
| `Record`     | 数据流式写入文件，支持预览                   |

## AITerminal 枚举

| 值              | 说明                                |
| -------------- | --------------------------------- |
| `RSE`          | 单端（Referenced Single-Ended），16 通道 |
| `Differential` | 差分，8 通道，共模抑制更好                    |

## AIBandwidth 枚举

| 值        | 带宽            |
| -------- | ------------- |
| `_15KHz` | 15 kHz 低通     |
| `_39KHz` | 39 kHz 低通     |
| `_80KHz` | 80 kHz 低通（默认） |

---

## AISampleClock — 时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.Internal;   // 默认
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 10000.0;         // 外部时钟期望频率
```

---

## AITrigger — 触发配置

```csharp
// 数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;   // DIO_0~DIO_15
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;    // Rising/Falling/HighLevel/LowLevel

// 软触发
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.SendSoftwareTrigger();   // 在 Start 后调用触发
```

## AITriggerType 枚举

| 值           | 说明        |
| ----------- | --------- |
| `Immediate` | 立即触发（默认）  |
| `Digital`   | 数字边沿/电平触发 |
| `Soft`      | 软件触发      |

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath   = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;           // 仅支持 Bin 格式
aiTask.Record.Mode       = RecordMode.Finite;        // Finite / Infinite
aiTask.Record.Length     = 10.0;                     // 录制时长（秒），Finite 有效
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

## AISignalExport — 信号导出

```csharp
// 将 AI 采样时钟导出到 DIO_0
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.DIO_0);
// 导出起始触发
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.DIO_1);
// 清除某个导出
aiTask.SignalExport.Clear(SignalExportDestination.DIO_0);
aiTask.SignalExport.ClearAll();
```

---

## JYUSB1601AOTask — 模拟输出任务

### 构造函数

```csharp
new JYUSB1601AOTask()
new JYUSB1601AOTask(string boardName)
```

### 属性

| 属性                      | 类型                    | 默认值     | 说明                                                    |
| ----------------------- | --------------------- | ------- | ----------------------------------------------------- |
| `Mode`                  | `AOMode`              | —       | Single/Finite/ContinuousWrapping/ContinuousNoWrapping |
| `UpdateRate`            | `double`              | 1000000 | 更新率（Sa/s）                                             |
| `SamplesToUpdate`       | `int`                 | —       | 缓冲区点数/通道（Finite/Continuous 有效）                        |
| `AvaliableLenInSamples` | `int`                 | —       | 缓冲区剩余可写点数                                             |
| `CompleteState`         | `OutputCompleteState` | `Hold`  | Zero/Hold                                             |
| `TransferedSamples`     | `ulong`               | —       | 已输出点数                                                 |
| `SampleClock`           | `AOSampleClock`       | —       | 时钟配置                                                  |
| `Trigger`               | `AOTrigger`           | —       | 触发配置                                                  |
| `SignalExport`          | `AOSignalExport`      | —       | 信号导出                                                  |

### 方法

```csharp
void AddChannel(int chnId)          // chnId: 0/1/-1（全部）
void AddChannel(int[] chnsId)
void RemoveChannel(int chnId)

void WriteSinglePoint(double[] writeValues)         // Single 模式，所有通道
void WriteSinglePoint(double writeValue, int channel) // Single 模式，指定通道

void WriteData(double[] buf, int timeout)           // 单通道缓冲写入
void WriteData(double[,] buf, int timeout)          // 多通道（列存储）
void WriteData(IntPtr buf, int samplesPerChannel, int timeout)

void Start()
void Stop()
void WaitUntilDone(int timeout)     // Finite 模式等待完成
void SendSoftwareTrigger()
```

## AOMode 枚举

| 值                      | 说明                                   |
| ---------------------- | ------------------------------------ |
| `Single`               | 写入寄存器，立即输出静态电压                       |
| `Finite`               | 输出 SamplesToUpdate 点后停止              |
| `ContinuousWrapping`   | 循环输出固定缓冲区（ContinuousWrapping 不可实时更新） |
| `ContinuousNoWrapping` | 持续消耗缓冲区数据，可实时追加（流式）                  |

## ContinuousWrapping vs NoWrapping

|      | Wrapping     | NoWrapping                |
| ---- | ------------ | ------------------------- |
| 写入时机 | Start 前一次性写入 | 可在 Start 后持续追加            |
| 适用场景 | 固定波形循环输出     | 实时可变波形                    |
| 注意   | 缓冲区耗尽后循环     | 缓冲区耗尽报 BufferDataDownflow |

---

## JYUSB1601CITask — 计数器输入任务

### 构造函数

```csharp
new JYUSB1601CITask(int channel)                    // 按槽位
new JYUSB1601CITask(string boardName, int channel)  // channel: 0 或 1
```

### CIType 枚举与对应配置

| CIType              | 配置对象                       | 说明                                |
| ------------------- | -------------------------- | --------------------------------- |
| `EdgeCounting`      | `ciTask.EdgeCounting`      | 边沿计数，InitialCount/Direction/Pause |
| `Frequency`         | `ciTask.FrequencyMeas`     | 频率测量（Hz）                          |
| `Period`            | `ciTask.PeriodMeas`        | 周期测量（秒）                           |
| `Pulse`             | `ciTask.PulseMeas`         | 脉冲宽度（高/低电平时长，秒）                   |
| `TwoEdgeSeparation` | `ciTask.TwoEdgeSeparation` | 两边沿分离时间（秒）                        |
| `QuadEncoder`       | `ciTask.QuadEncoder`       | 正交编码器（X2/X4）                      |

### 读取方法

```csharp
void ReadSinglePoint(ref uint count)                              // EdgeCounting/QuadEncoder
void ReadSinglePoint(ref double measurement, int timeout)         // Frequency/Period
void ReadSinglePoint(ref double m1, ref double m2, int timeout)   // Pulse/TwoEdgeSeparation
```

### EdgeCounting 配置

```csharp
ciTask.Type = CIType.EdgeCounting;
ciTask.EdgeCounting.InitialCount = 0;
ciTask.EdgeCounting.Direction = CountDirection.Up;    // Up / Down
ciTask.EdgeCounting.Pause.ActivePolarity = LevelPolarity.LowLevel;  // 暂停触发
ciTask.EdgeCounting.OutEvent.Threshold = 1000;       // 达到阈值输出脉冲
```

---

## JYUSB1601COTask — 计数器输出任务

### 构造函数

```csharp
new JYUSB1601COTask(int channel)
new JYUSB1601COTask(string boardName, int channel)
```

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
void WriteSinglePoint(COPulse pulse)  // 写入脉冲参数（Start 前或无限模式运行中均可）
void Start()
void Stop()
void WaitUntilDone(int timeout)
```

---

## JYUSB1601DITask / JYUSB1601DOTask

### DI — 数字输入（8 线，DIO_0~DIO_7）

```csharp
void AddChannel(int lineNum)
void AddChannel(int[] linesNum)
void ReadSinglePoint(ref bool readValue, int line)      // 读单线
void ReadSinglePoint(ref bool[] readValues)             // 读所有线
void Start()
void Stop()
```

### DO — 数字输出（8 线）

```csharp
void AddChannel(int lineNum)
void WriteSinglePoint(bool writeValue, int line)        // 写单线
void WriteSinglePoint(bool[] writeValues)               // 写所有线
void Start()
void Stop()
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

| 枚举值                                          | 含义               |
| -------------------------------------------- | ---------------- |
| `OpenDeviceFailed`                           | 打开设备失败（未连接/名称错误） |
| `CloseDeviceFailed`                          | 关闭设备失败           |
| `NoChannelAdded`                             | 未添加通道            |
| `StartTaskFailed`                            | 启动 Task 失败       |
| `StopTaskFailed`                             | 停止 Task 失败       |
| `TaskHasNotStarted`                          | Task 未启动即读写      |
| `TaskHasStartedCannotPerformTheSetOperation` | 运行中修改参数          |
| `BufferDataOverflow`                         | 采集缓冲区溢出          |
| `BufferDataDownflow`                         | 输出缓冲区数据不足        |
| `ReadDataTimeout`                            | 读取超时             |
| `WriteDataTimeout`                           | 写入超时             |
| `SampleRateParameterInvalid`                 | 采样率超限            |
| `ChannelNumberParameterInvalid`              | 无效通道号            |
| `ChannelInputRangeParameterInvalid`          | 无效输入量程           |
| `TriggerParameterInvalid`                    | 触发参数无效           |
| `CounterParametersInvalid`                   | 计数器参数无效          |

---

## JYUSB1601Miscellaneous — 校准控制

```csharp
// 禁用 AI 校准（调试用，默认启用）
JYUSB1601Miscellaneous.SetAICalibrationState(aiTask.Device, true);
// 恢复
JYUSB1601Miscellaneous.SetAICalibrationState(aiTask.Device, false);
```

---

## 设备硬件规格

| 参数           | 值                  |
| ------------ | ------------------ |
| AI 分辨率       | 16-bit             |
| AI SE 通道     | 16                 |
| AI 差分通道      | 8                  |
| AI 单通道最大采样率  | 250 kSa/s          |
| AI N 通道最大采样率 | 250k/N Sa/s        |
| AI 输入量程      | ±10V / ±5V / ±2.5V |
| AO 通道        | 2                  |
| AO 分辨率       | 16-bit             |
| AO 输出范围      | ±10V               |
| AO 单通道最大更新率  | 2.86 MSa/s         |
| DIO          | 8 线输入 + 8 线输出      |
| 计数器          | 2 个 32-bit         |
| 接口           | USB                |


---

# 完整代码示例

﻿# JYUSB-1601 代码示例集

> 所有示例均来自 `JYUSB-1601-1.Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点采集（Console）

```csharp
using JYUSB1601;

// 1. 创建 AI Task（按板卡别名）
JYUSB1601AITask aiTask = new JYUSB1601AITask("Dev1");

// 2. 设置模式
aiTask.Mode = AIMode.Single;

// 3. 添加通道：通道 0，量程 ±10V，单端接法
aiTask.AddChannel(0, -10, 10, AITerminal.RSE);

// 4. 启动 Task
aiTask.Start();

// 5. 循环读取单点
double readValue = 0;
aiTask.ReadSinglePoint(ref readValue, 0);   // 读取通道 0 的电压值
Console.WriteLine($"Channel 0: {readValue} V");

// 6. 停止并清除通道
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 2：AI 连续采集（WinForm + Timer）

```csharp
private JYUSB1601AITask aiTask;
private double[] readBuffer;

// 启动按钮
private void button_start_Click(object sender, EventArgs e)
{
    // 1. 创建 Task
    aiTask = new JYUSB1601AITask(textBox_cardName.Text);

    // 2. 添加通道（量程从 ComboBox 选择）
    aiTask.AddChannel(0, -10, 10, AITerminal.RSE);

    // 3. 配置连续模式参数
    aiTask.Mode = AIMode.Continuous;
    aiTask.SampleRate = (double)numericUpDown_sampleRate.Value;

    // 4. 启动 Task
    aiTask.Start();

    // 5. 分配读取缓冲区
    readBuffer = new double[(int)numericUpDown_samples.Value];

    // 6. 启动 Timer（每 10ms 刷新一次）
    timer_FetchData.Enabled = true;
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
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
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
```

---

## 示例 3：AI 有限采集（多通道）

```csharp
private JYUSB1601AITask aiTask;
private double[,] readValue;   // [采样点, 通道] 列存储

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JYUSB1601AITask("Dev1");

    // 添加多个通道（通道 0、1、2，相同量程）
    int[] channels = { 0, 1, 2 };
    aiTask.AddChannel(channels, -10, 10, AITerminal.RSE);

    // 配置有限模式
    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 1000;   // 每通道采集 1000 点
    aiTask.SampleRate = 10000;

    aiTask.Start();

    // 分配多通道缓冲区 [采样点数, 通道数]
    readValue = new double[1000, 3];

    // WaitUntilDone 等待采集完成（超时 5000ms）
    aiTask.WaitUntilDone(5000);

    // 读取所有通道数据
    aiTask.ReadData(ref readValue, 1000, -1);

    aiTask.Stop();
    aiTask.Channels.Clear();

    // readValue[i, ch] 即第 i 采样点、ch 通道的电压值
}
```

---

## 示例 4：AI 数字触发有限采集

```csharp
aiTask = new JYUSB1601AITask("Dev1");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 1000;
aiTask.SampleRate = 50000;

// 配置数字触发：DIO_0 上升沿触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发信号到来后自动开始采集
aiTask.WaitUntilDone(-1);

double[] buf = new double[1000];
aiTask.ReadData(ref buf, 1000, -1);
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 5：AI 软触发采集

```csharp
aiTask = new JYUSB1601AITask("Dev1");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 500;
aiTask.SampleRate = 10000;
aiTask.Trigger.Type = AITriggerType.Soft;

aiTask.Start();

// 在需要时手动发送软触发
aiTask.SendSoftwareTrigger();

aiTask.WaitUntilDone(5000);
double[] buf = new double[500];
aiTask.ReadData(ref buf, 500, -1);
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 6：AI 外部时钟采集

```csharp
aiTask = new JYUSB1601AITask("Dev1");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;

// 使用外部时钟（外部信号从板卡时钟输入引脚引入）
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 10000;  // 告知驱动期望频率

aiTask.Start();
// 后续读取同连续模式...
```

---

## 示例 7：AI 录制模式（数据流写入文件）

```csharp
aiTask = new JYUSB1601AITask("Dev1");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 50000;

// 配置录制参数
aiTask.Record.FilePath   = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode       = RecordMode.Finite;
aiTask.Record.Length     = 10.0;   // 录制 10 秒

aiTask.Start();

// 可在录制期间预览数据
double[] preview = new double[1000];
aiTask.GetRecordPreviewData(ref preview, 1000, -1);
easyChartX1.Plot(preview);

// 等待录制完成
double recordedLen;
bool recordDone;
do
{
    aiTask.GetRecordStatus(ref recordedLen, ref recordDone);
} while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 8：AO 单点输出（Console）

```csharp
JYUSB1601AOTask aoTask = new JYUSB1601AOTask("Dev1");

// AO 通道仅有 0 和 1，添加两个通道
aoTask.AddChannel(0);
aoTask.AddChannel(1);

aoTask.Mode = AOMode.Single;
aoTask.Start();

// 输出指定电压（索引与通道一一对应）
double[] writeValues = { 3.0, 0.0 };    // CH0=3V，CH1=0V
aoTask.WriteSinglePoint(writeValues);

// 动态更新输出值
writeValues[0] = 5.0;
aoTask.WriteSinglePoint(writeValues);

aoTask.Stop();
aoTask.Channels.Clear();
```

---

## 示例 9：AO 连续环形输出（Wrapping）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;  // 需要 Generation 类生成波形

private JYUSB1601AOTask aoTask;
private double[] writeBuffer;

private void button_start_Click(object sender, EventArgs e)
{
    aoTask = new JYUSB1601AOTask("Dev1");
    aoTask.AddChannel(0);
    aoTask.Mode = AOMode.ContinuousWrapping;
    aoTask.UpdateRate = 100000;     // 100 kSa/s
    aoTask.SamplesToUpdate = 1000;  // 缓冲区大小 1000 点

    // 使用 SeeSharpTools 生成正弦波
    writeBuffer = new double[1000];
    Generation.SineWave(ref writeBuffer, 5.0, 0, 50.0, aoTask.UpdateRate);
    // 参数：幅值 5V，初相 0°，频率 50Hz，采样率 100kSa/s

    // 先写数据，再 Start（Wrapping 模式要求）
    aoTask.WriteData(writeBuffer, -1);
    aoTask.Start();

    // 在 EasyChartX 上预览输出波形
    easyChartX_AO.Plot(writeBuffer);
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (aoTask != null)
    {
        aoTask.Stop();
        aoTask.Channels.Clear();
    }
    easyChartX_AO.Clear();
}
```

---

## 示例 10：AO 有限输出（带触发）

```csharp
aoTask = new JYUSB1601AOTask("Dev1");
aoTask.AddChannel(0);
aoTask.Mode = AOMode.Finite;
aoTask.UpdateRate = 50000;
aoTask.SamplesToUpdate = 500;

// 数字触发：DIO_0 上升沿触发输出
aoTask.Trigger.Type = AOTriggerType.Digital;
aoTask.Trigger.Digital.Source = AODigitalTriggerSource.DIO_0;
aoTask.Trigger.Digital.Edge   = AODigitalTriggerEdge.Rising;

double[] waveform = new double[500];
Generation.SineWave(ref waveform, 5.0, 0, 100.0, 50000);
aoTask.WriteData(waveform, -1);

aoTask.Start();              // 等待触发信号
aoTask.WaitUntilDone(-1);   // 输出完成后返回
aoTask.Stop();
aoTask.Channels.Clear();
```

---

## 示例 11：CI 频率测量（WinForm + Timer）

```csharp
private JYUSB1601CITask ciTask;
private double measValue;

private void button_start_Click(object sender, EventArgs e)
{
    // 计数器通道 0（CTR0_GATE 引脚连接被测信号）
    ciTask = new JYUSB1601CITask("Dev1", 0);
    ciTask.Type = CIType.Frequency;

    ciTask.Start();
    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    try
    {
        // timeout=0 表示立即读取当前测量值（非阻塞）
        ciTask.ReadSinglePoint(ref measValue, 0);
        label_freq.Text = $"{measValue:F2} Hz";
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
    timer_FetchData.Enabled = true;
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (ciTask != null) ciTask.Stop();
}
```

---

## 示例 12：CI 边沿计数

```csharp
ciTask = new JYUSB1601CITask("Dev1", 0);
ciTask.Type = CIType.EdgeCounting;
ciTask.EdgeCounting.InitialCount = 0;        // 初始计数值
ciTask.EdgeCounting.Direction = CountDirection.Up;  // 上升计数

ciTask.Start();

uint count = 0;
ciTask.ReadSinglePoint(ref count);   // 读取当前计数值
Console.WriteLine($"Count: {count}");

ciTask.Stop();
```

---

## 示例 13：CI 脉冲宽度测量

```csharp
ciTask = new JYUSB1601CITask("Dev1", 0);
ciTask.Type = CIType.Pulse;

ciTask.Start();

double highTime = 0, lowTime = 0;
// 阻塞等待一次完整脉冲测量，timeout=-1 永久等待
ciTask.ReadSinglePoint(ref highTime, ref lowTime, -1);
Console.WriteLine($"高电平: {highTime * 1000:F3} ms，低电平: {lowTime * 1000:F3} ms");

ciTask.Stop();
```

---

## 示例 14：CO 脉冲输出

```csharp
JYUSB1601COTask coTask = new JYUSB1601COTask("Dev1", 0);

// 输出 1kHz，占空比 50%，无限脉冲
var pulse = new COPulse(COPulseType.DutyCycleFrequency,
    1000.0,   // 频率 1 kHz
    0.5,      // 占空比 50%
    -1);      // -1 = 无限输出

coTask.WriteSinglePoint(pulse);
coTask.Start();

Console.WriteLine("脉冲输出中，按任意键停止...");
Console.ReadKey();

coTask.Stop();
```

---

## 示例 15：DI / DO 数字 I/O

```csharp
// 数字输入：读取 DIO_0
var diTask = new JYUSB1601DITask("Dev1");
diTask.AddChannel(0);
diTask.Start();
bool diValue = false;
diTask.ReadSinglePoint(ref diValue, 0);
Console.WriteLine($"DIO_0 = {diValue}");
diTask.Stop();
diTask.Channels.Clear();

// 数字输出：控制 DIO_0
var doTask = new JYUSB1601DOTask("Dev1");
doTask.AddChannel(0);
doTask.Start();
doTask.WriteSinglePoint(true, 0);   // 置高
System.Threading.Thread.Sleep(500);
doTask.WriteSinglePoint(false, 0);  // 置低
doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 16：AI 信号导出（同步两个 Task）

```csharp
// 将 AI 采样时钟导出到 DIO_0，供外部设备同步
aiTask = new JYUSB1601AITask("Dev1");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// 信号导出：AI SampleClock → DIO_0
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,
                        SignalExportDestination.DIO_0);

aiTask.Start();
// DIO_0 引脚现在输出与 AI 采样时钟同步的脉冲信号
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

### 量程与接线方式选择

```csharp
// 量程选择原则：选择略大于信号峰值的量程，提高精度
// 单端（RSE）：16 通道，适合信号源与地共用
// 差分（Differential）：8 通道，适合抗共模干扰

aiTask.AddChannel(0, -5, 5, AITerminal.Differential);  // ±5V 差分
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timerFetch != null) timerFetch.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```

---

## 示例 7.1AI Record 无限录制模式

无限录制的：ֶ的ֹͣ

```csharp
private JYUSB1601AITask aiTask;
private double[] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JYUSB1601AITask("Dev1");
        
        // 添加通道
        aiTask.AddChannel(0, -10, 10);
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;  // 50 kS/s
        
        // 的无限录制
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制
        
        aiTask.Start();
        
        previewData = new double[1000];
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
    
    if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
        // 显示预览数据...
    }
    
    timer_Preview.Enabled = true;
}

private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    aiTask.Stop();
    aiTask.Channels.Clear();
    
    button_Start.Enabled = true;
    button_Stop.Enabled = false;
}
```

---

## 示例 7.2录制的ݻط

读取已录制的 bin 文件回放示例

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[] playbackData;
private ulong totalSamples;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    
    if (!File.Exists(fileBrowser.FileName))
    {
        MessageBox.Show("文件的的");
        return;
    }
    
    // 数据格式为
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 1;  // 通道
    totalSamples = (ulong)(fileInfo.Length / sizeof(double) / channelCount);
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    playbackData = new double[1000];
    
    button_OpenFile.Enabled = false;
    button_Playback.Enabled = true;
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        // 读取的
        for (int i = 0; i < playbackData.Length; i++)
        {
            if (playbackStream.Position < playbackStream.Length)
            {
                playbackData[i] = playbackReader.ReadDouble();
            }
        }
        // easyChartX1.Plot(playbackData);
    }
    catch (EndOfStreamException)
    {
        timer_Playback.Enabled = false;
        playbackReader.Close();
        playbackStream.Close();
        MessageBox.Show("回放的");
    }
}
```

---

## 示例 7.1：AI Record 有限录制模式（Finite Streaming）

录制固定时长的数据到文件，录制完成后自动停止：

```csharp
private JYUSB1601AITask aiTask;
private double[] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JYUSB1601AITask("Dev1");
        aiTask.AddChannel(0, -10, 10);
        
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;
        
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 10.0;              // 录制 10 秒
        
        aiTask.Start();
        previewData = new double[1000];
        timer_Preview.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    double recordedLength;
    bool recordDone;
    aiTask.GetRecordStatus(out recordedLength, out recordDone);
    
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
        if (aiTask.AvailableSamples >= 1000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
        }
    }
    timer_Preview.Enabled = true;
}
```

---

## 示例 7.2：AI Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止：

```csharp
private JYUSB1601AITask aiTask;
private double[] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JYUSB1601AITask("Dev1");
        aiTask.AddChannel(0, -10, 10);
        
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;
        
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制模式
        
        aiTask.Start();
        previewData = new double[1000];
        timer_Preview.Enabled = true;
        
        button_Start.Enabled = false;
        button_Stop.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
    }
    timer_Preview.Enabled = true;
}

private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    try { if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); } }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    button_Start.Enabled = true;
    button_Stop.Enabled = false;
}
```

---

## 示例 7.3：录制数据回放

读取已录制的 bin 文件并回放显示：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[] playbackData;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    if (!File.Exists(fileBrowser.FileName)) { MessageBox.Show("文件不存在"); return; }
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    playbackData = new double[1000];
    
    button_OpenFile.Enabled = false;
    button_Playback.Enabled = true;
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        for (int i = 0; i < playbackData.Length; i++)
            if (playbackStream.Position < playbackStream.Length)
                playbackData[i] = playbackReader.ReadDouble();
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
