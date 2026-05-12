# JY6311 驱动开发指南

## 简介与适用场景

本技能提供 JYTEK JY6311 16通道RTD温度采集卡的完整 C# 驱动开发指引。JY6311是一款专为PT100/PT1000热电阻温度测量设计的高精度数据采集卡，支持电阻测量、温度测量和电压测量三种模式。

**适用场景：**
- 工业温度监测与控制系统
- PT100/PT1000热电阻温度采集
- 电阻精密测量（0~400Ω/0~1600Ω）
- 低电压信号采集（±1.25V至±38mV）
- 多通道同步温度数据记录

**自动触发关键词：** JY6311、JY6311AITask、JY6311DITask、JY6311DOTask、PT100、PT1000、RTD、温度采集、热电阻

---

## 驱动引用信息

**DLL文件路径：**
- 驱动DLL：`c:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.dll`
- XML文档：`c:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.xml`

**引用方式：**
```csharp
using JY6311;
```

**依赖库（可选，用于数据可视化）：**
- `SeeSharpTools.JY.GUI.dll` - 图表控件
- `SeeSharpTools.JY.ArrayUtility.dll` - 数组操作工具

---

## 硬件规格

### 模拟输入（AI）规格
| 参数 | 规格 |
|------|------|
| 通道数 | 16通道 |
| 测量类型 | 温度/电阻/电压 |
| RTD类型 | PT100 (0~400Ω)、PT1000 (0~1600Ω) |
| 温度系数(TCR) | Pt100_TCR3851/3916/3920/3911/3928、Pt1000_TCR3851/3750 |
| 接线方式 | 2线/3线/4线制 |
| 电压量程 | ±1.25V、±0.625V、±0.3125V、±0.15625V、±0.078125V、±0.0390625V |
| 采样率范围 | 0.275 Sa/s ~ 3000 Sa/s（每通道） |
| 工频抑制 | 50Hz/60Hz（总采样率≤40Sa/s时） |

### 数字I/O规格
| 参数 | 规格 |
|------|------|
| 数字输入 | 3路（PFI0~PFI2） |
| 数字输出 | 3路（PFI0~PFI2） |

### 触发与时钟
| 参数 | 规格 |
|------|------|
| 触发类型 | 立即触发、数字触发、软件触发 |
| 触发模式 | 开始触发、参考触发 |
| 数字触发源 | PFI0~PFI2、PXI_Trig0~7 |
| 采样时钟源 | 内部/外部 |
| 外部时钟端子 | PFI0~PFI2、PXI_Trig0~7 |

---

## 核心类与枚举

### 核心任务类

#### JY6311AITask - 模拟输入任务类
```csharp
// 构造函数
public JY6311AITask(int boardNum);           // 通过板卡号创建
public JY6311AITask(string boardName);       // 通过板卡名称创建

// 主要属性
public AIMode Mode { get; set; }              // 采集模式（单点/有限/连续）
public double SampleRate { get; set; }        // 采样率（Sa/s）
public int SamplesToAcquire { get; set; }     // 每通道采集点数（有限模式）
public ulong AvailableSamples { get; }         // 缓冲区可读样本数
public AITrigger Trigger { get; }              // 触发配置
public AISampleClock SampleClock { get; }      // 采样时钟配置
public AISignalExport SignalExport { get; }    // 信号导出配置
public List<AIChannel> Channels { get; }       // 通道列表
public PowerLineFrequency PowerLineFrequency { get; set; } // 工频抑制

// 主要方法
public void AddChannel(int chID, RTDType rtdType, RTDTerminal rtdTerminal, RTDTCRType rtdTCRType);  // 温度测量
public void AddChannel(int chID, RTDType rtdType, RTDTerminal rtdTerminal);  // 电阻测量
public void AddChannel(int chID, double voltageRangeLow, double voltageRangeHigh);  // 电压测量
public void RemoveChannel(int channelID);
public void Start();
public void Stop();
public void SendSoftwareTrigger();
public bool WaitUntilDone(int timeout);

// 数据读取方法
public void ReadSinglePoint(ref double readValue, int channelID);     // 单点读取单通道
public void ReadSinglePoint(ref double[] buf);                         // 单点读取所有通道
public void ReadData(ref double[] buf, int timeout);                   // 读取单通道数据
public void ReadData(ref double[,] buf, int timeout);                  // 读取多通道数据（每通道一列）
public void ReadData(ref double[] buf, int samplesPerChannel, int timeout);
public void ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
```

