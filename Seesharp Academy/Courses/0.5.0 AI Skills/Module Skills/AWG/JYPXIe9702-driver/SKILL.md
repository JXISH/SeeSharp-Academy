---
name: jypxie9702-driver
description: 提供 JYTEK JYPXIe9702（PXIe-9702）高精度双通道任意波形发生器（AWG）的完整 C# 驱动开发指引。涵盖模拟输出（AO）有限波形（Finite）、连续循环波形（ContinuousWrapping）、连续非循环流式波形（ContinuousNoWrapping）、直接数字合成（DDS，Sine/Square/Triangle）、外部参考时钟（10M/PXIe_Clock100）、数字触发（PFI/PXI_Trig）、软件触发、信号导出、多卡同步（Master/Slave）、数字输入/输出（DI/DO）、FPGA/DAC InverseSinc 插值、从文件加载波形等功能。当用户使用 JYPXIe9702、PXIe-9702、JYPXIe9702AOTask、JYPXIe9702DITask、JYPXIe9702DOTask、AOMode.Finite、AOMode.ContinuousWrapping、AOMode.ContinuousNoWrapping、AOMode.DDS、DDSConfig、WaveformType、AOTriggerType、SyncTopology 开发任意波形生成、信号激励、ATE测试、超声波测量、瞬态信号仿真应用时自动应用。
---

# JYPXIe9702（PXIe-9702）任意波形发生器驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\AWG\JYPXIe9702\Bin\JYPXIe9702.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\AWG\JYPXIe9702\Bin\JYPXIe9702.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JYPXIe9702;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查（基于简仪产品型录_20260417.pdf，文档页81）

| 功能                  | 规格                                                             |
| --------------------- | ---------------------------------------------------------------- |
| 产品类型              | 双通道高精度任意波形发生器（AWG）                                |
| DC 精度               | 0.22%                                                            |
| AO 通道数             | 2 通道                                                           |
| DAC 分辨率            | 16-bit                                                           |
| 最大更新率（单通道）  | 250 MS/s                                                         |
| 最大更新率（双通道）  | 125 MS/s（每通道）                                               |
| 默认更新率            | 50 MS/s                                                          |
| 模拟带宽              | 100 MHz                                                          |
| 输出量程（50Ω 负载）  | ±0.5V / ±1.25V / ±5V                                             |
| 输出量程（高阻负载）  | ±1V / ±2.5V / ±10V                                               |
| 负载阻抗              | 50Ω / 高阻（软件可选）                                           |
| 输出阻抗              | 50Ω                                                              |
| 输出耦合              | DC                                                               |
| DC 增益误差           | ±0.05%                                                           |
| DC 偏移误差           | ±0.07%（满量程）                                                 |
| 板载缓存              | 512 MB DDR3                                                      |
| 总线接口              | PCI Express Gen2 ×4 Lane（PXIe）                                 |
| 输出连接器            | SMA                                                              |
| PFI 通道              | 8 路双向（Mini-HDMI，3.3V LVCMOS，50Ω，DC，最高 30 MHz）         |
| 参考时钟              | 内部 10 MHz TCXO / 外部（REF_IN / PXIe_Clock100）                |
| 触发类型              | Immediate（立即）/ Digital（PFI/PXI_Trig）/ Soft（软件）         |
| 多卡同步              | Master / Slave（通过 PXI_Trig 路由）                             |
| 工作温度              | 0 ~ 55 ℃                                                         |

> **量程选用提示**：`AddChannel` 的 `rangeLow/rangeHigh` 参数取值必须匹配硬件规格，取值 `±0.5`、`±1.25`、`±5`（50Ω 负载）或 `±1`、`±2.5`、`±10`（高阻负载）。驱动通过范围值识别负载阻抗，见官方范例 `comboBox_rangeLimit`。

## 通用编程范式

所有 Task 均遵循：**创建 Task → AddChannel → 配置 Mode/UpdateRate → 配置参考时钟/触发 → WriteData/WriteSinglePoint → Start() → 输出完成 → Stop() → Channels.Clear()**

