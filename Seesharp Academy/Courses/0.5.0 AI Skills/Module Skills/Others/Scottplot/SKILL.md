## 📊 ScottPlot 4.1 专家 Skill 文档

### 🎯 一、角色定位与核心认知

你好！我是你的 **ScottPlot 4.1 专属技术专家**。

我专注于 **WinForms** 和 **WPF** 平台下的数据可视化开发，**精通 4.1.32 与 4.1.72 版本**（坚决不混淆 5.x 的 API）。我的核心目标是帮你用最优雅、最高效的代码，解决高频数据刷新的性能痛点，规避旧版本的内存陷阱。

---

### 🖥️ 二、核心前置知识：WinForms vs WPF

在 4.1 版本中，WinForms 和 WPF 的底层渲染机制不同，因此命名空间和控件用法有明确区分。**用错命名空间是导致很多奇葩报错的元凶！**

| 平台 | 命名空间 | 控件名 | 刷新方法 |
|------|----------|--------|----------|
| WinForms | `ScottPlot.FormsPlot` | `formsPlot1` | `.Refresh()` |
| WPF | `ScottPlot.WpfPlot` | `wpfPlot1` | `.Render()` |

> 💡 **专家提示**：所有对图表的修改（增删改数据、坐标轴），本质上都是在操作控件的 `.Plot` 属性。修改完后，务必调用 `.Refresh()` 或 `.Render()` 才能生效。

**NuGet 安装：**
- WinForms：`Install-Package ScottPlot.WinForms`
- WPF：`Install-Package ScottPlot.WPF`

**WPF XAML 引用：**
```xml
<Window xmlns:wpf="clr-namespace:ScottPlot.WPF;assembly=ScottPlot.WPF">
    <Grid>
        <wpf:WpfPlot x:Name="wpfPlot1"/>
    </Grid>
</Window>
```

---

### 🌊 三、Signal 波形绘制与实时更新

`Signal` 是 ScottPlot 4.1 中**性能最强**的线图，专为大容量、等间距的实时波形数据设计。

#### 1. WinForms 实战：正弦波动态刷新

```csharp
using ScottPlot;
using System.Windows.Forms;

public partial class MainForm : Form
{
    private double[] _data = new double[1000];
    private ScottPlot.Plottable.SignalPlot _signal;

    public MainForm()
    {
        InitializeComponent();
        InitPlot();
        var timer = new System.Windows.Forms.Timer() { Interval = 50 };
        timer.Tick += (s, e) => UpdateData();
        timer.Start();
    }

    private void InitPlot()
    {
        formsPlot1.Plot.Title("WinForms 实时波形");
        formsPlot1.Plot.XLabel("采样点");
        formsPlot1.Plot.YLabel("幅值");

        _signal = formsPlot1.Plot.AddSignal(_data, sampleRate: 1000);
        _signal.Color = System.Drawing.Color.DeepSkyBlue;
        _signal.LineWidth = 2;
    }

    private void UpdateData()
    {
        for (int i = 0; i < _data.Length; i++)
        {
            _data[i] = Math.Sin(i * 0.1 + DateTime.Now.Millisecond * 0.01) * 10;
        }
        formsPlot1.Plot.AxisAutoX(margin: 0.1);
        formsPlot1.Refresh();
    }
}
```

#### 2. WPF 实战：跨线程数据更新与 UI 刷新

WPF 开发中经常遇到**跨线程更新数据**的需求。直接操作 UI 会报异常，必须借助 `Dispatcher`。

```csharp
using ScottPlot;
using System.Threading.Tasks;
using System.Windows;

public partial class MainWindow : Window
{
    private double[] _data = new double[5000];
    private ScottPlot.Plottable.SignalPlot _signal;

    public MainWindow()
    {
        InitializeComponent();
        InitWpfPlot();
        StartDataAcquisitionThread();
    }

    private void InitWpfPlot()
    {
        wpfPlot1.Plot.Title("WPF 高频数据采集");
        wpfPlot1.Plot.YLabel("电压 (V)");

        _signal = wpfPlot1.Plot.AddSignal(_data, sampleRate: 48000);
        _signal.Color = System.Drawing.Color.OrangeRed;
    }

    private void StartDataAcquisitionThread()
    {
        Task.Run(async () =>
        {
            var rand = new Random();
            while (true)
            {
                await Task.Delay(20);
                for (int i = 0; i < _data.Length; i++)
                {
                    _data[i] = rand.NextDouble() * 5 - 2.5;
                }

                // WPF 跨线程访问 UI 控件必须使用 Dispatcher
                wpfPlot1.Dispatcher.Invoke(() =>
                {
                    wpfPlot1.Plot.AxisAutoY();
                    wpfPlot1.Render();
                });
            }
        });
    }
}
```

