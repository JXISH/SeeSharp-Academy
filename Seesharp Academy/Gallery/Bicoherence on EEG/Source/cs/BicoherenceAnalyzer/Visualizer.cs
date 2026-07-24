using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.TickGenerators;

namespace BicoherenceAnalyzer;

/// <summary>
/// 双相干性可视化工具 — 论文出版级风格
/// 
/// 参考论文:
///   Hayashi K et al. "Anesthesia depth-dependent features of electroencephalographic 
///   bicoherence spectrum during sevoflurane anesthesia." Anesthesiology 2008;108:841-50.
/// 
/// 风格特征:
///   - 仅显三角区域 (f₁+f₂ ≤ maxFreq)，上方用白色遮蔽
///   - Jet 色谱 (蓝→青→绿→黄→红)
///   - 大号清晰字体 (标题 24px, 轴标签 20px, 刻度 16px)
///   - 白色背景, 黑色边框/刻度, 无网格
///   - 对角线黑色虚线标记 f₁=f₂
///   - 高分辨率输出 (1600×1400)
/// </summary>
public class Visualizer
{
    // === 字体大小 (出版级) ===
    private const float TitleFontSize = 24;
    private const float AxisLabelFontSize = 20;
    private const float TickLabelFontSize = 16;
    private const float ColorbarLabelFontSize = 16;

    // === 颜色 ===
    private static readonly ScottPlot.Color Black = ScottPlot.Colors.Black;
    private static readonly ScottPlot.Color White = ScottPlot.Colors.White;

    /// <summary>
    /// 创建统一样式的基础 Plot
    /// </summary>
    private static Plot CreateStyledPlot(string title, string xLabel, string yLabel,
        double plotMaxFreq)
    {
        Plot myPlot = new();

        // 背景
        myPlot.FigureBackground.Color = White;
        myPlot.DataBackground.Color = White;

        // 标题
        myPlot.Title(title);
        myPlot.Axes.Title.Label.FontSize = TitleFontSize;
        myPlot.Axes.Title.Label.Bold = true;
        myPlot.Axes.Title.Label.ForeColor = Black;

        // X 轴标签
        myPlot.Axes.Bottom.Label.FontSize = AxisLabelFontSize;
        myPlot.Axes.Bottom.Label.Bold = true;
        myPlot.Axes.Bottom.Label.ForeColor = Black;
        myPlot.Axes.Bottom.Label.Text = xLabel;

        // Y 轴标签
        myPlot.Axes.Left.Label.FontSize = AxisLabelFontSize;
        myPlot.Axes.Left.Label.Bold = true;
        myPlot.Axes.Left.Label.ForeColor = Black;
        myPlot.Axes.Left.Label.Text = yLabel;

        // 刻度标签
        myPlot.Axes.Bottom.TickLabelStyle.FontSize = TickLabelFontSize;
        myPlot.Axes.Bottom.TickLabelStyle.ForeColor = Black;
        myPlot.Axes.Left.TickLabelStyle.FontSize = TickLabelFontSize;
        myPlot.Axes.Left.TickLabelStyle.ForeColor = Black;

        // 主刻度线 — 清晰可见
        myPlot.Axes.Bottom.MajorTickStyle.Length = 6;
        myPlot.Axes.Bottom.MajorTickStyle.Width = 1.5f;
        myPlot.Axes.Bottom.MajorTickStyle.Color = Black;
        myPlot.Axes.Left.MajorTickStyle.Length = 6;
        myPlot.Axes.Left.MajorTickStyle.Width = 1.5f;
        myPlot.Axes.Left.MajorTickStyle.Color = Black;

        // 次刻度 — 完全隐藏 (避免杂乱)
        myPlot.Axes.Bottom.MinorTickStyle.Length = 0;
        myPlot.Axes.Left.MinorTickStyle.Length = 0;

        // 边框线
        float frameWidth = 1.5f;
        myPlot.Axes.Bottom.FrameLineStyle.Width = frameWidth;
        myPlot.Axes.Bottom.FrameLineStyle.Color = Black;
        myPlot.Axes.Left.FrameLineStyle.Width = frameWidth;
        myPlot.Axes.Left.FrameLineStyle.Color = Black;
        myPlot.Axes.Top.FrameLineStyle.Width = frameWidth;
        myPlot.Axes.Top.FrameLineStyle.Color = Black;
        myPlot.Axes.Right.FrameLineStyle.Width = frameWidth;
        myPlot.Axes.Right.FrameLineStyle.Color = Black;

        // 隐藏顶部/右侧刻度
        myPlot.Axes.Top.TickLabelStyle.FontSize = 0;
        myPlot.Axes.Right.TickLabelStyle.FontSize = 0;
        myPlot.Axes.Top.MajorTickStyle.Length = 0;
        myPlot.Axes.Right.MajorTickStyle.Length = 0;

        // 坐标轴范围 (底部/左侧留更多空间放频段标注)
        double rightMargin = 0.6;
        double topMargin = 0.6;
        double leftMargin = 5.0;   // 给左侧频段标签留空间
        double bottomMargin = 5.0;  // 给底部频段标签留空间
        myPlot.Axes.SetLimitsX(-leftMargin, plotMaxFreq + rightMargin);
        myPlot.Axes.SetLimitsY(-bottomMargin, plotMaxFreq + topMargin);

        // 隐藏网格
        myPlot.Grid.IsVisible = false;

        return myPlot;
    }

