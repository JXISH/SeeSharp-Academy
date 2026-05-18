---
name: JY7133 Driver Skill
description: 简仪 JY7133 系列高压隔离型多功能数字输入 + 计数器板卡 C# 驱动使用规范。当用户需要使用 JY7133（PXIe-7133）进行 64 路 ±60 V_DC 隔离 DI、或 8 通道计数器/定时器（边沿计数 / 频率 / 周期 / 双边沿间隔 / 正交编码 / 双脉冲编码）测量、或进行多卡同步时必须加载本技能。
---

# JY7133 驱动使用指南

> 本文档覆盖 JY7133 系列板卡的 C# 驱动（命名空间 `JY7133`）。该系列为**高压隔离型工业数字输入 + 计数器/定时器**板卡：64 路隔离 DI（±60 V_DC）+ 8 路 32 bit 计数器（最高内部时钟 100 MHz，CI 输入频率 10 kHz）。
>
> **功能范围**：JY7133 **仅有 DI 与 CI**，**不含 DO、不含 CO、不含 DO 上电状态配置**。DI 为 Port-based（8 Port × 8 Line = 64 路）；CI 不支持 `Pulse` 与 `SemiPeriod` 测量类型。

## 1. 环境要求

| 项 | 说明 |
|---|---|
| 驱动 DLL | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY7133\Bin\JY7133.dll` |
| XML 文档 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY7133\Bin\JY7133.xml` |
| 范例代码 | `D:\JYTEK_Work\Examples\C#\DAQ\JY7133.Example` |
| .NET Framework | 4.0 及以上 |
| 命名空间 | `using JY7133;` |
| 可选 GUI 套件 | `SeeSharpTools.JY.GUI`（范例中使用的图表/LED 控件） |
| 异常类型 | `JY7133.JYDriverException`（所有驱动层错误都以此抛出） |

## 2. 硬件规格速查

> 数据来源：简仪产品型录 PXIe-7133 章节 + 驱动 XML。

### 2.1 隔离数字输入（DI）

| 项 | 规格 |
|---|---|
| 通道数 | 64（8 Port × 8 Line，`Port 0 ~ Port 7`） |
| 接地参考 | GND |
| 最大输入电压 | ±60 V_DC |
| 逻辑低电平 V_IL | 0 ~ ±1 V_DC |
| 逻辑高电平 V_IH | ±2 ~ ±60 V_DC |
| 输入类型 | Sourcing / Sinking |
| 共模隔离（Bank to Bank） | 60 V_DC |
| 输入电流（典型） | ±2 V 输入：±1.27 mA；±60 V 输入：±3.43 mA |

> **DI 与计数器共用 64 路 PFI 物理管脚**：PDF 标注 "64 路 DI **或者** 8 路计数器"，实际使用时计数器输入信号（Source/Aux/Gate、Trigger、外部采样时钟）也从 `PFI_In_0 ~ PFI_In_63` 选取。在同一物理管脚上只能选择一种用途。

### 2.2 计数器 / 定时器（8 通道 0~7）

| 项 | 规格 |
|---|---|
| 通道数 | 8（Counter 0 ~ Counter 7） |
| 分辨率 | 32 bits |
| 内部时钟频率 | 100 MHz |
| 工作电压 | 最高 60 V |
| CI 测量类型 | Edge Counting / Period / Frequency / Two-Edge Separation / Quadrature Encoder（×1/×2/×4）/ Two-Pulse Encoder |
| CI 输入频率 | 最高 **10 kHz**（高电平 ≤ 70 μs） |
| CI 时基 | 内部 100 MHz / 5 MHz / 100 kHz |
| CI 输入信号 | Source(A) / Aux(B) / Gate(Z) / External Digital Trigger |
| 计数器输出功能 | **无**（JY7133 不提供 CO） |

> **CI 输入频率较低（10 kHz）**：高压隔离输入电路限制了最高测量频率，适合低频工业场景（电机转速、阀门开关状态、流量计等）。

### 2.3 PFI 与总线

