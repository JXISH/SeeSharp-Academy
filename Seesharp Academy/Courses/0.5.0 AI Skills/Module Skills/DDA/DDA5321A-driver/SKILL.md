---
name: dda5321a-driver
description: 提供 JYTEK DDA-5321A 分布式数据采集平台的完整 C# 驱动开发指引。DDA-5321A 支持两种工作模式：EDAQ模式（与JY5321A完全一致，本地直连）和DDA模式（分布式网络采集，上位机通过TCP与下位机通信）。涵盖两种模式的模拟输入（AI）单点/有限/连续/录制采集、数字输入/输出（DI/DO）、计数器输入/输出（CI/CO）、触发配置、时钟配置。当用户使用 DDA-5321A、EDAQ模式、DDA模式、分布式采集、网络采集、JYDDA5321AAITask、JYTCPClient 开发远程数据采集应用时自动应用。
---

# DDA-5321A 驱动开发指引

## 概述

DDA-5321A 是分布式数据采集平台，内置 32-ch, 16-bit, 1 MS/s/ch 采集卡，支持两种工作模式：

| 模式         | 适用场景    | 连接方式               | 开发参考                                                             |
| ---------- | ------- | ------------------ | ---------------------------------------------------------------- |
| **EDAQ模式** | 本地直连采集  | USB/PCIe/PXIe 直接连接 | 与 JY5321A 完全一致，参考 [JY5321A SKILL.md](../JY5321A-driver/SKILL.md) |
| **DDA模式**  | 分布式远程采集 | 上位机通过TCP连接下位机      | 本文档 DDA模式章节                                                      |

---

## 环境要求

### EDAQ模式

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5321A\Bin\JY5321A.dll`
- **命名空间**：`using JY5321A;`
- **连接方式**：按槽位号直接连接板卡

### DDA模式

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5321A\Bin\JY5321A.dll`（下位机使用）
- **上位机引用**：
  - `SeeSharpTools.JY.TCP.dll`（TCP通信）
  - `JYDDAParameterTransmit.dll`（参数传输类）
  - `Newtonsoft.Json.dll`（JSON序列化）
- **通信端口**：
  - 指令端口：8088
  - 数据端口：8081
  - 设备端口：10004

---

## 硬件规格速查

| 功能       | 规格                                      |
| -------- | --------------------------------------- |
| AI 分辨率   | 16-bit                                  |
| AI 通道数   | 32 通道                                   |
| AI 最大采样率 | 1 MS/s（每通道，同步采样）                        |
| AI 输入量程  | ±10V / ±5V / ±2V / ±1V / ±0.5V / ±0.25V |
| 硬件定时 DIO | 16 通道                                   |
| 静态 DIO   | 32 通道                                   |
| 计数器      | 2 个                                     |
| 接口       | 以太网（DDA模式）/ USB / PCIe / PXIe（EDAQ模式）   |

---

## EDAQ模式开发

EDAQ模式下，DDA-5321A 与 JY5321A 用法完全一致：

```csharp
using JY5321A;

// 创建任务（按槽位号）
var task = new JY5321AAITask(0);
task.AddChannel(0, -10, 10);
task.Mode = AIMode.Continuous;
task.SampleRate = 10000;
task.Start();
// ... 读取数据 ...
task.Stop();
task.Channels.Clear();
```

**完整 API 参考**：[JY5321A SKILL.md](../JY5321A-driver/SKILL.md)

---

## DDA模式开发

### 核心概念

DDA模式采用**客户端-服务端**架构：

- **下位机（Target）**：运行在实际采集设备上，执行硬件采集
- **上位机（Host）**：运行在用户PC上，通过TCP发送参数和接收数据

### 编程范式

**创建TCP客户端 → 配置参数 → JSON序列化 → 发送指令 → 接收数据 → 发送停止指令**

### 核心类

#### JYDDA5321AAITask（AI参数类）

