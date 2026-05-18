---
name: jy6316-driver
description: 提供 JYTEK JY6316 系列（PXIe/PCIe-6316）通道间隔离多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、数字输入/输出（DI/DO）**仅单点模式（不支持缓冲/连续/有限模式）**、触发配置（数字/模拟/软件触发，含 Edge/Hysteresis/Window 三种模拟触发模式和参考触发、重复触发）、时钟配置（内部/外部时钟、PXIe_Clk100 参考时钟）、端子台切换（TB-6316/TB-6316H 低压/高压量程）、过采样滤波（Oversampling Filter）、多卡采样时钟同步、录制模式（Finite/Infinite Streaming）。当用户使用 JY6316、PXIe-6316、PCIe-6316、JY6316AITask、JY6316DITask、JY6316DOTask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record、RecordMode.Finite、RecordMode.Infinite、TerminalBlockType.TB_6316、TerminalBlockType.TB_6316H、AIOversamplingFilterMode 开发高电压数据采集、通道间隔离采集、长时间数据记录、自动化测试应用时自动应用。
---

# JY6316 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6316\Bin\JY6316.dll`（引用到 .csproj）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY6316;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等
- **端子台**：使用前需根据实际接线的端子台型号（TB-6316/TB-6316C 低压 或 TB-6316H/TB-6316HC 高压）设置 `aiTask.Device.TerminalBlock`

## 硬件规格速查

| 功能                        | 规格                                                           |
| --------------------------- | -------------------------------------------------------------- |
| AI 分辨率                   | 16-bit                                                         |
| AI 通道数                   | 10 通道（差分，**通道间隔离**）                                |
| AI 最大采样率               | 1.5 MS/s/ch                                                    |
| AI 输入量程（低压端子台）   | ±10 V / ±5 V / ±2.5 V / ±1.25 V（配 TB-6316 / TB-6316C）       |
| AI 输入量程（高压端子台）   | ±300 V / ±150 V / ±75 V（配 TB-6316H / TB-6316HC）             |
| AI 输入耦合                 | 直流耦合（DC）                                                 |
| AI 输入阻抗（上电）         | AI+/AI- 对 AI COM：> 10 GΩ                                     |
| AI 小信号带宽 (-3 dB)       | 900 kHz（低压）/ 200 kHz（高压）                               |
| AI CMRR（DC ~ 60 Hz）       | 120 dB                                                         |
| AI 通道串扰（@1 kHz）       | 115 dB                                                         |
| AI FIFO 缓存                | 512 MB（所有通道共享）                                         |
| AI 过压保护（通电）         | AI+ 和 AI- 之间 ±72 V；通道对地连续 300 Vrms (CAT II)，瞬态 2500 Vpk |
| 采样时序精度                | 50 ppm of sample rate                                          |
| 采样时序分辨率              | 10 ns                                                          |
| PFI（DIO）通道              | 3（DIO<0..2>），3.3 V LVTTL，10 kΩ 下拉                        |
| 触发方式                    | 模拟 / 数字 / 软件触发（支持 Start / Reference，重触发）        |
| 参考时钟源                  | On-board / CLKIN / PXIe_Clk100（Reference Clock）              |
| 采样时钟源                  | Internal / PXI_Trig / PFI                                      |
| 接口类型                    | PCIe / PXIe                                                    |

### 产品型号

- **PXIe-6316**：10 通道，16 位，1.5 MS/s/ch，通道间隔离 PXIe 模拟输入模块
- **PCIe-6316**：10 通道，16 位，1.5 MS/s/ch，通道间隔离 PCIe 模拟输入模块

### 配套端子台

| 端子台        | 量程类型 | 适配板卡                    |
| ------------- | -------- | --------------------------- |
| TB-6316       | ±10 V 低压 | PXIe-6316（直挂）            |
| TB-6316C      | ±10 V 低压 | PCIe-6316（直挂）            |
| TB-6316H      | ±300 V 高压 | PXIe-6316（直挂）            |
| TB-6316HC     | ±300 V 高压 | PCIe-6316（直挂）            |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY6316AITask(0);                         // 1. 创建（按槽位号）
task.Device.TerminalBlock = TerminalBlockType.TB_6316;  // 2. 选择端子台（AI 必需）
task.AddChannel(0, -10, 10);                            // 3. 添加通道
task.Mode = AIMode.Continuous;                          // 4. 配置
task.SampleRate = 100000;
task.Start();                                           // 5. 启动
// ... 读取数据 ...
task.Stop();                                            // 6. 停止
task.Channels.Clear();                                  // 7. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY6316AITask`

#### 关键属性

