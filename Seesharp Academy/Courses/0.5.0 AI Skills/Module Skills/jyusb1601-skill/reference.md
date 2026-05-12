# JYUSB-1601 驱动 API 完整参考

## 命名空间与引用

```csharp
using JYUSB1601;
// DLL 路径："C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB1601\Bin\JYUSB1601.dll"
```

---

## 设备信息类 `JYUSB1601Device`

通过 `aiTask.Device` / `aoTask.Device` 访问：

| 属性 | 类型 | 说明 |
|------|------|------|
| `BoardClockRate` | double | 板卡主时钟频率（Hz） |
| `DiffChannelCount` | int | 差分通道数（8） |
| `SEChannelCount` | int | 单端通道数（16） |
| `MaxSampleRateSingleChannel` | double | AI 单通道最大采样率 |
| `MaxSampleRateMutilChannel` | double | AI 多通道最大采样率 |
| `AOChannelCount` | int | AO 通道数（2） |
| `MaxUpdateRate` | double | AO 最大更新率 |
| `SerialNumber` | string | 设备序列号 |

---

## JYUSB1601AITask — 模拟输入任务

### 构造函数

```csharp
new JYUSB1601AITask()               // 按槽位号创建
new JYUSB1601AITask(string boardName)  // 按板卡别名创建（推荐）
```

### 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Mode` | `AIMode` | — | Single / Finite / Continuous / Record |
| `SampleRate` | `double` | 10000 | 每通道采样率（Sa/s） |
| `SamplesToAcquire` | `int` | — | Finite 模式采集点数/通道 |
| `BufLenInSamples` | `int` | — | 缓冲区容量（点/通道），须 ≥ SamplesToAcquire |
| `AvailableSamples` | `ulong` | — | 缓冲区可读点数（非 Single 模式有效） |
| `TransferedSamples` | `ulong` | — | 已传输点数（非 Single 模式有效） |
| `Bandwidth` | `AIBandwidth` | `_80KHz` | \_15KHz / \_39KHz / \_80KHz |
| `SampleClock` | `AISampleClock` | — | 时钟配置对象 |
| `Trigger` | `AITrigger` | — | 触发配置对象 |
| `Record` | `AIRecord` | — | 录制配置对象 |
| `SignalExport` | `AISignalExport` | — | 信号导出配置 |
| `Channels` | `List<AIChannel>` | — | 已添加的通道列表 |

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

| 值 | 说明 |
|----|------|
| `Single` | 软件触发单点读取，可循环调用 ReadSinglePoint |
| `Finite` | 采集固定点数后停止，通过 WaitUntilDone 等待完成 |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取 |
| `Record` | 数据流式写入文件，支持预览 |

## AITerminal 枚举

| 值 | 说明 |
|----|------|
| `RSE` | 单端（Referenced Single-Ended），16 通道 |
| `Differential` | 差分，8 通道，共模抑制更好 |

## AIBandwidth 枚举

| 值 | 带宽 |
|----|------|
| `_15KHz` | 15 kHz 低通 |
| `_39KHz` | 39 kHz 低通 |
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

| 值 | 说明 |
|----|------|
| `Immediate` | 立即触发（默认） |
| `Digital` | 数字边沿/电平触发 |
| `Soft` | 软件触发 |

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath   = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;           // 仅支持 Bin 格式
aiTask.Record.Mode       = RecordMode.Finite;        // Finite / Infinite
aiTask.Record.Length     = 10.0;                     // 录制时长（秒），Finite 有效
```

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

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Mode` | `AOMode` | — | Single/Finite/ContinuousWrapping/ContinuousNoWrapping |
| `UpdateRate` | `double` | 1000000 | 更新率（Sa/s） |
| `SamplesToUpdate` | `int` | — | 缓冲区点数/通道（Finite/Continuous 有效） |
| `AvaliableLenInSamples` | `int` | — | 缓冲区剩余可写点数 |
| `CompleteState` | `OutputCompleteState` | `Hold` | Zero/Hold |
| `TransferedSamples` | `ulong` | — | 已输出点数 |
| `SampleClock` | `AOSampleClock` | — | 时钟配置 |
| `Trigger` | `AOTrigger` | — | 触发配置 |
| `SignalExport` | `AOSignalExport` | — | 信号导出 |

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

