---
name: seesharptools-sdk
description: 提供 JYTEK SeeSharpTools SDK（v2.0.3）完整开发指引，涵盖 GUI 控件（EasyChartX、StripChartX、DigitalChart、仪表、LED、Switch 等）、DSP 信号处理（频谱、滤波、谐波分析）、数学运算（ArrayArithmetic、Statistics）、文件与数据库（CSV、BIN、Excel、Word、SQLite）、通信（TCP、串口、Modbus）、传感器换算、TEDS 传感器数据表、日志报告等模块。当用户使用 SeeSharpTools、JYTEK 仪器库、EasyChartX 控件、DSP 频谱分析、SeeSharp 信号处理、C# WinForm 数据采集可视化开发时自动应用。
---

# SeeSharpTools SDK 开发技能

## 📂 文档导航

本 Skill 由三个文件组成，SKILL.md 为入口速查，详细内容按需查阅：

| 文件 | 定位 | 主要章节 |
|------|------|----------|
| **SKILL.md**（本文件） | 模块速查 + 常见错误 + 开发模式 | GUI 控件、DSP 信号处理、数学运算、文件/数据库、通信、传感器 |
| [**reference.md**](reference.md) | 完整 API 签名与参数说明 | EasyChartX API · DSP.Fundamental · DSP.Utility · DSP.Measurements · DSP.Utility.Spectrum · DSP.Utility.Filter1D |
| [**examples.md**](examples.md) | 端到端可运行代码示例 | EasyChartX 专项(EX-A~J) · 采集显示 · 谐波分析 · 滤波 · 文件/DB · TCP · PowerSpectrum 专项 · DSP 分析专项 · Filter1D 范例 · **Utility.Spectrum 专项(EX-Y1~Y12)** |

> **查找规则**：速查代码片段 → 本文件 ｜ 完整方法签名/参数表 → reference.md ｜ 完整可运行范例 → examples.md

---

## 快速开始

**项目引用路径**：`C:\SeeSharp\JYTEK\SeeSharpTools\Bin\` 目录下的 DLL

**必选核心 DLL**：
- `SeeSharpTools.JY.GUI.dll` — WinForm GUI 控件
- `SeeSharpTools.JY.DSP.Fundamental.dll` — 信号生成与基础频谱
- `SeeSharpTools.JY.Mathematics.dll` — 数学运算
- `SeeSharpTools.JY.ArrayUtility.dll` — 数组操作

**按需引用**：
| 功能 | DLL |
|------|-----|
| 高级 DSP | `SeeSharpTools.JY.DSP.Utility.dll`, `SeeSharpTools.JY.DSP.Measurements.dll` |
| 高级频谱分析 | `SeeSharpTools.JY.DSP.Utility.Spectrum.dll`（ValueTuple 返回值，无需 ref 预分配） |
| 文件读写 | `SeeSharpTools.JY.File.dll` |
| 数据库 | `SeeSharpTools.JY.Database.dll` |
| TCP 通信 | `SeeSharpTools.JY.TCP.dll` |
| 传感器换算 | `SeeSharpTools.JY.Sensors.dll` |
| TEDS（IEEE 1451.4 传感器电子数据表） | `SeeSharpTools.JY.TEDS.dll` |（v2.0.3 新增）
| 报告与日志 | `SeeSharpTools.JY.Report.dll` |
| 3D 图形 | `SeeSharpTools.JY.Graph3D.dll` |
| 统计分析 | `SeeSharpTools.JY.Statistics.dll` |
| 音频 | `SeeSharpTools.JY.Audio.dll` |

---

## 模块速查

### 1. GUI 控件（SeeSharpTools.JY.GUI）

#### EasyChartX — 通用波形图

> → API 详情：[reference.md § EasyChartX](reference.md)　｜　示例代码：[examples.md § EasyChartX 专项 EX-A~J](examples.md)

##### 绘图方法速查

```csharp
using SeeSharpTools.JY.GUI;

// ── 最常用：单通道等间距 ──
easyChartX1.Plot(yData, xStart: 0, xIncrement: 1.0 / sampleRate);

// ── 多通道：2D 数组，每行一个通道（Row 为默认）──
double[,] data = new double[channelCount, sampleCount];
easyChartX1.Plot(data, 0, 1.0 / sampleRate, MajorOrder.Row);

// ── 多通道：每列一个通道（JYUSB1601 ReadData 返回格式）──
double[,] data = new double[sampleCount, channelCount];  // ReadData 返回 [samples, channels]
easyChartX1.Plot(data, 0, 1.0 / sampleRate, MajorOrder.Column);

// ── X-Y 散点图（自定义X轴）──
easyChartX1.Plot(xData, yData);  // double[], double[]

// ── 多通道 X-Y 散点图 ──
easyChartX1.Plot(xDataArray, yDataArray);  // double[][], double[][]

// ── List<double> 数据（支持追加绘图）──
easyChartX1.Plot(listData, xStart: 0, xIncrement: 1.0 / sampleRate, count: listData.Count);
```

##### 坐标轴设置

```csharp
// 轴标题（必须在 Plot 之前或之后均可设置）
easyChartX1.AxisX.Title = "时间 (s)";
easyChartX1.AxisY.Title = "幅值 (V)";

// 自动缩放（默认 true）
easyChartX1.AxisX.AutoScale = false;
easyChartX1.AxisX.Maximum = 1.0;
easyChartX1.AxisX.Minimum = 0.0;
easyChartX1.AxisY.AutoScale = false;
easyChartX1.AxisY.Maximum = 10.0;
easyChartX1.AxisY.Minimum = -10.0;

// 对数坐标轴（频谱幅值显示）
easyChartX1.AxisY.IsLogarithmic = true;
easyChartX1.AxisY.LogarithmBase = 10.0;
easyChartX1.AxisY.LogLabelStyle = EasyChartXAxis.LogarithmicLabelStyle.F0;  // 或 E2
easyChartX1.AxisY.ShowLogarithmicLines = true;  // 显示对数网格线
// ✅ v2.0.3 修复：对数模式下非正数点（≤0）的显示异常和可能抛出异常的问题已修复

// 标签格式（C# 标准格式字符串）
easyChartX1.AxisX.LabelFormat = "F3";  // 保留3位小数
easyChartX1.AxisY.LabelFormat = "E2";  // 科学计数法
```

##### 系列（曲线）配置——LineSeries 详细说明

> **⚠️ LineSeries 是 EasyChartX 中最容易出错的地方，请严格遵循以下规则。**

**规则一：初始化（Designer.cs / InitializeComponent）必须用 `LineSeries.Add()`**

```csharp
// ✅ 正确：Designer.cs 或 InitializeComponent() 中初始化
SeeSharpTools.JY.GUI.EasyChartXSeries series1 = new SeeSharpTools.JY.GUI.EasyChartXSeries();
series1.Name      = "通道1";
series1.Color     = System.Drawing.Color.DodgerBlue;
series1.Width     = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Thin;    // Thin / Middle / Thick
series1.Type      = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.FastLine; // ← 见下方线型说明
series1.Marker    = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.None;   // ← 见下方标记说明
series1.Visible   = true;
series1.XPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
series1.YPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;    // Primary=左轴 / Secondary=右轴
this.easyChartX1.LineSeries.Add(series1);  // ✅ 必须是 LineSeries.Add()，不是 SeriesCollection.Add()

// ❌ 错误写法（此属性不存在）：
// this.easyChartX1.SeriesCollection.Add(series1);
```

**线型枚举（LineType）完整说明**：

| 枚举值 | 整数值 | 说明 | Marker 支持 | 适用场景 |
|--------|--------|------|------------|----------|
| `FastLine` | 6 | 高性能折线（默认推荐） | ❌ | 连续模拟信号、时域波形、高刷新率 |
| `Line` | 3 | 标准折线 | ✅ | 数据点较少、需要标记点时 |
| `StepLine` | 5 | 阶梯线 | ✅ | 数字信号、离散状态变化显示 |
| `Point` | 0 | 点状线（散点图） | ✅ | 离散测量值、散点可视化 |
| `Spline` | 4 | 样条曲线（平滑插值） | ✅ | 少量数据点需平滑过渡的场景 |
| `Bar` | 10 | 柱状图 | ❌ | 分类统计、频率分布直方图、各通道对比 |
| `Area` | 13 | 面积图（填充折线下方区域） | ❌ | 累积量、面积对比、能量分布 |

> ⚠️ `FastLine`、`Bar`、`Area` **不显示 Marker**；若需要标记点请改用 `Line`、`StepLine`、`Spline`、`Point` 类型。

**Bar 柱状图关键说明**：

使用 `LineType.Bar` 时，EasyChartX 将该系列渲染为纵向柱状图。X 轴数据充当类别索引（0, 1, 2, …），Y 轴数据为柱高。多系列 Bar 类型时柱子将自动并列显示。

```csharp
// ── 柱状图核心写法 ──────────────────────────────────────────────────────────
// 方式一：SeriesCount 快速配置（推荐）
easyChartX1.SeriesCount = 1;
easyChartX1.LineSeries[0].Type  = EasyChartXSeries.LineType.Bar;  // ✅ 设为柱状图
easyChartX1.LineSeries[0].Color = Color.SteelBlue;
easyChartX1.LineSeries[0].Name  = "测量值";

double[] barData = { 3.2, 7.1, 5.5, 9.0, 4.3 };        // 5 根柱
easyChartX1.Plot(barData);                               // X 自动为 0,1,2,3,4

// 方式二：Designer.cs 初始化（必须用全限定名）
SeeSharpTools.JY.GUI.EasyChartXSeries barSeries = new SeeSharpTools.JY.GUI.EasyChartXSeries();
barSeries.Type  = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.Bar;
barSeries.Color = System.Drawing.Color.SteelBlue;
barSeries.Name  = "分类统计";
this.easyChartX1.LineSeries.Add(barSeries);
```

**标记点枚举（MarkerType）完整说明**：

| 枚举值 | 说明 |
|--------|------|
| `None` | 无标记（高性能，推荐用于大数据量） |
| `Circle` | 圆形 |
| `Square` | 方形 |
| `Diamond` | 菱形 |
| `Triangle` | 三角形 |
| `Cross` | 十字形 |
| `Star5` | 五角星 |

> ⚠️ `FastLine` 类型**不显示 Marker**，若需要标记点请改用 `Line` 类型。

**规则二：运行时访问系列，`LineSeries[]` 和 `Series[]` 均可**

```csharp
// ✅ 两种方式等价，官方范例都有使用
easyChartX1.LineSeries[0].Color   = Color.Red;       // 修改颜色
easyChartX1.LineSeries[0].Name    = "新名称";         // 修改图例名称
easyChartX1.LineSeries[0].Visible = false;            // 隐藏某条线
easyChartX1.LineSeries[0].Type    = EasyChartXSeries.LineType.StepLine; // 改线型

// Series[] 与 LineSeries[] 指向相同集合（官方 GUIControlOverView 中使用）
easyChartX1.Series[0].Width  = EasyChartXSeries.LineWidth.Middle;
easyChartX1.Series[0].Marker = EasyChartXSeries.MarkerType.Star5;

// 遍历所有系列（用 SeriesCount，不要硬编码数量）
for (int i = 0; i < easyChartX1.SeriesCount; i++)
{
    easyChartX1.Series[i].Type = EasyChartXSeries.LineType.StepLine;
}
```

**规则三：直接设置系列数量（SeriesCount），无需手动 Add**

```csharp
// 最简方式：直接设置数量，系列自动创建（使用默认属性）
easyChartX1.SeriesCount = 4;
// 之后再逐一配置属性
easyChartX1.LineSeries[0].Color = Color.Blue;
easyChartX1.LineSeries[1].Color = Color.Red;
```

**规则四：双Y轴时 YPlotAxis 必须在初始化时设置**

```csharp
// 绑定副Y轴（右轴）—— 需在 LineSeries.Add() 前设置好
series2.YPlotAxis = EasyChartXAxis.PlotAxis.Secondary;
this.easyChartX1.LineSeries.Add(series2);

// 副Y轴属性设置
easyChartX1.AxisY2.Title    = "温度 (°C)";
easyChartX1.AxisY2.AutoScale = false;
easyChartX1.AxisY2.Minimum  = 0.0;
easyChartX1.AxisY2.Maximum  = 100.0;
```

##### 游标与标记

```csharp
// X 游标（缩放/定位模式）
easyChartX1.XCursor.Mode  = EasyChartXCursor.CursorMode.Zoom;  // 或 Cursor / Disabled
easyChartX1.XCursor.Value = 0.5;

// Y 游标
easyChartX1.YCursor.Mode  = EasyChartXCursor.CursorMode.Disabled;

// Tab 游标（可添加多个垂直标记线）
TabCursor tc = new TabCursor();
tc.Value = 0.3;
easyChartX1.TabCursors.Add(tc);

// 数据标记点
List<double> mx = new List<double>() { 0.1, 0.5 };
List<double> my = new List<double>() { 1.0, 3.0 };
easyChartX1.AddDataMarker(mx, my, Color.Cyan, DataMarkerType.Triangle);
// DataMarkerType: Triangle / Circle / Square / Diamond / Cross
easyChartX1.ClearMarker();
```

##### 其他常用属性

```csharp
easyChartX1.AutoClear     = true;   // 每次 Plot 前自动清除上次数据
easyChartX1.LegendVisible = true;   // 显示图例
easyChartX1.SplitView     = false;  // 多通道分图显示（true = 每通道独立区域）

// 数据存储模式（高频刷新建议 NoClone）
easyChartX1.Miscellaneous.DataStorage = DataStorageType.NoClone; // 或 Clone（安全）

// NaN / Inf 检查（开启会降低性能，默认关闭）
easyChartX1.Miscellaneous.CheckNaN      = false;
easyChartX1.Miscellaneous.CheckInfinity = false;
```

##### 事件处理

```csharp
// 视图变化（缩放/平移后触发）
easyChartX1.AxisViewChanged += (s, e) => {
    if (e.IsRaisedByMouseEvent)  // 区分鼠标操作和代码操作
    {
        Console.WriteLine($"X轴范围：{easyChartX1.AxisX.ViewMinimum} ~ {easyChartX1.AxisX.ViewMaximum}");
    }
};