---

### 📈 四、散点图与柱状图

#### 1. 散点图 (Scatter)

适合展示离散数据点、XY 坐标关系。

```csharp
double[] xs = DataGen.Consecutive(100);
double[] ys = DataGen.Sin(100);

var scatter = myPlot.Plot.AddScatter(xs, ys);
scatter.Color = Color.ForestGreen;
scatter.MarkerSize = 5;
scatter.MarkerShape = MarkerShape.filledCircle;
scatter.LineStyle = LineStyle.Dash; // 虚线连接
```

#### 2. 柱状图 (Bar)

适合分类数据统计对比。

```csharp
double[] positions = { 1, 2, 3, 4 };
double[] values = { 10, 23, 15, 30 };

var bar = myPlot.Plot.AddBar(values, positions);
bar.FillColor = Color.SteelBlue;
bar.BorderColor = Color.Black;
bar.BarWidth = 0.8;

string[] labels = { "Q1", "Q2", "Q3", "Q4" };
myPlot.Plot.XTicks(positions, labels);
```

---

### 📡 五、频谱图显示

频谱图是工业测量和信号处理中最常见的可视化需求。ScottPlot 4.1 对此有很好的支持。

#### 1. 基础频谱绘制

假设已获得频谱幅值数组 `spectrumData` 和频谱分辨率 `frequencyResolution`（Hz/bin）：

```csharp
using ScottPlot;
using System.Linq;

private void PlotSpectrum(double[] spectrumData, double frequencyResolution)
{
    // 使用 AddSignal，sampleRate 设为 1/频率分辨率，X轴自动映射为频率
    var signalPlot = myPlot.Plot.AddSignal(
        ys: spectrumData,
        sampleRate: 1.0 / frequencyResolution
    );
    signalPlot.Color = System.Drawing.Color.DeepSkyBlue;
    signalPlot.LineWidth = 1;

    myPlot.Plot.Title("Frequency Spectrum");
    myPlot.Plot.XLabel("Frequency (Hz)");
    myPlot.Plot.YLabel("Amplitude");
    myPlot.Plot.AxisAuto();
    myPlot.Refresh();
}
```

#### 2. 使用 Scatter 绘制（更灵活控制 X 轴）

当需要精确控制频率轴标签或只显示部分频段时：

```csharp
private void PlotSpectrumScatter(double[] spectrumData, double frequencyResolution)
{
    double[] frequencies = Enumerable.Range(0, spectrumData.Length)
                                     .Select(i => i * frequencyResolution)
                                     .ToArray();

    var scatter = myPlot.Plot.AddScatter(frequencies, spectrumData);
    scatter.MarkerSize = 0; // 只显示线条，不显示标记点
    scatter.Color = System.Drawing.Color.DodgerBlue;

    myPlot.Plot.Title("Frequency Spectrum");
    myPlot.Plot.XLabel("Frequency (Hz)");
    myPlot.Plot.YLabel("Amplitude");
    myPlot.Plot.AxisAuto();
    myPlot.Refresh();
}
```

#### 3. 实时频谱动态更新

对于实时 FFT 频谱显示，关键是**只初始化一次 Plottable 对象**，后续只更新数组数据：

```csharp
private double[] _spectrumBuffer;
private ScottPlot.Plottable.SignalPlot _spectrumPlot;

private void InitSpectrum(int fftSize, double frequencyResolution)
{
    _spectrumBuffer = new double[fftSize / 2];
    _spectrumPlot = myPlot.Plot.AddSignal(
        _spectrumBuffer,
        sampleRate: 1.0 / frequencyResolution
    );
    _spectrumPlot.Color = System.Drawing.Color.Lime;
    myPlot.Plot.XLabel("Frequency (Hz)");
    myPlot.Plot.YLabel("Magnitude (dB)");
}

private void UpdateSpectrum(double[] newSpectrumData)
{
    // 直接覆写缓冲区，不要重新 AddSignal
    Array.Copy(newSpectrumData, _spectrumBuffer, _spectrumBuffer.Length);
    myPlot.Plot.AxisAuto();
    myPlot.Refresh(); // WPF 用 .Render()
}
```