#### JY6311DITask - 数字输入任务类
```csharp
// 构造函数
public JY6311DITask(int slotNum);
public JY6311DITask(string boardName);

// 主要属性
public List<DIChannel> Channels { get; }

// 主要方法
public void AddChannel(int lineID);
public void AddChannel(int[] linesID);
public void RemoveChannel(int lineID);
public void Start();
public void Stop();
public void ReadSinglePoint(ref bool readValue, int lineNum);   // 读取单路
public void ReadSinglePoint(ref bool[] readValues);              // 读取所有路
```

#### JY6311DOTask - 数字输出任务类
```csharp
// 构造函数
public JY6311DOTask(int slotNum);
public JY6311DOTask(string boardName);

// 主要属性
public List<DOChannel> Channels { get; }

// 主要方法
public void AddChannel(int lineID);
public void AddChannel(int[] linesID);
public void RemoveChannel(int lineID);
public void Start();
public void Stop();
public void WriteSinglePoint(bool writeValue, int lineID);      // 写入单路
public void WriteSinglePoint(bool[] writeValues);                // 写入所有路
```

### 重要枚举

```csharp
// 采集模式
public enum AIMode
{
    Single,      // 单点模式
    Finite,      // 有限点模式
    Continuous   // 连续模式
}

// RTD类型
public enum RTDType
{
    PT100,       // PT100热电阻
    PT1000       // PT1000热电阻
}

// 接线方式
public enum RTDTerminal
{
    TwoWire,     // 2线制
    ThreeWire,   // 3线制
    FourWire     // 4线制
}

// 温度系数类型
public enum RTDTCRType
{
    Pt100_TCR3851,  // TCR=3951, R0=100Ω（最常用）
    Pt100_TCR3916,  // TCR=3916, R0=100Ω
    Pt100_TCR3920,  // TCR=3920, R0=100Ω
    Pt100_TCR3911,  // TCR=3911, R0=100Ω
    Pt100_TCR3928,  // TCR=3928, R0=100Ω
    Pt1000_TCR3851, // Pt1000_TCR3851
    Pt1000_TCR3750  // Pt100_TCR3750
}

// 测量数据类型
public enum MeasureDataType
{
    Resistance,  // 电阻值
    Temperature, // 温度值
    Voltage      // 电压值
}

// 触发类型
public enum AITriggerType
{
    Immediate,   // 立即触发
    Digital,     // 数字触发
    Soft         // 软件触发
}

// 触发模式
public enum AITriggerMode
{
    Start,       // 开始触发
    Reference    // 参考触发
}

// 数字触发边沿
public enum AIDigitalTriggerEdge
{
    Rising,      // 上升沿
    Falling      // 下降沿
}

// 数字触发源
public enum AIDigitalTriggerSource
{
    PFI0, PFI1, PFI2,
    PXI_Trig0, PXI_Trig1, PXI_Trig2, PXI_Trig3,
    PXI_Trig4, PXI_Trig5, PXI_Trig6, PXI_Trig7
}

// 采样时钟源
public enum AISampleClockSource
{
    Internal,    // 内部时钟
    External     // 外部时钟
}

// 外部时钟端子
public enum ClockTerminal
{
    PFI0, PFI1, PFI2,
    PXI_Trig0, PXI_Trig1, PXI_Trig2, PXI_Trig3,
    PXI_Trig4, PXI_Trig5, PXI_Trig6, PXI_Trig7,
    AI_SampleClock
}

// 信号导出源
public enum AISignalExportSource
{
    StartTrig,      // 开始触发信号
    ReferenceTrig,  // 参考触发信号
    SampleClock     // 采样时钟信号
}

// 信号导出目标
public enum SignalExportDestination
{
    PFI0, PFI1, PFI2,
    PXI_Trig0, PXI_Trig1, PXI_Trig2, PXI_Trig3,
    PXI_Trig4, PXI_Trig5, PXI_Trig6, PXI_Trig7
}

// 工频抑制
public enum PowerLineFrequency
{
    _50Hz,       // 50Hz抑制
    _60Hz        // 60Hz抑制
}
```

---

## 开发指南

### 单点采集模式（Single Mode）

适用于需要按需读取当前温度/电阻/电压值的场景。

