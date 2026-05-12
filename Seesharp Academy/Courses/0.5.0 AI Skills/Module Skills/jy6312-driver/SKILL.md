---
name: jy6312-driver
description: 为 JYTEK JY-6312 PXIe 16 通道热电偶/低电压模拟输入模块编写 C# (.NET) 驱动代码。涵盖 AI 单点/有限/连续采集、热电偶测量（R/S/B/J/T/E/K/N/C/A/G/D）与内置冷端补偿 (TB68CJ)、低电压测量（±1.25V / ±625mV / ±312.5mV / ±156.2mV / ±78.125mV 五档量程）、数字触发 / 软件触发 / 重复触发、PFI 与 PXI_Trig 信号导出与多卡采样时钟同步、开路热电偶 (OTD) 检测、50/60 Hz 工频抑制、冷端温度自定义与原始数据 (EMF + 冷端温度) 读取。当用户提到 JY6312、JY-6312、6312 板卡、JY6312AITask、JY6312Device、热电偶采集、TB68CJ、冷端补偿 (CJC)、多通道温度采集、OTD、160 Sa/s 采样时自动应用本技能。
---

# JY6312 驱动开发技能

## 硬件概览

JY-6312 是 JYTEK PXIe 系列 **16 通道同步采样**热电偶/低电压模拟输入模块，每通道独立 ADC（支持通道级同步），专为低频高精度温度与微弱电压测量设计。

| 参数 | JY-6312 |
|------|---------|
| AI 通道数 | 16（每 ADC 1 通道） |
| AI 最大采样率（每通道） | **160 Sa/s** |
| AI 最小采样率（每通道） | **0.25 Sa/s** |
| 输入量程（电压模式） | **±1.25 V / ±625 mV / ±312.5 mV / ±156.2 mV / ±78.125 mV** （五档） |
| 热电偶类型 | R / S / B / J / T / E / K / N / C / A / G / D（IEC 60584-1-2013 / ASTM E230 / GB/T 29822-2013） |
| 冷端补偿 (CJC) | 通过 **TB68CJ** 接线端子内置传感器自动补偿，可关闭后手动设 `CustomCJTemperature` |
| 工频抑制 (Power Line Rejection) | 50 Hz / 60 Hz（**仅在 SampleRate ≤ 8 Sa/s 时有效**） |
| 触发端子 | PFI0 / PFI1，PXI_Trig0..PXI_Trig7，PXI_Star |
| 信号导出源 | SampleClock / TriggerOut / IsConverting（ADC 转换中） |
| 模块功能 | **仅 AI**（无独立 DI/DO 任务类） |

> JY-6312 与同系列的 JY-6314 共享大部分端子/触发/CJC 设计，但**采样率上限不同**（6312 = 160 Sa/s，6314 = 3 kSa/s），且 6312 增加一档 ±78.125 mV 量程。PDF 手册：`JY-6312 Spec and Manual_EN.pdf`。

## 驱动与依赖绝对路径

| 文件 | 绝对路径 |
|------|----------|
| 主驱动 DLL（**必须引用**） | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll` |
| 驱动 XML 注释文档 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.XML` |
| 规格与手册 PDF（参考） | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY-6312 Spec and Manual_EN.pdf` |
| 示例工程根目录 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\` |
| SeeSharpTools 绘图控件 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\SeeSharpTools.JY.GUI.dll` |
| SeeSharpTools 数组工具 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\SeeSharpTools.JY.ArrayUtility.dll` |
| 板卡测试面板 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312TestPanel.exe` |
| INF 驱动安装包 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\driver\INF\` |
| C++ 底层核心（自动随 .NET 层加载） | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\driver\Cpp\X64\JY6312Core.dll` |

**工程引用配置**（.csproj）：

```xml
<Reference Include="JY6312">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll</HintPath>
</Reference>
<Reference Include="SeeSharpTools.JY.GUI">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\SeeSharpTools.JY.GUI.dll</HintPath>
</Reference>
<Reference Include="SeeSharpTools.JY.ArrayUtility">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\SeeSharpTools.JY.ArrayUtility.dll</HintPath>
</Reference>
```

