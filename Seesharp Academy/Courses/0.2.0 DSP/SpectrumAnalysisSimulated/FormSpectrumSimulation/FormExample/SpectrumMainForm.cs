using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SeeSharpTools.JY.DSP.Fundamental;

namespace FormExample
{
    public partial class SpectrumMainForm : Form
    {
        public SpectrumMainForm()
        {
            InitializeComponent();
            InitFrontPanel();
        }

        private void InitFrontPanel()
        {
            //根据WindowType枚举类型初始化comboBoxWindowType选项
            foreach (WindowType windowType in Enum.GetValues(typeof(WindowType)))
            {
                comboBoxWindowType.Items.Add(windowType);
            }
            comboBoxWindowType.SelectedIndex = 1;
        }
        private void btAnalysis_Click(object sender, EventArgs e)
        {
            //读取面板控件
            double signalFrequency = (double)numericUpDownSignalFreq.Value;
            double sampleRate = (double)numericUpDownSampleRate.Value;
            int sampleLength = (int)numericUpDownSampleLength.Value;
            WindowType windowType = (WindowType)comboBoxWindowType.SelectedItem;
                       
            double[] waveform = new double[sampleLength];
            Generation.SineWave(ref waveform, 1, 90, signalFrequency, sampleRate);

            easyChartXWaveform.Plot(waveform);
            double[] spectrum = new double[(int)(sampleLength / 2) + 1];
            double df;

            Spectrum.PowerSpectrum(waveform, sampleRate,ref spectrum,out df,SpectrumUnits.dBV,windowType);

            easyChartXSpectrum.Plot(spectrum);
        }
    }
}
