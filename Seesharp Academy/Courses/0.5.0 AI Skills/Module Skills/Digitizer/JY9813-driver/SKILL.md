---
name: jy9813-driver
description: 提供 JYTEK JY9813 系列（PXIe/PCIe-9813）高速数字化仪（Digitizer）的完整 C# 驱动开发指引。涵盖模拟输入（AI）有限/连续/录制采集、多通道同步采集、触发配置（数字/模拟/软件触发、预触发采集）、参考时钟同步、多卡同步（Master/Slave拓扑）、AC/DC耦合、阻抗匹配（50Ω/1MΩ）、录制模式（Finite/Infinite Streaming）。当用户使用 JY9813、PXIe-9813、PCIe-9813、JY9813AITask、AIMode.Finite、AIMode.Continuous、AIMode.Record、SyncTopology.Master、SyncTopology.Slave 开发高速数据采集、数字化仪、波形记录应用时自动应用。
---

# JY9813 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9813\Bin\JY9813.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9813\Bin\JY9813.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9813;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AI 分辨率 | 14-bit |
| AI 通道数 | 4 通道同步采样 |
| AI 最大采样率 | 20 MS/s |
| AI 输入量程 | ±0.5V / ±1V / ±5V / ±10V |
| AI 输入阻抗 | 50Ω / 1MΩ 软件可选 |
| AI 模拟带宽 | 高达 7 MHz |
| AI DC 精度 | 0.3% |
| 接口 | PXIe / PCIe |

## 通用编程范式

所有 JY9813 采集任务遵循以下标准流程：

```
创建 Task → 添加通道 → 配置参数 → 启动 → 读取数据 → 停止 → 清除通道
```

### 标准代码框架

```csharp
// 1. 创建 Task
JY9813AITask aiTask = new JY9813AITask(boardNumber);

// 2. 添加通道
aiTask.AddChannel(channelID, lowRange, highRange, AICoupling.DC, AIImpedance.ImpedanceHigh);

// 3. 配置采集模式
aiTask.Mode = AIMode.Continuous;  // 或 Finite / Record
aiTask.SampleRate = 1000000;      // 采样率

// 4. 启动采集
aiTask.Start();

// 5. 读取数据
// 单通道：一维数组
double[] singleBuffer = new double[samplesToRead];
aiTask.ReadData(ref singleBuffer, samplesToRead, timeout);

// 多通道：二维数组 [每通道点数, 通道数]，每列一个通道
double[,] multiBuffer = new double[samplesToRead, aiTask.Channels.Count];
aiTask.ReadData(ref multiBuffer, timeout);

// 6. 停止并清理
aiTask.Stop();
aiTask.Channels.Clear();
```

## 关键 API 速查

### 通道配置

| API | 说明 |
|-----|------|
| `aiTask.AddChannel(channel, low, high, coupling, impedance)` | 添加单个通道 |
| `aiTask.AddChannel(channels[], low, high, coupling, impedance)` | 批量添加通道（统一参数） |
| `aiTask.AddChannel(channels[], low[], high[], coupling[], impedance[])` | 批量添加通道（各自独立参数） |
| `aiTask.Channels.Clear()` | 清除已添加的通道 |

### 采集模式

| 模式 | 说明 |
|------|------|
| `AIMode.Continuous` | 连续采集模式 |
| `AIMode.Finite` | 有限采集模式 |
| `AIMode.Record` | 记录模式（流式采集到板载内存） |

### 触发配置

```csharp
// 数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 模拟触发
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 0.5;  // 触发电平 0.5V

// 软触发
aiTask.Trigger.Type = AITriggerType.Soft;

// 触发模式
aiTask.Trigger.Mode = AITriggerMode.Start;      // 开始触发
aiTask.Trigger.Mode = AITriggerMode.Reference;  // 参考触发（支持预触发）
```

### 参考时钟同步（多卡同步）

