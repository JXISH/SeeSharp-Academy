using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using JYUSB1202;
using SeeSharpTools.JY.DSP.Fundamental;

namespace FormExample
{
    public partial class MainForm : Form
    {
        // ---------- Acquisition state ----------
        private JYUSB1202AITask aiTask;
        private double[] readBuf;
        private double[] spectrumBuf;
        private bool acquiring = false;

        // ---------- Waterfall state ----------
        private Bitmap waterfallBmp;
        private int waterfallWidth;       // = displayed spectrum length
        private const int WaterfallHeight = 240;
        private const double WaterfallMaxDb = 0.0;          // 255 -> full scale (0 dBV)
        private const double WaterfallStepDb = 0.5;         // 1 LSB = 0.5 dB
        private const double WaterfallMinDb = WaterfallMaxDb - 255.0 * WaterfallStepDb; // -127.5 dBV
        private readonly Color[] thermalLut = new Color[256];

        // ---------- Spectrum display ratio ----------
        private const double SpecKeepRatio = 0.80;          // keep first 80% of spectrum bins

        public MainForm()
        {
            InitializeComponent();
            BuildThermalLut();
            // Defaults (already set in designer; enforce selection here)
            cmbRange.SelectedIndex = 3;          // 0.32 V (closest physical range to requested 0.3 V)
            cmbCoupling.SelectedIndex = 0;       // AC
            cmbTerminal.SelectedIndex = 0;       // PSEUDIFF
            cmbTheme.SelectedIndex = 0;          // Steel Blue
            btStop.Enabled = false;
            ApplyTheme(0);
            InitWaterfall((int)numSamples.Value);
        }

        // =============================================================
        //                       Start / Stop
        // =============================================================
        private void btStart_Click(object sender, EventArgs e)
        {
            if (acquiring) return;
            try
            {
                int samples = (int)numSamples.Value;
                int channel = (int)numChannel.Value;
                double sampleRate = (double)numSampleRate.Value;
                double range = double.Parse(cmbRange.Text);
                AICoupling coupling = (AICoupling)Enum.Parse(typeof(AICoupling), cmbCoupling.Text);
                AITerminal terminal = (AITerminal)Enum.Parse(typeof(AITerminal), cmbTerminal.Text);
                bool iepe = chkIEPE.Checked;

                aiTask = new JYUSB1202AITask(txtModule.Text);
                aiTask.AddChannel(channel, -range, range, terminal, coupling, iepe);

                aiTask.Mode = AIMode.Finite;
                aiTask.SampleRate = sampleRate;
                aiTask.SamplesToAcquire = samples;
                aiTask.Trigger.Type = AITriggerType.Immediate;

                JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);

                readBuf = new double[samples];
                spectrumBuf = new double[samples / 2];

                // Re-init waterfall to match the new spectrum length
                InitWaterfall(samples);

                aiTask.Start();
                acquiring = true;

                SetConfigEnabled(false);
                btStart.Enabled = false;
                btStop.Enabled = true;
                lblStatus.Text = "  Acquiring...";

                timerAcq.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start acquisition:\r\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SafeStopTask();
                acquiring = false;
                SetConfigEnabled(true);
                btStart.Enabled = true;
                btStop.Enabled = false;
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            timerAcq.Stop();
            SafeStopTask();
            acquiring = false;
            SetConfigEnabled(true);
            btStart.Enabled = true;
            btStop.Enabled = false;
            lblStatus.Text = "  Stopped";
        }

