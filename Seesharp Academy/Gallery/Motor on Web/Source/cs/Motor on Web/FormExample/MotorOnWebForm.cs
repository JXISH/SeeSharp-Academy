using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using JYUSB1202;
using Seesharp.JY.SignalProcessing.SuperResolution;
using SeeSharpTools.JY.DSP.Fundamental;
using StackExchange.Redis;

namespace FormExample
{
    public partial class MotorOnWebForm : Form
    {
        // DAQ related variables
        private JYUSB1202AITask aiTask = null;
        private bool isAcquiring = false;
        private int acquisitionCount = 0;
        
        //Frequency Finder
        private FrequencyFinder frequencyFinder = new FrequencyFinder();

        // Redis connection
        private ConnectionMultiplexer redis = null;
        private IDatabase db = null;
        
        public MotorOnWebForm()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            //// Wire up event handlers
            //btStart.Click += BtStart_Click;
            //btStop.Click += BtStop_Click;
            //timerMain.Tick += TimerMain_Tick;
            
            // Initialize Redis connection
            try
            {
                redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
                db = redis.GetDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to Redis: {ex.Message}\nMake sure Redis server is running.");
            }
        }

        private void BtStart_Click(object sender, EventArgs e)
        {
            try
            {
                StartAcquisition();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start acquisition: {ex.Message}");
                StopAcquisition();
            }
        }

        private void BtStop_Click(object sender, EventArgs e)
        {
            try
            {
                StopAcquisition();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop acquisition: {ex.Message}");
            }
        }

        private void StartAcquisition()
        {
            if (isAcquiring)
            {
                return;
            }

            try
            {
                // Get acquisition parameters
                string deviceName = textBoxDeviceName.Text;
                int channelIndex = (int)numericUpDownChannelIndex.Value;
                double sampleRate = (double)numericUpDownSampleRate.Value;
                int blockLength = (int)numericUpDownBlockLength.Value;

                // Create AI task
                aiTask = new JYUSB1202AITask(deviceName);
                
                // Add channel
                aiTask.AddChannel(channelIndex, -2.5, 2.5, AITerminal.PSEUDIFF, AICoupling.AC, true);
                
                // Configure acquisition mode
                aiTask.Mode = AIMode.Finite;
                aiTask.SampleRate = sampleRate;
                aiTask.SamplesToAcquire = blockLength;
                
                // Start acquisition
                aiTask.Start();
                
                isAcquiring = true;
                acquisitionCount = 0;

                // Disable configuration controls
                groupBoxMonitorSettings.Enabled = false;
                btStart.Enabled = false;

                // Start the timer
                double interval = (double)numericUpDownMonitorInterval.Value * 1000; // Convert to milliseconds
                timerMain.Interval = (int)interval;
                timerMain.Start();

                // Perform initial acquisition
                PerformAcquisition();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start acquisition: {ex.Message}");
                StopAcquisition();
            }
        }

        private void StopAcquisition()
        {
            if (!isAcquiring)
            {
                return;
            }

            try
            {
                // Stop the timer
                timerMain.Stop();

                // Stop DAQ task
                if (aiTask != null)
                {
                    aiTask.Stop();
                    aiTask = null;
                }

                isAcquiring = false;

                // Enable configuration controls
                groupBoxMonitorSettings.Enabled = true;
                btStart.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping acquisition: {ex.Message}");
            }
        }

        private void TimerMain_Tick(object sender, EventArgs e)
        {
            timerMain.Enabled = false;
            if (isAcquiring)
            {
                PerformAcquisition();
            }
            timerMain.Enabled = true;
        }

        private void PerformAcquisition()
        {
            try
            {
                // Get acquisition parameters
                double sampleRate = (double)numericUpDownSampleRate.Value;
                int blockLength = (int)numericUpDownBlockLength.Value;
                
                // Perform finite acquisition - read into double array
                double[] waveform = new double[blockLength];
                aiTask.ReadData(ref waveform, -1);
                
                // Restart task for next acquisition
                aiTask.Stop();
                aiTask.Start();
                
                double dt = 1.0 / sampleRate;

                // Display waveform
                easyChartXWaveform.Plot(waveform, 0, dt);

                // Calculate spectrum
                double[] spectrum;
                double df;
                CalculateSpectrum(waveform, sampleRate, out spectrum, out df);

                // Find peaks in spectrum
                frequencyFinder.FindFrequencies(waveform, sampleRate, 0.02 * sampleRate, 0.45 * sampleRate, 15, 0.05);

                // Display spectrum
                UpdateSpectrumChartX(spectrum, df, frequencyFinder);

                // Update results table
                UpdateResultsTable(frequencyFinder);

                // Report to Redis
                ReportToRedis(waveform, dt, spectrum, df, frequencyFinder);

                acquisitionCount++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Acquisition error: {ex.Message}");
                StopAcquisition();
            }
        }