| 值 | 说明 |
|----|------|
| `Single` | 写入寄存器，立即输出静态电压 |
| `Finite` | 输出 SamplesToUpdate 点后停止 |
| `ContinuousWrapping` | 循环输出固定缓冲区（ContinuousWrapping 不可实时更新） |
| `ContinuousNoWrapping` | 持续消耗缓冲区数据，可实时追加（流式） |

## ContinuousWrapping vs NoWrapping

| | Wrapping | NoWrapping |
|-|----------|------------|
| 写入时机 | Start 前一次性写入 | 可在 Start 后持续追加 |
| 适用场景 | 固定波形循环输出 | 实时可变波形 |
| 注意 | 缓冲区耗尽后循环 | 缓冲区耗尽报 BufferDataDownflow |

---

## JYUSB1601CITask — 计数器输入任务

### 构造函数

```csharp
new JYUSB1601CITask(int channel)                    // 按槽位
new JYUSB1601CITask(string boardName, int channel)  // channel: 0 或 1
```

### CIType 枚举与对应配置

| CIType | 配置对象 | 说明 |
|--------|----------|------|
| `EdgeCounting` | `ciTask.EdgeCounting` | 边沿计数，InitialCount/Direction/Pause |
| `Frequency` | `ciTask.FrequencyMeas` | 频率测量（Hz） |
| `Period` | `ciTask.PeriodMeas` | 周期测量（秒） |
| `Pulse` | `ciTask.PulseMeas` | 脉冲宽度（高/低电平时长，秒） |
| `TwoEdgeSeparation` | `ciTask.TwoEdgeSeparation` | 两边沿分离时间（秒） |
| `QuadEncoder` | `ciTask.QuadEncoder` | 正交编码器（X2/X4） |

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

| 枚举值 | 含义 |
|--------|------|
| `OpenDeviceFailed` | 打开设备失败（未连接/名称错误） |
| `CloseDeviceFailed` | 关闭设备失败 |
| `NoChannelAdded` | 未添加通道 |
| `StartTaskFailed` | 启动 Task 失败 |
| `StopTaskFailed` | 停止 Task 失败 |
| `TaskHasNotStarted` | Task 未启动即读写 |
| `TaskHasStartedCannotPerformTheSetOperation` | 运行中修改参数 |
| `BufferDataOverflow` | 采集缓冲区溢出 |
| `BufferDataDownflow` | 输出缓冲区数据不足 |
| `ReadDataTimeout` | 读取超时 |
| `WriteDataTimeout` | 写入超时 |
| `SampleRateParameterInvalid` | 采样率超限 |
| `ChannelNumberParameterInvalid` | 无效通道号 |
| `ChannelInputRangeParameterInvalid` | 无效输入量程 |
| `TriggerParameterInvalid` | 触发参数无效 |
| `CounterParametersInvalid` | 计数器参数无效 |

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

| 参数 | 值 |
|------|----|
| AI 分辨率 | 16-bit |
| AI SE 通道 | 16 |
| AI 差分通道 | 8 |
| AI 单通道最大采样率 | 250 kSa/s |
| AI N 通道最大采样率 | 250k/N Sa/s |
| AI 输入量程 | ±10V / ±5V / ±2.5V |
| AO 通道 | 2 |
| AO 分辨率 | 16-bit |
| AO 输出范围 | ±10V |
| AO 单通道最大更新率 | 2.86 MSa/s |
| DIO | 8 线输入 + 8 线输出 |
| 计数器 | 2 个 32-bit |
| 接口 | USB |
