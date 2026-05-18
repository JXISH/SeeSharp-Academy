---
name: jy9511-driver
description: 提供 JYTEK JY9511 系列（USB/PCIe/PXIe-9511）动态信号分析仪（DSA）的完整 C# 驱动开发指引。涵盖模拟输入（AI）有限/连续/录制采集、模拟输出（AO）波形生成、数字输入/输出（DI/DO）、计数器输入（CI）、IEPE激励、TEDS兼容、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、多卡同步、录制模式（Finite/Infinite Streaming）。当用户使用 JY9511、USB-9511、PCIe-9511、PXIe-9511、JY9511AITask、JY9511AOTask、JY9511CITask、AIMode.Finite、AIMode.Continuous、AIMode.Record、IEPE、TEDS 开发振动分析、噪声测试、声学测量、模态分析应用时自动应用。
---

# JY9511 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DSA\JY9511\Bin\JY9511.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DSA\JY9511\Bin\JY9511.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9511;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 4 通道同步测量（差分输入） |
| AI 采样率 | 62.5 S/s ~ 256 kS/s |
| AI 输入量程 | ±0.3125V / ±0.625V / ±1.25V / ±2.5V / ±5V / ±10V / ±25V / ±50V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分（Differential）/ 伪差分（PseudoDifferential） |
| AI IEPE 激励 | 每通道 4mA IEPE 激励，软件使能 |
| AI TEDS 支持 | 兼容 TEDS 智能传感器 |
| AI 动态范围 | 111 dB |
| AO 分辨率 | 24-bit Δ-Σ DAC |
| AO 通道数 | 2 通道 |
| AO 更新率 | 最高 204.8 kS/s |
| AO 输出量程 | ±0.316V / ±1V / ±3.16V / ±10V |
| AO 动态范围 | 117 dB |
| 计数器 | 2 通道（边沿计数、频率/周期/脉冲测量） |
| 接口 | USB / PCIe / PXIe |

## 通用编程范式

### AI 采集任务流程

```
创建 AITask → 添加通道 → 配置参数 → 启动 → 读取数据 → 停止
```

### AO 输出任务流程

```
创建 AOTask → 添加通道 → 配置参数 → 写入数据 → 启动 → 停止
```

### 标准代码框架

```csharp
// ========== AI 采集 ==========
// 1. 创建 Task
JY9511AITask aiTask = new JY9511AITask(boardNumber);

// 2. 添加通道（支持 IEPE 激励）
aiTask.AddChannel(channelID, lowRange, highRange, Coupling.DC, AITerminal.Differential, iepeEnable);

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

// ========== AO 输出 ==========
// 1. 创建 Task
JY9511AOTask aoTask = new JY9511AOTask(boardNumber);

// 2. 添加通道
aoTask.AddChannel(channelID, lowRange, highRange, AOTerminal.Differential);

// 3. 配置输出模式
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 204800;

// 4. 写入波形数据
aoTask.WriteData(waveformData, -1);

// 5. 启动输出
aoTask.Start();

// 6. 停止
aoTask.Stop();
```

## 关键 API 速查

### AI 通道配置

| API | 说明 |
|-----|------|
| `aiTask.AddChannel(channel, low, high, coupling, terminal, iepeEnable)` | 添加模拟输入通道 |
| `aiTask.Channels.Count` | 已添加通道数 |
| `aiTask.Channels[i].ChannelID` | 获取通道 ID |

### AO 通道配置

| API | 说明 |
|-----|------|
| `aoTask.AddChannel(channel, low, high, terminal)` | 添加模拟输出通道 |
| `aoTask.WriteData(data, timeout)` | 写入输出数据 |

### 采集/输出模式

