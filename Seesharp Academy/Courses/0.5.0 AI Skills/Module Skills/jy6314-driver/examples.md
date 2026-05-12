# JY6314 完整示例代码

> 本文件是 [SKILL.md](SKILL.md) 的补充。所有示例对应 `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\JY6314.Examples\` 下的真实工程，可直接复制到目标项目改造。

工程引用（所有示例通用）：
```xml
<Reference Include="JY6314">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JY6314\Bin\JY6314.dll</HintPath>
</Reference>
<Reference Include="SeeSharpTools.JY.GUI">
  <HintPath>C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.GUI.dll</HintPath>
</Reference>
<Reference Include="SeeSharpTools.JY.ArrayUtility">
  <HintPath>C:\SeeSharp\JYTEK\SeeSharpTools\Bin\SeeSharpTools.JY.ArrayUtility.dll</HintPath>
</Reference>
```

---

## 示例 1：控制台 AI 单点读取（最简形式）

对应：`JY6314.Examples\Analog Input\Console AI Single Point\`

```csharp
using System;
using System.Threading;
using JY6314;

class AISinglePoint
{
    static void Main()
    {
        Console.Write("Board number: ");
        int boardNum = int.Parse(Console.ReadLine());

        double[] readValue = new double[16];
        JY6314AITask aiTask = null;

        try
        {
            aiTask = new JY6314AITask(boardNum);
            aiTask.Mode = AIMode.Single;
            aiTask.AddChannel(-1, -1.25, 1.25);   // 全部 16 通道，±1.25 V
            aiTask.SampleRate = 10;               // Single 模式下 = 单点刷新频率
            aiTask.Start();

            Thread.Sleep((int)(1000.0 / aiTask.SampleRate));  // 等待 1 个周期
            aiTask.ReadSinglePoint(ref readValue);

            for (int i = 0; i < readValue.Length; i++)
                Console.WriteLine($"Ch{i} = {readValue[i]:F6} V");
        }
        catch (JYDriverException ex) { Console.WriteLine(ex.Message); }
        finally { aiTask?.Stop(); }

        Console.ReadKey();
    }
}
```

---

## 示例 2：Finite 模式（单通道热电偶）

对应：`Winform AI Finite\`

```csharp
using JY6314;

JY6314AITask aiTask;
double[] readValue;

