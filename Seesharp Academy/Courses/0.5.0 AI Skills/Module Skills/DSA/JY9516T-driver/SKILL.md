---
name: jy9516t-driver
description: 提供 JYTEK JY9516T 系列（USB/PCIe/PXIe-9516T）温度监测型动态信号分析仪（DSA）的完整 C# 驱动开发指引。涵盖模拟输入（AI）有限/连续/录制采集、多通道同步采集、IEPE激励、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、多卡同步、录制模式（Finite/Infinite Streaming）、设备温度监测。当用户使用 JY9516T、USB-9516T、PCIe-9516T、PXIe-9516T、JY9516TAITask、AIMode.Finite、AIMode.Continuous、AIMode.Record、IEPE 开发温度监测、振动分析、噪声测试应用时自动应用。
---

# JY9516T 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DSA\JY9516T\Bin\JY9516T.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DSA\JY9516T\Bin\JY9516T.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9516T;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 16 通道同步测量（差分/伪差分输入） |
| AI 采样率 | 62.5 S/s ~ 256 kS/s |
| AI 输入量程 | ±0.3125V / ±0.625V / ±1.25V / ±2.5V / ±5V / ±10V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分（Differential）/ 伪差分（PseudoDifferential） |
| AI IEPE 激励 | 每通道 4mA IEPE 激励，软件使能 |
| AI 动态范围 | 111 dB |
| 设备温度监测 | 支持读取板卡温度（℃） |
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
JY9516TAITask aiTask = new JY9516TAITask(boardNumber);

// 2. 添加通道（支持 IEPE 激励）
aiTask.AddChannel(channelID, range, terminal, coupling, iepeEnable);

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
| `aiTask.AddChannel(chID, range, terminal, coupling, iepeEnable)` | 添加单个通道（使用 AIRange 枚举） |
| `aiTask.AddChannel(chID, rangeLow, rangeHigh, terminal, coupling, iepeEnable)` | 添加单个通道（使用量程数值） |
| `aiTask.AddChannel(chIDs, range, terminal, coupling, iepeEnable)` | 批量添加通道（统一 AIRange 枚举参数） |
| `aiTask.AddChannel(chIDs, rangeLow, rangeHigh, terminal, coupling, iepeEnable)` | 批量添加通道（统一量程数值参数） |
| `aiTask.AddChannel(chIDs, rangeLow[], rangeHigh[], terminal[], coupling[], iepeEnable[])` | 批量添加通道（各自独立参数数组） |
| `aiTask.AddChannel(chIDs, range[], terminal[], coupling[], iepeEnable[])` | 批量添加通道（各自独立 AIRange 枚举数组） |
| `aiTask.RemoveChannel(chID)` | 移除指定通道（chID=-1 移除所有） |
| `aiTask.RemoveChannel(chIDs)` | 批量移除通道 |
| `aiTask.Channels.Clear()` | 清除所有通道 |
| `aiTask.Channels.Count` | 已添加通道数 |
| `aiTask.Channels[i].ID` | 获取通道 ID |

### 采集模式

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
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 模拟触发（以边沿比较器为例）
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 0.5;

// 软件触发（Start() 后调用 SendSoftwareTrigger 触发采集）
aiTask.Trigger.Type = AITriggerType.Software;
// aiTask.Start();
// aiTask.SendSoftwareTrigger();
```

### 多卡同步

```csharp
// 主卡配置
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
masterTask.SignalExport.Add(AISignalExportSource.Sync, SignalExportDestination.PXI_Trig0);

// 从卡配置
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slaveTask.SignalExport.Add(AISignalExportSource.Sync, SignalExportDestination.PXI_Trig0);

// 提交配置（从卡先提交）
slaveTask.Commit();
masterTask.Commit();