| 属性                         | 类型                        | 说明                                       |
| ---------------------------- | --------------------------- | ------------------------------------------ |
| `Mode`                       | `AIMode`                    | Single / Finite / Continuous / Record      |
| `SampleRate`                 | `double`                    | 每通道采样率（Sa/s）                       |
| `SamplesToAcquire`           | `int`                       | Finite 模式下每通道采集点数                |
| `AvailableSamples`           | `ulong`                     | 缓冲区中可读取的点数（非 Single 模式）     |
| `SampleClock.Source`         | `AISampleClockSource`       | Internal（默认）/ External                 |
| `Trigger.Type`               | `AITriggerType`             | Immediate（默认）/ Digital / Analog / Soft / MultichannelAnalog |
| `Trigger.Mode`               | `AITriggerMode`             | Start（默认）/ Reference                   |
| `Trigger.Multichannel`       | `AIAnalogMultichannelComparator` | 多通道模拟触发比较器（Type=MultichannelAnalog 时使用）|
| `Trigger.PreTriggerSamples`  | `uint`                      | Reference 模式的触发前采样点数             |
| `Trigger.ReTriggerCount`     | `int`                       | 重触发次数（-1 表示无限）                  |
| `Device.TerminalBlock`       | `TerminalBlockType`         | TB_6316 / TB_6316H                         |
| `Advanced.Oversampling.Enabled`    | `bool`                | 是否启用过采样滤波                         |
| `Advanced.Oversampling.FilterMode` | `AIOversamplingFilterMode` | 过采样滤波模式                       |

#### AddChannel 重载

```csharp
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);

// 多通道（相同量程）
aiTask.AddChannel(int[] chnsId, double rangeLow, double rangeHigh);

// chnId 取值范围：0~9（共 10 通道）
// 低压端子台（TB_6316/TB_6316C）量程：±10 / ±5 / ±2.5 / ±1.25
// 高压端子台（TB_6316H/TB_6316HC）量程：±300 / ±150 / ±75
```

#### 读取数据重载

```csharp
// Single 模式 — 单通道
aiTask.ReadSinglePoint(ref double readValue, int channel);

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);

// Finite/Continuous — 多通道（行存储：[采样点, 通道]）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);

// 使用 Channels.Count 自动匹配读取量
aiTask.ReadData(ref double[,] buf);

// timeout = -1 → 永久等待；单位：毫秒
```

#### AI 四种模式速查

| 模式         | 典型配置                                 | 读取方式                                          |
| ------------ | ---------------------------------------- | ------------------------------------------------- |
| `Single`     | `Mode=AIMode.Single`                     | 轮询 `ReadSinglePoint`                            |
| `Finite`     | `+SamplesToAcquire=N +SampleRate`        | `WaitUntilDone()` 或轮询 `AvailableSamples` → `ReadData` |
| `Continuous` | `+SampleRate`                            | Timer/多线程轮询 `AvailableSamples` → `ReadData`  |
| `Record`     | `+Record.FilePath/Mode/Length`           | `GetRecordPreviewData` 预览，数据自动写入文件     |

#### 端子台配置（AI 必需）

JY6316 为端子台感知型板卡，使用 AI 前必须告知驱动所接入的端子台：

```csharp
// 低压端子台（量程 ±10/±5/±2.5/±1.25 V）
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;

// 高压端子台（量程 ±300/±150/±75 V）
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316H;
```

**注意**：TerminalBlockType 在 API 中以 `TB_6316` 和 `TB_6316H` 表示，同时对应 TB-6316/TB-6316C（低压）与 TB-6316H/TB-6316HC（高压）的物理端子台。

#### 过采样滤波（Oversampling）

JY6316 支持硬件过采样滤波以提升信噪比：

```csharp
aiTask.Advanced.Oversampling.Enabled = true;
aiTask.Advanced.Oversampling.FilterMode = AIOversamplingFilterMode.LowLatency; // 具体枚举依驱动提供
```

通过 `Enum.GetNames(typeof(AIOversamplingFilterMode))` 可枚举所有支持的滤波模式。

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

#### 模拟触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;  // Channel_0 ~ Channel_9（对应 AI 通道 0~9）
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;

// Edge 模式
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;                    // 触发电平（V）

// Hysteresis 模式（迟滞，抗噪）
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Hysteresis;
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 3.0;
aiTask.Trigger.Analog.Hysteresis.LowThreshold = 1.0;

// Window 模式（窗口进入/离开）
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Window;
aiTask.Trigger.Analog.Window.Condition = AIAnalogWindowCondition.Entering;  // Entering / Leaving
aiTask.Trigger.Analog.Window.HighThreshold = 5.0;
aiTask.Trigger.Analog.Window.LowThreshold = -5.0;
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Start();
// 在需要时手动发送软触发
aiTask.SendSoftwareTrigger();
```

#### 参考触发（Reference Trigger）

配合 `PreTriggerSamples` 实现触发前后数据同时采集：

```csharp
aiTask.Trigger.Mode = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 500;        // 触发前 500 点 + 触发后 (SamplesToAcquire-500) 点
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

#### 重触发（ReTriggerCount）