private void button_start_Click(object sender, EventArgs e)
{
    readValue = new double[(int)numericUpDown_Samples.Value];
    try
    {
        aiTask = new JY6314AITask(comboBox_BoardNumber.SelectedIndex);
        aiTask.AddChannel(comboBox_ChannelNumber.SelectedIndex,
            (ThermocoupleType)Enum.Parse(typeof(ThermocoupleType), comboBox_TerminalType.Text));

        aiTask.Mode             = AIMode.Finite;
        aiTask.SampleRate       = (double)numericUpDown_SampleRate.Value;
        aiTask.SamplesToAcquire = (int)numericUpDown_Samples.Value;

        aiTask.Start();
        aiTask.ReadData(ref readValue, -1);     // 阻塞到读满
        aiTask.Stop();

        stripChartX1.Plot(readValue);           // SeeSharpTools 绘图
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    aiTask?.Stop();
}
```

---

## 示例 3：Continuous 模式（单通道 + 定时轮询）

对应：`Winform AI Continuous\`

```csharp
using JY6314;

JY6314AITask aiTask;
double[] readValue;

private void button_start_Click(object sender, EventArgs e)
{
    stripChartX1.Clear(); stripChartX1.Series.Clear();
    readValue = new double[(int)numericUpDown_Samples.Value];

    try
    {
        aiTask = new JY6314AITask(comboBox_BoardNumber.SelectedIndex);
        aiTask.AddChannel(comboBox_ChannelNumber.SelectedIndex, ThermocoupleType.TypeK);

        aiTask.Mode               = AIMode.Continuous;
        aiTask.SampleClock.Source = AISampleClockSource.Internal;
        aiTask.SampleRate         = (double)numericUpDown_SampleRate.Value;

        // 可选：把采样时钟同步导出到 PFI0 & PXI_Trig0（给外部/从卡用）
        aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PFI0);
        aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

        aiTask.Start();
        timer1.Enabled = true;

        for (int i = 0; i < aiTask.Channels.Count; i++)
        {
            stripChartX1.Series.Add(new SeeSharpTools.JY.GUI.StripChartXSeries());
            stripChartX1.Series[i].Name = $"Ch{aiTask.Channels[i].ChannelID}";
        }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer1_Tick(object sender, EventArgs e)
{
    timer1.Enabled = false;
    if (aiTask.AvailableSamples >= (ulong)readValue.Length)
    {
        try
        {
            aiTask.ReadData(ref readValue, -1);
            stripChartX1.Plot(readValue);
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
    }
    timer1.Enabled = true;
}
```

---

## 示例 4：Continuous 多通道（热电偶 + 转置绘图）

对应：`Winform AI Continuous MutiChannel\`

```csharp
using JY6314;
using SeeSharpTools.JY.ArrayUtility;

JY6314AITask aiTask;
double[,] readValue;    // [samples, channels]
double[,] displayValue; // [channels, samples] — 绘图需转置

private void Start(List<int> channels, int samples, double rate)
{
    readValue    = new double[samples, channels.Count];
    displayValue = new double[channels.Count, samples];

    aiTask = new JY6314AITask(0);
    foreach (int ch in channels)
        aiTask.AddChannel(ch, ThermocoupleType.TypeK);

    aiTask.Mode       = AIMode.Continuous;
    aiTask.SampleRate = rate;
    aiTask.Start();
    timer1.Enabled = true;
}

private void timer1_Tick(object s, EventArgs e)
{
    timer1.Enabled = false;
    if (aiTask.AvailableSamples >= (ulong)readValue.GetLength(0))
    {
        aiTask.ReadData(ref readValue, -1);
        ArrayManipulation.Transpose(readValue, ref displayValue);
        stripChartX1.Plot(displayValue);
    }
    timer1.Enabled = true;
}
```

---

## 示例 5：数字触发（外部 PFI 边沿触发）

对应：`Winform AI Continuous Digital Trigger\`

```csharp
aiTask = new JY6314AITask(boardNum);
foreach (int ch in selectedChannels)
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);

aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 1000;

aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Mode           = AITriggerMode.Start;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;     // 外部边沿接入 PFI0
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;

aiTask.Start();    // 阻塞等待外部上升沿才开始真正采集
```

---

## 示例 6：软件触发 + 多次重触发（MultiRecord）

对应：`Winform AI MultiRecord Soft Trigger\`

```csharp
aiTask.Mode             = AIMode.Finite;
aiTask.SampleRate       = 1000;
aiTask.SamplesToAcquire = 2000;

aiTask.Trigger.Type           = AITriggerType.Soft;
aiTask.Trigger.Mode           = AITriggerMode.Start;   // 或 Reference（支持 PreTriggerSamples）
aiTask.Trigger.ReTriggerCount = 5;                     // 重触发 5 次；-1 无限
aiTask.Start();

// UI 每点一次按钮，发一次软触发
private void button_sendSoftTrigger_Click(object sender, EventArgs e)
{
    aiTask.SendSoftwareTrigger();
}

// 轮询：每次满 SamplesToAcquire 读一批
private void timer_Tick(object s, EventArgs e)
{
    if (aiTask.AvailableSamples >= (ulong)readValue.GetLength(0))
    {
        aiTask.ReadData(ref readValue, readValue.GetLength(0), -1);
        ArrayManipulation.Transpose(readValue, ref displayValue);
        stripChartX1.Plot(displayValue);
    }
}
```

Reference Trigger + PreTrigger 变体（采集触发前 N 点）：
```csharp
aiTask.Trigger.Mode             = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 200;         // 必须 ≤ SamplesToAcquire
```

---

## 示例 7：多卡采样时钟同步

对应：`Winform AI_MultiCard SampleClock Sync\`

Master 用内部时钟，把 `SampleClock` 导出到 `PXI_Trig0`；Slave 把 `PXI_Trig0` 作为外部时钟接入。

```csharp
// —— Master ——
var master = new JY6314AITask(masterSlot);
master.AddChannel(ch, -1.25, 1.25);
master.Mode             = AIMode.Finite;
master.SampleRate       = 1000;
master.SamplesToAcquire = samples;
master.Trigger.Type     = AITriggerType.Immediate;
master.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

// —— Slave ——
var slave = new JY6314AITask(slaveSlot);
slave.AddChannel(ch, -1.25, 1.25);
slave.Mode                          = AIMode.Finite;
slave.SamplesToAcquire              = samples;
slave.SampleClock.Source            = AISampleClockSource.External;
slave.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slave.SampleClock.External.ExpectedRate = 1000;

// 启动顺序：先 Slave 再 Master
slave.Start();
master.Start();

// 读取
double[] masterBuf = new double[samples];
double[] slaveBuf  = new double[samples];
master.ReadData(ref masterBuf, samples, 10000);
slave .ReadData(ref slaveBuf,  samples, 10000);

master.Stop();
slave .Stop();
```

配合 `SeeSharpTools.JY.DSP.Utility.ToneAnalyzer.SingleToneAnalysis(buf, rate)` 可计算两路相位差 / 时延差。

---

## 示例 8：自定义冷端参考温度（关闭内置 CJC）

对应：`Winform AI Continuous Custom RJ\`

```csharp
aiTask = new JY6314AITask(0);
foreach (int ch in channels)
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);

aiTask.Mode              = AIMode.Continuous;
aiTask.SampleRate        = 100;
aiTask.BuildInCJC.Enabled = false;                 // 关闭 TB68CJ 自动补偿

aiTask.Start();

// 随后从 UI 的 DataGridView 把每通道 RJ 温度写入
for (int i = 0; i < grid.Rows.Count; i++)
{
    int chId = (int)grid.Rows[i].Tag;
    double rjC = double.Parse(grid.Rows[i].Cells[1].Value.ToString());
    aiTask.SetCJTemperature(chId, rjC);
}
// 每次 ReadData 都会用最新的 CustomCJTemperature 做补偿
aiTask.ReadData(ref buf, -1);
```

---

## 示例 9：开路热电偶检测（OTD）

对应：`Winform Open Thermocouple Detection\`

```csharp
var aiTask = new JY6314AITask(boardNum);
foreach (int ch in checkedChannels)
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);

aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 1;

Dictionary<int, ThermocoupleConnectionStatus> results = aiTask.DetectOpenThermocouple();
foreach (var kv in results)
    Console.WriteLine($"Ch{kv.Key}: {kv.Value}");   // Normal / OpenCircuit
```

---

## 示例 10：TB68CJ 在位检测

对应：`Winform Terminal Block State\`

```csharp
JY6314AITask aiTask = new JY6314AITask(boardNum);

// Timer 定时刷新端子状态
private void timer1_Tick(object s, EventArgs e)
{
    led_Connector0.Value = aiTask.BuildInCJC.SensorStatus == CJSensorConnectionStatus.Normal;
}

// 启动任务前主动抛错（若必须接 TB68CJ）
aiTask.BuildInCJC.ReportBuildInCJException(true, true);
```

---

## 示例 11：原始 i32 数据采集（绕过温度/电压换算，用于校准或自定义换算）

对应：`Winform AI Continuous MutiChannel RawDataI32\`

```csharp
aiTask.Mode             = AIMode.Continuous;
aiTask.SampleRate       = 1000;
int[,] rawBuf = new int[samples, channels];
aiTask.Start();

// 读取 i32 原始 ADC 码
aiTask.ReadRawData(ref rawBuf, samples, -1);

// —— 或者：读取 EMF(V) + 冷端温度(℃)，自己做温度换算 ——
double[,] emfBuf = new double[samples, channels];
double[,] cjtBuf = new double[samples, channels];
aiTask.ReadRawData(ref emfBuf, ref cjtBuf, samples, -1);
// 用工具换算：
double tempC = JY6314.Utility.Thermocouple.ConvertEMFToTemperature(
                  ThermocoupleType.TypeK, emfBuf[0,0] * 1e6 /* V → μV */, cjtBuf[0,0]);
```

---

## 示例 12：DI 单点读取（带 LED 反馈）

对应：`Digital Input\Winform DI Single Point\`

```csharp
using JY6314;

JY6314DITask ditask;
bool diValue;

private void Start_Click(object s, EventArgs e)
{
    ditask = new JY6314DITask(boardNum);
    for (int i = 0; i < channelCheckList.Items.Count; i++)
        if (channelCheckList.GetItemChecked(i))
            ditask.AddChannel(i);

    if (ditask.Channels.Count > 0)
    {
        ditask.Start();
        timerDI.Enabled = true;
    }
}

private void timerDI_Tick(object s, EventArgs e)
{
    timerDI.Enabled = false;
    for (int i = 0; i < channelCheckList.Items.Count; i++)
    {
        if (channelCheckList.GetItemChecked(i))
        {
            ditask.ReadSinglePoint(ref diValue, i);
            if (i == 0) led0.Value = diValue;
            if (i == 1) led1.Value = diValue;
        }
    }
    timerDI.Enabled = true;
}

