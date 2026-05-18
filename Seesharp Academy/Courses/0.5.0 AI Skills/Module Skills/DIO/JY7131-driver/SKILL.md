---
name: JY7131 Driver Skill
description: 简仪 JY7131 系列高压隔离型多功能数字 IO 板卡 C# 驱动使用规范。当用户需要使用 JY7131（PXIe-7131）的 32 路隔离 DI、32 路隔离 DO（Sourcing/Sinking/Push-Pull）、8 通道计数器/定时器（边沿计数 / 频率 / 周期 / 双边沿间隔 / 正交编码 / 双脉冲编码 / 脉冲输出），或进行多卡同步、DO 上电状态配置时必须加载本技能。
---

# JY7131 驱动使用指南

> 本文档覆盖 JY7131 系列板卡的 C# 驱动（命名空间 `JY7131`）。该系列为**高压隔离型工业数字 IO + 计数器/定时器**板卡：32 路隔离 DI（0~55 V_DC）+ 32 路隔离 DO（10~50 V_DC，Sourcing/Sinking/Push-Pull）+ 8 路 32 bit 计数器。**不含 AI/AO 模拟通道**。DI/DO 为 **Port-based（4 Port × 8 Line）**、电气隔离、支持 DO 上电默认状态配置。

## 1. 环境要求

| 项 | 说明 |
|---|---|
| 驱动 DLL | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY7131\Bin\JY7131.dll` |
| XML 文档 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY7131\Bin\JY7131.xml` |
| 范例代码 | `D:\JYTEK_Work\Examples\C#\DAQ\JY7131.Example` |
| .NET Framework | 4.0 及以上 |
| 命名空间 | `using JY7131;` |
| 可选 GUI 套件 | `SeeSharpTools.JY.GUI`（范例中使用的图表/LED 控件） |
| 异常类型 | `JY7131.JYDriverException`（所有驱动层错误都以此抛出） |

## 2. 硬件规格速查

> 数据来源：简仪产品型录 PXIe-7131 章节 + 驱动 XML。

### 2.1 隔离数字输入（DI）

| 项 | 规格 |
|---|---|
| 通道数 | 32（4 Port × 8 Line，`Port 0 ~ Port 3`） |
| 最大输入电压 | 55 V_DC |
| 逻辑低电平 V_IL | 0 ~ ±8.5 V_DC |
| 逻辑高电平 V_IH | ±10 ~ ±55 V_DC |
| 输入类型 | Sourcing / Sinking |
| 输入隔离电压 | 2500 V_rms |
| 输入电流（典型） | 2.48 mA（min 2.23 / max 2.80 mA） |

### 2.2 隔离数字输出（DO）

| 项 | 规格 |
|---|---|
| 通道数 | 32（4 Port × 8 Line，`Port 0 ~ Port 3`） |
| V_DD（用户外部供电） | 10 ~ 50 V_DC |
| 输出电压 | 10 ~ 50 V_DC |
| 最大输出电流 | Out=High 640 mA / Out=Low 440 mA |
| 输出类型 | Sourcing / Sinking / Push-Pull（按 Port 粒度配置） |
| 输出隔离电压 | 3000 V_rms |
| 上电默认状态 | Tristate（高阻）/ High / Low（通过 `JY7131Device.SetPowerOn` 配置） |

**DO 模式电平真值表**：

| DOLineMode | DO 写入 1 | DO 写入 0 |
|---|---|---|
| Sourcing | High Level（V_DD） | High-Z（高阻） |
| Sinking | High-Z（高阻） | Low Level（GND） |
| Push-Pull | High Level（V_DD） | Low Level（GND） |

### 2.3 计数器 / 定时器（8 通道 0~7）

| 项 | 规格 |
|---|---|
| 通道数 | 8（Counter 0 ~ Counter 7） |
| 分辨率 | 32 bits |
| 内部时钟频率 | 100 MHz |
| 工作电压 | 最高 50 V |
| CI 测量类型 | Edge Counting / Period / Frequency / Two-Edge Separation / Quadrature Encoder（×1/×2/×4）/ Two-Pulse Encoder |
| CI 输入频率 | 最高 1 MHz（高电平 ≥ 300 ns） |
| CO 输出模式 | Single / Finite / Continuous Pulse / PWM |
| CO 输出频率 | 最高 200 kHz |
| 本地 FIFO | 16 M Samples |
| CI 输入信号 | Source(A) / Aux(B) / Gate(Z) / Digital Trigger / External Sample Clock |
| CO 输出信号 | OUT |
| CI 时基 | 内部 100 MHz / 5 MHz / 100 kHz，或外部时基（通过 `InputTerminal`） |

> **注意**：JY7131 的 `CIType` **不包含** `Pulse` 与 `SemiPeriod`。

### 2.4 PFI 与总线