// 启动（从卡先启动）
slaveTask.Start();
masterTask.Start();
```

## 关键枚举

### AIRange（输入量程）

- `AIRange._10V` - ±10V
- `AIRange._5V` - ±5V
- `AIRange._2p5V` - ±2.5V
- `AIRange._1p25V` - ±1.25V
- `AIRange._0p625V` - ±0.625V
- `AIRange._0p3125V` - ±0.3125V

### Coupling（耦合方式）

- `Coupling.DC` - DC 耦合
- `Coupling.AC` - AC 耦合

### AITerminal（AI 输入端子配置）

- `AITerminal.Differential` - 差分输入
- `AITerminal.PseudoDifferential` - 伪差分输入

### AITriggerType（触发类型）

- `AITriggerType.Immediate` - 立即触发（无触发）
- `AITriggerType.Digital` - 数字触发
- `AITriggerType.Analog` - 模拟触发
- `AITriggerType.Software` - 软件触发

### AITriggerMode（触发模式）

- `AITriggerMode.Start` - 启动触发
- `AITriggerMode.Reference` - 参考触发

### AIDigitalTriggerSource（数字触发源）

- `AIDigitalTriggerSource.Ext_Trig` - 外部触发
- `AIDigitalTriggerSource.PXI_Trig0` ~ `AIDigitalTriggerSource.PXI_Trig7` - PXI 触发总线

### AIDigitalTriggerEdge（数字触发边沿）

- `AIDigitalTriggerEdge.Rising` - 上升沿触发
- `AIDigitalTriggerEdge.Falling` - 下降沿触发

### AIAnalogTriggerSource（模拟触发源）

- `AIAnalogTriggerSource.Channel_0` ~ `AIAnalogTriggerSource.Channel_15` - 模拟输入通道 0~15 作为触发源

### AIAnalogTriggerComparator（模拟比较器类型）

- `AIAnalogTriggerComparator.Edge` - 边沿比较器（Edge.Slope + Edge.Threshold）
- `AIAnalogTriggerComparator.Hysteresis` - 迟滞比较器（Hysteresis.Slope + HighThreshold + LowThreshold）
- `AIAnalogTriggerComparator.Window` - 窗比较器（Window.Condition + HighThreshold + LowThreshold）

### AIAnalogWindowCondition（窗比较器条件）

- `AIAnalogWindowCondition.Entering` - 信号进入窗口时触发
- `AIAnalogWindowCondition.Leaving` - 信号离开窗口时触发

### AIAnalogTriggerEdge（模拟触发边沿）

- `AIAnalogTriggerEdge.Rising` - 上升沿触发
- `AIAnalogTriggerEdge.Falling` - 下降沿触发

### SyncTopology（同步拓扑）

- `SyncTopology.Independent` - 独立模式（默认）
- `SyncTopology.Master` - 主卡
- `SyncTopology.Slave` - 从卡

### SyncState（同步状态）

- `SyncState.WaitingSync` - 从卡等待同步信号
- `SyncState.Synchronized` - 从卡已收到同步信号
- `SyncState.Invalid` - 未启用同步或是主卡时（无效状态）

### SyncTerminal（同步终端）

- `SyncTerminal.Ext_Trig` - 外部触发
- `SyncTerminal.PXI_Trig0` ~ `PXI_Trig7` - PXI 触发总线

### ReferenceClockSource（参考时钟源）

- `ReferenceClockSource.Internal` - 内部时钟
- `ReferenceClockSource.External` - 外部时钟

### ExternalReferenceClockTerminal（外部参考时钟终端）

- `ExternalReferenceClockTerminal.PXIe_Clk100` - PXIe 100MHz 背板时钟

### RecordMode（录制模式）

- `RecordMode.Finite` - 有限录制，录制指定时长后自动停止
- `RecordMode.Infinite` - 无限录制，需手动调用 Stop 停止

### SamplingState（采样状态）

- `SamplingState.Idle` - 空闲
- `SamplingState.PreSampling` - 预采样阶段
- `SamplingState.WaitingForTrigger` - 等待触发
- `SamplingState.PostSampling` - 触发后采样中
- `SamplingState.Done` - 采集完成

## 设备属性

```csharp
// 获取设备信息
int totalChannels = JY9516TDevice.GetCapability(slotNumber).AI.NumberOfChannels;
double minRate = JY9516TDevice.GetCapability(slotNumber).AI.MinSampleRate;
double maxRate = JY9516TDevice.GetCapability(slotNumber).AI.MaxSampleRate;

