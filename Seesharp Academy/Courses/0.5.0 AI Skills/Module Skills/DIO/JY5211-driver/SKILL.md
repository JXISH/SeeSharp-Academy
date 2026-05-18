---
name: JY5211 Driver Skill
description: 简仪 JY5211 系列 8 通道计数器/定时器 + 40 路 PFI/DIO 板卡 C# 驱动使用规范。当用户需要使用 JY5211（PCIe-5211 / PXIe-5211）进行边沿计数 / 频率 / 周期 / 脉冲 / 半周期 / 双边沿间隔 / 正交编码 / 双脉冲编码测量，或进行单 / 有限 / 连续脉冲输出、静态 DI/DO、多卡同步时必须加载本技能。
---

# JY5211 驱动使用指南

> 本文档覆盖 JY5211 系列板卡的 C# 驱动（命名空间 `JY5211`）。该系列为**纯计数器 + 数字 IO** 板卡，**不含 AI/AO 模拟通道**。

## 1. 环境要求

| 项 | 说明 |
|---|---|
| 驱动 DLL | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY5211\Bin\JY5211.dll` |
| XML 文档 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY5211\Bin\JY5211.XML` |
| 范例代码 | `D:\JYTEK_Work\Examples\C#\DAQ\JY5211_V4.2.0_Examples` |
| .NET Framework | 4.0 及以上 |
| 命名空间 | `using JY5211;` |
| 可选 GUI 套件 | `SeeSharpTools.JY.GUI`（范例中使用的图表/LED 控件） |
| 异常类型 | `JY5211.JYDriverException`（所有驱动层错误都以此抛出） |

## 2. 硬件规格速查

> 数据来源：简仪产品型录 PCIe/PXIe-5211 章节 + 驱动 XML。

### 2.1 计数器 / 定时器（8 通道 0~7）

| 项 | 规格 |
|---|---|
| 通道数 | 8（Counter 0 ~ Counter 7） |
| 输入模式 | Edge Counting / Frequency / Period / SemiPeriod / Pulse / Two-Edge Separation / Quadrature Encoder / Two-Pulse Encoder |
| 输出模式 | Single / Finite / Continuous Pulse（Wrapping / NoWrapping） |
| Edge Counting / Frequency 最大源频率 | 50 MHz |
| Minimum Pulse Width（Period 测量） | 15 ns |
| Minimum Pulse Width（Pulse / Two-Edge Sep 测量） | 20 ns |
| 内部时基 | 100 MHz / 5 MHz / 100 kHz（TCXO，精度 2 ppm） |
| 外部时基范围 | 0 – 50 MHz |
| 最大采样时钟 | 10 MHz（使用 100 MHz 内部或 50 MHz 外部时基） |
| 输入端子 | Gate(Z) / Source(A) / Aux(B) / Digital Trigger / External Sample Clock（均可从 PFI0~PFI39 任选） |
| 输出端子 | OUT（映射到指定 PFI） |
| FIFO | 15 M Samples/通道 |

### 2.2 数字 IO（40 路）

| 项 | 规格 |
|---|---|
| 通道数 | 40（`PFI0 ~ PFI39`，与计数器端子共用） |
| Port 宽度 | 32 位（低 32 根可作并行端口） |
| UserVcc 可编程电平 | 1.8 / 2.5 / 3.3 / 5 V |
| 输入 VIH | 1.2 / 1.7 / 2 / 3.5 V（随 UserVcc） |
| 输入 VIL | 0.65 / 0.7 / 0.8 / 1.5 V |
| 输出电流 | ±4 / ±8 / ±24 / ±32 mA |

### 2.3 总线与时钟

| 项 | 规格 |
|---|---|
| PCIe 总线 | PCIe x1 / x2 / x4（PCIe-5211） |
| PXIe 背板触发 | PXI_Trig0 ~ PXI_Trig7、PXI_Star、PXIe_DStarA |
| 参考时钟源 | 内部 10 MHz TCXO / 外部 PXIe_Clk100（PXIe 版本）/ PXIe_DStarA |
| 附件 | DIN-68（SCSI 68 pin 接线端子）、ACL-1016868-1（1 m 68 pin VHDC-SCSI 双绞线） |

## 3. 产品型号

| 型号 | 总线 |
|---|---|
| PCIe-5211 | PCIe x1 |
| PXIe-5211 | PXI Express |

构造器接受 **槽位号**（从 0 开始，按系统枚举顺序）或 **板卡别名**（在 JYTEK Measurement & Automation Explorer 中配置）。

## 4. 通用编程范式

JY5211 驱动按 **功能类型** 划分 Task：`JY5211CITask`（计数器输入）、`JY5211COTask`（计数器输出）、`JY5211DITask`（数字输入）、`JY5211DOTask`（数字输出）。

生命周期：

```
new TaskXxx(slot/name, channel)
    → 配置属性 (Type / Mode / SampleClock / Trigger / Sync …)
    → Start()
    → ReadXxx / WriteXxx / ReadData / WriteData
    → Stop()
```

**关键规则**

