---
name: jy9802-driver
description: 提供 JYTEK JY9802（PXIe-9802）高速数字化仪（Digitizer）的完整 C# 驱动开发指引。涵盖高速模拟输入（AI）有限/连续/流盘采集、2 通道 14-bit 250 MS/s 同步采样、DDC 数字下变频（Real/Complex IQ 数据格式）、触发配置（立即/软件/数字/模拟触发，Power/Window/Hysteresis 比较器）、重触发（Retrigger）、预触发（PreTrigger）、参考时钟（内部 10 MHz TCXO/外部）、多卡同步（导出 10MHz 参考时钟 + TriggerOut/Trig_In）、板载 1GB DDR3 乒乓缓存流盘（VectorFile/Raw 格式）、AO 输出与回放。当用户使用 JY9802、PXIe-9802、JY9802AITask、JY9802AOTask、JY9802Device、AIMode.Finite、AIMode.Continuous、AIMode.Record、AITriggerType、AIAnalogTriggerType、AIRetrigger、RecordMode.Finite、RecordFormat.VectorFile、ExportClockSource、ExportSignalSource、DataFormat.Real、DataFormat.Complex 开发高速数据采集、波形流盘、瞬态信号采集、影像数字化、超声波测量、生物医疗、ATE 测试、雷达/声纳测试应用时自动应用。
---

# JY9802 高速数字化仪驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9802\Bin\JY9802.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9802\Bin\JY9802.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9802;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等
- **辅助依赖**：`SeeSharpTools.JY.ArrayUtility`（数据转置）、`SeeSharpTools.JY.FileIO.VectorFile`（流盘文件读写）

## 硬件规格速查（基于简仪产品型录_20260417.pdf，文档页46）

| 功能                  | 规格                                    |
| --------------------- | --------------------------------------- |
| 产品型号              | PXIe-9802（2 通道 14 位 250 MS/s 数字化仪） |
| AI 通道数             | 2 通道同步采样                          |
| AI 分辨率             | 14-bit（带宽 10MHz 以上）/ 16-bit（带宽 10MHz 以下） |
| AI 最大采样率         | 250 MS/s（每通道）                      |
| AI 输入范围           | ±0.316V（0 dBm）                        |
| AI 输入阻抗           | 50 Ω                                    |
| AI 输入耦合           | AC                                      |
| AI 模拟带宽（-3dB）   | 0.5 ~ 135 MHz                           |
| 通带平坦度            | ±1 dB（0.5~35 MHz ±0.5 dB，35~135 MHz ±0.5 dB） |
| 板载内存              | 1 GB DDR3（支持乒乓模式数据交换）        |
| 内部参考时钟          | 10 MHz TCXO，稳定度 ±0.5 ppm（0~55℃）   |
| 外部参考输入时钟      | 10 ± 0.25 MHz，50 Ω，-5~10 dBm，AC      |
| 输出时钟              | 10 MHz，±3 ppm，AC，10.5 dBm            |
| 总线接口              | PCI Express Gen2 ×8 Lane（PXIe）        |
| 触发方式              | 立即 / 软件 / 数字 / 模拟（Power/Window/Hysteresis） |
| 触发源（数字）        | Trig_In / PFI0 ~ PFI7                   |
| DDC 数字下变频        | 支持（Real / Complex IQ 数据格式）      |
| 工作温度              | 0 ~ 55 °C                               |
| 功耗                  | 22 W（Max），12 V 供电                  |

### 动态性能（-1dBFS 输入，100 MSps）

| 频率     | SFDR        | SNR         | THD        |
|---------|-------------|-------------|------------|
| 26 MHz  | 80 dBc      | 66 dBFS     | -75 dBc    |
| 42 MHz  | 82 dBc      | 67 dBFS     | -75 dBc    |
| 69 MHz  | 88 dBc      | 68 dBFS     | -76 dBc    |
| 96 MHz  | 86 dBc      | 68 dBFS     | -75 dBc    |

平均噪声密度 -142 dBm/Hz；SPUR -105 dBm；IP2 79（典型值）；IP3 32（典型值）

## 通用编程范式

所有 AI 任务遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读取数据 → Stop() → (可选) Channels.Clear()**

```csharp
var task = new JY9802AITask(0);          // 1. 创建（按槽位号）
task.AddChannel(0);                      // 2. 添加通道（仅通道号，无量程/耦合参数）
task.Mode = AIMode.Continuous;           // 3. 配置模式
task.SampleRate = 250e6;                 // 250 MS/s
task.Start();                            // 4. 启动
// ... 读取数据 ...
task.Stop();                             // 5. 停止
task.Channels.Clear();                   // 6. 清除通道（Record 模式后建议清除）
```

> **JY9802 特性注意**：
> - `AddChannel` 只接收 **通道号**（以及可选的 DDC 中心频率/数字增益），**没有** rangeLow/rangeHigh/coupling/impedance/bandwidth 参数（硬件输入固定 ±0.316V、50 Ω、AC）。
> - 没有独立的 `JY9802DITask` / `JY9802DOTask`（JY9802 不提供通用 DI/DO 功能，仅有 AI + AO 数字化仪/AWG 功能）。
> - `AITriggerType` 使用 `Software`（不是 `Soft`）。
> - 多卡同步通过 **外部 10 MHz 参考时钟 + TriggerOut/Trig_In** 实现，**不使用** PXI_Trig0 背板总线。
> - 流盘（Record）文件格式为 `VectorFile` 或 `Raw`，**不是** `.bin`。

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY9802AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Finite / Continuous / Record            |
| `SampleRate`         | `double`                | ADC 采样率（Sa/s），最高 250e6          |
| `TimeBaseRate`       | `double`                | 时钟基准                                |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集的样点数（默认 1000） |
| `BufLenInSamples`    | `ulong`                 | 缓冲区每通道 Sample 数                  |
| `AvailableSamples`   | `ulong`                 | 缓冲区内可读取的点数                    |
| `Trigger`            | `AITrigger`             | 触发配置对象                            |
| `Record`             | `Record`                | 流盘配置对象（Record 模式）             |
| `Advanced`           | `AIAdvanced`            | 高级属性（DataFormat/DDC/基带模式）     |
| `Channels`           | `List<AIChannel>`       | 已添加的通道列表                        |
| `Device`             | `JY9802Device`          | 设备对象（时钟源/同步导出）             |
| `EventQueue`         | -                       | WaitUntilDone/ReadBuffer 事件队列       |

#### AddChannel 重载

> **注意**：JY9802 的 `AddChannel` **不含** 量程、耦合、阻抗、带宽参数（硬件固定 ±0.316V、50Ω、AC）

