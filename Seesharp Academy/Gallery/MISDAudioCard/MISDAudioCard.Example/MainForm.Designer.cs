namespace MISDAudioCard.Example
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxConfiguration = new System.Windows.Forms.GroupBox();
            this.numericUpDownAmplitude = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numericUpDownFrequency = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDownSamples = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numericUpDownSampleRate = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBoxChannels = new System.Windows.Forms.GroupBox();
            this.checkBoxInputRight = new System.Windows.Forms.CheckBox();
            this.checkBoxInputLeft = new System.Windows.Forms.CheckBox();
            this.checkBoxOutputRight = new System.Windows.Forms.CheckBox();
            this.checkBoxOutputLeft = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonGenerate = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.chartOutput = new SeeSharpTools.JY.GUI.EasyChartX();
            this.chartInput = new SeeSharpTools.JY.GUI.EasyChartX();
            this.timerCheck = new System.Windows.Forms.Timer();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.groupBoxConfiguration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAmplitude)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFrequency)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSamples)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).BeginInit();
            this.groupBoxChannels.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxConfiguration
            // 
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownAmplitude);
            this.numericUpDownAmplitude.DecimalPlaces = 2;
            this.numericUpDownAmplitude.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericUpDownAmplitude.Location = new System.Drawing.Point(120, 125);
            this.numericUpDownAmplitude.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownAmplitude.Name = "numericUpDownAmplitude";
            this.numericUpDownAmplitude.Size = new System.Drawing.Size(140, 20);
            this.numericUpDownAmplitude.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 127);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(60, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Amplitude:";
            // 
            // numericUpDownFrequency
            // 
            this.numericUpDownFrequency.Location = new System.Drawing.Point(120, 95);
            this.numericUpDownFrequency.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.numericUpDownFrequency.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownFrequency.Name = "numericUpDownFrequency";
            this.numericUpDownFrequency.Size = new System.Drawing.Size(140, 20);
            this.numericUpDownFrequency.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Frequency:";
            // 
            // numericUpDownSamples
            // 
            this.numericUpDownSamples.Location = new System.Drawing.Point(120, 65);
            this.numericUpDownSamples.Maximum = new decimal(new int[] {
            441000,
            0,
            0,
            0});
            this.numericUpDownSamples.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownSamples.Name = "numericUpDownSamples";
            this.numericUpDownSamples.Size = new System.Drawing.Size(140, 20);
            this.numericUpDownSamples.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Samples (Total):";
            // 
            // numericUpDownSampleRate
            // 
            this.numericUpDownSampleRate.Location = new System.Drawing.Point(120, 35);
            this.numericUpDownSampleRate.Maximum = new decimal(new int[] {
            48000,
            0,
            0,
            0});
            this.numericUpDownSampleRate.Minimum = new decimal(new int[] {
            8000,
            0,
            0,
            0});
            this.numericUpDownSampleRate.Name = "numericUpDownSampleRate";
            this.numericUpDownSampleRate.Size = new System.Drawing.Size(140, 20);
            this.numericUpDownSampleRate.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Sample Rate:";
            // 
            // groupBoxConfiguration
            // 
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownAmplitude);
            this.groupBoxConfiguration.Controls.Add(this.label5);
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownFrequency);
            this.groupBoxConfiguration.Controls.Add(this.label4);
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownSamples);
            this.groupBoxConfiguration.Controls.Add(this.label3);
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownSampleRate);
            this.groupBoxConfiguration.Controls.Add(this.label2);
            this.groupBoxConfiguration.Location = new System.Drawing.Point(12, 12);
            this.groupBoxConfiguration.Name = "groupBoxConfiguration";
            this.groupBoxConfiguration.Size = new System.Drawing.Size(280, 180);
            this.groupBoxConfiguration.TabIndex = 0;
            this.groupBoxConfiguration.TabStop = false;
            this.groupBoxConfiguration.Text = "Waveform Configuration";
            // 
            // groupBoxChannels
            // 
            this.groupBoxChannels.Controls.Add(this.checkBoxInputRight);
            this.groupBoxChannels.Controls.Add(this.checkBoxInputLeft);
            this.groupBoxChannels.Controls.Add(this.checkBoxOutputRight);
            this.groupBoxChannels.Controls.Add(this.checkBoxOutputLeft);
            this.groupBoxChannels.Controls.Add(this.label6);
            this.groupBoxChannels.Controls.Add(this.label1);
            this.groupBoxChannels.Location = new System.Drawing.Point(308, 12);
            this.groupBoxChannels.Name = "groupBoxChannels";
            this.groupBoxChannels.Size = new System.Drawing.Size(200, 180);
            this.groupBoxChannels.TabIndex = 1;
            this.groupBoxChannels.TabStop = false;
            this.groupBoxChannels.Text = "Channel Selection";
            // 
            // checkBoxInputRight
            // 
            this.checkBoxInputRight.AutoSize = true;
            this.checkBoxInputRight.Checked = true;
            this.checkBoxInputRight.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxInputRight.Location = new System.Drawing.Point(120, 60);
            this.checkBoxInputRight.Name = "checkBoxInputRight";
            this.checkBoxInputRight.Size = new System.Drawing.Size(70, 17);
            this.checkBoxInputRight.TabIndex = 5;
            this.checkBoxInputRight.Text = "Right";
            this.checkBoxInputRight.UseVisualStyleBackColor = true;
            // 
            // checkBoxInputLeft
            // 
            this.checkBoxInputLeft.AutoSize = true;
            this.checkBoxInputLeft.Checked = true;
            this.checkBoxInputLeft.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxInputLeft.Location = new System.Drawing.Point(20, 60);
            this.checkBoxInputLeft.Name = "checkBoxInputLeft";
            this.checkBoxInputLeft.Size = new System.Drawing.Size(56, 17);
            this.checkBoxInputLeft.TabIndex = 4;
            this.checkBoxInputLeft.Text = "Left";
            this.checkBoxInputLeft.UseVisualStyleBackColor = true;
            // 
            // checkBoxOutputRight
            // 
            this.checkBoxOutputRight.AutoSize = true;
            this.checkBoxOutputRight.Checked = true;
            this.checkBoxOutputRight.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxOutputRight.Location = new System.Drawing.Point(120, 30);
            this.checkBoxOutputRight.Name = "checkBoxOutputRight";
            this.checkBoxOutputRight.Size = new System.Drawing.Size(70, 17);
            this.checkBoxOutputRight.TabIndex = 3;
            this.checkBoxOutputRight.Text = "Right";
            this.checkBoxOutputRight.UseVisualStyleBackColor = true;
            // 
            // checkBoxOutputLeft
            // 
            this.checkBoxOutputLeft.AutoSize = true;
            this.checkBoxOutputLeft.Checked = true;
            this.checkBoxOutputLeft.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxOutputLeft.Location = new System.Drawing.Point(20, 30);
            this.checkBoxOutputLeft.Name = "checkBoxOutputLeft";
            this.checkBoxOutputLeft.Size = new System.Drawing.Size(56, 17);
            this.checkBoxOutputLeft.TabIndex = 2;
            this.checkBoxOutputLeft.Text = "Left";
            this.checkBoxOutputLeft.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(10, 45);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(34, 13);
            this.label6.TabIndex = 1;
            this.label6.Text = "Input";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(10, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Output";
            // 
            // buttonGenerate
            // 
            this.buttonGenerate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGenerate.Location = new System.Drawing.Point(524, 20);
            this.buttonGenerate.Name = "buttonGenerate";
            this.buttonGenerate.Size = new System.Drawing.Size(120, 40);
            this.buttonGenerate.TabIndex = 2;
            this.buttonGenerate.Text = "Generate";
            this.buttonGenerate.UseVisualStyleBackColor = true;
            this.buttonGenerate.Click += new System.EventHandler(this.buttonGenerate_Click);
            // 
            // buttonStart
            // 
            this.buttonStart.Font = new System.Drawing.Font(" Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.ForeColor = System.Drawing.Color.Green;
            this.buttonStart.Location = new System.Drawing.Point(524, 70);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(120, 40);
            this.buttonStart.TabIndex = 3;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Enabled = false;
            this.buttonStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.ForeColor = System.Drawing.Color.Red;
            this.buttonStop.Location = new System.Drawing.Point(524, 120);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(120, 40);
            this.buttonStop.TabIndex = 4;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // chartOutput
            // 
            this.chartOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.chartOutput.Location = new System.Drawing.Point(12, 198);
            this.chartOutput.Name = "chartOutput";
            this.chartOutput.Size = new System.Drawing.Size(632, 210);
            this.chartOutput.TabIndex = 5;
            // 
            // chartInput
            // 
            this.chartInput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.chartInput.Location = new System.Drawing.Point(12, 425);
            this.chartInput.Name = "chartInput";
            this.chartInput.Size = new System.Drawing.Size(632, 210);
            this.chartInput.TabIndex = 6;
            // 
            // timerCheck
            // 
            this.timerCheck.Interval = 100;
            this.timerCheck.Tick += new System.EventHandler(this.timerCheck_Tick);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 643);
            this.statusStrip.Name = "statusStrip1";
            this.statusStrip.Size = new System.Drawing.Size(656, 22);
            this.statusStrip.TabIndex = 7;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(32, 17);
            this.toolStripStatusLabel.Text = "Ready";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(656, 665);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.chartInput);
            this.Controls.Add(this.chartOutput);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonGenerate);
            this.Controls.Add(this.groupBoxChannels);
            this.Controls.Add(this.groupBoxConfiguration);
            this.MinimumSize = new System.Drawing.Size(600, 500);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MISDAudioCard Example - Audio Input/Output";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBoxConfiguration.ResumeLayout(false);
            this.groupBoxConfiguration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAmplitude)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFrequency)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSamples)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).EndInit();
            this.groupBoxChannels.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxConfiguration;
        private System.Windows.Forms.NumericUpDown numericUpDownAmplitude;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numericUpDownFrequency;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDownSamples;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericUpDownSampleRate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBoxChannels;
        private System.Windows.Forms.CheckBox checkBoxInputRight;
        private System.Windows.Forms.CheckBox checkBoxInputLeft;
        private System.Windows.Forms.CheckBox checkBoxOutputRight;
        private System.Windows.Forms.CheckBox checkBoxOutputLeft;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonGenerate;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private SeeSharpTools.JY.GUI.EasyChartX chartOutput;
        private SeeSharpTools.JY.GUI.EasyChartX chartInput;
        private System.Windows.Forms.Timer timerCheck;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
    }
}
