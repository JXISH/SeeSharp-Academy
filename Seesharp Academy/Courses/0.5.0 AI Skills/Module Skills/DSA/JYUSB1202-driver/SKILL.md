---
name: jyusb1202-driver
description: 提供 JYTEK USB-1202 系列（USB-1202-P7/P3）高精度 USB 动态信号分析仪（DSA）的完整 C# 驱动开发指引。涵盖模拟输入（AI）有限/连续/录制采集、4 路计数器输入（CI）、计数器输出（CO）、4 路数字 I/O（DI/DO）、IEPE 激励、触发配置（立即/数字/软件）、采样时钟配置（内部/外部）、信号导出、多卡同步（IsAISync）、录制模式（Finite/Infinite Streaming）。当用户使用 USB-1202-P7、USB-1202-P3、JYUSB1202、JYUSB1202AITask、JYUSB1202CITask、JYUSB1202COTask、JYUSB1202DITask、JYUSB1202DOTask、AIMode.Finite、AIMode.Continuous、AIMode.Record、IEPE 开发精密传感、振动分析、声学测量、便携测试应用时自动应用。
---

# JYUSB1202 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DSA\JYUSB1202\Bin\JYUSB1202.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DSA\JYUSB1202\Bin\JYUSB1202.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JYUSB1202;`
- **设备名称/槽位号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number）或设置的板卡别名（Board Name），如 `0`、`"Dev1"` 等

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AI 分辨率 | 24-bit Δ-Σ ADC |
| AI 通道数 | 4 通道同步测量 |
| AI 采样率 | 1 kS/s ~ 256 kS/s（P7）/ 1 kS/s ~ 25.6 kS/s（P3） |
| AI 输入量程 | ±0.32V / ±1.25V / ±5V / ±12V |
| AI 耦合方式 | AC / DC 软件可选 |
| AI 输入配置 | 差分（Differential）/ 伪差分（PSEUDIFF） |
| AI IEPE 激励 | 每通道 4mA，软件使能 |
| AI 过压保护 | ±60V |
| AI 输入连接器 | SMA |
| AI 直流精度 | 270 ppm |
| AI 动态范围 | 96 dB ~ 116 dB |
| 计数器 | 支持边沿计数/频率/周期/脉宽/双边沿间隔/正交编码 |
| 数字 I/O | 4 路独立控制 |
| 触发 | 立即触发 / 数字触发（DIO_0~DIO_3） / 软件触发 |
| 接口 | USB（总线供电） |

## 通用编程范式

### AI 采集任务流程

```
创建 AITask → 添加通道 → 配置模式/采样率 → 配置触发（可选） → 启动 → 读取数据 → 停止
```

### 标准代码框架

```csharp
// 1. 创建 Task
JYUSB1202AITask aiTask = new JYUSB1202AITask(boardNumber);

// 2. 添加通道（使用量程数值，支持 IEPE 激励）
aiTask.AddChannel(channelID, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

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
| `aiTask.AddChannel(chID, rangeLow, rangeHigh, terminal, coupling, iepeEnable)` | 添加单个通道（使用量程数值） |
| `aiTask.AddChannel(chIDs, rangeLow, rangeHigh, terminal, coupling, iepeEnable)` | 批量添加通道（统一量程） |
| `aiTask.AddChannel(chIDs, rangeLow[], rangeHigh[], terminal[], coupling[], iepeEnable[])` | 批量添加通道（各自独立参数） |
| `aiTask.RemoveChannel(chID)` | 移除指定通道 |
| `aiTask.Channels.Count` | 已添加通道数 |
| `aiTask.Channels[i].ChannelID` | 获取通道 ID |

### 采集模式

| 模式 | 说明 |
|------|------|
| `AIMode.Finite` | AI 有限采集模式（采够 SamplesToAcquire 后停止） |
| `AIMode.Continuous` | AI 连续采集模式 |
| `AIMode.Record` | AI 录制模式（流式写入文件） |

### 触发配置

```csharp
// 立即触发（默认，启动即开始采集）
aiTask.Trigger.Type = AITriggerType.Immediate;