// 游标位置变化
easyChartX1.CursorPositionChanged += (s, e) => {
    double xVal = easyChartX1.XCursor.Value;
};
```

##### ⚠️ 常见错误与正确用法

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| `easyChartX1.SeriesCollection.Add(s)` | `easyChartX1.LineSeries.Add(s)` | **初始化**必须用 `LineSeries.Add()`，`SeriesCollection` 属性不存在 |
| `FastLine` 类型 + 设置 `Marker` 期望显示标记点 | 改为 `LineType.Line` + 设置 `Marker` | `FastLine` 不渲染 Marker，需用 `Line` 类型 |
| `new EasyChartXSeries()` 在 `using SeeSharpTools.JY.GUI` 情况下仅用短名 | 在 Designer.cs 中务必用全名 `SeeSharpTools.JY.GUI.EasyChartXSeries` | Designer.cs 通常不含 using，必须全限定名 |
| 初始化后用 `Series[i]` 修改 `.YPlotAxis` 期望更换Y轴 | 双Y轴绑定须在 `LineSeries.Add()` 前通过 `series.YPlotAxis` 设置 | `YPlotAxis` 在运行时改动可能不生效 |
| `Plot(data, 0, 1, MajorOrder.Column)` ← JYUSB1601 | 确认 ReadData 返回格式 `[samples, channels]`，用 `MajorOrder.Column` | ReadData 返回列优先 |
| `AxisY.IsLogarithmic = true` 显示 dBV | 预先在数据层转换为 dBV 数组，Y轴用线性轴 | EasyChartX 对数轴不等同于 dBV |
| 后台线程直接调用 `Plot()` | `this.Invoke(() => easyChartX1.Plot(...))` | GUI 必须在 UI 线程更新 |

> → 更多 EasyChartX 示例：[examples.md § EX-A 到 EX-J](examples.md)

##### 实用方法（非 Plot）

```csharp
easyChartX1.Clear();          // 清除已绘数据（保留系列配置）
easyChartX1.SaveAsCsv();      // 将当前图表数据导出为 CSV（弹出保存对话框）
easyChartX1.SaveAsImage();    // 将当前图表截图导出为图片文件
```

#### StripChartX — 滚动/连续波形图

```csharp
// 追加数据（滚动显示）
stripChartX1.Plot(newData);
```

#### 仪表与指示控件

| 控件类型 | 命名空间类 | 核心属性 |
|----------|-----------|---------|
| 线性仪表 | `GaugeLinear` | `Value`, `Max`, `Min` |
| 水压仪表 | `PressureGauge` | `Value` |
| 温度计 | `Thermometer` | `Value` |
| 水箱 | `Tank` | `Value` |
| LED 矩阵 | `LedMatrix` | `Value[]` |
| 旋钮 | `Knob` | `Value` |
| 滑块 | `Slide` | `Value` |
| 数码管 | `SegmentBright` | `Value` |
| 滚动文字 | `ScrollingText` | `Text` |

#### 开关控件

```csharp
// 单开关
buttonSwitch1.Checked = true;

// 开关数组
buttonSwitchArray1.Value = new bool[] { true, false, true };
buttonSwitchArray1.ControlValueChanged += (s, e) => {
    int idx = e.SelectedIndex;
    bool val = e.Data;
};
```

#### 强度图（IntensityGraph / Psudocolor）

```csharp
intensityGraph1.Plot(data2D);  // double[rows, cols]
```

#### DigitalChart — 数字/游标图表（v2.0.3 新增）

> **`DigitalChart` 是 v2.0.3 新增的 WinForm 控件，适用于以数字方式浏览波形数据、逐点显示当前游标处的索引与值。**
> 命名空间：`SeeSharpTools.JY.GUI.DigitalChart`

```csharp
using SeeSharpTools.JY.GUI.DigitalChart;

// 在 WinForm 设计器中拖放 DigitalChart 控件
// 主要属性：
// digitalChart1.SeriesCount — 曲线数
// digitalChart1.ShowValue   — 当前游标处的索引和值（只读）
```

---

### 2. 信号处理（DSP）

> → DSP 完整 API：[reference.md § DSP 模块](reference.md)

#### 信号生成（SeeSharpTools.JY.DSP.Fundamental）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;

double[] signal = new double[1000];

// 正弦波
Generation.SineWave(ref signal, amplitude: 1.0, phase: 0, frequency: 100, samplingRate: 10000);

// 方波
Generation.SquareWave(ref signal, amplitude: 1.0, dutyCycle: 50, frequency: 100, samplingRate: 10000);

// 等差数列
Generation.Ramp(ref signal, start: 0, delta: 0.1);

// 均匀白噪声
Generation.UniformWhiteNoise(ref signal, amplitude: 0.1);
```

#### 频谱分析（Spectrum.PowerSpectrum）——完整调用指南

> **⚠️ PowerSpectrum 是 DSP 中最常用也最容易出错的 API，请严格遵循以下说明。**

> **🚨 致命错误防范：`ref spectrum` 参数必须预分配 `new double[N/2]`**
> 
> `Spectrum.PowerSpectrum` 的 `ref double[] spectrum` 参数**要求调用前已分配好正确大小的数组**。
> - **传 `null` → 直接抛出 `NullReferenceException`**（运行时崩溃，无任何提示）
> - **传错误大小数组（如 `new double[N]`）→ 可能导致 `IndexOutOfRangeException` 或数据截断**
> - **正确做法：始终 `new double[N / 2]`，其中 `N = signal.Length`**
>
> ```csharp
> // ❌ 致命错误：spectrum 为 null → NullReferenceException
> double[] spectrum = null;
> Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df);  // 💥 崩溃！
>
> // ❌ 错误：大小不对 → IndexOutOfRangeException 或数据异常
> double[] spectrum = new double[signal.Length];  // 应该是 N/2，不是 N
>
> // ✅ 正确：预分配 N/2 大小
> double[] spectrum = new double[signal.Length / 2];
> Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df);  // ✅ 安全
> ```
>
> **此规则同样适用于所有频谱类方法**：`AmplitudeSpectrum`、`PhaseSpectrum`、`DBFullScaleSpectrum`、`AdvanceComplexFFT`、多通道 `PowerSpectrum`。

##### 基本调用（最简形式，输出 V² 功率谱）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;

int N = signal.Length;                      // 输入信号长度
double[] spectrum = new double[N / 2];      // 🚨 必须预分配 N/2，传 null 会导致 NullReferenceException
double df;                                  // 频率分辨率（Hz），由 API 输出

Spectrum.PowerSpectrum(signal, samplingRate, ref spectrum, out df);
// df = samplingRate / N
// spectrum[k] 对应频率 = k * df（k = 0, 1, ..., N/2-1）
// 最大频率 = samplingRate / 2（奈奎斯特频率）

// 绘制频谱（X轴从0开始，步长df）
easyChartX1.Plot(spectrum, 0, df);
```

##### 完整参数签名（单通道）

```csharp
// 重载 1：常用版（SpectrumUnits 枚举）
Spectrum.PowerSpectrum(
    double[] waveform,          // 输入时域信号
    double samplingRate,        // 采样率 (S/s)
    ref double[] spectrum,      // ⚠️ ref：必须预分配 new double[N/2]
    out double df,              // out：频率分辨率 (Hz)
    SpectrumUnits unit,         // 输出单位（默认 V²，推荐 dBV）
    WindowType windowType,      // 窗函数（默认 None=矩形窗）
    double windowPara,          // 窗参数（仅 Kaiser/Gaussian/Dolph-Chebyshev 用，其他窗传 0）
    bool PSD                    // 是否输出功率谱密度（false=功率谱，true=功率谱密度）
);

// 重载 2：高级单位设置版（UnitConvSetting 对象）
// 适用于需要自定义单位比例/偏移的场景，无特殊需求时优先用重载 1
Spectrum.PowerSpectrum(
    double[] waveform,          // 输入时域信号
    double samplingRate,        // 采样率 (S/s)
    ref double[] spectrum,      // ⚠️ ref：必须预分配 new double[N/2]
    out double df,              // out：频率分辨率 (Hz)
    UnitConvSetting unitSettings, // 高级单位转换设置对象
    WindowType windowType,      // 窗函数
    double windowPara           // 窗参数（Kaiser/Gaussian/Dolph-Chebyshev 用，其他传 0）
);
// UnitConvSetting 示例：
// UnitConvSetting unitCfg = new UnitConvSetting();
// unitCfg.SourceUnit = SpectrumUnits.V2;
// unitCfg.TargetUnit = SpectrumUnits.dBV;
// Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df, unitCfg, WindowType.Hanning, 0);
```

##### SpectrumUnits 直接输出 dBV（⭐ 推荐，无需手动转换）

```csharp
// ✅ 推荐：直接输出 dBV，无需手动 20*log10(sqrt(power))
double[] spectrum = new double[signal.Length / 2];
double df;
Spectrum.PowerSpectrum(signal, samplingRate, ref spectrum, out df, SpectrumUnits.dBV);

// 绘制——Y轴直接是 dBV，不需要对数轴
easyChartX1.Plot(spectrum, 0, df);
easyChartX1.AxisY.Title = "dBV";
// ⚠️ 不要设 IsLogarithmic=true，dBV 数据已是对数尺度的线性表示
```

**SpectrumUnits 枚举**：

| 枚举值 | 说明 | 典型用途 |
|--------|------|--------|
| `V` | 电压幅度 (V) | 幅度谱显示 |
| `V2` | 功率 (V²)，默认 | 功率谱基础单位 |
| `W` | 功率 (Watt) | 功率测量 |
| `dBV` | 分贝伏特 ⭐ | **最常用，频谱可视化推荐** |
| `dBm` | 分贝毫瓦 | RF 信号分析 |
| `dBW` | 分贝瓦 | 大功率信号 |
| `dBmV` | 分贝毫伏 | 通信信号 |
| `dBuV` | 分贝微伏 | EMC/EMI 测试 |

##### 加窗（减少频谱泄漏）

```csharp
// Hanning 窗 + dBV 输出（通用推荐组合）
Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df,
    SpectrumUnits.dBV, WindowType.Hanning, 0, false);
```

**常用 WindowType**：`None`（矩形窗）、`Hanning`（汉宁窗，⭐ 通用推荐）、`Hamming`、`Blackman_Harris`、`Flat_Top`

##### 多通道 PowerSpectrum

> **⚠️ `Spectrum.PowerSpectrum` 在 DSP.Fundamental 中只支持单通道 `double[]`，无 `double[,]` 多通道重载！多通道需循环调用或使用 `SpectrumTask`。**

```csharp
// ✅ 正确：循环对每个通道单独调用
double df = 0;
for (int ch = 0; ch < channelCount; ch++)
{
    // 提取第 ch 行为 1D数组
    double[] chData    = new double[N];
    double[] chSpectrum = new double[N / 2];
    for (int i = 0; i < N; i++)
        chData[i] = multiSignal[ch, i];
    Spectrum.PowerSpectrum(chData, samplingRate, ref chSpectrum, out df,
        SpectrumUnits.dBV, WindowType.Hanning, 0, false);
    // chSpectrum 即第 ch 通道的频谱
}

// ❌ 错误：以下类型不存在！
// double[,] multiSignal  = new double[channelCount, N];
// double[,] multiSpectrum = new double[channelCount, N / 2];
// Spectrum.PowerSpectrum(multiSignal, ...)  ← 编译错误！
```

##### SpectrumTask——高级面向对象频谱分析

> **`SpectrumTask` 是对 `Spectrum` 静态方法的高级封装，支持窗函数配置、平均频谱、峰值查找、频带功率测量，推荐在需要复用配置或多次计算时使用。**

```csharp
using SeeSharpTools.JY.DSP.Fundamental;

// 1. 创建 SpectrumTask，配置采样率和窗函数
SpectrumTask specTask = new SpectrumTask(InputDataType.Real, sampleRate: 10000);
specTask.WindowType = WindowType.Hanning;   // 设置窗类型
specTask.Unit.Type  = SpectrumUnits.dBV;    // 单位：dBV

// 2. 计算频谱（ref 参数，必须预分配 N/2）
double[] spectrum = new double[signal.Length / 2];
specTask.GetSpectrum(signal, ref spectrum);

// 频率分辨率（df）从 SpectralInfomation 获取（注意拼写）
double df = specTask.SpectralInfomation.FreqDelta;

// 3. 查找峰值（返回 Peak 结构：PeakFrequency / PeakValue / PeakIndexFreq）
Peak mainPeak = specTask.FindPeak(spectrum);  // 全局最大峰
double peakFreq = mainPeak.PeakFrequency;
double peakAmp  = mainPeak.PeakValue;

// 查找所有超过阈值的峰（返回 Peak[]）
Peak[] peaks = specTask.FindPeak(spectrum, threshold: -20.0); // 阈值 -20 dBV

// 在指定频段内查找峰值
Peak bandPeak = specTask.FindPeak(spectrum, freqStart: 100, freqStop: 5000);

// 4. 频带功率测量（centerFreq ± bandwidth/2 范围内的功率，单位同 Unit.Type）
double bandPower = specTask.MeasurePowerInBand(spectrum, centerFreq: 1000, bandwidth: 200);

// 5. 平均频谱（降低随机噪声）
specTask.Average.Mode = SpectrumAverageMode.RMSAveraging; // 均方根平均
specTask.Average.Size = 16;  // 平均次数
for (int i = 0; i < 16; i++)
{
    specTask.GetSpectrum(newData, ref spectrum);  // 多次调用自动累积平均
}