| 项 | 规格 |
|---|---|
| PFI 输入枚举 | `PFI_In_0 ~ PFI_In_63`（64 根） |
| PFI 输出能力 | **无**（`IOTerminal` 仅含 `PXI_Trig0~7` 与 `None`，不含 `PFI_Out_x`） |
| PFITerminal（Filter 配置） | `PFI0 ~ PFI63` |
| 参考时钟内部源 | 25 MHz（板载 TCXO） |
| 参考时钟外部源 | `PXIe_Clk100`（PXIe 背板） |
| 总线接口 | PXI Express 外设模块 |
| 背板触发线 | `PXI_Trig0 ~ PXI_Trig7`（用作模块间触发 / CI StartTrig 导出） |
| 附件 | ACL-1020100-1（1 m 100 pin SCSI 双绞线）/ -2（2 m）/ DIN-100-1（100 pin SCSI 接线端子） |

## 3. 产品型号

| 型号 | 总线 | 说明 |
|---|---|---|
| PXIe-7133 | PXI Express | 64 通道 ±60 V_DC Sink/Source 输入 + 8 计数器，Bank 隔离数字 IO 模块 |

构造器接受 **槽位号**（从 0 开始，按系统枚举顺序）或 **板卡别名**（在 JYTEK Measurement & Automation Explorer 中配置）。

## 4. 通用编程范式

JY7133 驱动按 **功能类型** 划分 Task：

- `JY7133CITask`：计数器输入（通道级，每 Task 1 个通道 0~7）
- `JY7133DITask`：数字输入（板卡级，`AddChannel(portNum)` 添加 Port）
- `JY7133Device`：板卡级设备对象，用于配置参考时钟、PFI 滤波

> **无** `JY7133COTask`、**无** `JY7133DOTask`、**无** `SetPowerOn`。

生命周期：

```
new TaskXxx(slot/name, [channel])
    → 配置属性 (Type / Mode / SampleClock / Trigger / SignalExport …)
    → [可选] Device.ReferenceClock 配置 + Commit（多卡同步时）
    → Start()
    → ReadXxx
    → Stop()
    → Channels.Clear()（DI Task 复用时）
```

**关键规则**

1. **CI 是通道级 Task**：构造时必须指定 `channel (0~7)`，每通道一个 Task。
2. **DI 是 Port 级 Task**：构造只传 slot/name；用 `AddChannel(portNum)` 添加要用的 Port（**0~7**，共 8 个 Port）。添加粒度是 Port（8 根线一组），不是单根线。
3. **属性修改必须在 `Start()` 之前完成**。
4. **CI 的测量配置是分离的配置类**：先设 `ciTask.Type = CIType.Xxx`，再操作对应子配置类（`ciTask.EdgeCounting.*` / `ciTask.FrequencyMeas.*` 等）。
5. **CI OutEvent / StartTrig 的 Terminal 使用 `IOTerminal`**：JY7133 的 `IOTerminal` 只包含 `PXI_Trig0~7` 与 `None`，**不能输出到 PFI**。这意味着 CI 的 OutEvent、SignalExport 只能导出到 PXIe 背板触发线。
6. **异常捕获**：所有调用均可能抛 `JY7133.JYDriverException`。

### 4.1 Task 构造模板

```csharp
using JY7133;

// CI：按槽位号 + 通道号
var ciTask = new JY7133CITask(slotNumber: 0, channel: 0);

// DI：按槽位号
var diTask = new JY7133DITask(slotNumber: 0);

// Device：用于参考时钟 / PFI 滤波
var device = new JY7133Device((ushort)0);

// 也可按 MAX 中配置的别名
var ciTask2 = new JY7133CITask("JY7133_0", 0);
```

## 5. 数字输入 DI

> JY7133 DI 共 64 路，分为 8 个 Port（Port 0~7），每 Port 8 根 Line。读写均以 `bool[]` 形式。

### 5.1 任务构造与 Port 添加