```csharp
aiTask.Trigger.ReTriggerCount = 5;    // 每次触发采集一段，共采集 5 段；-1 表示无限
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;     // PFI0~PFI2 / PXI_Trig0~7
aiTask.SampleClock.External.ExpectedRate = 100000;             // 期望外部时钟频率（Sa/s）
```

---

## 数字输入（DI）

### 任务类：`JY6316DITask`

JY6316 的数字 I/O 与 PFI 共用 3 条线（DIO<0..2>），驱动上以独立的 DI/DO 任务类暴露，每条线可独立控制方向。

> **模式说明**：JY6316 的 DI **仅支持单点模式（SinglePoint）**，不支持 Finite/Continuous 等缓冲模式，因此**无需设置 Mode、SampleRate 等属性**，创建任务 → 添加通道 → Start → `ReadSinglePoint` 即可。

#### 关键方法

```csharp
var diTask = new JY6316DITask(int slotNumber);

void AddChannel(int lineId);             // lineId: 0 / 1 / 2
void RemoveChannel(int lineId);

void Start();
void Stop();

// 单点读取（每线一个 bool）
void ReadSinglePoint(ref bool readValue, int lineId);
```

#### DI 单点采集示例

```csharp
var diTask = new JY6316DITask(0);
diTask.AddChannel(0);          // 读取 DIO0
diTask.AddChannel(1);          // 读取 DIO1
diTask.AddChannel(2);          // 读取 DIO2
diTask.Start();

bool val0 = false, val1 = false, val2 = false;
diTask.ReadSinglePoint(ref val0, 0);
diTask.ReadSinglePoint(ref val1, 1);
diTask.ReadSinglePoint(ref val2, 2);

Console.WriteLine($"DIO0={val0}, DIO1={val1}, DIO2={val2}");

diTask.Stop();
```

---

## 数字输出（DO）

### 任务类：`JY6316DOTask`

> **模式说明**：JY6316 的 DO **仅支持单点模式（SinglePoint）**，不支持 Finite/ContinuousWrapping/ContinuousNoWrapping 等缓冲输出模式，因此**无需设置 Mode、UpdateRate 等属性**，创建任务 → 添加通道 → Start → `WriteSinglePoint` 即可。

#### 关键方法

```csharp
var doTask = new JY6316DOTask(int slotNumber);

void AddChannel(int lineId);             // lineId: 0 / 1 / 2
void RemoveChannel(int lineId);

void Start();
void Stop();

// 单点写入（每线一个 bool）
void WriteSinglePoint(bool writeValue, int lineId);
```

#### DO 单点输出示例

```csharp
var doTask = new JY6316DOTask(0);
doTask.AddChannel(0);
doTask.AddChannel(1);
doTask.AddChannel(2);
doTask.Start();

doTask.WriteSinglePoint(true,  0);   // DIO0 = 高
doTask.WriteSinglePoint(false, 1);   // DIO1 = 低
doTask.WriteSinglePoint(true,  2);   // DIO2 = 高

// 动态更新
doTask.WriteSinglePoint(false, 0);

doTask.Stop();
doTask.Channels.Clear();
```

**电平规格**：3.3 V LVTTL，IOL max 24 mA（低电平），IOH max 24 mA（高电平）。每通道内置 10 kΩ 下拉电阻。

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
var aiTask = new JY6316AITask(0);
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;

// 添加多通道
for (int i = 0; i < 4; i++) aiTask.AddChannel(i, -10, 10);

aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 500000;

// 配置录制参数
aiTask.Record.FilePath = @"C:\Data\signal.bin";
aiTask.Record.Mode = RecordMode.Finite;   // Finite / Infinite
aiTask.Record.Length = 10.0;              // 录制 10 秒（Finite 有效）

aiTask.Start();

// 可在录制期间预览数据
double[,] preview = new double[1000, aiTask.Channels.Count];
aiTask.GetRecordPreviewData(ref preview, 1000, 10000);
// easyChartX1.Plot(preview);

// 等待录制完成（Finite）
double recordedLen;
bool recordDone;
do
{
    aiTask.GetRecordStatus(out recordedLen, out recordDone);
    System.Threading.Thread.Sleep(100);
} while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

**录制文件格式**：二进制 double，多通道按 **行存储**（每一行为一个采样点，列为通道）。回放时按同样顺序 `BinaryReader.ReadDouble()` 即可还原。

---

## 多卡同步（采样时钟同步）

多张 JY6316 板卡之间实现同步采集，主卡导出采样时钟和触发信号，从卡通过 PXI_Trig 总线接收：