1. **CI / CO 是通道级 Task**：构造时必须指定 `channel (0~7)`，每个 Task 只管 1 个计数器通道。如需同时用多个计数器通道（如同步测量），须为每个通道各建一个 Task。
2. **DI / DO 是板卡级 Task**：构造时只传 slot/name；用 `AddLine(IOTerminal)` / `AddLine(IOTerminal[])` 添加需要的 PFI 管脚。
3. **属性修改必须在 `Start()` 之前完成**。Start 之后修改属性不生效，部分还会抛异常。
4. **CI 的测量配置是分离的配置类**：例如 `ciTask.EdgeCounting.InitialCount`、`ciTask.FrequencyMeas.Timebase`、`ciTask.PulseMeas.Timebase`。需要先设置 `ciTask.Type = CIType.Xxx`，再操作对应配置类。仅 `Type` 对应的那一个配置类有效。
5. **CI 的 `Mode` 决定读取方式**：
   - `CIMode.Single`：只能 `ReadSinglePoint(...)`
   - `CIMode.Finite` / `CIMode.Continuous`：使用 `ReadData(...)`，可读 `AvailableSamples`
6. **所有端子枚举统一用 `IOTerminal` / `ClockTerminal` / `DigitalTriggerSource` / `SignalExportDestination`**，不同场合使用不同枚举（详见 §11）。
7. **异常捕获**：所有调用均可能抛 `JYDriverException`，典型错误码见 §10。

### 4.1 Task 构造模板

```csharp
// 按槽位号
var ciTask = new JY5211CITask(slotNumber: 0, channel: 0);

// 按板卡别名（在 MAX 中配置的 BoardName）
var ciTask2 = new JY5211CITask("JY5211_0", 0);
```

## 5. 数字输入 DI

### 5.1 任务构造与端口添加

```csharp
using JY5211;

var diTask = new JY5211DITask(slotNumber: 0);

// 添加单个线
diTask.AddLine(IOTerminal.PFI0);

// 或一次添加多个
diTask.AddLine(new IOTerminal[]
{
    IOTerminal.PFI0, IOTerminal.PFI1, IOTerminal.PFI2, IOTerminal.PFI3
});

diTask.Start();
```

### 5.2 静态读取

```csharp
// 方式 A：读取全部已添加的线，按 AddLine 顺序返回
bool[] values = new bool[4];
diTask.ReadSinglePoint(ref values);

// 方式 B：读取指定线（不依赖 AddLine 顺序）
bool singleValue;
diTask.ReadSinglePoint(IOTerminal.PFI5, out singleValue);
```

### 5.3 停止与资源

```csharp
diTask.Stop();
// Task 不再使用时置 null 让 GC 回收，析构会自动 Stop
diTask = null;
```

## 6. 数字输出 DO

```csharp
using JY5211;

var doTask = new JY5211DOTask(0);

// 添加所有 40 路：传入 IOTerminal 枚举完整数组
var allLines = (IOTerminal[])Enum.GetValues(typeof(IOTerminal));
// 注意 IOTerminal 还包含 PXI_Trig / PXI_Star 等非 DIO 端子，应过滤只保留 PFI0~PFI39
IOTerminal[] pfiLines = Array.FindAll(allLines,
    t => t >= IOTerminal.PFI0 && t <= IOTerminal.PFI39);
doTask.AddLine(pfiLines);

doTask.Start();

// 静态写入（长度必须等于已添加线数）
bool[] writeBuf = new bool[pfiLines.Length];
writeBuf[0] = true;
writeBuf[1] = false;
doTask.WriteSinglePoint(writeBuf);

doTask.Stop();
```

> DIO 与计数器端子共用，如果某 PFI 同时被 CO Task 的 `OutputTerminal` 占用，则 DO 不能再写该线，否则驱动抛冲突错误。

## 7. 计数器输入 CI

所有 CI 测量均通过 `JY5211CITask` 完成，根据 `Type` 切换到对应配置类。

### 7.1 共用配置

| 属性 | 说明 |
|---|---|
| `Type` (`CIType`) | EdgeCounting / Frequency / Period / Pulse / SemiPeriod / TwoEdgeSeparation / QuadEncoder / TwoPulseEncoder |
| `Mode` (`CIMode`) | Single / Finite / Continuous |
| `SamplesToAcquire` | 仅 Mode=Finite 时生效，每通道采集总点数 |
| `AvailableSamples` | Mode≠Single 时，缓冲区可读点数（只读） |
| `SampleClock` | 采样时钟：`Source = Internal / External / Implicit`；分别通过 `SampleClock.Internal.Rate` / `External.ExpectedRate + Terminal` / `Implicit.ExpectedRate` 配置 |
| `Trigger` | 开始触发：`Type = Immediate / Digital / Software`；`Digital.Source`（DigitalTriggerSource）、`Digital.Edge`（Rising/Falling） |
| `Device` | `Device.ReferenceClock` 参考时钟、`Device.PFISetting` PFI 电平/滤波器 |
| `Sync` | 多卡同步，见 §9 |
| `SignalExport` | 导出 `CI_StartTrig / CI_SampleClock / EventOut / PositionComparisonOut` 到 PFI / PXI_Trig |

