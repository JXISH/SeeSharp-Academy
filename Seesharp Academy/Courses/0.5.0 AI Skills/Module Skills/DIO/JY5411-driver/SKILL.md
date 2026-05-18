---
name: jy5411-driver
description: 提供 JYTEK JY5411 系列（PCIe/PXIe-5411）高速数字 I/O（HSDIO）模块的完整 C# 驱动开发指引。涵盖 32 通道高速数字输入（DI）/数字输出（DO）单点/有限/连续/录制/循环输出模式、触发配置（数字/软件触发）、时钟配置（内部/外部时钟）、PFI 电平标准（1.8V/2.5V/3.3V/5V）、参考时钟同步、DI/DO 互同步、多卡同步、录制模式（Finite/Infinite Streaming）。当用户使用 JY5411、PCIe-5411、PXIe-5411、JY5411DITask、JY5411DOTask、JY5411Device、DIMode.Single、DIMode.Finite、DIMode.Continuous、DIMode.Record、DOMode.Single、DOMode.Finite、DOMode.ContinuousWrapping、DOMode.ContinuousNoWrapping、RecordMode.Finite、RecordMode.Infinite、PowerLevel、PFISetting 开发高速数字信号采集、数字信号生成、码型发生、逻辑分析、通信协议测试、自动化测试应用时自动应用。
---

# JY5411 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\HSDIO\JY5411\Bin\JY5411.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\HSDIO\JY5411\Bin\JY5411.XML`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY5411;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                  | 规格                                              |
| --------------------- | ------------------------------------------------- |
| DIO 通道数            | 32 通道高速 DIO，每通道独立方向控制（DI 或 DO）   |
| 最大时钟频率          | 50 MHz（DI 采样率 / DO 更新率上限）               |
| 电平标准              | 1.8V / 2.5V / 3.3V / 5V（软件可选）               |
| DI FIFO 缓存          | 64 M 采样值                                       |
| DO FIFO 缓存          | 64 M 采样值                                       |
| PFI 通道数            | 8 通道多功能 PFI（PFI0~PFI7）                     |
| 板载时基              | 高性能 TCXO 10 MHz（时基时钟 200 MHz）            |
| 输入阻抗              | 2.5 MΩ                                            |
| 典型输出阻抗          | 50 Ω                                              |
| 输入保护范围          | -0.5 V ~ 6.5 V                                    |
| 输出保护范围          | -0.5 V ~ (UserVcc+0.5 V)                          |
| 最大直流驱动          | 4 mA @ 1.8V / 8 mA @ 2.5V / 24 mA @ 3.3V / 32 mA @ 5V |
| 触发方式              | 数字触发 / 软件触发 / 立即                        |
| 接口类型              | PCIe / PXIe                                       |

### 产品型号

- **PCIe-5411**：32 通道，50 MHz，PCIe 高速数字 I/O 模块
- **PXIe-5411**：32 通道，50 MHz，PXIe 高速数字 I/O 模块

### 可选配件

- **ACL-1016868-1**：1 m 68pin VHDC-SCSI 双绞线电缆
- **DIN-68**：SCSI 68-pin 接线端子板

### 采样时钟源可选项

板载 TCXO、25 MHz Clock、PXIe_Clk100、PXIe_SYNC100、PFI<0..7>、PXIe_DSTAR A/B、PXI_Star、PXI Trigger

### 采样时钟频率范围

| 时钟源       | 频率范围         |
| ------------ | ---------------- |
| PFI<0..7>    | 8 Hz ~ 50 MHz    |
| PXI_Star     | 8 Hz ~ 50 MHz    |
| PXIe_DSTAR   | 8 Hz ~ 50 MHz    |
| PXI Trigger  | 8 Hz ~ 20 MHz    |

### 逻辑电平阈值

| 电平标准 | 最大低输入 | 最小高输入 |
| -------- | ---------- | ---------- |
| 1.8 V    | 0.65 V     | 1.2 V      |
| 2.5 V    | 0.7 V      | 1.7 V      |
| 3.3 V    | 0.8 V      | 2.0 V      |
| 5 V      | 1.5 V      | 3.5 V      |

## JY5411 关键特性

JY5411 是 HSDIO（高速数字 I/O）模块，**不含任何模拟通道**。使用时请注意：

- **没有 AI 任务类**：仅有 `JY5411DITask` 和 `JY5411DOTask`，没有 `JY5411AITask`
- **Record（录制）模式绑定在 DI 上**：`diTask.Mode = DIMode.Record`，不是 AI
- **触发类型枚举名为 `Immediately`（注意拼写）**：`DITriggerType.Immediately` / `DOTriggerType.Immediately`
- **通道以线（line）为单位**：32 条独立通道，编号 0~31，非“端口”概念
- **DO 缓冲可用长度属性名**：`doTask.availableLenInSamples`（小写 a 开头；DI 侧为 `AvailableSamples`）
- **DO 有限点数属性名**：`doTask.SamplesToUpdate`（DI 侧为 `SamplesToAcquire`）

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var diTask = new JY5411DITask(0);        // 1. 创建（按槽位号）
diTask.AddChannel(0);                    // 2. 添加通道（按线编号 0~31）
diTask.Mode = DIMode.Continuous;         // 3. 配置
diTask.SampleRate = 1000000;             //    采样率 1 MSa/s
diTask.Start();                          // 4. 启动
// ... 读取数据 ...
diTask.Stop();                           // 5. 停止
diTask.Channels.Clear();                 // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 数字输入（DI）

### 任务类：`JY5411DITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                            |
| -------------------- | ----------------------- | ----------------------------------------------- |
| `Mode`               | `DIMode`                | Single / Finite / Continuous / Record           |
| `SampleRate`         | `double`                | 采样率（Sa/s），最大 50 MHz                     |
| `SamplesToAcquire`   | `int`                   | Finite 模式下每通道采集点数                     |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（非 Single 模式）          |
| `TransferedSamples`  | `ulong`                 | 已传输点数                                      |
| `BufLenInSamples`    | `uint`                  | 每通道缓冲区可存储的点数                        |
| `SampleClock.Source` | `DISampleClockSource`   | Internal（默认）/ External                      |
| `SampleClock.Edge`   | `ClockEdge`             | Rising（默认）/ Falling                         |
| `Trigger.Type`       | `DITriggerType`         | Immediately（默认）/ Digital / Soft             |
| `Trigger.Mode`       | `DITriggerMode`         | Start（默认）/ Reference                        |
| `Trigger.Digital`    | `DIDigitalTrigger`      | 数字触发设置（Source / Edge）                   |
| `Trigger.Pause`      | `DIPauseTrigger`        | 暂停触发设置（Source / Level）                  |
| `Trigger.PreTriggerSamples` | `uint`           | Reference 模式下触发前预采集点数                 |
| `Trigger.ReTriggerCount`    | `int`            | 重触发次数（再触发采集的重复次数）              |
| `Trigger.Delay`      | `int`                   | 数字触发延迟（单位：Sample）                    |
| `Record`             | `DIRecord`              | Record 模式的录制配置                           |
| `SignalExport`       | `DISignalExport`        | 信号导出配置（给其他 Task 或卡使用）            |
| `Device`             | `JY5411Device`          | 设备级配置（PFI 电平、参考时钟）                |
| `Channels`           | `List<DIChannel>`       | 已添加的通道列表                                |

