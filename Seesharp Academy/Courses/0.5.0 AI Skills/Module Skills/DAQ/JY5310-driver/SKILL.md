---
name: jy5310-driver
description: 提供 JYTEK JY5310 系列（PXIe/PCIe/USB-5310）多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、数字输入/输出（DI/DO）单点/有限/连续模式、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、多卡同步、录制模式（Finite/Infinite Streaming）。当用户使用 JY5310、PXIe-5310、PCIe-5310、USB-5310、JY5310AITask、JY5310DITask、JY5310DOTask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record、RecordMode.Finite、RecordMode.Infinite 开发数据采集、信号采集、波形记录、自动化测试应用时自动应用。
---

# JY5310 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5310\Bin\JY5310.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5310\Bin\JY5310.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY5310;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                  | 规格                                    |
| --------------------- | --------------------------------------- |
| AI 分辨率             | 16-bit                                  |
| AI 通道数             | 16 通道同步差分输入                      |
| AI 最大采样率         | 5 MS/s (PCIe/PXIe-5315) / 2 MS/s (PCIe/PXIe-5312) |
| AI 输入范围           | ±10.83V（参考 AIGND）                    |
| AI 输入耦合           | 直流耦合                                |
| AI 通道间隔离         | 相邻通道: -90 dB / 不相邻通道: -105 dB   |
| AI FIFO 缓存          | 256 M 采样值                            |
| DI/DO 通道            | 8 端口（每端口 2 通道，共 16 通道）       |
| 触发方式              | 模拟 / 数字 / 软件触发                   |
| 接口类型              | PCIe / PXIe                             |

### 产品型号

- **PCIe-5312**：16位，2MS/s/ch，16 DIO，PCIe 同步采样多功能 I/O 模块
- **PCIe-5315**：16位，5MS/s/ch，16 DIO，PCIe 同步采样多功能 I/O 模块
- **PXIe-5312**：16位，2MS/s/ch，16 DIO，PXIe 同步采样多功能 I/O 模块
- **PXIe-5315**：16位，5MS/s/ch，16 DIO，PXIe 同步采样多功能 I/O 模块

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY5310AITask(0);          // 1. 创建（按槽位号）
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

### 任务类：`JY5310AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Single / Finite / Continuous / Record   |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s）                    |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数             |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（非 Single 模式）  |
| `SampleClock.Source` | `AISampleClockSource`   | Internal（默认）/ External              |
| `Trigger.Type`       | `AITriggerType`         | Immediate（默认）/ Digital / Analog / Soft |
| `Trigger.Mode`       | `AITriggerMode`         | Start（默认）/ Reference                |

#### AddChannel 重载

```csharp
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);

// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2 / -1
// rangeHigh: 10 / 5 / 2 / 1
```

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
| `Continuous` | `+SampleRate`                 | Timer/多线程轮询 `AvailableSamples` → `ReadData`   |
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
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.AI0;     // AI0~AI15
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;                   // 触发电平（V）
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Start();
// 在需要时手动发送软触发
aiTask.SendSoftwareTrigger();
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;    // PFI0~PFI15
aiTask.SampleClock.External.ExpectedRate = 10000;              // 期望外部时钟频率
```

---

## 数字输入（DI）

### 任务类：`JY5310DITask`

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
bool[] readValue = new bool[2];
diTask.ReadSinglePoint(ref readValue, int portID);

// Continuous/Finite 模式
byte[,] dataBuf = new byte[samples, channels];
diTask.ReadData(ref dataBuf, uint samplesPerChannel, int timeout);
```

#### DI 连续模式示例

```csharp
var diTask = new JY5310DITask(0);
diTask.AddChannel(0);                    // 添加端口 0
diTask.Mode = DIMode.Continuous;
diTask.SampleRate = 10000;
diTask.Start();

// 在 Timer 中读取
byte[,] dataBuf = new byte[1000, 1];
if (diTask.AvailableSamples >= 1000)
{
    diTask.ReadData(ref dataBuf, 1000, -1);
}
```

---

## 数字输出（DO）

### 任务类：`JY5310DOTask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `DOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`         | `double`                | 更新率（Sa/s）                          |
| `Trigger.Type`       | `DOTriggerType`         | Immediate / Digital / Soft              |

