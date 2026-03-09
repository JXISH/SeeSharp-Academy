using SeeSharpTools.JY.DSP.Utility.Filter1D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seesharp.JY.DSP.SignalProcessing.Conditioning.Filter1D.EasyFilter
{
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
    public class EasyClassicFilter
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
        public EasyClassicFilter()
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
            , double PassBandFrequency, double CutoffFrequency, double PassBandFrequency2 = 0, double CutoffFrequency2 = 0)
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
            if (!settingsRational)
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
            if (_coef_a == null & _coef_b != null && _coef_b.Length > 0)
            {
                //FIR滤波
                return FIRFiltering.FIRFilter(_coef_b, InputSignal);
            }
            else
            {
                if (_coef_b != null && _coef_a.Length > 0 && _coef_b.Length > 0)
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