```csharp
// Real 模式（默认）——仅通道号
void AddChannel(int channel)
// channel = 0 或 1 ；channel = -1 表示添加所有通道

// Complex 模式（DDC 数字下变频）——可指定中心频率与数字增益
void AddChannel(int channel, double centerFrequency, double digitalGain)
// centerFrequency：DDC 中心频率（Hz）
// digitalGain：数字增益倍数，取值为 2^N（N=0~7），自动向下取最接近的 2^N 值
```

#### 移除通道

```csharp
void RemoveChannel(int channel)   // channel = -1 移除所有通道
```

#### 读取数据

```csharp
// Finite/Continuous/Record — 单通道（一维数组）
void ReadData(ref double[] buf, int timeout);                       // 读全部可用样本
void ReadData(ref double[] buf, int samplesPerChannel, int timeout);

// Finite/Continuous/Record — 多通道（二维数组）——【按列排列】
void ReadData(ref double[,] buf, int timeout);
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout);

// 原始 ADC 数据（Int16，节省带宽）
void ReadRawData(ref short[] buf, int timeout);
void ReadRawData(ref short[,] buf, int timeout);

// IntPtr 指针读取（零拷贝，Complex/IQ Interleaved 高性能场景）
void ReadData(IntPtr buf, int samplesPerChannel, int timeout);

// Complex/IQ 格式读取（需 Advanced.DataFormat = Complex）
void ReadData(ref System.Numerics.Complex[] buf, int timeout);
void ReadData(ref System.Numerics.Complex[,] buf, int timeout);

// timeout = -1 → 永久等待；单位 ms
```

**重要：多通道数据排列格式**

- **单通道**：`double[]` 一维数组，长度 = 每通道点数
- **多通道**：`double[每通道点数, 通道数]` 二维数组，**每列代表一个通道**

```csharp
// 示例（2 通道，每通道 10000 点）
double[,] readValue = new double[10000, 2];  // [每通道点数, 通道数]
aiTask.ReadData(ref readValue, -1);

// 提取单个通道数据
for (int i = 0; i < 10000; i++)
{
    ch0Data[i] = readValue[i, 0];   // 第0列 = 通道0
    ch1Data[i] = readValue[i, 1];   // 第1列 = 通道1
}

// 使用 ArrayManipulation.Transpose 转置后绘图
double[,] displayValue = new double[2, 10000];
ArrayManipulation.Transpose(readValue, ref displayValue);
easyChartX1.Plot(displayValue);
```

#### AI 三种模式速查

| 模式         | 典型配置                      | 读取方式                                     |
| ------------ | ----------------------------- | -------------------------------------------- |
| `Finite`     | `+SamplesToAcquire=N`         | 轮询 `AvailableSamples` 或 `WaitUntilDone` 后 `ReadData` |
| `Continuous` | 不设置 SamplesToAcquire       | Timer/Thread 轮询 `AvailableSamples` → `ReadData`   |
| `Record`     | `Mode=AIMode.Record`          | 流盘写入文件，通过 `GetRecordPreviewData` 预览，`GetRecordStatus` 查询进度 |

#### 连续采集 Thread 模式（最常用）

```csharp
// 后台线程循环
private void ReadDataThread()
{
    while (!_semStopAI.WaitOne(0))
    {
        try
        {
            aiTask.ReadData(ref _readValue, 100);  // 100ms 超时
        }
        catch (JYDriverException) { continue; }    // 超时继续
        // 绘图 / 处理数据...
    }
}
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source    = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Condition = AIDigitalTriggerCondition.RisingEdge;
aiTask.Trigger.PreTriggerSamples = 0;   // 立即/延迟触发为 0；预/中间触发 > 0
```

#### 模拟触发配置

##### Power 比较器（电平触发）

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Type      = AIAnalogTriggerType.Power;
aiTask.Trigger.Analog.Source    = AIAnalogTriggerSource.CH0Raw;       // CH0Raw / CH1Raw
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.PowerHigh; // PowerHigh / PowerLow
aiTask.Trigger.Analog.HighLevel = 0.2;    // 电平（V）
```

##### Window 比较器（窗口触发）

```csharp
aiTask.Trigger.Analog.Type      = AIAnalogTriggerType.Window;
aiTask.Trigger.Analog.Source    = AIAnalogTriggerSource.CH0Raw;
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.EnteringWindow; // EnteringWindow / LeavingWindow
aiTask.Trigger.Analog.HighLevel = 0.25;
aiTask.Trigger.Analog.LowLevel  = -0.25;
```

##### Hysteresis 比较器（迟滞触发）

```csharp
aiTask.Trigger.Analog.Type      = AIAnalogTriggerType.Hysteresis;
aiTask.Trigger.Analog.Source    = AIAnalogTriggerSource.CH0Raw;
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.RisingHysteresis; // RisingHysteresis / FallingHysteresis
aiTask.Trigger.Analog.HighLevel = 0.2;
aiTask.Trigger.Analog.LowLevel  = 0.1;
```

#### 软件触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Software;
aiTask.Start();
aiTask.SendSoftwareTrigger();    // 立即使触发条件满足
```

#### 重触发（Retrigger）

```csharp
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 10240;
aiTask.Trigger.Type  = AITriggerType.Software;
aiTask.Trigger.Retrigger.Count = 5;   // 触发 5 次；-1 = 无限；0 = 不使能
aiTask.Trigger.AutoTriggerTime = -1;  // us，-1 永久等待
aiTask.Trigger.HoldOffTime     = 0;   // us，两次触发之间最小间隔
aiTask.Start();

// 查询重触发状态
int availableCount  = aiTask.Trigger.Retrigger.AvailableCount;  // 已完成未读取
int completedCount  = aiTask.Trigger.Retrigger.CompletedCount;  // 已完成总数
```

#### 预触发（PreTrigger）

```csharp
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 10000;
aiTask.Trigger.PreTriggerSamples = 2000;  // 触发前采样 2000 点，触发后 8000 点
```

### DDC 数字下变频（Complex/IQ 模式）

JY9802 支持板载 DDC 数字下变频，可直接输出复数 IQ 数据，适合射频/中频信号分析：

```csharp
var aiTask = new JY9802AITask(0);
aiTask.Advanced.DataFormat           = DataFormat.Complex;   // Real / Complex
aiTask.Advanced.EnableDDC            = true;
aiTask.Advanced.EnableDDCLowLatency  = false;                // 仅使用 CIC，不使用 HB（低延时）
aiTask.Advanced.EnableBasebandMode   = false;

// Complex 模式下 AddChannel 可指定 DDC 中心频率
aiTask.AddChannel(0, centerFrequency: 70e6, digitalGain: 4); // 70 MHz 中心，增益 4（2^2）

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100e6;
aiTask.Start();

// 读取 Complex IQ 数据
var iqData = new System.Numerics.Complex[10000, aiTask.Channels.Count];
aiTask.ReadData(ref iqData, -1);
```

