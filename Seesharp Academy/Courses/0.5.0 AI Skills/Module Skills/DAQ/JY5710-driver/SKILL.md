---
name: jy5710-driver
description: 提供 JYTEK JY5710 系列（PXIe/PCIe/USB-5710）模拟输出卡（AO）的完整 C# 驱动开发指引。涵盖模拟输出（AO）单点/有限/连续输出、多通道同步波形生成、触发配置（数字/软件触发）、时钟配置（内部/外部时钟）。当用户使用 JY5710、PXIe-5710、PCIe-5710、USB-5710、JY5710AOTask、AOMode.Single、AOMode.Finite、AOMode.ContinuousWrapping、AOMode.ContinuousNoWrapping 开发信号生成、波形输出、激励信号、自动化测试应用时自动应用。
---

# JY5710 驱动开发指引

## 适用硬件

- USB-5710：USB 接口模拟输出卡
- PCIe-5710：PCIe 接口模拟输出卡
- PXIe-5710：PXIe 接口模拟输出卡

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5710\Bin\JY5710.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY5710\Bin\JY5710.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY5710;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AO 分辨率 | 16-bit |
| AO 通道数 | 32 通道 |
| AO 最大更新率 | 8 通道：2 MS/s；32 通道：1 MS/s |
| AO 输出量程 | ±10V |
| AO 精度 | 0.02% |
| AO 驱动电流 | ±10 mA |
| AO 过载电流 | 15 mA |
| 触发方式 | 数字 / 软件触发 |
| 接口 | USB / PCIe / PXIe |

## 通用编程范式

### AO 输出任务流程

```
创建 AOTask → 添加通道 → 配置参数 → 写入数据 → 启动 → 停止
```

### 标准代码框架

```csharp
// 1. 创建 AO Task
JY5710AOTask aoTask = new JY5710AOTask(boardNumber);

// 2. 添加通道
aoTask.AddChannel(channelID);

// 3. 配置输出模式和时钟
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.SampleClock.Source = AOSampleClockSource.Internal;
aoTask.UpdateRate = 1000000;  // 1 MS/s

// 4. 写入波形数据
aoTask.WriteData(waveformData, -1);

// 5. 启动输出
aoTask.Start();

// 6. 停止
aoTask.Stop();
aoTask.Channels.Clear();
```

## 关键 API 速查

### AO 通道配置

| API | 说明 |
|-----|------|
| `aoTask.AddChannel(channel)` | 添加模拟输出通道（量程固定±10V） |
| `aoTask.Channels.Count` | 已添加通道数 |
| `aoTask.Channels[i].ChannelID` | 获取通道 ID |
| `aoTask.WriteData(data, timeout)` | 写入输出数据 |

### 时钟配置

| API | 说明 |
|-----|------|
| `aoTask.SampleClock.Source` | 时钟源（Internal/External） |
| `aoTask.UpdateRate` | 更新率（Hz，内部时钟时有效） |
| `aoTask.SampleClock.External.Terminal` | 外部时钟终端 |
| `aoTask.SampleClock.External.ExpectedRate` | 期望的外部时钟频率 |

### 输出模式

| 模式 | 说明 |
|------|------|
| `AOMode.Single` | AO 单点输出模式 |
| `AOMode.Finite` | AO 有限输出模式 |
| `AOMode.ContinuousWrapping` | AO 连续循环输出模式 |
| `AOMode.ContinuousNoWrapping` | AO 连续不循环输出模式 |

### 触发配置

```csharp
// 数字触发
aoTask.Trigger.Type = AOTriggerType.Digital;
aoTask.Trigger.Digital.Source = AODigitalTriggerSource.PFI0;
aoTask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

// 软触发
aoTask.Trigger.Type = AOTriggerType.Soft;
```

### 多卡同步

```csharp
// 主卡配置
masterTask.SampleClock.Source = AOSampleClockSource.Internal;
masterTask.UpdateRate = 1000000;
masterTask.Trigger.Type = AOTriggerType.Immediate;

// 导出时钟和触发信号
masterTask.SignalExport.Add(AOSignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AOSignalExportSource.StartTrig, SignalExportDestination.PXI_Trig1);

// 从卡配置
slaveTask.SampleClock.Source = AOSampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 1000000;
slaveTask.Trigger.Type = AOTriggerType.Digital;
slaveTask.Trigger.Digital.Source = AODigitalTriggerSource.PXI_Trig1;
slaveTask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

// 写入数据
masterTask.WriteData(masterWave, -1);
slaveTask.WriteData(slaveWave, -1);

// 启动（从卡先启动）
slaveTask.Start();
masterTask.Start();
```

