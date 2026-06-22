# JY6314 驱动 API 详细参考

> 本文件是 [SKILL.md](SKILL.md) 的补充。API 条目均摘自驱动文档 `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\Bin\JY6314.xml`。

命名空间：`JY6314`

---

## 1. JY6314Device

板卡硬件实例。由 `JY6314AITask` / `JY6314DITask` / `JY6314DOTask` 通过 `Device` 属性暴露，一般不直接实例化。

| 成员 | 类型 | 说明 |
|------|------|------|
| `SerialNumber` | string | 序列号 |
| `Handle` | IntPtr | 硬件句柄 |
| `DeviceID` | int | PID |
| `BoardClockRate` | double | A/D 板载时钟频率 |
| `AIBufferSize` | int | 板载 DRAM 大小（bytes） |
| `MaxSampleRate` | double | 最大采样率（1 ADC 1 通道，共 16 个 ADC） |
| `MinSampleRate` | double | 最小采样率 |
| `MaxSamplesToAcquireSingleChannel` | int | 单通道使能时每通道最大采样数 |
| `MinSamplesToAcquire` | int | 最小待采样数 |
| `MaxVoltage` | double | 最大输入电压（V） |
| `TotalNumberOfChannels` | int | 最大通道数（=16） |
| `DIOLineCount` | int | DIO 通道数 |
| `FpgaTemperature` | double | FPGA 温度（℃），属性读取 |
| `IndoorTemperature` | double | 板卡室内温度（℃），属性读取 |

---

## 2. JY6314AITask

### 构造

| 方法 | 说明 |
|------|------|
| `JY6314AITask(int boardNum)` | 以槽位号构造；`boardNum = 0` 开第一块板 |
| `JY6314AITask(string boardName)` | 以 Device Manager 中设置的别名构造 |

### 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Device` | JY6314Device | 所属板卡 |
| `Mode` | AIMode | `Finite` / `Continuous` / `Single` |
| `SampleRate` | double | 每通道采样率（Sa/s）；`Single` 模式下无效。设置后可读回获取实际生效值 |
| `SamplesToAcquire` | int | 每通道采集点数，仅 `Finite` 下有效 |
| `AvailableSamples` | ulong | 可读取的每通道样本数 |
| `TransferedSamples` | ulong | 已从本地 buffer 传出的样本数 |
| `SampleClock` | AISampleClock | 采样时钟配置 |
| `Trigger` | AITrigger | 触发配置 |
| `Channels` | List\<AIChannel\> | 已添加通道列表 |
| `SignalExport` | AISignalExport | 信号导出配置 |
| `BuildInCJC` | BuildInCJC | 内置冷端补偿配置 |

### 通道管理

```csharp
// 热电偶
AddChannel(int chID, ThermocoupleType tc);                  // chID = -1 加全部
AddChannel(int[] chIDs, ThermocoupleType tc);               // 一组通道，同一 TC 类型
AddChannel(int[] chIDs, ThermocoupleType[] tcs);            // 一组通道，逐通道 TC 类型

// 电压
AddChannel(int chID, double rangeLow, double rangeHigh);    // 量程：±1.25 / ±0.625 / ±0.3125 / ±0.15625
AddChannel(int[] chIDs, double rangeLow, double rangeHigh); // 统一量程
AddChannel(int[] chIDs, double[] lows, double[] highs);     // 逐通道量程

// 删除
RemoveChannel(int chID);          // chID = -1 删全部
RemoveChannel(int[] chIDs);
```

### 运行控制

| 方法 | 说明 |
|------|------|
| `Start()` | 启动任务 |
| `Stop()` | 停止任务 |
| `WaitUntilDone(int timeoutMs)` | 仅 `Finite`，阻塞等待完成；`-1` 永不超时 |
| `SendSoftwareTrigger()` | 软件触发触发一次采集（`Trigger.Type = Soft`） |
| `DetectOpenThermocouple()` | 返回 `Dictionary<int, ThermocoupleConnectionStatus>`，键=物理通道 ID |

### 数据读取（经过补偿的温度 ℃ 或电压 V）