// 数字触发（DIO 线输入）
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 软件触发（需调用 SendSoftwareTrigger）
aiTask.Trigger.Type = AITriggerType.Soft;
// ... aiTask.Start();
aiTask.SendSoftwareTrigger();
```

### 采样时钟

```csharp
// 内部采样时钟（默认）
aiTask.SampleClock.Source = AISampleClockSource.Internal;

// 外部采样时钟
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 256000;  // 告知驱动期望速率
```

### 信号导出（DIO 线输出）

```csharp
// 将启动触发信号导出到 DIO_1
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.DIO_1);

// 将采样时钟导出到 DIO_2
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.DIO_2);

// 清除指定目标的导出
aiTask.SignalExport.Clear(SignalExportDestination.DIO_1);

// 清除全部导出
aiTask.SignalExport.ClearAll();
```

### 多卡同步检查

```csharp
// USB-1202 通过 JYUSB1202Device.IsAISync 标志指示设备间同步状态
bool isSync = JYUSB1202Device.GetInstance(0).IsAISync;
```

## 关键枚举

### AITerminal（AI 输入端子配置）

- `AITerminal.Differential` - 差分输入
- `AITerminal.PSEUDIFF` - 伪差分输入

### AICoupling（耦合方式）

- `AICoupling.DC` - DC 耦合
- `AICoupling.AC` - AC 耦合

### AITriggerType（触发类型）

- `AITriggerType.Immediate` - 立即触发（无触发，启动即采集）
- `AITriggerType.Digital` - 数字触发（由 DIO 输入触发）
- `AITriggerType.Soft` - 软件触发（由 SendSoftwareTrigger 触发）

### AIDigitalTriggerSource（数字触发源）

- `AIDigitalTriggerSource.DIO_0` ~ `DIO_3` - DIO 线 0~3

### AIDigitalTriggerEdge（数字触发/电平边沿）

- `AIDigitalTriggerEdge.Rising` - 上升沿触发
- `AIDigitalTriggerEdge.Falling` - 下降沿触发
- `AIDigitalTriggerEdge.HighLevel` - 高电平触发
- `AIDigitalTriggerEdge.LowLevel` - 低电平触发

### AISampleClockSource（采样时钟源）

- `AISampleClockSource.Internal` - 内部时钟
- `AISampleClockSource.External` - 外部时钟

### AISignalExportSource（信号导出源）

- `AISignalExportSource.StartTrig` - 启动触发信号
- `AISignalExportSource.SampleClock` - 采样时钟

### SignalExportDestination（信号导出目标）

- `SignalExportDestination.DIO_0` ~ `DIO_3` - DIO 线 0~3

### RecordMode（录制模式）

- `RecordMode.Finite` - 有限录制，录制指定时长后自动停止
- `RecordMode.Infinite` - 无限录制，需手动调用 Stop 停止

### FileFormat（录制文件格式）

- `FileFormat.Bin` - 二进制格式（64-bit double）

## 设备属性

```csharp
// 获取设备能力（静态方法）
int totalChannels = JYUSB1202Device.GetCapability(slotNumber).AI.NumberOfChannels;
double minRate = JYUSB1202Device.GetCapability(slotNumber).AI.MinSampleRate;
double maxRate = JYUSB1202Device.GetCapability(slotNumber).AI.MaxSampleRate;
```

### JYUSB1202Device 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `AIBufferSize` | `uint` | AI 缓冲区大小 |
| `IsAISync` | `bool` | AI 同步状态标志 |
| `Capability` | `DeviceCapability` | 设备能力（包含 AI 子项） |
| `Handle` | `IntPtr` | 设备句柄 |

### JYUSB1202Device 静态方法

| 方法 | 说明 |
|------|------|
| `GetCapability(int slotNumber)` | 获取指定槽位设备能力 |
| `GetCapability(string deviceName)` | 获取指定名称设备能力 |
| `Release()` | 释放设备资源 |

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

---

# 完整 API 参考

## 命名空间

```csharp
using JYUSB1202;
```

## JYUSB1202AITask（模拟输入任务）

### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JYUSB1202AITask(int slotNumber)` | 使用槽位号创建 AI 任务 |
| `JYUSB1202AITask(string cardName)` | 使用板卡名称创建 AI 任务 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `List<AIChannel>` | 通道集合 |
| `Mode` | `AIMode` | 采集模式（Finite/Continuous/Record） |
| `SampleRate` | `double` | 采样率（Hz） |
| `SamplesToAcquire` | `int` | 有限模式下要采集的样本数 |
| `BufLenInSamples` | `int` | 驱动缓冲区长度（样本数） |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数 |
| `TransferedSamples` | `ulong` | 已传输的样本数 |
| `SampleClock` | `AISampleClock` | 采样时钟配置 |
| `Trigger` | `AITrigger` | 触发配置 |
| `Record` | `AIRecord` | 录制配置 |
| `SignalExport` | `AISignalExport` | 信号导出配置 |
| `Device` | `JYUSB1202Device` | 设备实例 |

