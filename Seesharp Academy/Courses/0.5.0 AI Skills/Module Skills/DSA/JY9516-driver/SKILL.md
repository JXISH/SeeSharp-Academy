---
name: jy9516-driver
description: 提供 JYTEK JY9516 系列（USB/PCIe/PXIe-9516）动态信号分析仪（DSA）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、多通道同步采集、IEPE激励、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、录制模式（Finite/Infinite Streaming）。当用户使用 JY9516、USB-9516、PCIe-9516、PXIe-9516、JY9516AITask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record、IEPE 开发振动分析、噪声测试、声学测量、模态分析应用时自动应用。
---

# JY9516 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DSA\JY9516\Bin\JY9516.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DSA\JY9516\Bin\JY9516.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9516;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 16 通道同步测量（差分输入） |
| AI 采样率 | 62.5 S/s ~ 256 kS/s |
| AI 输入量程 | ±0.3125V / ±0.625V / ±1.25V / ±2.5V / ±5V / ±10V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分（Differential） |
| AI IEPE 激励 | 每通道 4mA IEPE 激励，软件使能 |
| AI 动态范围 | 111 dB |
| 接口 | USB / PCIe / PXIe |

## 通用编程范式

### AI 采集任务流程

```
创建 AITask → 添加通道 → 配置参数 → 启动 → 读取数据 → 停止
```

### 标准代码框架

```csharp
// ========== AI 采集 ==========
// 1. 创建 Task
JY9516AITask aiTask = new JY9516AITask(boardNumber);

// 2. 添加通道（支持 IEPE 激励）
// 方式1：使用 AIRange 枚举（推荐）
aiTask.AddChannel(channelID, AIRange._10V, Coupling.DC, iepeEnable);
// 方式2：使用量程值
// aiTask.AddChannel(channelID, lowRange, highRange, Coupling.DC, iepeEnable);

// 3. 配置采集模式
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 256000;

// 4. 启动采集
aiTask.Start();

// 5. 读取数据（二维数组，每列一个通道）
double[,] dataBuffer = new double[samplesToRead, aiTask.Channels.Count];
aiTask.ReadData(ref dataBuffer, timeout);

// 6. 停止
aiTask.Stop();
```

## 关键 API 速查

### AI 通道配置

| API | 说明 |
|-----|------|
| `aiTask.AddChannel(channel, AIRange._10V, coupling, iepeEnable)` | 添加模拟输入通道（使用枚举） |
| `aiTask.AddChannel(channel, lowRange, highRange, coupling, iepeEnable)` | 添加模拟输入通道（使用量程值） |
| `aiTask.Channels.Count` | 已添加通道数 |
| `aiTask.Channels[i].ID` | 获取通道 ID |

### 采集/输出模式

| 模式 | 说明 |
|------|------|
| `AIMode.Single` | AI 单点采集模式 |
| `AIMode.Finite` | AI 有限采集模式 |
| `AIMode.Continuous` | AI 连续采集模式 |
| `AIMode.Record` | AI 录制模式（流式写入文件） |

### 触发配置

```csharp
// 数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.Ext_Trig;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 模拟触发
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 0.5;

// 软触发
aiTask.Trigger.Type = AITriggerType.Software;
```

### 多卡同步

```csharp
// 主卡配置
masterTask.Sync.Topology = SyncTopology.Master;
masterTask.Sync.Terminal = SyncTerminal.PXI_Trig0;
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;

// 从卡配置
slaveTask.Sync.Topology = SyncTopology.Slave;
slaveTask.Sync.Terminal = SyncTerminal.PXI_Trig0;
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;

// 提交配置（从卡先提交）
slaveTask.Commit();
masterTask.Commit();

// 启动（从卡先启动）
slaveTask.Start();
masterTask.Start();
```

## 关键枚举

### Coupling（耦合方式）

- `Coupling.DC` - DC 耦合
- `Coupling.AC` - AC 耦合

### AIRange（AI 量程枚举）

- `AIRange._10V` - ±10V
- `AIRange._5V` - ±5V
- `AIRange._2p5V` - ±2.5V
- `AIRange._1p25V` - ±1.25V
- `AIRange._0p625V` - ±0.625V
- `AIRange._0p3125V` - ±0.3125V

