---
name: jy6314-driver
description: 为 JYTEK JY-6314 PXIe 16 通道热电偶/低电压模拟输入模块编写 C# (.NET) 驱动代码。涵盖 AI 单点/有限/连续采集、热电偶测量（R/S/B/J/T/E/K/N/C/A/G/D）与内置冷端补偿、电压测量（±1.25 V / ±0.625 V / ±0.3125 V / ±0.15625 V）、数字触发与软件触发、PXI_Trig/PFI 信号导出与多卡采样时钟同步、开路热电偶（OTD）检测、TB68CJ 接线端子状态、以及 DI/DO 单点读写。当用户提到 JY6314、JY-6314、6314 板卡、JY6314AITask、JY6314DITask、JY6314DOTask、热电偶采集、TB68CJ、冷端补偿 (CJC)、多通道温度采集时自动应用本技能。
---

# JY6314 驱动开发技能

## 硬件概览

JY-6314 是 JYTEK PXIe 系列 16 通道同步采样热电偶/低电压模拟输入模块，与 JY-6312 同属一个系列（以 `JY-6312 Spec and Manual_EN.pdf` 为参考手册），硬件差异主要在采样率：

| 参数 | JY-6314 |
|------|---------|
| AI 通道数 | 16（每 ADC 1 通道，共 16 个 ADC，支持通道级同步） |
| AI 最大采样率 | 3 kSa/s（每通道） |
| AI 最小采样率 | 0.275 Sa/s |
| 输入量程（电压模式） | ±1.25 V / ±0.625 V / ±0.3125 V / ±0.15625 V（四档） |
| 热电偶类型 | R / S / B / J / T / E / K / N / C / A / G / D |
| 冷端补偿 | 通过 TB68CJ 接线端子内置传感器自动补偿，亦可关闭后手动设置 |
| 工频抑制 | 50 Hz / 60 Hz（采样率低于 40 Sa/s 时有效） |
| 触发端子 | PFI0 / PFI1，PXI_Trig0..PXI_Trig7 |
| 数字 IO | 按 Line 单点读写 |

> 本技能以 JY-6312 的功能条目为蓝本，仅在"采样率上限 = 3 kSa/s"这一参数上偏离 6312 手册。其余（端子、量程、补偿、触发、信号导出、DI/DO）与 6312 一致。

## 驱动与依赖绝对路径

| 文件 | 绝对路径 |
|------|----------|
| 主驱动 DLL（**必引用**） | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\Bin\JY6314.dll` |
| 驱动 XML 注释文档 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\Bin\JY6314.xml` |
| 6312 规格说明手册（参考） | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\JY-6312 Spec and Manual_EN.pdf` |
| 示例工程根目录 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\JY6314.Examples\` |
| SeeSharpTools GUI（绘图控件） | `C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.GUI.dll` |
| SeeSharpTools 数组工具 | `C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.ArrayUtility.dll` |
| 板卡测试面板 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\Bin\JY6314TestPanel.exe` |
| INF 驱动 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\driver\INF\JY6314.inf` |

**工程引用配置**（.csproj）：

```xml
<Reference Include="JY6314">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\Bin\JY6314.dll</HintPath>
</Reference>
<Reference Include="SeeSharpTools.JY.GUI">
  <HintPath>C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.GUI.dll</HintPath>
