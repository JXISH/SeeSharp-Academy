# JYUSB1202 驱动 API 参考

## 命名空间

```csharp
using JYUSB1202;
```

## 驱动路径

| 文件 | 路径 |
|------|------|
| DLL | `C:\SeeSharp\JYTEK\Hardware\DSA\JYUSB1202\Bin\JYUSB1202.dll` |
| XML | `C:\SeeSharp\JYTEK\Hardware\DSA\JYUSB1202\Bin\JYUSB1202.xml` |

---

## JYUSB1202AITask（模拟输入任务）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB1202AITask(int slotNumber)` | 通过槽位号创建（从 0 开始） |
| `JYUSB1202AITask(string boardName)` | 通过设备别名创建 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `AIMode` | 采集模式：Finite / Continuous / Record |
| `SampleRate` | `double` | 每通道采样率 (Sa/s)，写入后读回可获取实际生效值 |
| `SamplesToAcquire` | `int` | 有限模式每通道采集点数；Continuous 模式表示每次读取期望点数 |
| `BufLenInSamples` | `int` | 驱动内部缓冲区大小（点/通道） |
| `AvailableSamples` | `ulong` | 当前可从缓冲区读取的点数（每通道） |
| `TransferedSamples` | `ulong` | 已从缓冲区传出的点数（每通道） |
| `Channels` | `List<AIChannel>` | 已添加通道列表，可调用 `.Clear()` 清空 |
| `SampleClock` | `AISampleClock` | 采样时钟配置 |
| `Trigger` | `AITrigger` | 触发配置对象 |
| `Record` | `AIRecord` | 录制配置对象 |
| `SignalExport` | `AISignalExport` | 信号导出配置 |
| `Device` | `JYUSB1202Device` | 底层设备对象 |

### 通道管理方法

```csharp
// 添加单通道（chnId=-1 添加全部）
void AddChannel(int chnId, double rangeLow, double rangeHigh,
    AITerminal terminalCfg, AICoupling couplingCfg, bool enableIEPE);

// 添加多通道（统一参数）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh,
    AITerminal terminalCfg, AICoupling couplingCfg, bool enableIEPE);

// 添加多通道（各自参数）
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh,
    AITerminal[] terminalCfg, AICoupling[] couplingCfg, bool[] enableIEPE);

// 删除通道（chnId=-1 删除全部）
void RemoveChannel(int chnId);
```

### 任务控制方法

```csharp
void Start();                         // 启动采集
void Stop();                          // 停止采集
void SendSoftwareTrigger();           // 发送软件触发
void WaitUntilDone(int timeout);      // 等待有限模式完成，timeout=-1 无限等待
```

### 读取工程值（电压）

```csharp
// 单通道
void ReadData(ref double[] buf, int samplesPerChannel, int timeout);
void ReadData(ref double[] buf, int timeout);

// 多通道（列优先：每列 = 一个通道）
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
void ReadData(ref double[,] buf, int timeout);

// 通用指针（非托管互操作）
void ReadData(IntPtr buf, int samplesPerChannel, int timeout);
```

### 读取原始值（16 位整数）

```csharp
void ReadRawData(ref short[] buf, int samplesPerChannel, int timeout);
void ReadRawData(ref short[,] buf, int samplesPerChannel, int timeout);
void ReadRawData(IntPtr buf, int samplesPerChannel, int timeout);
```

### 原始码值换算

```csharp
// Start() 之后调用获取换算系数
ScalingCoefficients sc = aiTask.GetScalingCoefficients();
double voltage = rawValue * sc.Gain + sc.Offset;

// 批量转换
void ScaleData(int[] rawData, ref double[] scaledData);
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `ScalingCoefficients.Gain` | `double` | 增益 |
| `ScalingCoefficients.Offset` | `double` | 偏置 |

### 录制相关方法

```csharp
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout);
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout);
void GetRecordPreviewData(ref double[,] buf, int timeout);
void GetRecordStatus(out double recordedLength, out bool recordDone);
```

---

## AIChannel

| 属性 | 类型 | 说明 |
|------|------|------|
| `ChannelID` | `int` | 通道编号（0-3） |
| `RangeLow` | `double` | 量程下限（V） |
| `RangeHigh` | `double` | 量程上限（V） |
| `Terminal` | `AITerminal` | 终端模式 |
| `Coupling` | `AICoupling` | 耦合方式 |
| `EnableIEPE` | `bool` | 是否启用 IEPE 激励 |

---

## 配置对象

### AITrigger

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AITriggerType` | 触发类型 |
| `Digital` | `AIDigitalTrigger` | 数字触发子对象 |

