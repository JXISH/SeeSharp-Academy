# JY6312 示例代码合集

> 根目录：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\`
> 解决方案：`JY6312.Examples.sln`
> 全部示例均基于 .NET Framework + `JY6312.dll` + `SeeSharpTools.JY.GUI/ArrayUtility.dll`。

---

## 0. 目录导航

| # | 示例目录 | 场景 | 入口 .cs |
|---|----------|------|----------|
| 1 | `Console AI Single Point\` | Console 单点热电偶 | `Console AI Single Point.cs` |
| 2 | `Console AI Single Point Raw Data\` | Console 单点 EMF | `Console AI Single Point RawData.cs` |
| 3 | `Winform AI Single Point MultiChannel\` | 多通道单点温度 | `Winform AI Single Point MultiChannel.cs` |
| 4 | `Winform AI Single Point MultiChannel Current Measure\` | 单点多通道电流 | 同名 .cs |
| 5 | `Winform AI Single Point MultiChannel Raw Data\` | 单点多通道 EMF | 同名 .cs |
| 6 | `Winform AI Finite\` | 单通道有限采集 | `Winform AI Finite.cs` |
| 7 | `Winform AI Finite MultiChannel\` | 多通道有限采集 | 同名 .cs |
| 8 | `Winform AI Finite Digital Trigger\` | 有限 + 数字触发 | 同名 .cs |
| 9 | `Winform AI Finite Current Measure\` | 单通道有限电流 | 同名 .cs |
| 10 | `Winform AI Finite MultiChannel Current Measure\` | 多通道有限电流 | 同名 .cs |
| 11 | `Winform AI Continuous\` | 单通道连续（内/外时钟） | `Winform AI Continuous.cs` |
| 12 | `Winform AI Continuous MutiChannel\` | 多通道连续热电偶 | `Winform AI Continuous MultiChannel.cs` |
| 13 | `Winform AI Continuous MutiChannel GeneralVoltage\` | 多通道连续电压 | `Winform AI Continuous MultiChannel Voltage.cs` |
| 14 | `Winform AI Continuous Digital Trigger\` | 连续 + 数字触发 | 同名 .cs |
| 15 | `Winform AI Continuous Soft Trigger\` | 连续 + 软件触发 | 同名 .cs |
| 16 | `Winform AI Continuous Custom RJ\` | 连续 + 自定义冷端 | 同名 .cs |
| 17 | `Winform AI Continuous Raw Data\` | 连续 + 原始数据 | 同名 .cs |
| 18 | `Winform AI Continuous Current Measure\` | 连续单通道电流 | 同名 .cs |
| 19 | `Winform AI Continuous MutiChannel Current Measure\` | 连续多通道电流 | `Winform AI Continuous MultiChannel Current.cs` |
| 20 | `Winform AI MultiRecord Soft Trigger\` | 软件触发 + Retrigger + Reference | 同名 .cs |
| 21 | `Winform AI_MultiCard SampleClock Sync\` | 多卡采样时钟同步 | 同名 .cs |
| 22 | `Winform Open Thermocouple Detection\` | 开路热电偶检测 (OTD) | `OpenThermocoupleDetection.cs` |
| 23 | `Winform Terminal Block State\` | TB68CJ 状态指示 | `Winform Terminal Block State.cs` |

---

## 1. Console 单点热电偶测量

**目录**：`Console AI Single Point\`

```csharp
using System;
using System.Threading;
using JY6312;