```csharp
// ===== 主卡配置（Slot 0）=====
var masterTask = new JY6316AITask(0);
masterTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
masterTask.AddChannel(1, -10, 10);
masterTask.Mode = AIMode.Finite;
masterTask.SamplesToAcquire = 10000;
masterTask.SampleRate = 1000000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟到 PXI_Trig0
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
// 导出开始触发信号到 PXI_Trig1
masterTask.SignalExport.Add(AISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig1);
// 若使用参考触发：导出到 PXI_Trig2 并设置 PreTriggerSamples
// masterTask.SignalExport.Add(AISignalExportSource.ReferenceTrig, SignalExportDestination.PXI_Trig2);
// masterTask.Trigger.PreTriggerSamples = 500;

// ===== 从卡配置（Slot 1）=====
var slaveTask = new JY6316AITask(1);
slaveTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
slaveTask.AddChannel(1, -10, 10);
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 10000;

// 接收主卡的 PXI_Trig0 作为采样时钟
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 1000000;

// 接收主卡的 PXI_Trig1 作为数字触发
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先从卡后主卡 =====
slaveTask.Start();                             // 从卡先启动，等待触发和时钟
System.Threading.Thread.Sleep(500);            // 确保从卡就绪
masterTask.Start();                            // 主卡启动，产生时钟 + 触发

// ===== 读取数据 =====
double[] masterData = new double[10000];
double[] slaveData  = new double[10000];

while (masterTask.AvailableSamples < 10000 || slaveTask.AvailableSamples < 10000)
    System.Threading.Thread.Sleep(10);

masterTask.ReadData(ref masterData, 10000, 10000);
slaveTask.ReadData(ref slaveData,  10000, 10000);

masterTask.Stop();
slaveTask.Stop();
masterTask.Channels.Clear();
slaveTask.Channels.Clear();
```

**关键要点：**
- 主卡使用默认内部时钟，从卡使用 `AISampleClockSource.External`
- 主卡通过 `SignalExport.Add()` 导出时钟（`SampleClock`）和触发（`StartTrig` 或 `ReferenceTrig`）
- 启动顺序必须为 **先从卡后主卡**，Sleep 500 ms 给从卡时间进入等待状态
- PXI 机箱上通过 `PXI_Trig0~PXI_Trig7` 互联主从卡信号
- 如需更高精度的多卡时钟同源，可让所有卡均采用同一参考时钟，配置方式：
  ```csharp
  task.Device.ReferenceClock.Source = ReferenceClockSource.External;
  task.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100; // 或 CLKIN
  task.Device.ReferenceClock.Commit();
  ```

---

## 常见错误处理

| 异常代码                                         | 原因                         | 处理建议                     |
| ------------------------------------------------ | ---------------------------- | ---------------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误       | 检查设备管理器槽位号          |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start   | 在 Start 前添加通道          |
| `BufferDataOverflow`                             | 读取速度慢于采集速度         | 增大读取频率或减小采样率     |
| `ReadDataTimeout`                                | timeout 内未读到足够数据     | 增大 timeout 或检查采样率    |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数          | Stop 后再修改                |
| `SampleRateParameterInvalid`                     | 采样率超过硬件上限 1.5 MS/s/ch | 检查采样率设置             |
| `ChannelInputRangeParameterInvalid`              | 量程不匹配所选端子台          | 检查 TerminalBlock 与 range 对应 |
| `TriggerParameterInvalid`                        | 触发参数无效                  | 检查 Source/Edge/Threshold   |

---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY6316;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DAQ\JY6316\Bin\JY6316.dll
```

---

## 设备信息类 `JY6316Device`

通过 `aiTask.Device`（或 `diTask.Device` / `doTask.Device`）访问：

| 属性               | 类型                    | 读写 | 说明                                              |
| ------------------ | ----------------------- | ---- | ------------------------------------------------- |
| `SerialNumber`     | `string`                | R    | 设备序列号                                      |
| `DeviceID`         | `int`                   | R    | 设备 ID                                          |
| `DiffChannelCount` | `uint`                  | R    | 差分 AI 通道数（10）                            |
| `DIOLineCount`     | `int`                   | R    | DIO 线数（3）                                   |
| `MaxSampleRate`    | `double`                | R    | 最大采样率（Hz）                                |
| `AIBufferSize`     | `uint`                  | R    | AI 缓冲区大小（字节）                          |
| `IsAISync`         | `bool`                  | R    | AI 是否处于同步状态                              |
| `Temperature`      | `double`                | R    | 板卡温度                                          |
| `FPGATemperature`  | `double`                | R    | FPGA 温度                                         |
| `TerminalBlock`    | `TerminalBlockType`     | R/W  | 当前所接端子台类型（使用前必须设置）              |
| `ReferenceClock`   | `ReferenceClock`        | R    | 参考时钟配置对象（见下文）                        |

### 参考时钟配置（JY6316）

`Device.ReferenceClock` 类成员：

| 路径                                             | 类型                               | 说明                              |
| ------------------------------------------------ | ---------------------------------- | --------------------------------- |
| `ReferenceClock.Source`                          | `ReferenceClockSource`             | `Internal`（默认）/ `External`    |
| `ReferenceClock.External.Terminal`               | `ExternalReferenceClockTerminal`   | `CLKIN` / `PXIe_Clk100`           |
| `ReferenceClock.Commit()`                        | 方法                               | 提交参考时钟配置（必调）                 |

```csharp
// 使用 PXIe 背板 100 MHz 参考时钟作为板卡 10 MHz VCXO 的锻相参考
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
aiTask.Device.ReferenceClock.Commit();