    /// <summary>
    /// 设置每 5 Hz 一个主刻度，清晰易读
    /// </summary>
    private static void SetCleanTicks(Plot myPlot, double maxFreq)
    {
        int tickInterval = 5; // 每 5 Hz 一个主刻度

        var xTicks = new NumericManual();
        var yTicks = new NumericManual();

        for (double f = 0; f <= maxFreq + 0.001; f += tickInterval)
        {
            string label = f.ToString("F0");
            xTicks.AddMajor(f, label);
            yTicks.AddMajor(f, label);
        }

        myPlot.Axes.Bottom.TickGenerator = xTicks;
        myPlot.Axes.Left.TickGenerator = yTicks;
    }

    /// <summary>
    /// 构建三角形遮罩热图数据 (f₁+f₂ ≤ maxFreq 为有效, 其余 NaN)
    /// 数据矩阵已做 Y 轴翻转 (使 f₂=0 在底部)
    /// </summary>
    private static double[,] BuildTriangleData(BispectrumResult result, int nFreq,
        double cellSize, double actualMaxFreq, bool useLogMagnitude = false)
    {
        double[,] data = new double[nFreq, nFreq];

        for (int f2 = 0; f2 < nFreq; f2++)
        {
            for (int f1 = 0; f1 < nFreq; f1++)
            {
                int row = nFreq - 1 - f2; // 翻转 Y: f2=0 → 底部行

                if (f1 * cellSize + f2 * cellSize <= actualMaxFreq + cellSize * 0.5)
                {
                    if (useLogMagnitude)
                    {
                        double mag = result.BispectrumMagnitude[f1, f2];
                        data[row, f1] = Math.Log10(Math.Max(mag, 1e-15));
                    }
                    else
                    {
                        data[row, f1] = result.Bicoherence[f1, f2] * 100.0;
                    }
                }
                else
                {
                    data[row, f1] = double.NaN;
                }
            }
        }

        return data;
    }

    /// <summary>
    /// 获取一致的数据维度参数
    /// </summary>
    private static (int nFreq, double actualMaxFreq) GetDataDimensions(
        BispectrumResult result, double displayMaxFreq)
    {
        double cellSize = result.FreqResolution;
        int maxIdx = Math.Min(result.MaxFreqIndex, (int)(displayMaxFreq / cellSize));
        int nFreq = maxIdx + 1;
        double actualMaxFreq = nFreq * cellSize;
        return (nFreq, actualMaxFreq);
    }

    // ═══════════════════════════════════════════════════════════
    //  Bicoherence 热图
    // ═══════════════════════════════════════════════════════════