```csharp
using JY7133;

var diTask = new JY7133DITask(slotNumber: 0);

// 添加单个 Port
diTask.AddChannel(0);                    // 添加 Port 0

// 一次添加多个 Port（全部 8 个）
diTask.AddChannel(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 });

// 删除某个 Port
diTask.RemoveChannel(2);
```

### 5.2 静态读取

```csharp
diTask.Start();

// 读指定 Port 的 8 根 Line
bool[] port0 = new bool[8];
diTask.ReadSinglePoint(ref port0, 0);

// 读全部 Port 的所有 Line（长度 = 64）
bool[] all = new bool[64];
diTask.ReadSinglePoint(ref all);

// 读指定 Port 指定 Line 的单个点
bool line3 = false;
diTask.ReadSinglePoint(ref line3, port: 0, line: 3);
```

### 5.3 停止与资源释放

```csharp
diTask.Stop();
diTask.Channels.Clear();   // 在 Task 重复使用场景（切换 Port 集合）前务必 Clear
```

## 6. 计数器输入 CI

### 6.1 共用配置

```csharp
var ciTask = new JY7133CITask(slotNumber: 0, channel: 0);
ciTask.Type = CIType.EdgeCounting;   // 必须先定 Type，再配置子类
ciTask.Mode = CIMode.Single;         // 目前 XML 中 CIMode 仅 Single（以驱动为准）
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

> **不支持** `Pulse` 与 `SemiPeriod` 两种测量类型。

### 6.2 Edge Counting（边沿计数）

```csharp
ciTask.Type = CIType.EdgeCounting;
ciTask.Mode = CIMode.Single;

ciTask.EdgeCounting.InitialCount             = 0;
ciTask.EdgeCounting.ActiveEdge               = EdgeType.Rising;             // Rising / Falling
ciTask.EdgeCounting.Direction                = CountDirection.Up;           // Up / Down / External
ciTask.EdgeCounting.Pause.ActivePolarity     = LevelPolarity.None;          // None / HighLevel / LowLevel
ciTask.EdgeCounting.OutEvent.Threshold       = 1000;
ciTask.EdgeCounting.OutEvent.Reset           = true;
ciTask.EdgeCounting.OutEvent.Terminal        = IOTerminal.PXI_Trig0;        // JY7133 只能导出到 PXI_Trig
ciTask.EdgeCounting.OutEvent.IdleState       = EdgeCntOunEvtIdleState.HighLevel;

// 可选：改变 Source/Aux 的 PFI 输入端子
ciTask.EdgeCounting.InputTerminal            = InputTerminal.PFI_In_0;
// 当 Direction = External 时，方向控制线通过 DirTerminal 指定
// ciTask.EdgeCounting.DirTerminal           = InputTerminal.PFI_In_1;

ciTask.Start();

uint count = 0;
ciTask.ReadSinglePoint(ref count);
ciTask.Stop();
```

### 6.3 Frequency / Period 测量

```csharp
ciTask.Type = CIType.Frequency;   // 或 CIType.Period
ciTask.Mode = CIMode.Single;

ciTask.FrequencyMeas.Timebase.Source        = CITimebaseSource.Internal100MHz; // 100MHz / 5MHz / 100kHz
ciTask.FrequencyMeas.StartingEdge           = EdgeType.Rising;
ciTask.FrequencyMeas.InputTerminal          = InputTerminal.PFI_In_0;

ciTask.Start();

double valueHz = 0;
ciTask.ReadSinglePoint(ref valueHz, timeout: 3000);   // Frequency 返回 Hz；Period 返回秒
ciTask.Stop();
```

### 6.4 Two-Edge Separation（双边沿间隔）

```csharp
ciTask.Type = CIType.TwoEdgeSeparation;
ciTask.Mode = CIMode.Single;

ciTask.TwoEdgeSeparation.Timebase.Source        = CITimebaseSource.Internal100MHz;
ciTask.TwoEdgeSeparation.FirstInputTerminal     = InputTerminal.PFI_In_0;
ciTask.TwoEdgeSeparation.SecondInputTerminal    = InputTerminal.PFI_In_1;
ciTask.TwoEdgeSeparation.EdgeSeparationMode     = ValueReturnMode.TwoValue;   // TwoValue / OneValue

