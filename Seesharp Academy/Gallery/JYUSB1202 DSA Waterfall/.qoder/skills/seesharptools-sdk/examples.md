# SeeSharpTools SDK 完整代码示例

---

## ★ EasyChartX 专项示例

### EX-A：Designer.cs 正确初始化（单通道 + 多通道）

```csharp
// ─── 在 InitializeComponent() 中 ───────────────────────────────────────────
// ⚠️ 关键规则：
//   1. 必须用 LineSeries.Add()，不存在 SeriesCollection.Add() 属性
//   2. Designer.cs 无 using 语句，必须写完整全限定名
//   3. FastLine 不支持 Marker，需要标记点时用 Line 类型

// 单通道（模拟信号 / 时域波形，推荐 FastLine）
SeeSharpTools.JY.GUI.EasyChartXSeries series1 = new SeeSharpTools.JY.GUI.EasyChartXSeries();
series1.Color     = System.Drawing.Color.DodgerBlue;
series1.Name      = "时域信号";
series1.Type      = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.FastLine;  // 高性能，不支持 Marker
series1.Width     = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Thin;
series1.Marker    = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.None;
series1.Visible   = true;
series1.XPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
series1.YPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
this.easyChartX1.LineSeries.Add(series1);

// 多通道（4通道，循环方式）
string[] colors = { "DodgerBlue", "OrangeRed", "LimeGreen", "Orchid" };
for (int i = 0; i < 4; i++)
{
    SeeSharpTools.JY.GUI.EasyChartXSeries s = new SeeSharpTools.JY.GUI.EasyChartXSeries();
    s.Color   = System.Drawing.Color.FromName(colors[i]);
    s.Name    = string.Format("Ch{0}", i);
    s.Type    = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.FastLine;
    s.Width   = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Thin;
    s.Marker  = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.None;
    s.Visible = true;
    this.easyChartX_multi.LineSeries.Add(s);
}
```

---

### EX-B：运行时动态绘图（单/多通道）

```csharp
using SeeSharpTools.JY.GUI;

// ── 单通道（时域波形）──────────────────────────────────────────────────────
double[] signal = new double[1024];
// ... 填充 signal 数据
easyChartX_time.Plot(signal, xStart: 0, xIncrement: 1.0 / sampleRate);
easyChartX_time.AxisX.Title = string.Format("时间 (s)  [采样率 {0} Hz]", sampleRate);
easyChartX_time.AxisY.Title = "幅值 (V)";

// ── 多通道 Row 优先（通道数×采样点）────────────────────────────────────────
// 适用于自行构造的数据：data[channelIdx, sampleIdx]
double[,] multiData = new double[4, 1024];
// ... 填充数据
easyChartX_multi.Plot(multiData, 0, 1.0 / sampleRate, MajorOrder.Row);

// ── 多通道 Column 优先（采样点×通道数）──────────────────────────────────────
// 适用于 JYUSB1601 AITask.ReadData 返回的缓冲区
double[,] readBuf = new double[1024, 4];  // [sampleCount, channelCount]
aiTask.ReadData(ref readBuf, 1024, -1);
easyChartX_multi.Plot(readBuf, 0, 1.0 / sampleRate, MajorOrder.Column);
```

---

### EX-C：坐标轴完整配置

```csharp
// ── 手动设置范围（关闭自动缩放）─────────────────────────────────────────────
easyChartX1.AxisX.AutoScale = false;
easyChartX1.AxisX.Minimum   = 0.0;
easyChartX1.AxisX.Maximum   = 1.0;   // 1秒
easyChartX1.AxisY.AutoScale = false;
easyChartX1.AxisY.Minimum   = -10.0;
easyChartX1.AxisY.Maximum   =  10.0;

// ── 轴标题 ──────────────────────────────────────────────────────────────────
easyChartX1.AxisX.Title = "时间 (s)";
easyChartX1.AxisY.Title = "幅值 (V)";

// ── 标签格式 ────────────────────────────────────────────────────────────────
easyChartX1.AxisX.LabelFormat = "F3";   // 3位小数
easyChartX1.AxisY.LabelFormat = "F2";

// ── 对数坐标（频谱幅值，单位 V²/Hz）────────────────────────────────────────
easyChartX_freq.AxisY.IsLogarithmic         = true;
easyChartX_freq.AxisY.LogarithmBase         = 10.0;
easyChartX_freq.AxisY.LogLabelStyle         = EasyChartXAxis.LogarithmicLabelStyle.E2;
easyChartX_freq.AxisY.ShowLogarithmicLines  = true;
easyChartX_freq.Miscellaneous.CheckNegtiveOrZero = true;  // 过滤 <=0 值，防对数轴异常

// ── 网格线 ──────────────────────────────────────────────────────────────────
easyChartX1.AxisX.MajorGridEnabled = true;
easyChartX1.AxisX.MajorGridCount   = 5;
easyChartX1.AxisX.MajorGridType    = EasyChartXAxis.GridStyle.Dash;
easyChartX1.AxisY.MinorGridEnabled = false;
```

---

### EX-D：dBV 频谱图

> **方法一：⭐ 推荐用 `SpectrumUnits.dBV` 直接输出（简洁准确），见 EX-L 场景 2。**
> **方法二：手动转换（如下），适用于需要对功率谱原始值做额外处理的场景。**

```csharp
using SeeSharpTools.JY.DSP.Fundamental;

// ── Step 1：计算功率谱 ──────────────────────────────────────────────────────
double[] signal   = new double[4096];
double[] powerSpectrum = new double[4096 / 2];
double df;
Spectrum.PowerSpectrum(signal, sampleRate, ref powerSpectrum, out df);

// ── Step 2：转换为 dBV（在数据层完成，不依赖图表对数轴）──────────────────────
const double DbVFloor = -120.0;
double[] spectrumDbV = new double[powerSpectrum.Length];
for (int i = 0; i < powerSpectrum.Length; i++)
{
    spectrumDbV[i] = (powerSpectrum[i] > 0)
        ? 20.0 * Math.Log10(Math.Sqrt(powerSpectrum[i]))
        : DbVFloor;
}

// ── Step 3：用线性Y轴显示（Y轴标题写 dBV 即可）───────────────────────────────
easyChartX_freq.Plot(spectrumDbV, 0.0, df);
easyChartX_freq.AxisX.Title = string.Format("频率 (Hz)  [分辨率 {0:F3} Hz/bin]", df);
easyChartX_freq.AxisY.Title = "dBV";
// ⚠️ 不要设置 IsLogarithmic = true，数据已是 dBV 线性表示
```

---

### EX-E：运行时动态修改系列属性

```csharp
// 修改颜色和名称
easyChartX1.LineSeries[0].Color = Color.Red;
easyChartX1.LineSeries[0].Name  = "通道 1（修改后）";

// 隐藏/显示某条曲线
easyChartX1.LineSeries[1].Visible = false;

// 动态重建系列（通道数不固定时）
easyChartX1.Series.Clear();
for (int i = 0; i < channelCount; i++)
{
    EasyChartXSeries s = new EasyChartXSeries();
    s.Name    = string.Format("Ch{0}", i);
    s.Color   = Color.FromArgb(255, i * 40 % 255, 100, 200);
    s.Type    = EasyChartXSeries.LineType.FastLine;
    s.Visible = true;
    easyChartX1.Series.Add(s);
}
easyChartX1.Plot(newMultiData, 0, 1.0 / sampleRate, MajorOrder.Row);
```

---

### EX-F：双Y轴（主Y轴 + 副Y轴）

```csharp
// ── Designer.cs 中 ──────────────────────────────────────────────────────────
EasyChartXSeries sCurrent = new EasyChartXSeries();
sCurrent.Name      = "电流 (A)";
sCurrent.Color     = Color.Blue;
sCurrent.YPlotAxis = EasyChartXAxis.PlotAxis.Primary;  // 绑定左Y轴
this.easyChartX1.LineSeries.Add(sCurrent);

EasyChartXSeries sTemp = new EasyChartXSeries();
sTemp.Name      = "温度 (°C)";
sTemp.Color     = Color.Red;
sTemp.YPlotAxis = EasyChartXAxis.PlotAxis.Secondary;   // 绑定右Y轴
this.easyChartX1.LineSeries.Add(sTemp);

// ── 运行时配置副Y轴 ──────────────────────────────────────────────────────────
easyChartX1.AxisY.Title   = "电流 (A)";
easyChartX1.AxisY2.Title  = "温度 (°C)";
easyChartX1.AxisY2.AutoScale = false;
easyChartX1.AxisY2.Minimum   = 0.0;
easyChartX1.AxisY2.Maximum   = 100.0;

// ── 绘图（使用多通道 Plot，系列绑定决定各曲线所用Y轴）─────────────────────────
double[,] data = new double[2, sampleCount];  // data[0]=电流, data[1]=温度
// ... 填充数据
easyChartX1.Plot(data, 0, 1.0 / sampleRate, MajorOrder.Row);
```

---

### EX-G：SplitView 多通道分图显示

```csharp
// ── 启用分图模式（每通道独立绘图区域，有各自坐标轴）─────────────────────────
easyChartX1.SplitView = true;
easyChartX1.Miscellaneous.SplitViewAutoLayout      = true;
easyChartX1.Miscellaneous.DirectionChartCount      = 2;   // 每行2图
easyChartX1.Miscellaneous.SplitLayoutDirection     = EasyChartXUtility.LayoutDirection.LeftToRight;

// ── 4通道分图绘制 ────────────────────────────────────────────────────────────
double[,] data = new double[4, 1024];
// ... 填充4通道数据
easyChartX1.Plot(data, 0, 1.0 / sampleRate, MajorOrder.Row);
```

---

### EX-H：高性能高频刷新（1kHz+）

```csharp
// ── 配置建议（在 InitializeComponent 完成后）────────────────────────────────
easyChartX1.Miscellaneous.DataStorage        = DataStorageType.NoClone; // 不复制数据
easyChartX1.Miscellaneous.CheckNaN           = false; // 关闭数值检查
easyChartX1.Miscellaneous.CheckInfinity      = false;
easyChartX1.Miscellaneous.CheckNegtiveOrZero = false;
easyChartX1.AutoClear = true;

// ── 后台线程定时刷新（必须通过 Invoke 切回 UI 线程）────────────────────────
private double[] _buffer = new double[4096];
private System.Windows.Forms.Timer _uiTimer;

private void StartPlotTimer()
{
    _uiTimer = new System.Windows.Forms.Timer();
    _uiTimer.Interval = 50;  // 20fps
    _uiTimer.Tick += (s, e) => {
        // buffer 由后台线程或硬件驱动填充
        easyChartX1.Plot(_buffer, 0, 1.0 / sampleRate);
    };
    _uiTimer.Start();
}
```

---

### EX-I：游标与数据标记

```csharp
// ── 启用 X 游标读值模式 ──────────────────────────────────────────────────────
easyChartX1.XCursor.Mode  = EasyChartXCursor.CursorMode.Cursor;
easyChartX1.XCursor.Color = Color.Red;
easyChartX1.XCursor.AutoInterval = true;

// 读取游标当前位置
double xVal = easyChartX1.XCursor.Value;

// ── Tab 游标（多根垂直线）────────────────────────────────────────────────────
easyChartX1.TabCursors.Add(new TabCursor { Value = 0.1 });
easyChartX1.TabCursors.Add(new TabCursor { Value = 0.5 });
easyChartX1.TabCursors.Add(new TabCursor { Value = 0.9 });

// ── 在指定数据点添加三角标记 ─────────────────────────────────────────────────
List<double> mx = new List<double>() { 0.05, 0.30 };
List<double> my = new List<double>() { 0.95, 2.80 };
easyChartX1.AddDataMarker(mx, my, Color.Cyan, DataMarkerType.Triangle);

// ── 事件：游标移动时读取 X 值并更新 Label ────────────────────────────────────
easyChartX1.CursorPositionChanged += (s, e) =>
{
    label_CursorX.Text = string.Format("X = {0:F4}", easyChartX1.XCursor.Value);
};
```

---

### EX-J：视图缩放/平移后读取可见范围

```csharp
// ── 订阅视图变化事件 ──────────────────────────────────────────────────────────
easyChartX1.AxisViewChanged += (sender, e) =>
{
    if (!e.IsRaisedByMouseEvent) return;  // 只响应鼠标操作

    double xMin = easyChartX1.AxisX.ViewMinimum;
    double xMax = easyChartX1.AxisX.ViewMaximum;
    double yMin = easyChartX1.AxisY.ViewMinimum;
    double yMax = easyChartX1.AxisY.ViewMaximum;

    label_ViewRange.Text = string.Format(
        "X: [{0:F3}, {1:F3}]  Y: [{2:F3}, {3:F3}]",
        xMin, xMax, yMin, yMax);
};

// ── 代码控制缩放区间（不触发事件）────────────────────────────────────────────
easyChartX1.AxisX.ViewMinimum = 0.0;
easyChartX1.AxisX.ViewMaximum = 0.5;  // 只显示前 0.5s
```

---



```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.ArrayUtility;
using SeeSharpTools.JY.GUI;

private void button_AnalyzeSpectrum_Click(object sender, EventArgs e)
{
    const double sampleRate = 10000;   // 采样率 10kHz
    const int dataLength = 4096;
    
    double[] signal = new double[dataLength];
    double[] noise = new double[dataLength];
    double[] spectrum = new double[dataLength / 2];
    double df;

    // 生成含噪声的正弦波
    Generation.SineWave(ref signal, amplitude: 1.0, phase: 0, frequency: 600, samplingRate: sampleRate);
    Generation.UniformWhiteNoise(ref noise, amplitude: 0.1);
    ArrayCalculation.Add(signal, noise, ref signal);

    // 计算功率谱（dBV 单位）
    Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df, SpectrumUnits.dBV);

    // 显示时域和频域
    easyChartX_time.Plot(signal, 0, 1.0 / sampleRate);
    easyChartX_freq.Plot(spectrum, 0, df);

    // 对数Y轴
    easyChartX_freq.AxisY.IsLogarithmic = false;  // dBV 时不需要对数轴
}
```

---

## 示例 2：多通道数据采集实时显示

```csharp
using System.Threading;
using SeeSharpTools.JY.GUI;

private Thread _acqThread;
private bool _isRunning;
private const int ChannelCount = 4;
private const int SamplePerChannel = 1000;
private const double SampleRate = 10000.0;

private void button_Start_Click(object sender, EventArgs e)
{
    _isRunning = true;
    _acqThread = new Thread(AcquisitionLoop) { IsBackground = true };
    _acqThread.Start();
}

private void button_Stop_Click(object sender, EventArgs e)
{
    _isRunning = false;
}

private void AcquisitionLoop()
{
    double[,] data = new double[ChannelCount, SamplePerChannel];
    Random rnd = new Random();

    while (_isRunning)
    {
        // 模拟多通道采集（实际替换为硬件读取）
        for (int ch = 0; ch < ChannelCount; ch++)
            for (int i = 0; i < SamplePerChannel; i++)
                data[ch, i] = Math.Sin(2 * Math.PI * (ch + 1) * i / SamplePerChannel) + rnd.NextDouble() * 0.1;

        // 跨线程更新 UI
        this.Invoke(new Action(() =>
        {
            easyChartX1.Plot(data, 0, 1.0 / SampleRate, MajorOrder.Row);
        }));

        Thread.Sleep(100);  // 10 fps 更新
    }
}
```

---

## 示例 3：谐波分析（THD 计算）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Measurements;
using SeeSharpTools.JY.ArrayUtility;