```csharp
ReadData(ref double[] buf, int samplesPerChannel, int timeoutMs);
ReadData(ref double[] buf, int timeoutMs);
ReadData(ref double[,] buf, int samplesPerChannel, int timeoutMs);  // [samples, channels]
ReadData(ref double[,] buf, int timeoutMs);
ReadData(IntPtr buf, int samplesPerChannel, int timeoutMs);         // 非托管

ReadSinglePoint(ref double[] buf);
ReadSinglePoint(IntPtr buf);
ReadSinglePoint(ref double value, int channelID);                   // 单通道单点
```

### 原始数据读取（热端 EMF + 冷端温度）

```csharp
ReadRawData(ref int[,] buf, int samplesPerChannel, int timeoutMs);  // i32 ADC 原始码
ReadRawData(ref int[]   buf, int samplesPerChannel, int timeoutMs);

ReadRawData(ref double[] hjEMF_V,  ref double[] cjTemp_C, int timeoutMs);
ReadRawData(ref double[,] hjEMF_V, ref double[,] cjTemp_C, int samplesPerChannel, int timeoutMs);
ReadRawData(IntPtr hjEMFBuf, IntPtr cjTempBuf, int samplesPerChannel, int timeoutMs);

ReadRawSinglePoint(ref double[] hjEMF_V, ref double[] cjTemp_C);
ReadRawSinglePoint(ref double   hjEMF_V, ref double   cjTemp_C, int channelID);
ReadRawSinglePoint(IntPtr hjEMFBuf, IntPtr cjTempBuf);
```

### 冷端温度手动设置（`BuildInCJC.Enabled = false` 时有效）

```csharp
SetCJTemperature(int channelID, double tempC);   // channelID = -1 全部通道
SetCJTemperature(double[] tempC);                // 按添加顺序对应每通道
```

---

## 3. AIChannel

| 属性 | 说明 |
|------|------|
| `ChannelID` | 物理通道号 0..15 |
| `MeasureDataType` | `Voltage` / `Thermocouple` |
| `ThermocoupleType` | 热电偶类型（仅 TC 模式） |
| `RangeHigh` / `RangeLow` | 电压量程上/下限（V） |
| `CustomCJTemperature` | 自定义参考结温（℃），仅在 `BuildInCJC.Enabled = false` 时、每次 `ReadData` 生效 |

构造（通常不直接用，由 `AddChannel` 内部创建）：
```csharp
new AIChannel(int channelId, ThermocoupleType tc, double customCJTempC);
new AIChannel(int channelId, double rangeLow, double rangeHigh);
```

---

## 4. 采样时钟 AISampleClock

```csharp
aiTask.SampleClock.Source = AISampleClockSource.Internal;   // 或 External
aiTask.SampleClock.External.Terminal     = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 1000;            // 外部时钟预期频率（用于反推 buffer 时序）
```

| 枚举 | 取值 |
|------|------|
| `AISampleClockSource` | `Internal`, `External` |
| `ClockTerminal` | `PFI0`, `PFI1`, `PXI_Trig0`..`PXI_Trig7` |

---

## 5. 触发 AITrigger

```csharp
aiTask.Trigger.Type             = AITriggerType.Digital;   // Immediate / Digital / Soft
aiTask.Trigger.Mode             = AITriggerMode.Start;     // Start / Reference
aiTask.Trigger.Digital.Source   = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge     = AIDigitalTriggerEdge.Rising;  // Rising / Falling
aiTask.Trigger.ReTriggerCount   = -1;   // 0/1 单次；-1 连续重触发；N 重触发 N 次
aiTask.Trigger.PreTriggerSamples = 100; // 仅 Reference 模式；≤ SamplesToAcquire
```

| 枚举 | 说明 |
|------|------|
| `AITriggerType` | `Immediate`（立即）/ `Digital`（外部数字）/ `Soft`（软件） |
| `AITriggerMode` | `Start`（启动触发）/ `Reference`（参考触发，含 PreTrigger） |
| `AIDigitalTriggerSource` | `PFI0`, `PFI1`, `PXI_Trig0..7` |
| `AIDigitalTriggerEdge` | `Rising`, `Falling` |

---