// 6. 重置平均累积
specTask.Reset();
```

> **⚠️ SpectrumTask 注意事项**：
> - `SpectralInfomation`（注意：Infomation 非 Information，原始 API 拼写错误）提供 `FreqDelta`、`FFTSize`、`FFTCount`、`FreqStart`
> - `GetSpectrum` 的 `ref spectrum` 同样需要预分配 `new double[N/2]`
> - `FindPeak` 的 threshold 单位与 `Unit.Type` 一致（如设 dBV 则 threshold 为 dBV 值）
> - 若需独立获取幅度谱和相位谱：`GetSpectrum(data, ref amplitude, ref phase)`
> - ✅ **v2.0.3 修复**：`FindPeak(spectrum, freqStart, freqStop)` 指定频率范围时，当最大值索引在左边沿（< 3）处 PeakFrequency 计算偏差的 Bug 已修复
> - ✅ **v2.0.3 修复**：平均频谱（`Average.Mode`）中，若某次数据包含 NaN/Infinity，移动平均结果现在会自动用"直接求和"法重新计算，可自恢复，不再永久污染平均结果

##### 相关快捷方法

```csharp
// 峰值频率快速分析（⚠️ 参数是 dt=采样间隔，不是 samplingRate）
double dt = 1.0 / samplingRate;  // 注意：是采样间隔（秒），不是采样率
double peakFreq, peakAmp;
Spectrum.PeakSpectrumAnalysis(signal, dt, out peakFreq, out peakAmp);
// peakAmp = 1.414 * RMS（峰值电压）

// dBFS 频谱（相对满量程分贝值，用于 ADC 动态范围分析）
double[] dbfsSpectrum = new double[N / 2];
Spectrum.DBFullScaleSpectrum(signal, samplingRate, refFullScale: 10.0,
    ref dbfsSpectrum, out df);
```

##### ⚠️ PowerSpectrum 常见错误

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| `double[] spectrum = null;` | `new double[signal.Length / 2]` | **🚨 传 null 直接导致 `NullReferenceException`，运行时崩溃** |
| `new double[signal.Length]` | `new double[signal.Length / 2]` | 频谱长度必须 = N/2，不是 N |
| `spectrum` 未分配或大小错误 | 必须 `new double[N/2]` 预分配 | `ref` 参数要求已初始化的正确大小数组 |
| `PowerSpectrum(signal, sr, spectrum, df)` | 加 `ref spectrum, out df` | 必须带 `ref` 和 `out` 关键字，否则编译错误 |
| `PeakSpectrumAnalysis(signal, sampleRate, ...)` | `PeakSpectrumAnalysis(signal, 1.0/sampleRate, ...)` | dt 是采样间隔，不是采样率 |
| 手动 `20*log10(sqrt(p))` 转 dBV | 传 `SpectrumUnits.dBV` 参数 | API 内置单位转换更准确简洁 |
| dBV 结果设 `IsLogarithmic=true` | Y轴保持线性，标题写 "dBV" | dBV 已是对数尺度的线性表示 |

> → PowerSpectrum 专项示例：[examples.md § Spectrum.PowerSpectrum 专项](examples.md)

#### 高级频谱分析（SeeSharpTools.JY.DSP.Utility.Spectrum）

> **与 `DSP.Fundamental.Spectrum` 的区别**：`Utility.Spectrum` 使用 **ValueTuple 返回值**（无需 `ref` 预分配数组），API 更现代、更安全。提供 Welch/Periodogram/交叉谱/传递函数/频谱平均等高级分析功能。
> 
> **完整 API 参考见** [reference.md — DSP.Utility.Spectrum](reference.md)，**示例代码见** [examples.md — DSP.Utility.Spectrum 示例](examples.md)

> **⚠️ WindowType 命名空间冲突**：`DSP.Fundamental` 和 `Utility.Spectrum` 都定义了 `WindowType` 枚举。同时引用两个 DLL 时必须使用全限定名：
> ```csharp
> // 用于 Utility.Spectrum 的窗类型
> SeeSharpTools.JY.DSP.Utility.Spectrum.WindowType.Hanning
> // 用于 DSP.Fundamental 的窗类型
> SeeSharpTools.JY.DSP.Fundamental.WindowType.Hanning
> ```

##### Fourier（FFT / ChirpZ）

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;

// FFT（返回 ValueTuple，无需 ref 预分配）
var (mag, phase, df) = Fourier.Forward(signal, sampleRate,
    WindowType.Hanning, zeroPadding: true);
// mag: 幅度谱 double[]，phase: 相位谱 double[]，df: 频率分辨率

// ChirpZ（指定频率范围的高分辨率 FFT）
var (czMag, czPhase, czDf) = Fourier.ChirpZ(signal, sampleRate,
    freqStart: 900, freqStop: 1100, numPoints: 4096);
```

##### SpectralEstimation（功率谱密度 / Welch / Periodogram）

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;

// Periodogram（单通道）
var (psd, freq) = SpectralEstimation.Periodogram(signal, sampleRate,
    WindowType.Hanning, PowerUnits.dBV);

// Pwelch（Welch 法功率谱密度，自动分段+重叠+平均）
var (welchPsd, welchFreq) = SpectralEstimation.Pwelch(signal, sampleRate,
    segmentLength: 1024, WindowType.Hanning, PowerUnits.dBV);

// 交叉功率谱密度
var (cpsd, cpsdFreq) = SpectralEstimation.CrossPowerSpectralDensity(
    signalX, signalY, sampleRate, segmentLength: 1024);

// 传递函数估计（H1 估计器）
var (tfMag, tfPhase, tfFreq) = SpectralEstimation.TransferFunction(
    stimulus, response, sampleRate, segmentLength: 1024,
    TFEstimator.H1, PowerUnits.dB);

// 频谱平均（多次采集降低噪声）
var avg = new SpectrumAveraging(AveragingMode.RMS, averageCount: 16);
var (avgMag, avgPhase, avgDf) = SpectralEstimation.AverageMagPhaseSpectrum(
    signal, sampleRate, WindowType.Hanning, avg);
```

##### SingleTone / Distortion / SpectralFeature

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;

// 单音提取
var tone = SingleTone.ExtractSingleTone(signal, sampleRate);
// tone.Frequency, tone.Amplitude, tone.Phase

// 多音提取
var tones = SingleTone.ExtractMultiTone(signal, sampleRate, numTones: 3);

// 失真测量（需先算功率谱）
var (psd, freq) = SpectralEstimation.Periodogram(signal, sampleRate);
double snr   = Distortion.SNR(psd, freq, fundamentalFreq: 1000);
double sinad = Distortion.SINAD(psd, freq, fundamentalFreq: 1000);
double thd   = Distortion.THD(psd, freq, fundamentalFreq: 1000);
double sfdr  = Distortion.SFDR(psd, freq);

// 频谱特征
double bp    = SpectralFeature.BandPower(psd, freq, fLow: 500, fHigh: 1500);
double obw   = SpectralFeature.OccupiedBandwidth(psd, freq, percent: 99);
```

##### Hilbert / DCT / Conversion / Windows

```csharp
// Hilbert 变换（解析信号）
var (real, imag) = Hilbert.Forward(signal);
// 包络 = sqrt(real² + imag²)

// 离散余弦变换
var dctResult = DCT.Forward(signal);

// 单位转换
double dbVal = Conversion.MagTodB(magnitude);
double mag   = Conversion.dBToMag(dbVal);

// 窗函数（24 种）
double[] win = Windows.GetWindow(WindowType.Hanning, length: 1024);
```

##### ⚠️ Utility.Spectrum 常见错误

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| 混用两个库的 `WindowType` 不加限定 | 全限定名 `Spectrum.WindowType` / `Fundamental.WindowType` | 同时引用两个 DLL 时编译错误 CS0104 |
| 把 `Fundamental.Spectrum.PowerSpectrum` 的 `ref` 模式用在此库 | 此库返回 ValueTuple，直接 `var (a,b,c) = ...` | 无需预分配数组 |
| `Periodogram` 和 `Pwelch` 混淆 | Periodogram=单段 FFT，Pwelch=分段+重叠+平均 | 长信号推荐 Pwelch |
| `Distortion.SNR` 直接传时域信号 | 先算 Periodogram/PowerSpectrum 再传频域数据 | Distortion 方法需要频域输入 |

> → Utility.Spectrum 完整 API：[reference.md § DSP.Utility.Spectrum](reference.md)　｜　示例：[examples.md § EX-Y1~Y12](examples.md)

#### 滤波器（SeeSharpTools.JY.DSP.Utility — Filtering）

> **⚠️ 滤波器系数需自行生成或使用 MathNet.Filtering 库计算，SeeSharpTools 只提供滤波执行。**

```csharp
using SeeSharpTools.JY.DSP.Utility;

// ── FIR 滤波（有限脉冲响应）──────────────────────────────────────────
// coefficients: FIR 滤波器系数数组（可由 MathNet.Filtering 生成）
double[] filtered = SignalOperation.Filtering.FIRFilter(signal, coefficients);

// ── IIR 滤波（无限脉冲响应）──────────────────────────────────────────
// forwardCoefficients: 前向系数（分子）  reverseCoefficients: 反向系数（分母）
double[] filtered = SignalOperation.Filtering.IIRFilter(signal, forwardCoeff, reverseCoeff);

// ── 零相位滤波（双向 IIR，无相位延迟，未进行边界延拓）──────────────────
double[] filtered = SignalOperation.Filtering.ZeroPhaseFilter(signal, forwardCoeff, reverseCoeff);

// ── 中值滤波（非线性滤波，去除脉冲/毛刺噪声）────────────────────────
// leftRank: 窗口左侧元素数（≥0）  rightRank: 右侧元素数（<0 则 = leftRank）
double[] filtered = SignalOperation.Filtering.MedianFilter(signal, leftRank: 5, rightRank: -1);
// 窗口大小 = leftRank + rightRank + 1（rightRank=-1 时为 5+5+1=11）
```

**FIR 滤波器系数生成（需引用 `MathNet.Filtering.dll`）**：

```csharp
using MathNet.Filtering.FIR;

// 低通滤波器系数
double[] lpCoeff = FirCoefficients.LowPass(sampleRate, cutoffFreq, order: 64);
// 高通滤波器系数
double[] hpCoeff = FirCoefficients.HighPass(sampleRate, cutoffFreq, order: 64);
// 带通滤波器系数
double[] bpCoeff = FirCoefficients.BandPass(sampleRate, lowCutoff, highCutoff, order: 64);
// 带阻滤波器系数
double[] bsCoeff = FirCoefficients.BandStop(sampleRate, lowCutoff, highCutoff, order: 64);

// 应用滤波
double[] result = SignalOperation.Filtering.FIRFilter(signal, lpCoeff);
```

##### ⚠️ 滤波器常见错误

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| 直接调用 FIRFilter 不传系数 | 先用 MathNet 生成系数再传入 | SeeSharpTools 不含滤波器设计功能 |
| IIR 前向/反向系数搞反 | forwardCoeff=分子, reverseCoeff=分母 | 对应传递函数 H(z)=B(z)/A(z) |
| 中值滤波 leftRank 传 0 | leftRank ≥ 1 才有滤波效果 | leftRank=0 等于不滤波 |
| ZeroPhaseFilter 边界失真 | 预留边界余量或截断首尾 | 未进行边界延拓，首尾可能失真 |

> → 滤波器示例：[examples.md § 示例 4 FIR 滤波器](examples.md)

#### MedianFilter 类（SeeSharpTools.JY.DSP.Utility）

> 滑动窗口中值滤波器，适合去除脉冲噪声和毛刺。与 `Filtering.MedianFilter` 功能类似但接口不同。

```csharp
using SeeSharpTools.JY.DSP.Utility;

// windowLength 必须为奇数且 ≥ 3（格式 2N+1）
double[] filtered = MedianFilter.Process(signal, windowLength: 5);
```

#### 高级滤波器设计与执行（SeeSharpTools.JY.DSP.Utility.Filter1D）

> **与旧版 `DSP.Utility` 滤波的区别**：旧版 `SignalOperation.Filtering` 只执行滤波，需要 MathNet 生成系数。`Filter1D` 是独立完整库，自带 IIR/FIR **设计** + **执行** + **分析**，不需要 MathNet。

**引用 DLL**：`SeeSharpTools.JY.DSP.Utility.Filter1D.dll`（独立 DLL，需单独添加引用）

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;
```

---

##### IIR 滤波器设计（IIRDesign）

`IIRDesign` 是 `abstract sealed static class`，所有方法为静态方法，**返回 `ValueTuple<double[], double[]>`（需 C# 7.0+ / .NET 4.7+）**。`wn` 参数单位为 **Hz**（非归一化），库内部自动完成归一化。

```csharp
// ── Butterworth 低通（最常用，通带平坦）──────────────────────────────
// n=4: 滤波器阶数   wn=截止频率(Hz)   sampleRate=采样率
var (b, a) = IIRDesign.Butter(4, new double[] { 500.0 }, IIRBandType.Lowpass, 10000.0);

// ── Butterworth 高通 ────────────────────────────────────────────────
var (b, a) = IIRDesign.Butter(4, new double[] { 500.0 }, IIRBandType.Highpass, 10000.0);

// ── Butterworth 带通（wn 必须传两个元素：[低截, 高截]）──────────────
var (b, a) = IIRDesign.Butter(4, new double[] { 200.0, 800.0 }, IIRBandType.Bandpass, 10000.0);

// ── Butterworth 带阻（陷波）──────────────────────────────────────────
var (b, a) = IIRDesign.Butter(4, new double[] { 400.0, 600.0 }, IIRBandType.Bandstop, 10000.0);

// ── Chebyshev I（通带等纹波，过渡带更陡）────────────────────────────
// rp=0.5: 通带纹波(dB)
var (b, a) = IIRDesign.Cheby1(4, new double[] { 500.0 }, rp: 0.5, IIRBandType.Lowpass, 10000.0);

// ── Chebyshev II（阻带等纹波，通带更平坦）───────────────────────────
// rs=60: 阻带衰减(dB)
var (b, a) = IIRDesign.Cheby2(4, new double[] { 500.0 }, rs: 60.0, IIRBandType.Lowpass, 10000.0);

// ── Elliptic（最小阶数，通带+阻带均等纹波）──────────────────────────
// rp=0.5: 通带纹波(dB)   rs=60: 阻带衰减(dB)
var (b, a) = IIRDesign.Ellip(4, new double[] { 500.0 }, rp: 0.5, rs: 60.0, IIRBandType.Lowpass, 10000.0);

