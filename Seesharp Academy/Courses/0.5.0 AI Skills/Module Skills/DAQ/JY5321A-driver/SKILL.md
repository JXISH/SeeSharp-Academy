---
name: jy5321a-driver
description: 提供 JYTEK JY5321A 系列（PXIe/PCIe/USB-5321A）多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、数字输入/输出（DI/DO）单点/有限/连续模式、计数器输入/输出（CI/CO）边沿计数/频率/周期/脉冲/编码器测量/脉冲生成、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、多卡同步、录制模式（Finite/Infinite Streaming）。当用户使用 JY5321A、PXIe-5321A、PCIe-5321A、USB-5321A、JY5321AAITask、JY5321ADITask、JY5321ADOTask、JY5321ACITask、JY5321ACOTask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record 开发数据采集、信号采集、计数器测量、PWM输出、自动化测试应用时自动应用。
---

# JY5321A 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5321A\Bin\JY5321A.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5321A\Bin\JY5321A.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY5321A;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                  | 规格                                    |
| --------------------- | --------------------------------------- |
| AI 分辨率             | 18-bit                                  |
| AI 通道数             | 32 通道（JY-5321A）/ 16 通道（JY-5322A） |
| AI 最大采样率         | 1 MS/s（每通道，同步采样）                |
| AI 输入量程           | ±10V（参考 AIGND）                       |
| AI 输入阻抗           | 高阻抗（AI+ vs AIGND / AI- vs AIGND）     |
| AI 抗混叠滤波器       | 25 kHz / 220 kHz 软件可选                |
| AI 过采样             | 支持板载过采样，降低噪声，提高有效分辨率   |
| AI DC 精度            | 220 ppm                                 |
| 硬件定时 DIO          | 16 通道                                  |
| 静态 DIO              | 32 通道（JY-5321A）/ 16 通道（JY-5322A） |
| 计数器                | 2 个（JY-5321A）/ 1 个（JY-5322A）       |
| 接口                  | PXIe / PCIe / USB / DDA                  |

### 产品型号

| 型号        | 描述                                          |
| ----------- | --------------------------------------------- |
| PXIe-5321A  | 32-ch, 18-bit, 1 MS/s/ch, 32 DIO, PXIe 同步采样 |
| PCIe-5321A  | 32-ch, 18-bit, 1 MS/s/ch, 32 DIO, PCIe 同步采样 |
| USB-5321A   | 32-ch, 18-bit, 1 MS/s/ch, 32 DIO, USB 同步采样  |
| PXIe-5322A  | 16-ch, 18-bit, 1 MS/s/ch, 16 DIO, PXIe 同步采样 |
| PCIe-5322A  | 16-ch, 18-bit, 1 MS/s/ch, 16 DIO, PCIe 同步采样 |
| USB-5322A   | 16-ch, 18-bit, 1 MS/s/ch, 16 DIO, USB 同步采样  |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY5321AAITask(0);         // 1. 创建（按槽位号）
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

### 任务类：`JY5321AAITask`

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
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);

// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2 / -1 / -0.5 / -0.25
// rangeHigh: 10 / 5 / 2 / 1 / 0.5 / 0.25
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
| `Continuous` | `+SampleRate`                 | Timer 轮询 `AvailableSamples` → `ReadData`   |
| `Record`     | `+Record.FilePath/Mode/Length`| `GetRecordPreviewData` 预览，数据写入文件    |

#### 连续采集 Timer 模式（最常用）

``csharp
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
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.AI0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
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

---

## 数字输入（DI）

### 任务类：`JY5321ADITask`

```csharp
var diTask = new JY5321ADITask(0);
diTask.AddChannel(0);                    // 添加第 0 线
diTask.Start();
bool readValue;
diTask.ReadSinglePoint(ref readValue, 0);  // 读单线
diTask.Stop();
```

---

## 数字输出（DO）

### 任务类：`JY5321ADOTask`

