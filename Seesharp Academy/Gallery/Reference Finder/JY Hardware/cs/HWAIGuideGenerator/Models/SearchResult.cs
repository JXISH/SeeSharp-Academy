using System;
using System.Collections.Generic;

namespace HWAIGuideGenerator.Models
{
    /// <summary>
    /// 驱动搜索结果模型
    /// Contains driver directory, DLL and XML file information
    /// </summary>
    public class DriverSearchResult
    {
        /// <summary>
        /// 驱动目录路径
        /// </summary>
        public string DriverDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 驱动DLL文件名列表
        /// </summary>
        public List<string> DllFiles { get; set; } = new();

        /// <summary>
        /// 驱动XML文件名列表
        /// </summary>
        public List<string> XmlFiles { get; set; } = new();

        /// <summary>
        /// 驱动名称
        /// </summary>
        public string DriverName { get; set; } = string.Empty;

        /// <summary>
        /// 紧凑驱动名(去除空格和连字符)
        /// </summary>
        public string CompactDriverName { get; set; } = string.Empty;

        /// <summary>
        /// 是否找到有效结果
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(DriverDirectory) && DllFiles.Count > 0;
    }

    /// <summary>
    /// 范例搜索结果模型
    /// Contains example solution directory and project information
    /// </summary>
    public class ExampleSearchResult
    {
        /// <summary>
        /// 范例目录路径
        /// </summary>
        public string ExampleDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 解决方案文件名
        /// </summary>
        public string SolutionName { get; set; } = string.Empty;

        /// <summary>
        /// 范例目录名
        /// </summary>
        public string DirectoryName { get; set; } = string.Empty;

        /// <summary>
        /// 是否找到有效结果
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(ExampleDirectory);
    }

    /// <summary>
    /// 范例项目树节点
    /// Tree node for example project selection
    /// </summary>
    public class ExampleTreeNode
    {
        /// <summary>
        /// 节点名称(显示文本)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 完整路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否展开
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// 子节点列表
        /// </summary>
        public List<ExampleTreeNode> Children { get; set; } = new();

        /// <summary>
        /// 是否为叶子节点(项目节点)
        /// </summary>
        public bool IsLeaf => Children.Count == 0;
    }

    /// <summary>
    /// 完整搜索结果模型
    /// Combined search result containing both driver and example information
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// 驱动搜索结果
        /// </summary>
        public DriverSearchResult Driver { get; set; } = new();

        /// <summary>
        /// 范例搜索结果
        /// </summary>
        public ExampleSearchResult Example { get; set; } = new();

        /// <summary>
        /// 硬件型号
        /// </summary>
        public string HardwareModel { get; set; } = string.Empty;

        /// <summary>
        /// 型号数值(提取的4-5位数字)
        /// </summary>
        public string ModelNumber { get; set; } = string.Empty;

        /// <summary>
        /// 操作系统
        /// </summary>
        public OperatingSystemType OperatingSystem { get; set; }

        /// <summary>
        /// 搜索时间
        /// </summary>
        public DateTime SearchTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 操作系统类型枚举
    /// </summary>
    public enum OperatingSystemType
    {
        Windows,
        Linux
    }
}