```csharp
var aotask = new JYPXIe9702AOTask(0);        // 1. 创建（按槽位号）
aotask.AddChannel(0, -5, 5);                 // 2. 添加通道（±5V @ 50Ω）
aotask.Mode = AOMode.Finite;                 // 3. 配置模式
aotask.UpdateRate = 50e6;                    // 4. 配置更新率 50 MS/s
aotask.SamplesToUpdate = 10000;              // 5. Finite 模式：每通道输出点数

// 6. 参考时钟（可选）
aotask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

// 7. 写数据并启动
aotask.WriteData(writeBuffer, -1);
aotask.Start();

// 8. 等待完成（仅 Finite）
aotask.WaitUntilDone(-1);

// 9. 停止并清理
aotask.Stop();
aotask.Channels.Clear();
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输出（AO）

### 任务类：`JYPXIe9702AOTask`

#### 构造函数

```csharp
new JYPXIe9702AOTask(int slotNum)       // 按槽位号创建（推荐）
new JYPXIe9702AOTask(string boardName)  // 按设备管理器别名创建
```

#### 关键属性

| 属性                        | 类型                | 说明                                                                   |
| --------------------------- | ------------------- | ---------------------------------------------------------------------- |
| `Mode`                      | `AOMode`            | Finite / ContinuousWrapping / ContinuousNoWrapping / DDS               |
| `UpdateRate`                | `double`            | 每通道更新率（Sa/s）。单通道最高 250M，双通道最高 125M，默认 50M       |
| `SamplesToUpdate`           | `int`               | Finite 模式下每通道输出点数                                            |
| `BufLenInSamples`           | `int`               | 缓冲区可存储的点数（每通道），`SamplesToUpdate` 必须 ≤ 此值            |
| `AvaliableLenInSamples`     | `int`               | 缓冲区中可继续写入的点数（每通道）                                     |
| `GeneratedSampleCount`      | `long`              | 已生成输出的点数（每通道）                                             |
| `TransferedSamples`         | `long`              | 已传输到硬件的点数（每通道）                                           |
| `Trigger`                   | `AOTrigger`         | 触发配置对象                                                           |
| `SignalExport`              | `AOSignalExport`    | 信号导出配置对象                                                       |
| `Sync`                      | `AISync`            | 多卡同步配置对象                                                       |
| `Advanced`                  | `AOAdvanced`        | 高级配置（InverseSinc / 插值 / 基带模式）                              |
| `Device`                    | `JYPXIe9702Device`  | 设备对象（含参考时钟 `Device.ReferenceClock`）                         |
| `Channels`                  | `List<AOChannel>`   | 已添加的通道列表（最多 2 个，元素属性：`ChannelID`/`RangeLow`/`RangeHigh`/`Gain`/`Delay`） |
| `DisableCalibration`        | `bool`              | 禁用出厂校准（默认 false）                                             |

#### AddChannel 重载

```csharp
// 单通道
void AddChannel(int chnId, double rangeLow, double rangeHigh, double delay = 0)

// 批量通道（统一量程）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh, double delay = 0)

// 批量通道（各自独立量程）
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh, double delay = 0)

// 参数说明
// chnId:     0 或 1（最多 2 通道，-1 表示添加全部通道）
// rangeLow:  -5 / -1.25 / -0.5（50Ω 负载）或 -10 / -2.5 / -1（高阻负载）
// rangeHigh:  5 /  1.25 /  0.5（50Ω 负载）或  10 /  2.5 /  1（高阻负载）
// delay:     AO 通道输出延迟（单位 ns），默认 0
```

#### 控制方法

```csharp
void Start()                             // 启动 AO Task
void Stop()                              // 停止 AO Task
bool WaitUntilDone(int timeout)          // 仅 Finite 有效，timeout=-1 永久等待
void SendSoftwareTrigger()               // 发送软触发（AOTriggerType.Soft）
void RemoveChannel(int chnId)            // 移除通道（-1 移除全部）
```

#### 写数据方法

```csharp
// 常规 double（电压值，自动按量程转换）
void WriteData(double[] buf, int timeout)                               // 单通道
void WriteData(double[,] buf, int timeout)                              // 多通道，按列排列
void WriteData(IntPtr buf, int samplesPerChannel, int timeout)          // 指针方式（大数据）

// 原始 16-bit 数据（直接写入 DAC 码值，性能最优）
void WriteRawData(short[] buf, int timeout)                             // 单通道
void WriteRawData(short[,] buf, int timeout)                            // 多通道，按列排列
void WriteRawData(IntPtr buf, int samplesPerChannel, int timeout = -1)  // 指针方式（最高性能）