```csharp
public class JYDDA5321AAITask
{
    public int ChannelCount { get; set; }           // 通道数量
    public List<int> ChannelID { get; set; }        // 通道ID列表
    public double LowRange { get; set; }            // 量程下限
    public double HighRange { get; set; }           // 量程上限
    public AIBandWidth BandWidth { get; set; }      // 带宽（25KHz/220KHz）
    public AIMode Mode { get; set; }                // 采集模式
    public RecordMode RecordMode { get; set; }      // 录制模式
    public double RecordLength { get; set; }        // 录制时长
    public bool EnableIEPE { get; set; }            // IEPE使能
    public float SampleRate { get; set; }           // 采样率
    public int SamplesToAcquire { get; set; }       // 采集点数
    public string FilePath { get; set; }            // 录制文件路径
    public AIClockSourceType ClockSource { get; set; }      // 时钟源
    public ClockTerminal ClockTerminal { get; set; }        // 外部时钟端子
    public int Frequency { get; set; }              // 外部时钟频率
    public AITriggerType TriggerType { get; set; }  // 触发类型
    public AITriggerMode TriggerMode { get; set; }  // 触发模式
    public AIAnalogTriggerSource TriggerChannel { get; set; }   // 模拟触发通道
    public AIAnalogTriggerComparator AIAnalogTriggerComparator { get; set; }
    public AIAnalogWindowCondition AIAnalogWindowCondition { get; set; }
    public float Threshold { get; set; }            // 触发阈值
    public double HighThreshold { get; set; }       // 窗口触发高阈值
    public double LowThreshold { get; set; }        // 窗口触发低阈值
    public AIAnalogTriggerEdge TriggerCondition { get; set; }   // 触发条件
    public int PreTriggerCount { get; set; }        // 预触发点数
    public int readTriggerCount { get; set; }       // 读取触发计数
    public int ReTriggerCount { get; set; }         // 重触发计数
    public AIDigitalTriggerEdge TriggerEdge { get; set; }       // 数字触发边沿
    public AIDigitalTriggerSource DigitalTriggerSource { get; set; }  // 数字触发源
    public ReferenceClockSource ReferenceClockSource { get; set; }
    public ExternalReferenceClockTerminal ExternalReferenceClockTerminal { get; set; }
    public double ExpectedRate { get; set; }
}
```

#### JYDDA5321ADevice（设备参数类）

```csharp
public class JYDDA5321ADevice
{
    public string cardName { get; set; }    // 板卡名称
}
```

### 关键枚举

#### AIMode（采集模式）

```csharp
public enum AIMode
{
    Finite = 0,         // 有限采集
    Continuous = 1,     // 连续采集
    Single = 2,         // 单点采集
    Record = 3          // 录制模式
}
```

#### RecordMode（录制模式）

```csharp
public enum RecordMode
{
    Finite = 0,         // 有限录制
    Infinite = 1        // 无限录制
}
```

#### AIClockSourceType（时钟源）

```csharp
public enum AIClockSourceType
{
    External = 0,       // 外部时钟
    Internal = 1        // 内部时钟
}
```

#### AITriggerMode（触发模式）

```csharp
public enum AITriggerMode
{
    Start,              // 启动触发
    Reference           // 参考触发（仅Finite模式有效）
}
```

---

## DDA模式代码示例

### 示例 1：连续采集（Host端）