### 通道/采集控制方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int chID, double rangeLow, double rangeHigh, AITerminal terminal, AICoupling coupling, bool iepeEnable)` | 添加单个通道 |
| `AddChannel(int[] chIDs, double rangeLow, double rangeHigh, AITerminal terminal, AICoupling coupling, bool iepeEnable)` | 批量添加通道（统一参数） |
| `AddChannel(int[] chIDs, double[] rangeLow, double[] rangeHigh, AITerminal[] terminal, AICoupling[] coupling, bool[] iepeEnable)` | 批量添加通道（独立参数） |
| `RemoveChannel(int chID)` | 移除指定通道 |
| `Start()` | 启动采集任务 |
| `Stop()` | 停止采集任务 |
| `SendSoftwareTrigger()` | 发送软件触发 |
| `WaitUntilDone(int timeout)` | 等待任务完成（Finite 模式） |

### 读取方法

| 方法 | 说明 |
|------|------|
| `ReadData(ref double[,] data, int samplesPerChannel, int timeout)` | 读取多通道数据（二维，指定样本数） |
| `ReadData(ref double[,] data, int timeout)` | 读取多通道数据（按 data 容量） |
| `ReadData(ref double[] data, int samplesPerChannel, int timeout)` | 读取一维数据 |
| `ReadData(ref double[] data, int timeout)` | 读取一维数据（按容量） |
| `ReadData(IntPtr data, int samplesPerChannel, int timeout)` | 读取到指针缓冲区 |
| `ReadRawData(ref short[,] data, int samplesPerChannel, int timeout)` | 读取原始码值数据（二维） |
| `ReadRawData(ref short[] data, int samplesPerChannel, int timeout)` | 读取原始码值数据（一维） |
| `ReadRawData(IntPtr data, int samplesPerChannel, int timeout)` | 读取原始码值到指针缓冲区 |
| `GetScalingCoefficients()` | 获取原始码值的电压换算系数（Start 后调用） |
| `ScaleData(int[] rawData, ref double[] scaledData)` | 将原始码值转换为电压值 |

### 录制相关方法

| 方法 | 说明 |
|------|------|
| `GetRecordPreviewData(ref double[,] buf, int samples, int timeout)` | 获取多通道预览数据 |
| `GetRecordPreviewData(ref double[,] buf, int timeout)` | 获取多通道预览数据（按容量） |
| `GetRecordPreviewData(ref double[] buf, int samples, int timeout)` | 获取一维预览数据 |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态 |

## 配置对象

### AITrigger

触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AITriggerType` | 触发类型（Immediate/Digital/Soft） |
| `Digital` | `AIDigitalTrigger` | 数字触发子对象 |

### AIDigitalTrigger

数字触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIDigitalTriggerSource` | 触发源（DIO_0~DIO_3） |
| `Edge` | `AIDigitalTriggerEdge` | 触发边沿/电平（Rising/Falling/HighLevel/LowLevel） |

### AISampleClock

采样时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AISampleClockSource` | 时钟源（Internal/External） |
| `External` | `AIExternalSampleClock` | 外部时钟子对象 |

### AIExternalSampleClock

外部采样时钟。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ExpectedRate` | `double` | 期望的外部时钟速率（Hz） |