```csharp
var doTask = new JY5321ADOTask(0);
doTask.AddChannel(0);
doTask.Start();
doTask.WriteSinglePoint(true, 0);        // 写单线
doTask.Stop();
```

---

## 计数器输入（CI）

### 任务类：`JY5321ACITask`

**构造**：`new JY5321ACITask(int slotNumber, int channel)` — channel 0 或 1

#### CI 测量类型

| CIType                | 说明                    |
| --------------------- | ----------------------- |
| `EdgeCounting`        | 边沿计数                |
| `Frequency`           | 频率测量                |
| `Period`              | 周期测量                |
| `Pulse`               | 脉冲宽度测量            |
| `TwoEdgeSeparation`   | 两边沿间隔测量          |
| `Encode`              | 正交编码器              |
| `EncoderTwoPulse`     | 双脉冲编码器            |
| `SemiPeriod`          | 半周期测量              |

```csharp
// 边沿计数示例
ciTask = new JY5321ACITask(0, 0);
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

### 任务类：`JY5321ACOTask`

```csharp
coTask = new JY5321ACOTask(0, 0);
coTask.Mode = COMode.Single;
coTask.IdleState = COIdleState.Low;
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
aiTask = new JY5321AAITask(0);
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

### 录制模式方法

| 方法 | 说明 |
|------|------|
| `GetRecordPreviewData(ref double[] buf, int samples, int timeout)` | 获取单通道预览数据 |
| `GetRecordPreviewData(ref double[,] buf, int samples, int timeout)` | 获取多通道预览数据 |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态 |

---

## 多卡同步

### AI 多卡采样时钟同步（有限采集）

多张 JY5321A 板卡之间实现同步采集，主卡导出采样时钟和触发信号，从卡接收：

```csharp
// ===== 主卡配置（Slot 0）=====
var masterTask = new JY5321AAITask(0);
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
var slaveTask = new JY5321AAITask(1);
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
System.Threading.Thread.Sleep(100);  // 可选延迟，确保从卡就绪
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

### AI 多卡连续同步采集

适用于长时间连续同步采集场景：

```csharp
// ===== 主卡配置 =====
var masterTask = new JY5321AAITask(0);
masterTask.AddChannel(0, -10, 10);
masterTask.Mode = AIMode.Continuous;
masterTask.SampleRate = 10000;

// 导出采样时钟
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

// ===== 从卡配置 =====
var slaveTask = new JY5321AAITask(1);
slaveTask.AddChannel(0, -10, 10);
slaveTask.Mode = AIMode.Continuous;

// 使用外部时钟
slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10000;

// 启动
slaveTask.Start();
masterTask.Start();

// 在 Timer 中同步读取
double[] masterBuf = new double[1000];
double[] slaveBuf = new double[1000];

if (masterTask.AvailableSamples >= 1000 && slaveTask.AvailableSamples >= 1000)
{
    masterTask.ReadData(ref masterBuf, 1000, -1);
    slaveTask.ReadData(ref slaveBuf, 1000, -1);
    // 处理同步数据...
}
```

### AI 与 DI 同步采集（连续模式）

AI 和 DI 使用相同的采样时钟和触发信号实现同步：

```csharp
// ===== AI 配置（主设备）=====
var aiTask = new JY5321AAITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// 导出采样时钟和触发
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== DI 配置（从设备）=====
var diTask = new JY5321ADITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Continuous;

// 使用 AI 的时钟和触发
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
diTask.SampleClock.External.ExpectedRate = 10000;
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.PXI_Trig1;

// 启动
diTask.Start();
aiTask.Start();

// 同步读取...
```

### AI 与 DI 同步采集（有限模式）

```
// ===== AI 配置 =====
var aiTask = new JY5321AAITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 5000;
aiTask.SampleRate = 10000;

aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== DI 配置 =====
var diTask = new JY5321ADITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Finite;
diTask.SamplesToAcquire = 5000;

diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
diTask.SampleClock.External.ExpectedRate = 10000;
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.PXI_Trig1;

// 启动
diTask.Start();
aiTask.Start();

// 等待完成
aiTask.WaitUntilDone(-1);
diTask.WaitUntilDone(-1);

// 读取数据
double[] aiData = new double[5000];
byte[,] diData = new byte[5000, 1];

aiTask.ReadData(ref aiData, 5000, -1);
diTask.ReadData(ref diData, 5000, -1);
```

**关键要点：**
- 主卡使用内部时钟，从卡使用外部时钟（`AISampleClockSource.External`）
- 主卡通过 `SignalExport.Add()` 导出时钟和触发信号
- 启动顺序：**先从卡后主卡**，确保从卡先准备好接收信号
- 使用 `PXI_Trig0~PXI_Trig7` 或 `PFI0~PFI15` 作为同步信号线
- 连续采集时需在 Timer 中同步轮询 `AvailableSamples`
- 有限采集时使用 `WaitUntilDone()` 等待完成

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

# JY5321A 驱动 API 完整参考

## 命名空间与引用

```csharp
using JY5321A;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DAQ\JY5321A\Bin\JY5321A.dll
```

---

## JY5321AAITask — 模拟输入任务

### 构造函数

```csharp
new JY5321AAITask(int slotNumber)      // 按槽位号创建（推荐）
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
void AddChannel(int chnId, double rangeLow, double rangeHigh)
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh)
// chnId = -1 → 添加全部通道
// rangeLow: -10 / -5 / -2 / -1 / -0.5 / -0.25
// rangeHigh: 10 / 5 / 2 / 1 / 0.5 / 0.25
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

---

## AITrigger — 触发配置

### 数字触发

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;     // PFI0~PFI15
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;       // Rising / Falling
```

### 模拟触发

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.AI0;        // AI0~AI31
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;  // Edge / Hysteresis / Window

// Edge 模式
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
```

### 多通道模拟触发

```csharp
aiTask.Trigger.Type = AITriggerType.MultichannelAnalog;
// MultichannelAnalog 使用与 Analog 相同的配置结构
aiTask.Trigger.Multichannel.Source = AIAnalogTriggerSource.AI0;
aiTask.Trigger.Multichannel.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Multichannel.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Multichannel.Edge.Threshold = 2.5;
```

### 触发模式（Trigger Mode）

```csharp
// Start 触发模式（默认）：收到触发信号后开始采集
aiTask.Trigger.Mode = AITriggerMode.Start;

// Reference 触发模式：仅在 Finite 模式下有效，收到触发信号后停止采集
aiTask.Trigger.Mode = AITriggerMode.Reference;
```

### 重触发配置

```csharp
// 配置重触发次数
aiTask.Trigger.ReTriggerCount = 5;  // 触发 5 次后停止
// ReTriggerCount = 0 或 1：只触发 1 次
// ReTriggerCount = -1：连续重触发，直到手动 Stop
```

### 预触发样本（Pre-Trigger Samples）

```csharp
// 仅在 Reference 触发模式下有效
aiTask.Trigger.Mode = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 1000;  // 保存触发前的 1000 个样本
```

### AITriggerType 枚举

| 值           | 说明                    |
| ------------ | ----------------------- |
| `Immediate`  | 立即触发（默认）        |
| `Digital`    | 数字边沿触发            |
| `Analog`     | 模拟边沿/窗口触发       |
| `Soft`       | 软件触发                |
| `MultichannelAnalog` | 多通道模拟触发  |

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;        // Finite / Infinite
aiTask.Record.Length = 10.0;                     // 录制时长（秒），Finite 有效
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

## JY5321ADITask — 数字输入任务

### 构造函数

```csharp
new JY5321ADITask(int slotNumber)
```

### 方法

