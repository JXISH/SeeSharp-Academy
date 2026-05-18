# JYUSB-1202 AI 完整代码示例

## 示例 1：单通道连续采集（Winform + Timer）

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

            aiTask.Mode            = AIMode.Continuous;
            aiTask.SampleRate      = (double)nudSampleRate.Value;
            aiTask.SamplesToAcquire = (int)nudSamples.Value;

            JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
            aiTask.Start();

            readBuf = new double[(int)nudSamples.Value];
            timer.Enabled = true;
            btnStart.Enabled = false;
            btnStop.Enabled  = true;
            lblStatus.Text   = "采集中...";
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void timer_Tick(object sender, EventArgs e)
    {
        timer.Enabled = false;
        try
        {
            if (aiTask.AvailableSamples >= (ulong)readBuf.Length)
            {
                aiTask.ReadData(ref readBuf, readBuf.Length, -1);
                chart.Plot(readBuf);          // EasyChartX 或自定义图表
            }
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
        timer.Enabled = true;
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        timer.Enabled = false;
        try
        {
            aiTask?.Stop();
            aiTask?.Channels.Clear();
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }

        btnStart.Enabled = true;
        btnStop.Enabled  = false;
        lblStatus.Text   = "已停止";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        try { aiTask?.Stop(); } catch { }
    }

    // 量程下拉框联动
    private void cmbRange_SelectedIndexChanged(object sender, EventArgs e)
    {
        double[] ranges = { 12, 5, 1.25, 0.32 };
        double r = ranges[cmbRange.SelectedIndex];
        lowRange  = -r;
        highRange =  r;
    }

    // 卡名失焦后查询最大采样率
    private void txtCardName_Leave(object sender, EventArgs e)
    {
        var cap = JYUSB1202Device.GetCapability(txtCardName.Text);
        nudSampleRate.Maximum = (decimal)cap.AI.MaxSampleRate;
        nudSampleRate.Value   = (decimal)cap.AI.MaxSampleRate;
    }
}
```

---

## 示例 2：多通道连续采集

```csharp
// 在 button_start_Click 中
aiTask = new JYUSB1202AITask(txtCardName.Text);

// 循环添加勾选的通道（通道 0-3）
for (int i = 0; i < 4; i++)
{
    if (checkedListBox.GetItemChecked(i))
        aiTask.AddChannel(i, lowRange, highRange,
            AITerminal.Differential, AICoupling.DC, false);
}

aiTask.Mode            = AIMode.Continuous;
aiTask.SampleRate      = sampleRate;          // 多通道时实际速率 = 设定值 / N
aiTask.SamplesToAcquire = samplesPerRead;

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

// 分配多通道缓冲区 [采样点数, 通道数]
double[,] buf = new double[samplesPerRead, aiTask.Channels.Count];

// Timer Tick 中读取多通道数据
if (aiTask.AvailableSamples >= (ulong)samplesPerRead)
{
    aiTask.ReadData(ref buf, samplesPerRead, -1);
    // 用列优先方式绘制：每列 = 一个通道
    easyChartX.Plot(buf, 0, 1, SeeSharpTools.JY.GUI.MajorOrder.Column);
}
```

---

## 示例 3：单通道有限采集

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode            = AIMode.Finite;
aiTask.SamplesToAcquire = 5000;     // 必须在 BufLenInSamples 以内
aiTask.SampleRate      = 50000;

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

// 方式 A：轮询等待（适合 Timer 回调）
double[] buf = new double[5000];
// Timer Tick：
if (aiTask.AvailableSamples >= 5000)
{
    aiTask.ReadData(ref buf, 5000, -1);
    aiTask.Stop();
    aiTask.Channels.Clear();
    timer.Enabled = false;
}

// 方式 B：阻塞等待完成
aiTask.WaitUntilDone(-1);
aiTask.ReadData(ref buf, 5000, -1);
aiTask.Stop();
```

---

## 示例 4：软件触发（Continuous + Soft Trigger）

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 10000;
aiTask.Trigger.Type = AITriggerType.Soft;     // 关键：设置软触发

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();   // 启动后等待软触发，不会立即采数据

// 用户按下"发送触发"按钮后：
private void btnSendTrigger_Click(object sender, EventArgs e)
{
    aiTask.SendSoftwareTrigger();   // 触发后才开始采数据
    btnSendTrigger.Enabled = false;
}
```

---

## 示例 5：数字触发（Continuous + Digital Trigger）

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 10000;
aiTask.SamplesToAcquire = 1000;

// 数字触发配置
aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();
lblStatus.Text = "等待 DIO_0 上升沿触发...";
// Timer 轮询与连续模式相同
```

---

## 示例 6：多通道有限采集 + 数字触发

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);

// 添加 4 个通道
aiTask.AddChannel(new int[]{0,1,2,3}, -10.0, 10.0,
    AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode            = AIMode.Finite;
aiTask.SamplesToAcquire = 2000;
aiTask.SampleRate      = 10000;   // 4 通道下每通道实际 2.5kSa/s

aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_1;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Falling;

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

// Timer Tick 等待数据
double[,] buf = new double[2000, 4];
if (aiTask.AvailableSamples >= 2000)
{
    aiTask.ReadData(ref buf, 2000, -1);
    aiTask.Stop();
    aiTask.Channels.Clear();
}
```

---

## 示例 7：录制（Record 模式流式存盘）

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode       = AIMode.Record;
aiTask.SampleRate = 50000;

// 录制配置
aiTask.Record.FilePath   = @"C:\Data\output.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode       = RecordMode.Finite;
aiTask.Record.Length     = 10.0;   // 录制 10 秒

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();

// Timer Tick 中轮询状态
double recordedLen;
bool   isDone;
aiTask.GetRecordStatus(out recordedLen, out isDone);
lblStatus.Text = $"已录制 {recordedLen:F1} 秒";
if (isDone)
{
    aiTask.Stop();
    timer.Enabled = false;
}

// 同时预览当前数据
double[] preview = new double[1000];
aiTask.GetRecordPreviewData(ref preview, 1000, 100);
```

---

## 示例 8：外部时钟采样

```csharp
aiTask = new JYUSB1202AITask(txtCardName.Text);
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

aiTask.Mode = AIMode.Continuous;

// 外部时钟配置（时钟信号接至设备外部时钟输入端）
aiTask.SampleClock.Source              = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 5000;   // 预期外部时钟频率

JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);
aiTask.Start();
```

---

## 示例 9：信号导出（将采样时钟导出至 DIO_0）

```csharp
// 在 aiTask.Start() 之前配置
aiTask.SignalExport.Add(
    AISignalExportSource.SampleClock,
    SignalExportDestination.DIO_0);
```

---

## 常见陷阱

| 问题 | 原因 | 解决 |
|------|------|------|
| `BufferDataOverflow` | 读取太慢，驱动缓冲区溢出 | 缩短 Timer 间隔或减少每次读取点数 |
| `ReadDataTimeout` | 缓冲区点数不足且设置了超时 | 用 `AvailableSamples` 判断后再读，或 timeout=-1 |
| 多通道采样率超限 | N 通道时最大速率 = 250k/N | 降低 `SampleRate` |
| `TaskHasStarted...` 异常 | Start 后仍修改参数 | 先 Stop，再修改，再 Start |
| 数据形状错误 | 多通道 `double[,]` 维度写反 | `new double[samplesPerCh, channelCount]` |
| 编译目标不对 | JYUSB1202.dll 为 32 位 | 项目目标平台设为 **x86** |