// ── 陷波滤波器（Notch，去除单频干扰，如 50Hz 工频）──────────────────
// w0=50: 中心频率(Hz)   bw=10: 带宽(Hz)
var (b, a) = IIRDesign.IIRNotch(w0: 50.0, bw: 10.0, sampleRate: 10000.0);

// ── 峰值/谐振滤波器（Peak）──────────────────────────────────────────
var (b, a) = IIRDesign.IIRPeak(w0: 1000.0, bw: 200.0, sampleRate: 10000.0);

// ── 自动估阶设计（TransferFunction，指定通带/阻带规格）──────────────
// wp=通带截止(Hz)  ws=阻带截止(Hz)  rp=通带纹波(dB)  rs=阻带衰减(dB)
var (b, a) = IIRDesign.TransferFunction(
    new double[] { 500.0 }, new double[] { 700.0 },
    rp: 3.0, rs: 60.0,
    IIRBandType.Lowpass, IIRDesignMethod.Butterworth, 10000.0);

// ── SOS（二阶节）设计——高阶滤波器推荐用 SOS，数值更稳定 ────────────
double[] sos = IIRDesign.SOSDesign(
    n: 8, new double[] { 500.0 }, rp: 3.0, rs: 60.0,
    IIRBandType.Lowpass, IIRDesignMethod.Butterworth, 10000.0);
```

##### IIR 滤波执行（IIRFiltering）

```csharp
// ── 基本 IIR 滤波（整段数据）──────────────────────────────────────
double[] filtered = IIRFiltering.IIRFilter(b, a, signal);

// ── SOS 格式滤波（高阶时推荐，数值更稳定）────────────────────────
double[] filtered = IIRFiltering.SOSFilter(sos, signal);

// ── 零相位滤波（双向，无相位延迟，适合离线分析）─────────────────
// PadType: None / Odd / Even / Constant   padLength: 建议 ≥ 滤波器阶数*3
double[] filtered = IIRFiltering.ZeroPhaseFilter(sos, signal, PadType.Odd, padLength: 50);
double[] filtered = IIRFiltering.ZeroPhaseFilter(b, a, signal, PadType.Odd, padLength: 50);

// ── 连续/流式滤波（保留状态 z，适合实时采集分段处理）────────────
// 初始化状态向量（使用 IIRFilter_IC 或手动零初始化）
double[] z = IIRFiltering.IIRFilter_IC(b, a);  // 稳态初始条件
// 处理第一段
var (out1, z) = IIRFiltering.IIRFilter(b, a, block1, z);
// 处理第二段（z 从上一段末尾继续）
var (out2, z) = IIRFiltering.IIRFilter(b, a, block2, z);

// ── SOS 流式滤波 ──────────────────────────────────────────────────
double[] zi = IIRFiltering.SOSFilter_zi(sos);  // SOS 初始条件
var (out1, zi) = IIRFiltering.SOSFilter(sos, block1, zi);
var (out2, zi) = IIRFiltering.SOSFilter(sos, block2, zi);

// ── 移动平均 / 指数平滑（SmoothingFilter）────────────────────────
// SmoothingType: MovingAverage / Exponential
// halfWidth: 移动平均半窗（窗口=2*halfWidth+1），指数平滑时忽略
// timeConstant: 指数平滑时间常数（MovingAverage 时忽略）
double[] smoothed = IIRFiltering.SmoothingFilter(
    signal, sampleRate: 10000,
    SmoothingType.MovingAverage, halfWidth: 5,
    FilterShape.Rectangular, timeConstant: 0);
double[] expSmoothed = IIRFiltering.SmoothingFilter(
    signal, sampleRate: 10000,
    SmoothingType.Exponential, halfWidth: 0,
    FilterShape.Exponential, timeConstant: 0.01);
```

##### FIR 滤波器设计（FIRDesign）

```csharp
// ── 窗函数法设计 FIR（最常用）───────────────────────────────────
// n=64: 滤波器阶数（偶数）  wn=截止频率(Hz)  WindowType: Hamming/Hann/Kaiser...
// scale=true: 归一化增益
double[] b = FIRDesign.Window(
    n: 64,
    wn: new double[] { 500.0 },         // 低通截止频率(Hz)
    WindowType.Hamming, windowParam: 0,
    FIRBandType.Lowpass, scale: true,
    sampleRate: 10000.0);

// ── FIR 带通（wn 传两个截止频率）────────────────────────────────
double[] b = FIRDesign.Window(
    64, new double[] { 200.0, 800.0 }, WindowType.Hamming, 0,
    FIRBandType.Bandpass, true, 10000.0);

// ── FIR 高通 ─────────────────────────────────────────────────────
double[] b = FIRDesign.Window(
    64, new double[] { 500.0 }, WindowType.Hamming, 0,
    FIRBandType.Highpass, true, 10000.0);

// ── Kaiser 窗（控制主瓣旁瓣，windowParam = beta）─────────────────
double[] b = FIRDesign.Window(
    100, new double[] { 500.0 }, WindowType.Kaiser, windowParam: 8.0,
    FIRBandType.Lowpass, true, 10000.0);

// ── Parks-McClellan（等纹波，性能最优）──────────────────────────
// bands: 频带边界(Hz)对  amplitudes: 各频带目标增益  weight: 各频带权重
double[] b = FIRDesign.ParksMcClellan(
    order: 64,
    bands:      new double[] { 0, 400, 600, 5000 },   // [通带低, 通带高, 阻带低, 阻带高]
    amplitudes: new double[] { 1, 1, 0, 0 },           // 通带增益=1，阻带增益=0
    weight:     new double[] { 1, 10 },                // 阻带权重更大=更好阻带抑制
    PMFilterType.Bandpass,
    sampleRate: 10000.0, griddensity: 16);
```

##### FIR 滤波执行（FIRFiltering）

```csharp
// ── 基本 FIR 滤波 ────────────────────────────────────────────────
double[] filtered = FIRFiltering.FIRFilter(b, signal);
// ⚠️ 输出长度 = signal.Length（与输入等长）

// ── FFT 快速滤波（长信号时速度更快）────────────────────────────
double[] filtered = FIRFiltering.FFTFilter(b, signal);

// ── 连续/流式 FIR 滤波（保留末尾状态）──────────────────────────
double[] z = new double[b.Length - 1];  // 状态向量长度 = 阶数
var (out1, z) = FIRFiltering.FIRFilter(b, block1, z);
var (out2, z) = FIRFiltering.FIRFilter(b, block2, z);

// ── Savitzky-Golay 多项式平滑滤波 ───────────────────────────────
// order=3: 多项式阶数  frameLength=11: 窗口长度（奇数）
double[] smoothed = FIRFiltering.SgolayFilter(
    signal, order: 3, frameLength: 11, weights: null);
```

##### 特殊滤波（SpecialFiltering）

```csharp
// ── 微分（Differentiate）────────────────────────────────────────
// BandwidthOption: NarrowBand / WideBand
// initVal: 初始状态（ref 参数，传 null 自动初始化）
double[] initVal = null;
double[] diff = SpecialFiltering.Differentiate(signal, fs: 10000, BandwidthOption.WideBand, ref initVal);

// ── 积分（Integrate）────────────────────────────────────────────
// HPF6dBFreq: 高通截止频率（Hz），防止直流积分漂移，一般设 0.5~5 Hz
double[] initVal = null;
double[] integ = SpecialFiltering.Integrate(signal, fs: 10000, HPF6dBFreq: 1.0, BandwidthOption.WideBand, ref initVal);
```

##### 多速率处理（Multirate）

```csharp
// ── 抽取（降采样 + 抗混叠滤波）─────────────────────────────────
// deciFactor=4: 抽取因子  AntialiasingFilterType: IIR / FIR
// order=8: 抗混叠滤波器阶数（IIR 推荐 8，FIR 推荐 32）
double[] decimated = Multirate.Decimate(signal, deciFactor: 4, AntialiasingFilterType.IIR, order: 8);

// ── 插值（升采样 + 低通滤波）────────────────────────────────────
double[] interpolated = Multirate.Interpolate(signal, interpFactor: 4);

// ── 有理数比率重采样（p/q，如 44100→48000 = 147/160）────────────
// p=升采样因子  q=降采样因子  n=滤波器阶数  beta=Kaiser 窗 beta
double[] resampled = Multirate.Resample(signal, p: 147, q: 160, n: 10, beta: 5.0);

// ── 简单降采样（不含抗混叠，可能产生频率混叠）────────────────────
double[] ds = Multirate.Downsample(signal, downsamplingFactor: 4, offset: 0);

// ── 简单升采样（插零）──────────────────────────────────────────
double[] us = Multirate.Upsample(signal, upsamplingFactor: 4, offset: 0);
```

##### 滤波器特性分析（IIRAnalysis / FIRAnalysis / FilterExploration）

```csharp
// ── IIR Bode 图（幅频 + 相频 + 频率轴）────────────────────────
// n=512: 频率点数
var (magnitude, phase, freq) = IIRAnalysis.Bode(b, a, sampleRate: 10000, n: 512);
easyChartX_bode.Plot(freq, magnitude);  // 绘制幅频响应

// ── FIR Bode 图 ─────────────────────────────────────────────────
var (magnitude, phase, freq) = FIRAnalysis.Bode(b, sampleRate: 10000, n: 512);

// ── 滤波器稳定性与相位特性检查 ──────────────────────────────────
bool stable   = FilterExploration.IsStable(b, a);       // 是否稳定
bool isFIR    = FilterExploration.IsFIR(a);              // a=[1] 即为 FIR
bool linPhase = FilterExploration.IsLinearPhase(b, a, tol: 1e-6); // 线性相位
int  order    = FilterExploration.FilterOrder(b, a);     // 滤波器阶数
```

##### ExpressFilter（设计器组件）

> `ExpressFilter` 是可拖放到 WinForm 设计器的组件（`Component`），适合在 UI 设计时配置滤波器参数，程序运行时直接调用。  
> ✅ **v2.0.3 修复**：新添加 ExpressFilter 组件时报错的问题已修复，初始化和配置逻辑已优化。

**FilterType 枚举**：`None`（未设置）/ `IIR` / `FIRWindow` / `Classic`（含 KaiserWindow / Equiripple 等经典设计）

**主要 API**

| 成员 | 类型 / 签名 | 说明 |
|------|------------|------|
| `Configuration` | `FilterConfig` | 读写滤波器配置 |
| `Coeff_b` | `double[]` | 分子系数（IIR / FIR 均有效） |
| `Coeff_a` | `double[]` | 分母系数（**仅 IIR 有效**，FIR 为 null） |
| `DoFilter(x, zeroPhase)` | `double[]` | 单次滤波；`zeroPhase=true` 零相位 |
| `DoFilter(x, z)` | `double[]` | 流式分帧滤波；`z` 为跨帧状态数组 |
| `DoFilter(x2D, z)` | `double[,]` | 多通道分帧滤波（2D 数组版） |

**用法 1：代码配置 IIR（最常用）**

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

// 在设计器拖放 ExpressFilter 组件后，代码中直接配置：
expressFilter1.Configuration = new FilterConfig
{
    Type = FilterType.IIR,
    IIR = new IIRConfig
    {
        DesignMethod        = "Butterworth",   // Butterworth/ChebyshevI/ChebyshevII/Elliptic/Bessel
        BandType            = IIRBandType.Lowpass,
        Order               = 4,
        LowCutoffFrequency  = 500.0,           // Hz（低通/带通低端）
        HighCutoffFrequency = 0,               // Hz（带通/带阻高端，低通置 0）
        SampleRate          = 10000.0,
        Rp                  = 3.0,             // 通带波纹 dB
        Rs                  = 60.0,            // 阻带衰减 dB
    }
};

double[] filtered = expressFilter1.DoFilter(signal, zeroPhase: false);  // 常规
double[] zeroLag  = expressFilter1.DoFilter(signal, zeroPhase: true);   // 零相位（适合离线）

// 获取设计后系数（可复用于 IIRFiltering 直接调用）
double[] b = expressFilter1.Coeff_b;   // 分子系数
double[] a = expressFilter1.Coeff_a;   // 分母系数（IIR 专用）
```

**用法 2：代码配置 FIR 窗函数**

```csharp
expressFilter1.Configuration = new FilterConfig
{
    Type = FilterType.FIRWindow,
    FIRWindow = new FIRWindowConfig
    {
        BandType            = FIRBandType.Lowpass,
        Order               = 64,
        LowCutoffFrequency  = 500.0,
        HighCutoffFrequency = 0,
        SampleRate          = 10000.0,
        WindowType          = WindowType.Hamming,
    }
};
double[] filtered = expressFilter1.DoFilter(signal, zeroPhase: false);
// ⚠️ FIR 时 Coeff_a 为 null，只有 Coeff_b 有效
```

**用法 3：通过 FilterDesignForm 动态交互设计（推荐用于 WinForm 产品）**

> `FilterDesignForm` 是 `SeeSharpTools.JY.DSP.Utility.Filter1D` 内建的图形化滤波器设计窗体，  
> 用户可在界面中选择滤波类型、设计方法、通带/阻带频率并实时预览频率响应，点击确定后结果写回 `Configuration`。

```csharp
// "设计" 按钮点击事件
private void btDesign_Click(object sender, EventArgs e)
{
    // ① 同步采样率：确保设计窗体使用当前正确的采样率
    //    ⚠️ 切换采集参数后必须同步，否则频率刻度与实际不符
    expressFilter1.Configuration.IIR.SampleRate = _sampleRate;

    // ② 创建 FilterDesignForm，传入当前 FilterConfig
    var filterDesignForm = new FilterDesignForm(expressFilter1.Configuration);

    // ③ 显示对话框；仅在用户点击"确定"时才更新配置
    if (filterDesignForm.ShowDialog() == DialogResult.OK)
    {
        // ④ 将用户设计好的 FilterConfig 写回 ExpressFilter 组件
        expressFilter1.Configuration = filterDesignForm.GetUpdatedConfig();
    }
    // 用户点击"取消"时 expressFilter1.Configuration 保持原值不变
}

// "滤波" 按钮——使用已配置的滤波器执行滤波
private void btFilter_Click(object sender, EventArgs e)
{
    double[] y = expressFilter1.DoFilter(signal, zeroPhase: true);
    easyChartX1.Plot(y, 0, 1.0 / _sampleRate);
}
```