    public void SaveBicoherenceHeatmap(BispectrumResult result, string outputPath,
        string channelName, string stateName, double displayMaxFreq = 40.0,
        double? colorMin = null, double? colorMax = null)
    {
        double cellSize = result.FreqResolution;
        var (nFreq, actualMaxFreq) = GetDataDimensions(result, displayMaxFreq);

        double[,] data = BuildTriangleData(result, nFreq, cellSize, actualMaxFreq,
            useLogMagnitude: false);

        string stateLabel = GetShortStateLabel(stateName);
        string title = $"{channelName} — {stateLabel}";

        Plot myPlot = CreateStyledPlot(title,
            "Frequency f₁ (Hz)", "Frequency f₂ (Hz)", actualMaxFreq);

        var hm = myPlot.Add.Heatmap(data);
        hm.Colormap = new Jet();
        hm.CellWidth = cellSize;
        hm.CellHeight = cellSize;
        hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
        hm.NaNCellColor = White;

        // 统一颜色范围（跨状态对比时使用）
        if (colorMin.HasValue && colorMax.HasValue)
            hm.ManualRange = new ScottPlot.Range(colorMin.Value, colorMax.Value);

        // 颜色条
        var colorBar = myPlot.Add.ColorBar(hm);
        colorBar.Label = "Bicoherence (%)";
        colorBar.LabelStyle.FontSize = ColorbarLabelFontSize;
        colorBar.LabelStyle.Bold = true;

        SetCleanTicks(myPlot, actualMaxFreq);
        AddDiagonalLine(myPlot, actualMaxFreq);
        AddFrequencyBandLabels(myPlot, actualMaxFreq);
        myPlot.Legend.IsVisible = false;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        myPlot.SavePng(outputPath, 1600, 1400);
        Console.WriteLine($"[可视] Bicoherence: {Path.GetFileName(outputPath)}");
    }

    // ═══════════════════════════════════════════════════════════
    //  双谱幅度热图
    // ═══════════════════════════════════════════════════════════

    public void SaveBispectrumMagnitudeHeatmap(BispectrumResult result, string outputPath,
        string channelName, string stateName, double displayMaxFreq = 40.0,
        double? colorMin = null, double? colorMax = null)
    {
        double cellSize = result.FreqResolution;
        var (nFreq, actualMaxFreq) = GetDataDimensions(result, displayMaxFreq);

        double[,] data = BuildTriangleData(result, nFreq, cellSize, actualMaxFreq,
            useLogMagnitude: true);

        string stateLabel = GetShortStateLabel(stateName);
        string title = $"{channelName} — {stateLabel}";

        Plot myPlot = CreateStyledPlot(title,
            "Frequency f₁ (Hz)", "Frequency f₂ (Hz)", actualMaxFreq);

        var hm = myPlot.Add.Heatmap(data);
        hm.Colormap = new Inferno();
        hm.CellWidth = cellSize;
        hm.CellHeight = cellSize;
        hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
        hm.NaNCellColor = White;

        // 统一颜色范围（跨状态对比时使用）
        if (colorMin.HasValue && colorMax.HasValue)
            hm.ManualRange = new ScottPlot.Range(colorMin.Value, colorMax.Value);

        var colorBar = myPlot.Add.ColorBar(hm);
        colorBar.Label = "log₁₀|B(f₁,f₂)|";
        colorBar.LabelStyle.FontSize = ColorbarLabelFontSize;
        colorBar.LabelStyle.Bold = true;

        SetCleanTicks(myPlot, actualMaxFreq);
        AddDiagonalLine(myPlot, actualMaxFreq);
        AddFrequencyBandLabels(myPlot, actualMaxFreq);
        myPlot.Legend.IsVisible = false;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        myPlot.SavePng(outputPath, 1600, 1400);
        Console.WriteLine($"[可视] Bispectrum: {Path.GetFileName(outputPath)}");
    }

    // ═══════════════════════════════════════════════════════════
    //  三状态对比图 — 水平并排布局
    // ═══════════════════════════════════════════════════════════

