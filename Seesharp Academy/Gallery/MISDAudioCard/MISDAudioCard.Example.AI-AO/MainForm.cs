using System;
using System.Windows.Forms;
using MISDAudioCard;
using SeeSharpTools.JY.GUI;

namespace MISDAudioCard.Example.AI_AO
{
    /// <summary>
    /// Main Form for MISDAudioCard AI-AO Example
    /// Demonstrates audio input (AI) and output (AO) functionality
    /// </summary>
    public partial class MainForm : Form
    {
        #region Private Fields

        private AITask aiTask;
        private AOTask aoTask;
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
            
            toolStripStatusLabel.Text = "Ready - Click AI to acquire audio data";
        }

        /// <summary>
        /// AI button click - acquire dual-channel audio data
        /// </summary>
        private void buttonAI_Click(object sender, EventArgs e)
        {
            if (isRunning)
            {
                MessageBox.Show("Already running. Please wait for current operation to complete.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                isRunning = true;
                buttonAI.Enabled = false;
                buttonAO.Enabled = false;
                toolStripStatusLabel.Text = "Acquiring audio data...";

                int sampleRate = (int)numericUpDownSampleRate.Value;
                int samples = (int)numericUpDownSamples.Value;

                // Create and configure AI task for dual-channel acquisition
                aiTask = new AITask("");
                aiTask.SampleRate = sampleRate;
                aiTask.Mode = AIMode.Finite;
                aiTask.SamplesToAcquire = (uint)samples;

                // Add both left and right channels
                aiTask.AddChannel(0, -1.0, 1.0); // Left channel
                aiTask.AddChannel(1, -1.0, 1.0); // Right channel

                // Start acquisition
                aiTask.Start();

                toolStripStatusLabel.Text = "Recording in progress...";

                // Start timer to check completion
                timerCheck.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting AI acquisition: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                isRunning = false;
                buttonAI.Enabled = true;
                toolStripStatusLabel.Text = "Ready - Error occurred";
            }
        }

        /// <summary>
        /// AO button click - output previously acquired audio data
        /// </summary>
        private void buttonAO_Click(object sender, EventArgs e)
        {
            if (recordedData == null || recordedData.Length == 0)
            {
                MessageBox.Show("No audio data available. Please acquire data using AI button first.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isRunning)
            {
                MessageBox.Show("Already running. Please wait for current operation to complete.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                isRunning = true;
                buttonAI.Enabled = false;
                buttonAO.Enabled = false;
                toolStripStatusLabel.Text = "Playing audio data...";

                int sampleRate = (int)numericUpDownSampleRate.Value;
                int samples = recordedData.GetLength(0);
                int channels = recordedData.GetLength(1);

                // Create and configure AO task
                aoTask = new AOTask("");
                aoTask.SampleRate = sampleRate;
                aoTask.Mode = AOMode.Finite;
                aoTask.SamplesToUpdate = (uint)samples;

                // Add both channels
                aoTask.AddChannel(0); // Left channel
                aoTask.AddChannel(1); // Right channel

                // Prepare output data (interleave channels)
                double[] outputData = new double[samples * channels];
                for (int i = 0; i < samples; i++)
                {
                    outputData[i * 2] = recordedData[i, 0];     // Left
                    outputData[i * 2 + 1] = recordedData[i, 1]; // Right
                }

                aoTask.WriteData(outputData, -1);
                aoTask.Start();

                toolStripStatusLabel.Text = "Playback in progress...";

                // Start timer to check completion
                timerCheck.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting AO playback: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                isRunning = false;
                buttonAI.Enabled = true;
                buttonAO.Enabled = recordedData != null;
                toolStripStatusLabel.Text = "Ready - Error occurred";
            }
        }

        /// <summary>
        /// Timer to check if AI acquisition or AO playback is complete
        /// </summary>
        private void timerCheck_Tick(object sender, EventArgs e)
        {
            timerCheck.Enabled = false;

            try
            {
                // Check if AI acquisition is complete
                if (aiTask != null && aiTask.AvailableSamples >= aiTask.SamplesToAcquire)
                {
                    // Read recorded data
                    aiTask.ReadData(ref recordedData, (int)aiTask.SamplesToAcquire, -1);

                    // Plot recorded data on chart
                    PlotRecordedData();

                    // Stop AI task
                    aiTask.Stop();
                    aiTask.Channels.Clear();
                    aiTask = null;

                    isRunning = false;
                    buttonAI.Enabled = true;
                    buttonAO.Enabled = true;
                    toolStripStatusLabel.Text = "Acquisition completed - " + 
                        recordedData.GetLength(0) + " samples acquired";
                }
                // Check if AO playback is complete
                else if (aoTask != null)
                {
                    // Wait for AO to complete
                    bool completed = aoTask.WaitUntilDone(100);

                    if (completed)
                    {
                        aoTask.Stop();
                        aoTask.Channels.Clear();
                        aoTask = null;

                        isRunning = false;
                        buttonAI.Enabled = true;
                        buttonAO.Enabled = true;
                        toolStripStatusLabel.Text = "Playback completed successfully";
                    }
                    else
                    {
                        // Continue checking
                        timerCheck.Enabled = true;
                    }
                }
                else
                {
                    // Continue checking
                    timerCheck.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Cleanup
                if (aiTask != null)
                {
                    try { aiTask.Stop(); } catch { }
                    aiTask = null;
                }
                if (aoTask != null)
                {
                    try { aoTask.Stop(); } catch { }
                    aoTask = null;
                }
                
                isRunning = false;
                buttonAI.Enabled = true;
                buttonAO.Enabled = recordedData != null;
                toolStripStatusLabel.Text = "Ready - Error occurred";
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (aiTask != null)
                {
                    aiTask.Stop();
                }
                if (aoTask != null)
                {
                    aoTask.Stop();
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
        /// Plot recorded data on chart
        /// </summary>
        private void PlotRecordedData()
        {
            if (recordedData == null) return;

            int samples = recordedData.GetLength(0);
            int channels = recordedData.GetLength(1);

            // Plot dual-channel data
            double[,] channelData = new double[channels, samples];
            for (int i = 0; i < samples; i++)
            {
                for (int ch = 0; ch < channels; ch++)
                {
                    channelData[ch, i] = recordedData[i, ch];
                }
            }
            
            // Display on chart with channel labels
            easyChartX.Plot(channelData);
        }

        #endregion
    }
}
