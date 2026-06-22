# 完整代码示例

> 所有示例均来自 `JY6311.Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点温度测量（Console）

```csharp
using System;
using System.Threading;
using JY6311;

Console.Write("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());

Console.Write("请输入通道号（0~15）：");
int channelID = Convert.ToInt32(Console.ReadLine());

JY6311AITask aiTask = new JY6311AITask(boardNum);

// 添加通道：PT100 四线制温度测量，TCR=0.003851
aiTask.AddChannel(channelID, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);     // 等待首次转换完成

double temperature = 0;
aiTask.ReadSinglePoint(ref temperature, channelID);
Console.WriteLine($"通道 {channelID} 温度：{temperature:F2} ℃");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 2：AI 单点电阻测量

```csharp
aiTask = new JY6311AITask(0);
// 添加通道：PT100 四线制电阻测量（不指定 TCR）
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire);
aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double resistance = 0;
aiTask.ReadSinglePoint(ref resistance, 0);
Console.WriteLine($"电阻值：{resistance:F3} Ω");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 3：AI 单点电压测量

```csharp
aiTask = new JY6311AITask(0);
// 电压模式：±1.25V 量程
aiTask.AddChannel(0, -1.25, 1.25);
aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double voltage = 0;
aiTask.ReadSinglePoint(ref voltage, 0);
Console.WriteLine($"电压值：{voltage:F6} V");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 4：AI 连续温度采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY6311;

public partial class MainForm : Form
{
    private JY6311AITask aiTask;
    private double[] readValue;

    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            readValue = new double[1000];

            // 创建 Task（槽位号 0）
            aiTask = new JY6311AITask(0);

            // 添加通道：PT100 四线制温度测量
            aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

            // 连续模式
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 100;                     // 100 Sa/s
            aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;

            aiTask.Start();

            timer1.Enabled = true;
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask.AvailableSamples >= (ulong)readValue.Length)
        {
            try
            {
                aiTask.ReadData(ref readValue, -1);
                easyChartX_AI.Plot(readValue);
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        timer1.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }
}
```

---

## 示例 5：AI 有限多通道温度采集

```csharp
JY6311AITask aiTask = new JY6311AITask(0);

// 多通道相同类型
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 500;
aiTask.SampleRate = 100;

aiTask.Start();
aiTask.WaitUntilDone(-1);

// 多通道读取：列存储 [采样点, 通道]
double[,] readValue = new double[500, 4];
aiTask.ReadData(ref readValue, 500, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 6：AI 多通道混合测量（不同 RTD 类型）

```csharp
JY6311AITask aiTask = new JY6311AITask(0);

// 各通道独立配置
int[] channels = { 0, 1, 2 };
RTDType[] types = { RTDType.PT100, RTDType.PT1000, RTDType.PT100 };
RTDTerminal[] terminals = { RTDTerminal.FourWire, RTDTerminal.ThreeWire, RTDTerminal.TwoWire };
RTDTCRType[] tcrs = { RTDTCRType.Pt100_TCR3851, RTDTCRType.Pt1000_TCR3851, RTDTCRType.Pt100_TCR3916 };

aiTask.AddChannel(channels, types, terminals, tcrs);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100;
aiTask.Start();

// ... 读取数据 ...
```

---

## 示例 7：AI 连续采集 + 数字触发

```csharp
aiTask = new JY6311AITask(0);
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100;

// 数字触发：PFI0 上升沿
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发到来后自动开始采集，后续读取同连续模式...
```

---

## 示例 8：AI 软触发采集

```csharp
aiTask = new JY6311AITask(0);
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 500;
aiTask.SampleRate = 100;

aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();

// 需要时手动发软触发
aiTask.SendSoftwareTrigger();

aiTask.WaitUntilDone(-1);
double[] data = new double[500];
aiTask.ReadData(ref data, 500, -1);
```

---

## 示例 9：AI 重触发采集（多段采集）

```csharp
using System;
using JY6311;

JY6311AITask aiTask = new JY6311AITask(0);

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

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 10：AI 16 通道同步温度采集

```csharp
using System;
using SeeSharpTools.JY.ArrayUtility;
using JY6311;

JY6311AITask aiTask = new JY6311AITask(0);

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

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 11：CSV 数据记录

```csharp
using System;
using System.IO;
using JY6311;

JY6311AITask aiTask = new JY6311AITask(0);
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
finally
{
    csvWriter?.Close();
    aiTask?.Stop();
    aiTask?.Channels.Clear();
}
```

---

## 示例 12：DI 单点读取（3 通道）

```csharp
JY6311DITask diTask = new JY6311DITask(0);
diTask.AddChannel(0);
diTask.AddChannel(1);
diTask.AddChannel(2);
diTask.Start();

bool[] values = new bool[3];
diTask.ReadSinglePoint(ref values);
Console.WriteLine($"DI0={values[0]}, DI1={values[1]}, DI2={values[2]}");

diTask.Stop();
diTask.Channels.Clear();
```

---

## 示例 13：DO 单点写入（3 通道）

```csharp
JY6311DOTask doTask = new JY6311DOTask(0);
doTask.AddChannel(0);
doTask.AddChannel(1);
doTask.AddChannel(2);
doTask.Start();

// 逐通道写入
doTask.WriteSinglePoint(true, 0);
doTask.WriteSinglePoint(false, 1);
doTask.WriteSinglePoint(true, 2);

// 或：一次写入全部
bool[] writeValue = { true, false, true };
doTask.WriteSinglePoint(writeValue);

doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 14：温度报警系统（AI + DO 联动）

```csharp
using System;
using System.Windows.Forms;
using JY6311;

public class TemperatureAlarm : Form
{
    private JY6311AITask aiTask;
    private JY6311DOTask doTask;
    private Timer timer;
    private Label statusLabel;
    private double alarmThreshold = 80.0;  // 报警阈值80℃

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
                statusLabel.Text = $"⚠ 温度报警: {temperature:F1}℃ 超过阈值 {alarmThreshold}℃";
                statusLabel.BackColor = System.Drawing.Color.Red;

                // 输出报警信号
                doTask.WriteSinglePoint(true, 0);
            }
            else
            {
                // 温度正常
                statusLabel.Text = $"温度正常: {temperature:F1}℃";
                statusLabel.BackColor = System.Drawing.Color.LightGreen;

                // 关闭报警信号
                doTask.WriteSinglePoint(false, 0);
            }
        }
        catch (Exception ex)
        {
            statusLabel.Text = $"错误: {ex.Message}";
        }
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
    }

    private void TemperatureAlarm_FormClosing(object sender, FormClosingEventArgs e)
    {
        StopSystem();
    }
}
```

---

## 示例 15：PXI 多卡同步采集

```csharp
using JY6311;