// 创建设备实例并获取温度
JY9516TDevice device = JY9516TDevice.GetInstance(slotNumber);
double temperature = device.Status.Temperature;  // 设备温度（℃）
```

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
aiTask = new JY9516TAITask(0);
aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);
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
```

### AIRecord 配置属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `Mode` | `RecordMode` | Finite（定长）/ Infinite（无限） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |

### 录制模式方法

| 方法 | 说明 |
|------|------|
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
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);
}
```

## 开发环境要求

- .NET Framework 4.0 或更高版本
- 驱动版本：JY9516T Installer_V1.0.0 或更高
- 依赖包：SeeSharpTools.JY.GUI 1.4.7


---

# 完整 API 参考

# JY9516T API 参考手册

## 命名空间

```csharp
using JY9516T;
```

## 核心类

### JY9516TAITask

JY9516T 模拟输入任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9516TAITask(int slotNumber)` | 使用槽位号创建 AI 任务 |
| `JY9516TAITask(string cardName)` | 使用板卡名称创建 AI 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `AIChannelCollection` | 通道集合 |
| `Mode` | `AIMode` | 采集模式（Single/Continuous/Finite/Record） |
| `SampleRate` | `double` | 采样率（Hz） |
| `SamplesToAcquire` | `int` | 有限模式下要采集的样本数 |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数 |
| `TransferedSamples` | `ulong` | 已传输的样本数 |
| `State` | `SamplingState` | 采样状态 |
| `Trigger` | `AITrigger` | 触发配置 |
| `Sync` | `AISync` | 同步配置 |
| `SignalExport` | `AISignalExport` | 信号导出配置 |
| `DigitalFiltering` | `AIDigitalFiltering` | 数字滤波配置 |
| `Record` | `AIRecord` | 录制配置对象 |
| `Device` | `JY9516TDevice` | 设备实例 |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int chID, AIRange range, AITerminal terminal, Coupling coupling, bool iepeEnable)` | 添加单个通道（使用枚举量程） |
| `AddChannel(int chID, double rangeLow, double rangeHigh, AITerminal terminal, Coupling coupling, bool iepeEnable)` | 添加单个通道（使用量程数值） |
| `AddChannel(int[] chIDs, AIRange range, AITerminal terminal, Coupling coupling, bool iepeEnable)` | 批量添加通道（统一枚举参数） |
| `AddChannel(int[] chIDs, double rangeLow, double rangeHigh, AITerminal terminal, Coupling coupling, bool iepeEnable)` | 批量添加通道（统一数值参数） |
| `AddChannel(int[] chIDs, double[] rangeLow, double[] rangeHigh, AITerminal[] terminal, Coupling[] coupling, bool[] iepeEnable)` | 批量添加通道（各自独立参数数组） |
| `AddChannel(int[] chIDs, AIRange[] range, AITerminal[] terminal, Coupling[] coupling, bool[] iepeEnable)` | 批量添加通道（各自独立枚举数组） |
| `RemoveChannel(int chID)` | 移除指定通道（chID=-1 移除所有） |
| `RemoveChannel(int[] chIDs)` | 批量移除通道 |
| `Commit()` | 提交任务配置（用于多卡同步） |
| `Start()` | 启动采集任务 |
| `Stop()` | 停止采集任务 |
| `StartSync()` | 重新启动多卡同步 |
| `ReadData(ref double[] buffer, int timeout)` | 读取单通道数据（一维数组，interleaved格式） |
| `ReadData(ref double[] buffer, int samplesPerChannel, int timeout)` | 读取单通道数据（指定样本数） |
| `ReadData(ref double[,] buffer, int timeout)` | 读取多通道数据（二维数组，按列格式） |
| `ReadData(ref double[,] buffer, int samplesPerChannel, int timeout)` | 读取多通道数据（指定每通道样本数） |
| `ReadRawData(ref int[] buffer, int timeout)` | 读取原始数据（一维数组） |
| `ReadRawData(ref int[] buffer, int samplesPerChannel, int timeout)` | 读取原始数据（指定样本数） |
| `ReadRawData(ref int[,] buffer, int samplesPerChannel, int timeout)` | 读取原始数据（二维数组，按列格式） |
| `GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout)` | 预览录制数据（二维数组，按列格式） |
| `GetRecordPreviewData(ref double[,] buf, int timeout)` | 预览录制数据（自动计算样本数） |
| `GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout)` | 预览录制数据（一维数组，interleaved格式） |
| `GetRecordPreviewData(ref double[] buf, int timeout)` | 预览录制数据（一维数组，自动计算样本数） |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态 |
| `WaitUntilDone(int timeout)` | 阻塞等待采集完成（毫秒，-1 表示无限等待） |
| `SendSoftwareTrigger()` | 发送软件触发（仅 `AITriggerType.Software` 有效） |
| `GetConversionStatus()` | 获取各 ADC 通道的转换状态（CRC 错误/滤波器饱和等） |
| `GetSamplingInfo()` | 获取当前采样信息（SampleRate/LPFMode/fMod/GroupDelay） |
| `GetSamplingInfo(double sampleRate, LPFMode lpfMode)` | 指定采样率与 LPF 模式查询采样信息 |
| `DetectIEPEConnection()` | 检测各通道 IEPE 传感器连接状态（Normal/OpenCircuit/ShortCircuit） |
| `IdentifyTEDSChip(int channel)` | 识别指定通道上的 TEDS 芯片型号 |
| `ReadTEDSData(int channel)` | 读取指定通道的 TEDS 数据（返回 byte[]） |
| `WriteTEDSData(int channel, byte[] data)` | 向指定通道 TEDS 芯片写入数据 |

---

## 设备硬件规格

| 参数 | 值 |
|------|-----|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 16 通道同步测量 |
| AI 采样率 | 62.5 S/s ~ 256 kS/s |
| AI 输入量程 | ±0.3125V / ±0.625V / ±1.25V / ±2.5V / ±5V / ±10V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分 / 伪差分 |
| AI IEPE 激励 | 每通道 4mA，软件使能 |
| AI 动态范围 | 111 dB |
| 设备温度监测 | 支持 |
| 接口 | USB / PCIe / PXIe |

### 产品型号

| 型号 | 描述 |
|------|------|
| USB-9516T | 16-CH 24-Bit 256 kS/s USB 温度监测型动态信号采集模块 |
| PCIe-9516T | 16-CH 24-Bit 256 kS/s PCIe 温度监测型动态信号采集模块 |
| PXIe-9516T | 16-CH 24-Bit 256 kS/s PXIe 温度监测型动态信号采集模块 |

---

## 枚举类型

### AIMode

AI 采集模式枚举。

| 值 | 说明 |
|----|------|
| `Single` | 单点采集模式 |
| `Continuous` | 连续采集模式 |
| `Finite` | 有限采集模式 |
| `Record` | 录制模式（数据流式写入文件） |

### AIRange

AI 输入量程枚举。

| 值 | 说明 |
|----|------|
| `_10V` | ±10V |
| `_5V` | ±5V |
| `_2p5V` | ±2.5V |
| `_1p25V` | ±1.25V |
| `_0p625V` | ±0.625V |
| `_0p3125V` | ±0.3125V |

### Coupling

输入耦合方式枚举。

| 值 | 说明 |
|----|------|
| `DC` | DC 耦合 |
| `AC` | AC 耦合 |

### AITerminal

AI 输入端子配置枚举。

| 值 | 说明 |
|----|------|
| `Differential` | 差分输入 |
| `PseudoDifferential` | 伪差分输入 |

### AITriggerType

AI 触发类型枚举。

| 值 | 说明 |
|----|------|
| `Immediate` | 立即触发（无触发） |
| `Digital` | 数字触发 |
| `Analog` | 模拟触发 |
| `Software` | 软件触发 |

### AITriggerMode

AI 触发模式枚举。

| 值 | 说明 |
|----|------|
| `Start` | 启动触发 |
| `Reference` | 参考触发 |

### AIDigitalTriggerSource

数字触发源枚举。

| 值 | 说明 |
|----|------|
| `Ext_Trig` | 外部触发 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

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
| `Channel_0` ~ `Channel_15` | 模拟输入通道 0~15 作为触发源 |

### AIAnalogTriggerComparator

模拟触发比较器类型枚举。

| 值 | 说明 |
|----|------|
| `Edge` | 边沿比较器 |
| `Hysteresis` | 迟滞比较器 |
| `Window` | 窗比较器 |

### AIAnalogWindowCondition

模拟窗比较器触发条件。

| 值 | 说明 |
|----|------|
| `Entering` | 信号进入窗口时触发 |
| `Leaving` | 信号离开窗口时触发 |

### AIAnalogTriggerEdge

模拟触发边沿枚举。

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿触发 |
| `Falling` | 下降沿触发 |

### SyncTopology

同步拓扑枚举。

| 值 | 说明 |
|----|------|
| `Independent` | 独立模式（默认） |
| `Master` | 主卡 |
| `Slave` | 从卡 |

### SyncState

同步状态枚举。

| 值 | 说明 |
|----|------|
| `WaitingSync` | 从卡等待同步信号 |
| `Synchronized` | 从卡已收到同步信号 |
| `Invalid` | 未启用同步或是主卡时（无效状态） |

### SyncTerminal

同步终端枚举。

| 值 | 说明 |
|----|------|
| `Ext_Trig` | 外部触发 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

### AISignalExportSource

信号导出源枚举。

| 值 | 说明 |
|----|------|
| `SampleClock` | AI 采样时钟 |
| `Sync` | 主卡同步信号（用于多卡同步） |
| `ReadyForStartTrigger` | 准备就绪信号 |
| `StartTrigger` | 触发输出信号 |
| `EndOfRecord` | 录制结束信号 |
| `EndOfAcquisition` | 采集结束信号 |

### AISignalExport 方法

| 方法 | 说明 |
|------|------|
| `Add(AISignalExportSource source, SignalExportDestination destination)` | 导出单个信号到指定目标 |
| `Add(AISignalExportSource source, List<SignalExportDestination> destinations)` | 导出单个信号到多个目标 |
| `Clear(SignalExportDestination destination)` | 清除指定目标的信号导出 |
| `ClearAll()` | 清除所有信号导出配置 |

### AIDigitalFiltering 配置

数字滤波器配置对象，包含低通滤波器（LPF）和高通滤波器（HPF）：

```csharp
// 配置低通滤波器模式
aiTask.DigitalFiltering.LPF.Mode = LPFMode.Normal;  // Normal / WideBandwidth / LowLatency

