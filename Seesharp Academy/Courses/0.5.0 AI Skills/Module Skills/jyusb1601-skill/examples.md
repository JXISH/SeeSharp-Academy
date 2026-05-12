# JYUSB-1601 代码示例集

> 所有示例均来自 `JYUSB-1601-1.Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点采集（Console）

```csharp
using JYUSB1601;

// 1. 创建 AI Task（按板卡别名）
JYUSB1601AITask aiTask = new JYUSB1601AITask("USBDev0");

// 2. 设置模式
aiTask.Mode = AIMode.Single;

// 3. 添加通道：通道 0，量程 ±10V，单端接法
aiTask.AddChannel(0, -10, 10, AITerminal.RSE);

// 4. 启动 Task
aiTask.Start();

// 5. 循环读取单点
double readValue = 0;
aiTask.ReadSinglePoint(ref readValue, 0);   // 读取通道 0 的电压值
Console.WriteLine($"Channel 0: {readValue} V");

// 6. 停止并清除通道
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 2：AI 连续采集（WinForm + Timer）

```csharp
private JYUSB1601AITask aiTask;
private double[] readBuffer;

// 启动按钮
private void button_start_Click(object sender, EventArgs e)
{
    // 1. 创建 Task
    aiTask = new JYUSB1601AITask(textBox_cardName.Text);

    // 2. 添加通道（量程从 ComboBox 选择）
    aiTask.AddChannel(0, -10, 10, AITerminal.RSE);

    // 3. 配置连续模式参数
    aiTask.Mode = AIMode.Continuous;
    aiTask.SampleRate = (double)numericUpDown_sampleRate.Value;

    // 4. 启动 Task
    aiTask.Start();

    // 5. 分配读取缓冲区
    readBuffer = new double[(int)numericUpDown_samples.Value];

    // 6. 启动 Timer（每 10ms 刷新一次）
    timer_FetchData.Enabled = true;
}

// Timer Tick：轮询缓冲区并读取
private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    try
    {
        // 只有缓冲区数据足够时才读取，避免阻塞
        if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
        {
            // ReadData：timeout=-1 表示等待直到数据就绪
            aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);

            // 在 EasyChartX 上显示波形
            easyChartX1.Plot(readBuffer);
        }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
    timer_FetchData.Enabled = true;
}

// 停止按钮
private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (aiTask != null)
    {
        aiTask.Stop();
        aiTask.Channels.Clear();
    }
}

// 窗体关闭时确保资源释放
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    if (aiTask != null) aiTask.Stop();
}
```

---

## 示例 3：AI 有限采集（多通道）

```csharp
private JYUSB1601AITask aiTask;
private double[,] readValue;   // [采样点, 通道] 列存储

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JYUSB1601AITask("USBDev0");

    // 添加多个通道（通道 0、1、2，相同量程）
    int[] channels = { 0, 1, 2 };
    aiTask.AddChannel(channels, -10, 10, AITerminal.RSE);

    // 配置有限模式
    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 1000;   // 每通道采集 1000 点
    aiTask.SampleRate = 10000;

    aiTask.Start();

    // 分配多通道缓冲区 [采样点数, 通道数]
    readValue = new double[1000, 3];

    // WaitUntilDone 等待采集完成（超时 5000ms）
    aiTask.WaitUntilDone(5000);

    // 读取所有通道数据
    aiTask.ReadData(ref readValue, 1000, -1);

    aiTask.Stop();
    aiTask.Channels.Clear();

    // readValue[i, ch] 即第 i 采样点、ch 通道的电压值
}
```

---

## 示例 4：AI 数字触发有限采集

```csharp
aiTask = new JYUSB1601AITask("USBDev0");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 1000;
aiTask.SampleRate = 50000;

// 配置数字触发：DIO_0 上升沿触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发信号到来后自动开始采集
aiTask.WaitUntilDone(-1);

double[] buf = new double[1000];
aiTask.ReadData(ref buf, 1000, -1);
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 5：AI 软触发采集

```csharp
aiTask = new JYUSB1601AITask("USBDev0");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 500;
aiTask.SampleRate = 10000;
aiTask.Trigger.Type = AITriggerType.Soft;

aiTask.Start();

// 在需要时手动发送软触发
aiTask.SendSoftwareTrigger();