```csharp
// 主卡配置
masterTask.Sync.Topology = SyncTopology.Master;
masterTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
masterTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
masterTask.Device.ReferenceClock.External.Frequency = 100e6;
masterTask.Device.ReferenceClock.Commit();

// 从卡配置
slaveTask.Sync.Topology = SyncTopology.Slave;
slaveTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
slaveTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slaveTask.Device.ReferenceClock.Commit();

// 提交同步配置
slaveTask.Sync.Commit();
masterTask.Sync.Commit();

// 先启动从卡，再启动主卡
slaveTask.Start();
masterTask.Start();
```

## 关键枚举

### AICoupling（耦合方式）

- `AICoupling.DC` - DC 耦合
- `AICoupling.AC` - AC 耦合

### AIImpedance（输入阻抗）

- `AIImpedance.Impedance50Ohm` - 50Ω
- `AIImpedance.ImpedanceHigh` - 1MΩ（高阻抗）

### AITriggerType（触发类型）

- `AITriggerType.Immediate` - 立即触发
- `AITriggerType.Digital` - 数字触发
- `AITriggerType.Analog` - 模拟触发
- `AITriggerType.Soft` - 软触发

### AIDigitalTriggerSource（数字触发源）

- `AIDigitalTriggerSource.PXI_Trig0` ~ `PXI_Trig7`
- `AIDigitalTriggerSource.PFI_0`
- `AIDigitalTriggerSource.PXIe_DStarB`
- `AIDigitalTriggerSource.PXIe_DStarC`

### SyncTopology（同步拓扑）

- `SyncTopology.Master` - 主卡
- `SyncTopology.Slave` - 从卡

---

## 录制模式（Record）

JY9813 支持将采集数据流式写入板载内存的 Record 模式，适用于长时间高速采集：

```csharp
aiTask = new JY9813AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 10000000;  // 10 MS/s

// 配置录制参数（具体属性请参考驱动版本）
aiTask.Start();

// 录制过程中可读取数据
aiTask.ReadData(ref buffer, samplesToRead, timeout);

aiTask.Stop();
aiTask.Channels.Clear();
```

### Record 模式特点

- 数据直接写入板载内存，支持高速采集
- 适用于需要长时间连续采集的场景
- 可与触发配置配合使用

---

## 异常处理

所有操作可能抛出 `JYDriverException`，应使用 try-catch 处理：

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
- 驱动版本：V1.0.0 或更高
- 依赖包：SeeSharpTools.JY.GUI 1.4.7


---

# 完整 API 参考

# JY9813 API 参考手册

## 命名空间

```csharp
using JY9813;
```

## 核心类

### JY9813AITask

