using BicoherenceAnalyzer;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.TickGenerators;
using ScottPlot.WinForms;
using Label = System.Windows.Forms.Label;
using FontStyle = System.Drawing.FontStyle;
using Font = System.Drawing.Font;

namespace BicoherenceViewer;

/// <summary>状态时间段（本地副本，避免跨项目 record 引用问题）</summary>
public record StateInfo(string State, double StartTimeSec, double EndTimeSec, double DurationSec);

public partial class MainForm : Form
{
    // ── 数据 ──
    private BdfReader? _bdfReader;
    private List<StateInfo> _stateSegments = new();
    private BispectrumCalculator? _calculator;
    private readonly Dictionary<string, BispectrumResult> _resultCache = new();
    private string _dataRoot = @"e:\数据集";

    // ── 当前选择 ──
    private string _subject = "sub-001";
    private string _session = "ses-01";
    private string _channel = "Pz";
    private string _analysisType = "Bicoherence"; // Bicoherence / Bispectrum / Biphase
    private double _maxDisplayFreq = 30;
    private double _bicoherenceColorMax = 100;
    private double _bispectrumColorMin = -5, _bispectrumColorMax = 5;

    // ── UI 控件 ──
    private ComboBox _cbSubject = null!, _cbSession = null!, _cbChannel = null!, _cbType = null!;
    private TrackBar _tbMaxFreq = null!, _tbBicoMax = null!;
    private Label _lblFreq = null!, _lblBicoMax = null!;
    private Button _btnRefresh = null!, _btnExport = null!;
    private FormsPlot _plotBase = null!, _plotMed = null!, _plotRec = null!, _plotDiag = null!;
    private Label _lblInfo = null!;

    public MainForm()
    {
        Text = "EEG Bicoherence 交互分析器";
        Size = new Size(1650, 1050);
        StartPosition = FormStartPosition.CenterScreen;
        InitializeUI();
        Load += async (_, _) => await InitializeDataAsync();
    }

    // ═════════════════════════════════════════════════════════
    //  UI 布局
    // ═════════════════════════════════════════════════════════

    private void InitializeUI()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(8)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(mainLayout);