```csharp
void AddChannel(int lineID)                         // 添加线（0~31）
void AddChannel(int[] lineIDs)
void RemoveChannel(int lineID)

void Start()
void Stop()

// Single 模式
void ReadSinglePoint(ref bool readValue, int line)
```

---

## JY5321ADOTask — 数字输出任务

### 构造函数

```csharp
new JY5321ADOTask(int slotNumber)
```

### 方法

```csharp
void AddChannel(int lineID)                         // 添加线（0~31）
void AddChannel(int[] lineIDs)
void RemoveChannel(int lineID)

void Start()
void Stop()

// Single 模式
void WriteSinglePoint(bool writeValue, int line)
```

---

## JY5321ACITask — 计数器输入任务

### 构造函数

```csharp
new JY5321ACITask(int slotNumber, int channel)      // channel: 0 或 1
```

### 属性

| 属性                  | 类型                | 说明                                    |
| --------------------- | ------------------- | --------------------------------------- |
| `Mode`                | `CIMode`            | Single / Finite / Continuous            |
| `Type`                | `CIType`            | 测量类型                                |
| `EdgeCounting`        | `CIEdgeCounting`    | 边沿计数配置                            |
| `Frequency`           | `CIFrequency`       | 频率测量配置                            |
| `Period`              | `CIPeriod`          | 周期测量配置                            |
| `Pulse`               | `CIPulse`           | 脉冲测量配置                            |

### CIType 枚举

| 值                    | 说明                    |
| --------------------- | ----------------------- |
| `EdgeCounting`        | 边沿计数                |
| `Frequency`           | 频率测量（Hz）          |
| `Period`              | 周期测量（秒）          |
| `Pulse`               | 脉冲宽度测量            |
| `TwoEdgeSeparation`   | 两边沿间隔测量          |
| `Encode`              | 正交编码器              |
| `EncoderTwoPulse`     | 双脉冲编码器            |
| `SemiPeriod`          | 半周期测量              |

### 读取方法

```csharp
void ReadSinglePoint(ref uint count)                              // EdgeCounting
void ReadSinglePoint(ref double measurement, int timeout)         // Frequency/Period
void ReadSinglePoint(ref double m1, ref double m2, int timeout)   // Pulse/TwoEdgeSeparation
```

---

## JY5321ACOTask — 计数器输出任务

### 构造函数

```csharp
new JY5321ACOTask(int slotNumber, int channel)      // channel: 0 或 1
```

### 属性

| 属性                  | 类型                | 说明                                    |
| --------------------- | ------------------- | --------------------------------------- |
| `Mode`                | `COMode`            | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `IdleState`           | `COIdleState`       | Low / High                              |
| `InitialDelay`        | `uint`              | 初始延迟                                |
| `TransferedPulses`    | `ulong`             | 已传输脉冲数                            |

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
bool WaitUntilDone()
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
| `NoChannelAdded`                                 | 未添加通道                  |
| `StartTaskFailed`                                | 启动 Task 失败              |
| `StopTaskFailed`                                 | 停止 Task 失败              |
| `BufferDataOverflow`                             | 采集缓冲区溢出              |
| `ReadDataTimeout`                                | 读取超时                    |
| `SampleRateParameterInvalid`                     | 采样率超限                  |

---

## 设备硬件规格

| 参数                  | 值                              |
| --------------------- | ------------------------------- |
| AI 分辨率             | 18-bit                          |
| AI 通道数             | 32（JY-5321A）/ 16（JY-5322A）   |
| AI 最大采样率         | 1 MS/s/ch（同步采样）            |
| AI 输入量程           | ±10V（参考 AIGND）               |
| AI 输入阻抗           | 高阻抗                          |
| AI DC 精度            | 220 ppm                         |
| AI 抗混叠滤波器       | 25 kHz / 220 kHz 软件可选        |
| AI 过采样             | 支持板载过采样                   |
| 硬件定时 DIO          | 16 通道                         |
| 静态 DIO              | 32（JY-5321A）/ 16（JY-5322A）   |
| 计数器                | 2 个（JY-5321A）/ 1 个（JY-5322A）|
| 接口                  | PXIe / PCIe / USB / DDA         |