| 模式 | 说明 |
|------|------|
| `AIMode.Single` | AI 单点采集模式 |
| `AIMode.Finite` | AI 有限采集模式 |
| `AIMode.Continuous` | AI 连续采集模式 |
| `AIMode.Record` | AI 录制模式（流式写入文件） |
| `AOMode.ContinuousWrapping` | AO 连续循环输出模式 |
| `AOMode.ContinuousNoWrapping` | AO 连续不循环输出模式 |
| `AOMode.Finite` | AO 有限输出模式 |

### 触发配置

````csharp
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

````csharp
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

### AITerminal（AI 输入端子配置）

- `AITerminal.Differential` - 差分输入
- `AITerminal.PseudoDifferential` - 伪差分输入

### AOTerminal（AO 输出端子配置）

- `AOTerminal.Differential` - 差分输出
- `AOTerminal.PseudoDifferential` - 伪差分输出

### CIType（计数器测量类型）

- `CIType.EdgeCounting` - 边沿计数
- `CIType.Frequency` - 频率测量
- `CIType.Period` - 周期测量
- `CIType.Pulse` - 脉冲测量
- `CIType.SemiPeriod` - 半周期测量

### CountDirection（计数方向）

- `CountDirection.Up` - 向上计数
- `CountDirection.Down` - 向下计数
- `CountDirection.UpDown` - 双向计数

## 设备属性

````csharp
// 获取设备信息
int totalAIChannels = JY9511Device.TotalNumberOfAIChannels;  // AI 通道总数
double minSampleRate = JY9511Device.AIMinSampleRate;         // 最小采样率
double maxSampleRate = JY9511Device.AIMaxSampleRate;         // 最大采样率
```

---

## IEPE 传感器检测

JY9511 支持 IEPE 传感器连接状态检测：

```csharp
aiTask = new JY9511AITask(0);
aiTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, true);  // 启用 IEPE

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

## TEDS 智能传感器支持

JY9511 兼容 TEDS（IEEE 1451.4）智能传感器：

```csharp
aiTask = new JY9511AITask(0);
aiTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);

// 识别 TEDS 芯片
var tedsInfo = aiTask.IdentifyTEDSChip(0);

// 读取 TEDS 数据
var tedsData = aiTask.ReadTEDSData(0);
```

---

## 录制模式（Record）

将采集数据流式写入文件，支持预览：