```csharp
using System;
using JY6311;

class SinglePointExample
{
    static void Main(string[] args)
    {
        try
        {
            // 1. 创建AI任务，板卡号0
            JY6311AITask aiTask = new JY6311AITask(0);

            // 2. 添加通道 - 温度测量模式
            // 通道0，PT100，3线制，TCR3851（最常用）
            aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

            // 或添加通道 - 电阻测量模式（不转换温度）
            // aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire);

            // 或添加通道 - 电压测量模式
            // aiTask.AddChannel(0, -1.25, 1.25);  // ±1.25V量程

            // 3. 配置采集模式为单点
            aiTask.Mode = AIMode.Single;

            // 4. 启动任务
            aiTask.Start();

            // 5. 读取数据
            double readValue = 0;
            aiTask.ReadSinglePoint(ref readValue, 0);  // 读取通道0
            Console.WriteLine($"温度值: {readValue} ℃");

            // 6. 停止任务
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine($"驱动错误: {ex.Message}");
        }
    }
}
```

### 连续采集模式（Continuous Mode）

适用于需要连续监测温度变化的场景。

```csharp
using System;
using System.Windows.Forms;
using JY6311;

public class ContinuousExample : Form
{
    private JY6311AITask aiTask;
    private double[] readValue;
    private Timer timer1;

    private void StartAcquisition()
    {
        try
        {
            // 1. 创建任务
            aiTask = new JY6311AITask(0);

            // 2. 添加通道 - 16通道PT100温度测量
            for (int i = 0; i < 16; i++)
            {
                aiTask.AddChannel(i, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
            }

            // 3. 配置连续采集模式
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 10;  // 每通道10 Sa/s

            // 4. 配置工频抑制（当总采样率≤40Sa/s时有效）
            aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;

            // 5. 分配数据缓冲区（每次读取100个点）
            readValue = new double[100];

            // 6. 启动任务
            aiTask.Start();

            // 7. 启动定时器读取数据
            timer1 = new Timer();
            timer1.Interval = 100;  // 100ms读取一次
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;

        // 检查缓冲区是否有足够数据
        if (aiTask.AvailableSamples >= (ulong)readValue.Length)
        {
            try
            {
                // 读取数据
                aiTask.ReadData(ref readValue, -1);  // -1表示无限等待

                // 处理数据（这里可以添加图表显示、数据保存等）
                Console.WriteLine($"读取到 {readValue.Length} 个数据点");
            }
            catch (JYDriverException ex)
            {
                timer1.Enabled = false;
                MessageBox.Show(ex.Message);
            }
        }

        timer1.Enabled = true;
    }

    private void StopAcquisition()
    {
        timer1?.Stop();
        aiTask?.Stop();
        aiTask?.Channels.Clear();
    }
}
```

### 有限点采集模式（Finite Mode）

适用于采集固定数量样本的场景。

```csharp
using System;
using JY6311;

class FiniteExample
{
    static void Main(string[] args)
    {
        try
        {
            // 1. 创建任务
            JY6311AITask aiTask = new JY6311AITask(0);

            // 2. 添加通道
            aiTask.AddChannel(0, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);

            // 3. 配置有限点采集
            aiTask.Mode = AIMode.Finite;
            aiTask.SampleRate = 100;           // 100 Sa/s
            aiTask.SamplesToAcquire = 1000;    // 采集1000个点

            // 4. 分配缓冲区
            double[] readValue = new double[1000];

            // 5. 启动任务
            aiTask.Start();

            // 6. 等待采集完成（阻塞方式）
            aiTask.WaitUntilDone(-1);  // -1表示无限等待

            // 7. 读取所有数据
            aiTask.ReadData(ref readValue, readValue.Length, -1);

            Console.WriteLine($"采集完成，共 {readValue.Length} 个点");

            // 8. 停止任务
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}
```

### 多通道采集

多通道采集使用二维数组存储数据，每列代表一个通道。

```csharp
using System;
using SeeSharpTools.JY.ArrayUtility;
using JY6311;

class MultiChannelExample
{
    static void Main(string[] args)
    {
        try
        {
            JY6311AITask aiTask = new JY6311AITask(0);

            // 添加多个通道
            int[] channels = { 0, 1, 2, 3, 4, 5, 6, 7 };
            foreach (int ch in channels)
            {
                aiTask.AddChannel(ch, RTDType.PT100, RTDTerminal.ThreeWire, RTDTCRType.Pt100_TCR3851);
            }

            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 10;

            // 分配二维缓冲区 [采样点数, 通道数]
            double[,] readValue = new double[100, channels.Length];
            double[,] displayValue = new double[channels.Length, 100];  // 转置后用于显示

            aiTask.Start();

            // 读取数据
            aiTask.ReadData(ref readValue, -1);

            // 转置数据（每行代表一个通道）
            ArrayManipulation.Transpose(readValue, ref displayValue);

            aiTask.Stop();
            aiTask.Channels.Clear();
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
```