// 配置高通滤波器
aiTask.DigitalFiltering.HPF.CutoffFrequency = 10.0;  // 截止频率（Hz），最大值为采样率的 1%
aiTask.DigitalFiltering.HPF.Order = HPFOrder.OneOrder;  // 一阶或二阶
aiTask.DigitalFiltering.HPF.EnableChannel(0);  // 启用通道 0 的高通滤波
```

#### LPFMode（低通滤波器模式）

| 值 | 说明 | 带宽 | 延迟 | 采样率范围 |
|----|------|------|------|------------|
| `Normal` | 正常模式 | 0.4×fs | 中等 | 62.5 S/s ~ 256 kS/s |
| `WideBandwidth` | 宽带宽模式 | 0.4535×fs | 高 | 62.5 S/s ~ 128 kS/s |
| `LowLatency` | 低延迟模式 | 0.1×fs | 低 | 7.8125 S/s ~ 4 kS/s |

#### HPFOrder（高通滤波器阶数）

| 值 | 说明 |
|----|------|
| `OneOrder` | 一阶（-3dB 衰减） |
| `TwoOrder` | 二阶（-6dB 衰减） |

### SignalExportDestination

信号导出目标枚举。

| 值 | 说明 |
|----|------|
| `Ext_Trig` | 外部触发端口 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

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

### RecordMode

录制模式枚举。

| 值 | 说明 |
|----|------|
| `Finite` | 有限录制，录制指定时长后自动停止 |
| `Infinite` | 无限录制，需手动调用 Stop 停止 |

### SamplingState

采样状态枚举。

| 值 | 说明 |
|----|------|
| `Idle` | 空闲状态 |
| `PreSampling` | 预采样阶段 |
| `WaitingForTrigger` | 等待触发 |
| `PostSampling` | 触发后采样中 |
| `Done` | 采集完成 |

### ChoppingFrequency

ADC 斩波频率枚举。

| 值 | 说明 |
|----|------|
| `fMod32` | fMod/32 |
| `fMod8` | fMod/8 |

---

## 设备类

### JY9516TDevice

设备信息类。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Info` | `JY9516TDeviceInfo` | 设备信息 |
| `Status` | `DeviceStatus` | 设备状态（含温度） |
| `Capability` | `DeviceCapability` | 设备能力信息 |
| `ReferenceClock` | `ReferenceClock` | 参考时钟配置 |