```csharp
using System;
using System.Windows.Forms;
using SeeSharpTools.JY.TCP;
using System.Threading;
using JYDDAParameterTransmit;
using Newtonsoft.Json;

public partial class Form1 : Form
{
    private JYDDA5321AAITask paramClass = new JYDDA5321AAITask();
    private JYDDA5321ADevice deviceParamClass = new JYDDA5321ADevice();
    private string paramString;
    private JYTCPClient _cmdClient;      // 指令端口
    private JYTCPClient _dataClient;     // 数据端口
    private JYTCPClient _deviceCmdClient;// 设备端口
    private double[,] dataBuffer;
    private Thread judge;
    bool connected;
    bool stop = false;

    // 连接下位机
    private void button_connect_Click(object sender, EventArgs e)
    {
        _cmdClient = new JYTCPClient(textBox_IpAddress.Text, 8088);
        _dataClient = new JYTCPClient(textBox_IpAddress.Text, 8081);
        _deviceCmdClient = new JYTCPClient(textBox_IpAddress.Text, 10004);

        _dataClient.BufferSize = 128 * 0x100000;
        _cmdClient.BufferSize = 0x100000;
        _deviceCmdClient.BufferSize = 0x100000;

        _dataClient.Connect();
        _cmdClient.Connect();
        _deviceCmdClient.Connect();

        connected = true;
    }

    // 启动采集
    private void button_start_Click(object sender, EventArgs e)
    {
        dataBuffer = new double[(int)numericUpDown_samplesToAcquire.Value, comboBox_channelNum.SelectedIndex + 1];

        // 配置参数
        paramClass.ChannelCount = Convert.ToInt32(comboBox_channelNum.Text);
        paramClass.Mode = AIMode.Continuous;
        paramClass.HighRange = 10;
        paramClass.LowRange = -10;
        paramClass.SampleRate = (float)numericUpDown_sampleRate.Value;
        paramClass.SamplesToAcquire = (int)numericUpDown_samplesToAcquire.Value;
        paramClass.ClockSource = AIClockSourceType.Internal;
        deviceParamClass.cardName = textBox_cardName.Text;

        if (_cmdClient.Connected && _dataClient.Connected && _deviceCmdClient.Connected)
        {
            // JSON序列化并发送
            paramString = JsonConvert.SerializeObject(paramClass);
            _cmdClient.SendData(System.Text.Encoding.Default.GetBytes(paramString));

            var deviceParamString = JsonConvert.SerializeObject(deviceParamClass);
            _deviceCmdClient.SendData(System.Text.Encoding.Default.GetBytes(deviceParamString));

            stop = false;
            timer1.Enabled = true;
        }
    }

    // 定时读取数据
    private void timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;

        if (_dataClient.AvailableSamples >= dataBuffer.Length * sizeof(double))
        {
            _dataClient.ReadData(ref dataBuffer, 10000);
            easyChartX1.Plot(dataBuffer, 0, 1, SeeSharpTools.JY.GUI.MajorOrder.Column);
        }

        timer1.Enabled = true;
    }

    // 停止采集
    private void button_stop_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        _cmdClient.SendData(System.Text.Encoding.Default.GetBytes("stop!"));
        stop = true;
    }

    // 断开连接
    private void button_disconnect_Click(object sender, EventArgs e)
    {
        connected = false;
        if (!stop)
        {
            _cmdClient.SendData(System.Text.Encoding.Default.GetBytes("stop!"));
            Thread.Sleep(100);
        }

        _cmdClient?.DisConnect();
        _dataClient?.DisConnect();
        _deviceCmdClient?.DisConnect();
        timer1.Enabled = false;
    }

    // 窗体关闭时释放资源
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        connected = false;
        judge?.Abort();

        if (_cmdClient != null && _cmdClient.Connected)
        {
            _cmdClient.SendData(System.Text.Encoding.Default.GetBytes("stop!"));
            Thread.Sleep(100);
            _cmdClient.DisConnect();
        }

        _dataClient?.DisConnect();
        _deviceCmdClient?.DisConnect();
        timer1.Enabled = false;
    }
}
```

### 示例 2：有限采集（Finite Mode）

```csharp
// 配置有限采集模式
paramClass.Mode = AIMode.Finite;
paramClass.SamplesToAcquire = 10000;    // 每通道采集10000点
paramClass.SampleRate = 50000;          // 50kS/s
paramClass.ChannelCount = 4;            // 4个通道
paramClass.HighRange = 10;
paramClass.LowRange = -10;

// 可选：配置触发
paramClass.TriggerType = AITriggerType.Digital;
paramClass.DigitalTriggerSource = AIDigitalTriggerSource.PFI0;
paramClass.TriggerEdge = AIDigitalTriggerEdge.Rising;

// 发送参数启动采集
paramString = JsonConvert.SerializeObject(paramClass);
_cmdClient.SendData(System.Text.Encoding.Default.GetBytes(paramString));

// 等待数据接收完成
while (_dataClient.AvailableSamples < expectedBytes)
{
    Thread.Sleep(10);
}
_dataClient.ReadData(ref dataBuffer, timeout);

// 发送停止指令
_cmdClient.SendData(System.Text.Encoding.Default.GetBytes("stop!"));
```

### 示例 3：录制模式（Record Mode）