## 关键枚举

### AOMode（AO 输出模式）

- `AOMode.Single` - 单点输出模式
- `AOMode.Finite` - 有限输出模式
- `AOMode.ContinuousWrapping` - 连续循环输出模式
- `AOMode.ContinuousNoWrapping` - 连续不循环输出模式

### AOSampleClockSource（采样时钟源）

- `AOSampleClockSource.Internal` - 内部时钟
- `AOSampleClockSource.External` - 外部时钟

### ClockTerminal（时钟终端）

- `ClockTerminal.PXI_Trig0` ~ `PXI_Trig7` - PXI 触发总线
- `ClockTerminal.PFI0` ~ `PFI2` - 前面板接口

### AOSignalExportSource（信号导出源）

- `AOSignalExportSource.SampleClock` - 采样时钟
- `AOSignalExportSource.StartTrig` - 启动触发信号

### SignalExportDestination（信号导出目标）

- `SignalExportDestination.PXI_Trig0` ~ `PXI_Trig7` - PXI 触发总线
- `SignalExportDestination.PFI0` ~ `PFI2` - 前面板接口

### AODigitalTriggerSource（数字触发源）

- `AODigitalTriggerSource.PFI0` ~ `PFI2` - 前面板接口
- `AODigitalTriggerSource.PXI_Trig0` ~ `PXI_Trig7` - PXI 触发总线

### AODigitalTriggerEdge（数字触发边沿）

- `AODigitalTriggerEdge.Rising` - 上升沿触发
- `AODigitalTriggerEdge.Falling` - 下降沿触发

### AOTriggerType（触发类型）

- `AOTriggerType.Immediate` - 立即触发（无触发）
- `AOTriggerType.Digital` - 数字触发
- `AOTriggerType.Soft` - 软触发

## 设备属性

```csharp
// 获取设备信息（需要先创建设备实例）
JY5710Device device = new JY5710Device(boardNumber);
int aoChannels = device.AOChannelCount;        // AO 通道总数
double boardClock = device.BoardClockRate;     // 板卡时钟频率
string serialNum = device.SerialNumber;        // 序列号

// 使用完毕后释放
device.Release();
```

---

## 异常处理

所有操作可能抛出异常，应使用 try-catch 处理：

```csharp
try
{
    aoTask.Start();
}
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);
}
```

## 开发环境要求

- .NET Framework 4.0 或更高版本
- 驱动版本：JY5710 Installer_V1.0.0 或更高
- 依赖包：SeeSharpTools.JY.GUI 1.4.7


---

# 完整 API 参考

# JY5710 API 参考手册

## 命名空间

```csharp
using JY5710;
```

## 核心类

### JY5710AOTask