| 项 | 规格 |
|---|---|
| PFI 输入枚举 | `PFI_In_0 ~ PFI_In_31`（32 根） |
| PFI 输出枚举 | `PFI_Out_0 ~ PFI_Out_31`（32 根） |
| PFITerminal（Filter 配置） | `PFI0 ~ PFI31` |
| 参考时钟内部源 | 25 MHz（板载 TCXO） |
| 参考时钟外部源 | `PXIe_Clk100`（PXIe 背板） |
| 总线接口 | x4 PXI Express 外设模块，兼容 x1/x4 PXIe 或 PXIe 混合槽 |
| 背板触发线 | `PXI_Trig0 ~ PXI_Trig7`（可作触发输入/输出） |
| 附件 | ACL-1020100-1（1 m 100 pin SCSI 双绞线）/ -2（2 m）/ DIN-100-1（100 pin SCSI 接线端子） |

### 2.5 工作环境

| 项 | 规格 |
|---|---|
| 工作温度 | 0 ~ 50 ℃ |
| 工作湿度 | 20 % ~ 80 % RH（无凝结） |
| 存储温度 | -20 ~ 50 ℃ |
| 存储湿度 | 10 % ~ 90 % RH（无凝结） |

## 3. 产品型号

| 型号 | 总线 | 说明 |
|---|---|---|
| PXIe-7131 | x4 PXI Express | 64 通道（32 DI + 32 DO），50 V_DC，Bank 隔离数字 IO 模块 |

构造器接受 **槽位号**（从 0 开始，按系统枚举顺序）或 **板卡别名**（在 JYTEK Measurement & Automation Explorer 中配置）。

## 4. 通用编程范式

JY7131 驱动按 **功能类型** 划分 Task：

- `JY7131CITask`：计数器输入（通道级，每 Task 1 个通道 0~7）
- `JY7131COTask`：计数器输出（通道级，每 Task 1 个通道 0~7）
- `JY7131DITask`：数字输入（板卡级，`AddChannel(portNum)` 添加 Port）
- `JY7131DOTask`：数字输出（板卡级，`AddChannel(portNum, DOTerminal)` 添加 Port 并指定输出模式）
- `JY7131Device`：板卡级设备对象，用于配置参考时钟、PFI 滤波、DO 上电状态

生命周期：

```
new TaskXxx(slot/name, [channel])
    → 配置属性 (Type / Mode / SampleClock / Trigger / Signal Export …)
    → [可选] Device.ReferenceClock 配置 + Commit（多卡同步时）
    → Start()
    → ReadXxx / WriteXxx / ReadData / WriteData
    → Stop()
    → Channels.Clear()（DI/DO 复用 Task 时）
```

**关键规则**

1. **CI / CO 是通道级 Task**：构造时必须指定 `channel (0~7)`，每通道一个 Task。
2. **DI / DO 是 Port 级 Task**：构造只传 slot/name；用 `AddChannel(portNum)` 添加要用的 Port（0~3）。**注意：添加粒度是 Port（8 根线一组），不是单根线。**
3. **DO 的输出模式（Sourcing/Sinking/Push-Pull）按 Port 配置**：`AddChannel(portNum, DOTerminal.Push_Pull)`。一次 `AddChannel` 可为不同 Port 指定不同 DOTerminal。
4. **属性修改必须在 `Start()` 之前完成**。Start 之后修改属性不生效，部分还会抛异常。
5. **CI 的测量配置是分离的配置类**：先设 `ciTask.Type = CIType.Xxx`，再操作对应子配置类（`ciTask.EdgeCounting.*` / `ciTask.FrequencyMeas.*` 等）。
6. **CI OutEvent 的 Terminal 使用 `IOTerminal`**：JY7131 支持 `PFI_Out_0~31` 或 `PXI_Trig0~7`，因此 OutEvent 可直接从 PFI 输出。
7. **CO Timebase 的外部源使用 `InputTerminal`**，CI SampleClock 的外部源使用 `ClockTerminal`。两者是不同枚举，不要混用。
8. **DO 上电状态**：需通过 `JY7131Device.SetPowerOn(DOTerminal[], bool[])` 配置，断电再上电后生效（详见 §9）。
9. **异常捕获**：所有调用均可能抛 `JY7131.JYDriverException`，典型错误码见 §11。

### 4.1 Task 构造模板

```csharp
using JY7131;

// CI / CO：按槽位号 + 通道号
var ciTask = new JY7131CITask(slotNumber: 0, channel: 0);
var coTask = new JY7131COTask(slotNumber: 0, channel: 0);

// DI / DO：按槽位号
var diTask = new JY7131DITask(slotNumber: 0);
var doTask = new JY7131DOTask(slotNumber: 0);

// Device：用于参考时钟 / PFI 滤波 / DO 上电状态
var device = new JY7131Device((ushort)0);

// 也可按 MAX 中配置的别名
var ciTask2 = new JY7131CITask("JY7131_0", 0);
```

## 5. 数字输入 DI

> JY7131 DI 共 32 路，分为 4 个 Port（Port 0~3），每 Port 8 根 Line。读写均以 `bool[]` 形式。

### 5.1 任务构造与 Port 添加