#### 静态方法

| 方法 | 说明 |
|------|------|
| `Scan()` | 扫描系统所有设备 |
| `GetCapability(int slotNumber)` | 获取指定槽位设备能力 |
| `GetCapability(string deviceName)` | 获取指定名称设备能力 |
| `GetInstance(int slotNum)` | 获取指定槽位设备实例 |
| `GetInstance(string cardName)` | 获取指定名称设备实例 |

### DeviceStatus

设备状态类。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Temperature` | `double` | 设备温度（℃） |

### DeviceCapability

设备能力类。

| 属性 | 类型 | 说明 |
|------|------|------|
| `AI` | `AICapability` | 模拟输入能力 |

### AICapability

AI 能力类。

| 属性 | 类型 | 说明 |
|------|------|------|
| `NumberOfChannels` | `int` | 通道总数 |
| `MinSampleRate` | `double` | 最小采样率 |
| `MaxSampleRate` | `double` | 最大采样率 |

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
| `ReTriggerCount` | `int` | 重触发次数 |
| `PreTriggerSamples` | `uint` | 预触发点数 |

**说明**：
- `Mode.Start` - 触发后开始采集
- `Mode.Reference` - 参考触发模式，用于特定同步场景

### AIDigitalTrigger

数字触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIDigitalTriggerSource` | 触发源 |
| `Edge` | `AIDigitalTriggerEdge` | 触发边沿 |