---

## 流盘模式（Record）

JY9802 支持将数据流式写入文件（板载 1 GB DDR3 乒乓缓存支持超长录制，推荐配合简仪磁盘阵列模块）：

```csharp
aiTask = new JY9802AITask(0);
aiTask.AddChannel(0);
aiTask.AddChannel(1);   // 或一次性 AddChannel(-1)

aiTask.Mode              = AIMode.Record;
aiTask.Record.Mode       = RecordMode.Finite;          // Finite 有限 / Infinite 无限
aiTask.Record.Length     = 10.0;                       // 录制时长（秒），Finite 模式有效
aiTask.Record.FilePath   = @"C:\Data\record.iq";       // 绝对路径（含文件名）
aiTask.Record.Format     = RecordFormat.VectorFile;    // VectorFile（含4K Header）/ Raw（纯二进制）
aiTask.Record.EnablePreview      = true;               // 启用预览
aiTask.Record.MaxMemorySizeInByte = 200;               // 最大内存占用（MB），最小 200

aiTask.SampleRate = 250e6;
aiTask.Start();
```

### Record 模式预览与状态查询

```csharp
// Timer/线程循环中：
double recordedLength;
bool   recordDone;
aiTask.GetRecordStatus(out recordedLength, out recordDone);

if (recordDone)
{
    aiTask.Stop();
    aiTask.Channels.Clear();
    MessageBox.Show($"记录完毕！已流盘 {recordedLength:F1} 秒");
}
else if (aiTask.Record.EnablePreview)
{
    // 获取预览数据（二维数组，按列排列）
    double[,] previewBuffer = new double[10240, aiTask.Channels.Count];
    aiTask.GetRecordPreviewData(ref previewBuffer, -1);

    double[,] displayBuffer = new double[aiTask.Channels.Count, 10240];
    ArrayManipulation.Transpose(previewBuffer, ref displayBuffer);
    easyChartX1.Plot(displayBuffer);
}
```

> **注意**：当采样率过高、硬盘写入速度成为瓶颈时，预览功能会自动被禁用。连续（Infinite）模式下务必关注硬盘持续写入能力。

---

## 多卡同步（MultiCard Sync）

JY9802 通过 **主卡导出 10 MHz 参考时钟 + AIRefTrigger0 触发信号到 Ref_Out / TriggerOut**，**从卡外部时钟接收 + Trig_In 数字触发** 实现多卡同步：

```csharp
// ===== 主卡配置（Slot 4）=====
var masterTask = new JY9802AITask(4);

// 1. 配置主卡导出时钟和触发信号
masterTask.Device.Sync.ExportClock[ExportClockDestination.Ref_Out].Source                  = ExportClockSource.SpecifiedFrequency;
masterTask.Device.Sync.ExportClock[ExportClockDestination.Ref_Out].ExportSourceFrequency   = 50e6;                // 导出 50 MHz 时钟
masterTask.Device.Sync.ExportSignal[ExportSignalDestination.TriggerOut]                    = ExportSignalSource.AIRefTrigger0;
masterTask.Device.Sync.Commit();

// 2. 主卡 AI 配置
masterTask.AddChannel(-1);                       // 两通道
masterTask.Mode = AIMode.Finite;
masterTask.Trigger.Type           = AITriggerType.Software;
masterTask.Trigger.Retrigger.Count = 10;
masterTask.SampleRate       = 250e6;
masterTask.SamplesToAcquire = 10240;
masterTask.Commit();

// ===== 从卡配置（Slot 6）=====
var slaveTask = new JY9802AITask(6);
slaveTask.AddChannel(-1);
slaveTask.Mode = AIMode.Finite;

// 1. 从卡接收外部参考时钟
slaveTask.Device.ClockSource           = ClockSource.External;
slaveTask.Device.ExternalClockFrequency = 50e6;

// 2. 从卡接收主卡触发信号
slaveTask.Trigger.Type            = AITriggerType.Digital;
slaveTask.Trigger.Digital.Source  = AIDigitalTriggerSource.Trig_In;
slaveTask.Trigger.Retrigger.Count = 10;

slaveTask.SampleRate       = 250e6;
slaveTask.SamplesToAcquire = 10240;

// ===== 启动顺序：先从卡后主卡 =====
slaveTask.Start();                   // 从卡先启动，等待触发
masterTask.Start();                  // 主卡启动
masterTask.SendSoftwareTrigger();    // 主卡软触发 → 从 TriggerOut 导出 → 从卡 Trig_In 接收
```

**关键要点：**
- 使用 `Device.Sync.ExportClock[...]` / `ExportSignal[...]` 索引器配置导出通道
- 主卡 `Sync.Commit()` 必须在 AI 任务 `Start()` 之前调用
- 从卡通过 `Device.ClockSource = External` + `ExternalClockFrequency` 接收外部参考时钟
- 从卡触发源固定为 `AIDigitalTriggerSource.Trig_In`
- 启动顺序：**先从卡后主卡**
- 两卡 `SampleRate`、`SamplesToAcquire`、`Retrigger.Count` 必须一致

---

## 参考时钟与设备对象

```csharp
// 通过 JY9802Device 访问底层硬件属性
var device = JY9802Device.GetInstance(slotNum);          // 单例，每个槽位唯一实例

// 参考时钟源
device.ClockSource             = ClockSource.Internal;   // Internal / External
device.ExternalClockFrequency  = 10e6;                   // 外部参考时钟频率（默认 10 MHz）

// 只读属性
double tempBoard = device.Temperature;                   // 板卡温度
double tempFPGA  = device.FPGATemperature;               // FPGA 温度
int    aiChCnt   = device.AIChannelCount;                // AI 通道数 = 2
int    aoChCnt   = device.AOChannelCount;                // AO 通道数

// 设备信息
var info = device.DeviceInformation;
Console.WriteLine($"SN: {info.ProductSerialNum}, HW: {info.HardwareVersion}, FW: {info.FirmwareVersion}");
Console.WriteLine($"板载内存: {info.OnboardMemoryCapacity}");
Console.WriteLine($"AWG 支持: {info.Ability.AWGSupported}, 数字化仪支持: {info.Ability.DigitizerSupported}");
```

---

## 模拟输出（AO / AWG）