```csharp
using JY7131;

var diTask = new JY7131DITask(slotNumber: 0);

// 添加单个 Port
diTask.AddChannel(0);                    // 添加 Port 0

// 一次添加多个 Port
diTask.AddChannel(new int[] { 0, 1, 2, 3 });

// 删除某个 Port
diTask.RemoveChannel(2);
```

### 5.2 静态读取

```csharp
diTask.Start();

// 读全部 Port 的所有 Line（长度 = 32）
bool[] all = new bool[32];
diTask.ReadSinglePoint(ref all);

// 读指定 Port 的 8 根 Line
bool[] port0 = new bool[8];
diTask.ReadSinglePoint(ref port0, 0);

// 读指定 Port 指定 Line 的单个点
bool line3 = false;
diTask.ReadSinglePoint(ref line3, port: 0, line: 3);
```

### 5.3 停止与资源释放

```csharp
diTask.Stop();
diTask.Channels.Clear();   // 在 Task 重复使用场景（切换 Port 集合）前务必 Clear
```

## 6. 数字输出 DO

> JY7131 DO 共 32 路，同样 4 个 Port × 8 Line。每个 Port 可独立选择 Sourcing / Sinking / Push-Pull 输出模式。

### 6.1 任务构造与 Port 添加

```csharp
using JY7131;

var doTask = new JY7131DOTask(slotNumber: 0);

// 添加单个 Port，指定输出模式
doTask.AddChannel(0, DOTerminal.Push_Pull);

// 一次添加多个 Port，统一模式
doTask.AddChannel(new int[] { 0, 1, 2, 3 }, DOTerminal.Sourcing);

// 一次添加多个 Port，各 Port 不同模式
doTask.AddChannel(
    new int[]      { 0,                  1,                 2,                 3 },
    new DOTerminal[] { DOTerminal.Push_Pull, DOTerminal.Sourcing, DOTerminal.Sinking, DOTerminal.Sourcing }
);

// 运行时全局切换 Line 模式（影响已添加 Port）
doTask.DOLineMode = DOLineMode.Push_Pull;    // 枚举：Sourcing / Sinking / Push_Pull

doTask.Start();
```

### 6.2 静态写入

```csharp
// 写所有 Line（长度 = 32，按 Port0 Line0..7, Port1 Line0..7, ... 顺序）
bool[] all = new bool[32];
for (int i = 0; i < 32; i++) all[i] = (i % 2 == 0);
doTask.WriteSinglePoint(all);

// 写指定 Port 的 8 根 Line
bool[] port1 = { true, false, true, false, true, false, true, false };
doTask.WriteSinglePoint(port1, port: 1);

// 写指定 Port 指定 Line 的单点
doTask.WriteSinglePoint(writeValue: true, port: 0, line: 5);
```

### 6.3 停止与资源释放

```csharp
doTask.Stop();
doTask.Channels.Clear();
```

> **电气注意**：
> - Sourcing 模式下 DO=1 输出 V_DD，DO=0 呈高阻；
> - Sinking 模式下 DO=1 呈高阻，DO=0 输出 GND；
> - Push-Pull 模式下 DO=1 输出 V_DD、DO=0 输出 GND；
> - 用户必须提供 10~50 V_DC 的 V_DD 给 DO Bank 供电，否则 DO 无输出。

## 7. 计数器输入 CI

### 7.1 共用配置

```csharp
var ciTask = new JY7131CITask(slotNumber: 0, channel: 0);
ciTask.Type = CIType.EdgeCounting;   // 必须先定 Type，再配置子类
ciTask.Mode = CIMode.Single;         // Single / Finite / Continuous
```

支持的 `CIType`：

| 枚举 | 说明 | 配置子类 |
|---|---|---|
| `CIType.EdgeCounting` | 简单边沿计数 | `ciTask.EdgeCounting` |
| `CIType.Frequency` | 频率测量 | `ciTask.FrequencyMeas` |
| `CIType.Period` | 周期测量 | `ciTask.PeriodMeas` |
| `CIType.TwoEdgeSeparation` | 双边沿间隔 | `ciTask.TwoEdgeSeparation` |
| `CIType.QuadEncoder` | 正交编码器 | `ciTask.QuadEncoder` |
| `CIType.TwoPulseEncoder` | 双脉冲编码器 | `ciTask.TwoPulseEncoder` |

> **注意**：JY7131 **不支持** `Pulse` 与 `SemiPeriod` 两种测量类型。

`CIMode`：

| 枚举 | 读取方式 |
|---|---|
| `CIMode.Single` | `ReadSinglePoint(ref count)` 或 `ReadSinglePoint(ref value, timeout)` |
| `CIMode.Finite` / `CIMode.Continuous` | `ReadData(...)`、`AvailableSamples` |

### 7.2 Edge Counting（边沿计数）