private void button_THD_Click(object sender, EventArgs e)
{
    const double sampleRate = 100000;
    const int dataLength = 100000;
    const double frequency = 50;

    double[] baseSignal = new double[dataLength];
    double[] harmonic2 = new double[dataLength];
    double[] harmonic3 = new double[dataLength];
    double[] combined = new double[dataLength];

    // 生成含二次、三次谐波的信号
    Generation.SineWave(ref baseSignal, 10.0, 0, frequency, sampleRate);
    Generation.SineWave(ref harmonic2, 0.5, 0, frequency * 2, sampleRate);
    Generation.SineWave(ref harmonic3, 0.2, 0, frequency * 3, sampleRate);

    ArrayCalculation.Add(baseSignal, harmonic2, ref combined);
    ArrayCalculation.Add(combined, harmonic3, ref combined);

    // THD 分析
    double fundamentalFreq, thd;
    // ⚠️ componentsLevel 必须预先 new！长度 = highestHarmonic + 1
    double[] componentsLevel = new double[6]; // highestHarmonic=5, 长度=6

    HarmonicAnalysis.THDAnalysis(
        combined,
        dt: 1.0 / sampleRate,
        out fundamentalFreq,
        out thd,
        ref componentsLevel,   // ⚠️ ref（不是 out）
        highestHarmonic: 5
    );

    label_THD.Text = $"THD: {thd * 100:F4}%";
    label_Fund.Text = $"基波频率: {fundamentalFreq:F2} Hz";
    label_Fundamental.Text = $"基波幅度: {componentsLevel[1]:F4} V";

    // 频谱显示
    double[] spectrum = new double[dataLength / 2];
    double df;
    Spectrum.PowerSpectrum(combined, sampleRate, ref spectrum, out df);
    easyChartX1.Plot(spectrum, 0, df);
}
```

---

## 示例 4：FIR 滤波器应用

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;
using MathNet.Filtering.FIR;  // 需要引用 MathNet.Filtering.dll

private void button_Filter_Click(object sender, EventArgs e)
{
    const double sampleRate = 10000;
    const int dataLength = 4096;

    // 生成混合信号：低频 100Hz + 高频 3000Hz
    double[] signal = new double[dataLength];
    double[] highFreqNoise = new double[dataLength];

    Generation.SineWave(ref signal, 1.0, 0, 100, sampleRate);
    Generation.SineWave(ref highFreqNoise, 0.5, 0, 3000, sampleRate);

    for (int i = 0; i < dataLength; i++)
        signal[i] += highFreqNoise[i];

    // Step 1: 用 MathNet 设计低通 FIR 滤波器（截止 500Hz，阶数 64）
    // ⚠️ SeeSharpTools 不含滤波器设计，需要 MathNet.Filtering.dll 生成系数
    double[] firCoeff = FirCoefficients.LowPass(sampleRate, 500, 64);

    // Step 2: 用 SeeSharpTools 执行滤波
    double[] filtered = SignalOperation.Filtering.FIRFilter(signal, firCoeff);
    // ⚠️ FIRFilter 返回 double[]，长度可能比输入短

    // 对比显示
    easyChartX_raw.Plot(signal, 0, 1.0 / sampleRate);
    easyChartX_filtered.Plot(filtered, 0, 1.0 / sampleRate);
}
```

---

## 示例 5：CSV 文件读写

```csharp
using SeeSharpTools.JY.File;

// 写入数据到 CSV
private void button_WriteCSV_Click(object sender, EventArgs e)
{
    double[,] data = new double[100, 3];
    for (int i = 0; i < 100; i++)
    {
        data[i, 0] = i * 0.001;                           // 时间
        data[i, 1] = Math.Sin(2 * Math.PI * 50 * data[i, 0]);  // 通道1
        data[i, 2] = Math.Cos(2 * Math.PI * 50 * data[i, 0]);  // 通道2
    }

    try
    {
        CsvHandler.WriteData(data);
        MessageBox.Show("保存成功");
    }
    catch (Exception ex)
    {
        MessageBox.Show($"保存失败：{ex.Message}");
    }
}

// 读取 CSV 数据
private void button_ReadCSV_Click(object sender, EventArgs e)
{
    try
    {
        // 从第1行（跳过标题行）、第0列开始读取
        double[,] readData = CsvHandler.Read<double>(startRow: 1, startColumn: 0);

        int rows = readData.GetLength(0);
        int cols = readData.GetLength(1);

        // 提取列数据绘图
        double[] col1 = new double[rows];
        double[] col2 = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            col1[i] = readData[i, 1];
            col2[i] = readData[i, 2];
        }

        easyChartX1.Plot(col1);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"读取失败：{ex.Message}");
    }
}
```

---

## 示例 6：数据库操作（SQLite）

```csharp
using SeeSharpTools.JY.Database;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

private DbOperation _db;

private void InitDatabase()
{
    string connStr = "Data Source=measurement.db;Version=3;";
    _db = new DbOperation(connStr, DbProviderType.SQLite);

    // 建表
    _db.ExecuteNonQuery(
        "CREATE TABLE IF NOT EXISTS measurement (id INTEGER PRIMARY KEY, timestamp TEXT, value REAL)",
        null
    );
}

// 写入数据
private void SaveMeasurement(double value)
{
    var parameters = new List<DbParameter>();
    // SQLite 参数使用具体实现类，实际开发中使用 SQLiteParameter
    _db.ExecuteNonQuery(
        $"INSERT INTO measurement (timestamp, value) VALUES ('{DateTime.Now:yyyy-MM-dd HH:mm:ss}', {value})",
        null
    );
}

// 查询数据
private void LoadAndPlotData()
{
    DataTable dt = _db.ExecuteDataTable("SELECT value FROM measurement ORDER BY id", null);

    double[] values = new double[dt.Rows.Count];
    for (int i = 0; i < dt.Rows.Count; i++)
        values[i] = Convert.ToDouble(dt.Rows[i]["value"]);

    easyChartX1.Plot(values);
}
```

---

## 示例 7：TCP 通信（服务端 + 客户端）

```csharp
using SeeSharpTools.JY.TCP;
using System.Text;

// 服务端
public class TCPServerDemo
{
    private EasyTCPServer _server;

    public void Start()
    {
        _server = new EasyTCPServer();
        _server.DataReceived += OnDataReceived;
        _server.ClientConnected += (s, e) => Console.WriteLine($"客户端连接: {e.ClientID}");
        _server.Start(port: 8888);
    }

    private void OnDataReceived(object sender, EasyTCPEventArgs e)
    {
        string msg = Encoding.UTF8.GetString(e.Data);
        Console.WriteLine($"收到 [{e.ClientID}]: {msg}");

        // 回应
        byte[] response = Encoding.UTF8.GetBytes($"已收到: {msg}");
        _server.Send(e.ClientID, response);
    }

    public void Stop() => _server.Stop();
}

// 客户端
public class TCPClientDemo
{
    private EasyTCPClient _client;

    public void Connect(string ip, int port)
    {
        _client = new EasyTCPClient();
        _client.DataReceived += (s, e) =>
            Console.WriteLine($"收到服务端: {Encoding.UTF8.GetString(e.Data)}");

        _client.Connect(ip, port);
    }

    public void Send(string message)
    {
        _client.Send(Encoding.UTF8.GetBytes(message));
    }
}
```

---

## 示例 8：传感器换算（PT100 温度 + 荷重传感器）

```csharp
using SeeSharpTools.JY.Sensors;

private void button_Convert_Click(object sender, EventArgs e)
{
    // PT100 温度换算（电阻 → 温度）
    double[] ptResistances = { 100.0, 103.9, 107.8, 111.7, 115.5 };
    double[] temperatures = RTD.Convert(ptResistances, RTDType.PT100);

    for (int i = 0; i < temperatures.Length; i++)
        Console.WriteLine($"R={ptResistances[i]:F1}Ω → T={temperatures[i]:F1}℃");

    // 荷重传感器换算（电压 → 荷重 kg）
    double[] voltages = { 0, 0.001, 0.002, 0.003, 0.004 };
    double[] loads = LoadCell.Convert(
        voltages,
        sensitivity: 2.0,          // 2 mV/V
        maxload: 100.0,             // 最大量程 100 kg
        ExcitationValtage: 5.0     // 激励电压 5V
    );

    // 自定义换算：线性缩放 4-20mA → 0-100%
    double[] currentSignals = { 0.004, 0.008, 0.012, 0.016, 0.020 };
    double[] percentages = CustomScaling.Convert(
        currentSignals,
        current => (current - 0.004) / (0.020 - 0.004) * 100.0
    );
}
```

---

## 示例 9：日志记录

```csharp
using SeeSharpTools.JY.Report;

public class DataAcquisitionApp
{
    public void Initialize()
    {
        // 初始化日志（分级、多文件轮转）
        var config = new LogConfig
        {
            FilePath = @"logs\app.log",
            MaxFileSize = 5 * 1024 * 1024,  // 5MB 滚动
            MaxFileCount = 10
        };
        Logger.Initialize(config);
        Logger.LogLevel = LogLevel.Info;
        Logger.Info("应用程序启动");
    }

    public void StartAcquisition(int sampleRate)
    {
        Logger.Info("开始采集，采样率：{0} S/s", sampleRate);
        try
        {
            // ... 采集代码 ...
            Logger.Debug("采集缓冲区就绪，大小：{0}", 4096);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "采集启动失败：{0}", ex.Message);
            throw;
        }
    }

    public void OnOverflow()
    {
        Logger.Warn("缓冲区溢出，丢失数据点");
    }
}
```

---

## 示例 10：仪表控件实时更新

```csharp
using SeeSharpTools.JY.GUI;
using System.Windows.Forms;

private System.Windows.Forms.Timer _displayTimer;
private double _currentTemp = 25.0;

private void InitControls()
{
    // 设置仪表量程
    gaugeLinear1.Maximum = 150;
    gaugeLinear1.Minimum = -50;

    thermometer1.Maximum = 150;
    thermometer1.Minimum = -50;

    tank1.Maximum = 100;
    tank1.Minimum = 0;

    // 启动定时刷新
    _displayTimer = new System.Windows.Forms.Timer { Interval = 100 };
    _displayTimer.Tick += UpdateDisplays;
    _displayTimer.Start();
}

private void UpdateDisplays(object sender, EventArgs e)
{
    // 模拟传感器读值
    _currentTemp += (new Random().NextDouble() - 0.5) * 0.5;

    // 更新仪表（已在 UI 线程，无需 Invoke）
    gaugeLinear1.Value = _currentTemp;
    thermometer1.Value = _currentTemp;
    tank1.Value = (_currentTemp + 50) / 200 * 100;  // 温度映射到液位
    segmentBright1.Value = _currentTemp.ToString("F1");

    // LED 警告指示
    ledMatrix1.Value[0] = _currentTemp > 100;  // 高温警告 LED
}
```

---

## 示例 11：多窗口 Docking 布局

```csharp
using WeifenLuo.WinFormsUI.Docking;  // 需引用 WeifenLuo.WinFormsUI.Docking.dll

public partial class MainForm : Form
{
    private DockPanel _dockPanel;

    public MainForm()
    {
        InitializeComponent();
        _dockPanel = new DockPanel();
        _dockPanel.Dock = DockStyle.Fill;
        Controls.Add(_dockPanel);
    }

    private void AddDocumentWindow(string title, Control content)
    {
        var form = new DockContent();
        form.Text = title;
        form.Controls.Add(content);
        content.Dock = DockStyle.Fill;
        form.Show(_dockPanel, DockState.Document);
    }

    private void AddToolWindow(string title, Control content, DockState state)
    {
        var form = new DockContent();
        form.Text = title;
        form.Controls.Add(content);
        content.Dock = DockStyle.Fill;
        form.Show(_dockPanel, state);
    }
}
```

---

## 示例 12：完整 WinForm 数据采集应用框架

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.File;
using SeeSharpTools.JY.GUI;
using SeeSharpTools.JY.Report;
using System.Threading;

public partial class MainForm : Form
{
    private Thread _acqThread;
    private bool _isRunning;
    private const double SampleRate = 10000.0;
    private const int FrameSize = 1000;

    public MainForm()
    {
        InitializeComponent();
        Logger.Initialize("app.log");
        Logger.Info("程序启动");
    }

    private void button_Start_Click(object sender, EventArgs e)
    {
        _isRunning = true;
        button_Start.Enabled = false;
        button_Stop.Enabled = true;
        _acqThread = new Thread(AcquireAndProcess) { IsBackground = true };
        _acqThread.Start();
        Logger.Info("采集启动，采样率：{0}", SampleRate);
    }

    private void button_Stop_Click(object sender, EventArgs e)
    {
        _isRunning = false;
        button_Start.Enabled = true;
        button_Stop.Enabled = false;
        Logger.Info("采集停止");
    }

    private void AcquireAndProcess()
    {
        double[] signal = new double[FrameSize];
        double[] spectrum = new double[FrameSize / 2];
        double df;

        while (_isRunning)
        {
            // 模拟采集（替换为实际硬件 API）
            Generation.SineWave(ref signal, 1.0, 0, 50, SampleRate);

            // 频谱分析
            Spectrum.PowerSpectrum(signal, SampleRate, ref spectrum, out df);

            // 更新 UI
            this.BeginInvoke(new Action(() =>
            {
                easyChartX_time.Plot(signal, 0, 1.0 / SampleRate);
                easyChartX_freq.Plot(spectrum, 0, df);

                // 更新仪表：取信号 RMS
                double rms = 0;
                foreach (var v in signal) rms += v * v;
                gaugeLinear1.Value = Math.Sqrt(rms / signal.Length);
            }));

            Thread.Sleep(50);  // 20 fps
        }
    }

