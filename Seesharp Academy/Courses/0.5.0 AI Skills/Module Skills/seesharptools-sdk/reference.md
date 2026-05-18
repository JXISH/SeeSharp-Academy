# SeeSharpTools SDK 详细 API 参考（v2.0.3）

## 一、GUI 模块（SeeSharpTools.JY.GUI）

### EasyChartX — 完整 API 参考

#### Plot 方法重载（全部）

| 方法签名 | 说明 | 典型用途 |
|---------|-----|---------|
| `Plot(double[] yData, double xStart, double xIncrement)` | 单通道等间距 | 时域波形 |
| `Plot(double[] xData, double[] yData)` | 单通道自定义X-Y | 李萨如图、特性曲线 |
| `Plot(double[,] data, double xStart, double xInc, MajorOrder order)` | 多通道2D数组 | 多通道采集 |
| `Plot(double[,] data, double xStart, double xInc, int count)` | 多通道，指定绘图点数 | 只绘制部分数据 |
| `Plot(List<double> data, double xStart, double xInc, int count)` | List 数据源 | 动态追加 |
| `Plot(double[][] xData, double[][] yData)` | 多通道X-Y散点 | 多通道自定义X轴 |

> **JYUSB1601 多通道绘图说明**：
> `AITask.ReadData(ref buf, count, -1)` 返回的 `buf` 维度为 `[采样点数, 通道数]`（列优先），
> 对应 `Plot(buf, 0, 1.0/sampleRate, MajorOrder.Column)`。

#### 坐标轴（AxisX / AxisY / AxisX2 / AxisY2）

EasyChartX 有四条坐标轴（主X、主Y、副X、副Y），均为 `EasyChartXAxis` 类型。

```
属性名                    类型                        说明
─────────────────────────────────────────────────────────────────────
Title                    string                      轴标题文字
TitleOrientation         AxisTextOrientation         Auto / Horizontal / Vertical
TitlePosition            AxisTextPosition            Near / Center / Far

AutoScale                bool                        是否自动缩放（默认 true）
Maximum                  double                      数据范围最大值（AutoScale=false 时有效）
Minimum                  double                      数据范围最小值
ViewMaximum              double                      当前视图最大值（缩放区间）
ViewMinimum              double                      当前视图最小值
AutoScalingMode          AutoScaleMode               ByGridCount（默认）/ ByWholeNumbers
AutoZoomReset            bool                        Plot 后是否重置缩放视图

IsLogarithmic            bool                        是否启用对数坐标轴（默认 false）
LogarithmBase            double                      对数底数（默认 10）
LogLabelStyle            LogarithmicLabelStyle       E2（科学计数）/ F0（整数）
ShowLogarithmicLines     bool                        是否显示对数刻度网格线

LabelEnabled             bool                        是否显示刻度标签
LabelAngle               int                         标签旋转角度（度）
LabelFormat              string                      标签格式（C# 格式字符串，如 "F2"）

MajorGridEnabled         bool                        主网格线可见
MajorGridCount           int                         主网格线条数
MajorGridColor           Color                       主网格线颜色
MajorGridType            GridStyle                   Solid / Dash / DashDot / DashDotDot
MinorGridEnabled         bool                        次网格线可见
MinorGridType            GridStyle                   同上

Color                    Color                       轴线颜色
TickWidth                float                       刻度线宽度
─────────────────────────────────────────────────────────────────────
```

**GridStyle 枚举**：`Solid`、`Dash`、`DashDot`、`DashDotDot`

**双Y轴用法**（主Y轴 + 副Y轴）：
```csharp
// 系列绑定到副Y轴
easyChartX1.LineSeries[1].YPlotAxis = EasyChartXAxis.PlotAxis.Secondary;
// 副Y轴设置
easyChartX1.AxisY2.AutoScale = false;
easyChartX1.AxisY2.Maximum = 100.0;
easyChartX1.AxisY2.Minimum = 0.0;
easyChartX1.AxisY2.Title = "温度 (°C)";
```

#### 系列（LineSeries / EasyChartXSeries）

> ⚠️ **关键**：**初始化时必须用 `LineSeries.Add()`**，不存在 `SeriesCollection` 属性。运行时访问系列 `LineSeries[i]` 和 `Series[i]` 均可（指向相同集合）。

**EasyChartXSeries 属性完整参考**：

```
属性名         类型                              说明
────────────────────────────────────────────────────────────────────────────
Name          string                            图例名称
Color         Color                             线条颜色
Width         LineWidth                         Thin / Middle / Thick
Type          LineType                          FastLine / Line / StepLine（见下方说明）
Marker        MarkerType                        None / Circle / Square / Diamond /
                                                Triangle / Cross / Star5（见下方说明）
Visible       bool                              是否可见（隐藏不删除）
XPlotAxis     EasyChartXAxis.PlotAxis           Primary / Secondary（绑定X轴）
YPlotAxis     EasyChartXAxis.PlotAxis           Primary / Secondary（绑定Y轴，双Y轴关键）
────────────────────────────────────────────────────────────────────────────
```

**LineType 枚举——全部可用线型完整列表**：

| 枚举值 | 整数值 | 性能 | Marker 支持 | 适用场景 |
|--------|--------|------|------------|----------|
| `FastLine` | 6 | 最高（推荐） | ❌ 不支持 | 连续模拟信号、时域波形、高刷新率采集 |
| `Line` | 3 | 中等 | ✅ 支持 | 数据点少、需要标记点的场景 |
| `StepLine` | 5 | 中等 | ✅ 支持 | 数字信号、离散状态值、DAQ 数字量显示 |
| `Point` | 0 | 高 | ✅ 支持 | 离散测量值、纯散点图，不连线 |
| `Spline` | 4 | 中等 | ✅ 支持 | 少量数据点需平滑过渡的场景 |
| `Bar` | 10 | 中等 | ❌ 不支持 | 分类统计、频率分布直方图、各通道对比 |
| `Area` | 13 | 中等 | ❌ 不支持 | 累积量、面积对比、能量分布可视化 |

> **⚠️ 重要**：用 `FastLine` 并设置 `Marker` 不会报错，但 Marker 不会显示。`Bar`、`Area` 同理不支持 Marker。如需标记点，必须用 `Line`、`StepLine`、`Spline`、`Point` 类型。

**Bar 柱状图输出说明**：
- X 轴数据充当类别索引（0, 1, 2, …），Y 轴数据为柱高
- 多系列同为 `Bar` 时，柱子将并列显示
- 单系列 `Plot(data)` 即可绘出柱状图，无需额外配置

**MarkerType 枚举**：

| 枚举值 | 说明 |
|--------|------|
| `None` | 无标记（高性能，推荐大数据量） |
| `Circle` | 圆形 |
| `Square` | 方形 |
| `Diamond` | 菱形 |
| `Triangle` | 三角形 |
| `Cross` | 十字形 |
| `Star5` | 五角星 |

**Designer.cs 正确初始化方式**（必须用全限定名，因 Designer.cs 无 using 语句）：
```csharp
// 在 InitializeComponent() 中
SeeSharpTools.JY.GUI.EasyChartXSeries series1 = new SeeSharpTools.JY.GUI.EasyChartXSeries();
series1.Color     = System.Drawing.Color.DodgerBlue;
series1.Name      = "通道1";
series1.Type      = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.FastLine;
series1.Width     = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Thin;
series1.Marker    = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.None;
series1.Visible   = true;
series1.XPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
series1.YPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
this.easyChartX1.LineSeries.Add(series1);  // ← 必须用 LineSeries.Add()
// ❌ 错误：this.easyChartX1.SeriesCollection.Add(series1);  // 此属性不存在
```

**运行时访问系列**（`LineSeries[]` 和 `Series[]` 等价）：
```csharp
// 修改属性
easyChartX1.LineSeries[0].Color   = Color.Red;
easyChartX1.LineSeries[0].Name    = "新名称";
easyChartX1.LineSeries[0].Visible = false;
easyChartX1.LineSeries[0].Type    = EasyChartXSeries.LineType.StepLine;

// Series[] 与 LineSeries[] 指向相同集合（官方范例两种写法都有）
easyChartX1.Series[0].Width  = EasyChartXSeries.LineWidth.Middle;
easyChartX1.Series[0].Marker = EasyChartXSeries.MarkerType.Star5;

// 批量配置（遍历所有系列）
for (int i = 0; i < easyChartX1.SeriesCount; i++)
{
    easyChartX1.Series[i].Type = EasyChartXSeries.LineType.StepLine;
}
```

**直接设置系列数量**（最简方式，系列自动创建）：
```csharp
easyChartX1.SeriesCount = 4;  // 自动创建 4 个默认系列
easyChartX1.LineSeries[0].Color = Color.Blue;    // 再逐一配置
easyChartX1.LineSeries[1].Color = Color.Red;
```

**高频易错点汇总**：

| 场景 | 错误做法 | 正确做法 |
|------|---------|---------|
| Designer.cs 初始化 | `SeriesCollection.Add(s)` | `LineSeries.Add(s)` |
| 期望显示标记点 | `FastLine` + `Marker=Circle` | `Line` + `Marker=Circle` |
| Designer.cs 中创建系列 | `new EasyChartXSeries()` (短名) | `new SeeSharpTools.JY.GUI.EasyChartXSeries()` (全名) |
| 运行时修改颜色 | 重新 Add 新系列 | `LineSeries[i].Color = newColor` |
| 双Y轴绑定 | Plot 后再改 YPlotAxis | 初始化时 Add 前设置好 YPlotAxis |

#### 游标（XCursor / YCursor）

```
XCursor / YCursor 属性：
─────────────────────────────────────────────────────────────────────
Mode              CursorMode        Zoom（默认，框选缩放）/ Cursor（读值）/ Disabled
Value             double            当前游标位置（读写）
Color             Color             游标线颜色
SelectionColor    Color             框选区域填充色
AutoInterval      bool              是否自动调整游标步长
Interval          double            手动设置游标步长
─────────────────────────────────────────────────────────────────────
```

**TabCursor（垂直标记线）**：
```csharp
TabCursor tc = new TabCursor();
tc.Value = 0.5;   // X轴位置
easyChartX1.TabCursors.Add(tc);
easyChartX1.TabCursors.Clear();  // 清除所有 TabCursor
```

#### 数据标记（AddDataMarker）

```csharp
// 添加标记（可多次调用，叠加显示）
List<double> xVals = new List<double>() { 0.1, 0.5, 0.9 };
List<double> yVals = new List<double>() { 1.0, 3.0, 2.0 };
easyChartX1.AddDataMarker(xVals, yVals, Color.Cyan, DataMarkerType.Triangle);

// DataMarkerType 枚举：Triangle / Circle / Square / Diamond / Cross
easyChartX1.ClearMarker();  // 清除标记（不影响波形数据）
```

#### Miscellaneous（高级选项）

```
属性名                     类型                     说明
─────────────────────────────────────────────────────────────────────
DataStorage               DataStorageType          Clone（复制，安全）/ NoClone（引用，高性能）
CheckNaN                  bool                     是否过滤 NaN 值（开启影响性能）
CheckInfinity             bool                     是否过滤无穷大值
CheckNegtiveOrZero        bool                     是否过滤 <=0 值（对数轴时有用）
MaxSeriesCount            int                       最大系列数（默认 32）
MaxSeriesPointCount       int                       每系列最大显示点数（超出则抽样，默认 4000）
MarkerSize                int                       数据标记尺寸（像素，默认 7）
ShowFunctionMenu          bool                      右键功能菜单可见（默认 true）
Fitting                   FitType                  Range（范围拟合）
SplitLayoutDirection      LayoutDirection          LeftToRight / TopToBottom（SplitView 排列方向）
SplitViewAutoLayout       bool                      SplitView 是否自动排列
DirectionChartCount       int                       SplitView 每行/列图表数
─────────────────────────────────────────────────────────────────────
```

#### EasyChartX 顶层属性

```
属性名               类型          说明
─────────────────────────────────────────────────────
AutoClear           bool          每次 Plot() 是否清除旧数据（默认 true）
LegendVisible       bool          图例可见性
LegendBackColor     Color         图例背景色
LegendForeColor     Color         图例文字颜色
LegendFont          Font          图例字体
SeriesCount         int           系列（通道）数量
SplitView           bool          多通道分图模式（每通道独立坐标区域）
Cumulitive          bool          是否累积绘图（追加模式）
BackColor           Color         控件背景色
ChartAreaBackColor  Color         绘图区背景色
GradientStyle       ChartGradientStyle  背景渐变样式（None / ...）
─────────────────────────────────────────────────────
```

#### 事件

```csharp
// 坐标轴视图变化（缩放/平移）
easyChartX1.AxisViewChanged += (sender, e) => {
    // e.IsRaisedByMouseEvent：true=鼠标操作，false=代码触发
    if (e.IsRaisedByMouseEvent)
    {
        double xMin = easyChartX1.AxisX.ViewMinimum;
        double xMax = easyChartX1.AxisX.ViewMaximum;
    }
};

// 游标位置变化
easyChartX1.CursorPositionChanged += (sender, e) => {
    double x = easyChartX1.XCursor.Value;
    double y = easyChartX1.YCursor.Value;
};
```

#### 清除方法

```csharp
easyChartX1.Clear();        // 清除所有波形数据和坐标轴显示
easyChartX1.ClearMarker();  // 仅清除 AddDataMarker 添加的标记点，波形数据保留
```

#### MajorOrder 枚举

| 值 | 说明 | 对应数组布局 |
|----|-----|------------|
| `MajorOrder.Row` | 每行一个通道（默认） | `data[channelIdx, sampleIdx]` |
| `MajorOrder.Column` | 每列一个通道 | `data[sampleIdx, channelIdx]`（JYUSB1601 ReadData 格式）|

#### DataStorageType 枚举

| 值 | 说明 | 建议场景 |
|----|-----|---------|
| `DataStorageType.Clone` | 复制数据（线程安全） | 采集频率低，或需复用数组 |
| `DataStorageType.NoClone` | 直接引用（不复制） | 高频实时更新，性能优先 |

#### ⚠️ 高频易错点汇总

