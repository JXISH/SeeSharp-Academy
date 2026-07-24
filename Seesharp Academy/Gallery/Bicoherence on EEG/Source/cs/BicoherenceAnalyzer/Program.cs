using System.Diagnostics;

namespace BicoherenceAnalyzer;

/// <summary>
/// EEG双谱/双相干性分析 - 主程序
/// 
/// 功能：读取BDF格式的EEG冥想数据，计算Bispectrum和Bicoherence，
///       生成论文风格的彩色热图，提取α/δ/θ峰特征。
/// 
/// 数据来源：OpenNeuro数据集 ds001787 (EEG meditation study)
/// 参考论文：Changes in Electroencephalographic Bicoherence During Sevoflurane Anesthesia
/// </summary>
class Program
{
    // === 配置参数（可修改） ===

    /// <summary>数据集根目录</summary>
    static string DataRoot = @"e:\数据集";

    /// <summary>被试ID</summary>
    static string SubjectId = "sub-001";

    /// <summary>会话ID（留空则自动检测第一个可用的ses）</summary>
    static string SessionId = "ses-01";

    /// <summary>结果输出目录</summary>
    static string ResultsRoot = @"e:\数据集\results";

    /// <summary>目标分析通道（论文中的三个中线通道）</summary>
    static string[] TargetChannels = { "Fz", "Cz", "Pz" };

    /// <summary>采样率 (Hz)</summary>
    static int SampleRate = 256;

    /// <summary>FFT分段长度（约2秒，频率分辨率0.5Hz）</summary>
    static int Nfft = 512;

    /// <summary>重叠点数（50%重叠）</summary>
    static int Noverlap = 256;

    /// <summary>最大分析频率 (Hz)</summary>
    static double MaxFrequency = 45.0;

    /// <summary>显示的最大频率 (Hz) — 聚焦 δ/θ/α/β 关键频段</summary>
    static double DisplayMaxFreq = 30.0;

    /// <summary>每个状态的分析时长（秒），默认5分钟</summary>
    static double SegmentDurationSec = 300.0;