    private void button_Save_Click(object sender, EventArgs e)
    {
        double[] lastSignal = new double[FrameSize];
        // ... 获取最新数据
        CsvHandler.WriteData(new double[FrameSize, 1]);
        Logger.Info("数据已保存");
    }
}
```

---

### EX-K：LineSeries LineType 与 Marker 完整示例（最易出错场景汇总）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// LineSeries 使用规则速记：
//   初始化  → LineSeries.Add()（不是 SeriesCollection.Add）
//   FastLine → 高性能，❌ 不支持 Marker
//   Line     → 标准，  ✅ 支持 Marker
//   StepLine → 阶梯，  ✅ 支持 Marker，数字信号专用
//   运行时   → LineSeries[i] 或 Series[i] 均可（等价）
// ══════════════════════════════════════════════════════════════════════════════

// ── 场景 1：连续模拟信号（FastLine，大数据量，高刷新）────────────────────────
// 在 Designer.cs 中：
SeeSharpTools.JY.GUI.EasyChartXSeries sAnalog = new SeeSharpTools.JY.GUI.EasyChartXSeries();
sAnalog.Type   = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.FastLine;  // ✅ 高性能
sAnalog.Marker = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.None;    // FastLine 下无效，设 None 即可
sAnalog.Name   = "模拟信号";
sAnalog.Width  = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Thin;
sAnalog.Color  = System.Drawing.Color.DodgerBlue;
this.easyChartX1.LineSeries.Add(sAnalog);
// 绘图：
easyChartX1.Plot(analogSignal, 0, 1.0 / sampleRate);

// ── 场景 2：带标记点的散点/小数据量曲线（Line + Marker）──────────────────────
// 在 Designer.cs 中：
SeeSharpTools.JY.GUI.EasyChartXSeries sScatter = new SeeSharpTools.JY.GUI.EasyChartXSeries();
sScatter.Type   = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.Line;        // ✅ 支持 Marker
sScatter.Marker = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.Diamond;   // ✅ 显示菱形标记
sScatter.Width  = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Middle;
sScatter.Color  = System.Drawing.Color.OrangeRed;
sScatter.Name   = "峰值点";
this.easyChartX2.LineSeries.Add(sScatter);

// ❌ 常见错误：FastLine + Marker，Marker 不会显示（不报错但无效）
// sAnalog.Type   = FastLine;
// sAnalog.Marker = MarkerType.Diamond;  // ← 无效！FastLine 不渲染 Marker

// ── 场景 3：数字信号（StepLine，阶梯显示）──────────────────────────────────
// 在 Designer.cs 中：
SeeSharpTools.JY.GUI.EasyChartXSeries sDigital = new SeeSharpTools.JY.GUI.EasyChartXSeries();
sDigital.Type   = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.StepLine;    // ✅ 阶梯线
sDigital.Marker = SeeSharpTools.JY.GUI.EasyChartXSeries.MarkerType.None;
sDigital.Color  = System.Drawing.Color.LimeGreen;
sDigital.Name   = "数字量";
this.easyChartX3.LineSeries.Add(sDigital);
// 运行时也可批量设置（读取所有通道后统一改线型）
for (int i = 0; i < easyChartX3.SeriesCount; i++)
{
    easyChartX3.Series[i].Type = EasyChartXSeries.LineType.StepLine;  // Series[] 等价于 LineSeries[]
}

// ── 场景 4：运行时修改系列样式（SeriesCount 简洁写法）────────────────────────
easyChartX4.SeriesCount = 3;  // 自动创建 3 个系列（默认 FastLine）
// 改第 0 个为带标记点的 Line
easyChartX4.LineSeries[0].Type   = EasyChartXSeries.LineType.Line;
easyChartX4.LineSeries[0].Marker = EasyChartXSeries.MarkerType.Circle;
easyChartX4.LineSeries[0].Color  = Color.Blue;
// 改第 1 个为阶梯线
easyChartX4.LineSeries[1].Type   = EasyChartXSeries.LineType.StepLine;
easyChartX4.LineSeries[1].Color  = Color.Green;
// 第 2 个保持默认 FastLine
easyChartX4.LineSeries[2].Color  = Color.Red;

// ── 场景 5：实用导出功能（图表内置）─────────────────────────────────────────
easyChartX1.SaveAsCsv();     // 弹出对话框，将当前图表数据导出为 CSV
easyChartX1.SaveAsImage();   // 弹出对话框，将当前图表截图导出为图片
easyChartX1.Clear();         // 清除已绘数据（保留系列配置，不需要重新 Add 系列）
```

---

### EX-K2：LineType.Bar 柱状图完整示例（单系列 / 多系列并列 / 分组展示）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// Bar 柱状图使用规则速记：
//   LineType.Bar  → 纵向柱状图，X 轴为类别索引，Y 轴为柱高
//   多系列       → 柱子自动并列（Clustered Bar）
//   Bar 不支持 Marker，设置了也不显示
//   Plot(data) 即可绘出，无需额外 X 数组
// ══════════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Windows.Forms;
using SeeSharpTools.JY.GUI;

public class BarChartExample : Form
{
    private EasyChartX easyChartX1;
    private EasyChartX easyChartX2;
    private Button btnSingle, btnMulti;

    public BarChartExample()
    {
        // ───────────────────────────────────────────────────────────────────
        // 初始化 EasyChartX
        // ───────────────────────────────────────────────────────────────────
        easyChartX1 = new EasyChartX { Dock = DockStyle.Top, Height = 280 };
        easyChartX2 = new EasyChartX { Dock = DockStyle.Top, Height = 280 };
        btnSingle = new Button { Text = "单系列柱状图", Dock = DockStyle.Bottom };
        btnMulti  = new Button { Text = "多系列柱状图", Dock = DockStyle.Bottom };

        btnSingle.Click += BtnSingle_Click;
        btnMulti.Click  += BtnMulti_Click;

        this.Controls.AddRange(new Control[] { easyChartX2, easyChartX1, btnMulti, btnSingle });
        this.Text = "EasyChartX Bar 柱状图示例";
        this.Size = new Size(800, 700);
    }

    // ───────────────────────────────────────────────────────────────────────
    // 场景 1：单系列柱状图（各通道平均值对比）
    // ───────────────────────────────────────────────────────────────────────
    private void BtnSingle_Click(object sender, EventArgs e)
    {
        // 每个元素代表一个通道的平均幅度（V）
        double[] channelAvg = { 1.23, 3.45, 2.78, 4.90, 3.12, 2.56, 1.88, 3.67 };

        // 配置系列：使用 SeriesCount 最简单
        easyChartX1.SeriesCount = 1;
        easyChartX1.LineSeries[0].Type  = EasyChartXSeries.LineType.Bar;     // ✅ 设为柱状图
        easyChartX1.LineSeries[0].Color = Color.SteelBlue;
        easyChartX1.LineSeries[0].Name  = "平均幅度 (V)";

        // 坐标轴配置
        easyChartX1.AxisX.Title    = "通道";
        easyChartX1.AxisY.Title    = "平均幅度 (V)";
        easyChartX1.AxisY.AutoScale = true;

        // 绘图：Plot(data) 即可，X 轴自动为 0~7
        easyChartX1.Plot(channelAvg);

        // ℹ️ 如需自定义 X 轴标签，EasyChartX 未内置字符串类别轴，
        //    可用整数 X + ToolTip 控件实现标签覆盖
    }

    // ───────────────────────────────────────────────────────────────────────
    // 场景 2：多系列并列柱状图（三组数据对比）
    // ───────────────────────────────────────────────────────────────────────
    private void BtnMulti_Click(object sender, EventArgs e)
    {
        // 三测试条件下各指标对比
        double[] groupA = { 85.2, 72.1, 91.0, 66.4, 78.9 }; // 条件 A
        double[] groupB = { 79.5, 88.3, 65.7, 82.6, 93.1 }; // 条件 B
        double[] groupC = { 90.1, 67.8, 84.5, 75.3, 88.7 }; // 条件 C
        // 指标索引对应 0=响应时间, 1=吉尼系数, 2=THD, 3=模拟幅度, 4=干扰抑制

        // 初始化三个系列（方法一：SeriesCount + 运行时配置）
        easyChartX2.SeriesCount = 3;

        easyChartX2.LineSeries[0].Type  = EasyChartXSeries.LineType.Bar;
        easyChartX2.LineSeries[0].Color = Color.SteelBlue;
        easyChartX2.LineSeries[0].Name  = "条件 A";

        easyChartX2.LineSeries[1].Type  = EasyChartXSeries.LineType.Bar;
        easyChartX2.LineSeries[1].Color = Color.OrangeRed;
        easyChartX2.LineSeries[1].Name  = "条件 B";

        easyChartX2.LineSeries[2].Type  = EasyChartXSeries.LineType.Bar;
        easyChartX2.LineSeries[2].Color = Color.SeaGreen;
        easyChartX2.LineSeries[2].Name  = "条件 C";

        // 坐标轴配置
        easyChartX2.AxisX.Title    = "指标索引";
        easyChartX2.AxisY.Title    = "指标得分";
        easyChartX2.AxisY.AutoScale = true;

        // 多系列绘图：double[,] data，行数 = 系列数，列数 = 数据点数
        double[,] multiData = new double[3, 5];
        for (int col = 0; col < 5; col++)
        {
            multiData[0, col] = groupA[col];
            multiData[1, col] = groupB[col];
            multiData[2, col] = groupC[col];
        }
        // MajorOrder.Row：行优先，每行为一个系列
        easyChartX2.Plot(multiData, 0, 1, MajorOrder.Row);
    }

    // ───────────────────────────────────────────────────────────────────────
    // 场景 3：运行时动态切换线型（将现有折线图改为柱状图）
    // ───────────────────────────────────────────────────────────────────────
    private void SwitchToBar()
    {
        // 运行时将系列线型由默认 FastLine 改为 Bar
        // 注意：重新 Plot 后才会生效
        for (int i = 0; i < easyChartX1.SeriesCount; i++)
        {
            easyChartX1.LineSeries[i].Type = EasyChartXSeries.LineType.Bar;  // ✅ 动态切换
        }
        // 重新绘制，使线型变更生效
        double[] newData = { 5.1, 3.8, 7.2, 6.4, 4.9 };
        easyChartX1.Plot(newData);
    }

    // ───────────────────────────────────────────────────────────────────────
    // 场景 4：Designer.cs 初始化写法（必须用全限定名）
    // ───────────────────────────────────────────────────────────────────────
    // 在 InitializeComponent() 中（Designer.cs）：
    //
    // SeeSharpTools.JY.GUI.EasyChartXSeries barSeries1 = new SeeSharpTools.JY.GUI.EasyChartXSeries();
    // barSeries1.Type  = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.Bar;   // ✅ 必须全限定名
    // barSeries1.Color = System.Drawing.Color.SteelBlue;
    // barSeries1.Name  = "分类统计";
    // barSeries1.Width = SeeSharpTools.JY.GUI.EasyChartXSeries.LineWidth.Thin;
    // barSeries1.Visible = true;
    // barSeries1.XPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
    // barSeries1.YPlotAxis = SeeSharpTools.JY.GUI.EasyChartXAxis.PlotAxis.Primary;
    // this.easyChartX1.LineSeries.Add(barSeries1);  // ✅ 必须用 LineSeries.Add()
    // ❌ this.easyChartX1.SeriesCollection.Add(barSeries1);  // 此属性不存在

    // ───────────────────────────────────────────────────────────────────────
    // │ LineType 全部类型对比表（快速选型参考）
    // ───────────────────────────────────────────────────────────────────────
    // 枚举值    整数值  Marker  典型用途
    // FastLine  6       ❌      高刷新率模拟信号（默认）
    // Line      3       ✅      少量数据点 + 带标记
    // StepLine  5       ✅      数字信号、离散状态
    // Point     0       ✅      纯散点图
    // Spline    4       ✅      平滑曲线（少点位场景）
    // Bar       10      ❌      柱状图、直方图  ← 本样例重点
    // Area      13      ❌      面积图（填充区域）
}
```

---

## ★ Spectrum.PowerSpectrum 专项示例

### EX-L：PowerSpectrum 完整调用示例（最易出错场景汇总）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// PowerSpectrum 使用规则速记：
//   spectrum 长度 = N / 2（N = 输入信号长度）
//   必须预分配 new double[N/2]，传 ref
//   df 用 out 接收，不需要分配
//   推荐用 SpectrumUnits.dBV 直接输出 dBV（无需手动转换）
//   推荐加 WindowType.Hanning 窗函数减少泄漏
//   PeakSpectrumAnalysis 的第二个参数是 dt（采样间隔），不是 sampleRate
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.ArrayUtility;
using SeeSharpTools.JY.GUI;

// ── 场景 1：基本 PowerSpectrum（V² 功率谱，对数Y轴显示）────────────────────
private void BasicPowerSpectrum(double[] signal, double sampleRate)
{
    int N = signal.Length;
    double[] spectrum = new double[N / 2];  // ⚠️ 长度必须 = N/2，不是 N
    double df;                              // 频率分辨率，由 API 输出

    // 最简调用：默认 V² 单位，无窗
    Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df);
    // ⚠️ ref 和 out 不能省略，否则编译错误

    // 绘制频谱（X轴从 0 Hz 开始，步长 df Hz）
    easyChartX_freq.Plot(spectrum, 0, df);
    easyChartX_freq.AxisX.Title = string.Format("频率 (Hz)  [df={0:F3} Hz]", df);
    easyChartX_freq.AxisY.Title = "V²/Hz";
    easyChartX_freq.AxisY.IsLogarithmic = true;   // V² 单位可以用对数轴
    easyChartX_freq.AxisY.LogLabelStyle = EasyChartXAxis.LogarithmicLabelStyle.E2;
}

// ── 场景 2：SpectrumUnits.dBV 直接输出（⭐ 推荐，无需手动转换）─────────
private void PowerSpectrumWithDbV(double[] signal, double sampleRate)
{
    double[] spectrum = new double[signal.Length / 2];
    double df;

    // ✅ 直接输出 dBV，无需 20*log10(sqrt(power))
    Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df, SpectrumUnits.dBV);

    easyChartX_freq.Plot(spectrum, 0, df);
    easyChartX_freq.AxisX.Title = "频率 (Hz)";
    easyChartX_freq.AxisY.Title = "dBV";
    // ⚠️ 不要设 IsLogarithmic=true，dBV 已是对数尺度的线性表示
    easyChartX_freq.AxisY.IsLogarithmic = false;
}

// ── 场景 3：加窗函数减少频谱泄漏（Hanning 窗 + dBV）────────────────
private void PowerSpectrumWithWindow(double[] signal, double sampleRate)
{
    double[] spectrum = new double[signal.Length / 2];
    double df;

    // dBV + Hanning 窗（通用推荐组合）
    Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df,
        SpectrumUnits.dBV, WindowType.Hanning, 0, false);
    // 参数说明：
    //   SpectrumUnits.dBV  → 输出 dBV 单位
    //   WindowType.Hanning → 汉宁窗（通用推荐）
    //   0                  → 窗参数（Hanning 不用，传 0）
    //   false              → 功率谱（不是功率谱密度）

    easyChartX_freq.Plot(spectrum, 0, df);
}

// ── 场景 4：PeakSpectrumAnalysis 快速峰值分析───────────────────────
private void QuickPeakAnalysis(double[] signal, double sampleRate)
{
    // ⚠️ PeakSpectrumAnalysis 的第二个参数是 dt（采样间隔），不是 sampleRate！
    double dt = 1.0 / sampleRate;  // 采样间隔 (秒)
    double peakFreq, peakAmp;
    Spectrum.PeakSpectrumAnalysis(signal, dt, out peakFreq, out peakAmp);
    // peakAmp = 1.414 * RMS（峰值电压）

    // ✔️ 正确：PeakSpectrumAnalysis(signal, 1.0/sampleRate, ...)
    // ❌ 错误：PeakSpectrumAnalysis(signal, sampleRate, ...)  ← 会得到错误结果！

    label_PeakFreq.Text = string.Format("峰值频率: {0:F2} Hz", peakFreq);
    label_PeakAmp.Text  = string.Format("峰值幅值: {0:F4} V", peakAmp);
}

// ── 场景 5：dBFS 频谱（相对满量程，用于 ADC 动态范围分析）───────────
private void DbfsSpectrum(double[] signal, double sampleRate, double adcFullScale)
{
    double[] dbfsSpectrum = new double[signal.Length / 2];
    double df;

    // refFullScale 是 ADC 满量程幅值（V），例如 ±10V 范围传 10.0
    Spectrum.DBFullScaleSpectrum(signal, sampleRate, adcFullScale,
        ref dbfsSpectrum, out df);

    easyChartX_freq.Plot(dbfsSpectrum, 0, df);
    easyChartX_freq.AxisY.Title = "dBFS";
}

// ── 场景 6：完整频谱分析流程（信号生成 + 加窗 dBV 频谱 + 显示）───────
private void FullSpectrumAnalysis()
{
    const double sampleRate = 10000;
    const int dataLength = 4096;

    // 1. 生成含噪声的正弦波
    double[] signal = new double[dataLength];
    double[] noise  = new double[dataLength];
    Generation.SineWave(ref signal, 1.0, 0, 600, sampleRate);
    Generation.UniformWhiteNoise(ref noise, 0.1);
    ArrayCalculation.Add(signal, noise, ref signal);

    // 2. 计算频谱（dBV + Hanning 窗）
    double[] spectrum = new double[dataLength / 2];  // ⚠️ N/2
    double df;
    Spectrum.PowerSpectrum(signal, sampleRate, ref spectrum, out df,
        SpectrumUnits.dBV, WindowType.Hanning, 0, false);

    // 3. 显示时域 + 频域
    easyChartX_time.Plot(signal, 0, 1.0 / sampleRate);
    easyChartX_time.AxisX.Title = "时间 (s)";
    easyChartX_time.AxisY.Title = "幅值 (V)";

    easyChartX_freq.Plot(spectrum, 0, df);
    easyChartX_freq.AxisX.Title = string.Format("频率 (Hz)  [df={0:F3} Hz]", df);
    easyChartX_freq.AxisY.Title = "dBV";
    easyChartX_freq.AxisY.IsLogarithmic = false;  // dBV 不用对数轴
}
```

