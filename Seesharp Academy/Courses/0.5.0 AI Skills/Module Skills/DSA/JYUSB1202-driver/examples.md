# JYUSB1202 完整代码示例

## 示例导航

| # | 功能 | 模式 |
|---|------|------|
| 1 | AI 单通道连续采集（WinForm + Timer） | Continuous |
| 2 | AI 多通道连续采集 | Continuous |
| 3 | AI 单通道有限采集 | Finite |
| 4 | AI 软件触发 | Soft Trigger |
| 5 | AI 数字触发 | Digital Trigger |
| 6 | AI 多通道有限 + 数字触发 | Finite + Digital |
| 7 | AI IEPE 传感器采集 | Continuous + IEPE |
| 8 | AI 外部时钟 | External Clock |
| 9 | AI 信号导出到 DIO | Signal Export |
| 10 | AI 录制（Finite Streaming） | Record Finite |
| 11 | AI 录制（Infinite Streaming） | Record Infinite |
| 12 | 录制数据回放 | Playback |
| 13 | 设备能力查询 | Device Properties |
| 14 | CI 边沿计数 | Counter Input |
| 15 | CI 频率测量 | Counter Input |
| 16 | CI 正交编码器 | Counter Input |
| 17 | CO 脉冲输出 | Counter Output |
| 18 | DI 单点读取 | Digital Input |
| 19 | DO 单点输出 | Digital Output |

---

## 1. AI 单通道连续采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JYUSB1202;

public partial class MainForm : Form
{
    private JYUSB1202AITask aiTask;
    private double[] readBuf;
    private double lowRange = -12, highRange = 12;

    private void btnStart_Click(object sender, EventArgs e)
    {
        try
        {
            aiTask = new JYUSB1202AITask(txtCardName.Text);
            aiTask.AddChannel(
                (int)cmbChannel.SelectedItem,
                lowRange, highRange,
                (AITerminal)Enum.Parse(typeof(AITerminal), cmbTerminal.Text),
                (AICoupling)Enum.Parse(typeof(AICoupling), cmbCoupling.Text),
                chkIEPE.Checked);

            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = (double)nudSampleRate.Value;
            aiTask.SamplesToAcquire = (int)nudSamples.Value;

            JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
            aiTask.Start();

            readBuf = new double[(int)nudSamples.Value];
            timer.Enabled = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    }

    private void timer_Tick(object sender, EventArgs e)
    {
        timer.Enabled = false;
        try
        {
            if (aiTask.AvailableSamples >= (ulong)readBuf.Length)
            {
                aiTask.ReadData(ref readBuf, readBuf.Length, -1);
                // chart.Plot(readBuf);
            }
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
        timer.Enabled = true;
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        timer.Enabled = false;
        try { aiTask?.Stop(); aiTask?.Channels.Clear(); }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
        btnStart.Enabled = true;
        btnStop.Enabled = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        try { aiTask?.Stop(); } catch { }
    }

    // 量程下拉联动
    private void cmbRange_SelectedIndexChanged(object sender, EventArgs e)
    {
        double[] ranges = { 12, 5, 1.25, 0.32 };
        double r = ranges[cmbRange.SelectedIndex];
        lowRange = -r; highRange = r;
    }

    // 卡名失焦后查询最大采样率
    private void txtCardName_Leave(object sender, EventArgs e)
    {
        var cap = JYUSB1202Device.GetCapability(txtCardName.Text);
        nudSampleRate.Maximum = (decimal)cap.AI.MaxSampleRate;
    }
}
```

---

## 2. AI 多通道连续采集

```csharp
using JYUSB1202;
using SeeSharpTools.JY.ArrayUtility;

aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 256000;

int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);
aiTask.Start();

int samplesToRead = 10000;
double[,] dataToRead = new double[samplesToRead, aiTask.Channels.Count];
double[,] dataToPlot = new double[aiTask.Channels.Count, samplesToRead];

// Timer Tick
if (aiTask.AvailableSamples >= (ulong)samplesToRead)
{
    aiTask.ReadData(ref dataToRead, 2000);
    ArrayManipulation.Transpose(dataToRead, ref dataToPlot);
    // easyChartX1.Plot(dataToPlot);
}
```

---

## 3. AI 单通道有限采集

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 5000;
aiTask.SampleRate = 50000;

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

double[] buf = new double[5000];

// 方式 A：轮询等待（适合 Timer）
if (aiTask.AvailableSamples >= 5000)
{
    aiTask.ReadData(ref buf, 5000, -1);
    aiTask.Stop();
    aiTask.Channels.Clear();
}

// 方式 B：阻塞等待
aiTask.WaitUntilDone(-1);
aiTask.ReadData(ref buf, 5000, -1);
aiTask.Stop();
```

---

## 4. AI 软件触发

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 100000;
aiTask.SamplesToAcquire = 10000;
aiTask.Trigger.Type = AITriggerType.Soft;

aiTask.Start();  // 启动后等待软触发

// 业务逻辑就绪后发送触发
aiTask.SendSoftwareTrigger();

double[,] data = new double[aiTask.SamplesToAcquire, 1];
aiTask.ReadData(ref data, 5000);
aiTask.Stop();
```

---

## 5. AI 数字触发

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 256000;
aiTask.SamplesToAcquire = 50000;

// DIO_0 上升沿触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// Timer 轮询 AvailableSamples 后 ReadData
```

---

## 6. AI 多通道有限 + 数字触发

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.AddChannel(new int[]{0,1,2,3}, -10.0, 10.0,
    AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 2000;
aiTask.SampleRate = 10000;  // 4 通道下每通道实际 2.5kSa/s

aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_1;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Falling;

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

double[,] buf = new double[2000, 4];
if (aiTask.AvailableSamples >= 2000)
{
    aiTask.ReadData(ref buf, 2000, -1);
    aiTask.Stop();
    aiTask.Channels.Clear();
}
```

---

## 7. AI IEPE 传感器采集

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 256000;

// 启用 IEPE 激励（4mA），AC 耦合，±5V 量程
// 适用于加速度计、麦克风等 IEPE 传感器
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.AC, true);

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

double[,] dataBuffer = new double[aiTask.SamplesToAcquire, 1];
while (aiTask.AvailableSamples < (ulong)aiTask.SamplesToAcquire)
    System.Windows.Forms.Application.DoEvents();
aiTask.ReadData(ref dataBuffer, 5000);
```

---

## 8. AI 外部时钟

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 5000;  // 预期外部时钟频率

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();
```

---

## 9. AI 信号导出到 DIO

```csharp
aiTask = new JYUSB1202AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100000;
aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

// 将采样时钟输出到 DIO_2
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.DIO_2);
// 将启动触发信号输出到 DIO_1
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.DIO_1);