```csharp
// 配置录制模式
paramClass.Mode = AIMode.Record;
paramClass.RecordMode = RecordMode.Finite;      // 或 Infinite
paramClass.RecordLength = 10.0;                 // 录制10秒（Finite模式）
paramClass.FilePath = @"C:\Data\record.bin";    // 下位机保存路径
paramClass.SampleRate = 250000;
paramClass.ChannelCount = 4;

// 启动录制
paramString = JsonConvert.SerializeObject(paramClass);
_cmdClient.SendData(System.Text.Encoding.Default.GetBytes(paramString));

// 可选：接收预览数据（如果下位机支持）
if (_dataClient.AvailableSamples > 0)
{
    _dataClient.ReadData(ref previewBuffer, 1000);
}

// 录制完成后发送停止（Infinite模式需要手动停止）
_cmdClient.SendData(System.Text.Encoding.Default.GetBytes("stop!"));
```

### 示例 4：外部时钟配置

```csharp
paramClass.ClockSource = AIClockSourceType.External;
paramClass.ClockTerminal = ClockTerminal.PFI0;      // 外部时钟输入端子
paramClass.Frequency = 10000;                        // 外部时钟频率

paramString = JsonConvert.SerializeObject(paramClass);
_cmdClient.SendData(System.Text.Encoding.Default.GetBytes(paramString));
```

### 示例 5：模拟触发配置

```csharp
paramClass.TriggerType = AITriggerType.Analog;
paramClass.TriggerChannel = AIAnalogTriggerSource.AI0;
paramClass.AIAnalogTriggerComparator = AIAnalogTriggerComparator.Edge;
paramClass.TriggerCondition = AIAnalogTriggerEdge.Rising;
paramClass.Threshold = 2.5f;                         // 触发阈值 2.5V
paramClass.PreTriggerCount = 100;                    // 预触发点数

paramString = JsonConvert.SerializeObject(paramClass);
_cmdClient.SendData(System.Text.Encoding.Default.GetBytes(paramString));
```

### 示例 6：Target端（下位机）完整实现