```csharp
ciTask.Type = CIType.EdgeCounting;
ciTask.Mode = CIMode.Single;

ciTask.EdgeCounting.InitialCount            = 0;
ciTask.EdgeCounting.ActiveEdge              = EdgeType.Rising;             // Rising / Falling（Any 在边沿计数不可用）
ciTask.EdgeCounting.Direction               = CountDirection.Up;           // Up / Down / External
ciTask.EdgeCounting.Pause.ActivePolarity    = LevelPolarity.None;          // None / HighLevel / LowLevel
ciTask.EdgeCounting.OutEvent.Threshold      = 1000;
ciTask.EdgeCounting.OutEvent.Reset          = true;
ciTask.EdgeCounting.OutEvent.Terminal       = IOTerminal.PFI_Out_0;        // 达到阈值时 OutEvent 输出到 PFI_Out_0
ciTask.EdgeCounting.OutEvent.IdleState      = EdgeCntOunEvtIdleState.LowLevel;

ciTask.Start();

uint count = 0;
ciTask.ReadSinglePoint(ref count);
ciTask.Stop();
```

### 7.3 Frequency / Period 测量

```csharp
ciTask.Type = CIType.Frequency;   // 或 CIType.Period
ciTask.Mode = CIMode.Single;

// 选择测量时基，决定测量分辨率
ciTask.FrequencyMeas.Timebase.Source        = CITimebaseSource.Internal100MHz; // 100MHz / 5MHz / 100kHz
ciTask.FrequencyMeas.StartingEdge           = EdgeType.Rising;

ciTask.Start();

double freqHz = 0;
ciTask.ReadSinglePoint(ref freqHz, timeout: 3000);   // Hz；Period 模式返回秒
ciTask.Stop();
```

### 7.4 Two-Edge Separation（双边沿间隔）

```csharp
ciTask.Type = CIType.TwoEdgeSeparation;
ciTask.Mode = CIMode.Single;

ciTask.TwoEdgeSeparation.Timebase.Source       = CITimebaseSource.Internal100MHz;
ciTask.TwoEdgeSeparation.EdgeSeparationMode    = ValueReturnMode.TwoValue; // TwoValue / OneValue

ciTask.Start();
double t1 = 0, t2 = 0;
ciTask.ReadSinglePoint(ref t1, ref t2, timeout: 3000);
ciTask.Stop();
```

### 7.5 Quadrature Encoder / Two-Pulse Encoder

```csharp
// 正交编码器
ciTask.Type = CIType.QuadEncoder;
ciTask.Mode = CIMode.Single;
ciTask.QuadEncoder.EncodingType = QuadEncodingType.X4;   // X1 / X2 / X4
ciTask.QuadEncoder.ZReloadEnabled = true;                // 使能 Z 信号重载

ciTask.Start();
uint pos = 0;
ciTask.ReadSinglePoint(ref pos);
ciTask.Stop();

// 双脉冲编码器
ciTask.Type = CIType.TwoPulseEncoder;
ciTask.Start();
ciTask.ReadSinglePoint(ref pos);
ciTask.Stop();
```

### 7.6 采样时钟与触发

```csharp
// 采样时钟（仅 Finite / Continuous 模式相关）
ciTask.SampleClock.Source                   = CISampleClockSource.Internal; // Internal / External
ciTask.SampleClock.Internal.Rate            = 10_000;                       // Hz，内部分频
ciTask.SampleClock.External.Terminal        = ClockTerminal.PFI_In_0;
ciTask.SampleClock.External.ExpectedRate    = 10_000;

// 启动触发
ciTask.Trigger.Type                         = CITriggerType.Digital;        // Immediate / Digital / Soft
ciTask.Trigger.Digital.Source               = CIDigitalTriggerSource.PFI_In_1;
ciTask.Trigger.Digital.Edge                 = CIDigitalTriggerEdge.Rising;  // Rising / Falling
```

### 7.7 信号导出（CI SignalExport）

```csharp
// 将 CI StartTrig / SampleClock 导出到 PFI 或 PXI_Trig
ciTask.SignalExport.Add(CISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig0);
ciTask.SignalExport.Add(CISignalExportSource.SampleClock, SignalExportDestination.PFI_In_2);
// 清除
ciTask.SignalExport.Clear(SignalExportDestination.PFI_In_2);
ciTask.SignalExport.ClearAll();
```

## 8. 计数器输出 CO（脉冲输出）

### 8.1 基本配置与单点输出（Single）

```csharp
var coTask = new JY7131COTask(slotNumber: 0, channel: 0);

coTask.Mode                    = COMode.Single;                    // Single / Finite / ContinuousNoWrapping / ContinuousWrapping
coTask.OutputTerminal          = IOTerminal.PFI_Out_0;             // 脉冲物理输出管脚
coTask.OutTerminalMode         = DOLineMode.Push_Pull;             // 脉冲输出驱动模式
coTask.IdleState               = COIdleState.LowLevel;             // HighLevel / LowLevel
coTask.InitialDelay            = 0;                                // 秒；实际延迟 = InitialDelay + 5/timebase

// 时基
coTask.Timebase.Source                = COTimebaseSource.Internal100MHz; // 100MHz / 5MHz / 100kHz / External
// 外部时基：
coTask.Timebase.External.Terminal     = InputTerminal.PFI_In_0;
coTask.Timebase.External.Frequency    = 10_000_000;

// 触发
coTask.Trigger.Type                   = COTriggerType.Immediate;   // Immediate / Digital / Soft
// 若用数字触发：
// coTask.Trigger.Digital.Source = CODigitalTriggerSource.PFI_In_0;
// coTask.Trigger.Digital.Edge   = CODigitalTriggerEdge.Rising;

coTask.Start();
coTask.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 1_000 /* Hz */, 0.5 /* Duty */, 10 /* count；-1 无限 */));
coTask.WaitUntilDone(-1);
coTask.Stop();
```