#### AddChannel 重载

```csharp
// 单通道（线编号 0~31）
diTask.AddChannel(int lineNum);

// 多通道
diTask.AddChannel(int[] linesNum);

// 删除通道
diTask.RemoveChannel(int lineNum);
```

#### 读取数据重载

```csharp
// Single 模式 — 全部已添加通道
bool[] readValues = new bool[32];
diTask.ReadSinglePoint(ref readValues);

// Single 模式 — 指定单通道
bool singleValue = false;
diTask.ReadSinglePoint(ref singleValue, int lineNum);

// Finite/Continuous — 单通道（byte 数组）
byte[] buf = new byte[samples];
diTask.ReadData(ref buf, uint samplesPerChannel, int timeOut);

// Finite/Continuous — 多通道（列存储，每列一个通道）
byte[,] buf = new byte[samples, channels];
diTask.ReadData(ref buf, uint samplesPerChannel, int timeOut);

// timeOut = -1 → 永久等待
```

#### DI 四种模式速查

| 模式         | 典型配置                       | 读取方式                                     |
| ------------ | ------------------------------ | -------------------------------------------- |
| `Single`     | `Mode=DIMode.Single`           | 轮询 `ReadSinglePoint`                       |
| `Finite`     | `+SamplesToAcquire=N`          | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                  | Timer/多线程轮询 `AvailableSamples` → `ReadData` |
| `Record`     | `+Record.FilePath/Mode/Length` | `GetRecordPreviewData` 预览，数据直接写入文件 |

#### DI 单点模式示例

```csharp
var diTask = new JY5411DITask(0);
const int lineCount = 32;
for (int i = 0; i < lineCount; i++)
    diTask.AddChannel(i);

diTask.Mode = DIMode.Single;
// 设置 PFI 逻辑电平（影响所有通道）
diTask.Device.PFI.LogicLevel = PowerLevel.Level_5p0;
diTask.Start();

bool[] dataBuffer = new bool[32];
diTask.ReadSinglePoint(ref dataBuffer);
// dataBuffer[i] 即为通道 i 的电平状态
```

#### DI 连续采集 Timer 模式（最常用）

```csharp
var diTask = new JY5411DITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Continuous;
diTask.SampleRate = 5000000;   // 5 MSa/s
diTask.SampleClock.Source = DISampleClockSource.Internal;
diTask.SampleClock.Edge = ClockEdge.Rising;
diTask.Start();

byte[] readBuffer = new byte[10000];

// Timer Tick 中：
timer.Enabled = false;
if (diTask.AvailableSamples >= (ulong)readBuffer.Length)
{
    diTask.ReadData(ref readBuffer, (uint)readBuffer.Length, 1000);
    easyChartX1.Plot(readBuffer);
}
timer.Enabled = true;
```

#### DI 数字触发配置

```csharp
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Mode = DITriggerMode.Start;                    // Start / Reference
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.PFI0;  // PFI0~7 / PXI_Trig0~7 / DO_StartTrig
diTask.Trigger.Digital.Edge = DIDigitalTriggerEdge.Rising;    // Rising / Falling
// Reference 模式可设置预触发点数
diTask.Trigger.PreTriggerSamples = 1000;
// 触发延迟（单位：Sample），触发发生后延迟指定点数再开始采集
diTask.Trigger.Delay = 100;
// 重触发次数（只在数字触发下有效，触发后可连续被触发 N 次）
diTask.Trigger.ReTriggerCount = 5;
```

#### DI 暂停触发（Pause Trigger）

暂停触发用于在采集过程中根据电平信号暂停/恢复采样（电平敏感，非沿敏感）：

```csharp
diTask.Trigger.Pause.Source = DIDigitalTriggerSource.PFI1;    // 电平信号源
diTask.Trigger.Pause.Level  = DIPauseTriggerLevel.High;       // None（禁用）/ Low（低电平暂停）/ High（高电平暂停）
```

#### DI 软触发配置

```csharp
diTask.Trigger.Type = DITriggerType.Soft;
diTask.Trigger.Mode = DITriggerMode.Start;
diTask.Start();
// 在需要时手动发送软触发
diTask.SendSoftwareTrigger();
```

#### DI 外部时钟配置

```csharp
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.PFI0;   // 详见 ClockTerminal 枚举
diTask.SampleClock.External.ExpectedRate = 10000000;         // 外部时钟期望频率
diTask.SampleClock.Edge = ClockEdge.Rising;
```

---

## 数字输出（DO）

### 任务类：`JY5411DOTask`

#### 关键属性

| 属性                      | 类型                    | 说明                                                    |
| ------------------------- | ----------------------- | ------------------------------------------------------- |
| `Mode`                    | `DOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`              | `double`                | 更新率（Sa/s），最大 50 MHz                             |
| `SamplesToUpdate`         | `int`                   | Finite / Wrapping 模式下每通道输出点数                  |
| `BufLenInSamples`         | `uint`                  | 每通道缓冲区可存储点数                                  |
| `availableLenInSamples`   | `uint`                  | 当前缓冲区可写入的点数（NoWrapping 模式）               |
| `TransferedSamples`       | `ulong`                 | 已传输点数                                              |
| `CompleteState`           | `OutputCompleteState`   | 输出完成后的保持状态：Zero（默认）/ Hold                |
| `SampleClock.Source`      | `DOSampleClockSource`   | Internal（默认）/ External                              |
| `SampleClock.Edge`        | `ClockEdge`             | Rising（默认）/ Falling                                 |
| `Trigger.Type`            | `DOTriggerType`         | Immediately（默认）/ Digital / Soft                     |
| `Trigger.Digital`         | `DODigitalTrigger`      | 数字触发设置（Source / Edge）                           |
| `Trigger.Pause`           | `DOPauseTrigger`        | 暂停触发设置（Source / Level）                          |
| `Trigger.Delay`           | `int`                   | 数字触发延迟（单位：Sample）                            |
| `SignalExport`            | `DOSignalExport`        | DO 信号导出配置                                         |
| `Device`                  | `JY5411Device`          | 设备级配置                                              |
| `Channels`                | `List<DOChannel>`       | 已添加的通道列表                                        |

#### AddChannel 重载

```csharp
// 单通道
doTask.AddChannel(int lineNum);

// 多通道
doTask.AddChannel(int[] linesNum);

// 带暂停空闲电平
doTask.AddChannel(int lineNum, DOPauseIdelLevel idleState);             // Low / High / HoldOn
doTask.AddChannel(int[] linesNum, DOPauseIdelLevel[] idleStates);

// 删除通道
doTask.RemoveChannel(int lineNum);
```

#### 写入数据重载

```csharp
// Single 模式 — 多通道同时写
bool[] writeValues = new bool[32];
doTask.WriteSinglePoint(writeValues);

// Single 模式 — 指定单通道写
doTask.WriteSinglePoint(bool writeValue, int lineNum);

// Finite/Continuous — 单通道 byte
byte[] buf = new byte[samples];
doTask.WriteData(buf, uint samplesPerChannel, int timeOut);

// Finite/Continuous — 多通道 byte（列存储，每列一个通道）
byte[,] buf = new byte[samples, channels];
doTask.WriteData(buf, uint samplesPerChannel, int timeOut);

// uint 打包数据（每个 uint 代表 32 通道的一个采样点）
uint[] udata = new uint[samples];
doTask.WriteData(udata, uint samplesToUpdate, int timeOut);
```