### 数字滤波器（AIDigitalFiltering）

JY9516 内置两级数字滤波（LPF/HPF），通过 `aiTask.DigitalFiltering` 配置：

```csharp
// 低通滤波器（LPF）模式
aiTask.DigitalFiltering.LPF.Mode = LPFMode.Normal;         // Normal / WideBandwidth

// 高通滤波器（HPF）
aiTask.DigitalFiltering.HPF.CutoffFrequency = 10.0;        // 截止频率 Hz，上限约为采样率的 1%
aiTask.DigitalFiltering.HPF.Order = HPFOrder.OneOrder;     // OneOrder / TwoOrder
aiTask.DigitalFiltering.HPF.EnableChannel(0);              // 对指定通道启用 HPF
// aiTask.DigitalFiltering.HPF.DisableChannel(0);
```

#### LPFMode

- `LPFMode.Normal` - 正常带宽（0.4×fs）
- `LPFMode.WideBandwidth` - 宽带宽（0.4535×fs，限 ≤128 kS/s）

#### HPFOrder

- `HPFOrder.OneOrder` - 一阶（-3 dB）
- `HPFOrder.TwoOrder` - 二阶（-6 dB）

### 信号导出（AISignalExport）

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.StartTrigger, SignalExportDestination.Ext_Trig);
// aiTask.SignalExport.Clear(SignalExportDestination.PXI_Trig0);
// aiTask.SignalExport.ClearAll();
```

#### AISignalExportSource

- `SampleClock` - AI 采样时钟
- `Sync` - 主卡同步信号（多卡同步）
- `ReadyForStartTrigger` - 就绪信号
- `StartTrigger` - 启动触发信号
- `EndOfRecord` - 录制结束信号
- `EndOfAcquisition` - 采集结束信号

#### SignalExportDestination

- `Ext_Trig` - 外部触发
- `PXI_Trig0` ~ `PXI_Trig7` - PXI 触发总线
- `PXI_Star` - PXI Star 触发

### 高级配置（AIAdvanced）

```csharp
// 停止采集时自动关闭 IEPE 激励（默认 false）
aiTask.Advanced.CloseIEPEOnStop = true;
```

## 设备属性

```csharp
// 获取设备信息
int totalChannels = JY9516Device.TotalNumberOfChannels;  // AI 通道总数
double minRate = JY9516Device.MinSampleRate;             // 最小采样率
double maxRate = JY9516Device.MaxSampleRate;             // 最大采样率
```

---

## IEPE 传感器检测

JY9516 支持 IEPE 传感器连接状态检测：

```csharp
aiTask = new JY9516AITask(0);
aiTask.AddChannel(0, AIRange._10V, Coupling.DC, true);  // 启用 IEPE

// 检测 IEPE 连接状态
var iepeStatus = aiTask.DetectIEPEConnection();
// 返回 Dictionary<int, IEPEConnectionStatus>，key 为通道 ID
// IEPEConnectionStatus: Normal / OpenCircuit（开路） / ShortCircuit（短路）