目标框架建议 `.NET Framework 4.0 或更高`（手册推荐 4.6.2+）。平台选择 `x86` 或 `x64`（推荐 `x64`），**禁止使用 AnyCPU**，否则无法加载底层 `JY6312Core.dll`。

代码顶部统一：
```csharp
using JY6312;
```

## 核心 API 速查

命名空间：`JY6312`

| 类型 | 用途 |
|------|------|
| `JY6312AITask` | AI 任务主类（唯一任务类，电压/热电偶复用） |
| `JY6312Device` | 板卡实例（`Scan()`, `GetInstance(cardNum)`, `Info`, `PFI`, `MaxSampleRate` 等常量） |
| `JYDriverException` | 驱动统一异常，必须 `try/catch` |
| `AIMode` | `Single` / `Finite` / `Continuous` |
| `AIChannel` | 通道项（`ID`, `ThermocoupleType`, `CustomCJTemperature`, `Range`, `RangeHigh`, `RangeLow`, `MeasurementType`, `Advanced`） |
| `AIChannelAdvanced` | 高级项（`VbiasEnabled`——热电偶必需；`InputBufferEnabled`） |
| `MeasurementType` | `Thermocouple` / `GeneralVoltage` |
| `ThermocoupleType` | `TypeR/S/B/J/T/E/K/N/C/A/G/D` |
| `AIRange` | `_1p25V` / `_625mV` / `_312p5mV` / `_156p2mV` / `_78p125mV` |
| `AISampleClockSource` | `Internal` / `External` |
| `ClockTerminal` | `PFI0/PFI1`, `PXI_Trig0..7`, `PXI_Star`（外部采样时钟输入） |
| `AITriggerType` | **`Immediately`** / `Digital` / `Software` |
| `AITriggerMode` | `Start` / `Reference`（Reference 仅 Finite 有效，配合 `PreTriggerSamples`） |
| `AIDigitalTriggerSource` | `PFI0/PFI1`, `PXI_Trig0..7`, `PXI_Star` |
| `AIDigitalTriggerEdge` | `Rising` / `Falling` |
| `AISignalExportSource` | `SampleClock` / `TriggerOut` / `IsConverting` |
| `SignalExportDestination` | `PXI_Trig0..7`, `PXI_Star`（用于多卡同步） |
| `BuildInCJC` | 内置冷端补偿配置（`Enabled`, `SensorStatus`, `SensorConnected`, `Advanced`, `Debouncing`） |
| `CJSensorConnectionStatus` | `Normal` / `LostConnect` / `Closed` |
| `ThermocoupleConnectionStatus` | `Normal` / `OpenCircuit`（OTD 检测结果） |
| `PowerLineRejection` | `RejectAt50Hz`, `RejectAt60Hz`, `Frequency`（≤ 8 Sa/s 有效） |
| `PFITerminal` / `PFISetting` / `PFIFilter` | PFI 滤波（`Enable(terminal, minPulseWidth_ns)`） |
| `Utility.Thermocouple` | 静态 EMF↔温度换算（`ConvertEMFToTemperature`, `ConvertTemperatureToEMF`） |

## 标准工作流（AI 任务）

所有 AI 操作均围绕 `JY6312AITask` 展开，遵循六步法：

```
1. 构造 task        : new JY6312AITask(boardNum)
2. 添加通道         : AddChannel(chID, ThermocoupleType) 或 AddChannel(chID, rangeLow, rangeHigh)
3. 配置模式/采样率  : Mode / SampleRate / SamplesToAcquire
4. [可选] 触发/时钟/信号导出 : Trigger / SampleClock / SignalExport
5. 启动             : Start()
6. 读取 → 停止      : ReadSinglePoint / ReadData / ReadRawData → Stop()
```

**关键约束**：
- `AddChannel` **必须在 `Start()` 之前**完成，否则抛 `aiIsStart` 错误。
- `Start()` 前若启用内置 CJC (默认)，驱动会检查 TB68CJ 连接状态，未接会抛异常。
- `Finite` 模式必须设置 `SamplesToAcquire`；`Continuous` 模式忽略该属性。
- `ReadData` 在连续模式下需先判断 `AvailableSamples >= 请求长度`，否则按 `timeout` 等待或抛超时。
- 多卡同步/外部时钟：**从卡必须先 `Start()`**（等待时钟），主卡后 `Start()`。