#### DO 四种模式速查

| 模式                     | 说明                                        | 数据写入顺序                  |
| ------------------------ | ------------------------------------------- | ----------------------------- |
| `Single`                 | 寄存器写入，立即输出                        | 直接 `WriteSinglePoint`       |
| `Finite`                 | 输出固定点数后停止                          | 先 `WriteData` 再 `Start`     |
| `ContinuousWrapping`     | 循环输出固定缓冲区（硬件 FIFO 内自动循环）  | 先 `WriteData` 再 `Start`     |
| `ContinuousNoWrapping`   | 持续消耗缓冲区，需实时追加新数据            | `Start` 后轮询 `availableLenInSamples` 追加 |

#### DO 单点输出示例

```csharp
var doTask = new JY5411DOTask(0);
doTask.AddChannel(0);
doTask.Mode = DOMode.Single;
doTask.Device.PFI.LogicLevel = PowerLevel.Level_3p3;   // 设置电平标准

bool writeValue = true;
doTask.WriteSinglePoint(writeValue, 0);
doTask.Start();
```

#### DO Continuous Wrapping 标准流程

```csharp
var doTask = new JY5411DOTask(0);
doTask.AddChannel(0);
doTask.Mode = DOMode.ContinuousWrapping;
doTask.SamplesToUpdate = 1000;           // 一个循环周期的点数
doTask.UpdateRate = 1000000;             // 1 MSa/s
doTask.SampleClock.Source = DOSampleClockSource.Internal;
doTask.SampleClock.Edge = ClockEdge.Rising;

// 生成一个周期的数字波形（50% 占空比方波）
byte[] writeValue = new byte[1000];
for (int i = 0; i < writeValue.Length; i++)
    writeValue[i] = (byte)((i < 500) ? 1 : 0);

// 先写数据，再 Start（Wrapping 模式要求）
doTask.WriteData(writeValue, (uint)writeValue.Length, -1);
doTask.Start();
```

#### DO Continuous NoWrapping 动态追加

```csharp
var doTask = new JY5411DOTask(0);
doTask.AddChannel(0);
doTask.Mode = DOMode.ContinuousNoWrapping;
doTask.UpdateRate = 1000000;
doTask.CompleteState = OutputCompleteState.Zero;  // 结束后输出 0

// 先写入初始缓冲
byte[] block = new byte[10000];
doTask.WriteData(block, (uint)block.Length, -1);
doTask.Start();

// Timer 中持续追加数据
timer.Enabled = false;
if (doTask.availableLenInSamples >= (uint)block.Length)
{
    // 生成下一段数据...
    doTask.WriteData(block, (uint)block.Length, 1000);
}
timer.Enabled = true;
```

#### DO 数字触发配置

```csharp
doTask.Trigger.Type = DOTriggerType.Digital;
doTask.Trigger.Digital.Source = DODigitalTriggerSource.PFI0;   // PFI0~7 / PXI_Trig0~7 / DI_StartTrig / DI_ReferenceTrig
doTask.Trigger.Digital.Edge = DODigitalTriggerEdge.Rising;
// 触发延迟（单位：Sample）
doTask.Trigger.Delay = 50;
```

#### DO 暂停触发（Pause Trigger）

```csharp
doTask.Trigger.Pause.Source = DODigitalTriggerSource.PFI2;
doTask.Trigger.Pause.Level  = DOPauseTriggerLevel.Low;        // None / Low / High
```

#### DO 软触发

```csharp
doTask.Trigger.Type = DOTriggerType.Soft;
doTask.Start();
doTask.SendSoftwareTrigger();
```

---

## 录制模式（Record）

Record 模式**绑定在 DI 上**，将采集数据流式写入文件，支持预览：

```csharp
var diTask = new JY5411DITask(0);
diTask.AddChannel(0);
// 多通道录制
// diTask.AddChannel(new int[] { 0, 1, 2, 3 });

diTask.Mode = DIMode.Record;
diTask.SampleRate = 10000000;                       // 10 MSa/s

// 配置录制参数
diTask.Record.FilePath = @"C:\Data\di_signal.bin";
diTask.Record.FileFormat = FileFormat.Bin;
diTask.Record.Mode = RecordMode.Finite;             // Finite / Infinite
diTask.Record.Length = 10.0;                        // 录制 10 秒（Finite 模式）

diTask.Start();

// 可在录制期间预览数据
byte[] preview = new byte[10000];
diTask.GetRecordPreviewData(ref preview, 10000, 1000);
easyChartX1.Plot(preview);

// 查询录制状态
double recordedLen;
bool recordDone;
do
{
    diTask.GetRecordStatus(out recordedLen, out recordDone);
    // 显示 recordedLen（已录制时长，单位：秒）
    System.Threading.Thread.Sleep(100);
} while (!recordDone);

diTask.Stop();
diTask.Channels.Clear();
```

**Infinite Streaming 模式**：不设置 `Length`，持续录制到文件，直到手动 `Stop()`。

---

## 设备级配置（`JY5411Device`）

通过 `diTask.Device` 或 `doTask.Device` 访问。

### PFI 电平标准

```csharp
// 设置 PFI 所有端口的输入/输出高电平标准（影响 DI 读入和 DO 输出的逻辑电平）
diTask.Device.PFI.LogicLevel = PowerLevel.Level_5p0;   // 1p8 / 2p5 / 3p3 / 5p0，默认 5p0
```

### 参考时钟配置

```csharp
// 使用内部 TCXO（默认）
diTask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

// 使用外部 PXIe_Clk100
diTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
diTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
diTask.Device.ReferenceClock.External.Frequency = 100000000;
diTask.Device.ReferenceClock.Commit();               // 必须 Commit 才生效
```

**警告**：错误的参考时钟参数会导致 PLL 无法锁定，需关机重启才能恢复。

---

## 信号导出与多卡同步

### DI/DO 同卡互同步（示例：DO 作为主设备）

```csharp
// DI 从 DO 接收采样时钟和开始触发（同一张卡）
var diTask = new JY5411DITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Continuous;
diTask.Trigger.Type = DITriggerType.Digital;
diTask.Trigger.Digital.Source = DIDigitalTriggerSource.DO_StartTrig;
diTask.Trigger.Digital.Edge = DIDigitalTriggerEdge.Rising;
diTask.SampleClock.Source = DISampleClockSource.External;
diTask.SampleClock.External.Terminal = ClockTerminal.DO_SampleClock;
diTask.SampleClock.External.ExpectedRate = 1000000;

// DO 主设备
var doTask = new JY5411DOTask(0);
doTask.AddChannel(1);
doTask.Mode = DOMode.ContinuousWrapping;
doTask.UpdateRate = 1000000;
doTask.Trigger.Type = DOTriggerType.Immediately;
doTask.SampleClock.Source = DOSampleClockSource.Internal;

doTask.WriteData(writeBuf, (uint)writeBuf.Length, -1);
diTask.Start();           // 先启动 DI（等待触发）
doTask.Start();           // 再启动 DO（产生触发和时钟）
```