ciTask.Start();
double t1 = 0, t2 = 0;
ciTask.ReadSinglePoint(ref t1, ref t2, timeout: 3000);
ciTask.Stop();
```

### 6.5 Quadrature Encoder / Two-Pulse Encoder

```csharp
// 正交编码器
ciTask.Type = CIType.QuadEncoder;
ciTask.Mode = CIMode.Single;
ciTask.QuadEncoder.EncodingType     = QuadEncodingType.X4;      // X1 / X2 / X4
ciTask.QuadEncoder.ZReloadEnabled   = true;                     // 使能 Z 信号重载
ciTask.QuadEncoder.AInputTerminal   = InputTerminal.PFI_In_0;
ciTask.QuadEncoder.BInputTerminal   = InputTerminal.PFI_In_1;
ciTask.QuadEncoder.ZInputTerminal   = InputTerminal.PFI_In_2;

ciTask.Start();
uint pos = 0;
ciTask.ReadSinglePoint(ref pos);
ciTask.Stop();

// 双脉冲编码器
ciTask.Type = CIType.TwoPulseEncoder;
ciTask.TwoPulseEncoder.AInputTerminal = InputTerminal.PFI_In_0;
ciTask.TwoPulseEncoder.BInputTerminal = InputTerminal.PFI_In_1;
ciTask.Start();
ciTask.ReadSinglePoint(ref pos);
ciTask.Stop();
```

### 6.6 触发

```csharp
ciTask.Trigger.Type                    = CITriggerType.Digital;         // Immediate / Digital / Soft
ciTask.Trigger.Digital.Source          = CIDigitalTriggerSource.PFI_In_1;
ciTask.Trigger.Digital.Edge            = CIDigitalTriggerEdge.Rising;   // Rising / Falling
```

`CIDigitalTriggerSource` 可选值覆盖：`PFI_In_0 ~ PFI_In_63`、`PXI_Trig0 ~ PXI_Trig7`、`CI_0_StartTrig ~ CI_7_StartTrig`。

### 6.7 信号导出（CI SignalExport）

```csharp
// JY7133 仅支持把 StartTrig 导出到 PXI_Trig0~7（SignalExportDestination 只含 PXI_Trig0~7）
ciTask.SignalExport.Add(CISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

// 清除
ciTask.SignalExport.Clear(SignalExportDestination.PXI_Trig0);
ciTask.SignalExport.ClearAll();
```

## 7. Device 级操作：参考时钟、PFI 滤波

```csharp
var device = new JY7133Device((ushort)0);

// 7.1 参考时钟（多卡同步必用）
device.ReferenceClock.Source            = ReferenceClockSource.External;       // Internal(25MHz) / External
device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
device.ReferenceClock.Commit();  // 仅当所有 Task 都 Stop 后才允许调用；参数错误会导致 PLL 无法锁定，必须断电重启

// 7.2 PFI 数字滤波器（对所有 Task 共享生效）
device.PFI.Filter.Enable(PFITerminal.PFI0, minimumPulseWidth: 100e-9);
device.PFI.Filter.Disable(PFITerminal.PFI0);

// 7.3 其他
int diPortCount = device.DIPortCount;    // 8
int diLineCount = device.DILineCount;    // 64
```

> **JY7133 没有 DO，因此也没有 `DOLineCount` / `DOPortCount` / `SetPowerOn`。**

## 8. 多卡同步（PXIe 背板）

典型 1 主 N 从模式：

1. 所有板卡 `ReferenceClock.Source = External`，`External.Terminal = PXIe_Clk100`，各自调用 `device.ReferenceClock.Commit()`（**必须在所有 Task 尚未 Start 时**）。
2. 主卡将 StartTrig 导出到 `PXI_Trig0`：

```csharp
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

## 9. 常见错误处理

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
    Console.WriteLine($"[JY7133] {ex.Message}");
}
```

**常见问题与排查**：

| 症状 | 可能原因 | 处理 |
|---|---|---|
| `Commit` 后板卡不响应 | ReferenceClock 参数错误导致 PLL 无法锁定 | `Commit` 前务必确认 External.Terminal 与机箱实际背板信号一致；出现问题需断电重启 |
| DI 读数全 0 | 输入电压低于 ±2 V（未进入 VIH） | 检查外部信号幅值是否超过 ±2 V_DC；确认 Sourcing/Sinking 接线方向 |
| CI 读到恒定值或不变 | Type 与子配置类不匹配 | 先确定 `Type`，只操作对应配置类 |
| CI 测量值异常偏大 / 偏小 | 输入信号频率超过 10 kHz 或高电平宽度 < 70 μs | JY7133 的 CI 最高输入频率 10 kHz，请确认输入信号频率及占空比在规格内 |
| CI OutEvent 没有输出到 PFI | JY7133 不支持 PFI 输出，只能导出到 PXI_Trig | 将 `OutEvent.Terminal` 改为 `IOTerminal.PXI_Trig0~7` |
| 多卡同步 Slave 未触发 | Master 先于 Slave Start | 先 Start Slave，再 Start Master |
| `JY7133DITask` 复用时 Port 冲突 | 上次 Stop 后没有 `Channels.Clear()` | Stop 后立即 `Channels.Clear()` |

## 10. 完整 API 参考

### 10.1 类

| 类 | 说明 |
|---|---|
| `JY7133Device` | 板卡级：ReferenceClock、PFI、DIPortCount、DILineCount |
| `JY7133CITask` | 计数器输入任务（通道级） |
| `JY7133DITask` | 数字输入任务（板卡级，Port 粒度） |
| `JYDriverException` | 驱动异常 |

> **不存在** `JY7133DOTask`、`JY7133COTask`。

### 10.2 关键枚举

| 枚举 | 取值 |
|---|---|
| `PowerOnState` | Tristate / High / Low（XML 定义，实际 JY7133 无 DO 因此该枚举无用途） |
| `ReferenceClockSource` | Internal（25 MHz）/ External |
| `ExternalReferenceClockTerminal` | PXIe_Clk100 |
| `PFITerminal` | PFI0 ~ PFI63 |
| `IOTerminal` | None / PXI_Trig0~7（**无** PFI_Out_x） |
| `InputTerminal` | PFI_In_0~63 / PXI_Trig0~7 / Timebase_100MHz / Timebase_5MHz / Timebase_100kHz |
| `SignalExportDestination` | PXI_Trig0~7（**无** PFI） |
| `CISignalExportSource` | StartTrig |
| `CIType` | EdgeCounting / Period / Frequency / TwoEdgeSeparation / QuadEncoder / TwoPulseEncoder |
| `CIMode` | Single |
| `CITimebaseSource` | Internal100MHz / Internal5MHz / Internal100kHz |
| `CITriggerType` | Immediate / Digital / Soft |
| `CIDigitalTriggerSource` | PFI_In_0~63 / PXI_Trig0~7 / CI_0~7_StartTrig |
| `CIDigitalTriggerEdge` | Rising / Falling |
| `EdgeType` | Rising / Falling / Any |
| `CountDirection` | Up / Down / External |
| `LevelPolarity` | LowLevel / HighLevel / None |
| `EdgeCntOunEvtIdleState` | LowLevel / HighLevel |
| `ValueReturnMode` | TwoValue / OneValue |
| `QuadEncodingType` | X1 / X2 / X4 |

### 10.3 JY7133CITask 主要成员

```csharp
// 构造
new JY7133CITask(int slotNum, int channel);   // channel: 0~7
new JY7133CITask(string boardName, int channel);

