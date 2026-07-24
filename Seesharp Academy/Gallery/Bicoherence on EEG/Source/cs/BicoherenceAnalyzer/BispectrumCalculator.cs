using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace BicoherenceAnalyzer;

/// <summary>
/// 双谱（Bispectrum）和双相干性（Bicoherence）计算器
/// 
/// 参考论文: "Changes in Electroencephalographic Bicoherence During Sevoflurane Anesthesia"
/// 
/// 公式:
///   Bispectrum:  B(f₁, f₂) = E[X(f₁) · X(f₂) · X*(f₁+f₂)]
///   Bicoherence: b²(f₁, f₂) = |B(f₁, f₂)|² / (E[|X(f₁)X(f₂)|²] · E[|X(f₁+f₂)|²])
/// 
/// 其中 X(f) 是信号的傅里叶变换，E[] 表示对所有分段的平均
/// </summary>
public class BispectrumCalculator
{
    /// <summary>采样率 (Hz)</summary>
    public int SampleRate { get; }

    /// <summary>FFT点数（分段长度）</summary>
    public int Nfft { get; }

    /// <summary>重叠点数</summary>
    public int Noverlap { get; }

    /// <summary>最大频率 (Hz)，只计算 f1+f2 ≤ maxFreq 的区域</summary>
    public double MaxFrequency { get; }

    /// <summary>频率分辨率 (Hz)</summary>
    public double FreqResolution => (double)SampleRate / Nfft;

    /// <summary>最大频率对应的索引</summary>
    public int MaxFreqIndex => (int)(MaxFrequency / FreqResolution);

    /// <summary>频率数组（Hz）</summary>
    public double[] Frequencies { get; }