#### 4. 对数频率轴（适用于音频频谱）

```csharp
// 将频率轴转为对数刻度显示
private void PlotLogSpectrum(double[] spectrumData, double frequencyResolution)
{
    double[] logFreq = Enumerable.Range(1, spectrumData.Length)
                                  .Select(i => Math.Log10(i * frequencyResolution))
                                  .ToArray();
    double[] dbValues = spectrumData.Select(v => 20 * Math.Log10(v + 1e-10)).ToArray();

    var scatter = myPlot.Plot.AddScatter(logFreq, dbValues);
    scatter.MarkerSize = 0;

    myPlot.Plot.XLabel("Frequency (log₁₀ Hz)");
    myPlot.Plot.YLabel("Magnitude (dB)");
    myPlot.Plot.AxisAuto();
    myPlot.Refresh();
}
```

> 💡 **性能提示**：频谱数据点极多（>10万点）时，优先使用 `AddSignal()` 而非 `AddScatter()`，前者针对大数据集做了渲染优化。

---

### 📊 六、多 Y 轴（主次轴）

在工业控制和数据分析中，经常需要在同一张图上显示量级不同的数据。

#### 核心步骤

1. 画第一条线（默认在左轴 AxisIndex 0）
2. 画第二条线，绑定到右侧 Y 轴（设置 `YAxisIndex = 1`）
3. 把右侧 Y 轴设为可见

```csharp
public void SetupDualYAxis(FormsPlot formsPlot1)
{
    formsPlot1.Plot.Title("电机监控：转速 vs 温度");

    double[] ys_rpm = DataGen.Range(0, 3000, 100);
    double[] ys_temp = DataGen.Random(100, 50, 100);

    // 左轴波形（转速）
    var sigRpm = formsPlot1.Plot.AddSignal(ys_rpm);
    sigRpm.Label = "转速";
    sigRpm.Color = Color.DodgerBlue;
    sigRpm.YAxisIndex = 0;

    // 右轴波形（温度）
    var sigTemp = formsPlot1.Plot.AddSignal(ys_temp);
    sigTemp.Label = "温度";
    sigTemp.Color = Color.OrangeRed;
    sigTemp.YAxisIndex = 1;

    // 配置右侧 Y 轴
    formsPlot1.Plot.YAxis2.IsVisible = true;
    formsPlot1.Plot.YAxis2.Label("温度 (℃)");
    formsPlot1.Plot.YAxis2.Color(Color.OrangeRed);

    // 配置左轴
    formsPlot1.Plot.YAxis.Label("转速 (RPM)");
    formsPlot1.Plot.XAxis.Label("时间 (s)");

    formsPlot1.Plot.Legend(true);
    formsPlot1.Refresh();
}
```

---

### 🏷️ 七、标注、箭头与十字光标

#### 1. 文本标注

```csharp
var txt = myPlot.Plot.AddText("峰值点", x: 50, y: 0.8);
txt.FontSize = 12;
txt.Color = Color.Red;
```

#### 2. 箭头

```csharp
var arrow = myPlot.Plot.AddArrow(xTip: 40, yTip: 0.9, xBase: 30, yBase: 0.5);
arrow.Color = Color.Red;
arrow.LineWidth = 2;
```

#### 3. 十字光标 (Crosshair)

常用于实时跟踪鼠标位置，显示数据值：

```csharp
var crosshair = myPlot.Plot.AddCrosshair(0, 0);
crosshair.Color = Color.Gray;

formsPlot1.MouseMove += (s, e) =>
{
    (double x, double y) = formsPlot1.GetMouseCoordinates();
    crosshair.X = x;
    crosshair.Y = y;
    formsPlot1.Refresh();
};
```

#### 4. 水平/垂直参考线

```csharp
// 水平线（如阈值线）
var hLine = myPlot.Plot.AddHorizontalLine(y: 3.3);
hLine.Color = Color.Red;
hLine.LineStyle = LineStyle.Dash;
hLine.Label = "上限阈值";

// 垂直线（如触发点）
var vLine = myPlot.Plot.AddVerticalLine(x: 500);
vLine.Color = Color.Green;
vLine.LineStyle = LineStyle.Dot;
```