### 7.2 边沿计数 EdgeCounting

```csharp
var ciTask = new JY5211CITask(0, 0);
ciTask.Type = CIType.EdgeCounting;
ciTask.Mode = CIMode.Single;

ciTask.EdgeCounting.InitialCount          = 0;                    // 计数初值
ciTask.EdgeCounting.ActiveEdge            = EdgeType.Rising;      // Rising / Falling
ciTask.EdgeCounting.Direction             = CountDirection.CountUp; // CountUp / CountDown / ExtControlled
ciTask.EdgeCounting.Pause.ActivePolarity  = LevelPolarity.None;   // None / HighLevel / LowLevel
ciTask.EdgeCounting.OutEvent.Threshold    = 100;                  // 达到此值触发 EventOut
ciTask.EdgeCounting.OutEvent.Reset        = false;                // 事件触发时是否清零

// 位置比较输出（可选）
ciTask.EdgeCounting.PositionComparison.Enable    = true;
ciTask.EdgeCounting.PositionComparison.Terminal  = IOTerminal.PFI1;
ciTask.EdgeCounting.PositionComparison.IdleState = EdgeCntOunEvtIdleState.HighLevel;
ciTask.EdgeCounting.PositionComparison.PositionValues(new uint[] { 100, 200, 300 });

ciTask.Start();

// 单点读
uint count;
ciTask.ReadSinglePoint(out count);

// 或有符号
int sCount;
ciTask.ReadSinglePoint(out sCount);

ciTask.Stop();
```

连续模式示例：

```csharp
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Internal;
ciTask.SampleClock.Internal.Rate = 1000; // 1 kHz

ciTask.Start();

uint[] buf = new uint[1000];
while (running)
{
    if ((int)ciTask.AvailableSamples >= buf.Length)
        ciTask.ReadData(ref buf, buf.Length, timeout: 10000);
}

ciTask.Stop();
```

### 7.3 频率测量 Frequency

```csharp
ciTask.Type = CIType.Frequency;
ciTask.Mode = CIMode.Continuous;

// 时基决定精度：100MHz > 5MHz > 100kHz
ciTask.FrequencyMeas.Timebase.Source = CITimebaseSource.Internal100MHz;
// 若使用外部时基：
// ciTask.FrequencyMeas.Timebase.Source = CITimebaseSource.External;
// ciTask.FrequencyMeas.Timebase.External.Terminal  = ClockTerminal.PFI33;
// ciTask.FrequencyMeas.Timebase.External.Frequency = 10e6;

ciTask.SampleClock.Source = CISampleClockSource.Internal;
ciTask.SampleClock.Internal.Rate = 10; // 每秒 10 次测量

ciTask.Start();
double[] freq = new double[10];
ciTask.ReadData(ref freq, freq.Length, 10000);  // 单位：Hz
ciTask.Stop();
```

### 7.4 周期测量 Period

```csharp
ciTask.Type = CIType.Period;
ciTask.PeriodMeas.Timebase.Source = CITimebaseSource.Internal100MHz;
// 其余字段同 FrequencyMeas
ciTask.Mode = CIMode.Single;
ciTask.Start();
double period;   // 单位：秒
ciTask.ReadSinglePoint(out period, timeout: 10000);
ciTask.Stop();
```

### 7.5 脉冲测量 Pulse（同时得到高/低电平持续时间）

```csharp
ciTask.Type = CIType.Pulse;
ciTask.PulseMeas.Timebase.Source = CITimebaseSource.Internal100MHz;
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Internal;
ciTask.SampleClock.Internal.Rate = 100;

ciTask.Start();

double[] highTime = new double[100];
double[] lowTime  = new double[100];
ciTask.ReadData(ref highTime, ref lowTime, highTime.Length, 10000); // 单位：秒

ciTask.Stop();
```

Single 模式：

```csharp
double high, low;
ciTask.ReadSinglePoint(out high, out low, 10000);
```

### 7.6 半周期测量 SemiPeriod

```csharp
ciTask.Type = CIType.SemiPeriod;
ciTask.SemiPeriodMeas.Timebase.Source = CITimebaseSource.Internal100MHz;
// API 与 Period 类似，ReadSinglePoint(out double)/ ReadData(ref double[], ...)
```

### 7.7 双边沿间隔测量 Two-Edge Separation

```csharp
ciTask.Type = CIType.TwoEdgeSeparation;
ciTask.TwoEdgeSeparation.Timebase.Source = CITimebaseSource.Internal100MHz;
ciTask.TwoEdgeSeparation.FirstEdge  = EdgeType.Rising;   // 第一个信号沿
ciTask.TwoEdgeSeparation.SecondEdge = EdgeType.Rising;   // 第二个信号沿
// 返回值与 Pulse 相同：(measurement1, measurement2) 见 XML 定义
ciTask.Start();
double m1, m2;
ciTask.ReadSinglePoint(out m1, out m2, 10000);
ciTask.Stop();
```

### 7.8 正交编码 Quadrature Encoder