aiTask.Start();
```

---

## 10. AI 录制（Finite Streaming）

```csharp
private JYUSB1202AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JYUSB1202AITask(0);
        aiTask.AddChannel(0, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 256000;
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.FileFormat = FileFormat.Bin;
        aiTask.Record.Mode = RecordMode.Finite;
        aiTask.Record.Length = 10.0;  // 录制 10 秒

        aiTask.Start();
        previewData = new double[1000, aiTask.Channels.Count];
        timer_Preview.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    double recordedLength;
    bool recordDone;
    aiTask.GetRecordStatus(out recordedLength, out recordDone);

    if (recordDone)
    {
        try { aiTask?.Stop(); } catch { }
        MessageBox.Show("录制完成！");
    }
    else
    {
        if (aiTask.AvailableSamples >= 1000)
            aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
        timer_Preview.Enabled = true;
    }
}
```

---

## 11. AI 录制（Infinite Streaming）

```csharp
aiTask = new JYUSB1202AITask(0);
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, -5.0, 5.0, AITerminal.PSEUDIFF, AICoupling.DC, false);

aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 51200;
aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Infinite;

aiTask.Start();
double[,] previewData = new double[1000, aiTask.Channels.Count];
timer_Preview.Enabled = true;

// Timer Tick
private void timer_Preview_Tick(object sender, EventArgs e)
{
    if (aiTask.AvailableSamples >= 1000)
        aiTask.GetRecordPreviewData(ref previewData, 1000, 1000);
}

// 手动停止
private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    aiTask?.Stop();
}
```

---

## 12. 录制数据回放

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private double[,] playbackData;

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;

    int channelCount = 4;
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    playbackData = new double[1000, channelCount];
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        for (int i = 0; i < playbackData.GetLength(0); i++)
            for (int ch = 0; ch < playbackData.GetLength(1); ch++)
                if (playbackStream.Position < playbackStream.Length)
                    playbackData[i, ch] = playbackReader.ReadDouble();
        // easyChartX1.Plot(playbackData);
    }
    catch (EndOfStreamException)
    {
        timer_Playback.Enabled = false;
        playbackReader.Close();
        playbackStream.Close();
    }
}
```