### DI 多卡同步（采样时钟 + 触发信号导出）

```csharp
// ===== 主卡配置（Slot 2）=====
var masterTask = new JY5411DITask(2);
masterTask.AddChannel(0);
masterTask.Mode = DIMode.Finite;
masterTask.SampleRate = 5000000;
masterTask.SamplesToAcquire = 10000;
masterTask.Trigger.Type = DITriggerType.Immediately;
masterTask.Trigger.Mode = DITriggerMode.Start;

// 导出采样时钟到 PXI_Trig0，开始触发到 PXI_Trig1
masterTask.SignalExport.Add(DISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(DISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 1）=====
var slaveTask = new JY5411DITask(1);
slaveTask.AddChannel(0);
slaveTask.Mode = DIMode.Finite;
slaveTask.SamplesToAcquire = 10000;

slaveTask.SampleClock.Source = DISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 5000000;
slaveTask.Trigger.Type = DITriggerType.Digital;
slaveTask.Trigger.Mode = DITriggerMode.Start;
slaveTask.Trigger.Digital.Source = DIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先从卡再主卡 =====
slaveTask.Start();        // 从卡先启动，等待时钟和触发
masterTask.Start();       // 主卡启动，产生信号

// ===== 读取数据 =====
byte[] masterData = new byte[10000];
byte[] slaveData  = new byte[10000];
masterTask.WaitUntilDone(-1);
slaveTask.WaitUntilDone(-1);
masterTask.ReadData(ref masterData, 10000, -1);
slaveTask.ReadData(ref slaveData, 10000, -1);

masterTask.Stop();  slaveTask.Stop();
masterTask.Channels.Clear();  slaveTask.Channels.Clear();
```

**关键要点：**
- 主卡使用内部时钟 + Immediately 触发，从卡使用外部时钟 + Digital 触发
- 使用 `PXI_Trig0~PXI_Trig7` 作为同步信号线
- 启动顺序：**先从卡后主卡**，确保从卡先准备好接收信号

---

## 常见错误处理

| 异常代码                                         | 原因                       | 处理建议                  |
| ------------------------------------------------ | -------------------------- | ------------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误     | 检查设备管理器槽位号      |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start | 在 Start 前添加通道       |
| `BufferDataOverflow`                             | 读取速度慢于采集速度       | 增大读取频率或减小采样率  |
| `BufferDataDownflow`                             | DO 输出速度快于写入        | 使用 Wrapping 模式或加大预写入 |
| `ReadDataTimeout`                                | timeout 内未读到足够数据   | 增大 timeout 或检查采样率 |
| `WriteDataTimeout`                               | timeout 内未写入完成       | 增大 timeout 或减小更新率 |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数        | Stop 后再修改             |
| `SampleRateParameterInvalid`                     | 采样率超过 50 MHz 上限     | 检查采样率参数            |
| `UpdateRateParameterInvalid`                     | 更新率超过 50 MHz 上限     | 检查更新率参数            |
| `ChannelNumberParameterInvalid`                  | 通道号越界（非 0~31）      | 检查 AddChannel 参数      |

---

## 更多详情

- 完整 API 参考：见下文 **完整 API 参考** 章节
- 各功能代码示例：见下文 **完整代码示例** 章节
- 范例工程目录：`D:\JYTEK_Work\Examples\C#\HSDIO\JY5411_V5.1.8_Examples`


---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY5411;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\HSDIO\JY5411\Bin\JY5411.dll
```

---

## 设备信息类 `JY5411Device`

通过 `diTask.Device` 或 `doTask.Device` 访问：

| 属性                   | 类型              | 说明                                   |
| ---------------------- | ----------------- | -------------------------------------- |
| `BoardClockRate`       | `double`          | 板卡主时钟频率（Hz）                   |
| `SerialNumber`         | `string`          | 设备序列号                             |
| `DeviceID`             | `int`             | 设备 ID                                |
| `Handle`               | `IntPtr`          | 设备句柄（内部使用）                   |
| `MaxLineCount`         | `int`             | 最大通道数（32）                       |
| `MaxSampleRate`        | `double`          | DI 最大采样率（50 MHz）                |
| `MaxUpdateRate`        | `double`          | DO 最大更新率（50 MHz）                |
| `PFI`                  | `PFISetting`      | PFI 配置对象                           |
| `ReferenceClock`       | `ReferenceClock`  | 参考时钟配置对象（配置完需 `Commit()`）|

### PFISetting

```csharp
diTask.Device.PFI.LogicLevel = PowerLevel.Level_5p0;   // 写入后立即生效
```

### ReferenceClock

| 属性              | 类型                         | 说明                                     |
| ----------------- | ---------------------------- | ---------------------------------------- |
| `Source`          | `ReferenceClockSource`       | Internal（板载 10MHz TCXO）/ External    |
| `External`        | `ExternalReferenceClock`     | 外部参考时钟配置                         |
| `IsCommitAllowed` | `bool`                       | 当前是否允许提交（运行中禁止提交）       |

```csharp
diTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
diTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
diTask.Device.ReferenceClock.External.Frequency = 100000000;
diTask.Device.ReferenceClock.Commit();
```

---

## JY5411DITask — 数字输入任务

### 构造函数

```csharp
new JY5411DITask(int slotNum)       // 按槽位号创建（推荐）
new JY5411DITask(string boardName)  // 按板卡名称创建
```

### 属性

| 属性                | 类型                  | 说明                                   |
| ------------------- | --------------------- | -------------------------------------- |
| `Mode`              | `DIMode`              | Single / Finite / Continuous / Record  |
| `SampleRate`        | `double`              | 每通道采样率（Sa/s）                   |
| `SamplesToAcquire`  | `int`                 | Finite 模式采集点数/通道               |
| `AvailableSamples`  | `ulong`               | 缓冲区可读点数（非 Single 有效）       |
| `TransferedSamples` | `ulong`               | 已传输点数                             |
| `BufLenInSamples`   | `uint`                | 每通道缓冲区容量                       |
| `SampleClock`       | `DISampleClock`       | 时钟配置                               |
| `Trigger`           | `DITrigger`           | 触发配置                               |
| `Record`            | `DIRecord`            | 录制配置                               |
| `SignalExport`      | `DISignalExport`      | 信号导出                               |
| `Channels`          | `List<DIChannel>`     | 已添加的通道列表                       |
| `Device`            | `JY5411Device`        | 设备对象                               |

### 方法

```csharp
void AddChannel(int lineNum)               // 单通道（0~31）
void AddChannel(int[] linesNum)            // 多通道
void RemoveChannel(int lineNum)            // 移除单通道

void Start()
void Stop()
void WaitUntilDone(int timeOut)            // Finite 模式等待完成
void SendSoftwareTrigger()                 // 软触发

// Single 模式
void ReadSinglePoint(ref bool[] readValues)              // 读所有已添加通道
void ReadSinglePoint(ref bool readValue, int lineNum)    // 读指定单通道