// DDS 模式专用
void WriteSinglePoint(DDSConfig ddsConfig)                              // 单通道 DDS
void WriteSinglePoint(DDSConfig[] ddsConfigs)                           // 多通道 DDS（按通道顺序）

// timeout = -1 表示永久等待
```

**多通道数据排列**：使用 `double[,]` 或 `short[,]`，**每列代表一个通道**。写交织数据时按 `[sample][channel]` 顺序排列。

### AOMode 四种模式速查

| 模式                    | 特点                                                         | 典型用法                                                |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------- |
| `Finite`                | 输出 `SamplesToUpdate` 点数后停止                            | `WriteData → Start → WaitUntilDone → Stop`              |
| `ContinuousWrapping`    | 数据循环输出（Host 只写一次，硬件循环播放）                  | `WriteData → Start`（无需再写）                         |
| `ContinuousNoWrapping`  | 流式输出（持续写新数据，硬件流式输出，需轮询缓冲区可用空间） | `WriteData → Start → 轮询 AvaliableLenInSamples 再写`   |
| `DDS`                   | 直接数字合成，硬件生成 Sine/Square/Triangle 波形             | `WriteSinglePoint(DDSConfig[])` 配置即可                |

### AOTrigger — 触发配置

```csharp
aotask.Trigger.Type = AOTriggerType.Digital;      // Immediate / Digital / Soft
// 默认 Immediate（立即触发，Start 后立即开始输出）
```

#### 数字触发（Digital）

```csharp
aotask.Trigger.Type = AOTriggerType.Digital;
aotask.Trigger.Digital.Source = AODigitalTriggerSource.PFI0;     // PFI0~PFI7 / PXI_Trig0~PXI_Trig7 / TRIG_IN
aotask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;       // Rising / Falling / High / Low
```

#### 软件触发（Soft）

```csharp
aotask.Trigger.Type = AOTriggerType.Soft;
aotask.Start();
// 在需要时手动触发：
aotask.SendSoftwareTrigger();
```

### AOSignalExport — 信号导出

将任务内部信号（如 StartTrig）路由到前面板 PFI 或 PXI_Trig 线上，便于多卡同步或触发外设。

#### AOSignalExportSource 枚举（导出源）

| 值          | 说明                              |
| ----------- | --------------------------------- |
| `StartTrig` | AO 启动触发信号（当前仅此一项）   |

#### SignalExportDestination 枚举（目的地）

| 值             | 说明                       |
| -------------- | -------------------------- |
| `TRG`          | 板载 TRG 端子              |
| `PFI0` ~ `PFI7`| PFI_Out_0 ~ PFI_Out_7      |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 背板触发线 0~7 |


```csharp
// 单目的地
aotask.SignalExport.Add(AOSignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

// 多目的地
var destList = new List<SignalExportDestination>
{
    SignalExportDestination.PFI0,
    SignalExportDestination.PXI_Trig1
};
aotask.SignalExport.Add(AOSignalExportSource.StartTrig, destList);

// 清除全部
aotask.SignalExport.ClearAll();
```

### Device.ReferenceClock — 参考时钟

```csharp
// 内部 10 MHz TCXO（默认）
aotask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

// 外部参考时钟
aotask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aotask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clock100;  // REF_IN / PXIe_Clock100
aotask.Device.ReferenceClock.External.Frequency = 10e6;  // 期望外部参考频率
aotask.Device.ReferenceClock.Commit();  // 提交参考时钟配置（所有 Task 停止时才能提交）
```

### AOAdvanced — 高级配置

| 属性                          | 类型      | 说明                                                       |
| ----------------------------- | --------- | ---------------------------------------------------------- |
| `EnableFPGAInverseSinc`       | `bool`    | 启用 FPGA 反 Sinc 滤波（不使用 DAC DUC 时使用）            |
| `EnableDACInverseSinc`        | `bool`    | 启用 DAC 反 Sinc 滤波（使用 DAC DUC 时使用）               |
| `EnableInterpolation`         | `bool`    | 启用插值（DDS 模式推荐开启）                               |
| `EnableOverflowProtection`    | `bool`    | 启用 FPGA 溢出保护                                         |
| `EnableBasebandMode`          | `bool`    | 启用基带模式                                               |
| `CenterFrequency`             | `double`  | DAC DUC 中心频率                                           |

---

## DDS — 直接数字合成模式

`AOMode.DDS` 下由硬件直接合成标准波形，无需向缓冲区写大量采样点，仅用 `WriteSinglePoint(DDSConfig)` 配置参数：

```csharp
aotask.Mode = AOMode.DDS;
aotask.UpdateRate = 250e6;
aotask.Advanced.EnableInterpolation = true;  // DDS 模式推荐开启插值

// 单通道
var dds = new DDSConfig
{
    WaveformType = WaveformType.Sine,   // Sine / Square / Triangle
    Frequency = 1e6,                    // 1 MHz
    Amplitude = 2.0,                    // 幅度（V）
    Offset = 0,                         // 直流偏置
    DutyCycle = 50,                     // 占空比（Square/Triangle 有效，0~100）
    PhaseOffset = 0,                    // 初始相位
    PhaseCompare = 0                    // Sync 引脚高电平输出阈值
};
aotask.WriteSinglePoint(dds);

// 双通道
var ddsArr = new DDSConfig[]
{
    new DDSConfig { WaveformType = WaveformType.Sine,   Frequency = 1e6, Amplitude = 2.0 },
    new DDSConfig { WaveformType = WaveformType.Square, Frequency = 1e6, Amplitude = 2.0, DutyCycle = 50 }
};
aotask.WriteSinglePoint(ddsArr);

aotask.Start();
```

### WaveformType 枚举

| 值         | 说明                         |
| ---------- | ---------------------------- |
| `Sine`     | 正弦波                       |
| `Square`   | 方波（DutyCycle 有效）       |
| `Triangle` | 三角波（DutyCycle 有效）     |

---

## 多卡同步

PXIe-9702 支持基于 PXI 背板触发线的多卡同步。通过 `Sync.Topology` 区分主从，使用 `SyncTriggerRouting` 和 `SyncPulseRouting` 路由同步信号：

```csharp
// ===== 主卡（Slot 2）=====
var master = new JYPXIe9702AOTask(2);
master.AddChannel(0, -5, 5);
master.Mode = AOMode.ContinuousWrapping;
master.UpdateRate = 100e6;
master.Trigger.Type = AOTriggerType.Immediate;

// 外部参考时钟：PXIe 100M
master.Device.ReferenceClock.Source = ReferenceClockSource.External;
master.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clock100;
master.Device.ReferenceClock.External.Frequency = 10e6;
master.Device.ReferenceClock.Commit();

// 同步配置
master.Sync.Topology = SyncTopology.Master;
master.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;  // 路由触发信号
master.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;      // 路由同步脉冲信号

// ===== 从卡（Slot 4）=====
var slave = new JYPXIe9702AOTask(4);
slave.AddChannel(0, -5, 5);
slave.Mode = AOMode.ContinuousWrapping;
slave.UpdateRate = 100e6;

// 从卡接收主卡触发
slave.Trigger.Type = AOTriggerType.Digital;
slave.Trigger.Digital.Source = AODigitalTriggerSource.PXI_Trig0;
slave.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

slave.Device.ReferenceClock.Source = ReferenceClockSource.External;
slave.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clock100;
slave.Device.ReferenceClock.External.Frequency = 10e6;
slave.Device.ReferenceClock.Commit();

slave.Sync.Topology = SyncTopology.Slave;
slave.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
slave.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;

// ===== 写数据（略）=====
master.WriteData(masterBuffer, -1);
slave.WriteData(slaveBuffer, -1);

// ===== 提交同步配置：先从卡后主卡 =====
slave.Sync.Commit();
master.Sync.Commit();

// ===== 启动顺序：先从卡后主卡 =====
slave.Start();
master.Start();
```

### SyncTopology 枚举

| 值            | 说明                     |
| ------------- | ------------------------ |
| `Independent` | 独立，不参与同步（默认） |
| `Master`      | 主卡                     |
| `Slave`       | 从卡                     |

### 多卡同步要点

- 主从卡都需配置 `ReferenceClock.External.Terminal = PXIe_Clock100` 并调用 `Commit()`
- `TriggerRouting` 与 `PulseRouting` 必须在主从间一致
- `Sync.Commit()` 顺序：**先从后主**
- `Start()` 顺序：**先从后主**
- 主卡使用 `Immediate` 触发自动发出启动脉冲

---

## 数字输入（DI）

### 任务类：`JYPXIe9702DITask`

```csharp
var diTask = new JYPXIe9702DITask(0);        // 按槽位号创建
// 或 new JYPXIe9702DITask("DevAlias")      // 按别名创建

diTask.AddChannel(0);                         // 添加 DI 通道（lineNum，0-based）
// 或 diTask.AddChannel(new int[] { 0, 1 }); // 批量添加
diTask.Start();

// 单点读取（指定通道）
bool readValue = false;
diTask.ReadSinglePoint(ref readValue, 0);    // 读取 line 0 的值

// 单点读取（全部已添加通道）
bool[] buf = new bool[diTask.Channels.Count];
diTask.ReadSinglePoint(ref buf);

diTask.Stop();
diTask.Channels.Clear();
```

---

## 数字输出（DO）

### 任务类：`JYPXIe9702DOTask`

```csharp
var doTask = new JYPXIe9702DOTask(0);        // 按槽位号创建

doTask.AddChannel(0);                         // 添加 DO 通道（lineNum，0-based）
// 或 doTask.AddChannel(new int[] { 0, 1 });

// 单点输出（指定通道）
doTask.WriteSinglePoint(true, 0);            // line 0 输出高电平

// 单点输出（全部通道）
bool[] writeBuf = { true, false };
doTask.WriteSinglePoint(writeBuf);

doTask.Start();
// ... 期间可继续 WriteSinglePoint 更新输出 ...
doTask.Stop();
doTask.Channels.Clear();
```

---

## 常见错误处理

| 异常代码                                           | 原因                             | 处理建议                                     |
| -------------------------------------------------- | -------------------------------- | -------------------------------------------- |
| `OpenDeviceFailed`                                 | 板卡未连接或槽位号错误           | 检查设备管理器槽位号                         |
| `NoChannelAdded`                                   | 未调用 AddChannel 就 Start       | 在 Start 前添加通道                          |
| `ChannelInputRangeParameterInvalid`                | 量程值不在 ±0.5/±1.25/±5/±1/±2.5/±10 范围 | 按硬件规格表选择正确量程                     |
| `UpdateRateParameterInvalid`                       | UpdateRate 超过硬件上限          | 单通道 ≤ 250M，双通道 ≤ 125M                 |
| `BufferDataDownflow`                               | ContinuousNoWrapping 写入慢于输出 | 提高写入频率或减小 UpdateRate                |
| `WriteDataTimeout`                                 | 缓冲区满，timeout 内无法写入     | 增大 timeout 或检查消费端                    |
| `PointInvalidInFiniteMode`                         | Finite 模式 SamplesToUpdate 非法 | 检查 `SamplesToUpdate ≤ BufLenInSamples`     |
| `TriggerParameterInvalid`                          | 触发源/边沿配置错误              | 检查 `AODigitalTriggerSource` 枚举值         |
| `SignalExportDestinationInvalid`                   | 信号导出目的地不受支持           | 检查 `SignalExportDestination` 枚举值        |

---

# 完整代码示例

> 所有示例均来自 `JYPXIe-9702_V1.1.4_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AO Finite 单通道输出

```csharp
using System;
using System.Windows.Forms;
using JYPXIe9702;
using SeeSharpTools.JY.DSP.Fundamental;

public partial class MainForm : Form
{
    private JYPXIe9702AOTask aotask;

    private void Start_Click(object sender, EventArgs e)
    {
        try
        {
            // 1. 创建 AO Task
            aotask = new JYPXIe9702AOTask(0);

            // 2. 添加通道（±5V，50Ω 负载）
            aotask.AddChannel(0, -5, 5);

            // 3. 配置 Finite 模式
            aotask.Mode = AOMode.Finite;
            aotask.UpdateRate = 50e6;        // 50 MS/s
            aotask.SamplesToUpdate = 10000;  // 每通道输出 10000 点

            // 4. 配置内部参考时钟（默认）
            aotask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

            // 5. 生成波形并写入缓冲区
            double[] writeValue = new double[aotask.SamplesToUpdate];
            Generation.SineWave(ref writeValue, 2.0, 0, 1e6, aotask.UpdateRate);  // 2Vpp 1MHz 正弦波
            aotask.WriteData(writeValue, -1);

            // 6. 启动输出
            aotask.Start();
            easyChartX_waveform.Plot(writeValue);

            // 7. 使用 Timer 轮询完成状态
            timer_FetchData.Enabled = true;
        }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    }

    private void timer_FetchData_Tick(object sender, EventArgs e)
    {
        // 检查输出是否完成
        if (aotask.WaitUntilDone(10))
        {
            Stop_Click(null, null);
        }
    }

    private void Stop_Click(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        aotask?.Stop();
        aotask?.Channels.Clear();
    }
}
```

---

## 示例 2：AO ContinuousNoWrapping 流式输出（WriteRawData + IntPtr）

流式输出需后台线程持续写入新数据，硬件流式消费：

```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JYPXIe9702;

private JYPXIe9702AOTask aotask;
private CancellationTokenSource cancellation;
private IntPtr dataStartPtr = IntPtr.Zero;
private IntPtr writeBufPtr = IntPtr.Zero;
private int writeSamples;
private Task writeDataTask;

private void Start_Click(object sender, EventArgs e)
{
    aotask = new JYPXIe9702AOTask(0);
    aotask.AddChannel(0, -5, 5);

    aotask.Mode = AOMode.ContinuousNoWrapping;  // 流式输出
    aotask.UpdateRate = 100e6;

    // 参考时钟
    aotask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

    // 生成初始波形（short[] 原始数据）
    short[] writeValue;
    InitWriteBuffer(5.0, out writeValue);    // 自行实现：填充 16bit 数据并 AllocHGlobal
    writeSamples = writeValue.Length;

    // 首次写入
    aotask.WriteRawData(dataStartPtr, writeSamples);

    // 后台持续写任务
    cancellation = new CancellationTokenSource();
    writeDataTask = new Task(DoWriteData, cancellation.Token);

    aotask.Start();
    writeDataTask.Start();
}

private void DoWriteData()
{
    try
    {
        while (!cancellation.IsCancellationRequested)
        {
            // 等待缓冲区有足够空间
            if (aotask.AvaliableLenInSamples < writeSamples)
            {
                Thread.Sleep(1);
                continue;
            }
            aotask.WriteRawData(dataStartPtr, writeSamples, 10000);
        }
    }
    catch (JYDriverException ex)
    {
        Invoke(new Action(() => { Stop_Click(null, null); MessageBox.Show(ex.Message); }));
    }
}

private void Stop_Click(object sender, EventArgs e)
{
    if (cancellation != null && !cancellation.IsCancellationRequested)
    {
        cancellation.Cancel();
        writeDataTask.Wait(1000);
        cancellation.Dispose();
        cancellation = null;
        aotask.Stop();
        aotask.Channels.Clear();
    }
    ReleaseWriteBuffer();  // 释放 AllocHGlobal
}
```

**性能提示**：大数据量流式输出建议使用 `WriteRawData(IntPtr, ...)` 配合 `Marshal.AllocHGlobal`，并按 16 字节对齐 `dataStartPtr`，以获得最佳传输性能。

---

## 示例 3：AO ContinuousWrapping 多通道循环输出

数据写一次，硬件循环输出，CPU 占用低：

```csharp
aotask = new JYPXIe9702AOTask(0);

// 两通道不同量程
aotask.AddChannel(0, -5, 5);      // CH0: ±5V
aotask.AddChannel(1, -1.25, 1.25);// CH1: ±1.25V

aotask.Mode = AOMode.ContinuousWrapping;
aotask.UpdateRate = 100e6;

int samplesPerChannel = 10000;
int channelCount = aotask.Channels.Count;

// 交织排列：writeValue[i*2]=CH0, writeValue[i*2+1]=CH1
double[] writeValue = new double[samplesPerChannel * channelCount];
double[] ch0 = new double[samplesPerChannel];
double[] ch1 = new double[samplesPerChannel];

Generation.SineWave(ref ch0, 4.0, 0, 1e6, aotask.UpdateRate);
Generation.SquareWave(ref ch1, 1.0, 50, 1e6, aotask.UpdateRate);

for (int j = 0; j < samplesPerChannel; j++)
{
    writeValue[j * 2]     = ch0[j];
    writeValue[j * 2 + 1] = ch1[j];
}

aotask.WriteData(writeValue, -1);
aotask.Start();

// 硬件持续循环播放，无需继续写入数据
// 停止时调用 Stop + Channels.Clear()
```

---

## 示例 4：AO DDS 硬件直接合成波形

```csharp
aotask = new JYPXIe9702AOTask(0);

// 双通道
aotask.AddChannel(0, -5, 5);
aotask.AddChannel(1, -5, 5);

aotask.Mode = AOMode.DDS;
aotask.UpdateRate = 250e6;
aotask.Advanced.EnableInterpolation = true;  // DDS 推荐开启插值

aotask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

// 配置两个通道的 DDS 参数
var ddsConfigs = new DDSConfig[]
{
    new DDSConfig { WaveformType = WaveformType.Sine,   Frequency = 1e6, Amplitude = 2.0 },
    new DDSConfig { WaveformType = WaveformType.Square, Frequency = 1e6, Amplitude = 2.0, DutyCycle = 50 }
};

aotask.WriteSinglePoint(ddsConfigs);
aotask.Start();
```

---

## 示例 5：AO Finite 数字触发

```csharp
aotask = new JYPXIe9702AOTask(0);
aotask.AddChannel(0, -5, 5, 1);  // 1 ns 延迟

aotask.Mode = AOMode.Finite;
aotask.UpdateRate = 50e6;
aotask.SamplesToUpdate = 10000;

// 外部参考时钟
aotask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aotask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clock100;
aotask.Device.ReferenceClock.External.Frequency = 10e6;

// 数字触发：PFI0 上升沿
aotask.Trigger.Type = AOTriggerType.Digital;
aotask.Trigger.Digital.Source = AODigitalTriggerSource.PFI0;
aotask.Trigger.Digital.Edge = AODigitalTriggerEdge.Rising;

double[] writeValue = new double[aotask.SamplesToUpdate];
Generation.SineWave(ref writeValue, 2.0, 0, 1e6, aotask.UpdateRate);

aotask.WriteData(writeValue, -1);
aotask.Start();  // 等待 PFI0 上升沿到来后自动开始输出
```

---

## 示例 6：AO Finite 软件触发

```csharp
aotask = new JYPXIe9702AOTask(0);
aotask.AddChannel(0, -5, 5);
aotask.Mode = AOMode.Finite;
aotask.UpdateRate = 50e6;
aotask.SamplesToUpdate = 10000;

// 软件触发
aotask.Trigger.Type = AOTriggerType.Soft;

double[] writeValue = new double[aotask.SamplesToUpdate];
Generation.SineWave(ref writeValue, 2.0, 0, 1e6, aotask.UpdateRate);

aotask.WriteData(writeValue, -1);
aotask.Start();        // Start 后等待软触发

// 手动触发
aotask.SendSoftwareTrigger();
```

---

## 示例 7：AO 信号导出（Export StartTrig 到 PXI_Trig0）

```csharp
aotask = new JYPXIe9702AOTask(0);
aotask.AddChannel(0, -5, 5);
aotask.Mode = AOMode.ContinuousWrapping;
aotask.UpdateRate = 100e6;

// 将启动触发信号导出到 PXI_Trig0，供其他板卡同步使用
aotask.SignalExport.Add(AOSignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

// 同时导出到 PFI0
var destList = new List<SignalExportDestination>
{
    SignalExportDestination.PFI0,
    SignalExportDestination.PXI_Trig0
};
aotask.SignalExport.Add(AOSignalExportSource.StartTrig, destList);

aotask.WriteData(writeBuffer, -1);
aotask.Start();
```

---

## 示例 8：DI / DO 单点 I/O

```csharp
using JYPXIe9702;

private JYPXIe9702DITask diTask;
private JYPXIe9702DOTask doTask;
private int diLineID = 1;
private int doLineID = 0;

private void Start_Click(object sender, EventArgs e)
{
    doTask = new JYPXIe9702DOTask(0);
    diTask = new JYPXIe9702DITask(0);

    doTask.AddChannel(doLineID);  // DO line 0
    diTask.AddChannel(diLineID);  // DI line 1

    // 先写输出值
    doTask.WriteSinglePoint(true, doLineID);  // line 0 输出高电平

    doTask.Start();
    diTask.Start();

    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    bool readValue = false;
    diTask.ReadSinglePoint(ref readValue, diLineID);
    led.Value = readValue;
    timer_FetchData.Enabled = true;
}

private void Switch_ValueChanged(object sender, EventArgs e)
{
    // 实时改变 DO 输出
    doTask.WriteSinglePoint(industrySwitch.Value, doLineID);
}

private void Stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    doTask?.Channels.Clear();
    doTask?.Stop();
    diTask?.Channels.Clear();
    diTask?.Stop();
}
```

---

## 综合技巧

### 量程选择建议

```csharp
// 50Ω 匹配负载（射频/示波器场景）
aotask.AddChannel(0, -5, 5);      // ±5V @ 50Ω
aotask.AddChannel(0, -1.25, 1.25);// ±1.25V @ 50Ω
aotask.AddChannel(0, -0.5, 0.5);  // ±0.5V @ 50Ω

// 高阻负载（高压输出场景）
aotask.AddChannel(0, -10, 10);    // ±10V @ 高阻
aotask.AddChannel(0, -2.5, 2.5);  // ±2.5V @ 高阻
aotask.AddChannel(0, -1, 1);      // ±1V @ 高阻
```

### 多通道数据交织写入

JYPXIe9702 多通道 `WriteData` 支持两种格式：

```csharp
// 方式 1：二维数组（推荐，按列排列）
double[,] buf2D = new double[samplesPerChannel, channelCount];
for (int i = 0; i < samplesPerChannel; i++)
{
    buf2D[i, 0] = ch0Data[i];   // 第 0 列 = 通道 0
    buf2D[i, 1] = ch1Data[i];   // 第 1 列 = 通道 1
}
aotask.WriteData(buf2D, -1);

// 方式 2：一维交织数组（[sample0_ch0, sample0_ch1, sample1_ch0, sample1_ch1, ...]）
double[] buf1D = new double[samplesPerChannel * channelCount];
for (int j = 0; j < samplesPerChannel; j++)
{
    buf1D[j * 2]     = ch0Data[j];
    buf1D[j * 2 + 1] = ch1Data[j];
}
aotask.WriteData(buf1D, -1);
```

### 实际更新率读取

配置 `UpdateRate` 后，硬件会选择最接近的可实现值，通过再读取该属性获取实际生效值：

```csharp
aotask.UpdateRate = 123.456e6;   // 设定期望值
aotask.Start();
double actualRate = aotask.UpdateRate;  // 读取实际生效值
```

### 窗体关闭资源释放

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (cancellation != null && !cancellation.IsCancellationRequested)
        {
            cancellation.Cancel();
            writeDataTask?.Wait(1000);
        }
        aotask?.Stop();
        aotask?.Channels.Clear();
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    finally { ReleaseWriteBuffer(); }
}
```

---

## 更多详情

- **驱动 DLL/XML**：`C:\SeeSharp\JYTEK\Hardware\AWG\JYPXIe9702\Bin\`
- **官方范例**：`D:\JYTEK_Work\Examples\C#\AWG\JYPXIe-9702_V1.1.4_Examples\`
  - **AO Finite**：`Analog Output/Winform AO Finite/`
  - **AO Finite 触发**：`Analog Output/Winform AO Finite Digital Trigger/`、`Winform AO Finite Soft Trigger/`
  - **AO Finite 多通道**：`Analog Output/Winform AO Finite MultiChannel/` 及其触发版本
  - **AO ContinuousNoWrapping**：`Analog Output/Winform AO Continuous NoWrapping/` 及其多通道/触发版本
  - **AO ContinuousWrapping**：`Analog Output/Winform AO Continuous Wrapping/` 及其多通道/触发版本
  - **AO DDS 标准波形**：`Analog Output/Winform AO Standard Waveform generation/`
  - **AO 从文件加载波形**：`Analog Output/Winform AO Output Wave From File/`
  - **DI 单点**：`Digital Input/Winform DI SinglePoint/`
  - **DO 单点**：`Digital Output/Winform DO SinglePoint/`
  - **DIO 单点**：`DIO/Winform DIO Single/`