#### 写入数据

```csharp
// Single 模式
bool[] writeValue = new bool[2] { true, false };
doTask.WriteSinglePoint(writeValue, int portID);

// Continuous/Finite 模式 — 多通道（列存储）
byte[,] writeBuf = new byte[samples, channels];
// ... 填充数据 ...
doTask.WriteData(writeBuf, uint samplesPerChannel, int timeout);
```

#### DO 连续环形输出标准流程

```csharp
doTask = new JY5310DOTask(0);
doTask.AddChannel(0);
doTask.Mode = DOMode.ContinuousWrapping;
doTask.UpdateRate = 100000;

// 生成数字波形数据
byte[,] writeBuf = new byte[1000, 1];
// ... 填充波形数据 ...

// 先写数据，再 Start（Wrapping 模式要求）
doTask.WriteData(writeBuf, 1000, -1);
doTask.Start();
```

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
aiTask = new JY5310AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 50000;

// 配置录制参数
aiTask.Record.FilePath = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;
aiTask.Record.Length = 10.0;              // 录制 10 秒

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

## 多卡同步

### AI 多卡采样时钟同步

多张 JY5310 板卡之间实现同步采集，主卡导出采样时钟和触发信号，从卡接收：

```csharp
// ===== 主卡配置（Slot 0）=====
var masterTask = new JY5310AITask(0);
masterTask.AddChannel(0, -10, 10);
masterTask.Mode = AIMode.Finite;
masterTask.SamplesToAcquire = 10000;
masterTask.SampleRate = 10000;

// 导出采样时钟到 PXI_Trig0
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

// 导出开始触发信号到 PXI_Trig1
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;
masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 1）=====
var slaveTask = new JY5310AITask(1);
slaveTask.AddChannel(0, -10, 10);
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 10000;

// 使用外部时钟（接收主卡的 PXI_Trig0）
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10000;

// 使用外部触发（接收主卡的 PXI_Trig1）
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();   // 从卡先启动，等待触发
task.Delay(100);     // 可选延迟，确保从卡就绪
masterTask.Start();  // 主卡启动，产生时钟和触发信号

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

**关键要点：**
- 主卡使用内部时钟（`AISampleClockSource.Internal`），从卡使用外部时钟（`AISampleClockSource.External`）
- 主卡通过 `SignalExport.Add()` 导出时钟和触发信号
- 启动顺序：**先从卡后主卡**，确保从卡先准备好接收信号
- 使用 `PXI_Trig0~PXI_Trig7` 作为同步信号线

### AI 多卡参考时钟同步

多张 JY5310 板卡共享外部参考时钟（PXIe_Clk100），实现高精度同步：

```csharp
// ===== 主卡配置（Slot 0）=====
var masterTask = new JY5310AITask(0);
masterTask.AddChannel(0, -10, 10);
masterTask.Mode = AIMode.Finite;
masterTask.SampleRate = 10000;
masterTask.SamplesToAcquire = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;

// 同步参数配置
masterTask.Sync.Topology = SyncTopology.Master;
masterTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;   // 路由采样时钟同步信号
masterTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;       // 路由触发同步信号

// 配置外部参考时钟（PXIe 100MHz 时钟）
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
masterTask.Device.ReferenceClock.Commit();

// ===== 从卡配置（Slot 1）=====
var slaveTask = new JY5310AITask(1);
slaveTask.AddChannel(0, -10, 10);
slaveTask.Mode = AIMode.Finite;
slaveTask.SampleRate = 10000;
slaveTask.SamplesToAcquire = 10000;
slaveTask.Trigger.Type = AITriggerType.Digital;

// 同步参数配置
slaveTask.Sync.Topology = SyncTopology.Slave;
slaveTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
slaveTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;

// 配置外部参考时钟
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slaveTask.Device.ReferenceClock.Commit();

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();   // 从卡先启动，等待触发
System.Threading.Thread.Sleep(100);  // 可选延迟
masterTask.Start();  // 主卡启动

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