## 典型代码模板

### 1. 单点热电偶测量（Console，Single 模式）

参考 `JY6312.Examples\Console AI Single Point\Console AI Single Point.cs`：

```csharp
using System;
using System.Threading;
using JY6312;

JY6312AITask aiTask = null;
double[] readValue = new double[1];
try
{
    aiTask = new JY6312AITask(boardNum: 0);
    aiTask.Mode = AIMode.Single;
    aiTask.AddChannel(chID: 0, ThermocoupleType.TypeK);
    aiTask.SampleRate = 1;                 // Single 下用于决定数据稳定窗口
    aiTask.Start();
    Thread.Sleep((int)(1000.0 / aiTask.SampleRate));   // 等待 1/fs 秒确保数据有效
    aiTask.ReadSinglePoint(ref readValue);
    aiTask.Stop();
}
catch (JYDriverException ex) { Console.WriteLine(ex.Message); }
Console.WriteLine($"Temperature = {readValue[0]:F3} ℃");
```

### 2. 单通道连续采集（内部时钟 / 外部时钟二选一）

```csharp
aiTask = new JY6312AITask(0);
aiTask.AddChannel(channelID, ThermocoupleType.TypeK);   // 热电偶模式
// aiTask.AddChannel(channelID, -1.25, 1.25);            // 或电压模式：±1.25V
aiTask.Mode = AIMode.Continuous;

// --- 时钟配置 ---
aiTask.SampleClock.Source = AISampleClockSource.Internal;
aiTask.SampleRate = 100;                                 // Sa/s，范围 0.25 ~ 160
// 外部时钟示例：
// aiTask.SampleClock.Source = AISampleClockSource.External;
// aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
// aiTask.SampleClock.External.ExpectedRate = 100;

aiTask.Start();

// 定时读取（例如 Timer 每 100 ms 触发一次）
double[] buf = new double[1000];
if (aiTask.AvailableSamples >= (ulong)buf.Length)
{
    aiTask.ReadData(ref buf, timeout: -1);
    // 绘图：stripChartX1.Plot(buf);
}
```

### 3. 多通道 + 数字触发（PXI_Trig/PFI 上升沿启动）

参考 `JY6312.Examples\Winform AI Continuous Digital Trigger\`：

```csharp
aiTask = new JY6312AITask(0);
foreach (var ch in new[] { 0, 1, 2, 3 })
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100;

aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Mode           = AITriggerMode.Start;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;

aiTask.Start();

// 读取为二维数组：[samplesPerChannel, channelCount]
double[,] readValue    = new double[1000, aiTask.Channels.Count];
double[,] displayValue = new double[aiTask.Channels.Count, 1000];
if (aiTask.AvailableSamples >= (ulong)readValue.GetLength(0))
{
    aiTask.ReadData(ref readValue, timeout: 4000);
    SeeSharpTools.JY.ArrayUtility.ArrayManipulation.Transpose(readValue, ref displayValue);
}
```

### 4. 软件触发 + Reference（参考）触发 + 多次重触发

参考 `JY6312.Examples\Winform AI MultiRecord Soft Trigger\`：

```csharp
aiTask.Mode              = AIMode.Finite;
aiTask.SampleRate        = 100;
aiTask.SamplesToAcquire  = 500;

aiTask.Trigger.Type              = AITriggerType.Software;
aiTask.Trigger.Mode              = AITriggerMode.Reference; // 或 Start
aiTask.Trigger.ReTriggerCount    = 5;                       // 0/1=单次，-1=无限
aiTask.Trigger.PreTriggerSamples = 1;                       // 仅 Reference 有效

aiTask.Start();
// 每次需要触发时：
aiTask.SendSoftwareTrigger();
```

### 5. 关闭内置 CJC 并自定义冷端温度

参考 `JY6312.Examples\Winform AI Continuous Custom RJ\`：

```csharp
aiTask.BuildInCJC.Enabled = false;                 // 关闭自动 CJC
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.Start();
aiTask.SetCJTemperature(chID: 0, temperature: 25.0);   // 每次 ReadData 前生效
// 或批量：aiTask.SetCJTemperature(new double[]{25.0, 25.0, ...});
```

### 6. 读取原始 EMF + 冷端温度

参考 `JY6312.Examples\Winform AI Continuous Raw Data\`：

```csharp
double[,] hjVoltage     = new double[samples, channels];   // 热端 EMF (V)
double[,] cjTemperature = new double[samples, channels];   // 冷端温度 (℃)
aiTask.ReadRawData(ref hjVoltage, ref cjTemperature, timeout: 1000);