// 或使用 CLKIN 端子外部接入参考时钟
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.CLKIN;
aiTask.Device.ReferenceClock.Commit();
```

### TerminalBlockType 枚举

| 值           | 说明                                             |
| ------------ | ------------------------------------------------ |
| `TB_6316`    | 低压端子台（TB-6316 / TB-6316C）量程 ±10 V 级   |
| `TB_6316H`   | 高压端子台（TB-6316H / TB-6316HC）量程 ±300 V 级 |

---

## JY6316AITask — 模拟输入任务

### 构造函数

```csharp
new JY6316AITask(int slotNumber)         // 按槽位号创建（推荐）
```

### 属性

| 属性                  | 类型                 | 默认值  | 说明                                       |
| --------------------- | -------------------- | ------- | ------------------------------------------ |
| `Mode`                | `AIMode`             | —       | Single / Finite / Continuous / Record      |
| `SampleRate`          | `double`             | —       | 每通道采样率（Sa/s），最大 1.5e6            |
| `SamplesToAcquire`    | `int`                | —       | Finite 模式采集点数/通道                    |
| `AvailableSamples`    | `ulong`              | —       | 缓冲区可读点数（非 Single 模式有效）        |
| `TransferedSamples`   | `ulong`              | —       | 已传输点数                                 |
| `SampleClock`         | `AISampleClock`      | —       | 时钟配置对象                                |
| `Trigger`             | `AITrigger`          | —       | 触发配置对象                                |
| `Record`              | `AIRecord`           | —       | 录制配置对象                                |
| `Advanced`            | `AIAdvanced`         | —       | 高级功能（含 Oversampling）                |
| `SignalExport`        | `AISignalExport`     | —       | 信号导出对象（多卡同步用）                  |
| `Device`              | `JY6316Device`       | —       | 设备信息对象                                |
| `Channels`            | `List<AIChannel>`    | —       | 已添加的通道列表                            |

### 方法

#### AddChannel

```csharp
void AddChannel(int chnId, double rangeLow, double rangeHigh);
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh);

// chnId / chnsId 取值范围：0 ~ 9
// 低压端子台量程组合：(-10,10) / (-5,5) / (-2.5,2.5) / (-1.25,1.25)
// 高压端子台量程组合：(-300,300) / (-150,150) / (-75,75)
```

#### 控制

```csharp
void Start();
void Stop();                             // 停止采集，保留参数配置，可再次 Start
void WaitUntilDone(int timeout);         // timeout=-1 永久等待，Finite 有效
void SendSoftwareTrigger();              // 软触发
```

#### 读取数据（电压值）

```csharp
// Single 模式
void ReadSinglePoint(ref double readValue, int channel);

// 缓冲模式（Finite / Continuous）
void ReadData(ref double[] buf, int samplesPerChannel, int timeout);
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
void ReadData(ref double[,] buf);        // 使用 SamplesToAcquire 自动匹配
```

#### 录制模式

```csharp
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout);
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout);
void GetRecordStatus(out double recordedLength, out bool recordDone);
```

---

## AIMode 枚举

| 值            | 说明                                             |
| ------------- | ------------------------------------------------ |
| `Single`      | 软件触发单点读取，可循环调用 ReadSinglePoint      |
| `Finite`      | 采集固定点数后停止，通过 WaitUntilDone 或轮询等待  |
| `Continuous`  | 持续采集，应用轮询 AvailableSamples 读取          |
| `Record`      | 数据流式写入文件，支持预览                       |

---

## AISampleClock — 时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.Internal;     // 默认
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;    // PFI0~PFI2 / PXI_Trig0~7
aiTask.SampleClock.External.ExpectedRate = 1000000.0;         // 外部时钟期望频率
```

### AISampleClockSource 枚举

| 值           | 说明             |
| ------------ | ---------------- |
| `Internal`   | 内部时钟（默认） |
| `External`   | 外部时钟         |

### ClockTerminal 枚举（常用值）

| 值                         | 说明            |
| -------------------------- | --------------- |
| `PFI0` ~ `PFI2`            | 前面板 PFI 引脚 |
| `PXI_Trig0` ~ `PXI_Trig7`  | PXI 触发总线    |

---

## AITrigger — 触发配置

### 属性

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;           // Digital / Analog / Soft / Immediate
aiTask.Trigger.Mode = AITriggerMode.Start;             // Start / Reference
aiTask.Trigger.PreTriggerSamples = 500;                // Reference 模式触发前采样点数
aiTask.Trigger.ReTriggerCount = 3;                     // 重触发次数，-1 表示无限
```

### 数字触发

```csharp
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // PFI0~PFI2 / PXI_Trig0~7
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;     // Rising / Falling
```

### 模拟触发

```csharp
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;  // Channel_0 ~ Channel_9（对应 AI 通道 0~9）
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