模拟输出任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY5710AOTask(int boardNumber)` | 创建指定板卡的 AO 任务（通过槽位号） |
| `JY5710AOTask(string boardName)` | 创建指定板卡的 AO 任务（通过设备别名，由设备管理器设置） |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `AOChannelCollection` | 通道集合 |
| `Mode` | `AOMode` | 输出模式（Single/Finite/ContinuousWrapping/ContinuousNoWrapping） |
| `UpdateRate` | `double` | 更新率（Hz） |
| `SamplesToUpdate` | `int` | 每通道输出样本数（仅Finite模式有效） |
| `BufLenInSamples` | `int` | 缓冲区每通道可存储的样本数 |
| `AvaliableLenInSamples` | `int` | AO缓冲区中每通道可写入的样本数 |
| `TransferedSamples` | `int` | 已从本地缓冲区传输的样本数（每通道），Non-Single模式下有效 |
| `CompleteState` | - | 任务完成状态 |
| `Trigger` | `AOTrigger` | 触发配置 |
| `SampleClock` | `AOSampleClock` | 采样时钟配置 |
| `SignalExport` | `AOSignalExport` | 信号导出配置 |
| `DisableCalibration` | - | 禁用校准 |
| `Device` | `JY5710Device` | 获取关联的设备对象 |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int channel)` | 添加模拟输出通道（量程固定±10V），chnId=-1表示添加所有通道 |
| `AddChannel(int[] channels)` | 添加一组模拟输出通道 |
| `RemoveChannel(int channel)` | 移除通道 |
| `Start()` | 启动输出任务 |
| `Stop()` | 停止输出任务 |
| `WriteData(double[] data, int timeout)` | 写入输出数据（单通道） |
| `WriteData(double[,] data, int timeout)` | 写入输出数据（多通道，每列代表一个通道） |
| `WriteData(IntPtr buf, int samplesPerChannel, int timeout)` | 通过IntPtr指针写入数据 |
| `WriteRawData(short[] data, int timeout)` | 写入原始数据（单通道，16位） |
| `WriteRawData(short[,] data, int timeout)` | 写入原始数据（多通道，16位，每列代表一个通道） |
| `WriteRawData(IntPtr buf, int samplesPerChannel, int timeout)` | 通过IntPtr指针写入原始数据（16位） |
| `WriteSinglePoint(double[] writeValues)` | 向所有通道写入一个样本（仅Single模式） |
| `WriteSinglePoint(double writeValue, int channel)` | 向指定通道写入一个样本（仅Single模式） |
| `WriteRawSinglePoint(short[] writeValues)` | 向所有通道写入一个原始样本（仅Single模式） |
| `WriteRawSinglePoint(short writeValue, int channel)` | 向指定通道写入一个原始样本（仅Single模式） |
| `WaitUntilDone(int timeout)` | 等待任务完成（Finite模式），timeout=-1表示一直等待 |
| `SendSoftwareTrigger()` | 发送软件触发 |

---

## 设备硬件规格

| 参数 | 值 |
|------|-----|
| AO 分辨率 | 16-bit |
| AO 通道数 | 32 通道 |
| AO 最大更新率 | 8 通道：2 MS/s；32 通道：1 MS/s |
| AO 输出量程 | ±10V |
| AO 精度 | 0.02% |
| AO 驱动电流 | ±10 mA |
| AO 过载电流 | 15 mA |
| 触发方式 | 数字 / 软件 |
| 接口 | USB / PCIe / PXIe |

### 产品型号

| 型号 | 描述 |
|------|------|
| USB-5710 | 32 通道 16-Bit 2 MS/s USB 模拟输出模块 |
| PCIe-5710 | 32 通道 16-Bit 2 MS/s PCIe 模拟输出模块 |
| PXIe-5710 | 32 通道 16-Bit 2 MS/s PXIe 模拟输出模块 |

---

## 枚举类型

### AOMode

AO 输出模式枚举。

| 值 | 说明 |
|----|------|
| `Single` | 单点输出模式 |
| `Finite` | 有限输出模式 |
| `ContinuousWrapping` | 连续循环输出模式 |
| `ContinuousNoWrapping` | 连续不循环输出模式 |

### AOTriggerType

触发类型枚举。

| 值 | 说明 |
|----|------|
| `Immediate` | 立即触发（无触发） |
| `Digital` | 数字触发 |
| `Soft` | 软触发 |

### AODigitalTriggerSource

数字触发源枚举。

| 值 | 说明 |
|----|------|
| `PFI0` ~ `PFI2` | 前面板接口 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

### AODigitalTriggerEdge

数字触发边沿枚举。

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿触发 |
| `Falling` | 下降沿触发 |

### AOSampleClockSource

采样时钟源枚举。

| 值 | 说明 |
|----|------|
| `Internal` | 内部时钟 |
| `External` | 外部时钟 |

### ClockTerminal

时钟终端枚举。

| 值 | 说明 |
|----|------|
| `PFI0` ~ `PFI2` | 前面板接口 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

### AOSignalExportSource

信号导出源枚举。

| 值 | 说明 |
|----|------|
| `SampleClock` | 采样时钟 |
| `StartTrig` | 启动触发信号 |

### SignalExportDestination

信号导出目标枚举。

| 值 | 说明 |
|----|------|
| `PFI0` ~ `PFI2` | 前面板接口 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

### ReferenceClockSource

参考时钟源枚举。

| 值 | 说明 |
|----|------|
| `Internal` | 内部板载25MHz时钟 |
| `External` | 外部参考时钟源 |

### ExternalReferenceClockTerminal

外部参考时钟终端枚举。

