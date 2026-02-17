using System;
using System.Windows.Forms;
using MISDAudioCard;
using SeeSharpTools.JY.GUI;

namespace MISDAudioCard.Example
{
    /// <summary>
    /// Main Form for MISDAudioCard Example
    /// Demonstrates audio output and input simultaneously
    /// </summary>
    public partial class MainForm : Form
    {
        #region Private Fields

        private AOTask aoTask;
        private AITask aiTask;
        private double[] generatedWaveform;
        private double[,] recordedData;
        private bool isRunning = false;

        #endregion

        #region Constructor

        public MainForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Initialize default values
            numericUpDownSampleRate.Value = 44100;
            numericUpDownSamples.Value = 44100; // 1 second at 44100 Hz
            numericUpDownFrequency.Value = 440; // A4 note
            numericUpDownAmplitude.Value = 0.5M; // 0.5 amplitude
        }

        /// <summary>
        /// Generate sine wave button click
        /// </summary>
        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                int sampleRate = (int)numericUpDownSampleRate.Value;
                int samples = (int)numericUpDownSamples.Value;
                double frequency = (double)numericUpDownFrequency.Value;
                double amplitude = (double)numericUpDownAmplitude.Value;

                // Generate sine wave
                generatedWaveform = new double[samples];
                for (int i = 0; i < samples; i++)
                {
                    double t = (double)i / sampleRate;
                    generatedWaveform[i] = amplitude * Math.Sin(2 * Math.PI * frequency * t);
                }

                // Plot on output chart
                PlotWaveform(chartOutput, generatedWaveform, "Generated Waveform");

                toolStripStatusLabel.Text = "Sine wave generated successfully";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating waveform: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Start button click - output waveform and record simultaneously
        /// </summary>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (generatedWaveform == null || generatedWaveform.Length == 0)
            {
                MessageBox.Show("Please generate a waveform first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isRunning)
            {
                MessageBox.Show("Already running.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                isRunning = true;
                buttonGenerate.Enabled = false;
                buttonStart.Enabled = false;
                buttonStop.Enabled = true;

                int sampleRate = (int)numericUpDownSampleRate.Value;
                int samples = (int)numericUpDownSamples.Value;

                // Get selected channels for output
                bool outputLeft = checkBoxOutputLeft.Checked;
                bool outputRight = checkBoxOutputRight.Checked;

                if (!outputLeft && !outputRight)
                {
                    throw new Exception("Please select at least one output channel.");
                }

                // Create and configure AO task
                aoTask = new AOTask("");
                aoTask.SampleRate = sampleRate;
                aoTask.Mode = AOMode.Finite;
                aoTask.SamplesToUpdate = (uint)samples;

                int outputChannels = 0;
                if (outputLeft)
                {
                    aoTask.AddChannel(0);
                    outputChannels++;
                }
                if (outputRight)
                {
                    aoTask.AddChannel(1);
                    outputChannels++;
                }

                // Prepare output data (interleave channels if stereo)
                double[] outputData;
                if (outputChannels == 2)
                {
                    // Stereo output - interleave channels
                    outputData = new double[samples * 2];
                    for (int i = 0; i < samples; i++)
                    {
                        outputData[i * 2] = generatedWaveform[i];     // Left
                        outputData[i * 2 + 1] = generatedWaveform[i]; // Right
                    }
                }
                else
                {
                    // Mono output
                    outputData = generatedWaveform;
                }

                aoTask.WriteData(outputData, -1);
                aoTask.Start();

                // Get selected channels for input
                bool inputLeft = checkBoxInputLeft.Checked;
                bool inputRight = checkBoxInputRight.Checked;

                if (!inputLeft && !inputRight)
                {
                    throw new Exception("Please select at least one input channel.");
                }

                // Create and configure AI task
                aiTask = new AITask("");
                aiTask.SampleRate = sampleRate;
                aiTask.Mode = AIMode.Finite;
                aiTask.SamplesToAcquire = (uint)samples;

                if (inputLeft)
                {
                    aiTask.AddChannel(0, -1.0, 1.0);
                }
                if (inputRight)
                {
                    aiTask.AddChannel(1, -1.0, 1.0);
                }

                aiTask.Start();

                toolStripStatusLabel.Text = "Playing and recording...";

                // Start timer to check completion
                timerCheck.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                isRunning = false;
                buttonGenerate.Enabled = true;
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
            }
        }

        /// <summary>
        /// Stop button click
        /// </summary>
        private void buttonStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (aoTask != null)
                {
                    aoTask.Stop();
                }
                if (aiTask != null)
                {
                    aiTask.Stop();
                }

                timerCheck.Enabled = false;
                isRunning = false;
                buttonGenerate.Enabled = true;
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
                toolStripStatusLabel.Text = "Stopped";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error stopping: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Timer to check if playback/recording is complete
        /// </summary>
        private void timerCheck_Tick(object sender, EventArgs e)
        {
            timerCheck.Enabled = false;

            try
            {
                // Check if recording is complete
                if (aiTask.AvailableSamples >= aiTask.SamplesToAcquire)
                {
                    // Read recorded data
                    aiTask.ReadData(ref recordedData, (int)aiTask.SamplesToAcquire, -1);

                    // Plot recorded data
                    PlotRecordedData();

                    // Wait for AO to complete
                    aoTask.WaitUntilDone(1000);

                    // Stop both tasks
                    if (aoTask != null)
                    {
                        aoTask.Stop();
                    }
                    if (aiTask != null)
                    {
                        aiTask.Stop();
                    }

                    // Clear channels
                    aoTask.Channels.Clear();
                    aiTask.Channels.Clear();

                    isRunning = false;
                    buttonGenerate.Enabled = true;
                    buttonStart.Enabled = true;
                    buttonStop.Enabled = false;
                    toolStripStatusLabel.Text = "Playback and recording completed";
                }
                else
                {
                    timerCheck.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                isRunning = false;
                buttonGenerate.Enabled = true;
                buttonStart.Enabled = true;
                buttonStop.Enabled = false;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (aoTask != null)
                {
                    aoTask.Stop();
                }
                if (aiTask != null)
                {
                    aiTask.Stop();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Plot waveform on chart
        /// </summary>
        private void PlotWaveform(EasyChartX chart, double[] data, string title)
        {
            chart.Plot(data);
        }

        /// <summary>
        /// Plot recorded data on input chart
        /// </summary>
        private void PlotRecordedData()
        {
            if (recordedData == null) return;

            int samples = recordedData.GetLength(0);
            int channels = recordedData.GetLength(1);

            // Plot each channel
            if (channels == 1)
            {
                double[] channelData = new double[samples];
                for (int i = 0; i < samples; i++)
                {
                    channelData[i] = recordedData[i, 0];
                }
                chartInput.Plot(channelData);
            }
            else
            {
                double[,] channelData = new double[channels, samples];
                for (int i = 0; i < samples; i++)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        channelData[ch, i] = recordedData[i, ch];
                    }
                }
                chartInput.Plot(channelData);
            }
        }

        #endregion
    }
}
