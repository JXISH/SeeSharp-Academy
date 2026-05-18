# JYUSB-1202 AI 驱动 API 参考

## 命名空间

```csharp
using JYUSB1202;
```

---

## JYUSB1202AITask — 核心任务类

### 构造函数

| 签名 | 说明 |
|------|------|
| `JYUSB1202AITask(int slotNumber)` | 通过插槽号创建（从 0 开始） |
| `JYUSB1202AITask(string boardName)` | 通过设备别名创建（在设备管理器中设置） |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `AIMode` | 采集模式：`Finite` / `Continuous` / `Record` |
| `SampleRate` | `double` | 每通道采样率 (Sa/s)，默认 10000；写入后读回可获取实际生效值 |
| `SamplesToAcquire` | `int` | 有限模式每通道采集点数；Continuous 模式表示每次读取的期望点数 |
| `BufLenInSamples` | `int` | 驱动内部缓冲区大小（点/通道），`SamplesToAcquire ≤ BufLenInSamples` |
| `AvailableSamples` | `ulong` | 当前可从缓冲区读取的点数（每通道） |
| `TransferedSamples` | `ulong` | 已从本地缓冲区传出的点数（每通道） |
| `Channels` | `List<AIChannel>` | 已添加的通道列表，可调用 `.Clear()` 清空 |
| `SampleClock` | `AISampleClock` | 采样时钟配置（内部 / 外部） |
| `Trigger` | `AITrigger` | 触发配置对象 |
| `Record` | `AIRecord` | 录制配置对象（Record 模式使用） |
| `SignalExport` | `AISignalExport` | 信号输出配置 |
| `Device` | `JYUSB1202Device` | 底层设备对象 |

### 方法

#### 通道管理

```csharp
// 添加单通道
void AddChannel(int chnId, double rangeLow, double rangeHigh,
    AITerminal terminalCfg, AICoupling couplingCfg, bool enableIEPE);
// chnId = -1 时添加全部通道

// 添加多通道（统一参数）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh,
    AITerminal terminalCfg, AICoupling couplingCfg, bool enableIEPE);

// 添加多通道（各自参数）
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh,
    AITerminal[] terminalCfg, AICoupling[] couplingCfg, bool[] enableIEPE);

// 删除通道（chnId=-1 删除全部）
void RemoveChannel(int chnId);
```

#### 任务控制

```csharp
void Start();                         // 启动采集
void Stop();                          // 停止采集
void SendSoftwareTrigger();           // 发送软件触发
void WaitUntilDone(int timeout);      // 等待有限模式完成，timeout=-1 无限等待
```

#### 读取工程值（电压）

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

#### 读取原始值（16 位整数）

```csharp
void ReadRawData(ref short[] buf, int samplesPerChannel, int timeout);
void ReadRawData(ref short[,] buf, int samplesPerChannel, int timeout);
void ReadRawData(IntPtr buf, int samplesPerChannel, int timeout);
```

#### 录制预览

```csharp
// Record 模式流式过程中预览数据
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout);
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout);

// 获取录制状态
void GetRecordStatus(out double recordedLength, out bool recordDone);
```

---

## AIChannel — 通道参数

| 属性 | 类型 | 说明 |
|------|------|------|
| `ChannelID` | `int` | 通道编号（0-3） |
| `RangeLow` | `double` | 量程下限（V） |
| `RangeHigh` | `double` | 量程上限（V） |
| `Terminal` | `AITerminal` | 终端模式 |
| `Coupling` | `AICoupling` | 耦合方式 |
| `EnableIEPE` | `bool` | 是否启用 IEPE 激励 |

---

## 枚举类型

### AIMode

| 值 | 说明 |
|----|------|
| `Finite` | 有限采集，到达 `SamplesToAcquire` 后停止 |
| `Continuous` | 连续循环采集 |
| `Record` | 流式录制到文件 |

### AITerminal

| 值 | 说明 |
|----|------|
| `Differential` | 差分输入（默认） |
| `PSEUDIFF` | 伪差分输入 |

### AICoupling

| 值 | 说明 |
|----|------|
| `DC` | 直流耦合（默认） |
| `AC` | 交流耦合 |

### AITriggerType

| 值 | 说明 |
|----|------|
| `Immediate` | 立即触发（默认） |
| `Digital` | 数字触发 |
| `Soft` | 软件触发 |

### AIDigitalTriggerEdge

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿 |
| `Falling` | 下降沿 |
| `HighLevel` | 高电平 |
| `LowLevel` | 低电平 |

### AIDigitalTriggerSource

DIO_0 / DIO_1 / DIO_2 / DIO_3

### AISampleClockSource

| 值 | 说明 |
|----|------|
| `Internal` | 内部时钟（默认） |
| `External` | 外部时钟 |

---

## AITrigger — 触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;
```

---

## AISampleClock — 采样时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 10000; // Hz
```

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath   = @"C:\data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode       = RecordMode.Finite;   // Finite / Infinite
aiTask.Record.Length     = 10.0;                // 秒，仅 Finite 有效
```

---

## AISignalExport — 信号输出

```csharp
// 将 AI 采样时钟输出到 DIO_0
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.DIO_0);

// 清除指定输出
aiTask.SignalExport.Clear(SignalExportDestination.DIO_0);

// 清除全部
aiTask.SignalExport.ClearAll();
```

---

## JYUSB1202Device — 设备对象

```csharp
// 不打开设备即可查询能力
var cap = JYUSB1202Device.GetCapability("DevName");
int maxChannels   = cap.AI.NumberOfChannels;   // AI 通道总数（4 通道）
double maxRate    = cap.AI.MaxSampleRate;       // 最大采样率 (Sa/s)
double minRate    = cap.AI.MinSampleRate;       // 最小采样率 (Sa/s)

// 通过 AITask 访问设备
JYUSB1202Device dev = aiTask.Device;
dev.Release();   // 释放资源（析构函数会自动调用）
```

---

## JYUSB1202Miscellaneous

```csharp
// 启用/禁用 AI 校准（推荐在 Start 前调用）
JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
```

---

## JYDriverException — 异常

```csharp
catch (JYDriverException ex)
{
    Console.WriteLine(ex.ErrorCode);      // int 错误码
    Console.WriteLine(ex.ExceptionName);  // 异常名称字符串
    Console.WriteLine(ex.Message);        // 完整消息
}
```

常见错误码枚举 (`JYDriverExceptionPublic`)：

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