高速数字化仪模拟输入任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9813AITask(int boardNumber)` | 创建指定板卡的 AI 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `AIChannelCollection` | 通道集合 |
| `Mode` | `AIMode` | 采集模式（Continuous/Finite/Record） |
| `SampleRate` | `double` | 采样率（Hz） |
| `SamplesToAcquire` | `ulong` | 有限模式下要采集的样本数 |
| `BufLenInSamples` | `ulong` | 缓冲区可存储的每通道样本数 |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数 |
| `TransferedSamples` | `ulong` | 已从本地缓冲区传输的样本数（每通道） |
| `Trigger` | `AITrigger` | 触发配置 |
| `Sync` | `AISync` | 同步配置 |
| `Device` | `AIDevice` | 设备配置（参考时钟等） |
| `SampleClock` | `AISampleClock` | 采样时钟配置（内部/外部时钟源） |
| `SignalExport` | `AISignalExport` | 信号导出配置 |
| `DisableTriggerDelay` | `bool` | 禁用触发延迟（用于触发对齐，低精度同步时从卡需禁用） |
| `DisableMultiCardTriggerSync` | `bool` | 禁用多卡同步时的触发同步（只同步时钟，不同步触发） |
| `DisableCalibration` | `bool` | 禁用校准 |
| `EventQueue` | `EventQueue` | 事件队列（用于 WaitUntilDone/ReadBuffer 事件通知） |
| `Record` | `AIRecord` | 录制配置对象 |
| `Osp` | `AIOSP` | 板载信号处理配置（可选） |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int channel, double low, double high, AICoupling coupling, AIImpedance impedance)` | 添加单个通道 |
| `AddChannel(int[] channels, double low, double high, AICoupling coupling, AIImpedance impedance)` | 批量添加通道（统一参数） |
| `AddChannel(int[] channels, double[] low, double[] high, AICoupling[] coupling, AIImpedance[] impedance)` | 批量添加通道（各自独立参数） |
| `RemoveChannel(int channel)` | 移除指定通道 |
| `RemoveChannel(int[] channels)` | 批量移除通道 |
| `Start()` | 启动采集任务 |
| `Stop()` | 停止采集任务 |
| `WaitUntilDone(int timeout)` | 等待任务完成（timeout=-1 无限等待，其他值为毫秒数） |
| `ReadData(IntPtr buffer, int samplesPerChannel, int timeout)` | 读取数据到 IntPtr 缓冲区 |
| `ReadData(ref double[] buffer, int samplesToRead, int timeout)` | 读取单通道数据（指定样本数） |
| `ReadData(ref double[] buffer, int timeout)` | 读取单通道数据（自动计算样本数） |
| `ReadData(ref double[,] buffer, int samplesToRead, int timeout)` | 读取多通道数据（指定每通道样本数） |
| `ReadData(ref double[,] buffer, int timeout)` | 读取多通道数据（自动计算样本数） |
| `ReadRawData(ref short[] buffer, int samplesToRead, int timeout)` | 读取单通道原始数据 |
| `ReadRawData(ref short[] buffer, int timeout)` | 读取单通道原始数据（自动计算样本数） |
| `ReadRawData(ref short[,] buffer, int samplesToRead, int timeout)` | 读取多通道原始数据 |
| `ReadRawData(ref short[,] buffer, int timeout)` | 读取多通道原始数据（自动计算样本数） |
| `ReadRawData(IntPtr buffer, int samplesPerChannel, int timeout)` | 读取原始数据到 IntPtr 缓冲区 |
| `GetRecordPreviewData(ref double[,] buf, int samples, int timeout)` | 预览录制数据（多通道） |
| `GetRecordPreviewData(ref double[] buf, int samples, int timeout)` | 预览录制数据（单通道） |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态（秒） |
| `GetRecordStatus(out uint recordedSamples, out bool recordDone)` | 获取录制状态（样本数） |

---

## 设备硬件规格

| 参数 | 值 |
|------|-----|
| AI 分辨率 | 14-bit |
| AI 通道数 | 4 通道同步采样 |
| AI 最大采样率 | 20 MS/s |
| AI 输入量程 | ±0.5V / ±1V / ±5V / ±10V |
| AI 输入阻抗 | 50Ω / 1MΩ 软件可选 |
| AI 模拟带宽 | 高达 7 MHz |
| AI DC 精度 | 0.3% |
| 接口 | PXIe / PCIe |

### 产品型号

| 型号 | 描述 |
|------|------|
| PXIe-9813 | 4-CH 14-bit 20 MS/s PXIe Express 数字化仪 |
| PCIe-9813 | 4-CH 14-bit 20 MS/s PCIe Express 数字化仪 |

---

## 枚举类型

### AIMode

采集模式枚举。

| 值 | 说明 |
|----|------|
| `Continuous` | 连续采集模式 |
| `Finite` | 有限采集模式 |
| `Record` | 记录模式（流式采集到板载内存） |

### AICoupling

输入耦合方式枚举。

| 值 | 说明 |
|----|------|
| `DC` | DC 耦合 |
| `AC` | AC 耦合 |

### AIImpedance

输入阻抗枚举。