    public void SaveComparisonFigure(
        BispectrumResult baselineResult,
        BispectrumResult meditationResult,
        BispectrumResult recoveryResult,
        string outputPath,
        string channelName,
        double displayMaxFreq = 40.0)
    {
        double cellSize = baselineResult.FreqResolution;
        var (nFreq, actualMaxFreq) = GetDataDimensions(baselineResult, displayMaxFreq);

        BispectrumResult[] results = { baselineResult, meditationResult, recoveryResult };
        string[] stateLabels = { "Baseline", "Meditation", "Recovery" };
        string[] stateLabelsCN = { "冥想前", "冥想中", "冥想后" };
        string[] fileSuffixes = { "baseline", "meditation", "recovery" };

        // === 计算全局统一颜色范围 ===
        double globalMax = 0;
        foreach (var res in results)
        {
            for (int f1 = 0; f1 < nFreq; f1++)
            {
                for (int f2 = 0; f2 < nFreq; f2++)
                {
                    if (f1 * cellSize + f2 * cellSize <= actualMaxFreq + cellSize * 0.5)
                        globalMax = Math.Max(globalMax, res.Bicoherence[f1, f2] * 100.0);
                }
            }
        }
        if (globalMax < 5) globalMax = 5;
        globalMax = Math.Ceiling(globalMax / 5) * 5;
        if (globalMax > 100) globalMax = 100;

        // === 分别保存三个单图 (统一颜色范围) ===
        for (int idx = 0; idx < 3; idx++)
        {
            double[,] data = BuildTriangleData(results[idx], nFreq, cellSize, actualMaxFreq,
                useLogMagnitude: false);

            Plot myPlot = CreateStyledPlot(
                $"{channelName} — {stateLabelsCN[idx]}",
                "f₁ (Hz)", "f₂ (Hz)", actualMaxFreq);

            // 子图标题略小
            myPlot.Axes.Title.Label.FontSize = 18;

            var hm = myPlot.Add.Heatmap(data);
            hm.Colormap = new Jet();
            hm.CellWidth = cellSize;
            hm.CellHeight = cellSize;
            hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
            hm.NaNCellColor = White;
            hm.ManualRange = new ScottPlot.Range(0, globalMax);

            var colorBar = myPlot.Add.ColorBar(hm);
            colorBar.Label = "Bicoherence (%)";
            colorBar.LabelStyle.FontSize = 14;

            SetCleanTicks(myPlot, actualMaxFreq);
            AddDiagonalLine(myPlot, actualMaxFreq);
            myPlot.Legend.IsVisible = false;

            string dir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(dir);
            string fname = Path.GetFileNameWithoutExtension(outputPath);
            string ext = Path.GetExtension(outputPath);
            string singlePath = Path.Combine(dir, $"{fname}_{fileSuffixes[idx]}{ext}");
            myPlot.SavePng(singlePath, 800, 700);
            Console.WriteLine($"[可视] 对比子图: {Path.GetFileName(singlePath)}");
        }

        // === 创建三合一水平对比大图 ===
        try
        {
            var multiplot = new ScottPlot.Multiplot();

            for (int idx = 0; idx < 3; idx++)
            {
                double[,] data = BuildTriangleData(results[idx], nFreq, cellSize, actualMaxFreq,
                    useLogMagnitude: false);

                Plot subPlot = CreateStyledPlot(
                    stateLabelsCN[idx],
                    "f₁ (Hz)", "f₂ (Hz)", actualMaxFreq);

                subPlot.Axes.Title.Label.FontSize = 16;
                subPlot.Axes.Bottom.Label.FontSize = 14;
                subPlot.Axes.Left.Label.FontSize = 14;
                subPlot.Axes.Bottom.TickLabelStyle.FontSize = 12;
                subPlot.Axes.Left.TickLabelStyle.FontSize = 12;

                var hm = subPlot.Add.Heatmap(data);
                hm.Colormap = new Jet();
                hm.CellWidth = cellSize;
                hm.CellHeight = cellSize;
                hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
                hm.NaNCellColor = White;
                hm.ManualRange = new ScottPlot.Range(0, globalMax);

                SetCleanTicks(subPlot, actualMaxFreq);
                AddDiagonalLine(subPlot, actualMaxFreq);
                subPlot.Legend.IsVisible = false;

                // 每个子图缩小 margins
                subPlot.Axes.SetLimitsX(-0.3, actualMaxFreq + 0.3);
                subPlot.Axes.SetLimitsY(-0.3, actualMaxFreq + 0.3);

                multiplot.AddPlot(subPlot);
            }

            // 尝试水平排列 (ScottPlot 5.0.54 默认垂直堆叠)
            // 若 DraggableRows 不能水平排列，则使用垂直堆叠
            try
            {
                multiplot.Layout = new ScottPlot.MultiplotLayouts.DraggableRows();
            }
            catch
            {
                // 保持默认布局
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            multiplot.SavePng(outputPath, 2800, 1000);
            Console.WriteLine($"[可视] 三状态对比图: {Path.GetFileName(outputPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[可视] 三合一对比图失败 ({ex.Message})，请使用单独保存的三张子图。");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  辅助方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 添加对角线虚线 (f₁ = f₂)
    /// </summary>
    private static void AddDiagonalLine(Plot myPlot, double maxFreq)
    {
        double[] x = { 0, maxFreq };
        double[] y = { 0, maxFreq };

        var line = myPlot.Add.Scatter(x, y);
        line.Color = Black;
        line.LineWidth = 1.8f;
        line.LinePattern = LinePattern.DenselyDashed;
        line.MarkerSize = 0;
    }

    /// <summary>
    /// 在坐标轴上添加频段标注 (δ, θ, α, β)
    /// 用彩色矩形 + 文字标签标注每个频段在 X 轴和 Y 轴上的位置
    /// </summary>
    private static void AddFrequencyBandLabels(Plot myPlot, double maxFreq)
    {
        var bands = new (string Name, double Low, double High, ScottPlot.Color RectColor, ScottPlot.Color TextColor)[]
        {
            ("δ",   1,  4,  ScottPlot.Colors.DodgerBlue.WithAlpha(0.35),  ScottPlot.Colors.White),
            ("θ",   4,  8,  ScottPlot.Colors.Green.WithAlpha(0.35),       ScottPlot.Colors.White),
            ("α",   8,  13, ScottPlot.Colors.Crimson.WithAlpha(0.35),     ScottPlot.Colors.White),
            ("β",  13,  30, ScottPlot.Colors.DarkOrange.WithAlpha(0.35),  ScottPlot.Colors.White),
        };

        foreach (var (name, low, high, rectColor, textColor) in bands)
        {
            double mid = (low + high) / 2;

            // ── X 轴底部: 彩色矩形 ──
            var xRect = myPlot.Add.Rectangle(low, high, -3.8, -1.0);
            xRect.FillColor = rectColor;
            xRect.LineWidth = 0;

            // ── X 轴底部: 文字标签 (白色) ──
            var xLabel = myPlot.Add.Text(name, mid, -2.4);
            xLabel.LabelStyle.FontSize = 16;
            xLabel.LabelStyle.ForeColor = textColor;
            xLabel.LabelStyle.Bold = true;
            xLabel.Alignment = Alignment.MiddleCenter;
            xLabel.LabelStyle.FontName = "Microsoft YaHei";

            // ── Y 轴左侧: 彩色矩形 ──
            var yRect = myPlot.Add.Rectangle(-3.8, -1.0, low, high);
            yRect.FillColor = rectColor;
            yRect.LineWidth = 0;

            // ── Y 轴左侧: 文字标签 (白色, 旋转90°) ──
            var yLabel = myPlot.Add.Text(name, -2.4, mid);
            yLabel.LabelStyle.FontSize = 16;
            yLabel.LabelStyle.ForeColor = textColor;
            yLabel.LabelStyle.Bold = true;
            yLabel.Alignment = Alignment.MiddleCenter;
            yLabel.LabelStyle.FontName = "Microsoft YaHei";
        }
    }

    /// <summary>
    /// 简短状态标签
    /// </summary>
    private static string GetShortStateLabel(string state) => state switch
    {
        "baseline" => "冥想前 (Baseline)",
        "meditation" => "冥想中 (Meditation)",
        "recovery" => "冥想后 (Recovery)",
        _ => state
    };

    /// <summary>
    /// 完整状态标签 (用于标题)
    /// </summary>
    public static string GetStateDisplayName(string state) => state switch
    {
        "baseline" => "冥想前 (Baseline)",
        "meditation" => "冥想中 (Meditation)",
        "recovery" => "冥想后 (Recovery)",
        _ => state
    };

    // ═══════════════════════════════════════════════════════════
    //  Bispectrum 相位 (Biphase) 热图
    //  显示 arg(B(f₁,f₂)) ∈ [-π, +π]
    //  接近 0 或 ±π → 对称波形耦合
    //  接近 ±π/2 → 不对称波形 (如锯齿波)
    // ═══════════════════════════════════════════════════════════

    public void SaveBispectrumPhaseHeatmap(BispectrumResult result, string outputPath,
        string channelName, string stateName, double displayMaxFreq = 40.0,
        double? colorMin = null, double? colorMax = null)
    {
        double cellSize = result.FreqResolution;
        var (nFreq, actualMaxFreq) = GetDataDimensions(result, displayMaxFreq);

        // 构建相位数据 (弧度 → 度数，便于阅读)
        double[,] phaseData = new double[nFreq, nFreq];
        for (int f2 = 0; f2 < nFreq; f2++)
        {
            for (int f1 = 0; f1 < nFreq; f1++)
            {
                int row = nFreq - 1 - f2;
                if (f1 * cellSize + f2 * cellSize <= actualMaxFreq + cellSize * 0.5)
                {
                    double phaseRad = result.Bispectrum[f1, f2].Phase; // [-π, +π]
                    phaseData[row, f1] = phaseRad * 180.0 / Math.PI;   // 转为角度 [-180°, +180°]
                }
                else
                {
                    phaseData[row, f1] = double.NaN;
                }
            }
        }

        string stateLabel = GetShortStateLabel(stateName);
        string title = $"{channelName} — {stateLabel}";

        Plot myPlot = CreateStyledPlot(title,
            "Frequency f₁ (Hz)", "Frequency f₂ (Hz)", actualMaxFreq);

        var hm = myPlot.Add.Heatmap(phaseData);
        // Turbo 色谱对相位数据有较好的区分度
        hm.Colormap = new Turbo();
        hm.CellWidth = cellSize;
        hm.CellHeight = cellSize;
        hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
        hm.NaNCellColor = White;

        // 相位固定范围 [-180°, +180°]，天然统一；也可手动覆盖
        if (colorMin.HasValue && colorMax.HasValue)
            hm.ManualRange = new ScottPlot.Range(colorMin.Value, colorMax.Value);
        else
            hm.ManualRange = new ScottPlot.Range(-180, 180);

        var colorBar = myPlot.Add.ColorBar(hm);
        colorBar.Label = "Biphase (°)";
        colorBar.LabelStyle.FontSize = ColorbarLabelFontSize;
        colorBar.LabelStyle.Bold = true;

        SetCleanTicks(myPlot, actualMaxFreq);
        AddDiagonalLine(myPlot, actualMaxFreq);
        AddFrequencyBandLabels(myPlot, actualMaxFreq);
        myPlot.Legend.IsVisible = false;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        myPlot.SavePng(outputPath, 1600, 1400);
        Console.WriteLine($"[可视] Biphase: {Path.GetFileName(outputPath)}");
    }

    // ═══════════════════════════════════════════════════════════
    //  Bispectrum / Bicoherence 对角线曲线图 (f₁=f₂)
    //  X轴: 频率 (Hz), Y轴: 幅度或百分比
    //  参考论文 Figure 3 (Seo 2021)
    // ═══════════════════════════════════════════════════════════

    public void SaveBispectrumDiagonalPlot(BispectrumResult result, string outputPath,
        string channelName, string stateName, double maxDiagonalFreq = 40.0)
    {
        double cellSize = result.FreqResolution;
        int maxIdx = Math.Min(result.MaxFreqIndex, (int)(maxDiagonalFreq / cellSize));
        int n = maxIdx + 1;

        // 对角线数据: f1=f2=f
        double[] freqs = new double[n];
        double[] bicoherence = new double[n];  // b²(f,f) * 100
        double[] bispectrumMag = new double[n]; // |B(f,f)| (log10)

        for (int i = 0; i < n; i++)
        {
            freqs[i] = i * cellSize;
            bicoherence[i] = result.Bicoherence[i, i] * 100.0;
            double mag = result.BispectrumMagnitude[i, i];
            bispectrumMag[i] = Math.Log10(Math.Max(mag, 1e-15));
        }

        string stateLabel = GetShortStateLabel(stateName);

        // 使用 Multiplot 垂直排列: 上方 Bicoherence 对角, 下方 Bispectrum 对角
        var multiplot = new ScottPlot.Multiplot();

        // --- 图1: Bicoherence 对角线 ---
        Plot plotBic = new();
        plotBic.FigureBackground.Color = White;
        plotBic.DataBackground.Color = White;
        plotBic.Grid.IsVisible = true;
        plotBic.Grid.MajorLineColor = ScottPlot.Colors.LightGray;

        plotBic.Title($"{channelName} — {stateLabel}  (Diagonal Bicoherence)");
        plotBic.Axes.Title.Label.FontSize = 18;
        plotBic.Axes.Title.Label.Bold = true;

        plotBic.Axes.Bottom.Label.Text = "Frequency (Hz)";
        plotBic.Axes.Bottom.Label.FontSize = 16;
        plotBic.Axes.Left.Label.Text = "Bicoherence (%)";
        plotBic.Axes.Left.Label.FontSize = 16;

        plotBic.Axes.Bottom.TickLabelStyle.FontSize = 14;
        plotBic.Axes.Left.TickLabelStyle.FontSize = 14;

        var bicLine = plotBic.Add.Scatter(freqs, bicoherence);
        bicLine.Color = ScottPlot.Colors.Crimson;
        bicLine.LineWidth = 2.5f;
        bicLine.MarkerSize = 0;

        // 标注频段背景
        AddFrequencyBandShading(plotBic, 1, 4, ScottPlot.Colors.LightBlue.WithAlpha(0.3f), maxDiagonalFreq);  // δ
        AddFrequencyBandShading(plotBic, 4, 8, ScottPlot.Colors.LightGreen.WithAlpha(0.3f), maxDiagonalFreq); // θ
        AddFrequencyBandShading(plotBic, 8, 13, ScottPlot.Colors.LightPink.WithAlpha(0.3f), maxDiagonalFreq); // α

        plotBic.Axes.SetLimitsX(0, maxDiagonalFreq);
        plotBic.Legend.IsVisible = false;
        multiplot.AddPlot(plotBic);

        // --- 图2: Bispectrum 对角线 ---
        Plot plotBispec = new();
        plotBispec.FigureBackground.Color = White;
        plotBispec.DataBackground.Color = White;
        plotBispec.Grid.IsVisible = true;
        plotBispec.Grid.MajorLineColor = ScottPlot.Colors.LightGray;

        plotBispec.Title($"{channelName} — {stateLabel}  (Diagonal Bispectrum)");
        plotBispec.Axes.Title.Label.FontSize = 18;
        plotBispec.Axes.Title.Label.Bold = true;

        plotBispec.Axes.Bottom.Label.Text = "Frequency (Hz)";
        plotBispec.Axes.Bottom.Label.FontSize = 16;
        plotBispec.Axes.Left.Label.Text = "log₁₀|B(f,f)|";
        plotBispec.Axes.Left.Label.FontSize = 16;

        plotBispec.Axes.Bottom.TickLabelStyle.FontSize = 14;
        plotBispec.Axes.Left.TickLabelStyle.FontSize = 14;

        var bispecLine = plotBispec.Add.Scatter(freqs, bispectrumMag);
        bispecLine.Color = ScottPlot.Colors.DarkBlue;
        bispecLine.LineWidth = 2.5f;
        bispecLine.MarkerSize = 0;

        AddFrequencyBandShading(plotBispec, 1, 4, ScottPlot.Colors.LightBlue.WithAlpha(0.3f), maxDiagonalFreq);
        AddFrequencyBandShading(plotBispec, 4, 8, ScottPlot.Colors.LightGreen.WithAlpha(0.3f), maxDiagonalFreq);
        AddFrequencyBandShading(plotBispec, 8, 13, ScottPlot.Colors.LightPink.WithAlpha(0.3f), maxDiagonalFreq);

        plotBispec.Axes.SetLimitsX(0, maxDiagonalFreq);
        plotBispec.Legend.IsVisible = false;
        multiplot.AddPlot(plotBispec);

        try { multiplot.Layout = new ScottPlot.MultiplotLayouts.DraggableRows(); }
        catch { /* 默认布局 */ }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        multiplot.SavePng(outputPath, 1600, 1200);
        Console.WriteLine($"[可视] 对角线: {Path.GetFileName(outputPath)}");
    }

    // ═══════════════════════════════════════════════════════════
    //  Bispectrum 三状态对比图
    // ═══════════════════════════════════════════════════════════

    public void SaveBispectrumComparisonFigure(
        BispectrumResult baselineResult,
        BispectrumResult meditationResult,
        BispectrumResult recoveryResult,
        string outputPath,
        string channelName,
        double displayMaxFreq = 40.0)
    {
        double cellSize = baselineResult.FreqResolution;
        var (nFreq, actualMaxFreq) = GetDataDimensions(baselineResult, displayMaxFreq);

        BispectrumResult[] results = { baselineResult, meditationResult, recoveryResult };
        string[] stateLabelsCN = { "冥想前", "冥想中", "冥想后" };
        string[] fileSuffixes = { "baseline", "meditation", "recovery" };

        // 计算全局 log 幅度范围
        double globalMin = double.MaxValue, globalMax = double.MinValue;
        foreach (var res in results)
        {
            for (int f1 = 0; f1 < nFreq; f1++)
            {
                for (int f2 = 0; f2 < nFreq; f2++)
                {
                    if (f1 * cellSize + f2 * cellSize <= actualMaxFreq + cellSize * 0.5)
                    {
                        double mag = res.BispectrumMagnitude[f1, f2];
                        double logMag = Math.Log10(Math.Max(mag, 1e-15));
                        globalMin = Math.Min(globalMin, logMag);
                        globalMax = Math.Max(globalMax, logMag);
                    }
                }
            }
        }
        if (double.IsInfinity(globalMin)) globalMin = 0;
        if (double.IsInfinity(globalMax)) globalMax = 10;

        // 保存三个单图 (统一颜色范围)
        for (int idx = 0; idx < 3; idx++)
        {
            double[,] data = BuildTriangleData(results[idx], nFreq, cellSize, actualMaxFreq,
                useLogMagnitude: true);

            Plot myPlot = CreateStyledPlot(
                $"{channelName} — {stateLabelsCN[idx]}",
                "f₁ (Hz)", "f₂ (Hz)", actualMaxFreq);
            myPlot.Axes.Title.Label.FontSize = 18;

            var hm = myPlot.Add.Heatmap(data);
            hm.Colormap = new Inferno();
            hm.CellWidth = cellSize;
            hm.CellHeight = cellSize;
            hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
            hm.NaNCellColor = White;
            hm.ManualRange = new ScottPlot.Range(globalMin, globalMax);

            var colorBar = myPlot.Add.ColorBar(hm);
            colorBar.Label = "log₁₀|B(f₁,f₂)|";
            colorBar.LabelStyle.FontSize = 14;

            SetCleanTicks(myPlot, actualMaxFreq);
            AddDiagonalLine(myPlot, actualMaxFreq);
            myPlot.Legend.IsVisible = false;

            string dir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(dir);
            string fname = Path.GetFileNameWithoutExtension(outputPath);
            string ext = Path.GetExtension(outputPath);
            string singlePath = Path.Combine(dir, $"{fname}_{fileSuffixes[idx]}{ext}");
            myPlot.SavePng(singlePath, 800, 700);
            Console.WriteLine($"[可视] Bispectrum对比子图: {Path.GetFileName(singlePath)}");
        }

        // 三合一水平大图
        try
        {
            var multiplot = new ScottPlot.Multiplot();
            for (int idx = 0; idx < 3; idx++)
            {
                double[,] data = BuildTriangleData(results[idx], nFreq, cellSize, actualMaxFreq,
                    useLogMagnitude: true);

                Plot subPlot = CreateStyledPlot(stateLabelsCN[idx], "f₁ (Hz)", "f₂ (Hz)", actualMaxFreq);
                subPlot.Axes.Title.Label.FontSize = 16;
                subPlot.Axes.Bottom.Label.FontSize = 14;
                subPlot.Axes.Left.Label.FontSize = 14;
                subPlot.Axes.Bottom.TickLabelStyle.FontSize = 12;
                subPlot.Axes.Left.TickLabelStyle.FontSize = 12;

                var hm = subPlot.Add.Heatmap(data);
                hm.Colormap = new Inferno();
                hm.CellWidth = cellSize;
                hm.CellHeight = cellSize;
                hm.Rectangle = new CoordinateRect(0, actualMaxFreq, 0, actualMaxFreq);
                hm.NaNCellColor = White;
                hm.ManualRange = new ScottPlot.Range(globalMin, globalMax);

                SetCleanTicks(subPlot, actualMaxFreq);
                AddDiagonalLine(subPlot, actualMaxFreq);
                subPlot.Legend.IsVisible = false;
                subPlot.Axes.SetLimitsX(-0.3, actualMaxFreq + 0.3);
                subPlot.Axes.SetLimitsY(-0.3, actualMaxFreq + 0.3);

                multiplot.AddPlot(subPlot);
            }

            try { multiplot.Layout = new ScottPlot.MultiplotLayouts.DraggableRows(); }
            catch { }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            multiplot.SavePng(outputPath, 2800, 1000);
            Console.WriteLine($"[可视] Bispectrum三状态对比: {Path.GetFileName(outputPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[可视] Bispectrum三合一对比失败 ({ex.Message})，请使用单图。");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  频段背景着色 (用于对角线图)
    // ═══════════════════════════════════════════════════════════

    private static void AddFrequencyBandShading(Plot myPlot, double lowHz, double highHz,
        ScottPlot.Color fillColor, double yTop)
    {
        var rect = myPlot.Add.Rectangle(lowHz, highHz, 0, yTop);
        rect.FillColor = fillColor;
        rect.LineWidth = 0;
    }
}