**参考时钟同步 vs 采样时钟同步：**
- **参考时钟同步**：多卡共享同一个参考时钟源（如 PXIe_Clk100），各卡使用自己的 PLL 生成采样时钟，适合 PXIe  chassis
- **采样时钟同步**：主卡直接导出采样时钟信号，从卡直接接收，更简单直接
- 参考时钟同步需要配置 `SyncTopology`、`SyncTriggerRouting`、`SyncPulseRouting`
- 需要调用 `Device.ReferenceClock.Commit()` 提交参考时钟配置

### DI/DO 同步示例

DO 作为主设备，DI 使用 DO 的采样时钟和触发信号：

```csharp
// DI 配置（从设备）
diTask = new JY5310DITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Continuous;
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.DO_StartTrig;
diTask.Trigger.Digital.Edge = DIDigitalTriggerEdge.Rising;
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.DO_SampleClock;
diTask.SampleClock.External.ExpectedRate = 10000;

// DO 配置（主设备）
doTask = new JY5310DOTask(0);
doTask.AddChannel(1);
doTask.Mode = DOMode.ContinuousWrapping;
doTask.UpdateRate = 10000;
doTask.Trigger.Type = DOTriggerType.Immediate;
doTask.SampleClock.Source = DOSampleClockSource.Internal;

// 先启动 DI（等待触发），再启动 DO（产生触发）
diTask.Start();
doTask.Start();
```

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

# JY5310 驱动 API 完整参考

## 命名空间与引用

```csharp
using JY5310;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DAQ\JY5310\Bin\JY5310.dll
```

---

## 设备信息类 `JY5310Device`

通过 `aiTask.Device` 访问：

| 属性                           | 类型     | 说明          |
| ---------------------------- | ------ | ----------- |
| `BoardClockRate`             | double | 板卡主时钟频率（Hz） |
| `DiffChannelCount`           | int    | 差分通道数（8）    |
| `SEChannelCount`             | int    | 单端通道数（16）   |
| `MaxSampleRateSingleChannel` | double | AI 单通道最大采样率 |
| `MaxSampleRateMutilChannel`  | double | AI 多通道最大采样率 |
| `SerialNumber`               | string | 设备序列号       |

---

## JY5310AITask — 模拟输入任务

### 构造函数

```csharp
new JY5310AITask(int slotNumber)      // 按槽位号创建（推荐）
```

### 属性

| 属性                  | 类型                | 默认值   | 说明                                    |
| ------------------- | ----------------- | ----- | ------------------------------------- |
| `Mode`              | `AIMode`          | —     | Single / Finite / Continuous / Record |
| `SampleRate`        | `double`          | 10000 | 每通道采样率（Sa/s），同步采集模块每通道独立 |
| `SamplesToAcquire`  | `int`             | —     | Finite 模式采集点数/通道                      |
| `AvailableSamples`  | `ulong`           | —     | 缓冲区可读点数（非 Single 模式有效）                |
| `TransferedSamples` | `ulong`           | —     | 已传输点数（非 Single 模式有效）                  |
| `SampleClock`       | `AISampleClock`   | —     | 时钟配置对象                                |
| `Trigger`           | `AITrigger`       | —     | 触发配置对象                                |
| `Record`            | `AIRecord`        | —     | 录制配置对象                                |
| `Channels`          | `List<AIChannel>` | —     | 已添加的通道列表                              |

### 方法

#### AddChannel

```csharp
void AddChannel(int chnId, double rangeLow, double rangeHigh)
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh)
// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2 / -1
// rangeHigh: 10 / 5 / 2 / 1
```

#### 控制

```csharp
void Start()
void Stop()               // 停止采集，保留参数配置，可再次Start
void WaitUntilDone(int timeout)   // timeout=-1 永久等待，仅 Finite 有效
void SendSoftwareTrigger()        // 软触发
```

**注意：**
- Stop() 后参数保留，可直接再次 Start()
- 调用 Channels.Clear() 才会清除通道配置

#### 读取数据（电压值）

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