        // ── Row 0: 控制栏 ──
        var ctrlPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 11, RowCount = 2, Padding = new Padding(4)
        };
        for (int i = 0; i < 11; i++)
            ctrlPanel.ColumnStyles.Add(new ColumnStyle(i >= 8 ? SizeType.Absolute : SizeType.AutoSize, i >= 8 ? 100 : 0));
        ctrlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        ctrlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        mainLayout.Controls.Add(ctrlPanel, 0, 0);
        mainLayout.SetColumnSpan(ctrlPanel, 3);

        // Row 0
        AddLabel(ctrlPanel, "被试:", 0, 0);
        _cbSubject = AddCombo(ctrlPanel, 1, 0, 100);
        AddLabel(ctrlPanel, "会话:", 2, 0);
        _cbSession = AddCombo(ctrlPanel, 3, 0, 100);
        AddLabel(ctrlPanel, "通道:", 4, 0);
        _cbChannel = AddCombo(ctrlPanel, 5, 0, 80, new[] { "Fz", "Cz", "Pz" });
        AddLabel(ctrlPanel, "类型:", 6, 0);
        _cbType = AddCombo(ctrlPanel, 7, 0, 120, new[] { "Bicoherence", "Bispectrum", "Biphase" });
        _btnRefresh = new Button { Text = "刷新 (F5)", Dock = DockStyle.Fill, Margin = new Padding(4, 2, 4, 2) };
        _btnRefresh.Click += (_, _) => RefreshPlots();
        ctrlPanel.Controls.Add(_btnRefresh, 8, 0);
        ctrlPanel.SetRowSpan(_btnRefresh, 2);
        _btnExport = new Button { Text = "导出 PNG", Dock = DockStyle.Fill, Margin = new Padding(4, 2, 4, 2) };
        _btnExport.Click += (_, _) => ExportCurrentView();
        ctrlPanel.Controls.Add(_btnExport, 9, 0);
        ctrlPanel.SetRowSpan(_btnExport, 2);

        // Row 1: 滑块
        AddLabel(ctrlPanel, "最大频率:", 0, 1);
        _tbMaxFreq = new TrackBar
        {
            Minimum = 10, Maximum = 45, Value = (int)_maxDisplayFreq,
            TickFrequency = 5, Dock = DockStyle.Fill, Margin = new Padding(2)
        };
        _tbMaxFreq.Scroll += (_, _) => { _maxDisplayFreq = _tbMaxFreq.Value; UpdateSliderLabels(); RefreshPlots(); };
        ctrlPanel.Controls.Add(_tbMaxFreq, 1, 1);
        ctrlPanel.SetColumnSpan(_tbMaxFreq, 2);
        _lblFreq = AddLabel(ctrlPanel, $"{_maxDisplayFreq} Hz", 3, 1);

        AddLabel(ctrlPanel, "Bicoherence 上限:", 4, 1);
        _tbBicoMax = new TrackBar
        {
            Minimum = 5, Maximum = 100, Value = (int)_bicoherenceColorMax,
            TickFrequency = 5, Dock = DockStyle.Fill, Margin = new Padding(2)
        };
        _tbBicoMax.Scroll += (_, _) => { _bicoherenceColorMax = _tbBicoMax.Value; UpdateSliderLabels(); RefreshPlots(); };
        ctrlPanel.Controls.Add(_tbBicoMax, 5, 1);
        ctrlPanel.SetColumnSpan(_tbBicoMax, 2);
        _lblBicoMax = AddLabel(ctrlPanel, $"{_bicoherenceColorMax}%", 7, 1);

        // ── Row 1+2: 三个热图 + 对角线 ──
        _plotBase = CreatePlotPanel("Baseline (冥想前)");
        _plotMed = CreatePlotPanel("Meditation (冥想中)");
        _plotRec = CreatePlotPanel("Recovery (冥想后)");
        mainLayout.Controls.Add(_plotBase, 0, 1);
        mainLayout.Controls.Add(_plotMed, 1, 1);
        mainLayout.Controls.Add(_plotRec, 2, 1);

        _plotDiag = CreatePlotPanel("Diagonal Bicoherence (f₁=f₂)");
        mainLayout.Controls.Add(_plotDiag, 0, 2);
        mainLayout.SetColumnSpan(_plotDiag, 3);

        // ── Row 3: 状态栏 ──
        _lblInfo = new Label { Text = "就绪", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        mainLayout.Controls.Add(_lblInfo, 0, 3);
        mainLayout.SetColumnSpan(_lblInfo, 3);

        // 事件绑定
        _cbSubject.SelectedIndexChanged += async (_, _) => await OnSubjectChanged();
        _cbSession.SelectedIndexChanged += async (_, _) => await OnSessionChanged();
        _cbChannel.SelectedIndexChanged += (_, _) => OnParamChanged();
        _cbType.SelectedIndexChanged += (_, _) => OnParamChanged();
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.F5) RefreshPlots(); };
    }

    private static Label AddLabel(TableLayoutPanel panel, string text, int col, int row)
    {
        var lbl = new Label
        {
            Text = text, TextAlign = ContentAlignment.MiddleRight,
            AutoSize = true, Margin = new Padding(4, 0, 2, 0)
        };
        panel.Controls.Add(lbl, col, row);
        return lbl;
    }

    private static ComboBox AddCombo(TableLayoutPanel panel, int col, int row, int width, string[]? items = null)
    {
        var cb = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = width, Margin = new Padding(2)
        };
        if (items != null) cb.Items.AddRange(items);
        panel.Controls.Add(cb, col, row);
        return cb;
    }

    private static FormsPlot CreatePlotPanel(string title)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4) };
        var lbl = new Label
        {
            Text = title, Dock = DockStyle.Top, Height = 22,
            TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
        };
        var plot = new FormsPlot { Dock = DockStyle.Fill };
        panel.Controls.Add(plot);
        panel.Controls.Add(lbl);
        return plot;
    }

    private void UpdateSliderLabels()
    {
        _lblFreq.Text = $"{_maxDisplayFreq} Hz";
        _lblBicoMax.Text = $"{_bicoherenceColorMax}%";
    }

    // ═════════════════════════════════════════════════════════
    //  数据加载
    // ═════════════════════════════════════════════════════════

    private async Task InitializeDataAsync()
    {
        _lblInfo.Text = "正在扫描被试列表...";
        var subs = Directory.GetDirectories(_dataRoot, "sub-*")
            .Select(Path.GetFileName).Where(d => d != null).Cast<string>()
            .OrderBy(s => s).ToArray();
        foreach (var s in subs) _cbSubject.Items.Add(s);
        _cbSubject.SelectedItem = _subject;
    }

    private async Task OnSubjectChanged()
    {
        if (_cbSubject.SelectedItem is not string s) return;
        _subject = s;
        _cbSession.Items.Clear();
        var subDir = Path.Combine(_dataRoot, _subject);
        if (!Directory.Exists(subDir)) return;

        var sessions = Directory.GetDirectories(subDir, "ses-*")
            .Select(Path.GetFileName).Where(d => d != null).Cast<string>()
            .OrderBy(x => x).ToArray();
        foreach (var ses in sessions) _cbSession.Items.Add(ses);
        if (sessions.Length > 0) _cbSession.SelectedIndex = 0;
    }

    private async Task OnSessionChanged()
    {
        if (_cbSession.SelectedItem is not string ses) return;
        _session = ses;
        await LoadBdfAndEvents();
        OnParamChanged();
    }

    private async Task LoadBdfAndEvents()
    {
        _lblInfo.Text = $"加载 {_subject}/{_session}...";
        _bdfReader?.Dispose();
        _resultCache.Clear();

        string subDir = Path.Combine(_dataRoot, _subject, _session, "eeg");
        if (!Directory.Exists(subDir)) { _lblInfo.Text = "数据目录不存在"; return; }

        var bdfFiles = Directory.GetFiles(subDir, "*_eeg.bdf");
        var tsvFiles = Directory.GetFiles(subDir, "*_events.tsv");
        if (bdfFiles.Length == 0 || tsvFiles.Length == 0) { _lblInfo.Text = "找不到 BDF/TSV"; return; }

        _bdfReader = new BdfReader(bdfFiles[0]);
        var eventReader = new EventReader(tsvFiles[0]);
        double totalDuration = _bdfReader.NumDataRecords * _bdfReader.RecordDuration;
        var segs = eventReader.ExtractStateSegments(totalDuration, 300);
        _stateSegments = segs.Select(s => new StateInfo(s.State, s.StartTimeSec, s.EndTimeSec, s.DurationSec)).ToList();
        _calculator = new BispectrumCalculator(256, 512, 256, 45);
        _lblInfo.Text = $"已加载: {_bdfReader.NumDataRecords} 记录, {_stateSegments.Count} 个状态段";
    }

    // ═════════════════════════════════════════════════════════
    //  参数变化处理
    // ═════════════════════════════════════════════════════════

    private void OnParamChanged()
    {
        if (_cbChannel.SelectedItem is string ch) _channel = ch;
        if (_cbType.SelectedItem is string t) _analysisType = t;
        ComputeCurrentChannel();
        RefreshPlots();
    }

    private void ComputeCurrentChannel()
    {
        if (_bdfReader == null || _stateSegments.Count == 0 || _calculator == null) return;

        string channelKey = $"{_subject}/{_session}/{_channel}";
        if (_resultCache.ContainsKey(channelKey)) return;

        _lblInfo.Text = $"正在计算 {_channel}...";
        int chIdx = FindChannelIndex(_bdfReader, _channel);
        if (chIdx < 0) { _lblInfo.Text = $"通道 {_channel} 未找到"; return; }

        foreach (var seg in _stateSegments)
        {
            double[] data = _bdfReader.ReadChannelDataByTime(chIdx, seg.StartTimeSec, seg.DurationSec, 256);
            if (data.Length < 512) continue;
            var result = _calculator.Compute(data);
            _resultCache[$"{channelKey}/{seg.State}"] = result;
        }
        _lblInfo.Text = $"{_channel} 计算完成 ({_stateSegments.Count} 个状态)";
    }

    private static int FindChannelIndex(BdfReader reader, string name)
    {
        // 用已知的 BDF 索引映射（与 console program 的 channels.tsv 方式一致）
        return name switch
        {
            "Pz" => 30,
            "Fz" => 37,
            "Cz" => 47,
            _ => -1
        };
    }

    // ═════════════════════════════════════════════════════════
    //  绘图核心
    // ═════════════════════════════════════════════════════════

    private void RefreshPlots()
    {
        if (_resultCache.Count == 0) return;

        string channelKey = $"{_subject}/{_session}/{_channel}";
        string[] states = { "baseline", "meditation", "recovery" };
        FormsPlot[] plots = { _plotBase, _plotMed, _plotRec };
        string[] stateLabels = { "冥想前", "冥想中", "冥想后" };

        // 获取全局颜色范围
        double globalMax = _analysisType == "Bicoherence" ? _bicoherenceColorMax : 100;
        double logMin = _bispectrumColorMin, logMax = _bispectrumColorMax;
        if (_analysisType == "Bispectrum")
        {
            logMin = double.MaxValue; logMax = double.MinValue;
            foreach (var st in states)
            {
                if (!_resultCache.TryGetValue($"{channelKey}/{st}", out var res)) continue;
                int nFreq = Math.Min(res.MaxFreqIndex, (int)(_maxDisplayFreq / res.FreqResolution)) + 1;
                double cell = res.FreqResolution;
                for (int f1 = 0; f1 < nFreq; f1++)
                    for (int f2 = 0; f2 < nFreq; f2++)
                        if (f1 * cell + f2 * cell <= nFreq * cell + cell * 0.5)
                        {
                            double mag = res.BispectrumMagnitude[f1, f2];
                            double lm = Math.Log10(Math.Max(mag, 1e-15));
                            logMin = Math.Min(logMin, lm);
                            logMax = Math.Max(logMax, lm);
                        }
            }
        }

        // 渲染三个热图
        for (int i = 0; i < 3; i++)
        {
            if (!_resultCache.TryGetValue($"{channelKey}/{states[i]}", out var result)) continue;
            RenderHeatmap(plots[i], result, $"{_channel} — {stateLabels[i]}", globalMax, logMin, logMax);
        }

        // 渲染对角线图
        if (_resultCache.TryGetValue($"{channelKey}/baseline", out var baseResult))
            RenderDiagonal(_plotDiag, $"{_channel} — 对角线 Bicoherence");

        _lblInfo.Text = $"{_channel} | {_analysisType} | 频率 0-{_maxDisplayFreq} Hz | 色域 max={globalMax:F0}";
    }

    private void RenderHeatmap(FormsPlot fp, BispectrumResult result, string title,
        double bicoMax, double logMin, double logMax)
    {
        fp.Reset();
        var plt = fp.Plot;
        double cell = result.FreqResolution;
        int maxIdx = Math.Min(result.MaxFreqIndex, (int)(_maxDisplayFreq / cell));
        int nFreq = maxIdx + 1;
        double actualMax = nFreq * cell;

        // 构建数据
        double[,] data = new double[nFreq, nFreq];
        for (int f2 = 0; f2 < nFreq; f2++)
        {
            for (int f1 = 0; f1 < nFreq; f1++)
            {
                int row = nFreq - 1 - f2;
                if (f1 * cell + f2 * cell <= actualMax + cell * 0.5)
                {
                    data[row, f1] = _analysisType switch
                    {
                        "Bicoherence" => result.Bicoherence[f1, f2] * 100.0,
                        "Bispectrum" => Math.Log10(Math.Max(result.BispectrumMagnitude[f1, f2], 1e-15)),
                        "Biphase" => result.Bispectrum[f1, f2].Phase * 180.0 / Math.PI,
                        _ => 0
                    };
                }
                else
                    data[row, f1] = double.NaN;
            }
        }

        // 热图
        var hm = plt.Add.Heatmap(data);
        hm.Colormap = _analysisType switch
        {
            "Bicoherence" => new Jet(),
            "Bispectrum" => new Inferno(),
            "Biphase" => new Turbo(),
            _ => new Jet()
        };
        hm.CellWidth = cell;
        hm.CellHeight = cell;
        hm.Rectangle = new CoordinateRect(0, actualMax, 0, actualMax);
        hm.NaNCellColor = ScottPlot.Colors.White;

        if (_analysisType == "Bicoherence")
            hm.ManualRange = new ScottPlot.Range(0, bicoMax);
        else if (_analysisType == "Bispectrum")
            hm.ManualRange = new ScottPlot.Range(logMin, logMax);
        else
            hm.ManualRange = new ScottPlot.Range(-180, 180);

        // 颜色条
        var cb = plt.Add.ColorBar(hm);
        cb.Label = _analysisType switch
        {
            "Bicoherence" => "Bicoherence (%)",
            "Bispectrum" => "log₁₀|B|",
            "Biphase" => "Phase (°)",
            _ => ""
        };
        cb.LabelStyle.FontSize = 12;

        // 样式
        plt.Axes.Title.Label.Text = title;
        plt.Axes.Title.Label.FontSize = 16;
        plt.Axes.Bottom.Label.Text = "f₁ (Hz)";
        plt.Axes.Left.Label.Text = "f₂ (Hz)";
        plt.Axes.Bottom.Label.FontSize = 13;
        plt.Axes.Left.Label.FontSize = 13;
        plt.Axes.Bottom.TickLabelStyle.FontSize = 11;
        plt.Axes.Left.TickLabelStyle.FontSize = 11;
        plt.Grid.IsVisible = false;
        plt.Axes.SetLimitsX(-4, actualMax + 0.6);
        plt.Axes.SetLimitsY(-4, actualMax + 0.6);

        // 对角线
        var diag = plt.Add.Scatter(new double[] { 0, actualMax }, new double[] { 0, actualMax });
        diag.Color = ScottPlot.Colors.Black;
        diag.LineWidth = 1.5f;
        diag.LinePattern = LinePattern.DenselyDashed;
        diag.MarkerSize = 0;

        // 频段标签（简化版，只标注 X 轴）
        AddSimpleBandLabels(plt, actualMax);

        fp.Refresh();
    }

    private void RenderDiagonal(FormsPlot fp, string title)
    {
        fp.Reset();
        var plt = fp.Plot;
        string channelKey = $"{_subject}/{_session}/{_channel}";
        string[] states = { "baseline", "meditation", "recovery" };
        ScottPlot.Color[] colors = { ScottPlot.Colors.DodgerBlue, ScottPlot.Colors.Crimson, ScottPlot.Colors.DarkOrange };
        string[] labels = { "冥想前", "冥想中", "冥想后" };

        double globalMax = 0;
        for (int i = 0; i < 3; i++)
        {
            if (!_resultCache.TryGetValue($"{channelKey}/{states[i]}", out var res)) continue;
            for (int f = 0; f < res.Frequencies.Length && f <= (int)(_maxDisplayFreq / res.FreqResolution); f++)
                globalMax = Math.Max(globalMax, res.Bicoherence[f, f] * 100);
        }
        if (globalMax < 5) globalMax = 5;

        for (int i = 0; i < 3; i++)
        {
            if (!_resultCache.TryGetValue($"{channelKey}/{states[i]}", out var res)) continue;
            int n = Math.Min(res.Frequencies.Length, (int)(_maxDisplayFreq / res.FreqResolution) + 1);
            double[] freqs = res.Frequencies.Take(n).ToArray();
            double[] vals = new double[n];
            for (int f = 0; f < n; f++) vals[f] = res.Bicoherence[f, f] * 100;
            var sp = plt.Add.Scatter(freqs, vals);
            sp.Color = colors[i];
            sp.LineWidth = 2.5f;
            sp.MarkerSize = 0;
            sp.LegendText = labels[i];
        }

        plt.Axes.Title.Label.Text = title;
        plt.Axes.Title.Label.FontSize = 14;
        plt.Axes.Bottom.Label.Text = "Frequency (Hz)";
        plt.Axes.Left.Label.Text = "Bicoherence (%)";
        plt.Axes.SetLimitsX(0, _maxDisplayFreq);
        plt.Axes.SetLimitsY(0, globalMax * 1.1);
        plt.Grid.IsVisible = true;
        plt.Grid.MajorLineColor = ScottPlot.Colors.Gray.WithAlpha(0.3);
        plt.ShowLegend();
        plt.Legend.FontSize = 12;
        plt.Legend.Alignment = Alignment.UpperRight;
        fp.Refresh();
    }

    private static void AddSimpleBandLabels(Plot plt, double maxFreq)
    {
        var bands = new (string name, double low, double high, ScottPlot.Color color)[]
        {
            ("δ", 1, 4, ScottPlot.Colors.DodgerBlue.WithAlpha(0.3)),
            ("θ", 4, 8, ScottPlot.Colors.Green.WithAlpha(0.3)),
            ("α", 8, 13, ScottPlot.Colors.Crimson.WithAlpha(0.3)),
            ("β", 13, 30, ScottPlot.Colors.DarkOrange.WithAlpha(0.3)),
        };
        foreach (var (name, low, high, color) in bands)
        {
            if (low > maxFreq) break;
            double h = Math.Min(high, maxFreq);
            var rect = plt.Add.Rectangle(low, h, -3.5, -1.0);
            rect.FillColor = color;
            rect.LineWidth = 0;
            var txt = plt.Add.Text(name, (low + h) / 2, -2.25);
            txt.LabelStyle.FontSize = 13;
            txt.LabelStyle.ForeColor = ScottPlot.Colors.White;
            txt.LabelStyle.Bold = true;
            txt.Alignment = Alignment.MiddleCenter;
        }
    }

    // ═════════════════════════════════════════════════════════
    //  导出
    // ═════════════════════════════════════════════════════════

    private void ExportCurrentView()
    {
        using var sfd = new SaveFileDialog
        {
            Title = "导出当前视图",
            Filter = "PNG图片|*.png",
            FileName = $"bicoherence_{_subject}_{_channel}_{_analysisType}.png"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        string dir = Path.GetDirectoryName(sfd.FileName)!;
        string baseName = Path.GetFileNameWithoutExtension(sfd.FileName);
        string ext = Path.GetExtension(sfd.FileName);

        // 使用 Visualizer 导出高质量图片
        var vis = new Visualizer();
        string channelKey = $"{_subject}/{_session}/{_channel}";
        string[] states = { "baseline", "meditation", "recovery" };
        string[] labels = { "冥想前 (Baseline)", "冥想中 (Meditation)", "冥想后 (Recovery)" };

        for (int i = 0; i < 3; i++)
        {
            if (!_resultCache.TryGetValue($"{channelKey}/{states[i]}", out var res)) continue;
            string path = Path.Combine(dir, $"{baseName}_{states[i]}{ext}");
            if (_analysisType == "Bicoherence")
                vis.SaveBicoherenceHeatmap(res, path, _channel, labels[i], _maxDisplayFreq, 0, _bicoherenceColorMax);
            else if (_analysisType == "Bispectrum")
                vis.SaveBispectrumMagnitudeHeatmap(res, path, _channel, labels[i], _maxDisplayFreq, _bispectrumColorMin, _bispectrumColorMax);
            else
                vis.SaveBispectrumPhaseHeatmap(res, path, _channel, labels[i], _maxDisplayFreq);
        }
        MessageBox.Show($"已导出到:\n{dir}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