</Reference>
```

目标框架建议 `.NET Framework 4.6.2 或更高`。代码顶部统一：
```csharp
using JY6314;
```

## 核心 API 速查

命名空间：`JY6314`

| 类型 | 用途 |
|------|------|
| `JY6314AITask` | 模拟输入任务（电压 / 热电偶） |
| `JY6314DITask` / `JY6314DOTask` | 数字 IO 单点读写 |
| `JY6314Device` | 板卡实例（SN、温度、DIOLineCount、MaxSampleRate 等） |
| `JYDriverException` | 驱动统一异常，需 `try/catch` |
| `AIMode` | `Single` / `Finite` / `Continuous` |
| `AIChannel` | AI 通道项（ChannelID / RangeHigh / RangeLow / ThermocoupleType / CustomCJTemperature） |
| `ThermocoupleType` | `TypeR/S/B/J/T/E/K/N/C/A/G/D` |
| `AISampleClockSource` | `Internal` / `External` |
| `ClockTerminal` | `PFI0`, `PFI1`, `PXI_Trig0..7` |
| `AITriggerType` | `Immediate` / `Digital` / `Soft` |
| `AITriggerMode` | `Start` / `Reference`（Reference 仅 Finite 有效，支持 `PreTriggerSamples`） |
| `AIDigitalTriggerSource` / `AIDigitalTriggerEdge` | `PFI0/PFI1/PXI_Trig0..7`，`Rising/Falling` |
| `AISignalExportSource` | `StartTrig` / `ReferenceTrig` / `SampleClock` |
| `SignalExportDestination` | `PFI0/PFI1`, `PXI_Trig0..7` |
| `BuildInCJC` | 内置冷端补偿配置（`Enabled`, `SensorStatus`, `SensorConnected`, `Advanced`, `Debouncing`） |
| `CJSensorConnectionStatus` | `Normal` / `LostConnect` / `Closed` |
| `ThermocoupleConnectionStatus` | `Normal` / `OpenCircuit`（OTD 结果） |

## 标准工作流（AI 任务）

1. 实例化任务：`var aiTask = new JY6314AITask(boardNumber);` 或 `new JY6314AITask(boardName);`
2. 添加通道：
   - 热电偶：`aiTask.AddChannel(chID, ThermocoupleType.TypeK);` 或 `AddChannel(-1, TypeK)` 添加全部。
   - 电压：`aiTask.AddChannel(chID, -1.25, 1.25);`（`chID = -1` 添加所有 16 通道）。
3. 设置模式：`aiTask.Mode = AIMode.Continuous | Finite | Single;`
4. 时钟：默认内部；外部时钟需同时设置 `SampleClock.Source`、`External.Terminal`、`External.ExpectedRate`。
5. 采样数：`SamplesToAcquire`（仅 Finite 有效）。
6. 触发（可选）：`aiTask.Trigger.Type = Digital/Soft/Immediate; aiTask.Trigger.Mode = Start/Reference;`
7. 信号导出（可选，用于多卡同步或触发联动）：`aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);`
8. 启动：`aiTask.Start();`
9. 读数据：
   - 单点（Single）：`aiTask.ReadSinglePoint(ref double[])`。
   - Finite：启动后直接 `ReadData(ref buf, -1)` 阻塞到完成，或 `WaitUntilDone(timeout)`。
   - Continuous：轮询 `AvailableSamples`，达到阈值后 `ReadData(ref buf, -1)`；多通道用 `double[samples, channels]`。
10. 停止：`aiTask.Stop();`（关窗/异常路径必须调用）。

## 代码模板

### 模板 1：连续采集（单通道，热电偶）

```csharp
using JY6314;

private JY6314AITask aiTask;
private double[] readValue;

private void Start()
{
    readValue = new double[2048];
    aiTask = new JY6314AITask(0);                                 // 板卡 0
    aiTask.AddChannel(0, ThermocoupleType.TypeK);                 // 通道 0, K 型
    aiTask.Mode = AIMode.Continuous;
    aiTask.SampleClock.Source = AISampleClockSource.Internal;
    aiTask.SampleRate = 100;                                      // 0.275 ~ 3000 Sa/s
    aiTask.Start();
}