```csharp
ciTask.Type = CIType.QuadEncoder;
ciTask.QuadEncoder.EncodingType = QuadEncodingType.X1; // X1 / X2 / X4
ciTask.Mode = CIMode.Continuous;
ciTask.SampleClock.Source = CISampleClockSource.Internal;
ciTask.SampleClock.Internal.Rate = 1000;

ciTask.Start();
uint[] pos = new uint[1000];
ciTask.ReadData(ref pos, pos.Length, 10000);
ciTask.Stop();
```

### 7.9 双脉冲编码 Two-Pulse Encoder

```csharp
ciTask.Type = CIType.TwoPulseEncoder;
// ciTask.TwoPulseEncoder.InitialCount = 0; 等
// 读取方式与 EdgeCounting 相同（uint / int 计数值）
```

### 7.10 采样时钟 3 种来源

| Source | 含义 | 关键字段 |
|---|---|---|
| `CISampleClockSource.Internal` | 板卡内部 100 MHz 时基分频 | `SampleClock.Internal.Rate`（Hz） |
| `CISampleClockSource.External` | 由 PFI/PXI_Trig 输入 | `SampleClock.External.Terminal`（ClockTerminal）+ `ExpectedRate` |
| `CISampleClockSource.Implicit` | 由被测信号节拍触发（例如每个脉冲得到一个样点） | `SampleClock.Implicit.ExpectedRate`（估计速率，用于缓冲分配） |

## 8. 计数器输出 CO（脉冲输出）

### 8.1 `COPulse` 脉冲描述

`COPulse(COPulseType type, double param1, double param2, int count)`

| `COPulseType` | param1 | param2 | 含义 |
|---|---|---|---|
| `HighLowTime` | 高电平时长(s) | 低电平时长(s) | 按秒指定 |
| `DutyCycleFrequency` | 频率(Hz) | 占空比(0~1) | 按频率 + 占空比 |
| `HighLowTick` | 高电平 Tick 数 | 低电平 Tick 数 | 按 Timebase 的 Tick 计数（最细粒度） |

`count` 为脉冲个数，**`-1` 表示连续无穷输出**（只在 `Single` 模式有意义）。

### 8.2 Single 模式（最常用）

```csharp
var coTask = new JY5211COTask(0, 0);
coTask.OutputTerminal = IOTerminal.PFI3;    // 脉冲输出端子
coTask.Mode           = COMode.Single;
coTask.IdleState      = COIdleState.LowLevel;
coTask.InitialDelay   = 0;                   // 秒

// 时基（仅 Single 模式支持非内部 100 MHz 时基）
coTask.Timebase.Source = COTimebaseSource.Internal100MHz;
// 若 External：coTask.Timebase.External.Terminal = ClockTerminal.PFI0; Frequency = 10e6;

// 1kHz, 50% 占空比，输出 1000 个脉冲
coTask.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 1000, 0.5, 1000));

coTask.Start();
coTask.WaitUntilDone(10000);
coTask.Stop();
```

连续脉冲（`count = -1`）：

```csharp
coTask.WriteSinglePoint(new COPulse(COPulseType.HighLowTime, 0.5, 0.5, -1));
coTask.Start();
// 任意时刻可以再次 WriteSinglePoint 更新波形（仅 count=-1 时允许）
coTask.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 2000, 0.25, -1));
```

### 8.3 Finite 模式（多段脉冲，有限次）

```csharp
coTask.Mode = COMode.Finite;
coTask.OutputTerminal = IOTerminal.PFI3;
coTask.SamplesToUpdate = 3;

var pulses = new COPulse[]
{
    new COPulse(COPulseType.HighLowTime, 0.5, 0.5, 10),
    new COPulse(COPulseType.DutyCycleFrequency, 5, 0.5, 20),
    new COPulse(COPulseType.HighLowTick, 1_000_000, 1_000_000, 30),
};
coTask.WriteData(pulses, pulses.Length, timeout: 10000); // Finite 模式下仅允许调用一次
coTask.Start();
coTask.WaitUntilDone(60000);
coTask.Stop();
```

### 8.4 Continuous 模式

| Mode | 特性 | 数据写入 |
|---|---|---|
| `ContinuousWrapping` | 缓冲区数据循环发送 | `Start` 前调用一次 `WriteData` |
| `ContinuousNoWrapping` | 流式送出，需不断补数据 | `Start` 前至少写入 2 组；运行期间继续 `WriteData` 补数据 |

```csharp
coTask.Mode = COMode.ContinuousNoWrapping;
coTask.OutputTerminal = IOTerminal.PFI3;
coTask.WriteData(pulses, pulses.Length, 10000);
coTask.Start();

while (running)
{
    // 根据 AvaliableLenInSamples 判断可写入空间
    if (coTask.AvaliableLenInSamples >= pulses.Length)
        coTask.WriteData(pulses, pulses.Length, 10000);
}
```

### 8.5 触发与暂停

```csharp
// 数字触发
coTask.Trigger.Type          = COTriggerType.Digital;
coTask.Trigger.Digital.Source = DigitalTriggerSource.PFI32;
coTask.Trigger.Digital.Edge   = CODigitalTriggerEdge.Rising;

// 软件触发
coTask.Trigger.Type = COTriggerType.Software;
coTask.Start();
coTask.SendSoftwareTrigger();

// 暂停触发（低/高电平暂停输出）
coTask.Pause.ActivePolarity = LevelPolarity.HighLevel;
```

