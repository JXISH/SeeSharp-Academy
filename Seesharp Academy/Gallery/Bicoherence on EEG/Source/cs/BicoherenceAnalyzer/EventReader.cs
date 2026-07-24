using System.Globalization;

namespace BicoherenceAnalyzer;

/// <summary>
/// EEG冥想实验事件标记读取器
/// 解析 _events.tsv 文件，提取冥想实验中的事件时间线和不同状态的时间段
/// </summary>
public class EventReader
{
    /// <summary>
    /// 所有事件列表
    /// </summary>
    public List<EegEvent> Events { get; private set; } = new();

    /// <summary>
    /// 冥想状态的时间段定义
    /// </summary>
    public record StateSegment(string State, double StartTimeSec, double EndTimeSec, double DurationSec);

    /// <summary>
    /// 构造函数：从TSV文件加载事件
    /// </summary>
    public EventReader(string eventsFilePath)
    {
        if (!File.Exists(eventsFilePath))
            throw new FileNotFoundException($"事件文件未找到: {eventsFilePath}");

        Events = ParseEvents(eventsFilePath);
        Console.WriteLine($"[事件] 加载了 {Events.Count} 个事件");
    }

    /// <summary>
    /// 解析TSV格式的事件文件
    /// </summary>
    private static List<EegEvent> ParseEvents(string filePath)
    {
        var events = new List<EegEvent>();
        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length < 2)
            return events; // 只有表头，无数据

