using CSV_Loader;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Double;
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
                //报告初始进度
                worker.ReportProgress(0, "开始分析...");

                #region 1. 预处理降采样
                // 降采样目的：
                // - 降低数据量，减少计算时间
                // - 提高频率分辨率（相对误差减小）
                // - 需要先进行抗混叠低通滤波
                
                worker.ReportProgress(5, "预处理降采样...");
                
                double maxFreq = (double)numericUpDownStopFreq.Value;
                int downSampleRate = (int)Math.Floor(sampleRate / (maxFreq * 2.5));
                downSampleRate = Math.Max(1, downSampleRate);
                double[] resampledSignal;
                double resampledRate = sampleRate / downSampleRate;
                if (downSampleRate > 1)
                {
                    EasyFilter lpf = new EasyFilter();
                    lpf.DesignIIRFilter(IIRDesignMethod.Elliptic, IIRBandType.Lowpass, 3, 70, 1, 0.4 / downSampleRate, 0.6 / downSampleRate);
                    double[] lowpassFiltered = lpf.Filtering(signal);
                    resampledSignal = new double[lowpassFiltered.Length / downSampleRate];
                    for (int i = 0; i < resampledSignal.Length; i++)
                    {
                        resampledSignal[i] = lowpassFiltered[(i+1) * downSampleRate-1];
                    }
                }
                else
                {
                    resampledSignal = signal;
                }
                #endregion

                #region 2. 预处理滤波
                // 滤波目的：
                // - 去除目标频率范围外的噪声和干扰
                // - 提高Matrix Pencil算法的检测精度
                // - 根据用户设置自动选择滤波器类型（带通/高通/低通）
                
                worker.ReportProgress(10, "设计滤波器...");
                
                EasyFilter easyFilter = new EasyFilter();
                double passband1 = (double)numericUpDownStartFreq.Value;
                double passband2 = (double)numericUpDownStopFreq.Value;
                double cutoffband1 = 0.01 * resampledRate;
                double cutoffband2 = 0.49 * resampledRate;
                IIRBandType bandType = IIRBandType.Bandpass;
                
                //如果通带=0，使用低通滤波
                if (passband1 == 0)
                {
                    bandType = IIRBandType.Lowpass;
                }
                else
                {
                    //起始频率在0.05-0.4采样率之间
                    passband1 = Math.Max(0.01 * resampledRate, passband1);
                    passband1 = Math.Min(0.4 * resampledRate, passband1);
                }
                if (passband2 <= passband1)
                    passband2 = 0.5*resampledRate;
                passband2 = Math.Min(0.5*resampledRate, passband2);
                if (passband2 >= 0.49*resampledRate)
                {
                    bandType = IIRBandType.Highpass;
                }
                cutoffband1 = Math.Max(0.001*resampledRate, passband1 - 0.05 * resampledRate);
                cutoffband2 = Math.Min(0.49*resampledRate, passband2 + 0.05 * resampledRate);
                
                switch (bandType)
                {
                    case IIRBandType.Bandpass:
                        {
                            easyFilter.DesignIIRFilter(IIRDesignMethod.Elliptic, bandType, 3, 50, resampledRate, passband1, cutoffband1, passband2, cutoffband2);
                            break;
                        }
                    case IIRBandType.Highpass:
                        {
                            easyFilter.DesignIIRFilter(IIRDesignMethod.Elliptic, bandType, 3, 50, resampledRate, passband1, cutoffband1);
                            break;
                        }
                    case IIRBandType.Lowpass:
                        {
                            easyFilter.DesignIIRFilter(IIRDesignMethod.Elliptic, bandType, 3, 50, resampledRate, passband2, cutoffband2);
                            break;
                        }
                    default:
                        {
                            throw new Exception("不支持的滤波类型");
                        }
                }

                worker.ReportProgress(15, "应用滤波器...");

                //滤波
                double[] filteredSignal = easyFilter.Filtering(resampledSignal);
                #endregion

                #region 3. 频谱分析与频率检测
                worker.ReportProgress(20, "计算频谱...");
                
                //频谱分析
                double[] _freqAxis;
                double[] _fftAmp;
                (_freqAxis, _fftAmp) = ComputeFftSpectrum(filteredSignal, resampledRate);

                worker.ReportProgress(30, "Matrix Pencil频率检出...");

                //频率检出 - 使用Progress回调
                Progress<int> progress = new Progress<int>(percent =>
                {
                    int overallProgress = 30 + (int)(percent * 0.5); // 30-80%用于Matrix Pencil
                    worker.ReportProgress(overallProgress, $"Matrix Pencil分析中... {percent}%");
                });

                var matrixPencilFreq = MatrixPencilEstimate(filteredSignal, resampledRate, passband1, passband2
                    , (double)numericUpDownRelativeThreshold.Value, progress);

                worker.ReportProgress(85, "构建频率分量...");

                var rawComponents = BuildComponentsWithFftAmplitude(matrixPencilFreq, _freqAxis, _fftAmp, passband1, passband2);
                
                worker.ReportProgress(90, "选择显示分量...");

                List<DetectedComponent> _detected = SelectDisplayComponents(rawComponents, passband1, passband2
                    ,(int)numericUpDownMaxPeakNum.Value, (double)numericUpDownRelativeThreshold.Value);

                worker.ReportProgress(95, "准备显示结果...");

                //准备显示数据
                var displayData = new
                {
                    passband1,
                    passband2,
                    detected = _detected,
                    freqAxis = _freqAxis,
                    fftAmp = _fftAmp,
                    isLogAxis = checkBoxYAxisIsLog.Checked
                };

                e.Result = displayData;
                worker.ReportProgress(100, "分析完成！");
                #endregion
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

                if (e.Result != null)
                {
                    //使用反射来检查是否有Error属性
                    var resultType = e.Result.GetType();
                    var errorProperty = resultType.GetProperty("Error");
                    
                    if (errorProperty != null)
                    {
                        var errorValue = errorProperty.GetValue(e.Result);
                        if (errorValue != null)
                        {
                            MessageBox.Show(errorValue.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    //显示结果
                    dynamic result = e.Result;
                    _detectedFrequencies = (List<DetectedComponent>)result.detected;
                    var _freqAxis = (double[])result.freqAxis;
                    var _fftAmp = (double[])result.fftAmp;

                    //修正频率控件值为实际值
                    numericUpDownStartFreq.Value = (decimal)result.passband1;
                    numericUpDownStopFreq.Value = (decimal)result.passband2;

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
                    easyChartXSpectrum.AxisY.IsLogarithmic = result.isLogAxis;
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
        /// <summary>
        /// 计算FFT频谱（不加窗，获得最佳频率分辨率）
        /// 
        /// 使用标准FFT算法计算信号的频谱幅度
        /// 不加窗函数可以获得最佳的频率分辨率（主瓣最窄）
        /// 
        /// 参数：
        /// - signal: 输入时域信号
        /// - fs: 采样率（Hz）
        /// 
        /// 返回：
        /// - freq: 频率轴（Hz）
        /// - amp: 幅度谱（与输入信号单位相同）
        /// </summary>
        private static (double[] freq, double[] amp) ComputeFftSpectrum(double[] signal, double fs)
        {
            var n = signal.Length;
            var complex = signal.Select(v => new Complex(v, 0)).ToArray();
            Fourier.Forward(complex, FourierOptions.Matlab);

            var half = n / 2;
            var freq = new double[half + 1];
            var amp = new double[half + 1];
            for (var k = 0; k <= half; k++)
            {
                freq[k] = k * fs / n;
                amp[k] = 2.0 * complex[k].Magnitude / n;
            }

            if (amp.Length > 0)
            {
                amp[0] *= 0.5;
                if (n % 2 == 0)
                {
                    amp[amp.Length - 1] *= 0.5;
                }
            }

            return (freq, amp);
        }

        /// <summary>
        /// 使用Matrix Pencil算法估计信号中的频率分量
        /// 
        /// 算法原理：
        /// Matrix Pencil是一种基于子空间分解的高精度频率估计方法
        /// 它将信号建模为指数函数的线性组合，通过SVD和特征值分解求解
        /// 
        /// 算法步骤：
        /// 1. 构建Hankel矩阵
        /// 2. 对矩阵进行奇异值分解(SVD)
        /// 3. 根据奇异值确定信号阶数（有效频率数量）
        /// 4. 构建Pencil矩阵并求解特征值
        /// 5. 从特征值计算频率
        /// 
        /// 优点：
        /// - 精度高于FFT峰值检测（理论上可达Cramér-Rao界）
        /// - 不受频率栅栏限制
        /// - 适用于短数据和低信噪比情况
        /// 
        /// 参数：
        /// - x: 输入信号
        /// - fs: 采样率
        /// - fMin/fMax: 频率搜索范围
        /// - relativeThreshold: 奇异值相对阈值（控制信号阶数）
        /// - progress: 进度报告回调
        /// 
        /// 返回：检测到的频率列表（Hz）
        /// </summary>
        private static List<double> MatrixPencilEstimate(double[] x, double fs, double fMin, double fMax, double relativeThreshold=0.1, IProgress<int> progress = null)
        {
            // 确定Hankel矩阵的维度（Pencil参数L）
            var n = x.Length;
            var l = n / 3;  // L通常取N/3，平衡精度和计算量
            if (l < 32)
            {
                l = n / 2;
            }

            // 限制L的范围：[16, N-2]
            l = Math.Min(Math.Max(l, 16), n - 2);
            var k = n - l;  // 第二个维度

            progress?.Report(10); // 矩阵构建完成

            // 构建Hankel矩阵 Y (L x K)
            // Y[i,j] = x[i+j]
            var y = DenseMatrix.Create(l, k, (r, c) => x[r + c]);
            
            // 构建两个移位的子矩阵（形成Pencil）
            var y1 = y.SubMatrix(0, l, 0, k - 1);  // 去掉最后一列
            var y2 = y.SubMatrix(0, l, 1, k - 1);  // 去掉第一列

            progress?.Report(30); // SVD计算完成

            // 对Y1进行奇异值分解
            var svd = y1.Svd(true);
            var s = svd.S.ToArray();  // 奇异值（按降序排列）
            
            if (s.Length == 0)
            {
                return new List<double>();
            }

            progress?.Report(50); // 奇异值处理完成

            // 根据奇异值确定信号阶数M（有效频率数量）
            // 阈值 = 最大奇异值 * 相对阈值系数
            var threshold = s[0] * relativeThreshold;
            var m = s.Count(v => v > threshold);  // 超过阈值的奇异值数量
            // 限制M的范围：[8, 80]，不超过矩阵维度
            m = Math.Min(Math.Max(m, 8), Math.Min(80, Math.Min(y1.RowCount, y1.ColumnCount)));

            // 提取前M个左/右奇异向量
            var u = svd.U.SubMatrix(0, svd.U.RowCount, 0, m);
            var vt = svd.VT.SubMatrix(0, m, 0, svd.VT.ColumnCount);
            
            // 构建奇异值逆矩阵
            var sigmaInv = DenseMatrix.CreateDiagonal(m, m, i => 1.0 / s[i]);

            progress?.Report(70); // 矩阵构建完成

            // 计算Y1的伪逆：Y1⁺ = V * Σ⁻¹ * U^T
            var y1Pinv = vt.TransposeThisAndMultiply(sigmaInv).Multiply(u.Transpose());
            
            // 构建Pencil矩阵并求解特征值问题：A = Y1⁺ * Y2
            var a = y1Pinv.Multiply(y2);

            progress?.Report(85); // 特征值分解完成

            // 对矩阵A进行特征值分解
            var evd = a.Evd();
            var freqList = new List<double>();
            
            // 从特征值计算频率
            // 特征值 z = e^(j*2π*f/fs)  =>  f = arg(z) * fs / (2π)
            foreach (var z in evd.EigenValues)
            {
                // 忽略接近零的特征值
                if (z.Magnitude < 1e-10)
                {
                    continue;
                }

                // 计算频率
                var w = Math.Atan2(z.Imaginary, z.Real);  // 相位角
                var f = w * fs / (2.0 * Math.PI);
                
                // 将负频率映射到正频率
                if (f < 0)
                {
                    f += fs;
                }

                // 只保留在指定频率范围内的频率
                if (f >= fMin && f <= fMax)
                {
                    freqList.Add(f);
                }
            }

            progress?.Report(95); // 频率筛选完成

            // 按频率排序
            freqList.Sort();

            // 合并接近的频率（相差<1Hz的认为是同一频率）
            var merged = new List<double>();
            const double mergeHz = 1.0;
            foreach (var f in freqList)
            {
                if (merged.Count == 0 || Math.Abs(f - merged[merged.Count - 1]) > mergeHz)
                {
                    merged.Add(f);
                }
            }

            progress?.Report(100); // 全部完成

            return merged;
        }

        /// <summary>
        /// 将Matrix Pencil检测到的频率与FFT幅度匹配，构建频率分量列表
        /// 
        /// 原理：
        /// Matrix Pencil提供高精度频率估计，但不直接提供幅度信息
        /// FFT提供幅度信息，但频率分辨率有限
        /// 本方法结合两者优点：用MP的频率在FFT谱中插值获取精确幅度
        /// </summary>
        private static List<DetectedComponent> BuildComponentsWithFftAmplitude(
            List<double> frequencies,
            double[] fftFreq,
            double[] fftAmp,
            double fMin,
            double fMax)
        {
            var list = new List<DetectedComponent>();
            foreach (var f in frequencies)
            {
                if (f < fMin || f > fMax)
                {
                    continue;
                }

                var amp = InterpolateAmplitude(fftFreq, fftAmp, f);
                if (amp <= 0)
                {
                    continue;
                }

                list.Add(new DetectedComponent
                {
                    FrequencyHz = f,
                    Amplitude = amp
                });
            }

            // 按频率分组（四舍五入到0.1Hz），保留每组中幅度最大的
            return list
                .GroupBy(c => Math.Round(c.FrequencyHz, 1))
                .Select(g => g.OrderByDescending(x => x.Amplitude).First())
                .OrderBy(c => c.FrequencyHz)
                .ToList();
        }

        /// <summary>
        /// 检测到的频率分量数据结构
        /// </summary>
        private sealed class DetectedComponent
        {
            public double FrequencyHz { get; set; }  // 频率（Hz）
            public double Amplitude { get; set; }    // 幅度
        }

        /// <summary>
        /// 在FFT频谱中插值计算指定频率的幅度
        /// 
        /// 使用线性插值，当目标频率不在FFT频率栅格上时，
        /// 从相邻的两个频率点线性插值获取幅度
        /// </summary>
        private static double InterpolateAmplitude(double[] freq, double[] amp, double targetFreq)
        {
            if (targetFreq <= freq[0])
            {
                return amp[0];
            }

            if (targetFreq >= freq[freq.Length-1])
            {
                return amp[amp.Length-1];
            }

            var idx = Array.BinarySearch(freq, targetFreq);
            if (idx >= 0)
            {
                return amp[idx];
            }

            idx = ~idx;
            var left = idx - 1;
            var right = idx;
            var denom = freq[right] - freq[left];
            if (Math.Abs(denom) < 1e-12)
            {
                return amp[left];
            }

            var t = (targetFreq - freq[left]) / denom;
            return amp[left] + (amp[right] - amp[left]) * t;
        }

        /// <summary>
        /// 从检测到的频率分量中选择用于显示的分量
        /// 
        /// 选择策略：
        /// 1. 幅度阈值筛选：只保留相对幅度大于阈值的分量
        /// 2. 确保包含最低频分量
        /// 3. 确保包含最高频分量
        /// 4. 确保包含低频段(0-80Hz)的最大峰值
        /// 5. 按幅度从大到小添加，直到达到最大数量
        /// 6. 去除过于接近的频率（>0.4Hz才认为是不同频率）
        /// 
        /// 参数：
        /// - components: 原始频率分量列表
        /// - fMin/fMax: 频率范围
        /// - MaxDisplayComponents: 最大显示数量（默认13个）
        /// - RelativeComponentThreshold: 相对幅度阈值（默认10%）
        /// </summary>
        private static List<DetectedComponent> SelectDisplayComponents(List<DetectedComponent> components, double fMin, double fMax
            , int MaxDisplayComponents=13, double RelativeComponentThreshold = 0.10)
        {
            var pool = components
                .Where(c => c.FrequencyHz >= fMin && c.FrequencyHz <= fMax && c.Amplitude > 0)
                .OrderBy(c => c.FrequencyHz)
                .ToList();

            if (pool.Count == 0)
            {
                return new List<DetectedComponent>();
            }

            var maxAmp = pool.Max(c => c.Amplitude);
            var threshold = Math.Max(maxAmp * RelativeComponentThreshold, 1e-16);
            var candidates = pool.Where(c => c.Amplitude >= threshold).ToList();
            if (candidates.Count == 0)
            {
                candidates = pool
                    .OrderByDescending(c => c.Amplitude)
                    .Take(Math.Min(MaxDisplayComponents, pool.Count))
                    .ToList();
            }

            var selected = new List<DetectedComponent>();
            AddDistinctByFrequency(selected, candidates.First(), 0.4);
            AddDistinctByFrequency(selected, candidates.Last(), 0.4);

            var lowBandUpper = Math.Min(fMax, fMin + 80.0);
            var lowBandPeak = candidates
                .Where(c => c.FrequencyHz >= fMin && c.FrequencyHz <= lowBandUpper)
                .OrderByDescending(c => c.Amplitude)
                .FirstOrDefault();
            if (lowBandPeak != null)
            {
                AddDistinctByFrequency(selected, lowBandPeak, 0.4);
            }

            foreach (var c in candidates.OrderByDescending(c => c.Amplitude))
            {
                if (selected.Count >= MaxDisplayComponents)
                {
                    break;
                }

                AddDistinctByFrequency(selected, c, 0.4);
            }

            var minKeep = Math.Min(8, pool.Count);
            if (selected.Count < minKeep)
            {
                foreach (var c in pool.OrderByDescending(c => c.Amplitude))
                {
                    if (selected.Count >= minKeep)
                    {
                        break;
                    }

                    AddDistinctByFrequency(selected, c, 0.4);
                }
            }

            if (selected.Count > MaxDisplayComponents)
            {
                var mandatory = new List<DetectedComponent>();
                AddDistinctByFrequency(mandatory, selected.OrderBy(c => c.FrequencyHz).First(), 0.4);
                AddDistinctByFrequency(mandatory, selected.OrderBy(c => c.FrequencyHz).Last(), 0.4);
                if (lowBandPeak != null)
                {
                    AddDistinctByFrequency(mandatory, lowBandPeak, 0.4);
                }

                var trimmed = new List<DetectedComponent>(mandatory);
                foreach (var c in selected.OrderByDescending(c => c.Amplitude))
                {
                    if (trimmed.Count >= MaxDisplayComponents)
                    {
                        break;
                    }

                    AddDistinctByFrequency(trimmed, c, 0.4);
                }

                selected = trimmed;
            }

            return selected.OrderBy(c => c.FrequencyHz).ToList();
        }
        /// <summary>
        /// 向列表添加频率分量，确保频率间隔足够大
        /// 
        /// 只有当新分量的频率与列表中所有已存在分量的频率
        /// 都相差超过toleranceHz时，才会添加到列表
        /// </summary>
        private static void AddDistinctByFrequency(ICollection<DetectedComponent> list, DetectedComponent component, double toleranceHz)
        {
            if (list.All(c => Math.Abs(c.FrequencyHz - component.FrequencyHz) > toleranceHz))
            {
                list.Add(component);
            }
        }

        #endregion


    }
    #region 滤波器设计类
    /// <summary>
    /// 简易滤波器设计类
    /// 
    /// 功能：
    /// - 支持FIR和IIR滤波器设计
    /// - 支持低通、高通、带通、带阻滤波器
    /// - 支持多种设计方法（Kaiser窗、Parks-McClellan、Butterworth、Chebyshev、Elliptic）
    /// - 提供滤波、波特图分析、冲激响应分析
    /// </summary>
    public class EasyFilter
    {
        #region public fields
        /// <summary>
        /// 设计的滤波器类型
        /// </summary>
        public AntialiasingFilterType SelectedFilterType { get; set; }
        /// <summary>
        /// 预计的滤波器阶数
        /// </summary>
        public int _estimatedFilterOrder { get; set; } = 0;
        /// <summary>
        /// 通带波动 (dB)
        /// </summary>
        public double PassbandRippleDB { get; set; } = 0.01;
        /// <summary>
        /// 阻带抑制(dB)
        /// </summary>
        public double StopbandRejectionDB { get; set; } = 60.0;
        /// <summary>
        /// 带通频率 (低通、高通的通带边界，和带通、带阻较低的通带边界)
        /// </summary>
        public double passbandFreq { get; set; } = 0;
        /// <summary>
        /// 第二通带频率（带通、带阻滤波器较高的通带边界）
        /// </summary>
        public double passbandFreq2 { get; set; } = 0;
        /// <summary>
        /// 截止频率 (高通、低通阻带边界，和带通、带阻较低的阻带边界)
        /// </summary>
        public double cutoffFreq { get; set; } = 0;
        /// <summary>
        /// 第二截止频率 (带通、带阻滤波器中的较高的阻带边界)
        /// </summary>
        public double cutoffFreq2 { get; set; } = 0;
        /// <summary>
        /// 采样率
        /// </summary>
        public double SampleRate { get; set; } = 1;
        /// <summary>
        /// FIR滤波器系数b，FIR滤波系数，分子系数
        /// </summary>
        public double[] _coef_b;
        /// <summary>
        /// FIR滤波器系数a，IIR滤波系数，分母系数
        /// </summary>
        public double[] _coef_a;
        /// <summary>
        /// 波特图幅度 (dB)
        /// </summary>
        public double[] bodeMag = null;
        /// <summary>
        /// 波特图相位 (degree)
        /// </summary>
        public double[] bodePhase = null;
        /// <summary>
        /// 波特图频率 (Hz)
        /// </summary>
        public double[] bodeFreq = null;
        /// <summary>
        ///  冲击响应幅度
        /// </summary>
        public double[] impulseResponseH = null;
        /// <summary>
        /// 冲击响应时间
        /// </summary>
        public double[] impulseResponseT = null;
        #endregion

        #region private fields
        double[] _firWeights;
        double[] _firAmps;
        double _firPassbandRipple;
        /// <summary>
        /// Normallized to sample rate = 2, hard code according to conventions
        /// </summary>
        double[] _firNormallizedFrequencyEdges;
        FIRDesignMethod _firDesignMethod = FIRDesignMethod.Equiripple;
        double _kaiserWindowBeta = 1;
        FIRBandType _firBandType = FIRBandType.Lowpass;

        IIRDesignMethod _iirDesignMethod = IIRDesignMethod.Elliptic;
        IIRBandType _iirBandType;
        double[] _iir3dBFrequencies;
        double[] _iirPassbandFrequencies;
        double[] _iirStopbandFrequencies;
        double _iirPassbandRippleDB;
        double _iirStopbandRippleDB;

        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数：初始化滤波器系数为空
        /// </summary>
        public EasyFilter()
        {
            // 滤波系数设空，如遇异常，输出空系数
            _coef_a = null;
            _coef_b = null;
        }
        #endregion
        
        #region FIR滤波器设计
        /// <summary>
        /// 经典FIR滤波器设计
        /// 
        /// 根据通带/阻带参数自动估计滤波器阶数并设计滤波器
        /// 
        /// 参数：
        /// - FIRDesignMethod: 设计方法（Kaiser窗或Parks-McClellan等纹波）
        /// - BandType: 滤波器类型（低通、高通、带通、带阻）
        /// - PassBandRipple_dB: 通带波动
        /// - StopBandRejection_dB: 阻带抑制
        /// - SampleRate: 采样率
        /// - PassBandFrequency: 通带频率
        /// - CutoffFrequency: 截止频率
        /// - PassBandFrequency2: 第二通带频率（带通/带阻）
        /// - CutoffFrequency2: 第二截止频率（带通/带阻）
        /// </summary>
        public void DesignFIRFilter(FIRDesignMethod FIRDesignMethod, FIRBandType BandType, double PassBandRipple_dB, double StopBandRejection_dB, double SampleRate
            , double PassBandFrequency, double CutoffFrequency, double PassBandFrequency2 = 0, double CutoffFrequency2 =0)
        {
            //滤波系数设空，如遇异常，输出空系数
            _coef_a = null;
            _coef_b = null;

            SelectedFilterType = AntialiasingFilterType.FIR;
            //参数传递给本地变量
            _firDesignMethod = FIRDesignMethod;
            _firBandType = BandType;
            PassbandRippleDB = PassBandRipple_dB;
            StopbandRejectionDB = StopBandRejection_dB;
            this.SampleRate = SampleRate;
            passbandFreq = PassBandFrequency;
            cutoffFreq = CutoffFrequency;
            passbandFreq2 = PassBandFrequency2;
            cutoffFreq2 = CutoffFrequency2;

            //阶数估计
            EstimateFilterOrder();

            //滤波器设计
            DesignFilter();
        }
        #endregion
        
        #region IIR滤波器设计
        /// <summary>
        /// 经典IIR滤波器设计
        /// 
        /// 根据通带/阻带参数自动估计滤波器阶数并设计滤波器
        /// 
        /// 参数：
        /// - IIRDesignMethod: 设计方法（Butterworth/Chebyshev I/Chebyshev II/Elliptic）
        /// - BandType: 滤波器类型（低通、高通、带通、带阻）
        /// - PassBandRipple_dB: 通带波动
        /// - StopBandRejection_dB: 阻带抑制
        /// - SampleRate: 采样率
        /// - PassBandFrequency: 通带频率
        /// - CutoffFrequency: 截止频率
        /// - PassBandFrequency2: 第二通带频率（带通/带阻）
        /// - CutoffFrequency2: 第二截止频率（带通/带阻）
        /// </summary>
        public void DesignIIRFilter(IIRDesignMethod IIRDesignMethod, IIRBandType BandType, double PassBandRipple_dB, double StopBandRejection_dB, double SampleRate
            , double PassBandFrequency, double CutoffFrequency, double PassBandFrequency2 = 0, double CutoffFrequency2 = 0)
        {
            //滤波系数设空，如遇异常，输出空系数
            _coef_a = null;
            _coef_b = null;

            SelectedFilterType = AntialiasingFilterType.IIR;
            //参数传递给本地变量
            _iirDesignMethod = IIRDesignMethod;
            _iirBandType = BandType;
            PassbandRippleDB = PassBandRipple_dB;
            StopbandRejectionDB = StopBandRejection_dB;
            this.SampleRate = SampleRate;
            passbandFreq = PassBandFrequency;
            cutoffFreq = CutoffFrequency;
            passbandFreq2 = PassBandFrequency2;
            cutoffFreq2 = CutoffFrequency2;

            //阶数估计
            EstimateFilterOrder();

            //滤波器设计
            DesignFilter();
        }
        #endregion
        
        #region 滤波器阶数估计
        /// <summary>
        /// 从滤波器设计参数估计所需的滤波器阶数
        /// 
        /// FIR阶数估计：
        /// - Kaiser窗：基于Kaiser公式
        /// - Parks-McClellan：基于经验公式
        /// 
        /// IIR阶数估计：
        /// - 基于滤波器类型和规格参数（通带/阻带波动）
        /// </summary>
        private void EstimateFilterOrder()
        {
            //如果参数合理，估计滤波阶数

            _firPassbandRipple = Math.Pow(10, (double)PassbandRippleDB / 20) - 1;
            double stopbandAmplitude = Math.Pow(10, -(double)StopbandRejectionDB / 20);
            switch (SelectedFilterType)
            {
                case AntialiasingFilterType.FIR:
                    double[] bands = null;
                    double[] amps = null;
                    double[] ripples = null;

                    switch (_firBandType)
                    {
                        case FIRBandType.Lowpass:
                            bands = new double[] { passbandFreq, cutoffFreq };
                            amps = new double[] { 1.0, 0.0 };
                            ripples = new double[] { _firPassbandRipple, stopbandAmplitude };
                            break;
                        case FIRBandType.Highpass:
                            bands = new double[] { cutoffFreq, passbandFreq };
                            amps = new double[] { 0.0, 1.0 };
                            ripples = new double[] { stopbandAmplitude, _firPassbandRipple };
                            break;
                        case FIRBandType.Bandpass:
                            bands = new double[] { cutoffFreq, passbandFreq, passbandFreq2, cutoffFreq2 };
                            amps = new double[] { 0.0, 1.0, 0.0 };
                            ripples = new double[] { stopbandAmplitude, _firPassbandRipple, stopbandAmplitude };
                            break;
                        case FIRBandType.Bandstop:
                            bands = new double[] { passbandFreq, cutoffFreq, cutoffFreq2, passbandFreq2 };
                            amps = new double[] { 1.0, 0.0, 1.0 };
                            ripples = new double[] { _firPassbandRipple, stopbandAmplitude, _firPassbandRipple };
                            break;
                        default:
                            bands = new double[] { passbandFreq, cutoffFreq };
                            amps = new double[] { 1.0, 0.0 };
                            ripples = new double[] { _firPassbandRipple, stopbandAmplitude };
                            break;
                    }

                    _firWeights = new double[bands.Length / 2];
                    switch (_firDesignMethod)
                    {
                        case FIRDesignMethod.KaiserWindow:
                            (_estimatedFilterOrder, _firNormallizedFrequencyEdges, _kaiserWindowBeta, _firBandType)
                                = FIRDesign.KaiseOrd(bands, amps, ripples, SampleRate);
                            break;
                        default:
                            (_estimatedFilterOrder, _firNormallizedFrequencyEdges, _firAmps, _firWeights)
                                = FIRDesign.FIRPMOrd(bands, amps, ripples, SampleRate);
                            break;
                    }
                    break;
                case AntialiasingFilterType.IIR:
                    switch (_iirBandType)
                    {
                        case IIRBandType.Lowpass:
                        case IIRBandType.Highpass:
                            _iirPassbandFrequencies = new double[] { passbandFreq };
                            _iirStopbandFrequencies = new double[] { cutoffFreq };
                            break;
                        case IIRBandType.Bandpass:
                        case IIRBandType.Bandstop:
                        default:
                            _iirPassbandFrequencies = new double[] { passbandFreq, passbandFreq2 };
                            _iirStopbandFrequencies = new double[] { cutoffFreq, cutoffFreq2 };
                            break;
                    }
                    _iirPassbandRippleDB = PassbandRippleDB;
                    _iirStopbandRippleDB = StopbandRejectionDB;
                    (_estimatedFilterOrder, _iir3dBFrequencies) = IIRDesign.OrderEstimation(
                        _iirPassbandFrequencies, _iirStopbandFrequencies, _iirPassbandRippleDB, _iirStopbandRippleDB
                        , _iirDesignMethod, SampleRate);
                    break;
                default:
                    break;
            }
        }
        #endregion
        
        #region 滤波器设计
        /// <summary>
        /// 根据估计的阶数和设计参数进行滤波器系数计算
        /// 
        /// FIR设计：
        /// - Kaiser窗：使用Kaiser窗函数
        /// - Equiripple：使用Parks-McClellan算法
        /// 
        /// IIR设计：
        /// - Butterworth：最大平坦响应
        /// - Chebyshev I：通带等纹波
        /// - Chebyshev II：阻带等纹波
        /// - Elliptic：通带和阻带都是等纹波（阶数最低）
        /// 
        /// 参数验证：
        /// - 确保频率参数合理（符合奈奎斯特定理）
        /// - 确保通带和截止频率顺序正确
        /// </summary>
        private void DesignFilter()
        {
            //** 设计滤波器**
            //针对波段滤波类型，检查参数是否合理
            //低通滤波 0<=通带频率<截止频率<采样率；通带频率<0.5采样率
            //高通滤波 通带频率>截止频率>=0；通带频率<0.5采样率
            //带通滤波 0<=截止频率<通带频率<=通带频率2<截止频率2<采样率；通带频率2<0.5采样率
            //带阻滤波 0<=通带频率<截止频率<=截止频率2<通带频率2<0.5采样率
            bool settingsRational = false;
            string sanitaryReport = "";

            switch (_iirBandType)
            {
                case IIRBandType.Lowpass:
                    settingsRational = (0 <= passbandFreq && passbandFreq < cutoffFreq
                        && cutoffFreq < SampleRate && passbandFreq < 0.5 * SampleRate);
                    if (!settingsRational)
                        sanitaryReport = "低通滤波器参数不合理， 需要满足：0<=通带频率<截止频率<采样率；通带频率<0.5采样率";
                    break;
                case IIRBandType.Highpass:
                    settingsRational = (passbandFreq > cutoffFreq && cutoffFreq >= 0
                        && passbandFreq < 0.5 * SampleRate);
                    if (!settingsRational)
                        sanitaryReport = "高通滤波器参数不合理， 需要满足：通带频率>截止频率>=0；通带频率<0.5采样率";
                    break;
                case IIRBandType.Bandpass:
                    settingsRational = (0 <= cutoffFreq && cutoffFreq < passbandFreq && passbandFreq <= passbandFreq2
                        && passbandFreq2 < cutoffFreq2 && cutoffFreq2 < SampleRate
                        && passbandFreq2 < 0.5 * SampleRate);
                    if (!settingsRational)
                        sanitaryReport = "带通滤波器参数不合理， 需要满足：0<=截止频率<通带频率<=通带频率2<截止频率2<采样率；通带频率2<0.5采样率";
                    break;
                case IIRBandType.Bandstop:
                    settingsRational = (0 <= passbandFreq && passbandFreq < cutoffFreq && cutoffFreq <= cutoffFreq2
                        && cutoffFreq2 < passbandFreq2 && passbandFreq2 < 0.5 * SampleRate);
                    if (!settingsRational)
                        sanitaryReport = "带阻滤波器参数不合理， 需要满足：0<=通带频率<截止频率<=截止频率2<通带频率2<0.5采样率";
                    break;
                default:
                    break;
            }
            //如果参数不合理，抛异常
            if(!settingsRational)
            {
                throw new Exception(sanitaryReport);
            }
            //区分FIR和IIR设计
            switch (SelectedFilterType)
            {
                case AntialiasingFilterType.FIR:
                    //根据设计方法对应窗函数设计
                    switch (_firDesignMethod)
                    {
                        case FIRDesignMethod.Equiripple:
                            _coef_b = FIRDesign.ParksMcClellan(_estimatedFilterOrder, _firNormallizedFrequencyEdges
                                , _firAmps, _firWeights);
                            break;
                        case FIRDesignMethod.KaiserWindow:
                            _coef_b = FIRDesign.Window(_estimatedFilterOrder, _firNormallizedFrequencyEdges
                                , WindowType.Kaiser, _kaiserWindowBeta, _firBandType, true, 2);
                            break;
                        default:
                            _coef_b = new double[] { 1 };
                            break;
                    }
                    //FIR analysis
                    (bodeMag, bodePhase, bodeFreq) = FIRAnalysis.Bode(_coef_b, SampleRate, 512);
                    (impulseResponseH, impulseResponseT) = FIRAnalysis.ImpulseResponse(_coef_b, SampleRate, _coef_b.Length);
                    break;
                case AntialiasingFilterType.IIR:
                    switch (_iirDesignMethod)
                    {
                        case IIRDesignMethod.Butterworth:
                            (_coef_b, _coef_a) = IIRDesign.Butter(_estimatedFilterOrder, _iir3dBFrequencies
                                , _iirBandType, SampleRate);
                            break;
                        case IIRDesignMethod.ChebyshevI:
                            (_coef_b, _coef_a) = IIRDesign.Cheby1(_estimatedFilterOrder, _iir3dBFrequencies
                                , _iirPassbandRippleDB, _iirBandType, SampleRate);
                            break;
                        case IIRDesignMethod.ChebyshevII:
                            (_coef_b, _coef_a) = IIRDesign.Cheby2(_estimatedFilterOrder, _iir3dBFrequencies
                                , _iirStopbandRippleDB, _iirBandType, SampleRate);
                            break;
                        case IIRDesignMethod.Elliptic:
                            (_coef_b, _coef_a) = IIRDesign.Ellip(_estimatedFilterOrder, _iir3dBFrequencies
                                , _iirPassbandRippleDB, _iirStopbandRippleDB, _iirBandType, SampleRate);
                            break;
                        default:
                            _coef_b = new double[] { 1 };
                            _coef_a = new double[] { 1 };
                            break;
                    }
                    (bodeMag, bodePhase, bodeFreq) = IIRAnalysis.Bode(_coef_b, _coef_a
                        , SampleRate, 512);
                    (impulseResponseH, impulseResponseT) = IIRAnalysis.ImpulseResponse(_coef_b, _coef_a, SampleRate, 512);
                    break;
                default:

                    break;
            }
        }
        #endregion
        
        #region 滤波操作
        /// <summary>
        /// 对输入信号应用设计的滤波器
        /// 
        /// 根据滤波器类型选择相应的滤波方法：
        /// - FIR滤波：直接卷积
        /// - IIR滤波：递归滤波（差分方程）
        /// - 未设计滤波器：直接返回原信号
        /// </summary>
        /// <param name="InputSignal">输入信号</param>
        /// <returns>滤波后的信号</returns>
        public double[] Filtering(double[] InputSignal)
        { 
            if(_coef_a==null & _coef_b!=null && _coef_b.Length>0)
            {
                //FIR滤波
                return FIRFiltering.FIRFilter(_coef_b, InputSignal);
            }
            else
            {
                if(_coef_b!=null && _coef_a.Length>0 && _coef_b.Length>0)
                {
                    return IIRFiltering.IIRFilter(_coef_b, _coef_a, InputSignal);
                }
                else
                {
                    return InputSignal;
                }
            }
        }
        #endregion
        
        #region 枚举定义
        /// <summary>
        /// FIR滤波器设计方法枚举
        /// </summary>
        public enum FIRDesignMethod
        {
            KaiserWindow,   // Kaiser窗函数法
            Equiripple      // 等纹波法（Parks-McClellan算法）
        }
        #endregion
    }
    #endregion
}