| 错误 | 原因 | 正确做法 |
|------|------|---------|
| `SeriesCollection.Add()` 编译失败 | 属性名错误 | 改用 `LineSeries.Add()` |
| `LineSeries[0].Color` 运行时异常 | 系列未初始化 | 确保 Designer 中已 `LineSeries.Add()` 至少一个系列 |
| 多通道 JYUSB1601 数据显示错乱 | MajorOrder 方向错误 | ReadData 返回 `[samples,channels]`，用 `MajorOrder.Column` |
| dBV 频谱用 `IsLogarithmic=true` | 对数轴 ≠ dBV | 在数据层转换：`dBV[i]=20*log10(sqrt(power[i]))`，Y轴保持线性 |
| 后台线程 Plot() 崩溃 | 跨线程访问 UI | `this.Invoke(() => easyChartX1.Plot(...))` |
| 波形叠加不显示 | AutoClear=true | `easyChartX1.AutoClear = false` |
| 大数据绘图卡顿 | DataStorage=Clone + 大数组 | 改 `DataStorageType.NoClone` |
| 对数轴显示异常 | 数据含 <=0 值 | 开启 `CheckNegtiveOrZero=true` 或预处理数据 |

### StripChartX（滚动图）

```csharp
// 追加单通道数据
stripChartX1.Plot(double[] data);

// 追加多通道数据
stripChartX1.Plot(double[,] data, MajorOrder order);

// 清空
stripChartX1.Clear();
```

### 仪表控件通用属性

所有仪表（GaugeLinear、PressureGauge、Thermometer、Tank、Knob、Slide）均有：
- `Value` — 当前值
- `Maximum` / `Minimum` — 量程
- `LabelVisible` — 是否显示标签
- `ValueChangedEvent` — 值变更事件

### 开关控件

```csharp
// ButtonSwitch
buttonSwitch1.Checked = true/false;
buttonSwitch1.CheckedChanged += handler;

// ButtonSwitchArray
buttonSwitchArray1.Dimension = 4;          // 数量
buttonSwitchArray1.Direction = Direction.Horizontal;
buttonSwitchArray1.Value = new bool[]{};
buttonSwitchArray1.SetSingleValue(index, value);
buttonSwitchArray1.ControlValueChanged += (s, e) => {
    int idx = e.SelectedIndex;
    bool val = e.Data;
};
```

### EasyButton

```csharp
easyButton1.PresetImage = EasyButton.ButtonPresetImage.Check;
// 可选: None / Check / Close / Cancel / Back / Down / Go / Up
//       Folder / Refresh / Setting / FolderOpen / Document / ...
```

---

## 二、DSP 模块

### SeeSharpTools.JY.DSP.Fundamental

#### Generation 类

```csharp
// 正弦波（频率/采样率模式）
Generation.SineWave(ref double[] x, double amplitude, double phase, double frequency, double samplingRate);

// 正弦波（整数周期模式）
Generation.SineWave(ref double[] x, double amplitude, double phase, int numberOfCycles);

// 方波（频率模式）
Generation.SquareWave(ref double[] x, double amplitude, double dutyCycle, double frequency, double samplingRate);

// 方波（高低电平模式）
Generation.SquareWave(ref double[] x, double highLevel, double lowLevel, double dutyCycle, double frequency, double phase, double samplingRate);

// 等差数列
Generation.Ramp(ref double[] x, double start, double delta);

// 白噪声
Generation.UniformWhiteNoise(ref double[] x, double amplitude);
```

#### Generation.Analog 类（高级波形）

```csharp
// 高斯调制正弦波
double[] sig = Generation.Analog.GaussianModulatedSinePattern(
    sampleCount, ref envelope, amplitude, delay, samplingRate, centralFreq, attenuation, normalizedBandwidth);

// 高斯单脉冲
double[] sig = Generation.Analog.GaussianMonopulse(sampleCount, amplitude, delay, samplingRate, centralFreq);

// Sinc 信号
double[] sig = Generation.Analog.SincPattern(sampleCount, amplitude, delay, delta);

// Chirp 信号
double[] sig = Generation.Analog.ChirpPattern(sampleCount, startFreq, stopFreq, amplitude);

// 脉冲信号
double[] sig = Generation.Analog.PulsePattern(sampleCount, amplitude, delay, width);

// 冲激信号
double[] sig = Generation.Analog.ImpulsePattern(sampleCount, amplitude, delay);
```

#### Generation.Digital 类（数字波形）

```csharp
bool[,] digital;

// 梯度数字信号（周期依次减半）
digital = Generation.Digital.Gradient(sampleCount, signalCount);

// 移位数字信号
digital = Generation.Digital.Marching(sampleCount, signalCount, marchingValue: DigitalState.High, holdValue: DigitalState.Low);

// 周期数字信号
digital = Generation.Digital.Periodic(sampleCount, signalCount, periodCount, idleCountPerPeriod, initialState, idleValue);

// 全零或全一
digital = Generation.Digital.Single(sampleCount, signalCount, DigitalState.Low);

// 随机数字信号
digital = Generation.Digital.Random(sampleCount, signalCount);

// 交替数字信号
digital = Generation.Digital.Switch(sampleCount, signalCount, DigitalState.Low, DigitalState.High);
```

#### Spectrum 类——完整 API 参考

> **⚠️ Spectrum 类是 DSP 模块最核心的频谱分析 API，参数很多且容易混淆，请仔细阅读。**

##### PowerSpectrum——功率谱

> **🚨 致命错误防范：`ref spectrum` 必须预分配 `new double[N/2]`，传 `null` 直接导致 `NullReferenceException` 崩溃！**
>
> `Spectrum.PowerSpectrum` 内部不会检查 `spectrum` 是否为 `null` 或大小是否正确，而是直接对数组进行索引操作。
> - 传 `null` → `NullReferenceException`（运行时崩溃，无提示）
> - 传错误大小（如 `new double[N]`）→ `IndexOutOfRangeException` 或数据截断
> - **正确做法：始终 `new double[signal.Length / 2]`，多通道 `new double[channelCount, N / 2]`**
> - **此规则适用于所有 Spectrum 类方法：`PowerSpectrum`、`DBFullScaleSpectrum`、`AdvanceComplexFFT`、`AdvanceRealFFT`**

**重载 1：单通道（最常用）**

```csharp
Spectrum.PowerSpectrum(
    double[] waveform,          // 输入时域信号
    double samplingRate,        // 采样率 (S/s)
    ref double[] spectrum,      // 输出功率谱，必须预分配 new double[N/2]
    out double df,              // 输出频率分辨率 (Hz) = samplingRate/N
    SpectrumUnits unit,         // 输出单位（可选，默认 V2）
    WindowType windowType,      // 窗函数（可选，默认 None）
    double windowPara,          // 窗参数（可选，仅 Kaiser/Gaussian/Dolph-Chebyshev 用，其他传 0）
    bool PSD                    // 是否输出功率谱密度（可选，默认 false）
);
```

**重载 2：多通道**

```csharp
Spectrum.PowerSpectrum(
    double[,] waveform,         // 多通道输入，每行一个通道的数据
    double samplingRate,        // 采样率 (S/s)
    ref double[,] spectrum,     // 多通道输出，预分配 new double[channelCount, N/2]
    out double df,              // 频率分辨率 (Hz)
    SpectrumUnits unit,         // 输出单位
    WindowType windowType,      // 窗函数
    double windowPara,          // 窗参数
    bool PSD                    // 是否功率谱密度
);
```

**重载 3：单通道 + UnitConvSetting 高级单位设置**

```csharp
// UnitConvSetting 封装了复杂的单位转换设置，适用于需要自定义单位比例或偏移的场景
// 如无特殊需求，推荐优先使用重载 1（SpectrumUnits 枚举）
Spectrum.PowerSpectrum(
    double[] waveform,
    double samplingRate,
    ref double[] spectrum,          // ⚠️ 必须预分配 new double[N/2]
    out double df,
    UnitConvSetting unitSettings,   // 高级单位转换设置对象
    WindowType windowType,          // 窗函数
    double windowPara               // 窗参数（Kaiser/Gaussian/Dolph-Chebyshev 专用，其他传 0）
);
// UnitConvSetting 典型使用：
// UnitConvSetting unitCfg = new UnitConvSetting();
// unitCfg.SourceUnit = SpectrumUnits.V2;   // 原始单位
// unitCfg.TargetUnit = SpectrumUnits.dBV;  // 目标单位
// Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df, unitCfg, WindowType.Hanning, 0);
```

**关键参数说明**：

| 参数 | 类型 | 说明 |
|------|------|------|
| `waveform` | `double[]` / `double[,]` | 输入时域信号（多通道每行一个通道） |
| `samplingRate` | `double` | 采样率 (S/s)，不是采样间隔 |
| `spectrum` | `ref double[]` / `ref double[,]` | 🚨 必须预分配，长度 = N/2，**传 null 导致 NullReferenceException** |
| `df` | `out double` | 频率分辨率 = samplingRate / N |
| `unit` | `SpectrumUnits` | 输出单位，推荐 `dBV` 直接可视化 |
| `windowType` | `WindowType` | 窗函数，推荐 `Hanning` |
| `windowPara` | `double` | 窗参数，仅 Kaiser/Gaussian/Dolph-Chebyshev 有用，其他传 0 |
| `PSD` | `bool` | false=功率谱，true=功率谱密度 |

**调用示例**：

```csharp
// 最简调用（默认 V² + 无窗）
Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df);

// ⭐ 推荐调用（dBV + Hanning 窗）
Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df,
    SpectrumUnits.dBV, WindowType.Hanning, 0, false);

// 多通道
double[,] mSpec = new double[chCount, N / 2];
Spectrum.PowerSpectrum(multiSignal, sampleRate, ref mSpec, out df,
    SpectrumUnits.dBV, WindowType.Hanning, 0, false);
```

##### AmplitudeSpectrum / PhaseSpectrum——幅度谱 / 相位谱

```csharp
// 幅度谱
Spectrum.AmplitudeSpectrum(double[] signal, double sampleRate, ref double[] spectrum, out double df);

// 相位谱
Spectrum.PhaseSpectrum(double[] signal, double sampleRate, ref double[] spectrum, out double df);
```

##### PeakSpectrumAnalysis——峰值频率快速分析

```csharp
Spectrum.PeakSpectrumAnalysis(
    double[] waveform,      // 输入时域信号
    double dt,              // ⚠️ 采样间隔 (s) = 1.0 / samplingRate，不是采样率！
    out double peakFreq,    // 输出峰值频率 (Hz)
    out double peakAmp      // 输出峰值幅值 = 1.414 * RMS
);
// 用法示例：
// double dt = 1.0 / samplingRate;
// Spectrum.PeakSpectrumAnalysis(signal, dt, out peakFreq, out peakAmp);
```

##### DBFullScaleSpectrum——dBFS 频谱（相对满量程）

```csharp
Spectrum.DBFullScaleSpectrum(
    double[] waveform,          // 输入时域信号
    double samplingRate,        // 采样率 (S/s)
    double refFullScale,        // 满量程幅值 (V)，例如 ADC 满量程 10V
    ref double[] spectrum,      // 输出 dBFS 频谱，预分配 new double[N/2]
    out double df               // 频率分辨率 (Hz)
);
```

##### AdvanceComplexFFT——复数频谱

```csharp
// double 输入 → Complex 输出
Spectrum.AdvanceComplexFFT(
    double[] waveform,                          // 时域波形
    WindowType windowType,                      // 窗函数
    ref System.Numerics.Complex[] spectrum      // 输出复数频谱，预分配 new Complex[N/2+1]
);

// Complex 输入 → Complex 输出
Spectrum.AdvanceComplexFFT(
    System.Numerics.Complex[] waveform,
    WindowType windowType,
    ref System.Numerics.Complex[] spectrum
);
```

##### AdvanceRealFFT——实数高级 FFT（输出 double[] + SpectralInfo）

> **与 `AdvanceComplexFFT` 不同**：`AdvanceRealFFT` 输出 `double[]`（非 `Complex[]`），并通过 `SpectralInfo` 返回频谱参数信息。

```csharp
// 单通道
Spectrum.AdvanceRealFFT(
    double[] waveform,             // 时域波形
    int spectralLines,              // 频谱线条数（通常 = N/2）
    WindowType windowType,          // 窗函数
    double[] spectrum,              // 输出频谱数据（预分配 new double[spectralLines]）
    out SpectralInfo spectralInfo   // 输出频谱参数信息
);

// 多通道
Spectrum.AdvanceRealFFT(
    double[,] waveform,            // 多通道时域波形
    int spectralLines,              // 频谱线条数
    WindowType windowType,          // 窗函数
    double[,] spectrum,             // 输出频谱（预分配 new double[channelCount, spectralLines]）
    out SpectralInfo spectralInfo   // 输出频谱参数信息
);
```

**SpectralInfo 结构体**：由 `AdvanceRealFFT` 的 `out` 参数返回，包含频谱的元信息。

| 字段 | 类型 | 说明 |
|------|------|------|
| 用于描述频谱分辨率、频率范围等参数 | — | 具体字段视版本而定，作为后续单位转换的输入 |

##### UnitConversion——频谱单位转换

```csharp
// 将非线性单位 (dBV 等) 转换为线性单位
Spectrum.UnitConversion(ref double[] spectrum, SpectrumUnits unit);

// 高级转换（指定频谱类型 + 单位设置）
Spectrum.UnitConversion(double[] spectrum, double df,
    SpectrumType spectrumType, UnitConvSetting unitSetting, double equivalentNoiseBw);
```

##### ⚠️ Spectrum 高频易错点