`COPulseType` 含义：

| 枚举 | 参数 1 | 参数 2 |
|---|---|---|
| `COPulseType.DutyCycleFrequency` | 频率 Hz | 占空比（0~1） |
| `COPulseType.HighLowTime` | 高电平时长（秒） | 低电平时长（秒） |
| `COPulseType.HighLowTick` | 高电平时基 Tick 数 | 低电平时基 Tick 数 |

### 8.2 Finite / Continuous 多脉冲输出

```csharp
coTask.Mode = COMode.Finite;    // 或 COMode.ContinuousNoWrapping / ContinuousWrapping

var pulses = new COPulse[3]
{
    new COPulse(COPulseType.HighLowTime, 1e-3, 1e-3,  10),   // 500 Hz × 10
    new COPulse(COPulseType.HighLowTime, 5e-4, 5e-4,  20),   // 1 kHz × 20
    new COPulse(COPulseType.HighLowTime, 2e-4, 2e-4, -1),    // 2.5 kHz 无限
};
coTask.WriteData(pulses, samplesToWrite: pulses.Length, timeout: 3000);

coTask.Start();
// 查看进度
long sentSamples = coTask.TransferedSamples;
long sentPulses  = coTask.TransferedPulses;
coTask.WaitUntilDone(timeout: -1);
coTask.Stop();
```

### 8.3 CO 暂停触发与信号导出

```csharp
coTask.Pause.ActivePolarity = LevelPolarity.HighLevel;
coTask.Pause.Terminal       = InputTerminal.PFI_In_2;

// 导出 CO 输出到 PXI_Trig0
coTask.SignalExport.Add(COSignalExportSource.Output, SignalExportDestination.PXI_Trig0);
```

## 9. Device 级操作：参考时钟、PFI 滤波、DO 上电状态

```csharp
var device = new JY7131Device((ushort)0);

// 9.1 参考时钟（多卡同步必用）
device.ReferenceClock.Source            = ReferenceClockSource.External;       // Internal(25MHz) / External
device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
device.ReferenceClock.Commit();   // 仅当所有 Task 都 Stop 后才允许调用；参数错误会导致 PLL 无法锁定，必须断电重启

// 9.2 PFI 数字滤波器（对所有 Task 共享生效）
device.PFI.Filter.Enable(PFITerminal.PFI0, minimumPulseWidth: 100e-9);  // 单位秒；步进详见 XML
device.PFI.Filter.Disable(PFITerminal.PFI0);

// 9.3 DO 上电默认状态（断电再上电后生效）
int lineCount = device.DOLineCount;         // 应为 32
var terminals = new DOTerminal[lineCount];
var states    = new bool[lineCount];
for (int i = 0; i < lineCount; i++)
{
    terminals[i] = DOTerminal.Push_Pull;
    states[i]    = false;
}
device.SetPowerOn(terminals, states);       // 保存到板卡 Flash，下次通电后 DO Line 以该模式/电平启动
```

## 10. 多卡同步（PXIe 背板）

典型 1 主 N 从模式：

1. 所有板卡 `ReferenceClock.Source = External`，`External.Terminal = PXIe_Clk100`，各自调用 `device.ReferenceClock.Commit()`（**必须在所有 Task 尚未 Start 时**）。
2. 主卡将 StartTrig 导出到 `PXI_Trig0`：

```csharp
// 以 CI 为例（CO 同理，使用 COSignalExportSource.StartTrig）
masterCiTask.SignalExport.Add(CISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);
masterCiTask.Trigger.Type = CITriggerType.Immediate;
```

3. 从卡以 `PXI_Trig0` 为数字触发源：

```csharp
slaveCiTask.Trigger.Type         = CITriggerType.Digital;
slaveCiTask.Trigger.Digital.Source = CIDigitalTriggerSource.PXI_Trig0;
slaveCiTask.Trigger.Digital.Edge   = CIDigitalTriggerEdge.Rising;
```

4. **必须先 Start 所有 Slave，再 Start Master**。否则 Slave 会漏掉 Master 的触发脉冲。

5. 回收：先 Stop Master，再 Stop Slave。

## 11. 常见错误处理

```csharp
try
{
    ciTask.Start();
}
catch (JYDriverException ex)
{
    // ex.Message 含有错误码与说明
    // 典型错误码见 JYDriverExceptionPublic 枚举：ParameterError / DeviceNotFound /
    // TaskNotStarted / TaskAlreadyStarted / Timeout / RefClockPLLUnlocked 等
    Console.WriteLine($"[JY7131] {ex.Message}");
}
```