### 产品型号

| 型号        | 描述                                          |
| ----------- | --------------------------------------------- |
| PXIe-5321A  | 32-ch, 18-bit, 1 MS/s/ch, 32 DIO, PXIe 同步采样 |
| PCIe-5321A  | 32-ch, 18-bit, 1 MS/s/ch, 32 DIO, PCIe 同步采样 |
| USB-5321A   | 32-ch, 18-bit, 1 MS/s/ch, 32 DIO, USB 同步采样  |
| PXIe-5322A  | 16-ch, 18-bit, 1 MS/s/ch, 16 DIO, PXIe 同步采样 |
| PCIe-5322A  | 16-ch, 18-bit, 1 MS/s/ch, 16 DIO, PCIe 同步采样 |
| USB-5322A   | 16-ch, 18-bit, 1 MS/s/ch, 16 DIO, USB 同步采样  |


---

# 完整代码示例

# JY5321A 代码示例集

> 所有示例均来自 `JY5321A_V1.0.0_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点采集（Console）

``csharp
using System;
using JY5321A;

// 1. 创建 AI Task（按槽位号）
Console.WriteLine("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());
JY5321AAITask aiTask = new JY5321AAITask(boardNum);

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
```

---

## 示例 2：AI 连续采集（WinForm + Timer）

``csharp
private JY5321AAITask aiTask;
private double[] readBuffer;

// 启动按钮
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        // 1. 创建 Task
        aiTask = new JY5321AAITask(0);

        // 2. 添加通道
        aiTask.AddChannel(0, -10, 10);

        // 3. 配置连续模式
        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 10000;

        // 4. 启动 Task
        aiTask.Start();

        // 5. 分配缓冲区
        readBuffer = new double[1000];

        // 6. 启动 Timer
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

// Timer Tick
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
```

---

## 示例 3：AI 有限采集（多通道）

``csharp
private JY5321AAITask aiTask;
private double[,] readValue;

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JY5321AAITask(0);

    // 添加多个通道
    int[] channels = { 0, 1, 2 };
    aiTask.AddChannel(channels, -10, 10);

    // 配置有限模式
    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 1000;
    aiTask.SampleRate = 10000;

    aiTask.Start();

    // 分配多通道缓冲区
    readValue = new double[1000, 3];

    // 等待采集完成
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

## 示例 4：AI 数字触发采集

``csharp
aiTask = new JY5321AAITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 50000;

// 配置数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发信号...
```

---

## 示例 5：AI 外部时钟采集

``csharp
aiTask = new JY5321AAITask(0);
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;

// 使用外部时钟
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10000;

aiTask.Start();
```

---

## 示例 6：DI 单点采集

``csharp
var diTask = new JY5321ADITask(0);
diTask.AddChannel(0);
diTask.Start();

bool readValue;
diTask.ReadSinglePoint(ref readValue, 0);
Console.WriteLine($"Line 0 = {readValue}");

diTask.Stop();
```

---

## 示例 7：DO 单点输出

``csharp
var doTask = new JY5321ADOTask(0);
doTask.AddChannel(0);
doTask.Start();

// 输出高电平
doTask.WriteSinglePoint(true, 0);
System.Threading.Thread.Sleep(500);

// 输出低电平
doTask.WriteSinglePoint(false, 0);

doTask.Stop();
```

---

## 示例 8：CI 边沿计数

``csharp
private JY5321ACITask ciTask;

private void button_start_Click(object sender, EventArgs e)
{
    // 计数器通道 0
    ciTask = new JY5321ACITask(0, 0);
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

## 示例 9：CI 频率测量

``csharp
ciTask = new JY5321ACITask(0, 0);
ciTask.Mode = CIMode.Single;
ciTask.Type = CIType.Frequency;

ciTask.Start();

double freq;
ciTask.ReadSinglePoint(ref freq, -1);  // 阻塞等待测量完成
Console.WriteLine($"频率: {freq:F2} Hz");

ciTask.Stop();
```