        private void CalculateSpectrum(double[] waveform, double sampleRate, out double[] spectrum, out double df)
        {
            // Calculate power spectrum with Hanning window and dBV units
            SpectrumUnits unit = SpectrumUnits.dBV;
            WindowType windowType = WindowType.Hanning;
            bool asPSD = false;
            
            // Initialize spectrum array (PowerSpectrum needs ref parameter)
            spectrum = new double[waveform.Length / 2];
            Spectrum.PowerSpectrum(waveform, sampleRate, ref spectrum, out df, unit, windowType, 0.0, asPSD);
        }

        private void UpdateSpectrumChartX(double[] Spectrum, double dF, FrequencyFinder FreqFinder)
        {
            //显示频谱
            double[][] spectrumDisplayX = new double[2][];
            double[][] spectrumDisplayY = new double[2][];
            spectrumDisplayY[0] = Spectrum;
            spectrumDisplayX[0] = new double[Spectrum.Length];
            for (int i = 0; i < Spectrum.Length; i++)
            {
                spectrumDisplayX[0][i] = i * dF;
            }
            //第二对显示_detected全部元素
            spectrumDisplayX[1] = new double[FreqFinder.Detected.Count];
            spectrumDisplayY[1] = new double[FreqFinder.Detected.Count];
            for (int i = 0; i < FreqFinder.Detected.Count; i++)
            {
                spectrumDisplayX[1][i] = FreqFinder.Detected[i].FrequencyHz;
                //取频谱幅度显示检出频率
                int index = (int)Math.Round(FreqFinder.Detected[i].FrequencyHz / dF);
                spectrumDisplayY[1][i] = Spectrum[index];
            }
            easyChartXSpectrum.Plot(spectrumDisplayX, spectrumDisplayY);
            //设置第二条series为散点图
            easyChartXSpectrum.Series[1].Type = SeeSharpTools.JY.GUI.EasyChartXSeries.LineType.Point;
            easyChartXSpectrum.Series[1].Color = System.Drawing.Color.Red;
        }
        private void UpdateResultsTable(FrequencyFinder FreqFinder)
        {


            // Update DataGridView
            dgvResults.Rows.Clear();
            for (int i = 0; i < FreqFinder.Detected.Count; i++)
            {
                dgvResults.Rows.Add(
                    i + 1,
                    FreqFinder.Detected[i].FrequencyHz.ToString("F1"),
                    FreqFinder.Detected[i].Amplitude.ToString("0.000e0")
                );
            }
        }

        private void ReportToRedis(double[] waveform, double dt, double[] spectrum, double df, FrequencyFinder FreqFinder)
        {
            if (db == null)
            {
                return;
            }

            try
            {
                // Convert waveform to comma-separated string
                string waveformStr = string.Join(",", waveform.Select(v => v.ToString("F6")));
                
                // Convert spectrum to comma-separated string
                string spectrumStr = string.Join(",", spectrum.Select(v => v.ToString("F6")));

                // Generate frequency array for spectrum
                List<double> frequencies = new List<double>();
                for (int i = 0; i < spectrum.Length; i++)
                {
                    frequencies.Add(i * df);
                }
                //get frequency and amplitude from frequencyFinder
                string frequenciesStr = "";
                string amplitudesStr = "";
                for (int i = 0; i < FreqFinder.Detected.Count; i++)
                {
                    frequenciesStr += FreqFinder.Detected[i].FrequencyHz.ToString("F1") + ",";
                    amplitudesStr += FreqFinder.Detected[i].Amplitude.ToString("0.000e0") + ",";
                }
                //trim out last comma
                frequenciesStr = frequenciesStr.TrimEnd(',');
                amplitudesStr = amplitudesStr.TrimEnd(',');

                // Write to Redis
                db.StringSet("SeesharpMotor:TimeWaveform", waveformStr);
                db.StringSet("SeesharpMotor:Waveform_dT", dt.ToString("F9"));
                db.StringSet("SeesharpMotor:Spectrum", spectrumStr);
                db.StringSet("SeesharpMotor:Spectrum_dF", df.ToString("F9"));
                db.StringSet("SeesharpMotor:Frequencies", frequenciesStr);
                db.StringSet("SeesharpMotor:Amplitudes", amplitudesStr);
            }
            catch (Exception ex)
            {
                // Log but don't interrupt acquisition
                System.Diagnostics.Debug.WriteLine($"Redis write error: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop acquisition if running
            if (isAcquiring)
            {
                StopAcquisition();
            }

            // Dispose Redis connection
            if (redis != null)
            {
                redis.Close();
                redis.Dispose();
            }

            base.OnFormClosing(e);
        }
    }

    // Helper class for spectrum peaks
    internal class SpectrumPeak
    {
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
    }
}
