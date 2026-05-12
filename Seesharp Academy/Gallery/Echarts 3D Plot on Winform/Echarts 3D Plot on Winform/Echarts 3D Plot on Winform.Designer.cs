namespace Echarts_3D_Plot_on_Winform
{
    partial class FormEcharts3DPlot
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
            this.webView23DSurface = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.buttonShow3DSurface = new System.Windows.Forms.Button();
            this.webView23DBar = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.buttonDisplay3DBar = new System.Windows.Forms.Button();
            this.numericUpDownTransparency = new System.Windows.Forms.NumericUpDown();
            this.webView23DWaterfall = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.buttonShowWaterfall = new System.Windows.Forms.Button();
            this.checkBoxDB = new System.Windows.Forms.CheckBox();
            this.checkBoxFilter = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.webView23DSurface)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView23DBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTransparency)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView23DWaterfall)).BeginInit();
            this.SuspendLayout();
            // 
            // webView23DSurface
            // 
            this.webView23DSurface.AllowExternalDrop = true;
            this.webView23DSurface.CreationProperties = null;
            this.webView23DSurface.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView23DSurface.Location = new System.Drawing.Point(12, 12);
            this.webView23DSurface.Name = "webView23DSurface";
            this.webView23DSurface.Size = new System.Drawing.Size(509, 395);
            this.webView23DSurface.TabIndex = 0;
            this.webView23DSurface.ZoomFactor = 1D;
            // 
            // buttonShow3DSurface
            // 
            this.buttonShow3DSurface.Location = new System.Drawing.Point(414, 413);
            this.buttonShow3DSurface.Name = "buttonShow3DSurface";
            this.buttonShow3DSurface.Size = new System.Drawing.Size(107, 23);
            this.buttonShow3DSurface.TabIndex = 1;
            this.buttonShow3DSurface.Text = "Show 3D Surface";
            this.buttonShow3DSurface.UseVisualStyleBackColor = true;
            this.buttonShow3DSurface.Click += new System.EventHandler(this.buttonShow3DSurface_Click);
            // 
            // webView23DBar
            // 
            this.webView23DBar.AllowExternalDrop = true;
            this.webView23DBar.CreationProperties = null;
            this.webView23DBar.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView23DBar.Location = new System.Drawing.Point(527, 12);
            this.webView23DBar.Name = "webView23DBar";
            this.webView23DBar.Size = new System.Drawing.Size(509, 395);
            this.webView23DBar.TabIndex = 0;
            this.webView23DBar.ZoomFactor = 1D;
            // 
            // buttonDisplay3DBar
            // 
            this.buttonDisplay3DBar.Location = new System.Drawing.Point(929, 413);
            this.buttonDisplay3DBar.Name = "buttonDisplay3DBar";
            this.buttonDisplay3DBar.Size = new System.Drawing.Size(107, 23);
            this.buttonDisplay3DBar.TabIndex = 1;
            this.buttonDisplay3DBar.Text = "Show 3D Bar";
            this.buttonDisplay3DBar.UseVisualStyleBackColor = true;
            this.buttonDisplay3DBar.Click += new System.EventHandler(this.buttonDisplay3DBar_Click);
            // 
            // numericUpDownTransparency
            // 
            this.numericUpDownTransparency.Location = new System.Drawing.Point(823, 416);
            this.numericUpDownTransparency.Name = "numericUpDownTransparency";
            this.numericUpDownTransparency.Size = new System.Drawing.Size(68, 21);
            this.numericUpDownTransparency.TabIndex = 2;
            this.numericUpDownTransparency.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // webView23DWaterfall
            // 
            this.webView23DWaterfall.AllowExternalDrop = true;
            this.webView23DWaterfall.CreationProperties = null;
            this.webView23DWaterfall.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView23DWaterfall.Location = new System.Drawing.Point(1042, 12);
            this.webView23DWaterfall.Name = "webView23DWaterfall";
            this.webView23DWaterfall.Size = new System.Drawing.Size(509, 395);
            this.webView23DWaterfall.TabIndex = 0;
            this.webView23DWaterfall.ZoomFactor = 1D;
            // 
            // buttonShowWaterfall
            // 
            this.buttonShowWaterfall.Location = new System.Drawing.Point(1444, 413);
            this.buttonShowWaterfall.Name = "buttonShowWaterfall";
            this.buttonShowWaterfall.Size = new System.Drawing.Size(107, 23);
            this.buttonShowWaterfall.TabIndex = 1;
            this.buttonShowWaterfall.Text = "Show Waterfall";
            this.buttonShowWaterfall.UseVisualStyleBackColor = true;
            this.buttonShowWaterfall.Click += new System.EventHandler(this.buttonShowWaterfall_Click);
            // 
            // checkBoxDB
            // 
            this.checkBoxDB.AutoSize = true;
            this.checkBoxDB.Location = new System.Drawing.Point(1348, 417);
            this.checkBoxDB.Name = "checkBoxDB";
            this.checkBoxDB.Size = new System.Drawing.Size(36, 16);
            this.checkBoxDB.TabIndex = 3;
            this.checkBoxDB.Text = "dB";
            this.checkBoxDB.UseVisualStyleBackColor = true;
            // 
            // checkBoxFilter
            // 
            this.checkBoxFilter.AutoSize = true;
            this.checkBoxFilter.Location = new System.Drawing.Point(1264, 417);
            this.checkBoxFilter.Name = "checkBoxFilter";
            this.checkBoxFilter.Size = new System.Drawing.Size(60, 16);
            this.checkBoxFilter.TabIndex = 3;
            this.checkBoxFilter.Text = "Filter";
            this.checkBoxFilter.UseVisualStyleBackColor = true;
            // 
            // FormEcharts3DPlot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1615, 445);
            this.Controls.Add(this.checkBoxFilter);
            this.Controls.Add(this.checkBoxDB);
            this.Controls.Add(this.numericUpDownTransparency);
            this.Controls.Add(this.buttonDisplay3DBar);
            this.Controls.Add(this.buttonShowWaterfall);
            this.Controls.Add(this.buttonShow3DSurface);
            this.Controls.Add(this.webView23DWaterfall);
            this.Controls.Add(this.webView23DBar);
            this.Controls.Add(this.webView23DSurface);
            this.Name = "FormEcharts3DPlot";
            this.Text = "Echarts 3D Plot Form";
            ((System.ComponentModel.ISupportInitialize)(this.webView23DSurface)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView23DBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownTransparency)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webView23DWaterfall)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView23DSurface;
        private System.Windows.Forms.Button buttonShow3DSurface;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView23DBar;
        private System.Windows.Forms.Button buttonDisplay3DBar;
        private System.Windows.Forms.NumericUpDown numericUpDownTransparency;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView23DWaterfall;
        private System.Windows.Forms.Button buttonShowWaterfall;
        private System.Windows.Forms.CheckBox checkBoxDB;
        private System.Windows.Forms.CheckBox checkBoxFilter;
    }
}