**常见问题与排查**：

| 症状 | 可能原因 | 处理 |
|---|---|---|
| `Commit` 后板卡不响应，需要断电重启 | ReferenceClock 参数错误导致 PLL 无法锁定 | `Commit` 前务必确认 External.Terminal 与机箱实际背板信号一致 |
| DO 输出始终为 0 / 高阻 | V_DD 未接外部供电 | 按 10~50 V_DC 接入用户外部电源到 DO Bank |
| CI 读到恒定 0 或不变 | Type 与子配置类不匹配（如 Type=Frequency 却配置了 EdgeCounting.*） | 先确定 `Type`，只操作对应配置类 |
| CI OutEvent 无输出 | `Terminal=IOTerminal.None` 或物理管脚未接通 | 设为 `PFI_Out_x` 且确认外部接线 |
| 多卡同步 Slave 未触发 | Master 先于 Slave Start | 先 Start Slave，再 Start Master |
| `JY7131DOTask` 复用时 Port 冲突 | 上次 Stop 后没有 `Channels.Clear()` | Stop 后立即 `Channels.Clear()` |

## 12. 完整 API 参考

### 12.1 类

| 类 | 说明 |
|---|---|
| `JY7131Device` | 板卡级：ReferenceClock、PFI、DO 上电状态、DOLineCount |
| `JY7131CITask` | 计数器输入任务（通道级） |
| `JY7131COTask` | 计数器输出任务（通道级） |
| `JY7131DITask` | 数字输入任务（板卡级，Port 粒度） |
| `JY7131DOTask` | 数字输出任务（板卡级，Port 粒度） |
| `JYDriverException` | 驱动异常 |

### 12.2 关键枚举

| 枚举 | 取值 |
|---|---|
| `PowerOnState` | Tristate / High / Low |
| `ReferenceClockSource` | Internal（25 MHz）/ External |
| `ExternalReferenceClockTerminal` | PXIe_Clk100 |
| `PFITerminal` | PFI0 ~ PFI31 |
| `IOTerminal` | None / PFI_Out_0~31 / PXI_Trig0~7（用作 CI OutEvent 输出、CO OutputTerminal） |
| `InputTerminal` | PFI_In_0~31 / PXI_Trig0~7 / CI_0_OutPut~CI_7_OutPut / Timebase_100MHz / Timebase_5MHz / Timebase_100kHz（用作 CO Timebase 外部源 / CO Pause Terminal） |
| `ClockTerminal` | PFI_In_0~31 / PXI_Trig0~7 / CI0_SampleClock~CI7_SampleClock / CO0_Output~CO7_Output（用作 CI 外部采样时钟源） |
| `SignalExportDestination` | PFI_In_0~31 / PXI_Trig0~7 |
| `CISignalExportSource` | StartTrig / SampleClock |
| `COSignalExportSource` | StartTrig / Output |
| `CIType` | EdgeCounting / Period / Frequency / TwoEdgeSeparation / QuadEncoder / TwoPulseEncoder |
| `CIMode` | Single / Finite / Continuous |
| `CISampleClockSource` | Internal / External |
| `CITimebaseSource` | Internal100MHz / Internal5MHz / Internal100kHz |
| `CITriggerType` | Immediate / Digital / Soft |
| `CIDigitalTriggerSource` | PFI_In_0~31 / PXI_Trig0~7 / CIO_0~7_StartTrig / CO_0~7_OutPut |
| `CIDigitalTriggerEdge` | Rising / Falling |
| `EdgeType` | Rising / Falling / Any |
| `CountDirection` | Up / Down / External |
| `LevelPolarity` | LowLevel / HighLevel / None |
| `EdgeCntOunEvtIdleState` | LowLevel / HighLevel |
| `ValueReturnMode` | TwoValue / OneValue |
| `QuadEncodingType` | X1 / X2 / X4 |
| `COMode` | Single / Finite / ContinuousNoWrapping / ContinuousWrapping |
| `COIdleState` | HighLevel / LowLevel |
| `COPulseType` | DutyCycleFrequency / HighLowTime / HighLowTick |
| `COTimebaseSource` | Internal100MHz / Internal5MHz / Internal100kHz / External |
| `COTriggerType` | Immediate / Digital / Soft |
| `CODigitalTriggerSource` | PFI_In_0~31 / PXI_Trig0~7 / CIO_0~7_StartTrig / CO_0~7_OutPut |
| `CODigitalTriggerEdge` | Rising / Falling |
| `DOTerminal` | Sourcing / Sinking / Push_Pull |
| `DOLineMode` | Sourcing / Sinking / Push_Pull |

### 12.3 JY7131CITask 主要成员

