using System;
using System.Collections.Generic;

namespace HWAIGuideGenerator.Models
{
    /// <summary>
    /// 历史记录数据模型
    /// Contains all history data for inputs and outputs
    /// </summary>
    public class HistoryData
    {
        /// <summary>
        /// JYPEDIA文件路径历史记录
        /// </summary>
        public List<string> JypediaFilePaths { get; set; } = new();

        /// <summary>
        /// 硬件型号历史记录
        /// </summary>
        public List<string> HardwareModels { get; set; } = new();

        /// <summary>
        /// 驱动目录历史记录
        /// </summary>
        public List<string> DriverDirectories { get; set; } = new();

        /// <summary>
        /// 生成的AI训导词历史记录
        /// </summary>
        public List<HistoryOutputRecord> OutputRecords { get; set; } = new();
    }

    /// <summary>
    /// 输出历史记录项
    /// Single output history record with timestamp and content
    /// </summary>
    public class HistoryOutputRecord
    {
        /// <summary>
        /// 记录时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 硬件型号
        /// </summary>
        public string HardwareModel { get; set; } = string.Empty;

        /// <summary>
        /// 生成的AI训导词内容
        /// </summary>
        public string GeneratedPrompt { get; set; } = string.Empty;

        /// <summary>
        /// 驱动信息摘要
        /// </summary>
        public string DriverSummary { get; set; } = string.Empty;

        /// <summary>
        /// 范例信息摘要
        /// </summary>
        public string ExampleSummary { get; set; } = string.Empty;

        /// <summary>
        /// 显示标题(用于列表显示)
        /// 格式: [时间] 型号 - 范例名1, 范例名2...
        /// </summary>
        public string DisplayTitle
        {
            get
            {
                string baseTitle = $"[{Timestamp:yyyy-MM-dd HH:mm}] {HardwareModel}";
                if (!string.IsNullOrEmpty(ExampleSummary) && ExampleSummary != "(无选择)")
                {
                    return $"{baseTitle} - {ExampleSummary}";
                }
                return baseTitle;
            }
        }
    }

    /// <summary>
    /// JYPEDIA Excel数据行模型
    /// Represents a row from the "Drivers and Software" sheet
    /// </summary>
    public class JypediaRow
    {
        /// <summary>
        /// A列: 文件名和链接
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// B列
        /// </summary>
        public string ColumnB { get; set; } = string.Empty;

        /// <summary>
        /// C列: 类型(Driver/Example)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// D列: 型号信息
        /// </summary>
        public string ModelInfo { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// 是否为驱动行
        /// </summary>
        public bool IsDriver => Type?.Equals("Driver", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// 是否为范例行(排除Python, C++, LabVIEW)
        /// </summary>
        public bool IsCSharpExample => Type?.Equals("Example", StringComparison.OrdinalIgnoreCase) == true
            && !FileName.Contains("Python", StringComparison.OrdinalIgnoreCase)
            && !FileName.Contains("C++", StringComparison.OrdinalIgnoreCase)
            && !FileName.Contains("LabVIEW", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 是否为Windows相关
        /// </summary>
        public bool IsWindows => FileName.Contains("Win", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 是否为Linux相关
        /// </summary>
        public bool IsLinux => FileName.Contains("Linux", StringComparison.OrdinalIgnoreCase);
    }
}