> **采样率同步原则**：`Configuration.IIR.SampleRate`（以及 `Configuration.FIRWindow.SampleRate`）  
> 必须与实际采集任务的采样率保持一致，否则滤波器截止频率会出现偏差。  
> 每次修改采集参数后，打开 `FilterDesignForm` 前先同步：
>
> ```csharp
> // IIR 类型
> expressFilter1.Configuration.IIR.SampleRate = _sampleRate;
> // FIRWindow 类型
> expressFilter1.Configuration.FIRWindow.SampleRate = _sampleRate;
> ```

**用法 4：实时流式分帧（保持跨帧状态 z）**

```csharp
// 在 Form 字段中保存状态数组，首次使用前初始化
private double[] _filterState = null;

private void OnNewFrameArrived(double[] frame)
{
    // _filterState 首次为 null 时传入，DoFilter 内部会自动初始化
    double[] filtered = expressFilter1.DoFilter(frame, _filterState);
    // ⚠️ 若 DoFilter 不接受 null 状态，需手动预分配：
    // if (_filterState == null)
    //     _filterState = new double[Math.Max(expressFilter1.Coeff_b.Length,
    //                                        expressFilter1.Coeff_a?.Length ?? 0) - 1];
    easyChartX1.Plot(filtered, 0, 1.0 / sampleRate);
}
```

**用法 5：FilterType.Classic（设计器预设经典滤波器）**

```csharp
// Classic 配置通常由 FilterDesignForm 或 Designer.cs 自动生成，一般无需手写
// 若需手动设置：
expressFilter1.Configuration = new FilterConfig
{
    Type = FilterType.Classic,
    Classic = new ClassicDesignFilterConfig
    {
        Type            = AntialiasingFilterType.FIR,   // 或 IIR
        BandType        = "Lowpass",                    // "Lowpass/Highpass/Bandpass/Bandstop"
        DesignModel     = "KaiserWindow",               // FIR: KaiserWindow/Equiripple
        SampleRate      = 1.0,                          // 归一化采样率
        LowerPassFrequency  = 0.2,
        HigherPassFrequency = 0.25,
        LowerStopFrequency  = 0.25,
        HigherStopFrequency = 0.33,
        PassbandRipple      = 0.08,
        StopbandRejection   = 65.0,
        // b/a 系数由 FilterDesignForm 设计后自动填入
    }
};
```

##### ⚠️ Filter1D 常见错误

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| `using SeeSharpTools.JY.DSP.Utility;` | `using SeeSharpTools.JY.DSP.Utility.Filter1D;` | Filter1D 是独立命名空间和 DLL |
| `wn = new double[]{0.1}` 传归一化频率 | `wn = new double[]{500.0}` 传 Hz，加 `sampleRate` | 库内部已自动归一化 |
| 带通/带阻 `wn` 只传一个值 | `wn = new double[]{低截, 高截}` | 带通/带阻必须两个截止频率 |
| 高阶 IIR 用 `IIRFilter(b,a,x)` 精度差 | 改用 `SOSDesign` + `SOSFilter` | 高阶(>8) IIR 用 SOS 防止数值不稳定 |
| 流式滤波不传状态 `z` | 每帧保留 `z`，下帧继续传入 | 忽略状态导致帧间断点/跳跃 |
| `ZeroPhaseFilter` 用于实时采集 | 仅用于离线后处理 | 零相位是双向滤波，因果性为 0，无法实时 |
| `FIRBandType.DC_pass` vs `DC_stop` 混淆 | DC_pass=低通, DC_stop=高通 | 非标准命名，易混淆 |
| `var (b,a) = IIRDesign.Butter(...)` 编译失败 | 确认项目目标框架 ≥ .NET 4.7 | ValueTuple 需要 .NET 4.7+ 或 `System.ValueTuple` NuGet |
| 不检查 `IsStable` 直接滤波 | 设计后先调 `FilterExploration.IsStable(b,a)` | 参数不当可能设计出不稳定滤波器 |

> → Filter1D 完整 API：[reference.md § DSP.Utility.Filter1D](reference.md)　｜　示例：[examples.md § Filter1D 范例](examples.md)

#### 信号操作（SeeSharpTools.JY.DSP.Utility — Analog）

```csharp
using SeeSharpTools.JY.DSP.Utility;

// ── 基础信号操作 ──────────────────────────────────────────────────────
// 求反（每个元素取相反数）
double[] inverted = SignalOperation.Analog.Invert(signal);

// 卷积（线性卷积 direct 方法）
double[] conv = SignalOperation.Analog.Convolution(signal1, signal2);
// 返回长度 = signal1.Length + signal2.Length - 1

// 零填充至 2 的幂次（FFT 前常用）
double[] padded = SignalOperation.Analog.ZeroPadder(signal, ignoreCurrent: false);
// ignoreCurrent=false: 总是填充  true: 仅非2的幂时填充

// ── 相关分析 ──────────────────────────────────────────────────────────
// 自相关（Normalization: None / Biased / Unbiased）
double[] acorr = SignalOperation.Analog.AutoCorrelation(signal, Normalization.Biased);

// 互相关
double[] xcorr = SignalOperation.Analog.CrossCorrelation(signal1, signal2, Normalization.Unbiased);

// ── 重采样 ────────────────────────────────────────────────────────────
// 均匀采样重采样（InterpolationMode: Linear / Spline / FIRFilter）
double[] resampled = SignalOperation.Analog.Resampling(
    signal, originalSamplingRate: 10000, newSamplingRate: 44100,
    InterpolationMode.Spline);

// 非均匀采样重采样
double[] resampled = SignalOperation.Analog.Resampling(
    signal, indexes, newSamplingRate: 44100, InterpolationMode.Linear);

// ── 升降采样 ──────────────────────────────────────────────────────────
// 升采样（在采样间插入零，upsamplingFactor > 0）
double[] upsampled = SignalOperation.Analog.Upsample(signal, upsamplingFactor: 4, leadingZeros: 0);

// 降采样（downsamplingFactor > 0，averaging: 是否平均）
double[] downsampled = SignalOperation.Analog.Downsample(signal, downsamplingFactor: 4, averaging: true);

// ── 去趋势 ────────────────────────────────────────────────────────────
// DetrendMethod: Linear / Polynomial / Exponential
double[] detrended, trend;
SignalOperation.Analog.Detrend(signal, out detrended, out trend,
    DetrendMethod.Linear, order: 1);  // order 仅 Polynomial 有效

// ── 归一化与缩放 ──────────────────────────────────────────────────────
// 缩放至 [-1, 1]
double scale, offset;
double[] scaled = SignalOperation.Analog.Scale(signal, out scale, out offset);

// 归一化至 (0, 1) 统计分布
double stdDev, mean;
double[] normalized = SignalOperation.Analog.Normalize(signal, out stdDev, out mean);

// ── 相位展开 ──────────────────────────────────────────────────────────
// PhaseUnit: radian / degree
double[] unwrapped = SignalOperation.Analog.UnwrapPhase(phaseArray, PhaseUnit.radian);

// ── 移动平均 ──────────────────────────────────────────────────────────
// WeightAverageType: Spencer_15_term / Henderson_7_term / Henderson_9_term / Henderson_13_term / Henderson_23_term
double[] smoothed = SignalOperation.Analog.WeightMovingAverage(signal, WeightAverageType.Henderson_7_term, null);
```

#### 峰谷分析（PeakValleyAnalysis）

> **⚠️ 实际 API 与网络文档差异极大，必须使用以下正确签名（来自 SeeSharpExamples）。**

```csharp
using SeeSharpTools.JY.DSP.Utility;

// ✅ 正确：FindPeaks / FindValleys 分两个方法，通过 out 参数返回结果
double[] peaksIndex = null;  // ⚠️ 可传 null，API 内部分配
double[] peakValue  = null;
double[] valleyIndex = null;
double[] valleyValue = null;

// 查找波峰（prominence = 最小峰突出度，越大越严格）
PeakValleyAnalysis.FindPeaks(data, prominence: 0.5, out peaksIndex, out peakValue);
// peaksIndex[i] — 第i个峰的位置（样本索引）
// peakValue[i]  — 第i个峰的幅值

// 查找波谷（FindValleys 是独立方法，不是 FindPeaks 的 isValley 参数）
PeakValleyAnalysis.FindValleys(data, prominence: 0.5, out valleyIndex, out valleyValue);
// valleyIndex[i] — 第i个谷的位置
// valleyValue[i] — 第i个谷的幅值

// ❌ 错误写法（此签名不存在）：
// double[,] peaks = PeakValleyAnalysis.FindPeaks(data, x, minPeakHeight, minPeakDistance, threshold, FlatSelection.First, isValley: false);

// ── 典型：在 EasyChartX 上标记峰值 ──
for (int i = 0; i < peakValue.Length; i++)
    easyChartX1.AddDataMarker(
        new List<double> { peaksIndex[i] },
        new List<double> { peakValue[i] },
        Color.Red, DataMarkerType.Triangle);
```

**FlatSelection 枚举**（仅在 FindPeaks 的完整重载中用）：`First`、`Center`、`Last`、`All`

#### 相位测量（Phase）

```csharp
using SeeSharpTools.JY.DSP.Utility;

// 计算两个波形的相位差（返回 -180° ~ 180°）
double phaseShift = Phase.CalPhaseShift(signal1, signal2);
// 支持 double[] 和 float[] 两种重载
```

#### 信号同步（Synchronizer）

> **⚠️ `Synchronizer` 是静态类，不要 `new Synchronizer()`！发发双通道入口为 `double[,]`，返回同为 `double[,]`。**

```csharp
using SeeSharpTools.JY.ArrayUtility;
using SeeSharpTools.JY.DSP.Utility;

// ── 准备多通道信号（行 = 通道，列 = 采样点）──
double[,] multiChannelData = new double[2, signalLength];
// 将单山 double[] 写入多维数组
ArrayManipulation.ReplaceArraySubset(signal1, ref multiChannelData, 0, ArrayManipulation.IndexType.row);
ArrayManipulation.ReplaceArraySubset(signal2, ref multiChannelData, 1, ArrayManipulation.IndexType.row);

// ── 静态调用（不需要 new）──
// shiftPoints = 相位差 * 采样率 / (信号频率 * 360)
double shiftPoints = phaseShift * samplingRate / (signalRate * 360);
double[,] syncedData = Synchronizer.Sync(multiChannelData, shiftPoints); // 返回 double[,]
// ⚠️ 输出数组比输入小（截断了稳定点前的数据）

// 不指定 shiftPoints（自动检测）
double[,] syncedData2 = Synchronizer.Sync(multiChannelData);

// ── 提取同步后各通道 ──
int syncLen = syncedData.GetLength(1);
double[] ch1Synced = new double[syncLen];
double[] ch2Synced = new double[syncLen];
ArrayManipulation.GetArraySubset(syncedData, 0, ref ch1Synced); // 读RowIndex=0
ArrayManipulation.GetArraySubset(syncedData, 1, ref ch2Synced);

// ❌ 错误：
// var sync = new Synchronizer();
// double[,] result = sync.Sync(multiChannelData, shiftPoints);  // Synchronizer 是静态类！
```

#### 信号处理工具（SignalProcessing）

```csharp
using SeeSharpTools.JY.DSP.Utility;

// 阈值检测（列举超过/低于阈值的集合）
var result = SignalProcessing.CheckThreshold(data, threshold: 2.0, isAboveLevel: true);

// 过零点检测
var zeros = SignalProcessing.CheckCrossZeroPoints(data, isAbove: true);

// 自定义上下限范围检测（返回 1=超上限, -1=低于下限, 0=范围内）
var inRange = SignalProcessing.CheckInRange(data, highLimit, lowLimit);

// AC/DC 成分估算（单位 RMS）
double acTerm, dcTerm;
SignalProcessing.EstimateACDC(signal, out acTerm, out dcTerm);

// 内插值计算（IntepolationType: Linear / CubicSpline / LogLinear / Polynomial / Step）
double yInterp = SignalProcessing.Interpolate(xValues, yValues, xPoint, IntepolationType.Linear);
```

#### 系统噪声计算（SystemNoiseCalculation / HarmonicAnalysis）

```csharp
using SeeSharpTools.JY.DSP.Utility;

// 频域法：指定频段内的系统噪声（去直流）
double noise = SystemNoiseCalculation.CalculateSystemNoise(
    signal, dt: 1.0/sampleRate, startFrequency: 100, stopFrequency: 5000);

// 时域法：整体系统噪声（去直流）
double noise = SystemNoiseCalculation.CalculateSystemNoise(signal);

// 也可通过 HarmonicAnalysis 类调用相同方法
double noise = HarmonicAnalysis.CalculateSystemNoise(signal, dt, startFreq, stopFreq);
```

#### 测量分析（SeeSharpTools.JY.DSP.Measurements）——HarmonicAnalysis 完整调用指南

> **🚨 核心规则（必读，违反将导致结果完全错误）：**
> 1. **`dt` = 采样间隔（秒）= `1.0 / sampleRate`，不是采样率！**
>    - ✅ `dt = 1.0 / 100000.0`（采样率 100kHz 时 dt = 0.00001）
>    - ❌ `dt = 100000`（直接传采样率，结果差 N² 倍）
> 2. **`componentsLevel`、`componentsPhase` 是 `ref` 参数，不是 `out`！必须预先 `new`！**
>    - ✅ `double[] levels = new double[highestHarmonic + 1];`
>    - ❌ `double[] levels;`（未初始化，调用时报 `NullReferenceException`）
> 3. **`THDAnalysis` 返回的 `thd` 是比值，不是百分比，不是 dB！**
>    - 转百分比：`thd * 100`
>    - 转 dB：`20 * Math.Log10(thd)`
> 4. **`componentsLevel` 数组索引：[0]=DC，[1]=基波，[2]=2次谐波，[3]=3次谐波...**
>    - 单位：峰値电压（Vpk）= 1.414 × RMS
> 5. **`highestHarmonic` 不能超过奈奎斯特频率限制：**
>    - 要求：`highestHarmonic × fundamentalFreq < sampleRate / 2`
>    - 超出部分的谐波分量自动置 0，不会报错但结果不准
> 6. **所有函数都在 `SeeSharpTools.JY.DSP.Measurements` 命名空间**