// 驱动端手动换算（可选，Utility.Thermocouple 提供）
double tempC = Utility.Thermocouple.ConvertEMFToTemperature(
    ThermocoupleType.TypeK, hjVoltage[0, 0] * 1e6, cjTemperature[0, 0]);
```

### 7. 开路热电偶 (OTD) 检测

参考 `JY6312.Examples\Winform Open Thermocouple Detection\`：

```csharp
aiTask = new JY6312AITask(0);
foreach (var ch in selectedChannels)
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 1;

// 直接调用检测，不需 Start()
Dictionary<int, ThermocoupleConnectionStatus> results = aiTask.DetectOpenThermocouple();
foreach (var kv in results)
    Console.WriteLine($"Ch{kv.Key}: {kv.Value}");   // Normal / OpenCircuit
```

### 8. TB68CJ 端子连接状态轮询

参考 `JY6312.Examples\Winform Terminal Block State\`：

```csharp
aiTask = new JY6312AITask(0);
// 定时器中：
bool connected = aiTask.BuildInCJC.SensorStatus == CJSensorConnectionStatus.Normal;
// CJSensorConnectionStatus：Normal / LostConnect / Closed
```

### 9. 多卡采样时钟同步（主卡导出 → 从卡接收）

参考 `JY6312.Examples\Winform AI_MultiCard SampleClock Sync\`：

```csharp
var master = new JY6312AITask(masterSlot);
master.AddChannel(0, -1.25, 1.25);
master.Mode             = AIMode.Finite;
master.SampleRate       = 100;
master.SamplesToAcquire = 1000;
master.Trigger.Type     = AITriggerType.Immediately;
master.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

var slave = new JY6312AITask(slaveSlot);
slave.AddChannel(0, -1.25, 1.25);
slave.Mode                              = AIMode.Finite;
slave.SamplesToAcquire                  = 1000;
slave.SampleClock.Source                = AISampleClockSource.External;
slave.SampleClock.External.Terminal     = ClockTerminal.PXI_Trig0;
slave.SampleClock.External.ExpectedRate = 100;

