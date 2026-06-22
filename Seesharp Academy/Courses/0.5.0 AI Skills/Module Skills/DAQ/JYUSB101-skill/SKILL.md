---
name: jyusb101
description: Generate and review C#/Python code for the JYTEK JYUSB101 USB DAQ module. Use when the user asks about JYUSB101 analog input/output, digital input/output, counter input/output, driver references, or converting the official examples into new projects.
---

# JYUSB101 模块仪器使用技能

## 驱动与依赖引用（绝对路径）

本工作目录的驱动与辅助 DLL 统一放在：

```
C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\
```

| 文件 | 作用 | 绝对路径 |
|------|------|----------|
| `JYUSB101.dll` | JYUSB101 板卡驱动主程序集 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\JYUSB101.dll` |
| `JYUSB101.xml` | 驱动 IntelliSense / 文档注释 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\JYUSB101.xml` |
| `SeeSharpTools.JY.GUI.dll` | 官方范例中常用的 GUI 控件（LED、图表等） | `C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.GUI.dll` |
| `SeeSharpTools.JY.DSP.Fundamental.dll` | 波形生成辅助库 | `C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.DSP.Fundamental.dll` |
| `SeeSharpTools.JY.ArrayUtility.dll` | 多通道数组转置等工具 | `C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.ArrayUtility.dll` |

官方 C# 范例（`.csproj`）中的引用写法：

```xml
<Reference Include="JYUSB101">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\JYUSB101.dll</HintPath>
</Reference>
<Reference Include="SeeSharpTools.JY.GUI">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\SeeSharpTools.JY.GUI.dll</HintPath>
</Reference>
```

## 环境要求

- **.NET Framework**：4.0 或更高版本（官方范例目标框架为 `v4.0`）。
- **Python**：需要 `pythonnet` / `clr` 包，并通过 `clr.AddReference` 加载上述 DLL。
- **驱动版本**：范例基于 JYTek USB-101 Driver 1.8.1。
- **SeeSharpTools.JY.GUI**：1.4.7 或更高版本（若使用官方 GUI 控件）。

## 核心命名空间与任务类

```csharp
using JYUSB101;
```

| 功能 | 任务类 | 构造参数 | 工作模式枚举 |
|------|--------|----------|--------------|
| 模拟输入 | `JYUSB101AITask` | `new JYUSB101AITask(boardNum)` | `AIMode.Single / Finite / Continuous / Record` |
| 模拟输出 | `JYUSB101AOTask` | `new JYUSB101AOTask(boardNum)` | `AOMode.Single / Finite / ContinuousNoWrapping / ContinuousWrapping` |
| 数字输入 | `JYUSB101DITask` | `new JYUSB101DITask(boardNum)` | 单点读写 |
| 数字输出 | `JYUSB101DOTask` | `new JYUSB101DOTask(boardNum)` | 单点读写 |
| 计数器输入 | `JYUSB101CITask` | `new JYUSB101CITask(boardNum, counterId)` | `CIMode.Counter / Measure` |
| 计数器输出 | `JYUSB101COTask` | `new JYUSB101COTask(boardNum, counterId)` | `COMode.SingleGatedPulseGen / RetrigSinglePulseGen / SingleTrigContPulseGen / ContGatedPulseGen / SingleTrigContPulseGenPWM / ContGatedPulseGenPWM` |

所有操作都应包裹在 `try/catch (JYDriverException)` 中。

## 通用编程模式

### 1. 任务生命周期

```csharp
var task = new JYUSB101AITask(0);   // 1. 创建任务
task.AddChannel(0);                  // 2. 添加通道
task.Mode = AIMode.Continuous;       // 3. 配置模式/参数
task.SampleRate = 10000;
task.Start();                        // 4. 启动
// ... 读取或写入 ...
task.Stop();                         // 5. 停止
task.Channels.Clear();               // 6. 清理通道（需要时）
```

### 2. 异常处理模板

