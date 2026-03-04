namespace FormExample
{
    partial class SpectrumMainForm
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
            this.easyChartXWaveform = new SeeSharpTools.JY.GUI.EasyChartX();
            this.easyChartXSpectrum = new SeeSharpTools.JY.GUI.EasyChartX();
            this.btAnalysis = new SeeSharpTools.JY.GUI.EasyButton();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownSignalFreq = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownSampleRate = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownSampleLength = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBoxWindowType = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSignalFreq)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleLength)).BeginInit();
            this.SuspendLayout();
            // 
            // easyChartX1
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
            this.easyChartXWaveform.Location = new System.Drawing.Point(12, 24);
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
            this.easyChartXWaveform.Name = "easyChartX1";
            this.easyChartXWaveform.Series.Count = 0;
            this.easyChartXWaveform.Size = new System.Drawing.Size(830, 286);
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
            // easyChartX2
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
            this.easyChartXSpectrum.Location = new System.Drawing.Point(12, 328);
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
            this.easyChartXSpectrum.Name = "easyChartX2";
            this.easyChartXSpectrum.Series.Count = 0;
            this.easyChartXSpectrum.Size = new System.Drawing.Size(830, 286);
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
            // btAnalysis
            // 
            this.btAnalysis.BackgroundStyle = SeeSharpTools.JY.GUI.BackgroundStyle.Normal;
            this.btAnalysis.BorderColor = System.Drawing.Color.Silver;
            this.btAnalysis.BorderRadius = 20;
            this.btAnalysis.BorderThickness = 1;
            this.btAnalysis.ButtonColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(180)))), ((int)(((byte)(205)))));
            this.btAnalysis.ButtonColor2 = System.Drawing.Color.Silver;
            this.btAnalysis.ButtonImage = null;
            this.btAnalysis.Image = null;
            this.btAnalysis.ImageSize = new System.Drawing.Size(0, 0);
            this.btAnalysis.ImageStretch = false;
            this.btAnalysis.Location = new System.Drawing.Point(865, 223);
            this.btAnalysis.Name = "btAnalysis";
            this.btAnalysis.PreSetImage = SeeSharpTools.JY.GUI.EasyButton.ButtonPresetImage.None;
            this.btAnalysis.Shadow = true;
            this.btAnalysis.Size = new System.Drawing.Size(80, 32);
            this.btAnalysis.TabIndex = 1;
            this.btAnalysis.Text = "Analysis";
            this.btAnalysis.ThreeDimensional = false;
            this.btAnalysis.Click += new System.EventHandler(this.btAnalysis_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "Time Waveform";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 313);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "Spectrum";
            // 
            // numericUpDownSignalFreq
            // 
            this.numericUpDownSignalFreq.DecimalPlaces = 1;
            this.numericUpDownSignalFreq.Location = new System.Drawing.Point(850, 39);
            this.numericUpDownSignalFreq.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDownSignalFreq.Name = "numericUpDownSignalFreq";
            this.numericUpDownSignalFreq.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownSignalFreq.TabIndex = 3;
            this.numericUpDownSignalFreq.ThousandsSeparator = true;
            this.numericUpDownSignalFreq.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // numericUpDownSampleRate
            // 
            this.numericUpDownSampleRate.Location = new System.Drawing.Point(850, 78);
            this.numericUpDownSampleRate.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDownSampleRate.Name = "numericUpDownSampleRate";
            this.numericUpDownSampleRate.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownSampleRate.TabIndex = 3;
            this.numericUpDownSampleRate.ThousandsSeparator = true;
            this.numericUpDownSampleRate.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // numericUpDownSampleLength
            // 
            this.numericUpDownSampleLength.Location = new System.Drawing.Point(850, 117);
            this.numericUpDownSampleLength.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDownSampleLength.Name = "numericUpDownSampleLength";
            this.numericUpDownSampleLength.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownSampleLength.TabIndex = 3;
            this.numericUpDownSampleLength.ThousandsSeparator = true;
            this.numericUpDownSampleLength.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(848, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(131, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "Signal Frequency (Hz)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(848, 63);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(107, 12);
            this.label4.TabIndex = 2;
            this.label4.Text = "Sample Rate (S/s)";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(848, 102);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(83, 12);
            this.label5.TabIndex = 2;
            this.label5.Text = "Sample Length";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(848, 141);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(71, 12);
            this.label6.TabIndex = 2;
            this.label6.Text = "Window Type";
            // 
            // comboBoxWindowType
            // 
            this.comboBoxWindowType.FormattingEnabled = true;
            this.comboBoxWindowType.Location = new System.Drawing.Point(850, 156);
            this.comboBoxWindowType.Name = "comboBoxWindowType";
            this.comboBoxWindowType.Size = new System.Drawing.Size(120, 20);
            this.comboBoxWindowType.TabIndex = 4;
            // 
            // SpectrumMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(985, 633);
            this.Controls.Add(this.comboBoxWindowType);
            this.Controls.Add(this.numericUpDownSampleLength);
            this.Controls.Add(this.numericUpDownSampleRate);
            this.Controls.Add(this.numericUpDownSignalFreq);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btAnalysis);
            this.Controls.Add(this.easyChartXSpectrum);
            this.Controls.Add(this.easyChartXWaveform);
            this.Name = "SpectrumMainForm";
            this.Text = "Simulated Signal Spectrum Analysis";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSignalFreq)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleLength)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SeeSharpTools.JY.GUI.EasyChartX easyChartXWaveform;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXSpectrum;
        private SeeSharpTools.JY.GUI.EasyButton btAnalysis;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDownSignalFreq;
        private System.Windows.Forms.NumericUpDown numericUpDownSampleRate;
        private System.Windows.Forms.NumericUpDown numericUpDownSampleLength;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBoxWindowType;
    }
}