```csharp
// 构造
new JY7131CITask(int slotNum, int channel);   // channel: 0~7
new JY7131CITask(string boardName, int channel);

// 属性
CIType          Type;
CIMode          Mode;                 // Single / Finite / Continuous
int             SamplesToAcquire;    // Finite 模式下期望采集点数
int             AvailableSamples;    // 当前可用样本数
long            TransferedSamples;   // 累计已传输样本数
EdgeCounting    EdgeCounting;
FrequencyMeas   FrequencyMeas;
PeriodMeas      PeriodMeas;
TwoEdgeSeparation TwoEdgeSeparation;
QuadEncoder     QuadEncoder;
TwoPulseEncoder TwoPulseEncoder;
CISampleClock   SampleClock;
CITrigger       Trigger;
CISignalExport  SignalExport;
JY7131Device    Device;

// 方法
void Start();
void Stop();
void SendSoftwareTrigger();
// 单点读取（Single 模式）
void ReadSinglePoint(ref uint count);                              // EdgeCounting / QuadEncoder / TwoPulseEncoder
void ReadSinglePoint(ref double value, int timeout);               // Frequency / Period
void ReadSinglePoint(ref double v1, ref double v2, int timeout);   // TwoEdgeSeparation
// 批量读取（Finite / Continuous 模式）
void ReadData(ref uint[]   data, int samplesToRead, int timeout);  // EdgeCounting / QuadEncoder / TwoPulseEncoder
void ReadData(ref uint[]   data, int samplesToRead);
void ReadData(ref double[] data, int samplesToRead, int timeout);  // Frequency / Period
void ReadData(ref double[] data, int samplesToRead);
void ReadData(ref double[] d1, ref double[] d2, int samplesToRead, int timeout); // TwoEdgeSeparation
void ReadData(ref double[] d1, ref double[] d2, int samplesToRead);
```

### 12.4 JY7131COTask 主要成员

```csharp
new JY7131COTask(int slotNum, int channel);
new JY7131COTask(string boardName, int channel);

// 属性
COMode        Mode;
IOTerminal    OutputTerminal;
DOLineMode    OutTerminalMode;
COTimebase    Timebase;
double        InitialDelay;
COIdleState   IdleState;
PauseTrigger  Pause;
int           SamplesToUpdate;
int           AvaliableLenInSamples;
long          TransferedSamples;
long          TransferedPulses;
COTrigger     Trigger;
COSignalExport SignalExport;
JY7131Device  Device;

// 方法
void WriteSinglePoint(COPulse pulse);                             // Mode == Single
void WriteData(COPulse[] pulses, int samplesToWrite, int timeout);// Mode != Single
void SendSoftwareTrigger();
void Start();
void Stop();
void WaitUntilDone(int timeout);
```

### 12.5 JY7131DITask / JY7131DOTask 主要成员

```csharp
// DI
new JY7131DITask(int slotNum);
new JY7131DITask(string aliasName);

JY7131Device     Device;
IList<DIChannel> Channels;

void AddChannel(int portNum);
void AddChannel(int[] portsNum);
void RemoveChannel(int portNum);
void Start();
void Stop();
void ReadSinglePoint(ref bool value, int port, int line);
void ReadSinglePoint(ref bool[] readValues);                       // 全部 Port
void ReadSinglePoint(ref bool[] readValues, int port);             // 指定 Port
void ConfigChannels();                                             // 手动提交通道配置（通常不需显式调用）

// DO
new JY7131DOTask(int slotNum);
new JY7131DOTask(string aliasName);

JY7131Device     Device;
DOLineMode       DOLineMode;
IList<DOChannel> Channels;

void AddChannel(int portNum, DOTerminal terminal);
void AddChannel(int[] portsNum, DOTerminal terminal);
void AddChannel(int[] portsNum, DOTerminal[] terminals);
void RemoveChannel(int portNum);
void Start();
void Stop();
void WriteSinglePoint(bool value, int port, int line);
void WriteSinglePoint(bool[] writeValues);                          // 全部 Port
void WriteSinglePoint(bool[] writeValues, int port);                // 指定 Port
```

### 12.6 JY7131Device 主要成员

```csharp
new JY7131Device(ushort cardNum);
new JY7131Device(string aliasName);
static JY7131Device GetInstance(ushort cardNum);      // 保证每卡只一个注册实例
static JY7131Device GetInstance(string aliasName);

int             BoardClockRate;
string          SerialNumber;
int             DIOPortCount;                         // DI/DO 共用的 Port 计数 = 4
int             DILineCount;                          // = 32
int             DOLineCount;                          // = 32
ReferenceClock  ReferenceClock;
PFISetting      PFI;
int             DeviceID;

void SetPowerOn(DOTerminal[] terminals, bool[] states);
Terminal GetDefautTerminal(CounterInterface counterInterface, uint channel); // 查询指定通道 Source/Aux/Gate/Out 的默认物理端子
void Release();
```

## 13. 完整代码示例

### 13.1 DI 32 路轮询 + DO 32 路 Push-Pull 回写