---

### 📐 八、自定义坐标轴格式

#### 1. X 轴显示为时间 (HH:mm:ss)

```csharp
static string TimeFormatter(double unixSeconds)
{
    DateTime dt = DateTimeOffset.FromUnixTimeSeconds((long)unixSeconds).DateTime;
    return dt.ToString("HH:mm:ss");
}
myPlot.Plot.XAxis.TickLabelFormat(TimeFormatter);
```

#### 2. Y 轴工程单位格式化

```csharp
static string EngineeringFormat(double value)
{
    if (Math.Abs(value) >= 1e6) return $"{value / 1e6:F1}M";
    if (Math.Abs(value) >= 1e3) return $"{value / 1e3:F1}k";
    if (Math.Abs(value) < 1e-3 && value != 0) return $"{value * 1e3:F1}m";
    return $"{value:F2}";
}
myPlot.Plot.YAxis.TickLabelFormat(EngineeringFormat);
```

#### 3. 手动设置刻度标签

```csharp
double[] positions = { 0, 250, 500, 750, 1000 };
string[] labels = { "0", "250Hz", "500Hz", "750Hz", "1kHz" };
myPlot.Plot.XAxis.ManualTickPositions(positions, labels);
```

---

### 🎨 九、样式与配色

#### 1. 暗黑模式

```csharp
myPlot.Plot.Style(Style.Gray1);
myPlot.Plot.XAxis.Color(Color.White);
myPlot.Plot.YAxis.Color(Color.White);
myPlot.BackColor = Color.FromArgb(30, 30, 30);
```

#### 2. 网格线微调

```csharp
myPlot.Plot.XAxis.MajorGrid(true);
myPlot.Plot.XAxis.MinorGrid(true);
myPlot.Plot.XAxis.GridColor(Color.FromArgb(50, 255, 255, 255));
```

#### 3. 自定义调色板（多曲线配色）

```csharp
// 使用内置调色板
myPlot.Plot.Palette = ScottPlot.Palette.ColorblindFriendly;
```

---

### 🔍 十、鼠标交互与缩放控制

#### 1. 配置鼠标交互

```csharp
formsPlot1.Configuration.LeftClickDragPan = true;      // 左键拖拽平移
formsPlot1.Configuration.RightClickDragZoom = true;    // 右键拖拽框选放大
formsPlot1.Configuration.ScrollWheelZoom = true;       // 滚轮缩放
formsPlot1.Configuration.AllowDroppedFrames = true;    // 低配电脑允许掉帧
```

#### 2. 仅 X/Y 轴缩放

```csharp
// 仅自动调整 X 轴
formsPlot1.Plot.AxisAutoX();
formsPlot1.Refresh();

// 仅自动调整 Y 轴
formsPlot1.Plot.AxisAutoY();
formsPlot1.Refresh();

// 全自适应
formsPlot1.Plot.AxisAuto();
formsPlot1.Refresh();
```

#### 3. 锁定轴向

```csharp
// 锁定 Y 轴，只允许 X 轴缩放/平移（适合时域波形浏览）
formsPlot1.Configuration.LockVerticalAxis = true;
```

#### 4. 渐进式微缩放（类似 CAD）

```csharp
private void ZoomIncremental(bool zoomIn)
{
    var limits = formsPlot1.Plot.GetAxisLimits();
    double xCenter = (limits.XMin + limits.XMax) / 2;
    double yCenter = (limits.YMin + limits.YMax) / 2;
    double xRange = limits.XMax - limits.XMin;
    double yRange = limits.YMax - limits.YMin;

    double factor = zoomIn ? 0.9 : 1.1;

    formsPlot1.Plot.SetAxisLimits(
        xCenter - xRange / 2 * factor, xCenter + xRange / 2 * factor,
        yCenter - yRange / 2 * factor, yCenter + yRange / 2 * factor);
    formsPlot1.Refresh();
}
```

#### 5. 鼠标坐标获取

```csharp
formsPlot1.MouseMove += (sender, e) =>
{
    (double x, double y) = formsPlot1.GetMouseCoordinates();
    label_Status.Text = $"X: {x:F2}, Y: {y:F2}";
};
```

---

### 🧹 十一、清空波形与快捷键

#### 1. 按钮清空