| 值            | 说明                              |
| ------------ | ------------------------------- |
| `Single`     | 软件触发单点读取，可循环调用 ReadSinglePoint  |
| `Finite`     | 采集固定点数后停止，通过 WaitUntilDone 等待完成 |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取   |
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
| `PFI0` ~ `PFI15`          | 前面板 PFI 引脚 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线   |
| `PXI_Star`                | PXI 星型触发   |
| `DI_SampleClock`          | DI 采样时钟    |
| `DO_SampleClock`          | DO 采样时钟    |
| `AI_SampleClock`          | AI 采样时钟    |

---

## AITrigger — 触发配置

### 属性

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;    // Digital / Analog / Soft / Immediate
aiTask.Trigger.Mode = AITriggerMode.Start;      // Start / Reference
```

### 数字触发

```csharp
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;     // PFI0~PFI15
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;       // Rising / Falling
```

### 模拟触发

```csharp
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.AI0;        // AI0~AI15
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;  // Edge / Hysteresis / Window

// Edge 模式
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;   // Rising / Falling
aiTask.Trigger.Analog.Edge.Threshold = 2.5;                      // 触发电平（V）

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

## JY5310DITask — 数字输入任务

### 构造函数

```csharp
new JY5310DITask(int slotNumber)
```

### 属性

| 属性                 | 类型              | 默认值 | 说明                           |
| ------------------ | --------------- | --- | ---------------------------- |
| `Mode`             | `DIMode`        | —   | Single / Finite / Continuous |
| `SampleRate`       | `double`        | —   | 采样率（Sa/s）                    |
| `AvailableSamples` | `ulong`         | —   | 缓冲区可读点数                      |
| `SampleClock`      | `DISampleClock` | —   | 时钟配置对象                       |
| `Trigger`          | `DITrigger`     | —   | 触发配置对象                       |

### 方法

```csharp
void AddChannel(int portID)                         // 添加端口（0~7）
void AddChannel(int[] portIDs)
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

## JY5310DOTask — 数字输出任务

### 构造函数

```csharp
new JY5310DOTask(int slotNumber)
```

### 属性

| 属性            | 类型              | 默认值 | 说明                                                    |
| ------------- | --------------- | --- | ----------------------------------------------------- |
| `Mode`        | `DOMode`        | —   | Single/Finite/ContinuousWrapping/ContinuousNoWrapping |
| `UpdateRate`  | `double`        | —   | 更新率（Sa/s）                                             |
| `SampleClock` | `DOSampleClock` | —   | 时钟配置对象                                                |
| `Trigger`     | `DOTrigger`     | —   | 触发配置对象                                                |

### 方法

```csharp
void AddChannel(int portID)                         // 添加端口（0~7）
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

| 枚举值                                          | 含义                |
| -------------------------------------------- | ----------------- |
| `OpenDeviceFailed`                           | 打开设备失败（未连接/槽位号错误） |
| `CloseDeviceFailed`                          | 关闭设备失败            |
| `NoChannelAdded`                             | 未添加通道             |
| `StartTaskFailed`                            | 启动 Task 失败        |
| `StopTaskFailed`                             | 停止 Task 失败        |
| `TaskHasNotStarted`                          | Task 未启动即读写       |
| `TaskHasStartedCannotPerformTheSetOperation` | Task 运行中修改参数      |
| `BufferDataOverflow`                         | 采集缓冲区溢出           |
| `BufferDataDownflow`                         | 输出缓冲区数据不足         |
| `ReadDataTimeout`                            | 读取超时              |
| `WriteDataTimeout`                           | 写入超时              |
| `SampleRateParameterInvalid`                 | 采样率超限             |
| `ChannelNumberParameterInvalid`              | 无效通道号             |
| `ChannelInputRangeParameterInvalid`          | 无效输入量程            |
| `TriggerParameterInvalid`                    | 触发参数无效            |

---

## 设备硬件规格