foreach (var status in iepeStatus)
{
    Console.WriteLine($"Channel {status.Key}: {status.Value}");
}
```

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
aiTask = new JY9516AITask(0);
aiTask.AddChannel(0, AIRange._10V, Coupling.DC, false);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 256000;

// 配置录制参数
aiTask.Record.FilePath = @"C:\Data\signal.bin";
aiTask.Record.Mode = RecordMode.Finite;          // Finite / Infinite
aiTask.Record.Length = 10.0;                     // 录制时长（秒），Finite 有效

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

## 异常处理

所有操作可能抛出异常，应使用 try-catch 处理：

```csharp
try
{
    aiTask.Start();
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message);
}
```

## 开发环境要求

- .NET Framework 4.0 或更高版本
- 驱动版本：JY9516 Installer_V1.0.0 或更高
- 依赖包：SeeSharpTools.JY.GUI 1.4.7


---

# 完整 API 参考

# JY9516 API 参考手册

## 命名空间

```csharp
using JY9516;
```

## 核心类

### JY9516AITask

动态信号分析仪模拟输入任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9516AITask(int boardNumber)` | 创建指定板卡的 AI 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `List<AIChannel>` | 通道集合（List类型） |
| `Mode` | `AIMode` | 采集模式（Single/Finite/Continuous/Record） |
| `SampleRate` | `double` | 采样率（Hz） |
| `SamplesToAcquire` | `int` | 有限模式下要采集的样本数 |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数 |
| `TransferedSamples` | `ulong` | 已传输的样本数 |
| `State` | `SamplingState` | 采样状态（只读） |
| `Trigger` | `AITrigger` | 触发配置 |
| `Sync` | `Sync` | 同步配置 |
| `SignalExport` | `AISignalExport` | 信号导出配置 |
| `Device` | `AIDevice` | 设备配置（参考时钟等） |
| `DigitalFiltering` | `AIDigitalFiltering` | 数字滤波器配置（LPF/HPF） |
| `Advanced` | `AIAdvanced` | 高级选项（CloseIEPEOnStop 等） |
| `Record` | `AIRecord` | 录制配置对象 |
| `SampleInfo` | `SampleInfo` | 采样信息（调制频率、群延迟） |
| `DisableCalibration` | `bool` | 禁用校准（默认false） |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int channel, AIRange range, Coupling coupling, bool iepeEnable)` | 添加模拟输入通道（使用枚举） |
| `AddChannel(int channel, double low, double high, Coupling coupling, bool iepeEnable)` | 添加模拟输入通道（使用量程值） |
| `RemoveChannel(int channel)` | 移除通道 |
| `Start()` | 启动采集任务 |
| `Stop()` | 停止采集任务 |
| `Commit()` | 提交任务配置（用于多卡同步） |
| `StartSync()` | 重新启动多卡同步（从卡先调用，再调用主卡） |
| `ReadData(ref double[,] buffer, int timeout)` | 读取采集数据（二维数组） |
| `ReadSinglePoint(ref double[] values)` | 单点读取所有通道（Single 模式） |
| `WaitUntilDone(int timeout)` | 等待任务完成（Finite 模式） |
| `SendSoftwareTrigger()` | 发送软件触发 |
| `GetRecordPreviewData(ref double[,] buf, int samples, int timeout)` | 预览录制数据 |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态 |
| `DetectIEPEConnection()` | 检测 IEPE 传感器连接状态 |
| `GetConversionStatus()` | 获取各 ADC 通道的转换状态（CRC 错误/滤波器饱和等） |
| `SampleInfo` | 采样信息属性（包含调制频率和群延迟） |

---

## 设备硬件规格

| 参数 | 值 |
|------|-----|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 16 通道同步测量 |
| AI 采样率 | 62.5 S/s ~ 256 kS/s |
| AI 输入量程 | ±0.3125V / ±0.625V / ±1.25V / ±2.5V / ±5V / ±10V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分 |
| AI IEPE 激励 | 每通道 4mA，软件使能 |
| AI 动态范围 | 111 dB |
| 接口 | USB / PCIe / PXIe |

### 产品型号

| 型号 | 描述 |
|------|------|
| USB-9516 | 16-CH 24-Bit 256 kS/s USB 高分辨率动态信号采集模块 |
| PCIe-9516 | 16-CH 24-Bit 256 kS/s PCIe 高分辨率动态信号采集模块 |
| PXIe-9516 | 16-CH 24-Bit 256 kS/s PXIe 高分辨率动态信号采集模块 |

---

## 枚举类型

### AIMode

AI 采集模式枚举。

| 值 | 说明 |
|----|------|
| `Single` | 单点采集模式 |
| `Finite` | 有限采集模式 |
| `Continuous` | 连续采集模式 |
| `Record` | 录制模式（数据流式写入文件） |

### Coupling

输入耦合方式枚举。

| 值 | 说明 |
|----|------|
| `DC` | DC 耦合 |
| `AC` | AC 耦合 |

### AIRange

AI 电压量程枚举。

| 值 | 说明 |
|----|------|
| `_10V` | ±10V |
| `_5V` | ±5V |
| `_2p5V` | ±2.5V |
| `_1p25V` | ±1.25V |
| `_0p625V` | ±0.625V |
| `_0p3125V` | ±0.3125V |

### AITriggerType

触发类型枚举。

| 值 | 说明 |
|----|------|
| `Immediately` | 立即触发（无触发） |
| `Digital` | 数字触发 |
| `Analog` | 模拟触发 |
| `Software` | 软触发 |

### AIDigitalTriggerSource

数字触发源枚举。

| 值 | 说明 |
|----|------|
| `Ext_Trig` | 外部触发（PFI0） |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |
| `PXI_Star` | PXI Star 触发 |

### AIDigitalTriggerEdge

数字触发边沿枚举。

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿触发 |
| `Falling` | 下降沿触发 |

### AIAnalogTriggerSource

模拟触发源枚举。

| 值 | 说明 |
|----|------|
| `Channel_0` ~ `Channel_15` | 使用对应通道作为触发源 |

### AIAnalogTriggerComparator

模拟触发比较器类型枚举。

| 值 | 说明 |
|----|------|
| `Edge` | 边沿比较器 |
| `Hysteresis` | 滞回比较器 |
| `Window` | 窗口比较器 |

### AIAnalogTriggerEdge

模拟触发边沿枚举。

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿触发 |
| `Falling` | 下降沿触发 |

### AIAnalogWindowCondition

模拟触发窗口条件枚举。

| 值 | 说明 |
|----|------|
| `Entering` | 进入窗口触发 |
| `Leaving` | 离开窗口触发 |

### SyncTopology

同步拓扑枚举。

| 值 | 说明 |
|----|------|
| `Independent` | 独立模式（默认） |
| `Master` | 主卡 |
| `Slave` | 从卡 |

### SyncTerminal

同步终端枚举。

| 值 | 说明 |
|----|------|
| `Ext_Trig` | 外部触发（PFI0） |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |
| `PXI_Star` | PXI Star 触发 |

### ReferenceClockSource

参考时钟源枚举。

| 值 | 说明 |
|----|------|
| `Internal` | 内部时钟 |
| `External` | 外部时钟 |

### ExternalReferenceClockTerminal

外部参考时钟终端枚举。

| 值 | 说明 |
|----|------|
| `PXIe_Clk100` | PXIe 100MHz 背板时钟 |

### SamplingState

采样状态枚举（只读）。

| 值 | 说明 |
|----|------|
| `Idle` | 空闲状态 |
| `PreSampling` | 预采样状态 |
| `WaitingForTrigger` | 等待触发状态 |
| `PostSampling` | 后采样状态 |
| `Done` | 完成状态 |

---

## 设备类

### JY9516Device

设备信息类（静态属性）。

| 属性 | 类型 | 说明 |
|------|------|------|
| `TotalNumberOfChannels` | `int` | AI 通道总数 |
| `MinSampleRate` | `double` | AI 最小采样率 |
| `MaxSampleRate` | `double` | AI 最大采样率 |

---

## 配置对象

### AITrigger

触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AITriggerType` | 触发类型 |
| `Mode` | `AITriggerMode` | 触发模式（Start/Reference） |
| `Digital` | `AIDigitalTrigger` | 数字触发配置 |
| `Analog` | `AIAnalogTrigger` | 模拟触发配置 |
| `ReTriggerCount` | `int` | 重复触发次数 |
| `PreTriggerSamples` | `uint` | 预触发采样点数（仅Reference模式有效） |