// ===== 主卡配置（Slot 2）=====
var masterTask = new JY6311AITask(2);
masterTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);
masterTask.Mode = AIMode.Finite;
masterTask.SamplesToAcquire = 1000;
masterTask.SampleRate = 100;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟和触发信号
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 4）=====
var slaveTask = new JY6311AITask(4);
slaveTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 1000;

slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 100;

slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();
masterTask.Start();

// ===== 读取数据 =====
double[] masterData = new double[1000];
double[] slaveData = new double[1000];

masterTask.WaitUntilDone(-1);
slaveTask.WaitUntilDone(-1);

masterTask.ReadData(ref masterData, 1000, -1);
slaveTask.ReadData(ref slaveData, 1000, -1);

masterTask.Stop();
slaveTask.Stop();
masterTask.Channels.Clear();
slaveTask.Channels.Clear();
```

---

## 综合技巧

### 多通道数据解析

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[500, 4];
aiTask.ReadData(ref buf, 500, -1);

// 提取各通道数据
double[] ch0 = new double[500];
for (int i = 0; i < 500; i++) ch0[i] = buf[i, 0];
```

### 测量类型选择策略

```csharp
// 1. 高精度温度测量（推荐）：4 线制 + PT100 + 正确 TCR
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire, RTDTCRType.Pt100_TCR3851);

// 2. 中距离布线：3 线制（补偿引线电阻）
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

// 3. 需原始电阻值：电阻模式（不做温度换算）
aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire);

// 4. 自定义传感器或直接测量小信号：电压模式
aiTask.AddChannel(0, -0.078125, 0.078125);    // ±78.125mV 最高精度量程
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer1 != null) timer1.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
        if (diTask != null) { diTask.Stop(); diTask.Channels.Clear(); }
        if (doTask != null) { doTask.Stop(); doTask.Channels.Clear(); }
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```