| 参数                  | 值                              |
| --------------------- | ------------------------------- |
| AI 分辨率             | 16-bit                          |
| AI 通道数             | 16 通道同步差分输入              |
| AI 单通道最大采样率   | 5 MS/s (PCIe/PXIe-5315)         |
|                       | 2 MS/s (PCIe/PXIe-5312)         |
| AI 输入量程           | ±10.83V（参考 AIGND）            |
| AI 输入接线方式       | 差分（DIFF）                    |
| AI 输入阻抗           | 高阻抗                          |
| AI 输入耦合           | 直流耦合                        |
| AI 通道间隔离         | 相邻通道: -90 dB                |
|                       | 不相邻通道: -105 dB             |
| AI FIFO 缓存          | 256 M 采样值                    |
| DI/DO 端口            | 8 端口（每端口 2 通道，共 16 通道）|
| 触发方式              | 模拟 / 数字 / 软件触发           |
| 模拟触发电压量程      | 5 采样值                        |
| 接口                  | PCIe / PXIe                     |

### 产品型号

| 型号        | 描述                                          |
| ----------- | --------------------------------------------- |
| PCIe-5312   | 16位，2MS/s/ch，16 DIO，PCIe 同步采样多功能 I/O 模块 |
| PCIe-5315   | 16位，5MS/s/ch，16 DIO，PCIe 同步采样多功能 I/O 模块 |
| PXIe-5312   | 16位，2MS/s/ch，16 DIO，PXIe 同步采样多功能 I/O 模块 |
| PXIe-5315   | 16位，5MS/s/ch，16 DIO，PXIe 同步采样多功能 I/O 模块 |


---

# 完整代码示例

﻿# JY5310 代码示例集

> 所有示例均来自 `JY5310_V3.1.5_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点采集（Console）

```csharp
using System;
using JY5310;