class Program
{
    static void Main()
    {
        JY6312AITask aiTask = null;
        double[] readValue = new double[1];
        try
        {
            aiTask = new JY6312AITask(boardNum: 0);
            aiTask.Mode = AIMode.Single;
            aiTask.AddChannel(0, ThermocoupleType.TypeK);
            aiTask.BuildInCJC.Enabled = true;
            aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
            aiTask.SampleRate = 1;          // 稳定窗口 1 s
            aiTask.Start();
            Thread.Sleep((int)(1000.0 / aiTask.SampleRate));
            aiTask.ReadSinglePoint(ref readValue);
            Console.WriteLine($"T = {readValue[0]:F3} ℃");
        }
        catch (JYDriverException ex) { Console.WriteLine(ex.Message); }
        finally { aiTask?.Stop(); }
    }
}
```

---

## 2. Console 单点 EMF（原始数据）

**目录**：`Console AI Single Point Raw Data\`

```csharp
aiTask.Mode = AIMode.Single;
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.SampleRate = 1;
aiTask.Start();
Thread.Sleep((int)(1000.0 / aiTask.SampleRate));

double[,] emf = new double[1, 1];   // V
double[,] cj  = new double[1, 1];   // ℃
aiTask.ReadRawData(ref emf, ref cj, timeout: 1000);

double tempC = Utility.Thermocouple.ConvertEMFToTemperature(
    ThermocoupleType.TypeK, emf[0, 0] * 1e6, cj[0, 0]);
```

---

## 3. 单点电压测量

```csharp
var aiTask = new JY6312AITask(0);

// 电压模式：±1.25V 量程
aiTask.AddChannel(0, -1.25, 1.25);
aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double[] voltage = new double[1];
aiTask.ReadSinglePoint(ref voltage);
Console.WriteLine($"电压值：{voltage[0]:F6} V");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 4. 单通道连续采集（内 / 外时钟切换）

**目录**：`Winform AI Continuous\`

```csharp
aiTask = new JY6312AITask(0);
aiTask.AddChannel(channelID, ThermocoupleType.TypeK);
aiTask.Mode = AIMode.Continuous;

if (rbInternal.Checked)
{
    aiTask.SampleClock.Source     = AISampleClockSource.Internal;
    aiTask.SampleRate             = double.Parse(tbRate.Text);  // 0.25 ~ 160
}
else
{
    aiTask.SampleClock.Source                = AISampleClockSource.External;
    aiTask.SampleClock.External.Terminal     = ClockTerminal.PFI0;
    aiTask.SampleClock.External.ExpectedRate = double.Parse(tbRate.Text);
}

aiTask.Start();

// Timer.Tick 中：
double[] buf = new double[(int)nudBlock.Value];
if (aiTask.AvailableSamples >= (ulong)buf.Length)
{
    aiTask.ReadData(ref buf, -1);
    stripChartX1.Plot(buf);
}
```

---

## 5. 多通道连续采集（热电偶）

**目录**：`Winform AI Continuous MutiChannel\`

```csharp
aiTask = new JY6312AITask(0);
foreach (int ch in selectedChannels)
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);

aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 50;
aiTask.Start();

int chCnt = aiTask.Channels.Count;
double[,] raw     = new double[blockSize, chCnt];
double[,] display = new double[chCnt, blockSize];
if (aiTask.AvailableSamples >= (ulong)blockSize)
{
    aiTask.ReadData(ref raw, 3000);
    SeeSharpTools.JY.ArrayUtility.ArrayManipulation.Transpose(raw, ref display);
    stripChartX1.Plot(display);
}
```

---

## 6. 多通道混合热电偶采集（不同类型）

```csharp
JY6312AITask aiTask = new JY6312AITask(0);

// 各通道独立配置热电偶类型
int[] channels = { 0, 1, 2, 3 };
ThermocoupleType[] types =
{
    ThermocoupleType.TypeK,
    ThermocoupleType.TypeJ,
    ThermocoupleType.TypeT,
    ThermocoupleType.TypeE,
};

aiTask.AddChannel(channels, types);
aiTask.BuildInCJC.Enabled = true;
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10;
aiTask.Start();

