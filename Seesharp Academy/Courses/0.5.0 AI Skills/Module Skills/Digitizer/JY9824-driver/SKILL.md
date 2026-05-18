---
name: jy9824-driver
description: 提供 JYTEK JY9824/PXIe-9824 高速数字化仪（Digitizer）的完整 C# 驱动开发指引。涵盖高速模拟输入（AI）有限/连续/录制采集、多通道同步采集、触发配置（数字/模拟/软件触发、StartTrigger/ReferenceTrigger/ReTrigger）、参考时钟同步、多卡同步（<100 ps精度）、DC耦合、50Ω输入阻抗、1 Vpp/2 Vpp量程切换、3.5 GB DDR4缓存、录制模式（Finite/Infinite Streaming）。当用户使用 JY9824、PXIe-9824、JY9824AITask、AIMode.Finite、AIMode.Continuous、AIMode.Record、SyncTopology.Master、SyncTopology.Slave 开发高速数据采集、数字化仪、波形记录、瞬态信号捕捉应用时自动应用。
---

# JY9824 高速数字化仪驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9824\Bin\JY9824.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9824\Bin\JY9824.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9824;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查（基于简仪产品型录_20260331.pdf）

| 功能 | 规格 |
|------|------|
| AI 分辨率 | 16-bit |
| AI 通道数 | 4 通道同步采样 |
| AI 最大采样率 | 1 GS/s（每通道） |
| AI 采样率范围 | 69.4 kS/s ~ 1 GS/s |
| AI 输入量程 | 1 Vpp / 2 Vpp（软件可选） |
| AI 输入阻抗 | 50Ω（固定） |
| AI 输入耦合 | DC（固定） |
| AI 输入模式 | RSE（单端） |
| AI 模拟带宽 | DC ~ 200 MHz（-3 dB） |
| AI DC 精度 | 0.16% 满量程 |
| 同步精度 | <100 ps |
| 板载缓存 | 3.5 GB DDR4 |
| 串扰 (@1 MHz) | -70 dB |
| 触发类型 | 模拟 / 数字 / 软件 |
| 触发模式 | StartTrigger / ReferenceTrigger / ReTrigger |
| 工作温度 | 0℃ ~ 50℃ |
| 接口 | PXIe |

### 动态性能（50Ω阻抗，1 Vpp量程）

| 输入频率 | SFDR | SNR | SINAD | THD | ENOB |
|---------|------|-----|-------|-----|------|
| 10 MHz | 78.27 dB | 63.98 dB | 63.89 dB | 80.73 dB | 10.49 bits |
| 20 MHz | 79.60 dB | 65.58 dB | 65.43 dB | 80.09 dB | 10.75 bits |

### 动态性能（50Ω阻抗，2 Vpp量程）

| 输入频率 | SFDR | SNR | SINAD | THD | ENOB |
|---------|------|-----|-------|-----|------|
| 10 MHz | 76.57 dB | 66.27 dB | 66.12 dB | 80.77 dB | 10.87 bits |
| 20 MHz | 74.49 dB | 66.48 dB | 66.31 dB | 80.32 dB | 10.89 bits |

### DC 精度规格

| 标称量程 (V) | 24小时精度 Tcal±1°C | 24小时满量程精度 |
|-------------|---------------------|-----------------|
| 0.5 | 0.050% Reading + 0.120% Range (820 μV, 1640 ppm) |
| 1 | 0.070% Reading + 0.090% Range (1600 μV, 1600 ppm) |

## 通用编程范式

所有 JY9824 采集任务遵循以下标准流程：

```
创建 Task → 添加通道 → 配置参数 → 启动 → 读取数据 → 停止 → 清除通道
```

### 标准代码框架