### 触发配置

#### 数字触发
```csharp
// 配置数字触发 - 上升沿触发
aiTask.Mode = AIMode.Continuous;
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // 触发源PFI0
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;     // 上升沿触发

aiTask.Start();  // 启动后等待数字触发信号
```

#### 软件触发
```csharp
// 配置软件触发
aiTask.Mode = AIMode.Finite;
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;  // 开始触发模式

aiTask.Start();  // 启动后等待软件触发

// 发送软件触发信号
aiTask.SendSoftwareTrigger();
```

#### 重触发模式（多段采集）
```csharp
// 配置重触发 - 采集多段数据
aiTask.Mode = AIMode.Finite;
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Trigger.ReTriggerCount = 5;  // 触发5次（-1表示无限次）

aiTask.Start();

for (int i = 0; i < 5; i++)
{
    aiTask.SendSoftwareTrigger();  // 每次触发采集一段数据
    // 等待采集完成...
}
```

#### 参考触发（预触发）
```csharp
// 配置参考触发 - 保存触发前后的数据
aiTask.Mode = AIMode.Finite;
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Mode = AITriggerMode.Reference;  // 参考触发模式
aiTask.Trigger.PreTriggerSamples = 100;         // 触发前保存100个点
aiTask.SamplesToAcquire = 500;                  // 总共采集500个点
```

### 外部采样时钟配置
```csharp
// 使用外部时钟源
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;  // 外部时钟输入端子
aiTask.SampleClock.External.ExpectedRate = 1000;             // 期望的外部时钟频率
```

### 信号导出配置
```csharp
// 导出采样时钟到PFI1
aiTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PFI1);

// 导出开始触发信号到PXI_Trig0
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

// 清除信号导出
aiTask.SignalExport.ClearAll();
```

---

## 数字I/O开发

### 数字输入（DI）
```csharp
using JY6311;

class DIExample
{
    static void Main()
    {
        try
        {
            // 创建DI任务
            JY6311DITask diTask = new JY6311DITask(0);

            // 添加数字输入通道（0~2）
            diTask.AddChannel(0);
            diTask.AddChannel(1);
            diTask.AddChannel(2);

            // 启动任务
            diTask.Start();

            // 读取单路
            bool value0;
            diTask.ReadSinglePoint(ref value0, 0);
            Console.WriteLine($"PFI0 = {value0}");

            // 读取所有路
            bool[] values = new bool[3];
            diTask.ReadSinglePoint(ref values);
            Console.WriteLine($"PFI0={values[0]}, PFI1={values[1]}, PFI2={values[2]}");

            diTask.Stop();
            diTask.Channels.Clear();
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
```

### 数字输出（DO）
```csharp
using JY6311;

class DOExample
{
    static void Main()
    {
        try
        {
            // 创建DO任务
            JY6311DOTask doTask = new JY6311DOTask(0);

            // 添加数字输出通道
            doTask.AddChannel(0);
            doTask.AddChannel(1);

            // 启动任务
            doTask.Start();

            // 写入单路
            doTask.WriteSinglePoint(true, 0);   // PFI0输出高电平
            doTask.WriteSinglePoint(false, 1);  // PFI1输出低电平

            // 写入所有路
            bool[] values = { true, false, true };
            doTask.WriteSinglePoint(values);

            doTask.Stop();
            doTask.Channels.Clear();
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
```

---

## 最佳实践

### 1. 异常处理
```csharp
try
{
    // 驱动操作代码
}
catch (JYDriverException ex)
{
    // 处理驱动异常
    Console.WriteLine($"驱动错误: {ex.Message}");
}
catch (Exception ex)
{
    // 处理其他异常
    Console.WriteLine($"系统错误: {ex.Message}");
}
```

### 2. 资源释放
```csharp
// 确保任务正确停止和清理
private void OnFormClosing(object sender, FormClosingEventArgs e)
{
    if (aiTask != null)
    {
        aiTask.Stop();
        aiTask.Channels.Clear();
    }
}
```

### 3. 缓冲区管理
- 连续模式下定期检查`AvailableSamples`，避免缓冲区溢出
- 读取数据时使用合适的超时时间（-1表示无限等待）
- 根据采样率和读取频率合理设置缓冲区大小

### 4. 采样率设置
- 总采样率 = 每通道采样率 × 通道数
- 最大总采样率受硬件限制，超过时会自动调整
- 可通过`ActualSampleRate`属性获取实际生效的采样率