| 值                   | 说明                                            |
| -------------------- | ----------------------------------------------- |
| `Immediate`          | 立即触发（默认）                                |
| `Digital`            | 数字边沿触发                                    |
| `Analog`             | 单通道模拟边沿/迟滞/窗口触发                       |
| `Soft`               | 软件触发                                        |
| `MultichannelAnalog` | 多通道模拟组合触发（使用 `Trigger.Multichannel` 配置，允许以 AND/OR 逻辑组合多个通道条件） |

### AITriggerMode 枚举

| 值          | 说明            |
| ----------- | --------------- |
| `Start`     | 开始触发（默认）|
| `Reference` | 参考触发        |

### AIAnalogTriggerComparator 枚举

| 值           | 说明                                       |
| ------------ | ------------------------------------------ |
| `Edge`       | 单阈值边沿触发                             |
| `Hysteresis` | 双阈值迟滞触发（抗噪声）                   |
| `Window`     | 窗口触发（进入或离开 [Low, High] 区间）   |

### AIAnalogWindowCondition 枚举

| 值         | 说明                  |
| ---------- | --------------------- |
| `Entering` | 信号进入窗口时触发    |
| `Leaving`  | 信号离开窗口时触发    |

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.Mode = RecordMode.Finite;              // Finite / Infinite
aiTask.Record.Length = 10.0;                         // 录制时长（秒），Finite 有效
```

| 属性         | 类型          | 说明                                  |
| ------------ | ------------- | ------------------------------------- |
| `FilePath`   | `string`      | 录制文件路径（.bin）                  |
| `Mode`       | `RecordMode`  | 录制模式（Finite/Infinite）           |
| `Length`     | `double`      | 录制时长（秒），Finite 模式有效        |

### RecordMode 枚举

| 值         | 说明                                 |
| ---------- | ------------------------------------ |
| `Finite`   | 有限录制，录制指定时长后自动停止     |
| `Infinite` | 无限录制，需手动调用 Stop 停止       |

---

## AISignalExport — 信号导出（多卡同步）

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,   SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.StartTrig,     SignalExportDestination.PXI_Trig1);
aiTask.SignalExport.Add(AISignalExportSource.ReferenceTrig, SignalExportDestination.PXI_Trig2);
```

### AISignalExportSource 枚举（全量）

| 值                     | 说明                                            |
| ---------------------- | ----------------------------------------------- |
| `SampleClock`          | 采样时钟                                        |
| `StartTrig`            | 开始触发信号                                    |
| `ReferenceTrig`        | 参考触发信号                                    |
| `ChannelComparatorOut` | 通道模拟触发比较器命中信号（可导出用于下游信号源） |

### SignalExportDestination 枚举（常用）

| 值                         | 说明            |
| -------------------------- | --------------- |
| `PXI_Trig0` ~ `PXI_Trig7`  | PXI 触发总线    |
| `PFI0` ~ `PFI2`            | 前面板 PFI 引脚 |

---

## AIOversamplingFilterMode 枚举

| 值            | 说明                                                            |
| ------------- | --------------------------------------------------------------- |
| `LowLatency`  | 低延迟模式（默认）：稳态滤波延迟小，适合实时闭环控制                      |
| `Wideband`    | 宽带模式：稳态滤波延迟大但遮止带衰减强，适合频域分析                |

```csharp
aiTask.Advanced.Oversampling.Enabled = true;
aiTask.Advanced.Oversampling.FilterMode = AIOversamplingFilterMode.LowLatency;
```

---

## JY6316DITask — 数字输入任务

> **仅支持单点模式（SinglePoint）**，无 Mode / SampleRate / Trigger / SampleClock 等属性需要配置。

### 构造函数

```csharp
new JY6316DITask(int slotNumber);
```

### 方法

```csharp
void AddChannel(int lineNum);             // lineNum：0 / 1 / 2（DIO<0..2>）
void AddChannel(int[] linesNum);          // 批量添加
void RemoveChannel(int lineNum);

void Start();
void Stop();

// 单点读取
void ReadSinglePoint(ref bool readValues, int line);   // 读单条线
void ReadSinglePoint(ref bool[] readValues);           // 一次性读所有已添加的线（按 Channels 顺序）
```

---

## JY6316DOTask — 数字输出任务

> **仅支持单点模式（SinglePoint）**，无 Mode / UpdateRate / Trigger / SampleClock 等属性需要配置。

### 构造函数

```csharp
new JY6316DOTask(int slotNumber);
```

### 方法

```csharp
void AddChannel(int lineNum);             // lineNum：0 / 1 / 2（DIO<0..2>）
void AddChannel(int[] linesNum);          // 批量添加
void RemoveChannel(int lineNum);

void Start();
void Stop();

// 单点写入
void WriteSinglePoint(bool writeValues, int line);     // 写单条线
void WriteSinglePoint(bool[] writeValues);             // 一次性写所有已添加的线（按 Channels 顺序）
```