```csharp
// 1. 创建 Task
JY9824AITask aiTask = new JY9824AITask(boardNumber);

// 2. 添加通道（DataFormat=Real时）
// 1 Vpp量程：rangeLow=-0.5, rangeHigh=0.5
// 2 Vpp量程：rangeLow=-1, rangeHigh=1
aiTask.AddChannel(channelID, -1, 1);  // 2 Vpp 量程

// 3. 配置采集模式
aiTask.Mode = AIMode.Continuous;  // 或 Finite / Record
aiTask.SampleRate = 1000000000;    // 1 GS/s 最高采样率

// 4. 启动采集
aiTask.Start();

// 5. 读取数据
// 注意：JY9824的ReadData没有samplesToRead参数，自动读取所有可用数据

// 单通道：一维数组
double[] singleBuffer = new double[samplesToRead];
aiTask.ReadData(ref singleBuffer, timeout);

// 多通道：二维数组 [每通道点数, 通道数]，每列一个通道
double[,] multiBuffer = new double[samplesToRead, aiTask.Channels.Count];
aiTask.ReadData(ref multiBuffer, timeout);

// 6. 停止并清理
aiTask.Stop();
aiTask.Channels.Clear();
```

## 关键 API 速查

### 通道配置

| API | 说明 |
|-----|------|
| `aiTask.AddChannel(channel, rangeLow, rangeHigh)` | 添加模拟输入通道（Real模式） |
| `aiTask.AddChannel(channel, centerFrequency, digitalGain, rangeLow, rangeHigh)` | 添加模拟输入通道（Complex模式） |
| `aiTask.Channels.Clear()` | 清除已添加的通道 |

### 采集模式

| 模式 | 说明 |
|------|------|
| `AIMode.Continuous` | 连续采集模式 |
| `AIMode.Finite` | 有限采集模式 |
| `AIMode.Record` | 记录模式（流式采集到板载内存） |

### 触发配置

```csharp
// 数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig_0;  // 注意：带下划线
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

// 模拟触发
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.RisingEdge;
aiTask.Trigger.Analog.Level = 0.5;

// 软触发
aiTask.Trigger.Type = AITriggerType.Software;  // 注意：是Software不是Soft

// 触发模式
aiTask.Trigger.Mode = AITriggerMode.Start;      // 开始触发
aiTask.Trigger.Mode = AITriggerMode.Reference;  // 参考触发（支持预触发）
```

### 参考时钟同步（多卡同步）

**重要**：JY9824的多卡同步使用`SignalExport`而非`Sync`对象。主卡导出触发信号，从卡接收触发信号。

```csharp
// 主卡配置：导出开始触发信号到PXI_Trig0
masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
masterTask.Device.ReferenceClock.External.Frequency = 100e6;
masterTask.Device.ReferenceClock.Commit();

// 从卡配置：从PXI_Trig0接收触发
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig_0;  // 注意：带下划线
slaveTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slaveTask.Device.ReferenceClock.Commit();

// 先启动从卡，再启动主卡
slaveTask.Start();
masterTask.Start();
```

## 关键枚举

### AIRange（输入量程）

JY9824 通过 `rangeLow` 和 `rangeHigh` 参数设置量程：

| 量程 | rangeLow | rangeHigh |
|------|----------|-----------|
| 1 Vpp | -0.5 | 0.5 |
| 2 Vpp | -1 | 1 |

**注意**：JY9824 仅支持 DC 耦合、50Ω 输入阻抗、RSE 单端输入模式

### AITriggerType（触发类型）

- `AITriggerType.Immediate` - 立即触发
- `AITriggerType.Digital` - 数字触发
- `AITriggerType.Analog` - 模拟触发
- `AITriggerType.Software` - 软触发（注意：是Software不是Soft）

### AIDigitalTriggerSource（数字触发源）

- `AIDigitalTriggerSource.PFI0`
- `AIDigitalTriggerSource.PXI_Trig_0` ~ `PXI_Trig_7`（注意：带下划线）

### AISignalExportSource（信号导出源）