// 属性
CIType            Type;
CIMode            Mode;
EdgeCounting      EdgeCounting;
FrequencyMeas     FrequencyMeas;
PeriodMeas        PeriodMeas;
TwoEdgeSeparation TwoEdgeSeparation;
QuadEncoder       QuadEncoder;
TwoPulseEncoder   TwoPulseEncoder;
CITrigger         Trigger;
CISignalExport    SignalExport;
JY7133Device      Device;

// 方法
void Start();
void Stop();
void SendSoftwareTrigger();
void ReadSinglePoint(ref uint count);                            // EdgeCounting / QuadEncoder / TwoPulseEncoder
void ReadSinglePoint(ref double value, int timeout);             // Frequency / Period
void ReadSinglePoint(ref double v1, ref double v2, int timeout); // TwoEdgeSeparation
```

### 10.4 JY7133DITask 主要成员

```csharp
new JY7133DITask(int slotNum);
new JY7133DITask(string aliasName);

IList<DIChannel> Channels;
JY7133Device Device;

void AddChannel(int portNum);
void AddChannel(int[] portsNum);
void RemoveChannel(int portNum);
void Start();
void Stop();
void ReadSinglePoint(ref bool value, int port, int line);
void ReadSinglePoint(ref bool[] readValues);                     // 全部 Port
void ReadSinglePoint(ref bool[] readValues, int port);           // 指定 Port
void ConfigChannels();                                           // 手动提交通道配置（通常不需显式调用）
```

### 10.5 JY7133Device 主要成员

```csharp
new JY7133Device(ushort cardNum);
new JY7133Device(string aliasName);
static JY7133Device GetInstance(ushort cardNum);
static JY7133Device GetInstance(string aliasName);

