# JY6311 API 参考手册

## 目录
- [JY6311AITask 类](#jy6311aitask-类)
- [JY6311DITask 类](#jy6311ditask-类)
- [JY6311DOTask 类](#jy6311dotask-类)
- [枚举定义](#枚举定义)
- [异常处理](#异常处理)

---

## JY6311AITask 类

模拟输入任务类，用于PT100/PT1000温度测量、电阻测量和电压测量。

### 构造函数

| 构造函数 | 说明 |
|---------|------|
| `JY6311AITask(int boardNum)` | 通过板卡号创建任务，boardNum=0表示第一张卡 |
| `JY6311AITask(string boardName)` | 通过板卡名称创建任务，名称可在设备管理器中设置 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Device` | `JY6311Device` | 获取当前任务使用的设备实例 |
| `Mode` | `AIMode` | 采集模式：Single/Finite/Continuous |
| `SampleRate` | `double` | 每通道采样率（Sa/s），范围0.275~800 |
| `ActualSampleRate` | `double` | 实际生效的采样率（只读） |
| `SamplesToAcquire` | `int` | 每通道采集点数（仅Finite模式有效） |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数（每通道） |
| `TransferedSamples` | `ulong` | 已从缓冲区读取的样本数（每通道） |
| `PowerLineFrequency` | `PowerLineFrequency` | 工频抑制设置：_50Hz/_60Hz |
| `Channels` | `List<AIChannel>` | 通道列表 |
| `Trigger` | `AITrigger` | 触发配置对象 |
| `SampleClock` | `AISampleClock` | 采样时钟配置对象 |
| `SignalExport` | `AISignalExport` | 信号导出配置对象 |

### 方法

#### AddChannel - 添加通道

**温度测量模式：**
```csharp
// 单通道温度测量
public void AddChannel(int chID, RTDType rtdType, RTDTerminal rtdTerminal, RTDTCRType rtdTCRType)

// 多通道统一配置
public void AddChannel(int[] chIDs, RTDType rtdType, RTDTerminal rtdTerminal, RTDTCRType rtdTCRType)

// 多通道独立配置
public void AddChannel(int[] chIDs, RTDType[] rtdTypes, RTDTerminal[] rtdTerminals, RTDTCRType[] rtdTCRTypes)
```

| 参数 | 说明 |
|------|------|
| `chID` | 通道号（0~15），-1表示所有通道 |
| `rtdType` | RTD类型：PT100/PT1000 |
| `rtdTerminal` | 接线方式：TwoWire/ThreeWire/FourWire |
| `rtdTCRType` | 温度系数类型 |

**电阻测量模式：**
```csharp
public void AddChannel(int chID, RTDType rtdType, RTDTerminal rtdTerminal)
public void AddChannel(int[] chIDs, RTDType rtdType, RTDTerminal rtdTerminal)
public void AddChannel(int[] chIDs, RTDType[] rtdTypes, RTDTerminal[] rtdTerminals)
```

**电压测量模式：**
```csharp
public void AddChannel(int chID, double voltageRangeLow, double voltageRangeHigh)
public void AddChannel(int[] chIDs, double voltageRangeLow, double voltageRangeHigh)
public void AddChannel(int[] chIDs, double[] voltageRangeLows, double[] voltageRangeHighs)
```

| 参数 | 说明 |
|------|------|
| `voltageRangeLow` | 电压下限（V）：-1.25/-0.625/-0.3125/-0.15625/-0.078125/-0.0390625 |
| `voltageRangeHigh` | 电压上限（V）：1.25/0.625/0.3125/0.15625/0.078125/0.0390625 |

#### RemoveChannel - 移除通道
```csharp
public void RemoveChannel(int channelID)
```
- `channelID`: 要移除的通道号，-1表示移除所有通道

#### Start - 启动任务
```csharp
public void Start()
```
启动AI采集任务。根据触发配置，可能立即开始采集或等待触发信号。

#### Stop - 停止任务
```csharp
public void Stop()
```
停止AI采集任务，释放硬件资源。

#### SendSoftwareTrigger - 发送软件触发
```csharp
public void SendSoftwareTrigger()
```
发送软件触发信号。仅在触发类型为Soft时有效。

#### WaitUntilDone - 等待完成
```csharp
public bool WaitUntilDone(int timeout)
```
等待有限点采集完成（仅Finite模式有效）。

| 参数 | 说明 |
|------|------|
| `timeout` | 超时时间（毫秒），-1表示无限等待 |

| 返回值 | 说明 |
|--------|------|
| `true` | 采集已完成 |
| `false` | 超时 |

#### ReadSinglePoint - 单点读取

**单通道读取：**
```csharp
public void ReadSinglePoint(ref double readValue, int channelID)
```

**多通道读取：**
```csharp
public void ReadSinglePoint(ref double[] buf)
public void ReadSinglePoint(IntPtr buf)
```

| 参数 | 说明 |
|------|------|
| `readValue` | 读取的值（温度℃/电阻Ω/电压V） |
| `channelID` | 通道号 |
| `buf` | 数据缓冲区，长度≥通道数 |

#### ReadData - 缓冲读取

**单通道读取：**
```csharp
public void ReadData(ref double[] buf, int timeout)
public void ReadData(ref double[] buf, int samplesPerChannel, int timeout)
public void ReadData(IntPtr buf, int samplesPerChannel, int timeout)
```

**多通道读取（每通道一列）：**
```csharp
public void ReadData(ref double[,] buf, int timeout)
public void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)
public void ReadData(IntPtr buf, int samplesPerChannel, int timeout)
```

| 参数 | 说明 |
|------|------|
| `buf` | 数据缓冲区 |
| `samplesPerChannel` | 每通道读取点数 |
| `timeout` | 超时时间（毫秒），-1表示无限等待 |

**缓冲区维度说明：**
- 单通道：`double[样本数]`
- 多通道：`double[样本数, 通道数]` - 每列代表一个通道

#### ReadRawData - 读取原始数据
```csharp
public void ReadRawData(ref int[] buf, int timeout)
public void ReadRawData(ref int[] buf, int samplesPerChannel, int timeout)
public void ReadRawData(ref int[,] buf, int timeout)
public void ReadRawData(ref int[,] buf, int samplesPerChannel, int timeout)
public void ReadRawData(IntPtr buf, int samplesPerChannel, int timeout)
```
读取原始ADC数据（未经换算的整数值）。

---

## JY6311DITask 类

数字输入任务类，用于读取PFI端口状态。

### 构造函数

| 构造函数 | 说明 |
|---------|------|
| `JY6311DITask(int slotNum)` | 通过槽位号创建任务 |
| `JY6311DITask(string boardName)` | 通过板卡名称创建任务 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Device` | `JY6311Device` | 设备实例 |
| `Channels` | `List<DIChannel>` | DI通道列表 |

### 方法

#### AddChannel - 添加通道
```csharp
public void AddChannel(int lineID)
public void AddChannel(int[] linesID)
```

| 参数 | 说明 |
|------|------|
| `lineID` | 端口号（0~2对应PFI0~PFI2） |
| `linesID` | 端口号数组 |

#### RemoveChannel - 移除通道
```csharp
public void RemoveChannel(int lineID)
```

#### Start - 启动任务
```csharp
public void Start()
```

#### Stop - 停止任务
```csharp
public void Stop()
```

#### ReadSinglePoint - 读取数字输入
```csharp
// 读取单路
public void ReadSinglePoint(ref bool readValue, int lineNum)

// 读取所有路
public void ReadSinglePoint(ref bool[] readValues)
```

| 参数 | 说明 |
|------|------|
| `readValue` | 读取的电平值（true=高电平，false=低电平） |
| `lineNum` | 端口号 |
| `readValues` | 缓冲区，长度≥通道数 |

---

## JY6311DOTask 类

数字输出任务类，用于控制PFI端口输出。

### 构造函数

| 构造函数 | 说明 |
|---------|------|
| `JY6311DOTask(int slotNum)` | 通过槽位号创建任务 |
| `JY6311DOTask(string boardName)` | 通过板卡名称创建任务 |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Device` | `JY6311Device` | 设备实例 |
| `Channels` | `List<DOChannel>` | DO通道列表 |

### 方法

#### AddChannel - 添加通道
```csharp
public void AddChannel(int lineID)
public void AddChannel(int[] linesID)
```

#### RemoveChannel - 移除通道
```csharp
public void RemoveChannel(int lineID)
```

#### Start - 启动任务
```csharp
public void Start()
```

#### Stop - 停止任务
```csharp
public void Stop()
```

#### WriteSinglePoint - 写入数字输出
```csharp
// 写入单路
public void WriteSinglePoint(bool writeValue, int lineID)

// 写入所有路
public void WriteSinglePoint(bool[] writeValues)
```

| 参数 | 说明 |
|------|------|
| `writeValue` | 输出电平（true=高电平，false=低电平） |
| `lineID` | 端口号 |
| `writeValues` | 输出值数组，长度≥通道数 |

---

## 配置类详解

### AITrigger 类

触发配置类，通过 `aiTask.Trigger` 访问。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AITriggerType` | 触发类型：Immediate/Digital/Soft |
| `Mode` | `AITriggerMode` | 触发模式：Start/Reference |
| `Digital` | `AIDigitalTrigger` | 数字触发配置 |
| `ReTriggerCount` | `int` | 重触发次数（0或1=单次，-1=无限次） |
| `PreTriggerSamples` | `uint` | 预触发样本数（仅Reference模式） |

### AIDigitalTrigger 类

数字触发配置类，通过 `aiTask.Trigger.Digital` 访问。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIDigitalTriggerSource` | 触发源：PFI0~PFI2、PXI_Trig0~7 |
| `Edge` | `AIDigitalTriggerEdge` | 触发边沿：Rising/Falling |

### AISampleClock 类

采样时钟配置类，通过 `aiTask.SampleClock` 访问。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AISampleClockSource` | 时钟源：Internal/External |
| `External` | `AIExternalSampleClock` | 外部时钟配置 |

### AIExternalSampleClock 类

外部采样时钟配置类，通过 `aiTask.SampleClock.External` 访问。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ClockTerminal` | 外部时钟输入端子 |
| `ExpectedRate` | `double` | 期望的外部时钟频率 |

### AISignalExport 类

信号导出配置类，通过 `aiTask.SignalExport` 访问。

| 方法 | 说明 |
|------|------|
| `Add(AISignalExportSource source, SignalExportDestination destination)` | 导出信号到指定端子 |
| `Add(AISignalExportSource source, List<SignalExportDestination> destinations)` | 导出信号到多个端子 |
| `Clear(SignalExportDestination destination)` | 清除指定端子的信号导出 |
| `ClearAll()` | 清除所有信号导出 |

---

## 枚举定义

### AIMode - 采集模式
```csharp
public enum AIMode
{
    Single,      // 单点模式：按需读取当前值
    Finite,      // 有限点模式：采集固定数量样本
    Continuous   // 连续模式：持续采集直到停止
}
```

### RTDType - RTD类型
```csharp
public enum RTDType
{
    PT100,       // PT100热电阻，量程0~400Ω
    PT1000       // PT1000热电阻，量程0~1600Ω
}
```

### RTDTerminal - 接线方式
```csharp
public enum RTDTerminal
{
    TwoWire,     // 2线制
    ThreeWire,   // 3线制（推荐）
    FourWire     // 4线制（精度最高）
}
```

### RTDTCRType - 温度系数类型
```csharp
public enum RTDTCRType
{
    Pt100_TCR3851,  // TCR=0.003851/℃，R0=100Ω（最常用，IEC 60751标准）
    Pt100_TCR3916,  // TCR=0.003916/℃，R0=100Ω
    Pt100_TCR3920,  // TCR=0.003920/℃，R0=100Ω
    Pt100_TCR3911,  // TCR=0.003911/℃，R0=100Ω
    Pt100_TCR3928,  // TCR=0.003928/℃，R0=100Ω
    Pt1000_TCR3851, // Pt1000 TCR=0.003851/℃
    Pt1000_TCR3750  // Pt1000 TCR=0.003750/℃
}
```

### MeasureDataType - 测量数据类型
```csharp
public enum MeasureDataType
{
    Resistance,  // 电阻值（Ω）
    Temperature, // 温度值（℃）
    Voltage      // 电压值（V）
}
```

### AITriggerType - 触发类型
```csharp
public enum AITriggerType
{
    Immediate,   // 立即触发：Start()后立即开始采集
    Digital,     // 数字触发：等待外部数字信号触发
    Soft         // 软件触发：等待SendSoftwareTrigger()调用
}
```

### AITriggerMode - 触发模式
```csharp
public enum AITriggerMode
{
    Start,       // 开始触发：触发后开始采集
    Reference    // 参考触发：触发前后都采集数据（需设置PreTriggerSamples）
}
```

### AIDigitalTriggerEdge - 数字触发边沿
```csharp
public enum AIDigitalTriggerEdge
{
    Rising,      // 上升沿触发
    Falling      // 下降沿触发
}
```

### AIDigitalTriggerSource - 数字触发源
```csharp
public enum AIDigitalTriggerSource
{
    PFI0,        // PFI 0端子
    PFI1,        // PFI 1端子
    PFI2,        // PFI 2端子
    PXI_Trig0,   // PXI触发总线0
    PXI_Trig1,   // PXI触发总线1
    PXI_Trig2,   // PXI触发总线2
    PXI_Trig3,   // PXI触发总线3
    PXI_Trig4,   // PXI触发总线4
    PXI_Trig5,   // PXI触发总线5
    PXI_Trig6,   // PXI触发总线6
    PXI_Trig7    // PXI触发总线7
}
```

### AISampleClockSource - 采样时钟源
```csharp
public enum AISampleClockSource
{
    Internal,    // 内部时钟（默认）
    External     // 外部时钟
}
```

### ClockTerminal - 时钟端子
```csharp
public enum ClockTerminal
{
    PFI0,           // PFI 0
    PFI1,           // PFI 1
    PFI2,           // PFI 2
    PXI_Trig0,      // PXI触发总线0
    PXI_Trig1,      // PXI触发总线1
    PXI_Trig2,      // PXI触发总线2
    PXI_Trig3,      // PXI触发总线3
    PXI_Trig4,      // PXI触发总线4
    PXI_Trig5,      // PXI触发总线5
    PXI_Trig6,      // PXI触发总线6
    PXI_Trig7,      // PXI触发总线7
    AI_SampleClock  // AI采样时钟输出
}
```

### AISignalExportSource - 信号导出源
```csharp
public enum AISignalExportSource
{
    StartTrig,      // 开始触发信号
    ReferenceTrig,  // 参考触发信号
    SampleClock     // 采样时钟信号
}
```

### SignalExportDestination - 信号导出目标
```csharp
public enum SignalExportDestination
{
    PFI0,        // PFI 0端子
    PFI1,        // PFI 1端子
    PFI2,        // PFI 2端子
    PXI_Trig0,   // PXI触发总线0
    PXI_Trig1,   // PXI触发总线1
    PXI_Trig2,   // PXI触发总线2
    PXI_Trig3,   // PXI触发总线3
    PXI_Trig4,   // PXI触发总线4
    PXI_Trig5,   // PXI触发总线5
    PXI_Trig6,   // PXI触发总线6
    PXI_Trig7    // PXI触发总线7
}
```

### PowerLineFrequency - 工频抑制
```csharp
public enum PowerLineFrequency
{
    _50Hz,       // 50Hz工频抑制
    _60Hz        // 60Hz工频抑制
}
```

---

## 异常处理

### JYDriverException 类

驱动异常类，所有驱动操作失败时抛出。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ErrorCode` | `int` | 错误代码 |
| `ExceptionName` | `string` | 异常名称 |
| `FollowingException` | `JYDriverException` | 链式异常指针 |

### JYDriverExceptionPublic 枚举

常用错误代码定义。

| 错误代码 | 说明 |
|---------|------|
| `Unknown` | 未知异常 |
| `InterError` | 内部错误 |
| `Timeout` | 超时 |
| `ErrorParam` | 参数错误 |
| `CannotCall` | 当前配置或状态下不允许调用该方法 |
| `UserBufferError` | 用户缓冲区错误 |
| `BufferOverflow` | 缓冲区溢出（缓冲区设置太小） |
| `BufferDownflow` | 缓冲区下溢出（本地缓冲区数据不足） |
| `UserBufferLenError` | 用户缓冲区大小错误 |
| `HardwareResourceReserved` | 硬件资源已被占用 |
| `InitializeFailed` | 初始化失败 |
| `TriggerStateNotMatch` | 触发状态不匹配 |
| `PLLLockFailed` | 锁相环未能成功锁住时钟源 |
| `NotSupportOperationForCurrentDevice` | 当前设备不支持该操作 |
| `StartTaskFailed` | 开始任务失败 |
| `StopTaskFailed` | 停止任务失败 |
| `SignalExportDestinationInvalid` | 信号导出端口无效 |
| `HardwareResourceIsReserved` | 硬件资源被占用 |
| `PerformCalibrationFailed` | 执行校准失败 |
| `ResetCalibratiobFailed` | 重置校准失败 |

### 异常处理示例
```csharp
try
{
    aiTask.Start();
}
catch (JYDriverException ex)
{
    switch (ex.ExceptionName)
    {
        case nameof(JYDriverExceptionPublic.HardwareResourceReserved):
            Console.WriteLine("硬件资源已被占用，请检查是否有其他程序正在使用该设备");
            break;
        case nameof(JYDriverExceptionPublic.ErrorParam):
            Console.WriteLine($"参数错误: {ex.Message}");
            break;
        case nameof(JYDriverExceptionPublic.Timeout):
            Console.WriteLine("操作超时");
            break;
        default:
            Console.WriteLine($"驱动错误: {ex.Message}");
            break;
    }
}
```

---

## 数据类型与量程

### 温度测量

| RTD类型 | 温度范围 | 分辨率 |
|---------|---------|--------|
| PT100 | -200℃~+850℃ | 0.01℃ |
| PT1000 | -200℃~+850℃ | 0.01℃ |

### 电阻测量

| RTD类型 | 电阻范围 | 分辨率 |
|---------|---------|--------|
| PT100 | 0~400Ω | 0.001Ω |
| PT1000 | 0~1600Ω | 0.01Ω |

### 电压测量

| 量程 | 范围 | 分辨率 |
|------|------|--------|
| ±1.25V | -1.25V ~ +1.25V | 38μV |
| ±0.625V | -0.625V ~ +0.625V | 19μV |
| ±0.3125V | -0.3125V ~ +0.3125V | 9.5μV |
| ±0.15625V | -0.15625V ~ +0.15625V | 4.75μV |
| ±0.078125V | -0.078125V ~ +0.078125V | 2.375μV |
| ±0.0390625V | -0.0390625V ~ +0.0390625V | 1.1875μV |

---

## 采样率限制

### 单通道最大采样率
- 电压模式：800 Sa/s
- 电阻/温度模式：取决于量程和滤波设置

### 多通道采样率计算
```
总采样率 = 每通道采样率 × 通道数
```

实际采样率可能因硬件限制而自动调整，可通过 `ActualSampleRate` 属性获取。

### 工频抑制与采样率关系
- 启用50Hz/60Hz工频抑制时，总采样率必须 ≤ 40 Sa/s
- 建议工频抑制采样率：10 Sa/s、20 Sa/s、40 Sa/s