- `AISignalExportSource.None`
- `AISignalExportSource.PFI0`
- `AISignalExportSource.ATrig` - 面板TriggerIn
- `AISignalExportSource.StartTrig` - AI_StartTrig
- `AISignalExportSource.ReferenceTrig` - AI_ReferenceTrig
- `AISignalExportSource.PXI_Trig0` ~ `PXI_Trig7`

### SignalExportDestination（信号导出目标）

- `SignalExportDestination.None` - 无
- `SignalExportDestination.PXI_Trig0` ~ `PXI_Trig7` - PXI触发总线
- `SignalExportDestination.PFI0` - 前面板PFI0端子

---

## 录制模式（Record）

JY9824 支持将采集数据流式写入板载内存的 Record 模式，3.5 GB DDR4 缓存支持长时间高速采集：

```csharp
aiTask = new JY9824AITask(0);
aiTask.AddChannel(0, -1, 1);  // 2 Vpp 量程 (rangeLow=-1, rangeHigh=1)
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 1000000000;  // 1 GS/s 全速采集

// 配置录制参数（具体属性请参考驱动版本）
aiTask.Start();

// 录制过程中可读取数据（注意：JY9824没有samplesToRead参数）
aiTask.ReadData(ref buffer, timeout);

aiTask.Stop();
aiTask.Channels.Clear();
```

### Record 模式特点

- 数据直接写入板载内存，支持高速采集
- 适用于需要长时间连续采集的场景
- 可与触发配置配合使用

---

## 异常处理

所有操作可能抛出 `JYCommon.JYDriverException`，应使用 try-catch 处理：

```csharp
try
{
    aiTask.Start();
}
catch (JYCommon.JYDriverException ex)
{
    // 检查异常类型
    if (ex.ExceptionName == JYCommon.JYDriverExceptionPublic.TimeOut)
    {
        // 超时异常
    }
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 枚举

- `UnKnown` - 未知错误
- `InitializeFailed` - 初始化失败
- `TimeOut` - 超时
- `ErrorParam` - 参数错误
- `IncorrectCallOrder` - 调用顺序错误
- `CannotCall` - 无法调用
- `UserBufferError` - 用户缓冲区错误
- `BufferOverflow` - 缓冲区溢出
- `BufferDownflow` - 缓冲区下溢

## 开发环境要求

- .NET Framework 4.0 或更高版本
- 驱动版本：V1.0.0 或更高
- 依赖包：SeeSharpTools.JY.GUI 1.4.7


---

# 完整 API 参考

# JY9824 API 参考手册

## 命名空间

```csharp
using JY9824;
```

## 核心类

### JY9824AITask

高速数字化仪模拟输入任务类。

#### 构造函数

| 构造函数 | 说明 |
|----------|------|
| `JY9824AITask(int boardNumber)` | 创建指定板卡的 AI 任务 |

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channels` | `AIChannelCollection` | 通道集合 |
| `Mode` | `AIMode` | 采集模式（Continuous/Finite/Record） |
| `SampleRate` | `double` | 采样率（Hz） |
| `SamplesToAcquire` | `uint` | 有限模式下要采集的样本数（注意：是uint不是ulong） |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的样本数 |
| `Trigger` | `AITrigger` | 触发配置 |
| `SignalExport` | `AISignalExport` | 信号导出配置（用于多卡同步） |
| `Device` | `AIDevice` | 设备配置（参考时钟等） |
| `Record` | `AIRecord` | 录制配置对象 |

#### 方法