JY9802 同时支持 AWG（任意波形发生器）功能，通过 `JY9802AOTask` 使用：

```csharp
var aoTask = new JY9802AOTask(0);
aoTask.AddChannel(0);                         // 或 AddChannel(0, delay, digitalGain, centerFrequency)
aoTask.Mode         = AOMode.Finite;          // Finite / ContinuousWrapping / Stream
aoTask.UpdateRate   = 250e6;                  // DAC 更新率（31.25M ~ 250M）
aoTask.DataSource   = AODataSource.Waveform;  // Waveform（用户数据）/ BIT（板载测试信号）

double[] wave = new double[10000];            // 生成波形...
aoTask.WaveformLength = wave.Length;
aoTask.WriteData(wave, -1);
aoTask.Start();
```

> 详细 AO API 参见 [reference.md](reference.md)。

---

## 常见错误处理

| 异常代码                          | 错误码     | 原因                     | 处理建议              |
| --------------------------------- | ---------- | ------------------------ | --------------------- |
| `ErrorOpenDeviceFailed`           | -10001     | 板卡未连接或槽位号错误   | 检查设备管理器槽位号  |
| `ErrorDeviceSlotNum`              | -10004     | 槽位号无效               | 核对 JYTEK 管理器     |
| `ErrorTimeOut`                    | -10008     | 读取数据超时             | 增大 timeout 或检查采样率 |
| `ErrorCountOfDataOverflow`        | -10009     | 缓冲区溢出               | 增大读取频率或减小采样率 |
| `ErrorCountOfDataNotEnough`       | -10010     | 数据不足                 | 等待 AvailableSamples 足够 |
| `ErrorSampleRateParam`            | -10017     | 采样率超过硬件上限       | 最大 250 MS/s         |
| `ErrorTriggerTypeParam`           | -10032     | 触发类型参数错误         | 检查 AITriggerType 枚举 |
| `ErrorPreTriggerSamplesParam`     | -10041     | 预触发点数超范围         | PreTriggerSamples < SamplesToAcquire |
| `ErrorChannelParam`               | -10038     | 通道号非法               | 仅支持 0、1、-1       |
| `ErrorClockSource`                | -10015     | 时钟源配置错误           | 检查外部时钟是否接入   |

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)


---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY9802;
using System.Numerics;                       // Complex IQ 数据
using SeeSharpTools.JY.ArrayUtility;         // 数组转置工具
using SeeSharpTools.JY.FileIO.VectorFile;    // VectorFile 读写
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9802\Bin\JY9802.dll
```

---

## JY9802AITask — 模拟输入任务

### 构造函数

```csharp
new JY9802AITask(int slotNumber)      // 按槽位号创建（推荐）
new JY9802AITask(string aliasName)    // 按板卡别名创建
```

### 属性

| 属性                | 类型                | 说明                                        |
| ------------------- | ------------------- | ------------------------------------------- |
| `Mode`              | `AIMode`            | Finite / Continuous / Record                |
| `SampleRate`        | `double`            | ADC 采样率（Sa/s），最高 250e6；Commit 前是设定值，Commit 后是实际值 |
| `TimeBaseRate`      | `double`            | 时钟基准                                    |
| `SamplesToAcquire`  | `int`               | Finite 每通道采集点数（默认 1000）          |
| `BufLenInSamples`   | `ulong`             | 缓冲区大小（每通道样点数）                  |
| `AvailableSamples`  | `ulong`             | 缓冲区内可读取的点数                        |
| `Trigger`           | `AITrigger`         | AI 触发参数配置                             |
| `Advanced`          | `AIAdvanced`        | 高级属性（DataFormat/DDC/基带）             |
| `Record`            | `Record`            | 流盘相关参数                                |
| `Channels`          | `List<AIChannel>`   | 通道列表                                    |
| `Device`            | `JY9802Device`      | 底层设备对象                                |
| `EventQueue`        | -                   | 事件通知队列                                |

### 方法

```csharp
void AddChannel(int channel)
void AddChannel(int channel, double centerFrequency, double digitalGain)   // DDC Complex 模式
void RemoveChannel(int channel)         // -1 移除所有

void Commit()                           // 提交参数至设备，之后可获取实际 SampleRate
void Start()
void Stop()
void WaitUntilDone(int timeout)         // Finite 模式等待完成，-1 永久
void WaitForDataReady(int blockSize, int timeout)  // Continuous 流控
void SendSoftwareTrigger()              // 软件触发

// Real/Complex 数据读取
void ReadData(ref double[] buf, int timeout)
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)
void ReadData(ref double[,] buf, int timeout)
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)
void ReadData(ref Complex[] buf, int timeout)
void ReadData(ref Complex[,] buf, int timeout)
void ReadData(IntPtr buf, int samplesPerChannel, int timeout)

// Int16 原始数据读取
void ReadRawData(ref short[] buf, int timeout)
void ReadRawData(ref short[] buf, int samplesPerChannel, int timeout)
void ReadRawData(ref short[,] buf, int timeout)
void ReadRawData(ref short[,] buf, int samplesPerChannel, int timeout)
void ReadRawData(ref short[] buf, ref double[] scaleFactor, ref bool[] isOverLoad, int timeout)
void ReadRawData(IntPtr buf, int samplesToRead, int timeout)

// 基带（Baseband）专用读取
void ReadData_Baseband(ref double[] buf, int timeout)
void ReadData_Baseband(ref double[,] buf, int timeout)

// 中频（IF）专用读取
void ReadData_IF(ref double[] buf, int timeout)
void ReadData_IF(ref double[,] buf, int timeout)

// 流盘相关
void GetRecordPreviewData(ref double[,] buf, int timeout)
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout)
void GetRecordStatus(out double recordedLength, out bool recordDone)
```

---

## JY9802AOTask — 模拟输出任务（AWG）

### 构造函数

```csharp
new JY9802AOTask(int slotNumber)
new JY9802AOTask(string aliasName)
```

### 属性

| 属性                    | 类型          | 说明                                |
| ----------------------- | ------------- | ----------------------------------- |
| `Mode`                  | `AOMode`      | Finite / ContinuousWrapping / Stream |
| `UpdateRate`            | `double`      | DAC 更新率（Real：31.25M ~ 250M）   |
| `TimeBaseRate`          | `double`      | 时钟基准                            |
| `WaveformLength`        | `int`         | 任意波形长度（Sample 为单位）       |
| `Channels`              | `List<AOChannel>` | 通道列表                        |
| `AvailableStreamSpace`  | `ulong`       | Stream Buffer 空闲点数              |
| `Trigger`               | `AOTrigger`   | AO 触发配置                         |
| `BuildInTestSignal`     | -             | 板载数字发生器（BIT）               |
| `DataSource`            | `AODataSource`| Waveform / BIT                      |
| `Playback`              | `Playback`    | 回放（流盘读取）配置                |

### 方法

```csharp
void AddChannel(int channel)
void AddChannel(int channel, double delay, double digitalGain, int centerFrequency)
void RemoveChannel(int channel)

