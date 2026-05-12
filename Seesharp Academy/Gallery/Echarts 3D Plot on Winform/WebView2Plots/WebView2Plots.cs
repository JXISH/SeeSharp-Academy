using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace WebView2Plots
{
    /// <summary>
    /// Static class for rendering a 3D surface plot in a WebView2 control using ECharts.
    /// </summary>
    public static class WebView2ThreeDSurface
    {
        private static bool _loaded = false;
        private static WebView2 _bindedWebView = null;

        /// <summary>
        /// Render a 3D surface plot.
        /// </summary>
        /// <param name="webView">Target WebView2 control</param>
        /// <param name="data">2D array where indices map to X,Y coordinates and values represent Z height</param>
        /// <param name="x0">X axis start value</param>
        /// <param name="xStep">X axis step size</param>
        /// <param name="y0">Y axis start value</param>
        /// <param name="yStep">Y axis step size</param>
        /// <param name="xAxisName">X axis display name</param>
        /// <param name="yAxisName">Y axis display name</param>
        /// <param name="zAxisName">Z axis display name</param>
        public static async Task RenderAsync(WebView2 webView, double[,] data,
            double x0, double xStep, double y0, double yStep,
            string xAxisName = "X", string yAxisName = "Y", string zAxisName = "Z", ThreeDSurfaceType surfaceType = ThreeDSurfaceType.Surface)
        {
            int xCount = data.GetLength(0);
            int yCount = data.GetLength(1);

            // Build JSON data array: [[x, y, z], ...]
            string jsonData = BuildJsonData(data, x0, xStep, y0, yStep, xCount, yCount);

            string templateHtml = "threeDSurfaceWeb.html";
            if(surfaceType == ThreeDSurfaceType.Waterfall)
                templateHtml = "threeDWaterfallWeb.html";
            // Load threeDSurfaceWeb.html if not already loaded or if control changed
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templateHtml);
            if (!File.Exists(htmlPath))
            {
                MessageBox.Show("{templateHtml} not found at: " + htmlPath);
                return;
            }

            if (!_loaded || _bindedWebView != webView)
            {
                _loaded = false;
                _bindedWebView = webView;

                webView.Source = new Uri(htmlPath);

                var tcs = new TaskCompletionSource<bool>();
                EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> handler = null;
                handler = (s, args) =>
                {
                    webView.NavigationCompleted -= handler;
                    tcs.SetResult(true);
                };
                webView.NavigationCompleted += handler;
                await tcs.Task;
                _loaded = true;
            }

            // Pass data and axis names to JavaScript
            string script = string.Format("renderSurface({0}, {1}, {2}, '{3}', '{4}', '{5}');",
                jsonData, xCount, yCount,
                EscapeJs(xAxisName), EscapeJs(yAxisName), EscapeJs(zAxisName));
            await webView.ExecuteScriptAsync(script);
        }

        private static string BuildJsonData(double[,] data, double x0, double xStep, double y0, double yStep, int xCount, int yCount)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    double xVal = x0 + i * xStep;
                    double yVal = y0 + j * yStep;
                    sb.AppendFormat("[{0},{1},{2}]",
                        xVal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        yVal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        data[i, j].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeJs(string s)
        {
            return s.Replace("\\", "\\\\").Replace("'", "\\'");
        }
    }

    /// <summary>
    /// Static class for rendering a 3D bar plot in a WebView2 control using ECharts.
    /// </summary>
    public static class WebView2Bar3D
    {
        private static bool _loaded = false;
        private static WebView2 _bindedWebView = null;

        /// <summary>
        /// Render a 3D bar plot.
        /// </summary>
        /// <param name="webView">Target WebView2 control</param>
        /// <param name="data">2D array where indices map to X,Y coordinates and values represent Z height</param>
        /// <param name="x0">X axis start value</param>
        /// <param name="xStep">X axis step size</param>
        /// <param name="y0">Y axis start value</param>
        /// <param name="yStep">Y axis step size</param>
        /// <param name="xAxisName">X axis display name</param>
        /// <param name="yAxisName">Y axis display name</param>
        /// <param name="zAxisName">Z axis display name</param>
        /// <param name="transparencyPercent">Transparency percentage (0 = opaque, 100 = fully transparent)</param>
        public static async Task RenderAsync(WebView2 webView, double[,] data,
            double x0, double xStep, double y0, double yStep,
            string xAxisName = "X", string yAxisName = "Y", string zAxisName = "Z",
            int transparencyPercent = 50)
        {
            int xCount = data.GetLength(0);
            int yCount = data.GetLength(1);

            // Build JSON data array: [[x, y, z], ...]
            string jsonData = BuildJsonData(data, x0, xStep, y0, yStep, xCount, yCount);

            // Load threeDBarWeb.html if not already loaded or if control changed
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "threeDBarWeb.html");
            if (!File.Exists(htmlPath))
            {
                MessageBox.Show("threeDBarWeb.html not found at: " + htmlPath);
                return;
            }

            if (!_loaded || _bindedWebView != webView)
            {
                _loaded = false;
                _bindedWebView = webView;

                webView.Source = new Uri(htmlPath);

                var tcs = new TaskCompletionSource<bool>();
                EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> handler = null;
                handler = (s, args) =>
                {
                    webView.NavigationCompleted -= handler;
                    tcs.SetResult(true);
                };
                webView.NavigationCompleted += handler;
                await tcs.Task;
                _loaded = true;
            }

            // Pass data, transparency and axis names to JavaScript
            string script = string.Format("render3DBar({0}, {1}, '{2}', '{3}', '{4}');",
                jsonData, transparencyPercent,
                EscapeJs(xAxisName), EscapeJs(yAxisName), EscapeJs(zAxisName));
            await webView.ExecuteScriptAsync(script);
        }

        private static string BuildJsonData(double[,] data, double x0, double xStep, double y0, double yStep, int xCount, int yCount)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    double xVal = x0 + i * xStep;
                    double yVal = y0 + j * yStep;
                    sb.AppendFormat("[{0},{1},{2}]",
                        xVal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        yVal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        data[i, j].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string EscapeJs(string s)
        {
            return s.Replace("\\", "\\\\").Replace("'", "\\'");
        }
    }

    /// <summary>
    /// 3D曲面类型
    /// </summary>
    public enum ThreeDSurfaceType
    {
        /// <summary>
        /// 具有网格的曲面
        /// </summary>
        Surface,
        /// <summary>
        /// 瀑布图
        /// </summary>
        Waterfall
    }
}