| 方法 | 说明 |
|------|------|
| `AddChannel(int channel, double rangeLow, double rangeHigh)` | 添加模拟输入通道（Real模式） |
| `AddChannel(int channel, double centerFrequency, double digitalGain, double rangeLow, double rangeHigh)` | 添加模拟输入通道（Complex模式） |
| `Start()` | 启动采集任务 |
| `Stop()` | 停止采集任务 |
| `ReadData(ref double[] buffer, int timeout)` | 读取单通道数据（一维数组，注意：没有samplesToRead参数） |
| `ReadData(ref double[,] buffer, int timeout)` | 读取多通道数据（二维数组，按列排列，注意：没有samplesToRead参数） |
| `ReadRawData(ref short[,] buffer, int timeout)` | 读取多通道原始数据（short格式，注意：没有samplesToRead参数） |
| `GetRecordPreviewData(ref double[,] buf, int timeout)` | 预览录制数据（多通道，注意：没有samples参数） |
| `GetRecordPreviewData(ref double[] buf, int samples, int timeout)` | 预览录制数据（单通道） |
| `GetRecordStatus(out double recordedLength, out bool recordDone)` | 获取录制状态（秒） |

---

## 设备硬件规格

| 参数 | 值 |
|------|-----|
| AI 分辨率 | 16-bit |
| AI 通道数 | 4 通道同步采样 |
| AI 最大采样率 | 1 GS/s（每通道） |
| AI 采样率范围 | 69.4 kS/s ~ 1 GS/s |
| AI 输入量程 | 1 Vpp / 2 Vpp（软件可选） |
| AI 输入阻抗 | 50Ω（固定） |
| AI 输入耦合 | DC（固定） |
| AI 模拟带宽 | DC ~ 200 MHz（-3 dB） |
| AI DC 精度 | 0.16% 满量程 |
| 同步精度 | <100 ps |
| 板载缓存 | 3.5 GB DDR4 |
| 接口 | PXIe |

### 产品型号

| 型号 | 描述 |
|------|------|
| PXIe-9824 | 4-CH, 1 GS/s, 16-bit, 2 Vpp, BW: 200 MHz PXIe Digitizer |

---

## 枚举类型

### AIMode

采集模式枚举。

| 值 | 说明 |
|----|------|
| `Continuous` | 连续采集模式 |
| `Finite` | 有限采集模式 |
| `Record` | 记录模式（流式采集到板载内存） |

### AIRange

输入量程通过参数设置：

| rangeLow | rangeHigh | 量程 |
|----------|-----------|------|
| -0.5 | 0.5 | 1 Vpp |
| -1 | 1 | 2 Vpp |

**注意**：JY9824 仅支持 DC 耦合、50Ω 输入阻抗，无需配置耦合方式和阻抗参数

### AITriggerType

触发类型枚举。

| 值 | 说明 |
|----|------|
| `Immediate` | 立即触发（无触发） |
| `Digital` | 数字触发 |
| `Analog` | 模拟触发 |
| `Software` | 软触发（注意：是Software不是Soft） |

### AITriggerMode

触发模式枚举。

| 值 | 说明 |
|----|------|
| `Start` | 开始触发 |
| `Reference` | 参考触发（支持预触发采集） |

### AIDigitalTriggerSource

数字触发源枚举。

| 值 | 说明 |
|----|------|
| `PFI0` | 前面板PFI0接口 |
| `PXI_Trig_0` ~ `PXI_Trig_7` | PXI触发总线（注意：带下划线） |

### AIDigitalTriggerEdge

数字触发边沿枚举。

| 值 | 说明 |
|----|------|
| `Rising` | 上升沿触发 |
| `Falling` | 下降沿触发 |

### AIAnalogTriggerSource

模拟触发源枚举。

| 值 | 说明 |
|----|------|
| `Channel_0` ~ `Channel_3` | 模拟输入通道 0~3 |

### AIAnalogTriggerCondition

模拟触发条件枚举。

| 值 | 说明 |
|----|------|
| `RisingEdge` | 上升沿触发 |
| `FallingEdge` | 下降沿触发 |
| `EnteringWindow` | 进入窗口触发 |
| `LeavingWindow` | 离开窗口触发 |

### SyncTopology

同步拓扑枚举。

| 值 | 说明 |
|----|------|
| `Independent` | 独立模式（不支持同步） |
| `Master` | 主卡 |
| `Slave` | 从卡 |