        // =============================================================
        //              100 ms Timer - finite acquisition cycle
        // =============================================================
        private void timerAcq_Tick(object sender, EventArgs e)
        {
            if (!acquiring || aiTask == null) return;
            timerAcq.Stop();
            try
            {
                // Finite mode: wait until the buffer holds the full block, then read it.
                if (aiTask.AvailableSamples >= (ulong)readBuf.Length)
                {
                    aiTask.ReadData(ref readBuf, readBuf.Length, -1);

                    double sampleRate = aiTask.SampleRate;
                    double dt = 1.0 / sampleRate;

                    // 1. Plot waveform
                    easyChartXWave.Plot(readBuf, 0.0, dt);

                    // 2. Spectrum analysis (Hanning window, dBV output)
                    double df;
                    Spectrum.PowerSpectrum(readBuf, sampleRate, ref spectrumBuf, out df,
                        SpectrumUnits.dBV, WindowType.Hanning, 0, false);

                    // 3. Keep first 80% of bins (ignore high-frequency tail)
                    int keep = (int)(spectrumBuf.Length * SpecKeepRatio);
                    if (keep < 2) keep = spectrumBuf.Length;
                    double[] specShown = new double[keep];
                    Array.Copy(spectrumBuf, specShown, keep);

                    easyChartXSpec.Plot(specShown, 0.0, df);

                    // 4. Update waterfall (one new row at top, scroll the rest down)
                    UpdateWaterfall(specShown);

                    // 5. Restart finite acquisition for the next 100 ms cycle
                    aiTask.Stop();
                    aiTask.Start();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "  " + ex.Message;
            }
            finally
            {
                if (acquiring) timerAcq.Start();
            }
        }

        // =============================================================
        //                       Waterfall
        // =============================================================
        private void InitWaterfall(int samples)
        {
            int specLen = samples / 2;
            int keep = (int)(specLen * SpecKeepRatio);
            if (keep < 2) keep = specLen;
            waterfallWidth = keep;

            if (waterfallBmp != null) waterfallBmp.Dispose();
            waterfallBmp = new Bitmap(waterfallWidth, WaterfallHeight, PixelFormat.Format24bppRgb);

            // Fill with background (black = lowest level)
            using (Graphics g = Graphics.FromImage(waterfallBmp))
                g.Clear(Color.Black);

            pictureBoxWaterfall.Image = waterfallBmp;
            pictureBoxWaterfall.Refresh();
        }

        private void UpdateWaterfall(double[] specDbv)
        {
            if (waterfallBmp == null) return;

            BitmapData bd = waterfallBmp.LockBits(
                new Rectangle(0, 0, waterfallBmp.Width, waterfallBmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                int w = waterfallBmp.Width;
                int h = waterfallBmp.Height;
                IntPtr scan0 = bd.Scan0;
                byte[] row = new byte[stride * h];
                System.Runtime.InteropServices.Marshal.Copy(scan0, row, 0, row.Length);

                // Shift everything down by one row (top row will be rewritten)
                Buffer.BlockCopy(row, 0, row, stride, stride * (h - 1));

                // Build the new top row from the spectrum values
                int copyLen = Math.Min(w, specDbv.Length);
                for (int x = 0; x < copyLen; x++)
                {
                    int idx = (int)Math.Round((specDbv[x] - WaterfallMinDb) / WaterfallStepDb);
                    if (idx < 0) idx = 0;
                    if (idx > 255) idx = 255;
                    Color c = thermalLut[idx];
                    int p = x * 3;
                    row[p + 0] = c.B;
                    row[p + 1] = c.G;
                    row[p + 2] = c.R;
                }
                // Pad any remaining width with the lowest color
                for (int x = copyLen; x < w; x++)
                {
                    Color c = thermalLut[0];
                    int p = x * 3;
                    row[p + 0] = c.B;
                    row[p + 1] = c.G;
                    row[p + 2] = c.R;
                }

                System.Runtime.InteropServices.Marshal.Copy(row, 0, scan0, row.Length);
            }
            finally
            {
                waterfallBmp.UnlockBits(bd);
            }
            pictureBoxWaterfall.Invalidate();
        }

        // Thermal palette: black -> dark red -> red -> bright yellow -> white
        // Anchors: 0=Black, 64=DarkRed(128,0,0), 128=Red(255,0,0),
        //          192=BrightYellow(255,255,0), 255=White(255,255,255)
        private void BuildThermalLut()
        {
            for (int i = 0; i < 256; i++)
            {
                int r, g, b;
                if (i < 64)            // 0..64   : (0,0,0) -> (128,0,0)
                {
                    double t = i / 64.0;
                    r = (int)(128 * t); g = 0; b = 0;
                }
                else if (i < 128)      // 64..128 : (128,0,0) -> (255,0,0)
                {
                    double t = (i - 64) / 64.0;
                    r = (int)(128 + 127 * t); g = 0; b = 0;
                }
                else if (i < 192)      // 128..192: (255,0,0) -> (255,255,0)
                {
                    double t = (i - 128) / 64.0;
                    r = 255; g = (int)(255 * t); b = 0;
                }
                else                   // 192..255: (255,255,0) -> (255,255,255)
                {
                    double t = (i - 192) / 63.0;
                    if (t > 1.0) t = 1.0;
                    r = 255; g = 255; b = (int)(255 * t);
                }
                thermalLut[i] = Color.FromArgb(r, g, b);
            }
        }

        // =============================================================
        //                          Themes
        // =============================================================
        private void cmbTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyTheme(cmbTheme.SelectedIndex);
        }

        private void ApplyTheme(int idx)
        {
            // Two industrial-flavor light palettes
            Color formBg, panelBg, accent, foreText, chartBg;
            if (idx == 1) // Warm Sand
            {
                formBg   = Color.FromArgb(245, 240, 230);
                panelBg  = Color.FromArgb(232, 222, 200);
                accent   = Color.FromArgb(140, 90, 50);
                foreText = Color.FromArgb(60, 45, 30);
                chartBg  = Color.FromArgb(252, 248, 240);
            }
            else // Steel Blue (default)
            {
                formBg   = Color.FromArgb(236, 240, 245);
                panelBg  = Color.FromArgb(214, 224, 236);
                accent   = Color.FromArgb(40, 80, 130);
                foreText = Color.FromArgb(30, 45, 70);
                chartBg  = Color.FromArgb(248, 250, 253);
            }

            this.BackColor = formBg;
            this.ForeColor = foreText;
            grpConfig.BackColor = panelBg;
            grpConfig.ForeColor = accent;
            lblStatus.BackColor = panelBg;
            lblStatus.ForeColor = foreText;
            lblWave.ForeColor = accent;
            lblSpec.ForeColor = accent;
            lblWaterfall.ForeColor = accent;

            try
            {
                easyChartXWave.ChartAreaBackColor = chartBg;
                easyChartXSpec.ChartAreaBackColor = chartBg;
                easyChartXWave.BackColor = panelBg;
                easyChartXSpec.BackColor = panelBg;
            }
            catch { }
        }

        // =============================================================
        //                          Helpers
        // =============================================================
        private void SetConfigEnabled(bool enabled)
        {
            txtModule.Enabled = enabled;
            numSampleRate.Enabled = enabled;
            numSamples.Enabled = enabled;
            numChannel.Enabled = enabled;
            cmbRange.Enabled = enabled;
            cmbCoupling.Enabled = enabled;
            cmbTerminal.Enabled = enabled;
            chkIEPE.Enabled = enabled;
        }

        private void SafeStopTask()
        {
            try
            {
                if (aiTask != null)
                {
                    aiTask.Stop();
                    aiTask.Channels.Clear();
                }
            }
            catch { }
            aiTask = null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            timerAcq.Stop();
            SafeStopTask();
            if (waterfallBmp != null) waterfallBmp.Dispose();
        }
    }
}