slave.Start();    // 从卡先启动（等待时钟）
master.Start();   // 主卡后启动（驱动时钟）
// ...并行 ReadData(ref masterBuf) / ReadData(ref slaveBuf)
```

### 10. 50/60 Hz 工频抑制（低速测量降噪）

```csharp
aiTask.SampleRate = 2;   // 必须 ≤ 8 Sa/s 才生效
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
// 读取 RejectAt50Hz / RejectAt60Hz 确认当前是否实际启用
```

## 异常与错误处理

所有驱动调用**必须**包在 `try { ... } catch (JYDriverException ex) { ... }` 中。常见错误：

| 场景 | 错误枚举 | 诊断 |
|------|----------|------|
| Start 后仍 AddChannel | `aiIsStart` | 在 Start() 之前完成所有通道 |
| 读数请求超时 | `Timeout` | 增大 `timeout`，或先判断 `AvailableSamples` |
| Finite 下读取多于 `SamplesToAcquire` | `BufferDownflow` | 限制请求长度 |
| 量程参数非法 | `ErrorParam1` | 仅允许 ±1.25 / ±0.625 / ±0.3125 / ±0.1562 / ±0.078125 V |
| 开启内置 CJC 但未接 TB68CJ | `InitializeFailed` | 检查 `aiTask.BuildInCJC.SensorConnected`，或关闭 `BuildInCJC.Enabled` |
| 外部时钟 PLL 未锁定 | `PLLLockFailed` | 检查物理接线、`ExpectedRate` 是否匹配实际时钟 |

**窗口/程序关闭时**：`FormClosing` / `finally` 中调用 `aiTask?.Stop()` 释放板卡资源。

## 代码风格与约定

- **板卡构造**：`new JY6312AITask(int boardNum)` 或 `new JY6312AITask(string boardName)`。`boardNum = 0` 对应系统扫描到的第一块 JY6312。
- **读数数组维度**：
  - 一维 `double[samples]`：单通道，或多通道按时序交错排列。
  - 二维 `double[samples, channels]`：推荐多通道使用，便于转置给 `StripChartX.Plot`。
- **采样率取整**：`SampleRate` 属性写后再读会得到驱动实际生效值，受通道数与量化舍入影响。
- **UI 线程更新**：定时读取数据时，在 `Timer.Tick` 中短暂禁用 `timer.Enabled`，防止重入。
- **平台**：`x64` 推荐（与 `driver\Cpp\X64\JY6312Core.dll` 对齐）。AnyCPU 会在 64 位系统上随机失败。

## 示例工程映射

所有示例位于 `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\`，按需求选取即可：

| 需求 | 示例目录 |
|------|----------|
| 单点温度（Console） | `Console AI Single Point\` |
| 单点原始 EMF（Console） | `Console AI Single Point Raw Data\` |
| 单通道连续采集 | `Winform AI Continuous\` |
| 多通道连续采集（热电偶） | `Winform AI Continuous MutiChannel\` |
| 多通道连续采集（电压） | `Winform AI Continuous MutiChannel GeneralVoltage\` |
| 连续采集 + 数字触发 | `Winform AI Continuous Digital Trigger\` |
| 连续采集 + 软件触发 | `Winform AI Continuous Soft Trigger\` |
| 连续采集 + 自定义冷端 | `Winform AI Continuous Custom RJ\` |
| 连续采集 + 原始数据 (EMF + CJ T) | `Winform AI Continuous Raw Data\` |
| 电流测量（外接分流电阻） | `Winform AI Continuous Current Measure\` 与 `... MutiChannel Current Measure\` |
| 有限采集（单/多通道） | `Winform AI Finite\`, `Winform AI Finite MultiChannel\` |
| 有限采集 + 数字触发 | `Winform AI Finite Digital Trigger\` |
| 多记录软件触发 (Retrigger) | `Winform AI MultiRecord Soft Trigger\` |
| 多板采样时钟同步 | `Winform AI_MultiCard SampleClock Sync\` |
| 开路热电偶检测 (OTD) | `Winform Open Thermocouple Detection\` |
| TB68CJ 状态指示 | `Winform Terminal Block State\` |
| Single-Point 多通道 | `Winform AI Single Point MultiChannel\` 等 |

解决方案入口：`JY6312.Examples\JY6312.Examples.sln`（Visual Studio 打开即可编译）。

## 扩展资料

- 规格书章节（见 `JY-6312 Spec and Manual_EN.pdf`）：
  - 端子接线图（TB68CJ）
  - 绝对精度表（不同热电偶类型、不同温区）
  - 共模抑制与输入阻抗
  - 冷端温度传感器更新周期与去抖动配置建议
- 板卡常量（`JY6312Device` 静态字段）：
  - `MaxSampleRate = 160 Sa/s`，`MinSampleRate = 0.25 Sa/s`
  - `TotalNumberOfChannels = 16`
  - `MaxSamplesToAcquireSingleChannel` / `MinSamplesToAcquire`
- EMF↔温度换算工具：`JY6312.Utility.Thermocouple.ConvertEMFToTemperature / ConvertTemperatureToEMF`，以及每种类型的 `GetTmin/GetTmax/GetVmin/GetVmax`。

## 进阶参考（按需加载）

当本 SKILL 列出的内容不足以解决问题时，按需打开以下配套文档（均位于本 skill 目录下）：

| 文档 | 何时查阅 |
|------|----------|
| [`reference.md`](reference.md) | 需要查阅完整 API 定义（类/属性/方法签名）、枚举全表、错误码、PFI/SignalExport/BuildInCJC 细节、手册章节映射 |
| [`examples.md`](examples.md) | 需要完整可运行的示例代码（含 Console/WinForm 骨架、多卡同步、OTD、Retrigger、电流测量、工频抑制、通用模板 18 条） |

- `reference.md` ←→ `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.XML` 的结构化精炼
- `examples.md` ←→ `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\` 全部 22 个工程的代码片段化