---

## 13. 设备能力查询

```csharp
private void MainForm_Load(object sender, EventArgs e)
{
    int slotNumber = 0;
    int totalChannels = JYUSB1202Device.GetCapability(slotNumber).AI.NumberOfChannels;
    double minRate = JYUSB1202Device.GetCapability(slotNumber).AI.MinSampleRate;
    double maxRate = JYUSB1202Device.GetCapability(slotNumber).AI.MaxSampleRate;

    numericUpDown_SampleRate.Minimum = (decimal)minRate;
    numericUpDown_SampleRate.Maximum = (decimal)maxRate;

    for (int i = 0; i < totalChannels; i++)
        comboBox_Channel.Items.Add($"Ch{i}");
}
```

---

## 14. CI 边沿计数

```csharp
JYUSB1202CITask ciTask = new JYUSB1202CITask();
ciTask.Type = CIType.EdgeCounting;
ciTask.EdgeCounting.InitialCount = 0;
ciTask.EdgeCounting.Direction = CountDirection.Up;

ciTask.Start();

uint count;
ciTask.ReadSinglePoint(out count);
Console.WriteLine($"Edge count: {count}");

ciTask.Stop();
```

---

## 15. CI 频率测量

```csharp
JYUSB1202CITask ciTask = new JYUSB1202CITask();
ciTask.Type = CIType.Frequency;

ciTask.Start();

double frequency;
ciTask.ReadSinglePoint(out frequency, 5000);
Console.WriteLine($"Frequency: {frequency} Hz");

ciTask.Stop();
```

---

## 16. CI 正交编码器

```csharp
JYUSB1202CITask ciTask = new JYUSB1202CITask();
ciTask.Type = CIType.QuadEncoder;
ciTask.QuadEncoder.EncodingType = QuadEncodingType.X4;
ciTask.QuadEncoder.InitialCount = 0;

ciTask.Start();

double position;
ciTask.ReadSinglePoint(out position, 1000);

ciTask.Stop();
```

---

## 17. CO 脉冲输出（频率+占空比）

```csharp
JYUSB1202COTask coTask = new JYUSB1202COTask();
coTask.IdleState = COIdleState.LowLevel;

// 1 kHz, 50% 占空比, 100 个脉冲
COPulse pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, 100);
coTask.WriteSinglePoint(pulse);
coTask.Start();

coTask.WaitUntilDone(-1);
coTask.Stop();
```

---

## 18. DI 单点读取

```csharp
JYUSB1202DITask diTask = new JYUSB1202DITask();
diTask.AddChannel(new int[] { 0, 1, 2, 3 });
diTask.Start();

bool[] values;
diTask.ReadSinglePoint(out values);
for (int i = 0; i < values.Length; i++)
    Console.WriteLine($"DIO_{i}: {values[i]}");

diTask.Stop();
```

---

## 19. DO 单点输出

```csharp
JYUSB1202DOTask doTask = new JYUSB1202DOTask();
doTask.AddChannel(new int[] { 0, 1, 2, 3 });
doTask.Start();

bool[] values = { true, false, true, false };
doTask.WriteSinglePoint(values);

doTask.Stop();
```

---

## 常见陷阱

| 问题 | 原因 | 解决 |
|------|------|------|
| `BufferDataOverflow` | 读取太慢，驱动缓冲区溢出 | 缩短 Timer 间隔或减少每次读取点数 |
| `ReadDataTimeout` | 缓冲区点数不足且设置了超时 | 用 `AvailableSamples` 判断后再读，或 timeout=-1 |
| 多通道采样率超限 | N 通道时最大速率 = 256k/N（P7） | 降低 `SampleRate` |
| `TaskHasStarted...` 异常 | Start 后仍修改参数 | 先 Stop，再修改，再 Start |
| 数据形状错误 | 多通道 `double[,]` 维度写反 | `new double[samplesPerCh, channelCount]` |
| 编译目标不对 | JYUSB1202.dll 为 32 位 | 项目目标平台设为 **x86** |
| IEPE 不生效 | 耦合设为 DC | IEPE 传感器应使用 `AICoupling.AC` |