### AISignalExport

信号导出配置。

| 方法 | 说明 |
|------|------|
| `Add(AISignalExportSource source, SignalExportDestination destination)` | 导出单个信号到指定 DIO 目标 |
| `Add(AISignalExportSource source, List<SignalExportDestination> destinations)` | 导出到多个目标 |
| `Clear(SignalExportDestination destination)` | 清除指定目标的信号导出 |
| `ClearAll()` | 清除所有信号导出 |

### AIRecord

录制配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |

### AIChannel

| 属性 | 类型 | 说明 |
|------|------|------|
| `ChannelID` | `int` | 通道 ID |
| `RangeLow` | `double` | 量程下限 |
| `RangeHigh` | `double` | 量程上限 |
| `Terminal` | `AITerminal` | 输入端子配置 |
| `Coupling` | `AICoupling` | 耦合方式 |
| `EnableIEPE` | `bool` | 是否使能 IEPE 激励 |

---

## JYUSB1202CITask（计数器输入任务）

### 构造函数与属性

| 成员 | 类型 | 说明 |
|------|------|------|
| `JYUSB1202CITask()` | 构造 | 创建 CI 任务 |
| `JYUSB1202CITask(string cardName)` | 构造 | 使用板卡名称创建 CI 任务 |
| `Type` | `CIType` | 计数器类型 |
| `EdgeCounting` | `EdgeCounting` | 边沿计数配置 |
| `FrequencyMeas` | `FrequencyMeas` | 频率测量配置 |
| `PeriodMeas` | `PeriodMeas` | 周期测量配置 |
| `PulseMeas` | `PulseMeas` | 脉宽测量配置 |
| `TwoEdgeSeparation` | `TwoEdgeSeparation` | 双边沿间隔测量配置 |
| `QuadEncoder` | `QuadEncoder` | 正交编码器配置 |
| `Device` | `JYUSB1202Device` | 设备实例 |

### 方法

| 方法 | 说明 |
|------|------|
| `Start()` | 启动 CI 任务 |
| `Stop()` | 停止 CI 任务 |
| `ReadSinglePoint(out uint count)` | 读取单点计数值 |
| `ReadSinglePoint(out double value, int timeout)` | 读取单点 double 值（频率/周期等） |
| `ReadSinglePoint(out double value1, out double value2, int timeout)` | 读取双值（例如正交编码器） |

### CIType（计数器类型）

- `CIType.EdgeCounting` - 边沿计数
- `CIType.Period` - 周期测量
- `CIType.Frequency` - 频率测量
- `CIType.Pulse` - 脉宽测量
- `CIType.TwoEdgeSeparation` - 双边沿间隔
- `CIType.QuadEncoder` - 正交编码器

### 相关枚举

- `EdgeType`：`Rising` / `Falling` / `Any`
- `CountDirection`：`Up` / `Down`
- `LevelPolarity`：`LowLevel` / `HighLevel` / `None`
- `EdgeCntOunEvtIdleState`：`LowLevel` / `HighLevel`
- `QuadEncodingType`：`X2` / `X4`

### EdgeCounting 配置

| 属性 | 类型 | 说明 |
|------|------|------|
| `InitialCount` | `uint` | 初始计数值 |
| `Direction` | `CountDirection` | 计数方向 |
| `Pause` | `PauseTrigger` | 暂停触发配置 |
| `OutEvent` | `EdgeCountingOutEvent` | 输出事件配置 |

### QuadEncoder 配置

| 属性 | 类型 | 说明 |
|------|------|------|
| `EncodingType` | `QuadEncodingType` | 编码类型（X2/X4） |
| `ZReloadEnabled` | `bool` | 是否允许 Z 相重载 |
| `InitialCount` | `uint` | 初始计数值 |

---

## JYUSB1202COTask（计数器输出任务）

### 构造函数与属性