| 错误 | 原因 | 正确做法 |
|------|------|--------|
| **spectrum 传 null** | **`ref` 不检查 null** | **🚨 必须 `new double[N/2]`，否则 `NullReferenceException` 崩溃** |
| spectrum 数组长度错误 | 功率谱输出点数 = N/2 | `new double[signal.Length / 2]` |
| spectrum 未预分配 | `ref` 参数要求已初始化 | 必须 `new double[N/2]` 后再传 |
| 缺少 `ref` / `out` 关键字 | 编译错误 | `ref spectrum, out df` |
| `PeakSpectrumAnalysis` 传采样率 | 参数是 dt=采样间隔 | `dt = 1.0 / samplingRate` |
| 手动转 dBV 不如 API 准确 | 缺少窗函数补偿等 | 用 `SpectrumUnits.dBV` 参数 |
| dBV 结果用对数Y轴显示 | dBV 已是对数线性值 | Y轴保持线性，标题写 "dBV" |
| AdvanceComplexFFT 数组长度错 | 复数谱点数 = N/2+1 | `new Complex[N/2 + 1]` |

#### WindowType 枚举（完整列表）

| 枚举值 | 说明 | 推荐场景 |
|--------|------|--------|
| `None` | 矩形窗（无窗） | 整周期采样、瞬态信号 |
| `Hanning` | 汉宁窗 (2-Term) ⭐ | **通用默认推荐** |
| `Hamming` | 海明窗 (2-Term) | 窗边缘不归零场景 |
| `Blackman_Harris` | 布莱克曼-哈里斯 (3-Term) | 高动态范围 |
| `Exact_Blackman` | 精确布莱克曼 (3-Term) | 高旁瓣抑制 |
| `Blackman` | 布莱克曼 (3-Term) | 通用 |
| `Three_Term_Nuttal` | 3-Term Nuttal | 高精度频谱 |
| `Three_Term_Blackman_Nuttal` | 3-Term 最小旁瓣 | 最小旁瓣场景 |
| `Four_Term_B_Harris` | 4-Term B-Harris | 高动态范围 |
| `Four_Term_Nuttal` | 4-Term Nuttal | 高精度 |
| `Four_Term_Blackman_Nuttal` | 4-Term 最小旁瓣 | 最小旁瓣 |
| `Flat_Top` | 平顶窗 | **幅值精确测量** |
| `Flat_Top_95` | HFT95 (-95dB) | 高精度幅值 |
| `Flat_Top_90D` | HFT90D (-90.2dB) | 高精度幅值 |
| `Five_Term_Least_Sidelobe` | 5-Term 低旁瓣 | 低旁瓣 |
| `Six_Term_Flat_Top` | 6-Term HFT116D (-116.8dB) | 极高精度 |
| `Six_Term_Least_Sidelobe` | 6-Term 低旁瓣 | 极低旁瓣 |
| `Seven_Term_Flat_Top` | 7-Term HFT144D (-144.1dB) | 最高精度 |
| `Seven_Term_Least_Sidelobe` | 7-Term 低旁瓣 | 最低旁瓣 |
| `Seven_Term_B_Harris` | 7-Term B-Harris | 最高动态范围 |

#### SpectrumUnits 枚举

| 枚举值 | 说明 | 典型用途 |
|--------|------|--------|
| `V` | 电压 (Voltage) | 幅度谱 |
| `V2` | V²（功率），默认 | 功率谱基础单位 |
| `W` | Watt | 功率测量 |
| `dBm` | 分贝毫瓦 | RF 信号 |
| `dBW` | 分贝瓦 | 大功率 |
| `dBV` | 分贝伏特 ⭐ | **可视化频谱推荐** |
| `dBmV` | 分贝毫伏 | 通信信号 |
| `dBuV` | 分贝微伏 | EMC/EMI 测试 |

### SeeSharpTools.JY.DSP.Utility

#### SignalOperation.Filtering——信号滤波

```csharp
// FIR 滤波（有限脉冲响应）
double[] SignalOperation.Filtering.FIRFilter(
    double[] x,               // 输入序列
    double[] coefficients      // FIR 滤波器系数（需自行生成或由 MathNet.Filtering 计算）
);
// 返回：滤波后序列

// IIR 滤波（无限脉冲响应）
double[] SignalOperation.Filtering.IIRFilter(
    double[] x,                    // 输入序列
    double[] forwardCoefficients,   // 前向系数（传递函数分子 B(z)）
    double[] reverseCoefficients    // 反向系数（传递函数分母 A(z)）
);
// 返回：滤波后序列

// 零相位滤波（双向 IIR，无相位延迟，未进行边界延拓）
double[] SignalOperation.Filtering.ZeroPhaseFilter(
    double[] x,                    // 输入序列
    double[] forwardCoefficients,   // 前向系数
    double[] reverseCoefficients    // 反向系数
);
// 返回：滤波后序列（⚠️ 未进行边界延拓，首尾可能失真）

// 中值滤波（非线性滤波，适合去毛刺）
double[] SignalOperation.Filtering.MedianFilter(
    double[] x,       // 输入序列
    int leftRank,     // 滤波器左侧元素数（必须 ≥ 0）
    int rightRank     // 右侧元素数（< 0 则 = leftRank）→ 窗口大小 = left + right + 1
);
```

#### MedianFilter 类——滑动窗口中值滤波

```csharp
// 滑动窗口中值滤波，逐采样点计算窗口内中值
double[] MedianFilter.Process(
    double[] signal,       // 输入信号
    int windowLength        // 窗口长度，必须为奇数且 ≥ 3（格式 2N+1）
);
// 返回：滤波后信号
```

#### SignalOperation.Analog——模拟信号操作

```csharp
// 求反（每个元素取相反数）
double[] SignalOperation.Analog.Invert(double[] x);

// 卷积（线性卷积 direct 方法）
double[] SignalOperation.Analog.Convolution(double[] x, double[] y);
// 返回长度 = x.Length + y.Length - 1

// 自相关
double[] SignalOperation.Analog.AutoCorrelation(
    double[] x,                  // 输入序列
    Normalization normalization   // 归一化方法: None / Biased / Unbiased
);

// 互相关
double[] SignalOperation.Analog.CrossCorrelation(
    double[] x, double[] y,      // 两个输入序列
    Normalization normalization   // 归一化方法
);

// 零填充至 2 的幂次
double[] SignalOperation.Analog.ZeroPadder(
    double[] x,         // 输入序列
    bool ignoreCurrent   // true: 仅当长度不是 2 的幂时填充；false: 总是填充到下一个 2的幂
);

// 相位展开（去除 ±π 跳变）
double[] SignalOperation.Analog.UnwrapPhase(
    double[] phase,     // 相位数组
    PhaseUnit phaseUnit  // radian / degree
);

// 缩放至 [-1, 1]
double[] SignalOperation.Analog.Scale(
    double[] x,           // 输入数组
    out double scale,     // 缩放因子
    out double offset     // 偏移量
);

// 归一化至 (0,1) 统计分布
double[] SignalOperation.Analog.Normalize(
    double[] x,               // 输入向量
    out double standDeviation, // 标准差
    out double mean            // 均值
);

// 升采样（在采样间插入零）
double[] SignalOperation.Analog.Upsample(
    double[] x,            // 输入序列
    int upsamplingFactor,   // 升采样因子（> 0）
    int leadingZeros        // 前导零数目（≥ 0 且 < upsamplingFactor）
);

// 降采样
double[] SignalOperation.Analog.Downsample(
    double[] x,             // 输入序列
    int downsamplingFactor,  // 降采样因子（> 0）
    bool averaging           // 是否对窗口内元素取平均
);

// 去趋势
void SignalOperation.Analog.Detrend(
    double[] x,                   // 输入序列
    out double[] detrended,       // 去趋势后的序列
    out double[] trend,           // 趋势序列
    DetrendMethod detrendMethod,  // 拟合方法: Linear / Polynomial / Exponential
    int order                     // 拟合阶数（仅 Polynomial 有效）
);

// 重采样（均匀采样）
double[] SignalOperation.Analog.Resampling(
    double[] x,                    // 原始序列
    double originalSamplingRate,    // 原采样率 (S/s)
    double newSamplingRate,         // 新采样率 (S/s)
    InterpolationMode mode          // Linear / Spline / FIRFilter
);

// 重采样（非均匀采样）
double[] SignalOperation.Analog.Resampling(
    double[] x,              // 原始序列
    double[] indexes,        // 输入序列的索引集合
    double newSamplingRate,   // 新采样率 (S/s)
    InterpolationMode mode    // 插值模式
);

// 权重移动平均（类似低通滤波）
double[] SignalOperation.Analog.WeightMovingAverage(
    double[] x,
    WeightAverageType type,          // Spencer_15_term / Henderson_7_term / Henderson_9_term / Henderson_13_term / Henderson_23_term
    double[] userDefinedWeights      // 自定义权重（奇数个对称数组），使用预定义类型时传 null
);

// 指数平均
double[] SignalOperation.Analog.ExponentialAverage(
    double[] x,
    WeightingFactors factors,    // 权重因子（Level, Trend, Season 属性）
    ExponentialType type,        // Single / Double / Tripple
    int seasonPeriod,            // 季节周期（仅 Tripple 有效）
    SeasonType seasonType        // Multiplicative / Additive（仅 Tripple 有效）
);
```

#### Normalization 枚举

| 枚举值 | 说明 |
|--------|------|
| `None` | 无归一化 |
| `Biased` | 偏差归一化（除以 N） |
| `Unbiased` | 无偏差归一化（除以 N-|lag|） |

#### PhaseUnit 枚举

| 枚举值 | 说明 |
|--------|------|
| `radian` | 弧度 |
| `degree` | 角度 |

#### InterpolationMode 枚举

| 枚举值 | 说明 |
|--------|------|
| `Linear` | 线性插值 |
| `Spline` | 样条插值 |
| `FIRFilter` | FIR 滤波插值 |

#### DetrendMethod 枚举

| 枚举值 | 说明 |
|--------|------|
| `Linear` | 线性拟合 |
| `Polynomial` | 多项式拟合（需指定 order） |
| `Exponential` | 指数拟合 |

#### WeightAverageType 枚举

| 枚举值 | 说明 |
|--------|------|
| `Spencer_15_term` | Spencer 15点移动平均 |
| `Henderson_7_term` | Henderson 7点移动平均 |
| `Henderson_9_term` | Henderson 9点移动平均 |
| `Henderson_13_term` | Henderson 13点移动平均 |
| `Henderson_23_term` | Henderson 23点移动平均 |

#### FlatSelection 枚举（峰谷分析平台区策略）

| 枚举值 | 说明 |
|--------|------|
| `First` | 保留平台区第一个元素 |
| `Center` | 保留平台区中心元素 |
| `Last` | 保留平台区最后一个元素 |
| `All` | 保留所有元素 |

#### PeakValleyAnalysis 类——峰谷分析

```csharp
// 查找波峰/波谷
double[,] PeakValleyAnalysis.FindPeaks(
    double[] data,               // 输入数据
    double[] x,                  // 位置向量（长度同 data）
    double minPeakHeight,        // 最小峰值高度
    double minPeakDistance,      // 最小峰间距
    double threshold,            // 峰值与邻点最小高度差（排除平峰）
    FlatSelection flatSelection, // 平台区策略
    bool isValley                // false=峰值, true=谷值
);
// 返回: double[peakCount, 2]，列0=峰值高度，列1=峰值位置
```

#### Phase 类——相位测量

```csharp
// 计算两个波形的相位差（基于希尔伯特变换）
double Phase.CalPhaseShift(double[] signal1, double[] signal2);   // 返回 -180° ~ 180°
double Phase.CalPhaseShift(float[] signal1, float[] signal2);    // float 重载
```

#### ToneAnalyzer 类——单音分析

```csharp
// ⚠️ 注意：参数是 Fs（采样率），不是 dt！与 HarmonicAnalysis.ToneAnalysis 参数不同
ToneInfo ToneAnalyzer.SingleToneAnalysis(
    double[] timewaveform,   // 时域波形
    double Fs,               // 采样率 (Hz)，不是 dt！
    double initialGuess,     // 预估频率 (Hz)
    double searchRange       // 搜索范围 (Hz)
);
// 返回 ToneInfo { Frequency, Amplitude, Phase }

// ToneInfo 结构体
public struct ToneInfo {
    public double Frequency;   // 频率 (Hz)
    public double Amplitude;   // 幅值
    public double Phase;       // 相位 (弧度)
}
```

#### SquarewaveMeasurements 类——方波测量

```csharp
var sqm = new SquarewaveMeasurements();
sqm.SetWaveform(double[] inputWaveform);      // 设置第 1 路方波
double high    = sqm.GetHighStateLevel();      // 获取高电平
double low     = sqm.GetLowStateLevel();       // 获取低电平
double period  = sqm.GetPeriod();              // 获取周期
sqm.SetWaveform2(double[] inputWaveform);     // 设置第 2 路方波
double phase   = sqm.GetPhase();               // 获取两路方波相位差
```

#### Synchronizer 类——多通道同步

```csharp
var sync = new Synchronizer();
// 同步多通道信号（仅适用于带限信号如正弦波）
double[,] Synchronizer.Sync(
    double[,] data           // 输入: [numberOfChannels, samplesPerChannel]
);
// 返回：同步后数据（⚠️ 输出数组比输入小，截断了稳定点）

double[,] Synchronizer.Sync(
    double[,] data,          // 输入数据
    double shiftPoints       // 移位点数
);
```

#### SystemNoiseCalculation 类——系统噪声

```csharp
// 频域法（指定频段内的噪声，去 DC）
double SystemNoiseCalculation.CalculateSystemNoise(
    double[] timewaveform,    // 时域波形
    double dt,                // 采样间隔 = 1/sampleRate
    double startFrequency,    // 起始频率（FFT bin0 和 bin1 已移除）
    double stopFrequency      // 终止频率
);

// 时域法（整体噪声，去 DC）
double SystemNoiseCalculation.CalculateSystemNoise(
    double[] timewaveform     // 时域波形
);
```

#### SignalProcessing 类——信号处理工具