---

## ★ DSP 分析功能专项示例

### EX-M：FIR / IIR / ZeroPhase 滤波器完整示例

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// 滤波器使用规则速记：
//   SeeSharpTools 只执行滤波（FIRFilter/IIRFilter/ZeroPhaseFilter）
//   滤波器系数需用 MathNet.Filtering.dll 生成
//   FIRFilter 返回 double[]（长度可能 < 输入长度）
//   ZeroPhaseFilter 需要 forwardCoeff + reverseCoeff 两组系数
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;
using MathNet.Filtering.FIR;

// ── 场景 1：FIR 低通 / 高通 / 带通 ────────────────────────────────────
private void FirFilterVariants(double[] signal, double sampleRate)
{
    // MathNet 生成滤波器系数
    double[] lpCoeff = FirCoefficients.LowPass(sampleRate, 500, 64);
    double[] hpCoeff = FirCoefficients.HighPass(sampleRate, 2000, 64);
    double[] bpCoeff = FirCoefficients.BandPass(sampleRate, 200, 2000, 128);

    // SeeSharpTools 执行滤波（返回 double[]）
    double[] lpFiltered = SignalOperation.Filtering.FIRFilter(signal, lpCoeff);
    double[] hpFiltered = SignalOperation.Filtering.FIRFilter(signal, hpCoeff);
    double[] bpFiltered = SignalOperation.Filtering.FIRFilter(signal, bpCoeff);
    // ⚠️ 输出长度 ≤ 输入长度（取决于系数长度）
}

// ── 场景 2：ZeroPhase 零相位滤波（推荐用于后处理分析）────────────────
private void ZeroPhaseExample(double[] signal, double sampleRate)
{
    double[] firCoeff = FirCoefficients.LowPass(sampleRate, 500, 64);
    // ZeroPhaseFilter 需要 forward + reverse 两组系数
    // 用 FIR 做零相位：forward = firCoeff, reverse = {1.0}
    double[] filtered = SignalOperation.Filtering.ZeroPhaseFilter(
        signal, firCoeff, new double[] { 1.0 });
    // ✅ 无相位延迟，适合波形对比分析
    // ⚠️ 未做边界延拓，首尾可能失真
}

// ── 场景 3：滤波前后频谱对比 ──────────────────────────────────────────
private void FilterSpectrumCompare(double[] signal, double sampleRate)
{
    double[] firCoeff = FirCoefficients.LowPass(sampleRate, 500, 64);
    double[] filtered = SignalOperation.Filtering.FIRFilter(signal, firCoeff);

    double[] specOrig = new double[signal.Length / 2];
    double[] specFilt = new double[filtered.Length / 2];
    double df1, df2;
    Spectrum.PowerSpectrum(signal, sampleRate, ref specOrig, out df1, SpectrumUnits.dBV);
    Spectrum.PowerSpectrum(filtered, sampleRate, ref specFilt, out df2, SpectrumUnits.dBV);

    easyChartX_origSpec.Plot(specOrig, 0, df1);
    easyChartX_filtSpec.Plot(specFilt, 0, df2);
}
```

---

### EX-N：MedianFilter 去除脉冲噪声

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

private void MedianFilterDeglitch()
{
    const double sampleRate = 10000;
    const int N = 4096;

    // 生成干净正弦波 + 随机脉冲毛刺
    double[] signal = new double[N];
    Generation.SineWave(ref signal, 1.0, 0, 50, sampleRate);
    Random rnd = new Random(42);
    for (int i = 0; i < 20; i++)
    {
        int idx = rnd.Next(N);
        signal[idx] += (rnd.NextDouble() > 0.5 ? 5.0 : -5.0);
    }

    // 方法 1：MedianFilter 类（⚠️ windowLength 必须奇数 ≥ 3）
    MedianFilter mf = new MedianFilter();
    double[] cleaned1 = mf.Process(signal, windowLength: 5);

    // 方法 2：SignalOperation.Filtering.MedianFilter（指定左右阶数）
    double[] cleaned2 = SignalOperation.Filtering.MedianFilter(
        signal, leftRank: 2, rightRank: 2);
    // 窗口大小 = leftRank + rightRank + 1 = 5

    easyChartX_raw.Plot(signal, 0, 1.0 / sampleRate);
    easyChartX_clean.Plot(cleaned1, 0, 1.0 / sampleRate);
}
```

---

### EX-O：信号操作（自相关 / 互相关 / 重采样 / 去趋势 / 归一化）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

// ── 场景 1：自相关检测信号周期 ────────────────────────────────────────
private void AutoCorrelationPeriod(double[] signal, double sampleRate)
{
    double[] autoCorr = SignalOperation.Analog.AutoCorrelation(
        signal, Normalization.Biased);
    // autoCorr 第一个非零峰位置 = 信号周期（采样点数）
    // 周期(秒) = 峰位置 / sampleRate
    easyChartX1.Plot(autoCorr, 0, 1.0 / sampleRate);
}

// ── 场景 2：互相关测量两信号延迟 ──────────────────────────────────────
private void CrossCorrelationDelay(double[] sig1, double[] sig2)
{
    double[] crossCorr = SignalOperation.Analog.CrossCorrelation(
        sig1, sig2, Normalization.None);
    // crossCorr 最大值位置 → 延迟（采样点数）
    easyChartX1.Plot(crossCorr);
}

// ── 场景 3：重采样（改变采样率）───────────────────────────────────────
private void ResampleExample(double[] signal, double origRate, double newRate)
{
    double[] resampled = SignalOperation.Analog.Resampling(
        signal, origRate, newRate, InterpolationMode.Spline);
    easyChartX1.Plot(resampled, 0, 1.0 / newRate);
}

// ── 场景 4：降采样 ────────────────────────────────────────────────────
private void DownsampleExample(double[] signal, double sampleRate)
{
    int factor = 4;
    double[] downsampled = SignalOperation.Analog.Downsample(
        signal, factor, averaging: true);
    // averaging=true: 窗口内取平均（抗混叠）; false: 直接抽取
    easyChartX1.Plot(downsampled, 0, factor / sampleRate);
}

// ── 场景 5：去趋势（去除直流偏移和漂移）───────────────────────────────
private void DetrendExample(double[] signal)
{
    double[] detrended, trend;
    SignalOperation.Analog.Detrend(signal, out detrended, out trend,
        DetrendMethod.Linear, order: 1);
    // detrended = 去除线性漂移后的信号
    // trend = 拟合的趋势线
    easyChartX_detrended.Plot(detrended);
    easyChartX_trend.Plot(trend);
}

// ── 场景 6：归一化 + 缩放 ────────────────────────────────────────────
private void ScaleNormalizeExample(double[] signal)
{
    // 统计标准化（均值0, 标准差1）
    double stdDev, mean;
    double[] normalized = SignalOperation.Analog.Normalize(
        signal, out stdDev, out mean);

    // 缩放至 [-1, 1]
    double scale, offset;
    double[] scaled = SignalOperation.Analog.Scale(
        signal, out scale, out offset);
}
```

---

### EX-P：完整谐波分析（THD / SINAD / SNR / SFDR）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// HarmonicAnalysis 使用规则速记：
//   ⚠️ dt = 1.0 / sampleRate（采样间隔，秒），不是采样率！
//   THDAnalysis → thd 是比值（0.05 = 5%），不是 dB、不是百分比
//   componentsLevel: [0]=DC, [1]=基波, [2]=2次谐波... 单位峰值电压(V)
//   命名空间：SeeSharpTools.JY.DSP.Measurements
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Measurements;
using SeeSharpTools.JY.ArrayUtility;

// ── 场景 1：完整 ADC 性能评估 ─────────────────────────────────────────
private void FullAdcEvaluation()
{
    const double sampleRate = 100000;
    const int N = 100000;
    double dt = 1.0 / sampleRate;  // ⚠️ 采样间隔！
    // ✔️ 正确：dt = 1.0 / 100000 = 0.00001
    // ❌ 错误：dt = 100000  ← 结果完全错误！

    // 生成含谐波+噪声的测试信号
    double[] signal = new double[N];
    double[] h2 = new double[N], h3 = new double[N], noise = new double[N];
    Generation.SineWave(ref signal, 10.0, 0, 1000, sampleRate);
    Generation.SineWave(ref h2, 0.5, 0, 2000, sampleRate);
    Generation.SineWave(ref h3, 0.2, 0, 3000, sampleRate);
    Generation.UniformWhiteNoise(ref noise, 0.05);
    ArrayCalculation.Add(signal, h2, ref signal);
    ArrayCalculation.Add(signal, h3, ref signal);
    ArrayCalculation.Add(signal, noise, ref signal);

    double freq; 
    // ⚠️ 所有 componentsLevel 必须预先 new！长度 = highestHarmonic + 1
    double[] levels = new double[11];
    
    // THD 分析（返回比值，非百分比）
    double thd;
    HarmonicAnalysis.THDAnalysis(signal, dt, out freq, out thd, ref levels, 10);
    label_THD.Text = string.Format("THD: {0:F4}%", thd * 100);  // ⚠️ 乘 100 转百分比
    // levels: [0]=DC, [1]=基波(Vpk), [2]=2次谐波...
    
    // SINAD 分析（返回 dB）
    double sinad; double[] sinadLevels = new double[11];
    HarmonicAnalysis.SINADAnalysis(signal, dt, out freq, out sinad, ref sinadLevels, 10);
    label_SINAD.Text = string.Format("SINAD: {0:F2} dB", sinad);
    
    // SNR 分析（返回 dB）
    double snr; double[] snrLevels = new double[11];
    HarmonicAnalysis.SNRAnalysis(signal, dt, out freq, out snr, ref snrLevels, 10);
    label_SNR.Text = string.Format("SNR: {0:F2} dB", snr);
    
    // SFDR 分析（返回 dB）
    double sfdr; double[] sfdrLevels = new double[11];
    HarmonicAnalysis.SFDRAnalysis(signal, dt, out freq, out sfdr, ref sfdrLevels, 10);
    label_SFDR.Text = string.Format("SFDR: {0:F2} dB", sfdr);
    
    // componentsLevel 说明
    // levels[0] = DC 分量, levels[1] = 基波幅值 (Vpk), levels[2] = 2次谐波...
    // 单位：峰値电压 V（= 1.414 × RMS）
}

// ── 场景 2：BasicAnalysis 功率分解详情 ────────────────────────────────
private void BasicAnalysisExample(double[] signal, double sampleRate)
{
    double dt = 1.0 / sampleRate;
    double freq, fundPower, harmPower, noisePower, secondPeak;
    // ⚠️ basicLevels 是 ref 参数，必须预先 new
    double[] levels = new double[11];

    HarmonicAnalysis.BasicAnalysis(signal, dt, out freq,
        out fundPower, out harmPower, out noisePower, out secondPeak,
        ref levels, highestHarmonic: 10);

    // fundPower: 基波功率 (V²)
    // harmPower: 所有谐波总功率 (V²)
    // noisePower: 噪声功率 (V²)
    // secondPeak: 最大非基波/非DC功率 (V²)
    // 手动计算 SNR (dB) = 10*log10(基波功率/噪声功率)
    double snrCalc = 10 * Math.Log10(fundPower / noisePower);
}

// ── 场景 3：THDAnalysis 含相位信息（扩展重载）─────────────────────
private void THDWithPhase(double[] signal, double sampleRate)
{
    double dt = 1.0 / sampleRate;  // ⚠️ 采样间隔，不是采样率
    double freq, thd;
    // ⚠️ levels 、phases 均是 ref 参数，必须预先 new
    double[] levels = new double[6];  // highestHarmonic=5, 长度=6
    double[] phases = new double[6];

    // 扩展重载：同时输出各次谐波的相位
    HarmonicAnalysis.THDAnalysis(
        signal, dt,
        out freq,           // 检测到的基波频率 (Hz)
        out thd,            // THD 比值（非 %，非 dB）
        ref levels,         // ⚠️ ref（不是 out）：[0]=DC,[1]=基波,[2]=2次... (Vpk)
        ref phases,         // ⚠️ ref（不是 out）：[0]=DC,[1]=基波,[2]=2次... (弧度)
        highestHarmonic: 5,
        autoFundamentalFreqDetection: true
    );

    // 结果输出
    label_Freq.Text = string.Format("基波: {0:F2} Hz", freq);
    label_THD.Text  = string.Format("THD: {0:F4}%", thd * 100);  // ⚠️ 比值→百分比
    label_THDdB.Text = string.Format("THD: {0:F2} dB", 20 * Math.Log10(thd));

    // 各次谐波幅值与相位
    for (int i = 1; i <= 5; i++)
    {
        double ampVpk  = levels[i];                         // 幅值 (Vpk)
        double ampVrms = levels[i] / Math.Sqrt(2.0);        // 幅值 (Vrms)
        double phaseDeg = phases[i] * 180.0 / Math.PI;      // 相位 (度)
        Console.WriteLine("H{0}: {1:F4} Vpk = {2:F4} Vrms, 相位 {3:F2}°",
            i, ampVpk, ampVrms, phaseDeg);
    }

    // 注意：levels[0] = DC 分量（不是基波！），phases[0] = DC 相位
    label_DC.Text = string.Format("DC: {0:F4} V", levels[0]);
}

// ── 场景 4：FullHarmonicAnalysis + 奈奎斯特验证 ───────────────────
private void FullHarmonicWithValidation(double[] signal, double sampleRate)
{
    double dt = 1.0 / sampleRate;
    double freq;

    // 先确定安全的最高谐波次数（避免超过奈奎斯特）
    double estimatedFundFreq = 1000.0;  // 预估基波频率（Hz）
    double nyquist = sampleRate / 2.0;
    int maxSafeHarmonic = (int)(nyquist / estimatedFundFreq) - 1;
    int highestHarmonic = Math.Min(10, maxSafeHarmonic);  // 不超过安全上限
    Console.WriteLine("安全最高谐波次数: {0}（奈奎斯特 {1:F0} Hz）",
        highestHarmonic, nyquist);

    // ⚠️ 必须预先分配！长度 = highestHarmonic + 1
    double[] allComponents = new double[highestHarmonic + 1];

    // FullHarmonicAnalysis：返回 HarmonicAnalysisResult + 各次谐波幅值
    HarmonicAnalysisResult result = HarmonicAnalysis.FullHarmonicAnalysis(
        signal, dt,
        out freq,              // 检测到的基波频率 (Hz)
        ref allComponents,     // ⚠️ ref（不是 out）！必须预先 new
        highestHarmonic        // 分析到第 n 次谐波
    );

    // 直接使用返回的全部指标（均为 dB）
    Console.WriteLine("检测基波: {0:F2} Hz", freq);
    Console.WriteLine("THD:   {0:F2} dB",  result.THD);
    Console.WriteLine("SINAD: {0:F2} dB",  result.SINAD);
    Console.WriteLine("SNR:   {0:F2} dB",  result.SNR);
    Console.WriteLine("SFDR:  {0:F2} dBc", result.SFDR);
    Console.WriteLine("ENOB:  {0:F2} bits",result.ENOB);

    // 输出各次谐波分量
    for (int i = 1; i <= highestHarmonic; i++)
    {
        double harmonicFreq = freq * i;
        double ampVpk = allComponents[i];
        // 超过奈奎斯特的谐波幅值为 0（自动置零，不报错）
        bool valid = (harmonicFreq < nyquist);
        Console.WriteLine("H{0} ({1:F0} Hz): {2:F4} Vpk {3}",
            i, harmonicFreq, ampVpk, valid ? "" : "[超奈奎斯特，已置0]");
    }
}
```