private void Timer_Tick(object s, EventArgs e)                    // 每 ~100 ms 轮询一次
{
    if (aiTask.AvailableSamples < (ulong)readValue.Length) return;
    try { aiTask.ReadData(ref readValue, -1); }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```

### 模板 2：多通道 Finite 电压采集

```csharp
var aiTask = new JY6314AITask(0);
int[] chs = { 0, 1, 2, 3 };
aiTask.AddChannel(chs, -1.25, 1.25);     // 统一量程，也支持每通道独立量程
aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 1000;
aiTask.SamplesToAcquire = 2000;

double[,] buf = new double[aiTask.SamplesToAcquire, chs.Length];
aiTask.Start();
aiTask.ReadData(ref buf, aiTask.SamplesToAcquire, -1);            // timeout=-1 永不超时
aiTask.Stop();
// 绘图前可通过 SeeSharpTools.JY.ArrayUtility.ArrayManipulation.Transpose 转置
```

### 模板 3：单点读取

```csharp
double[] v = new double[16];             // 长度 >= 通道数
var aiTask = new JY6314AITask(boardNum);
aiTask.Mode = AIMode.Single;
aiTask.AddChannel(-1, -1.25, 1.25);      // 所有 16 通道
aiTask.SampleRate = 10;
aiTask.Start();
Thread.Sleep((int)(1000.0 / aiTask.SampleRate));   // 等待 1 个采样周期确保数据就绪
aiTask.ReadSinglePoint(ref v);
aiTask.Stop();
```

### 模板 4：数字触发（Start 触发，上升沿，PFI0）

```csharp
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 1000;
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;
aiTask.Start();                          // 阻塞等待外部边沿
```

### 模板 5：软件触发 + 多次重触发（MultiRecord）

```csharp
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 2000;
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Trigger.ReTriggerCount = 5;       // 0/1 = 单次；-1 = 无限
aiTask.Start();
// UI 上多次调用 aiTask.SendSoftwareTrigger();
```

### 模板 6：多卡采样时钟同步（Master → Slave via PXI_Trig0）

```csharp
// Master（内部时钟 + 导出到 PXI_Trig0）
master.Mode = AIMode.Finite;
master.SampleRate = 1000;
master.SamplesToAcquire = samples;
master.Trigger.Type = AITriggerType.Immediate;
master.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

// Slave（外部时钟从 PXI_Trig0 接入）
slave.Mode = AIMode.Finite;
slave.SamplesToAcquire = samples;
slave.SampleClock.Source = AISampleClockSource.External;
slave.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slave.SampleClock.External.ExpectedRate = 1000;

slave.Start();                            // 必须先启动 slave，再启动 master 产生时钟
master.Start();
```

### 模板 7：手动冷端补偿（不使用 TB68CJ 时）

```csharp
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.BuildInCJC.Enabled = false;                  // 关闭内置 CJC
aiTask.SetCJTemperature(0, 25.0);                   // 给通道 0 设定参考结温 25 ℃
// 或对所有通道：aiTask.SetCJTemperature(new double[]{25, 25, ...});
aiTask.Start();
```

### 模板 8：开路热电偶检测（OTD）

```csharp
var aiTask = new JY6314AITask(0);
aiTask.AddChannel(-1, ThermocoupleType.TypeK);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 1;
Dictionary<int, ThermocoupleConnectionStatus> r = aiTask.DetectOpenThermocouple();
// r[channelID] == ThermocoupleConnectionStatus.OpenCircuit 表示未接热电偶
```

### 模板 9：TB68CJ 接线端子在位检查

```csharp
if (aiTask.BuildInCJC.SensorStatus == CJSensorConnectionStatus.Normal) { /* 正常 */ }
// 启动时未检测到必需 TB68CJ 会抛异常，可预先：
aiTask.BuildInCJC.ReportBuildInCJException(true, true);
```

### 模板 10：DI / DO 单点

```csharp
// DI
var di = new JY6314DITask(0);
di.AddChannel(0);                        // 或 AddChannel(new[]{0,1,2});
di.Start();
bool v; di.ReadSinglePoint(ref v, 0);
di.Stop();

// DO
var dos = new JY6314DOTask(0);
dos.AddChannel(new[] { 0, 1 });
dos.Start();
dos.WriteSinglePoint(true, 0);
dos.WriteSinglePoint(new[] { true, false });
dos.Stop();
```

## 必须遵守的规则

1. **异常类型**：驱动抛 `JY6314.JYDriverException`。生产代码必须 `try/catch`，UI 上用 `MessageBox.Show(ex.Message)` 或写入日志。
2. **资源释放**：`FormClosing` / 异常分支 / Stop 按钮必须调用 `aiTask.Stop()`；连续任务 `Stop` 前不要先 Dispose buffer。
3. **采样率边界**：热电偶内部偏移清零需要 `< 40 Sa/s` 才能触发工频抑制；电压测量可到 3 kSa/s 上限。设置后建议读回 `aiTask.SampleRate`（实际生效值）。
4. **`SamplesToAcquire`** 仅 `AIMode.Finite` 有效；`SampleRate` 在 `AIMode.Single` 下语义变为"单点采样触发频率"。
5. **多通道 ReadData 布局**：`double[samples, channels]`——**行是样本，列是通道**，显示前需用 `ArrayManipulation.Transpose` 转成 `double[channels, samples]` 喂给 `StripChartX.Plot(...)`。
6. **多卡同步**：先启动 Slave（等时钟），再启动 Master（产生时钟）。反向会丢首个时钟。
7. **板卡编号**：`boardNumber` 从 0 开始，是槽位物理编号，不是任务序号。
8. **工程引用路径**：始终使用 `Bin\` 下的绝对路径 DLL，不要从示例 `bin\Debug\` 拷贝副本引用。
9. **命名空间**：`using JY6314;` 是唯一入口，不要混淆 `JYUSB1601/1202` 系列的 Task 类名。
10. **UI 控件**：示例统一使用 `SeeSharpTools.JY.GUI.StripChartX` 绘时序、`IndustrySwitch`/`LED` 做 DIO 指示；有 GUI 需求时优先复用，不要自己重写。

## 示例索引（可 100 % 参考复用）

示例根：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\JY6314.Examples\`。解决方案：`JY6314.Examples.sln`。

| 功能 | 目录（`Analog Input\` 下） |
|------|------|
| 控制台 AI 单点 | `Console AI Single Point\` |
| 控制台 AI 单点原始数据 | `Console AI Single Point Raw Data\` |
| AI 连续（单通道） | `Winform AI Continuous\` |
| AI 连续 + 自定义参考结温 | `Winform AI Continuous Custom RJ\` |
| AI 连续 + 数字触发 | `Winform AI Continuous Digital Trigger\` |
| AI 连续 + 软件触发 | `Winform AI Continuous Soft Trigger\` |
| AI 连续多通道（热电偶） | `Winform AI Continuous MutiChannel\` |
| AI 连续多通道（通用电压） | `Winform AI Continuous MutiChannel GeneralVoltage\` |
| AI 连续多通道（I32 原始值） | `Winform AI Continuous MutiChannel RawDataI32\` |
| AI 连续原始数据 | `Winform AI Continuous Raw Data\` |
| AI 有限 | `Winform AI Finite\` |
| AI 有限 + 数字触发 | `Winform AI Finite Digital Trigger\` |
| AI 有限 + 数字触发（电压） | `Winform AI Finite Digital Trigger Voltage\` |
| AI 有限多通道 | `Winform AI Finite MultiChannel\` |
| AI 多记录软件触发 | `Winform AI MultiRecord Soft Trigger\` |
| AI 单点多通道 | `Winform AI Single Point MultiChannel\` |
| AI 单点多通道（原始） | `Winform AI Single Point MultiChannel Raw Data\` |
| 多卡采样时钟同步 | `Winform AI_MultiCard SampleClock Sync\` |
| 开路热电偶检测 | `Winform Open Thermocouple Detection\` |
| TB68CJ 端子状态 | `Winform Terminal Block State\` |
| DI 单点 | `..\Digital Input\Winform DI Single Point\` |
| DO 单点 | `..\Digital Output\Winform DO Single Point\` |

## 附加资料（按需查阅）

- **完整 API 参考**（含所有属性/方法/重载/枚举取值/校准/工具类/P-Invoke）：见 [reference.md](reference.md)
- **15 个可复制运行的示例代码片段**（从控制台单点到多卡同步、OTD、自定义 RJ、生产级错误处理骨架）：见 [examples.md](examples.md)

## 新建项目检查表

- [ ] 目标框架 ≥ .NET Framework 4.6.2
- [ ] 添加 `JY6314.dll` 引用（HintPath 指向 `Bin\JY6314.dll`）
- [ ] 需要绘图/DIO 控件时添加 `SeeSharpTools.JY.GUI.dll`、`SeeSharpTools.JY.ArrayUtility.dll`
- [ ] `using JY6314;` 已加
- [ ] 所有 Task 创建、配置、`Start`/`ReadData`/`Stop` 均包含 `try/catch (JYDriverException)`
- [ ] `FormClosing` 中调用 `task?.Stop()`
- [ ] 连续采集使用 `AvailableSamples` 守卫后再 `ReadData`
- [ ] 多通道读取后通过 `ArrayManipulation.Transpose` 转置再绘图
- [ ] 采样率在 `[0.275, 3000] Sa/s` 区间
- [ ] 使用热电偶且有 TB68CJ 时保持 `BuildInCJC.Enabled = true`（默认）；否则手动 `SetCJTemperature`
