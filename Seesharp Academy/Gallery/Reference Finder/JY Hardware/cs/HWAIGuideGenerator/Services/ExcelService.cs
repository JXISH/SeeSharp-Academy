using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HWAIGuideGenerator.Models;
using OfficeOpenXml;

namespace HWAIGuideGenerator.Services
{
    /// <summary>
    /// Excel文件读取服务
    /// Service for reading JYPEDIA Excel files without Office dependency
    /// </summary>
    public class ExcelService
    {
        private const string SheetName = "Drivers and Software";
        private const int TitleRow = 2;
        private const int DataStartRow = 3;

        public ExcelService()
        {
            // EPPlus需要设置LicenseContext (非商业用途使用NonCommercial)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 从JYPEDIA Excel文件读取数据
        /// Reads data from the "Drivers and Software" sheet
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>JYPEDIA数据行列表</returns>
        public List<JypediaRow> ReadJypediaFile(string filePath)
        {
            var rows = new List<JypediaRow>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"JYPEDIA文件不存在: {filePath}");
            }

            using var package = new ExcelPackage(new FileInfo(filePath));
            
            // 查找指定的工作表
            var worksheet = package.Workbook.Worksheets[SheetName];
            if (worksheet == null)
            {
                // 尝试模糊匹配
                worksheet = package.Workbook.Worksheets
                    .FirstOrDefault(ws => ws.Name.Contains("Driver", StringComparison.OrdinalIgnoreCase));
                
                if (worksheet == null)
                {
                    throw new InvalidOperationException($"未找到工作表: {SheetName}");
                }
            }

            // 读取数据行
            int rowCount = worksheet.Dimension?.Rows ?? 0;
            for (int row = DataStartRow; row <= rowCount; row++)
            {
                var jypediaRow = new JypediaRow
                {
                    RowNumber = row,
                    FileName = GetCellValue(worksheet, row, 1),   // A列
                    ColumnB = GetCellValue(worksheet, row, 2),    // B列
                    Type = GetCellValue(worksheet, row, 3),       // C列
                    ModelInfo = GetCellValue(worksheet, row, 4)   // D列
                };

                // 只添加非空行
                if (!string.IsNullOrWhiteSpace(jypediaRow.FileName))
                {
                    rows.Add(jypediaRow);
                }
            }

            return rows;
        }

        /// <summary>
        /// 根据型号数值筛选匹配的驱动行
        /// Filters rows that match the model number and operating system for drivers
        /// </summary>
        /// <param name="rows">所有数据行</param>
        /// <param name="modelNumber">型号数值(4-5位数字)</param>
        /// <param name="osType">操作系统类型</param>
        /// <returns>匹配的驱动行列表</returns>
        public List<JypediaRow> FilterDriverRows(List<JypediaRow> rows, string modelNumber, OperatingSystemType osType)
        {
            return rows.Where(r => 
                r.IsDriver &&
                r.ModelInfo.Contains(modelNumber) &&
                (osType == OperatingSystemType.Windows ? r.IsWindows : r.IsLinux)
            ).ToList();
        }

        /// <summary>
        /// 根据型号数值筛选匹配的C#范例行
        /// Filters rows that match the model number for C# examples
        /// </summary>
        /// <param name="rows">所有数据行</param>
        /// <param name="modelNumber">型号数值(4-5位数字)</param>
        /// <returns>匹配的C#范例行列表</returns>
        public List<JypediaRow> FilterExampleRows(List<JypediaRow> rows, string modelNumber)
        {
            return rows.Where(r => 
                r.IsCSharpExample &&
                r.ModelInfo.Contains(modelNumber)
            ).ToList();
        }

        /// <summary>
        /// 从文件名提取驱动名
        /// Extracts driver name from the installation package filename
        /// </summary>
        /// <param name="fileName">安装包文件名</param>
        /// <returns>驱动名和紧凑驱动名</returns>
        public (string DriverName, string CompactDriverName) ExtractDriverName(string fileName)
        {
            // 提取第一个下划线之前的内容作为驱动名
            int underscoreIndex = fileName.IndexOf('_');
            string driverName = underscoreIndex > 0 
                ? fileName.Substring(0, underscoreIndex) 
                : fileName;

            // 去除空格和连字符形成紧凑驱动名
            string compactName = driverName
                .Replace(" ", "")
                .Replace("-", "");

            return (driverName, compactName);
        }

        /// <summary>
        /// 从文件名提取范例目录名
        /// Extracts example directory name from the installation package filename
        /// </summary>
        /// <param name="fileName">安装包文件名</param>
        /// <returns>范例目录名</returns>
        public string ExtractExampleDirectoryName(string fileName)
        {
            // 去掉后缀
            return Path.GetFileNameWithoutExtension(fileName);
        }

        /// <summary>
        /// 获取单元格值
        /// </summary>
        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            return cell?.Value?.ToString()?.Trim() ?? string.Empty;
        }
    }
}
