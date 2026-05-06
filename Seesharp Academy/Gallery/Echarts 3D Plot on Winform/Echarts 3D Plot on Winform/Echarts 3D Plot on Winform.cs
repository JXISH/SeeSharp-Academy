using System;
using System.Windows.Forms;
using WebView2Plots;
using SeeSharpTools.JY.File;

namespace Echarts_3D_Plot_on_Winform
{
    public partial class FormEcharts3DPlot : Form
    {
        public FormEcharts3DPlot()
        {
            InitializeComponent();
        }

        private async void buttonShow3DSurface_Click(object sender, EventArgs e)
        {
            // X axis parameters
            double x0 = 0;
            double xStep = 0.5;
            int xCount = 40;

            // Y axis parameters
            double y0 = 0;
            double yStep = 0.5;
            int yCount = 40;

            // Generate demo surface data
            double[,] surfaceData = new double[xCount, yCount];
            Random rn = new Random();
            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    double x = x0 + i * xStep;
                    double y = y0 + j * yStep;
                    double r = Math.Sqrt(x * x + y * y);
                    surfaceData[i, j] = Math.Sin(r) * 10.0 / (r + 1.0) + rn.Next(100) / 500.0;
                }
            }

            // Call static class to render
            await WebView2ThreeDSurface.RenderAsync(webView23DSurface, surfaceData,
                x0, xStep, y0, yStep, "X", "Y", "Z");
        }

        private async void buttonDisplay3DBar_Click(object sender, EventArgs e)
        {
            // X axis parameters
            double x0 = -5.0;
            double xStep = 0.5;
            int xCount = 20;

            // Y axis parameters
            double y0 = -5.0;
            double yStep = 0.5;
            int yCount = 20;

            // Generate center-symmetric Gaussian + noise data
            Random rand = new Random(42);
            double[,] barData = new double[xCount, yCount];
            double centerX = x0 + (xCount - 1) * xStep / 2.0;
            double centerY = y0 + (yCount - 1) * yStep / 2.0;
            double sigma = 2.0;

            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    double x = x0 + i * xStep;
                    double y = y0 + j * yStep;
                    double dx = x - centerX;
                    double dy = y - centerY;
                    double gaussian = Math.Exp(-(dx * dx + dy * dy) / (2.0 * sigma * sigma));
                    double noise = (rand.NextDouble() - 0.5) * 0.2;
                    barData[i, j] = gaussian + noise;
                }
            }

            // Get transparency percentage from numericUpDown
            int transparencyPercent = (int)numericUpDownTransparency.Value;

            // Call static class to render
            await WebView2Bar3D.RenderAsync(webView23DBar, barData,
                x0, xStep, y0, yStep, "X", "Y", "Z", transparencyPercent);
        }

    }
}