```csharp
// 阈值检测（返回超过/低于阈值的集合）
var result = SignalProcessing.CheckThreshold(
    double[] data, double threshold, bool isAboveLevel);
// isAboveLevel: true=侵测波峰, false=侵测波谷

// 过零点检测
var result = SignalProcessing.CheckCrossZeroPoints(
    double[] data, bool isAbove);

// 自定义上下限范围检测（三个数组长度相同）
var result = SignalProcessing.CheckInRange(
    double[] data, double[] highLimit, double[] lowLimit);
// 返回: 1=超上限, -1=低于下限, 0=范围内

// AC/DC 成分估算（单位 RMS）
void SignalProcessing.EstimateACDC(
    double[] signalData, out double acTerm, out double dcTerm);

// 内插值计算
double SignalProcessing.Interpolate(
    double[] xValues, double[] yValues, double xPoint,
    IntepolationType type);  // Linear / CubicSpline / LogLinear / Polynomial / Step
```

#### IntepolationType 枚举

| 枚举值 | 说明 |
|--------|------|
| `Linear` | 线性内插 |
| `CubicSpline` | 三次样条内插 |
| `LogLinear` | 对数线性内插 |
| `Polynomial` | 多项式内插 |
| `Step` | 阶梯内插 |

### SeeSharpTools.JY.DSP.Measurements

#### HarmonicAnalysis 类——完整 API 参考

> **🚨 核心规则（必读，违反将导致结果完全错误）：**
> - `dt` = 采样间隔（秒）= `1.0 / sampleRate`，不是采样率！
> - **`componentsLevel`、`componentsPhase` 是 `ref` 参数（不是 `out`）！必须预先 `new double[highestHarmonic + 1]`，否则 `NullReferenceException`**
> - `THDAnalysis` 返回的 `thd` 是比值（如 0.05），不是百分比，不是 dB
> - `componentsLevel` 索引：[0]=DC，[1]=基波，[2]=2次谐波，单位是峰値电压（Vpk = 1.414×RMS）
> - `highestHarmonic` 超过奈奎斯特频率的谐波分量自动置 0，不报错
> - 命名空间：`SeeSharpTools.JY.DSP.Measurements`， DLL：`SeeSharpTools.JY.DSP.Measurements.dll`

```csharp
// ──────────────────────────────────────────────────────────
// THDAnalysis 重载 1：基本版（最常用）
// 公式：THD = sqrt(∑谐波功率 / 基波功率)
// ⚠️ componentsLevel 是 ref（不是 out），必须预先 new double[highestHarmonic+1]
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.THDAnalysis(
    double[] timewaveform,              // 时域信号（单位 V）
    double dt,                          // ⚠️ 采样间隔 (s) = 1/sampleRate
    out double detectedFundamentalFreq, // 输出：基波频率 (Hz)
    out double THD,                     // 输出：THD 比值（非 %，非 dB）
    ref double[] componentsLevel,       // ⚠️ ref：[0]=DC,[1]=基波,[2]=2次... Vpk
    int highestHarmonic = 10            // 默认值 10
);
// THD 转百分比：THD * 100
// THD 转 dB：20 * Math.Log10(THD)

// ──────────────────────────────────────────────────────────
// THDAnalysis 重载 2：含相位信息版（allowAliasing 控制混叠）
// ⚠️ componentsLevel、componentsPhase 均是 ref，必须预先 new
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.THDAnalysis(
    double[] timewaveform,              // 时域信号（单位 V）
    double dt,                          // ⚠️ 采样间隔 (s) = 1/sampleRate
    out double detectedFundamentalFreq, // 输出：基波频率 (Hz)
    out double THD,                     // 输出：THD 比值
    ref double[] componentsLevel,       // ⚠️ ref：幅值[0]=DC,[1]=基波... Vpk
    ref double[] componentsPhase,       // ⚠️ ref：相位[0]=DC,[1]=基波... (弧度)
    int highestHarmonic = 10,           // 默认值 10
    bool allowAliasing = true           // true=允许超奈奎斯特谐波（置0），false=严格限制
);
// componentsPhase 单位弧度，转角度：phase * 180.0 / Math.PI

// ──────────────────────────────────────────────────────────
// ToneAnalysis 重载 1：实数时域信号（基波类型为 sin）
// 适用于：精确获取基波频率/幅值/相位（如相位差测量）
// initialGuess=0 表示自动搜索全频段，searchRange=0.05 为相对范围
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.ToneAnalysis(
    double[] timewaveform,              // 时域信号（double[]，实数）
    double dt,                          // ⚠️ 采样间隔 (s) = 1/sampleRate
    out double detectedFundamentalFreq, // 输出：基波频率 (Hz)
    out double amplitude,               // 输出：基波幅值（峰值 Vpk）
    out double phase,                   // 输出：基波相位（弧度）
    double initialGuess = 0,            // 预估基波频率 (Hz)，0=自动全频搜索
    double searchRange  = 0.05          // 搜索范围（相对值或 Hz）
);
// ⚠️ phase 单位弧度，转角度：phase * 180.0 / Math.PI
// ⚠️ initialGuess=0 时 searchRange 被忽略，全频段搜索最大峰值

// ──────────────────────────────────────────────────────────
// ToneAnalysis 重载 2：复数时域信号（基波类型为 cos）
// 适用于：已通过 IQ 解调或复数采样器得到的复数时域信号
// 与重载 1 区别：输入为 Complex[]，基波类型为 cos（不是 sin）
// initialGuess 默认值为 NaN（全频搜索）
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.ToneAnalysis(
    System.Numerics.Complex[] timewaveform, // 复数时域信号（IQ 数据）
    double dt,                          // ⚠️ 采样间隔 (s) = 1/sampleRate
    out double detectedFundamentalFreq, // 输出：基波频率 (Hz)
    out double amplitude,               // 输出：基波幅值（峰值 Vpk）
    out double phase,                   // 输出：基波相位（弧度）
    double initialGuess = double.NaN,   // NaN=全频搜索；指定 Hz=在附近搜索
    double searchRange  = 0.05          // 在 initialGuess 附近搜索的范围
);
// ⚠️ phase 单位弧度，转角度：phase * 180.0 / Math.PI
// ★ 示例：
// using System.Numerics;
// // 构造复数信号（实际应用中通常来自 IQ 彩源或复数 ADC）
// Complex[] iqSignal = new Complex[N];
// for (int i = 0; i < N; i++)
//     iqSignal[i] = new Complex(realPart[i], imagPart[i]);
// double freq, amp, phase;
// HarmonicAnalysis.ToneAnalysis(
//     iqSignal, dt,
//     out freq, out amp, out phase);
// // 如需在附近 1000 Hz 内搜索：
// HarmonicAnalysis.ToneAnalysis(
//     iqSignal, dt,
//     out freq, out amp, out phase,
//     initialGuess: 1000.0, searchRange: 100.0);

// ──────────────────────────────────────────────────────────
// SINADAnalysis：信纳比 (S+D+N)/(D+N)，单位 dB
// ⚠️ componentsLevel 是 ref，必须预先 new double[highestHarmonic+1]
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.SINADAnalysis(
    double[] timewaveform, double dt,
    out double detectedFundamentalFreq,
    out double SINAD,                   // 输出：SINAD 值 (dB)
    ref double[] componentsLevel,       // ⚠️ ref：[0]=DC,[1]=基波... Vpk
    int highestHarmonic = 10            // 默认值 10
);

// ──────────────────────────────────────────────────────────
// SNRAnalysis：信噪比 S/N，单位 dB（不含谐波失真，仅噪声）
// ⚠️ componentsLevel 是 ref，必须预先 new double[highestHarmonic+1]
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.SNRAnalysis(
    double[] timewaveform, double dt,
    out double detectedFundamentalFreq,
    out double SNR,                     // 输出：SNR 值 (dB)
    ref double[] componentsLevel,       // ⚠️ ref：[0]=DC,[1]=基波... Vpk
    int highestHarmonic = 10            // 默认值 10
);

// ──────────────────────────────────────────────────────────
// SFDRAnalysis：无杂散动态范围——基波功率/最大杂散功率，单位 dBc
// ⚠️ componentsLevel 是 ref，必须预先 new double[highestHarmonic+1]
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.SFDRAnalysis(
    double[] timewaveform, double dt,
    out double detectedFundamentalFreq,
    out double SFDR,                    // 输出：SFDR 值 (dBc)
    ref double[] componentsLevel,       // ⚠️ ref：[0]=DC,[1]=基波... Vpk
    int highestHarmonic = 10            // 默认值 10
);

// ──────────────────────────────────────────────────────────
// FullHarmonicAnalysis：完整谐波分析，返回 HarmonicAnalysisResult
// ⚠️ componentsLevel 必须预先分配！使用 ref 传入（不是 out）
// ──────────────────────────────────────────────────────────
double[] componentsLevel = new double[highestHarmonic + 1]; // ⚠️ 必须预先 new
HarmonicAnalysisResult result = HarmonicAnalysis.FullHarmonicAnalysis(
    double[] timewaveform, double dt,
    out double detectedFundamentalFreq,
    ref double[] componentsLevel,       // ⚠️ ref（不是 out！）：[0]=DC,[1]=基波,[2]=2次... Vpk
    int highestHarmonic = 10
);
// 返回的 HarmonicAnalysisResult 字段（全部为 dB）：
//   result.THD    — 总谐波失真 (dB)
//   result.SINAD  — 信纳比 (dB)
//   result.SNR    — 信噪比 (dB)
//   result.SFDR   — 无杂散动态范围 (dBc)
//   result.ENOB   — 有效位数 (bits)
// componentsLevel 长度 = highestHarmonic + 1

// ──────────────────────────────────────────────────────────
// BasicAnalysis：基础分析（功率分解详情）
// ⚠️ componentsLevel 是 ref，必须预先 new
// ──────────────────────────────────────────────────────────
HarmonicAnalysis.BasicAnalysis(
    double[] timewaveform, double dt,
    out double detectedFundamentalFreq,
    out double fundamentalPower,        // 输出：基波功率 (V²)
    out double powerTotalHarmonic,      // 输出：所有谐波总功率 (V²)
    out double noisePower,              // 输出：噪声功率 (V²)
    out double secondPeak,              // 输出：除DC和基波外最大分量功率 (V²)
    ref double[] componentsLevel,       // ⚠️ ref（不是 out）：尺度 Vpk
    int highestHarmonic
);
// 基于功率的手动计算：
// SNR  = 10 * log10(fundamentalPower / noisePower)
// THD  = sqrt(powerTotalHarmonic / fundamentalPower)
// SFDR = 10 * log10(fundamentalPower / secondPeak)

// ──────────────────────────────────────────────────────────
// SingleToneAnalysis：高精度单音分析——⚠️ Fs = 采样率 (Hz)！
// ──────────────────────────────────────────────────────────
ToneInfo HarmonicAnalysis.SingleToneAnalysis(
    double[] timewaveform,
    double Fs,            // ⚠️ 采样率 (Hz)，不是 dt！
    double initialGuess, double searchRange
);
// ToneInfo 字段：.Frequency (Hz), .Amplitude (Vpk), .Phase (弧度)
```

#### HarmonicAnalysis 指标对比表

| 方法 | 输出指标 | 单位 | 公式定义 |
|------|----------|------|----------|
| `THDAnalysis` | THD | 比值（*100=%，20log=dB） | sqrt(∑谐波功率/基波功率) |
| `SINADAnalysis` | SINAD | dB | (S+D+N)/(D+N) |
| `SNRAnalysis` | SNR | dB | S/N（排除谐波失真） |
| `SFDRAnalysis` | SFDR | dBc | 基波/最大杂散分量 |
| `ToneAnalysis` | freq+amp+phase | Hz/Vpk/rad | 基波轨边修正 |
| `FullHarmonicAnalysis` | componentsLevel[] | Vpk | 各次谐波幅度 |
| `BasicAnalysis` | 功率分解 | V² | 基波/谐波/噪声/次峰功率 |

#### JitterAnalysis 类——抱动分析（高级）

```csharp
using SeeSharpTools.JY.DSP.Measurements;

// 1. 测量状态电平
StateLevels levels = JitterAnalysis.Level.MeasureStateLevel(signal, new StateLevelsSettings());

// 2. 测量参考电平
ReferenceLevels refLevels = JitterAnalysis.Level.MeasureReferenceLevel(
    levels, new ReferenceLevelsSettings());

// 3. 查找跳变
Transition[] transitions = JitterAnalysis.Level.FindTransitions(signal, refLevels);

// 4. 时间测量
LevelCrossings crossings = JitterAnalysis.Timing.FindLevelCrossings(
    signal, dt, transitions, crossingLevel);
double[] periods = JitterAnalysis.Timing.MeasurePeriod(crossings);
double[] riseTime = JitterAnalysis.Timing.MeasureRiseTime(transitions, dt);
double[] fallTime = JitterAnalysis.Timing.MeasureFallTime(transitions, dt);
```

### ~~SeeSharpTools.JY.DSP.SoundVibration~~（音频/振动谐波分析）— ⚠️ 已淘汰

> **⚠️ `HarmonicAnalyzer.ToneAnalysis` 已淘汰，新代码应改用 `SeeSharpTools.JY.DSP.Measurements.HarmonicAnalysis.FullHarmonicAnalysis`**
>
> | 已过时（SoundVibration） | 替代（Measurements） |
> |------------------------|-------------------|
> | `HarmonicAnalyzer.ToneAnalysis(data, dt, n, inDB)` → `ToneAnalysisResult` | `HarmonicAnalysis.FullHarmonicAnalysis(data, dt, out freq, ref levels, n)` → `HarmonicAnalysisResult` |
> | `HarmonicAnalyzer.ToneAnalysis(data, dt, out freq, out thd, ref levels, n)` | `HarmonicAnalysis.THDAnalysis(data, dt, out freq, out thd, ref levels, n)` |
>
> **字段对照**：
> - `ToneAnalysisResult`（旧）：`THD`, `THDplusN`, `SINAD`, `SNR`, `NoiseFloor`, `ENOB`
> - `HarmonicAnalysisResult`（新）：`THD`, `SINAD`, `SNR`, `ENOB`, `SFDR`（**无 THDplusN / NoiseFloor**）
> - ⚠️ `HarmonicAnalysis.ToneAnalysis(...)` 是单音频率/幅值/相位测量，**不是谐波指标评估的替代**

#### HarmonicAnalyzer 类（已淘汰）

