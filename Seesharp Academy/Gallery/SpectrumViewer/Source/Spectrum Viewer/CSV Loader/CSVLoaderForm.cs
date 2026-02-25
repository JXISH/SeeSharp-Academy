using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CSV_Loader
{
    /// <summary>
    /// CSV Loader Form - 用于加载和预览CSV文件数据
    /// </summary>
    public partial class CSVLoaderForm : Form
    {
        // 公共属性 - 用于存储加载的数据和采样率
        public double[] LoadedSeries { get; private set; }
        public double SampleRate { get; private set; }
        public string LoadedFileName { get; private set; }

        // 私有字段
        private string selectedFilePath;
        private DataTable csvData;
        private List<string> availableSeries;
        private List<double> timeValues;

        public CSVLoaderForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            csvData = new DataTable();
            availableSeries = new List<string>();
            timeValues = new List<double>();
            LoadedSeries = new double[0];
            SampleRate = 1.0;
        }

        /// <summary>
        /// 窗体加载事件 - 打开文件选择对话框
        /// </summary>
        private void CSVLoaderForm_Load(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "选择CSV文件",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                selectedFilePath = openFileDialog.FileName;
                LoadCSVFile(selectedFilePath);
            }
            else
            {
                // 用户未选择文件，关闭窗体
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        /// <summary>
        /// 加载CSV文件
        /// </summary>
        private void LoadCSVFile(string filePath)
        {
            try
            {
                // 读取CSV文件
                var lines = File.ReadAllLines(filePath).ToList();
                
                if (lines.Count == 0)
                {
                    MessageBox.Show("CSV文件为空！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                // 解析CSV数据
                ParseCSVData(lines);

                // 显示前5行
                DisplayPreviewData();

                // 分析并显示数据系列
                AnalyzeAndDisplaySeries();

                // 计算并显示采样率
                CalculateAndDisplaySampleRate();

                //提取文件名给属性
                LoadedFileName = Path.GetFileName(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载CSV文件时出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        /// <summary>
        /// 解析CSV数据
        /// </summary>
        private void ParseCSVData(List<string> lines)
        {
            csvData.Clear();
            availableSeries.Clear();
            timeValues.Clear();

            // 分析每行的列数
            int maxColumns = 0;
            List<string[]> parsedLines = new List<string[]>();

            foreach (var line in lines)
            {
                var columns = ParseCSVLine(line);
                parsedLines.Add(columns);
                if (columns.Length > maxColumns)
                {
                    maxColumns = columns.Length;
                }
            }

            // 检查是否有至少2列
            if (maxColumns < 2)
            {
                MessageBox.Show("CSV文件格式不正确：至少需要2列数据！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return;
            }

            // 创建数据表列
            for (int i = 0; i < maxColumns; i++)
            {
                csvData.Columns.Add($"Column{i}");
            }

            // 填充数据（只保留前5行用于显示）
            int rowsToAdd = Math.Min(5, parsedLines.Count);
            for (int i = 0; i < rowsToAdd; i++)
            {
                var row = csvData.NewRow();
                for (int j = 0; j < parsedLines[i].Length; j++)
                {
                    row[j] = parsedLines[i][j];
                }
                csvData.Rows.Add(row);
            }

            // 第一行从第二列开始是数据系列名称
            if (parsedLines.Count > 0)
            {
                var firstRow = parsedLines[0];
                for (int i = 1; i < firstRow.Length; i++)
                {
                    availableSeries.Add(firstRow[i]);
                }

                // 解析时间值（第一列，从第二行开始）
                for (int i = 1; i < parsedLines.Count; i++)
                {
                    if (parsedLines[i].Length > 0)
                    {
                        if (double.TryParse(parsedLines[i][0], out double timeValue))
                        {
                            timeValues.Add(timeValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解析CSV行（处理引号和逗号）
        /// </summary>
        private string[] ParseCSVLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            result.Add(currentField.Trim());
            return result.ToArray();
        }

        /// <summary>
        /// 显示预览数据（前5行）
        /// </summary>
        private void DisplayPreviewData()
        {
            // 如果CSV数据表有数据，使用第一行作为列标题
            if (csvData.Rows.Count > 0)
            {
                // 创建新的数据表用于显示，第一行作为标题
                DataTable displayTable = new DataTable();
                
                // 使用第一行作为列名
                var firstRow = csvData.Rows[0];
                for (int i = 0; i < csvData.Columns.Count; i++)
                {
                    string columnName = firstRow[i]?.ToString() ?? $"Column{i}";
                    displayTable.Columns.Add(columnName);
                }
                
                // 添加数据行（从第二行开始，最多4行）
                int dataRowsToAdd = Math.Min(4, csvData.Rows.Count - 1);
                for (int i = 1; i <= dataRowsToAdd; i++)
                {
                    var row = displayTable.NewRow();
                    for (int j = 0; j < csvData.Columns.Count; j++)
                    {
                        row[j] = csvData.Rows[i][j];
                    }
                    displayTable.Rows.Add(row);
                }
                
                dgvPreview.DataSource = null;
                dgvPreview.DataSource = displayTable;
            }
            else
            {
                dgvPreview.DataSource = csvData;
            }

            // 设置列宽
            for (int i = 0; i < dgvPreview.Columns.Count; i++)
            {
                dgvPreview.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        /// <summary>
        /// 分析并显示数据系列
        /// </summary>
        private void AnalyzeAndDisplaySeries()
        {
            clbSeries.Items.Clear();
            
            foreach (var series in availableSeries)
            {
                clbSeries.Items.Add(series);
            }
            
            // 默认选中第一个系列
            if (clbSeries.Items.Count > 0)
            {
                clbSeries.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 计算并显示采样率
        /// </summary>
        private void CalculateAndDisplaySampleRate()
        {
            if (timeValues.Count < 2)
            {
                nudSampleRate.Value = 1.0m;
                SampleRate = 1.0;
                return;
            }

            // 计算时间差
            double firstTime = timeValues[0];
            double lastTime = timeValues[timeValues.Count - 1];
            double timeDifference = lastTime - firstTime;

            // 计算间隔行数
            int intervalRows = timeValues.Count - 1;

            // 计算采样间隔
            double samplingInterval = timeDifference / intervalRows;

            // 转换为采样率
            double calculatedSampleRate = samplingInterval > 0 ? 1.0 / samplingInterval : 1.0;

            // 显示采样率
            nudSampleRate.Value = Convert.ToDecimal(Math.Round(calculatedSampleRate, 2));
            SampleRate = calculatedSampleRate;
        }

        /// <summary>
        /// Load按钮点击事件 - 加载选中的系列数据
        /// </summary>
        private void btnLoad_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取选中的系列索引（单选模式）
                if (clbSeries.SelectedIndex == -1)
                {
                    MessageBox.Show("请选择一个数据系列！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int seriesIndex = clbSeries.SelectedIndex;

                // 读取CSV文件以获取完整数据
                var lines = File.ReadAllLines(selectedFilePath).ToList();
                List<double> allData = new List<double>();

                // 跳过第一行（标题行）
                for (int i = 1; i < lines.Count; i++)
                {
                    var columns = ParseCSVLine(lines[i]);
                    
                    // 读取选中的系列数据（从第2列开始，索引1）
                    int columnIndex = seriesIndex + 1; // +1因为第一列是时间
                    if (columnIndex < columns.Length)
                    {
                        if (double.TryParse(columns[columnIndex], out double value))
                        {
                            allData.Add(value);
                        }
                    }
                }

                // 将数据赋值给公共属性
                LoadedSeries = allData.ToArray();
                SampleRate = (double)nudSampleRate.Value;

                // 设置对话框结果并关闭
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据时出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// 采样率数值改变事件
        /// </summary>
        private void nudSampleRate_ValueChanged(object sender, EventArgs e)
        {
            SampleRate = (double)nudSampleRate.Value;
        }
    }
}