```csharp
aiTask = new JY9511AITask(0);
aiTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);
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
- 驱动版本：JY9511 Installer_V1.0.0 或更高
- 依赖包：SeeSharpTools.JY.GUI 1.4.7


---

# 完整 API 参考

# JY9511 API 参考手册

## 命名空间

```csharp
using JY9511;
```

## 核心类

### JY9511AITask

动态信号分析仪模拟输入任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9511AITask(int boardNumber)` | 创建指定板卡的 AI 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `AIChannelCollection` | 通道集合 |
| `Mode` | `AIMode` | 采集模式（Single/Continuous/Finite/Record） |
| `SampleRate` | `double` | 采样率（Hz） |
| `SamplesToAcquire` | `int` | 有限模式下要采集的样本数 |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数 |
| `TransferedSamples` | `ulong` | 已传输的样本数 |
| `Trigger` | `AITrigger` | 触发配置 |
| `SignalExport` | `AISignalExport` | 信号导出配置 |
| `Sync` | `AISync` | 同步配置 |
| `DigitalFiltering` | `AIDigitalFiltering` | 数字滤波配置 |
| `Device` | `AIDevice` | 设备配置（参考时钟等） |
| `Record` | `AIRecord` | 录制配置对象 |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int channel, double lowRange, double highRange, Coupling coupling, AITerminal terminal, bool iepeEnable)` | 添加模拟输入通道 |
| `RemoveChannel(int channel)` | 移除通道 |
| `Start()` | 启动采集任务 |
| `Stop()` | 停止采集任务 |
| `Commit()` | 提交任务配置（用于多卡同步） |
| `ReadData(ref double[,] buffer, int timeout)` | 读取采集数据（二维数组） |
| `ReadSinglePoint(ref double value, int channel)` | 单点读取（Single 模式） |
| `ReadSinglePoint(ref double[] values)` | 单点读取所有通道 |
| `WaitUntilDone(int timeout)` | 等待任务完成（Finite 模式） |
| `SendSoftwareTrigger()` | 发送软件触发 |
| `GetRecordPreviewData(ref double[,] buf, int samples, int timeout)` | 预览录制数据 |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态 |
| `DetectIEPEConnection()` | 检测 IEPE 传感器连接状态 |
| `IdentifyTEDSChip(int channel)` | 识别 TEDS 芯片 |
| `ReadTEDSData(int channel)` | 读取 TEDS 数据 |
| `GetSamplingInfo()` | 获取采样信息 |
| `GetSamplingInfo(double sampleRate, LPFMode lpfMode)` | 根据采样率获取采样信息 |

---

### JY9511AOTask

动态信号分析仪模拟输出任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9511AOTask(int boardNumber)` | 创建指定板卡的 AO 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `AOChannelCollection` | 通道集合 |
| `Mode` | `AOMode` | 输出模式（Finite/ContinuousNoWrapping/ContinuousWrapping/DDS） |
| `UpdateRate` | `double` | 更新率（Hz） |
| `Trigger` | `AOTrigger` | 触发配置 |
| `DDS` | `DDSConfig` | DDS 波形配置（DDS模式有效） |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int channel, double lowRange, double highRange, AOTerminal terminal)` | 添加模拟输出通道 |
| `WriteData(double[] data, int timeout)` | 写入输出数据 |
| `Start()` | 启动输出任务 |
| `Stop()` | 停止输出任务 |

---

### JY9511CITask

动态信号分析仪计数器输入任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9511CITask(int boardNumber, int counterID)` | 创建指定板卡和计数器的 CI 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `CIMode` | 计数器模式（Single/Continuous/Finite） |
| `Type` | `CIType` | 测量类型 |
| `Trigger` | `CITrigger` | 触发配置 |
| `EdgeCounting` | `EdgeCountingConfig` | 边沿计数配置 |
| `Frequency` | `FrequencyConfig` | 频率测量配置 |
| `Period` | `PeriodConfig` | 周期测量配置 |
| `Pulse` | `PulseConfig` | 脉冲测量配置 |

#### 方法

| 方法 | 说明 |
|------|------|
| `Start()` | 启动计数器任务 |
| `Stop()` | 停止计数器任务 |
| `SendSoftwareTrigger()` | 发送软件触发 |
| `ReadSinglePoint(ref uint value)` | 单点读取计数值（EdgeCounting模式） |
| `ReadSinglePoint(ref int value)` | 单点读取计数值（有符号） |
| `ReadSinglePoint(ref double value, int timeout)` | 单点读取频率/周期值 |
| `ReadData(ref uint[] buffer, int timeout)` | 读取计数数据 |
| `ReadData(ref int[] buffer, int timeout)` | 读取计数数据（有符号） |
| `ReadData(ref double[] buffer, int timeout)` | 读取频率/周期数据 |

---

## 设备硬件规格

| 参数 | 值 |
|------|-----|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 4 通道同步测量 |
| AI 采样率 | 62.5 S/s ~ 256 kS/s |
| AI 输入量程 | ±0.3125V / ±0.625V / ±1.25V / ±2.5V / ±5V / ±10V / ±25V / ±50V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分 / 伪差分 |
| AI IEPE 激励 | 每通道 4mA，软件使能 |
| AI TEDS 支持 | 兼容 TEDS 智能传感器 |
| AI 动态范围 | 111 dB |
| AO 分辨率 | 24-bit Δ-Σ DAC |
| AO 通道数 | 2 通道 |
| AO 更新率 | 最高 204.8 kS/s |
| AO 输出量程 | ±0.316V / ±1V / ±3.16V / ±10V |
| AO 动态范围 | 117 dB |
| 计数器 | 2 通道 |
| 接口 | USB / PCIe / PXIe |

### 产品型号