| 值 | 说明 |
|----|------|
| `PXIe_Clk100` | PXIe 100MHz时钟 |

---

## 设备类

### JY5710Device

设备信息类（需要实例化）。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY5710Device(ushort boardNumber)` | 创建设备实例（通过槽位号） |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `AOChannelCount` | `int` | AO 通道总数 |
| `BoardClockRate` | `double` | 板卡时钟频率 |
| `SerialNumber` | `string` | 设备序列号 |
| `DeviceID` | `ushort` | 设备 ID |
| `ReferenceClock` | `ReferenceClock` | 参考时钟配置 |
| `Handle` | `IntPtr` | 设备句柄 |

| 方法 | 说明 |
|------|------|
| `Release()` | 关闭设备并释放资源 |

---

## 配置对象

### AOTrigger

触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AOTriggerType` | 触发类型（Immediate/Digital/Soft），默认值为Immediate |
| `Digital` | `AODigitalTrigger` | 数字触发配置，当Type为Digital时有效 |

### AODigitalTrigger

数字触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AODigitalTriggerSource` | 数字触发源（PFI0~PFI2, PXI_Trig0~PXI_Trig7） |
| `Edge` | `AODigitalTriggerEdge` | 触发边沿（Rising/Falling） |

### AOSampleClock

采样时钟配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AOSampleClockSource` | 时钟源（Internal/External） |
| `External` | `AOExternalSampleClock` | 外部时钟配置 |

### AOExternalSampleClock

外部采样时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ClockTerminal` | 时钟终端 |
| `ExpectedRate` | `double` | 期望的时钟频率（Hz） |

### AOSignalExport

信号导出配置对象。

| 方法 | 说明 |
|------|------|
| `Add(AOSignalExportSource source, SignalExportDestination destination)` | 添加信号导出路由（一个信号到一个目标） |
| `Add(AOSignalExportSource source, List<SignalExportDestination> destinations)` | 添加信号导出路由（一个信号到多个目标） |
| `Clear(SignalExportDestination destination)` | 清除指定目标的信号导出配置 |
| `ClearAll()` | 清除所有信号导出配置 |

### ReferenceClock

参考时钟配置对象（通过JY5710Device.ReferenceClock访问）。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `ReferenceClockSource` | 参考时钟源（Internal/External），默认值为Internal |
| `External` | `ExternalReferenceClock` | 外部参考时钟配置，当Source为External时有效 |
| `IsCommitAllowed` | `bool` | 当前是否允许提交参考时钟配置（设备空闲时为true） |

| 方法 | 说明 |
|------|------|
| `Commit()` | 提交参考时钟配置（仅在设备空闲时可用） |

### ExternalReferenceClock

外部参考时钟配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ExternalReferenceClockTerminal` | 外部参考时钟输入终端（PXIe_Clk100） |

---

## 异常类

### JYDriverException

驱动异常类，所有 API 操作失败时抛出。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Message` | `string` | 错误描述信息 |


---

# 完整代码示例

# JY5710 代码示例

## 1. AO 连续循环输出（正弦波）

```csharp
using System;
using System.Windows.Forms;
using JY5710;
using SeeSharpTools.JY.DSP.Fundamental;

namespace JY5710Example
{
    public partial class MainForm : Form
    {
        private JY5710AOTask aoTask;
        private double[] writeValue;

        private void button_start_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建 AO Task
                aoTask = new JY5710AOTask(0);

                // 添加通道
                aoTask.AddChannel(0);

                // 配置输出模式和时钟
                aoTask.Mode = AOMode.ContinuousWrapping;
                aoTask.SampleClock.Source = AOSampleClockSource.Internal;
                aoTask.UpdateRate = 1000000;  // 1 MS/s

                // 生成正弦波
                writeValue = new double[100000];  // 100ms 数据
                double amplitude = 5.0;
                double frequency = 1000.0;
                Generation.SineWave(ref writeValue, amplitude, 0, frequency, aoTask.UpdateRate);

                // 写入数据
                aoTask.WriteData(writeValue, -1);