// 1. 创建 AI Task（按槽位号）
Console.WriteLine("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());
JY5310AITask aiTask = new JY5310AITask(boardNum);

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
using JY5310;

public partial class MainForm : Form
{
    private JY5310AITask aiTask;
    private double[] readBuffer;
    private double lowRange = -10;
    private double highRange = 10;

    // 启动按钮
    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            // 1. 创建 Task（槽位号 0）
            aiTask = new JY5310AITask(0);

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
private JY5310AITask aiTask;
private double[,] readValue;   // [采样点, 通道] 列存储

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JY5310AITask(0);

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
aiTask = new JY5310AITask(0);
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
aiTask = new JY5310AITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// 配置模拟触发：AI0 通道，上升沿触发，阈值 2.5V
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.AI0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;

aiTask.Start();
// 等待触发信号到来后自动开始采集
```

---

## 示例 6：AI 软触发采集

```csharp
aiTask = new JY5310AITask(0);
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
aiTask = new JY5310AITask(0);
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
aiTask = new JY5310AITask(0);
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

## 示例 9：DI 单点采集

```csharp
// 数字输入（8 端口，每端口 2 线）
var diTask = new JY5310DITask(0);
diTask.AddChannel(0);       // 添加端口 0
diTask.Start();

bool[] readValue = new bool[2];
diTask.ReadSinglePoint(ref readValue, 0);   // 读端口 0 的 2 线状态
Console.WriteLine($"Port 0 Line 0: {readValue[0]}, Line 1: {readValue[1]}");

diTask.Stop();
```

---

## 示例 10：DI 连续采集

```csharp
private JY5310DITask diTask;
private byte[,] dataBufByte;

private void button_start_Click(object sender, EventArgs e)
{
    diTask = new JY5310DITask(0);

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

## 示例 11：DO 单点输出

```csharp
JY5310DOTask doTask = new JY5310DOTask(0);

// 添加端口 0 和 1
doTask.AddChannel(0);
doTask.AddChannel(1);

doTask.Mode = DOMode.Single;
doTask.Start();

// 输出指定电平（每端口 2 线）
bool[] writeValue0 = { true, false };   // Port 0: Line0=高, Line1=低
bool[] writeValue1 = { false, true };   // Port 1: Line0=低, Line1=高

doTask.WriteSinglePoint(writeValue0, 0);
doTask.WriteSinglePoint(writeValue1, 1);

// 动态更新输出值
writeValue0[0] = false;
doTask.WriteSinglePoint(writeValue0, 0);

doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 12：DO 连续环形输出（Wrapping）

```csharp
private JY5310DOTask doTask;
private byte[,] writeValue;

private void button_start_Click(object sender, EventArgs e)
{
    doTask = new JY5310DOTask(0);
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
                writeValue[i * oneCyclePoints + j, 0] = 0x3;  // 两线都高
            else
                writeValue[i * oneCyclePoints + j, 0] = 0x0;  // 两线都低
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

## 示例 13：DO 有限输出（带触发）

```csharp
doTask = new JY5310DOTask(0);
doTask.AddChannel(0);
doTask.Mode = DOMode.Finite;
doTask.UpdateRate = 50000;

// 数字触发：PFI0 上升沿触发输出
doTask.Trigger.Type = DOTriggerType.Digital;
doTask.Trigger.Digital.Source = DODigitalTriggerSource.PFI0;
doTask.Trigger.Digital.Edge = DODigitalTriggerEdge.Rising;

// 生成波形数据
byte[,] waveform = new byte[500, 1];
// ... 填充波形数据 ...

doTask.WriteData(waveform, 500, -1);
doTask.Start();              // 等待触发信号
doTask.WaitUntilDone(-1);    // 输出完成后返回
doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 14：DI/DO 同步

```csharp
// DI 配置（从设备）
diTask = new JY5310DITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Continuous;
diTask.SampleRate = 10000;

// 使用 DO 的起始触发和采样时钟
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.DO_StartTrig;
diTask.Trigger.Digital.Edge = DIDigitalTriggerEdge.Rising;
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.DO_SampleClock;
diTask.SampleClock.External.ExpectedRate = 10000;

// DO 配置（主设备）
doTask = new JY5310DOTask(0);
doTask.AddChannel(1);
doTask.Mode = DOMode.ContinuousWrapping;
doTask.UpdateRate = 10000;
doTask.Trigger.Type = DOTriggerType.Immediate;
doTask.SampleClock.Source = DOSampleClockSource.Internal;

// 生成数字信号并写入
byte[] writeValue = new byte[10000];
// ... 生成 50% 占空比信号 ...
doTask.WriteData(writeValue, (uint)writeValue.Length, 1000);

// 先启动 DI（等待触发），再启动 DO（产生触发和时钟）
diTask.Start();
doTask.Start();

// DI 现在与 DO 同步采集...
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
// JY5310 支持：±10V / ±5V / ±2V / ±1V

aiTask.AddChannel(0, -5, 5);   // ±5V 量程
aiTask.AddChannel(1, -1, 1);   // ±1V 量程（小信号高精度）
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timerFetch != null) timerFetch.Enabled = false;
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

---

## 示例 8.1AI Record 无限录制模式

无限录制的：ֶ的ֹͣ

```csharp
private JY5310AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5310AITask(0);
        
        // 添加多通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, -10, 10);
        }
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;  // 50 kS/s
        
        // 的无限录制
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制
        
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

## 示例 8.2录制的ݻط

读取已录制的 bin 文件回放示例

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;
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
    int channelCount = 4;  // 录制时长通道
    totalSamples = (ulong)(fileInfo.Length / sizeof(double) / channelCount);
    
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
        // 读取的
        for (int i = 0; i < playbackData.GetLength(0); i++)
        {
            for (int ch = 0; ch < playbackData.GetLength(1); ch++)
            {
                if (playbackStream.Position < playbackStream.Length)
                {
                    playbackData[i, ch] = playbackReader.ReadDouble();
                }
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

## 示例 8.1：AI Record 有限录制模式（Finite Streaming）

录制固定时长的数据到文件，录制完成后自动停止：

```csharp
private JY5310AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5310AITask(0);
        for (int i = 0; i < 4; i++) aiTask.AddChannel(i, -10, 10);
        
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;
        
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 10.0;              // 录制 10 秒
        
        aiTask.Start();
        previewData = new double[1000, aiTask.Channels.Count];
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

## 示例 8.2：AI Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止：

```csharp
private JY5310AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5310AITask(0);
        for (int i = 0; i < 4; i++) aiTask.AddChannel(i, -10, 10);
        
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;
        
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制模式
        
        aiTask.Start();
        previewData = new double[1000, aiTask.Channels.Count];
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

## 示例 8.3：录制数据回放

读取已录制的 bin 文件并回放显示：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    if (!File.Exists(fileBrowser.FileName)) { MessageBox.Show("文件不存在"); return; }
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    int channelCount = 4;
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