### SyncTriggerRouting

同步触发路由枚举。

| 值 | 说明 |
|----|------|
| `NONE` | 无路由 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

### SyncPulseRouting

同步脉冲路由枚举。

| 值 | 说明 |
|----|------|
| `NONE` | 无路由 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 |

### ReferenceClockSource

参考时钟源枚举。

| 值 | 说明 |
|----|------|
| `Internal` | 内部时钟 |
| `External` | 外部时钟 |

### ExternalReferenceClockTerminal

外部参考时钟终端枚举。

| 值 | 说明 |
|----|------|
| `CLK_IN` | 前面板 CLK_IN（10MHz） |
| `PXIe_Clk100` | PXIe 100MHz 背板时钟 |

---

## 异常类

### JYCommon.JYDriverException

驱动异常类，所有 API 操作失败时抛出。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Message` | `string` | 错误描述信息 |
| `ExceptionName` | `JYCommon.JYDriverExceptionPublic` | 异常类型枚举 |

---

## 配置对象

### AITrigger

触发配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Type` | `AITriggerType` | 触发类型 |
| `Mode` | `AITriggerMode` | 触发模式 |
| `PreTriggerSamples` | `int` | 预触发样本数（Reference 模式） |
| `Digital` | `AIDigitalTrigger` | 数字触发配置 |
| `Analog` | `AIAnalogTrigger` | 模拟触发配置 |

### AIDigitalTrigger

数字触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIDigitalTriggerSource` | 触发源 |
| `Edge` | `AIDigitalTriggerEdge` | 触发边沿 |

### AIAnalogTrigger

模拟触发配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `AIAnalogTriggerSource` | 触发源 |
| `Condition` | `AIAnalogTriggerCondition` | 触发条件 |
| `Level` | `double` | 触发电平 |

### AISignalExport

信号导出配置对象（用于多卡同步）。

| 方法 | 说明 |
|------|------|
| `Add(AISignalExportSource source, SignalExportDestination destination)` | 添加一路信号路由 |
| `Clear(SignalExportDestination destination)` | 删除一路信号路由 |
| `ClearAll()` | 删除全部信号路由 |

### AISignalExportSource

信号导出源枚举。

| 值 | 说明 |
|----|------|
| `None` | 无 |
| `PFI0` | PFI0 |
| `ATrig` | 面板TriggerIn |
| `StartTrig` | AI_StartTrig |
| `ReferenceTrig` | AI_ReferenceTrig |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI触发总线 |

### SignalExportDestination

信号导出目标枚举。

| 值 | 说明 |
|----|------|
| `None` | 无 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI触发总线 |
| `PFI0` | 前面板PFI0端子 |

### AIDevice

设备配置对象。

| 属性 | 类型 | 说明 |
|------|------|------|
| `ReferenceClock` | `AIReferenceClock` | 参考时钟配置 |

### AIReferenceClock

参考时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Source` | `ReferenceClockSource` | 时钟源 |
| `External` | `ExternalReferenceClock` | 外部时钟配置 |

| 方法 | 说明 |
|------|------|
| `Commit()` | 提交参考时钟配置 |

### ExternalReferenceClock

外部参考时钟配置。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Terminal` | `ExternalReferenceClockTerminal` | 时钟终端 |
| `Frequency` | `double` | 时钟频率（Hz） |

---

### AIRecord

录制配置对象。

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;
aiTask.Record.Mode = RecordMode.Finite;  // Finite / Infinite
aiTask.Record.Length = 5.0;               // 录制时长（秒），Finite 有效
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），Finite 模式有效 |
| `Advanced` | `AIRecordAdvanced` | 高级录制配置 |

---

### RecordMode

录制模式枚举。

| 值 | 说明 |
|----|------|
| `Finite` | 有限录制，录制指定时长后自动停止 |
| `Infinite` | 无限录制，需手动调用 Stop 停止 |

---

### FileFormat

文件格式枚举。

| 值 | 说明 |
|----|------|
| `Bin` | 二进制格式（short 类型，需缩放转换） |


---

# 完整代码示例

# JY9824 代码示例

## 1. AI 连续采集（单通道）

```csharp
using System;
using System.Windows.Forms;
using JY9824;

