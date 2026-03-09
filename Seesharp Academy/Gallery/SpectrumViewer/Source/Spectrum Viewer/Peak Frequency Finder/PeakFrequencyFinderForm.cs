using CSV_Loader;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Double;
using Seesharp.JY.DSP.SignalProcessing.Conditioning.Filter1D.EasyFilter;
using Seesharp.JY.SignalProcessing.SuperResolution;
using SeeSharpTools.JY.DSP.Utility.Filter1D;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static Seesharp.JY.SignalProcessing.SuperResolution.FrequencyFinder;

namespace Peak_Frequency_Finder
{
    /// <summary>
    /// 峰值频率查找器主窗体类
    /// 
    /// 功能概述：
    /// 1. 从CSV文件加载波形数据
    /// 2. 对信号进行预处理（降采样、带通/高通/低通滤波）
    /// 3. 使用FFT计算功率谱
    /// 4. 使用Matrix Pencil算法进行高精度频率检测
    /// 5. 在频谱图上标注检测到的峰值频率
    /// 6. 在表格中显示频率和幅度信息
    /// 
    /// 核心算法：
    /// - Matrix Pencil：基于奇异值分解(SVD)和特征值分解的高精度频率估计方法
    /// - 适用于多分量信号、指数衰减信号等复杂场景
    /// - 精度高于传统FFT峰值检测方法
    /// 
    /// 使用流程：
    /// 1. 点击Load按钮加载CSV波形
    /// 2. 设置频率范围（StartFreq/StopFreq）
    /// 3. 点击Analysis按钮执行分析
    /// 4. 查看频谱图和检测结果表格
    /// </summary>
    public partial class PeakFrequencyFinderForm : Form
    {
        #region 公共属性
        /// <summary>
        /// 实数信号波形数据
        /// </summary>
        public double[] signal { get; set; }
        
        /// <summary>
        /// 采样率（Hz）
        /// </summary>
        public double sampleRate { get; set; }
        
        /// <summary>
        /// 检测到的频率列表
        /// </summary>
        public List<double> frequencies { get; set; }
        #endregion

        #region 私有域
        FrequencyFinder freqFinder;
        /// <summary>
        /// 后台工作线程：用于执行耗时的频率分析，避免界面卡顿
        /// </summary>
        private BackgroundWorker analysisWorker;

        List<DetectedComponent> _detectedFrequencies;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 窗体构造函数
        /// 初始化组件和后台工作线程
        /// </summary>
        public PeakFrequencyFinderForm()
        {
            InitializeComponent();
            
            // 初始化数据
            signal = null;
            sampleRate = 0;
            _detectedFrequencies = null;
            freqFinder = new FrequencyFinder();
            // 隐藏进度指示器
            labelProgress.Visible = false;
            progressBarAnalysis.Visible = false;
            
            // 初始化后台工作线程
            InitializeBackgroundWorker();
        }

        /// <summary>
        /// 初始化BackgroundWorker后台工作线程
        /// 
        /// BackgroundWorker用于在后台线程执行耗时的频率分析操作，
        /// 避免阻塞UI线程，同时支持进度报告和取消操作
        /// </summary>
        private void InitializeBackgroundWorker()
        {
            analysisWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,    // 支持进度报告
                WorkerSupportsCancellation = true // 支持取消操作
            };
            
            // 绑定事件处理器
            analysisWorker.DoWork += AnalysisWorker_DoWork;              // 执行分析
            analysisWorker.ProgressChanged += AnalysisWorker_ProgressChanged;  // 更新进度
            analysisWorker.RunWorkerCompleted += AnalysisWorker_RunWorkerCompleted;  // 完成处理
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// Load按钮点击事件：从CSV文件加载波形数据
        /// </summary>
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            // 启动CSV加载器对话框
            CSVLoaderForm csvLoader = new CSVLoaderForm();
            csvLoader.ShowDialog();
            
            // 判断用户是否取消加载
            if (csvLoader.DialogResult == DialogResult.Cancel)
            {
                return;
            }
            
            // 获取并复制波形数据
            signal = new double[csvLoader.LoadedSeries.Length];
            Array.Copy(csvLoader.LoadedSeries, signal, csvLoader.LoadedSeries.Length);
            sampleRate = csvLoader.SampleRate;

            // 更新时域波形显示和频率范围
            UpdateTimeWaveform();
        }