### AIAnalogTrigger

模拟触发配置。通过 `Comparator` 选择比较器类型后，再配置对应子对象（`Edge` / `Hysteresis` / `Window`）。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIAnalogTriggerSource` | 触发源（Channel_0~Channel_15） |
| `Comparator` | `AIAnalogTriggerComparator` | 比较器类型（Edge/Hysteresis/Window） |
| `Edge` | `AIAnalogEdgeComparator` | 边沿比较器：`Slope`（边沿方向）/`Threshold`（阈值） |
| `Hysteresis` | `AIAnalogHysteresisComparator` | 迟滞比较器：`Slope`/`HighThreshold`/`LowThreshold` |
| `Window` | `AIAnalogWindowComparator` | 窗比较器：`Condition`/`HighThreshold`/`LowThreshold` |

### AISync

同步配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Topology` | `SyncTopology` | 同步拓扑（Independent/Master/Slave），默认 Independent |
| `Terminal` | `SyncTerminal` | 同步终端 |
| `State` | `SyncState` | 同步状态（仅从卡有效） |

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
| `Frequency` | `double` | 外部时钟频率 |

---

### AIRecord

录制配置对象。

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.Mode = RecordMode.Finite;  // Finite / Infinite
aiTask.Record.Length = 10.0;              // 录制时长（秒），Finite 有效
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |

---

# 完整代码示例

# JY9516T 代码示例

## 1. AI 连续采集（多通道）