### 5. 接线方式选择
- **2线制**：简单但精度较低，适用于短距离、精度要求不高的场合
- **3线制**：平衡了精度和复杂度，最常用的接线方式
- **4线制**：精度最高，适用于精密测量场合

### 6. TCR类型选择
- **Pt100_TCR3851**：最常用的工业标准（TCR=0.003851/℃）
- 根据传感器规格选择正确的TCR类型，否则会导致温度换算误差

---

## 完整示例：16通道PT100温度采集系统

```csharp
using System;
using System.IO;
using System.Windows.Forms;
using JY6311;

namespace PT100TemperatureSystem
{
    public class TemperatureMonitor : Form
    {
        private JY6311AITask aiTask;
        private double[,] readBuffer;
        private double[,] displayBuffer;
        private Timer readTimer;
        private StreamWriter dataLogger;
        
        private const int ChannelCount = 16;
        private const int SamplesPerRead = 10;
        private const double SampleRate = 10;  // 每通道10 Sa/s

        public TemperatureMonitor()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // 初始化UI组件...
        }

        private void StartMonitoring()
        {
            try
            {
                // 1. 创建任务
                aiTask = new JY6311AITask(0);

                // 2. 添加16个PT100通道
                for (int i = 0; i < ChannelCount; i++)
                {
                    aiTask.AddChannel(
                        i, 
                        RTDType.PT100, 
                        RTDTerminal.ThreeWire,      // 3线制接线
                        RTDTCRType.Pt100_TCR3851    // 标准TCR
                    );
                }

                // 3. 配置采集参数
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = SampleRate;
                aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;  // 50Hz工频抑制

                // 4. 分配数据缓冲区
                readBuffer = new double[SamplesPerRead, ChannelCount];
                displayBuffer = new double[ChannelCount, SamplesPerRead];

                // 5. 打开数据记录文件
                string fileName = $"TemperatureData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                dataLogger = new StreamWriter(fileName);
                dataLogger.WriteLine("Timestamp,Ch0,Ch1,Ch2,Ch3,Ch4,Ch5,Ch6,Ch7,Ch8,Ch9,Ch10,Ch11,Ch12,Ch13,Ch14,Ch15");

                // 6. 启动采集
                aiTask.Start();

                // 7. 启动定时器读取数据
                readTimer = new Timer();
                readTimer.Interval = 500;  // 500ms读取一次
                readTimer.Tick += OnTimerTick;
                readTimer.Start();

                Console.WriteLine("温度监测已启动...");
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show($"启动失败: {ex.Message}");
                Cleanup();
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            readTimer.Enabled = false;

            try
            {
                // 检查可用样本数
                if (aiTask.AvailableSamples >= (ulong)SamplesPerRead)
                {
                    // 读取数据
                    aiTask.ReadData(ref readBuffer, -1);

                    // 转置数据
                    ArrayManipulation.Transpose(readBuffer, ref displayBuffer);

                    // 获取当前时间戳
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                    // 处理每通道的最新数据
                    for (int ch = 0; ch < ChannelCount; ch++)
                    {
                        double latestValue = displayBuffer[ch, SamplesPerRead - 1];
                        Console.WriteLine($"Ch{ch}: {latestValue:F2} ℃");
                    }

                    // 记录到CSV文件
                    for (int i = 0; i < SamplesPerRead; i++)
                    {
                        string line = timestamp;
                        for (int ch = 0; ch < ChannelCount; ch++)
                        {
                            line += $",{displayBuffer[ch, i]:F3}";
                        }
                        dataLogger.WriteLine(line);
                    }
                    dataLogger.Flush();
                }
            }
            catch (JYDriverException ex)
            {
                Console.WriteLine($"读取错误: {ex.Message}");
            }

            readTimer.Enabled = true;
        }

        private void StopMonitoring()
        {
            readTimer?.Stop();
            Cleanup();
            Console.WriteLine("温度监测已停止");
        }

        private void Cleanup()
        {
            aiTask?.Stop();
            aiTask?.Channels.Clear();
            dataLogger?.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMonitoring();
            base.OnFormClosing(e);
        }
    }
}
```

---

## 参考资源

- **硬件手册：** `c:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\JY-6311 Specs and Manual_EN.pdf`
- **示例代码：** `c:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\JY6311.Examples\`
- **驱动版本：** JY6311 Driver 4.0.0 or newer
- **开发环境：** .NET Framework 4.0 or later