---

### EX-Q：ToneAnalyzer 与 ToneAnalysis 参数对比

```csharp
// ⚠️ 关键区别：两个 API 的采样参数含义相反！
//   HarmonicAnalysis.ToneAnalysis(data, dt, ...)  → dt = 1.0/sampleRate（采样间隔）
//   ToneAnalyzer.SingleToneAnalysis(data, Fs, ...) → Fs = sampleRate（采样率）
//   混用会导致结果差 N² 倍！
using SeeSharpTools.JY.DSP.Measurements;
using SeeSharpTools.JY.DSP.Utility;

private void CompareToneAPIs(double[] signal, double sampleRate)
{
    // 方法 1：HarmonicAnalysis.ToneAnalysis（dt = 采样间隔）
    double freq, amplitude, phase;
    HarmonicAnalysis.ToneAnalysis(
        signal,
        dt: 1.0 / sampleRate,  // ⚠️ 采样间隔 = 1/Fs
        out freq, out amplitude, out phase,
        initialGuess: 1000, searchRange: 500);

    // 方法 2：ToneAnalyzer.SingleToneAnalysis（Fs = 采样率）
    ToneInfo toneInfo = ToneAnalyzer.SingleToneAnalysis(
        signal,
        Fs: sampleRate,  // ⚠️ 采样率（与上面相反！）
        initialGuess: 1000, searchRange: 500);

    label1.Text = string.Format("ToneAnalysis: {0:F2} Hz, {1:F4} V", freq, amplitude);
    label2.Text = string.Format("ToneAnalyzer: {0:F2} Hz, {1:F4} V",
        toneInfo.Frequency, toneInfo.Amplitude);
}
```

---

### EX-R：峰谷检测（PeakValleyAnalysis）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

private void PeakDetection(double[] signal, double sampleRate)
{
    // 构造位置向量（时间轴）
    double[] xAxis = new double[signal.Length];
    for (int i = 0; i < signal.Length; i++)
        xAxis[i] = i / sampleRate;

    // 查找峰值
    double[,] peaks = PeakValleyAnalysis.FindPeaks(
        signal, xAxis,
        minPeakHeight: 0.5,       // 最小峰高
        minPeakDistance: 0.005,   // 最小峰间距（与 x 同单位）
        threshold: 0.1,           // 峰与邻点最小高差
        FlatSelection.Center,     // 平台区取中心
        isValley: false);         // false=峰, true=谷
    // 返回 double[peakCount, 2]：列0=峰值高度, 列1=峰值位置

    int peakCount = peaks.GetLength(0);
    for (int i = 0; i < peakCount; i++)
        Console.WriteLine("峰 {0}: 高度={1:F3}V, 时间={2:F6}s",
            i, peaks[i, 0], peaks[i, 1]);

    // 查找谷值（isValley=true）
    double[,] valleys = PeakValleyAnalysis.FindPeaks(
        signal, xAxis, -0.5, 0.005, 0.1, FlatSelection.Center, isValley: true);
}
```

---

### EX-S：相位差测量（Phase.CalPhaseShift）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

private void PhaseMeasurement()
{
    const double sampleRate = 10000;
    const int N = 4096;

    // 生成两路信号，已知相位差 45°
    double[] sig1 = new double[N];
    double[] sig2 = new double[N];
    Generation.SineWave(ref sig1, 1.0, 0, 100, sampleRate);
    Generation.SineWave(ref sig2, 1.0, 45, 100, sampleRate);

    // 测量相位差（基于希尔伯特变换）
    double phaseShift = Phase.CalPhaseShift(sig1, sig2);
    // 返回 -180° ~ 180°
    // ⚠️ 不需要 sampleRate 或 frequency 参数

    label_Phase.Text = string.Format("相位差: {0:F2}°（预期 ≈ 45°）", phaseShift);
}
```

---

### EX-T：方波参数测量（SquarewaveMeasurements）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

private void SquarewaveMeasure()
{
    const double sampleRate = 100000;
    const int N = 10000;

    double[] squareWave = new double[N];
    Generation.SquareWave(ref squareWave, 3.3, 1000, 0.6, sampleRate);

    // 测量方波参数
    SquarewaveMeasurements swm = new SquarewaveMeasurements();
    swm.SetWaveform(squareWave);

    double highLevel = swm.GetHighStateLevel();   // 高电平
    double lowLevel  = swm.GetLowStateLevel();    // 低电平
    double period    = swm.GetPeriod();           // 周期（采样点数）

    label_High.Text   = string.Format("高电平: {0:F3} V", highLevel);
    label_Low.Text    = string.Format("低电平: {0:F3} V", lowLevel);
    label_Period.Text = string.Format("周期: {0:F1} 点 ({1:F1} Hz)",
        period, sampleRate / period);

    // 两路方波相位差
    double[] squareWave2 = new double[N];
    Generation.SquareWave(ref squareWave2, 3.3, 1000, 0.6, sampleRate);
    swm.SetWaveform2(squareWave2);
    double phase = swm.GetPhase();
    label_Phase.Text = string.Format("方波相位差: {0:F2}°", phase);
}
```

---

### EX-U：多通道同步（Synchronizer）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

private void SynchronizerExample()
{
    const double sampleRate = 10000;
    const int N = 4096;

    // 模拟两路采样不同步的通道
    double[] ch1 = new double[N];
    double[] ch2 = new double[N];
    Generation.SineWave(ref ch1, 1.0, 0, 100, sampleRate);
    Generation.SineWave(ref ch2, 1.0, 30, 100, sampleRate);  // 30° 相位差

    // 构造 [channels, samples] 二维数组
    double[,] multiCh = new double[2, N];
    for (int i = 0; i < N; i++)
    {
        multiCh[0, i] = ch1[i];
        multiCh[1, i] = ch2[i];
    }

    // 同步（仅适用于带限信号如正弦波）
    Synchronizer sync = new Synchronizer();
    double[,] synced = sync.Sync(multiCh);
    // ⚠️ synced 列数 < N（截断了稳定点）

    // 验证同步效果
    int syncLen = synced.GetLength(1);
    double[] syncCh1 = new double[syncLen];
    double[] syncCh2 = new double[syncLen];
    for (int i = 0; i < syncLen; i++)
    {
        syncCh1[i] = synced[0, i];
        syncCh2[i] = synced[1, i];
    }
    double phaseAfter = Phase.CalPhaseShift(syncCh1, syncCh2);
    label_SyncResult.Text = string.Format(
        "同步后相位差: {0:F2}°（应接近 0°）", phaseAfter);
}
```

---

### EX-V：HarmonicAnalysis.FullHarmonicAnalysis（替代已淘汰的 HarmonicAnalyzer）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// ✅ 新版谐波分析（SeeSharpTools.JY.DSP.Measurements）使用规则：
//   ★ HarmonicAnalyzer.ToneAnalysis（SoundVibration）已淘汰
//   ★ 改用 HarmonicAnalysis.FullHarmonicAnalysis 获取 THD/SINAD/SNR/ENOB/SFDR
//   ⚠️ components 必须预分配：new double[highestHarmonic + 1]
//   ⚠️ dt = 1.0/sampleRate（采样间隔，不是采样率）
//   ⚠️ HarmonicAnalysis.ToneAnalysis 是单音频率/幅值测量，不是谐波指标替代
//   命名空间：SeeSharpTools.JY.DSP.Measurements
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Measurements;
using SeeSharpTools.JY.Mathematics; // ArrayArithmetic

// ── 场景 1：FullHarmonicAnalysis 全指标分析（取代旧版 HarmonicAnalyzer）────
private void FullHarmonicAnalysis_Example()
{
    const int N = 100000;
    const double sampleRate = 100000.0;
    double[] baseData  = new double[N];
    double[] harmonics = new double[N];
    double[] data      = new double[N];

    // 生成含二次谐波的信号
    Generation.SineWave(ref baseData, 10.0, 0, 50.0, sampleRate);       // 基波 50Hz
    Generation.SineWave(ref harmonics, 0.1, 0, 100.0, sampleRate);      // 2次谐波
    ArrayArithmetic.Add(baseData, harmonics, ref data);

    double dt = 1.0 / sampleRate;         // ⚠️ 采样间隔
    double fundFreq;
    double[] components = new double[11]; // ⚠️ 必须预分配，长度 = highestHarmonic + 1

    // ★ 新版写法：一次调用获取所有指标
    HarmonicAnalysisResult result = HarmonicAnalysis.FullHarmonicAnalysis(
        data, dt, out fundFreq, ref components, highestHarmonic: 10);

    label_THD.Text   = string.Format("THD: {0:F8}", result.THD);
    label_SINAD.Text = string.Format("SINAD: {0:F2}", result.SINAD);
    label_SNR.Text   = string.Format("SNR: {0:F2}", result.SNR);
    label_ENOB.Text  = string.Format("ENOB: {0:F2} bits", result.ENOB);
    label_SFDR.Text  = string.Format("SFDR: {0:F2}", result.SFDR);       // 新增
    label_Fund.Text  = string.Format("基波: {0:F2} Hz, {1:F4} Vpk", fundFreq, components[1]);
    // ⚠️ result 无 THDplusN 和 NoiseFloor（如仍需这两项请保留旧版调用）
}

// ── 场景 2：只需 THD 时直接调用 THDAnalysis ──────────────────────────────
private void THDOnly_Example(double[] signal, double sampleRate)
{
    double dt = 1.0 / sampleRate;
    double fundFreq, thd;
    double[] levels = new double[11]; // ⚠️ ref 参数，必须预先 new
    HarmonicAnalysis.THDAnalysis(signal, dt, out fundFreq, out thd, ref levels, 10);
    label_THD.Text = string.Format("THD: {0:F6}（比值）/ {1:F2} dB",
        thd, 20 * Math.Log10(thd));
    label_Fund.Text = string.Format("基波: {0:F2} Hz", fundFreq);
}

// ── 场景 3：新旧 API 对比（迁移参考）─────────────────────────────────────
private void MigrationComparison(double[] signal, double sampleRate)
{
    double dt = 1.0 / sampleRate;
    double fundFreq;
    double[] components = new double[11];

    // ✅ 新版（推荐）：SeeSharpTools.JY.DSP.Measurements.HarmonicAnalysis
    HarmonicAnalysisResult newResult = HarmonicAnalysis.FullHarmonicAnalysis(
        signal, dt, out fundFreq, ref components, highestHarmonic: 10);
    // newResult.THD / .SINAD / .SNR / .ENOB / .SFDR

    // ❌ 旧版（已淘汰）：SeeSharpTools.JY.DSP.SoundVibration.HarmonicAnalyzer
    // ToneAnalysisResult oldResult = HarmonicAnalyzer.ToneAnalysis(signal, dt, 10, false);
    // oldResult.THD / .THDplusN / .SINAD / .SNR / .NoiseFloor / .ENOB
    // ★ 若需 THDplusN 或 NoiseFloor，且尚无新替代，才保留旧版调用
}
```

---

### EX-W：AdvanceComplexFFT 复数频谱（幅度谱 + 相位谱）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// AdvanceComplexFFT 使用规则：
//   ★ 输出数组长度 = N/2 + 1（不是 N/2！）
//   ★ 必须预分配 new Complex[N/2 + 1]
//   ★ 传 ref 参数
//   ★ 输出包含幅度 (Magnitude) 和相位 (Phase) 信息
//   命名空间：SeeSharpTools.JY.DSP.Fundamental + System.Numerics
// ══════════════════════════════════════════════════════════════════════════════
using System.Numerics;
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.ArrayUtility;

private void AdvanceComplexFFTExample()
{
    const int sampleRate = 10000;
    const int N = sampleRate;  // 1 秒数据
    const double frequency1 = 100;
    const double amplitude1 = 2;
    const double frequency2 = 800;
    const double amplitude2 = 1;

    // 生成混合信号：正弦波 + 方波
    double[] data1 = new double[N];
    double[] data2 = new double[N];
    double[] waveform = new double[N];
    Generation.SineWave(ref data1, amplitude1, 0, frequency1, sampleRate);
    Generation.SquareWave(ref data2, amplitude2, 50, frequency2, sampleRate);
    ArrayCalculation.Add(data1, data2, ref waveform);

    // ★ 关键：复数频谱数组长度 = N/2 + 1
    Complex[] spectrum = new Complex[N / 2 + 1];
    // ⚠️ 不是 N/2！与 PowerSpectrum 不同
    // ⚠️ 不能传 null，否则 NullReferenceException

    Spectrum.AdvanceComplexFFT(waveform, WindowType.Hanning, ref spectrum);

    // 提取幅度谱和相位谱
    double[,] spectrumData = new double[2, spectrum.Length];
    for (int i = 0; i < spectrum.Length; i++)
    {
        spectrumData[0, i] = spectrum[i].Magnitude / spectrum.Length;  // 归一化幅度
        spectrumData[1, i] = spectrum[i].Phase;                        // 相位 (弧度)
    }

    // SplitView 分图显示幅度谱和相位谱
    easyChartX_spectrum.SplitView = true;
    easyChartX_spectrum.Plot(spectrumData);

    // ✔️ AdvanceComplexFFT 输出点数 = N/2+1
    // ❌ 常见错误：new Complex[N/2] → IndexOutOfRangeException
}
```

---

### EX-X：WaveformMeasurements 完整方波分析（新版 API）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// WaveformMeasurements (SeeSharpTools.JY.DSP.Measurements) 使用规则：
//   ★ 新版静态 API，功能比旧版 SquarewaveMeasurements (Utility) 更全面
//   ⚠️ dt = 1.0 / sampleRate（采样间隔，不是采样率！）
//   ⚠️ pulseNumber / cycleNumber 从 1 开始，不是 0
//   ⚠️ 所有测量方法应包裹 try-catch（信号质量差时可能抛异常）
//   命名空间：SeeSharpTools.JY.DSP.Measurements
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Measurements;

private const double sampleRate = 1000;
private double _increment => 1.0 / sampleRate;

// ── 步骤 1：配置高低电平和参考电平设置 ────────────────────────
private HighLowStateSetting _stateSetting;
private PulseReferenceLevel _refLevelSetting;

private void InitSettings()
{
    _stateSetting = new HighLowStateSetting()
    {
        Method = HighLowStateMethod.HistogramMode,
        HistogramSize = 256
    };

    _refLevelSetting = new PulseReferenceLevel()
    {
        High = 0.9,
        Low = 0.1,
        Middle = 0.5,
        Unit = PulseReferenceUnit.Percentage
    };
}

// ── 步骤 2：AmplitudeLevelAnalysis（幅度电平分析）──────────────────
private void AnalyzeAmplitude(double[] signal)
{
    try
    {
        double amplitude, highLevel, lowLevel;
        SquarewaveMeasurements.AmplitudeLevelAnalysis(
            signal, _stateSetting,
            out amplitude, out highLevel, out lowLevel);

        label_Amp.Text  = string.Format("幅值: {0:F6}", amplitude);
        label_High.Text = string.Format("高电平: {0:F6}", highLevel);
        label_Low.Text  = string.Format("低电平: {0:F6}", lowLevel);
    }
    catch (Exception ex)
    {
        label_Amp.Text = "Error: " + ex.Message;
    }
}