namespace JY9824Example
{
    public partial class MainForm : Form
    {
        private JY9824AITask aiTask;
        private double[] readValue;
        private double lowRange = -10;
        private double highRange = 10;

        private void button_start_Click(object sender, EventArgs e)
        {
            try
            {
                // 创建 Task
                aiTask = new JY9824AITask(0);  // 板卡 0

                // 添加通道（2 Vpp 量程）
                aiTask.AddChannel(0, -1, 1);  // rangeLow=-1, rangeHigh=1

                // 配置采集参数
                aiTask.Mode = AIMode.Continuous;
                aiTask.SampleRate = 1000000000;  // 1 GS/s

                // 启动采集
                aiTask.Start();

                readValue = new double[10000];
                timer_FetchData.Enabled = true;
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void timer_FetchData_Tick(object sender, EventArgs e)
        {
            timer_FetchData.Enabled = false;
            try
            {
                if (aiTask.AvailableSamples >= (ulong)readValue.Length)
                {
                    aiTask.ReadData(ref readValue, -1);  // 注意：没有samplesToRead参数
                    // 处理数据...
                }
            }
            catch (JYCommon.JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
            }
            timer_FetchData.Enabled = true;
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            try
            {
                if (aiTask != null)
                {
                    aiTask.Stop();
                    aiTask.Channels.Clear();
                }
            }
            catch (JYCommon.JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
```

## 2. AI 有限采集（多通道）

```csharp
private JY9824AITask aiTask;
private double[,] readValue;      // 多通道数据：[每通道点数, 通道数]
private double[,] displayValue;   // 转置后的数据用于显示

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        // 创建 Task
        aiTask = new JY9824AITask(0);

        // 添加多个通道（全部使用 2 Vpp 量程）
        for (int i = 0; i < 4; i++)
        {
            aiTask.AddChannel(i, -1, 1);  // 2 Vpp: rangeLow=-1, rangeHigh=1
        }

        // 配置有限采集
        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;  // 每通道采集 10000 个点
        aiTask.SampleRate = 1000000000;   // 1 GS/s

        // 启动采集
        aiTask.Start();

        // 创建缓冲区：[每通道点数, 通道数]
        readValue = new double[10000, aiTask.Channels.Count];
        displayValue = new double[aiTask.Channels.Count, 10000];  // 转置后用于绘图
        
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_Tick(object sender, EventArgs e)
{
    try
    {
        if (aiTask.AvailableSamples >= aiTask.SamplesToAcquire)
        {
            // 读取多通道数据到二维数组
            aiTask.ReadData(ref readValue, -1);  // 注意：没有samplesToRead参数
            
            // 转置数据用于显示（图表控件需要 [通道数, 每通道点数] 格式）
            ArrayManipulation.Transpose(readValue, ref displayValue);
            easyChartX_readData.Plot(displayValue);
            
            // 或提取单个通道数据
            double[] ch0Data = new double[10000];
            for (int i = 0; i < 10000; i++)
            {
                ch0Data[i] = readValue[i, 0];  // 第0列 = 通道0
            }
            
            aiTask.Stop();
            aiTask.Channels.Clear();
            
            timer_FetchData.Enabled = false;
        }
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 3. AI 数字触发采集

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        aiTask.AddChannel(0, -1, 1);  // 2 Vpp 量程

        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 1000000000;  // 1 GS/s

        // 配置数字触发
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig_0;  // 注意：带下划线
        aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 4. AI 模拟触发采集

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        aiTask.AddChannel(0, -1, 1);  // 2 Vpp 量程

        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;
        aiTask.SampleRate = 1000000000;  // 1 GS/s

        // 配置模拟触发（触发电平相对于量程，2Vpp量程下0.5 = 0.5V）
        aiTask.Trigger.Type = AITriggerType.Analog;
        aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
        aiTask.Trigger.Analog.Condition = AIAnalogTriggerCondition.RisingEdge;
        aiTask.Trigger.Analog.Level = 0.5;  // 触发电平 0.5V

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 5. AI 软触发采集

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        aiTask.AddChannel(0, -1, 1);  // 2 Vpp 量程

        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;
        aiTask.SampleRate = 1000000000;  // 1 GS/s

        // 配置软触发
        aiTask.Trigger.Type = AITriggerType.Software;  // 注意：是Software不是Soft

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 6. 参考触发（预触发采集）

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        aiTask.AddChannel(0, -1, 1);  // 2 Vpp 量程

        aiTask.Mode = AIMode.Finite;
        aiTask.SamplesToAcquire = 10000;
        aiTask.SampleRate = 1000000000;  // 1 GS/s

        // 配置参考触发（支持预触发）
        aiTask.Trigger.Type = AITriggerType.Digital;
        aiTask.Trigger.Mode = AITriggerMode.Reference;
        aiTask.Trigger.PreTriggerSamples = 1000;  // 触发前采集 1000 个点
        aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig_0;  // 注意：带下划线

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 7. 多卡参考时钟同步

**重要**：JY9824使用SignalExport进行多卡同步，而非Sync对象。

```csharp
private JY9824AITask masterTask;
private JY9824AITask slaveTask;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        // 主卡配置
        masterTask = new JY9824AITask(2);  // 主卡槽位 2
        masterTask.AddChannel(0, -1, 1);  // 2 Vpp 量程
        masterTask.Mode = AIMode.Finite;
        masterTask.SamplesToAcquire = 10000;
        masterTask.SampleRate = 1000000000;  // 1 GS/s
        masterTask.Trigger.Type = AITriggerType.Immediate;

        // 同步配置 - 主卡：导出触发信号
        masterTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);
        masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
        masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
        masterTask.Device.ReferenceClock.External.Frequency = 100e6;
        masterTask.Device.ReferenceClock.Commit();
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show("主卡配置失败: " + ex.Message);
        return;
    }

    try
    {
        // 从卡配置
        slaveTask = new JY9824AITask(4);  // 从卡槽位 4
        slaveTask.AddChannel(0, -1, 1);  // 2 Vpp 量程
        slaveTask.Mode = AIMode.Finite;
        slaveTask.SamplesToAcquire = 10000;
        slaveTask.SampleRate = 1000000000;  // 1 GS/s

        // 从卡使用数字触发，从PXI_Trig0接收
        slaveTask.Trigger.Type = AITriggerType.Digital;
        slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig_0;  // 注意：带下划线
        slaveTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

        // 从卡参考时钟配置
        slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
        slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
        slaveTask.Device.ReferenceClock.Commit();
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show("从卡配置失败: " + ex.Message);
        return;
    }

    try
    {
        // 先启动从卡，再启动主卡
        slaveTask.Start();
        masterTask.Start();
    }
    catch (JYCommon.JYDriverException ex)
    {
        MessageBox.Show("启动失败: " + ex.Message);
    }
}
```

## 8. 高频信号采集（200MHz带宽）

```csharp
private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        
        // JY9824 支持 DC ~ 200 MHz 带宽，50Ω 阻抗匹配
        // 适合采集高速瞬态信号
        aiTask.AddChannel(0, -0.5, 0.5);  // 1 Vpp 量程适合小信号

        aiTask.Mode = AIMode.Continuous;
        aiTask.SampleRate = 1000000000;  // 1 GS/s 全速采集

        aiTask.Start();
        readValue = new double[10000];
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

## 9. Record 有限录制模式（Finite Streaming）

JY9824 支持将数据流式录制到文件，录制完成后自动停止。定时器中通过`GetRecordStatus`检查录制状态：

```csharp
private JY9824AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        
        // 添加通道（2 Vpp 量程）
        aiTask.AddChannel(0, -1, 1);  // rangeLow=-1, rangeHigh=1
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 1000000000;  // 1 GS/s 全速录制
        
        // 配置录制参数（3.5 GB DDR4 缓存支持长时间录制）
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 1.0;               // 录制 1 秒（约 4GB 数据）
        aiTask.SamplesToAcquire = 100000;         // 预览缓冲区样本数
        
        aiTask.Start();
        
        previewData = new double[10000, aiTask.Channels.Count];
        timer_Preview.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    // 检查录制状态
    double recordedLength;
    bool recordDone;
    aiTask.GetRecordStatus(out recordedLength, out recordDone);
    
    // 录制完成，自动停止
    if (recordDone)
    {
        try
        {
            if (aiTask != null) aiTask.Stop();
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
        
        aiTask.Channels.Clear();
        timer_Preview.Enabled = false;
        MessageBox.Show("录制完成！");
    }
    else
    {
        // 录制中，预览数据
        if (aiTask.AvailableSamples >= 10000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
            // 显示预览数据...
        }
    }
    
    timer_Preview.Enabled = true;
}
```

---

## 10. Record 无限录制模式（Infinite Streaming）

持续录制数据，需要手动调用Stop停止。定时器中只读取预览数据，不检查录制状态：

```csharp
private JY9824AITask aiTask;
private double[,] previewData;

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9824AITask(0);
        
        // 添加通道（2 Vpp 量程）
        aiTask.AddChannel(0, -1, 1);  // rangeLow=-1, rangeHigh=1
        
        // 配置 Record 模式
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 1000000000;  // 1 GS/s 全速录制
        
        // 配置无限录制（3.5 GB DDR4 缓存支持）
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制模式
        
        aiTask.Start();
        
        previewData = new double[10000, aiTask.Channels.Count];
        timer_Preview.Enabled = true;
        
        button_Start.Enabled = false;
        button_Stop.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    // 无限模式下只读取预览数据，不检查录制状态
    if (aiTask.AvailableSamples >= 10000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
        // 显示预览数据...
    }
    
    timer_Preview.Enabled = true;
}

private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    try
    {
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
    
    button_Start.Enabled = true;
    button_Stop.Enabled = false;
}
```

---

## 11. 录制数据回放

读取已录制的 bin 文件并回放显示（JY9824 数据格式为 short）：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private short[,] rawData;
private double[,] playbackData;
private ulong totalSamples;
private double scaleValue = 21.0;  // 根据量程设置

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    
    if (!File.Exists(fileBrowser.FileName))
    {
        MessageBox.Show("文件不存在");
        return;
    }
    
    // 计算总样本数（JY9824 数据格式为 short）
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 1;
    totalSamples = (ulong)(fileInfo.Length / sizeof(short) / channelCount);
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    rawData = new short[10000, channelCount];
    playbackData = new double[10000, channelCount];
    
    button_OpenFile.Enabled = false;
    button_Playback.Enabled = true;
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        for (int i = 0; i < rawData.GetLength(0); i++)
        {
            for (int ch = 0; ch < rawData.GetLength(1); ch++)
            {
                if (playbackStream.Position < playbackStream.Length)
                {
                    rawData[i, ch] = playbackReader.ReadInt16();
                    playbackData[i, ch] = rawData[i, ch] * scaleValue / 32768.0;
                }
            }
        }
        // easyChartX1.Plot(playbackData);
    }
    catch (EndOfStreamException)
    {
        timer_Playback.Enabled = false;
        playbackReader.Close();
        playbackStream.Close();
        MessageBox.Show("回放完成");
    }
}
```