| 值 | 说明 |
|----|------|
| `Impedance50Ohm` | 50Ω 输入阻抗 |
| `ImpedanceHigh` | 1MΩ 高输入阻抗 |

### AITriggerType

触发类型枚举。

| 值 | 说明 |
|----|------|
| `Immediate` | 立即触发（无触发） |
| `Digital` | 数字触发 |
| `Analog` | 模拟触发 |
| `Soft` | 软触发 |
| `MultichannelAnalog` | 多通道模拟触发 |

### AITriggerMode

触发模式枚举。

| 值 | 说明 |
|----|------|
| `Start` | 开始触发 |
| `Reference` | 参考触发（支持预触发采集） |

### AIDigitalTriggerSource

数字触发源枚举。

| 值 | 说明 |
|----|------|
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |
| `PFI_0` | 前面板 PFI_0 |

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
| `Channel_0` ~ `Channel_3` | 模拟输入通道 0~3 |

### AIAnalogTriggerEdge

模拟触发边沿枚举。

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿触发 |
| `Falling` | 下降沿触发 |

### AIAnalogTriggerComparator

模拟触发比较器类型枚举。

| 值 | 说明 |
|----|------|
| `Edge` | 边沿比较器 |
| `Hysteresis` | 迟滞比较器 |
| `Window` | 窗口比较器 |

### AIAnalogWindowCondition

模拟窗口触发条件枚举。

| 值 | 说明 |
|----|------|
| `Entering` | 进入窗口触发 |
| `Leaving` | 离开窗口触发 |

### SyncTopology

同步拓扑枚举。

| 值 | 说明 |
|----|------|
| `Independent` | 独立模式（不支持同步） |
| `Master` | 主卡 |
| `Slave` | 从卡 |

### SyncTriggerRouting

同步触发路由枚举。

| 值 | 说明 |
|----|------|
| `NONE` | 无路由 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |
| `PFI_0` | 前面板 PFI_0 |

### SyncPulseRouting

同步脉冲路由枚举。

| 值 | 说明 |
|----|------|
| `NONE` | 无路由 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |
| `PFI_0` | 前面板 PFI_0 |

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
| `CLK_IN` | 前面板 CLK_IN（10MHz） |
| `PXIe_Clk100` | PXIe 100MHz 背板时钟 |

### AISampleClockSource

采样时钟源枚举。

| 值 | 说明 |
|----|------|
| `Internal` | 内部采样时钟 |
| `External` | 外部采样时钟 |

### ClockTerminal

外部采样时钟终端枚举。

| 值 | 说明 |
|----|------|
| `CLK_IN` | 前面板 CLK_IN |

### AISignalExportSource

AI 信号导出源枚举。

| 值 | 说明 |
|----|------|
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7 |
| `StartTrig` | AI 起始触发信号 |
| `ReferenceTrig` | AI 参考触发信号 |
| `SyncTriggerOut` | PLL 同步触发输出 |
| `ChannelComparatorOut` | AI 模拟多通道比较器输出 |

### SignalExportDestination

信号导出目标终端枚举。

| 值 | 说明 |
|----|------|
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7 |
| `PFI_0` | 前面板 PFI_0 |

### SignalExportOutputType

信号导出输出类型枚举。

| 值 | 说明 |
|----|------|
| `Pulse` | 脉冲输出 |
| `Level` | 电平输出 |

---

## 异常类

### JYDriverException

驱动异常类，所有 API 操作失败时抛出。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Message` | `string` | 错误描述信息 |

---

## 配置对象

### AITrigger

触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AITriggerType` | 触发类型 |
| `Mode` | `AITriggerMode` | 触发模式 |
| `PreTriggerSamples` | `int` | 预触发样本数（Reference 模式） |
| `Digital` | `AIDigitalTrigger` | 数字触发配置 |
| `Analog` | `AIAnalogTrigger` | 模拟触发配置 |

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
| `Comparator` | `AIAnalogTriggerComparator` | 触发比较器类型（Edge/Hysteresis/Window） |
| `Edge` | `AIAnalogEdgeComparator` | 边沿比较器配置（当Comparator=Edge时有效） |
| `Hysteresis` | `AIAnalogHysteresisComparator` | 迟滞比较器配置（当Comparator=Hysteresis时有效） |
| `Window` | `AIAnalogWindowComparator` | 窗口比较器配置（当Comparator=Window时有效） |