## 9. 多卡同步

JY5211 支持 PXIe 背板的参考时钟同步 + 触发线路由同步。拓扑分 `SyncTopology.Master` 与 `SyncTopology.Slave`，由 Master 输出开始触发到 PXI_Trig 线上，所有 Slave 监听该 PXI_Trig。

### 9.1 参考时钟

```csharp
// Master 与 Slave 都必须把参考时钟锁到同一个源（PXIe_Clk100）
ciTask.Device.ReferenceClock.Source            = ReferenceClockSource.External;
ciTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
ciTask.Device.ReferenceClock.Commit();   // 必须调用 Commit 才生效
```

| ReferenceClockSource | 含义 |
|---|---|
| `Internal` | 板卡内部 10 MHz TCXO（默认） |
| `External` | 由 `External.Terminal` 指定：`PXIe_Clk100` / `PXIe_DStarA` |

### 9.2 同步拓扑（CI）

```csharp
// Master
ciTaskMaster.Sync.Topology       = SyncTopology.Master;
ciTaskMaster.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;  // Master 把 StartTrig 发到 PXI_Trig0

// Slave
ciTaskSlave.Sync.Topology       = SyncTopology.Slave;
ciTaskSlave.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;   // Slave 监听 PXI_Trig0

// 启动顺序：必须先启动所有 Slave，最后启动 Master
ciTaskSlave.Start();
ciTaskMaster.Start();
```

| SyncTopology | 含义 |
|---|---|
| `Independent` | 独立运行（默认） |
| `Master` | 主卡，生成同步触发 |
| `Slave` | 从卡，接收同步触发 |

> CO Task 也有同 API 的 `Sync`（`JY5211COTask.Sync`），用法一致。

### 9.3 PFI 电平与滤波器

```csharp
// UserVcc（每通道独立）
ciTask.Device.PFISetting.PowerLevel = PowerLevel.Level_3V3;  // 1V8 / 2V5 / 3V3 / 5V
ciTask.Device.PFISetting.Commit();

// PFI 数字滤波器（去除毛刺）
ciTask.Device.PFISetting.Filter.MinPulseWidth = 50e-9;       // 50 ns
ciTask.Device.PFISetting.Filter.Enable(IOTerminal.PFI0);
```

### 9.4 信号导出

```csharp
// 把 CI 的 SampleClock 导出到 PFI / PXI_Trig 供其他板卡使用
ciTask.SignalExport.Add(CISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig1);
ciTask.SignalExport.Add(CISignalExportSource.StartTrig,   SignalExportDestination.PFI10);

// 清除
ciTask.SignalExport.Clear(SignalExportDestination.PFI10);
ciTask.SignalExport.ClearAll();
```

CO 的导出源为 `COSignalExportSource.StartTrig / Output`，API 一致（`coTask.SignalExport`）。

## 10. 常见错误处理

所有驱动错误都以 `JY5211.JYDriverException` 抛出。建议在每个 Start/Read/Write 调用周围 `try-catch`。

| 错误码（`JYDriverExceptionPublic`） | 典型原因与对策 |
|---|---|
| `OpenDeviceFailed` | 槽位号或板卡名不对；驱动未安装；板卡被其他进程占用 |
| `NoChannelAdded` | DI/DO 在 `Start` 前未 `AddLine`；或 CI/CO 构造未指定 channel |
| `BufferDataOverflow` | 读取不及时导致板载/驱动缓冲溢出——提高读取频率或降低采样率 |
| `ReadDataTimeout` | 超时时间太短，或信号未到达、触发未满足 |
| `SyncParameterInvalid` | Master/Slave 的 `TriggerRouting` 不一致或参考时钟未 Commit |
| `PLLUnlocked` | 外部参考时钟缺失/漂移，检查 PXIe_Clk100 接入 |
| `InvalidParameter` | 属性取值超范围（如 Rate 过高、占空比 ≤0 或 ≥1） |
| `OperationNotAllowedAfterStart` | 在 Start 之后修改了只允许启动前修改的属性 |

错误处理推荐写法：

```csharp
try
{
    ciTask.Start();
    ciTask.ReadData(ref buf, buf.Length, 5000);
}
catch (JYDriverException jex)
{
    // 优先使用驱动自带的错误码与中文描述
    MessageBox.Show($"JY5211 驱动错误 [{jex.ErrorCode}]: {jex.Message}");
    ciTask?.Stop();
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message);
    ciTask?.Stop();
}
```

## 11. 完整 API 参考

### 11.1 核心 Task 类