```csharp
// ── 旧 API（仅供维护旧项目参考）──
// 重载 1：最简调用（默认参数，自动检测基波）
ToneAnalysisResult HarmonicAnalyzer.ToneAnalysis(
    double[] timewaveform
);

// 重载 2：指定 dt 和最高谐波次数
ToneAnalysisResult HarmonicAnalyzer.ToneAnalysis(
    double[] timewaveform,
    double dt,                     // ⚠️ 采样间隔 = 1.0 / sampleRate
    int highestHarmonic,
    bool resultInDB
);

// 重载 3：详细输出
void HarmonicAnalyzer.ToneAnalysis(
    double[] timewaveform,
    double dt,
    out double detectedFundamentalFreq,
    out double THD,
    out double[] componentsLevel,
    int highestHarmonic
);
```

#### ToneAnalysisResult 结构体（已淘汰）

| 字段 | 类型 | 说明 | 新版对应 |
|------|------|------|--------|
| `THD` | `double` | 总谐波失真 | `HarmonicAnalysisResult.THD` |
| `THDplusN` | `double` | THD + 噪声 | ❌ 无替代 |
| `SINAD` | `double` | 信纳比 | `HarmonicAnalysisResult.SINAD` |
| `SNR` | `double` | 信噪比 | `HarmonicAnalysisResult.SNR` |
| `NoiseFloor` | `double` | 噪声本底 | ❌ 无替代 |
| `ENOB` | `double` | 有效位数 | `HarmonicAnalysisResult.ENOB` |

**✅ 迁移示例（新写法）**：

```csharp
using SeeSharpTools.JY.DSP.Measurements;

double dt = 1.0 / sampleRate;         // ⚠️ 采样间隔
double fundFreq;
double[] components = new double[11]; // ⚠️ 必须预分配，长度 = highestHarmonic + 1
HarmonicAnalysisResult result = HarmonicAnalysis.FullHarmonicAnalysis(
    signal, dt, out fundFreq, ref components, highestHarmonic: 10);
// result.THD / result.SINAD / result.SNR / result.ENOB / result.SFDR
// fundFreq：检测到的基波频率 (Hz)
// components[1]：基波幅值；components[2]：2次谐波幅值...
```

#### ⚠️ SoundVibration 迁移易错点

| 错误 | 原因 | 正确做法 |
|------|------|--------|
| 用 `HarmonicAnalysis.ToneAnalysis` 替代 `HarmonicAnalyzer.ToneAnalysis` | 完全不同用途 | `HarmonicAnalysis.ToneAnalysis` 是单音频率/幅值测量 |
| `components` 未预分配 | `FullHarmonicAnalysis` 的 `ref` 参数必须 `new` | `new double[highestHarmonic + 1]` |
| 迁移后访问 `.THDplusN` / `.NoiseFloor` | `HarmonicAnalysisResult` 无此字段 | 如需这两项，保留旧版 `HarmonicAnalyzer` 调用 |
| `dt` 传入采样率 | `dt = 1.0 / sampleRate`（采样间隔） | `double dt = 1.0 / sampleRate` |

### SeeSharpTools.JY.DSP.Measurements.WaveformMeasurements（波形测量，新版 API）

> **与旧版 `SquarewaveMeasurements`（SeeSharpTools.JY.DSP.Utility）的区别**：
> - 旧版：实例方法 `SetWaveform()` → `GetHighStateLevel()` 等
> - 新版（本节）：**静态方法**，功能更全面，支持多通道，位于 `SeeSharpTools.JY.DSP.Measurements` 命名空间
> - 新版在 SeeSharpExamples 范例中使用 `SquarewaveMeasurements` 类名调用（实际类为 `WaveformMeasurements`）

> **⚠️ 所有方法的 `dt` 参数 = 采样间隔 = 1.0/sampleRate！**

#### 配置类型

```csharp
// 高低电平判定设置
var stateSetting = new HighLowStateSetting()
{
    Method = HighLowStateMethod.HistogramMode,  // 或 HistogramMode
    HistogramSize = 256                          // 直方图 bin 数量
};

// 脉冲参考电平设置
var refLevel = new PulseReferenceLevel()
{
    High = 0.9,                           // 高参考电平（百分比或绝对值）
    Low = 0.1,                            // 低参考电平
    Middle = 0.5,                         // 中参考电平
    Unit = PulseReferenceUnit.Percentage  // Percentage 或 Absolute
};
```

#### AmplitudeLevelAnalysis——幅度电平分析

```csharp
// 单通道
WaveformMeasurements.AmplitudeLevelAnalysis(
    double[] waveform,                    // 输入波形
    HighLowStateSetting setting,          // 高低电平设置
    out double amplitude,                 // 输出幅值
    out double highLevel,                 // 输出高电平
    out double lowLevel                   // 输出低电平
);

// 多通道
WaveformMeasurements.AmplitudeLevelAnalysis(
    double[][] waveforms,                 // 多通道输入
    HighLowStateSetting setting,
    out double[] amplitudes,
    out double[] highLevels,
    out double[] lowLevels
);
```

#### PulseMeasurement——脉冲测量（周期/脉宽/占空比）

```csharp
// 单通道
WaveformMeasurements.PulseMeasurement(
    double[] waveform,                    // 输入波形
    double dt,                            // ⚠️ 采样间隔 = 1.0/sampleRate
    HighLowStateSetting setting,          // 高低电平设置
    PulsePolarity polarity,               // High 或 Low
    PulseReferenceLevel refLevel,         // 参考电平设置
    int pulseNumber,                      // 要测量的脉冲序号（1-based）
    out double period,                    // 输出周期 (s)
    out double pulseDuration,             // 输出脉宽 (s)
    out double dutyCycle,                 // 输出占空比（0~1）
    out PulseMeasurementInfo info         // 输出脉冲信息（PulseCenter, PulseReferenceLevel）
);
```

#### PeriodAnalysis——周期统计分析

```csharp
// 单通道（统计所有周期的 min/max/avg）
WaveformMeasurements.PeriodAnalysis(
    double[] waveform,
    double dt,
    HighLowStateSetting setting,
    PulsePolarity polarity,
    PulseReferenceLevel refLevel,
    out PeriodAnalysisResult periodResult,     // 周期统计
    out PeriodAnalysisResult dutyCycleResult,  // 占空比统计
    out PeriodAnalysisResult pulseResult,      // 脉宽统计
    out MeasurementInfo info
);
// PeriodAnalysisResult 属性: Count, Minimum, Maximum, Average
```

#### CycleRmsMeanAnalysis——周期 RMS/均值分析

```csharp
// 单通道 - 指定周期
WaveformMeasurements.CycleRmsMeanAnalysis(
    double[] waveform,
    double dt,
    HighLowStateSetting setting,
    PulseReferenceLevel refLevel,
    int cycleNumber,                      // 要测量的周期序号（1-based）
    out double rmsResult,                 // 输出 RMS
    out double meanResult,                // 输出均值
    out MeasurementInfo info
);

// 单通道 - 所有周期
WaveformMeasurements.CycleRmsMeanAnalysis(
    double[] waveform,
    double dt,
    HighLowStateSetting setting,
    PulseReferenceLevel refLevel,
    out double[] rmsResults,              // 每个周期的 RMS
    out double[] meanResults,             // 每个周期的均值
    out MeasurementInfo[] infos
);
```

#### TransitionMeasurement——跳变/边沿测量

```csharp
// 单通道
WaveformMeasurements.TransitionMeasurement(
    double[] waveform,
    double dt,
    HighLowStateSetting setting,
    EdgePolarity polarity,                // Rising 或 Falling
    PulseReferenceLevel refLevel,
    int edgeNumber,                       // 要测量的边沿序号（1-based）
    out double slope,                     // 输出斜率
    out double transitionDuration,        // 输出上升/下降时间 (s)
    out TransitionInfo preTransInfo,      // 输出预转换信息
    out TransitionInfo postTransInfo,     // 输出后转换信息
    out MeasurementInfo info
);
// TransitionInfo 属性: Overshoot, Undershoot
```

#### 辅助类型

| 类型 | 字段/属性 | 说明 |
|------|----------|------|
| `PeriodAnalysisResult` | `Count`, `Minimum`, `Maximum`, `Average` | 周期/占空比/脉宽的统计结果 |
| `PulseMeasurementInfo` | `PulseCenter`, `PulseReferenceLevel` | 脉冲中心时间和参考电平 |
| `MeasurementInfo` | `StartTime`, `EndTime`, `PulseReferenceLevel` | 测量区间和参考电平 |
| `TransitionInfo` | `Overshoot`, `Undershoot` | 过冲和下冲 |
| `PulsePolarity` 枚举 | `High`, `Low` | 脉冲极性 |
| `EdgePolarity` 枚举 | `Rising`, `Falling` | 边沿极性 |
| `PulseReferenceUnit` 枚举 | `Percentage`, `Absolute` | 参考电平单位 |
| `HighLowStateMethod` 枚举 | `HistogramMode` 等 | 高低电平判定方法 |

#### ⚠️ WaveformMeasurements 高频易错点

| 错误 | 原因 | 正确做法 |
|------|------|--------|
| dt 传采样率 | 参数是采样间隔 | `dt = 1.0 / sampleRate` |
| pulseNumber / cycleNumber 传 0 | 序号是 1-based | 从 1 开始 |
| 不配置 HighLowStateSetting | 默认值可能不适合 | 根据信号幅度配置 Method 和 HistogramSize |
| 不配置 PulseReferenceLevel | 默认参考电平可能不匹配 | 设置 High=0.9, Low=0.1, Middle=0.5, Unit=Percentage |
| 忘记 try-catch | 信号质量差时可能抛异常 | 所有测量方法应包裹 try-catch |
| 新旧 API 混用 | 命名空间不同 | Utility 是旧版实例 API，Measurements 是新版静态 API |

### SeeSharpTools.JY.DSP.Utility.Spectrum（高级频谱分析库）

> **DLL**：`SeeSharpTools.JY.DSP.Utility.Spectrum.dll`（需单独添加引用，与 `SeeSharpTools.JY.DSP.Fundamental` 中的 `Spectrum` 类为**不同模块**）
> **命名空间**：`SeeSharpTools.JY.DSP.Utility.Spectrum`
> **特点**：提供 Fourier 变换（FFT/ChirpZ）、功率谱估计（Periodogram/Pwelch）、频谱平均、单音/多音提取、失真分析（THD/SNR/SFDR/SINAD/TOI）、频谱特征测量（带宽/频率）、Hilbert 变换、DCT、窗函数库等完整频谱分析功能
>
> **⚠️ 与 `SeeSharpTools.JY.DSP.Fundamental.Spectrum` 的区别**：
> - `DSP.Fundamental.Spectrum`：基础频谱（`PowerSpectrum`、`AmplitudeSpectrum`、`PeakSpectrumAnalysis` 等），需 `ref` 预分配数组
> - `DSP.Utility.Spectrum`：高级频谱（`SpectralEstimation.Pwelch`、`Fourier.Forward`、`Distortion.THD` 等），使用 ValueTuple 返回值，API 更现代

#### Fourier——傅里叶变换

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;
using System.Numerics;

// ── FFT 正变换（实数输入）──
Complex[] y = Fourier.Forward(double[] x, double size);
// size: FFT 长度（可大于 x.Length 补零，提高频率分辨率）
// 返回：单边复数频谱

// ── FFT 正变换（原位 Complex 数组）──
Fourier.Forward(ref Complex[] x);

// ── FFT 逆变换 ──
Fourier.Inverse(ref Complex[] x);
Complex[] y = Fourier.Inverse(double[] x, double size);

// ── ChirpZ 变换（线性调频 Z 变换，指定频率范围高分辨率分析）──
// 用于在指定频段内获得比 FFT 更高的频率分辨率
Complex[] y = Fourier.ChirpZ(double[] x, Complex ratio, Complex startPoint, double size);
Complex[] y = Fourier.ChirpZ(double[] x, double sampleRate, double startFreq, double stopFreq, double size);

// ── FFTShift（将零频分量移至中心）──
T[] shifted = Fourier.FFTShift(T[] x);
T[] unshifted = Fourier.InverseFFTShift(T[] x);
```

#### SpectralEstimation——频谱估计（核心类）

> **SpectralEstimation 是 Utility.Spectrum 最常用的类，提供功率谱、交叉谱、传递函数等估计方法。**
> **所有方法均使用 ValueTuple 返回多个结果，支持 C# 7.0+ 解构语法。**

##### Periodogram——周期图法

```csharp
// 单通道实数信号
var (psd, f) = SpectralEstimation.Periodogram(
    double[] x,              // 输入信号
    double sampleRate,       // 采样率 (Hz)
    double[] window,         // 窗函数系数（用 Windows.GetCoefficients 生成，或 null 用矩形窗）
    double nfft,             // FFT 长度（double.NaN 则自动 = x.Length）
    FrequencyRange freqRange, // OneSided / TwoSided / Centered
    ExportMode exportMode,   // PowerSpectrum / PowerSpectrumDensity
    bool dB                  // true=输出 dB 单位
);
// 返回：(double[] psd, double[] f)

// 多通道信号
var (psd, f) = SpectralEstimation.Periodogram(
    double[,] x, double sampleRate, double[] window, double nfft,
    FrequencyRange freqRange, ExportMode exportMode, bool dB);
// 返回：(double[,] psd, double[] f)

// 简化调用（默认参数）
var (psd, f) = SpectralEstimation.Periodogram(x, sampleRate, dB: true);
```

##### Pwelch——Welch 平均周期图法

```csharp
var (power, f) = SpectralEstimation.Pwelch(
    double[] x,              // 输入信号
    double sampleRate,       // 采样率 (Hz)
    double segLength,        // 分段长度（采样点数）
    WindowType windowType,   // 窗函数类型
    double windowParam,      // 窗参数（double.NaN 使用默认）
    double noverlap,         // 重叠长度（采样点数）
    double nfft,             // FFT 长度
    TraceMode traceMode,     // Average / MaxHold / MinHold
    FrequencyRange freqRange, // OneSided / TwoSided / Centered
    ExportMode exportMode,   // PowerSpectrum / PowerSpectrumDensity
    bool dB                  // true=dB 输出
);
// 返回：(double[] power, double[] f)

