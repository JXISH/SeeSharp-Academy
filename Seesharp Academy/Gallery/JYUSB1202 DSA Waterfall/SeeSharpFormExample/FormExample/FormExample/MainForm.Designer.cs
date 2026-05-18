namespace FormExample
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.grpConfig = new System.Windows.Forms.GroupBox();
            this.lblModule = new System.Windows.Forms.Label();
            this.txtModule = new System.Windows.Forms.TextBox();
            this.lblSampleRate = new System.Windows.Forms.Label();
            this.numSampleRate = new System.Windows.Forms.NumericUpDown();
            this.lblSamples = new System.Windows.Forms.Label();
            this.numSamples = new System.Windows.Forms.NumericUpDown();
            this.lblChannel = new System.Windows.Forms.Label();
            this.numChannel = new System.Windows.Forms.NumericUpDown();
            this.lblRange = new System.Windows.Forms.Label();
            this.cmbRange = new System.Windows.Forms.ComboBox();
            this.lblCoupling = new System.Windows.Forms.Label();
            this.cmbCoupling = new System.Windows.Forms.ComboBox();
            this.lblTerminal = new System.Windows.Forms.Label();
            this.cmbTerminal = new System.Windows.Forms.ComboBox();
            this.chkIEPE = new System.Windows.Forms.CheckBox();
            this.lblTheme = new System.Windows.Forms.Label();
            this.cmbTheme = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btStart = new SeeSharpTools.JY.GUI.EasyButton();
            this.btStop = new SeeSharpTools.JY.GUI.EasyButton();
            this.easyChartXWave = new SeeSharpTools.JY.GUI.EasyChartX();
            this.easyChartXSpec = new SeeSharpTools.JY.GUI.EasyChartX();
            this.pictureBoxWaterfall = new System.Windows.Forms.PictureBox();
            this.lblWave = new System.Windows.Forms.Label();
            this.lblSpec = new System.Windows.Forms.Label();
            this.lblWaterfall = new System.Windows.Forms.Label();
            this.timerAcq = new System.Windows.Forms.Timer(this.components);
            this.grpConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSampleRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSamples)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numChannel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWaterfall)).BeginInit();
            this.SuspendLayout();
            // 
            // grpConfig
            // 
            this.grpConfig.Controls.Add(this.lblModule);
            this.grpConfig.Controls.Add(this.txtModule);
            this.grpConfig.Controls.Add(this.lblSampleRate);
            this.grpConfig.Controls.Add(this.numSampleRate);
            this.grpConfig.Controls.Add(this.lblSamples);
            this.grpConfig.Controls.Add(this.numSamples);
            this.grpConfig.Controls.Add(this.lblChannel);
            this.grpConfig.Controls.Add(this.numChannel);
            this.grpConfig.Controls.Add(this.lblRange);
            this.grpConfig.Controls.Add(this.cmbRange);
            this.grpConfig.Controls.Add(this.lblCoupling);
            this.grpConfig.Controls.Add(this.cmbCoupling);
            this.grpConfig.Controls.Add(this.lblTerminal);
            this.grpConfig.Controls.Add(this.cmbTerminal);
            this.grpConfig.Controls.Add(this.chkIEPE);
            this.grpConfig.Controls.Add(this.lblTheme);
            this.grpConfig.Controls.Add(this.cmbTheme);
            this.grpConfig.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.grpConfig.Location = new System.Drawing.Point(10, 8);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(1060, 100);
            this.grpConfig.TabIndex = 0;
            this.grpConfig.TabStop = false;
            this.grpConfig.Text = "Acquisition Configuration";
            // 
            // lblModule
            // 
            this.lblModule.AutoSize = true;
            this.lblModule.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblModule.Location = new System.Drawing.Point(10, 24);
            this.lblModule.Name = "lblModule";
            this.lblModule.Size = new System.Drawing.Size(80, 15);
            this.lblModule.Text = "Module Name";
            // 
            // txtModule
            // 
            this.txtModule.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtModule.Location = new System.Drawing.Point(10, 42);
            this.txtModule.Name = "txtModule";
            this.txtModule.Size = new System.Drawing.Size(110, 23);
            this.txtModule.TabIndex = 0;
            this.txtModule.Text = "USBDev0";
            // 
            // lblSampleRate
            // 
            this.lblSampleRate.AutoSize = true;
            this.lblSampleRate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSampleRate.Location = new System.Drawing.Point(135, 24);
            this.lblSampleRate.Name = "lblSampleRate";
            this.lblSampleRate.Size = new System.Drawing.Size(95, 15);
            this.lblSampleRate.Text = "Sample Rate (Hz)";
            // 
            // numSampleRate
            // 
            this.numSampleRate.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numSampleRate.Location = new System.Drawing.Point(135, 42);
            this.numSampleRate.Maximum = new decimal(new int[] { 250000, 0, 0, 0 });
            this.numSampleRate.Minimum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numSampleRate.Name = "numSampleRate";
            this.numSampleRate.Size = new System.Drawing.Size(110, 23);
            this.numSampleRate.TabIndex = 1;
            this.numSampleRate.Value = new decimal(new int[] { 20480, 0, 0, 0 });
            // 
            // lblSamples
            // 
            this.lblSamples.AutoSize = true;
            this.lblSamples.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSamples.Location = new System.Drawing.Point(260, 24);
            this.lblSamples.Name = "lblSamples";
            this.lblSamples.Size = new System.Drawing.Size(75, 15);
            this.lblSamples.Text = "Samples / Acq";
            // 
            // numSamples
            // 
            this.numSamples.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numSamples.Location = new System.Drawing.Point(260, 42);
            this.numSamples.Maximum = new decimal(new int[] { 65536, 0, 0, 0 });
            this.numSamples.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            this.numSamples.Name = "numSamples";
            this.numSamples.Size = new System.Drawing.Size(95, 23);
            this.numSamples.TabIndex = 2;
            this.numSamples.Value = new decimal(new int[] { 1024, 0, 0, 0 });
            // 
            // lblChannel
            // 
            this.lblChannel.AutoSize = true;
            this.lblChannel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblChannel.Location = new System.Drawing.Point(370, 24);
            this.lblChannel.Name = "lblChannel";
            this.lblChannel.Size = new System.Drawing.Size(55, 15);
            this.lblChannel.Text = "Channel";
            // 
            // numChannel
            // 
            this.numChannel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.numChannel.Location = new System.Drawing.Point(370, 42);
            this.numChannel.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numChannel.Name = "numChannel";
            this.numChannel.Size = new System.Drawing.Size(60, 23);
            this.numChannel.TabIndex = 3;
            // 
            // lblRange
            // 
            this.lblRange.AutoSize = true;
            this.lblRange.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRange.Location = new System.Drawing.Point(445, 24);
            this.lblRange.Name = "lblRange";
            this.lblRange.Size = new System.Drawing.Size(70, 15);
            this.lblRange.Text = "Range (V)";
            // 
            // cmbRange
            // 
            this.cmbRange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRange.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbRange.Items.AddRange(new object[] { "12", "5", "1.25", "0.32" });
            this.cmbRange.Location = new System.Drawing.Point(445, 42);
            this.cmbRange.Name = "cmbRange";
            this.cmbRange.Size = new System.Drawing.Size(80, 23);
            this.cmbRange.TabIndex = 4;
            // 
            // lblCoupling
            // 
            this.lblCoupling.AutoSize = true;
            this.lblCoupling.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCoupling.Location = new System.Drawing.Point(540, 24);
            this.lblCoupling.Name = "lblCoupling";
            this.lblCoupling.Size = new System.Drawing.Size(60, 15);
            this.lblCoupling.Text = "Coupling";
            // 
            // cmbCoupling
            // 
            this.cmbCoupling.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCoupling.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbCoupling.Items.AddRange(new object[] { "AC", "DC" });
            this.cmbCoupling.Location = new System.Drawing.Point(540, 42);
            this.cmbCoupling.Name = "cmbCoupling";
            this.cmbCoupling.Size = new System.Drawing.Size(70, 23);
            this.cmbCoupling.TabIndex = 5;
            // 
            // lblTerminal
            // 
            this.lblTerminal.AutoSize = true;
            this.lblTerminal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTerminal.Location = new System.Drawing.Point(625, 24);
            this.lblTerminal.Name = "lblTerminal";
            this.lblTerminal.Size = new System.Drawing.Size(60, 15);
            this.lblTerminal.Text = "Terminal";
            // 
            // cmbTerminal
            // 
            this.cmbTerminal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTerminal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbTerminal.Items.AddRange(new object[] { "PSEUDIFF", "Differential" });
            this.cmbTerminal.Location = new System.Drawing.Point(625, 42);
            this.cmbTerminal.Name = "cmbTerminal";
            this.cmbTerminal.Size = new System.Drawing.Size(110, 23);
            this.cmbTerminal.TabIndex = 6;
            // 
            // chkIEPE
            // 
            this.chkIEPE.AutoSize = true;
            this.chkIEPE.Checked = true;
            this.chkIEPE.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIEPE.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkIEPE.Location = new System.Drawing.Point(750, 44);
            this.chkIEPE.Name = "chkIEPE";
            this.chkIEPE.Size = new System.Drawing.Size(95, 19);
            this.chkIEPE.TabIndex = 7;
            this.chkIEPE.Text = "IEPE Excite";
            // 
            // lblTheme
            // 
            this.lblTheme.AutoSize = true;
            this.lblTheme.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTheme.Location = new System.Drawing.Point(870, 24);
            this.lblTheme.Name = "lblTheme";
            this.lblTheme.Size = new System.Drawing.Size(45, 15);
            this.lblTheme.Text = "Theme";
            // 
            // cmbTheme
            // 
            this.cmbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTheme.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbTheme.Items.AddRange(new object[] { "Steel Blue", "Warm Sand" });
            this.cmbTheme.Location = new System.Drawing.Point(870, 42);
            this.cmbTheme.Name = "cmbTheme";
            this.cmbTheme.Size = new System.Drawing.Size(170, 23);
            this.cmbTheme.TabIndex = 8;
            this.cmbTheme.SelectedIndexChanged += new System.EventHandler(this.cmbTheme_SelectedIndexChanged);
            // 
            // lblStatus
            // 
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.Location = new System.Drawing.Point(10, 690);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(720, 24);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "  Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btStart
            // 
            this.btStart.BackgroundStyle = SeeSharpTools.JY.GUI.BackgroundStyle.Normal;
            this.btStart.BorderColor = System.Drawing.Color.Silver;
            this.btStart.BorderRadius = 12;
            this.btStart.BorderThickness = 1;
            this.btStart.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(180)))), ((int)(((byte)(205)))));
            this.btStart.ButtonColor2 = System.Drawing.Color.Silver;
            this.btStart.Location = new System.Drawing.Point(800, 685);
            this.btStart.Name = "btStart";
            this.btStart.Shadow = true;
            this.btStart.Size = new System.Drawing.Size(120, 32);
            this.btStart.TabIndex = 2;
            this.btStart.Text = "Start";
            this.btStart.Click += new System.EventHandler(this.btStart_Click);
            // 
            // btStop
            // 
            this.btStop.BackgroundStyle = SeeSharpTools.JY.GUI.BackgroundStyle.Normal;
            this.btStop.BorderColor = System.Drawing.Color.Silver;
            this.btStop.BorderRadius = 12;
            this.btStop.BorderThickness = 1;
            this.btStop.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(170)))), ((int)(((byte)(160)))));
            this.btStop.ButtonColor2 = System.Drawing.Color.Silver;
            this.btStop.Location = new System.Drawing.Point(940, 685);
            this.btStop.Name = "btStop";
            this.btStop.Shadow = true;
            this.btStop.Size = new System.Drawing.Size(120, 32);
            this.btStop.TabIndex = 3;
            this.btStop.Text = "Stop";
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // easyChartXWave
            // 
            this.easyChartXWave.AutoClear = true;
            this.easyChartXWave.AxisX.Title = "Time (s)";
            this.easyChartXWave.AxisY.Title = "Voltage (V)";
            this.easyChartXWave.LegendVisible = false;
            this.easyChartXWave.Location = new System.Drawing.Point(10, 140);
            this.easyChartXWave.Name = "easyChartXWave";
            this.easyChartXWave.Size = new System.Drawing.Size(525, 250);
            this.easyChartXWave.TabIndex = 4;
            // 
            // easyChartXSpec
            // 
            this.easyChartXSpec.AutoClear = true;
            this.easyChartXSpec.AxisX.Title = "Frequency (Hz)";
            this.easyChartXSpec.AxisY.Title = "Magnitude (dBV)";
            this.easyChartXSpec.LegendVisible = false;
            this.easyChartXSpec.Location = new System.Drawing.Point(545, 140);
            this.easyChartXSpec.Name = "easyChartXSpec";
            this.easyChartXSpec.Size = new System.Drawing.Size(525, 250);
            this.easyChartXSpec.TabIndex = 5;
            // 
            // pictureBoxWaterfall
            // 
            this.pictureBoxWaterfall.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxWaterfall.Location = new System.Drawing.Point(10, 420);
            this.pictureBoxWaterfall.Name = "pictureBoxWaterfall";
            this.pictureBoxWaterfall.Size = new System.Drawing.Size(1060, 250);
            this.pictureBoxWaterfall.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxWaterfall.TabIndex = 6;
            this.pictureBoxWaterfall.TabStop = false;
            // 
            // lblWave
            // 
            this.lblWave.AutoSize = true;
            this.lblWave.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblWave.Location = new System.Drawing.Point(12, 120);
            this.lblWave.Name = "lblWave";
            this.lblWave.Size = new System.Drawing.Size(110, 15);
            this.lblWave.Text = "Time-domain Waveform";
            // 
            // lblSpec
            // 
            this.lblSpec.AutoSize = true;
            this.lblSpec.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpec.Location = new System.Drawing.Point(547, 120);
            this.lblSpec.Name = "lblSpec";
            this.lblSpec.Size = new System.Drawing.Size(95, 15);
            this.lblSpec.Text = "Spectrum (dBV)";
            // 
            // lblWaterfall
            // 
            this.lblWaterfall.AutoSize = true;
            this.lblWaterfall.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblWaterfall.Location = new System.Drawing.Point(12, 400);
            this.lblWaterfall.Name = "lblWaterfall";
            this.lblWaterfall.Size = new System.Drawing.Size(120, 15);
            this.lblWaterfall.Text = "Spectrum Waterfall";
            // 
            // timerAcq
            // 
            this.timerAcq.Interval = 100;
            this.timerAcq.Tick += new System.EventHandler(this.timerAcq_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 728);
            this.Controls.Add(this.lblWaterfall);
            this.Controls.Add(this.lblSpec);
            this.Controls.Add(this.lblWave);
            this.Controls.Add(this.pictureBoxWaterfall);
            this.Controls.Add(this.easyChartXSpec);
            this.Controls.Add(this.easyChartXWave);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.btStart);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.grpConfig);
            this.Name = "MainForm";
            this.Text = "JYUSB1202 Spectrum & Waterfall Analyzer";
            this.grpConfig.ResumeLayout(false);
            this.grpConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSampleRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSamples)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numChannel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxWaterfall)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox grpConfig;
        private System.Windows.Forms.Label lblModule;
        private System.Windows.Forms.TextBox txtModule;
        private System.Windows.Forms.Label lblSampleRate;
        private System.Windows.Forms.NumericUpDown numSampleRate;
        private System.Windows.Forms.Label lblSamples;
        private System.Windows.Forms.NumericUpDown numSamples;
        private System.Windows.Forms.Label lblChannel;
        private System.Windows.Forms.NumericUpDown numChannel;
        private System.Windows.Forms.Label lblRange;
        private System.Windows.Forms.ComboBox cmbRange;
        private System.Windows.Forms.Label lblCoupling;
        private System.Windows.Forms.ComboBox cmbCoupling;
        private System.Windows.Forms.Label lblTerminal;
        private System.Windows.Forms.ComboBox cmbTerminal;
        private System.Windows.Forms.CheckBox chkIEPE;
        private System.Windows.Forms.Label lblTheme;
        private System.Windows.Forms.ComboBox cmbTheme;
        private System.Windows.Forms.Label lblStatus;
        private SeeSharpTools.JY.GUI.EasyButton btStart;
        private SeeSharpTools.JY.GUI.EasyButton btStop;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXWave;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXSpec;
        private System.Windows.Forms.PictureBox pictureBoxWaterfall;
        private System.Windows.Forms.Label lblWave;
        private System.Windows.Forms.Label lblSpec;
        private System.Windows.Forms.Label lblWaterfall;
        private System.Windows.Forms.Timer timerAcq;
    }
}