---

## 异常类 JYDriverException

```csharp
try { /* ... */ }
catch (JYDriverException ex)
{
    // ex.Message — 驱动错误描述
    // ex.ExceptionName — 异常枚举名
    // ex.ErrorCode — 错误码
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 枚举（常见值）

| 枚举值                                          | 含义                          |
| ---------------------------------------------- | ----------------------------- |
| `OpenDeviceFailed`                             | 打开设备失败（未连接/槽位号错误）|
| `CloseDeviceFailed`                            | 关闭设备失败                  |
| `NoChannelAdded`                               | 未添加通道                    |
| `StartTaskFailed`                              | 启动 Task 失败                |
| `StopTaskFailed`                               | 停止 Task 失败                |
| `TaskHasNotStarted`                            | Task 未启动即读写             |
| `TaskHasStartedCannotPerformTheSetOperation`   | Task 运行中修改参数           |
| `BufferDataOverflow`                           | 采集缓冲区溢出                |
| `ReadDataTimeout`                              | 读取超时                      |
| `WriteDataTimeout`                             | 写入超时                      |
| `SampleRateParameterInvalid`                   | 采样率超限                    |
| `ChannelNumberParameterInvalid`                | 无效通道号                    |
| `ChannelInputRangeParameterInvalid`            | 无效输入量程                  |
| `TriggerParameterInvalid`                      | 触发参数无效                  |

---

# 完整代码示例

> 所有示例均来自 `JY6316_V1.0.1_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

## 示例 1：AI 单点采集（Console）

```csharp
using System;
using JY6316;

// 1. 创建 AI Task
Console.WriteLine("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());
var aiTask = new JY6316AITask(boardNum);

// 2. 设置端子台与模式
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;   // 低压端子台
aiTask.Mode = AIMode.Single;

// 3. 添加通道
Console.WriteLine("请输入通道号（0~9）：");
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
```

---

## 示例 2：AI 连续采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY6316;

public partial class MainForm : Form
{
    private JY6316AITask aiTask;
    private double[] readBuffer;

    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            aiTask = new JY6316AITask(0);
            aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
            aiTask.AddChannel(0, -10, 10);

            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 100000;        // 100 kSa/s
            aiTask.Advanced.Oversampling.Enabled = true;

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
}
```

---

## 示例 3：AI 有限采集（多通道）

```csharp
private JY6316AITask aiTask;
private double[,] readValue;

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JY6316AITask(0);
    aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;

    int[] channels = { 0, 1, 2, 3 };
    aiTask.AddChannel(channels, -10, 10);

    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 1000;
    aiTask.SampleRate = 100000;

    aiTask.Start();
    readValue = new double[1000, channels.Length];
    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    if (aiTask.AvailableSamples >= (ulong)aiTask.SamplesToAcquire)
    {
        aiTask.ReadData(ref readValue, 1000, -1);
        easyChartX1.Plot(readValue, 0, 1, SeeSharpTools.JY.GUI.MajorOrder.Column);

        aiTask.Stop();
        aiTask.Channels.Clear();
        timer_FetchData.Enabled = false;
    }
}
```

---

## 示例 4：AI 数字触发 + 参考触发 + 重触发

```csharp
aiTask = new JY6316AITask(0);
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
aiTask.AddChannel(0, -10, 10);

aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 2000;
aiTask.SampleRate = 500000;

// 参考触发：触发前 500 点 + 触发后 1500 点
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Mode = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 500;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 重触发：完成 5 次后自动停止
aiTask.Trigger.ReTriggerCount = 5;

aiTask.Start();
```

---

## 示例 5：AI 模拟触发（Hysteresis 迟滞）

```csharp
aiTask = new JY6316AITask(0);
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 1000000;

aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Hysteresis;
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 3.0;
aiTask.Trigger.Analog.Hysteresis.LowThreshold = 1.0;

aiTask.Start();
```

---

## 示例 6：AI 软触发采集

```csharp
aiTask = new JY6316AITask(0);
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100000;

aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;

aiTask.Start();
// 在需要时手动发送
aiTask.SendSoftwareTrigger();
```

---

## 示例 7：AI 外部时钟采集

```csharp
aiTask = new JY6316AITask(0);
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;

aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 100000;   // 告知驱动期望频率

aiTask.Start();
```

---

## 示例 8：高压端子台采集（TB-6316H）

```csharp
aiTask = new JY6316AITask(0);

// 切换到高压端子台
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316H;

// 使用高压量程 ±300 V
aiTask.AddChannel(0, -300, 300);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 500000;        // 高压时小信号带宽 200 kHz，采样率适度降低
aiTask.Start();
```

---

## 示例 9：AI Record 有限录制模式（Finite Streaming）

```csharp
private JY6316AITask aiTask;
private double[,] previewData;
private double recordedLength;
private bool recordDone;

