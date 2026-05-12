# JY6312 驱动 API 参考

> 命名空间根：`JY6312`。本文档内容来源于 `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.XML`
> 与 `JY-6312 Spec and Manual_EN.pdf`，涵盖 JY6312.dll 公开的全部类型、成员与枚举。

---

## 1. 类型索引（Top-level）

| 类型 | 种类 | 作用 |
|------|------|------|
| `JY6312AITask` | class | **唯一** AI 任务类（电压 + 热电偶 复用） |
| `JY6312Device` | class | 板卡设备实例（信息/PFI/常量） |
| `JYDriverException` | class | 驱动统一异常 |
| `AIChannel` / `AIChannelCollection` | class | 通道项与通道集合 |
| `AIChannelAdvanced` | class | 通道高级配置 |
| `AISampleClock` / `AIInternalSampleClock` / `AIExternalSampleClock` | class | 时钟 |
| `AITrigger` / `AIDigitalTrigger` / `AISoftwareTrigger` | class | 触发 |
| `BuildInCJC` / `BuildInCJCAdvanced` | class | 内置冷端补偿 |
| `SignalExportList` | class | 信号导出列表 |
| `PowerLineRejection` | class | 工频抑制 |
| `PFISetting` / `PFIFilter` | class | PFI 端子与滤波 |
| `DeviceInformation` | class | 板卡标识 |
| `Utility.Thermocouple` | static | 热电偶 EMF↔温度换算 |
| 枚举：`AIMode` / `AIRange` / `MeasurementType` / `ThermocoupleType` / `AITriggerType` / `AITriggerMode` / `AIDigitalTriggerEdge` / `AIDigitalTriggerSource` / `AISignalExportSource` / `SignalExportDestination` / `AISampleClockSource` / `ClockTerminal` / `PFITerminal` / `CJSensorConnectionStatus` / `ThermocoupleConnectionStatus` / `PowerLineFrequency` / `ErrorCode` | enum | 见 §8 |

---

## 2. JY6312AITask

AI 任务主类。**所有模拟输入相关功能唯一入口**。

### 构造函数

```csharp
JY6312AITask();                             // 默认 boardNum = 0
JY6312AITask(int boardNum);                 // 按板卡号
JY6312AITask(string boardName);             // 按板卡名（如 "JY6312/0"）
```

### 属性

| 属性 | 类型 | 读/写 | 说明 |
|------|------|-------|------|
| `Channels` | `AIChannelCollection` | R | 已添加通道集合（顺序与 `AddChannel` 一致） |
| `Mode` | `AIMode` | R/W | `Single` / `Finite` / `Continuous` |
| `SampleRate` | `double` | R/W | 每通道采样率，**0.25 ~ 160 Sa/s**。写后再读为实际生效值 |
| `SamplesToAcquire` | `int` | R/W | Finite 模式每通道采样点数 |
| `AvailableSamples` | `ulong` | R | 当前缓冲区可读取的每通道样本数 |
| `SampleClock` | `AISampleClock` | R | 时钟配置入口 |
| `Trigger` | `AITrigger` | R | 触发配置入口 |
| `BuildInCJC` | `BuildInCJC` | R | 内置冷端补偿配置 |
| `PowerLineRejection` | `PowerLineRejection` | R | 工频抑制配置（≤ 8 Sa/s 生效） |
| `SignalExport` | `SignalExportList` | R | 信号导出列表 |
| `AITaskIsDone` | `bool` | R | Finite 模式任务是否完成 |

### 方法

```csharp
// 通道添加
void AddChannel(int channelID, double rangeLow, double rangeHigh);        // 电压模式
void AddChannel(int channelID, ThermocoupleType type);                    // 热电偶模式
void AddChannel(int[] channelIDs, double rangeLow, double rangeHigh);     // 批量电压
void AddChannel(int[] channelIDs, ThermocoupleType type);                 // 批量热电偶
void RemoveChannel(int channelID);
void ClearChannels();

// 启停
void Start();
void Stop();

// 读取（Thermocouple 模式返回温度 ℃，GeneralVoltage 返回电压 V）
void ReadData(ref double[] data, int timeout = -1);                       // 1D 单通道
void ReadData(ref double[,] data, int timeout = -1);                      // 2D 多通道 [samples, channels]
void ReadData(ref double[] data, int samplesToRead, int timeout = -1);
void ReadSinglePoint(ref double[] data);                                  // Single 模式

// 原始数据（EMF + 冷端温度）
void ReadRawData(ref double[,] hjVoltage, ref double[,] cjTemperature, int timeout = -1);

// 软件触发
void SendSoftwareTrigger();

// 自定义冷端温度（必须先 BuildInCJC.Enabled = false）
void SetCJTemperature(int channelID, double temperature);
void SetCJTemperature(double[] temperatures);                             // 与 Channels 顺序对齐

// 开路热电偶检测
Dictionary<int, ThermocoupleConnectionStatus> DetectOpenThermocouple();
```

---

## 3. JY6312Device

板卡设备句柄/常量。

### 静态方法

