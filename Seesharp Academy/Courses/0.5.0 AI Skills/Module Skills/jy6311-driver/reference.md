# JY6311 示例代码集

## 目录
- [基础示例](#基础示例)
- [温度采集示例](#温度采集示例)
- [触发采集示例](#触发采集示例)
- [多通道采集示例](#多通道采集示例)
- [数据记录示例](#数据记录示例)
- [数字IO示例](#数字io示例)
- [高级应用示例](#高级应用示例)

---

## 基础示例

### 示例1：最简单的单点温度读取

```csharp
using System;
using JY6311;

namespace JY6311Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("JY6311 单点温度读取示例");
            Console.WriteLine("========================");

            JY6311AITask aiTask = null;

            try
            {
                // 创建任务（板卡号0）
                aiTask = new JY6311AITask(0);

                // 添加通道0：PT100，3线制，标准TCR
                aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

                // 设置为单点模式
                aiTask.Mode = AIMode.Single;

                // 启动任务
                aiTask.Start();

                // 读取温度
                double temperature = 0;
                aiTask.ReadSinglePoint(ref temperature, 0);

                Console.WriteLine($"当前温度: {temperature:F2} ℃");
            }
            catch (JYDriverException ex)
            {
                Console.WriteLine($"驱动错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"系统错误: {ex.Message}");
            }
            finally
            {
                // 清理资源
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
```

### 示例2：循环读取多个通道

```csharp
using System;
using JY6311;

namespace JY6311Examples
{
    class MultiChannelRead
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                // 添加4个通道
                for (int i = 0; i < 4; i++)
                {
                    aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                }

                aiTask.Mode = AIMode.Single;
                aiTask.Start();

                // 循环读取10次
                for (int cycle = 0; cycle < 10; cycle++)
                {
                    Console.Write($"第{cycle + 1}次读取: ");

                    for (int ch = 0; ch < 4; ch++)
                    {
                        double temp = 0;
                        aiTask.ReadSinglePoint(ref temp, ch);
                        Console.Write($"Ch{ch}={temp:F1}℃  ");
                    }
                    Console.WriteLine();

                    System.Threading.Thread.Sleep(1000);  // 每秒读取一次
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

---

## 温度采集示例

### 示例3：PT100温度连续监测

```csharp
using System;
using System.Windows.Forms;
using JY6311;

namespace JY6311Examples
{
    public class TemperatureMonitor : Form
    {
        private JY6311AITask aiTask;
        private Timer timer;
        private double[] readBuffer;
        private Label[] tempLabels;
        private int channelCount = 8;

        public TemperatureMonitor()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "PT100温度监测系统";
            this.Size = new System.Drawing.Size(600, 400);

            // 创建温度显示标签
            tempLabels = new Label[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                tempLabels[i] = new Label
                {
                    Text = $"通道{i}: -- ℃",
                    Location = new System.Drawing.Point(20, 20 + i * 35),
                    Size = new System.Drawing.Size(200, 30),
                    Font = new System.Drawing.Font("Arial", 12)
                };
                this.Controls.Add(tempLabels[i]);
            }

            // 创建按钮
            Button btnStart = new Button
            {
                Text = "开始监测",
                Location = new System.Drawing.Point(300, 20),
                Size = new System.Drawing.Size(100, 40)
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            Button btnStop = new Button
            {
                Text = "停止监测",
                Location = new System.Drawing.Point(300, 70),
                Size = new System.Drawing.Size(100, 40)
            };
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            // 创建定时器
            timer = new Timer();
            timer.Interval = 500;  // 500ms更新一次
            timer.Tick += Timer_Tick;

            this.FormClosing += TemperatureMonitor_FormClosing;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                aiTask = new JY6311AITask(0);

                // 添加8个PT100通道
                for (int i = 0; i < channelCount; i++)
                {
                    aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                }

                // 配置连续采集
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 10;  // 10 Sa/s每通道
                aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;

                // 分配缓冲区（每次读取5个点）
                readBuffer = new double[5];

                aiTask.Start();
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            try
            {
                if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
                {
                    // 读取每个通道的最新数据
                    for (int ch = 0; ch < channelCount; ch++)
                    {
                        aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
                        double latestTemp = readBuffer[readBuffer.Length - 1];
                        tempLabels[ch].Text = $"通道{ch}: {latestTemp:F2} ℃";

                        // 温度超限报警（示例：超过100℃）
                        if (latestTemp > 100)
                        {
                            tempLabels[ch].ForeColor = System.Drawing.Color.Red;
                        }
                        else
                        {
                            tempLabels[ch].ForeColor = System.Drawing.Color.Black;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取错误: {ex.Message}");
            }

            timer.Start();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void StopMonitoring()
        {
            timer?.Stop();
            aiTask?.Stop();
            aiTask?.Channels.Clear();
        }

        private void TemperatureMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopMonitoring();
        }
    }
}
```

### 示例4：PT1000温度采集

```csharp
using System;
using JY6311;

namespace JY6311Examples
{
    class PT1000Example
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                // 添加PT1000通道（4线制，高精度）
                aiTask.AddChannel(0, RTDType.PT1000, RTDTerminal.FourWire, RTDTCRType.Pt1000_TCR3851);

                // 同时添加PT100通道进行对比
                aiTask.AddChannel(1, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 5;

                aiTask.Start();

                double[] buffer = new double[10];

                Console.WriteLine("PT1000 vs PT100 温度对比");
                Console.WriteLine("========================");

                for (int i = 0; i < 20; i++)
                {
                    // 读取PT1000
                    aiTask.ReadData(ref buffer, 10, -1);
                    double pt1000Temp = buffer[buffer.Length - 1];

                    // 读取PT100
                    aiTask.ReadData(ref buffer, 10, -1);
                    double pt100Temp = buffer[buffer.Length - 1];

                    Console.WriteLine($"PT1000: {pt1000Temp:F3}℃  |  PT100: {pt100Temp:F3}℃  |  差值: {Math.Abs(pt1000Temp - pt100Temp):F3}℃");

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

---

## 触发采集示例

### 示例5：数字触发采集

```csharp
using System;
using JY6311;

namespace JY6311Examples
{
    class DigitalTriggerExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                // 添加通道
                aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

                // 配置有限点采集
                aiTask.Mode = AIMode.Finite;
                aiTask.SampleRate = 100;
                aiTask.SamplesToAcquire = 1000;

                // 配置数字触发
                aiTask.Trigger.Type = AITriggerType.Digital;
                aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;  // PFI0作为触发源
                aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;    // 上升沿触发

                Console.WriteLine("等待数字触发信号（PFI0上升沿）...");

                aiTask.Start();

                // 等待采集完成
                bool completed = aiTask.WaitUntilDone(-1);

                if (completed)
                {
                    double[] data = new double[1000];
                    aiTask.ReadData(ref data, 1000, -1);

                    Console.WriteLine($"采集完成！共{data.Length}个数据点");
                    Console.WriteLine($"温度范围: {GetMin(data):F2}℃ ~ {GetMax(data):F2}℃");
                    Console.WriteLine($"平均温度: {GetAverage(data):F2}℃");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }

        static double GetMin(double[] data)
        {
            double min = data[0];
            foreach (var v in data) if (v < min) min = v;
            return min;
        }

        static double GetMax(double[] data)
        {
            double max = data[0];
            foreach (var v in data) if (v > max) max = v;
            return max;
        }

        static double GetAverage(double[] data)
        {
            double sum = 0;
            foreach (var v in data) sum += v;
            return sum / data.Length;
        }
    }
}
```

### 示例6：软件触发采集

```csharp
using System;
using System.Threading;
using JY6311;

namespace JY6311Examples
{
    class SoftwareTriggerExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                // 添加4个通道
                for (int i = 0; i < 4; i++)
                {
                    aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                }

                // 配置有限点采集
                aiTask.Mode = AIMode.Finite;
                aiTask.SampleRate = 50;
                aiTask.SamplesToAcquire = 500;

                // 配置软件触发
                aiTask.Trigger.Type = AITriggerType.Soft;
                aiTask.Trigger.Mode = AITriggerMode.Start;

                aiTask.Start();

                Console.WriteLine("任务已启动，等待软件触发...");
                Console.WriteLine("按Enter键发送触发信号...");
                Console.ReadLine();

                // 发送软件触发
                aiTask.SendSoftwareTrigger();
                Console.WriteLine("触发信号已发送，正在采集数据...");

                // 等待采集完成
                aiTask.WaitUntilDone(-1);

                // 读取数据（多通道）
                double[,] data = new double[500, 4];
                aiTask.ReadData(ref data, 500, -1);

                Console.WriteLine("采集完成！");
                Console.WriteLine($"数据维度: {data.GetLength(0)} 样本 x {data.GetLength(1)} 通道");

                // 显示每个通道的统计信息
                for (int ch = 0; ch < 4; ch++)
                {
                    double sum = 0, min = data[0, ch], max = data[0, ch];
                    for (int i = 0; i < 500; i++)
                    {
                        sum += data[i, ch];
                        if (data[i, ch] < min) min = data[i, ch];
                        if (data[i, ch] > max) max = data[i, ch];
                    }
                    Console.WriteLine($"通道{ch}: 平均={sum / 500:F2}℃  最小={min:F2}℃  最大={max:F2}℃");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

### 示例7：重触发采集（多段采集）

```csharp
using System;
using JY6311;

namespace JY6311Examples
{
    class RetriggerExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

                // 配置有限点采集
                aiTask.Mode = AIMode.Finite;
                aiTask.SampleRate = 100;
                aiTask.SamplesToAcquire = 200;  // 每次触发采集200个点

                // 配置软件触发和重触发
                aiTask.Trigger.Type = AITriggerType.Soft;
                aiTask.Trigger.Mode = AITriggerMode.Start;
                aiTask.Trigger.ReTriggerCount = 5;  // 触发5次

                aiTask.Start();

                double[] buffer = new double[200];

                for (int trigger = 1; trigger <= 5; trigger++)
                {
                    Console.WriteLine($"\n第{trigger}次触发，按Enter键发送触发信号...");
                    Console.ReadLine();

                    aiTask.SendSoftwareTrigger();

                    // 等待本次采集完成
                    while (aiTask.AvailableSamples < 200)
                    {
                        System.Threading.Thread.Sleep(10);
                    }

                    aiTask.ReadData(ref buffer, 200, -1);

                    // 计算本次触发的统计值
                    double sum = 0, max = buffer[0], min = buffer[0];
                    foreach (var v in buffer)
                    {
                        sum += v;
                        if (v > max) max = v;
                        if (v < min) min = v;
                    }

                    Console.WriteLine($"第{trigger}次采集完成: 平均={sum / 200:F2}℃  范围={min:F2}~{max:F2}℃");
                }

                Console.WriteLine("\n所有触发完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

---

## 多通道采集示例

### 示例8：16通道同步温度采集

```csharp
using System;
using SeeSharpTools.JY.ArrayUtility;
using JY6311;

namespace JY6311Examples
{
    class FullChannelExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                const int channelCount = 16;
                const int samplesPerRead = 50;

                // 添加所有16个通道
                for (int i = 0; i < channelCount; i++)
                {
                    aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                }

                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 20;  // 每通道20 Sa/s
                aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;

                // 分配缓冲区 [样本数, 通道数]
                double[,] readBuffer = new double[samplesPerRead, channelCount];
                double[,] displayBuffer = new double[channelCount, samplesPerRead];

                aiTask.Start();

                Console.WriteLine("16通道温度采集");
                Console.WriteLine("==============");

                for (int readCount = 0; readCount < 10; readCount++)
                {
                    // 等待数据就绪
                    while (aiTask.AvailableSamples < (ulong)samplesPerRead)
                    {
                        System.Threading.Thread.Sleep(10);
                    }

                    // 读取数据
                    aiTask.ReadData(ref readBuffer, samplesPerRead, -1);

                    // 转置数据（每行一个通道）
                    ArrayManipulation.Transpose(readBuffer, ref displayBuffer);

                    Console.WriteLine($"\n第{readCount + 1}次读取:");

                    // 显示每个通道的最新温度
                    for (int ch = 0; ch < channelCount; ch++)
                    {
                        double latestTemp = displayBuffer[ch, samplesPerRead - 1];
                        Console.Write($"Ch{ch,2}:{latestTemp,6:F1}℃  ");

                        // 每4个通道换行
                        if ((ch + 1) % 4 == 0) Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

---

## 数据记录示例

### 示例9：CSV数据记录

```csharp
using System;
using System.IO;
using JY6311;

namespace JY6311Examples
{
    class DataLoggingExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;
            StreamWriter csvWriter = null;

            try
            {
                // 创建CSV文件
                string fileName = $"TemperatureLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                csvWriter = new StreamWriter(fileName);

                // 写入CSV头
                csvWriter.Write("Timestamp");
                for (int i = 0; i < 8; i++) csvWriter.Write($",Channel{i}");
                csvWriter.WriteLine();
                csvWriter.Flush();

                aiTask = new JY6311AITask(0);

                // 添加8个通道
                for (int i = 0; i < 8; i++)
                {
                    aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                }

                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 10;

                double[,] buffer = new double[10, 8];

                aiTask.Start();

                Console.WriteLine($"数据记录到: {fileName}");
                Console.WriteLine("按Ctrl+C停止记录...");
                Console.WriteLine();

                int recordCount = 0;

                while (recordCount < 100)  // 记录100次
                {
                    while (aiTask.AvailableSamples < 10)
                    {
                        System.Threading.Thread.Sleep(5);
                    }

                    aiTask.ReadData(ref buffer, 10, -1);

                    // 记录每个样本
                    for (int i = 0; i < 10; i++)
                    {
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        csvWriter.Write(timestamp);

                        for (int ch = 0; ch < 8; ch++)
                        {
                            csvWriter.Write($",{buffer[i, ch]:F3}");
                        }
                        csvWriter.WriteLine();
                    }
                    csvWriter.Flush();

                    recordCount++;
                    Console.Write($"\r已记录: {recordCount * 10} 个样本");
                }

                Console.WriteLine("\n记录完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                csvWriter?.Close();
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

### 示例10：带统计信息的数据记录

```csharp
using System;
using System.IO;
using JY6311;

namespace JY6311Examples
{
    class StatisticsLoggingExample
    {
        class ChannelStatistics
        {
            public double Min { get; set; }
            public double Max { get; set; }
            public double Avg { get; set; }
            public double StdDev { get; set; }
        }

        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                const int channelCount = 4;

                for (int i = 0; i < channelCount; i++)
                {
                    aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                }

                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 100;

                double[,] buffer = new double[1000, channelCount];
                ChannelStatistics[] stats = new ChannelStatistics[channelCount];

                for (int i = 0; i < channelCount; i++)
                {
                    stats[i] = new ChannelStatistics();
                }

                aiTask.Start();

                Console.WriteLine("温度统计监测");
                Console.WriteLine("============");

                for (int cycle = 0; cycle < 60; cycle++)  // 运行60秒
                {
                    while (aiTask.AvailableSamples < 1000)
                    {
                        System.Threading.Thread.Sleep(10);
                    }

                    aiTask.ReadData(ref buffer, 1000, -1);

                    // 计算每个通道的统计信息
                    for (int ch = 0; ch < channelCount; ch++)
                    {
                        double sum = 0, sumSq = 0;
                        double min = buffer[0, ch], max = buffer[0, ch];

                        for (int i = 0; i < 1000; i++)
                        {
                            double v = buffer[i, ch];
                            sum += v;
                            sumSq += v * v;
                            if (v < min) min = v;
                            if (v > max) max = v;
                        }

                        double avg = sum / 1000;
                        double variance = (sumSq / 1000) - (avg * avg);
                        double stdDev = Math.Sqrt(variance);

                        stats[ch].Min = min;
                        stats[ch].Max = max;
                        stats[ch].Avg = avg;
                        stats[ch].StdDev = stdDev;
                    }

                    Console.WriteLine($"\n第{cycle + 1}秒:");
                    Console.WriteLine($"{'通道',-8} {'最小(℃)',-10} {'最大(℃)',-10} {'平均(℃)',-10} {'标准差',-10}");
                    Console.WriteLine(new string('-', 50));

                    for (int ch = 0; ch < channelCount; ch++)
                    {
                        Console.WriteLine($"Ch{ch,-7} {stats[ch].Min,-10:F2} {stats[ch].Max,-10:F2} {stats[ch].Avg,-10:F2} {stats[ch].StdDev,-10:F4}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

---

## 数字IO示例

### 示例11：数字输入监测

```csharp
using System;
using System.Windows.Forms;
using JY6311;

namespace JY6311Examples
{
    public class DIMonitor : Form
    {
        private JY6311DITask diTask;
        private Timer timer;
        private Label[] diLabels;

        public DIMonitor()
        {
            this.Text = "数字输入监测";
            this.Size = new System.Drawing.Size(300, 200);

            diLabels = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                diLabels[i] = new Label
                {
                    Text = $"PFI{i}: 低电平",
                    Location = new System.Drawing.Point(20, 20 + i * 40),
                    Size = new System.Drawing.Size(150, 30),
                    Font = new System.Drawing.Font("Arial", 12)
                };
                this.Controls.Add(diLabels[i]);
            }

            Button btnStart = new Button
            {
                Text = "开始监测",
                Location = new System.Drawing.Point(180, 20),
                Size = new System.Drawing.Size(90, 35)
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            Button btnStop = new Button
            {
                Text = "停止",
                Location = new System.Drawing.Point(180, 65),
                Size = new System.Drawing.Size(90, 35)
            };
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            timer = new Timer();
            timer.Interval = 100;
            timer.Tick += Timer_Tick;

            this.FormClosing += DIMonitor_FormClosing;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                diTask = new JY6311DITask(0);

                // 添加3个DI通道
                for (int i = 0; i < 3; i++)
                {
                    diTask.AddChannel(i);
                }

                diTask.Start();
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                bool[] values = new bool[3];
                diTask.ReadSinglePoint(ref values);

                for (int i = 0; i < 3; i++)
                {
                    if (values[i])
                    {
                        diLabels[i].Text = $"PFI{i}: 高电平";
                        diLabels[i].ForeColor = System.Drawing.Color.Green;
                    }
                    else
                    {
                        diLabels[i].Text = $"PFI{i}: 低电平";
                        diLabels[i].ForeColor = System.Drawing.Color.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取错误: {ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void StopMonitoring()
        {
            timer?.Stop();
            diTask?.Stop();
            diTask?.Channels.Clear();
        }

        private void DIMonitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopMonitoring();
        }
    }
}
```

### 示例12：数字输出控制

```csharp
using System;
using System.Windows.Forms;
using JY6311;

namespace JY6311Examples
{
    public class DOControl : Form
    {
        private JY6311DOTask doTask;
        private CheckBox[] doCheckBoxes;

        public DOControl()
        {
            this.Text = "数字输出控制";
            this.Size = new System.Drawing.Size(300, 250);

            doCheckBoxes = new CheckBox[3];
            for (int i = 0; i < 3; i++)
            {
                doCheckBoxes[i] = new CheckBox
                {
                    Text = $"PFI{i} 输出高电平",
                    Location = new System.Drawing.Point(20, 20 + i * 40),
                    Size = new System.Drawing.Size(150, 30),
                    Enabled = false
                };
                int index = i;  // 捕获循环变量
                doCheckBoxes[i].CheckedChanged += (s, e) => DoCheckBox_CheckedChanged(index);
                this.Controls.Add(doCheckBoxes[i]);
            }

            Button btnStart = new Button
            {
                Text = "启动DO",
                Location = new System.Drawing.Point(180, 20),
                Size = new System.Drawing.Size(90, 35)
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            Button btnStop = new Button
            {
                Text = "停止DO",
                Location = new System.Drawing.Point(180, 65),
                Size = new System.Drawing.Size(90, 35)
            };
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            this.FormClosing += DOControl_FormClosing;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                doTask = new JY6311DOTask(0);

                // 添加3个DO通道
                for (int i = 0; i < 3; i++)
                {
                    doTask.AddChannel(i);
                }

                doTask.Start();

                // 启用控制
                foreach (var cb in doCheckBoxes)
                {
                    cb.Enabled = true;
                }

                // 初始输出低电平
                for (int i = 0; i < 3; i++)
                {
                    doTask.WriteSinglePoint(false, i);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}");
            }
        }

        private void DoCheckBox_CheckedChanged(int channel)
        {
            try
            {
                bool value = doCheckBoxes[channel].Checked;
                doTask.WriteSinglePoint(value, channel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输出错误: {ex.Message}");
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopDO();
        }

        private void StopDO()
        {
            // 禁用控制
            foreach (var cb in doCheckBoxes)
            {
                cb.Enabled = false;
                cb.Checked = false;
            }

            doTask?.Stop();
            doTask?.Channels.Clear();
        }

        private void DOControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopDO();
        }
    }
}
```

---

## 高级应用示例

### 示例13：信号导出与同步

```csharp
using System;
using System.Threading;
using JY6311;

namespace JY6311Examples
{
    class SignalExportExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 1000;

                // 导出采样时钟到PFI1
                aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PFI1);

                // 导出开始触发信号到PXI_Trig0
                aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

                Console.WriteLine("信号导出配置:");
                Console.WriteLine("- 采样时钟 -> PFI1");
                Console.WriteLine("- 开始触发 -> PXI_Trig0");
                Console.WriteLine();

                aiTask.Start();

                Console.WriteLine("采集进行中，信号已导出...");
                Console.WriteLine("按Enter键停止...");
                Console.ReadLine();

                // 清除信号导出
                aiTask.SignalExport.ClearAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }
    }
}
```

### 示例14：外部时钟采集

```csharp
using System;
using JY6311;

namespace JY6311Examples
{
    class ExternalClockExample
    {
        static void Main(string[] args)
        {
            JY6311AITask aiTask = null;

            try
            {
                aiTask = new JY6311AITask(0);

                aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

                // 配置外部时钟
                aiTask.SampleClock.Source = AISampleClockSource.External;
                aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
                aiTask.SampleClock.External.ExpectedRate = 1000;  // 期望1kHz外部时钟

                aiTask.Mode = AIMode.Continuous;

                Console.WriteLine("外部时钟采集配置:");
                Console.WriteLine("- 时钟源: PFI0");
                Console.WriteLine("- 期望频率: 1000 Hz");
                Console.WriteLine();
                Console.WriteLine("请连接外部时钟信号到PFI0，然后按Enter键开始...");
                Console.ReadLine();

                aiTask.Start();

                double[] buffer = new double[100];

                for (int i = 0; i < 10; i++)
                {
                    while (aiTask.AvailableSamples < 100)
                    {
                        System.Threading.Thread.Sleep(10);
                    }

                    aiTask.ReadData(ref buffer, 100, -1);
                    Console.WriteLine($"读取批次 {i + 1}: 平均温度 = {CalculateAverage(buffer):F2}℃");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            finally
            {
                aiTask?.Stop();
                aiTask?.Channels.Clear();
            }
        }

        static double CalculateAverage(double[] data)
        {
            double sum = 0;
            foreach (var v in data) sum += v;
            return sum / data.Length;
        }
    }
}
```

### 示例15：温度报警系统

```csharp
using System;
using System.Windows.Forms;
using JY6311;

namespace JY6311Examples
{
    public class TemperatureAlarm : Form
    {
        private JY6311AITask aiTask;
        private JY6311DOTask doTask;
        private Timer timer;
        private Label statusLabel;
        private double alarmThreshold = 80.0;  // 报警阈值80℃

        public TemperatureAlarm()
        {
            this.Text = "温度报警系统";
            this.Size = new System.Drawing.Size(400, 200);

            statusLabel = new Label
            {
                Text = "系统就绪",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(350, 60),
                Font = new System.Drawing.Font("Arial", 14),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this.Controls.Add(statusLabel);

            Button btnStart = new Button
            {
                Text = "启动监测",
                Location = new System.Drawing.Point(50, 100),
                Size = new System.Drawing.Size(120, 40)
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            Button btnStop = new Button
            {
                Text = "停止",
                Location = new System.Drawing.Point(220, 100),
                Size = new System.Drawing.Size(120, 40)
            };
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;

            this.FormClosing += TemperatureAlarm_FormClosing;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                // 启动AI任务
                aiTask = new JY6311AITask(0);
                aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 10;
                aiTask.Start();

                // 启动DO任务（用于报警输出）
                doTask = new JY6311DOTask(0);
                doTask.AddChannel(0);  // PFI0作为报警输出
                doTask.Start();
                doTask.WriteSinglePoint(false, 0);  // 初始关闭报警

                timer.Start();
                statusLabel.Text = "监测中...";
                statusLabel.BackColor = System.Drawing.Color.LightGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                double temperature = 0;
                aiTask.ReadSinglePoint(ref temperature, 0);

                if (temperature > alarmThreshold)
                {
                    // 温度超限，触发报警
                    statusLabel.Text = $"⚠ 温度报警: {temperature:F1}℃\n超过阈值 {alarmThreshold}℃";
                    statusLabel.BackColor = System.Drawing.Color.Red;
                    statusLabel.ForeColor = System.Drawing.Color.White;

                    // 输出报警信号
                    doTask.WriteSinglePoint(true, 0);
                }
                else
                {
                    // 温度正常
                    statusLabel.Text = $"温度正常: {temperature:F1}℃";
                    statusLabel.BackColor = System.Drawing.Color.LightGreen;
                    statusLabel.ForeColor = System.Drawing.Color.Black;

                    // 关闭报警信号
                    doTask.WriteSinglePoint(false, 0);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"错误: {ex.Message}";
                statusLabel.BackColor = System.Drawing.Color.Yellow;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopSystem();
        }

        private void StopSystem()
        {
            timer?.Stop();

            // 关闭报警输出
            doTask?.WriteSinglePoint(false, 0);
            doTask?.Stop();
            doTask?.Channels.Clear();

            aiTask?.Stop();
            aiTask?.Channels.Clear();

            statusLabel.Text = "系统已停止";
            statusLabel.BackColor = System.Drawing.SystemColors.Control;
        }

        private void TemperatureAlarm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopSystem();
        }
    }
}
```

---

## 参考资源

- **驱动路径：** `c:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.dll`
- **示例代码路径：** `c:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\JY6311.Examples\`
- **API参考：** 参见 `jy6311-reference.md`
- **完整开发指南：** 参见 `jy6311-driver.md`