// 多通道读取：列存储 [采样点, 通道]
double[,] readValue = new double[100, 4];
while (aiTask.AvailableSamples < 100) Thread.Sleep(10);
aiTask.ReadData(ref readValue, 100, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 7. 多通道连续采集（电压模式）

**目录**：`Winform AI Continuous MutiChannel GeneralVoltage\`

```csharp
foreach (int ch in selectedChannels)
    aiTask.AddChannel(ch, rangeLow: -1.25, rangeHigh: 1.25);  // ±1.25 V
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 100;
// 其他读法同 §5
```

可选量程：±1.25 / ±0.625 / ±0.3125 / ±0.15625 / **±0.078125** V。

---

## 8. 连续 + 数字触发

**目录**：`Winform AI Continuous Digital Trigger\`

```csharp
aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Mode           = AITriggerMode.Start;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;
aiTask.Start();
```

等待 PFI0 上升沿到来后，采样才真正开始。

---

## 9. 连续 + 软件触发

**目录**：`Winform AI Continuous Soft Trigger\`

```csharp
aiTask.Trigger.Type = AITriggerType.Software;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Start();
// 按钮点击：
private void btnTrigger_Click(object s, EventArgs e) => aiTask.SendSoftwareTrigger();
```

---

## 10. 软件触发 + Retrigger + Reference 模式

**目录**：`Winform AI MultiRecord Soft Trigger\`

```csharp
aiTask.Mode             = AIMode.Finite;
aiTask.SampleRate       = 100;
aiTask.SamplesToAcquire = 500;

aiTask.Trigger.Type              = AITriggerType.Software;
aiTask.Trigger.Mode              = AITriggerMode.Reference;
aiTask.Trigger.ReTriggerCount    = 5;     // 触发 5 次后自动停止；-1 表示无限
aiTask.Trigger.PreTriggerSamples = 10;    // Reference 下的预触发点

aiTask.Start();
// 每次点击：
aiTask.SendSoftwareTrigger();
// 每次触发收齐 500 点后：
aiTask.ReadData(ref buf, -1);
```

---

## 11. 连续 + 自定义冷端温度

**目录**：`Winform AI Continuous Custom RJ\`

```csharp
aiTask.BuildInCJC.Enabled = false;                 // 关闭内置 CJC
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 10;
aiTask.Start();

// 通过外部温度计实时填入冷端温度
private void timerCJ_Tick(object s, EventArgs e)
{
    double cjT = ReadExternalCJTemperature();
    aiTask.SetCJTemperature(chID: 0, cjT);
}
```

---

## 12. 连续 + 原始数据（EMF + 冷端 T）

**目录**：`Winform AI Continuous Raw Data\`

```csharp
int chCnt   = aiTask.Channels.Count;
int samples = blockSize;
double[,] emf = new double[samples, chCnt];
double[,] cj  = new double[samples, chCnt];

if (aiTask.AvailableSamples >= (ulong)samples)
{
    aiTask.ReadRawData(ref emf, ref cj, timeout: 3000);
    // emf 单位 V，cj 单位 ℃
    // 可用 Utility.Thermocouple.ConvertEMFToTemperature 离线换算
}
```

---

## 13. 电流测量（外接分流电阻）

**目录**：`Winform AI Continuous Current Measure\` 与 `... MutiChannel Current Measure\`

```csharp
const double Rshunt = 10.0;     // Ω，精密电阻
aiTask.AddChannel(0, -1.25, 1.25);   // 电压模式 → ±125 mA
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 100;
aiTask.Start();

double[] voltage = new double[1000];
aiTask.ReadData(ref voltage, -1);
double[] currentA = voltage.Select(v => v / Rshunt).ToArray();
```

| 电压量程 | 对应电流（R=10Ω） |
|---------|------------------|
| ±1.25 V | ±125 mA |
| ±625 mV | ±62.5 mA |
| ±312.5 mV | ±31.25 mA |
| ±156.2 mV | ±15.62 mA |
| ±78.125 mV | ±7.8125 mA |

---

## 14. 开路热电偶检测 (OTD)

**目录**：`Winform Open Thermocouple Detection\`（主 .cs 为 `OpenThermocoupleDetection.cs`）

```csharp
aiTask = new JY6312AITask(0);
foreach (int ch in selectedChannels)
    aiTask.AddChannel(ch, ThermocoupleType.TypeK);
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 1;

// 不需要 Start()，驱动内部自行测试
ThermocoupleConnectionStatus[] result = aiTask.DetectOpenThermocouple();
for (int i = 0; i < result.Length; i++)
{
    // result[i] : Normal / OpenCircuit
    string state = result[i] == ThermocoupleConnectionStatus.OpenCircuit ? "开路" : "正常";
    Console.WriteLine($"Ch{aiTask.Channels[i].ID}: {state}");
}
```

---

## 15. TB68CJ 端子状态轮询

**目录**：`Winform Terminal Block State\`

```csharp
aiTask = new JY6312AITask(0);
// 状态定时器：
private void timer1_Tick(object s, EventArgs e)
{
    switch (aiTask.BuildInCJC.SensorStatus)
    {
        case CJSensorConnectionStatus.Normal:      lbl.Text = "TB68CJ Connected";    break;
        case CJSensorConnectionStatus.LostConnect: lbl.Text = "TB68CJ Disconnected"; break;
        case CJSensorConnectionStatus.Closed:      lbl.Text = "CJC Disabled";        break;
    }
}
```

可选调整 `aiTask.BuildInCJC.Advanced.Debouncing = 500;` 减少抖动误报。

---

## 16. 多卡采样时钟同步

**目录**：`Winform AI_MultiCard SampleClock Sync\`

```csharp
// 主卡
masterTask = new JY6312AITask(masterSlot);
masterTask.AddChannel(0, -1.25, 1.25);
masterTask.Mode             = AIMode.Finite;
masterTask.SampleRate       = 100;
masterTask.SamplesToAcquire = 1000;
masterTask.Trigger.Type     = AITriggerType.Immediately;
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

// 从卡
slaveTask = new JY6312AITask(slaveSlot);
slaveTask.AddChannel(0, -1.25, 1.25);
slaveTask.Mode                              = AIMode.Finite;
slaveTask.SamplesToAcquire                  = 1000;
slaveTask.SampleClock.Source                = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal     = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 100;

// 顺序非常关键：从卡必须先 Start（等待时钟），主卡后 Start
slaveTask.Start();
masterTask.Start();

double[] mBuf = new double[1000], sBuf = new double[1000];
masterTask.ReadData(ref mBuf, 5000);
slaveTask.ReadData(ref sBuf, 5000);
```

---

## 17. 有限采集 + 数字触发

**目录**：`Winform AI Finite Digital Trigger\`

```csharp
aiTask.Mode             = AIMode.Finite;
aiTask.SampleRate       = 100;
aiTask.SamplesToAcquire = 1000;

aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Mode           = AITriggerMode.Start;       // 或 Reference
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;

aiTask.Start();

// 轮询或事件等待任务完成
while (!aiTask.AITaskIsDone) Thread.Sleep(10);
double[] buf = new double[aiTask.SamplesToAcquire];
aiTask.ReadData(ref buf, -1);
```

---

## 18. 有限采集（多通道）

```csharp
JY6312AITask aiTask = new JY6312AITask(0);
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, ThermocoupleType.TypeK);