| 类 | 构造 | 说明 |
|---|---|---|
| `JY5211CITask` | `(int slot, int channel)` / `(string name, int channel)` | 计数器输入任务，通道 0~7 |
| `JY5211COTask` | `(int slot, int channel)` / `(string name, int channel)` | 计数器输出任务，通道 0~7 |
| `JY5211DITask` | `(int slot)` / `(string name)` | 数字输入任务（板卡级） |
| `JY5211DOTask` | `(int slot)` / `(string name)` | 数字输出任务（板卡级） |
| `JY5211Device` | 由 `Task.Device` 属性暴露 | 参考时钟、PFI 电平、硬件信息 |

### 11.2 `JY5211CITask` 属性

| 属性 | 类型 | 说明 |
|---|---|---|
| `Type` | `CIType` | 测量类型，切换时对应的配置类才生效 |
| `Mode` | `CIMode` | Single / Finite / Continuous |
| `SamplesToAcquire` | `int` | Finite 模式的每通道样点数 |
| `AvailableSamples` | `long` | 可读点数（只读） |
| `TransferedSamples` | `long` | 已传输点数（只读） |
| `MinSamplesToTransfer` | `int` | 每次 DMA 最小传输样点数，默认 0（自动） |
| `SizeOfSample` | `int` | 当前 Type 下每样点字节数（只读） |
| `IsRuning` | `bool` | 任务是否在运行（只读） |
| `ChannelID` | `int` | 通道号（只读） |
| `Trigger` | `CITrigger` | 开始触发配置 |
| `SampleClock` | `CISampleClock` | 采样时钟配置 |
| `Sync` | `Sync` | 同步配置 |
| `Device` | `JY5211Device` | 板卡级配置 |
| `SignalExport` | `CISignalExport` | 信号导出配置 |
| `EdgeCounting` | `EdgeCounting` | 仅 Type=EdgeCounting 生效 |
| `FrequencyMeas` | `FrequencyMeas` | 仅 Type=Frequency 生效 |
| `PeriodMeas` | `PeriodMeas` | 仅 Type=Period 生效 |
| `PulseMeas` | `PulseMeas` | 仅 Type=Pulse 生效 |
| `SemiPeriodMeas` | `SemiPeriodMeas` | 仅 Type=SemiPeriod 生效 |
| `TwoEdgeSeparation` | `TwoEdgeSeparation` | 仅 Type=TwoEdgeSeparation 生效 |
| `QuadEncoder` | `QuadEncoder` | 仅 Type=QuadEncoder 生效 |
| `TwoPulseEncoder` | `TwoPulseEncoder` | 仅 Type=TwoPulseEncoder 生效 |

### 11.3 `JY5211CITask` 方法

```csharp
void Start();
void Stop();
void SendSoftwareTrigger();                // Trigger.Type == Software 时才有效
void WaitUntilDone(int timeout);           // Finite 模式等待任务完成

// EdgeCounting / QuadEncoder / TwoPulseEncoder
void ReadSinglePoint(out uint count);
void ReadSinglePoint(out int  count);
void ReadData(ref uint[] buf, int samples, int timeout);
void ReadData(ref int[]  buf, int samples, int timeout);
void ReadData(ref uint[] buf, int timeout);   // 读满 buf.Length
void ReadData(ref int[]  buf, int timeout);

// Frequency / Period / SemiPeriod
void ReadSinglePoint(out double meas, int timeout);
void ReadData(ref double[] buf, int samples, int timeout);
void ReadData(ref double[] buf, int timeout);

// Pulse / TwoEdgeSeparation（两个结果）
void ReadSinglePoint(out double m1, out double m2, int timeout);
void ReadData(ref double[] buf1, ref double[] buf2, int samples, int timeout);
void ReadData(ref double[] buf1, ref double[] buf2, int timeout);
```

### 11.4 `JY5211COTask` 属性与方法

| 属性 | 类型 | 说明 |
|---|---|---|
| `Mode` | `COMode` | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `OutputTerminal` | `IOTerminal` | 脉冲输出端子（PFI0~PFI39） |
| `IdleState` | `COIdleState` | HighLevel / LowLevel，默认 LowLevel |
| `InitialDelay` | `double` | 起始延迟（秒），硬件内部固定 + 5 Tick |
| `Timebase` | `COTimebase` | 仅 Mode=Single 时支持非 100 MHz 时基 |
| `Trigger` | `COTrigger` | Immediate / Digital / Software |
| `Pause` | `PauseTrigger` | 暂停触发 |
| `Sync` | `Sync` | 多卡同步 |
| `SignalExport` | `COSignalExport` | StartTrig / Output 导出 |
| `SamplesToUpdate` | `int` | Finite / ContinuousWrapping 时缓冲写入长度 |
| `AvaliableLenInSamples` | `long` | 板载缓冲可写长度（只读，Mode≠Single） |
| `TransferedSamples` | `long` | 已传输样点数（只读） |
| `TransferedPulses` | `long` | 已发送脉冲总数（只读） |
| `Device` | `JY5211Device` | |
| `ChannelID` | `int` | |

```csharp
void WriteSinglePoint(COPulse pulse);                     // Mode=Single
void WriteData(COPulse[] pulsesGroup, int samples, int timeout = -1);  // Mode≠Single
void Start();
void Stop();
void SendSoftwareTrigger();
void WaitUntilDone(int timeout);
```

### 11.5 `JY5211DITask` / `JY5211DOTask`