int             BoardClockRate;
string          SerialNumber;
int             DIPortCount;     // 8
int             DILineCount;     // 64
ReferenceClock  ReferenceClock;
PFISetting      PFI;
int             DeviceID;

Terminal GetDefautTerminal(CounterInterface counterInterface, uint channel); // 查询指定通道 Source/Aux/Gate 的默认物理端子
void Release();
```

## 11. 完整代码示例

### 11.1 DI 64 路轮询

```csharp
using System;
using System.Threading;
using JY7133;

class DemoDiScan
{
    static void Main()
    {
        int slot = 0;

        var di = new JY7133DITask(slot);
        di.AddChannel(new[] { 0, 1, 2, 3, 4, 5, 6, 7 });   // 全部 8 个 Port

        try
        {
            di.Start();
            bool[] buf = new bool[64];
            for (int i = 0; i < 5; i++)
            {
                di.ReadSinglePoint(ref buf);
                Console.WriteLine($"iter {i}: Port0 = {Convert.ToString(BitsToByte(buf, 0), 2).PadLeft(8, '0')}");
                Thread.Sleep(100);
            }
        }
        catch (JYDriverException ex)
        {
            Console.WriteLine("JY7133 error: " + ex.Message);
        }
        finally
        {
            di.Stop(); di.Channels.Clear();
        }
    }

    static byte BitsToByte(bool[] buf, int port)
    {
        byte v = 0;
        for (int i = 0; i < 8; i++) if (buf[port * 8 + i]) v |= (byte)(1 << i);
        return v;
    }
}
```

### 11.2 CI 单点边沿计数

```csharp
using JY7133;

var ci = new JY7133CITask(slotNumber: 0, channel: 0);
ci.Type = CIType.EdgeCounting;
ci.Mode = CIMode.Single;

ci.EdgeCounting.InitialCount             = 0;
ci.EdgeCounting.ActiveEdge               = EdgeType.Rising;
ci.EdgeCounting.Direction                = CountDirection.Up;
ci.EdgeCounting.InputTerminal            = InputTerminal.PFI_In_0;

ci.Start();
uint count = 0;
for (int i = 0; i < 10; i++)
{
    System.Threading.Thread.Sleep(200);
    ci.ReadSinglePoint(ref count);
    System.Console.WriteLine($"count = {count}");
}
ci.Stop();
```

### 11.3 CI 单点频率测量

```csharp
using JY7133;

var ci = new JY7133CITask(slotNumber: 0, channel: 0);
ci.Type = CIType.Frequency;
ci.Mode = CIMode.Single;