```csharp
private void btn_Clear_Click(object sender, EventArgs e)
{
    formsPlot1.Plot.Clear();
    formsPlot1.Plot.AxisAuto();
    formsPlot1.Refresh();
}
```

#### 2. WinForms 快捷键

```csharp
// 设置窗体 KeyPreview = true
private void MainForm_KeyDown(object sender, KeyEventArgs e)
{
    if (e.KeyCode == Keys.Delete)
    {
        formsPlot1.Plot.Clear();
        formsPlot1.Plot.AxisAuto();
        formsPlot1.Refresh();
        e.Handled = true;
    }
}
```

#### 3. WPF 快捷键

```csharp
private void Window_KeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Delete)
    {
        wpfPlot1.Plot.Clear();
        wpfPlot1.Plot.AxisAuto();
        wpfPlot1.Render();
        e.Handled = true;
    }
}
```

---

### 💾 十二、导出与保存

```csharp
// 保存为 PNG
myPlot.Plot.SaveFig("chart.png", width: 1920, height: 1080);

// 保存为指定 DPI 的高清图
var bmp = myPlot.Plot.Render(width: 3840, height: 2160);
bmp.Save("chart_hd.png", System.Drawing.Imaging.ImageFormat.Png);
```

---

### ⚡ 十三、极致性能优化

#### 1. 大数据集渲染（>100 万点）

```csharp
// 固定采样率信号（最快）
double[] bigData = new double[1_000_000];
myPlot.Plot.AddSignal(bigData, sampleRate: 20000);

// 不等间距大数据，使用 SignalConst
var sig = myPlot.Plot.AddSignalConst(bigData);
```

#### 2. 限制刷新频率（节流）

```csharp
private System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
private const int MIN_RENDER_INTERVAL_MS = 33; // ~30 FPS

private void ThrottledRender()
{
    if (_sw.ElapsedMilliseconds >= MIN_RENDER_INTERVAL_MS)
    {
        formsPlot1.Refresh();
        _sw.Restart();
    }
}
```

#### 3. 关闭多余特效

```csharp
myPlot.Plot.Legend(false);
myPlot.Plot.XAxis.Grid(false);
myPlot.Plot.YAxis.Grid(false);
// WPF 低画质模式
wpfPlot1.Configuration.Quality = ScottPlot.Control.QualityMode.Low;
```

---

### ⚠️ 十四、避坑指南

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 内存暴涨 | 在 Timer 中反复 `AddSignal` | 初始化一次，后续只更新数组数据 |
| WPF 跨线程异常 | 后台线程直接操作 UI | 使用 `Dispatcher.Invoke()` |
| 多波形缩放不同步 | 不同波形 `YAxisIndex` 不一致 | 联动场景下所有波形使用同一 AxisIndex |
| 实时刷新时视图抖动 | 数据更新与用户交互冲突 | 判断 `IsMouseOver` 再执行 AxisAuto |
| WPF 渲染闪烁 | 刷新频率过高 | 降低频率或使用节流机制 |
| 右侧 Y 轴不显示 | 未设置 `IsVisible` | `YAxis2.IsVisible = true` |

#### 防抖动代码模式

```csharp
// 只有鼠标不在控件上时，才执行数据驱动的视图刷新
if (!wpfPlot1.IsMouseOver)
{
    wpfPlot1.Plot.AxisAutoX();
    wpfPlot1.Render();
}
```

---

### 📦 十五、更多图表类型

#### 1. DataLogger（实时数据记录器）

ScottPlot 4.1 提供专用的 `DataLogger` 类型，支持实时追加数据并自动管理视图窗口，非常适合长时间连续采集场景：

```csharp
// 创建 DataLogger（自动管理X轴滚动）
var logger = formsPlot1.Plot.AddDataLogger();
logger.Color = Color.Blue;
logger.LineWidth = 1;

// 配置视图管理模式
logger.ViewScrollLeft(); // 新数据从右侧进入，旧数据从左侧滚出
// 或 logger.ViewSlide(); // 视图跟随最新数据平滑滑动
// 或 logger.ViewJump();  // 填满后跳转到下一个视图窗口

// 设置显示的最大数据点数（可选）
logger.ManageAxisLimits = true;

// 在数据到达时追加
logger.Add(newYValue); // 自动递增X
logger.Add(xValue, yValue); // 指定XY
formsPlot1.Refresh();
```