void Commit()
void Start()
void Stop()
void WaitUntilDone(int timeout)
void SendSoftwareTrigger()
void GetGenerationStatus(out bool done, out long sampleGenerated, out int availableBufferLengthInSample)
void GetPlaybackStatus(out AOPlaybackStatus status)
void GetPlaybackStatus(out double playedLength, out bool playedDone)

// 数据写入
void WriteData(double[] data, int timeout)
void WriteData(double[,] data, int timeout)
void WriteData(Complex[] data, int timeout)
void WriteData(Complex[,] data, int timeout)
void WriteData(IntPtr data, int samplesToWrite, int timeout)
void WriteRawData(short[] data, int timeout)
void WriteRawData(short[,] data, int timeout)
void WriteRawData(IntPtr data, int samplesToWrite, int timeout)
```

---

## JY9802Device — 设备对象

```csharp
var device = JY9802Device.GetInstance(slotNum);         // 单例
// 或 JY9802Device.GetInstance(aliasName);
```

### 属性

| 属性                        | 类型                        | 说明                       |
| --------------------------- | --------------------------- | -------------------------- |
| `ClockSource`               | `ClockSource`               | Internal / External        |
| `ExternalClockFrequency`    | `double`                    | 外部参考时钟频率（默认 10e6） |
| `DeviceInformation`         | `DeviceInformation`         | SN / HW / FW / 板载内存     |
| `Temperature`               | `double`                    | 板卡温度                   |
| `FPGATemperature`           | `double`                    | FPGA 温度                  |
| `Sync`                      | `Sync`                      | 同步（导出时钟/信号）配置  |
| `CardID`                    | `int`                       | 注册后的 cardID            |
| `AIChannelCount`            | `int`                       | AI 通道总数（2）           |
| `AOChannelCount`            | `int`                       | AO 通道总数                |
| `List`                      | -                           | 所有设备实例列表           |

### 方法

```csharp
static JY9802Device GetInstance(int slotNum)
static JY9802Device GetInstance(string aliasName)
void AITaskInit()
void AOTaskInit()
void Release()
```

---

## Sync — 同步配置

```csharp
device.Sync.ExportClock[ExportClockDestination.Ref_Out].Source                = ExportClockSource.AISampleClk;
device.Sync.ExportClock[ExportClockDestination.Ref_Out].ExportSourceFrequency = 50e6;
device.Sync.ExportSignal[ExportSignalDestination.TriggerOut]                  = ExportSignalSource.AIRefTrigger0;
device.Sync.Commit();
```

### ExportClockDestination 枚举

| 值        | 说明         |
| --------- | ------------ |
| `Ref_Out` | Ref_Out 端口 |

### ExportClockSource 枚举

| 值                   | 说明                                  |
| -------------------- | ------------------------------------- |
| `None`               | 不导出                                |
| `AISampleClk`        | AI 采样时钟                           |
| `SpecifiedFrequency` | 导出指定频率（需设 ExportSourceFrequency） |

### ExportSignalDestination 枚举

| 值                         | 说明        |
| -------------------------- | ----------- |
| `PFIOut0` ~ `PFIOut7`      | PFI 输出 0~7 |
| `TriggerOut`               | Trigger Out 端口 |

### ExportSignalSource 枚举

| 值                             | 说明                        |
| ------------------------------ | --------------------------- |
| `None`                         | 不导出                      |
| `AIReadyForStartTrigger`       | AI 已就绪等待 Start Trigger |
| `AIReadyForRefTrigger`         | AI 已就绪等待 Ref Trigger   |
| `AIReadyForAdvanceTrigger`     | AI 已就绪等待 Advance Trigger |
| `AIStartTrigger`               | AI 启动触发                 |
| `AIRefTrigger0`                | AI 参考触发 0               |
| `AIAdvanceTriggerO`            | AI 推进触发                 |
| `AIEndOfRecord`                | 单次记录结束                |
| `AIEndOfAcquisition`           | 采集结束                    |
| `AIPowerTrigger`               | 电平触发输出                |
| `AIAnalogTrigger`              | 模拟触发输出                |

---

## AIMode 枚举

| 值           | 说明                                         |
| ------------ | -------------------------------------------- |
| `Finite`     | 有限点方式（达到 SamplesToAcquire 后停止）   |
| `Continuous` | 连续方式（持续采集，应用轮询读取）           |
| `Record`     | 流盘方式（板载 1 GB 乒乓缓存流式写入文件）   |

---

## AITriggerType 枚举

| 值          | 说明               |
| ----------- | ------------------ |
| `Immediate` | 立即触发（默认）   |
| `Software`  | 软件触发           |
| `Digital`   | 外部数字触发       |
| `Analog`    | 外部模拟触发       |

---

## AITrigger — 触发配置类

| 属性                  | 类型                   | 说明                                                           |
| --------------------- | ---------------------- | -------------------------------------------------------------- |
| `Type`                | `AITriggerType`        | 触发类型                                                       |
| `PreTriggerSamples`   | `uint`                 | 触发前采样点数（立即/延迟为 0，预/中间触发 > 0）               |
| `AutoTriggerTime`     | `double`               | 两次触发间最大等待时间，超时自动触发，-1 永久等待（单位 us）    |
| `HoldOffTime`         | `double`               | 两次触发最小间隔（Hold-off 期间不接受新触发，单位 us）          |
| `Retrigger`           | `AIRetrigger`          | 重触发配置                                                     |
| `Digital`             | `AIDigitalTrigger`     | 数字触发配置                                                   |
| `Analog`              | `AIAnalogTrigger`      | 模拟触发配置                                                   |

### AIRetrigger

| 属性               | 类型    | 说明                                 |
| ------------------ | ------- | ------------------------------------ |
| `Count`            | `int`   | -1 无限、0 不使能、>0 重触发次数     |
| `AvailableCount`   | `int`   | 已完成未读取的触发次数（只读）       |
| `CompletedCount`   | `int`   | 已完成触发次数（只读）               |

### AIDigitalTrigger

| 属性        | 类型                       | 说明                 |
| ----------- | -------------------------- | -------------------- |
| `Source`    | `AIDigitalTriggerSource`   | 数字触发源           |
| `Condition` | `AIDigitalTriggerCondition`| 边沿/电平触发条件    |

### AIDigitalTriggerSource 枚举

| 值         | 说明                          |
| ---------- | ----------------------------- |
| `Trig_In`  | 外部数字触发输入端子          |
| `PFI0` ~ `PFI7` | 前面板 PFI 端子 0~7       |

### AIDigitalTriggerCondition 枚举

| 值             | 说明       |
| -------------- | ---------- |
| `RisingEdge`   | 上升沿触发 |
| `FallingEdge`  | 下降沿触发 |
| `HighLevel`    | 高电平触发 |
| `LowLevel`     | 低电平触发 |

### AIAnalogTrigger

| 属性         | 类型                          | 说明                |
| ------------ | ----------------------------- | ------------------- |
| `Type`       | `AIAnalogTriggerType`         | 比较器类型          |
| `Source`     | `AIAnalogTriggerSource`       | 模拟触发源          |
| `Condition`  | `AIAnalogTriggerCondition`    | 触发条件            |
| `HighLevel`  | `double`                      | 高阈值（V）         |
| `LowLevel`   | `double`                      | 低阈值（V）         |

### AIAnalogTriggerType 枚举

| 值            | 说明          |
| ------------- | ------------- |
| `Power`       | 电平（功率）触发 |
| `Window`      | 窗口触发      |
| `Hysteresis`  | 迟滞触发      |

### AIAnalogTriggerSource 枚举

| 值        | 说明      |
| --------- | --------- |
| `CH0Raw`  | 通道 0 原始信号 |
| `CH1Raw`  | 通道 1 原始信号 |

### AIAnalogTriggerCondition 枚举

| 值                       | 说明                    |
| ------------------------ | ----------------------- |
| `EnteringWindow`         | 信号进入窗口（Window 类型） |
| `LeavingWindow`          | 信号离开窗口（Window 类型） |
| `RisingHysteresis`       | 上升沿迟滞（Hysteresis） |
| `FallingHysteresis`      | 下降沿迟滞（Hysteresis） |
| `PowerHigh`              | 高电平触发（Power）     |
| `PowerLow`               | 低电平触发（Power）     |

---

## AIAdvanced — 高级属性

| 属性                    | 类型          | 说明                                      |
| ----------------------- | ------------- | ----------------------------------------- |
| `DataFormat`            | `DataFormat`  | Real（ADC 原始数据）/ Complex（DDC IQ）   |
| `EnableDDC`             | `bool`        | 是否使能 DDC 数字下变频                   |
| `EnableDDCLowLatency`   | `bool`        | 低延时 DDC（仅用 CIC，不用 HB 半带滤波器）|
| `EnableBasebandMode`    | `bool`        | 是否使能基带模式                          |

### DataFormat 枚举

| 值        | 说明                   |
| --------- | ---------------------- |
| `Real`    | 获取 ADC 原始数据       |
| `Complex` | 获取 DDC 的 IQ 数据     |

---

## Record — 流盘配置

| 属性                    | 类型           | 说明                              |
| ----------------------- | -------------- | --------------------------------- |
| `FilePath`              | `string`       | 流盘文件绝对路径（含文件名）       |
| `Mode`                  | `RecordMode`   | Finite / Infinite                 |
| `Length`                | `double`       | 流盘时长（秒），Finite 模式有效   |
| `EnablePreview`         | `bool`         | 是否使能预览                      |
| `Format`                | `RecordFormat` | VectorFile / Raw                  |
| `MaxMemorySizeInByte`   | `int`          | 最大使用内存（MB），最小 200      |

### RecordMode 枚举

| 值         | 说明                                |
| ---------- | ----------------------------------- |
| `Finite`   | 有限长度流盘（按 Length 秒数）      |
| `Infinite` | 无限长度流盘（直到手动停止）        |

### RecordFormat 枚举

| 值           | 说明                                    |
| ------------ | --------------------------------------- |
| `VectorFile` | 简仪矢量文件格式（包含 4K Header，推荐） |
| `Raw`        | 原始二进制文件（无 Header）             |

---

## ClockSource 枚举（Device 层）

| 值         | 说明         |
| ---------- | ------------ |
| `Internal` | 内部时钟源   |
| `External` | 外部时钟源   |

---

## AIChannel — 通道对象

| 属性             | 类型      | 说明                                             |
| ---------------- | --------- | ------------------------------------------------ |
| `ChannelID`      | `int`     | 通道物理序号（0 或 1）                           |
| `Frequency`      | `double`  | DDC 中心频率（Complex 模式有效）                 |
| `PreFilterGain`  | `double`  | 前置滤波器增益                                   |
| `DigitalGain`    | `int`     | 数字增益，取值为 2^N（N=0~7）                    |
| `ScaleFactor`    | `double`  | 电压换算系数（Commit 后可获取）                  |

---

## 异常类 JYDriverException

```csharp
try
{
    aiTask.Start();
}
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);
}
```

---

## 设备硬件规格（总表）

| 参数              | 值                                   |
| ----------------- | ------------------------------------ |
| AI 通道数         | 2                                    |
| AI 分辨率         | 14 bits（>10 MHz 带宽）/ 16 bits（<10 MHz 带宽） |
| AI 最大采样率     | 250 MS/s                             |
| AI 输入范围       | ±0.316V（0 dBm）                     |
| AI 输入阻抗       | 50 Ω（固定）                         |
| AI 输入耦合       | AC（固定）                           |
| AI 带宽（-3dB）   | 0.5 ~ 135 MHz                        |
| 板载内存          | 1 GB DDR3                            |
| 参考时钟          | 10 MHz 内部 TCXO ±0.5 ppm / 外部 10±0.25 MHz |
| 接口              | PCI Express Gen2 ×8 Lane（PXIe）     |
| DDC 支持          | 是（Real / Complex IQ）              |
| AO（AWG）         | 支持                                 |
| DI / DO           | 不提供                               |

---

# 完整代码示例

> 所有示例均来自 `JY9802.Examples/Digitizer/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 连续采集（后台线程 + Transpose 绘图）