### AIDigitalTrigger

数字触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIDigitalTriggerSource` | 触发源 |
| `Edge` | `AIDigitalTriggerEdge` | 触发边沿 |

### AIAnalogTrigger

模拟触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIAnalogTriggerSource` | 触发源 |
| `Comparator` | `AIAnalogTriggerComparator` | 比较器类型 |
| `Edge` | `AIAnalogEdgeComparator` | 边沿比较器配置 |
| `Hysteresis` | `AIAnalogHysteresisComparator` | 滞回比较器配置 |
| `Window` | `AIAnalogWindowComparator` | 窗口比较器配置 |

### AIAnalogEdgeComparator

边沿比较器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Slope` | `AIAnalogTriggerEdge` | 触发边沿 |
| `Threshold` | `double` | 触发电平 |

### AIAnalogHysteresisComparator

滞回比较器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Slope` | `AIAnalogTriggerEdge` | 触发边沿 |
| `HighThreshold` | `double` | 高阈值 |
| `LowThreshold` | `double` | 低阈值 |

### AIAnalogWindowComparator

窗口比较器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Condition` | `AIAnalogWindowCondition` | 窗口条件 |
| `HighThreshold` | `double` | 高阈值 |
| `LowThreshold` | `double` | 低阈值 |

### Sync

同步配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Topology` | `SyncTopology` | 同步拓扑（主/从） |
| `Terminal` | `SyncTerminal` | 同步终端 |
| `State` | `SyncState` | 同步状态（只读，仅从卡有效） |

