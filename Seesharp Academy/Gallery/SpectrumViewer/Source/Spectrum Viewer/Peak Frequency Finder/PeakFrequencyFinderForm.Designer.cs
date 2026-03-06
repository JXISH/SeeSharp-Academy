namespace Peak_Frequency_Finder
{
    partial class PeakFrequencyFinderForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            SeeSharpTools.JY.GUI.TabCursor tabCursor1 = new SeeSharpTools.JY.GUI.TabCursor();
            SeeSharpTools.JY.GUI.TabCursor tabCursor2 = new SeeSharpTools.JY.GUI.TabCursor();
            SeeSharpTools.JY.GUI.TabCursor tabCursor3 = new SeeSharpTools.JY.GUI.TabCursor();
            SeeSharpTools.JY.GUI.TabCursor tabCursor4 = new SeeSharpTools.JY.GUI.TabCursor();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.easyChartXTimeWaveform = new SeeSharpTools.JY.GUI.EasyChartX();
            this.easyChartXSpectrum = new SeeSharpTools.JY.GUI.EasyChartX();
            this.numericUpDownStartFreq = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownStopFreq = new System.Windows.Forms.NumericUpDown();
            this.buttonAnalysis = new System.Windows.Forms.Button();
            this.dgvResults = new System.Windows.Forms.DataGridView();
            this.colIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFrequency = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAmplitude = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.checkBoxYAxisIsLog = new System.Windows.Forms.CheckBox();
            this.numericUpDownRelativeThreshold = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numericUpDownMaxPeakNum = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartFreq)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStopFreq)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRelativeThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxPeakNum)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(12, 606);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(75, 23);
            this.buttonLoad.TabIndex = 3;
            this.buttonLoad.Text = "Load";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // easyChartXTimeWaveform
            // 
            this.easyChartXTimeWaveform.AutoClear = true;
            this.easyChartXTimeWaveform.AxisX.AutoScale = true;
            this.easyChartXTimeWaveform.AxisX.AutoZoomReset = false;
            this.easyChartXTimeWaveform.AxisX.Color = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisX.IsLogarithmic = false;
            this.easyChartXTimeWaveform.AxisX.LabelAngle = 0;
            this.easyChartXTimeWaveform.AxisX.LabelEnabled = true;
            this.easyChartXTimeWaveform.AxisX.LabelFormat = "";
            this.easyChartXTimeWaveform.AxisX.LogarithmBase = 10D;
            this.easyChartXTimeWaveform.AxisX.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisX.MajorGridEnabled = true;
            this.easyChartXTimeWaveform.AxisX.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXTimeWaveform.AxisX.Maximum = 10D;
            this.easyChartXTimeWaveform.AxisX.MinGridCountPerPixel = 0.01D;
            this.easyChartXTimeWaveform.AxisX.Minimum = 1D;
            this.easyChartXTimeWaveform.AxisX.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXTimeWaveform.AxisX.MinorGridEnabled = false;
            this.easyChartXTimeWaveform.AxisX.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXTimeWaveform.AxisX.ShowLogarithmicLines = false;
            this.easyChartXTimeWaveform.AxisX.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisX.TickWidth = 1F;
            this.easyChartXTimeWaveform.AxisX.Title = "";
            this.easyChartXTimeWaveform.AxisX.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXTimeWaveform.AxisX.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXTimeWaveform.AxisX.ViewMaximum = 10D;
            this.easyChartXTimeWaveform.AxisX.ViewMinimum = 1D;
            this.easyChartXTimeWaveform.AxisY.AutoScale = true;
            this.easyChartXTimeWaveform.AxisY.AutoZoomReset = false;
            this.easyChartXTimeWaveform.AxisY.Color = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisY.IsLogarithmic = false;
            this.easyChartXTimeWaveform.AxisY.LabelAngle = 0;
            this.easyChartXTimeWaveform.AxisY.LabelEnabled = true;
            this.easyChartXTimeWaveform.AxisY.LabelFormat = "";
            this.easyChartXTimeWaveform.AxisY.LogarithmBase = 10D;
            this.easyChartXTimeWaveform.AxisY.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisY.MajorGridEnabled = true;
            this.easyChartXTimeWaveform.AxisY.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXTimeWaveform.AxisY.Maximum = 10D;
            this.easyChartXTimeWaveform.AxisY.MinGridCountPerPixel = 0.01D;
            this.easyChartXTimeWaveform.AxisY.Minimum = 1D;
            this.easyChartXTimeWaveform.AxisY.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXTimeWaveform.AxisY.MinorGridEnabled = false;
            this.easyChartXTimeWaveform.AxisY.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXTimeWaveform.AxisY.ShowLogarithmicLines = false;
            this.easyChartXTimeWaveform.AxisY.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisY.TickWidth = 1F;
            this.easyChartXTimeWaveform.AxisY.Title = "";
            this.easyChartXTimeWaveform.AxisY.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXTimeWaveform.AxisY.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXTimeWaveform.AxisY.ViewMaximum = 10D;
            this.easyChartXTimeWaveform.AxisY.ViewMinimum = 1D;
            this.easyChartXTimeWaveform.AxisY2.AutoScale = true;
            this.easyChartXTimeWaveform.AxisY2.AutoZoomReset = false;
            this.easyChartXTimeWaveform.AxisY2.Color = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisY2.IsLogarithmic = false;
            this.easyChartXTimeWaveform.AxisY2.LabelAngle = 0;
            this.easyChartXTimeWaveform.AxisY2.LabelEnabled = true;
            this.easyChartXTimeWaveform.AxisY2.LabelFormat = "";
            this.easyChartXTimeWaveform.AxisY2.LogarithmBase = 10D;
            this.easyChartXTimeWaveform.AxisY2.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisY2.MajorGridEnabled = true;
            this.easyChartXTimeWaveform.AxisY2.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXTimeWaveform.AxisY2.Maximum = 10D;
            this.easyChartXTimeWaveform.AxisY2.MinGridCountPerPixel = 0.01D;
            this.easyChartXTimeWaveform.AxisY2.Minimum = 1D;
            this.easyChartXTimeWaveform.AxisY2.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXTimeWaveform.AxisY2.MinorGridEnabled = false;
            this.easyChartXTimeWaveform.AxisY2.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXTimeWaveform.AxisY2.ShowLogarithmicLines = false;
            this.easyChartXTimeWaveform.AxisY2.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.AxisY2.TickWidth = 1F;
            this.easyChartXTimeWaveform.AxisY2.Title = "";
            this.easyChartXTimeWaveform.AxisY2.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXTimeWaveform.AxisY2.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXTimeWaveform.AxisY2.ViewMaximum = 10D;
            this.easyChartXTimeWaveform.AxisY2.ViewMinimum = 1D;
            this.easyChartXTimeWaveform.BackColor = System.Drawing.Color.White;
            this.easyChartXTimeWaveform.ChartAreaBackColor = System.Drawing.Color.Empty;
            this.easyChartXTimeWaveform.GradientStyle = SeeSharpTools.JY.GUI.EasyChartX.ChartGradientStyle.None;
            this.easyChartXTimeWaveform.LegendBackColor = System.Drawing.Color.Transparent;
            this.easyChartXTimeWaveform.LegendFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.easyChartXTimeWaveform.LegendForeColor = System.Drawing.Color.Black;
            this.easyChartXTimeWaveform.LegendVisible = false;
            this.easyChartXTimeWaveform.Location = new System.Drawing.Point(12, 12);
            this.easyChartXTimeWaveform.Miscellaneous.CheckNegtiveOrZero = false;
            this.easyChartXTimeWaveform.Miscellaneous.DataStorage = SeeSharpTools.JY.GUI.DataStorageType.Clone;
            this.easyChartXTimeWaveform.Miscellaneous.DirectionChartCount = 3;
            this.easyChartXTimeWaveform.Miscellaneous.EnableSimplification = true;
            this.easyChartXTimeWaveform.Miscellaneous.MarkerSize = 7;
            this.easyChartXTimeWaveform.Miscellaneous.MaxSeriesCount = 32;
            this.easyChartXTimeWaveform.Miscellaneous.MaxSeriesPointCount = 4000;
            this.easyChartXTimeWaveform.Miscellaneous.ShowFunctionMenu = true;
            this.easyChartXTimeWaveform.Miscellaneous.SplitLayoutColumnInterval = 0F;
            this.easyChartXTimeWaveform.Miscellaneous.SplitLayoutDirection = SeeSharpTools.JY.GUI.EasyChartXUtility.LayoutDirection.LeftToRight;
            this.easyChartXTimeWaveform.Miscellaneous.SplitLayoutRowInterval = 0F;
            this.easyChartXTimeWaveform.Miscellaneous.SplitViewAutoLayout = true;
            this.easyChartXTimeWaveform.Name = "easyChartXTimeWaveform";
            this.easyChartXTimeWaveform.Series.Count = 0;
            this.easyChartXTimeWaveform.Size = new System.Drawing.Size(1241, 280);
            this.easyChartXTimeWaveform.SplitView = false;
            tabCursor1.Color = System.Drawing.Color.Cyan;
            tabCursor1.Direction = SeeSharpTools.JY.GUI.TabCursorUtility.TabCursorDirection.Vertical;
            tabCursor1.Enabled = true;
            tabCursor1.Name = "TabCursor1";
            tabCursor1.SeriesIndex = -1;
            tabCursor1.Value = 0D;
            tabCursor1.XValue = 0D;
            tabCursor1.YValue = double.NaN;
            tabCursor2.Color = System.Drawing.Color.Orange;
            tabCursor2.Direction = SeeSharpTools.JY.GUI.TabCursorUtility.TabCursorDirection.Vertical;
            tabCursor2.Enabled = true;
            tabCursor2.Name = "TabCursor2";
            tabCursor2.SeriesIndex = -1;
            tabCursor2.Value = 0D;
            tabCursor2.XValue = 0D;
            tabCursor2.YValue = double.NaN;
            this.easyChartXTimeWaveform.TabCursorContainer.Add(tabCursor1);
            this.easyChartXTimeWaveform.TabCursorContainer.Add(tabCursor2);
            this.easyChartXTimeWaveform.TabIndex = 2;
            this.easyChartXTimeWaveform.XCursor.AutoInterval = true;
            this.easyChartXTimeWaveform.XCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXTimeWaveform.XCursor.Interval = 0.001D;
            this.easyChartXTimeWaveform.XCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXTimeWaveform.XCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXTimeWaveform.XCursor.Value = double.NaN;
            this.easyChartXTimeWaveform.YCursor.AutoInterval = true;
            this.easyChartXTimeWaveform.YCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXTimeWaveform.YCursor.Interval = 0.001D;
            this.easyChartXTimeWaveform.YCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXTimeWaveform.YCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXTimeWaveform.YCursor.Value = double.NaN;
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
            this.easyChartXSpectrum.AxisY.IsLogarithmic = true;
            this.easyChartXSpectrum.AxisY.LabelAngle = 0;
            this.easyChartXSpectrum.AxisY.LabelEnabled = true;
            this.easyChartXSpectrum.AxisY.LabelFormat = "0.00e0";
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
            this.easyChartXSpectrum.AxisY.ShowLogarithmicLines = true;
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
            this.easyChartXSpectrum.BackColor = System.Drawing.Color.White;
            this.easyChartXSpectrum.ChartAreaBackColor = System.Drawing.Color.Empty;
            this.easyChartXSpectrum.GradientStyle = SeeSharpTools.JY.GUI.EasyChartX.ChartGradientStyle.None;
            this.easyChartXSpectrum.LegendBackColor = System.Drawing.Color.Transparent;
            this.easyChartXSpectrum.LegendFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.easyChartXSpectrum.LegendForeColor = System.Drawing.Color.Black;
            this.easyChartXSpectrum.LegendVisible = false;
            this.easyChartXSpectrum.Location = new System.Drawing.Point(12, 298);
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
            this.easyChartXSpectrum.Size = new System.Drawing.Size(1241, 280);
            this.easyChartXSpectrum.SplitView = false;
            tabCursor3.Color = System.Drawing.Color.Cyan;
            tabCursor3.Direction = SeeSharpTools.JY.GUI.TabCursorUtility.TabCursorDirection.Vertical;
            tabCursor3.Enabled = true;
            tabCursor3.Name = "TabCursor1";
            tabCursor3.SeriesIndex = -1;
            tabCursor3.Value = 0D;
            tabCursor3.XValue = 0D;
            tabCursor3.YValue = double.NaN;
            tabCursor4.Color = System.Drawing.Color.Orange;
            tabCursor4.Direction = SeeSharpTools.JY.GUI.TabCursorUtility.TabCursorDirection.Vertical;
            tabCursor4.Enabled = true;
            tabCursor4.Name = "TabCursor2";
            tabCursor4.SeriesIndex = -1;
            tabCursor4.Value = 0D;
            tabCursor4.XValue = 0D;
            tabCursor4.YValue = double.NaN;
            this.easyChartXSpectrum.TabCursorContainer.Add(tabCursor3);
            this.easyChartXSpectrum.TabCursorContainer.Add(tabCursor4);
            this.easyChartXSpectrum.TabIndex = 2;
            this.easyChartXSpectrum.XCursor.AutoInterval = true;
            this.easyChartXSpectrum.XCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXSpectrum.XCursor.Interval = 0.001D;
            this.easyChartXSpectrum.XCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXSpectrum.XCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXSpectrum.XCursor.Value = double.NaN;
            this.easyChartXSpectrum.YCursor.AutoInterval = true;
            this.easyChartXSpectrum.YCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXSpectrum.YCursor.Interval = 0.001D;
            this.easyChartXSpectrum.YCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXSpectrum.YCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXSpectrum.YCursor.Value = double.NaN;
            // 
            // numericUpDownStartFreq
            // 
            this.numericUpDownStartFreq.Location = new System.Drawing.Point(240, 608);
            this.numericUpDownStartFreq.Maximum = new decimal(new int[] {
            1410065408,
            2,
            0,
            0});
            this.numericUpDownStartFreq.Name = "numericUpDownStartFreq";
            this.numericUpDownStartFreq.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownStartFreq.TabIndex = 4;
            this.numericUpDownStartFreq.ThousandsSeparator = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(240, 593);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "Start Frequency";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(372, 593);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "Stop Frequency";
            // 
            // numericUpDownStopFreq
            // 
            this.numericUpDownStopFreq.Location = new System.Drawing.Point(372, 608);
            this.numericUpDownStopFreq.Maximum = new decimal(new int[] {
            1410065408,
            2,
            0,
            0});
            this.numericUpDownStopFreq.Name = "numericUpDownStopFreq";
            this.numericUpDownStopFreq.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownStopFreq.TabIndex = 4;
            this.numericUpDownStopFreq.ThousandsSeparator = true;
            // 
            // buttonAnalysis
            // 
            this.buttonAnalysis.Location = new System.Drawing.Point(804, 606);
            this.buttonAnalysis.Name = "buttonAnalysis";
            this.buttonAnalysis.Size = new System.Drawing.Size(75, 23);
            this.buttonAnalysis.TabIndex = 3;
            this.buttonAnalysis.Text = "Analysis";
            this.buttonAnalysis.UseVisualStyleBackColor = true;
            this.buttonAnalysis.Click += new System.EventHandler(this.buttonAnalysis_Click);
            // 
            // dgvResults
            // 
            this.dgvResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colIndex,
            this.colFrequency,
            this.colAmplitude});
            this.dgvResults.Location = new System.Drawing.Point(1259, 12);
            this.dgvResults.Name = "dgvResults";
            this.dgvResults.RowHeadersVisible = false;
            this.dgvResults.RowTemplate.Height = 23;
            this.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.Size = new System.Drawing.Size(273, 619);
            this.dgvResults.TabIndex = 6;
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
            this.colFrequency.HeaderText = "Frequency";
            this.colFrequency.Name = "colFrequency";
            this.colFrequency.ReadOnly = true;
            // 
            // colAmplitude
            // 
            this.colAmplitude.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colAmplitude.HeaderText = "Amplitude";
            this.colAmplitude.Name = "colAmplitude";
            this.colAmplitude.ReadOnly = true;
            // 
            // checkBoxYAxisIsLog
            // 
            this.checkBoxYAxisIsLog.AutoSize = true;
            this.checkBoxYAxisIsLog.Checked = true;
            this.checkBoxYAxisIsLog.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxYAxisIsLog.Location = new System.Drawing.Point(12, 298);
            this.checkBoxYAxisIsLog.Name = "checkBoxYAxisIsLog";
            this.checkBoxYAxisIsLog.Size = new System.Drawing.Size(42, 16);
            this.checkBoxYAxisIsLog.TabIndex = 7;
            this.checkBoxYAxisIsLog.Text = "Log";
            this.checkBoxYAxisIsLog.UseVisualStyleBackColor = true;
            this.checkBoxYAxisIsLog.CheckedChanged += new System.EventHandler(this.checkBoxYAxisIsLog_CheckedChanged);
            // 
            // numericUpDownRelativeThreshold
            // 
            this.numericUpDownRelativeThreshold.Cursor = System.Windows.Forms.Cursors.Default;
            this.numericUpDownRelativeThreshold.DecimalPlaces = 3;
            this.numericUpDownRelativeThreshold.Location = new System.Drawing.Point(644, 608);
            this.numericUpDownRelativeThreshold.Maximum = new decimal(new int[] {
            1410065408,
            2,
            0,
            0});
            this.numericUpDownRelativeThreshold.Name = "numericUpDownRelativeThreshold";
            this.numericUpDownRelativeThreshold.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownRelativeThreshold.TabIndex = 4;
            this.numericUpDownRelativeThreshold.ThousandsSeparator = true;
            this.numericUpDownRelativeThreshold.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(642, 593);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 12);
            this.label3.TabIndex = 5;
            this.label3.Text = "Relative Threshold";
            // 
            // numericUpDownMaxPeakNum
            // 
            this.numericUpDownMaxPeakNum.Cursor = System.Windows.Forms.Cursors.Default;
            this.numericUpDownMaxPeakNum.Location = new System.Drawing.Point(498, 609);
            this.numericUpDownMaxPeakNum.Maximum = new decimal(new int[] {
            1410065408,
            2,
            0,
            0});
            this.numericUpDownMaxPeakNum.Name = "numericUpDownMaxPeakNum";
            this.numericUpDownMaxPeakNum.Size = new System.Drawing.Size(120, 21);
            this.numericUpDownMaxPeakNum.TabIndex = 4;
            this.numericUpDownMaxPeakNum.ThousandsSeparator = true;
            this.numericUpDownMaxPeakNum.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(496, 594);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(95, 12);
            this.label4.TabIndex = 5;
            this.label4.Text = "Max Peak Number";
            // 
            // PeakFrequencyFinderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1544, 641);
            this.Controls.Add(this.checkBoxYAxisIsLog);
            this.Controls.Add(this.dgvResults);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDownMaxPeakNum);
            this.Controls.Add(this.numericUpDownRelativeThreshold);
            this.Controls.Add(this.numericUpDownStopFreq);
            this.Controls.Add(this.numericUpDownStartFreq);
            this.Controls.Add(this.buttonAnalysis);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.easyChartXSpectrum);
            this.Controls.Add(this.easyChartXTimeWaveform);
            this.Name = "PeakFrequencyFinderForm";
            this.Text = "Peak Frequency Finder";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartFreq)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStopFreq)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResults)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownRelativeThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxPeakNum)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonLoad;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXTimeWaveform;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXSpectrum;
        private System.Windows.Forms.NumericUpDown numericUpDownStartFreq;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDownStopFreq;
        private System.Windows.Forms.Button buttonAnalysis;
        private System.Windows.Forms.DataGridView dgvResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIndex;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFrequency;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAmplitude;
        private System.Windows.Forms.CheckBox checkBoxYAxisIsLog;
        private System.Windows.Forms.NumericUpDown numericUpDownRelativeThreshold;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxPeakNum;
        private System.Windows.Forms.Label label4;
    }
}