## 6. 信号导出 AISignalExport

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,    SignalExportDestination.PFI0);
aiTask.SignalExport.Add(AISignalExportSource.StartTrig,      SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.ReferenceTrig,
        new List<SignalExportDestination>{ SignalExportDestination.PFI1, SignalExportDestination.PXI_Trig1 });

aiTask.SignalExport.Clear(SignalExportDestination.PFI0);
aiTask.SignalExport.ClearAll();
```

| 枚举 | 取值 |
|------|------|
| `AISignalExportSource` | `StartTrig`, `ReferenceTrig`, `SampleClock` |
| `SignalExportDestination` | `PFI0`, `PFI1`, `PXI_Trig0..7` |

---

## 7. 冷端补偿 BuildInCJC

```csharp
aiTask.BuildInCJC.Enabled              = true;   // 默认 true：自动补偿
aiTask.BuildInCJC.SensorConnected;               // bool，TB68CJ 是否连接
aiTask.BuildInCJC.SensorStatus;                  // CJSensorConnectionStatus
aiTask.BuildInCJC.ReportBuildInCJException(true, true);  // 启动前校验端子
aiTask.BuildInCJC.OpenSensorComminucation();     // 主动开启 I2C 通信
aiTask.BuildInCJC.CloseSensorComminucation();

// 进阶
aiTask.BuildInCJC.Advanced.IgnoreTimeoutException = false;  // 默认 false
aiTask.BuildInCJC.Advanced.UpdateTimeout          = 5000;   // ms

// 防抖
aiTask.BuildInCJC.Debouncing.Enabled    = true;
aiTask.BuildInCJC.Debouncing.MaxBounce  = 2.0;   // ℃，超限压缩到 ±MaxBounce
aiTask.BuildInCJC.Debouncing.ValidBounce = 1.0;  // ℃，超限忽略；持续 5 s 后告警
aiTask.BuildInCJC.Debouncing.Reset(delayedReturn: true);
```

| `CJSensorConnectionStatus` | 含义 |
|---|---|
| `Normal` | 正常 |
| `LostConnect` | 曾连接，现已一段时间未读到 |
| `Closed` | I2C 通信被关闭 |

| `ThermocoupleConnectionStatus` | 含义（`DetectOpenThermocouple` 返回） |
|---|---|
| `Normal` | 已接 |
| `OpenCircuit` | 开路/未接 |

---

## 8. JY6314DITask / JY6314DOTask

```csharp
var di = new JY6314DITask(int slot);    // 或 new JY6314DITask(string boardName);
di.AddChannel(int lineID);
di.AddChannel(int[] lineIDs);
di.RemoveChannel(int lineID);           // -1 删全部
di.Start();
di.ReadSinglePoint(ref bool[] values);            // 按添加顺序
di.ReadSinglePoint(ref bool value, int lineNum);
di.Stop();
// di.Channels : List<DIChannel>，每项有 LineID