```csharp
static JY6312Device[] Scan();                  // 扫描所有 JY6312 板卡
static JY6312Device GetInstance(int boardNum); // 按板卡号获取
```

### 实例属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Info` | `DeviceInformation` | 板卡信息（ProductName、SlotNumber、SerialNumber、FWRevision 等） |
| `PFI` | `PFISetting[]` | PFI 端子配置数组（支持 `Enable(ns)`、`Disable()`） |

### 静态常量

| 常量 | 值 | 说明 |
|------|----|------|
| `MaxSampleRate` | 160 | 每通道最大采样率 |
| `MinSampleRate` | 0.25 | 每通道最小采样率 |
| `TotalNumberOfChannels` | 16 | AI 通道数 |
| `MaxSamplesToAcquireSingleChannel` | —— | Finite 模式单通道最大采样点数 |
| `MinSamplesToAcquire` | —— | Finite 模式最小采样点数 |

---

## 4. 通道与高级配置

### AIChannel

| 成员 | 类型 | 说明 |
|------|------|------|
| `ID` | `int` | 通道号 0..15 |
| `MeasurementType` | `MeasurementType` | `Thermocouple` / `GeneralVoltage` |
| `ThermocoupleType` | `ThermocoupleType` | 热电偶类型（`Thermocouple` 模式有效） |
| `Range` | `AIRange` | 电压量程档位 |
| `RangeHigh` / `RangeLow` | `double` | 实际量程上下限（只读，反映 Range 设置） |
| `CustomCJTemperature` | `double` | 自定义冷端温度（℃） |
| `Advanced` | `AIChannelAdvanced` | 高级参数 |

### AIChannelAdvanced

| 成员 | 类型 | 说明 |
|------|------|------|
| `VbiasEnabled` | `bool` | 偏置电压使能；**热电偶测量必须 `true`**（默认） |
| `InputBufferEnabled` | `bool` | 输入缓冲器使能 |

---

## 5. 时钟（AISampleClock）

```
AISampleClock
├── Source        : AISampleClockSource  { Internal, External }
├── Internal      : AIInternalSampleClock
│   └── Rate      : double
└── External      : AIExternalSampleClock
    ├── Terminal      : ClockTerminal  { PFI0, PFI1, PXI_Trig0..7, PXI_Star }
    ├── ExpectedRate  : double          // 期望频率，用于驱动锁相
    └── Edge          : AIDigitalTriggerEdge { Rising, Falling }
```

---

## 6. 触发（AITrigger）

```
AITrigger
├── Type                : AITriggerType  { Immediately, Digital, Software }
├── Mode                : AITriggerMode  { Start, Reference }
├── ReTriggerCount      : int            // 0/1 = 单次，-1 = 无限重复触发
├── PreTriggerSamples   : int            // Reference 模式下的预触发采样点
├── Digital             : AIDigitalTrigger
│   ├── Source : AIDigitalTriggerSource { PFI0/1, PXI_Trig0..7, PXI_Star }
│   └── Edge   : AIDigitalTriggerEdge   { Rising, Falling }
└── Software            : AISoftwareTrigger  // 通过 task.SendSoftwareTrigger() 驱动
```

`AITriggerMode.Reference` 仅在 `AIMode.Finite` 下有效。

---

## 7. 冷端补偿（BuildInCJC）

```
BuildInCJC
├── Enabled          : bool                              // 默认 true；关闭后需 SetCJTemperature
├── SensorStatus     : CJSensorConnectionStatus         // Normal / LostConnect / Closed
├── SensorConnected  : bool                             // TB68CJ 是否已接
├── Advanced         : BuildInCJCAdvanced
│   └── Debouncing   : int                              // 状态去抖动（ms）
```

`CJSensorConnectionStatus` 枚举值：
- `Normal` —— 传感器连接正常
- `LostConnect` —— 曾连接但已断开（需 `Debouncing` 时间确认）
- `Closed` —— 已禁用 CJC 或从未连接

---

## 8. 枚举

### AIMode
| 值 | 含义 |
|----|------|
| `Single` | 单点测量（调 `ReadSinglePoint`） |
| `Finite` | 有限采样（需 `SamplesToAcquire`） |
| `Continuous` | 连续采集 |

### AIRange（电压量程）
| 枚举 | 实际量程 |
|------|----------|
| `_1p25V` | ±1.25 V |
| `_625mV` | ±625 mV |
| `_312p5mV` | ±312.5 mV |
| `_156p2mV` | ±156.25 mV |
| `_78p125mV` | **±78.125 mV**（JY6312 独有） |

### MeasurementType
| 值 | 含义 |
|----|------|
| `Thermocouple` | 热电偶测温（返回 ℃） |
| `GeneralVoltage` | 普通电压（返回 V） |

### ThermocoupleType
支持 **12 种**：`TypeR`, `TypeS`, `TypeB`, `TypeJ`, `TypeT`, `TypeE`, `TypeK`, `TypeN`, `TypeC`, `TypeA`, `TypeG`, `TypeD`
（依据 IEC 60584-1-2013 / ASTM E230 / GB/T 29822-2013）

