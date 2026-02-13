using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JYUSB1601;
using System.Timers; // 如果使用系统定时器
using SeeSharpTools.JY.DSP.Fundamental;  // 频谱分析所需
using System.IO;

namespace FormExample
{
    public partial class MainForm : Form
    {
        #region Private Fields

        /// <summary>
        /// aiTask
        /// </summary>
        private JYUSB1601AITask aiTask;

        /// <summary>
        ///  the Buffer of data acquisition by the aiTask
        /// </summary>
        private double[,] readValue;

        // 添加私有字段存储频率分辨率
        private double spectralFrequencyResolution = 1.0;

        /// <summary>
        /// 保存频谱计算结果
        /// </summary>
        private double[] spectrumData;

        /// <summary>
        /// 采样率
        /// </summary>
        private double sampleRate = 10000.0;
        #endregion
        public MainForm()
        {
            InitializeComponent();
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            try
            {
                // 清除图表数据
                easyChartX1.Clear();
                easyChartX1.Series.Clear();
                easyChartX2.Clear();  // 清除频谱图表
                easyChartX2.Series.Clear();

                // 添加一个系列用于显示通道0数据
                easyChartX1.Series.Add(new SeeSharpTools.JY.GUI.EasyChartXSeries());
                easyChartX1.Series[0].Name = "Channel 0";

                // 添加一个系列用于显示频谱数据
                easyChartX2.Series.Add(new SeeSharpTools.JY.GUI.EasyChartXSeries());
                easyChartX2.Series[0].Name = "Power Spectrum";

                // 创建AI任务 - 使用默认的板卡名称或指定板卡名称
                aiTask = new JYUSB1601AITask("JYUSB-1601"); // 或者使用 textBox_cardName.Text

                // 添加通道0，设置电压范围 ±10V
                aiTask.AddChannel(0, -10, 10, AITerminal.RSE); // 或使用 AITerminal.SE

                // 配置采集参数
                aiTask.Mode = AIMode.Finite;         // 有限模式
                aiTask.SamplesToAcquire = 5000;      // 采集5000个点
                aiTask.SampleRate = 10000;           // 采样率10kHz

                // 启动采集
                aiTask.Start();

                // 初始化数据缓冲区
                readValue = new double[5000, 1]; // 5000个点，1个通道

                // 使用定时器轮询读取数据
                System.Windows.Forms.Timer fetchDataTimer = new System.Windows.Forms.Timer();
                fetchDataTimer.Interval = 10; // 10ms间隔
                fetchDataTimer.Tick += (timerSender, timerArgs) =>
                {
                    try
                    {
                        if (aiTask.AvailableSamples >= 5000)
                        {
                            // 读取数据
                            aiTask.ReadData(ref readValue, 5000, -1);

                            // 显示在图表上
                            // 将二维数组的第一列转为一维数组显示
                            double[] channelData = new double[5000];
                            for (int i = 0; i < 5000; i++)
                            {
                                channelData[i] = readValue[i, 0];
                            }
                            easyChartX1.Plot(channelData);

                            // 停止任务
                            aiTask.Stop();

                            // 进行功率谱分析
                            PerformSpectralAnalysis(channelData, 10000); // 10kHz采样率

                            // 停止并释放定时器
                            fetchDataTimer.Stop();
                            fetchDataTimer.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("数据读取失败: " + ex.Message);
                        fetchDataTimer.Stop();
                        fetchDataTimer.Dispose();
                    }
                };

                fetchDataTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("采集启动失败: " + ex.Message);
            }

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                // 如果任务正在运行，则停止它
                if (aiTask != null)
                {
                    aiTask.Stop();
                }
            }
            catch (Exception ex)
            {
                // 忽略停止时的错误
                Console.WriteLine("停止任务时出错: " + ex.Message);
            }

        }

        /// <summary>
        /// 执行功率谱分析并在easyChartX2上显示结果
        /// </summary>
        /// <param name="signal">输入信号数据</param>
        /// <param name="sampleRate">采样率</param>
        private void PerformSpectralAnalysis(double[] signal, double sampleRate)
        {
            try
            {
                // 保存采样率
                this.sampleRate = sampleRate;

                // 创建频谱数组（长度为信号长度的一半）
                spectrumData = new double[signal.Length / 2];
                double df; // 频率分辨率

                // 计算功率谱
                Spectrum.PowerSpectrum(signal, sampleRate, ref spectrumData, out df);

                // 保存频率分辨率供后续使用
                spectralFrequencyResolution = df;

                // 在easyChartX2上显示频谱
                easyChartX2.Plot(spectrumData, 0, df);
            }
            catch (Exception ex)
            {
                MessageBox.Show("频谱分析失败: " + ex.Message);
            }
        }

        private void btSaveSpectrum_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否有频谱数据可以保存
                if (spectrumData == null || spectrumData.Length == 0)
                {
                    MessageBox.Show("没有频谱数据可保存，请先进行数据采集和频谱分析。");
                    return;
                }

                // 创建保存文件对话框
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*";
                saveDialog.Title = "保存频谱数据";
                saveDialog.DefaultExt = "csv";
                saveDialog.FileName = "spectrum_data.csv";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // 写入CSV文件
                    using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                    {
                        // 写入标题行
                        writer.WriteLine("Frequency(Hz),Magnitude");

                        // 写入数据行
                        for (int i = 0; i < spectrumData.Length; i++)
                        {
                            double frequency = i * spectralFrequencyResolution;
                            double magnitude = spectrumData[i];
                            writer.WriteLine($"{frequency:F2},{magnitude:F6}");
                        }
                    }

                    MessageBox.Show("频谱数据已成功保存到: " + saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存频谱数据失败: " + ex.Message);
            }
        }
    }
}
