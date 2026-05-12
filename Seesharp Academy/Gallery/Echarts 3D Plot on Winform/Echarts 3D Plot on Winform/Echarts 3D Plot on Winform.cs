using System;
using System.Windows.Forms;
using WebView2Plots;
using SeeSharpTools.JY.File;
using SeeSharpTools.JY.DSP.Fundamental;
using SeeSharpTools.JY.DSP.JTFA;
using System.Threading.Tasks;

namespace Echarts_3D_Plot_on_Winform
{
    public partial class FormEcharts3DPlot : Form
    {
        public FormEcharts3DPlot()
        {
            InitializeComponent();
        }

        private async void buttonShow3DSurface_Click(object sender, EventArgs e)
        {
            // X axis parameters
            double x0 = 0;
            double xStep = 0.5;
            int xCount = 40;

            // Y axis parameters
            double y0 = 0;
            double yStep = 0.5;
            int yCount = 40;

            // Generate demo surface data
            double[,] surfaceData = new double[xCount, yCount];
            Random rn = new Random();
            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    double x = x0 + i * xStep;
                    double y = y0 + j * yStep;
                    double r = Math.Sqrt(x * x + y * y);
                    surfaceData[i, j] = Math.Sin(r) * 10.0 / (r + 1.0) + rn.Next(100) / 500.0;
                }
            }

            // Call static class to render
            await WebView2ThreeDSurface.RenderAsync(webView23DSurface, surfaceData,
                x0, xStep, y0, yStep, "X", "Y", "Z");
        }

        private async void buttonDisplay3DBar_Click(object sender, EventArgs e)
        {
            // X axis parameters
            double x0 = -5.0;
            double xStep = 0.5;
            int xCount = 20;

            // Y axis parameters
            double y0 = -5.0;
            double yStep = 0.5;
            int yCount = 20;

            // Generate center-symmetric Gaussian + noise data
            Random rand = new Random(42);
            double[,] barData = new double[xCount, yCount];
            double centerX = x0 + (xCount - 1) * xStep / 2.0;
            double centerY = y0 + (yCount - 1) * yStep / 2.0;
            double sigma = 2.0;

            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    double x = x0 + i * xStep;
                    double y = y0 + j * yStep;
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double gaussian = Math.Exp(-(dx * dx + dy * dy) / (2.0 * sigma * sigma));
                    double noise = (rand.NextDouble() - 0.5) * 0.2;
                    barData[i, j] = gaussian + noise;
                }
            }

            // Get transparency percentage from numericUpDown
            int transparencyPercent = (int)numericUpDownTransparency.Value;

            // Call static class to render
            await WebView2Bar3D.RenderAsync(webView23DBar, barData,
                x0, xStep, y0, yStep, "X", "Y", "Z", transparencyPercent);
        }

        private async void buttonShowWaterfall_Click(object sender, EventArgs e)
        {
            //生成一个线性调频的方波
            int sampleRate = 44100;
            int sampleLength = 100000;
            double f0 = 100;
            double fc = 3000;
            double[] signal = new double[sampleLength];
            double t = 0;
            double dt = 1.0 / sampleRate;
            double w0 = 2 * Math.PI * f0;
            double wc = 2 * Math.PI * fc;
            double[] noise = new double[sampleLength];

            NotchBandpassFilter filter = new NotchBandpassFilter();
            Generation.GaussianNiose(noise, 0.2);
            bool dBOn = checkBoxDB.Checked;
            bool filterOn = checkBoxFilter.Checked;
            for (int i = 0; i < sampleLength; i++)
            {
                double p = w0 * Math.Exp(t / 0.8) * t;
                double o1 = Math.Sin(p);
                signal[i] = o1 + 0.5 * Math.Sin(2 * p) + 0.5 * Math.Sin(5 * p) + 0.25 * Math.Sin(wc * t)
                    + noise[i];
                if(filterOn)
                    signal[i] = filter.Process(signal[i]) + signal[i]; //带通滤波叠加，变化小一些
                t += dt;
            }

            //时频联合分析
            double[,] timeFreqSpectrogram=null;
            GeneralJTFATask task = new GeneralJTFATask();
            task.SampleRate = sampleRate;
            task.WindowType = WindowType.Flat_Top;
            task.GetJTFA(signal, ref timeFreqSpectrogram);

            //transpose for display
            double[,] waterfallData = new double[timeFreqSpectrogram.GetLength(1), timeFreqSpectrogram.GetLength(0)];
            for (int i = 0; i < timeFreqSpectrogram.GetLength(0); i++)
            {
                for (int j = 0; j < timeFreqSpectrogram.GetLength(1); j++)
                {
                    if(dBOn)
                        waterfallData[j, i] = Math.Max(-100, 20* Math.Log10(timeFreqSpectrogram[i, j]));
                    else
                        waterfallData[j, i] = timeFreqSpectrogram[i, j];
                }
            }
            //显示瀑布图
            await WebView2ThreeDSurface.RenderAsync(webView23DWaterfall, waterfallData,
               task.JTFAInfomation.f0, task.JTFAInfomation.df, 0, task.JTFAInfomation.dt,"Frequency", "Time", "Magnitude", ThreeDSurfaceType.Waterfall);
        }

        /// <summary>
        /// 极简 IIR 陷波（带通）滤波器
        /// 低选择性 | 中心频率 0.2 采样率 | 二阶直接I型
        /// </summary>
        public class NotchBandpassFilter
        {
            // 滤波系数（低选择性 0.2Fs 中心频率）
            private readonly double _b0 = 0.05817, _b1 = 0, _b2 = -0.05817;
            private readonly double _a1 = -0.5258, _a2 = 0.8837;

            // 历史状态（极简实现）
            private double _x1, _x2, _y1, _y2;

            public NotchBandpassFilter()
            {
                Reset();
            }
            /// <summary>
            /// 单样本滤波（实时/流式处理）
            /// </summary>
            public double Process(double input)
            {
                // IIR 二阶滤波核心公式
                double output = _b0 * input + _b1 * _x1 + _b2 * _x2
                             - _a1 * _y1 - _a2 * _y2;

                // 更新历史状态
                _x2 = _x1;
                _x1 = input;
                _y2 = _y1;
                _y1 = output;

                return output;
            }

            /// <summary>
            /// 重置滤波器状态
            /// </summary>
            public void Reset() => _x1 = _x2 = _y1 = _y2 = 0;
        }
    }
}