| 型号 | 描述 |
|------|------|
| USB-9511 | 16-CH 24-Bit 256 kS/s USB 高分辨率动态信号采集模块 |
| PCIe-9511 | 16-CH 24-Bit 256 kS/s PCIe 高分辨率动态信号采集模块 |
| PXIe-9511 | 16-CH 24-Bit 256 kS/s PXIe 高分辨率动态信号采集模块 |

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

### AOMode

AO 输出模式枚举。

| 值 | 说明 |
|----|------|
| `Finite` | 有限输出模式 |
| `ContinuousNoWrapping` | 连续不循环输出模式 |
| `ContinuousWrapping` | 连续循环输出模式 |
| `DDS` | 直接数字合成模式 |

### CIMode

计数器模式枚举。

| 值 | 说明 |
|----|------|
| `Single` | 单点模式 |
| `Continuous` | 连续模式 |
| `Finite` | 有限模式 |

### CIType

计数器测量类型枚举。

| 值 | 说明 |
|----|------|
| `EdgeCounting` | 边沿计数 |
| `Frequency` | 频率测量 |
| `Period` | 周期测量 |
| `Pulse` | 脉冲测量 |
| `SemiPeriod` | 半周期测量 |

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

### AOTerminal

AO 输出端子配置枚举。

| 值 | 说明 |
|----|------|
| `Differential` | 差分输出 |
| `PseudoDifferential` | 伪差分输出 |

### CountDirection

计数方向枚举。

| 值 | 说明 |
|----|------|
| `Up` | 向上计数 |
| `Down` | 向下计数 |
| `UpDown` | 双向计数 |

### TriggerType（触发类型）

- `AITriggerType.Immediately` - 立即触发（无触发）
- `AITriggerType.Digital` - 数字触发
- `AITriggerType.Analog` - 模拟触发
- `AITriggerType.Software` - 软触发

### AITriggerMode（触发模式）

- `AITriggerMode.Start` - 启动触发
- `AITriggerMode.Reference` - 参考触发（仅Finite模式有效）

### AIDigitalTriggerSource（数字触发源）

- `AIDigitalTriggerSource.Ext_Trig` - 外部触发
- `AIDigitalTriggerSource.PXI_Trig0` ~ `AIDigitalTriggerSource.PXI_Trig7` - PXI 触发总线
- `AIDigitalTriggerSource.AO_StartTrig` - AO 启动触发

### AIDigitalTriggerEdge（数字触发边沿）

- `AIDigitalTriggerEdge.Rising` - 上升沿触发
- `AIDigitalTriggerEdge.Falling` - 下降沿触发

### AIAnalogTriggerSource（模拟触发源）

- `AIAnalogTriggerSource.Channel_0` ~ `AIAnalogTriggerSource.Channel_3` - AI 通道 0-3

### AIAnalogTriggerComparator（模拟触发比较器）

- `AIAnalogTriggerComparator.Edge` - 边沿比较器
- `AIAnalogTriggerComparator.Hysteresis` - 迟滞比较器
- `AIAnalogTriggerComparator.Window` - 窗口比较器

### AIAnalogTriggerEdge（模拟触发边沿）

- `AIAnalogTriggerEdge.Rising` - 上升沿
- `AIAnalogTriggerEdge.Falling` - 下降沿

### AOTriggerType（AO触发类型）

- `AOTriggerType.Immediate` - 立即触发（无触发）
- `AOTriggerType.Digital` - 数字触发
- `AOTriggerType.Soft` - 软触发

### AODigitalTriggerSource（AO数字触发源）

- `AODigitalTriggerSource.Ext_Trig` - 外部触发
- `AODigitalTriggerSource.PXI_Trig0` ~ `AODigitalTriggerSource.PXI_Trig7` - PXI 触发总线
- `AODigitalTriggerSource.AI_StartTrig` - AI 启动触发
- `AODigitalTriggerSource.AI_ReferenceTrig` - AI 参考触发