### AIDigitalTrigger

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIDigitalTriggerSource` | 触发源（DIO_0~DIO_3） |
| `Edge` | `AIDigitalTriggerEdge` | 触发边沿/电平 |

### AISampleClock

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AISampleClockSource` | 时钟源（Internal/External） |
| `External` | `AIExternalSampleClock` | 外部时钟子对象 |

### AIExternalSampleClock

| 属性 | 类型 | 说明 |
|------|------|------|
| `ExpectedRate` | `double` | 期望的外部时钟速率（Hz） |

### AISignalExport

| 方法 | 说明 |
|------|------|
| `Add(AISignalExportSource source, SignalExportDestination destination)` | 导出信号到指定 DIO |
| `Add(AISignalExportSource source, List<SignalExportDestination> destinations)` | 导出到多个目标 |
| `Clear(SignalExportDestination destination)` | 清除指定目标 |
| `ClearAll()` | 清除全部 |

### AIRecord

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 有效 |

---

## JYUSB1202CITask（计数器输入任务）

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB1202CITask()` | 默认创建 |
| `JYUSB1202CITask(string cardName)` | 使用板卡名称创建 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
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
| `ReadSinglePoint(out double value, int timeout)` | 读取单点值（频率/周期等） |
| `ReadSinglePoint(out double value1, out double value2, int timeout)` | 读取双值（正交编码器） |

### EdgeCounting 配置

| 属性 | 类型 | 说明 |
|------|------|------|
| `InitialCount` | `uint` | 初始计数值 |
| `Direction` | `CountDirection` | 计数方向（Up/Down） |
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

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB1202COTask()` | 默认创建 |
| `JYUSB1202COTask(string cardName)` | 使用板卡名称创建 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Pause` | `PauseTrigger` | 暂停触发配置 |
| `IdleState` | `COIdleState` | 空闲电平 |
| `Device` | `JYUSB1202Device` | 设备实例 |

### 方法

| 方法 | 说明 |
|------|------|
| `WriteSinglePoint(COPulse pulse)` | 写入脉冲配置并输出 |
| `Start()` | 启动脉冲输出 |
| `Stop()` | 停止脉冲输出 |
| `WaitUntilDone(int timeout)` | 等待输出完成 |

### COPulse

```csharp
COPulse pulse = new COPulse(COPulseType type, double param1, double param2, int count);
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `Count` | `int` | 脉冲个数 |
| `IsCountUnlimited` | `bool` | 是否无限脉冲输出 |

---

## JYUSB1202DITask（数字输入任务）

| 方法 | 说明 |
|------|------|
| `JYUSB1202DITask()` / `JYUSB1202DITask(string cardName)` | 构造 |
| `AddChannel(int lineNum)` / `AddChannel(int[] lineNums)` | 添加 DIO 线 |
| `RemoveChannel(int lineNum)` | 移除 DIO 线 |
| `Start()` / `Stop()` | 启停任务 |
| `ReadSinglePoint(out bool value, int timeout)` | 读单线状态 |
| `ReadSinglePoint(out bool[] values)` | 读所有已添加线状态 |

---

## JYUSB1202DOTask（数字输出任务）

| 方法 | 说明 |
|------|------|
| `JYUSB1202DOTask()` / `JYUSB1202DOTask(string cardName)` | 构造 |
| `AddChannel(int lineNum)` / `AddChannel(int[] lineNums)` | 添加 DIO 线 |
| `RemoveChannel(int lineNum)` | 移除 DIO 线 |
| `Start()` / `Stop()` | 启停任务 |
| `WriteSinglePoint(bool value, int timeout)` | 输出单点电平 |
| `WriteSinglePoint(bool[] values)` | 输出多线电平 |

---

## JYUSB1202Device