```csharp
// 共同
bool IsRuning { get; }
JY5211Device Device { get; }
IOTerminal[] Lines { get; }
void AddLine(IOTerminal line);
void AddLine(IOTerminal[] lines);
void RemoveLine(IOTerminal line);
void RemoveLine(IOTerminal[] lines);
void Start();
void Stop();

// DI
void JY5211DITask.ReadSinglePoint(ref bool[] buf);                   // 按 AddLine 顺序
void JY5211DITask.ReadSinglePoint(IOTerminal line, out bool value);  // 指定线

// DO
void JY5211DOTask.WriteSinglePoint(bool[] buf);                      // 长度需等于已添加线数
```

### 11.6 关键枚举速查

| 枚举 | 取值 |
|---|---|
| `CIType` | EdgeCounting, Frequency, Period, Pulse, SemiPeriod, TwoEdgeSeparation, QuadEncoder, TwoPulseEncoder |
| `CIMode` | Single, Finite, Continuous |
| `CITimebaseSource` | Internal100MHz, Internal5MHz, Internal100kHz, External |
| `CISampleClockSource` | Internal, External, Implicit |
| `CITriggerType` | Immediate, Digital, Software |
| `CIDigitalTriggerEdge` | Rising, Falling |
| `COMode` | Single, Finite, ContinuousWrapping, ContinuousNoWrapping |
| `COPulseType` | DutyCycleFrequency, HighLowTime, HighLowTick |
| `COTriggerType` | Immediate, Digital, Software |
| `CODigitalTriggerEdge` | Rising |
| `COTimebaseSource` | Internal100MHz, Internal5MHz, Internal100kHz, External |
| `COIdleState` | HighLevel, LowLevel |
| `EdgeType` | Rising, Falling |
| `LevelPolarity` | None, HighLevel, LowLevel |
| `CountDirection` | CountUp, CountDown, ExtControlled |
| `QuadEncodingType` | X1, X2, X4 |
| `SyncTopology` | Independent, Master, Slave |
| `SyncTriggerRouting` | PFI0~PFI39, PXI_Trig0~PXI_Trig7 |
| `ReferenceClockSource` | Internal, External |
| `ExternalReferenceClockTerminal` | PXIe_Clk100, PXIe_DStarA |
| `PowerLevel` | Level_1V8, Level_2V5, Level_3V3, Level_5V |
| `IOTerminal` | PFI0~PFI39, PXI_Trig0~PXI_Trig7, PXI_Star |
| `ClockTerminal` | PFI0~PFI39, PXI_Trig0~PXI_Trig7, CO_0_Out~CO_7_Out, CI_0_SampleClock~CI_7_SampleClock |
| `DigitalTriggerSource` | PFI0~PFI39, PXI_Trig0~PXI_Trig7, PXI_Star, CO_0~7_Out, CIO_0~7_StartTrig/EventOut/PositionComparisonOut |
| `SignalExportDestination` | PFI0~PFI39, PXI_Trig0~PXI_Trig7 |
| `CISignalExportSource` | StartTrig, SampleClock, EventOut, PositionComparisonOut |
| `COSignalExportSource` | StartTrig, Output |

## 12. 完整代码示例

### 12.1 示例：连续边沿计数 + 外部触发 + 采样时钟 1 kHz

```csharp
using System;
using JY5211;

class Demo
{
    static void Main()
    {
        using var ciTask = new JY5211CITask(slotNumber: 0, channel: 0);

        ciTask.Type = CIType.EdgeCounting;
        ciTask.Mode = CIMode.Continuous;

        ciTask.EdgeCounting.InitialCount = 0;
        ciTask.EdgeCounting.ActiveEdge   = EdgeType.Rising;
        ciTask.EdgeCounting.Direction    = CountDirection.CountUp;

        ciTask.SampleClock.Source       = CISampleClockSource.Internal;
        ciTask.SampleClock.Internal.Rate = 1000;    // 1 kHz

        ciTask.Trigger.Type           = CITriggerType.Digital;
        ciTask.Trigger.Digital.Source = DigitalTriggerSource.PFI32;
        ciTask.Trigger.Digital.Edge   = CIDigitalTriggerEdge.Rising;

        ciTask.Start();

        uint[] buf = new uint[1000];
        for (int i = 0; i < 10; i++)
        {
            while (ciTask.AvailableSamples < buf.Length)
                System.Threading.Thread.Sleep(10);
            ciTask.ReadData(ref buf, buf.Length, 5000);
            Console.WriteLine($"Block {i}: last count = {buf[buf.Length - 1]}");
        }

        ciTask.Stop();
    }
}
```

### 12.2 示例：连续脉冲输出 1 kHz 方波，中途改频率

```csharp
using var coTask = new JY5211COTask(0, 0);
coTask.OutputTerminal      = IOTerminal.PFI3;
coTask.Mode                = COMode.Single;
coTask.IdleState           = COIdleState.LowLevel;
coTask.Timebase.Source     = COTimebaseSource.Internal100MHz;

// 先输出 1 kHz，50% 占空比，无限
coTask.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 1000, 0.5, -1));
coTask.Start();

System.Threading.Thread.Sleep(3000);
// 在线改为 2 kHz，25% 占空比（仅 count=-1 时支持在线更新）
coTask.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 2000, 0.25, -1));

System.Threading.Thread.Sleep(3000);
coTask.Stop();
```