| 成员 | 类型 | 说明 |
|------|------|------|
| `JYUSB1202COTask()` | 构造 | 创建 CO 任务 |
| `JYUSB1202COTask(string cardName)` | 构造 | 使用板卡名称创建 CO 任务 |
| `Pause` | `PauseTrigger` | 暂停触发配置 |
| `IdleState` | `COIdleState` | 空闲电平（HighLevel/LowLevel） |
| `Device` | `JYUSB1202Device` | 设备实例 |

### 方法

| 方法 | 说明 |
|------|------|
| `WriteSinglePoint(COPulse pulse)` | 写入单个脉冲配置并输出 |
| `Start()` | 启动脉冲输出 |
| `Stop()` | 停止脉冲输出 |
| `WaitUntilDone(int timeout)` | 等待输出完成 |

### COPulse 构造与属性

```csharp
// 构造函数
COPulse pulse = new COPulse(COPulseType type, double param1, double param2, int count);
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `Count` | `int` | 脉冲个数 |
| `IsCountUnlimited` | `bool` | 是否无限脉冲输出 |

### COPulseType

- `COPulseType.DutyCycleFrequency` - 按频率+占空比
- `COPulseType.HighLowTime` - 按高/低电平时间（秒）
- `COPulseType.HighLowTick` - 按高/低电平计数（Tick）

### COIdleState

- `COIdleState.HighLevel` - 空闲时高电平
- `COIdleState.LowLevel` - 空闲时低电平

---

## JYUSB1202DITask（数字输入任务）

### 构造函数与属性

| 成员 | 类型 | 说明 |
|------|------|------|
| `JYUSB1202DITask()` | 构造 | 创建 DI 任务 |
| `JYUSB1202DITask(string cardName)` | 构造 | 使用板卡名称创建 DI 任务 |
| `Channels` | `List<DIChannel>` | 通道集合（每通道对应一条 DIO 线） |
| `Device` | `JYUSB1202Device` | 设备实例 |

### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int lineNum)` | 添加单条 DIO 线 |
| `AddChannel(int[] lineNums)` | 添加多条 DIO 线 |
| `RemoveChannel(int lineNum)` | 移除 DIO 线 |
| `Start()` | 启动 DI 任务 |
| `Stop()` | 停止 DI 任务 |
| `ReadSinglePoint(out bool value, int timeout)` | 读取单条 DIO 线状态 |
| `ReadSinglePoint(out bool[] values)` | 读取所有已添加线的状态 |

---

## JYUSB1202DOTask（数字输出任务）

### 构造函数与属性

| 成员 | 类型 | 说明 |
|------|------|------|
| `JYUSB1202DOTask()` | 构造 | 创建 DO 任务 |
| `JYUSB1202DOTask(string cardName)` | 构造 | 使用板卡名称创建 DO 任务 |
| `Channels` | `List<DOChannel>` | 通道集合 |
| `Device` | `JYUSB1202Device` | 设备实例 |

### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int lineNum)` | 添加单条 DIO 线 |
| `AddChannel(int[] lineNums)` | 添加多条 DIO 线 |
| `RemoveChannel(int lineNum)` | 移除 DIO 线 |
| `Start()` | 启动 DO 任务 |
| `Stop()` | 停止 DO 任务 |
| `WriteSinglePoint(bool value, int timeout)` | 输出单点电平 |
| `WriteSinglePoint(bool[] values)` | 输出多线电平 |

---

## 校准控制（JYUSB1202Miscellaneous）

可禁用驱动自动校准以便开发者进行原始码值处理：

```csharp
JYUSB1202Device device = JYUSB1202Device.GetInstance(0);

// 查询/设置 AI 校准使能
bool aiCal = JYUSB1202Miscellaneous.GetAICalibrationState(device);
JYUSB1202Miscellaneous.SetAICalibrationState(device, false);

// 查询/设置 AO 校准使能
bool aoCal = JYUSB1202Miscellaneous.GetAOCalibrationState(device);
JYUSB1202Miscellaneous.SetAOCalibrationState(device, false);
```

### DeviceMiscellaneousParam

| 属性 | 类型 | 说明 |
|------|------|------|
| `Device` | `JYUSB1202Device` | 目标设备 |
| `DisableAICalibration` | `bool` | 禁用 AI 校准标志 |
| `DisableAOCalibration` | `bool` | 禁用 AO 校准标志 |