```csharp
using System;
using JY5321A;
using System.Threading;
using SeeSharpTools.JY.TCP;
using JYDDAParameterTransmit;
using Newtonsoft.Json;

namespace Target_AIContinuous
{
    public class Program
    {
        // 指令传输通道
        private static JYTCPServer _commandServer;
        // 数据传输通道
        private static JYTCPServer _dataServer;
        // 设备指令通道
        private static JYTCPServer _deviceCommandServer;
        // 模拟输入任务
        private static JY5321AAITask aiTask;
        // 参数对象
        private static JYDDA5321AAITask param;
        private static JYDDA5321ADevice deviceParam;

        public static void Main(string[] args)
        {
            // 创建TCP服务端（监听上位机连接）
            _commandServer = new JYTCPServer(8088);     // 指令端口
            _dataServer = new JYTCPServer(8081);        // 数据端口
            _deviceCommandServer = new JYTCPServer(10004);  // 设备端口

            _commandServer.BufferSize = 0x100000;
            _dataServer.BufferSize = 0x9000000;
            _deviceCommandServer.BufferSize = 0x100000;

            _commandServer.Start();
            _dataServer.Start();
            _deviceCommandServer.Start();

            bool flag = true;   // 是否循环读取数据
            bool run = true;    // 是否运行服务
            double[,] data;
            string paramString;

            while (run)
            {
                // 等待上位机连接所有端口
                if (_commandServer.IsClientConnect && 
                    _dataServer.IsClientConnect && 
                    _deviceCommandServer.IsClientConnect)
                {
                    // 接收参数配置
                    if (_commandServer.AvailableSamples > 200 && 
                        _deviceCommandServer.AvailableSamples > 20)
                    {
                        // 读取AI参数
                        byte[] paramData = new byte[_commandServer.AvailableSamples];
                        _commandServer.ReadData(ref paramData, 10);
                        paramString = System.Text.Encoding.Default.GetString(paramData);
                        param = JsonConvert.DeserializeObject<JYDDA5321AAITask>(paramString);

                        // 读取设备参数
                        byte[] deviceParamData = new byte[_deviceCommandServer.AvailableSamples];
                        _deviceCommandServer.ReadData(ref deviceParamData, 10);
                        string deviceParamString = System.Text.Encoding.Default.GetString(deviceParamData);
                        deviceParam = JsonConvert.DeserializeObject<JYDDA5321ADevice>(deviceParamString);

                        // 初始化采集任务
                        aiTask = new JY5321AAITask(deviceParam.cardName);

                        // 添加通道
                        for (int i = 0; i < param.ChannelCount; i++)
                        {
                            aiTask.AddChannel(i, param.LowRange, param.HighRange);
                        }

                        // 配置参数
                        aiTask.Mode = (JY5321A.AIMode)Enum.Parse(typeof(JY5321A.AIMode), param.Mode.ToString());
                        aiTask.SampleClock.Source = (JY5321A.AISampleClockSource)Enum.Parse(
                            typeof(JY5321A.AISampleClockSource), param.ClockSource.ToString());

                        // 外部时钟配置
                        if (aiTask.SampleClock.Source == JY5321A.AISampleClockSource.External)
                        {
                            aiTask.SampleClock.External.Terminal = (JY5321A.ClockTerminal)Enum.Parse(
                                typeof(JY5321A.ClockTerminal), param.ClockTerminal.ToString());
                            aiTask.SampleClock.External.ExpectedRate = param.SampleRate;
                        }
                        else
                        {
                            aiTask.SampleRate = param.SampleRate;
                        }

                        aiTask.SamplesToAcquire = param.SamplesToAcquire;
                        aiTask.Advanced.RawDataWidth = AIRawDataWidth._16Bits;

                        // 分配数据缓冲区
                        data = new double[param.SamplesToAcquire, param.ChannelCount];

                        // 启动采集
                        try
                        {
                            aiTask.Start();
                            flag = true;
                            Console.WriteLine("采集已启动");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"启动失败: {ex.Message}");
                            continue;
                        }

                        // 数据读取循环
                        while (flag)
                        {
                            try
                            {
                                // 检查停止指令
                                if (_commandServer.AvailableSamples > 0)
                                {
                                    paramData = new byte[_commandServer.AvailableSamples];
                                    _commandServer.ReadData(ref paramData, 10);
                                    aiTask.Stop();
                                    flag = false;
                                    Console.WriteLine("收到停止指令");
                                }
                                else
                                {
                                    // 读取并发送数据
                                    if (aiTask.AvailableSamples >= (ulong)aiTask.SamplesToAcquire)
                                    {
                                        Console.WriteLine("读取数据...");
                                        aiTask.ReadData(ref data, 10);
                                        _dataServer.SendData(data);
                                        Console.WriteLine("发送数据...");
                                    }
                                    else
                                    {
                                        Console.WriteLine("数据不足，等待...");
                                        Thread.Sleep(10);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"错误: {ex.Message}");
                                aiTask.Stop();
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                        Console.WriteLine("等待参数指令...");
                    }
                }
                else
                {
                    Thread.Sleep(100);
                    Console.WriteLine("等待上位机连接...");
                }
            }
        }
    }
}
```

---

## DI（数字输入）开发指南

### DI任务初始化

```csharp
using JY5321A;

// 创建DI任务（使用设备名称字符串）
JY5321ADITask diTask = new JY5321ADITask("DDADev0");

// 添加通道（支持多个通道）
int[] channels = { 0, 1 };  // DI通道0和1
diTask.AddChannel(channels);

// 配置采样模式
diTask.Mode = DIMode.Continuous;  // 连续模式
diTask.SampleRate = 10000;        // 采样率 10kHz
diTask.SamplesToAcquire = 1000;   // 每次读取1000个样本
```

### DI数据读取（轮询方式）

**重要：DI数据读取使用轮询方式，不使用事件驱动！**

```csharp
// 分配数据缓冲区（二维数组）
byte[,] dataBuffer = new byte[diTask.SamplesToAcquire, 2];  // 2通道

// 启动采集
diTask.Start();

// 数据读取循环（轮询方式）
while (isRunning)
{
    // 检查是否有足够的数据
    if (diTask.AvailableSamples >= (ulong)diTask.SamplesToAcquire)
    {
        // 读取数据
        diTask.ReadData(ref dataBuffer, (uint)dataBuffer.GetLength(0), -1);
        
        // 处理数据
        ProcessDIData(dataBuffer);
    }
    else
    {
        Thread.Sleep(10);  // 数据不足，等待10ms
    }
}
```

### DI数据格式说明

DI数据是二维数组 `byte[samples, channels]`，存储格式为：
```
[ch0_sample0, ch1_sample0, ch0_sample1, ch1_sample1, ...]
```