```csharp
using SeeSharpTools.JY.DSP.Measurements;
using SeeSharpTools.JY.DSP.Fundamental;   // Generation
using SeeSharpTools.JY.ArrayUtility;       // ArrayCalculation

// ══════════════════════════════════════════════════════════
// 通用设置
// ══════════════════════════════════════════════════════════
const double sampleRate = 100000.0;        // 采样率 100 kHz
const int    N          = 100000;          // 采样点数
double       dt         = 1.0 / sampleRate; // ⚠️ 采样间隔！不是采样率

// ── THD 分析（总谐波失真，基本重载）─────────────────────────────────
double fundamentalFreq, thd;
// ⚠️ componentsLevel 必须预先 new！长度 = highestHarmonic + 1
double[] componentsLevel = new double[11];

HarmonicAnalysis.THDAnalysis(
    signal,              // 时域信号（单位 V）
    dt,                  // ⚠️ 采样间隔 = 1/sampleRate
    out fundamentalFreq, // 输出：检测到的基波频率 (Hz)
    out thd,             // 输出：THD 比值（非 %，非 dB）
    ref componentsLevel, // ⚠️ ref（不是 out）：[0]=DC,[1]=基波,...
    highestHarmonic: 10  // 分析到第 10 次谐波
);
// ★ 结果换算：
label_THD.Text  = string.Format("THD: {0:F4}%", thd * 100);          // 比值 → 百分比
label_THDdB.Text = string.Format("THD: {0:F2} dB", 20*Math.Log10(thd)); // 比值 → dB
label_Fund.Text = string.Format("基波: {0:F4} Vpk", componentsLevel[1]);  // [1]=基波
label_H2.Text   = string.Format("2次谐波: {0:F4} Vpk", componentsLevel[2]);
label_H3.Text   = string.Format("3次谐波: {0:F4} Vpk", componentsLevel[3]);

// ── THD 分析（含相位信息，扩展重载）─────────────────────────────────
// ⚠️ componentsPhase 同样是 ref，必须预先 new
double[] componentsPhase = new double[11];
HarmonicAnalysis.THDAnalysis(
    signal, dt,
    out fundamentalFreq, out thd,
    ref componentsLevel,
    ref componentsPhase,          // ⚠️ ref：相位数组（弧度）
    highestHarmonic: 10,
    allowAliasing: true  // true=允许超奈奎斯特谐波（置0），false=严格限制
);
// componentsPhase[0]=DC相位, [1]=基波相位, [2]=2次谐波相位...
double fundPhaseRad = componentsPhase[1];
double fundPhaseDeg = fundPhaseRad * 180.0 / Math.PI;

// ── ToneAnalysis（单音分析：频率 + 幅值 + 相位）───────────────────
// 重载 1：double[] 输入（基波为 sin）
double freq, amplitude, phase;
HarmonicAnalysis.ToneAnalysis(
    signal, dt,
    out freq,          // 基波频率 (Hz)
    out amplitude,     // 基波幅值（峰值电压 Vpk）
    out phase,         // 基波相位（弧度）
    initialGuess: 1000.0,  // 预估频率 (Hz)，0=自动全频搜索
    searchRange:  500.0    // 搜索范围 (Hz)
);
// ⚠️ phase 单位是弧度，转角度：phase * 180.0 / Math.PI
// ⚠️ initialGuess=0 时全频搜索，searchRange 被忽略

// 重载 2：Complex[] 输入（基波为 cos，默认 initialGuess=NaN）
// 适用场景：已通过 IQ 解调或复数采样器得到的复数时域信号
// ⚠️ 输入为 Complex[]，基波类型为 cos（不是 sin）
using System.Numerics;
// 构造复数信号（实际应用中通常来自 IQ 彩源或复数 ADC）
Complex[] iqSignal = new Complex[N];
for (int i = 0; i < N; i++)
    iqSignal[i] = new Complex(realPart[i], imagPart[i]);
// 全频搜索（不指定 initialGuess）
double iqFreq, iqAmp, iqPhase;
HarmonicAnalysis.ToneAnalysis(
    iqSignal, dt,
    out iqFreq, out iqAmp, out iqPhase);
// 在指定频率附近搜索
HarmonicAnalysis.ToneAnalysis(
    iqSignal, dt,
    out iqFreq, out iqAmp, out iqPhase,
    initialGuess: 1000.0, searchRange: 100.0);

// ── SINAD 分析（信纳比）──────────────────────────────────────────────
// 公式：SINAD = (S + D + N) / (D + N)，单位 dB
// 典型值：理想 ADC 约 60~120 dB
double sinad;
double[] sinadLevels = new double[11]; // ⚠️ ref 参数，必须预先 new
HarmonicAnalysis.SINADAnalysis(
    signal, dt,
    out fundamentalFreq,
    out sinad,         // 输出：SINAD 值（dB）
    ref sinadLevels,   // ⚠️ ref（不是 out）：各谐波分量
    highestHarmonic: 10
);
label_SINAD.Text = string.Format("SINAD: {0:F2} dB", sinad);

// ── SNR 分析（信噪比）────────────────────────────────────────────────
// 公式：SNR = S / N，单位 dB
// 注：SNR 排除谐波失真，只关注噪声；SINAD 包含失真
double snr;
double[] snrLevels = new double[11]; // ⚠️ ref 参数，必须预先 new
HarmonicAnalysis.SNRAnalysis(
    signal, dt,
    out fundamentalFreq,
    out snr,           // 输出：SNR 值（dB）
    ref snrLevels,     // ⚠️ ref（不是 out）
    highestHarmonic: 10
);
label_SNR.Text = string.Format("SNR: {0:F2} dB", snr);

// ── SFDR 分析（无杂散动态范围）────────────────────────────────────────
// 公式：SFDR = 基波功率 / 最大杂散分量功率，单位 dBc
// 典型值：高性能 ADC 约 80~100 dBc
double sfdr;
double[] sfdrLevels = new double[11]; // ⚠️ ref 参数，必须预先 new
HarmonicAnalysis.SFDRAnalysis(
    signal, dt,
    out fundamentalFreq,
    out sfdr,          // 输出：SFDR 值（dB）
    ref sfdrLevels,    // ⚠️ ref（不是 out）
    highestHarmonic: 10
);
label_SFDR.Text = string.Format("SFDR: {0:F2} dBc", sfdr);

// ── FullHarmonicAnalysis（完整谐波分析，返回 HarmonicAnalysisResult）────
// 适用于：一次获取各次谐波幅值 + THD/SINAD/SNR/SFDR/ENOB 全部指标
// ⚠️ componentsLevel 必须预先分配！使用 ref 传入（不是 out）
double[] allComponents = new double[11]; // ⚠️ 必须预先 new，长度 = highestHarmonic + 1
HarmonicAnalysisResult result = HarmonicAnalysis.FullHarmonicAnalysis(
    signal, dt,
    out fundamentalFreq,
    ref allComponents,  // ⚠️ ref！不是 out，必须预先分配
    highestHarmonic: 10
);
// 返回值包含所有指标（均为 dB）
label_THD.Text   = string.Format("THD: {0:F2} dB",   result.THD);
label_SINAD.Text = string.Format("SINAD: {0:F2} dB", result.SINAD);
label_SNR.Text   = string.Format("SNR: {0:F2} dB",   result.SNR);
label_SFDR.Text  = string.Format("SFDR: {0:F2} dBc", result.SFDR);
label_ENOB.Text  = string.Format("ENOB: {0:F2} bits",result.ENOB);
for (int i = 1; i <= 5; i++)
    Console.WriteLine("H{0}: {1:F4} Vpk ({2:F2} Hz)",
        i, allComponents[i], fundamentalFreq * i);

// ── BasicAnalysis（基础功率分解分析）─────────────────────────────────
// ⚠️ BasicAnalysis 在 DSP.Measurements 中不存在，请不要调用！改用 FullHarmonicAnalysis。
```

#### 单音分析器（ToneAnalyzer — SeeSharpTools.JY.DSP.Utility）

```csharp
using SeeSharpTools.JY.DSP.Utility;

// ⚠️ 注意：ToneAnalyzer 的参数是 Fs（采样率），不是 dt！
ToneInfo toneInfo = ToneAnalyzer.SingleToneAnalysis(
    signal,
    Fs: sampleRate,          // 采样率 (Hz)，不是 dt
    initialGuess: 1000,      // 预估频率 (Hz)
    searchRange: 100);       // 搜索范围 (Hz)
// toneInfo.Frequency — 频率 (Hz)
// toneInfo.Amplitude — 幅值
// toneInfo.Phase     — 相位 (弧度)
```

#### 方波测量（SquarewaveMeasurements — SeeSharpTools.JY.DSP.Utility）

```csharp
using SeeSharpTools.JY.DSP.Utility;

var sqm = new SquarewaveMeasurements();
sqm.SetWaveform(squareSignal);

double highLevel = sqm.GetHighStateLevel();   // 高电平
double lowLevel  = sqm.GetLowStateLevel();    // 低电平
double period    = sqm.GetPeriod();            // 周期

// 两路方波相位差
sqm.SetWaveform2(squareSignal2);
double phaseShift = sqm.GetPhase();            // 相位差
```

##### ⚠️ 测量分析常见错误

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| `THDAnalysis(signal, sampleRate, ...)` | `THDAnalysis(signal, 1.0/sampleRate, ...)` | **dt 是采样间隔，不是采样率** |
| `ToneAnalysis(signal, sampleRate, ...)` | `ToneAnalysis(signal, 1.0/sampleRate, ...)` | 同上，dt = 1/sampleRate |
| `HarmonicAnalyzer.ToneAnalysis(signal, sampleRate, ...)` | `HarmonicAnalyzer.ToneAnalysis(signal, 1.0/sampleRate, ...)` | **SoundVibration 的 dt 同样是采样间隔** |
| THD 值直接当百分比用 | `thd * 100` 转百分比 | THD 返回的是比值（如 0.05 = 5%） |
| `componentsLevel[0]` 当基波 | `componentsLevel[1]` 才是基波 | [0]=DC, [1]=基波, [2]=2次谐波... |
| `double[] levels; THDAnalysis(..., out levels, ...)` | `double[] levels = new double[11]; THDAnalysis(..., ref levels, ...)` | **componentsLevel 是 ref 不是 out，必须预先 new！** |
| `ToneAnalyzer.SingleToneAnalysis(signal, dt, ...)` | `ToneAnalyzer.SingleToneAnalysis(signal, Fs, ...)` | **ToneAnalyzer 用 Fs（采样率），与 HarmonicAnalysis 不同** |
| highestHarmonic 设太大 | 确保 `highestHarmonic × fundamentalFreq < sampleRate/2` | 超奈奎斯特频率的谐波分量为 0 |
| `thd * 20 * Math.Log10(thd)` | `20 * Math.Log10(thd)` | THD转 dB 不需再乘 thd |
| 使用 `BasicAnalysis` | **此方法不存在**，改用 `FullHarmonicAnalysis` | DSP.Measurements中没有BasicAnalysis |

> → 谐波分析完整示例：[examples.md § 示例 3 谐波分析](examples.md)　｜　DSP 分析专项：[examples.md § DSP 分析功能专项](examples.md)

#### 复数 FFT（AdvanceComplexFFT / AdvanceRealFFT）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using System.Numerics;

// ── AdvanceComplexFFT：获取复数频谱（含幅度+相位）────────────────
// 🚨 spectrum 必须预分配，长度 = N/2 + 1（不是 N/2）
Complex[] spectrum = new Complex[signal.Length / 2 + 1];
Spectrum.AdvanceComplexFFT(signal, WindowType.Hanning, ref spectrum);

// 提取幅度谱和相位谱
double[,] spectrumData = new double[2, spectrum.Length];
for (int i = 0; i < spectrum.Length; i++)
{
    spectrumData[0, i] = spectrum[i].Magnitude / spectrum.Length; // 幅度
    spectrumData[1, i] = spectrum[i].Phase / spectrum.Length;     // 相位
}
easyChartX_spectrum.SplitView = true;
easyChartX_spectrum.Plot(spectrumData);

// ── AdvanceRealFFT：获取实数频谱 + SpectralInfo ────────────────
int spectralLines = signal.Length / 2;
double[] realSpectrum = new double[spectralLines];
SpectralInfo spectralInfo = new SpectralInfo();  // ⚠️ ref 参数，必须初始化
Spectrum.AdvanceRealFFT(signal, spectralLines, WindowType.Hanning,
    realSpectrum, ref spectralInfo);             // ✅ ref，不是 out