```csharp
using System;
using System.Threading;
using System.Windows.Forms;
using JY9802;
using SeeSharpTools.JY.ArrayUtility;

public partial class MainForm : Form
{
    private JY9802AITask aiTask;
    private double[,] _readValue;
    private double[,] _displayValue;
    private int _readCnt;
    private Semaphore _semStopAI = new Semaphore(0, 1);
    private Semaphore _semPlot = new Semaphore(1, 1);
    private Thread _threadReadData;
    private bool _aiStartFlag = false;

    private void Start_Click(object sender, EventArgs e)
    {
        aiTask = new JY9802AITask(0);
        try
        {
            _readCnt = 10 * 1024;
            aiTask.AddChannel(0);               // 通道 0
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 250e6;          // 250 MS/s
            aiTask.Start();
            _aiStartFlag = true;

            _readValue    = new double[_readCnt, aiTask.Channels.Count];
            _displayValue = new double[aiTask.Channels.Count, _readCnt];

            _threadReadData = new Thread(ReadDataThread);
            _threadReadData.Start();
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    }

    private void ReadDataThread()
    {
        while (!_semStopAI.WaitOne(0))
        {
            try
            {
                aiTask.ReadData(ref _readValue, 100);
            }
            catch (JYDriverException) { continue; }

            if (_semPlot.WaitOne(0))
            {
                _semPlot.Release();
                this.BeginInvoke(new Action(() =>
                {
                    _semPlot.WaitOne(-1);
                    ArrayManipulation.Transpose(_readValue, ref _displayValue);
                    easyChartX1.Plot(_displayValue);
                    Thread.Sleep(50);
                    _semPlot.Release();
                }));
            }
        }
    }

    private void Stop_Click(object sender, EventArgs e)
    {
        if (_aiStartFlag)
        {
            _semStopAI.Release();
            _threadReadData.Join(500);
            aiTask.Stop();
            _aiStartFlag = false;
        }
    }
}
```