aiTask.WaitUntilDone(5000);
double[] buf = new double[500];
aiTask.ReadData(ref buf, 500, -1);
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 6：AI 外部时钟采集

```csharp
aiTask = new JYUSB1601AITask("USBDev0");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;

// 使用外部时钟（外部信号从板卡时钟输入引脚引入）
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 10000;  // 告知驱动期望频率

aiTask.Start();
// 后续读取同连续模式...
```

---

## 示例 7：AI 录制模式（数据流写入文件）

```csharp
aiTask = new JYUSB1601AITask("USBDev0");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 50000;

// 配置录制参数
aiTask.Record.FilePath   = @"C:\Data\signal.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode       = RecordMode.Finite;
aiTask.Record.Length     = 10.0;   // 录制 10 秒

aiTask.Start();

// 可在录制期间预览数据
double[] preview = new double[1000];
aiTask.GetRecordPreviewData(ref preview, 1000, -1);
easyChartX1.Plot(preview);

// 等待录制完成
double recordedLen;
bool recordDone;
do
{
    aiTask.GetRecordStatus(ref recordedLen, ref recordDone);
} while (!recordDone);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 8：AO 单点输出（Console）

```csharp
JYUSB1601AOTask aoTask = new JYUSB1601AOTask("USBDev0");

// AO 通道仅有 0 和 1，添加两个通道
aoTask.AddChannel(0);
aoTask.AddChannel(1);

aoTask.Mode = AOMode.Single;
aoTask.Start();

// 输出指定电压（索引与通道一一对应）
double[] writeValues = { 3.0, 0.0 };    // CH0=3V，CH1=0V
aoTask.WriteSinglePoint(writeValues);

// 动态更新输出值
writeValues[0] = 5.0;
aoTask.WriteSinglePoint(writeValues);

aoTask.Stop();
aoTask.Channels.Clear();
```

---

## 示例 9：AO 连续环形输出（Wrapping）

```csharp
using SeeSharpTools.JY.DSP.Fundamental;  // 需要 Generation 类生成波形

private JYUSB1601AOTask aoTask;
private double[] writeBuffer;

private void button_start_Click(object sender, EventArgs e)
{
    aoTask = new JYUSB1601AOTask("USBDev0");
    aoTask.AddChannel(0);
    aoTask.Mode = AOMode.ContinuousWrapping;
    aoTask.UpdateRate = 100000;     // 100 kSa/s
    aoTask.SamplesToUpdate = 1000;  // 缓冲区大小 1000 点

    // 使用 SeeSharpTools 生成正弦波
    writeBuffer = new double[1000];
    Generation.SineWave(ref writeBuffer, 5.0, 0, 50.0, aoTask.UpdateRate);
    // 参数：幅值 5V，初相 0°，频率 50Hz，采样率 100kSa/s

    // 先写数据，再 Start（Wrapping 模式要求）
    aoTask.WriteData(writeBuffer, -1);
    aoTask.Start();

    // 在 EasyChartX 上预览输出波形
    easyChartX_AO.Plot(writeBuffer);
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (aoTask != null)
    {
        aoTask.Stop();
        aoTask.Channels.Clear();
    }
    easyChartX_AO.Clear();
}
```

---

## 示例 10：AO 有限输出（带触发）

```csharp
aoTask = new JYUSB1601AOTask("USBDev0");
aoTask.AddChannel(0);
aoTask.Mode = AOMode.Finite;
aoTask.UpdateRate = 50000;
aoTask.SamplesToUpdate = 500;

// 数字触发：DIO_0 上升沿触发输出
aoTask.Trigger.Type = AOTriggerType.Digital;
aoTask.Trigger.Digital.Source = AODigitalTriggerSource.DIO_0;
aoTask.Trigger.Digital.Edge   = AODigitalTriggerEdge.Rising;

double[] waveform = new double[500];
Generation.SineWave(ref waveform, 5.0, 0, 100.0, 50000);
aoTask.WriteData(waveform, -1);

aoTask.Start();              // 等待触发信号
aoTask.WaitUntilDone(-1);   // 输出完成后返回
aoTask.Stop();
aoTask.Channels.Clear();
```

---

## 示例 11：CI 频率测量（WinForm + Timer）

```csharp
private JYUSB1601CITask ciTask;
private double measValue;