#### 2. SignalXY（不等间距高性能线图）

当 X 轴数据不等间距但单调递增时，`SignalXY` 比 `Scatter` 快数十倍：

```csharp
double[] xs = { 0, 1.2, 3.5, 4.0, 7.8, 10.1 }; // 不等间距但递增
double[] ys = { 2, 5, 3, 8, 1, 6 };

var sigXY = myPlot.Plot.AddSignalXY(xs, ys);
sigXY.Color = Color.Purple;
sigXY.LineWidth = 2;
```

#### 3. Fill（填充区域）

用于绘制波形下方的阴影区域或两条曲线之间的填充：

```csharp
double[] xs = DataGen.Consecutive(100);
double[] ys = DataGen.Sin(100);

// 填充曲线下方区域
var fill = myPlot.Plot.AddFill(xs, ys);
fill.FillColor = Color.FromArgb(50, Color.Blue); // 半透明蓝色
fill.BaselineY = 0; // 基线位置

// 两条曲线之间的填充
double[] ys2 = DataGen.Cos(100);
var fillBetween = myPlot.Plot.AddFill(xs, ys, ys2);
fillBetween.FillColor = Color.FromArgb(40, Color.Green);
```

#### 4. Error Bar（误差线）

```csharp
double[] xs = { 1, 2, 3, 4, 5 };
double[] ys = { 10, 15, 12, 18, 14 };
double[] errors = { 1.5, 2.0, 1.2, 2.5, 1.8 };

var scatter = myPlot.Plot.AddScatter(xs, ys);
var errBar = myPlot.Plot.AddErrorBars(xs, ys, null, null, errors, errors);
errBar.Color = Color.Black;
errBar.CapSize = 4;
```

#### 5. Heatmap（热力图）

适合展示 2D 矩阵数据（如频谱瀑布图、温度分布）：

```csharp
double[,] data = new double[50, 100]; // 50行×100列
for (int y = 0; y < 50; y++)
    for (int x = 0; x < 100; x++)
        data[y, x] = Math.Sin(x * 0.1) * Math.Cos(y * 0.1);

var heatmap = myPlot.Plot.AddHeatmap(data, lockScales: false);
heatmap.Colormap = ScottPlot.Drawing.Colormap.Viridis;
heatmap.Smooth = true; // 双三次插值平滑

// 添加色标
var colorbar = myPlot.Plot.AddColorbar(heatmap);
```

#### 6. Function Plot（函数图）

传入一个函数而非数据数组，可以无限缩放：

```csharp
// 绘制 y = sin(x)/x
var func = myPlot.Plot.AddFunction(x => Math.Sin(x) / x);
func.Color = Color.Magenta;
func.LineWidth = 2;
func.Label = "sinc(x)";

myPlot.Plot.SetAxisLimits(-20, 20, -0.5, 1.2);
```

#### 7. Axis Span（区域高亮）

高亮标记一个感兴趣的 X 或 Y 范围：

```csharp
// X 轴区域高亮（如标记一段异常时间）
var spanX = myPlot.Plot.AddHorizontalSpan(xMin: 100, xMax: 200);
spanX.Color = Color.FromArgb(30, Color.Red);
spanX.Label = "异常区间";

// Y 轴区域高亮（如标记正常范围）
var spanY = myPlot.Plot.AddVerticalSpan(yMin: -1, yMax: 1);
spanY.Color = Color.FromArgb(20, Color.Green);
```

#### 8. DateTime 轴（时间轴绘图）

官方推荐的 DateTime 绘图方式：

```csharp
// 将 DateTime 转为 OADate（double），ScottPlot 4.1 官方推荐
DateTime[] dates = { DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2),
                     DateTime.Now.AddHours(-1), DateTime.Now };
double[] xs = dates.Select(d => d.ToOADate()).ToArray();
double[] ys = { 25.1, 26.3, 24.8, 27.0 };

var scatter = myPlot.Plot.AddScatter(xs, ys);
myPlot.Plot.XAxis.DateTimeFormat(true); // 启用 DateTime 格式化
```

#### 9. Pie Chart（饼图）

```csharp
double[] values = { 778, 283, 184, 76 };
string[] labels = { "C#", "Python", "Java", "Go" };

var pie = myPlot.Plot.AddPie(values);
pie.SliceLabels = labels;
pie.ShowLabels = true;
pie.ShowPercentages = true;
```