### AODigitalTriggerEdge（AO数字触发边沿）

- `AODigitalTriggerEdge.Rising` - 上升沿触发
- `AODigitalTriggerEdge.Falling` - 下降沿触发

### CITriggerType（CI触发类型）

- `CITriggerType.Immediate` - 立即触发（无触发）
- `CITriggerType.Digital` - 数字触发
- `CITriggerType.Soft` - 软触发

### CIDigitalTriggerSource（CI数字触发源）

- `CIDigitalTriggerSource.PXI_Trig0` ~ `CIDigitalTriggerSource.PXI_Trig7` - PXI 触发总线
- （共32个触发源，包括各种内部信号）

### CIDigitalTriggerEdge（CI数字触发边沿）

- `CIDigitalTriggerEdge.Rising` - 上升沿触发
- `CIDigitalTriggerEdge.Falling` - 下降沿触发

### SyncTopology

同步拓扑枚举。

| 值 | 说明 |
|----|------|
| `Master` | 主卡 |
| `Slave` | 从卡 |

### SyncTerminal

同步终端枚举。

| 值 | 说明 |
|----|------|
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

---

## 设备类

### JY9511Device

设备信息类（静态属性）。

| 属性 | 类型 | 说明 |
|------|------|------|
| `TotalNumberOfAIChannels` | `int` | AI 通道总数 |
| `AIMinSampleRate` | `double` | AI 最小采样率 |
| `AIMaxSampleRate` | `double` | AI 最大采样率 |

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
| `PreTriggerSamples` | `uint` | 预触发样本数（Reference触发模式有效） |

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
| `Source` | `AIAnalogTriggerSource` | 触发源（Channel_0~Channel_3） |
| `Comparator` | `AIAnalogTriggerComparator` | 比较器类型（Edge/Hysteresis/Window） |
| `Edge` | `AIAnalogEdgeComparator` | 边沿比较器配置（Edge 比较器时有效），含 `Slope`（AIAnalogTriggerEdge）、`Threshold`（double） |
| `Hysteresis` | `AIAnalogHysteresisComparator` | 迟滞比较器配置（Hysteresis 比较器时有效），含 `Slope`、`HighThreshold`、`LowThreshold` |
| `Window` | `AIAnalogWindowComparator` | 窗口比较器配置（Window 比较器时有效），含 `Condition`（AIAnalogWindowCondition：Entering/Leaving）、`HighThreshold`、`LowThreshold` |

### AISync

同步配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Topology` | `SyncTopology` | 同步拓扑（Independent/Master/Slave） |
| `Terminal` | `SyncTerminal` | 同步终端 |
| `Synchronized` | `bool` | 是否已同步（只读） |

---

### AISignalExport

AI 信号导出配置对象。

| 属性/方法 | 类型 | 说明 |
|-----------|------|------|
| `RoutingPath` | `Dictionary<AISignalExportSource, List<SignalExportDestination>>` | 导出路由映射表 |
| `Add(source, destination)` | `void` | 添加单个信号导出 |
| `Add(source, destinations)` | `void` | 添加信号到多个目标 |
| `Clear(destination)` | `void` | 清除指定目标的导出 |
| `ClearAll()` | `void` | 清除所有信号导出 |

### AISignalExportSource（AI信号导出源）

- `AISignalExportSource.StartTrig` - AI 启动触发信号
- `AISignalExportSource.ReferenceTrig` - AI 参考触发信号

### SignalExportDestination（信号导出目标）

- `SignalExportDestination.Ext_Trig` - 外部触发端口
- `SignalExportDestination.PXI_Trig0` ~ `PXI_Trig7` - PXI 触发总线

### AIDevice

设备配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ReferenceClock` | `ReferenceClock` | 参考时钟配置 |

---

### AIDigitalFiltering

数字滤波配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `LPF` | `LowPassFilter` | 低通滤波器（抗混叠滤波器）配置 |

### LowPassFilter