// Finite / Continuous 模式
void ReadData(ref byte[] buf, uint samplesPerChannel, int timeOut)      // 单通道
void ReadData(ref byte[,] buf, uint samplesPerChannel, int timeOut)     // 多通道（列存储）
void ReadData(ref uint[] buf, uint samplesPerChannel, int timeOut)      // uint 打包
void ReadData(ref uint[] buf, int timeOut)                              // uint 打包（长度即点数）
void ReadData(IntPtr buf, uint samplesPerChannel, int timeOut)          // 指针

// Record 模式
void GetRecordPreviewData(ref byte[] buf, uint samplesPerChannel, int timeOut)
void GetRecordPreviewData(ref byte[,] buf, int samplesPerChannel, int timeOut)
void GetRecordPreviewData(ref uint[] buf, int samplesPerChannel, int timeOut)
void GetRecordPreviewData(ref uint[] buf, int timeOut)                  // uint 简化重载
void GetRecordStatus(out double recordedLength, out bool recordDone)
```

---

## JY5411DOTask — 数字输出任务

### 构造函数

```csharp
new JY5411DOTask(int slotNum)
new JY5411DOTask(string boardName)
```

### 属性

| 属性                      | 类型                    | 说明                                                    |
| ------------------------- | ----------------------- | ------------------------------------------------------- |
| `Mode`                    | `DOMode`                | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate`              | `double`                | 每通道更新率（Sa/s）                                    |
| `SamplesToUpdate`         | `int`                   | Finite/Wrapping 模式每通道输出点数                      |
| `BufLenInSamples`         | `uint`                  | 每通道缓冲区容量                                        |
| `availableLenInSamples`   | `uint`                  | 当前可写入的点数（NoWrapping 使用）                     |
| `TransferedSamples`       | `ulong`                 | 已传输点数                                              |
| `CompleteState`           | `OutputCompleteState`   | 输出完成状态：Zero（默认）/ Hold                        |
| `SampleClock`             | `DOSampleClock`         | 时钟配置                                                |
| `Trigger`                 | `DOTrigger`             | 触发配置                                                |
| `SignalExport`            | `DOSignalExport`        | 信号导出                                                |
| `Channels`                | `List<DOChannel>`       | 已添加的通道列表                                        |
| `Device`                  | `JY5411Device`          | 设备对象                                                |

### 方法

```csharp
void AddChannel(int lineNum)
void AddChannel(int[] linesNum)
void AddChannel(int lineNum, DOPauseIdelLevel idleState)
void AddChannel(int[] linesNum, DOPauseIdelLevel[] idleStates)
void RemoveChannel(int lineNum)

void Start()
void Stop()
void WaitUntilDone(int timeOut)
void SendSoftwareTrigger()

// Single 模式
void WriteSinglePoint(bool[] writeValues)                // 多通道同时写（长度 >= 通道数）
void WriteSinglePoint(bool writeValue, int lineNum)      // 指定单通道

// Finite / Continuous 模式
void WriteData(byte[] buf, uint samplesPerChannel, int timeOut)      // 单通道
void WriteData(byte[,] buf, uint samplesPerChannel, int timeOut)     // 多通道（列存储）
void WriteData(uint[] buf, uint samplesToUpdate, int timeOut)        // uint 打包
void WriteData(uint[] buf, int timeOut)                              // uint（长度即点数）
void WriteData(IntPtr buf, uint samplesToUpdate, int timeOut)        // 指针
```

---

## 枚举类型参考

### DIMode / DOMode

| DIMode       | 说明                            |
| ------------ | ------------------------------- |
| `Single`     | 单点读取                        |
| `Finite`     | 有限点数采集                    |
| `Continuous` | 连续采集                        |
| `Record`     | 录制到文件（流盘）              |

| DOMode                   | 说明                              |
| ------------------------ | --------------------------------- |
| `Single`                 | 寄存器写入，立即输出              |
| `Finite`                 | 输出固定点数后停止                |
| `ContinuousWrapping`     | 循环输出固定缓冲区（硬件自动循环）|
| `ContinuousNoWrapping`   | 持续消耗缓冲区，需实时追加数据    |

### DITriggerType / DOTriggerType（注意：`Immediately` 而非 `Immediate`）

| 值            | 说明                |
| ------------- | ------------------- |
| `Immediately` | 立即触发（默认）    |
| `Digital`     | 数字边沿触发        |
| `Soft`        | 软件触发            |

### DITriggerMode

| 值          | 说明           |
| ----------- | -------------- |
| `Start`     | 开始触发（默认）|
| `Reference` | 参考触发        |

### DIDigitalTriggerSource

`PFI0~PFI7` / `PXI_Trig0~PXI_Trig7` / `DO_StartTrig`

### DODigitalTriggerSource

`PFI0~PFI7` / `PXI_Trig0~PXI_Trig7` / `DI_StartTrig` / `DI_ReferenceTrig`

### DIDigitalTriggerEdge / DODigitalTriggerEdge / ClockEdge

`Rising` / `Falling`

### DISampleClockSource / DOSampleClockSource

`Internal` / `External`

### ClockTerminal（外部采样时钟端口）

| 值                             | 说明                  |
| ------------------------------ | --------------------- |
| `PXIe_DStarA` / `PXIe_DStarB`  | PXIe DStar 时钟       |
| `PXI_Star`                     | PXI Star 时钟         |
| `PXIe_Sync100`                 | PXIe SYNC100 时钟     |
| `PXI_Trig0` ~ `PXI_Trig7`      | PXI 触发总线          |
| `PFI0` ~ `PFI7`                | 前面板 PFI 引脚       |
| `DI_SampleClock`               | DI 采样时钟（互同步） |
| `DO_SampleClock`               | DO 采样时钟（互同步） |

### PowerLevel（PFI 电平标准）

| 值            | 说明   |
| ------------- | ------ |
| `Level_1p8`   | 1.8 V  |
| `Level_2p5`   | 2.5 V  |
| `Level_3p3`   | 3.3 V  |
| `Level_5p0`   | 5 V（默认）|

### DIPauseTriggerLevel / DOPauseTriggerLevel（暂停触发电平）

| 值       | 说明                           |
| -------- | ------------------------------ |
| `None`   | 暂停触发无效（默认）            |
| `Low`    | 低电平有效（低电平时暂停采样/更新）|
| `High`   | 高电平有效（高电平时暂停采样/更新）|

### DOPauseIdelLevel（DO 暂停空闲电平）

| 值        | 说明                 |
| --------- | -------------------- |
| `Low`     | 空闲时输出低电平     |
| `High`    | 空闲时输出高电平     |
| `HoldOn`  | 空闲时保持最后值     |

### OutputCompleteState（DO 输出完成状态）

| 值     | 说明                     |
| ------ | ------------------------ |
| `Zero` | 输出完成后归零（默认）   |
| `Hold` | 输出完成后保持最后一点   |

### DISignalExportSource

`SampleClock` / `StartTrig` / `ReferenceTrig`

### DOSignalExportSource

`DO_0` ~ `DO_31` / `SampleClock` / `StartTrig`

### SignalExportDestination（信号导出目的端）

`PFI0~PFI7` / `PXI_Trig0~PXI_Trig7`

### ReferenceClockSource

| 值          | 说明                          |
| ----------- | ----------------------------- |
| `Internal`  | 板载 10 MHz TCXO（默认）      |
| `External`  | 外部参考时钟                  |