private void button_Start_Click(object sender, EventArgs e)
{
    aiTask = new JY6316AITask(0);
    aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;

    for (int i = 0; i < 4; i++) aiTask.AddChannel(i, -10, 10);

    aiTask.Mode = AIMode.Record;
    aiTask.SampleRate = 500000;

    aiTask.Record.FilePath = @"C:\Data\record.bin";
    aiTask.Record.Mode = RecordMode.Finite;
    aiTask.Record.Length = 10.0;

    aiTask.Start();
    previewData = new double[1000, aiTask.Channels.Count];
    timer_Preview.Enabled = true;
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    aiTask.GetRecordStatus(out recordedLength, out recordDone);
    if (recordDone)
    {
        aiTask.Stop();
        aiTask.Channels.Clear();
        timer_Preview.Enabled = false;
        MessageBox.Show("录制完成！");
    }
    else if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 10000);
        // easyChartX1.Plot(previewData);
    }
}
```

---

## 示例 10：AI Record 无限录制模式（Infinite Streaming）

```csharp
aiTask = new JY6316AITask(0);
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;

for (int i = 0; i < 4; i++) aiTask.AddChannel(i, -10, 10);

aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 500000;

aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
aiTask.Record.Mode = RecordMode.Infinite;          // 手动停止

aiTask.Start();
// ... 预览 ...
// 需要时手动调用 aiTask.Stop();
```

---

## 示例 11：AI 多卡采样时钟同步

```csharp
// 主卡 Slot 0
var masterTask = new JY6316AITask(0);
masterTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
masterTask.AddChannel(1, -10, 10);
masterTask.Mode = AIMode.Finite;
masterTask.SampleRate = 1000000;
masterTask.SamplesToAcquire = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig1);

// 从卡 Slot 1
var slaveTask = new JY6316AITask(1);
slaveTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
slaveTask.AddChannel(1, -10, 10);
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 10000;
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 1000000;
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// 先启动从卡，延时 500 ms 后启动主卡
slaveTask.Start();
System.Threading.Thread.Sleep(500);
masterTask.Start();

double[] masterData = new double[10000];
double[] slaveData  = new double[10000];
masterTask.ReadData(ref masterData, 10000, 10000);
slaveTask.ReadData(ref slaveData,  10000, 10000);

masterTask.Stop(); slaveTask.Stop();
masterTask.Channels.Clear(); slaveTask.Channels.Clear();
```

---

## 示例 12：DI 单点采集

```csharp
var diTask = new JY6316DITask(0);
diTask.AddChannel(0);
diTask.AddChannel(1);
diTask.AddChannel(2);
diTask.Start();

bool line0 = false, line1 = false, line2 = false;
diTask.ReadSinglePoint(ref line0, 0);
diTask.ReadSinglePoint(ref line1, 1);
diTask.ReadSinglePoint(ref line2, 2);

Console.WriteLine($"DIO0={line0}, DIO1={line1}, DIO2={line2}");

diTask.Stop();
```

---

## 示例 13：DO 单点输出

```csharp
var doTask = new JY6316DOTask(0);
doTask.AddChannel(0);
doTask.AddChannel(1);
doTask.AddChannel(2);
doTask.Start();

doTask.WriteSinglePoint(true,  0);   // DIO0 = 高
doTask.WriteSinglePoint(false, 1);   // DIO1 = 低
doTask.WriteSinglePoint(true,  2);   // DIO2 = 高

// 动态刷新
doTask.WriteSinglePoint(false, 0);

doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 14：录制数据回放

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var dlg = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (dlg.ShowDialog() != DialogResult.OK) return;

    playbackStream = new FileStream(dlg.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);

    int channelCount = 4;      // 录制时设置的通道数
    playbackData = new double[1000, channelCount];
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        for (int i = 0; i < playbackData.GetLength(0); i++)
            for (int ch = 0; ch < playbackData.GetLength(1); ch++)
                if (playbackStream.Position < playbackStream.Length)
                    playbackData[i, ch] = playbackReader.ReadDouble();

        // easyChartX1.Plot(playbackData);
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

---

## 综合技巧

### 多通道数据解析（行存储）

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 行存储
double[,] buf = new double[1000, 4];
aiTask.ReadData(ref buf, 1000, -1);

// 提取各通道数据
double[] ch0 = new double[1000];
for (int i = 0; i < 1000; i++) ch0[i] = buf[i, 0];
```

### 量程选择

```csharp
// 低压端子台：选择略大于信号峰值的量程，提高精度
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316;
aiTask.AddChannel(0, -5, 5);        // ±5V 量程
aiTask.AddChannel(1, -1.25, 1.25);  // ±1.25V 小信号高精度

// 高压端子台：绝对不要在低压端子台下配置高压量程，会报 ChannelInputRangeParameterInvalid
aiTask.Device.TerminalBlock = TerminalBlockType.TB_6316H;
aiTask.AddChannel(2, -150, 150);    // ±150V 量程
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