```csharp
// 静态方法：不打开设备即可查询能力
var cap = JYUSB1202Device.GetCapability(slotNumber);  // 或 GetCapability(deviceName)
int maxChannels = cap.AI.NumberOfChannels;  // 4
double maxRate  = cap.AI.MaxSampleRate;     // 256000 (P7) / 25600 (P3)
double minRate  = cap.AI.MinSampleRate;

// 获取实例
JYUSB1202Device dev = JYUSB1202Device.GetInstance(0);
dev.Release();  // 释放资源
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `AIBufferSize` | `uint` | AI 缓冲区大小 |
| `IsAISync` | `bool` | AI 同步状态标志（多卡同步） |
| `Capability` | `DeviceCapability` | 设备能力 |
| `Handle` | `IntPtr` | 设备句柄 |

---

## JYUSB1202Miscellaneous（校准控制）

```csharp
JYUSB1202Device device = JYUSB1202Device.GetInstance(0);

// AI 校准
bool aiCal = JYUSB1202Miscellaneous.GetAICalibrationState(device);
JYUSB1202Miscellaneous.SetAICalibrationState(device, true);

// AO 校准
bool aoCal = JYUSB1202Miscellaneous.GetAOCalibrationState(device);
JYUSB1202Miscellaneous.SetAOCalibrationState(device, false);
```

| 属性（DeviceMiscellaneousParam） | 类型 | 说明 |
|------|------|------|
| `Device` | `JYUSB1202Device` | 目标设备 |
| `DisableAICalibration` | `bool` | 禁用 AI 校准标志 |
| `DisableAOCalibration` | `bool` | 禁用 AO 校准标志 |

---

## 枚举类型

### AIMode
`Finite` | `Continuous` | `Record`

### AITerminal
`Differential`（差分，默认） | `PSEUDIFF`（伪差分）

### AICoupling
`DC`（直流，默认） | `AC`（交流）

### AITriggerType
`Immediate`（立即，默认） | `Digital`（数字） | `Soft`（软件）

### AIDigitalTriggerSource
`DIO_0` | `DIO_1` | `DIO_2` | `DIO_3`

### AIDigitalTriggerEdge
`Rising` | `Falling` | `HighLevel` | `LowLevel`

### AISampleClockSource
`Internal`（默认） | `External`

### AISignalExportSource
`StartTrig` | `SampleClock`

### SignalExportDestination
`DIO_0` | `DIO_1` | `DIO_2` | `DIO_3`

### RecordMode
`Finite`（有限，录制指定时长后停止） | `Infinite`（无限，需手动 Stop）

### FileFormat
`Bin`（64-bit double 二进制）

### CIType
`EdgeCounting` | `Period` | `Frequency` | `Pulse` | `TwoEdgeSeparation` | `QuadEncoder`

### CI 相关枚举
- `EdgeType`：`Rising` / `Falling` / `Any`
- `CountDirection`：`Up` / `Down`
- `LevelPolarity`：`LowLevel` / `HighLevel` / `None`
- `EdgeCntOunEvtIdleState`：`LowLevel` / `HighLevel`
- `QuadEncodingType`：`X2` / `X4`

### COPulseType
`DutyCycleFrequency`（频率+占空比） | `HighLowTime`（高/低电平时间，秒） | `HighLowTick`（高/低电平 Tick）

### COIdleState
`HighLevel` | `LowLevel`

---

## JYDriverException

```csharp
catch (JYDriverException ex)
{
    Console.WriteLine(ex.ErrorCode);      // int 错误码
    Console.WriteLine(ex.ExceptionName);  // 异常名称字符串
    Console.WriteLine(ex.Message);        // 完整消息
}
```

### 常见错误码（JYDriverExceptionPublic）

| 枚举值 | 含义 |
|--------|------|
| `OpenDeviceFailed` | 打开设备失败 |
| `StartTaskFailed` | 启动任务失败 |
| `StopTaskFailed` | 停止任务失败 |
| `ReadDataFailed` | 读取数据失败 |
| `ReadDataTimeout` | 读取超时 |
| `BufferDataOverflow` | 缓冲区溢出 |
| `NoChannelAdded` | 未添加通道 |
| `SampleRateParameterInvalid` | 采样率参数无效 |
| `ChannelInputRangeParameterInvalid` | 量程参数无效 |
| `TriggerParameterInvalid` | 触发参数无效 |
| `TaskHasStartedCannotPerformTheSetOperation` | 任务运行中不能修改配置 |