var dos = new JY6314DOTask(int slot);   // API 对称
dos.AddChannel(...);
dos.Start();
dos.WriteSinglePoint(bool[] values);
dos.WriteSinglePoint(bool value, int lineID);
dos.Stop();
// dos.Channels : List<DOChannel>，每项有 LineID
```

---

## 9. PFI 滤波 PFIFilter（通过 `JY6314Device` 的 PFISetting 访问）

```csharp
PFIFilter.Enable(PFITerminal terminal, int minPulseWidthNs);
PFIFilter.Enable(PFITerminal[] terminals, int minPulseWidthNs);
PFIFilter.Enable(PFITerminal[] terminals, int[] widthsNs);
PFIFilter.Disable(PFITerminal terminal);
PFIFilter.Disable(PFITerminal[] terminals);
PFIFilter.DisableAll();
```

`minPulseWidthNs` 量化档位：
- 10 ~ 2550 ns，步长 10 ns
- 2550 ~ 81600 ns，步长 320 ns
- 81600 ~ 1 305 600 ns，步长 5120 ns

`PFITerminal`: `PFI0`, `PFI1`

---

## 10. 枚举汇总

### AIMode
`Finite`：采集固定数量点后停止；`Continuous`：循环 DMA，靠 `AvailableSamples` + `ReadData` 取数；`Single`：单点模式（`SampleRate` 作为单点触发频率）。

### MeasureDataType
`Voltage`（电压值）/ `Thermocouple`（温度值）。由 `AddChannel` 的重载决定。

### ThermocoupleType
`TypeR`, `TypeS`, `TypeB`, `TypeJ`, `TypeT`, `TypeE`, `TypeK`, `TypeN`, `TypeC`, `TypeA`, `TypeG`, `TypeD`。常数遵循 IEC 60584-1-2013 / ASTM E230 / GB/T 29822-2013。

### AIRange
低电压量程标识（与 `AddChannel` 的 `rangeLow/High` 形式等价）。

### PowerLineFrequency
`_50Hz`, `_60Hz`。通过 `JY6314Import.JY6314_AI_SetPowerLineRejectionPreference(h, 0|1)` 设置；对 **< 40 Sa/s** 生效。

---

## 11. 异常与日志

### JYDriverException
驱动抛出的统一异常。捕获标准：
```csharp
try { ... }
catch (JY6314.JYDriverException ex)
{
    // ex.Message 含中文/英文错误描述
    // 推荐：UI 弹窗 + 日志落盘
}
```

### JYLog / JYLogLevel
用于驱动日志级别控制，调试期可提高日志等级定位硬件问题。

### JYErrorCode
驱动底层错误码枚举（一般由异常内部封装，上层不必直接处理）。

---

## 12. 工具类（Utility 子命名空间）

- `Utility.Thermocouple.ConvertEMFToTemperature(ThermocoupleType, double E_uV, double refTempC)` → ℃
- `Utility.Thermocouple.ConvertTemperatureToEMF(ThermocoupleType, double tempC)` → μV
- `Utility.Thermocouple.GetVmin/GetVmax/GetTmin/GetTmax(ThermocoupleType)` 获取有效范围
- `Utility.Polynomial.Solve(double x, double[] coefficients)` 多项式求值
- `Utility.Interpolation.LinearInterpolation1D(...)` 一维分段线性插值/反查

用途：在 `BuildInCJC.Enabled = false` 且仅用 `ReadRawData` 的场景下，自行实现温度换算、校准曲线拟合。

---

## 13. 板卡校准 JY6314ProductCal

> 校准操作会改写 EEPROM，**非生产调试不要调用**。

```csharp
JY6314ProductCal.InitCalInfo(device, CalibrationType.AIDC);
JY6314ProductCal.SetEnvironmentParam(device, CalibrationType.AIDC, tempC);
JY6314ProductCal.SetAIDCCoef(device, chID, rangeV, float[] coefs);
JY6314ProductCal.GetAIDCCoef(device, chID, rangeV, float[] coefs);
JY6314ProductCal.SaveToEEPROM(device, CalibrationType.AIDC);
JY6314ProductCal.SelfCalibration(device, double[] offsets);
JY6314ProductCal.ClearCalibration(device);
```

`CalibrationType`: `AIDC`, `AIFlatness`, `AILinearity`, `AODC`, `AOFlatness`, `AOLinearity`。

---

## 14. 低层 P/Invoke（JY6314Import）

`JY6314Import` 暴露了所有底层 `JY6314_XXX` 原始 C API（如 `JY6314_Open`, `JY6314_AI_EnableChannel`, `JY6314_AI_Start` 等）。**除非需要封装自定义高级 Task，否则不要直接调用**——`JY6314AITask/DITask/DOTask` 已完成了正确顺序与错误码处理。

---

## 15. 常见返回值与语义

| 场景 | 语义 |
|------|------|
| `ReadData(..., timeout = -1)` | 永不超时，阻塞到数据就绪（或驱动错误） |
| `ReadData(..., timeout = 0)` | 立即返回，若数据不足抛超时异常 |
| `AvailableSamples` 为 ulong | 多通道时也是**每通道**样本数 |
| `ReadData(double[,] buf, ...)` | buf 形状 `[samples, channels]`（行=样本，列=通道） |
| `ReadData(double[] buf, ...)`（多通道） | 交织 `[ch0_s0, ch1_s0, ..., ch0_s1, ch1_s1, ...]` 或按通道分段，以 XML 为准；**多通道建议用二维重载** |
| `Channels.Clear()` | 清空已注册通道，需在 Stop 之后调用以便下次 Start 重新 Add |