### ScalingCoefficients（原始码值换算系数）

```csharp
aiTask.Start();
ScalingCoefficients sc = aiTask.GetScalingCoefficients();
double voltage = rawValue * sc.Gain + sc.Offset;
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `Gain` | `double` | 增益 |
| `Offset` | `double` | 偏置 |

---

# 完整代码示例

## 1. AI 连续采集（4 通道）

```csharp
using System;
using System.Windows.Forms;
using JYUSB1202;
using SeeSharpTools.JY.ArrayUtility;

public partial class MainForm : Form
{
    private JYUSB1202AITask aiTask;
    private double[,] dataToRead;
    private double[,] dataToPlot;

    private void btn_Start_Click(object sender, EventArgs e)
    {
        try
        {
            aiTask = new JYUSB1202AITask(0);  // 板卡 0
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 256000;

            int[] channels = { 0, 1, 2, 3 };
            aiTask.AddChannel(channels, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

            aiTask.Start();

            int samplesToUpdate = 10000;
            dataToRead = new double[samplesToUpdate, aiTask.Channels.Count];
            dataToPlot = new double[aiTask.Channels.Count, samplesToUpdate];
            timer_CheckStatus.Enabled = true;
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    }

    private void timer_CheckStatus_Tick(object sender, EventArgs e)
    {
        if (aiTask.AvailableSamples >= (ulong)dataToRead.GetLength(0))
        {
            aiTask.ReadData(ref dataToRead, 2000);
            ArrayManipulation.Transpose(dataToRead, ref dataToPlot);
            // easyChartX1.Plot(dataToPlot);
        }
    }

    private void btn_Stop_Click(object sender, EventArgs e)
    {
        aiTask.Stop();
        timer_CheckStatus.Enabled = false;
    }
}
```

## 2. AI 有限采集（单通道）

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 256000;
aiTask.SamplesToAcquire = 100000;  // 采集 100k 个点

// 启用 IEPE 激励，±5V 量程
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.AC, true);

aiTask.Start();

double[,] dataBuffer = new double[aiTask.SamplesToAcquire, 1];
while (aiTask.AvailableSamples < (ulong)aiTask.SamplesToAcquire)
{
    Application.DoEvents();
}
aiTask.ReadData(ref dataBuffer, 5000);
aiTask.Stop();
```

## 3. AI 数字触发采集（DIO 触发）

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 256000;
aiTask.SamplesToAcquire = 50000;

aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

// 使用 DIO_0 作为数字触发输入，上升沿触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// ... 等待触发并读取数据
```

## 4. AI 软件触发采集

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 100000;
aiTask.SamplesToAcquire = 10000;
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();

// 业务逻辑就绪后，由软件发送触发
aiTask.SendSoftwareTrigger();

double[,] data = new double[aiTask.SamplesToAcquire, 1];
aiTask.ReadData(ref data, 5000);
aiTask.Stop();
```

## 5. AI 信号导出到 DIO

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100000;
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

// 将采样时钟输出到 DIO_2
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.DIO_2);
// 将启动触发信号输出到 DIO_1
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.DIO_1);

aiTask.Start();
```

## 6. IEPE 传感器采集

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 256000;

// 启用 IEPE 激励（4mA），AC 耦合，±5V 量程
// 适用于加速度计、麦克风等 IEPE 传感器
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.AC, true);

aiTask.Start();
```

## 7. 使用设备属性

```csharp
private void MainForm_Load(object sender, EventArgs e)
{
    int slotNumber = 0;
    int totalChannels = JYUSB1202Device.GetCapability(slotNumber).AI.NumberOfChannels;
    double minRate = JYUSB1202Device.GetCapability(slotNumber).AI.MinSampleRate;
    double maxRate = JYUSB1202Device.GetCapability(slotNumber).AI.MaxSampleRate;

    numericUpDown_SampleRate.Minimum = (decimal)minRate;
    numericUpDown_SampleRate.Maximum = (decimal)maxRate;

    for (int i = 0; i < totalChannels; i++)
        comboBox_Channel.Items.Add(string.Format("Ch{0}", i));
}
```