private void button_start_Click(object sender, EventArgs e)
{
    // 计数器通道 0（CTR0_GATE 引脚连接被测信号）
    ciTask = new JYUSB1601CITask("USBDev0", 0);
    ciTask.Type = CIType.Frequency;

    ciTask.Start();
    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    try
    {
        // timeout=0 表示立即读取当前测量值（非阻塞）
        ciTask.ReadSinglePoint(ref measValue, 0);
        label_freq.Text = $"{measValue:F2} Hz";
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
    timer_FetchData.Enabled = true;
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (ciTask != null) ciTask.Stop();
}
```

---

## 示例 12：CI 边沿计数

```csharp
ciTask = new JYUSB1601CITask("USBDev0", 0);
ciTask.Type = CIType.EdgeCounting;
ciTask.EdgeCounting.InitialCount = 0;        // 初始计数值
ciTask.EdgeCounting.Direction = CountDirection.Up;  // 上升计数

ciTask.Start();

uint count = 0;
ciTask.ReadSinglePoint(ref count);   // 读取当前计数值
Console.WriteLine($"Count: {count}");

ciTask.Stop();
```

---

## 示例 13：CI 脉冲宽度测量

```csharp
ciTask = new JYUSB1601CITask("USBDev0", 0);
ciTask.Type = CIType.Pulse;

ciTask.Start();

double highTime = 0, lowTime = 0;
// 阻塞等待一次完整脉冲测量，timeout=-1 永久等待
ciTask.ReadSinglePoint(ref highTime, ref lowTime, -1);
Console.WriteLine($"高电平: {highTime * 1000:F3} ms，低电平: {lowTime * 1000:F3} ms");

ciTask.Stop();
```

---

## 示例 14：CO 脉冲输出

```csharp
JYUSB1601COTask coTask = new JYUSB1601COTask("USBDev0", 0);

// 输出 1kHz，占空比 50%，无限脉冲
var pulse = new COPulse(COPulseType.DutyCycleFrequency,
    1000.0,   // 频率 1 kHz
    0.5,      // 占空比 50%
    -1);      // -1 = 无限输出

coTask.WriteSinglePoint(pulse);
coTask.Start();

Console.WriteLine("脉冲输出中，按任意键停止...");
Console.ReadKey();

coTask.Stop();
```

---

## 示例 15：DI / DO 数字 I/O

```csharp
// 数字输入：读取 DIO_0
var diTask = new JYUSB1601DITask("USBDev0");
diTask.AddChannel(0);
diTask.Start();
bool diValue = false;
diTask.ReadSinglePoint(ref diValue, 0);
Console.WriteLine($"DIO_0 = {diValue}");
diTask.Stop();
diTask.Channels.Clear();

// 数字输出：控制 DIO_0
var doTask = new JYUSB1601DOTask("USBDev0");
doTask.AddChannel(0);
doTask.Start();
doTask.WriteSinglePoint(true, 0);   // 置高
System.Threading.Thread.Sleep(500);
doTask.WriteSinglePoint(false, 0);  // 置低
doTask.Stop();
doTask.Channels.Clear();
```

---

## 示例 16：AI 信号导出（同步两个 Task）

```csharp
// 将 AI 采样时钟导出到 DIO_0，供外部设备同步
aiTask = new JYUSB1601AITask("USBDev0");
aiTask.AddChannel(0, -10, 10);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;

// 信号导出：AI SampleClock → DIO_0
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,
                        SignalExportDestination.DIO_0);

aiTask.Start();
// DIO_0 引脚现在输出与 AI 采样时钟同步的脉冲信号
```

---

## 综合技巧

### 多通道数据解析

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[1000, 3];   // 1000 点，3 通道
aiTask.ReadData(ref buf, 1000, -1);

// 提取各通道数据
double[] ch0 = new double[1000];
double[] ch1 = new double[1000];
for (int i = 0; i < 1000; i++)
{
    ch0[i] = buf[i, 0];
    ch1[i] = buf[i, 1];
}
```

### 量程与接线方式选择

```csharp
// 量程选择原则：选择略大于信号峰值的量程，提高精度
// 单端（RSE）：16 通道，适合信号源与地共用
// 差分（Differential）：8 通道，适合抗共模干扰

aiTask.AddChannel(0, -5, 5, AITerminal.Differential);  // ±5V 差分
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timerFetch != null) timerFetch.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```