发送前需要转换为一维数组：
```csharp
int samples = dataBuffer.GetLength(0);
int channelCount = dataBuffer.GetLength(1);
byte[] tempBuf = new byte[samples * channelCount];

for (int i = 0; i < samples; i++)
{
    for (int j = 0; j < channelCount; j++)
    {
        tempBuf[channelCount * i + j] = dataBuffer[i, j];
    }
}
```

### DI触发配置

```csharp
// 数字触发
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
diTask.Trigger.Digital.Edge = DITriggerEdge.Rising;

// 软件触发
diTask.Trigger.Type = DITriggerType.Soft;
// 启动后发送软件触发
diTask.SendSoftwareTrigger();
```

---

## CO（计数器输出）开发指南

### CO任务初始化

```csharp
using JY5321A;

// 创建CO任务（使用设备名称字符串和通道号）
JY5321ACOTask coTask = new JY5321ACOTask("DDADev0", 0);  // CO通道0
```

### CO脉冲配置（使用WriteSinglePoint方法）

**重要：CO脉冲配置使用WriteSinglePoint方法，不直接设置Frequency/DutyCycle属性！**

```csharp
// 配置CO基本参数
coTask.Mode = COMode.Finite;  // 有限脉冲输出
coTask.IdleState = COIdleState.LowLevel;  // 空闲状态为低电平
coTask.InitialDelay = 0;  // 无初始延迟
coTask.Pause.ActivePolarity = LevelPolarity.None;  // 不使用暂停功能

// 计算高低电平时间
double frequency = 1000.0;  // 频率 1kHz
double dutyCycle = 0.5;     // 占空比 50%
double period = 1.0 / frequency;  // 周期（秒）
double highTime = period * dutyCycle;  // 高电平时间
double lowTime = period * (1 - dutyCycle);  // 低电平时间

// 创建COPulse对象（使用HighLowTime模式）
COPulse pulse = new COPulse(
    COPulseType.HighLowTime,  // 使用高低电平时间模式
    highTime,                 // 高电平时间（秒）
    lowTime,                  // 低电平时间（秒）
    100                       // 脉冲个数（-1表示无限）
);

// 写入脉冲参数
coTask.WriteSinglePoint(pulse);

// 启动输出
coTask.Start();

// 等待完成
coTask.WaitUntilDone();

// 停止
coTask.Stop();
```

### CO输出模式

```csharp
public enum COMode
{
    Finite = 0,             // 有限脉冲输出
    ContinuousWrapping = 1, // 连续循环输出
    ContinuousNoWrapping = 2, // 连续不循环输出
    Single = 3              // 单脉冲输出
}
```

### COPulseType说明

```csharp
public enum COPulseType
{
    DutyCycleFrequency = 0,  // 使用占空比和频率（不推荐）
    HighLowTime = 1,         // 使用高低电平时间（推荐）
    HighLowTick = 2          // 使用高低电平刻度
}
```

**推荐使用 `HighLowTime` 模式，更直观且精度高。**

---

## 常见错误处理

| 问题      | 原因            | 解决方案                                 |
| ------- | ------------- | ------------------------------------ |
| TCP连接失败 | 下位机IP错误或端口未开放 | 检查网络连接，确认下位机IP和端口配置                  |
| 数据接收超时  | 网络延迟或下位机未启动采集 | 检查网络质量，确认下位机已正确接收参数                  |
| 数据格式错误  | 缓冲区大小不匹配      | 确保dataBuffer大小与发送的SamplesToAcquire一致 |
| 采集无法停止  | stop!指令未送达    | 检查_cmdClient连接状态，必要时强制断开             |

---

## 更多详情

- **EDAQ模式完整API**：参考 [JY5321A SKILL.md](../JY5321A-driver/SKILL.md)
- **DDA模式范例代码**：`DDA5321A.Examples` 文件夹
  - **AI范例**：
    - Host端：`Analog Input/AI Continuous/Host-AIContinuous/`
    - Target端：`Analog Input/AI Continuous/Target-AIContinuous/`
  - **DI范例**：
    - Host端：`Digital Input/DI Continuous/Host-DIContinuous/`
    - Target端：`Digital Input/DI Continuous/Target-DIContinuous/`
  - **CO范例**：
    - Target端：`Counter Output/Target_COSinglePulseGeneration/`
    - Target端：`Counter Output/Target_COContinuousPulseGeneration/`