---

## 示例 10：CO 脉冲输出

``csharp
private JY5321ACOTask coTask;

private void button_start_Click(object sender, EventArgs e)
{
    coTask = new JY5321ACOTask(0, 0);
    coTask.Mode = COMode.Single;
    coTask.IdleState = COIdleState.Low;
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

## 示例 11：CO 连续脉冲输出

``csharp
coTask = new JY5321ACOTask(0, 0);
coTask.Mode = COMode.ContinuousWrapping;
coTask.IdleState = COIdleState.Low;

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

## 综合技巧

### 多通道数据解析

``csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[1000, 3];
aiTask.ReadData(ref buf, 1000, -1);

// 提取各通道数据
double[] ch0 = new double[1000];
for (int i = 0; i < 1000; i++)
{
    ch0[i] = buf[i, 0];
}
```

### 窗体关闭资源释放

``csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timerFetch != null) timerFetch.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
        if (ciTask != null) ciTask.Stop();
        if (coTask != null) coTask.Stop();
    }
    catch (JYDriverException ex) 
    { 
        MessageBox.Show(ex.Message); 
    }
}
```

---

## 12. Record 有限录制模式（Finite Streaming）

录制固定时长的数据到文件，录制期间可预览数据：

``csharp
private JY5321AAITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5321AAITask(0);
        
        // 通道
        aiTask.AddChannel(0, -10, 10);
        
        //  Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 250000;
        
        // 配置录制参数
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 10.0;              // 录制 10 秒 
        
        aiTask.Start();
        
        // 开始预览
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
    
    // 预览数据
    if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
        // 显示预览数据...
    }
    
    // 录制状态
    double recordedLength;
    bool recordDone;
    aiTask.GetRecordStatus(out recordedLength, out recordDone);
    
    if (recordDone)
    {
        timer_Preview.Enabled = false;
        aiTask.Stop();
        aiTask.Channels.Clear();
        MessageBox.Show("录制完成！");
    }
    else
    {
        timer_Preview.Enabled = true;
    }
}
```

---

## 13. Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止。定时器中只读取预览数据，不检查录制状态：

``csharp
private JY5322AAITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY5322AAITask(0);
        
        // 添加多通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, -10, 10);
        }
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 50000;  // 50 kS/s
        
        // 配置无限录制（不设置 Length）
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
        // 显示预览数据...
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

## 14. 录制数据回放

读取已录制的 bin 文件并回放显示（JY5321A 数据格式为 short）：

``csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private short[,] rawData;
private double[,] playbackData;
private ulong totalSamples;
private ScalingCoefficients[] scalingCoefficients;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    
    if (!File.Exists(fileBrowser.FileName))
    {
        MessageBox.Show("文件不存在");
        return;
    }
    
    // 计算总样本数（JY5321A 数据格式为 short）
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 4;  // 录制时的通道数
    totalSamples = (ulong)(fileInfo.Length / sizeof(short) / channelCount);
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    rawData = new short[1000, channelCount];
    playbackData = new double[1000, channelCount];
    
    button_OpenFile.Enabled = false;
    button_Playback.Enabled = true;
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        for (int i = 0; i < rawData.GetLength(0); i++)
        {
            for (int ch = 0; ch < rawData.GetLength(1); ch++)
            {
                if (playbackStream.Position < playbackStream.Length)
                {
                    rawData[i, ch] = playbackReader.ReadInt16();
                    playbackData[i, ch] = rawData[i, ch] * scalingCoefficients[ch].Scale;
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
        MessageBox.Show("回放完成");
    }
}
```