---

## 示例 2：AI 有限点采集（双通道 + 软件触发）

```csharp
private JY9802AITask aiTask;

private void Start_Click(object sender, EventArgs e)
{
    aiTask = new JY9802AITask(0);
    try
    {
        aiTask.AddChannel(-1);                       // 添加所有通道（通道 0 和 1）
        aiTask.Mode              = AIMode.Finite;
        aiTask.Trigger.Type      = AITriggerType.Software;
        aiTask.SampleRate        = 250e6;
        aiTask.SamplesToAcquire  = 10 * 1024;

        aiTask.Start();
        aiTask.SendSoftwareTrigger();

        double[,] readValue = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
        while (aiTask.AvailableSamples < (ulong)aiTask.SamplesToAcquire)
            Thread.Sleep(1);

        aiTask.ReadData(ref readValue, 5000);
        aiTask.Stop();

        double[,] displayValue = new double[aiTask.Channels.Count, aiTask.SamplesToAcquire];
        ArrayManipulation.Transpose(readValue, ref displayValue);
        easyChartX1.Plot(displayValue);
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```

---

## 示例 3：AI 数字触发（PFI0 上升沿，带预触发）

```csharp
aiTask = new JY9802AITask(0);
aiTask.AddChannel(0);
aiTask.Mode        = AIMode.Continuous;
aiTask.SampleRate  = 250e6;

aiTask.Trigger.Type              = AITriggerType.Digital;
aiTask.Trigger.Digital.Source    = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Condition = AIDigitalTriggerCondition.RisingEdge;
aiTask.Trigger.PreTriggerSamples = 1024;        // 预触发 1024 点

aiTask.Start();
// 等待触发信号到来后自动采集
```

---

## 示例 4：AI 模拟触发（CH0 电平触发 / 窗口触发 / 迟滞触发）

```csharp
// Power（电平）触发
aiTask.Trigger.Type             = AITriggerType.Analog;
aiTask.Trigger.Analog.Type      = AIAnalogTriggerType.Power;
aiTask.Trigger.Analog.Source    = AIAnalogTriggerSource.CH0Raw;
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.PowerHigh;
aiTask.Trigger.Analog.HighLevel = 0.2;      // 超过 0.2 V 触发

// Window（窗口）触发
aiTask.Trigger.Analog.Type      = AIAnalogTriggerType.Window;
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.EnteringWindow;
aiTask.Trigger.Analog.HighLevel = 0.25;
aiTask.Trigger.Analog.LowLevel  = -0.25;

// Hysteresis（迟滞）触发
aiTask.Trigger.Analog.Type      = AIAnalogTriggerType.Hysteresis;
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.RisingHysteresis;
aiTask.Trigger.Analog.HighLevel = 0.20;
aiTask.Trigger.Analog.LowLevel  = 0.10;
```

---

## 示例 5：AI 重触发（Retrigger）+ 数字触发

```csharp
aiTask = new JY9802AITask(0);
aiTask.AddChannel(-1);
aiTask.Mode              = AIMode.Finite;
aiTask.SamplesToAcquire  = 10 * 1024;
aiTask.SampleRate        = 250e6;

aiTask.Trigger.Type              = AITriggerType.Digital;
aiTask.Trigger.Digital.Source    = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Condition = AIDigitalTriggerCondition.RisingEdge;
aiTask.Trigger.Retrigger.Count   = 10;          // 重触发 10 次

aiTask.Start();

// 每次触发完成后读取一段数据
double[,] segment = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
for (int i = 0; i < 10; i++)
{
    while (aiTask.AvailableSamples < (ulong)aiTask.SamplesToAcquire)
        Thread.Sleep(1);
    aiTask.ReadData(ref segment, -1);
    // 处理第 i 段波形...
}
aiTask.Stop();
```

---

## 示例 6：AI 流盘（Record）+ 实时预览

```csharp
private JY9802AITask aiTask;
private double[,] previewBuffer;
private double[,] previewBufferTansposed;
private int previewSamplesPerChannel;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9802AITask(0);
        // 设置参考时钟源
        JY9802Device.GetInstance(0).ClockSource = ClockSource.Internal;

        // 添加通道
        aiTask.AddChannel(0);
        aiTask.AddChannel(1);

        // 配置流盘
        aiTask.Mode               = AIMode.Record;
        aiTask.Record.Mode        = RecordMode.Finite;      // 有限流盘
        aiTask.Record.Length      = 10.0;                    // 10 秒
        aiTask.Record.FilePath    = @"C:\Data\record.iq";
        aiTask.Record.Format      = RecordFormat.VectorFile; // VectorFile / Raw
        aiTask.Record.EnablePreview = true;

        aiTask.SampleRate = 250e6;

        if (aiTask.Record.EnablePreview)
        {
            previewSamplesPerChannel = 10 * 1024;
            previewBuffer           = new double[previewSamplesPerChannel, aiTask.Channels.Count];
            previewBufferTansposed  = new double[aiTask.Channels.Count, previewSamplesPerChannel];
        }

        aiTask.Start();
        timer1.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer1_Tick(object sender, EventArgs e)
{
    try
    {
        timer1.Enabled = false;

        double recordedLength;
        bool   recordDone;
        aiTask.GetRecordStatus(out recordedLength, out recordDone);

        if (recordDone)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
            MessageBox.Show("记录完毕！");
            return;
        }

        toolStripStatusLabel1.Text = $"已流盘时长：{(int)recordedLength} s";

        if (aiTask.Record.EnablePreview)
        {
            aiTask.GetRecordPreviewData(ref previewBuffer, -1);
            ArrayManipulation.Transpose(previewBuffer, ref previewBufferTansposed);
            easyChartX1.Plot(previewBufferTansposed);
        }

        timer1.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```