ci.FrequencyMeas.Timebase.Source   = CITimebaseSource.Internal100MHz;
ci.FrequencyMeas.StartingEdge      = EdgeType.Rising;
ci.FrequencyMeas.InputTerminal     = InputTerminal.PFI_In_0;

ci.Start();
double hz = 0;
ci.ReadSinglePoint(ref hz, timeout: 3000);
System.Console.WriteLine($"Frequency = {hz} Hz");
ci.Stop();
```

### 11.4 多卡同步（1 主 2 从的 CI 边沿计数）

```csharp
using JY7133;

JY7133Device[] devs = { new JY7133Device(0), new JY7133Device(1), new JY7133Device(2) };
foreach (var d in devs)
{
    d.ReferenceClock.Source            = ReferenceClockSource.External;
    d.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
    d.ReferenceClock.Commit();
}

var master = new JY7133CITask(0, 0);
var slave1 = new JY7133CITask(1, 0);
var slave2 = new JY7133CITask(2, 0);

foreach (var t in new[] { master, slave1, slave2 })
{
    t.Type = CIType.EdgeCounting;
    t.Mode = CIMode.Single;
    t.EdgeCounting.InputTerminal = InputTerminal.PFI_In_0;
}

master.SignalExport.Add(CISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);
master.Trigger.Type = CITriggerType.Immediate;

foreach (var s in new[] { slave1, slave2 })
{
    s.Trigger.Type                  = CITriggerType.Digital;
    s.Trigger.Digital.Source        = CIDigitalTriggerSource.PXI_Trig0;
    s.Trigger.Digital.Edge          = CIDigitalTriggerEdge.Rising;
}

// 先 Start Slave，再 Start Master
slave1.Start(); slave2.Start();
master.Start();
// ... 读值、停止：先停 Master 再停 Slave
master.Stop(); slave1.Stop(); slave2.Stop();
```

## 12. 注意事项速记

1. **高压隔离 DI**：输入电压范围 ±2 ~ ±60 V_DC，低于 ±2 V 时读数无定义；Bank 间共模隔离 60 V_DC，多 Bank 混用时注意共地策略。
2. **无 DO、无 CO**：JY7133 不具备数字输出与计数器输出能力。
3. **Port-based DI**：`AddChannel` 粒度为 Port（8 根线），共 8 个 Port。Port 编号 0~7。
4. **CI 最高频率 10 kHz**：输入信号高电平时长必须 ≥ 70 μs，否则测量可能漏计。
5. **CI 类型无 Pulse / SemiPeriod**：可用 `TwoEdgeSeparation` 变通测量脉宽。
6. **OutEvent / SignalExport 只能用 PXI_Trig**：JY7133 的 `IOTerminal` 与 `SignalExportDestination` 仅包含 PXI_Trig0~7，**不支持** PFI 输出。
7. **Type 决定配置类**：`ciTask.Type = ...` 之后只操作对应子类；切换 Type 时建议重新构造 Task。
8. **`ReferenceClock.Commit()` 仅在所有 Task Stop 时允许**；参数错误会导致 PLL 锁不住，需要断电重启。
9. **多卡同步顺序**：先 Slave 后 Master Start；Stop 顺序相反。
10. **端子枚举区分**：
    - CI 的 Source/Aux/Gate/Pause/DirTerminal 等外部输入 → `InputTerminal`
    - CI OutEvent 输出 → `IOTerminal`（只能 PXI_Trig）
    - 信号导出目的地 → `SignalExportDestination`（只能 PXI_Trig）
11. **Task 重用**：DI Stop 后务必 `Channels.Clear()`，否则再次 `AddChannel` 会累积端口。
12. **DI 与 Counter 共用物理 PFI**：产品型录标注"64 DI **或** 8 计数器"，当某根 PFI 被分配给计数器作信号输入时，该管脚就不应再当作 DI 使用。
13. **异常分层**：所有驱动层错误统一是 `JY7133.JYDriverException`；建议在每个 `Start` / `Read` / `Commit` 附近单独 `try-catch`，方便定位。