### AIAnalogEdgeComparator

边沿比较器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Slope` | `AIAnalogTriggerEdge` | 触发边沿（Rising/Falling） |
| `Threshold` | `double` | 触发电平阈值 |

### AIAnalogHysteresisComparator

迟滞比较器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Slope` | `AIAnalogTriggerEdge` | 触发边沿 |
| `HighThreshold` | `double` | 高阈值 |
| `LowThreshold` | `double` | 低阈值 |

### AIAnalogWindowComparator

窗口比较器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Condition` | `AIAnalogWindowCondition` | 窗口条件（Entering/Leaving） |
| `HighThreshold` | `double` | 高阈值 |
| `LowThreshold` | `double` | 低阈值 |

### AISync

同步配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Topology` | `SyncTopology` | 同步拓扑（主/从） |
| `TriggerRouting` | `SyncTriggerRouting` | 触发信号路由 |
| `PulseRouting` | `SyncPulseRouting` | 脉冲信号路由 |

| 方法 | 说明 |
|------|------|
| `Commit()` | 提交同步配置 |

### AIDevice

设备配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ReferenceClock` | `AIReferenceClock` | 参考时钟配置 |

### AIReferenceClock

参考时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `ReferenceClockSource` | 时钟源 |
| `External` | `ExternalReferenceClock` | 外部时钟配置 |

| 方法 | 说明 |
|------|------|
| `Commit()` | 提交参考时钟配置 |

### ExternalReferenceClock

外部参考时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ExternalReferenceClockTerminal` | 时钟终端 |
| `Frequency` | `double` | 时钟频率（Hz） |

### AISampleClock

采样时钟配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AISampleClockSource` | 采样时钟源（Internal/External） |
| `Internal` | `AIInternalSampleClock` | 内部时钟配置 |
| `External` | `AIExternalSampleClock` | 外部时钟配置 |

### AIInternalSampleClock

内部采样时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Rate` | `double` | 采样时钟频率 |
| `RateAuto` | `bool` | 是否自动选择采样时钟频率 |

### AIExternalSampleClock

外部采样时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ClockTerminal` | 外部时钟输入终端 |
| `ExpectedRate` | `double` | 期望的外部时钟频率 |

### AISignalExport

信号导出配置对象。

```csharp
// 将起始触发信号导出到 PXI 触发总线
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