---

## 示例 7：DDC 数字下变频 Complex/IQ 采集

```csharp
using System.Numerics;

var aiTask = new JY9802AITask(0);

// 配置 DDC Complex 模式
aiTask.Advanced.DataFormat           = DataFormat.Complex;
aiTask.Advanced.EnableDDC            = true;
aiTask.Advanced.EnableDDCLowLatency  = false;

// DDC 通道：中心频率 70 MHz，数字增益 4 (2^2)
aiTask.AddChannel(0, centerFrequency: 70e6, digitalGain: 4);

aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 100e6;

aiTask.Start();

Complex[] iq = new Complex[10000];
aiTask.ReadData(ref iq, -1);

// 计算瞬时幅度与相位
double[] magnitude = new double[iq.Length];
double[] phase     = new double[iq.Length];
for (int i = 0; i < iq.Length; i++)
{
    magnitude[i] = iq[i].Magnitude;
    phase[i]     = iq[i].Phase;
}
```

---

## 示例 8：多卡同步（主卡导出时钟 + TriggerOut，从卡接收 Trig_In）

```csharp
private JY9802AITask aitaskMaster;
private JY9802AITask aitaskSlave;

private void Start_Click(object sender, EventArgs e)
{
    aitaskMaster = new JY9802AITask(4);   // 主卡
    aitaskSlave  = new JY9802AITask(6);   // 从卡

    try
    {
        // === 主卡导出时钟 + AIRefTrigger0 到 TriggerOut ===
        aitaskMaster.Device.Sync.ExportSignal[ExportSignalDestination.TriggerOut] =
            ExportSignalSource.AIRefTrigger0;
        aitaskMaster.Device.Sync.ExportClock[ExportClockDestination.Ref_Out].Source =
            ExportClockSource.SpecifiedFrequency;
        aitaskMaster.Device.Sync.ExportClock[ExportClockDestination.Ref_Out].ExportSourceFrequency = 50e6;
        aitaskMaster.Device.Sync.Commit();

        // === 主卡 AI 配置 ===
        aitaskMaster.AddChannel(-1);
        aitaskMaster.Mode                 = AIMode.Finite;
        aitaskMaster.Trigger.Type         = AITriggerType.Software;
        aitaskMaster.Trigger.Retrigger.Count = 5;
        aitaskMaster.SampleRate           = 250e6;
        aitaskMaster.SamplesToAcquire     = 10 * 1024;
        aitaskMaster.Commit();

        // === 从卡 AI 配置 ===
        aitaskSlave.AddChannel(-1);
        aitaskSlave.Mode                  = AIMode.Finite;
        aitaskSlave.Device.ClockSource    = ClockSource.External;    // 接收外部时钟
        aitaskSlave.Device.ExternalClockFrequency = 50e6;
        aitaskSlave.Trigger.Type          = AITriggerType.Digital;
        aitaskSlave.Trigger.Digital.Source = AIDigitalTriggerSource.Trig_In; // 接收 Trig_In
        aitaskSlave.Trigger.Retrigger.Count = 5;
        aitaskSlave.SampleRate            = 250e6;
        aitaskSlave.SamplesToAcquire      = 10 * 1024;

        // === 先启动从卡，再启动主卡 ===
        aitaskSlave.Start();
        aitaskMaster.Start();

        // 发送主卡软件触发 → 通过 TriggerOut 路由给从卡
        aitaskMaster.SendSoftwareTrigger();
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer1_Tick(object sender, EventArgs e)
{
    if (aitaskMaster.AvailableSamples >= (ulong)aitaskMaster.SamplesToAcquire &&
        aitaskSlave.AvailableSamples  >= (ulong)aitaskMaster.SamplesToAcquire)
    {
        var masterData = new double[aitaskMaster.SamplesToAcquire, aitaskMaster.Channels.Count];
        var slaveData  = new double[aitaskMaster.SamplesToAcquire, aitaskSlave.Channels.Count];
        aitaskMaster.ReadData(ref masterData, -1);
        aitaskSlave.ReadData(ref slaveData, -1);
    }
}
```

---

## 示例 9：外部参考时钟（10 MHz 外部输入）

```csharp
var aiTask = new JY9802AITask(0);

// 切换到外部参考时钟
var device = JY9802Device.GetInstance(0);
device.ClockSource            = ClockSource.External;
device.ExternalClockFrequency = 10e6;       // 10 MHz 外部参考

aiTask.AddChannel(0);
aiTask.Mode       = AIMode.Continuous;
aiTask.SampleRate = 250e6;
aiTask.Start();
```

---

## 综合技巧

### Stop 后参数保留，可再次 Start

```csharp
aiTask.Stop();              // 停止，但通道/模式/触发配置保留
// 修改部分参数（如 SampleRate）
aiTask.SampleRate = 100e6;
aiTask.Start();             // 直接重新启动
```

> 仅在 Record 模式结束后建议 `aiTask.Channels.Clear()` 清除通道。

### 多通道按列排列到行排列的转置

```csharp
int channelCount       = aiTask.Channels.Count;  // 最多 2
int samplesPerChannel  = 10000;

double[,] readValue    = new double[samplesPerChannel, channelCount];  // [每通道点数, 通道数]
aiTask.ReadData(ref readValue, -1);

// 提取各通道
double[] ch0 = new double[samplesPerChannel];
double[] ch1 = new double[samplesPerChannel];
for (int i = 0; i < samplesPerChannel; i++)
{
    ch0[i] = readValue[i, 0];
    ch1[i] = readValue[i, 1];
}

// 转置后绘制多通道图
double[,] displayValue = new double[channelCount, samplesPerChannel];
ArrayManipulation.Transpose(readValue, ref displayValue);
easyChartX_readData.Plot(displayValue);
```

### Continuous 模式流控（防缓冲区溢出）

```csharp
// 在 ReadData 之前使用 WaitForDataReady 提高效率
aiTask.WaitForDataReady(blockSize: 10240, timeout: 1000);
aiTask.ReadData(ref readBuffer, -1);
```

### 窗体关闭资源释放

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer1 != null) timer1.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}
```

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)