// ── 步骤 3：PulseMeasurement（脉冲测量）────────────────────────
private void MeasurePulse(double[] signal)
{
    try
    {
        double period, pulseWidth, dutyCycle;
        PulseMeasurementInfo info;
        SquarewaveMeasurements.PulseMeasurement(
            signal, _increment, _stateSetting,
            PulsePolarity.High, _refLevelSetting,
            1,   // ⚠️ pulseNumber 从 1 开始（不是 0）
            out period, out pulseWidth, out dutyCycle, out info);

        label_Period.Text   = string.Format("周期: {0:F6} s", period);
        label_Pulse.Text    = string.Format("脉宽: {0:F6} s", pulseWidth);
        label_Duty.Text     = string.Format("占空比: {0:F6}", dutyCycle);
        // info.PulseCenter 可用于在图表上标记脉冲位置
    }
    catch (Exception ex)
    {
        label_Period.Text = "Error: " + ex.Message;
    }
}

// ── 步骤 4：PeriodAnalysis（周期统计分析）───────────────────────
private void AnalyzePeriod(double[] signal)
{
    try
    {
        PeriodAnalysisResult periodResult, dutyCycleResult, pulseResult;
        MeasurementInfo info;
        SquarewaveMeasurements.PeriodAnalysis(
            signal, _increment, _stateSetting,
            PulsePolarity.High, _refLevelSetting,
            out periodResult, out dutyCycleResult, out pulseResult, out info);

        // PeriodAnalysisResult 包含 Count, Minimum, Maximum, Average
        label_PMax.Text = string.Format("周期 Max: {0:F6} s", periodResult.Maximum);
        label_PMin.Text = string.Format("周期 Min: {0:F6} s", periodResult.Minimum);
        label_PAvg.Text = string.Format("周期 Avg: {0:F6} s", periodResult.Average);

        // 占空比是 0~1 范围，乘 100 转百分比
        label_DutyAvg.Text = string.Format("占空比 Avg: {0:F3}%",
            dutyCycleResult.Average * 100);
    }
    catch (Exception ex)
    {
        label_PMax.Text = "Error: " + ex.Message;
    }
}

// ── 步骤 5：CycleRmsMeanAnalysis（周期 RMS/均值）─────────────────
private void AnalyzeCycleRms(double[] signal)
{
    try
    {
        double rms, mean;
        MeasurementInfo info;
        SquarewaveMeasurements.CycleRmsMeanAnalysis(
            signal, _increment, _stateSetting, _refLevelSetting,
            1,  // ⚠️ cycleNumber 从 1 开始
            out rms, out mean, out info);

        label_RMS.Text  = string.Format("Cycle RMS: {0:F6}", rms);
        label_Mean.Text = string.Format("Cycle Mean: {0:F6}", mean);
    }
    catch (Exception ex)
    {
        label_RMS.Text = "Error: " + ex.Message;
    }
}

// ── 步骤 6：TransitionMeasurement（跳变/边沿测量）─────────────────
private void MeasureTransition(double[] signal)
{
    try
    {
        double slope, transitionDuration;
        TransitionInfo preTransInfo, postTransInfo;
        MeasurementInfo info;
        SquarewaveMeasurements.TransitionMeasurement(
            signal, _increment, _stateSetting,
            EdgePolarity.Rising, _refLevelSetting,
            1,  // edgeNumber 从 1 开始
            out slope, out transitionDuration,
            out preTransInfo, out postTransInfo, out info);

        label_Slope.Text = string.Format("斜率: {0:F2}", slope);
        label_Duration.Text = string.Format("上升时间: {0:F6} s", transitionDuration);
        label_PreOS.Text  = string.Format("预过冲: {0:F6}", preTransInfo.Overshoot);
        label_PostOS.Text = string.Format("后过冲: {0:F6}", postTransInfo.Overshoot);

        // MeasurementInfo 可用于在图表上标记边沿位置
        // info.StartTime, info.EndTime
    }
    catch (Exception ex)
    {
        label_Slope.Text = "Error: " + ex.Message;
    }
}

// ── 完整调用流程 ────────────────────────────────────────
private void FullSquarewaveAnalysis(double[] signal)
{
    InitSettings();
    easyChartX1.Plot(signal, 0, _increment);
    AnalyzeAmplitude(signal);
    MeasurePulse(signal);
    AnalyzePeriod(signal);
    AnalyzeCycleRms(signal);
    MeasureTransition(signal);
}
```

---

### EX-Y：PeakValleyAnalysis 与 FindPeaks/FindValleys out 参数重载

```csharp
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility;

private void PeakValleyWithOutParams(double[] signal, double sampleRate)
{
    // 方法 1：经典 FindPeaks（返回 double[,]）
    double[] xAxis = new double[signal.Length];
    for (int i = 0; i < signal.Length; i++)
        xAxis[i] = i / sampleRate;

    double[,] peaks = PeakValleyAnalysis.FindPeaks(
        signal, xAxis, 0.5, 0.005, 0.1, FlatSelection.Center, isValley: false);
    // peaks[peakCount, 2]：列0=峰值高度, 列1=峰值位置

    // 方法 2：FindPeaks 重载（out 参数，返回索引和值分离）
    int[] peakIndexes;
    double[] peakValues;
    PeakValleyAnalysis.FindPeaks(
        signal, 0.3,   // prominence（突出度）
        out peakIndexes, out peakValues);

    for (int i = 0; i < peakIndexes.Length; i++)
    {
        double time = peakIndexes[i] / sampleRate;
        Console.WriteLine(string.Format("峰 {0}: 索引={1}, 时间={2:F6}s, 值={3:F4}V",
            i, peakIndexes[i], time, peakValues[i]));
    }

    // FindValleys 重载（同样支持 out 参数）
    int[] valleyIndexes;
    double[] valleyValues;
    PeakValleyAnalysis.FindValleys(
        signal, 0.3,
        out valleyIndexes, out valleyValues);

    // 在图表上标记峰谷点
    List<double> peakX = new List<double>();
    List<double> peakY = new List<double>();
    for (int i = 0; i < peakIndexes.Length; i++)
    {
        peakX.Add(peakIndexes[i] / sampleRate);
        peakY.Add(peakValues[i]);
    }
    easyChartX1.Plot(signal, 0, 1.0 / sampleRate);
    easyChartX1.AddDataMarker(peakX, peakY, Color.Red, DataMarkerType.Triangle);
}
```

---

## Filter1D 滤波器设计完整范例

### EX-Z1：IIR 常用滤波器设计与执行（Butter / Notch）

```csharp
// 项目需引用 SeeSharpTools.JY.DSP.Utility.Filter1D.dll
using SeeSharpTools.JY.DSP.Utility.Filter1D;

// ───────────────────────────────────────────────────────────────────────
// 案例：混合信号（100Hz 有用信号 + 50Hz 工频干扰 + 3000Hz 高频噪声）
const double sampleRate = 10000.0;
const int    N          = 8192;
double[] t      = new double[N];
double[] signal = new double[N];
var rng = new Random(42);
for (int i = 0; i < N; i++)
{
    t[i]      = i / sampleRate;
    signal[i] = Math.Sin(2 * Math.PI * 100 * t[i])         // 有用信号
               + 0.8 * Math.Sin(2 * Math.PI * 50 * t[i])  // 50 Hz 工频干扰
               + 0.3 * (rng.NextDouble() * 2 - 1);        // 高频宽带噪声
}

// ── Step1: 陷波滤波器去除 50 Hz 工频──────────────────────────────────
// ⚠️ wn 单位是 Hz，不是归一化频率
var (nb, na) = IIRDesign.IIRNotch(w0: 50.0, bw: 5.0, sampleRate);
double[] notchFiltered = IIRFiltering.IIRFilter(nb, na, signal);

// ── Step2: Butterworth 4阶 500Hz 低通───────────────────────────────────
var (bb, ba) = IIRDesign.Butter(4, new double[] { 500.0 }, IIRBandType.Lowpass, sampleRate);
// 先检查滤波器稳定性
bool stable = FilterExploration.IsStable(bb, ba);
if (!stable) throw new InvalidOperationException("IIR 滤波器不稳定，请调整阶数或改用 SOS");

double[] finalFiltered = IIRFiltering.IIRFilter(bb, ba, notchFiltered);

// ── 绘图对比 ──────────────────────────────────────────────────────────
easyChartX_raw.Plot(signal, 0, 1.0 / sampleRate);
easyChartX_filtered.Plot(finalFiltered, 0, 1.0 / sampleRate);
```

---

### EX-Z2：SOS 高阶 IIR（数值稳定）与零相位滤波

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

const double sampleRate = 10000.0;
const int    N          = 4096;

// 生成测试信号
double[] signal = new double[N];
for (int i = 0; i < N; i++)
    signal[i] = Math.Sin(2 * Math.PI * 200 * i / sampleRate)
              + 0.5 * Math.Sin(2 * Math.PI * 2000 * i / sampleRate);

// ── SOS 设计（8阶 Butterworth 低通，高阶时強烈推荐 SOS）───────────────
double[] sos = IIRDesign.SOSDesign(
    n: 8,
    wn: new double[] { 500.0 },
    rp: 3.0, rs: 60.0,
    IIRBandType.Lowpass,
    IIRDesignMethod.Butterworth,
    sampleRate);

// ── SOS 基本滤波（带 IIR 相位延迟）────────────────────────────────
double[] filtered = IIRFiltering.SOSFilter(sos, signal);

// ── 零相位 SOS 滤波（无相位延迟，仅适合离线处理）───────────────────
// PadType.Odd: 奇对称延拓，减少边界效应； padLength 建议≥阶数*3
double[] zeroPhaseFiltered = IIRFiltering.ZeroPhaseFilter(sos, signal, PadType.Odd, padLength: 100);

// 绘图对比
easyChartX1.Plot(signal,            0, 1.0 / sampleRate);  // 原始信号
easyChartX2.Plot(filtered,          0, 1.0 / sampleRate);  // SOS IIR（有延迟）
easyChartX3.Plot(zeroPhaseFiltered, 0, 1.0 / sampleRate);  // 零相位（无延迟）
```

---

### EX-Z3：FIR 滤波器设计与执行（窗函数 / Parks-McClellan）

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

const double sampleRate = 10000.0;
const int    N          = 4096;

// 生成测试信号
double[] signal = new double[N];
for (int i = 0; i < N; i++)
    signal[i] = Math.Sin(2 * Math.PI * 200 * i / sampleRate)
              + Math.Sin(2 * Math.PI * 3000 * i / sampleRate);

// ── 方法 A：窗函数设计 FIR 低通滤波器（Hamming）─────────────────────
// n=64: 阶数（偶数，窗函数法需偶数否则自动+1）
double[] firCoeff_a = FIRDesign.Window(
    n: 64,
    wn: new double[] { 500.0 },       // 截止频率(Hz)
    WindowType.Hamming, windowParam: 0,
    FIRBandType.Lowpass, scale: true,
    sampleRate);

double[] filteredA = FIRFiltering.FIRFilter(firCoeff_a, signal);

// ── 方法 B：Park-McClellan 最优 FIR（过渡带更陡）────────────────────
double[] firCoeff_b = FIRDesign.ParksMcClellan(
    order: 80,
    bands:      new double[] { 0, 400, 600, 5000 },  // [通带低, 通带高, 阻带低, 阻带高]
    amplitudes: new double[] { 1, 1, 0, 0 },          // 通带增益=1, 阻带增益=0
    weight:     new double[] { 1, 20 },               // 阻带权重=20：求更好阻带抟模
    PMFilterType.Bandpass,
    sampleRate, griddensity: 16);

// FFT 快速滤波（长信号时效率更高）
double[] filteredB = FIRFiltering.FFTFilter(firCoeff_b, signal);

// ── 查看 FIR 滤波器幅频响应 ───────────────────────────────────────
var (magnitude, phase, freq) = FIRAnalysis.Bode(firCoeff_a, sampleRate, n: 512);
easyChartX_bode.Plot(freq, magnitude);
```

---

### EX-Z4：实时流式连续滤波（帧间保留状态）

```csharp
// 项目需引用 SeeSharpTools.JY.DSP.Utility.Filter1D.dll
using SeeSharpTools.JY.DSP.Utility.Filter1D;

public partial class MainForm : Form
{
    private double[] _b, _a;      // IIR 滤波器系数
    private double[] _z;          // 滤波器状态（帧间保留）
    private const double SampleRate = 10000.0;

    private void btnStart_Click(object sender, EventArgs e)
    {
        // 设计 Butterworth 4阶 500Hz 低通滤波器
        var (b, a) = IIRDesign.Butter(4, new double[] { 500.0 }, IIRBandType.Lowpass, SampleRate);
        _b = b;
        _a = a;

        // 初始化滤波器状态（稳态初始条件，避免起始电平跳跃）
        _z = IIRFiltering.IIRFilter_IC(_b, _a);

        // 启动传感器采集任务。每帧回调 OnDataReady
        // StartAcquisition(callback: OnDataReady);
    }

    private void OnDataReady(double[] newBlock)
    {
        // ── 项目关键：帧间传递状态 _z，避免断点 ──
        var (filtered, newZ) = IIRFiltering.IIRFilter(_b, _a, newBlock, _z);
        _z = newZ;  // 更新状态到下一帧

        // 跨线程更新 UI
        this.Invoke((Action)(() =>
        {
            easyChartX1.Plot(filtered, 0, 1.0 / SampleRate);
        }));
    }
}
```

---

### EX-Z5：多速率处理（Decimate / Interpolate / Resample）

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

const double originalRate = 44100.0;  // 44.1 kHz 音频信号
const int    N            = 44100;    // 1 秒数据

// 生成测试信号
double[] signal = new double[N];
for (int i = 0; i < N; i++)
    signal[i] = Math.Sin(2 * Math.PI * 440 * i / originalRate);  // 440 Hz 标准音

// ── 抽取（降采样）：44100 → 11025 Hz（因子 4）───────────────────────────
// ⚠️ 一定要用 Decimate（带抗混叠滤波），不要用 Downsample（将出现混叠失真）
double[] decimated = Multirate.Decimate(signal, deciFactor: 4, AntialiasingFilterType.IIR, order: 8);
// decimated.Length ≈ N/4 = 11025

// ── 插值（升采样）：44100 → 176400 Hz（因子 4）──────────────────────────
double[] interpolated = Multirate.Interpolate(signal, interpFactor: 4);
// interpolated.Length = N*4

// ── 有理数重采样：44100 →48000 Hz（p=160, q=147）──────────────────────
// 计算 p/q: 48000/44100 = 160/147
double[] resampled48k = Multirate.Resample(signal, p: 160, q: 147, n: 10, beta: 5.0);
```

---

### EX-Z6：Savitzky-Golay 平滑、微分与积分

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

const double sampleRate = 1000.0;
const int    N          = 2048;

double[] t      = new double[N];
double[] signal = new double[N];
var rng = new Random();
for (int i = 0; i < N; i++)
{
    t[i]      = i / sampleRate;
    signal[i] = Math.Sin(2 * Math.PI * 5 * t[i]) + 0.5 * (rng.NextDouble() - 0.5);
}

// ── Savitzky-Golay 平滑（保留信号形状，平滑噪声）──────────────────
// order=3：多项式阶数  frameLength=11：窗口长度（必须奇数且 > order）
double[] sgSmooth = FIRFiltering.SgolayFilter(signal, order: 3, frameLength: 11, weights: null);

// ── 移动平均滤波 ──────────────────────────────────────────────────
double[] maSmooth = IIRFiltering.SmoothingFilter(
    signal, sampleRate,
    SmoothingType.MovingAverage, halfWidth: 5,
    FilterShape.Rectangular, timeConstant: 0);
// 窗口大小 = 2*5+1 = 11

// ── 微分（生成速度信号）──────────────────────────────────────────────
double[] initDiff = null;  // 首次传 null 自动初始化
double[] velocity = SpecialFiltering.Differentiate(
    signal, fs: sampleRate, BandwidthOption.WideBand, ref initDiff);