### ExternalReferenceClockTerminal

| 值             | 说明            |
| -------------- | --------------- |
| `PXIe_Clk100`  | PXIe 100 MHz 时钟 |

### RecordMode / FileFormat

| RecordMode  | 说明                                  |
| ----------- | ------------------------------------- |
| `Finite`    | 有限录制，录制指定时长后自动停止      |
| `Infinite`  | 无限录制，需手动调用 Stop 停止        |

| FileFormat  | 说明          |
| ----------- | ------------- |
| `Bin`       | 二进制格式    |

---

## DIRecord — 录制配置

通过 `diTask.Record` 访问：

| 属性         | 类型          | 说明                                    |
| ------------ | ------------- | --------------------------------------- |
| `FilePath`   | `string`      | 录制文件路径                            |
| `FileFormat` | `FileFormat`  | 文件格式（仅 Bin）                      |
| `Mode`       | `RecordMode`  | Finite / Infinite                       |
| `Length`     | `double`      | 录制时长（秒），仅 Finite 模式有效      |

---

## DITrigger / DOTrigger — 触发配置对象

### DITrigger（通过 `diTask.Trigger` 访问）

| 属性                  | 类型                  | 说明                                    |
| --------------------- | --------------------- | --------------------------------------- |
| `Type`                | `DITriggerType`       | Immediately / Digital / Soft            |
| `Mode`                | `DITriggerMode`       | Start / Reference                       |
| `Digital`             | `DIDigitalTrigger`    | 数字触发子对象                            |
| `Pause`               | `DIPauseTrigger`      | 暂停触发子对象                            |
| `PreTriggerSamples`   | `uint`                | Reference 模式预触发采集点数               |
| `ReTriggerCount`      | `int`                 | 重触发次数                                |
| `Delay`               | `int`                 | 数字触发延迟（单位：Sample）             |

### DIDigitalTrigger（`Trigger.Digital`）

| 属性      | 类型                       | 说明                                  |
| --------- | -------------------------- | ------------------------------------- |
| `Source`  | `DIDigitalTriggerSource`   | PFI0~7 / PXI_Trig0~7 / DO_StartTrig   |
| `Edge`    | `DIDigitalTriggerEdge`     | Rising / Falling                      |

### DIPauseTrigger（`Trigger.Pause`）

| 属性      | 类型                       | 说明                                       |
| --------- | -------------------------- | ------------------------------------------ |
| `Source`  | `DIDigitalTriggerSource`   | 暂停信号源（PFI / PXI_Trig / DO_StartTrig） |
| `Level`   | `DIPauseTriggerLevel`      | None（默认）/ Low / High                    |

### DOTrigger（通过 `doTask.Trigger` 访问）

| 属性        | 类型                | 说明                                    |
| ----------- | ------------------- | --------------------------------------- |
| `Type`      | `DOTriggerType`     | Immediately / Digital / Soft            |
| `Digital`   | `DODigitalTrigger`  | 数字触发子对象                            |
| `Pause`     | `DOPauseTrigger`    | 暂停触发子对象                            |
| `Delay`     | `int`               | 数字触发延迟（单位：Sample）             |

### DODigitalTrigger（`Trigger.Digital`）

| 属性      | 类型                       | 说明                                                      |
| --------- | -------------------------- | --------------------------------------------------------- |
| `Source`  | `DODigitalTriggerSource`   | PFI0~7 / PXI_Trig0~7 / DI_StartTrig / DI_ReferenceTrig    |
| `Edge`    | `DODigitalTriggerEdge`     | Rising / Falling                                          |

### DOPauseTrigger（`Trigger.Pause`）

| 属性      | 类型                       | 说明                                     |
| --------- | -------------------------- | -------------------------------------- |
| `Source`  | `DODigitalTriggerSource`   | 暂停信号源                               |
| `Level`   | `DOPauseTriggerLevel`      | None（默认）/ Low / High                 |

---

## DIChannel / DOChannel — 通道对象

| DIChannel 属性 | 类型    | 说明                   |
| -------------- | ------- | ---------------------- |
| `LineNum`      | `int`   | 通道线编号（0~31）        |

| DOChannel 属性  | 类型                | 说明                            |
| --------------- | ------------------- | ------------------------------ |
| `LineNum`       | `int`               | 通道线编号（0~31）                |
| `IdleState`     | `DOPauseIdelLevel`  | 暂停/空闲时电平（Low / High / HoldOn）|

通道对象通过 `diTask.Channels[i]` / `doTask.Channels[i]` 访问；`AddChannel` 时自动构造并加入 `Channels` 列表。

---

## DISignalExport / DOSignalExport — 信号导出

```csharp
// 添加单个导出路由
diTask.SignalExport.Add(DISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);

// 一个源导出到多个目的
var dests = new List<SignalExportDestination>
{
    SignalExportDestination.PXI_Trig0,
    SignalExportDestination.PFI1
};
diTask.SignalExport.Add(DISignalExportSource.StartTrig, dests);

// 清除指定目的端的导出
diTask.SignalExport.Clear(SignalExportDestination.PXI_Trig0);

// 清除所有导出
diTask.SignalExport.ClearAll();
```

---

## 异常类 JYDriverException

```csharp
try { /* ... */ }
catch (JYDriverException ex)
{
    // ex.Message — 驱动错误描述
    // ex.ExceptionName — 异常枚举名
    // ex.ErrorCode — 错误码
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 常见枚举值

| 枚举值                                          | 含义                       |
| ----------------------------------------------- | -------------------------- |
| `OpenDeviceFailed`                              | 打开设备失败（未连接/槽位号错误）|
| `CloseDeviceFailed`                             | 关闭设备失败               |
| `HardwareResourceIsReserved`                    | 硬件资源被占用             |
| `NoChannelAdded`                                | 未添加通道                 |
| `StartTaskFailed`                               | 启动 Task 失败             |
| `StopTaskFailed`                                | 停止 Task 失败             |
| `TaskHasNotStarted`                             | Task 未启动即读写          |
| `TaskHasStartedCannotPerformTheSetOperation`    | Task 运行中修改参数        |
| `BufferDataOverflow`                            | 采集缓冲区溢出             |
| `BufferDataDownflow`                            | 输出缓冲区数据不足         |
| `ReadDataTimeout` / `WriteDataTimeout`          | 读/写超时                  |
| `SampleRateParameterInvalid`                    | 采样率超限                 |
| `UpdateRateParameterInvalid`                    | 更新率超限                 |
| `ChannelNumberParameterInvalid`                 | 无效通道号                 |
| `TriggerParameterInvalid`                       | 触发参数无效               |
| `ReferenceTriggerDoesNotMatchWithSampleMode`    | 参考触发与采集模式不匹配   |
| `PretriggerPointCanNotBeLongerThanTheNumberOfFinitePoint` | 预触发点数 > 有限采集点数 |
| `OnlySupportRecordMode`                         | 仅支持流盘模式             |
| `SendSoftTirggerFailed`                         | 软触发发送失败             |

---

# 完整代码示例

> 所有示例均来自 `JY5411_V5.1.8_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：DI 单点读取（WinForm，32 通道同时读）

