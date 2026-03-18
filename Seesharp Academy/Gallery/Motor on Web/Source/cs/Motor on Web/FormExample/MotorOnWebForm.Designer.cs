namespace FormExample
{
    partial class MotorOnWebForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.easyChartXWaveform = new SeeSharpTools.JY.GUI.EasyChartX();
            this.easyChartXSpectrum = new SeeSharpTools.JY.GUI.EasyChartX();
            this.btStart = new SeeSharpTools.JY.GUI.EasyButton();
            this.textBoxDeviceName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.timerMain = new System.Windows.Forms.Timer(this.components);
            this.numericUpDownChannelIndex = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownSampleRate = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownBlockLength = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownMonitorInterval = new System.Windows.Forms.NumericUpDown();
            this.btStop = new SeeSharpTools.JY.GUI.EasyButton();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.colIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFrequency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAmplitude = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBoxMonitorSettings = new System.Windows.Forms.GroupBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownChannelIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBlockLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMonitorInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            this.groupBoxMonitorSettings.SuspendLayout();
            this.SuspendLayout();
            // 
            // easyChartXWaveform
            // 
            this.easyChartXWaveform.AutoClear = true;
            this.easyChartXWaveform.AxisX.AutoScale = true;
            this.easyChartXWaveform.AxisX.AutoZoomReset = false;
            this.easyChartXWaveform.AxisX.Color = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisX.IsLogarithmic = false;
            this.easyChartXWaveform.AxisX.LabelAngle = 0;
            this.easyChartXWaveform.AxisX.LabelEnabled = true;
            this.easyChartXWaveform.AxisX.LabelFormat = "";
            this.easyChartXWaveform.AxisX.LogarithmBase = 10D;
            this.easyChartXWaveform.AxisX.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisX.MajorGridEnabled = true;
            this.easyChartXWaveform.AxisX.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXWaveform.AxisX.Maximum = 10D;
            this.easyChartXWaveform.AxisX.MinGridCountPerPixel = 0.01D;
            this.easyChartXWaveform.AxisX.Minimum = 1D;
            this.easyChartXWaveform.AxisX.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXWaveform.AxisX.MinorGridEnabled = false;
            this.easyChartXWaveform.AxisX.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXWaveform.AxisX.ShowLogarithmicLines = false;
            this.easyChartXWaveform.AxisX.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisX.TickWidth = 1F;
            this.easyChartXWaveform.AxisX.Title = "";
            this.easyChartXWaveform.AxisX.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXWaveform.AxisX.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXWaveform.AxisX.ViewMaximum = 10D;
            this.easyChartXWaveform.AxisX.ViewMinimum = 1D;
            this.easyChartXWaveform.AxisY.AutoScale = true;
            this.easyChartXWaveform.AxisY.AutoZoomReset = false;
            this.easyChartXWaveform.AxisY.Color = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisY.IsLogarithmic = false;
            this.easyChartXWaveform.AxisY.LabelAngle = 0;
            this.easyChartXWaveform.AxisY.LabelEnabled = true;
            this.easyChartXWaveform.AxisY.LabelFormat = "";
            this.easyChartXWaveform.AxisY.LogarithmBase = 10D;
            this.easyChartXWaveform.AxisY.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisY.MajorGridEnabled = true;
            this.easyChartXWaveform.AxisY.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXWaveform.AxisY.Maximum = 10D;
            this.easyChartXWaveform.AxisY.MinGridCountPerPixel = 0.01D;
            this.easyChartXWaveform.AxisY.Minimum = 1D;
            this.easyChartXWaveform.AxisY.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXWaveform.AxisY.MinorGridEnabled = false;
            this.easyChartXWaveform.AxisY.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXWaveform.AxisY.ShowLogarithmicLines = false;
            this.easyChartXWaveform.AxisY.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisY.TickWidth = 1F;
            this.easyChartXWaveform.AxisY.Title = "";
            this.easyChartXWaveform.AxisY.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXWaveform.AxisY.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXWaveform.AxisY.ViewMaximum = 10D;
            this.easyChartXWaveform.AxisY.ViewMinimum = 1D;
            this.easyChartXWaveform.AxisY2.AutoScale = true;
            this.easyChartXWaveform.AxisY2.AutoZoomReset = false;
            this.easyChartXWaveform.AxisY2.Color = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisY2.IsLogarithmic = false;
            this.easyChartXWaveform.AxisY2.LabelAngle = 0;
            this.easyChartXWaveform.AxisY2.LabelEnabled = true;
            this.easyChartXWaveform.AxisY2.LabelFormat = "";
            this.easyChartXWaveform.AxisY2.LogarithmBase = 10D;
            this.easyChartXWaveform.AxisY2.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisY2.MajorGridEnabled = true;
            this.easyChartXWaveform.AxisY2.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXWaveform.AxisY2.Maximum = 10D;
            this.easyChartXWaveform.AxisY2.MinGridCountPerPixel = 0.01D;
            this.easyChartXWaveform.AxisY2.Minimum = 1D;
            this.easyChartXWaveform.AxisY2.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXWaveform.AxisY2.MinorGridEnabled = false;
            this.easyChartXWaveform.AxisY2.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXWaveform.AxisY2.ShowLogarithmicLines = false;
            this.easyChartXWaveform.AxisY2.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.AxisY2.TickWidth = 1F;
            this.easyChartXWaveform.AxisY2.Title = "";
            this.easyChartXWaveform.AxisY2.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXWaveform.AxisY2.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXWaveform.AxisY2.ViewMaximum = 10D;
            this.easyChartXWaveform.AxisY2.ViewMinimum = 1D;
            this.easyChartXWaveform.ChartAreaBackColor = System.Drawing.Color.Empty;
            this.easyChartXWaveform.GradientStyle = SeeSharpTools.JY.GUI.EasyChartX.ChartGradientStyle.None;
            this.easyChartXWaveform.LegendBackColor = System.Drawing.Color.Transparent;
            this.easyChartXWaveform.LegendFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.easyChartXWaveform.LegendForeColor = System.Drawing.Color.Black;
            this.easyChartXWaveform.LegendVisible = true;
            this.easyChartXWaveform.Location = new System.Drawing.Point(12, 12);
            this.easyChartXWaveform.Miscellaneous.CheckNegtiveOrZero = false;
            this.easyChartXWaveform.Miscellaneous.DataStorage = SeeSharpTools.JY.GUI.DataStorageType.Clone;
            this.easyChartXWaveform.Miscellaneous.DirectionChartCount = 3;
            this.easyChartXWaveform.Miscellaneous.EnableSimplification = true;
            this.easyChartXWaveform.Miscellaneous.MarkerSize = 7;
            this.easyChartXWaveform.Miscellaneous.MaxSeriesCount = 32;
            this.easyChartXWaveform.Miscellaneous.MaxSeriesPointCount = 4000;
            this.easyChartXWaveform.Miscellaneous.ShowFunctionMenu = true;
            this.easyChartXWaveform.Miscellaneous.SplitLayoutColumnInterval = 0F;
            this.easyChartXWaveform.Miscellaneous.SplitLayoutDirection = SeeSharpTools.JY.GUI.EasyChartXUtility.LayoutDirection.LeftToRight;
            this.easyChartXWaveform.Miscellaneous.SplitLayoutRowInterval = 0F;
            this.easyChartXWaveform.Miscellaneous.SplitViewAutoLayout = true;
            this.easyChartXWaveform.Name = "easyChartXWaveform";
            this.easyChartXWaveform.Series.Count = 0;
            this.easyChartXWaveform.Size = new System.Drawing.Size(560, 200);
            this.easyChartXWaveform.SplitView = false;
            this.easyChartXWaveform.TabIndex = 0;
            this.easyChartXWaveform.XCursor.AutoInterval = true;
            this.easyChartXWaveform.XCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXWaveform.XCursor.Interval = 0.001D;
            this.easyChartXWaveform.XCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Zoom;
            this.easyChartXWaveform.XCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXWaveform.XCursor.Value = double.NaN;
            this.easyChartXWaveform.YCursor.AutoInterval = true;
            this.easyChartXWaveform.YCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXWaveform.YCursor.Interval = 0.001D;
            this.easyChartXWaveform.YCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXWaveform.YCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXWaveform.YCursor.Value = double.NaN;
            // 
            // easyChartXSpectrum
            // 
            this.easyChartXSpectrum.AutoClear = true;
            this.easyChartXSpectrum.AxisX.AutoScale = true;
            this.easyChartXSpectrum.AxisX.AutoZoomReset = false;
            this.easyChartXSpectrum.AxisX.Color = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisX.IsLogarithmic = false;
            this.easyChartXSpectrum.AxisX.LabelAngle = 0;
            this.easyChartXSpectrum.AxisX.LabelEnabled = true;
            this.easyChartXSpectrum.AxisX.LabelFormat = "";
            this.easyChartXSpectrum.AxisX.LogarithmBase = 10D;
            this.easyChartXSpectrum.AxisX.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisX.MajorGridEnabled = true;
            this.easyChartXSpectrum.AxisX.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXSpectrum.AxisX.Maximum = 10D;
            this.easyChartXSpectrum.AxisX.MinGridCountPerPixel = 0.01D;
            this.easyChartXSpectrum.AxisX.Minimum = 1D;
            this.easyChartXSpectrum.AxisX.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXSpectrum.AxisX.MinorGridEnabled = false;
            this.easyChartXSpectrum.AxisX.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXSpectrum.AxisX.ShowLogarithmicLines = false;
            this.easyChartXSpectrum.AxisX.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisX.TickWidth = 1F;
            this.easyChartXSpectrum.AxisX.Title = "";
            this.easyChartXSpectrum.AxisX.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXSpectrum.AxisX.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXSpectrum.AxisX.ViewMaximum = 10D;
            this.easyChartXSpectrum.AxisX.ViewMinimum = 1D;
            this.easyChartXSpectrum.AxisY.AutoScale = true;
            this.easyChartXSpectrum.AxisY.AutoZoomReset = false;
            this.easyChartXSpectrum.AxisY.Color = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisY.IsLogarithmic = false;
            this.easyChartXSpectrum.AxisY.LabelAngle = 0;
            this.easyChartXSpectrum.AxisY.LabelEnabled = true;
            this.easyChartXSpectrum.AxisY.LabelFormat = "";
            this.easyChartXSpectrum.AxisY.LogarithmBase = 10D;
            this.easyChartXSpectrum.AxisY.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisY.MajorGridEnabled = true;
            this.easyChartXSpectrum.AxisY.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXSpectrum.AxisY.Maximum = 10D;
            this.easyChartXSpectrum.AxisY.MinGridCountPerPixel = 0.01D;
            this.easyChartXSpectrum.AxisY.Minimum = 1D;
            this.easyChartXSpectrum.AxisY.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXSpectrum.AxisY.MinorGridEnabled = false;
            this.easyChartXSpectrum.AxisY.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXSpectrum.AxisY.ShowLogarithmicLines = false;
            this.easyChartXSpectrum.AxisY.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisY.TickWidth = 1F;
            this.easyChartXSpectrum.AxisY.Title = "";
            this.easyChartXSpectrum.AxisY.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXSpectrum.AxisY.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXSpectrum.AxisY.ViewMaximum = 10D;
            this.easyChartXSpectrum.AxisY.ViewMinimum = 1D;
            this.easyChartXSpectrum.AxisY2.AutoScale = true;
            this.easyChartXSpectrum.AxisY2.AutoZoomReset = false;
            this.easyChartXSpectrum.AxisY2.Color = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisY2.IsLogarithmic = false;
            this.easyChartXSpectrum.AxisY2.LabelAngle = 0;
            this.easyChartXSpectrum.AxisY2.LabelEnabled = true;
            this.easyChartXSpectrum.AxisY2.LabelFormat = "";
            this.easyChartXSpectrum.AxisY2.LogarithmBase = 10D;
            this.easyChartXSpectrum.AxisY2.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisY2.MajorGridEnabled = true;
            this.easyChartXSpectrum.AxisY2.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXSpectrum.AxisY2.Maximum = 10D;
            this.easyChartXSpectrum.AxisY2.MinGridCountPerPixel = 0.01D;
            this.easyChartXSpectrum.AxisY2.Minimum = 1D;
            this.easyChartXSpectrum.AxisY2.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXSpectrum.AxisY2.MinorGridEnabled = false;
            this.easyChartXSpectrum.AxisY2.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXSpectrum.AxisY2.ShowLogarithmicLines = false;
            this.easyChartXSpectrum.AxisY2.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.AxisY2.TickWidth = 1F;
            this.easyChartXSpectrum.AxisY2.Title = "";
            this.easyChartXSpectrum.AxisY2.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXSpectrum.AxisY2.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXSpectrum.AxisY2.ViewMaximum = 10D;
            this.easyChartXSpectrum.AxisY2.ViewMinimum = 1D;
            this.easyChartXSpectrum.ChartAreaBackColor = System.Drawing.Color.Empty;
            this.easyChartXSpectrum.GradientStyle = SeeSharpTools.JY.GUI.EasyChartX.ChartGradientStyle.None;
            this.easyChartXSpectrum.LegendBackColor = System.Drawing.Color.Transparent;
            this.easyChartXSpectrum.LegendFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.easyChartXSpectrum.LegendForeColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.LegendVisible = true;
            this.easyChartXSpectrum.Location = new System.Drawing.Point(12, 218);
            this.easyChartXSpectrum.Miscellaneous.CheckNegtiveOrZero = false;
            this.easyChartXSpectrum.Miscellaneous.DataStorage = SeeSharpTools.JY.GUI.DataStorageType.Clone;
            this.easyChartXSpectrum.Miscellaneous.DirectionChartCount = 3;
            this.easyChartXSpectrum.Miscellaneous.EnableSimplification = true;
            this.easyChartXSpectrum.Miscellaneous.MarkerSize = 7;
            this.easyChartXSpectrum.Miscellaneous.MaxSeriesCount = 32;
            this.easyChartXSpectrum.Miscellaneous.MaxSeriesPointCount = 4000;
            this.easyChartXSpectrum.Miscellaneous.ShowFunctionMenu = true;
            this.easyChartXSpectrum.Miscellaneous.SplitLayoutColumnInterval = 0F;
            this.easyChartXSpectrum.Miscellaneous.SplitLayoutDirection = SeeSharpTools.JY.GUI.EasyChartXUtility.LayoutDirection.LeftToRight;
            this.easyChartXSpectrum.Miscellaneous.SplitLayoutRowInterval = 0F;
            this.easyChartXSpectrum.Miscellaneous.SplitViewAutoLayout = true;
            this.easyChartXSpectrum.Name = "easyChartXSpectrum";
            this.easyChartXSpectrum.Series.Count = 0;
            this.easyChartXSpectrum.Size = new System.Drawing.Size(560, 200);
            this.easyChartXSpectrum.SplitView = false;
            this.easyChartXSpectrum.TabIndex = 0;
            this.easyChartXSpectrum.XCursor.AutoInterval = true;
            this.easyChartXSpectrum.XCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXSpectrum.XCursor.Interval = 0.001D;
            this.easyChartXSpectrum.XCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Zoom;
            this.easyChartXSpectrum.XCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXSpectrum.XCursor.Value = double.NaN;
            this.easyChartXSpectrum.YCursor.AutoInterval = true;
            this.easyChartXSpectrum.YCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXSpectrum.YCursor.Interval = 0.001D;
            this.easyChartXSpectrum.YCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXSpectrum.YCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXSpectrum.YCursor.Value = double.NaN;
            // 
            // btStart
            // 
            this.btStart.BackgroundStyle = SeeSharpTools.JY.GUI.BackgroundStyle.Normal;
            this.btStart.BorderColor = System.Drawing.Color.Silver;
            this.btStart.BorderRadius = 20;
            this.btStart.BorderThickness = 1;
            this.btStart.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(180)))), ((int)(((byte)(205)))));
            this.btStart.ButtonColor2 = System.Drawing.Color.Silver;
            this.btStart.ButtonImage = null;
            this.btStart.Image = null;
            this.btStart.ImageSize = new System.Drawing.Size(0, 0);
            this.btStart.ImageStretch = false;
            this.btStart.Location = new System.Drawing.Point(628, 218);
            this.btStart.Name = "btStart";
            this.btStart.PreSetImage = SeeSharpTools.JY.GUI.EasyButton.ButtonPresetImage.None;
            this.btStart.Shadow = true;
            this.btStart.Size = new System.Drawing.Size(80, 32);
            this.btStart.TabIndex = 1;
            this.btStart.Text = "Start";
            this.btStart.ThreeDimensional = false;
            this.btStart.Click += new System.EventHandler(this.BtStart_Click);
            // 
            // textBoxDeviceName
            // 
            this.textBoxDeviceName.Location = new System.Drawing.Point(147, 58);
            this.textBoxDeviceName.Name = "textBoxDeviceName";
            this.textBoxDeviceName.Size = new System.Drawing.Size(100, 21);
            this.textBoxDeviceName.TabIndex = 2;
            this.textBoxDeviceName.Text = "USB-1202";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 61);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "Device Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Channel Index";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 114);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "Sample Rate (S/s)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 141);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "Block Length";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 168);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(125, 12);
            this.label5.TabIndex = 3;
            this.label5.Text = "Monitor Interval (s)";
            // 
            // timerMain
            // 
            this.timerMain.Interval = 200;
            this.timerMain.Tick += new System.EventHandler(this.TimerMain_Tick);
            // 
            // numericUpDownChannelIndex
            // 
            this.numericUpDownChannelIndex.Location = new System.Drawing.Point(147, 85);
            this.numericUpDownChannelIndex.Name = "numericUpDownChannelIndex";
            this.numericUpDownChannelIndex.Size = new System.Drawing.Size(100, 21);
            this.numericUpDownChannelIndex.TabIndex = 4;
            // 
            // numericUpDownSampleRate
            // 
            this.numericUpDownSampleRate.Location = new System.Drawing.Point(147, 112);
            this.numericUpDownSampleRate.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.numericUpDownSampleRate.Name = "numericUpDownSampleRate";
            this.numericUpDownSampleRate.Size = new System.Drawing.Size(100, 21);
            this.numericUpDownSampleRate.TabIndex = 4;
            this.numericUpDownSampleRate.Value = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            // 
            // numericUpDownBlockLength
            // 
            this.numericUpDownBlockLength.Location = new System.Drawing.Point(147, 139);
            this.numericUpDownBlockLength.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDownBlockLength.Name = "numericUpDownBlockLength";
            this.numericUpDownBlockLength.Size = new System.Drawing.Size(100, 21);
            this.numericUpDownBlockLength.TabIndex = 4;
            this.numericUpDownBlockLength.Value = new decimal(new int[] {
            512,
            0,
            0,
            0});
            // 
            // numericUpDownMonitorInterval
            // 
            this.numericUpDownMonitorInterval.DecimalPlaces = 1;
            this.numericUpDownMonitorInterval.Location = new System.Drawing.Point(147, 166);
            this.numericUpDownMonitorInterval.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDownMonitorInterval.Name = "numericUpDownMonitorInterval";
            this.numericUpDownMonitorInterval.Size = new System.Drawing.Size(100, 21);
            this.numericUpDownMonitorInterval.TabIndex = 4;
            this.numericUpDownMonitorInterval.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // btStop
            // 
            this.btStop.BackgroundStyle = SeeSharpTools.JY.GUI.BackgroundStyle.Normal;
            this.btStop.BorderColor = System.Drawing.Color.Silver;
            this.btStop.BorderRadius = 20;
            this.btStop.BorderThickness = 1;
            this.btStop.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(180)))), ((int)(((byte)(205)))));
            this.btStop.ButtonColor2 = System.Drawing.Color.Silver;
            this.btStop.ButtonImage = null;
            this.btStop.Image = null;
            this.btStop.ImageSize = new System.Drawing.Size(0, 0);
            this.btStop.ImageStretch = false;
            this.btStop.Location = new System.Drawing.Point(731, 218);
            this.btStop.Name = "btStop";
            this.btStop.PreSetImage = SeeSharpTools.JY.GUI.EasyButton.ButtonPresetImage.None;
            this.btStop.Shadow = true;
            this.btStop.Size = new System.Drawing.Size(80, 32);
            this.btStop.TabIndex = 1;
            this.btStop.Text = "Stop";
            this.btStop.ThreeDimensional = false;
            this.btStop.Click += new System.EventHandler(this.BtStop_Click);
            // 
            // dgvResults
            // 
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIndex,
            this.colFrequency,
            this.colAmplitude});
            this.dgvResults.Location = new System.Drawing.Point(580, 276);
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.RowTemplate.Height = 23;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(273, 142);
            this.dgvResults.TabIndex = 7;
            // 
            // colIndex
            // 
            this.colIndex.HeaderText = "#";
            this.colIndex.Name = "colIndex";
            this.colIndex.ReadOnly = true;
            this.colIndex.Width = 40;
            // 
            // colFrequency
            // 
            this.colFrequency.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle3.Format = "N2";
            this.colFrequency.DefaultCellStyle = dataGridViewCellStyle3;
            this.colFrequency.HeaderText = "Frequency";
            this.colFrequency.Name = "colFrequency";
            this.colFrequency.ReadOnly = true;
            // 
            // colAmplitude
            // 
            this.colAmplitude.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle4.Format = "0.000e0";
            this.colAmplitude.DefaultCellStyle = dataGridViewCellStyle4;
            this.colAmplitude.HeaderText = "Amplitude";
            this.colAmplitude.Name = "colAmplitude";
            this.colAmplitude.ReadOnly = true;
            // 
            // groupBoxMonitorSettings
            // 
            this.groupBoxMonitorSettings.Controls.Add(this.comboBox1);
            this.groupBoxMonitorSettings.Controls.Add(this.numericUpDownMonitorInterval);
            this.groupBoxMonitorSettings.Controls.Add(this.numericUpDownBlockLength);
            this.groupBoxMonitorSettings.Controls.Add(this.numericUpDownSampleRate);
            this.groupBoxMonitorSettings.Controls.Add(this.numericUpDownChannelIndex);
            this.groupBoxMonitorSettings.Controls.Add(this.label5);
            this.groupBoxMonitorSettings.Controls.Add(this.label4);
            this.groupBoxMonitorSettings.Controls.Add(this.label3);
            this.groupBoxMonitorSettings.Controls.Add(this.label2);
            this.groupBoxMonitorSettings.Controls.Add(this.label6);
            this.groupBoxMonitorSettings.Controls.Add(this.label1);
            this.groupBoxMonitorSettings.Controls.Add(this.textBoxDeviceName);
            this.groupBoxMonitorSettings.Location = new System.Drawing.Point(583, 12);
            this.groupBoxMonitorSettings.Name = "groupBoxMonitorSettings";
            this.groupBoxMonitorSettings.Size = new System.Drawing.Size(269, 200);
            this.groupBoxMonitorSettings.TabIndex = 8;
            this.groupBoxMonitorSettings.TabStop = false;
            this.groupBoxMonitorSettings.Text = "Monitor Settings";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "JYUSB-1202",
            "JYUSB-1601"});
            this.comboBox1.Location = new System.Drawing.Point(147, 32);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(100, 20);
            this.comboBox1.TabIndex = 5;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(17, 35);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(77, 12);
            this.label6.TabIndex = 3;
            this.label6.Text = "Device Model";
            // 
            // MotorOnWebForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(864, 432);
            this.Controls.Add(this.btStart);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.groupBoxMonitorSettings);
            this.Controls.Add(this.dgvResults);
            this.Controls.Add(this.easyChartXSpectrum);
            this.Controls.Add(this.easyChartXWaveform);
            this.Name = "MotorOnWebForm";
            this.Text = "Form example";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownChannelIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBlockLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMonitorInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            this.groupBoxMonitorSettings.ResumeLayout(false);
            this.groupBoxMonitorSettings.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private SeeSharpTools.JY.GUI.EasyChartX easyChartXWaveform;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXSpectrum;
        private SeeSharpTools.JY.GUI.EasyButton btStart;
        private System.Windows.Forms.TextBox textBoxDeviceName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Timer timerMain;
        private System.Windows.Forms.NumericUpDown numericUpDownChannelIndex;
        private System.Windows.Forms.NumericUpDown numericUpDownSampleRate;
        private System.Windows.Forms.NumericUpDown numericUpDownBlockLength;
        private System.Windows.Forms.NumericUpDown numericUpDownMonitorInterval;
        private SeeSharpTools.JY.GUI.EasyButton btStop;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIndex;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFrequency;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAmplitude;
        private System.Windows.Forms.GroupBox groupBoxMonitorSettings;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label6;
    }
}