// ── 积分（生成位移信号，含高通防漂移）──────────────────────────────
double[] initInteg = null;
double[] displacement = SpecialFiltering.Integrate(
    signal, fs: sampleRate, HPF6dBFreq: 1.0, BandwidthOption.WideBand, ref initInteg);

// 绘图
easyChartX1.Plot(signal,      0, 1.0 / sampleRate);  // 原始（加噪）
easyChartX2.Plot(sgSmooth,    0, 1.0 / sampleRate);  // S-G 平滑
easyChartX3.Plot(velocity,    0, 1.0 / sampleRate);  // 微分
```

---

### EX-Z7：滤波器特性分析与 Bode 图

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

const double sampleRate = 10000.0;

// 设计滤波器
var (b, a) = IIRDesign.Butter(4, new double[] { 500.0 }, IIRBandType.Lowpass, sampleRate);

// ── 滤波器属性检查 ─────────────────────────────────────────────────
bool stable   = FilterExploration.IsStable(b, a);
bool isFIR    = FilterExploration.IsFIR(a);         // a=[1.0] 则为 FIR
bool linPhase = FilterExploration.IsLinearPhase(b, a, tol: 1e-6);
int  order    = FilterExploration.FilterOrder(b, a);

Console.WriteLine($"稳定: {stable}, FIR: {isFIR}, 线性相位: {linPhase}, 阶数: {order}");

// ── Bode 图（幅频 + 相频 + 频率轴）────────────────────────────────────
var (magnitude, phase, freq) = IIRAnalysis.Bode(b, a, sampleRate, n: 512);

// 绘制幅频响应
// magnitude 单位：线性增益（若需 dB： 20*log10(mag)）
double[] magdB = new double[magnitude.Length];
for (int i = 0; i < magnitude.Length; i++)
    magdB[i] = magnitude[i] > 0 ? 20 * Math.Log10(magnitude[i]) : -120.0;

easyChartX_magnitude.Plot(freq, magdB);
easyChartX_magnitude.AxisX.Title = "频率 (Hz)";
easyChartX_magnitude.AxisY.Title = "幅度 (dB)";
```

---

### EX-Z8：ExpressFilter 设计器组件（WinForm 可拖放）

> 场景还原自官方范例：含噪声的心电图信号（ECG + 均匀白噪声）→ FIR 低通滤波去噪，  
> 搭配 FilterDesignForm 图形设计界面实现参数交互调整。

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;

// ── 1. Form 字段 ──────────────────────────────────────────────
private double[] _signal;
private double   _fs = 500.0;
private double[] _filterState = null;   // 流式分帧时跨帧状态

// ── 2. 加载信号 ───────────────────────────────────────────────
private void Form_Load(object sender, EventArgs e)
{
    // 生成含噪心电图信号（模拟 DAQ 采集的原始数据）
    _signal = GenerateNoisyECG(length: 500, noiseAmp: 0.25);
    chartRawWaveform.Plot(_signal, 0, 1.0 / _fs);
}

// ── 3A. 代码配置 IIR 滤波器 ───────────────────────────────────
private void ConfigureIIR()
{
    expressFilter1.Configuration = new FilterConfig
    {
        Type = FilterType.IIR,
        IIR = new IIRConfig
        {
            DesignMethod        = "ChebyshevII",    // Butterworth/ChebyshevI/ChebyshevII/Elliptic/Bessel
            BandType            = IIRBandType.Lowpass,
            Order               = 8,
            LowCutoffFrequency  = 50.0,             // Hz（低通截止）
            HighCutoffFrequency = 0,                // 低通模式置 0
            SampleRate          = _fs,
            Rp                  = 1.0,              // 通带波纹 dB
            Rs                  = 80.0,             // 阻带衰减 dB
        }
    };
}

// ── 3B. 代码配置 FIR 窗函数滤波器 ────────────────────────────
private void ConfigureFIR()
{
    expressFilter1.Configuration = new FilterConfig
    {
        Type = FilterType.FIRWindow,
        FIRWindow = new FIRWindowConfig
        {
            BandType            = FIRBandType.Lowpass,
            Order               = 80,               // 阶数越高过渡带越陡
            LowCutoffFrequency  = 50.0,
            HighCutoffFrequency = 0,
            SampleRate          = _fs,
            WindowType          = WindowType.Hamming,
        }
    };
    // ⚠️ FIR 时 Coeff_a 为 null，仅 Coeff_b 有效
}

// ── 4. 图形交互设计（推荐用于 WinForm 产品）──────────────────
// "设计" 按钮——打开 FilterDesignForm 图形界面，用户可在界面中
// 选择滤波类型、通带/阻带频率、设计方法，并实时预览频率响应
private void btDesign_Click(object sender, EventArgs e)
{
    var form = new FilterDesignForm(expressFilter1.Configuration);
    if (form.ShowDialog() == DialogResult.OK)
    {
        expressFilter1.Configuration = form.GetUpdatedConfig();
        _filterState = null;    // 切换滤波器后重置状态
    }
}

// ── 5. 单次离线滤波 ───────────────────────────────────────────
private void btFilter_Click(object sender, EventArgs e)
{
    // zeroPhase=true：零相位（适合离线分析，无相位延迟但不能实时）
    // zeroPhase=false：常规单向滤波（适合实时，有群延迟）
    double[] y = expressFilter1.DoFilter(_signal, zeroPhase: true);
    chartFilterWaveform.Plot(y, 0, 1.0 / _fs);

    // 获取设计后系数（可复用于 IIRFiltering.IIRFilter 直接调用）
    double[] b = expressFilter1.Coeff_b;   // 分子系数（IIR/FIR 均有效）
    double[] a = expressFilter1.Coeff_a;   // 分母系数（仅 IIR 有效）
}

// ── 6. 实时流式分帧滤波（保持跨帧状态 z）────────────────────
private void OnNewFrameArrived(double[] frame)
{
    // z 数组保存上一帧末尾的滤波器状态，实现帧间无缝连续
    // 状态数组长度 = max(len(b), len(a)) - 1
    if (_filterState == null)
    {
        int bLen = expressFilter1.Coeff_b?.Length ?? 0;
        int aLen = expressFilter1.Coeff_a?.Length ?? 0;
        _filterState = new double[Math.Max(bLen, aLen) - 1];
    }
    double[] filtered = expressFilter1.DoFilter(frame, _filterState);
    chartFilterWaveform.Plot(filtered, 0, 1.0 / _fs);
}
```

**关键注意事项**

| 场景 | 推荐用法 | 说明 |
|------|---------|------|
| 离线信号分析 | `DoFilter(x, zeroPhase: true)` | 零相位，无延迟，不能实时 |
| 实时采集滤波 | `DoFilter(x, zeroPhase: false)` | 单向，有群延迟 |
| 连续分帧流式 | `DoFilter(frame, z)` | 保持 `z` 状态跨帧，切换滤波器后需重置 `z` |
| 图形化调参 | `FilterDesignForm` | 内置频率响应预览，设计后写回 `Configuration` |
| FIR 时读系数 | `Coeff_b` 有效，`Coeff_a` 为 null | 切勿在 FIR 模式下读 `Coeff_a` |

---

### 小结：Filter1D 滤波器设计选择指南

```
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║                     Filter1D 设计方法选择指南                              ║
// ╠════════════════╤═════════════════════════════════════════════╣
// ║ 情景           │ 推荐方案                                          ║
// ╟────────────────┼─────────────────────────────────────────────╢
// ║ 一般低通/高通  │ IIRDesign.Butter + IIRFiltering.IIRFilter          ║
// ║ 去除工频干扰  │ IIRDesign.IIRNotch + IIRFiltering.IIRFilter        ║
// ║ 高阶滤波器     │ IIRDesign.SOSDesign + IIRFiltering.SOSFilter        ║
// ║ 离线无延迟     │ IIRFiltering.ZeroPhaseFilter (SOS 版)              ║
// ║ 尖锐过渡带     │ FIRDesign.ParksMcClellan + FIRFiltering.FFTFilter   ║
// ║ 实时流式处理  │ IIRFilter/FIRFilter + 保留状态 z 分帧传递           ║
// ║ 信号平滑去噪  │ FIRFiltering.SgolayFilter 或 IIRFiltering.SmoothingFilter ║
// ║ 微分/积分       │ SpecialFiltering.Differentiate / Integrate          ║
// ║ 降采样（防混叠）  │ Multirate.Decimate（勿用 Downsample！）              ║
// ╚════════════════╧═════════════════════════════════════════════╝
```

---

## ★ DSP.Utility.Spectrum 高级频谱分析专项示例

> **命名空间**：`SeeSharpTools.JY.DSP.Utility.Spectrum`
> **DLL**：`SeeSharpTools.JY.DSP.Utility.Spectrum.dll`
> **与 `DSP.Fundamental.Spectrum` 的区别**：Utility.Spectrum 使用 ValueTuple 返回值，API 更现代，无需 `ref` 预分配输出数组

### EX-Y1：Fourier FFT 变换（基础频谱 + 补零改善栅栏效应）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// Fourier.Forward 使用规则：
//   ★ 返回 Complex[]，无需 ref 预分配
//   ★ size 参数可大于信号长度（补零提高分辨率）
//   ★ 输出为单边复数频谱，需自行计算幅值和频率轴
// ══════════════════════════════════════════════════════════════════════════════
using MathNet.Numerics;
using SeeSharpTools.JY.DSP.Utility.Spectrum;
using System.Numerics;

private void FourierFFTExample()
{
    double fs = 80;
    int L = 65;
    double[] t = Generate.LinearRange(0, 1 / fs, (L - 1) / fs);

    // 创建多频叠加信号：2Hz + 4Hz + 6Hz
    double[] signal = new double[t.Length];
    for (int i = 0; i < signal.Length; i++)
    {
        signal[i] = 3 * Math.Cos(2 * Math.PI * 2 * t[i]) + 2 * Math.Cos(2 * Math.PI * 4 * t[i]) +
            Math.Sin(2 * Math.PI * 6 * t[i]);
    }

    // ── 基础 FFT（不补零）──
    int n = signal.Length;
    Complex[] y = Fourier.Forward(signal, n);
    double[] f = Generate.LinearRange(0, (L - 1) / 2).Multiply(fs / L);
    double[] p2 = y.Divide(L).Absolute();  // 归一化幅值
    double[] p1 = p2.SubArray(0, (L + 1) / 2);
    for (int i = 1; i < p1.Length; i++) p1[i] = 2 * p1[i]; // 单边谱双倍补偿
    easyChartX1.Plot(f, p1);

    // ── 补零至 2 的幂次（改善栅栏效应）──
    n = Euclid.CeilingToPowerOfTwo(L);  // 65 → 128
    y = Fourier.Forward(signal, n);
    f = Generate.LinearRange(0, n / 2).Multiply(fs / n);
    p2 = y.Divide(L).Absolute();
    p1 = p2.SubArray(0, n / 2 + 1);
    for (int i = 1; i < p1.Length - 1; i++) p1[i] = 2 * p1[i];
    easyChartX2.Plot(f, p1);
}
```

---

### EX-Y2：Welch 法功率谱估计（参数配置全覆盖）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// SpectralEstimation.Pwelch 使用规则：
//   ★ 返回 ValueTuple (power, f)，无需 ref 预分配
//   ★ segLength: 分段长度，不得超过信号长度
//   ★ noverlap: 重叠长度，应 < segLength
//   ★ nfft: FFT 长度，推荐 2 的幂次
//   ★ WindowType 命名空间可能与 DSP.Fundamental 冲突，用全限定名
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void WelchExample()
{
    // 生成多频信号 + 噪声
    double sampleRate = 200e3;
    double[] t = Generate.LinearRange(0, 1 / sampleRate, 0.1 - 1 / sampleRate);
    double[] signal = new double[t.Length];
    for (int i = 0; i < t.Length; i++)
    {
        signal[i] = Math.Sin(2 * Math.PI * 1e3 * t[i])
                  + 0.1 * Math.Sin(2 * Math.PI * 1e4 * t[i])
                  + 10 * Math.Sin(2 * Math.PI * 2e4 * t[i]);
    }
    double[] noise = new double[t.Length];
    Generation.UniformWhiteNoise(ref noise, 0.1);
    signal = signal.Add(noise.Multiply(0.1));

    // Welch 平均周期图法
    var (power, f) = SpectralEstimation.Pwelch(
        signal, sampleRate,
        segLength: 4444,                                                    // 分段长度
        windowType: SeeSharpTools.JY.DSP.Utility.Spectrum.WindowType.Hamming, // ❗ 全限定名避免冲突
        windowParam: double.NaN,                                             // 默认窗参数
        noverlap: 2222,                                                      // 50% 重叠
        nfft: 8192,                                                          // FFT 长度
        traceMode: TraceMode.Average,                                        // 平均模式
        freqRange: FrequencyRange.OneSided,                                  // 单边谱
        exportMode: ExportMode.PowerSpectrum,                                // 功率谱
        dB: true                                                             // dB 输出
    );

    easyChartX1.Plot(f.Divide(1000), power);
    easyChartX1.AxisY.Title = "功率谱 (dBV)";
    easyChartX1.AxisX.Title = "频率 (kHz)";
}
```

---

### EX-Y3：Periodogram 周期图法（单通道 + 多通道）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// SpectralEstimation.Periodogram 使用规则：
//   ★ window 参数需用 Windows.GetCoefficients() 生成
//   ★ nfft 传 double.NaN 则自动等于信号长度
//   ★ 多通道使用 double[,] 输入，返回 double[,] 结果
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void PeriodogramExample()
{
    double sampleRate = 1000;
    double[] t = Generate.LinearRange(0, 1 / sampleRate, 1 - 1 / sampleRate);

    // ── 单通道 ──
    double[] signal = new double[t.Length];
    for (int i = 0; i < t.Length; i++)
        signal[i] = Math.Sin(2 * Math.PI * 50 * t[i]) + 2 * Math.Sin(2 * Math.PI * 140 * t[i]);

    double[] window = Windows.GetCoefficients(
        SeeSharpTools.JY.DSP.Utility.Spectrum.WindowType.Hamming, t.Length);

    var (psd, f) = SpectralEstimation.Periodogram(
        signal, sampleRate, window, double.NaN,
        FrequencyRange.OneSided, ExportMode.PowerSpectrum, dB: true);
    easyChartX1.Plot(f, psd);

    // ── 多通道（使用 BuildArray 合并）──
    double[] ch1 = signal;  // 第一通道
    double[] ch2 = new double[t.Length];
    for (int i = 0; i < t.Length; i++)
        ch2[i] = 2 * Math.Sin(2 * Math.PI * 140 * t[i]);
    double[,] multiSignal = ArrayUtility.BuildArray(ch1, ch2, Direction.Column);

    var (multiPsd, f2) = SpectralEstimation.Periodogram(
        multiSignal, sampleRate, window, double.NaN,
        FrequencyRange.OneSided, ExportMode.PowerSpectrum, dB: true);
    easyChartX2.Plot(f2, multiPsd, SeeSharpTools.JY.GUI.MajorOrder.Column);
}
```

---

### EX-Y4：单频/多频信号提取（SingleTone + ExtractMultiTone）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// SingleTone 使用规则：
//   ★ ExtractSingleTone: 返回 (freq, amp, phase)，phase 单位为度
//   ★ ExtractMultiTone: 返回 (freq[], amp[], phase[])  
//   ★ FFTWithHanning: 用 Hanning 窗做 FFT，返回 Complex[]，配合 Conversion.MagTodB 可视化
//   ★ OutputSorting: IncreasingFrequency=按频率升序, DecreasingAmplitude=按幅值降序
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Utility.Spectrum;
using System.Numerics;