// 多通道
var (power, f) = SpectralEstimation.Pwelch(
    double[,] x, double sampleRate,
    windowType: WindowType.Hanning, nfft: x.GetLength(0), dB: true);
```

##### PowerSpectrum——功率谱（简化 API）

```csharp
// 自动选择频率分辨率和泄漏参数
var (spectrum, f) = SpectralEstimation.PowerSpectrum(
    double[] x,             // 输入信号
    double sampleRate,      // 采样率
    double frequencyResolution,  // 频率分辨率（double.NaN 自动）
    double leakage,         // 泄漏参数 0~1（控制窗函数选择）
    bool twoSided,          // 是否双边谱
    bool dB                 // 是否 dB 输出
);
// 多通道
var (spectrum, f) = SpectralEstimation.PowerSpectrum(
    double[,] x, double sampleRate, double frequencyResolution, double leakage,
    bool twoSided, bool dB);
```

##### AverageMagPhaseSpectrum——平均幅度/相位谱

```csharp
var (mag, phase, df, averagesCompleted, averagingDone) =
    SpectralEstimation.AverageMagPhaseSpectrum(
        double[] x,              // 输入信号
        double sampleRate,       // 采样率
        WindowType windowType,   // 窗函数类型
        double windowParam,      // 窗参数（double.NaN 默认）
        SpectrumAveraging averaging,  // 平均参数对象
        bool dBMag,              // true=幅度以 dB 输出
        bool unwrapPhase,        // 是否展开相位
        bool convertToDegree,    // true=相位转角度
        bool restartAveraging    // true=重启平均计算
    );
// 返回：(double[] mag, double[] phase, double df, int averagesCompleted, bool averagingDone)
```

##### AveragePowerSpectrum——平均功率谱

```csharp
var (spectrum, df, averagesCompleted, averagingDone) =
    SpectralEstimation.AveragePowerSpectrum(
        double[] x, double sampleRate,
        WindowType windowType, double windowParam,
        ExportMode exportMode, SpectrumAveraging averaging,
        bool dB, bool restartAveraging);
```

##### CrossPowerSpectralDensity——交叉功率谱密度

```csharp
// 简化调用（自动设置窗函数和参数）
var (pxy, phase, f) = SpectralEstimation.CrossPowerSpectralDensity(
    double[] x, double[] y, double sampleRate);
// pxy: Complex[] 交叉功率谱
// phase: double[] 交叉相位谱
// f: double[] 频率向量

// 完整参数
var (pxy, phase, f) = SpectralEstimation.CrossPowerSpectralDensity(
    double[] x, double[] y, double sampleRate,
    double[] window, double noverlap, double nfft, FrequencyRange freqRange);
```

##### MagnitudeSquaredCoherence——幅值平方相干性

```csharp
var (cxy, f) = SpectralEstimation.MagnitudeSquaredCoherence(
    double[] x, double[] y, double sampleRate,
    double[] window, double noverlap, double nfft, FrequencyRange freqRange);
// cxy: double[] 相干性 0~1，1=完全相关

// 简化调用
var (cxy, f) = SpectralEstimation.MagnitudeSquaredCoherence(x, y, sampleRate);
```

##### TransferFunction——传递函数估计

```csharp
var (txy, f) = SpectralEstimation.TransferFunction(
    double[] x, double[] y, double sampleRate,
    double[] window, double noverlap, double nfft, TFEstimator estimator);
// txy: Complex[] 传递函数
// estimator: H1 / H2

// 简化调用
var (txy, f) = SpectralEstimation.TransferFunction(x, y, sampleRate, window);
```

##### LombScargle——非均匀采样频谱

```csharp
var (psd, f) = SpectralEstimation.LombScargle(
    double[] x, double[] t, double maxFreq, int overSamplingFactor, ExportMode exportMode);
```

##### AverageFRFSpectrum——频率响应函数平均

```csharp
var (mag, phase, coherence, df, averagesCompleted, averagingDone) =
    SpectralEstimation.AverageFRFSpectrum(
        double[] x, double[] y, double sampleRate,
        WindowType windowType, double windowParam,
        FRFMode frfMode, SpectrumAveraging averaging,
        bool dBMag, bool unwrapPhase, bool convertToDegree, bool restartAveraging);
```

##### AverageCrossSpectrum——交叉谱平均

```csharp
var (mag, phase, df, averagesCompleted, averagingDone) =
    SpectralEstimation.AverageCrossSpectrum(
        double[] x, double[] y, double sampleRate,
        WindowType windowType, double windowParam,
        SpectrumAveraging averaging,
        bool dBMag, bool unwrapPhase, bool convertToDegree, bool restartAveraging);
```

#### SingleTone——单频/多频信号提取

```csharp
// ── 提取单频信号（最高幅度的频率成分）──
var (freq, amp, phase) = SingleTone.ExtractSingleTone(double[] x, double sampleRate);
// freq: 频率(Hz)  amp: 幅值  phase: 相位(度)

// 指定搜索范围
var (freq, amp, phase) = SingleTone.ExtractSingleTone(
    double[] x, double sampleRate, double approxFreq, double freqWidth);

// 多通道
var (freq, amp, phase) = SingleTone.ExtractSingleTone(
    double[,] x, double sampleRate, double approxFreq, double freqWidth);

// ── 提取多频信号（超过阈值的所有频率成分）──
var (freq, amp, phase) = SingleTone.ExtractMultiTone(
    double[] x, double sampleRate,
    double threshold,          // 幅值阈值（低于此值忽略）
    int maxNumTones,           // 最多提取频率数
    OutputSorting sorting      // IncreasingFrequency / DecreasingAmplitude
);
// freq[]: 频率数组  amp[]: 幅值数组  phase[]: 相位数组

// ── Hanning 窗 FFT（用于频谱可视化）──
Complex[] spectrum = SingleTone.FFTWithHanning(double[] x, bool correctDC, bool excludeNyquist);
Complex[] spectrum = SingleTone.FFTWithHanning(double[] x);
```

#### Distortion——失真分析

```csharp
// ── 信噪比 SNR（时域输入）──
var (snr, noisePower) = Distortion.SNR(double[] x, double sampleRate, int nHarm, bool omitAliases);
// snr: dB

// ── 信噪比 SNR（频域输入）──
var (snr, noisePower) = Distortion.SNR(double[] psd, double[] f, int nHarm, bool omitAliases);

// ── SINAD 信号与噪声加失真比 ──
var (sinad, totalDistortionPower) = Distortion.SINAD(double[] x, double sampleRate);

// ── SFDR 无杂散动态范围 ──
var (sfdr, spurPower, spurFrequency) = Distortion.SFDR(double[] x, double sampleRate, int minSpurDistance);

// ── THD 总谐波失真 ──
var (thd, harmonicPowers, harmonicFreqs) = Distortion.THD(double[] x, double sampleRate, int nHarm, bool omitAliases);

// ── TOI 三阶截断点（互调失真分析）──
var (oip3, fundPow, fundFreq, imodPow, imodFreq) = Distortion.TOI(double[] x, double sampleRate);
// 从功率谱密度计算
var (oip3, fundPow, fundFreq, imodPow, imodFreq) = Distortion.TOI(double[] psd, double[] f);

// ── HarmonicAnalyzer（谐波分析器）──
var (thd, sinad, harmonics) = Distortion.HarmonicAnalyzer(
    double[] x, double sampleRate, int highestHarmonic,
    double approxFreq, double freqWidth, bool stopSearchAtNyquist);
```

#### SpectralFeature——频谱特征测量

```csharp
// ── 频带功率 ──
double power = SpectralFeature.BandPower(
    double[] x, double sampleRate, double[] freqRange, PowerUnits units, double impedance);
// freqRange: null=全频段，或 new double[]{lowFreq, highFreq}
// units: V / V2 / W / dbm / dbW / dbV / dbmV / dBuV

// 从功率谱密度计算
double power = SpectralFeature.BandPower(double[] psd, double[] f, double[] freqRange, PowerUnits units, double impedance);

// ── 占用带宽（99% 功率占用的频段）──
var (bw, flowFreq, fhighFreq, power) = SpectralFeature.OccupiedBandwidth(double[] x, double sampleRate);

// ── -3dB 功率带宽 ──
var (bw, flowFreq, fhighFreq, power) = SpectralFeature.PowerBandwidth(double[] x, double sampleRate);

// ── 平均频率 ──
var (meanFreq, power) = SpectralFeature.MeanFreq(double[] x, double sampleRate, double[] frequencyRange);

// ── 中位数频率 ──
double medianFreq = SpectralFeature.MedianFreq(double[] x, double sampleRate);

// ── Buneman 频率估计 ──
double freq = SpectralFeature.BunemanFrequency(double[] x, double sampleRate);

// ── 峰值功率频率 ──
var (peakFreq, peakPower) = SpectralFeature.PeakPowerFrequency(
    double[] spectrum, double df, double peakFreq, int span, double enbw);
```

#### Hilbert——希尔伯特变换

```csharp
// 正变换：将实信号转为解析信号（复数，实部=原信号，虚部=Hilbert变换后信号）
Complex[] analyticSignal = Hilbert.Forward(double[] x);
Complex[] y = Hilbert.Forward(double[] x, double size);

// 逆变换
Complex[] y = Hilbert.Inverse(double[] x, double size);
```

#### DCT——离散余弦变换

```csharp
// 正变换
double[] Y = DCT.Forward(double[] x, int size, DCTType type);
// type: Type_I / Type_II / Type_III / Type_IV

// 逆变换
double[] y = DCT.Inverse(double[] X, int size, DCTType type);
```

#### Conversion——单位转换工具

```csharp
// dB 与线性值互转
double mag = Conversion.dBToMag(double dBValue);       // dB → 幅值
double[] mag = Conversion.dBToMag(double[] dBValues);
double dB = Conversion.MagTodB(double mag);             // 幅值 → dB
double[] dB = Conversion.MagTodB(double[] mag);
double[] dB = Conversion.MagTodB(Complex[] x);          // Complex 幅值 → dB
double dB = Conversion.PowerTodB(double power);         // 功率 → dB
double[] dB = Conversion.PowerTodB(double[] power);
double power = Conversion.dBToPower(double dBValue);    // dB → 功率
```

#### Windows——窗函数库

```csharp
// 通用接口：获取任意窗函数系数
double[] w = Windows.GetCoefficients(WindowType windowType, int width, double windowParam, bool symmetric, bool scaled);
double[] w = Windows.GetCoefficients(WindowType.Hanning, signalLength);

// 常用窗函数（直接调用）
double[] w = Windows.Hann(int width, bool symmetric);        // 汉宁窗（推荐）
double[] w = Windows.Hamming(int width, bool symmetric);     // 海明窗
double[] w = Windows.Blackman(int width, bool symmetric);    // 布莱克曼窗
double[] w = Windows.Kaiser(int width, double beta, bool symmetric);  // 凯泽窗
double[] w = Windows.FlatTop(int width, bool symmetric);     // 平顶窗（幅值精确测量）
double[] w = Windows.Chebyshev(int width, double sidelobeRatio, bool symmetric);
double[] w = Windows.Gaussian(int width, double sigma, bool symmetric);
double[] w = Windows.Tukey(int width, double cosineFraction, bool symmetric);
double[] w = Windows.BartlettHann(int width, bool symmetric);
double[] w = Windows.Welch(int width, bool symmetric);

// 缩放窗函数（用于频谱分析时的归一化）
double[] scaled = Windows.ScaledWindow(double[] x, WindowType windowType, double windowParam);

// 窗函数属性
var (coherentGain, enbw) = Windows.WindowProperties(double[] coeffs);
var (coherentGain, enbw, mainLobeWidth, sideLobeLevel) = Windows.WindowProperties(WindowType type, int width, double param);

// 等效噪声带宽
double enbw = Windows.ENBW(double[] coeffs, double sampleRate);
```

#### SpectrumAveraging——频谱平均参数类

```csharp
// 创建平均参数
var averaging = new SpectrumAveraging
{
    AveragingMode = AveragingMode.VectorAveraging,  // None / VectorAveraging / RMSAveraging / PeakHold
    WeightingMode = WeightingMode.Linear,            // Linear / Exponential
    NumberOfAverages = 10                            // 平均次数
};