```csharp
using System;
using System.Windows.Forms;
using JY9516T;
using SeeSharpTools.JY.ArrayUtility;

namespace JY9516TExample
{
    public partial class MainForm : Form
    {
        private JY9516TAITask aiTask;
        private double[,] dataToRead;
        private double[,] dataToPlot;

        private void btn_Start_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建 Task
                aiTask = new JY9516TAITask(0);  // 板卡 0
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 256000;    // 256 kS/s

                // 添加通道（启用所有 16 个通道，IEPE 关闭）
                for (int i = 0; i < 16; i++)
                {
                    aiTask.AddChannel(i, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);
                }

                // 启动采集
                aiTask.Start();

                // 初始化数据缓冲区
                int samplesToUpdate = 10000;
                dataToRead = new double[samplesToUpdate, aiTask.Channels.Count];
                dataToPlot = new double[aiTask.Channels.Count, samplesToUpdate];

                timer_CheckStatus.Enabled = true;
            }
            catch (JYDriverException ex)
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
        aiTask = new JY9516TAITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 100000;  // 采集 100k 个点

        // 添加单通道，启用 IEPE 激励
        aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.AC, true);

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
    catch (JYDriverException ex)
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
        aiTask = new JY9516TAITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;

        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);

        // 配置数字触发
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
        aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

        aiTask.Start();
        
        // 等待触发并采集完成...
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 3.5 AI 数字触发采集（重触发 + 预触发）

当需要多次触发采集或需要触发前的数据时，可以配置重触发和预触发：

```csharp
private JY9516TAITask aiTask;
private double[,] dataToRead;
private double[,] dataToPlot;

// 记录已完成的重触发次数
private int retriggerCompletedCount;

private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        retriggerCompletedCount = 0;
        
        aiTask = new JY9516TAITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;  // 每次触发采集 50k 个点

        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);

        // 配置数字触发
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
        aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
        
        // 配置触发模式为 Reference（参考触发），启用预触发
        aiTask.Trigger.Mode = AITriggerMode.Reference;
        aiTask.Trigger.PreTriggerSamples = 10000;  // 预触发点数：保留触发前 10k 个点
        
        // 配置重触发次数（-1 表示无限重触发）
        aiTask.Trigger.ReTriggerCount = 3;  // 触发 3 次

        aiTask.Start();

        // 初始化数据缓冲区
        dataToRead = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
        dataToPlot = new double[aiTask.Channels.Count, aiTask.SamplesToAcquire];
        
        timer_CheckStatus.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_CheckStatus_Tick(object sender, EventArgs e)
{
    try
    {
        // 检查缓冲区数据是否足够
        if (aiTask.AvailableSamples >= (ulong)dataToRead.GetLength(0))
        {
            // 读取数据
            aiTask.ReadData(ref dataToRead, 2000);
            retriggerCompletedCount++;

            // 转置数据用于显示
            ArrayManipulation.Transpose(dataToRead, ref dataToPlot);
            
            // 显示数据...
            
            // 检查是否所有重触发都已完成
            if ((aiTask.Trigger.ReTriggerCount != -1) && 
                (retriggerCompletedCount >= aiTask.Trigger.ReTriggerCount))
            {
                // 所有触发完成，停止任务
                aiTask.Stop();
                timer_CheckStatus.Enabled = false;
                MessageBox.Show(string.Format("所有 {0} 次触发采集已完成", retriggerCompletedCount));
            }
        }
    }
    catch (JYDriverException ex)
    {
        aiTask.Stop();
        timer_CheckStatus.Enabled = false;
        MessageBox.Show(ex.Message);
    }
}
```

**说明**：
- `Trigger.Mode = AITriggerMode.Reference` - 参考触发模式，允许配置预触发点数
- `Trigger.PreTriggerSamples` - 预触发点数，保留触发前的数据
- `Trigger.ReTriggerCount` - 重触发次数，-1 表示无限次，0 或 1 表示不重触发

## 4. AI 模拟触发采集

```csharp
private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516TAITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;

        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);

        // 配置模拟触发（边沿比较器）
        aiTask.Trigger.Type = AITriggerType.Analog;
        aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
        aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
        aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
        aiTask.Trigger.Analog.Edge.Threshold = 1.0;  // 触发电平 1.0V

        aiTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 5. 多卡同步采集