// 清除导出
aiTask.SignalExport.Clear(SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.ClearAll();
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `Destination` | `List<SignalExportDestination>` | 已导出的信号目标列表 |

| 方法 | 说明 |
|------|------|
| `Add(source, destination)` | 导出一个信号到目标 |
| `Add(source, destinations)` | 导出一个信号到多个目标 |
| `Add(source, destination, outType, invert)` | 导出信号（带输出类型和反相） |
| `Clear(destination)` | 清除指定目标的信号导出 |
| `ClearAll()` | 清除所有信号导出 |

---

### AIRecord

录制配置对象。

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;  // Finite / Infinite
aiTask.Record.Length = 5.0;               // 录制时长（秒），Finite 有效
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |
| `Advanced` | `AIRecordAdvanced` | 高级录制配置 |

### AIRecordAdvanced

高级录制配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `PreviewBufferSize` | `uint` | 预览数据循环缓冲区大小（字节） |
| `SamplesToPreviewBuffer` | `uint` | 每次写入预览缓冲区的数据长度（样本数） |
| `BlockSize` | `uint` | 每次录制数据缓冲区大小（字节） |
| `BlockCount` | `uint` | 录制缓冲区数量 |

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
| `Bin` | 二进制格式（short 类型，需缩放转换） |


---

# 完整代码示例

# JY9813 代码示例

## 1. AI 连续采集（单通道）

```csharp
using System;
using System.Windows.Forms;
using JY9813;

namespace JY9813Example
{
    public partial class MainForm : Form
    {
        private JY9813AITask aiTask;
        private double[] readValue;
        private double lowRange = -10;
        private double highRange = 10;

        private void button_start_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建 Task
                aiTask = new JY9813AITask(0);  // 板卡 0

                // 添加通道
                aiTask.AddChannel(0, lowRange, highRange, 
                    AICoupling.DC, AIImpedance.ImpedanceHigh);

                // 配置采集参数
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 1000000;  // 1 MS/s

                // 启动采集
                aiTask.Start();

                readValue = new double[10000];
                timer_FetchData.Enabled = true;
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void timer_FetchData_Tick(object sender, EventArgs e)
        {
            timer_FetchData.Enabled = false;
            try
            {
                if (aiTask.AvailableSamples >= (ulong)readValue.Length)
                {
                    aiTask.ReadData(ref readValue, readValue.Length, -1);
                    // 处理数据...
                }
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
            }
            timer_FetchData.Enabled = true;
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
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
        }
    }
}
```

## 2. AI 有限采集（多通道）

```csharp
private JY9813AITask aiTask;
private double[,] readValue;      // 多通道数据：[每通道点数, 通道数]
private double[,] displayValue;   // 转置后的数据用于显示

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        // 创建 Task
        aiTask = new JY9813AITask(0);

        // 添加多个通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        }

        // 配置有限采集
        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;  // 每通道采集 10000 个点
        aiTask.SampleRate = 1000000;      // 1 MS/s

        // 启动采集
        aiTask.Start();

        // 创建缓冲区：[每通道点数, 通道数]
        readValue = new double[10000, aiTask.Channels.Count];
        displayValue = new double[aiTask.Channels.Count, 10000];  // 转置后用于绘图
        
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_Tick(object sender, EventArgs e)
{
    try
    {
        if (aiTask.AvailableSamples >= (ulong)aiTask.SamplesToAcquire)
        {
            // 读取多通道数据到二维数组
            aiTask.ReadData(ref readValue, -1);
            
            // 转置数据用于显示（图表控件需要 [通道数, 每通道点数] 格式）
            ArrayManipulation.Transpose(readValue, ref displayValue);
            easyChartX_readData.Plot(displayValue);
            
            // 或提取单个通道数据
            double[] ch0Data = new double[10000];
            for (int i = 0; i < 10000; i++)
            {
                ch0Data[i] = readValue[i, 0];  // 第0列 = 通道0
            }
            
            aiTask.Stop();
            aiTask.Channels.Clear();
            
            timer_FetchData.Enabled = false;
        }
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 3. AI 数字触发采集

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);

        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 1000000;

        // 配置数字触发
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
        aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 4. AI 模拟触发采集

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);

        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;
        aiTask.SampleRate = 1000000;

        // 配置模拟触发
        aiTask.Trigger.Type = AITriggerType.Analog;
        aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
        aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
        aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
        aiTask.Trigger.Analog.Edge.Threshold = 0.5;  // 触发电平 0.5V

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 5. AI 软触发采集

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);

        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;
        aiTask.SampleRate = 1000000;

        // 配置软触发
        aiTask.Trigger.Type = AITriggerType.Soft;

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 6. 参考触发（预触发采集）

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);

        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;
        aiTask.SampleRate = 1000000;

        // 配置参考触发（支持预触发）
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Mode = AITriggerMode.Reference;
        aiTask.Trigger.PreTriggerSamples = 1000;  // 触发前采集 1000 个点
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 7. 多卡参考时钟同步

```csharp
private JY9813AITask masterTask;
private JY9813AITask slaveTask;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        // 主卡配置
        masterTask = new JY9813AITask(2);  // 主卡槽位 2
        masterTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        masterTask.Mode = AIMode.Finite;
        masterTask.SamplesToAcquire = 10000;
        masterTask.SampleRate = 1000000;
        masterTask.Trigger.Type = AITriggerType.Immediate;

        // 同步配置 - 主卡
        masterTask.Sync.Topology = SyncTopology.Master;
        masterTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
        masterTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;
        masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
        masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
        masterTask.Device.ReferenceClock.External.Frequency = 100e6;
        masterTask.Device.ReferenceClock.Commit();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show("主卡配置失败: " + ex.Message);
        return;
    }

    try
    {
        // 从卡配置
        slaveTask = new JY9813AITask(4);  // 从卡槽位 4
        slaveTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        slaveTask.Mode = AIMode.Finite;
        slaveTask.SamplesToAcquire = 10000;
        slaveTask.SampleRate = 1000000;

        // 从卡使用数字触发
        slaveTask.Trigger.Type = AITriggerType.Digital;
        slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;

        // 同步配置 - 从卡
        slaveTask.Sync.Topology = SyncTopology.Slave;
        slaveTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
        slaveTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;
        slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
        slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
        slaveTask.Device.ReferenceClock.Commit();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show("从卡配置失败: " + ex.Message);
        return;
    }

    try
    {
        // 提交同步配置
        slaveTask.Sync.Commit();
        masterTask.Sync.Commit();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show("同步提交失败: " + ex.Message);
        return;
    }

    try
    {
        // 先启动从卡，再启动主卡
        slaveTask.Start();
        masterTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show("启动失败: " + ex.Message);
    }
}
```

## 8. AC 耦合采集（高频信号）

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        
        // 使用 AC 耦合采集高频信号
        aiTask.AddChannel(0, -10, 10, AICoupling.AC, AIImpedance.Impedance50Ohm);

        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 20000000;  // 20 MS/s

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 9. Record 有限录制模式（Finite Streaming）

JY9813 支持将数据流式录制到文件，录制完成后自动停止。定时器中通过`GetRecordStatus`检查录制状态：

```csharp
private JY9813AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        
        // 添加通道
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 10000000;  // 10 MS/s
        
        // 配置录制参数
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 5.0;               // 录制 5 秒
        aiTask.SamplesToAcquire = 100000;         // 预览缓冲区样本数
        
        aiTask.Start();
        
        previewData = new double[10000, aiTask.Channels.Count];
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
            if (aiTask != null) aiTask.Stop();
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
        if (aiTask.AvailableSamples >= 10000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
            // 显示预览数据...
        }
    }
    
    timer_Preview.Enabled = true;
}
```

---

## 10. Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止。定时器中只读取预览数据，不检查录制状态：

```csharp
private JY9813AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9813AITask(0);
        
        // 添加通道
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 10000000;  // 10 MS/s
        
        // 配置无限录制（不设置 Length）
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制模式
        
        aiTask.Start();
        
        previewData = new double[10000, aiTask.Channels.Count];
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
    if (aiTask.AvailableSamples >= 10000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
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

## 11. 录制数据回放

读取已录制的 bin 文件并回放显示（JY9813 数据格式为 short）：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private short[,] rawData;
private double[,] playbackData;
private ulong totalSamples;
private double scaleValue = 21.0;  // 根据量程设置

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    
    if (!File.Exists(fileBrowser.FileName))
    {
        MessageBox.Show("文件不存在");
        return;
    }
    
    // 计算总样本数（JY9813 数据格式为 short）
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 1;
    totalSamples = (ulong)(fileInfo.Length / sizeof(short) / channelCount);
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    rawData = new short[10000, channelCount];
    playbackData = new double[10000, channelCount];
    
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
                    playbackData[i, ch] = rawData[i, ch] * scaleValue / 32768.0;
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