        // 第一行是表头
        string[] headers = lines[0].Split('\t');
        int onsetIdx = Array.FindIndex(headers, h => h.Trim() == "onset");
        int durationIdx = Array.FindIndex(headers, h => h.Trim() == "duration");
        int trialTypeIdx = Array.FindIndex(headers, h => h.Trim() == "trial_type");
        int sampleIdx = Array.FindIndex(headers, h => h.Trim() == "sample");
        int valueIdx = Array.FindIndex(headers, h => h.Trim() == "value");

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] parts = line.Split('\t');
            if (parts.Length < 3) continue;

            var evt = new EegEvent
            {
                Onset = ParseDoubleSafe(parts[onsetIdx]),
                Duration = durationIdx >= 0 && parts[durationIdx] != "n/a"
                    ? ParseDoubleSafe(parts[durationIdx]) : 0,
                TrialType = trialTypeIdx >= 0 ? parts[trialTypeIdx].Trim() : "",
                Sample = sampleIdx >= 0 ? (int)ParseDoubleSafe(parts[sampleIdx]) : 0,
                Value = valueIdx >= 0 ? (int)ParseDoubleSafe(parts[valueIdx]) : 0
            };
            events.Add(evt);
        }

        return events;
    }

    /// <summary>
    /// 提取冥想实验的状态时间段
    /// 
    /// 策略说明：
    /// 1. 实验每约2分钟出现一个问题（stimulus），被试回答（response）后继续冥想
    /// 2. Baseline（冥想前）: 实验开始后的前5分钟 (0-300s)，此时被试刚开始进入冥想状态
    /// 3. Meditation（冥想中）: 录音中间段落的连续冥想期 - 选择实验中间最长的冥想时段
    /// 4. Recovery（冥想后）: 实验最后5分钟 (总时长-300 到 总时长)
    ///
    /// 更精确的策略：找到两个相邻 question onset 之间的最大间隔作为深度冥想期
    /// </summary>
    public List<StateSegment> ExtractStateSegments(double totalRecordingDurationSec, double segmentDurationSec = 300.0)
    {
        var segments = new List<StateSegment>();

        // === 策略1: Baseline = 前5分钟 ===
        double baselineEnd = Math.Min(segmentDurationSec, totalRecordingDurationSec * 0.15);
        segments.Add(new StateSegment("baseline", 0, baselineEnd, baselineEnd));

        // === 策略2: Meditation = 找到最长的无问题干扰期（中间段落） ===
        var stimulusEvents = Events
            .Where(e => e.TrialType == "stimulus")
            .OrderBy(e => e.Onset)
            .ToList();

        if (stimulusEvents.Count >= 2)
        {
            // 寻找相邻 stimulus 之间的最大间隔（被试在此期间冥想）
            double maxGap = 0;
            double maxGapStart = 0;
            double maxGapEnd = 0;

            for (int i = 1; i < stimulusEvents.Count; i++)
            {
                double gap = stimulusEvents[i].Onset - stimulusEvents[i - 1].Onset;
                if (gap > maxGap)
                {
                    maxGap = gap;
                    // 冥想期从上一个问题回答后开始（如果存在回答）
                    // 简化：使用前一个stimulus之后20秒（给被试回答问题的时间）
                    maxGapStart = stimulusEvents[i - 1].Onset + 20;
                    maxGapEnd = stimulusEvents[i].Onset;
                }
            }

            if (maxGap > 60) // 至少1分钟的冥想期
            {
                // 限制冥想段长度不超过 segmentDurationSec
                if (maxGap > segmentDurationSec)
                {
                    double midPoint = (maxGapStart + maxGapEnd) / 2;
                    maxGapStart = midPoint - segmentDurationSec / 2;
                    maxGapEnd = midPoint + segmentDurationSec / 2;
                }
                segments.Add(new StateSegment("meditation", maxGapStart, maxGapEnd, maxGapEnd - maxGapStart));
            }

            // 如果事件法没找到合适冥想期，使用时间中点法
            if (!segments.Any(s => s.State == "meditation"))
            {
                double meditationStart = totalRecordingDurationSec * 0.3;
                double meditationEnd = meditationStart + segmentDurationSec;
                if (meditationEnd > totalRecordingDurationSec)
                    meditationEnd = totalRecordingDurationSec;
                segments.Add(new StateSegment("meditation", meditationStart, meditationEnd,
                    meditationEnd - meditationStart));
            }
        }
        else
        {
            // 无足够事件，使用时间中点法
            double meditationStart = totalRecordingDurationSec * 0.3;
            double meditationEnd = meditationStart + segmentDurationSec;
            if (meditationEnd > totalRecordingDurationSec)
                meditationEnd = totalRecordingDurationSec;
            segments.Add(new StateSegment("meditation", meditationStart, meditationEnd,
                meditationEnd - meditationStart));
        }

        // === 策略3: Recovery = 最后5分钟 ===
        double recoveryStart = Math.Max(0, totalRecordingDurationSec - segmentDurationSec);
        segments.Add(new StateSegment("recovery", recoveryStart, totalRecordingDurationSec,
            totalRecordingDurationSec - recoveryStart));

        return segments;
    }

    /// <summary>
    /// 获取从指定时间范围内的事件
    /// </summary>
    public List<EegEvent> GetEventsInRange(double startSec, double endSec)
    {
        return Events.Where(e => e.Onset >= startSec && e.Onset <= endSec).ToList();
    }

    private static double ParseDoubleSafe(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || s == "n/a")
            return 0;
        return double.Parse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// EEG事件数据结构
/// </summary>
public class EegEvent
{
    /// <summary>事件发生的起始时间（秒）</summary>
    public double Onset { get; set; }

    /// <summary>事件持续时间（秒），n/a表示瞬时事件</summary>
    public double Duration { get; set; }

    /// <summary>事件类型：stimulus（问题出现），response（被试回答）</summary>
    public string TrialType { get; set; } = "";

    /// <summary>事件对应的采样点编号（0-based）</summary>
    public int Sample { get; set; }

    /// <summary>事件数值标记：2/4/8=回答，128=问题开始</summary>
    public int Value { get; set; }

    public override string ToString()
    {
        return $"onset={Onset:F3}s, type={TrialType}, value={Value}, sample={Sample}";
    }
}