// 构造函数
var avg = new SpectrumAveraging(AveragingMode.RMSAveraging, WeightingMode.Linear, 16);
```

#### ZoomAveraging——变焦平均参数类

```csharp
var zoomAvg = new ZoomAveraging
{
    AveragingMode = AveragingMode.VectorAveraging,
    WeightingType = WeightingMode.Exponential,
    LinearWeighting = LinearWeighting.MovingAverage,  // OneShot / AutoRestart / MovingAverage / Continuous
    NumberOfAverages = 20
};
```

#### ArrayUtility——数组工具（Spectrum 库内置）

> Utility.Spectrum 内置的 `ArrayUtility` 提供了丰富的数组运算扩展方法（可直接在数组上调用），包括：
> `Add`、`Substract`、`Multiply`、`Divide`、`Absolute`、`Negate`、`Power`、`Sqrt`、`Exp`、`Log`、
> `Sum`、`Mean`、`Max`、`Min`、`Sort`、`SubArray`、`BuildArray`、`Reverse`、`Diff`、`CumSum`、
> `ComplexToReal`、`ComplexToImaginary`、`RealToComplex`、`Conjugate`、`DotProduct`、`Mod` 等。

```csharp
// 示例（扩展方法调用风格）
double[] result = x.Add(y);           // 数组加法
double[] abs = complexArray.Absolute(); // Complex[] → 取模
double[] real = complexArray.ComplexToReal(); // 取实部
double[] imag = complexArray.ComplexToImaginary(); // 取虚部
Complex[] cx = realArray.RealToComplex(); // double[] → Complex[]
double[,] merged = ArrayUtility.BuildArray(x, y, Direction.Column); // 合并为二维
double[] sub = x.SubArray(startIndex, count); // 子数组
int mod = ArrayUtility.Mod(x, y);    // 模运算
```

#### 主要枚举类型

| 枚举 | 值 | 说明 |
|------|------|------|
| `WindowType` | `Hanning` / `Hamming` / `Blackman` / `BlackmanHarris` / `Kaiser` / `FlatTop` / `Gaussian` / `Chebyshev` / `Cosine` / `Tukey` / `Welch` / ... (共24种) | 窗函数类型 |
| `FrequencyRange` | `OneSided` / `TwoSided` / `Centered` | 频率范围 |
| `ExportMode` | `PowerSpectrum` / `PowerSpectrumDensity` | 输出模式 |
| `TraceMode` | `Average` / `MaxHold` / `MinHold` | 迹线模式 |
| `AveragingMode` | `None` / `VectorAveraging` / `RMSAveraging` / `PeakHold` | 平均模式 |
| `WeightingMode` | `Linear` / `Exponential` | 加权模式 |
| `OutputSorting` | `IncreasingFrequency` / `DecreasingAmplitude` | 多频输出排序 |
| `PowerUnits` | `V` / `V2` / `W` / `dbm` / `dbW` / `dbV` / `dbmV` / `dBuV` | 功率单位 |
| `TFEstimator` | `H1` / `H2` | 传递函数估计器 |
| `FRFMode` | `H1` / `H2` / `H3` | 频率响应函数模式 |
| `DCTType` | `Type_I` / `Type_II` / `Type_III` / `Type_IV` | DCT 类型 |
| `Direction` | `Row` / `Column` | 数组方向 |

#### ⚠️ DSP.Utility.Spectrum 高频易错点

| 错误 | 原因 | 正确做法 |
|------|------|--------|
| 混淆 `DSP.Fundamental.Spectrum` 和 `DSP.Utility.Spectrum` | 不同 DLL、不同命名空间、不同 API 风格 | Fundamental 用 `ref`/`out`，Utility.Spectrum 用 ValueTuple 返回 |
| `WindowType` 命名空间冲突 | 两个库都有 `WindowType` 枚举 | 使用全限定名 `SeeSharpTools.JY.DSP.Utility.Spectrum.WindowType` |
| `Periodogram` 窗参数传 null | 需要 `double[]` 窗系数 | 用 `Windows.GetCoefficients(...)` 生成 |
| `Pwelch` 分段长度 > 信号长度 | 分段长度不能超过输入信号长度 | `segLength <= x.Length` |
| 忘记设置 `restartAveraging=true` | 平均频谱会持续累积 | 首次调用或切换信号时传 `true` |
| `ExtractMultiTone` 阈值过高 | 频率成分被忽略 | 根据信号幅度合理设置阈值 |
| `Distortion.TOI` 输入非双音信号 | TOI 仅适用于双音信号 | 确保输入包含两个频率成分 |
| `Conversion.MagTodB` 传入功率值 | 应用 `PowerTodB` | 幅值用 `MagTodB`（20log10），功率用 `PowerTodB`（10log10）|

---

### SeeSharpTools.JY.DSP.Utility.Filter1D（完整滤波器设计库）

> **DLL**：`SeeSharpTools.JY.DSP.Utility.Filter1D.dll`（需单独添加引用，与 `SeeSharpTools.JY.DSP.Utility.dll` 独立）
> **命名空间**：`SeeSharpTools.JY.DSP.Utility.Filter1D`
> **特点**：自带 IIR/FIR 设计、执行、分析全套功能，不需 MathNet

#### IIRDesign（静态密封 sealed static 类）

```csharp
// ―― Butter（Butterworth，通带最平坦）――
// n: 阶数  wn: 截止频率(Hz)[]
//   低通/高通: wn.Length=1
//   带通/带阻: wn.Length=2, wn[0]=低截, wn[1]=高截
// 返回 (double[] b, double[] a) => ValueTuple<double[],double[]>
static ValueTuple<double[], double[]> Butter(
    int n, double[] wn, IIRBandType bandType, double sampleRate);

// ―― Cheby1（Chebyshev I，通带等纹波）――
// wp: 通带截止频率(Hz)[]   rp: 通带纹波(dB)
static ValueTuple<double[], double[]> Cheby1(
    int n, double[] wp, double rp, IIRBandType bandType, double sampleRate);

// ―― Cheby2（Chebyshev II，阻带等纹波）――
// ws: 阻带截止频率(Hz)[]   rs: 阻带衰减(dB)
static ValueTuple<double[], double[]> Cheby2(
    int n, double[] ws, double rs, IIRBandType bandType, double sampleRate);

// ―― Ellip（Elliptic，最小阶数设计）――
// wp: 通带截止频率(Hz)[]   rp: 通带纹波(dB)   rs: 阻带衰减(dB)
static ValueTuple<double[], double[]> Ellip(
    int n, double[] wp, double rp, double rs, IIRBandType bandType, double sampleRate);

// ―― TransferFunction（传递函数，自动估阶）――
// wp: 通带截止(Hz)[]  ws: 阻带截止(Hz)[]  rp/rs: 纹波/衰减(dB)
static ValueTuple<double[], double[]> TransferFunction(
    double[] wp, double[] ws, double rp, double rs,
    IIRDesignMethod designMethod, double sampleRate);
static ValueTuple<double[], double[]> TransferFunction(
    int n, double[] wn, double rp, double rs,
    IIRBandType bandType, IIRDesignMethod designMethod, double sampleRate);

// ―― SOSDesign（SOS 二阶节，高阶推荐）――
static double[] SOSDesign(
    double[] wp, double[] ws, double rp, double rs,
    IIRDesignMethod designMethod, double sampleRate);
static double[] SOSDesign(
    int n, double[] wn, double rp, double rs,
    IIRBandType bandType, IIRDesignMethod designMethod, double sampleRate);

// ―― IIRNotch（陷波/降噪）――
// w0: 中心频率(Hz)   bw: -3dB 带宽(Hz)
static ValueTuple<double[], double[]> IIRNotch(double w0, double bw, double sampleRate);

// ―― IIRPeak（峰值/调谐）――
static ValueTuple<double[], double[]> IIRPeak(double w0, double bw, double sampleRate);

// ―― 辅助方法（模型转换）――
static ValueTuple<double[], double> tf2sos(double[] b, double[] a);          // b,a → sos
static ValueTuple<double[], double[]> sos2tf(double[] sos);                  // sos → b,a
static ValueTuple<Complex[], Complex[], double> tf2zpk(double[] b, double[] a); // 零极点表示
```

#### IIRFiltering（静态密封 sealed static 类）

```csharp
// ―― 基本 IIR 滤波（整段，无状态）――
static double[] IIRFilter(double[] b, double[] a, double[] x);

// ―― 带状态 IIR 滤波（ValueTuple 返回）――
// zi: 输入状态向量  返回 (y, zf): 输出 + 末尾状态
static ValueTuple<double[], double[]> IIRFilter(
    double[] b, double[] a, double[] x, double[] z);
static ValueTuple<double[], double[]> IIRFilter(
    double[] b, double[] a, double[] x, double[] zi, bool resetFilter);

// ―― 带状态 IIR 滤波（ref 写回）――
static double[] IIRFilter(
    double[] b, double[] a, double[] x, ref double[] z, bool resetFilter);

// ―― SOS 滤波（二阶节，高阶滤波器推荐）――
static double[] SOSFilter(double[] sos, double[] x);
static ValueTuple<double[], double[]> SOSFilter(double[] sos, double[] x, double[] zi);

// ―― SOS 初始条件计算（流式滤波第一帧前调用）――
static double[] SOSFilter_zi(double[] sos);

// ―― 零相位滤波（双向，无相位延迟，仅局离线处理）――
// padType: None/Odd/Even/Constant  padLength: 建议 ≥ 阶数*3
static double[] ZeroPhaseFilter(
    double[] sos, double[] x, PadType padType, int padLength);
static double[] ZeroPhaseFilter(
    double[] b, double[] a, double[] x, PadType padType, int padLength);

// ―― 移动平均 / 指数平滑――
// type: MovingAverage/Exponential
// halfWidth: 平均半窗(MovingAverage)—窗口=2N+1
// timeConstant: 时间常数(s)（Exponential）
static double[] SmoothingFilter(
    double[] x, double sampleRate, SmoothingType type,
    int halfWidth, FilterShape shape, double timeConstant);

// ―― IIR 初始条件（稳态起始）――
static double[] IIRFilter_IC(double[] b, double[] a);
static double[] IIRFilter_IC(double[] b, double[] a, double[] ypast, double[] xpast);
```

#### IIRAnalysis（静态密封 sealed static 类）

```csharp
// Bode 图：返回 (magnitude[], phase[], freq[])
// n: 频率点数
static ValueTuple<double[], double[], double[]> Bode(
    double[] b, double[] a, double sampleRate, int n);

// 频率响应 Complex[]
static Complex[] FrequencyResponse(
    double[] b, double[] a, double[] f, double sampleRate);
static ValueTuple<Complex[], double[]> FrequencyResponse(
    double[] b, double[] a, double sampleRate, int n, bool whole);

// 群延迟
static ValueTuple<double[], double[]> GroupDelay(
    double[] b, double[] a, double sampleRate, int n, bool whole);

// 冲激响应 / 阶跃响应
static ValueTuple<double[], double[]> ImpulseResponse(
    double[] b, double[] a, double sampleRate, double n);
static ValueTuple<double[], double[]> StepResponse(
    double[] b, double[] a, double sampleRate);

// 零相位响应
static ValueTuple<double[], double[], double[]> ZeroPhaseResponse(
    double[] b, double[] a, double sampleRate, int n, bool whole);
```

#### FIRDesign（静态密封 sealed static 类）

```csharp
// ―― 窗函数法（最常用）――
// n: 滤波器阶数  wn: 截止频率(Hz)[]
// type: WindowType枚举  windowParam: Kaiser时为beta，其他为0
// bandType: FIRBandType枚举  scale: true=归一化增益
static double[] Window(
    int n, double[] wn, WindowType type, double windowParam,
    FIRBandType bandType, bool scale, double sampleRate);

// ―― Parks-McClellan（最优等纹波）――
// bands: 频带边界(Hz)对数组  amplitudes: 各频带目标增益
// weight: 各频带权重  type: Bandpass/Differentiator/Hilbert
// griddensity: 网格密度，默认16
static double[] ParksMcClellan(
    int order, double[] bands, double[] amplitudes,
    double[] weight, PMFilterType type, double sampleRate, int griddensity);

// ―― 最小二乘设计――
static double[] LeastSquare(
    int n, double[] bands, double[] amplitudes, double[] weight, double sampleRate);

// ―― 频率采样设计――
static double[] FrequencySampling(
    int n, double[] bands, double[] amplitudes,
    WindowType windowType, double windowParam, double sampleRate);

// ―― Kaiser 阶贯估阶――
// 根据规格自动估算阶数和 Kaiser 窗 beta
// frequency[]: 频带边界(Hz)  amplitude[]: 各频带增益  ripple[]: 纹波
static ValueTuple<int, double[], double, FIRBandType> KaiseOrd(
    double[] frequency, double[] amplitude, double[] ripple,
    double sampleRate, RippleScale scale);
```

#### FIRFiltering（静态密封 sealed static 类）

```csharp
// ―― 基本 FIR 滤波 ――
static double[] FIRFilter(double[] b, double[] x);

// ―― 带状态 FIR 滤波（ValueTuple 返回）――
// z: 状态向量，长度 = b.Length - 1
static ValueTuple<double[], double[]> FIRFilter(double[] b, double[] x, double[] z);
static ValueTuple<double[], double[]> FIRFilter(
    double[] b, double[] x, double[] zi, bool resetFilter);

// ―― 带状态 FIR 滤波（ref 写回）――
static double[] FIRFilter(
    double[] b, double[] x, ref double[] z, bool resetFilter);

// ―― FFT 快速滤波（长信号更快）――
static double[] FFTFilter(double[] b, double[] x);

// ―― Savitzky-Golay 多项式平滑滤波――
// order: 多项式阶数  frameLength: 窗口长度（奇数）  weights: null 则均匀加权
static double[] SgolayFilter(double[] x, int order, int frameLength, double[] weights);
```

#### FIRAnalysis（静态密封 sealed static 类）

```csharp
static ValueTuple<double[], double[], double[]> Bode(
    double[] b, double sampleRate, int n);
static Complex[] FrequencyResponse(double[] b, double[] f, double sampleRate);
static ValueTuple<double[], double[]> GroupDelay(
    double[] b, double sampleRate, int n, bool whole);
static ValueTuple<double[], double[]> ImpulseResponse(
    double[] b, double sampleRate, double n);
static ValueTuple<double[], double[]> StepResponse(double[] b, double sampleRate);
static ValueTuple<double[], double[]> PhaseResponse(
    double[] b, double sampleRate, int n, bool whole);
```

#### FilterExploration（实例类，静态方法）

```csharp
// 滤波器阶数、稳定性、相位特性
static int   FilterOrder(double[] b, double[] a);
static bool  IsFIR(double[] a);                     // a=[1.0] 即 FIR
static bool  IsStable(double[] b, double[] a);      // 所有极点在单位圆内
static bool  IsLinearPhase(double[] b, double[] a, double tol);
static bool  IsMinPhase(double[] b, double[] a);
static bool  IsMaxPhase(double[] b, double[] a);
```

#### SpecialFiltering（静态密封 sealed static 类）

```csharp
// ―― 微分（追分滤波）――
// fs: 采样率(Hz)  bw: BandwidthOption.NarrowBand/WideBand
// initVal: ref 状态（首次传 null 自动初始化）
static double[] Differentiate(
    double[] x, double fs, BandwidthOption bw, ref double[] initVal);

// ―― 积分（保随滤波 + 高通防漂移）――
// HPF6dBFreq: 高通截止频率(Hz)，防止直流积分漂移
static double[] Integrate(
    double[] x, double fs, double HPF6dBFreq, BandwidthOption bw, ref double[] initVal);
```

#### Multirate（静态密封 sealed static 类）

```csharp
// ―― 抽取（降采样 + 抗混叠滤波）――
// deciFactor: 抽取因子  filterType: IIR/FIR  order: 滤波器阶数
static double[] Decimate(
    double[] x, int deciFactor, AntialiasingFilterType filterType, int order);