```csharp
using JY5411;

public partial class MainForm : Form
{
    private JY5411DITask diTask;
    private const int lineCount = 32;   // JY5411 共 32 通道

    private void button_start_Click(object sender, EventArgs e)
    {
        // 1. 按槽位号创建 DI Task
        diTask = new JY5411DITask(comboBox_slotNumber.SelectedIndex);

        // 2. 添加全部 32 个通道
        for (int i = 0; i < lineCount; i++)
            diTask.AddChannel(i);

        // 3. 单点模式 + 设置 PFI 逻辑电平（5V）
        diTask.Mode = DIMode.Single;
        diTask.Device.PFI.LogicLevel = PowerLevel.Level_5p0;

        // 4. 启动
        try { diTask.Start(); }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }

        timer_FetchData.Enabled = true;
    }

    private void timer_FetchData_Tick(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        bool[] dataBuffer = new bool[lineCount];
        diTask.ReadSinglePoint(ref dataBuffer);
        // dataBuffer[i] 为通道 i 的高/低电平状态
        for (int i = 0; i < lineCount; i++)
            ledArray[i].Value = dataBuffer[i];
        timer_FetchData.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        diTask?.Stop();
        diTask?.Channels.Clear();
    }
}
```

---

## 示例 2：DI 连续采集（WinForm + Timer）

```csharp
using JY5411;

public partial class MainForm : Form
{
    private JY5411DITask diTask;
    private byte[] readValue;

    private void button_start_Click(object sender, EventArgs e)
    {
        // 1. 创建 Task 并添加单通道
        diTask = new JY5411DITask(comboBox_slotNumber.SelectedIndex);
        diTask.AddChannel(comboBox_channelNumber.SelectedIndex);

        // 2. 连续模式 + 采样率 + 内部时钟
        diTask.Mode = DIMode.Continuous;
        diTask.SampleRate = (double)numericUpDown_sampleRate.Value;   // 例如 5 MSa/s
        diTask.SampleClock.Source = DISampleClockSource.Internal;
        diTask.SampleClock.Edge = ClockEdge.Rising;

        // 3. 启动
        try { diTask.Start(); }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }

        readValue = new byte[(int)numericUpDown_samples.Value];
        timer_FetchData.Enabled = true;
    }

    private void timer_FetchData_Tick(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;

        // 4. 缓冲区够用则读取
        if (diTask.AvailableSamples >= (ulong)readValue.Length)
        {
            try
            {
                diTask.ReadData(ref readValue, (uint)readValue.Length, 1000);
                easyChartX_readData.Plot(readValue);
            }
            catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
        }
        timer_FetchData.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        diTask?.Stop();
    }
}
```

---

## 示例 3：DI 有限点数采集 + 数字触发

```csharp
using JY5411;

private void button_start_Click(object sender, EventArgs e)
{
    var diTask = new JY5411DITask(0);
    diTask.AddChannel(0);

    // 有限采集模式
    diTask.Mode = DIMode.Finite;
    diTask.SampleRate = 10000000;         // 10 MSa/s
    diTask.SamplesToAcquire = 100000;

    // PFI0 上升沿触发
    diTask.Trigger.Type = DITriggerType.Digital;
    diTask.Trigger.Mode = DITriggerMode.Start;
    diTask.Trigger.Digital.Source = DIDigitalTriggerSource.PFI0;
    diTask.Trigger.Digital.Edge = DIDigitalTriggerEdge.Rising;

    diTask.Start();

    // 等待采集完成后读取
    byte[] data = new byte[100000];
    diTask.WaitUntilDone(-1);
    diTask.ReadData(ref data, 100000, -1);

    diTask.Stop();
    diTask.Channels.Clear();
}
```

---

## 示例 4：DI 流盘录制（Finite Streaming）

```csharp
using JY5411;

private JY5411DITask diTask;
private byte[] previewBuf;

private void button_start_Click(object sender, EventArgs e)
{
    diTask = new JY5411DITask(0);

    // 支持多通道同步录制
    int[] lines = { 0, 1, 2, 3 };
    foreach (int ln in lines) diTask.AddChannel(ln);

    diTask.Mode = DIMode.Record;
    diTask.SampleRate = 20000000;         // 20 MSa/s

    diTask.Record.FilePath   = @"C:\Data\di_stream.bin";
    diTask.Record.FileFormat = FileFormat.Bin;
    diTask.Record.Mode       = RecordMode.Finite;
    diTask.Record.Length     = 5.0;       // 录制 5 秒

    diTask.Start();

    previewBuf = new byte[10000];
    timer_preview.Enabled = true;
}

private void timer_preview_Tick(object sender, EventArgs e)
{
    timer_preview.Enabled = false;

    // 实时预览（不影响磁盘写入）
    diTask.GetRecordPreviewData(ref previewBuf, (uint)previewBuf.Length, 1000);
    easyChartX1.Plot(previewBuf);

    // 查询录制状态
    double recordedLen;
    bool recordDone;
    diTask.GetRecordStatus(out recordedLen, out recordDone);
    label_status.Text = $"已录制 {recordedLen:F2} s";

    if (recordDone)
    {
        diTask.Stop();
        diTask.Channels.Clear();
        return;
    }
    timer_preview.Enabled = true;
}
```

**Infinite 模式**：去掉 `Record.Length`，设 `Record.Mode = RecordMode.Infinite`，手动调用 `Stop()` 结束。

---

## 示例 5：DO 单点输出

```csharp
using JY5411;

private JY5411DOTask doTask;
private int lineID = 0;

private void button_start_Click(object sender, EventArgs e)
{
    doTask = new JY5411DOTask(0);
    doTask.AddChannel(lineID);
    doTask.Mode = DOMode.Single;
    doTask.Device.PFI.LogicLevel = PowerLevel.Level_3p3;

    // 先写入初始值，再 Start
    doTask.WriteSinglePoint(industrySwitch.Value, lineID);
    doTask.Start();
}

private void industrySwitch_ValueChanged(object sender, EventArgs e)
{
    // 开关切换时更新输出
    doTask.WriteSinglePoint(industrySwitch.Value, lineID);
}

private void button_stop_Click(object sender, EventArgs e)
{
    doTask?.Channels.Clear();
    doTask?.Stop();
}
```

---

## 示例 6：DO Continuous Wrapping（循环输出数字波形）

```csharp
using JY5411;

private void button_start_Click(object sender, EventArgs e)
{
    var doTask = new JY5411DOTask(0);
    doTask.AddChannel(0);

    doTask.Mode = DOMode.ContinuousWrapping;
    doTask.SamplesToUpdate = 1000;                 // 一个循环周期 1000 点
    doTask.UpdateRate = 1000000;                   // 1 MSa/s
    doTask.SampleClock.Source = DOSampleClockSource.Internal;
    doTask.SampleClock.Edge = ClockEdge.Rising;

    // 生成 50% 占空比方波
    byte[] writeValue = new byte[1000];
    for (int i = 0; i < 500; i++) writeValue[i] = 1;
    // 后 500 个保持为 0（byte[] 默认值）

    // Wrapping 模式：先写后启动
    doTask.WriteData(writeValue, (uint)writeValue.Length, -1);
    doTask.Start();
}
```

---

## 示例 7：DO Continuous NoWrapping（动态追加）

