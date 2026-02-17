namespace MISDAudioCard.Example.AI_AO
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
            this.numericUpDownSamples = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownSampleRate = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonAI = new System.Windows.Forms.Button();
            this.buttonAO = new System.Windows.Forms.Button();
            this.easyChartX = new SeeSharpTools.JY.GUI.EasyChartX();
            this.timerCheck = new System.Windows.Forms.Timer();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.groupBoxConfiguration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSamples)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxConfiguration
            // 
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownSamples);
            this.groupBoxConfiguration.Controls.Add(this.label2);
            this.groupBoxConfiguration.Controls.Add(this.numericUpDownSampleRate);
            this.groupBoxConfiguration.Controls.Add(this.label1);
            this.groupBoxConfiguration.Location = new System.Drawing.Point(12, 12);
            this.groupBoxConfiguration.Name = "groupBoxConfiguration";
            this.groupBoxConfiguration.Size = new System.Drawing.Size(280, 120);
            this.groupBoxConfiguration.TabIndex = 0;
            this.groupBoxConfiguration.TabStop = false;
            this.groupBoxConfiguration.Text = "Configuration";
            // 
            // numericUpDownSamples
            // 
            this.numericUpDownSamples.Location = new System.Drawing.Point(120, 70);
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
            this.numericUpDownSamples.Value = new decimal(new int[] {
            44100,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Samples (Total):";
            // 
            // numericUpDownSampleRate
            // 
            this.numericUpDownSampleRate.Location = new System.Drawing.Point(120, 30);
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
            this.numericUpDownSampleRate.Value = new decimal(new int[] {
            44100,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Sample Rate:";
            // 
            // buttonAI
            // 
            this.buttonAI.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAI.ForeColor = System.Drawing.Color.Blue;
            this.buttonAI.Location = new System.Drawing.Point(308, 20);
            this.buttonAI.Name = "buttonAI";
            this.buttonAI.Size = new System.Drawing.Size(120, 50);
            this.buttonAI.TabIndex = 1;
            this.buttonAI.Text = "AI";
            this.buttonAI.UseVisualStyleBackColor = true;
            this.buttonAI.Click += new System.EventHandler(this.buttonAI_Click);
            // 
            // buttonAO
            // 
            this.buttonAO.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAO.ForeColor = System.Drawing.Color.Green;
            this.buttonAO.Location = new System.Drawing.Point(308, 82);
            this.buttonAO.Name = "buttonAO";
            this.buttonAO.Size = new System.Drawing.Size(120, 50);
            this.buttonAO.TabIndex = 2;
            this.buttonAO.Text = "AO";
            this.buttonAO.UseVisualStyleBackColor = true;
            this.buttonAO.Click += new System.EventHandler(this.buttonAO_Click);
            // 
            // easyChartX
            // 
            this.easyChartX.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.easyChartX.Location = new System.Drawing.Point(12, 145);
            this.easyChartX.Name = "easyChartX";
            this.easyChartX.Size = new System.Drawing.Size(632, 380);
            this.easyChartX.TabIndex = 3;
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
            this.statusStrip.Location = new System.Drawing.Point(0, 535);
            this.statusStrip.Name = "statusStrip1";
            this.statusStrip.Size = new System.Drawing.Size(656, 22);
            this.statusStrip.TabIndex = 4;
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
            this.ClientSize = new System.Drawing.Size(656, 557);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.easyChartX);
            this.Controls.Add(this.buttonAO);
            this.Controls.Add(this.buttonAI);
            this.Controls.Add(this.groupBoxConfiguration);
            this.MinimumSize = new System.Drawing.Size(600, 500);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MISDAudioCard AI-AO Example";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBoxConfiguration.ResumeLayout(false);
            this.groupBoxConfiguration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSamples)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxConfiguration;
        private System.Windows.Forms.NumericUpDown numericUpDownSamples;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDownSampleRate;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonAI;
        private System.Windows.Forms.Button buttonAO;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartX;
        private System.Windows.Forms.Timer timerCheck;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
    }
}