// ―― 插值（升采样 + 低通滤波）――
static double[] Interpolate(double[] x, int interpFactor);

// ―― 有理数比率重采样 p/q ――
// p: 升采样因子  q: 降采样因子  n: 滤波器阶数  beta: Kaiser beta
static double[] Resample(double[] x, int p, int q, int n, double beta);

// ―― 简单降采样（不带滤波）――
static double[] Downsample(double[] x, int downsamplingFactor, int offset);

// ―― 简单升采样（插零）――
static double[] Upsample(double[] x, int upsamplingFactor, int offset);

// ―― Up-FIR-Down（升采样-FIR-降采样一体）――
static double[] UpFIRDown(double[] x, double[] h, int p, int q);

// ―― 有理数近似――
static ValueTuple<int, int> RationalFraction(double x, double tol);
```

#### Windows（窗函数，静态密封 sealed static 类）

```csharp
// sym=true: 对称窗（滤波器设计用）   sym=false: 非对称（频谱分析用）
static double[] Hamming(int width, bool sym);
static double[] Hann(int width, bool sym);
static double[] Blackman(int width, bool sym);
static double[] Kaiser(int width, double beta, bool sym);
static double[] FlatTop(int width, bool sym);
static double[] BartlettHann(int width, bool sym);
static double[] Chebyshev(int width, double atten, bool sym);
static double[] GetWindow(WindowType type, int width, double windowParam, bool symmetric);
```

#### 主要枚举类型

| 枚举 | 值 | 说明 |
|------|------|------|
| `IIRBandType` | `Lowpass/Highpass/Bandstop/Bandpass` | IIR 频带类型 |
| `IIRDesignMethod` | `Butterworth/ChebyshevI/ChebyshevII/Elliptic/Bessel` | IIR 设计方法 |
| `FIRBandType` | `Lowpass/Highpass/Bandstop/Bandpass/DC_pass/DC_stop` | FIR 频带类型 |
| `WindowType` | `Hamming/Hann/Kaiser/Blackman/...` | 窗函数类型 |
| `PadType` | `None/Odd/Even/Constant` | 零相位滤波边界延拓 |
| `SmoothingType` | `MovingAverage/Exponential` | 平滑滤波类型 |
| `FilterShape` | `Rectangular/Exponential` | 滤波形状 |
| `AntialiasingFilterType` | `IIR/FIR` | Decimate 抗混叠滤波器 |
| `PMFilterType` | `Bandpass/Differentiator/Hilbert` | Parks-McClellan 类型 |
| `BandwidthOption` | `NarrowBand/WideBand` | 微分/积分带宽选项 |
| `FilterType` | `None/IIR/FIRWindow/Classic` | ExpressFilter 用 |

---

## 三、数学模块（SeeSharpTools.JY.Mathematics）

### ArrayArithmetic

```csharp
// 基础运算（有 out 参数版本）
ArrayArithmetic.Add(a, b, ref result);
ArrayArithmetic.Subtract(a, b, ref result);
ArrayArithmetic.Multiply(a, b, ref result);
ArrayArithmetic.Divide(a, b, ref result);

// 原地运算（写回第一个数组）
ArrayArithmetic.Add(srcDest, src2);

// 标量运算
ArrayArithmetic.Add(a, scalar, ref result);
ArrayArithmetic.Multiply(a, scalar, ref result);

// 数学函数
ArrayArithmetic.Absolute(a, ref result);
ArrayArithmetic.ACos(a, ref result);
ArrayArithmetic.Sum(a);          // 返回 double
ArrayArithmetic.Initialize(ref a, value);
```

### ProviderEngine 枚举

| 值 | 说明 |
|----|-----|
| `ProviderEngine.CSharp` | 纯托管实现，无额外依赖 |
| `ProviderEngine.IntelMKL` | Intel MKL 加速，需 `libiomp5md.dll` |

设置方式：`Engine.Provider = ProviderEngine.CSharp;`

### ArrayOperation

```csharp
// 字符串数组转数值数组
ArrayOperation.ConvertTo(string[] src, ref double[] dst);
ArrayOperation.ConvertTo(string[] src, ref float[] dst);

// GetSubSet — 从二维数组提取一行或一列（v2.0.3 修复按列获取的 Bug）
// 签名（实际参数名以API为准，示意如下）：
double[] ArrayOperation.GetSubSet(double[,] src, int index, MajorOrder order);
// order = MajorOrder.Row    → 提取第 index 行
// order = MajorOrder.Column → 提取第 index 列（v2.0.3 前按列有 Bug）
```

---

## 四、数组工具（SeeSharpTools.JY.ArrayUtility）

### ArrayManipulation

```csharp
// 连接多个一维数组 → 2D数组
double[,] result = ArrayManipulation.BuildArray<double>(double[][] arrays);

// 连接两个2D数组
double[,] result = ArrayManipulation.BuildArray<double>(src1, src2, MajorOrder.Row);

// 插入元素到1D数组
ArrayManipulation.Insert_1D_Array(src, element, index, out dst);

// 插入行/列到2D数组
ArrayManipulation.Insert_2D_Array(src, insertArray, insertIndex, out dst, MajorOrder.Row);

// 连接两个1D数组
ArrayManipulation.Connect_1D_Array(src1, src2, out dst);

// 连接两个1D数组成2列2D数组
ArrayManipulation.Connected_2D_Array(src1, src2, out dst2D);
```

### ArrayCalculation

```csharp
// 数组加法（配合 DSP 使用）
ArrayCalculation.Add(a, b, ref result);
```

---

## 五、文件模块（SeeSharpTools.JY.File）

### CsvHandler

```csharp
// 写 CSV（自动弹出另存对话框）
CsvHandler.WriteData(string[,] data);
CsvHandler.WriteData(double[,] data);

// 读 CSV（从 startRow 行、startColumn 列起）
T[,] data = CsvHandler.Read<T>(int startRow, int startColumn);

// 读 CSV（指定多列）
T[,] data = CsvHandler.Read<T>(int startRow, int[] columns);
```

### BinHandler

```csharp
// 写二进制文件
BinHandler.WriteData<T>(T[] data, string filePath);
BinHandler.WriteData<T>(T[,] data, string filePath);

// 读二进制文件
T[] data = BinHandler.ReadData<T>(string filePath);
T[,] data2D = BinHandler.ReadData2D<T>(string filePath);
```

### AnalogWaveformFile（.tdm / .tdms 格式）

```csharp
// 写波形文件
AnalogWaveformFile.Write(double[,] waveformData, double sampleRate, string filePath);

// 读波形文件
double[,] data = AnalogWaveformFile.Read(string filePath, out double sampleRate);
```

---

## 六、数据库模块（SeeSharpTools.JY.Database）

### DbOperation

```csharp
// 初始化
var db = new DbOperation(string connectionString, DbProviderType providerType);

// 增删改
int rows = db.ExecuteNonQuery(string sql, IList<DbParameter> parameters);
int rows = db.ExecuteNonQuery(string sql, IList<DbParameter> parameters, CommandType commandType);

// 查询返回 DataReader
IDataReader reader = db.ExecuteReader(string sql, IList<DbParameter> parameters);

// 查询返回 DataTable
DataTable dt = db.ExecuteDataTable(string sql, IList<DbParameter> parameters);

// 查询返回标量
object scalar = db.ExecuteScalar(string sql, IList<DbParameter> parameters);
```

### DbProviderType 枚举

`SQLite`、`SQLServer`、`MySQL`、`OleDb`、`Oracle`

---

## 七、TCP 通信（SeeSharpTools.JY.TCP）

### EasyTCPServer

```csharp
var server = new EasyTCPServer();
server.Start(int port);
server.Stop();

server.DataReceived += (sender, EasyTCPEventArgs e) => {
    byte[] data = e.Data;
    string clientId = e.ClientID;
};
server.ClientConnected += handler;
server.ClientDisconnected += handler;

server.Send(string clientId, byte[] data);
server.SendToAll(byte[] data);
```

### EasyTCPClient

```csharp
var client = new EasyTCPClient();
client.Connect(string ip, int port);
client.Disconnect();

client.DataReceived += (sender, EasyTCPEventArgs e) => {
    byte[] data = e.Data;
};
client.Send(byte[] data);
```

### CircularBuffer（泛型循环缓冲）

```csharp
var buffer = new CircularBuffer<double>(bufferSize);
buffer.Enqueue(double value);
buffer.Enqueue(double[] values);
buffer.Dequeue(ref double value);
buffer.Dequeue(ref double[] buffer, int count);  // 返回实际读取数
buffer.Clear();
int count = buffer.NumOfElement;
```

---

## 八、传感器换算（SeeSharpTools.JY.Sensors）

### RTD（电阻温度传感器）

```csharp
// 电阻数组 → 温度数组（单位℃）
double[] temp = RTD.Convert(double[] resValues, RTDType rtdType);

// RTDType: PT100 / PT200 / PT500 / PT1000 / ...
```

### LoadCell（荷重传感器）

```csharp
// 电压 → 荷重
double[] load = LoadCell.Convert(double[] rawValues, double sensitivity, double maxload, double excitationVoltage);

// 单点版本
double load = LoadCell.Convert(double rawValue, double sensitivity, double maxload, double excitationVoltage);
```

### Thermocouple（热电偶）

```csharp
double[] temp = Thermocouple.Convert(double[] voltValues, ThermocoupleType type);
```

### CustomScaling（自定义换算）

```csharp
double[] result = CustomScaling.Convert(double[] voltValues, Func<double, double> function);
double result = CustomScaling.Convert(double voltValue, Func<double, double> function);

// 示例：线性换算 y = 100*x + 20
double[] temp = CustomScaling.Convert(voltArray, v => 100 * v + 20);
```

### Interpolation（分段线性插值）

```csharp
// 从查找表按Y反求X
double x = Interpolation.LinearInterpolation1D(double[] table, double xIncrement, double xOffset, double y);
double x = Interpolation.LinearInterpolation1D(int[] table, double xIncrement, double xOffset, int y);
```

---

## 九、日志报告（SeeSharpTools.JY.Report）

### Logger

```csharp
// 初始化（单文件）
Logger.Initialize(string filePath);

// 初始化（自定义配置）
var config = new LogConfig {
    FilePath = "app.log",
    MaxFileSize = 10 * 1024 * 1024,  // 10MB
    MaxFileCount = 5,
    IsThreadSafe = true
};
Logger.Initialize(config);

// 日志级别控制
Logger.LogLevel = LogLevel.Info;   // 低于 Info 的不记录
Logger.Enabled = true;

// 记录日志
Logger.Trace("消息 {0}", arg);
Logger.Debug("消息");
Logger.Info("采样率：{0} Hz", sampleRate);
Logger.Warn("警告信息");
Logger.Error("错误：{0}", msg);
Logger.Fatal("严重错误");

// 带异常
Logger.Error(exception, "发生异常：{0}", ex.Message);
Logger.Print(string message, LogLevel level, params object[] args);
```

### LogLevel 枚举（从低到高）

`Trace` → `Debug` → `Info` → `Warn` → `Error` → `Fatal`

---

## 十、3D 图形（SeeSharpTools.JY.Graph3D）

```csharp
using SeeSharpTools.JY.Graph3D;

// 在 WinForm 中添加 Graph3D 控件后
graph3D1.Plot(double[,] data);
```

---

## 十一、本地化（SeeSharpTools.JY.Localization）

```csharp
using SeeSharpTools.JY.Localization;

// 支持语言切换：zh-CN / en-US
JYLocalization.SetLanguage("zh-CN");
```

---

## 十二、依赖库说明

| DLL | 用途 |
|-----|-----|
| `MathNet.Numerics.dll` | 数值计算基础库（DSP 依赖） |
| `MathNet.Filtering.dll` | FIR/IIR 滤波器设计 |
| `ILNumerics.Net.dll` | 3D 绘图引擎 |
| `OpenTK.dll` | OpenGL 3D 渲染支持 |
| `libiomp5md.dll` | Intel MKL 并行运行时 |
| `System.Windows.Forms.Ribbon.dll` | Ribbon 控件 |
| `WeifenLuo.WinFormsUI.Docking.dll` | Docking 控件 |
| `DSPMatlab.dll` | Matlab 引擎 DSP 扩展 |
| `AudioLibrary.dll` | 音频采集底层 |

---

## 十三、SeeSharpTools.JY.TEDS（v2.0.3 新增）

> **TEDS（Transducer Electronic Data Sheet）** 是 IEEE 1451.4 标准定义的传感器电子数据表，用于存储传感器制造商、型号、版本、序列号及标定数据。

### 主要类型

| 类型 | 说明 |
|------|------|
| `BasicTEDS` | 基本 TEDS 信息：制造商ID、型号、版本号、序列号 |
| `Manufacture` | 制造商枚举（IEEE 注册 MID，如 `BruelKjaer`） |
| `IEEETemplate` | IEEE 1451.4 标准模板枚举（TID） |
| `CalibrationTEDS.IEEECalTableTEDS` | IEEE 校准表模板（TID=40），存储校准点偏差 |
| `CalibrationTEDS.IEEECalCurveTEDS` | IEEE 校准曲线模板（TID=41），分段多项式 |
| `CalibrationTEDS.IEEEFreqRespTableTEDS` | IEEE 频响表模板（TID=42） |
| `TEDSVersion` | TEDS 标准版本（`v0p9` 等） |
| `TEDSPropertyValue` | TEDS 属性值容器 |

```csharp
using SeeSharpTools.JY.TEDS;

// 读取传感器基本信息
BasicTEDS basic = ...; // 从 TEDS 接口获取
string model    = basic.ModelNumber;
string serial   = basic.SerialNumber;

// 使用制造商枚举
Manufacture mfr = Manufacture.BruelKjaer;

// 使用 IEEE 模板枚举
IEEETemplate tmpl = IEEETemplate.AccelerometerV0p9;
```

**所需 DLL**：`SeeSharpTools.JY.TEDS.dll`（v2.0.3 首次发布）