private void SingleToneExample()
{
    double sampleRate = 51200;
    int blockSize = 10000;
    double toneFreq = 1000;
    double toneAmp = 5.0;
    double tonePhase = 45.0;

    // 生成信号
    double[] signal = new double[blockSize];
    for (int i = 0; i < blockSize; i++)
        signal[i] = toneAmp * Math.Sin(2 * Math.PI * toneFreq / sampleRate * i + tonePhase * Math.PI / 180);

    // 提取单频信号
    var (freq, amp, phase) = SingleTone.ExtractSingleTone(signal, sampleRate);
    phase = ArrayUtility.Mod(phase, 360); // 相位归一化到 [0,360)

    // FFT 可视化（Hanning 窗 + dB）
    Complex[] spectrum = SingleTone.FFTWithHanning(signal);
    double[] mag = Conversion.MagTodB(spectrum.Absolute());
    double df = sampleRate / signal.Length;
    easyChartX1.Plot(mag, 0, df);
    easyChartX1.AxisX.Title = "频率 (Hz)";
    easyChartX1.AxisY.Title = " (dB)";
}

private void MultiToneExample()
{
    double sampleRate = 51200;
    int samples = 1000;
    double[] toneFreqs = new double[] { 249.5, 471.67, 876.7, 2493.7 };
    double[] toneAmps = new double[] { 1, 0.25, 0.5, 0.0656 };

    double[] t = Generate.LinearRange(0, 1 / sampleRate, (samples - 1) / sampleRate);
    double[] signal = new double[samples];
    for (int i = 0; i < toneFreqs.Length; i++)
        signal = signal.Add(t.Select(num => toneAmps[i] * Math.Sin(2 * Math.PI * toneFreqs[i] * num)).ToArray());

    // 提取多频信号（阈值 0.05，最多 10 个，按频率升序）
    var (freq, amp, phase) = SingleTone.ExtractMultiTone(
        signal, sampleRate, threshold: 0.05, maxNumTones: 10,
        sorting: OutputSorting.IncreasingFrequency);

    for (int i = 0; i < freq.Length; i++)
        Console.WriteLine($"Tone {i}: {freq[i]:F0} Hz, {amp[i]:F2} V, {phase[i]:F0}°");
}
```

---

### EX-Y5：频谱测量与失真分析（SpectralFeature + Distortion）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// SpectralFeature + Distortion 使用规则：
//   ★ 支持时域输入或频域输入（两组重载）
//   ★ SpectralFeature.BandPower: freqRange=null 表示全频段
//   ★ Distortion.TOI: 仅适用于双音信号
//   ★ Conversion.dBToMag: 用于将 SNR(dB) 转为幅值比
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void SpectrumMeasureExample(double[] signal, double sampleRate)
{
    // 计算功率谱（dB 输出）
    var (psd, f) = SpectralEstimation.Periodogram(signal, sampleRate, dB: true);
    easyChartX1.Plot(f, psd);

    // ── 频带功率 ──
    double bandPower = SpectralFeature.BandPower(signal, sampleRate, null, PowerUnits.V2);

    // ── 99% 占用带宽 ──
    var (obw, fLow, fHigh, _) = SpectralFeature.OccupiedBandwidth(signal, sampleRate);

    // ── -3dB 功率带宽 ──
    var (bw3dB, _, _, _) = SpectralFeature.PowerBandwidth(signal, sampleRate);

    // ── 平均频率 / 中位数频率 ──
    var (meanFreq, _) = SpectralFeature.MeanFreq(signal, sampleRate);
    double medianFreq = SpectralFeature.MedianFreq(signal, sampleRate);

    // ── 动态参数测量（时域直接输入）──
    var (snr, noisePower) = Distortion.SNR(signal, sampleRate);
    var (sinad, totalDistPower) = Distortion.SINAD(signal, sampleRate);
    var (sfdr, spurPower, spurFreq) = Distortion.SFDR(signal, sampleRate);
    var (thd, harmonicPowers, harmonicFreqs) = Distortion.THD(signal, sampleRate);
}
```

---

### EX-Y6：交叉功率谱密度 + 相干性分析

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void CrossSpectrumExample()
{
    double sampleRate = 1000;
    double signalFreq = 50;
    double tau = 1.0 / 400; // 2.5ms 滞后 = 45° 相位差
    double[] t = Generate.LinearRange(0, 1 / sampleRate, 0.296);
    double[] noise = new double[t.Length];
    Generation.UniformWhiteNoise(ref noise, 0.1);

    double[] x = new double[t.Length];
    double[] y = new double[t.Length];
    for (int i = 0; i < t.Length; i++)
    {
        x[i] = Math.Cos(2 * Math.PI * t[i] * signalFreq) + 0.1 * noise[i];
        y[i] = Math.Cos(2 * Math.PI * (t[i] - tau) * signalFreq) + 0.1 * noise[i];
    }

    // 交叉功率谱密度
    var (pxy, phase, f) = SpectralEstimation.CrossPowerSpectralDensity(x, y, sampleRate);
    easyChartX1.Plot(f, Conversion.PowerTodB(pxy.Absolute()));

    // 幅值平方相干性（0~1，1=完全相关）
    var (cxy, _) = SpectralEstimation.MagnitudeSquaredCoherence(x, y, sampleRate);
    easyChartX2.Plot(f, cxy);

    // 交叉相位谱（弧度 → 角度）
    easyChartX3.Plot(f, phase.Divide(Math.PI / 180));
}
```

---

### EX-Y7：Hilbert 变换解析信号

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;
using System.Numerics;

private void HilbertExample()
{
    double sampleRate = 1e4;
    double[] t = Generate.LinearRange(0, 1 / sampleRate, 1);
    double[] signal = new double[t.Length];
    for (int i = 0; i < t.Length; i++)
        signal[i] = 2.5 + Math.Cos(2 * Math.PI * 203 * t[i]) + Math.Sin(2 * Math.PI * 721 * t[i]);

    // Hilbert 变换：实信号 → 解析信号（实部=原信号，虚部=Hilbert变换）
    Complex[] analyticSignal = Hilbert.Forward(signal);

    // 显示实部和虚部
    double[] realPart = analyticSignal.ComplexToReal();
    double[] imagPart = analyticSignal.ComplexToImaginary();
    double[,] plotData = realPart.BuildArray(imagPart, Direction.Row);
    easyChartX1.Plot(t, plotData);
    easyChartX1.Series[0].Name = "实部";
    easyChartX1.Series[1].Name = "虚部";

    // 对比原信号和 Hilbert 变换后的频谱（中心频谱）
    var (psd, f) = SpectralEstimation.Pwelch(
        signal.RealToComplex().BuildArray(analyticSignal, Direction.Column),
        sampleRate, 256, noverlap: 0, freqRange: FrequencyRange.Centered, dB: true);
    easyChartX2.Plot(f, psd, majorOrder: SeeSharpTools.JY.GUI.MajorOrder.Column);
}
```

---

### EX-Y8：互调失真分析（TOI 三阶截断点）

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void IMDExample()
{
    // 双音信号：5kHz + 6kHz，采样率 48kHz
    double fi1 = 5e3, fi2 = 6e3, fs = 48e3;
    int n = 1000;
    double[] x = new double[n];
    for (int i = 0; i < n; i++)
        x[i] = Math.Sin(2 * Math.PI * fi1 / fs * (i + 1)) + Math.Sin(2 * Math.PI * fi2 / fs * (i + 1));

    // 通过多项式非线性化
    double[] p = new double[] { 3e-3, 0.1, 1e-7, 0.5e-3 };
    double[] y = new double[n];
    for (int i = 0; i < n; i++)
        y[i] = Polynomial.Evaluate(x[i], p);

    // Kaiser 窗周期图
    double[] window = Windows.Kaiser(y.Length, 38);
    var (spectrum, f) = SpectralEstimation.Periodogram(y, fs, window, n,
        exportMode: ExportMode.PowerSpectrumDensity);
    easyChartX1.Plot(f, Conversion.PowerTodB(spectrum));

    // 计算三阶截断点（从 PSD 输入）
    var (oip3, fundPow, fundFreq, imodPow, imodFreq) = Distortion.TOI(spectrum, f);
    // oip3: 三阶截断点 (dBm)
    // fundPow/fundFreq: 两个基波的功率和频率
    // imodPow/imodFreq: 两个互调产物的功率和频率
}
```

---

### EX-Y9：传递函数估计

```csharp
using SeeSharpTools.JY.DSP.Utility.Filter1D;
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void TransferFunctionExample()
{
    // 设计 FIR 低通滤波器
    double[] b = FIRDesign.Window(30, new double[] { 0.2 },
        SeeSharpTools.JY.DSP.Utility.WindowType.Rectwin);

    // 生成输入信号（白噪声）和输出信号（滤波后）
    double[] x = GaussianWhiteNoise(16384);
    double[] y = FIRFiltering.FIRFilter(b, x);
    double fs = 500;

    // 窗函数
    double[] window = Windows.GetCoefficients(WindowType.Hamming, 1024);

    // 估计传递函数
    var (txy, f) = SpectralEstimation.TransferFunction(x, y, fs, window);
    easyChartX1.Plot(f, Conversion.MagTodB(txy));  // 传递函数幅度响应

    // 对比理论响应
    var (mag, _, f1) = FIRAnalysis.Bode(b, fs);
    easyChartX2.Plot(f1, mag);
}
```

---

### EX-Y10：频谱平均（项平均 + RMS 平均）

```csharp
// ══════════════════════════════════════════════════════════════════════════════
// SpectrumAveraging 使用规则：
//   ★ 多次调用 AverageMagPhaseSpectrum 自动累积平均
//   ★ restartAveraging=true 重置平均计算（切换信号或首次调用时传 true）
//   ★ averagingDone=true 表示已达到指定平均次数
// ══════════════════════════════════════════════════════════════════════════════
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private bool _restartAveraging = true;

private void AveragingSpectrumTick()
{
    double sampleRate = 51200;
    int samples = 1024;
    double[] signal = new double[samples];
    Generation.SineWave(ref signal, 1.0, 0, 1000, sampleRate);
    double[] noise = new double[samples];
    Generation.UniformWhiteNoise(ref noise, 0.01);
    signal = signal.Add(noise);

    // 配置平均参数
    SpectrumAveraging averaging = new SpectrumAveraging
    {
        AveragingMode = AveragingMode.VectorAveraging,
        WeightingMode = WeightingMode.Linear,
        NumberOfAverages = 16
    };

    // 计算平均幅度/相位谱
    var (mag, phase, df, averagesCompleted, averagingDone) =
        SpectralEstimation.AverageMagPhaseSpectrum(
            signal, sampleRate,
            SeeSharpTools.JY.DSP.Utility.Spectrum.WindowType.Hanning, double.NaN,
            averaging, dBMag: true, unwrapPhase: false, convertToDegree: true,
            restartAveraging: _restartAveraging);

    easyChartX1.Plot(mag, 0, df);   // 幅度谱
    easyChartX2.Plot(phase, 0, df); // 相位谱
    _restartAveraging = false;  // 后续调用不重启
}
```

---

### EX-Y11：ChirpZ 变换（指定频段高分辨率分析）

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;
using System.Numerics;

private void ChirpZExample()
{
    double fs = 200;
    int n = 640;
    double[] t = Generate.LinearRange(0, 1 / fs, (n - 1) / fs);
    double[] x = new double[t.Length];
    for (int i = 0; i < x.Length; i++)
    {
        x[i] = 10 * Math.Sin(2 * Math.PI * 32 * t[i]) + 10 * Math.Sin(2 * Math.PI * 50 * t[i]) +
            20 * Math.Sin(2 * Math.PI * 54 * t[i]) + 20 * Math.Sin(2 * Math.PI * 56 * t[i]) +
            30 * Math.Sin(2 * Math.PI * 59 * t[i]) + 20 * Math.Sin(2 * Math.PI * 83 * t[i]);
    }

    // 在 50~100 Hz 范围内做高分辨率分析
    int nfft = 64;
    double start = 50, stop = 100;
    Complex ratio = Complex.Exp(-Complex.ImaginaryOne * 2 * Math.PI * (stop - start) / (nfft * fs));
    var y = Fourier.ChirpZ(x, ratio, Complex.One, nfft);

    double[] f = Generate.LinearRange(0, 1 / (double)nfft, 1 - 1 / (double)nfft)
        .Select(num => (stop - start) * num + start).ToArray();
    easyChartX1.Plot(f, y.Absolute().Multiply(2.0 / nfft));
    easyChartX1.AxisX.Title = "频率 (Hz)";
    easyChartX1.AxisY.Title = "幅值";
}
```

---

### EX-Y12：功率谱干扰与泄漏分析

```csharp
using SeeSharpTools.JY.DSP.Utility.Spectrum;

private void InterferenceExample()
{
    double sampleRate = 100;
    double[] t = Generate.LinearRange(0, 1 / sampleRate, 2 - 1 / sampleRate);

    // 两个场景：相近频率(20+21Hz) vs 大小幅值(20Hz + 0.01*30Hz)
    double[] x1 = new double[t.Length];
    double[] x2 = new double[t.Length];
    for (int i = 0; i < t.Length; i++)
    {
        x1[i] = Math.Sin(2 * Math.PI * 20 * t[i]) + Math.Sin(2 * Math.PI * 21 * t[i]);
        x2[i] = Math.Sin(2 * Math.PI * 20 * t[i]) + 0.01 * Math.Sin(2 * Math.PI * 30 * t[i]);
    }

    double[,] x = ArrayUtility.BuildArray(x1, x2, Direction.Column);

    // PowerSpectrum（调整 leakage 参数控制窗函数）0~1）
    var (spectrum, f) = SpectralEstimation.PowerSpectrum(
        x, sampleRate, double.NaN, leakage: 0.5, twoSided: false, dB: true);
    easyChartX1.Plot(f, spectrum, SeeSharpTools.JY.GUI.MajorOrder.Column);
}
```

---

### 小结：DSP.Utility.Spectrum 选择指南

```
// ╔══════════════════════════════════════════════════════════════════════════════╗
// ║                DSP.Utility.Spectrum 方法选择指南                       ║
// ╠══════════════════════╤═══════════════════════════════════════╣
// ║ 应用场景               │ 推荐方法                                  ║
// ╟──────────────────────┼───────────────────────────────────────╢
// ║ 基础 FFT 频谱         │ Fourier.Forward                            ║
// ║ 单次功率谱             │ SpectralEstimation.Periodogram             ║
// ║ 平均功率谱（降噪）     │ SpectralEstimation.Pwelch                  ║
// ║ 实时平均幅度/相位谱  │ SpectralEstimation.AverageMagPhaseSpectrum ║
// ║ 指定频段高分辨率     │ Fourier.ChirpZ                             ║
// ║ 单音频率提取         │ SingleTone.ExtractSingleTone               ║
// ║ 多音频率提取         │ SingleTone.ExtractMultiTone                ║
// ║ SNR/THD/SFDR/SINAD   │ Distortion.SNR/THD/SFDR/SINAD             ║
// ║ 互调失真(TOI)        │ Distortion.TOI                             ║
// ║ 带宽/频率测量        │ SpectralFeature.BandPower/MeanFreq/...     ║
// ║ 交叉谱/相干性         │ SpectralEstimation.CrossPowerSpectralDensity ║
// ║ 传递函数估计         │ SpectralEstimation.TransferFunction        ║
// ║ Hilbert 解析信号     │ Hilbert.Forward                            ║
// ║ 窗函数生成            │ Windows.GetCoefficients / Windows.Hann     ║
// ║ dB转换               │ Conversion.MagTodB / PowerTodB             ║
// ╚══════════════════════╧═══════════════════════════════════════╝
```