                // 启动输出
                aoTask.Start();
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            if (aoTask != null)
            {
                aoTask.Stop();
                aoTask.Channels.Clear();
            }
        }
    }
}
```

## 2. AO 有限输出（方波）

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY5710AOTask(0);
        aoTask.AddChannel(0);

        aoTask.Mode = AOMode.Finite;
        aoTask.SampleClock.Source = AOSampleClockSource.Internal;
        aoTask.UpdateRate = 1000000;

        // 生成方波
        writeValue = new double[1000000];
        double amplitude = 5.0;
        double frequency = 1000.0;
        Generation.SquareWave(ref writeValue, amplitude, 50, frequency, aoTask.UpdateRate);

        aoTask.WriteData(writeValue, -1);
        aoTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 3. AO 多通道输出

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY5710AOTask(0);
        aoTask.Mode = AOMode.ContinuousWrapping;
        aoTask.SampleClock.Source = AOSampleClockSource.Internal;
        aoTask.UpdateRate = 1000000;

        // 添加四个通道
        aoTask.AddChannel(0);
        aoTask.AddChannel(1);
        aoTask.AddChannel(2);
        aoTask.AddChannel(3);

        // 生成四路不同频率的正弦波
        double[] ch0Data = new double[100000];
        double[] ch1Data = new double[100000];
        double[] ch2Data = new double[100000];
        double[] ch3Data = new double[100000];
        
        Generation.SineWave(ref ch0Data, 5.0, 0, 1000.0, aoTask.UpdateRate);
        Generation.SineWave(ref ch1Data, 5.0, 0, 2000.0, aoTask.UpdateRate);
        Generation.SineWave(ref ch2Data, 5.0, 0, 3000.0, aoTask.UpdateRate);
        Generation.SineWave(ref ch3Data, 5.0, 0, 4000.0, aoTask.UpdateRate);

        // 交错写入多通道数据（行存储：每行一个通道）
        double[,] interleavedData = new double[4, ch0Data.Length];
        for (int i = 0; i < ch0Data.Length; i++)
        {
            interleavedData[0, i] = ch0Data[i];
            interleavedData[1, i] = ch1Data[i];
            interleavedData[2, i] = ch2Data[i];
            interleavedData[3, i] = ch3Data[i];
        }

        aoTask.WriteData(interleavedData, -1);
        aoTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 4. AO 数字触发输出

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY5710AOTask(0);
        aoTask.AddChannel(0);

        aoTask.Mode = AOMode.ContinuousWrapping;
        aoTask.SampleClock.Source = AOSampleClockSource.Internal;
        aoTask.UpdateRate = 1000000;

        // 配置数字触发
        aoTask.Trigger.Type = AOTriggerType.Digital;
        aoTask.Trigger.Digital.Source = AODigitalTriggerSource.PFI0;
        aoTask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

        // 生成并写入波形
        writeValue = new double[100000];
        Generation.SineWave(ref writeValue, 5.0, 0, 1000.0, aoTask.UpdateRate);
        aoTask.WriteData(writeValue, -1);

        // 启动（等待触发）
        aoTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 5. AO 软触发输出

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY5710AOTask(0);
        aoTask.AddChannel(0);

        aoTask.Mode = AOMode.Finite;
        aoTask.SampleClock.Source = AOSampleClockSource.Internal;
        aoTask.UpdateRate = 1000000;

        // 配置软触发
        aoTask.Trigger.Type = AOTriggerType.Soft;

        // 生成并写入波形
        writeValue = new double[1000000];
        Generation.SineWave(ref writeValue, 5.0, 0, 1000.0, aoTask.UpdateRate);
        aoTask.WriteData(writeValue, -1);

        // 启动（等待软触发）
        aoTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void button_trigger_Click(object sender, EventArgs e)
{
    // 发送软触发
    aoTask.SendSoftwareTrigger();
}
```

## 6. 多卡同步输出

