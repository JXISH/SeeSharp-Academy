using CSV_Loader;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
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
    public partial class SpectrumViewerForm : Form
    {
        /// <summary>
        /// Global waveform Data
        /// </summary>
        double[] waveform;
        double sampleRate;
        /// <summary>
        /// 频谱分析截断波形
        /// </summary>
        double[] waveformSection;
        /// <summary>
        /// 截断波形的频谱
        /// </summary>
        double[] waveformSectionSpectrum;
        /// <summary>
        /// 频谱的频率步进
        /// </summary>
        double spectrumDeltaF;
        /// <summary>
        /// 联合时频分析类对象
        /// </summary>
        GeneralJTFATask _task;
        /// <summary>
        /// 时频图谱数据(dB)
        /// </summary>
        double[,] tfDistributionDB;
        public SpectrumViewerForm()
        {
            InitializeComponent();
            //初始化JTFA控件
            var windowTypes = Enum.GetNames(typeof(SeeSharpTools.JXI.SignalProcessing.Window.WindowType));
            foreach (var item in windowTypes)
            {
                WindowTypes.Items.Add(item);//Add window type
            }
            WindowTypes.SelectedIndex = 3;// window
            //清除comboBoxColorType
            comboBoxColorType.Items.Clear();
            var colorTypes = Enum.GetNames(typeof(ScottHeatMapColor));
            foreach (var item in colorTypes)
            {
                comboBoxColorType.Items.Add(item);//Add color type
            }
            comboBoxColorType.SelectedIndex = 29; //Turbo color
            _task = new GeneralJTFATask();
        }
        //从csv导入数据
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            //启动对话窗体
            CSVLoaderForm csvLoader = new CSVLoaderForm();
            csvLoader.ShowDialog();
            //判断对话窗是否被取消
            if (csvLoader.DialogResult == DialogResult.Cancel)
            {
                return;
            }
            //获取数据
            waveform = new double[csvLoader.LoadedSeries.Length];
            Array.Copy(csvLoader.LoadedSeries, waveform, csvLoader.LoadedSeries.Length);
            sampleRate = csvLoader.SampleRate;
            
            numericUpDownSampleRate.Value = (decimal)sampleRate;
            //更新控件显示波形
            DisplayWaveform();

            //文件名显示到窗体Title
            this.Text = "Spectrum Viewer: " + csvLoader.LoadedFileName;
        }
        //光标移动，切割数据
        private void easyChartXTimeWaveform_TabCursorChanged(object sender, SeeSharpTools.JY.GUI.TabCursorEventArgs e)
        {
            UpdateCutWaveform();
        }

        private void DisplayWaveform()
        {
            //显示数据
            easyChartXTimeWaveform.Plot(waveform, 0, 1.0 / sampleRate);
        }

        private void UpdateCutWaveform()
        {
            sampleRate = (double)numericUpDownSampleRate.Value;
            //更新波形切割
            double t0 = easyChartXTimeWaveform.TabCursors[0].XValue;
            int startIndex = (int)(t0 * sampleRate);
            int winLength = (int)numericUpDownWindowLength.Value;
            //根据波形大小合理化修订起点和长度
            if (startIndex < 0)
            {
                startIndex = 0;
            }
            //截取长度不超过原始数据
            if(winLength > waveform.Length)
                winLength = waveform.Length;
            
            //选择长出数据，则取最后一段
            if (startIndex + winLength > waveform.Length)
            {
                startIndex = waveform.Length - winLength;
            }
            //波形图光标置选择尾部
            easyChartXTimeWaveform.TabCursors[1].XValue = (startIndex + winLength)/ sampleRate;

            //拷贝数据
            waveformSection = new double[winLength];
            Array.Copy(waveform, startIndex, waveformSection, 0, winLength);
            //显示数据
            easyChartXWaveformSection.Plot(waveformSection, 0, 1.0 / sampleRate);
            //分析
            SectionAnalysis();
        }
        //截取波形分析
        private void SectionAnalysis()
        {
            sampleRate = (double)numericUpDownSampleRate.Value;
            //计算频谱
            waveformSectionSpectrum = new double[(waveformSection.Length + 1) / 2];
            SpectrumUnits spectrumUnit = SpectrumUnits.V2;
            if (checkBoxCepstrum.Checked)
            {
                //倒谱 y = real(ifft(log(abs(fft(x)))));
                //傅里叶变换
                double[] dataReal = new double[waveformSection.Length];
                Array.Copy(waveformSection, dataReal, waveformSection.Length);
                double[] dataImg = new double[waveformSection.Length];
                Fourier.Forward(dataReal, dataImg, FourierOptions.Default);
                //绝对值取对数  虚部归零
                for (int i = 0; i < dataReal.Length; i++)
                {
                    dataReal[i] = Math.Log(Math.Sqrt(dataReal[i]* dataReal[i]+ dataImg[i]* dataImg[i]));
                    dataImg[i] = 0;
                }
                //傅里叶反变换
                Fourier.Inverse(dataReal, dataImg, FourierOptions.Default);
                //显示倒谱
                easyChartXSectionSpectrum.Plot(dataReal, 0, 1.0 / sampleRate);
            }
            else
            {
                //频谱
                if (checkBoxSpectrumDB.Checked)
                    spectrumUnit = SpectrumUnits.dBV;
                Spectrum.PowerSpectrum(waveformSection, sampleRate, ref waveformSectionSpectrum, out spectrumDeltaF, spectrumUnit);
                //显示频谱
                easyChartXSectionSpectrum.Plot(waveformSectionSpectrum, 0, spectrumDeltaF);
            }
            
        }
        private void numericUpDownSampleRate_ValueChanged(object sender, EventArgs e)
        {
            DisplayWaveform();
        }

        private void numericUpDownWindowLength_ValueChanged(object sender, EventArgs e)
        {
            UpdateCutWaveform();
        }

        private void checkBoxSpectrumDB_CheckedChanged(object sender, EventArgs e)
        {
            SectionAnalysis();
        }

        private void checkBoxCepstrum_CheckedChanged(object sender, EventArgs e)
        {
            SectionAnalysis();
        }
        /// <summary>
        /// 实施JTFA
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJTFA_Click(object sender, EventArgs e)
        {
            JTFA();
            PlotJTFA();
        }
        /// <summary>
        /// 当色板选择变化，绘制JTFA
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxColorType_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlotJTFA();
        }
        private void JTFA()
        {
            //根据startTime和DurationSec从Waveform提取波形
            int startIndex= (int)((double)numericUpDownStartTime.Value * sampleRate);
            int length = (int)((double)numericUpDownDurationSec.Value * sampleRate);
            int windowLength = (int)numericUpDownJTFAWinLength.Value;
            if ((waveform == null || waveform.Length < startIndex + length) || (length < windowLength))
            {
                MessageBox.Show("波形数据不足");
                return;
            }
            double[] analysisWaveform = new double[length];
            Array.Copy(waveform, startIndex, analysisWaveform, 0, length);
            _task.SampleRate = sampleRate;//Sampling rate
            _task.FrequencyBins = windowLength;//Window size, is each FFT point
            _task.WindowType = (SeeSharpTools.JXI.SignalProcessing.Window.WindowType)Enum.Parse(
                typeof(SeeSharpTools.JXI.SignalProcessing.Window.WindowType), WindowTypes.Text);//Window type 
            _task.ColorTable = (GeneralJTFATask.ColorTableType)comboBoxColorType.SelectedIndex;
            //运算
            double[,] spec = null;
            _task.GetJTFA(analysisWaveform, ref spec);//Get JTFA
            //Convert to DB
            int freqSize = (int)(spec.GetLength(1)*0.8);
            tfDistributionDB = new double[spec.GetLength(0), freqSize];
            for (int i = 0; i < spec.GetLength(0); i++)
            {
                for (int j = 0; j < freqSize; j++)
                {
                    tfDistributionDB[i, j] = 10 * Math.Log10(spec[spec.GetLength(0)-1-i, j]);
                }
            }
        }
        private void PlotJTFA()
        {
            if (tfDistributionDB != null && tfDistributionDB.Length > 0)
            {
                //Get the intensity map
                //_task.ColorTable = (GeneralJTFATask.ColorTableType)comboBoxColorType.SelectedIndex;
                //Bitmap myImage = new Bitmap(tfDistributionDB.GetLength(1), tfDistributionDB.GetLength(0));
                //_task.GetImage(tfDistributionDB, ref myImage);
                //pictureBox_frequency_time.Image = myImage;

                formsScottPlot.Name = "Time-Frequency Chart";
                //清除formsScottPlot.
                formsScottPlot.Plot.Clear();
                //添加热力图
                //从comboBoxColorType中获取颜色
                ScottHeatMapColor selectedColor = (ScottHeatMapColor)comboBoxColorType.SelectedIndex;
                var hm = formsScottPlot.Plot.AddHeatmap(tfDistributionDB, GetScottColorMap(selectedColor), false);
                var cb = formsScottPlot.Plot.AddColorbar(hm);
                //调节X轴量化
                //formsScottPlot.Plot.AxisScale(_task.JTFAInfomation.df,_task.JTFAInfomation.dt);
                formsScottPlot.Plot.XLabel("Frequency (Hz)");
                formsScottPlot.Plot.YLabel("Time (s)");
                formsScottPlot.Plot.SetAxisLimitsX(0, _task.JTFAInfomation.df * tfDistributionDB.GetLength(1));
                formsScottPlot.Plot.SetAxisLimitsY(0, _task.JTFAInfomation.dt * tfDistributionDB.GetLength(0));
                // apply width and height to the heatmap
                hm.Interpolation =  System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                hm.XMin=0;
                hm.XMax=_task.JTFAInfomation.df * tfDistributionDB.GetLength(1);
                hm.YMin=0;
                hm.YMax=_task.JTFAInfomation.dt * tfDistributionDB.GetLength(0);
                formsScottPlot.Plot.AxisAuto(0, 0);
                formsScottPlot.Plot.Margins(0, 0);
                formsScottPlot.Refresh();
            }
        }
        public ScottPlot.Drawing.Colormap GetScottColorMap(ScottHeatMapColor color)
        {
            ScottPlot.Drawing.Colormap _scottColormap;
            switch (color) {
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
    }

    public enum ScottHeatMapColor
    {
        Algae,Amp,Balance,Blues,Curl,Deep,Delta,Dense,Diff,Grayscale, GrayscaleR,Greens,Haline,Ice,Inferno,Jet,Magma,Matter,Oxy,Phase,Plasma,Rain,Solar,Speed,Tarn,Tempo,Thermal,Topo,Turbid,Turbo,Viridis,      
    }
}
