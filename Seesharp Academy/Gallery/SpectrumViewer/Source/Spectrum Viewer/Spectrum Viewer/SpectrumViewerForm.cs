using CSV_Loader;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Peak_Frequency_Finder;
using ScottPlot;
using SeeSharpTools.JXI.SignalProcessing.JTFA;
using SeeSharpTools.JY.DSP.Fundamental;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Spectrum_Viewer
{
    /// <summary>
    /// 频谱分析器主窗体类
    /// 功能：
    /// 1. 从CSV文件加载波形数据
    /// 2. 显示时域波形
    /// 3. 支持光标切割波形段
    /// 4. 对切割波形进行频谱分析（功率谱/倒谱）
    /// 5. 进行联合时频分析(JTFA)
    /// 6. 调用峰值频率查找器
    /// </summary>
    public partial class SpectrumViewerForm : Form
    {
        #region 全局变量
        /// <summary>
        /// 全局波形数据（从CSV加载）
        /// </summary>
        double[] waveform;
        
        /// <summary>
        /// 采样率（Hz）
        /// </summary>
        double sampleRate;
        
        /// <summary>
        /// 频谱分析截断波形（通过光标选择）
        /// </summary>
        double[] waveformSection;
        
        /// <summary>
        /// 截断波形的频谱数据
        /// </summary>
        double[] waveformSectionSpectrum;
        
        /// <summary>
        /// 频谱的频率步进（Hz）
        /// </summary>
        double spectrumDeltaF;
        
        /// <summary>
        /// 联合时频分析(JTFA)任务对象
        /// </summary>
        GeneralJTFATask _task;
        
        /// <summary>
        /// 时频图谱数据（单位：dB）
        /// 第一维：时间轴
        /// 第二维：频率轴
        /// </summary>
        double[,] tfDistributionDB;
        #endregion
        #region 构造函数
        /// <summary>
        /// 构造函数：初始化窗体组件和JTFA相关控件
        /// </summary>
        public SpectrumViewerForm()
        {
            InitializeComponent();
            
            // 初始化JTFA窗函数类型下拉框
            var windowTypes = Enum.GetNames(typeof(SeeSharpTools.JXI.SignalProcessing.Window.WindowType));
            foreach (var item in windowTypes)
            {
                WindowTypes.Items.Add(item);
            }
            WindowTypes.SelectedIndex = 3; // 默认选择第4个窗函数类型
            
            // 初始化热力图颜色类型下拉框
            comboBoxColorType.Items.Clear();
            var colorTypes = Enum.GetNames(typeof(ScottHeatMapColor));
            foreach (var item in colorTypes)
            {
                comboBoxColorType.Items.Add(item);
            }
            comboBoxColorType.SelectedIndex = 29; // 默认选择Turbo颜色映射
            
            // 创建JTFA任务对象
            _task = new GeneralJTFATask();
        }
        #endregion
        #region 事件处理 - 数据加载
        /// <summary>
        /// 从CSV文件导入波形数据
        /// 操作步骤：
        /// 1. 打开CSV加载器对话框
        /// 2. 获取加载的波形数据和采样率
        /// 3. 显示波形到时域图表
        /// 4. 更新窗体标题显示文件名
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
            waveform = new double[csvLoader.LoadedSeries.Length];
            Array.Copy(csvLoader.LoadedSeries, waveform, csvLoader.LoadedSeries.Length);
            sampleRate = csvLoader.SampleRate;
            
            // 更新采样率显示
            numericUpDownSampleRate.Value = (decimal)sampleRate;
            
            // 在时域图表中显示完整波形
            DisplayWaveform();

            // 在窗体标题中显示当前加载的文件名
            this.Text = "Spectrum Viewer: " + csvLoader.LoadedFileName;
        }
        #endregion
        #region 事件处理 - 光标切割
        /// <summary>
        /// 时域波形图表光标位置改变事件
        /// 当用户移动光标时，自动更新切割的波形段
        /// </summary>
        private void easyChartXTimeWaveform_TabCursorChanged(object sender, SeeSharpTools.JY.GUI.TabCursorEventArgs e)
        {
            UpdateCutWaveform();
        }
        /// <summary>
        /// 在时域图表中显示完整波形
        /// </summary>
        private void DisplayWaveform()
        {
            if (waveform != null && waveform.Length > 0)
            {
                // 绘制波形，X轴间隔为采样周期
                easyChartXTimeWaveform.Plot(waveform, 0, 1.0 / sampleRate);
            }
        }

        /// <summary>
        /// 根据光标位置和窗口长度更新切割的波形段
        /// 步骤：
        /// 1. 从光标位置计算起始索引
        /// 2. 确保索引和窗口长度在有效范围内
        /// 3. 提取波形段数据
        /// 4. 显示切割后的波形
        /// 5. 对切割波形进行频谱分析
        /// </summary>
        private void UpdateCutWaveform()
        {
            if (waveform == null || waveform.Length == 0)
            {
                return;
            }

            sampleRate = (double)numericUpDownSampleRate.Value;
            
            // 根据光标位置计算起始索引
            double t0 = easyChartXTimeWaveform.TabCursors[0].XValue;
            int startIndex = (int)(t0 * sampleRate);
            int winLength = (int)numericUpDownWindowLength.Value;
            
            // 边界检查：确保起始索引非负
            if (startIndex < 0)
            {
                startIndex = 0;
            }
            
            // 确保窗口长度不超过数据长度
            if(winLength > waveform.Length)
            {
                winLength = waveform.Length;
            }
            
            // 如果超出数据范围，调整起始位置
            if (startIndex + winLength > waveform.Length)
            {
                startIndex = waveform.Length - winLength;
                if (startIndex < 0) startIndex = 0;
            }
            
            // 更新第二个光标位置到选择区间的末尾
            easyChartXTimeWaveform.TabCursors[1].XValue = (startIndex + winLength) / sampleRate;

            // 复制切割的波形数据
            waveformSection = new double[winLength];
            Array.Copy(waveform, startIndex, waveformSection, 0, winLength);
            
            // 在切割波形图表中显示
            easyChartXWaveformSection.Plot(waveformSection, 0, 1.0 / sampleRate);
            
            // 对切割波形进行频谱分析
            SectionAnalysis();
        }
        #endregion
        #region 频谱分析
        /// <summary>
        /// 对切割的波形段进行频谱分析
        /// 根据用户选择执行：
        /// - 倒谱分析(Cepstrum)：用于检测信号的周期性
        /// - 功率谱分析：线性(dBv)或对数(dBV)幅度显示
        /// </summary>
        private void SectionAnalysis()
        {
            if (waveformSection == null || waveformSection.Length == 0)
            {
                return;
            }

            sampleRate = (double)numericUpDownSampleRate.Value;
            
            // 计算频谱数据（长度为原始信号的一半，因为FFT结果对称）
            waveformSectionSpectrum = new double[(waveformSection.Length + 1) / 2];
            SpectrumUnits spectrumUnit = SpectrumUnits.V2;
            
            if (checkBoxCepstrum.Checked)
            {
                // === 倒谱分析 ===
                // 公式：y = real(ifft(log(abs(fft(x)))))
                // 应用：检测信号的谐波周期性，常用于回声检测、音高检测等
                
                // 步骤1：傅里叶变换(FFT)
                double[] dataReal = new double[waveformSection.Length];
                Array.Copy(waveformSection, dataReal, waveformSection.Length);
                double[] dataImg = new double[waveformSection.Length];
                Fourier.Forward(dataReal, dataImg, FourierOptions.Default);
                
                // 步骤2：计算幅度谱并取对数，虚部置零
                for (int i = 0; i < dataReal.Length; i++)
                {
                    // 计算复数幅度：sqrt(real^2 + imag^2)
                    double magnitude = Math.Sqrt(dataReal[i] * dataReal[i] + dataImg[i] * dataImg[i]);
                    // 取对数
                    dataReal[i] = Math.Log(magnitude);
                    dataImg[i] = 0;
                }
                
                // 步骤3：傅里叶反变换(IFFT)
                Fourier.Inverse(dataReal, dataImg, FourierOptions.Default);
                
                // 显示倒谱结果（倒频率轴）
                easyChartXSectionSpectrum.Plot(dataReal, 0, 1.0 / sampleRate);
            }
            else
            {
                // === 功率谱分析 ===
                // 根据用户选择使用线性(dBv)或对数(dBV)单位
                if (checkBoxSpectrumDB.Checked)
                {
                    spectrumUnit = SpectrumUnits.dBV;
                }
                
                // 计算功率谱
                Spectrum.PowerSpectrum(waveformSection, sampleRate, ref waveformSectionSpectrum, out spectrumDeltaF, spectrumUnit);
                
                // 显示频谱
                easyChartXSectionSpectrum.Plot(waveformSectionSpectrum, 0, spectrumDeltaF);
            }
        }
        #endregion
        #region 事件处理 - 控件值变化
        /// <summary>
        /// 采样率值改变事件
        /// 重新显示时域波形
        /// </summary>
        private void numericUpDownSampleRate_ValueChanged(object sender, EventArgs e)
        {
            DisplayWaveform();
        }

        /// <summary>
        /// 窗口长度值改变事件
        /// 重新更新切割波形并分析
        /// </summary>
        private void numericUpDownWindowLength_ValueChanged(object sender, EventArgs e)
        {
            UpdateCutWaveform();
        }

        /// <summary>
        /// 频谱dB显示选项改变事件
        /// 重新计算并显示频谱
        /// </summary>
        private void checkBoxSpectrumDB_CheckedChanged(object sender, EventArgs e)
        {
            SectionAnalysis();
        }

        /// <summary>
        /// 倒谱分析选项改变事件
        /// 重新计算并显示倒谱或频谱
        /// </summary>
        private void checkBoxCepstrum_CheckedChanged(object sender, EventArgs e)
        {
            SectionAnalysis();
        }
        #endregion
        #region 事件处理 - JTFA分析
        /// <summary>
        /// JTFA(联合时频分析)按钮点击事件
        /// 执行时频分析并绘制结果热力图
        /// </summary>
        private void buttonJTFA_Click(object sender, EventArgs e)
        {
            JTFA();
            PlotJTFA();
        }
        
        /// <summary>
        /// 热力图颜色类型选择改变事件
        /// 使用新选择的颜色方案重新绘制JTFA结果
        /// </summary>
        private void comboBoxColorType_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlotJTFA();
        }
        #endregion
        #region JTFA分析实现
        /// <summary>
        /// 执行联合时频分析(JTFA)
        /// 
        /// 算法说明：
        /// JTFA使用短时傅里叶变换(STFT)来分析信号的频率成分随时间的变化
        /// 将信号分成多个重叠的时间窗口，对每个窗口执行FFT
        /// 
        /// 步骤：
        /// 1. 从完整波形中提取指定时间段的信号
        /// 2. 设置JTFA参数（采样率、窗口大小、窗函数类型）
        /// 3. 执行JTFA计算，得到时频谱矩阵
        /// 4. 将结果转换为dB单位并反转Y轴（时间从上到下）
        /// </summary>
        private void JTFA()
        {
            if (waveform == null || waveform.Length == 0)
            {
                MessageBox.Show("请先加载波形数据");
                return;
            }

            // 根据起始时间和持续时间提取波形段
            int startIndex = (int)((double)numericUpDownStartTime.Value * sampleRate);
            int length = (int)((double)numericUpDownDurationSec.Value * sampleRate);
            int windowLength = (int)numericUpDownJTFAWinLength.Value;
            
            // 验证数据有效性
            if ((waveform == null || waveform.Length < startIndex + length) || (length < windowLength))
            {
                MessageBox.Show("波形数据不足，无法执行JTFA分析");
                return;
            }
            
            // 提取用于JTFA分析的波形段
            double[] analysisWaveform = new double[length];
            Array.Copy(waveform, startIndex, analysisWaveform, 0, length);
            
            // 配置JTFA参数
            _task.SampleRate = sampleRate;                          // 采样率
            _task.FrequencyBins = windowLength;                     // FFT窗口大小（频率分辨率）
            _task.WindowType = (SeeSharpTools.JXI.SignalProcessing.Window.WindowType)Enum.Parse(
                typeof(SeeSharpTools.JXI.SignalProcessing.Window.WindowType), WindowTypes.Text);  // 窗函数类型
            _task.ColorTable = (GeneralJTFATask.ColorTableType)comboBoxColorType.SelectedIndex;  // 颜色表
            
            // 执行JTFA计算
            double[,] spec = null;
            _task.GetJTFA(analysisWaveform, ref spec);  // 获取时频谱矩阵
            
            // 将功率谱转换为dB单位
            int freqSize = (int)(spec.GetLength(1) * 0.8);  // 只保留有效频段（去掉高频部分）
            tfDistributionDB = new double[spec.GetLength(0), freqSize];
            
            for (int i = 0; i < spec.GetLength(0); i++)
            {
                for (int j = 0; j < freqSize; j++)
                {
                    // 转换为dB：10*log10(power)
                    // 反转时间轴，使最新时间在上面
                    tfDistributionDB[i, j] = 10 * Math.Log10(spec[spec.GetLength(0) - 1 - i, j]);
                }
            }
        }
        #endregion
        #region JTFA结果绘制
        /// <summary>
        /// 绘制联合时频分析结果热力图
        /// 
        /// 使用ScottPlot库绘制2D热力图：
        /// - X轴：频率 (Hz)
        /// - Y轴：时间 (s)
        /// - 颜色：功率谱密度 (dB)
        /// </summary>
        private void PlotJTFA()
        {
            if (tfDistributionDB == null || tfDistributionDB.Length == 0)
            {
                return;
            }

            // 设置图表标题
            formsScottPlot.Name = "Time-Frequency Chart";
            
            // 清除之前的绘图内容
            formsScottPlot.Plot.Clear();
            
            // 获取用户选择的热力图颜色方案
            ScottHeatMapColor selectedColor = (ScottHeatMapColor)comboBoxColorType.SelectedIndex;
            
            // 添加热力图（false表示不自动缩放）
            var hm = formsScottPlot.Plot.AddHeatmap(tfDistributionDB, GetScottColorMap(selectedColor), false);
            
            // 添加颜色条图例
            var cb = formsScottPlot.Plot.AddColorbar(hm);
            
            // 设置坐标轴标签
            formsScottPlot.Plot.XLabel("Frequency (Hz)");
            formsScottPlot.Plot.YLabel("Time (s)");
            
            // 设置坐标轴范围
            formsScottPlot.Plot.SetAxisLimitsX(0, _task.JTFAInfomation.df * tfDistributionDB.GetLength(1));  // 频率范围
            formsScottPlot.Plot.SetAxisLimitsY(0, _task.JTFAInfomation.dt * tfDistributionDB.GetLength(0));  // 时间范围
            
            // 设置热力图的坐标和插值方式
            hm.Interpolation = System.Drawing.Drawing2D.InterpolationMode.Bilinear;  // 双线性插值使图像更平滑
            hm.XMin = 0;
            hm.XMax = _task.JTFAInfomation.df * tfDistributionDB.GetLength(1);
            hm.YMin = 0;
            hm.YMax = _task.JTFAInfomation.dt * tfDistributionDB.GetLength(0);
            
            // 自动调整轴比例并移除边距
            formsScottPlot.Plot.AxisAuto(0, 0);
            formsScottPlot.Plot.Margins(0, 0);
            
            // 刷新显示
            formsScottPlot.Refresh();
        }
        #endregion
        #region 辅助方法 - 颜色映射
        /// <summary>
        /// 根据枚举值获取ScottPlot对应的颜色映射对象
        /// 
        /// 颜色映射用于将数值（dB）映射到颜色，
        /// 不同的颜色方案适用于不同的应用场景：
        /// - Turbo/Jet: 通用热力图，高对比度
        /// - Viridis/Plasma: 色盲友好，感知均匀
        /// - Grayscale: 黑白打印友好
        /// </summary>
        /// <param name="color">热力图颜色枚举</param>
        /// <returns>ScottPlot颜色映射对象</returns>
        public ScottPlot.Drawing.Colormap GetScottColorMap(ScottHeatMapColor color)
        {
            ScottPlot.Drawing.Colormap _scottColormap;
            
            switch (color) 
            {
                case ScottHeatMapColor.Algae:
                    _scottColormap = ScottPlot.Drawing.Colormap.Algae;
                    break;
                case ScottHeatMapColor.Amp:
                    _scottColormap = ScottPlot.Drawing.Colormap.Amp;
                    break;
                case ScottHeatMapColor.Balance:
                    _scottColormap = ScottPlot.Drawing.Colormap.Balance;
                    break;
                case ScottHeatMapColor.Blues:
                    _scottColormap = ScottPlot.Drawing.Colormap.Blues;
                    break;
                case ScottHeatMapColor.Curl:
                    _scottColormap = ScottPlot.Drawing.Colormap.Curl;
                    break;
                case ScottHeatMapColor.Deep:
                    _scottColormap = ScottPlot.Drawing.Colormap.Deep;
                    break;
                case ScottHeatMapColor.Delta:
                    _scottColormap = ScottPlot.Drawing.Colormap.Delta;
                    break;
                case ScottHeatMapColor.Dense:
                    _scottColormap = ScottPlot.Drawing.Colormap.Dense;
                    break;
                case ScottHeatMapColor.Diff:
                    _scottColormap = ScottPlot.Drawing.Colormap.Diff;
                    break;
                case ScottHeatMapColor.Grayscale:
                    _scottColormap = ScottPlot.Drawing.Colormap.Grayscale;
                    break;
                case ScottHeatMapColor.Greens:
                    _scottColormap = ScottPlot.Drawing.Colormap.Greens;
                    break;
                case ScottHeatMapColor.Haline:
                    _scottColormap = ScottPlot.Drawing.Colormap.Haline;
                    break;
                case ScottHeatMapColor.Ice:
                    _scottColormap = ScottPlot.Drawing.Colormap.Ice;
                    break;
                case ScottHeatMapColor.Inferno:
                    _scottColormap = ScottPlot.Drawing.Colormap.Inferno;
                    break;
                case ScottHeatMapColor.Jet:
                    _scottColormap = ScottPlot.Drawing.Colormap.Jet;
                    break;
                case ScottHeatMapColor.Magma:
                    _scottColormap = ScottPlot.Drawing.Colormap.Magma;
                    break;
                case ScottHeatMapColor.Matter:
                    _scottColormap = ScottPlot.Drawing.Colormap.Matter;
                    break;
                case ScottHeatMapColor.Oxy:
                    _scottColormap = ScottPlot.Drawing.Colormap.Oxy;
                    break;
                case ScottHeatMapColor.Phase:
                    _scottColormap = ScottPlot.Drawing.Colormap.Phase;
                    break;
                case ScottHeatMapColor.Plasma:
                    _scottColormap = ScottPlot.Drawing.Colormap.Plasma;
                    break;
                case ScottHeatMapColor.Rain:
                    _scottColormap = ScottPlot.Drawing.Colormap.Rain;
                    break;
                case ScottHeatMapColor.Solar:
                    _scottColormap = ScottPlot.Drawing.Colormap.Solar;
                    break;
                case ScottHeatMapColor.Speed:
                    _scottColormap = ScottPlot.Drawing.Colormap.Speed;
                    break;
                case ScottHeatMapColor.Tarn:
                    _scottColormap = ScottPlot.Drawing.Colormap.Tarn;
                    break;
                case ScottHeatMapColor.Tempo:
                    _scottColormap = ScottPlot.Drawing.Colormap.Tempo;
                    break;
                case ScottHeatMapColor.Thermal:
                    _scottColormap = ScottPlot.Drawing.Colormap.Thermal;
                    break;
                case ScottHeatMapColor.Topo:
                    _scottColormap = ScottPlot.Drawing.Colormap.Topo;
                    break;

                case ScottHeatMapColor.Turbid:
                    _scottColormap = ScottPlot.Drawing.Colormap.Turbid;
                    break;
                case ScottHeatMapColor.Turbo:
                    _scottColormap = ScottPlot.Drawing.Colormap.Turbo;
                    break;
                case ScottHeatMapColor.Viridis:
                    _scottColormap = ScottPlot.Drawing.Colormap.Viridis;
                    break;
                default:
                    _scottColormap = ScottPlot.Drawing.Colormap.Turbo;
                    break;
            }


            
            return _scottColormap;
        }
        #endregion

        #region 事件处理 - 峰值频率查找
        /// <summary>
        /// 打开峰值频率查找器对话框
        /// 
        /// 功能：
        /// - 将当前切割的波形段传递给频率查找器
        /// - 使用模态对话框显示频率分析结果
        /// - 支持Matrix Pencil算法的高精度频率检测
        /// </summary>
        private void buttonFindFrequencies_Click(object sender, EventArgs e)
        {
            // 验证切割波形数据有效
            if (waveformSection != null && waveformSection.Length > 0 && (double)numericUpDownSampleRate.Value > 0)
            {
                using (var peakFrequencyFinderForm = new PeakFrequencyFinderForm())
                {
                    // 将切割的信号数据传递给频率查找器
                    peakFrequencyFinderForm.signal = new double[waveformSection.Length];
                    Array.Copy(waveformSection, peakFrequencyFinderForm.signal, waveformSection.Length);
                    peakFrequencyFinderForm.sampleRate = (double)numericUpDownSampleRate.Value;
                    
                    // 更新频率查找器的时域波形显示
                    peakFrequencyFinderForm.UpdateTimeWaveform();
                    
                    // 以模态方式显示对话框，阻塞主窗体直到子窗体关闭
                    peakFrequencyFinderForm.ShowDialog(this);
                }
            }
            else
            {
                MessageBox.Show("请先加载波形数据并进行切割", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion
    }

    #region 枚举定义
    /// <summary>
    /// ScottPlot热力图颜色方案枚举
    /// 包含多种预定义的颜色映射，适用于不同的数据可视化需求
    /// </summary>
    public enum ScottHeatMapColor
    {
        Algae,      // 藻类色调（绿-蓝）
        Amp,        // 放大器色调
        Balance,    // 平衡色调（红-蓝发散）
        Blues,      // 蓝色渐变
        Curl,       // 卷曲色调
        Deep,       // 深色调
        Delta,      // 增量色调
        Dense,      // 密集色调
        Diff,       // 差异色调
        Grayscale,  // 灰度
        GrayscaleR, // 反向灰度
        Greens,     // 绿色渐变
        Haline,     // 盐度色调
        Ice,        // 冰色调（白-蓝）
        Inferno,    // 地狱色调（黑-红-黄）
        Jet,        // Jet色调（蓝-青-黄-红，经典MATLAB）
        Magma,      // 岩浆色调（黑-红-白）
        Matter,     // 物质色调
        Oxy,        // 氧气色调
        Phase,      // 相位色调（循环色）
        Plasma,     // 等离子色调（蓝-红-黄）
        Rain,       // 雨色调
        Solar,      // 太阳色调
        Speed,      // 速度色调
        Tarn,       // 污渍色调
        Tempo,      // 节拍色调
        Thermal,    // 热力色调（黑-红-黄-白）
        Topo,       // 地形色调
        Turbid,     // 浑浊色调
        Turbo,      // Turbo色调（改进的Jet，感知更均匀）
        Viridis,    // Viridis色调（紫-蓝-绿-黄，色盲友好）
    }
    #endregion
}