### AITriggerType
`Immediately` / `Digital` / `Software`（注意拼写为 `Immediately`，不是 `Immediate`）

### AITriggerMode
`Start` / `Reference`

### AIDigitalTriggerEdge
`Rising` / `Falling`

### AISignalExportSource / SignalExportDestination
- **Source**: `SampleClock` / `TriggerOut` / `IsConverting`
- **Destination**: `PXI_Trig0..7` / `PXI_Star`

### ClockTerminal / AIDigitalTriggerSource
`PFI0` / `PFI1` / `PXI_Trig0..7` / `PXI_Star`

### PowerLineFrequency
`_50Hz` / `_60Hz`

### ThermocoupleConnectionStatus
`Normal` / `OpenCircuit`（OTD 返回）

---

## 9. 工频抑制（PowerLineRejection）

| 成员 | 类型 | 说明 |
|------|------|------|
| `Frequency` | `PowerLineFrequency` | 设置抑制频率（`_50Hz` / `_60Hz`） |
| `RejectAt50Hz` | `bool` | 当前实际是否在抑制 50 Hz |
| `RejectAt60Hz` | `bool` | 当前实际是否在抑制 60 Hz |

**仅在 `SampleRate ≤ 8 Sa/s` 时实际生效**，高于此阈值驱动自动忽略。

---

## 10. PFI（PFISetting / PFIFilter）

```csharp
device.PFI[0].Enable(minPulseWidth_ns: 100);   // 启用 PFI0 输入去毛刺
device.PFI[0].Disable();
```

`PFITerminal` 枚举：`PFI0` / `PFI1`。

---

## 11. 信号导出（SignalExportList）

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,  SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.TriggerOut,   SignalExportDestination.PXI_Trig1);
aiTask.SignalExport.Add(AISignalExportSource.IsConverting, SignalExportDestination.PXI_Trig2);
aiTask.SignalExport.Clear();
```

多卡同步典型用法：主卡 `Add(SampleClock, PXI_Trig0)` → 从卡 `SampleClock.External.Terminal = PXI_Trig0`。

---

## 12. Utility.Thermocouple（静态）

```csharp
namespace JY6312.Utility;

public static class Thermocouple
{
    // EMF（单位：μV） ↔ 温度（单位：℃） 换算
    static double ConvertEMFToTemperature(ThermocoupleType type, double emf_uV, double cjTemperature_C);
    static double ConvertTemperatureToEMF(ThermocoupleType type, double temperature_C);

    // 各热电偶类型的物理量程
    static double GetTmin(ThermocoupleType type);
    static double GetTmax(ThermocoupleType type);
    static double GetVmin(ThermocoupleType type);     // μV
    static double GetVmax(ThermocoupleType type);     // μV
}
```

配合 `ReadRawData` 可实现离线冷端补偿与二次滤波。

---

## 13. 异常（JYDriverException）

所有驱动调用均可能抛 `JYDriverException`。关键属性：
- `ErrorCode` —— 枚举 `ErrorCode`
- `Message` —— 文字描述

常见错误码（摘录）：

| 枚举 | 场景 | 处置 |
|------|------|------|
| `aiIsStart` | Start 之后再 AddChannel/改配置 | 改配置前必须 Stop() |
| `Timeout` | ReadData 超时 | 增大 timeout 或先判 AvailableSamples |
| `BufferDownflow` | Finite 请求超过 SamplesToAcquire | 限制每次读取量 |
| `BufferOverflow` | Continuous 缓冲溢出 | 提升读取频率/增大缓冲 |
| `ErrorParam1` | 量程/采样率非法 | 对齐枚举或手册阈值 |
| `InitializeFailed` | 板卡/TB68CJ 初始化失败 | 检查 CJC 连接或关闭 `BuildInCJC.Enabled` |
| `PLLLockFailed` | 外部时钟 PLL 锁相失败 | 核对 `ExpectedRate` 与物理接线 |
| `DeviceNotFound` | 板卡未识别 | 确认 INF 驱动已安装（`driver\INF\`） |

---

## 14. 手册参考映射

对应 `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY-6312 Spec and Manual_EN.pdf`：

| API 对象 | 手册章节（章节名） |
|----------|--------------------|
| AIRange / Accuracy | "Analog Input Specifications" |
| ThermocoupleType / CJC | "Thermocouple Measurement" / "Cold-Junction Compensation" |
| TB68CJ 接线 | "Terminal Block TB68CJ" |
| AITrigger / PFI | "Triggering" / "PFI Signals" |
| SignalExport | "Signal Routing" |
| PowerLineRejection | "Noise Rejection" |
| 多卡同步 | "Synchronization" |

---

## 15. 驱动路径总表

| 项 | 绝对路径 |
|----|----------|
| 主 DLL | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll` |
| XML 注释 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.XML` |
| C++ 底层 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\driver\Cpp\X64\JY6312Core.dll` |
| INF | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\driver\INF\` |
| 手册 PDF | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY-6312 Spec and Manual_EN.pdf` |
| 示例集 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\` |
| 测试面板 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312TestPanel.exe` |