### SyncState

同步状态枚举。

| 值 | 说明 |
|----|------|
| `WaitingSync` | 等待同步信号（从卡） |
| `Synchronized` | 已接收到同步信号（从卡） |
| `Invalid` | 无效状态（未启用同步或主卡） |

### AIDevice

设备配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ReferenceClock` | `ReferenceClock` | 参考时钟配置 |

### ReferenceClock

参考时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `ReferenceClockSource` | 时钟源 |
| `External` | `ExternalReferenceClock` | 外部时钟配置 |

### ExternalReferenceClock

外部参考时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ExternalReferenceClockTerminal` | 时钟终端 |

---

### AIRecord

录制配置对象。

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;  // Finite / Infinite
aiTask.Record.Length = 10.0;              // 录制时长（秒），Finite 有效
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |

---

### RecordMode

录制模式枚举。

| 值 | 说明 |
|----|------|
| `Finite` | 有限录制，录制指定时长后自动停止 |
| `Infinite` | 无限录制，需手动调用 Stop 停止 |

---

### FileFormat

文件格式枚举。

| 值 | 说明 |
|----|------|
| `Bin` | 二进制格式（double 类型） |


---

# JY9516 代码示例

## 1. AI 连续采集（多通道）

```csharp
using System;
using System.Windows.Forms;
using JY9516;
using SeeSharpTools.JY.ArrayUtility;

namespace JY9516Example
{
    public partial class MainForm : Form
    {
        private JY9516AITask aiTask;
        private double[,] dataToRead;
        private double[,] dataToPlot;

        private void btn_Start_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建 Task
                aiTask = new JY9516AITask(0);  // 板卡 0
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 256000;    // 256 kS/s

                // 添加通道（启用所有 16 个通道，IEPE 关闭）
                for (int i = 0; i < 16; i++)
                {
                    aiTask.AddChannel(i, AIRange._10V, Coupling.DC, false);
                }

                // 启动采集
                aiTask.Start();

                // 初始化数据缓冲区
                int samplesToUpdate = 10000;
                dataToRead = new double[samplesToUpdate, aiTask.Channels.Count];
                dataToPlot = new double[aiTask.Channels.Count, samplesToUpdate];

                timer_CheckStatus.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void timer_CheckStatus_Tick(object sender, EventArgs e)
        {
            ulong availableSamples = aiTask.AvailableSamples;
            
            if (availableSamples >= (ulong)dataToRead.GetLength(0))
            {
                // 读取数据
                aiTask.ReadData(ref dataToRead, 2000);
                
                // 转置数据用于显示（通道 x 样本）
                ArrayManipulation.Transpose(dataToRead, ref dataToPlot);
                
                // 显示数据...
            }
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            aiTask.Stop();
            timer_CheckStatus.Enabled = false;
        }
    }
}
```

## 2. AI 有限采集（单通道）

```csharp
private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516AITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 100000;  // 采集 100k 个点

        // 添加单通道，启用 IEPE 激励
        aiTask.AddChannel(0, AIRange._10V, Coupling.AC, true);

        aiTask.Start();

        double[,] dataBuffer = new double[aiTask.SamplesToAcquire, 1];
        
        // 等待采集完成
        while (aiTask.AvailableSamples < (ulong)aiTask.SamplesToAcquire)
        {
            Application.DoEvents();
        }

        aiTask.ReadData(ref dataBuffer, 5000);
        aiTask.Stop();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 3. AI 数字触发采集

```csharp
private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516AITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;

        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, Coupling.DC, false);

        // 配置数字触发
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.Ext_Trig;
        aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

        aiTask.Start();
        
        // 等待触发并采集完成...
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 4. AI 模拟触发采集