## 8. Record 有限录制（Finite Streaming）

录制固定时长的数据到文件，录制完成后自动停止。通过 `GetRecordStatus` 检查完成状态：

```csharp
private JYUSB1202AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JYUSB1202AITask(0);
        aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 256000;
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;
        aiTask.Record.Length = 10.0;  // 录制 10 秒

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
        try { aiTask?.Stop(); }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
        MessageBox.Show("录制完成！");
    }
    else
    {
        if (aiTask.AvailableSamples >= 1000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
            // easyChartX1.Plot(previewData);
        }
        timer_Preview.Enabled = true;
    }
}
```

## 9. Record 无限录制（Infinite Streaming）

持续录制数据，需手动 `Stop` 停止；定时器中仅读取预览数据：

```csharp
aiTask = new JYUSB1202AITask(0);
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 51200;
aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
aiTask.Record.Mode = RecordMode.Infinite;  // 不设置 Length

aiTask.Start();

previewData = new double[1000, aiTask.Channels.Count];
timer_Preview.Enabled = true;

// 在定时器中仅读取预览数据
private void timer_Preview_Tick(object sender, EventArgs e)
{
    if (aiTask.AvailableSamples >= 1000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
    }
}

// 手动停止
private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    aiTask?.Stop();
}
```

## 10. 录制数据回放

读取已录制的 bin 文件（double 格式）并回放显示：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;

    int channelCount = 4;  // 录制时通道数
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
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
    }
}
```

## 11. CI 边沿计数

```csharp
JYUSB1202CITask ciTask = new JYUSB1202CITask();
ciTask.Type = CIType.EdgeCounting;
ciTask.EdgeCounting.InitialCount = 0;
ciTask.EdgeCounting.Direction = CountDirection.Up;

ciTask.Start();

uint count;
ciTask.ReadSinglePoint(out count);
Console.WriteLine($"Edge count: {count}");

ciTask.Stop();
```

## 12. CI 频率测量

```csharp
JYUSB1202CITask ciTask = new JYUSB1202CITask();
ciTask.Type = CIType.Frequency;

ciTask.Start();

double frequency;
ciTask.ReadSinglePoint(out frequency, 5000);
Console.WriteLine($"Frequency: {frequency} Hz");

ciTask.Stop();
```

## 13. CI 正交编码器

```csharp
JYUSB1202CITask ciTask = new JYUSB1202CITask();
ciTask.Type = CIType.QuadEncoder;
ciTask.QuadEncoder.EncodingType = QuadEncodingType.X4;
ciTask.QuadEncoder.InitialCount = 0;

ciTask.Start();

double position;
ciTask.ReadSinglePoint(out position, 1000);

ciTask.Stop();
```

## 14. CO 脉冲输出（按频率+占空比）

```csharp
JYUSB1202COTask coTask = new JYUSB1202COTask();
coTask.IdleState = COIdleState.LowLevel;

// 1 kHz, 50% 占空比, 100 个脉冲
COPulse pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, 100);
coTask.WriteSinglePoint(pulse);
coTask.Start();

coTask.WaitUntilDone(-1);
coTask.Stop();
```

## 15. DI 单点读取

```csharp
JYUSB1202DITask diTask = new JYUSB1202DITask();
diTask.AddChannel(new int[] { 0, 1, 2, 3 });  // 读取 DIO_0 ~ DIO_3
diTask.Start();

bool[] values;
diTask.ReadSinglePoint(out values);
for (int i = 0; i < values.Length; i++)
    Console.WriteLine($"DIO_{i}: {values[i]}");

diTask.Stop();
```

## 16. DO 单点输出

```csharp
JYUSB1202DOTask doTask = new JYUSB1202DOTask();
doTask.AddChannel(new int[] { 0, 1, 2, 3 });
doTask.Start();

// 输出 DIO_0=High, DIO_1=Low, DIO_2=High, DIO_3=Low
bool[] values = { true, false, true, false };
doTask.WriteSinglePoint(values);

doTask.Stop();
```