    /// <summary>汉宁窗系数（预计算）</summary>
    private readonly double[] _hanningWindow;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sampleRate">采样率 (Hz)</param>
    /// <param name="nfft">FFT点数（分段长度），默认512点</param>
    /// <param name="noverlap">重叠点数，默认256点（50%重叠）</param>
    /// <param name="maxFrequency">最大计算频率 (Hz)，默认45 Hz</param>
    public BispectrumCalculator(int sampleRate = 256, int nfft = 512, int noverlap = 256, double maxFrequency = 45.0)
    {
        SampleRate = sampleRate;
        Nfft = nfft;
        Noverlap = noverlap;
        MaxFrequency = maxFrequency;

        // 生成频率轴（只到 Nyquist 频率的一半以下，因为我们限制了 maxFreq）
        int nFreqBins = nfft / 2 + 1;
        double[] allFreqs = new double[nFreqBins];
        for (int i = 0; i < nFreqBins; i++)
            allFreqs[i] = i * FreqResolution;

        // 只保留到 maxFreqIndex
        Frequencies = allFreqs.Take(MaxFreqIndex + 1).ToArray();

        // 预计算汉宁窗
        _hanningWindow = new double[nfft];
        for (int i = 0; i < nfft; i++)
            _hanningWindow[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (nfft - 1)));
    }

    /// <summary>
    /// 计算双谱和双相干性
    /// </summary>
    /// <param name="signal">输入EEG信号（一维数组）</param>
    /// <returns>包含双谱矩阵和双相干性矩阵的结果</returns>
    public BispectrumResult Compute(double[] signal)
    {
        int nFreq = MaxFreqIndex + 1; // 频率轴长度

        if (signal.Length < Nfft)
            throw new ArgumentException($"信号长度 ({signal.Length}) 小于 FFT 长度 ({Nfft})，无法计算");

        // 分段的步长
        int step = Nfft - Noverlap;

        // 计算分段数量
        int numSegments = (signal.Length - Nfft) / step + 1;
        if (numSegments < 2)
            throw new ArgumentException($"信号太短，只有 {numSegments} 个分段（至少需要2个）");

        Console.WriteLine($"[双谱计算] 信号长度: {signal.Length}, 分段数: {numSegments}, 频率分辨率: {FreqResolution:F2} Hz, 频率点数: {nFreq}");

        // === 初始化累加器 ===
        // Bispectrum累加器：复数
        Complex[,] bispectrumAccum = new Complex[nFreq, nFreq];
        // 分母项1：E[|X(f1)X(f2)|²] 累加器
        double[,] powerProductAccum = new double[nFreq, nFreq];
        // 分母项2：E[|X(f1+f2)|²] 累加器（只依赖于和频率）
        double[] powerSumAccum = new double[nFreq]; // 索引对应 f1+f2 的和频率

        // 用于并行计算的锁
        object lockObj = new object();

        // === 逐段处理（并行） ===
        Parallel.For(0, numSegments, segIdx =>
        {
            int startSample = segIdx * step;
            double[] segment = new double[Nfft];
            Array.Copy(signal, startSample, segment, 0, Math.Min(Nfft, signal.Length - startSample));

            // 减去均值（去直流分量）
            double mean = segment.Average();
            for (int i = 0; i < Nfft; i++)
                segment[i] = (segment[i] - mean) * _hanningWindow[i];

            // 计算FFT
            Complex[] spectrum = new Complex[Nfft];
            for (int i = 0; i < Nfft; i++)
                spectrum[i] = new Complex(segment[i], 0);
            Fourier.Forward(spectrum, FourierOptions.Default);

            // 提取正频率部分（0到N/2），作为 Complex[]
            // 只保留到 MaxFreqIndex
            Complex[] X = new Complex[nFreq];
            double[] magSq = new double[nFreq];
            for (int i = 0; i < nFreq; i++)
            {
                X[i] = spectrum[i];
                magSq[i] = X[i].Real * X[i].Real + X[i].Imaginary * X[i].Imaginary; // |X[i]|²
            }

            // 计算本段的贡献
            Complex[,] localBispectrum = new Complex[nFreq, nFreq];
            double[,] localPowerProduct = new double[nFreq, nFreq];
            double[] localPowerSum = new double[nFreq];

            // 只计算 f1+f2 <= maxFreq 的区域（避免无效循环）
            for (int f1 = 0; f1 < nFreq; f1++)
            {
                for (int f2 = 0; f2 < nFreq; f2++)
                {
                    int fSum = f1 + f2;
                    if (fSum >= nFreq) break; // f1+f2 超出有效频率范围

                    // Bispectrum 累积: X(f1) * X(f2) * conj(X(f1+f2))
                    Complex prod = X[f1] * X[f2];
                    localBispectrum[f1, f2] = prod * Complex.Conjugate(X[fSum]);

                    // 分母项1: |X(f1) * X(f2)|²
                    localPowerProduct[f1, f2] = (prod.Real * prod.Real + prod.Imaginary * prod.Imaginary);

                    // 分母项2: |X(f1+f2)|²
                    localPowerSum[fSum] += magSq[fSum];
                }
            }

            // 线程安全地累加到全局累加器
            lock (lockObj)
            {
                for (int f1 = 0; f1 < nFreq; f1++)
                {
                    for (int f2 = 0; f2 < nFreq; f2++)
                    {
                        if (f1 + f2 >= nFreq) break;
                        bispectrumAccum[f1, f2] += localBispectrum[f1, f2];
                        powerProductAccum[f1, f2] += localPowerProduct[f1, f2];
                    }
                }
                for (int f = 0; f < nFreq; f++)
                    powerSumAccum[f] += localPowerSum[f];
            }
        });

        // === 计算平均值和双相干性 ===
        // Bispectrum: B(f1,f2) = average of segment bispectra
        Complex[,] bispectrum = new Complex[nFreq, nFreq];
        double[,] bicoherence = new double[nFreq, nFreq];
        double[,] bispectrumMagnitude = new double[nFreq, nFreq];

        double epsilon = 1e-15; // 避免除以零

        for (int f1 = 0; f1 < nFreq; f1++)
        {
            for (int f2 = 0; f2 < nFreq; f2++)
            {
                if (f1 + f2 >= nFreq) break;

                // 平均双谱
                bispectrum[f1, f2] = bispectrumAccum[f1, f2] / numSegments;

                // 双谱幅度
                double mag = bispectrum[f1, f2].Magnitude;
                bispectrumMagnitude[f1, f2] = mag;

                // 平均功率乘积
                double avgPowerProduct = powerProductAccum[f1, f2] / numSegments;
                // 平均功率和频率（注意 powerSumAccum[fSum] 被重复加了每个对 f1,f2 的次数）
                // 需要除以该频率出现的次数 = nFreq - fSum (所有使得 f1+f2=fSum 的(f1,f2)对)
                // 实际上我们只加了 nFreq - fSum 个对，所以：
                int fSum = f1 + f2;
                int countForSum = nFreq - fSum; // 使得 f1+f2=fSum 的有效对数
                double avgPowerSum = 0;
                if (countForSum > 0)
                    avgPowerSum = powerSumAccum[fSum] / (numSegments * countForSum);

                // 计算 Bicoherence
                // b²(f1,f2) = |B(f1,f2)|² / (E[|X(f1)X(f2)|²] * E[|X(f1+f2)|²])
                double denominator = avgPowerProduct * avgPowerSum;
                if (denominator > epsilon)
                {
                    bicoherence[f1, f2] = (mag * mag) / denominator;
                    // 限制在 [0, 1] 范围内
                    if (bicoherence[f1, f2] > 1.0) bicoherence[f1, f2] = 1.0;
                    if (bicoherence[f1, f2] < 0.0) bicoherence[f1, f2] = 0.0;
                }
            }
        }

        return new BispectrumResult
        {
            Bispectrum = bispectrum,
            BispectrumMagnitude = bispectrumMagnitude,
            Bicoherence = bicoherence,
            Frequencies = Frequencies,
            FreqResolution = FreqResolution,
            MaxFreqIndex = MaxFreqIndex,
            NumSegments = numSegments
        };
    }
}

/// <summary>
/// 双谱计算结果
/// </summary>
public class BispectrumResult
{
    /// <summary>复数双谱矩阵 B[f1, f2]</summary>
    public Complex[,] Bispectrum { get; init; } = new Complex[0, 0];

    /// <summary>双谱幅度矩阵 |B[f1, f2]|</summary>
    public double[,] BispectrumMagnitude { get; init; } = new double[0, 0];

    /// <summary>双相干性矩阵 b²[f1, f2]，取值范围 [0, 1]</summary>
    public double[,] Bicoherence { get; init; } = new double[0, 0];

    /// <summary>频率轴 (Hz)</summary>
    public double[] Frequencies { get; init; } = Array.Empty<double>();

    /// <summary>频率分辨率 (Hz)</summary>
    public double FreqResolution { get; init; }

    /// <summary>最大频率索引</summary>
    public int MaxFreqIndex { get; init; }

    /// <summary>分段数量</summary>
    public int NumSegments { get; init; }

    /// <summary>获取对角线 b²(f, f) 上的双相干性值</summary>
    public (double[] freqs, double[] values) GetDiagonalBicoherence()
    {
        int n = Math.Min(Frequencies.Length, MaxFreqIndex + 1);
        var freqs = new double[n];
        var values = new double[n];
        for (int i = 0; i < n; i++)
        {
            freqs[i] = Frequencies[i];
            // 对角线上的值：f1=f2=f
            if (i < Bicoherence.GetLength(0) && i < Bicoherence.GetLength(1))
                values[i] = Bicoherence[i, i];
        }
        return (freqs, values);
    }
}
