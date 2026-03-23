using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HWAIGuideGenerator.Models;

namespace HWAIGuideGenerator.Services
{
    /// <summary>
    /// AI训导词生成服务
    /// Service for generating AI training prompts based on search results
    /// </summary>
    public class PromptGeneratorService
    {
        /// <summary>
        /// 生成AI训导词
        /// Generates AI training prompt based on search results and selected examples
        /// </summary>
        /// <param name="searchResult">搜索结果</param>
        /// <param name="selectedExamples">选中的范例节点列表</param>
        /// <param name="copyDriver">是否勾选复制驱动</param>
        /// <param name="targetDriverDirectory">目标驱动目录(如果勾选复制驱动)</param>
        /// <returns>生成的Markdown格式AI训导词</returns>
        public string GeneratePrompt(
            SearchResult searchResult,
            List<ExampleTreeNode> selectedExamples,
            bool copyDriver,
            string? targetDriverDirectory)
        {
            var sb = new StringBuilder();

            // 确定驱动目录
            string driverDir = copyDriver && !string.IsNullOrEmpty(targetDriverDirectory)
                ? targetDriverDirectory
                : searchResult.Driver.DriverDirectory;

            // 如果勾选了复制驱动，添加复制指令
            if (copyDriver && !string.IsNullOrEmpty(targetDriverDirectory))
            {
                sb.AppendLine("## 驱动文件准备");
                sb.AppendLine();

                foreach (var dll in searchResult.Driver.DllFiles)
                {
                    string sourceDllPath = Path.Combine(searchResult.Driver.DriverDirectory, "Bin", dll);
                    sb.AppendLine($"将 `{sourceDllPath}` 拷贝到 `{targetDriverDirectory}`");
                }

                foreach (var xml in searchResult.Driver.XmlFiles)
                {
                    string sourceXmlPath = Path.Combine(searchResult.Driver.DriverDirectory, "Bin", xml);
                    sb.AppendLine($"将 `{sourceXmlPath}` 拷贝到 `{targetDriverDirectory}`");
                }

                sb.AppendLine();
            }

            // 生成主体训导词
            sb.AppendLine("## AI编程训导词");
            sb.AppendLine();

            // 硬件型号
            sb.AppendLine($"我要对 **{searchResult.HardwareModel}** 编程");
            sb.AppendLine();

            // 驱动引用
            foreach (var dll in searchResult.Driver.DllFiles)
            {
                sb.AppendLine($"将 `{driverDir}` 目录下的 `{dll}` 添加到引用。");
            }
            sb.AppendLine();

            // 编程参考
            foreach (var xml in searchResult.Driver.XmlFiles)
            {
                sb.AppendLine($"编程方法和属性参考 `{driverDir}` 目录下的 `{xml}`。");
            }
            sb.AppendLine();

            // 范例参考
            if (selectedExamples.Any())
            {
                sb.AppendLine("编程步骤逻辑参考以下范例：");
                sb.AppendLine();

                foreach (var example in selectedExamples)
                {
                    // 获取项目名称(目录名)
                    string projectName = example.Name;
                    string solutionPath = GetParentSolutionPath(example.FullPath, searchResult.Example.ExampleDirectory);

                    sb.AppendLine($"- `{solutionPath}` 下的项目：`{projectName}`");
                }

                sb.AppendLine();
            }

            // 功能描述占位符
            sb.AppendLine("*这里写你要编写的功能描述*");

            return sb.ToString();
        }

        /// <summary>
        /// 获取范例所在的解决方案路径
        /// </summary>
        private string GetParentSolutionPath(string examplePath, string exampleRootDirectory)
        {
            // 尝试找到最近的包含.sln文件的目录
            var dir = new DirectoryInfo(examplePath);
            
            while (dir != null && dir.FullName.Length >= exampleRootDirectory.Length)
            {
                if (dir.GetFiles("*.sln").Any())
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            // 如果没找到解决方案，返回范例目录
            return examplePath;
        }

        /// <summary>
        /// 生成搜索结果摘要
        /// Generates a summary of the search results for display
        /// </summary>
        public string GenerateSearchResultSummary(SearchResult searchResult)
        {
            var sb = new StringBuilder();

            sb.AppendLine("## 驱动信息");
            sb.AppendLine($"- 驱动目录: `{searchResult.Driver.DriverDirectory}`");
            sb.AppendLine($"- 驱动名称: {searchResult.Driver.DriverName}");
            sb.AppendLine($"- DLL文件: {string.Join(", ", searchResult.Driver.DllFiles)}");
            sb.AppendLine($"- XML文件: {string.Join(", ", searchResult.Driver.XmlFiles)}");
            sb.AppendLine();

            sb.AppendLine("## 范例信息");
            sb.AppendLine($"- 范例目录: `{searchResult.Example.ExampleDirectory}`");
            sb.AppendLine($"- 目录名称: {searchResult.Example.DirectoryName}");

            return sb.ToString();
        }

        /// <summary>
        /// 创建输出历史记录
        /// </summary>
        public HistoryOutputRecord CreateOutputRecord(
            SearchResult searchResult,
            string generatedPrompt,
            List<ExampleTreeNode> selectedExamples)
        {
            return new HistoryOutputRecord
            {
                Timestamp = DateTime.Now,
                HardwareModel = searchResult.HardwareModel,
                GeneratedPrompt = generatedPrompt,
                DriverSummary = $"{searchResult.Driver.DriverDirectory} ({string.Join(", ", searchResult.Driver.DllFiles)})",
                ExampleSummary = selectedExamples.Any()
                    ? string.Join(", ", selectedExamples.Select(e => e.Name))
                    : "(无选择)"
            };
        }
    }
}