```csharp
using JY5411;

private JY5411DOTask doTask;
private byte[] writeBlock;

private void button_start_Click(object sender, EventArgs e)
{
    doTask = new JY5411DOTask(0);
    doTask.AddChannel(0);
    doTask.Mode = DOMode.ContinuousNoWrapping;
    doTask.UpdateRate = 2000000;
    doTask.CompleteState = OutputCompleteState.Zero;

    writeBlock = new byte[10000];
    GenerateBlock(writeBlock);        // 用户自定义波形生成

    // 启动前先预写入一块
    doTask.WriteData(writeBlock, (uint)writeBlock.Length, -1);
    doTask.Start();

    timer_write.Enabled = true;
}

private void timer_write_Tick(object sender, EventArgs e)
{
    timer_write.Enabled = false;

    // 当缓冲区有足够空间时追加下一段
    if (doTask.availableLenInSamples >= (uint)writeBlock.Length)
    {
        GenerateBlock(writeBlock);
        doTask.WriteData(writeBlock, (uint)writeBlock.Length, 1000);
    }
    timer_write.Enabled = true;
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_write.Enabled = false;
    doTask?.Stop();
    doTask?.Channels.Clear();
}
```

---

## 示例 8：DI/DO 同卡互同步（DO 主导 DI 从属）

```csharp
using JY5411;

private JY5411DITask diTask;
private JY5411DOTask doTask;
private byte[] readData;
private byte[] writeValue;

private void easyButton_Start_Click(object sender, EventArgs e)
{
    int slot = comboBox_slotNumber.SelectedIndex;

    // === DI 从设备：接收 DO 的采样时钟和开始触发 ===
    diTask = new JY5411DITask(slot);
    diTask.AddChannel(comboBox_dilineID.SelectedIndex);
    diTask.Mode = DIMode.Continuous;
    diTask.Trigger.Type = DITriggerType.Digital;
    diTask.Trigger.Mode = DITriggerMode.Start;
    diTask.Trigger.Digital.Source = DIDigitalTriggerSource.DO_StartTrig;
    diTask.Trigger.Digital.Edge   = DIDigitalTriggerEdge.Rising;
    diTask.SampleClock.Source     = DISampleClockSource.External;
    diTask.SampleClock.External.Terminal     = ClockTerminal.DO_SampleClock;
    diTask.SampleClock.External.ExpectedRate = (double)numericUpDown_DIsamplerate.Value;

    // === DO 主设备：Immediately 触发 + 内部时钟 ===
    doTask = new JY5411DOTask(slot);
    doTask.AddChannel(comboBox_dolineID.SelectedIndex);
    doTask.Mode = DOMode.ContinuousWrapping;
    doTask.UpdateRate = (double)numericUpDown_DOupdateRate.Value;
    doTask.Trigger.Type = DOTriggerType.Immediately;
    doTask.SampleClock.Source = DOSampleClockSource.Internal;

    // 准备数据
    writeValue = new byte[(int)numericUpDown_samples.Value];
    GenerateDutyCycleWaveform(writeValue, 0.5);
    doTask.WriteData(writeValue, (uint)writeValue.Length, 1000);
    readData = new byte[(int)numericUpDown_diSamples.Value];

    // 先启动 DI（等待触发），再启动 DO（产生触发）
    diTask.Start();
    doTask.Start();

    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (diTask.AvailableSamples > (ulong)readData.Length)
    {
        diTask.ReadData(ref readData, (uint)readData.Length, 1000);
        easyChartX_readData.Plot(readData);
    }
    timer_FetchData.Enabled = true;
}
```

---

## 示例 9：DI 多卡同步（主卡导出采样时钟和触发）

```csharp
using JY5411;

private JY5411DITask masterTask, slaveTask;

private void button_start_Click(object sender, EventArgs e)
{
    // === 主卡（Slot 2）===
    masterTask = new JY5411DITask(2);
    masterTask.AddChannel(0);
    masterTask.Mode = DIMode.Finite;
    masterTask.SampleRate = (double)numericUpDown_sampleRate.Value;
    masterTask.SamplesToAcquire = (int)numericUpDown_samples.Value;
    masterTask.Trigger.Type = DITriggerType.Immediately;
    masterTask.Trigger.Mode = DITriggerMode.Start;

    // 导出时钟和触发到 PXI Trigger Bus
    masterTask.SignalExport.Add(DISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
    masterTask.SignalExport.Add(DISignalExportSource.StartTrig,   SignalExportDestination.PXI_Trig1);

    // === 从卡（Slot 1）===
    slaveTask = new JY5411DITask(1);
    slaveTask.AddChannel(0);
    slaveTask.Mode = DIMode.Finite;
    slaveTask.SamplesToAcquire = masterTask.SamplesToAcquire;
    slaveTask.SampleClock.Source = DISampleClockSource.External;
    slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
    slaveTask.SampleClock.External.ExpectedRate = masterTask.SampleRate;
    slaveTask.Trigger.Type = DITriggerType.Digital;
    slaveTask.Trigger.Mode = DITriggerMode.Start;
    slaveTask.Trigger.Digital.Source = DIDigitalTriggerSource.PXI_Trig1;

    // 启动顺序：先从卡，再主卡
    slaveTask.Start();
    masterTask.Start();

    // 等待并读取
    byte[] master = new byte[masterTask.SamplesToAcquire];
    byte[] slave  = new byte[masterTask.SamplesToAcquire];
    masterTask.WaitUntilDone(-1);
    slaveTask.WaitUntilDone(-1);
    masterTask.ReadData(ref master, (uint)master.Length, -1);
    slaveTask.ReadData (ref slave,  (uint)slave.Length,  -1);

    masterTask.Stop(); slaveTask.Stop();
    masterTask.Channels.Clear(); slaveTask.Channels.Clear();
}
```

---

## 示例 10：外部参考时钟配置（PXIe_Clk100）

```csharp
using JY5411;

var diTask = new JY5411DITask(0);
diTask.AddChannel(0);
diTask.Mode = DIMode.Continuous;
diTask.SampleRate = 10000000;

// 切换到外部参考时钟（PXIe 100 MHz）
diTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
diTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
diTask.Device.ReferenceClock.External.Frequency = 100000000;
diTask.Device.ReferenceClock.Commit();   // !! 必须 Commit 才生效

diTask.Start();
```

> **警告**：错误的参考时钟参数会导致 PLL 无法锁定，需关机重启才能恢复。仅在空闲状态下调用 `Commit()`。

---

## 示例 11：DO 数字触发 + 单周期输出

```csharp
using JY5411;

var doTask = new JY5411DOTask(0);
doTask.AddChannel(0);
doTask.Mode = DOMode.Finite;
doTask.SamplesToUpdate = 5000;
doTask.UpdateRate = 1000000;

// PFI2 下降沿触发开始输出
doTask.Trigger.Type = DOTriggerType.Digital;
doTask.Trigger.Digital.Source = DODigitalTriggerSource.PFI2;
doTask.Trigger.Digital.Edge   = DODigitalTriggerEdge.Falling;

byte[] writeBuf = new byte[5000];
// ... 填充波形 ...

doTask.WriteData(writeBuf, (uint)writeBuf.Length, 1000);
doTask.Start();
doTask.WaitUntilDone(-1);
doTask.Stop();
```