aiTask.BuildInCJC.Enabled = true;
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 200;
aiTask.SampleRate = 10;

aiTask.Start();

double[,] readValue = new double[200, 4];
aiTask.ReadData(ref readValue, 200, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 19. 50 / 60 Hz 工频抑制

```csharp
aiTask.SampleRate = 2;                                         // 必须 ≤ 8 Sa/s
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
aiTask.Start();

bool actuallyRejecting = aiTask.PowerLineRejection.RejectAt50Hz;
```

> 若 `SampleRate > 8 Sa/s`，`RejectAt50Hz / RejectAt60Hz` 永远为 `false`，
> 请降低采样率后再读该标志确认生效。

---

## 20. 连续温度采集完整 WinForm 示例

```csharp
using System;
using System.Windows.Forms;
using JY6312;

public partial class MainForm : Form
{
    private JY6312AITask aiTask;
    private double[,] readValue;

    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            int pointsPerRead = 10;
            int channelCount = 1;
            readValue = new double[pointsPerRead, channelCount];

            aiTask = new JY6312AITask(0);
            aiTask.AddChannel(0, ThermocoupleType.TypeK);
            aiTask.BuildInCJC.Enabled = true;
            aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 10;

            aiTask.CheckTerminalBlockConnectionStatusBeforeStart();
            aiTask.Start();
            timer1.Enabled = true;
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask.AvailableSamples >= (ulong)readValue.GetLength(0))
        {
            try
            {
                aiTask.ReadData(ref readValue, readValue.GetLength(0), -1);
                easyChartX_AI.Plot(readValue);
            }
            catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
        }
        timer1.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
    }
}
```

---

## 21. 通用骨架（全流程模板）

```csharp
JY6312AITask aiTask = null;
try
{
    // 1. 构造
    aiTask = new JY6312AITask(0);

    // 2. 通道
    aiTask.AddChannel(new[] {0, 1, 2, 3}, ThermocoupleType.TypeK);

    // 3. 模式 + 采样
    aiTask.Mode             = AIMode.Continuous;
    aiTask.SampleRate       = 100;

    // 4. (可选) 触发 / 时钟 / 导出
    aiTask.Trigger.Type = AITriggerType.Immediately;
    aiTask.SampleClock.Source = AISampleClockSource.Internal;

    // 5. (可选) CJC / 工频
    aiTask.BuildInCJC.Enabled = true;

    // 6. 启动 + 读取循环
    aiTask.Start();
    double[,] buf = new double[1000, aiTask.Channels.Count];
    while (running)
    {
        if (aiTask.AvailableSamples >= (ulong)buf.GetLength(0))
            aiTask.ReadData(ref buf, -1);
        else
            Thread.Sleep(5);
    }
}
catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
finally { aiTask?.Stop(); }
```

---

## 综合技巧

### 多通道数据解析

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[100, 4];
aiTask.ReadData(ref buf, 100, -1);

// 提取各通道数据
double[] ch0 = new double[100];
for (int i = 0; i < 100; i++) ch0[i] = buf[i, 0];
```

### 同时读取电压和温度（诊断校准用）

```csharp
double[,] voltages     = new double[100, 4];
double[,] temperatures = new double[100, 4];
aiTask.ReadRawData(ref voltages, ref temperatures, 100, -1);
// voltages：热电偶原始电压（V）；temperatures：经 CJC 补偿后的温度（℃）
```

### 冷端补偿策略选择

```csharp
// 1. 推荐：使用 TB-68CJ 内置冷端传感器
aiTask.BuildInCJC.Enabled = true;

// 2. 无 TB-68CJ：外接高精度 RTD / 温度计手动提供 RJ
aiTask.BuildInCJC.Enabled = false;
aiTask.SetCJTemperature(0, readFromExternalRTD());

// 3. 恒温槽环境（RJ 固定已知）：直接写常数
aiTask.SetCJTemperature(0, 0.0);   // 冰浴
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer1 != null) timer1.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```

---

## 参考索引

- 每个示例均含 `App.config`、`.Designer.cs`、`.resx` 与 `bin\Debug\`（含已编译可执行文件）。
- 所有 WinForm 工程都会引用 `SeeSharpTools.JY.GUI.StripChartX` 作图，引用路径为
  `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\SeeSharpTools.JY.GUI.dll`。
- 解决方案统一入口：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\JY6312.Examples.sln`。
