using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Double;
using Seesharp.JY.DSP.SignalProcessing.Conditioning.Filter1D.EasyFilter;
using SeeSharpTools.JY.DSP.Utility.Filter1D;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Seesharp.JY.SignalProcessing.SuperResolution
{
    public class FrequencyFinder
    {
        /// <summary>
        /// 实际分析的起始频率
        /// </summary>
        public double Passband1 { get => passband1; }
        /// <summary>
        /// 实际分析的终止频率
        /// </summary>
        public double Passband2 { get => passband2; }
        /// <summary>
        /// 检测到的频率
        /// </summary>
        public List<DetectedComponent> Detected { get => _detected; }

        /// <summary>
        /// 频谱分析-输出:频率轴
        /// </summary>
        public double[] _freqAxis;
        /// <summary>
        /// 频谱分析-输出:频谱幅值
        /// </summary>
        public double[] _fftAmp;

        #region Private Fields
        BackgroundWorker worker = new BackgroundWorker();

        private double[] signal;
        private double sampleRate;

        double passband1;
        double passband2;
        double cutoffband1;
        double cutoffband2;

        double[] resampledSignal;
        double resampledRate;

        double[] filteredSignal;



        List<DetectedComponent> _detected;
        #endregion

        public FrequencyFinder(double passband2 = 0)
        {
            this.passband2 = passband2;
        }

        public void FindFrequencies(double[] inputSignal, double inputSampleRate, double Fstart, double Fstop,
            int maxFrequencies=10, double relativePeakThreshold=0.1, BackgroundWorker parentBGW = null)
        {
            worker = parentBGW;
            //报告初始进度
            worker?.ReportProgress(0, "开始分析...");
            signal = inputSignal;
            sampleRate = inputSampleRate;
            //1. 预处理降采样
            worker?.ReportProgress(5, "预处理降采样...");
            //允许通带单边定义
            double wantedFmax = Fstop;
            if(Fstart>=Fstop || Fstop <= 0)
                wantedFmax = 0.5 * inputSampleRate;
            DownSampling(wantedFmax); 
            //2. 预处理滤波
            worker?.ReportProgress(15, "滤波...");
            ConditioningFiltering(Fstart, Fstop);
            //3. 频谱分析与频率检测
            worker?.ReportProgress(20, "计算频谱...");
            SpectrumAnalysis();
            //4. 矩阵 pencil 频率检测
            worker?.ReportProgress(30, "Matrix Pencil频率检出...");
            //频率检出 - 使用Progress回调
            Progress<int> progress = new Progress<int>(percent =>
            {
                int overallProgress = 30 + (int)(percent * 0.5); // 30-80%用于Matrix Pencil
                worker?.ReportProgress(overallProgress, $"Matrix Pencil分析中... {percent}%");
            });

            var matrixPencilFreq = MatrixPencilEstimate(filteredSignal, resampledRate, passband1, passband2
                , relativePeakThreshold, progress);

            worker?.ReportProgress(85, "构建频率分量...");

            var rawComponents = BuildComponentsWithFftAmplitude(matrixPencilFreq, _freqAxis, _fftAmp, passband1, passband2);

            worker?.ReportProgress(90, "选择显示分量...");

            _detected = SelectDisplayComponents(rawComponents, passband1, passband2
                , maxFrequencies, relativePeakThreshold);


            worker?.ReportProgress(100, "分析完成！");
        }
        /// <summary>
        /// 降采样目的：
        /// - 降低数据量，减少计算时间
        /// - 提高频率分辨率（相对误差减小）
        /// - 需要先进行抗混叠低通滤波
        /// </summary>
        /// <param name="Fmax">最高感兴趣频率</param>
        public void DownSampling(double Fmax)
        {
            double maxFreq = Fmax;
            int downSampleRate = (int)Math.Floor(sampleRate / (maxFreq * 2.5));
            downSampleRate = Math.Max(1, downSampleRate);
            
            resampledRate = sampleRate / downSampleRate;
            if (downSampleRate > 1)
            {
                EasyFilter lpf = new EasyFilter();
                lpf.DesignIIRFilter(IIRDesignMethod.Elliptic, IIRBandType.Lowpass, 3, 70, 1, 0.4 / downSampleRate, 0.6 / downSampleRate);
                double[] lowpassFiltered = lpf.Filtering(signal);
                resampledSignal = new double[lowpassFiltered.Length / downSampleRate];
                for (int i = 0; i < resampledSignal.Length; i++)
                {
                    resampledSignal[i] = lowpassFiltered[(i + 1) * downSampleRate - 1];
                }
            }
            else
            {
                resampledSignal = signal;
                resampledRate = sampleRate;
            }
        }
        /// <summary>
        /// 滤波目的：
        /// - 去除目标频率范围外的噪声和干扰
        /// - 提高Matrix Pencil算法的检测精度
        /// - 根据用户设置自动选择滤波器类型（带通/高通/低通）
        /// </summary>
        /// <param name="Fstart"></param>
        /// <param name="Fstop"></param>
        /// <exception cref="Exception"></exception>
        public void ConditioningFiltering(double Fstart, double Fstop)
        {
            EasyFilter easyFilter = new EasyFilter();
            passband1 = Fstart;
            passband2 = Fstop;
            cutoffband1 = 0.01 * resampledRate;
            cutoffband2 = 0.49 * resampledRate;
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
                passband2 = 0.5 * resampledRate;
            passband2 = Math.Min(0.5 * resampledRate, passband2);
            if (passband2 >= 0.49 * resampledRate)
            {
                bandType = IIRBandType.Highpass;
            }
            cutoffband1 = Math.Max(0.001 * resampledRate, passband1 - 0.05 * resampledRate);
            cutoffband2 = Math.Min(0.49 * resampledRate, passband2 + 0.05 * resampledRate);

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
            filteredSignal = easyFilter.Filtering(resampledSignal);
        }

        public void SpectrumAnalysis()
        {
            (_freqAxis, _fftAmp) = ComputeFftSpectrum(filteredSignal, resampledRate);
        }

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
        private static List<double> MatrixPencilEstimate(double[] x, double fs, double fMin, double fMax
            , double relativeThreshold = 0.1, IProgress<int> progress=null)
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
        public class DetectedComponent
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

            if (targetFreq >= freq[freq.Length - 1])
            {
                return amp[amp.Length - 1];
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
            , int MaxDisplayComponents = 13, double RelativeComponentThreshold = 0.10)
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

    }

    
}