```csharp
private JY5710AOTask masterTask;
private JY5710AOTask slaveTask;

private void button_Commit_Click(object sender, EventArgs e)
{
    try
    {
        // 创建任务
        slaveTask = new JY5710AOTask(1);  // 从卡槽位 1
        masterTask = new JY5710AOTask(0); // 主卡槽位 0

        // 添加通道
        slaveTask.AddChannel(0);
        masterTask.AddChannel(0);

        // 主卡配置：使用内部时钟
        masterTask.Mode = AOMode.ContinuousWrapping;
        masterTask.SampleClock.Source = AOSampleClockSource.Internal;
        masterTask.UpdateRate = 1000000;
        masterTask.Trigger.Type = AOTriggerType.Immediate;
        
        // 导出时钟和触发信号到 PXI 触发总线
        masterTask.SignalExport.Add(AOSignalExportSource.SampleClock, 
            SignalExportDestination.PXI_Trig0);
        masterTask.SignalExport.Add(AOSignalExportSource.StartTrig,
            SignalExportDestination.PXI_Trig1);

        // 从卡配置：使用外部时钟和触发
        slaveTask.Mode = AOMode.ContinuousWrapping;
        slaveTask.SampleClock.Source = AOSampleClockSource.External;
        slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
        slaveTask.SampleClock.External.ExpectedRate = 1000000;
        
        slaveTask.Trigger.Type = AOTriggerType.Digital;
        slaveTask.Trigger.Digital.Source = AODigitalTriggerSource.PXI_Trig1;
        slaveTask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

        // 写入波形数据
        double[] masterWave = new double[100000];
        double[] slaveWave = new double[100000];
        Generation.SineWave(ref masterWave, 5.0, 0, 1000.0, masterTask.UpdateRate);
        Generation.SineWave(ref slaveWave, 5.0, 0, 1000.0, slaveTask.UpdateRate);
        
        masterTask.WriteData(masterWave, -1);
        slaveTask.WriteData(slaveWave, -1);
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        // 启动（从卡先启动）
        slaveTask.Start();
        masterTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 7. 使用设备属性

```csharp
private void MainForm_Load(object sender, EventArgs e)
{
    // 获取设备信息
    JY5710Device device = new JY5710Device(0);
    int totalChannels = device.AOChannelCount;
    double boardClock = device.BoardClockRate;
    device.Release();

    // 设置更新率范围
    numericUpDown_UpdateRate.Minimum = 1;
    numericUpDown_UpdateRate.Maximum = (decimal)boardClock;

    // 添加所有通道到选择列表
    for (int i = 0; i < totalChannels; i++)
    {
        comboBox_Channel.Items.Add(string.Format("Ch{0}", i));
    }
}
```

## 8. 生成多种波形

```csharp
private void GenerateWaveform(int waveformType, double amplitude, double frequency, double dutyCycle)
{
    writeValue = new double[100000];
    
    switch (waveformType)
    {
        case 0: // 正弦波
            Generation.SineWave(ref writeValue, amplitude, 0, frequency, aoTask.UpdateRate);
            break;
        case 1: // 方波
            Generation.SquareWave(ref writeValue, amplitude, dutyCycle, frequency, aoTask.UpdateRate);
            break;
        case 2: // 三角波
            Generation.TriangleWave(ref writeValue, amplitude, frequency, aoTask.UpdateRate);
            break;
        case 3: // 锯齿波
            Generation.SawtoothWave(ref writeValue, amplitude, frequency, aoTask.UpdateRate);
            break;
        case 4: // 直流
            for (int i = 0; i < writeValue.Length; i++)
                writeValue[i] = amplitude;
            break;
    }
}
```

## 9. 连续不循环输出（实时更新）

```csharp
private JY5710AOTask aoTask;
private bool isRunning = false;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aoTask = new JY5710AOTask(0);
        aoTask.AddChannel(0);
        
        // 配置为不循环模式，可以实时追加数据
        aoTask.Mode = AOMode.ContinuousNoWrapping;
        aoTask.SampleClock.Source = AOSampleClockSource.Internal;
        aoTask.UpdateRate = 1000000;
        
        // 先写入初始数据
        double[] initialData = new double[100000];
        Generation.SineWave(ref initialData, 5.0, 0, 1000.0, aoTask.UpdateRate);
        aoTask.WriteData(initialData, -1);
        
        aoTask.Start();
        isRunning = true;
        
        // 启动后台线程持续写入数据
        Task.Run(() => ContinuousWrite());
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void ContinuousWrite()
{
    while (isRunning)
    {
        // 生成新数据并写入
        double[] newData = new double[10000];
        // ... 生成数据 ...
        aoTask.WriteData(newData, 1000);
        Thread.Sleep(5);
    }
}

private void button_stop_Click(object sender, EventArgs e)
{
    isRunning = false;
    if (aoTask != null)
    {
        aoTask.Stop();
        aoTask.Channels.Clear();
    }
}
```

## 10. 窗体关闭资源释放

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (aoTask != null)
        {
            aoTask.Stop();
            aoTask.Channels.Clear();
        }
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```