```csharp
using System;
using System.Threading;
using JY7131;

class DemoDioLoopback
{
    static void Main()
    {
        int slot = 0;

        var di = new JY7131DITask(slot);
        di.AddChannel(new[] { 0, 1, 2, 3 });

        var doTask = new JY7131DOTask(slot);
        doTask.AddChannel(new[] { 0, 1, 2, 3 }, DOTerminal.Push_Pull);

        try
        {
            di.Start();
            doTask.Start();

            bool[] buf = new bool[32];
            for (int i = 0; i < 5; i++)
            {
                di.ReadSinglePoint(ref buf);
                // 将 DI 读到的 32 bit 原样回写 DO
                doTask.WriteSinglePoint(buf);
                Thread.Sleep(100);
            }
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine("JY7131 error: " + ex.Message);
        }
        finally
        {
            di.Stop(); di.Channels.Clear();
            doTask.Stop(); doTask.Channels.Clear();
        }
    }
}
```

### 13.2 CI 连续边沿计数 + OutEvent 输出到 PFI_Out_0

```csharp
using JY7131;

var ci = new JY7131CITask(slotNumber: 0, channel: 0);
ci.Type = CIType.EdgeCounting;
ci.Mode = CIMode.Continuous;

ci.EdgeCounting.InitialCount             = 0;
ci.EdgeCounting.ActiveEdge               = EdgeType.Rising;
ci.EdgeCounting.Direction                = CountDirection.Up;
ci.EdgeCounting.OutEvent.Threshold       = 1000;
ci.EdgeCounting.OutEvent.Reset           = true;
ci.EdgeCounting.OutEvent.Terminal        = IOTerminal.PFI_Out_0;
ci.EdgeCounting.OutEvent.IdleState       = EdgeCntOunEvtIdleState.LowLevel;

ci.SampleClock.Source                    = CISampleClockSource.Internal;
ci.SampleClock.Internal.Rate             = 10_000;

ci.Start();
// 在另外线程循环 ReadData(...) 消费数据，略
// ci.Stop();
```

### 13.3 CO 连续 PWM（50 % 占空比 1 kHz）

```csharp
using JY7131;

var co = new JY7131COTask(slotNumber: 0, channel: 0);
co.Mode                = COMode.Single;          // 单点写入后持续输出，直到 Stop
co.OutputTerminal      = IOTerminal.PFI_Out_1;
co.OutTerminalMode     = DOLineMode.Push_Pull;
co.IdleState           = COIdleState.LowLevel;
co.Timebase.Source     = COTimebaseSource.Internal100MHz;
co.Trigger.Type        = COTriggerType.Immediate;

co.Start();
co.WriteSinglePoint(new COPulse(COPulseType.DutyCycleFrequency, 1_000, 0.5, count: -1));
// ... 运行一段时间后
co.Stop();
```

### 13.4 设置 DO 上电默认状态为 Push-Pull 低电平

```csharp
var dev = new JY7131Device((ushort)0);
int n = dev.DOLineCount;  // 32
var t = new DOTerminal[n];
var s = new bool[n];
for (int i = 0; i < n; i++) { t[i] = DOTerminal.Push_Pull; s[i] = false; }
dev.SetPowerOn(t, s);
// 断电 / 重启后，DO 32 路以 Push-Pull 低电平启动
```

## 14. 注意事项速记

1. **高压模块**：DO 侧必须为 V_DD 接 10 ~ 50 V_DC 外部电源；DI 逻辑高电平至少 ±10 V，低于此阈值读数无定义。请按 ±60 V_DC / 2500 V_rms（DI 侧）/ 3000 V_rms（DO 侧）的隔离等级规划系统布线。
2. **Port-based DIO**：`AddChannel` 粒度为 Port（8 根线）。
3. **DO 输出模式按 Port 配置**：Sourcing / Sinking / Push-Pull 通过 `DOTerminal` 逐 Port 决定；同一 Task 内可混用。
4. **CI 类型无 Pulse / SemiPeriod**：JY7131 可用 `TwoEdgeSeparation` 变通测量脉宽。
5. **Type 决定配置类**：`ciTask.Type = ...` 之后只操作对应子类；切换 Type 时建议重新构造 Task。
6. **`ReferenceClock.Commit()` 仅在所有 Task Stop 时允许**；参数错误会导致 PLL 锁不住，需要断电重启。
7. **多卡同步顺序**：先 Slave 后 Master Start；Stop 顺序相反。
8. **端子枚举区分**：
   - CI 采样时钟外部输入 → `ClockTerminal`
   - CO 外部时基 / Pause / EdgeCounting InputTerminal → `InputTerminal`
   - CI OutEvent / CO Output 物理输出 → `IOTerminal`
   - 信号导出目的地 → `SignalExportDestination`
9. **Task 重用**：DI/DO Stop 后务必 `Channels.Clear()`，否则再次 `AddChannel` 会累积端口。
10. **SetPowerOn 生效时机**：参数被写入板卡 Flash，**下次通电**后 DO 才按该状态启动；对当前会话的 DO 输出无直接影响。
11. **PFI 物理共用**：`PFI_In_x` 与 `PFI_Out_x` 是同一物理管脚的不同方向视图；用作 OutEvent / CO Output 时请确保外部电路允许输出方向。
12. **异常分层**：所有驱动层错误统一是 `JY7131.JYDriverException`；建议在每个 `Start` / `Read` / `Write` / `Commit` 附近单独 `try-catch`，方便定位。