    static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║     EEG 双谱/双相干性 分析程序 (Bicoherence)       ║");
        Console.WriteLine("║     数据: OpenNeuro ds001787 - Meditation Study     ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // 解析命令行参数
        ParseCommandLine(args);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await RunAnalysis();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[错误] 分析过程中发生异常:");
            Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"  {ex.StackTrace}");
            return;
        }

        stopwatch.Stop();
        Console.WriteLine($"\n══════════════════════════════════════════════");
        Console.WriteLine($"  分析完成！总耗时: {stopwatch.Elapsed.TotalMinutes:F1} 分钟");
        Console.WriteLine($"  结果保存在: {ResultsRoot}\\{SubjectId}\\");
        Console.WriteLine($"══════════════════════════════════════════════");
    }

    static async Task RunAnalysis()
    {
        // === 步骤1: 定位数据文件 ===
        Console.WriteLine("[步骤1] 定位数据文件...");
        string subjectDir = Path.Combine(DataRoot, SubjectId);

        if (!Directory.Exists(subjectDir))
            throw new DirectoryNotFoundException($"被试目录不存在: {subjectDir}");

        // 自动检测可用的session
        string[] sessions = Directory.GetDirectories(subjectDir, "ses-*");
        if (sessions.Length == 0)
            throw new DirectoryNotFoundException($"未找到session目录: {subjectDir}\\ses-*");

        // 如果未指定session或指定的不存在，使用第一个
        string selectedSession = SessionId;
        if (!sessions.Any(s => Path.GetFileName(s) == selectedSession))
            selectedSession = Path.GetFileName(sessions[0]);

        string eegDir = Path.Combine(subjectDir, selectedSession, "eeg");
        if (!Directory.Exists(eegDir))
            throw new DirectoryNotFoundException($"EEG目录不存在: {eegDir}");

        // 查找BDF文件
        string[] bdfFiles = Directory.GetFiles(eegDir, "*_eeg.bdf");
        if (bdfFiles.Length == 0)
            throw new FileNotFoundException($"未找到BDF文件: {eegDir}\\*_eeg.bdf");
        string bdfPath = bdfFiles[0];

        // 查找事件文件
        string[] tsvFiles = Directory.GetFiles(eegDir, "*_events.tsv");
        if (tsvFiles.Length == 0)
            throw new FileNotFoundException($"未找到事件文件: {eegDir}\\*_events.tsv");
        string eventsPath = tsvFiles[0];

        Console.WriteLine($"  被试: {SubjectId}, 会话: {selectedSession}");
        Console.WriteLine($"  BDF文件: {Path.GetFileName(bdfPath)}");
        Console.WriteLine($"  事件文件: {Path.GetFileName(eventsPath)}");

        // === 步骤2: 读取BDF文件头，从_channels.tsv获取标准通道名映射 ===
        Console.WriteLine("\n[步骤2] 读取BDF文件头和通道映射...");
        using var bdfReader = new BdfReader(bdfPath);

        string[] bdfLabels = bdfReader.GetChannelNames();
        Console.WriteLine($"  BDF总通道数: {bdfLabels.Length}");

        // 从数据集根目录读取通道TSV文件（包含标准10-20命名）
        string channelsTsvPath = Path.Combine(DataRoot, "task-meditation_channels.tsv");
        string[] standardNames = ReadChannelNamesFromTsv(channelsTsvPath);
        Console.WriteLine($"  TSV通道数: {standardNames.Length}");

        // 建立索引映射：通过TSV中的行号（对应BDF通道索引）找到目标通道
        var channelIndices = new Dictionary<string, int>();
        for (int i = 0; i < standardNames.Length && i < bdfLabels.Length; i++)
        {
            string standardName = standardNames[i];
            if (TargetChannels.Contains(standardName, StringComparer.OrdinalIgnoreCase))
            {
                channelIndices[standardName] = i;
                Console.WriteLine($"  ✓ 通道 {standardName} → BDF索引 {i} (BDF标签: '{bdfLabels[i]}')");
            }
        }

        // 报告未找到的通道
        foreach (string chName in TargetChannels)
        {
            if (!channelIndices.ContainsKey(chName))
                Console.WriteLine($"  ✗ 通道 {chName} 在TSV中未找到");
        }

        if (channelIndices.Count == 0)
            throw new InvalidOperationException("没有找到任何目标通道 (Fz, Cz, Pz)");

        // 计算总记录时长
        double totalDuration = bdfReader.NumDataRecords * bdfReader.RecordDuration;
        Console.WriteLine($"  总记录时长: {totalDuration:F1} 秒 ({totalDuration / 60:F1} 分钟)");

        // === 步骤3: 读取事件文件，提取状态时间段 ===
        Console.WriteLine("\n[步骤3] 读取事件标记...");
        var eventReader = new EventReader(eventsPath);

        // 打印事件摘要
        var stimulusEvents = eventReader.Events.Where(e => e.TrialType == "stimulus").ToList();
        var responseEvents = eventReader.Events.Where(e => e.TrialType == "response").ToList();
        Console.WriteLine($"  刺激事件数: {stimulusEvents.Count}, 响应事件数: {responseEvents.Count}");

        // 提取状态时间段
        var stateSegments = eventReader.ExtractStateSegments(totalDuration, SegmentDurationSec);
        Console.WriteLine("\n  状态分段:");
        foreach (var seg in stateSegments)
        {
            Console.WriteLine($"    {seg.State,-12}: {seg.StartTimeSec,8:F1}s - {seg.EndTimeSec,8:F1}s (时长: {seg.DurationSec:F1}s)");
        }

        // === 步骤4: 创建结果目录 ===
        string resultDir = Path.Combine(ResultsRoot, SubjectId);
        Directory.CreateDirectory(resultDir);

        // === 步骤5: 初始化计算器和可视化器 ===
        var calculator = new BispectrumCalculator(SampleRate, Nfft, Noverlap, MaxFrequency);
        var visualizer = new Visualizer();
        var extractor = new FeatureExtractor();

        // 存储所有结果用于最终对比图
        var allResults = new Dictionary<string, Dictionary<string, BispectrumResult>>();

        // === 步骤6: 对每个通道、每个状态进行分析 ===
        Console.WriteLine("\n[步骤4] 开始双谱/双相干性分析...");
        Console.WriteLine(new string('═', 60));

        foreach (var (chName, chIdx) in channelIndices)
        {
            Console.WriteLine($"\n  ▶ 处理通道: {chName} (索引 {chIdx})");
            allResults[chName] = new Dictionary<string, BispectrumResult>();

            // ── 第一阶段：计算所有三个状态的 Bispectrum/Bicoherence ──
            foreach (var seg in stateSegments)
            {
                Console.WriteLine($"    ▶ 状态: {seg.State} ({seg.StartTimeSec:F0}s - {seg.EndTimeSec:F0}s)");

                Console.WriteLine($"      → 读取EEG数据...");
                double[] eegData = bdfReader.ReadChannelDataByTime(
                    chIdx, seg.StartTimeSec, seg.DurationSec, SampleRate);

                if (eegData.Length < Nfft)
                {
                    Console.WriteLine($"      ⚠ 数据不足 ({eegData.Length} 点 < {Nfft} 点FFT长度)，跳过此状态");
                    continue;
                }

                Console.WriteLine($"      → 数据点数: {eegData.Length}, 时长: {eegData.Length / (double)SampleRate:F1}s");
                Console.WriteLine($"      → 计算双谱和双相干性...");
                var calcStopwatch = Stopwatch.StartNew();
                var result = calculator.Compute(eegData);
                calcStopwatch.Stop();
                Console.WriteLine($"      → 计算完成，耗时: {calcStopwatch.Elapsed.TotalSeconds:F1}s");
                Console.WriteLine($"      → 分段数: {result.NumSegments}, 频率分辨率: {result.FreqResolution:F3} Hz");

                allResults[chName][seg.State] = result;
            }

            // ── 第二阶段：计算全局统一颜色范围 ──
            var computedStates = allResults[chName];
            if (computedStates.Count < 2) continue; // 至少2个状态才能做对比

            double globalBicoherenceMax = 0;
            double globalBispectrumMin = double.MaxValue;
            double globalBispectrumMax = double.MinValue;

            double cellSize = calculator.FreqResolution;
            int maxFreqIdx = Math.Min(calculator.MaxFreqIndex, (int)(DisplayMaxFreq / cellSize));
            int nFreq = maxFreqIdx + 1;
            double actualMaxFreq = nFreq * cellSize;

            foreach (var (state, res) in computedStates)
            {
                for (int f1 = 0; f1 < nFreq; f1++)
                {
                    for (int f2 = 0; f2 < nFreq; f2++)
                    {
                        if (f1 * cellSize + f2 * cellSize <= actualMaxFreq + cellSize * 0.5)
                        {
                            // Bicoherence 最大值 (百分比)
                            globalBicoherenceMax = Math.Max(globalBicoherenceMax,
                                res.Bicoherence[f1, f2] * 100.0);

                            // Bispectrum log 幅度范围
                            double mag = res.BispectrumMagnitude[f1, f2];
                            double logMag = Math.Log10(Math.Max(mag, 1e-15));
                            globalBispectrumMin = Math.Min(globalBispectrumMin, logMag);
                            globalBispectrumMax = Math.Max(globalBispectrumMax, logMag);
                        }
                    }
                }
            }

            // 对 Bicoherence 上限取整到 5 的倍数
            if (globalBicoherenceMax < 5) globalBicoherenceMax = 5;
            globalBicoherenceMax = Math.Ceiling(globalBicoherenceMax / 5) * 5;
            if (globalBicoherenceMax > 100) globalBicoherenceMax = 100;

            Console.WriteLine($"    [统一色域] Bicoherence: 0% – {globalBicoherenceMax:F0}%");
            Console.WriteLine($"    [统一色域] Bispectrum log: {globalBispectrumMin:F2} – {globalBispectrumMax:F2}");

            // ── 第三阶段：生成所有图片（统一颜色范围）──
            foreach (var (state, result) in computedStates)
            {
                // 生成Bicoherence热图 (统一色域)
                string heatmapPath = Path.Combine(resultDir,
                    $"bicoherence_{SubjectId}_{chName}_{state}.png");
                visualizer.SaveBicoherenceHeatmap(result, heatmapPath, chName,
                    GetStateDisplayName(state), DisplayMaxFreq,
                    colorMin: 0, colorMax: globalBicoherenceMax);

                // 生成双谱幅度图 (统一色域)
                string bispecPath = Path.Combine(resultDir,
                    $"bispectrum_{SubjectId}_{chName}_{state}.png");
                visualizer.SaveBispectrumMagnitudeHeatmap(result, bispecPath, chName,
                    GetStateDisplayName(state), DisplayMaxFreq,
                    colorMin: globalBispectrumMin, colorMax: globalBispectrumMax);

                // 生成双谱相位图 (Biphase) — 相位天然 [-180°,+180°]
                string phasePath = Path.Combine(resultDir,
                    $"biphase_{SubjectId}_{chName}_{state}.png");
                visualizer.SaveBispectrumPhaseHeatmap(result, phasePath, chName,
                    GetStateDisplayName(state), DisplayMaxFreq);

                // 生成对角线Bispectrum/Bicoherence曲线图
                string diagPath = Path.Combine(resultDir,
                    $"diagonal_{SubjectId}_{chName}_{state}.png");
                visualizer.SaveBispectrumDiagonalPlot(result, diagPath, chName,
                    GetStateDisplayName(state), DisplayMaxFreq);

                // 提取特征
                var (diagFreqs, diagValues) = result.GetDiagonalBicoherence();
                var features = extractor.ExtractFeatures(diagFreqs, diagValues);
                FeatureExtractor.PrintFeatureSummary(chName, GetStateDisplayName(state), features);
            }
        }

        // === 步骤7: 生成多状态对比图（使用第一个通道） ===
        Console.WriteLine("\n[步骤5] 生成多状态对比图...");
        string firstChannel = channelIndices.Keys.First();
        if (allResults.TryGetValue(firstChannel, out var channelResults) &&
            channelResults.ContainsKey("baseline") &&
            channelResults.ContainsKey("meditation") &&
            channelResults.ContainsKey("recovery"))
        {
            string comparePath = Path.Combine(resultDir,
                $"bicoherence_compare_{SubjectId}.png");
            visualizer.SaveComparisonFigure(
                channelResults["baseline"],
                channelResults["meditation"],
                channelResults["recovery"],
                comparePath,
                firstChannel,
                DisplayMaxFreq);

            // 同时生成 Bispectrum 三状态对比图
            string bispecComparePath = Path.Combine(resultDir,
                $"bispectrum_compare_{SubjectId}.png");
            visualizer.SaveBispectrumComparisonFigure(
                channelResults["baseline"],
                channelResults["meditation"],
                channelResults["recovery"],
                bispecComparePath,
                firstChannel,
                DisplayMaxFreq);
        }
        else
        {
            Console.WriteLine("  ⚠ 缺少某些状态的结果，无法生成完整对比图。");
            Console.WriteLine("    将尝试使用可用状态生成部分对比图...");

            // 尝试用可用的状态生成对比图
            var availableStates = channelResults?.Keys.ToList() ?? new List<string>();
            if (availableStates.Count >= 2 && channelResults != null)
            {
                var fallbackResults = new List<BispectrumResult>();
                foreach (var st in availableStates.Take(3))
                {
                    fallbackResults.Add(channelResults[st]);
                }
                // 补齐到3个
                while (fallbackResults.Count < 3)
                    fallbackResults.Add(fallbackResults.Last());

                string comparePath = Path.Combine(resultDir,
                    $"bicoherence_compare_{SubjectId}_partial.png");
                visualizer.SaveComparisonFigure(
                    fallbackResults[0], fallbackResults[1], fallbackResults[2],
                    comparePath, firstChannel, DisplayMaxFreq);
            }
        }

        // === 步骤8: 输出最终摘要 ===
        Console.WriteLine("\n[步骤6] 最终摘要");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"  被试: {SubjectId}");
        Console.WriteLine($"  分析通道: {string.Join(", ", channelIndices.Keys)}");
        Console.WriteLine($"  FFT参数: N={Nfft}, 重叠={Noverlap}, 频率分辨率={calculator.FreqResolution:F3} Hz");
        Console.WriteLine($"  生成图片数: {channelIndices.Count * stateSegments.Count * 2 + 1}");
        Console.WriteLine($"  结果目录: {resultDir}");
        Console.WriteLine(new string('═', 60));
    }

    /// <summary>
    /// 解析命令行参数
    /// 用法: dotnet run -- [--subject sub-001] [--session ses-01] [--datadir path]
    /// </summary>
    static void ParseCommandLine(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--subject":
                case "-s":
                    if (i + 1 < args.Length) SubjectId = args[++i];
                    break;
                case "--session":
                case "-ses":
                    if (i + 1 < args.Length) SessionId = args[++i];
                    break;
                case "--datadir":
                case "-d":
                    if (i + 1 < args.Length) DataRoot = args[++i];
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    break;
            }
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("用法: dotnet run -- [选项]");
        Console.WriteLine("选项:");
        Console.WriteLine("  --subject, -s  <ID>     被试ID (默认: sub-001)");
        Console.WriteLine("  --session, -ses <ID>    会话ID (默认: 自动检测)");
        Console.WriteLine("  --datadir, -d  <path>   数据根目录 (默认: e:\\数据集)");
        Console.WriteLine("  --help, -h              显示帮助");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  dotnet run -- --subject sub-002");
        Console.WriteLine("  dotnet run -- --subject sub-001 --session ses-02");
    }

    /// <summary>
    /// 从_channels.tsv文件中读取标准通道名称列表（顺序与BDF文件一致）
    /// </summary>
    static string[] ReadChannelNamesFromTsv(string tsvPath)
    {
        if (!File.Exists(tsvPath))
            throw new FileNotFoundException($"通道TSV文件未找到: {tsvPath}");

        var names = new List<string>();
        string[] lines = File.ReadAllLines(tsvPath);

        // 跳过表头行（第一行）
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] parts = line.Split('\t');
            if (parts.Length > 0)
            {
                names.Add(parts[0].Trim());
            }
        }

        return names.ToArray();
    }

    static string GetStateDisplayName(string state) => state switch
    {
        "baseline" => "冥想前 (Baseline)",
        "meditation" => "冥想中 (Meditation)",
        "recovery" => "冥想后 (Recovery)",
        _ => state
    };
}
