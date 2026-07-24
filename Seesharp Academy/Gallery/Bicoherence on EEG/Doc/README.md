# EEG双谱分析工具 — 详细说明

## 技术栈
.NET 10.0 + C# + ScottPlot 5.0.54 + MathNet.Numerics 5.0.0 + WinForms

## 分析方法
| 指标 | 说明 |
|------|------|
| Bispectrum | B(f1,f2) = (1/K) sum Xk(f1)Xk(f2)Xk*(f1+f2) |
| Bicoherence | b^2(f1,f2) = |B|^2 / (E[|X(f1)X(f2)|^2] * E[|X(f1+f2)|^2]) |
| Biphase | arg B(f1,f2) |

FFT: N=512, Hanning窗, 50%重叠, 0.5 Hz分辨率, 采样率 256 Hz

## 实验设计
A-B-A: Baseline(前5min) -> Meditation -> Recovery(后5min)
分析通道: Fz/Cz/Pz (10-20系统中线)

## 运行
cd Source\cs\BicoherenceAnalyzer
dotnet run

cd Source\cs\BicoherenceViewer
dotnet run

## 数据
OpenNeuro ds001787 (BioSemi ActiveTwo, 64通道, 256 Hz)