```csharp
private JY9516TAITask masterTask;
private JY9516TAITask slaveTask;

private void button_Commit_Click(object sender, EventArgs e)
{
    try
    {
        // 创建任务
        slaveTask = new JY9516TAITask(1);  // 从卡槽位 1
        masterTask = new JY9516TAITask(0); // 主卡槽位 0

        // 添加通道
        slaveTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);
        masterTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);

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

        // 信号导出配置（主卡导出同步信号到从卡）
        masterTask.SignalExport.Add(AISignalExportSource.Sync, SignalExportDestination.PXI_Trig0);
        slaveTask.SignalExport.Add(AISignalExportSource.Sync, SignalExportDestination.PXI_Trig0);

        // 提交配置（从卡先提交）
        slaveTask.Commit();
        masterTask.Commit();
    }
    catch (JYDriverException ex)
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
    catch (JYDriverException ex)
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
        aiTask = new JY9516TAITask(0);
        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 256000;

        // 添加通道，启用 IEPE 激励（4mA）
        // 适用于加速度计、麦克风等 IEPE 传感器
        aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.AC, true);

        aiTask.Start();
        // ...
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 7. 读取设备温度

```csharp
private void MainForm_Load(object sender, EventArgs e)
{
    // 获取设备信息
    int slotNumber = 0;
    int totalChannels = JY9516TDevice.GetCapability(slotNumber).AI.NumberOfChannels;
    double minRate = JY9516TDevice.GetCapability(slotNumber).AI.MinSampleRate;
    double maxRate = JY9516TDevice.GetCapability(slotNumber).AI.MaxSampleRate;

    // 创建设备实例并读取温度
    JY9516TDevice device = JY9516TDevice.GetInstance(slotNumber);
    double temperature = device.Status.Temperature;  // 设备温度（℃）
    
    label_Temperature.Text = string.Format("设备温度: {0:F2} ℃", temperature);

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
private JY9516TAITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516TAITask(0);
        
        // 添加通道
        aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);
        
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
private JY9516TAITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9516TAITask(0);
        
        // 添加多通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, false);
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

## 11. TEDS 传感器读写

```csharp
// 先创建 AITask 并添加目标通道，TEDS 操作无需启动采集
aiTask = new JY9516TAITask(0);
aiTask.AddChannel(0, AIRange._10V, AITerminal.PseudoDifferential, Coupling.DC, true);

// 1) 识别通道 0 上的 TEDS 芯片
var chipModel = aiTask.IdentifyTEDSChip(0);
Console.WriteLine($"TEDS Chip: {chipModel}");

// 2) 读取 TEDS 数据
byte[] tedsData = aiTask.ReadTEDSData(0);

// 3) 写入 TEDS 数据（数据需包含校验和）
byte[] newData = new byte[64];
// ... 填充 TEDS 标准定义的模板内容 ...
aiTask.WriteTEDSData(0, newData);
```

## 12. IEPE 连接检测与诊断

```csharp
aiTask = new JY9516TAITask(0);
for (int i = 0; i < 4; i++)
{
    aiTask.AddChannel(i, AIRange._10V, AITerminal.PseudoDifferential, Coupling.AC, true);
}

// 检测各通道 IEPE 连接状态
var statuses = aiTask.DetectIEPEConnection();
for (int i = 0; i < statuses.Length; i++)
{
    Console.WriteLine($"Ch{i}: {statuses[i]}");   // Normal / OpenCircuit / ShortCircuit
}

// 查询当前采样信息
aiTask.SampleRate = 51200;
var info = aiTask.GetSamplingInfo();
Console.WriteLine($"实际采样率 = {info.SampleRate}, LPF 模式 = {info.LPFMode}, GroupDelay = {info.GroupDelay}");
```

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