        /// <summary>
        /// Analysis按钮点击事件：启动频率分析流程
        /// 使用后台线程执行分析，避免界面冻结
        /// </summary>
        private void buttonAnalysis_Click(object sender, EventArgs e)
        {
            // 防止重复点击：如果分析正在进行，提示用户等待
            if (analysisWorker.IsBusy)
            {
                MessageBox.Show("分析正在进行中，请稍候...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 验证数据有效性
            if (signal != null && signal.Length > 0 && sampleRate > 0)
            {
                try
                {
                    // 禁用按钮，防止重复操作
                    buttonAnalysis.Enabled = false;
                    buttonLoad.Enabled = false;
                    
                    // 初始化并显示进度条
                    progressBarAnalysis.Value = 0;
                    progressBarAnalysis.Visible = true;
                    labelProgress.Visible = true;
                    labelProgress.Text = "准备分析...";

                    Application.DoEvents();  // 强制刷新UI

                    // 启动后台工作线程执行分析
                    analysisWorker.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    ResetUIState();
                }
            }
            else
            {
                MessageBox.Show("请先加载信号数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        /// <summary>
        /// Y轴对数坐标显示选项改变事件
        /// </summary>
        private void checkBoxYAxisIsLog_CheckedChanged(object sender, EventArgs e)
        {
            easyChartXSpectrum.AxisY.IsLogarithmic = checkBoxYAxisIsLog.Checked;
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (_detectedFrequencies==null || _detectedFrequencies.Count == 0)
            {
                MessageBox.Show("No data to export. Run analysis first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv",
                Title = "Export Detected Components",
                FileName = "detected_components.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var lines = new List<string> { "Index,Frequency_Hz,Amplitude" };
            for (var i = 0; i < _detectedFrequencies.Count; i++)
            {
                var c = _detectedFrequencies[i];
                lines.Add($"{i + 1},{c.FrequencyHz.ToString("F4", CultureInfo.InvariantCulture)},{c.Amplitude.ToString("G8", CultureInfo.InvariantCulture)}");
            }
            File.WriteAllLines(dlg.FileName, lines);
        }

        private void buttonCopy_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. 检查DataGridView是否有数据
                if (dgvResults.Rows.Count == 0)
                {
                    MessageBox.Show("数据表格中暂无内容可复制！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 用于拼接剪贴板文本（Excel识别格式：制表符分隔单元格，换行分隔行）
                StringBuilder sb = new StringBuilder();

                // 2. 拼接表头（第一行）
                for (int colIndex = 0; colIndex < dgvResults.Columns.Count; colIndex++)
                {
                    // 跳过第一个单元格的制表符，避免开头空列
                    if (colIndex > 0)
                    {
                        sb.Append("\t");
                    }
                    // 拼接列名作为表头
                    sb.Append(dgvResults.Columns[colIndex].HeaderText);
                }
                // 表头结束，换行（Excel识别的换行符）
                sb.Append(Environment.NewLine);

                // 3. 拼接所有行数据（跳过空行）
                foreach (DataGridViewRow row in dgvResults.Rows)
                {
                    // 跳过DataGridView默认的空行（底部的新行）
                    if (row.IsNewRow)
                    {
                        continue;
                    }

                    // 拼接当前行的所有单元格
                    for (int colIndex = 0; colIndex < dgvResults.Columns.Count; colIndex++)
                    {
                        if (colIndex > 0)
                        {
                            sb.Append("\t");
                        }
                        // 处理空值：避免单元格为空时抛出异常，空值显示为空字符串
                        string cellValue = row.Cells[colIndex].Value?.ToString() ?? string.Empty;
                        // 替换特殊字符（防止制表符/换行符导致Excel列错位）
                        cellValue = cellValue.Replace("\t", " ").Replace("\n", " ").Replace("\r", "");
                        sb.Append(cellValue);
                    }
                    // 行结束，换行
                    sb.Append(Environment.NewLine);
                }

                // 4. 将拼接好的文本写入剪贴板
                Clipboard.SetText(sb.ToString());

                // 提示复制成功
                MessageBox.Show("全部数据已复制到剪贴板！可直接粘贴到Excel中。", "复制成功", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                // 异常处理：捕获复制过程中的错误（如剪贴板被占用）
                MessageBox.Show($"复制失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
        #region 辅助方法
        /// <summary>
        /// 初始化起始和停止频率控件
        /// 如果当前设置的频率不合理（超过奈奎斯特频率），则重置为合理值
        /// 
        /// 奈奎斯特定理：采样率为fs时，只能检测到fs/2以下的频率
        /// </summary>
        private void InitStartStopFrequency()
        {
            // 如果停止频率超过奈奎斯特频率（采样率的一半），则重置
            if ((double)numericUpDownStopFreq.Value > sampleRate * 0.5)
            {
                numericUpDownStartFreq.Value = (decimal)(sampleRate * 0.1);  // 10% 采样率
                numericUpDownStopFreq.Value = (decimal)(sampleRate * 0.4);    // 40% 采样率
            }
        }

        /// <summary>
        /// 更新时域波形显示
        /// 并初始化频率范围控件
        /// </summary>
        public void UpdateTimeWaveform()
        {
            // 在时域图表中显示完整波形
            easyChartXTimeWaveform.Plot(signal, 0, 1.0 / sampleRate);

            // 初始化频率范围控件为合理值
            InitStartStopFrequency();
        }
        /// <summary>
        /// 将检测到的频率组件填充到结果表格中
        /// 同时在控制台输出CSV格式数据，便于导出和分析
        /// </summary>
        /// <param name="components">检测到的频率组件列表</param>
        private void PopulateResultsGrid(List<DetectedComponent> components)
        {
            // 清空表格
            dgvResults.Rows.Clear();
            
            // 添加每一行数据
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                dgvResults.Rows.Add(i + 1, c.FrequencyHz.ToString("F2"), c.Amplitude.ToString("0.000e0"));
            }

            // 在控制台输出CSV格式数据（方便复制到Excel等工具）
            Console.WriteLine("Frequency_Hz,Amplitude_FFT");
            foreach (var c in components)
            {
                Console.WriteLine($"{c.FrequencyHz.ToString("F2", CultureInfo.InvariantCulture)},{c.Amplitude.ToString("0.000e0", CultureInfo.InvariantCulture)}");
            }
        }
        #endregion

        #region BackgroundWorker 事件处理
        /// <summary>
        /// BackgroundWorker DoWork事件 - 执行频率分析
        /// 
        /// 分析流程：
        /// 1. 预处理降采样（降低计算量）
        /// 2. 设计并应用带通/高通/低通滤波器
        /// 3. 计算FFT频谱
        /// 4. 使用Matrix Pencil算法进行高精度频率估计
        /// 5. 将频率检测结果与FFT幅度匹配
        /// 6. 选择并优化显示的频率分量
        /// </summary>
        private void AnalysisWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            try
            {
                freqFinder.FindFrequencies(signal, sampleRate, (double)numericUpDownStartFreq.Value, (double)numericUpDownStopFreq.Value
                    , (int)numericUpDownMaxPeakNum.Value, (double)numericUpDownRelativeThreshold.Value,worker);

            }
            catch (Exception ex)
            {
                e.Result = new { Error = ex.Message };
            }
        }

        /// <summary>
        /// BackgroundWorker ProgressChanged事件 - 更新进度条和提示信息
        /// </summary>
        private void AnalysisWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage <= 100)
            {
                progressBarAnalysis.Value = e.ProgressPercentage;
            }
            
            if (e.UserState != null && e.UserState is string message)
            {
                labelProgress.Text = message;
            }
            
            Application.DoEvents();
        }

        /// <summary>
        /// BackgroundWorker RunWorkerCompleted事件 - 分析完成处理
        /// 
        /// 操作：
        /// 1. 检查是否有错误发生
        /// 2. 提取分析结果
        /// 3. 在频谱图上显示FFT和检测到的频率点
        /// 4. 在表格中显示频率和幅度数据
        /// 5. 重置UI状态
        /// </summary>
        private void AnalysisWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                //首先检查是否有错误
                if (e.Error != null)
                {
                    MessageBox.Show(e.Error.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (freqFinder.Detected != null && freqFinder.Detected.Count > 0)
                {
                    //显示结果
                    _detectedFrequencies = freqFinder.Detected;
                    var _freqAxis = freqFinder._freqAxis;
                    var _fftAmp = freqFinder._fftAmp;

                    //修正频率控件值为实际值
                    numericUpDownStartFreq.Value = (decimal)freqFinder.Passband1;
                    numericUpDownStopFreq.Value = (decimal)freqFinder.Passband2;

                    //显示列表
                    PopulateResultsGrid(_detectedFrequencies);

                    //显示频谱
                    double[][] spectrumDisplayX = new double[2][];
                    double[][] spectrumDisplayY = new double[2][];
                    spectrumDisplayX[0] = _freqAxis;
                    spectrumDisplayY[0] = _fftAmp;
                    //第二对显示_detected全部元素
                    spectrumDisplayX[1] = new double[_detectedFrequencies.Count];
                    spectrumDisplayY[1] = new double[_detectedFrequencies.Count];
                    for (int i = 0; i < _detectedFrequencies.Count; i++)
                    {
                        spectrumDisplayX[1][i] = _detectedFrequencies[i].FrequencyHz;
                        spectrumDisplayY[1][i] = _detectedFrequencies[i].Amplitude;
                    }
                    easyChartXSpectrum.Plot(spectrumDisplayX, spectrumDisplayY);
                    //设置第二条series为散点图
                    easyChartXSpectrum.Series[1].Type = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.Point;
                    easyChartXSpectrum.Series[1].Color = System.Drawing.Color.Red;
                    easyChartXSpectrum.AxisY.IsLogarithmic = checkBoxYAxisIsLog.Checked;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ResetUIState();
            }
        }

        /// <summary>
        /// 重置UI状态
        /// 重新启用按钮，延迟隐藏进度条
        /// </summary>
        private void ResetUIState()
        {
            buttonAnalysis.Enabled = true;
            buttonLoad.Enabled = true;
            
            //延迟隐藏进度条，让用户看到100%
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ => 
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => 
                    {
                        progressBarAnalysis.Visible = false;
                        labelProgress.Visible = false;
                    }));
                }
                else
                {
                    progressBarAnalysis.Visible = false;
                    labelProgress.Visible = false;
                }
            });
        }
        #endregion
        #region 计算分析

        #endregion
    }
}