```csharp
private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516AITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;

        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, Coupling.DC, false);

        // 配置模拟触发
        aiTask.Trigger.Type = AITriggerType.Analog;
        aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
        aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
        aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
        aiTask.Trigger.Analog.Edge.Threshold = 1.0;  // 触发电平 1.0V

        aiTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 5. 多卡同步采集

```csharp
private JY9516AITask masterTask;
private JY9516AITask slaveTask;

private void button_Commit_Click(object sender, EventArgs e)
{
    try
    {
        // 创建任务
        slaveTask = new JY9516AITask(1);  // 从卡槽位 1
        masterTask = new JY9516AITask(0); // 主卡槽位 0

        // 添加通道
        slaveTask.AddChannel(0, AIRange._10V, Coupling.DC, false);
        masterTask.AddChannel(0, AIRange._10V, Coupling.DC, false);

        // 配置采集参数
        slaveTask.Mode = AIMode.Finite;
        masterTask.Mode = AIMode.Finite;
        slaveTask.SampleRate = 256000;
        masterTask.SampleRate = 256000;
        slaveTask.SamplesToAcquire = 100000;
        masterTask.SamplesToAcquire = 100000;

        // 参考时钟配置
        slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
        slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
        masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
        masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;

        // 同步拓扑配置
        slaveTask.Sync.Topology = SyncTopology.Slave;
        masterTask.Sync.Topology = SyncTopology.Master;
        slaveTask.Sync.Terminal = SyncTerminal.PXI_Trig0;
        masterTask.Sync.Terminal = SyncTerminal.PXI_Trig0;

        // 提交配置（从卡先提交）
        slaveTask.Commit();
        masterTask.Commit();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        // 启动（从卡先启动）
        slaveTask.Start();
        masterTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 6. IEPE 传感器采集

```csharp
private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516AITask(0);
        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 256000;

        // 添加通道，启用 IEPE 激励（4mA）
        // 适用于加速度计、麦克风等 IEPE 传感器
        aiTask.AddChannel(0, AIRange._10V, Coupling.AC, true);

        aiTask.Start();
        // ...
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 7. 使用设备属性

```csharp
private void MainForm_Load(object sender, EventArgs e)
{
    // 获取设备信息
    int totalChannels = JY9516Device.TotalNumberOfChannels;
    double minRate = JY9516Device.MinSampleRate;
    double maxRate = JY9516Device.MaxSampleRate;

    // 设置采样率范围
    numericUpDown_SampleRate.Minimum = (decimal)minRate;
    numericUpDown_SampleRate.Maximum = (decimal)maxRate;

    // 添加所有通道到选择列表
    for (int i = 0; i < totalChannels; i++)
    {
        comboBox_Channel.Items.Add(string.Format("Ch{0}", i));
    }
}
```

## 8. Record 有限录制模式（Finite Streaming）

录制固定时长的数据到文件，录制完成后自动停止。定时器中通过`GetRecordStatus`检查录制状态：

```csharp
private JY9516AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516AITask(0);
        
        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, Coupling.DC, false);
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 256000;
        
        // 配置录制参数
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 10.0;              // 录制 10 秒
        
        aiTask.Start();
        
        // 初始化预览缓冲区
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
        try
        {
            if (aiTask != null)
            {
                aiTask.Stop();
            }
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
        
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
            // 显示预览数据...
        }
    }
    
    timer_Preview.Enabled = true;
}
```

---

## 9. Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止。定时器中只读取预览数据，不检查录制状态：

```csharp
private JY9516AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516AITask(0);
        
        // 添加多通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, AIRange._10V, Coupling.DC, false);
        }
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 51200;  // 51.2 kS/s
        
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

## 10. 录制数据回放

读取已录制的 bin 文件并回放显示：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;
private ulong totalSamples;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    // 录制文件
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    
    if (!File.Exists(fileBrowser.FileName))
    {
        MessageBox.Show("文件不存在");
        return;
    }
    
    // 数据格式为 double
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 4;  // 录制时通道数
    totalSamples = (ulong)(fileInfo.Length / sizeof(double) / channelCount);
    
    // 打开文件
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
        // 读取数据
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
        
        // 显示数据
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
