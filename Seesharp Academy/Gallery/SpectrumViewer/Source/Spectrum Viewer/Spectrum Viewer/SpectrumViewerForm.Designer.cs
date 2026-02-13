namespace Spectrum_Viewer
{
    partial class SpectrumViewerForm
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
            SeeSharpTools.JY.GUI.TabCursor tabCursor9 = new SeeSharpTools.JY.GUI.TabCursor();
            SeeSharpTools.JY.GUI.TabCursor tabCursor10 = new SeeSharpTools.JY.GUI.TabCursor();
            this.easyChartXTimeWaveform = new SeeSharpTools.JY.GUI.EasyChartX();
            this.easyChartXWaveformSection = new SeeSharpTools.JY.GUI.EasyChartX();
            this.easyChartXSectionSpectrum = new SeeSharpTools.JY.GUI.EasyChartX();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.buttonJTFA = new System.Windows.Forms.Button();
            this.numericUpDownSampleRate = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownWindowLength = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.checkBoxSpectrumDB = new System.Windows.Forms.CheckBox();
            this.checkBoxCepstrum = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.numericUpDownJTFAWinLength = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownJTFAStep = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownStartTime = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownDurationSec = new System.Windows.Forms.NumericUpDown();
            this.groupBoxSpectrumSettings = new System.Windows.Forms.GroupBox();
            this.groupBoxJTFASettgins = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownWindowLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownJTFAWinLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownJTFAStep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDurationSec)).BeginInit();
            this.groupBoxSpectrumSettings.SuspendLayout();
            this.groupBoxJTFASettgins.SuspendLayout();
            this.SuspendLayout();
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
            this.easyChartXTimeWaveform.Size = new System.Drawing.Size(531, 200);
            this.easyChartXTimeWaveform.SplitView = false;
            tabCursor9.Color = System.Drawing.Color.Cyan;
            tabCursor9.Direction = SeeSharpTools.JY.GUI.TabCursorUtility.TabCursorDirection.Vertical;
            tabCursor9.Enabled = true;
            tabCursor9.Name = "TabCursor1";
            tabCursor9.SeriesIndex = -1;
            tabCursor9.Value = 0D;
            tabCursor9.XValue = 0D;
            tabCursor9.YValue = double.NaN;
            tabCursor10.Color = System.Drawing.Color.Orange;
            tabCursor10.Direction = SeeSharpTools.JY.GUI.TabCursorUtility.TabCursorDirection.Vertical;
            tabCursor10.Enabled = true;
            tabCursor10.Name = "TabCursor2";
            tabCursor10.SeriesIndex = -1;
            tabCursor10.Value = 0D;
            tabCursor10.XValue = 0D;
            tabCursor10.YValue = double.NaN;
            this.easyChartXTimeWaveform.TabCursorContainer.Add(tabCursor9);
            this.easyChartXTimeWaveform.TabCursorContainer.Add(tabCursor10);
            this.easyChartXTimeWaveform.TabIndex = 0;
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
            this.easyChartXTimeWaveform.TabCursorChanged += new SeeSharpTools.JY.GUI.EasyChartX.TabCursorEvent(this.easyChartXTimeWaveform_TabCursorChanged);
            // 
            // easyChartXWaveformSection
            // 
            this.easyChartXWaveformSection.AutoClear = true;
            this.easyChartXWaveformSection.AxisX.AutoScale = true;
            this.easyChartXWaveformSection.AxisX.AutoZoomReset = false;
            this.easyChartXWaveformSection.AxisX.Color = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisX.IsLogarithmic = false;
            this.easyChartXWaveformSection.AxisX.LabelAngle = 0;
            this.easyChartXWaveformSection.AxisX.LabelEnabled = true;
            this.easyChartXWaveformSection.AxisX.LabelFormat = "";
            this.easyChartXWaveformSection.AxisX.LogarithmBase = 10D;
            this.easyChartXWaveformSection.AxisX.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisX.MajorGridEnabled = true;
            this.easyChartXWaveformSection.AxisX.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXWaveformSection.AxisX.Maximum = 10D;
            this.easyChartXWaveformSection.AxisX.MinGridCountPerPixel = 0.01D;
            this.easyChartXWaveformSection.AxisX.Minimum = 1D;
            this.easyChartXWaveformSection.AxisX.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXWaveformSection.AxisX.MinorGridEnabled = false;
            this.easyChartXWaveformSection.AxisX.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXWaveformSection.AxisX.ShowLogarithmicLines = false;
            this.easyChartXWaveformSection.AxisX.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisX.TickWidth = 1F;
            this.easyChartXWaveformSection.AxisX.Title = "";
            this.easyChartXWaveformSection.AxisX.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXWaveformSection.AxisX.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXWaveformSection.AxisX.ViewMaximum = 10D;
            this.easyChartXWaveformSection.AxisX.ViewMinimum = 1D;
            this.easyChartXWaveformSection.AxisY.AutoScale = true;
            this.easyChartXWaveformSection.AxisY.AutoZoomReset = false;
            this.easyChartXWaveformSection.AxisY.Color = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisY.IsLogarithmic = false;
            this.easyChartXWaveformSection.AxisY.LabelAngle = 0;
            this.easyChartXWaveformSection.AxisY.LabelEnabled = true;
            this.easyChartXWaveformSection.AxisY.LabelFormat = "";
            this.easyChartXWaveformSection.AxisY.LogarithmBase = 10D;
            this.easyChartXWaveformSection.AxisY.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisY.MajorGridEnabled = true;
            this.easyChartXWaveformSection.AxisY.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXWaveformSection.AxisY.Maximum = 10D;
            this.easyChartXWaveformSection.AxisY.MinGridCountPerPixel = 0.01D;
            this.easyChartXWaveformSection.AxisY.Minimum = 1D;
            this.easyChartXWaveformSection.AxisY.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXWaveformSection.AxisY.MinorGridEnabled = false;
            this.easyChartXWaveformSection.AxisY.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXWaveformSection.AxisY.ShowLogarithmicLines = false;
            this.easyChartXWaveformSection.AxisY.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisY.TickWidth = 1F;
            this.easyChartXWaveformSection.AxisY.Title = "";
            this.easyChartXWaveformSection.AxisY.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXWaveformSection.AxisY.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXWaveformSection.AxisY.ViewMaximum = 10D;
            this.easyChartXWaveformSection.AxisY.ViewMinimum = 1D;
            this.easyChartXWaveformSection.AxisY2.AutoScale = true;
            this.easyChartXWaveformSection.AxisY2.AutoZoomReset = false;
            this.easyChartXWaveformSection.AxisY2.Color = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisY2.IsLogarithmic = false;
            this.easyChartXWaveformSection.AxisY2.LabelAngle = 0;
            this.easyChartXWaveformSection.AxisY2.LabelEnabled = true;
            this.easyChartXWaveformSection.AxisY2.LabelFormat = "";
            this.easyChartXWaveformSection.AxisY2.LogarithmBase = 10D;
            this.easyChartXWaveformSection.AxisY2.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisY2.MajorGridEnabled = true;
            this.easyChartXWaveformSection.AxisY2.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXWaveformSection.AxisY2.Maximum = 10D;
            this.easyChartXWaveformSection.AxisY2.MinGridCountPerPixel = 0.01D;
            this.easyChartXWaveformSection.AxisY2.Minimum = 1D;
            this.easyChartXWaveformSection.AxisY2.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXWaveformSection.AxisY2.MinorGridEnabled = false;
            this.easyChartXWaveformSection.AxisY2.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXWaveformSection.AxisY2.ShowLogarithmicLines = false;
            this.easyChartXWaveformSection.AxisY2.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.AxisY2.TickWidth = 1F;
            this.easyChartXWaveformSection.AxisY2.Title = "";
            this.easyChartXWaveformSection.AxisY2.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXWaveformSection.AxisY2.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXWaveformSection.AxisY2.ViewMaximum = 10D;
            this.easyChartXWaveformSection.AxisY2.ViewMinimum = 1D;
            this.easyChartXWaveformSection.ChartAreaBackColor = System.Drawing.Color.Empty;
            this.easyChartXWaveformSection.GradientStyle = SeeSharpTools.JY.GUI.EasyChartX.ChartGradientStyle.None;
            this.easyChartXWaveformSection.LegendBackColor = System.Drawing.Color.Transparent;
            this.easyChartXWaveformSection.LegendFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.easyChartXWaveformSection.LegendForeColor = System.Drawing.Color.Black;
            this.easyChartXWaveformSection.LegendVisible = false;
            this.easyChartXWaveformSection.Location = new System.Drawing.Point(549, 12);
            this.easyChartXWaveformSection.Miscellaneous.CheckNegtiveOrZero = false;
            this.easyChartXWaveformSection.Miscellaneous.DataStorage = SeeSharpTools.JY.GUI.DataStorageType.Clone;
            this.easyChartXWaveformSection.Miscellaneous.DirectionChartCount = 3;
            this.easyChartXWaveformSection.Miscellaneous.EnableSimplification = true;
            this.easyChartXWaveformSection.Miscellaneous.MarkerSize = 7;
            this.easyChartXWaveformSection.Miscellaneous.MaxSeriesCount = 32;
            this.easyChartXWaveformSection.Miscellaneous.MaxSeriesPointCount = 4000;
            this.easyChartXWaveformSection.Miscellaneous.ShowFunctionMenu = true;
            this.easyChartXWaveformSection.Miscellaneous.SplitLayoutColumnInterval = 0F;
            this.easyChartXWaveformSection.Miscellaneous.SplitLayoutDirection = SeeSharpTools.JY.GUI.EasyChartXUtility.LayoutDirection.LeftToRight;
            this.easyChartXWaveformSection.Miscellaneous.SplitLayoutRowInterval = 0F;
            this.easyChartXWaveformSection.Miscellaneous.SplitViewAutoLayout = true;
            this.easyChartXWaveformSection.Name = "easyChartXWaveformSection";
            this.easyChartXWaveformSection.Series.Count = 0;
            this.easyChartXWaveformSection.Size = new System.Drawing.Size(269, 200);
            this.easyChartXWaveformSection.SplitView = false;
            this.easyChartXWaveformSection.TabIndex = 0;
            this.easyChartXWaveformSection.XCursor.AutoInterval = true;
            this.easyChartXWaveformSection.XCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXWaveformSection.XCursor.Interval = 0.001D;
            this.easyChartXWaveformSection.XCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Zoom;
            this.easyChartXWaveformSection.XCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXWaveformSection.XCursor.Value = double.NaN;
            this.easyChartXWaveformSection.YCursor.AutoInterval = true;
            this.easyChartXWaveformSection.YCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXWaveformSection.YCursor.Interval = 0.001D;
            this.easyChartXWaveformSection.YCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXWaveformSection.YCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXWaveformSection.YCursor.Value = double.NaN;
            // 
            // easyChartXSectionSpectrum
            // 
            this.easyChartXSectionSpectrum.AutoClear = true;
            this.easyChartXSectionSpectrum.AxisX.AutoScale = true;
            this.easyChartXSectionSpectrum.AxisX.AutoZoomReset = false;
            this.easyChartXSectionSpectrum.AxisX.Color = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisX.IsLogarithmic = false;
            this.easyChartXSectionSpectrum.AxisX.LabelAngle = 0;
            this.easyChartXSectionSpectrum.AxisX.LabelEnabled = true;
            this.easyChartXSectionSpectrum.AxisX.LabelFormat = "";
            this.easyChartXSectionSpectrum.AxisX.LogarithmBase = 10D;
            this.easyChartXSectionSpectrum.AxisX.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisX.MajorGridEnabled = true;
            this.easyChartXSectionSpectrum.AxisX.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXSectionSpectrum.AxisX.Maximum = 10D;
            this.easyChartXSectionSpectrum.AxisX.MinGridCountPerPixel = 0.01D;
            this.easyChartXSectionSpectrum.AxisX.Minimum = 1D;
            this.easyChartXSectionSpectrum.AxisX.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXSectionSpectrum.AxisX.MinorGridEnabled = false;
            this.easyChartXSectionSpectrum.AxisX.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXSectionSpectrum.AxisX.ShowLogarithmicLines = false;
            this.easyChartXSectionSpectrum.AxisX.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisX.TickWidth = 1F;
            this.easyChartXSectionSpectrum.AxisX.Title = "";
            this.easyChartXSectionSpectrum.AxisX.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXSectionSpectrum.AxisX.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXSectionSpectrum.AxisX.ViewMaximum = 10D;
            this.easyChartXSectionSpectrum.AxisX.ViewMinimum = 1D;
            this.easyChartXSectionSpectrum.AxisY.AutoScale = true;
            this.easyChartXSectionSpectrum.AxisY.AutoZoomReset = false;
            this.easyChartXSectionSpectrum.AxisY.Color = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisY.IsLogarithmic = false;
            this.easyChartXSectionSpectrum.AxisY.LabelAngle = 0;
            this.easyChartXSectionSpectrum.AxisY.LabelEnabled = true;
            this.easyChartXSectionSpectrum.AxisY.LabelFormat = "";
            this.easyChartXSectionSpectrum.AxisY.LogarithmBase = 10D;
            this.easyChartXSectionSpectrum.AxisY.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisY.MajorGridEnabled = true;
            this.easyChartXSectionSpectrum.AxisY.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXSectionSpectrum.AxisY.Maximum = 10D;
            this.easyChartXSectionSpectrum.AxisY.MinGridCountPerPixel = 0.01D;
            this.easyChartXSectionSpectrum.AxisY.Minimum = 1D;
            this.easyChartXSectionSpectrum.AxisY.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXSectionSpectrum.AxisY.MinorGridEnabled = false;
            this.easyChartXSectionSpectrum.AxisY.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXSectionSpectrum.AxisY.ShowLogarithmicLines = false;
            this.easyChartXSectionSpectrum.AxisY.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisY.TickWidth = 1F;
            this.easyChartXSectionSpectrum.AxisY.Title = "";
            this.easyChartXSectionSpectrum.AxisY.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXSectionSpectrum.AxisY.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXSectionSpectrum.AxisY.ViewMaximum = 10D;
            this.easyChartXSectionSpectrum.AxisY.ViewMinimum = 1D;
            this.easyChartXSectionSpectrum.AxisY2.AutoScale = true;
            this.easyChartXSectionSpectrum.AxisY2.AutoZoomReset = false;
            this.easyChartXSectionSpectrum.AxisY2.Color = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisY2.IsLogarithmic = false;
            this.easyChartXSectionSpectrum.AxisY2.LabelAngle = 0;
            this.easyChartXSectionSpectrum.AxisY2.LabelEnabled = true;
            this.easyChartXSectionSpectrum.AxisY2.LabelFormat = "";
            this.easyChartXSectionSpectrum.AxisY2.LogarithmBase = 10D;
            this.easyChartXSectionSpectrum.AxisY2.MajorGridColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisY2.MajorGridEnabled = true;
            this.easyChartXSectionSpectrum.AxisY2.MajorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.Dash;
            this.easyChartXSectionSpectrum.AxisY2.Maximum = 10D;
            this.easyChartXSectionSpectrum.AxisY2.MinGridCountPerPixel = 0.01D;
            this.easyChartXSectionSpectrum.AxisY2.Minimum = 1D;
            this.easyChartXSectionSpectrum.AxisY2.MinorGridColor = System.Drawing.Color.DimGray;
            this.easyChartXSectionSpectrum.AxisY2.MinorGridEnabled = false;
            this.easyChartXSectionSpectrum.AxisY2.MinorGridType = SeeSharpTools.JY.GUI.EasyChartXAxis.GridStyle.DashDot;
            this.easyChartXSectionSpectrum.AxisY2.ShowLogarithmicLines = false;
            this.easyChartXSectionSpectrum.AxisY2.TickLineColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.AxisY2.TickWidth = 1F;
            this.easyChartXSectionSpectrum.AxisY2.Title = "";
            this.easyChartXSectionSpectrum.AxisY2.TitleOrientation = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextOrientation.Auto;
            this.easyChartXSectionSpectrum.AxisY2.TitlePosition = SeeSharpTools.JY.GUI.EasyChartXAxis.AxisTextPosition.Center;
            this.easyChartXSectionSpectrum.AxisY2.ViewMaximum = 10D;
            this.easyChartXSectionSpectrum.AxisY2.ViewMinimum = 1D;
            this.easyChartXSectionSpectrum.ChartAreaBackColor = System.Drawing.Color.Empty;
            this.easyChartXSectionSpectrum.GradientStyle = SeeSharpTools.JY.GUI.EasyChartX.ChartGradientStyle.None;
            this.easyChartXSectionSpectrum.LegendBackColor = System.Drawing.Color.Transparent;
            this.easyChartXSectionSpectrum.LegendFont = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.easyChartXSectionSpectrum.LegendForeColor = System.Drawing.Color.Black;
            this.easyChartXSectionSpectrum.LegendVisible = false;
            this.easyChartXSectionSpectrum.Location = new System.Drawing.Point(12, 226);
            this.easyChartXSectionSpectrum.Miscellaneous.CheckNegtiveOrZero = false;
            this.easyChartXSectionSpectrum.Miscellaneous.DataStorage = SeeSharpTools.JY.GUI.DataStorageType.Clone;
            this.easyChartXSectionSpectrum.Miscellaneous.DirectionChartCount = 3;
            this.easyChartXSectionSpectrum.Miscellaneous.EnableSimplification = true;
            this.easyChartXSectionSpectrum.Miscellaneous.MarkerSize = 7;
            this.easyChartXSectionSpectrum.Miscellaneous.MaxSeriesCount = 32;
            this.easyChartXSectionSpectrum.Miscellaneous.MaxSeriesPointCount = 4000;
            this.easyChartXSectionSpectrum.Miscellaneous.ShowFunctionMenu = true;
            this.easyChartXSectionSpectrum.Miscellaneous.SplitLayoutColumnInterval = 0F;
            this.easyChartXSectionSpectrum.Miscellaneous.SplitLayoutDirection = SeeSharpTools.JY.GUI.EasyChartXUtility.LayoutDirection.LeftToRight;
            this.easyChartXSectionSpectrum.Miscellaneous.SplitLayoutRowInterval = 0F;
            this.easyChartXSectionSpectrum.Miscellaneous.SplitViewAutoLayout = true;
            this.easyChartXSectionSpectrum.Name = "easyChartXSectionSpectrum";
            this.easyChartXSectionSpectrum.Series.Count = 0;
            this.easyChartXSectionSpectrum.Size = new System.Drawing.Size(806, 200);
            this.easyChartXSectionSpectrum.SplitView = false;
            this.easyChartXSectionSpectrum.TabIndex = 0;
            this.easyChartXSectionSpectrum.XCursor.AutoInterval = true;
            this.easyChartXSectionSpectrum.XCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXSectionSpectrum.XCursor.Interval = 0.001D;
            this.easyChartXSectionSpectrum.XCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Zoom;
            this.easyChartXSectionSpectrum.XCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXSectionSpectrum.XCursor.Value = double.NaN;
            this.easyChartXSectionSpectrum.YCursor.AutoInterval = true;
            this.easyChartXSectionSpectrum.YCursor.Color = System.Drawing.Color.DeepSkyBlue;
            this.easyChartXSectionSpectrum.YCursor.Interval = 0.001D;
            this.easyChartXSectionSpectrum.YCursor.Mode = SeeSharpTools.JY.GUI.EasyChartXCursor.CursorMode.Disabled;
            this.easyChartXSectionSpectrum.YCursor.SelectionColor = System.Drawing.Color.LightGray;
            this.easyChartXSectionSpectrum.YCursor.Value = double.NaN;
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(892, 12);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(75, 23);
            this.buttonLoad.TabIndex = 1;
            this.buttonLoad.Text = "Load";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // buttonJTFA
            // 
            this.buttonJTFA.Location = new System.Drawing.Point(892, 362);
            this.buttonJTFA.Name = "buttonJTFA";
            this.buttonJTFA.Size = new System.Drawing.Size(75, 23);
            this.buttonJTFA.TabIndex = 1;
            this.buttonJTFA.Text = "JTFA";
            this.buttonJTFA.UseVisualStyleBackColor = true;
            this.buttonJTFA.Click += new System.EventHandler(this.buttonJTFA_Click);
            // 
            // numericUpDownSampleRate
            // 
            this.numericUpDownSampleRate.Location = new System.Drawing.Point(119, 15);
            this.numericUpDownSampleRate.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numericUpDownSampleRate.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numericUpDownSampleRate.Name = "numericUpDownSampleRate";
            this.numericUpDownSampleRate.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownSampleRate.TabIndex = 2;
            this.numericUpDownSampleRate.ThousandsSeparator = true;
            this.numericUpDownSampleRate.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDownSampleRate.ValueChanged += new System.EventHandler(this.numericUpDownSampleRate_ValueChanged);
            // 
            // numericUpDownWindowLength
            // 
            this.numericUpDownWindowLength.Location = new System.Drawing.Point(119, 42);
            this.numericUpDownWindowLength.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDownWindowLength.Name = "numericUpDownWindowLength";
            this.numericUpDownWindowLength.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownWindowLength.TabIndex = 2;
            this.numericUpDownWindowLength.ThousandsSeparator = true;
            this.numericUpDownWindowLength.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.numericUpDownWindowLength.ValueChanged += new System.EventHandler(this.numericUpDownWindowLength_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "Sample Rate (S/s)";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Window Length";
            // 
            // checkBoxSpectrumDB
            // 
            this.checkBoxSpectrumDB.AutoSize = true;
            this.checkBoxSpectrumDB.Checked = true;
            this.checkBoxSpectrumDB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSpectrumDB.Location = new System.Drawing.Point(8, 69);
            this.checkBoxSpectrumDB.Name = "checkBoxSpectrumDB";
            this.checkBoxSpectrumDB.Size = new System.Drawing.Size(90, 16);
            this.checkBoxSpectrumDB.TabIndex = 4;
            this.checkBoxSpectrumDB.Text = "Spectrum dB";
            this.checkBoxSpectrumDB.UseVisualStyleBackColor = true;
            this.checkBoxSpectrumDB.CheckedChanged += new System.EventHandler(this.checkBoxSpectrumDB_CheckedChanged);
            // 
            // checkBoxCepstrum
            // 
            this.checkBoxCepstrum.AutoSize = true;
            this.checkBoxCepstrum.Location = new System.Drawing.Point(119, 69);
            this.checkBoxCepstrum.Name = "checkBoxCepstrum";
            this.checkBoxCepstrum.Size = new System.Drawing.Size(72, 16);
            this.checkBoxCepstrum.TabIndex = 4;
            this.checkBoxCepstrum.Text = "Cepstrum";
            this.checkBoxCepstrum.UseVisualStyleBackColor = true;
            this.checkBoxCepstrum.CheckedChanged += new System.EventHandler(this.checkBoxCepstrum_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "Start Time (s)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 76);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "Window Length";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 103);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(71, 12);
            this.label5.TabIndex = 3;
            this.label5.Text = "Window Step";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 49);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(77, 12);
            this.label6.TabIndex = 3;
            this.label6.Text = "Duration (s)";
            // 
            // numericUpDownJTFAWinLength
            // 
            this.numericUpDownJTFAWinLength.Location = new System.Drawing.Point(119, 74);
            this.numericUpDownJTFAWinLength.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDownJTFAWinLength.Name = "numericUpDownJTFAWinLength";
            this.numericUpDownJTFAWinLength.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownJTFAWinLength.TabIndex = 2;
            this.numericUpDownJTFAWinLength.ThousandsSeparator = true;
            this.numericUpDownJTFAWinLength.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.numericUpDownJTFAWinLength.ValueChanged += new System.EventHandler(this.numericUpDownWindowLength_ValueChanged);
            // 
            // numericUpDownJTFAStep
            // 
            this.numericUpDownJTFAStep.Location = new System.Drawing.Point(119, 101);
            this.numericUpDownJTFAStep.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDownJTFAStep.Name = "numericUpDownJTFAStep";
            this.numericUpDownJTFAStep.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownJTFAStep.TabIndex = 2;
            this.numericUpDownJTFAStep.ThousandsSeparator = true;
            this.numericUpDownJTFAStep.Value = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numericUpDownJTFAStep.ValueChanged += new System.EventHandler(this.numericUpDownWindowLength_ValueChanged);
            // 
            // numericUpDownStartTime
            // 
            this.numericUpDownStartTime.DecimalPlaces = 3;
            this.numericUpDownStartTime.Location = new System.Drawing.Point(119, 20);
            this.numericUpDownStartTime.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDownStartTime.Name = "numericUpDownStartTime";
            this.numericUpDownStartTime.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownStartTime.TabIndex = 2;
            this.numericUpDownStartTime.ThousandsSeparator = true;
            this.numericUpDownStartTime.ValueChanged += new System.EventHandler(this.numericUpDownWindowLength_ValueChanged);
            // 
            // numericUpDownDurationSec
            // 
            this.numericUpDownDurationSec.DecimalPlaces = 3;
            this.numericUpDownDurationSec.Location = new System.Drawing.Point(119, 47);
            this.numericUpDownDurationSec.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numericUpDownDurationSec.Name = "numericUpDownDurationSec";
            this.numericUpDownDurationSec.Size = new System.Drawing.Size(75, 21);
            this.numericUpDownDurationSec.TabIndex = 2;
            this.numericUpDownDurationSec.ThousandsSeparator = true;
            this.numericUpDownDurationSec.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownDurationSec.ValueChanged += new System.EventHandler(this.numericUpDownWindowLength_ValueChanged);
            // 
            // groupBoxSpectrumSettings
            // 
            this.groupBoxSpectrumSettings.Controls.Add(this.label1);
            this.groupBoxSpectrumSettings.Controls.Add(this.numericUpDownSampleRate);
            this.groupBoxSpectrumSettings.Controls.Add(this.checkBoxCepstrum);
            this.groupBoxSpectrumSettings.Controls.Add(this.label2);
            this.groupBoxSpectrumSettings.Controls.Add(this.checkBoxSpectrumDB);
            this.groupBoxSpectrumSettings.Controls.Add(this.numericUpDownWindowLength);
            this.groupBoxSpectrumSettings.Location = new System.Drawing.Point(824, 53);
            this.groupBoxSpectrumSettings.Name = "groupBoxSpectrumSettings";
            this.groupBoxSpectrumSettings.Size = new System.Drawing.Size(203, 97);
            this.groupBoxSpectrumSettings.TabIndex = 5;
            this.groupBoxSpectrumSettings.TabStop = false;
            this.groupBoxSpectrumSettings.Text = "Spectrum Settings";
            // 
            // groupBoxJTFASettgins
            // 
            this.groupBoxJTFASettgins.Controls.Add(this.numericUpDownStartTime);
            this.groupBoxJTFASettgins.Controls.Add(this.label3);
            this.groupBoxJTFASettgins.Controls.Add(this.label5);
            this.groupBoxJTFASettgins.Controls.Add(this.label6);
            this.groupBoxJTFASettgins.Controls.Add(this.label4);
            this.groupBoxJTFASettgins.Controls.Add(this.numericUpDownDurationSec);
            this.groupBoxJTFASettgins.Controls.Add(this.numericUpDownJTFAStep);
            this.groupBoxJTFASettgins.Controls.Add(this.numericUpDownJTFAWinLength);
            this.groupBoxJTFASettgins.Location = new System.Drawing.Point(824, 226);
            this.groupBoxJTFASettgins.Name = "groupBoxJTFASettgins";
            this.groupBoxJTFASettgins.Size = new System.Drawing.Size(203, 130);
            this.groupBoxJTFASettgins.TabIndex = 5;
            this.groupBoxJTFASettgins.TabStop = false;
            this.groupBoxJTFASettgins.Text = "JTFA Settings";
            // 
            // SpectrumViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1038, 440);
            this.Controls.Add(this.groupBoxJTFASettgins);
            this.Controls.Add(this.groupBoxSpectrumSettings);
            this.Controls.Add(this.buttonJTFA);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.easyChartXSectionSpectrum);
            this.Controls.Add(this.easyChartXWaveformSection);
            this.Controls.Add(this.easyChartXTimeWaveform);
            this.Name = "SpectrumViewerForm";
            this.Text = "Spectrum Viewer";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownSampleRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownWindowLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownJTFAWinLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownJTFAStep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownStartTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDurationSec)).EndInit();
            this.groupBoxSpectrumSettings.ResumeLayout(false);
            this.groupBoxSpectrumSettings.PerformLayout();
            this.groupBoxJTFASettgins.ResumeLayout(false);
            this.groupBoxJTFASettgins.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private SeeSharpTools.JY.GUI.EasyChartX easyChartXTimeWaveform;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXWaveformSection;
        private SeeSharpTools.JY.GUI.EasyChartX easyChartXSectionSpectrum;
        private System.Windows.Forms.Button buttonLoad;
        private System.Windows.Forms.Button buttonJTFA;
        private System.Windows.Forms.NumericUpDown numericUpDownSampleRate;
        private System.Windows.Forms.NumericUpDown numericUpDownWindowLength;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBoxSpectrumDB;
        private System.Windows.Forms.CheckBox checkBoxCepstrum;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numericUpDownJTFAWinLength;
        private System.Windows.Forms.NumericUpDown numericUpDownJTFAStep;
        private System.Windows.Forms.NumericUpDown numericUpDownStartTime;
        private System.Windows.Forms.NumericUpDown numericUpDownDurationSec;
        private System.Windows.Forms.GroupBox groupBoxSpectrumSettings;
        private System.Windows.Forms.GroupBox groupBoxJTFASettgins;
    }
}