// SpectralInfo 字段说明（无 .df 字段！）
// spectralInfo.FFTSize      — FFT 点数
// spectralInfo.spectralLines — 频线数
// spectralInfo.windowType   — 窗函数类型
// 频率分辨率需手动计算：df = samplingRate / spectralInfo.FFTSize;
```

#### ~~音频/振动谐波分析器~~（SeeSharpTools.JY.DSP.SoundVibration）— ⚠️ 已淘汰

> **⚠️ `HarmonicAnalyzer.ToneAnalysis` 已淘汰，新代码应改用 `HarmonicAnalysis.FullHarmonicAnalysis`（命名空间：`SeeSharpTools.JY.DSP.Measurements`）**
>
> | 已过时（SoundVibration） | 替代（Measurements） |
> |------------------------|-------------------|
> | `HarmonicAnalyzer.ToneAnalysis(data, dt, n, inDB)` → `ToneAnalysisResult` | `HarmonicAnalysis.FullHarmonicAnalysis(data, dt, out freq, ref levels, n)` → `HarmonicAnalysisResult` |
> | `HarmonicAnalyzer.ToneAnalysis(data, dt, out freq, out thd, ref levels, n)` | `HarmonicAnalysis.THDAnalysis(data, dt, out freq, out thd, ref levels, n)` |
>
> **迁移注意**：
> - `ToneAnalysisResult.THDplusN` / `.NoiseFloor` → `HarmonicAnalysis` **无对应字段**，如仍需这两项请保留旧调用
> - `HarmonicAnalysisResult` 额外提供 `.SFDR`（无杂散动态范围）
> - ⚠️ `HarmonicAnalysis.ToneAnalysis(...)` 是**单音频率/幅值/相位测量**，与谐波指标评估完全不同，**不可用作替代**

```csharp
// ✅ 新写法：用 HarmonicAnalysis.FullHarmonicAnalysis 替代 HarmonicAnalyzer.ToneAnalysis
using SeeSharpTools.JY.DSP.Measurements;
using SeeSharpTools.JY.DSP.Fundamental; // Generation
using SeeSharpTools.JY.Mathematics;     // ArrayArithmetic

const int N = 100000;
const double sampleRate = 100000.0;
double[] baseData  = new double[N];
double[] harmonics = new double[N];
double[] data      = new double[N];
Generation.SineWave(ref baseData,  10.0, 0, 50,  sampleRate); // 基波 50Hz, 10Vpk
Generation.SineWave(ref harmonics,  0.1, 0, 100, sampleRate); // 2次谐波 100Hz, 0.1Vpk
ArrayArithmetic.Add(baseData, harmonics, ref data);            // 替代 ArrayCalculation.Add

double dt = 1.0 / sampleRate;          // ⚠️ 采样间隔，不是采样率
double fundFreq;
double[] components = new double[11]; // ⚠️ 必须预分配：长度 = highestHarmonic + 1
HarmonicAnalysisResult result = HarmonicAnalysis.FullHarmonicAnalysis(
    data, dt,
    out fundFreq,         // 检测到的基波频率 (Hz)
    ref components,       // 各次谐波电平：[0]=DC,[1]=基波,[2]=2次谐波,...
    highestHarmonic: 10);

label_THD.Text   = string.Format("THD: {0:F8}", result.THD);
label_SINAD.Text = string.Format("SINAD: {0:F2}", result.SINAD);
label_SNR.Text   = string.Format("SNR: {0:F2}", result.SNR);
label_ENOB.Text  = string.Format("ENOB: {0:F2} bits", result.ENOB);
label_SFDR.Text  = string.Format("SFDR: {0:F2}", result.SFDR);   // 新增字段
label_Fund.Text  = string.Format("基波: {0:F2} Hz, {1:F4} Vpk", fundFreq, components[1]);
// ⚠️ result 没有 THDplusN 和 NoiseFloor 字段
```

```csharp
// ── 旧写法（已淘汰，仅供维护旧项目参考；THDplusN/NoiseFloor 暂无新版替代）──
// using SeeSharpTools.JY.DSP.SoundVibration;
// ToneAnalysisResult r = HarmonicAnalyzer.ToneAnalysis(data);
// r.THD / r.THDplusN / r.SINAD / r.SNR / r.NoiseFloor / r.ENOB
```

**所需 DLL**：旧版 `SeeSharpTools.JY.DSP.SoundVibration.dll`；新版 `SeeSharpTools.JY.DSP.Measurements.dll`

##### HarmonicAnalysis vs HarmonicAnalyzer 选择指南

| 场景 | 推荐 API | 原因 |
|------|------------|------|
| 谐波全面评估（THD/SINAD/SNR/ENOB/SFDR） | `HarmonicAnalysis.FullHarmonicAnalysis()` | **取代已淘汰的 `HarmonicAnalyzer`** |
| 只需 THD | `HarmonicAnalysis.THDAnalysis()` | 准确控制输出 |
| 只需 SINAD | `HarmonicAnalysis.SINADAnalysis()` | 同上 |
| 只需 SNR | `HarmonicAnalysis.SNRAnalysis()` | 同上 |
| 单音频率/幅值/相位测量 | `HarmonicAnalysis.ToneAnalysis()` | 提供频率+幅值+相位（与谐波指标评估完全不同） |
| 需要 THD+N 或 NoiseFloor | `HarmonicAnalyzer.ToneAnalysis()` | **仅此场景保留旧 API**（HarmonicAnalysis 无这两个字段） |

#### 波形测量（WaveformMeasurements — SeeSharpTools.JY.DSP.Measurements）

> 提供全面的方波测量功能：幅度电平分析、脉冲测量、周期分析、周期 RMS/均值、边沿特性测量。
> **⚠️ 此类名为 `WaveformMeasurements`（静态类），不是 `SquarewaveMeasurements`！`SquarewaveMeasurements` 存在于 DSP.Utility 中且是实例类。**

```csharp
using SeeSharpTools.JY.DSP.Measurements;

// ── 设置高低电平检测方法 ──────────────────────────────────────────
var stateSetting = new HighLowStateSetting
{
    Method = HighLowStateMethod.Histogram,  // Peak 或 Histogram
    HistogramSize = 256
};
var refLevel = new PulseReferenceLevel
{
    High = 0.9, Low = 0.1, Middle = 0.5,
    Unit = PulseReferenceUnit.Percentage    // Percentage 或 Absolute
};
double dt = 1.0 / sampleRate;  // ⚠️ 采样间隔

// ── 1. 幅度电平分析 ─────────────────────────────────────────────
double amplitude, highLevel, lowLevel;
WaveformMeasurements.AmplitudeLevelAnalysis(squareWave, stateSetting,
    out amplitude, out highLevel, out lowLevel);

// ── 2. 脉冲测量（指定脉冲序号）─────────────────────────────────
double period, pulseWidth, dutyCycle;
PulseMeasurementInfo pulseInfo;
WaveformMeasurements.PulseMeasurement(squareWave, dt, stateSetting,
    PulsePolarity.High, refLevel, pulseNumber: 1,
    out period, out pulseWidth, out dutyCycle, out pulseInfo);

// ── 3. 周期分析（统计所有周期的 min/max/avg）─────────────────────
PeriodAnalysisResult periodResult, dutyCycleResult, pulseResult;
MeasurementInfo periodInfo;
WaveformMeasurements.PeriodAnalysis(squareWave, dt, stateSetting,
    PulsePolarity.High, refLevel,
    out periodResult, out dutyCycleResult, out pulseResult, out periodInfo);
// periodResult.Average / Minimum / Maximum

// ── 4. 周期 RMS 和均值 ──────────────────────────────────────────
double rms, mean;
MeasurementInfo cycleInfo;
WaveformMeasurements.CycleRmsMeanAnalysis(squareWave, dt, stateSetting,
    refLevel, cycleNumber: 1, out rms, out mean, out cycleInfo);

// ── 5. 边沿特性测量（过冲/下冲、斜率、转换时间）─────────────────
double slope, transitionDuration;
TransitionInfo preTransInfo, postTransInfo;
MeasurementInfo edgeInfo;
WaveformMeasurements.TransitionMeasurement(squareWave, dt, stateSetting,
    EdgePolarity.Rising, refLevel, transitionNumber: 1,
    out slope, out transitionDuration, out preTransInfo, out postTransInfo, out edgeInfo);
// preTransInfo.Overshoot / Undershoot：前沿过冲/下冲
```

##### ⚠️ WaveformMeasurements 常见错误

| 错误用法 | 正确用法 | 说明 |
|---------|---------|------|
| `PulseMeasurement(data, sampleRate, ...)` | `PulseMeasurement(data, 1.0/sampleRate, ...)` | **dt 是采样间隔，不是采样率** |
| `PeriodAnalysis` 忽略返回值 | 检查 `periodResult.Count > 0` | 信号太短可能无法检测到周期 |
| 不包 try-catch | 所有测量方法应加 try-catch | 信号不合规时可能抛异常 |

#### 窗函数类型（WindowType）

`None`（矩形窗）、`Hanning`（汉宁窗，⭐通用推荐）、`Hamming`、`Blackman_Harris`、`Exact_Blackman`、`Blackman`、`Flat_Top`（平顶窗，⭐幅值精确测量）、`Flat_Top_95`、`Four_Term_B_Harris`、`Seven_Term_B_Harris` 等（共 20 种，详见 [reference.md](reference.md))

---

### 3. 数学运算（SeeSharpTools.JY.Mathematics）

```csharp
using SeeSharpTools.JY.Mathematics;

// 计算引擎选择（可选IntelMKL加速）
Engine.Provider = ProviderEngine.CSharp;  // 或 ProviderEngine.IntelMKL
```

#### ArrayArithmetic — 数组算术运算

```csharp
using SeeSharpTools.JY.Mathematics;

double[] result = new double[a.Length];
// ── 四则运算（有 ref 输出版本和原地版本）──
ArrayArithmetic.Add(a, b, ref result);           // 数组加法
ArrayArithmetic.Substract(a, b, ref result);     // ⚠️ 注意拼写：Substract（非 Subtract）
ArrayArithmetic.Multiply(a, b, ref result);      // 逐元素乘法
ArrayArithmetic.Divide(a, b, ref result);        // 逐元素除法
// ── 原地运算（写回第一参数）──
ArrayArithmetic.Add(srcDest, src2);
ArrayArithmetic.Substract(srcDest, scalar);

// ── 标量运算 ──
ArrayArithmetic.Add(a, 3.0, ref result);         // 加标量
ArrayArithmetic.Multiply(a, 2.0, ref result);    // 乘标量

// ── 数学函数 ──
ArrayArithmetic.Absolute(a, ref result);         // 绝对值
ArrayArithmetic.Sqrt(a, ref result);             // 开方
ArrayArithmetic.Pow(a, 2.0, ref result);         // 幂运算
ArrayArithmetic.Exp(a, ref result);              // e^x
ArrayArithmetic.Ln(a, ref result);               // 自然对数
ArrayArithmetic.Log(a, ref result);              // log10

// ── 聚合运算 ──
double sum     = ArrayArithmetic.Sum(a);         // 求和（返回 double）
double product = ArrayArithmetic.Product(a);     // 求积

// ── 初始化与查找 ──
ArrayArithmetic.Initialize(ref a, 0.0);          // 全部置为 0（替代 ArrayCalculation.Zero）
ArrayArithmetic.FindMaxMin(a,
    out double max, out double min,
    out int maxIdx, out int minIdx);             // 替代 ArrayCalculation.ArrayMaxAndMin
```

#### Statistics — 统计分析（**替代 ArrayCalculation 的统计函数**）

> **⚠️ 新代码应优先使用 `Statistics`，而非 `ArrayCalculation.Average/RMS/Sum`**

```csharp
using SeeSharpTools.JY.Mathematics;

double mean   = Statistics.Mean(data);            // 均值（替代 ArrayCalculation.Average）
double rms    = Statistics.RMS(data);             // 均方根（替代 ArrayCalculation.RMS）
double stdDev = Statistics.StandardDeviation(data); // 标准差
double var    = Statistics.Variance(data);        // 方差
double median = Statistics.Median(data);          // 中位数
double kurt   = Statistics.Kurtosis(data);        // 峰度
double skew   = Statistics.Skewness(data);        // 偏度
double p90    = Statistics.Percentile(data, 90);  // 第 90 百分位

// 直方图（返回 int[] 各分箱计数）
// binSize: 分箱数量   min/max: 若两者相等则自动适配范围
int[] hist = Statistics.Histogram(data, binSize: 20, min: 0.0, max: 10.0);
int[] hist2 = Statistics.Histogram(data, binSize: 20, min: 0.0, max: 0.0); // 自动范围
```

#### ArrayOperation — 数组操作

```csharp
ArrayOperation.ConvertTo(strArray, ref doubleArray);  // 字符串转数值

// GetSubSet — 从二维数组中提取子集（v2.0.3 修复按列获取的问题）
// 按行提取：获取第 rowIdx 行的所有数据
double[] row0 = ArrayOperation.GetSubSet(data2D, rowIdx: 0, MajorOrder.Row);
// 按列提取：v2.0.3 前此功能有 Bug，v2.0.3 已修复
double[] col0 = ArrayOperation.GetSubSet(data2D, colIdx: 0, MajorOrder.Column);
```

---

### 4. 数组工具（SeeSharpTools.JY.ArrayUtility）

> **⚠️ `ArrayCalculation` 中的统计/聚合函数已基本被淘汰，请改用 `SeeSharpTools.JY.Mathematics`：**
>
> | 已过时（ArrayCalculation） | 替代（Mathematics） |
> |--------------------------|-------------------|
> | `ArrayCalculation.Average(data)` | `Statistics.Mean(data)` |
> | `ArrayCalculation.RMS(data)` | `Statistics.RMS(data)` |
> | `ArrayCalculation.Sum(data)` | `ArrayArithmetic.Sum(data)` |
> | `ArrayCalculation.Add(a, b, ref c)` | `ArrayArithmetic.Add(a, b, ref c)` |
> | `ArrayCalculation.Multiply(a, b, ref c)` | `ArrayArithmetic.Multiply(a, b, ref c)` |
> | `ArrayCalculation.Zero(ref a)` | `ArrayArithmetic.Initialize(ref a, 0.0)` |
> | `ArrayCalculation.InitializeArray(ref a, v)` | `ArrayArithmetic.Initialize(ref a, v)` |
> | `ArrayCalculation.ArrayMaxAndMin(...)` | `ArrayArithmetic.FindMaxMin(...)` |

**`ArrayManipulation`（仍现役，无替代）**——用于数组结构操作：

```csharp
using SeeSharpTools.JY.ArrayUtility;

// ── 连接数组 ──
// ⚠️ BuildArray 返回 1D 展平数组（T[]），不是 2D！
double[] merged1D  = ArrayManipulation.BuildArray(arrays);       // double[][] -> double[] 展平
double[] merged1D2 = ArrayManipulation.BuildArray(src1, src2);  // 两个 1D 数组拼接
// ❌ 错误：
// double[,] merged = ArrayManipulation.BuildArray(arrays);  // 返回类型是 double[]，不是 double[,]
```

**`ArrayCalculation` 仍可接受的残留用法**（仅限以下两种场景）：

```csharp
using SeeSharpTools.JY.ArrayUtility;