```csharp
try
{
    aiTask = new JYUSB101AITask(boardNum);
    aiTask.AddChannel(0);
    aiTask.Mode = AIMode.Single;
    aiTask.ReadSinglePoint(ref value);
}
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);
}
```

### 3. 窗口关闭时释放资源

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    if (aiTask != null) aiTask.Stop();
    if (aoTask != null) aoTask.Stop();
    if (ciTask != null) ciTask.Stop();
    if (coTask != null) coTask.Stop();
}
```

## 模拟输入（AI）

### 单点采集

```csharp
var aiTask = new JYUSB101AITask(0);
aiTask.Mode = AIMode.Single;
aiTask.AddChannel(0);
double value = 0;
aiTask.ReadSinglePoint(ref value);
Console.WriteLine($"AI0 = {value}");
```

### 有限点采集

```csharp
var aiTask = new JYUSB101AITask(0);
aiTask.AddChannel(0);
aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 10000;
aiTask.SamplesToAcquire = 1000;
aiTask.Start();

var buf = new double[1000];
aiTask.ReadData(ref buf, buf.Length, -1);
aiTask.Stop();
```

### 连续采集（单通道）

```csharp
var aiTask = new JYUSB101AITask(0);
aiTask.AddChannel(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;
aiTask.Start();

var buf = new double[aiTask.SampleRate / 2];
if (aiTask.AvailableSamples >= buf.Length)
{
    aiTask.ReadData(ref buf, buf.Length, -1);
    // 处理 buf...
}
```

### 多通道连续采集

多通道时，`ReadData` 返回二维数组 `[samples, channels]`；若需按通道显示，可使用 `SeeSharpTools.JY.ArrayUtility.ArrayManipulation.Transpose`。

```csharp
int chnCount = 4;
var aiTask = new JYUSB101AITask(0);
for (int i = 0; i < chnCount; i++) aiTask.AddChannel(i);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

var readValue = new double[1000, chnCount];
var displayValue = new double[chnCount, 1000];

aiTask.Start();
if (aiTask.AvailableSamples >= readValue.Length)
{
    aiTask.ReadData(ref readValue, -1);
    ArrayManipulation.Transpose(readValue, ref displayValue);
}
```

## 模拟输出（AO）

### 单点输出

```csharp
var aoTask = new JYUSB101AOTask(0);
aoTask.AddChannel(0);
aoTask.Mode = AOMode.Single;
aoTask.WriteSinglePoint(5.0);
```

### 有限点波形输出

```csharp
var aoTask = new JYUSB101AOTask(0);
aoTask.AddChannel(0);
aoTask.Mode = AOMode.Finite;
aoTask.UpdateRate = 10000;
aoTask.SamplesToUpdate = 10000;

var buf = new double[aoTask.SamplesToUpdate];
Generation.SineWave(ref buf, 5.0, 0, 1); // SeeSharpTools.JY.DSP.Fundamental

aoTask.WriteData(buf, -1);
aoTask.Start();
aoTask.WaitUntilDone(-1);
aoTask.Stop();
aoTask.Channels.Clear();
```

### 连续环绕输出

```csharp
var aoTask = new JYUSB101AOTask(0);
aoTask.AddChannel(0);
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 10000;

var buf = new double[10000];
Generation.SineWave(ref buf, 5.0, 0, 1);
aoTask.WriteData(buf, -1);
aoTask.Start();
```

## 数字输入/输出（DI/DO）

DI/DO 为单点非缓冲式操作。`AddChannel(new int[0])` 表示添加整个端口。

### 数字输入

```csharp
var diTask = new JYUSB101DITask(0);
diTask.AddChannel(new int[0]);          // 添加整个端口
bool[] values = new bool[8];
diTask.ReadSinglePoint(ref values);
```

### 数字输出

```csharp
var doTask = new JYUSB101DOTask(0);
doTask.AddChannel(new int[0]);          // 添加整个端口
bool[] values = new bool[] { true, false, true, false };
doTask.WriteSinglePoint(values);
```

## 计数器输入（CI）

`JYUSB101CITask` 构造函数需要 `counterId`（0 或 1）。

### 简单计数

```csharp
var ciTask = new JYUSB101CITask(0, 0);
ciTask.Mode = CIMode.Counter;
ciTask.Counter.InitialCount = 0;
ciTask.Counter.Direction = CountDirection.Up;
ciTask.Counter.ClockSource = CIClockSource.Internal;
ciTask.Start();

uint count = 0;
ciTask.ReadCounter(ref count);
```

### 单周期测量

```csharp
var ciTask = new JYUSB101CITask(0, 0);
ciTask.Mode = CIMode.Measure;
ciTask.Measure.Type = MeasureType.SinglePeriodMSR;
ciTask.Start();

double value = 0;
ciTask.ReadMeasure(ref value);
```

## 计数器输出（CO）

### 单门控脉冲生成

```csharp
var coTask = new JYUSB101COTask(0, 0);
coTask.Mode = COMode.SingleGatedPulseGen;
coTask.Clock.Source = COClockSource.Internal;
coTask.Gate.Source = COGateSource.External;
coTask.Pulse.Type = COPulseType.HighLowTime;
coTask.Pulse.Time.High = 0.005;
coTask.Pulse.InitialDelay = 0;
coTask.Start();
```

## Python 调用方式

```python
import clr
clr.AddReference("System")
from System import Double, Array

# 使用绝对路径加载驱动
clr.AddReference(r"C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\JYUSB101.dll")
from JYUSB101 import *

aiTask = JYUSB101AITask(0)
aiTask.Mode = AIMode.Finite
aiTask.SampleRate = 10000
aiTask.SamplesToAcquire = 1000
aiTask.AddChannel(0)

buf = Array.CreateInstance(Double, aiTask.SamplesToAcquire)
aiTask.Start()
aiTask.ReadData(buf, -1)
aiTask.Stop()
```

> Python 字符串中反斜杠需转义，或使用原始字符串 `r"..."`。

## 常见错误与注意事项

1. **驱动初始化失败 / `InitializeFailed`**：检查 USB 线缆连接与板卡供电。
2. **通道号越界**：AI 通道号取决于板卡规格；CI/CO 仅支持 `0` 或 `1`。
3. **采样率超限**：通过 `JYUSB101Device.MaxSampleRateSingleChannel` 查询单通道最大采样率。
4. **未停止任务就重新创建**：同一 boardNum 的同一资源可能被占用，需先 `Stop()` 并 `Channels.Clear()`。
5. **连续模式读取阻塞**：`ReadData` 的超时参数传 `-1` 表示无限等待；循环采集时建议先判断 `AvailableSamples`。
6. **DO/DI 使用 `new int[0]`**：表示操作整个端口；若操作单线可传入具体位号数组。
7. **拷贝 DLL**：生成项目时建议将 `JYUSB101.dll` 设为 `Copy Local = True`，避免运行时找不到依赖。

## 范例代码位置

官方范例位于：

```
C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\JYUSB101.Examples_V1.1.1\
```

| 目录 | 内容 |
|------|------|
| `Analog Input\` | 单点、有限点、连续、多通道、数字触发、流盘 |
| `Analog Output\` | 单点、有限点、连续 NoWrapping/Wrapping、多通道、数字触发 |
| `Digital Input\Winform DI Read` | 数字输入单点读取 |
| `Digital Output\Winform DO Write` | 数字输出单点写入 |
| `Counter Input\` | 计数、单周期/脉宽/边沿间隔测量 |
| `Counter Output\` | 单门控/可重触发/触发连续脉冲、PWM |
| `Multi Function\Winform AI+AO` | AI 与 AO 同时运行 |
| `Python\Samples\` | `AI Finite.py`、`AO Finite.py` 等 |

## 附加参考

- 完整类、枚举与属性速查表见 [reference.md](reference.md)。
- 驱动 XML 文档注释源文件：`C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB101\Bin\JYUSB101.xml`。