### 12.3 示例：两张卡 Master-Slave 同步边沿计数

```csharp
using JY5211;

var master = new JY5211CITask(slotNumber: 0, channel: 0);
var slave  = new JY5211CITask(slotNumber: 1, channel: 0);

foreach (var t in new[] { master, slave })
{
    t.Device.ReferenceClock.Source            = ReferenceClockSource.External;
    t.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
    t.Device.ReferenceClock.Commit();

    t.Type = CIType.EdgeCounting;
    t.Mode = CIMode.Continuous;
    t.EdgeCounting.InitialCount = 0;
    t.EdgeCounting.ActiveEdge   = EdgeType.Rising;
    t.EdgeCounting.Direction    = CountDirection.CountUp;

    t.SampleClock.Source        = CISampleClockSource.Internal;
    t.SampleClock.Internal.Rate = 10;
}

// Master 产生 StartTrig 到 PXI_Trig0
master.Trigger.Type           = CITriggerType.Software;
master.Sync.Topology          = SyncTopology.Master;
master.Sync.TriggerRouting    = SyncTriggerRouting.PXI_Trig0;

// Slave 监听 PXI_Trig0
slave.Sync.Topology           = SyncTopology.Slave;
slave.Sync.TriggerRouting     = SyncTriggerRouting.PXI_Trig0;

// 关键：必须先 Slave 后 Master
slave.Start();
master.Start();

master.SendSoftwareTrigger();   // 一起启动

uint[] bufM = new uint[100];
uint[] bufS = new uint[100];
master.ReadData(ref bufM, bufM.Length, 10000);
slave .ReadData(ref bufS, bufS.Length, 10000);

master.Stop();
slave.Stop();
```

### 12.4 示例：DO 输出 + DI 回读

```csharp
using var doTask = new JY5211DOTask(0);
using var diTask = new JY5211DITask(0);

var outLines = new[] { IOTerminal.PFI0, IOTerminal.PFI1 };
var inLines  = new[] { IOTerminal.PFI4, IOTerminal.PFI5 };

doTask.AddLine(outLines);
diTask.AddLine(inLines);

doTask.Start();
diTask.Start();

doTask.WriteSinglePoint(new bool[] { true, false });

bool[] read = new bool[2];
diTask.ReadSinglePoint(ref read);
Console.WriteLine($"PFI4={read[0]}, PFI5={read[1]}");

doTask.Stop();
diTask.Stop();
```

### 12.5 示例：脉冲宽度测量（同时得到高低电平时间）

```csharp
using var ciTask = new JY5211CITask(0, 0);
ciTask.Type = CIType.Pulse;
ciTask.Mode = CIMode.Continuous;

ciTask.PulseMeas.Timebase.Source = CITimebaseSource.Internal100MHz;

ciTask.SampleClock.Source        = CISampleClockSource.Implicit;
ciTask.SampleClock.Implicit.ExpectedRate = 1000;  // 估计每秒 1000 个脉冲

ciTask.Start();

double[] high = new double[500];
double[] low  = new double[500];
ciTask.ReadData(ref high, ref low, high.Length, 10000);

for (int i = 0; i < 5; i++)
    Console.WriteLine($"High={high[i]*1e6:F3}us, Low={low[i]*1e6:F3}us");

ciTask.Stop();
```

## 13. 注意事项速记

1. **CI Task 是单通道级**：每个计数器通道必须独立建 Task；同一 slot+channel 不能重复构造。
2. **修改属性必须在 Start 之前**。只有 `Single + count=-1` 的 CO 允许 Start 之后 `WriteSinglePoint` 更新波形。
3. **`ciTask.Type` 切换后，只有对应的配置类（如 `FrequencyMeas`）起作用**——不要混用多个配置类，且切换前建议先 Stop。
4. **多卡同步**：参考时钟必须 `Commit()`；必须先启动所有 Slave，再启动 Master；`TriggerRouting` 必须一致。
5. **`-1` 超时**表示永不超时，生产代码不推荐。
6. **IOTerminal 枚举混合了 PFI 与 PXI 触发线**：DI/DO 只应使用 `PFI0~PFI39`，不要把 `PXI_Trig*` 加到 AddLine。
7. **Finite 模式下 CO 的 `WriteData` 只允许在 Start 前调用一次**；`ContinuousNoWrapping` 至少先写 2 组数据再 Start。
8. **`DigitalTriggerSource` 范围比 `IOTerminal` 更广**：可以用内部 CI/CO 的 StartTrig/EventOut 作为触发源，用于板内任务链式触发。
9. **时基精度**：2 ppm（TCXO）；需要更高精度请锁到 PXIe_Clk100。
10. **FIFO 15 M Samples/通道**：高速连续采集时，驱动端应保证读取速率 ≥ 采样速率，否则 `BufferDataOverflow`。