private void Stop_Click(object s, EventArgs e)
{
    timerDI.Enabled = false;
    ditask?.Stop();
    ditask.Channels.Clear();
}
```

批量读取：
```csharp
bool[] values = new bool[ditask.Channels.Count];
ditask.ReadSinglePoint(ref values);      // 按 AddChannel 顺序
```

---

## 示例 13：DO 单点写入（开关联动）

对应：`Digital Output\Winform DO Single Point\`

```csharp
using JY6314;

JY6314DOTask dotask;

private void Start_Click(object s, EventArgs e)
{
    dotask = new JY6314DOTask(boardNum);
    dotask.AddChannel(new[] { 0, 1 });
    dotask.Start();
}

// 开关事件 → 写 DO
private void industrySwitch0_ValueChanged(object s, EventArgs e)
{
    dotask.WriteSinglePoint(industrySwitch0.Value, 0);
}

private void Stop_Click(object s, EventArgs e)
{
    dotask?.Stop();
    dotask.Channels.Clear();
}
```

批量写入：
```csharp
dotask.WriteSinglePoint(new[] { true, false, true });   // 按 AddChannel 顺序
```

---

## 示例 14：模块电压量程分档选择（工具方法）

JY6314 电压量程为四档对称：±1.25 V / ±0.625 V / ±0.3125 V / ±0.15625 V。根据实测幅值挑选最贴近但不饱和的档位：

```csharp
static (double lo, double hi) PickRange(double expectedAbsV)
{
    double[] ranges = { 0.15625, 0.3125, 0.625, 1.25 };
    foreach (var r in ranges)
        if (expectedAbsV <= r * 0.9)     // 10 % 余量
            return (-r, r);
    return (-1.25, 1.25);
}

var (lo, hi) = PickRange(0.4);           // → (-0.625, 0.625)
aiTask.AddChannel(0, lo, hi);
```

---

## 示例 15：常用错误处理骨架（生产级）

```csharp
using JY6314;

JY6314AITask aiTask = null;
try
{
    aiTask = new JY6314AITask(boardNum);
    aiTask.AddChannel(chList, ThermocoupleType.TypeK);
    aiTask.Mode = AIMode.Continuous;
    aiTask.SampleRate = 1000;

    // 关键：TB68CJ 不在位时立刻报错，不要等采集完才发现数据异常
    aiTask.BuildInCJC.ReportBuildInCJException(true, true);

    aiTask.Start();
    // ...
}
catch (JYDriverException ex)
{
    Log.Error($"[JY6314] {ex.Message}");
    MessageBox.Show($"JY6314 驱动错误：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
    aiTask?.Stop();
}
catch (Exception ex)
{
    Log.Error($"[JY6314] Unexpected: {ex}");
    aiTask?.Stop();
    throw;
}
finally
{
    // 连续/有限任务长期运行时不要在此 Stop；仅在 Dispose/FormClosing 走此路径
}

// FormClosing 兜底：
private void MainForm_FormClosing(object s, FormClosingEventArgs e)
{
    try { aiTask?.Stop(); } catch { /* 已经 stopped 时忽略 */ }
}
```

---

## 速查：示例与 API 的映射

| 示例编号 | 主要 API |
|---------|----------|
| 1 控制台单点 | `AIMode.Single`, `AddChannel(int, double, double)`, `ReadSinglePoint(ref double[])` |
| 2 Finite | `AIMode.Finite`, `SamplesToAcquire`, `ReadData(-1)` |
| 3 Continuous | `AIMode.Continuous`, `AvailableSamples`, `SignalExport.Add` |
| 4 多通道 | `AddChannel(int[], ThermocoupleType)`, `ReadData(ref double[,], -1)`, `ArrayManipulation.Transpose` |
| 5 数字触发 | `AITriggerType.Digital`, `Trigger.Digital.Source/Edge` |
| 6 软触发 | `AITriggerType.Soft`, `ReTriggerCount`, `SendSoftwareTrigger()` |
| 7 多卡同步 | `SignalExport.Add(SampleClock, PXI_Trig0)`, `SampleClock.External.Terminal` |
| 8 自定义 RJ | `BuildInCJC.Enabled = false`, `SetCJTemperature(chID, tempC)` |
| 9 OTD | `DetectOpenThermocouple()` |
| 10 TB68CJ 状态 | `BuildInCJC.SensorStatus`, `ReportBuildInCJException` |
| 11 原始数据 | `ReadRawData`, `Utility.Thermocouple.ConvertEMFToTemperature` |
| 12 DI | `JY6314DITask`, `ReadSinglePoint(ref bool, int)` |
| 13 DO | `JY6314DOTask`, `WriteSinglePoint(bool, int)` |
