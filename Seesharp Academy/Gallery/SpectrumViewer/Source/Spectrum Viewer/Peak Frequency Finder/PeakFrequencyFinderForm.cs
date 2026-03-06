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
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Peak_Frequency_Finder
{
    /// <summary>
    /// 频率分析程序
    /// 点击Load按钮，调用CSV Loader，从文件加载波形 signal和sampleRate，并显示到easyChartXTimeWaveform,调整numericUpDownStartFreq和numericUpDownStopFreq的值为0.1-0.4倍sampleRate
    /// 点击Analysis按钮，调用预处理对信号做带通滤波，对滤波输出做频谱分析，和MatrixPencil 频率检出，检出频率的幅度采用频谱对应频率的幅度，频谱和频点显示在easyChartXFrequencySpectrum,频谱和频点显示在easyChartXFrequencyPoints,频率和幅度列表显示在dgvResults
    /// </summary>
    public partial class PeakFrequencyFinderForm : Form
    {
        #region 公共属性
        //实数信号波形
        public double[] signal { get; set; }
        //采样率
        public double sampleRate { get; set; }
        //频率列表
        public List<double> frequencies { get; set; }
        #endregion

        #region 私有域

        #endregion

        /// <summary>
        /// 窗体构造函数
        /// </summary>
        public PeakFrequencyFinderForm()
        {
            InitializeComponent();
            signal = null;
            sampleRate = 0;
        }
        #region 内部方法
        private void InitStartStopFrequency()
        {
            //如果频率控制不合理，则重置
            if ((double)numericUpDownStopFreq.Value > sampleRate * 0.5)
            {
                numericUpDownStartFreq.Value = (decimal)(sampleRate * 0.1);
                numericUpDownStopFreq.Value = (decimal)(sampleRate * 0.4);
            }
        }
        #endregion
        #region 事件响应
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            //** 导入信号 **
            //启动对话窗体
            CSVLoaderForm csvLoader = new CSVLoaderForm();
            csvLoader.ShowDialog();
            //判断对话窗是否被取消
            if (csvLoader.DialogResult == DialogResult.Cancel)
            {
                return;
            }
            //获取数据
            signal = new double[csvLoader.LoadedSeries.Length];
            Array.Copy(csvLoader.LoadedSeries, signal, csvLoader.LoadedSeries.Length);
            sampleRate = csvLoader.SampleRate;

            //显示数据更新控件
            UpdateTimeWaveform();

        }


        private void buttonAnalysis_Click(object sender, EventArgs e)
        {
            //如果存在合理数据，进行分析
            if (signal != null && signal.Length > 0 && sampleRate > 0)
            {
                try
                {
                    #region 预处理降采样
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
                    #region 预处理滤波
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

                    //滤波
                    double[] filteredSignal = easyFilter.Filtering(resampledSignal);
                    #endregion
                    #region 分析
                    //频谱分析
                    double[] _freqAxis;
                    double[] _fftAmp;
                    (_freqAxis, _fftAmp) = ComputeFftSpectrum(filteredSignal, resampledRate);

                    //频率检出
                    var matrixPencilFreq = MatrixPencilEstimate(filteredSignal, resampledRate, passband1, passband2
                        , (double)numericUpDownRelativeThreshold.Value);
                    var rawComponents = BuildComponentsWithFftAmplitude(matrixPencilFreq, _freqAxis, _fftAmp, passband1, passband2);
                    List<DetectedComponent> _detected = SelectDisplayComponents(rawComponents, passband1, passband2
                        ,(int)numericUpDownMaxPeakNum.Value, (double)numericUpDownRelativeThreshold.Value);
                    //显示
                    //修正频率控件值为实际值
                    numericUpDownStartFreq.Value = (decimal)passband1;
                    numericUpDownStopFreq.Value = (decimal)passband2;
                    //显示列表
                    PopulateResultsGrid(_detected);
                    //显示频谱
                    double[][] spectrumDisplayX=new double[2][];
                    double[][] spectrumDisplayY=new double[2][];
                    spectrumDisplayX[0] = _freqAxis;
                    spectrumDisplayY[0] = _fftAmp;
                    //第二对显示_detected全部元素
                    spectrumDisplayX[1]=new double[_detected.Count];
                    spectrumDisplayY[1]=new double[_detected.Count];
                    for (int i = 0; i < _detected.Count; i++)
                    {
                        spectrumDisplayX[1][i] = _detected[i].FrequencyHz;
                        spectrumDisplayY[1][i] = _detected[i].Amplitude;
                    }
                    easyChartXSpectrum.Plot(spectrumDisplayX, spectrumDisplayY);
                    //设置第二条series为散点图
                    easyChartXSpectrum.Series[1].Type = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.Point;
                    easyChartXSpectrum.Series[1].Color = System.Drawing.Color.Red;
                    easyChartXSpectrum.AxisY.IsLogarithmic = checkBoxYAxisIsLog.Checked;
                    #endregion

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
        }
        private void PopulateResultsGrid(List<DetectedComponent> components)
        {
            dgvResults.Rows.Clear();
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                dgvResults.Rows.Add(i + 1, c.FrequencyHz.ToString("F2"), c.Amplitude.ToString("G6"));
            }

            Console.WriteLine("Frequency_Hz,Amplitude_FFT");
            foreach (var c in components)
            {
                Console.WriteLine($"{c.FrequencyHz.ToString("F4", CultureInfo.InvariantCulture)},{c.Amplitude.ToString("G8", CultureInfo.InvariantCulture)}");
            }
        }
        /// <summary>
        /// 不加窗的频谱，取得最佳频率分辨
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="fs"></param>
        /// <returns></returns>
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
        /// 矩阵铅笔估计频率
        /// </summary>
        /// <param name="x"></param>
        /// <param name="fs"></param>
        /// <param name="fMin"></param>
        /// <param name="fMax"></param>
        /// <param name="relativeThreshold">查找峰值的相对最大值的门限</param>
        /// <returns></returns>
        private static List<double> MatrixPencilEstimate(double[] x, double fs, double fMin, double fMax, double relativeThreshold=0.1)
        {
            var n = x.Length;
            var l = n / 3;
            if (l < 32)
            {
                l = n / 2;
            }

            l = Math.Min(Math.Max(l, 16), n - 2);
            var k = n - l;

            var y = DenseMatrix.Create(l, k, (r, c) => x[r + c]);
            var y1 = y.SubMatrix(0, l, 0, k - 1);
            var y2 = y.SubMatrix(0, l, 1, k - 1);

            var svd = y1.Svd(true);
            var s = svd.S.ToArray();
            if (s.Length == 0)
            {
                return new List<double>();
            }

            var threshold = s[0] * relativeThreshold;
            var m = s.Count(v => v > threshold);
            m = Math.Min(Math.Max(m, 8), Math.Min(80, Math.Min(y1.RowCount, y1.ColumnCount)));

            var u = svd.U.SubMatrix(0, svd.U.RowCount, 0, m);
            var vt = svd.VT.SubMatrix(0, m, 0, svd.VT.ColumnCount);
            var sigmaInv = DenseMatrix.CreateDiagonal(m, m, i => 1.0 / s[i]);

            var y1Pinv = vt.TransposeThisAndMultiply(sigmaInv).Multiply(u.Transpose());
            var a = y1Pinv.Multiply(y2);

            var evd = a.Evd();
            var freqList = new List<double>();
            foreach (var z in evd.EigenValues)
            {
                if (z.Magnitude < 1e-10)
                {
                    continue;
                }

                var w = Math.Atan2(z.Imaginary, z.Real);
                var f = w * fs / (2.0 * Math.PI);
                if (f < 0)
                {
                    f += fs;
                }

                if (f >= fMin && f <= fMax)
                {
                    freqList.Add(f);
                }
            }

            freqList.Sort();

            var merged = new List<double>();
            const double mergeHz = 1.0;
            foreach (var f in freqList)
            {
                if (merged.Count == 0 || Math.Abs(f - merged[merged.Count - 1]) > mergeHz)
                {
                    merged.Add(f);
                }
            }

            return merged;
        }

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

            return list
                .GroupBy(c => Math.Round(c.FrequencyHz, 1))
                .Select(g => g.OrderByDescending(x => x.Amplitude).First())
                .OrderBy(c => c.FrequencyHz)
                .ToList();
        }

        private sealed class DetectedComponent
        {
            public double FrequencyHz { get; set; }
            public double Amplitude { get; set; }
        }
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
        private static void AddDistinctByFrequency(ICollection<DetectedComponent> list, DetectedComponent component, double toleranceHz)
        {
            if (list.All(c => Math.Abs(c.FrequencyHz - component.FrequencyHz) > toleranceHz))
            {
                list.Add(component);
            }
        }
        #endregion
        public void UpdateTimeWaveform()
        {
            //更新时域波形
            easyChartXTimeWaveform.Plot(signal, 0, 1.0 / sampleRate);

            //初始化频率范围
            InitStartStopFrequency();
        }
        private void checkBoxYAxisIsLog_CheckedChanged(object sender, EventArgs e)
        {
            easyChartXSpectrum.AxisY.IsLogarithmic = checkBoxYAxisIsLog.Checked;
        }
    }

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

        public EasyFilter()
        {
            //滤波系数设空，如遇异常，输出空系数
            _coef_a = null;
            _coef_b = null;
        }
        /// <summary>
        /// 经典FIR滤波器设计，根据通带阻带参数估计阶数，设计滤波器
        /// </summary>
        /// <param name="BandType"></param>
        /// <param name="PassBandRipple_dB"></param>
        /// <param name="StopBandRejection_dB"></param>
        /// <param name="SampleRate"></param>
        /// <param name="PassBandFrequency"></param>
        /// <param name="CutoffFrequency"></param>
        /// <param name="PassBandFrequency2">带通、带阻设置</param>
        /// <param name="CutoffFrequency2">带通、带阻设置</param>
        /// <returns>估计的滤波阶数</returns>
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
        /// <summary>
        /// 经典IIR滤波器设计，根据通带阻带参数估计阶数，设计滤波器
        /// </summary>
        /// <param name="BandType"></param>
        /// <param name="PassBandRipple_dB"></param>
        /// <param name="StopBandRejection_dB"></param>
        /// <param name="SampleRate"></param>
        /// <param name="PassBandFrequency"></param>
        /// <param name="CutoffFrequency"></param>
        /// <param name="PassBandFrequency2">带通、带阻设置</param>
        /// <param name="CutoffFrequency2">带通、带阻设置</param>
        /// <returns>估计的滤波阶数</returns>
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
        /// <summary>
        /// 从滤波设计参数进行滤波器阶数估计
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
        /// <summary>
        /// 根据估计阶数、参数和窗函数，进行滤波器设计
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
        /// <summary>
        /// 实施滤波
        /// </summary>
        /// <param name="InputSignal"></param>
        /// <returns></returns>
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
        /// <summary>
        /// FIR设计方法
        /// </summary>
        public enum FIRDesignMethod
        {
            KaiserWindow,
            Equiripple
        }
    }
}
