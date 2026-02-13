using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSV_Loader;
using SeeSharpTools.JY.DSP.Fundamental;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Spectrum_Viewer
{
    public partial class SpectrumViewerForm : Form
    {
        //Data
        double[] waveform;
        double sampleRate;
        double[] waveformSection;
        double[] waveformSectionSpectrum;
        double spectrumDeltaF;

        public SpectrumViewerForm()
        {
            InitializeComponent();
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
        /// 跳转到JTFA界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJTFA_Click(object sender, EventArgs e)
        {

        }
    }
}