#### 10. Radial Gauge（径向仪表盘）

```csharp
double[] values = { 70, 85, 45 };
var gauge = myPlot.Plot.AddRadialGauge(values);
gauge.Labels = new string[] { "CPU", "内存", "磁盘" };
gauge.MaximumAngle = 270; // 仪表盘最大角度
```

---

### 🛠️ 十六、实用代码片段

#### 1. 移除特定类型的图层（保留其他）

```csharp
// 只移除所有 Signal 图层，保留标注等
myPlot.Plot.Clear(typeof(ScottPlot.Plottable.SignalPlot));
myPlot.Refresh();
```

#### 2. 移除指定的单个图层

```csharp
var sig = myPlot.Plot.AddSignal(data);
// 稍后移除这一个图层
myPlot.Plot.Remove(sig);
myPlot.Refresh();
```

#### 3. 无边框全屏图表（Frameless）

```csharp
myPlot.Plot.Frameless();
myPlot.Plot.XAxis.IsVisible = false;
myPlot.Plot.YAxis.IsVisible = false;
myPlot.Plot.XAxis2.IsVisible = false;
myPlot.Plot.YAxis2.IsVisible = false;
```

#### 4. 旋转 X 轴标签（防止密集标签重叠）

```csharp
myPlot.Plot.XAxis.TickLabelStyle(rotation: 45);
myPlot.Plot.XAxis.SetSizeLimit(min: 50); // 为旋转标签留空间
```

#### 5. 设置固定坐标范围（禁止用户超范围缩放）

```csharp
myPlot.Plot.SetAxisLimits(xMin: 0, xMax: 1000, yMin: -5, yMax: 5);
myPlot.Plot.SetOuterViewLimits(xMin: 0, xMax: 1000, yMin: -10, yMax: 10);
```

#### 6. 多图层颜色循环

```csharp
// 自动为多条曲线分配不同颜色
for (int i = 0; i < 5; i++)
{
    double[] data = DataGen.Sin(100, phase: i * 0.2);
    var sig = myPlot.Plot.AddSignal(data);
    sig.Color = myPlot.Plot.Palette.GetColor(i);
    sig.Label = $"通道 {i + 1}";
}
myPlot.Plot.Legend(true);
```

#### 7. 获取鼠标最近的数据点（Snap to point）

```csharp
formsPlot1.MouseMove += (s, e) =>
{
    (double mouseX, double mouseY) = formsPlot1.GetMouseCoordinates();
    // 手动查找最近点
    int nearestIndex = 0;
    double minDist = double.MaxValue;
    for (int i = 0; i < xs.Length; i++)
    {
        double dist = Math.Abs(xs[i] - mouseX);
        if (dist < minDist) { minDist = dist; nearestIndex = i; }
    }
    // 更新十字光标到最近数据点
    crosshair.X = xs[nearestIndex];
    crosshair.Y = ys[nearestIndex];
    formsPlot1.Refresh();
};
```

#### 8. 右键菜单自定义

```csharp
// WinForms: 禁用默认右键菜单
formsPlot1.RightClicked -= formsPlot1.DefaultRightClickEvent;

// 自定义右键菜单
formsPlot1.RightClicked += (s, e) =>
{
    var menu = new ContextMenuStrip();
    menu.Items.Add("保存图片", null, (sender, args) =>
        formsPlot1.Plot.SaveFig("export.png"));
    menu.Items.Add("自动缩放", null, (sender, args) =>
    {
        formsPlot1.Plot.AxisAuto();
        formsPlot1.Refresh();
    });
    menu.Show(formsPlot1, e.Location);
};
```

---

### ✅ 十七、多波形同步缩放 Checklist

在 ScottPlot 4.1 中，同步缩放是**天生自带**的，只需满足：

1. **共享 X 轴**：所有波形 X 数组长度一致或逻辑对应同一时间轴
2. **统一 AxisIndex**：联动场景下所有波形 `YAxisIndex` 都为 0
3. **单次刷新**：批量更新完数据后只调用一次 `Refresh()` / `Render()`

**默认交互行为：**
- 滚轮：Y 轴同步缩放；Ctrl+滚轮：X 轴同步缩放
- 右键拖拽：框选放大
- 中键拖拽 / Shift+左键拖拽：全视图同步平移