低通滤波器配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `LPFMode` | 低通滤波模式 |

### LPFMode（低通滤波模式）

- `LPFMode.Normal` - 普通模式（0.4Fs带宽，中等延迟，最高256kS/s）
- `LPFMode.WideBandwidth` - 宽带宽模式（0.4535Fs带宽，高延迟，最高128kS/s）

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
| `Frequency` | `double` | 外部参考时钟频率（Hz） |

### EdgeCountingConfig

边沿计数配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Direction` | `CountDirection` | 计数方向 |
| `InitialCount` | `int` | 初始计数值 |

---

### AOTrigger

AO 触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AOTriggerType` | 触发类型 |
| `Digital` | `AODigitalTrigger` | 数字触发配置 |

### AODigitalTrigger

AO 数字触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AODigitalTriggerSource` | 触发源 |
| `Edge` | `AODigitalTriggerEdge` | 触发边沿 |

---

### CITrigger

CI 触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `CITriggerType` | 触发类型 |
| `Digital` | `CIDigitalTrigger` | 数字触发配置 |

### CIDigitalTrigger

CI 数字触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `CIDigitalTriggerSource` | 触发源 |
| `Edge` | `CIDigitalTriggerEdge` | 触发边沿 |

---

### AIRecord

录制配置对象。

````csharp
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

# 完整代码示例

﻿# JY9511 代码示例

## 1. AI 连续采集（多通道）

```csharp
using System;
using System.Windows.Forms;
using JY9511;
using SeeSharpTools.JY.ArrayUtility;

namespace JY9511Example
{
    public partial class MainForm : Form
    {
        private JY9511AITask aiTask;
        private double[,] dataToRead;
        private double[,] dataToPlot;

        private void btn_Start_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建 Task
                aiTask = new JY9511AITask(0);  // 板卡 0
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 256000;    // 256 kS/s

                // 添加通道（启用所有 4 个通道，IEPE 关闭）
                for (int i = 0; i < 4; i++)
                {
                    aiTask.AddChannel(i, -10, 10, Coupling.DC, AITerminal.Differential, false);
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
        aiTask = new JY9511AITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 100000;  // 采集 100k 个点

        // 添加单通道，启用 IEPE 激励
        aiTask.AddChannel(0, -10, 10, Coupling.AC, AITerminal.Differential, true);

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
        aiTask = new JY9511AITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;

        // 添加通道
        aiTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);

        // 配置数字触发
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;
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
        aiTask = new JY9511AITask(0);
        aiTask.Mode = AIMode.Finite;
        aiTask.SampleRate = 256000;
        aiTask.SamplesToAcquire = 50000;

        // 添加通道
        aiTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);

        // 配置模拟触发（Edge 比较器：需同时设置 Slope 与 Threshold）
        aiTask.Trigger.Type = AITriggerType.Analog;
        aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
        aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
        aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
        aiTask.Trigger.Analog.Edge.Threshold = 0.5;

        aiTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 5. AO 连续循环输出（正弦波）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;

private JY9511AOTask aoTask;
private double[] writeValue;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        // 创建 AO Task
        aoTask = new JY9511AOTask(0);

        // 添加通道
        aoTask.AddChannel(0, -10, 10, AOTerminal.Differential);

        // 配置输出模式
        aoTask.Mode = AOMode.ContinuousWrapping;
        aoTask.UpdateRate = 204800;  // 204.8 kS/s

        // 生成正弦波
        writeValue = new double[20480];  // 100ms 数据
        double amplitude = 5.0;
        double frequency = 1000.0;
        Generation.SineWave(ref writeValue, amplitude, 0, frequency, aoTask.UpdateRate);

        // 写入数据
        aoTask.WriteData(writeValue, -1);