// 场景1：配合旧版 SoundVibration 示例（官方范例原生写法，不影响功能）
ArrayCalculation.Add(baseData, harmonics, ref data);

// 场景2：需要同时获取 maxIndex[] 数组（多个相同最大值索引）
ArrayCalculation.ArrayMaxAndMin(signal,
    out double maxVal, out int[] maxIndexes,   // int[] 含所有最大值位置
    out double minVal, out int[] minIndexes);
// ✅ 注意：ArrayArithmetic.FindMaxMin 只返回单个 maxIdx/minIdx（int 非 int[]）
```

---

### 5. 文件与数据库

#### CSV（SeeSharpTools.JY.File）

```csharp
using SeeSharpTools.JY.File;

// 写 CSV（弹出 SaveDialog）
string[,] data = { {"Name","Value"}, {"Temp","25.0"} };
CsvHandler.WriteData(data, WriteMode.Append);  // ⚠️ 需要 WriteMode 参数

// 读 CSV（类型专用方法，非泛型）
// ⚠️ 不存在 CsvHandler.Read<T>()，必须用类型专用方法！
double[] doubleData = CsvHandler.ReadDoubleData(
    filePath: "data.csv",
    startRow: 1,       // 0-based 行索引
    startColumn: 0,
    rowCount: 100,
    encoding: System.Text.Encoding.UTF8);

// 读取多列（指定列索引）
double[] colData = CsvHandler.ReadDoubleData(
    filePath: "data.csv",
    startRow: 1,
    columns: new uint[] { 0, 2 },  // 读第0列和第2列
    rowCount: 100,
    encoding: System.Text.Encoding.UTF8);

// ❌ 错误（此 API 不存在）：
// double[,] readData = CsvHandler.Read<double>(startRow: 1, startColumn: 0);
```

#### 二进制文件（BIN）

```csharp
using SeeSharpTools.JY.File;

// 写（必须指定 WriteMode ）
// 写入并覆盖（自动弹出对话框选路径）
BinHandler.WriteData(doubleArray, WriteMode.Overwrite);
// 写入到指定路径
BinHandler.WriteData(filePath, doubleArray, WriteMode.Overwrite);
// 追加写入
BinHandler.WriteData(filePath, doubleArray, WriteMode.Append);

// 读（类型专用方法，非泛型）
double[] data = BinHandler.ReadDoubleData(filePath);
short[] shortData = BinHandler.ReadShortData(filePath);
int[] intData = BinHandler.ReadIntData(filePath);
// 读取列数（返回二维）
double[,] data2D = BinHandler.ReadDoubleData(colNum: 4); // colNum=4列，弹出选文件

// 流式读取（大文件分批读）
binHandler = new BinHandler(filePath, colNum: 4);
while (!binHandler.ReadOver) {
    double[,] chunk = binHandler.StreamRead<double>(sampleCount: 100);
    // 处理 chunk...
}
binHandler.Dispose();

// ❌ 错误（这些方法不存在）：
// BinHandler.WriteData(doubleArray, filePath);  // 参数顺序错误！
// BinHandler.ReadData<double>(filePath);        // 无泛型 Read！
```

#### 模拟波形文件（.tdm / .tdms 格式）

```csharp
AnalogWaveformFile.Write(waveformData, sampleRate, filePath);
double[,] waveData = AnalogWaveformFile.Read(filePath, out sampleRate);
```

#### 数据库（SeeSharpTools.JY.Database）

```csharp
using SeeSharpTools.JY.Database;

var db = new DbOperation(connectionString, DbProviderType.SQLite);

// 增删改
db.ExecuteNonQuery("INSERT INTO t VALUES(@p1)", parameters);

// 查询（返回 DataReader）
var reader = db.ExecuteReader("SELECT * FROM t", null);

// 查询（返回 DataTable）
DataTable dt = db.ExecuteDataTable("SELECT * FROM t", null);
```

**支持数据库类型**：`SQLite`、`SQLServer`、`MySQL`、`OleDb`

---

### 6. 通信（SeeSharpTools.JY.TCP）

```csharp
using SeeSharpTools.JY.TCP;

// TCP 服务端
var server = new EasyTCPServer();
server.Start(port: 8888);
server.DataReceived += (s, e) => { /* e.Data */ };
server.Send(clientId, data);

// TCP 客户端
var client = new EasyTCPClient();
client.Connect("192.168.1.100", 8888);
client.Send(data);
client.DataReceived += (s, e) => { /* e.Data */ };
```

---

### 7. 传感器换算（SeeSharpTools.JY.Sensors）

```csharp
using SeeSharpTools.JY.Sensors;

// PT100 温度传感器（电阻值 → 摄氏度）
double[] temp = RTD.Convert(resistanceArray, RTDType.PT100);

// 荷重传感器（电压 → 荷重）
double[] load = LoadCell.Convert(voltArray, sensitivity: 2.0, maxload: 100, ExcitationValtage: 2.5);

// 自定义换算公式
double[] result = CustomScaling.Convert(voltArray, v => v * 100.0 + 20.0);

// 分段线性插值（查表法）
double x = Interpolation.LinearInterpolation1D(table, xIncrement, xOffset, y: 25.0);
```

---

### 8. 日志与报告（SeeSharpTools.JY.Report）

```csharp
using SeeSharpTools.JY.Report;

// 初始化日志
Logger.Initialize("app.log");
Logger.LogLevel = LogLevel.Info;

// 写日志
Logger.Info("采样开始，采样率：{0} S/s", sampleRate);
Logger.Warn("缓冲区接近满载");
Logger.Error("采集失败：{0}", ex.Message);
Logger.Debug("调试信息");

// 带异常记录
Logger.Error(exception, "发生异常：{0}", ex.Message);
```

**日志级别**（从低到高）：`Trace` → `Debug` → `Info` → `Warn` → `Error` → `Fatal`

---

## 常用开发模式

### 连续数据采集 + 实时显示

```csharp
// 在 Timer 或后台线程中
private void OnDataAcquired(double[] newData)
{
    // 追加到 StripChart（滚动）
    this.Invoke(() => stripChartX1.Plot(newData));

    // 或全量刷新 EasyChartX
    this.Invoke(() => easyChartX1.Plot(newData, 0, 1.0 / sampleRate));
}
```

### 频谱分析完整流程

```csharp
// 1. 生成或采集信号
double[] signal = new double[4096];
Generation.SineWave(ref signal, 1.0, 0, 1000, 44100);

// 2. 计算频谱（推荐 dBV 单位 + Hanning 窗）
double[] spectrum = new double[signal.Length / 2];  // ⚠️ 长度必须 = N/2
double df;
Spectrum.PowerSpectrum(signal, 44100, ref spectrum, out df,
    SpectrumUnits.dBV, WindowType.Hanning, 0, false);

// 3. 显示
easyChartX_time.Plot(signal, 0, 1.0/44100);
easyChartX_freq.Plot(spectrum, 0, df);
easyChartX_freq.AxisY.Title = "dBV";  // dBV 已是对数线性值，不需要对数轴
```

### EasyChartX 多通道二维数组绘制

```csharp
// 多通道（通道数 × 采样点数），行优先
double[,] data = new double[4, 1000];
// ... 填充数据
easyChartX1.Plot(data, 0, 1.0/sampleRate, MajorOrder.Row);
```

---

## 注意事项

1. **跨线程 UI 更新**：后台线程必须用 `this.Invoke(()=>...)` 更新 GUI 控件
2. **DataStorageType**：高频更新选 `NoClone`；需数据持久化选 `Clone`
3. **MKL 加速**：`Engine.Provider = ProviderEngine.IntelMKL` 需要 `libiomp5md.dll` 同目录
4. **EasyChartX 2D 数组**：默认 `MajorOrder.Row`（每行一个通道）；JYUSB1601 `ReadData` 返回 `[samples, channels]`，需用 `MajorOrder.Column`
5. **🚨 频谱数组预分配（致命）**：`PowerSpectrum` 的 `ref spectrum` 参数必须预分配 `new double[N/2]`，**传 `null` 会直接导致 `NullReferenceException` 崩溃**；传错误大小会导致 `IndexOutOfRangeException`。此规则适用于所有频谱方法（`AmplitudeSpectrum`、`PhaseSpectrum`、`DBFullScaleSpectrum` 等）
6. **AdvanceComplexFFT 频谱长度**：复数频谱点数 = N/2+1（不是 N/2），预分配 `new Complex[N/2 + 1]`
7. **EasyChartX 系列添加**：`LineSeries.Add()`，**不是** `SeriesCollection.Add()`（编译错误来源）
8. **dBV 显示**：推荐用 `SpectrumUnits.dBV` 参数让 PowerSpectrum 直接输出 dBV（或手动 `20*log10(sqrt(power))`）；Y轴保持线性，不要设 `IsLogarithmic=true`
9. **PeakSpectrumAnalysis 参数**：第二个参数是 `dt`（采样间隔 = 1/samplingRate），不是采样率
10. **AutoClear**：默认 `true`，每次 `Plot()` 覆盖旧数据；若需叠加波形请设为 `false`
11. **Clear() vs ClearMarker()**：`Clear()` 清除波形数据，`ClearMarker()` 只清除数据标记点
12. **SplitView**：`true` 时多通道各自独立绘图区域，`false` 时共享坐标轴
13. **DSP 全局 dt vs sampleRate**：`HarmonicAnalysis`、`PeakSpectrumAnalysis`、`WaveformMeasurements` 的 `dt` = 1.0/sampleRate；但 `ToneAnalyzer.SingleToneAnalysis` 的 `Fs` 是采样率
14. **滤波器系数（旧版 DSP.Utility）**：`SignalOperation.Filtering`（`SeeSharpTools.JY.DSP.Utility`）只执行滤波，设计需配合 `MathNet.Filtering.dll`；若使用 `SeeSharpTools.JY.DSP.Utility.Filter1D.dll` 则不需 MathNet，自带设计与执行
15. **MedianFilter.Process 窗口长度**：必须为奇数且 ≥ 3（格式 2N+1），窗口长度 ≥ 毛刺宽度*2+1
16. **THD 返回值**：是比值而非百分比，如 THD=0.05 表示 5%；转 dB 用 `20*Math.Log10(thd)`
17. **componentsLevel 索引**：`[0]=DC`, `[1]=基波`, `[2]=2次谐波`... 单位为峰値电压 = 1.414*RMS
18. **SoundVibration.HarmonicAnalyzer**：一次调用返回 THD/THD+N/SINAD/SNR/ENOB 全部指标，需额外引用 `SeeSharpTools.JY.DSP.SoundVibration.dll`
19. **SquarewaveMeasurements 方法**：所有测量方法应包裹 try-catch，信号不合规时会抛异常
20. **HarmonicAnalysis.BasicAnalysis 不存在**：`SeeSharpTools.JY.DSP.Measurements` 中 `HarmonicAnalysis` 只有 `FullHarmonicAnalysis`、`THDAnalysis`、`SINADAnalysis`、`SNRAnalysis`、`SFDRAnalysis`、`ToneAnalysis`，**不存在 `BasicAnalysis`**，误用会编译错误
21. **SpectrumTask vs Spectrum 静态方法**：`Spectrum.PowerSpectrum` 适合一次性计算；`SpectrumTask` 适合需要复用配置（窗函数/单位/平均）或调用 `FindPeak`/`MeasurePowerInBand` 的场景。注意其属性 `SpectralInfomation`（拼写保留原始 API 的拼写错误）
22. **多通道频谱无直接 API**：`Spectrum.PowerSpectrum` 只支持单通道 `double[]`，多通道须手动循环提取每通道调用，或使用 `SpectrumTask` 逐通道调用
23. **Filter1D 命名空间独立**：`SeeSharpTools.JY.DSP.Utility.Filter1D` 是独立 DLL 和命名空间，`using SeeSharpTools.JY.DSP.Utility;` 无法访问其内容，必须单独引用 DLL 并 `using SeeSharpTools.JY.DSP.Utility.Filter1D;`
24. **Filter1D 的 `wn` 单位为 Hz**：`IIRDesign.Butter`/`Cheby1`/`Cheby2`/`Ellip` 的 `wn` 参数传实际频率（Hz），不是归一化频率；带通/带阻必须传两个元素 `new double[]{lowerHz, upperHz}`
25. **ValueTuple 语法需求**：`var (b, a) = IIRDesign.Butter(...)` 需要 C# 7.0+，.NET Framework 项目需 ≥ 4.7（或安装 `System.ValueTuple` NuGet）
26. **高阶 IIR 首选 SOS**：阶数 ≥ 8 时建议用 `IIRDesign.SOSDesign` + `IIRFiltering.SOSFilter`，避免 `IIRFilter(b,a,x)` 的数值不稳定问题
27. **流式滤波必保留状态**：实时分帧处理时必须将帧末状态 `z` 传递到下一帧（`var (y, newZ) = IIRFiltering.IIRFilter(b,a,block,z); z=newZ;`），忽略 `z` 会导致帧间断点
28. **ZeroPhaseFilter 仅适离线**：`IIRFiltering.ZeroPhaseFilter` 是双向滤波，因果延迟 = 0，不可用于实时采集；边界延拓长度 `padLength` 建议至少 ≥ 滤波器阶数×3
29. **Decimate vs Downsample**：降采样必用 `Multirate.Decimate`（自带抗混叠滤波），不要用 `Multirate.Downsample`（无滤波，会产生频率混叠）

## 更多资源

- 详细 API 参考 → [reference.md](reference.md)（GUI · DSP.Fundamental · DSP.Utility · DSP.Measurements · **DSP.Utility.Spectrum** · DSP.Utility.Filter1D）
- 完整代码示例 → [examples.md](examples.md)（EasyChartX EX-A~J · 采集/谐波/滤波/文件/DB/TCP · PowerSpectrum 专项 · DSP 分析专项 · **Utility.Spectrum EX-Y1~Y12** · Filter1D 范例）