        // 启动输出
        aoTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (aoTask != null)
    {
        aoTask.Stop();
        aoTask.Channels.Clear();
    }
}
```

## 6. AO 有限输出（方波）

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY9511AOTask(0);
        aoTask.AddChannel(0, -10, 10, AOTerminal.Differential);

        aoTask.Mode = AOMode.Finite;
        aoTask.UpdateRate = 204800;

        // 生成方波
        writeValue = new double[40960];
        double amplitude = 5.0;
        double frequency = 1000.0;
        Generation.SquareWave(ref writeValue, amplitude, 50, frequency, aoTask.UpdateRate);

        aoTask.WriteData(writeValue, -1);
        aoTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 7. AO DDS 多通道输出

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY9511AOTask(0);
        aoTask.Mode = AOMode.ContinuousWrapping;
        aoTask.UpdateRate = 204800;

        // 添加两个通道
        aoTask.AddChannel(0, -10, 10, AOTerminal.Differential);
        aoTask.AddChannel(1, -10, 10, AOTerminal.Differential);

        // 生成两路不同频率的正弦波
        double[] ch0Data = new double[20480];
        double[] ch1Data = new double[20480];
        
        Generation.SineWave(ref ch0Data, 5.0, 0, 1000.0, aoTask.UpdateRate);
        Generation.SineWave(ref ch1Data, 3.0, 0, 2000.0, aoTask.UpdateRate);

        // 交错写入多通道数据
        double[] interleavedData = new double[ch0Data.Length * 2];
        for (int i = 0; i < ch0Data.Length; i++)
        {
            interleavedData[i * 2] = ch0Data[i];
            interleavedData[i * 2 + 1] = ch1Data[i];
        }

        aoTask.WriteData(interleavedData, -1);
        aoTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 8. 计数器边沿计数（单点）

```csharp
private JY9511CITask ciTask;
private uint counterValue = 0;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        // 创建计数器任务（板卡 0，计数器 0）
        ciTask = new JY9511CITask(0, 0);

        // 配置为单点边沿计数模式
        ciTask.Mode = CIMode.Single;
        ciTask.Type = CIType.EdgeCounting;
        ciTask.EdgeCounting.Direction = CountDirection.Up;
        ciTask.EdgeCounting.InitialCount = 0;

        ciTask.Start();
        timer_FetchData.Enabled = true;
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    try
    {
        ciTask.ReadSinglePoint(ref counterValue);
        textBox_countValue.Text = counterValue.ToString();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (ciTask != null)
    {
        ciTask.Stop();
    }
    timer_FetchData.Enabled = false;
}
```

## 9. 计数器频率测量

```csharp
private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        ciTask = new JY9511CITask(0, 0);
        ciTask.Mode = CIMode.Continuous;
        ciTask.Type = CIType.Frequency;

        ciTask.Start();
        timer_FetchData.Enabled = true;
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    try
    {
        double[] freqData = new double[1];
        ciTask.ReadData(ref freqData, 1000);
        textBox_frequency.Text = freqData[0].ToString();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 9.1 计数器触发配置

```csharp
private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        ciTask = new JY9511CITask(0, 0);
        ciTask.Mode = CIMode.Continuous;
        ciTask.Type = CIType.EdgeCounting;
        ciTask.EdgeCounting.Direction = CountDirection.Up;
        ciTask.EdgeCounting.InitialCount = 0;

        // 配置数字触发
        ciTask.Trigger.Type = CITriggerType.Digital;
        ciTask.Trigger.Digital.Source = CIDigitalTriggerSource.PXI_Trig0;
        ciTask.Trigger.Digital.Edge = CIDigitalTriggerEdge.Rising;

        ciTask.Start();
        timer_FetchData.Enabled = true;
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 10. 多卡同步采集

````csharp
private JY9511AITask masterTask;
private JY9511AITask slaveTask;

private void button_Commit_Click(object sender, EventArgs e)
{
    try
    {
        // 创建任务
        slaveTask = new JY9511AITask(1);  // 从卡槽位 1
        masterTask = new JY9511AITask(0); // 主卡槽位 0

        // 添加通道
        slaveTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);
        masterTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);

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

## 11. IEPE 传感器采集

```csharp
private void btn_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9511AITask(0);
        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 256000;

        // 添加通道，启用 IEPE 激励（4mA）
        // 适用于加速度计、麦克风等 IEPE 传感器
        aiTask.AddChannel(0, -10, 10, Coupling.AC, AITerminal.Differential, true);

        aiTask.Start();
        // ...
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 12. 使用设备属性

```csharp
private void MainForm_Load(object sender, EventArgs e)
{
    // 获取设备信息
    int totalChannels = JY9511Device.TotalNumberOfAIChannels;
    double minRate = JY9511Device.AIMinSampleRate;
    double maxRate = JY9511Device.AIMaxSampleRate;

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

## 13. AO 数字触发输出

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY9511AOTask(0);
        aoTask.AddChannel(0, -10, 10, AOTerminal.Differential);

        aoTask.Mode = AOMode.ContinuousWrapping;
        aoTask.UpdateRate = 204800;

        // 配置数字触发
        aoTask.Trigger.Type = AOTriggerType.Digital;
        aoTask.Trigger.Digital.Source = AODigitalTriggerSource.Ext_Trig;
        aoTask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

        // 生成并写入波形
        writeValue = new double[20480];
        Generation.SineWave(ref writeValue, 5.0, 0, 1000.0, aoTask.UpdateRate);
        aoTask.WriteData(writeValue, -1);

        // 启动（等待触发）
        aoTask.Start();
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 14. 读取传输样本数

```csharp
private void timer_CheckStatus_Tick(object sender, EventArgs e)
{
    // 获取可用样本数
    ulong availableSamples = aiTask.AvailableSamples;
    toolStripStatusLabel_AvailableSamples.Text = 
        string.Format("Available Samples: {0}", availableSamples);

    // 获取已传输样本数
    ulong transferedSamples = aiTask.TransferedSamples;
    toolStripStatusLabel_TransferedSamples.Text = 
        string.Format("Transfered Samples: {0}", transferedSamples);

    if (availableSamples >= (ulong)samplesToUpdate)
    {
        aiTask.ReadData(ref dataToRead, 2000);
        // 处理数据...
    }
}
```

---

## 15. Record 有限录制模式（Finite Streaming）

录制固定时长的数据到文件，录制完成后自动停止。定时器中通过`GetRecordStatus`检查录制状态：

```csharp
private JY9511AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9511AITask(0);
        
        // 添加通道
        aiTask.AddChannel(0, -10, 10, Coupling.DC, AITerminal.Differential, false);
        
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
    // 获取预览数据
    if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
        // 显示预览数据...
    }
    
    // 检查录制状态
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
}
```

---

## 16. Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止。定时器中只读取预览数据，不检查录制状态：

```csharp
private JY9511AITask aiTask;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9511AITask(0);
        
        // 添加多通道
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, -10, 10, Coupling.DC, AITerminal.Differential, false);
        }
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 51200;  // 51.2 kS/s
        
        // 配置无限录制（不设置 Length）
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制
        
        aiTask.Start();
        timer_Preview.Enabled = true;
        
        button_Start.Enabled = false;
        button_Stop.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
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

## 17. 录制文件回放

读取已录制的 bin 文件回放示例

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;
private ulong totalSamples;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    // 打开录制文件
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    
    if (!File.Exists(fileBrowser.FileName))
    {
        MessageBox.Show("文件不存在！");
        return;
    }
    
    // 数据格式为
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 4;  // 录制时长通道
    totalSamples = (ulong)(fileInfo.Length / sizeof(double) / channelCount);
    
    // 文件
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
        
        // 显示图表
        // easyChartX1.Plot(playbackData);
    }
    catch (EndOfStreamException)
    {
        timer_Playback.Enabled = false;
        playbackReader.Close();
        playbackStream.Close();
        MessageBox.Show("回放完成！");
    }
}
```