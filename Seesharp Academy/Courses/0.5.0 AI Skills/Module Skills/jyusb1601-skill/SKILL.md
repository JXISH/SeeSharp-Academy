---
name: jyusb1601-driver
description: 提供 JYTEK JYUSB-1601 多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、模拟输出（AO）单点/有限/连续输出、数字输入/输出（DI/DO）、计数器输入（CI）边沿计数/频率/周期/脉冲测量、计数器输出（CO）脉冲生成，以及触发、时钟配置。当用户使用 JYUSB1601、USB-1601、JYUSB1601AITask、JYUSB1601AOTask、JYUSB1601CITask、JYUSB1601COTask、JYUSB1601DITask、JYUSB1601DOTask 开发数据采集应用时自动应用。
---

# JYUSB-1601 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB1601\Bin\JYUSB1601.dll`（引用到 .csproj）
- **目标框架**：.NET Framework 4.8 或更高版本
- **命名空间**：`using JYUSB1601;`
- **设备名称**：在 JYTEK 设备管理器中设置的板卡别名（Board Name），如 `"USBDev0"`

## 硬件规格速查

| 功能 | 规格 |
|------|------|
| AI 通道数 | 16 SE / 8 差分 |
| AI 最大采样率 | 单通道 250 kSa/s，N 通道 250k/N Sa/s |
| AI 输入范围 | ±10V / ±5V / ±2.5V |
| AO 通道数 | 2 通道 |
| AO 最大更新率 | 单通道 2.86 MSa/s，双通道 2 MSa/s |
| DIO | 8 线输入 + 8 线输出 |
| 计数器 | 2 个（CTR0 / CTR1） |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JYUSB1601AITask("USBDev0");   // 1. 创建
task.AddChannel(0, -10, 10);             // 2. 添加通道
task.Mode = AIMode.Continuous;            // 3. 配置
task.SampleRate = 10000;
task.Start();                            // 4. 启动
// ... 读取数据 ...
task.Stop();                             // 5. 停止
task.Channels.Clear();                   // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JYUSB1601AITask`

#### 关键属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `AIMode` | Single / Finite / Continuous / Record |
| `SampleRate` | `double` | 每通道采样率（Sa/s） |
| `SamplesToAcquire` | `int` | Finite 模式下每通道采集点数 |
| `AvailableSamples` | `ulong` | 缓冲区中可读取的点数（非 Single 模式） |
| `Bandwidth` | `AIBandwidth` | \_15KHz / \_39KHz / \_80KHz（默认） |
| `SampleClock.Source` | `AISampleClockSource` | Internal（默认）/ External |
| `Trigger.Type` | `AITriggerType` | Immediate（默认）/ Digital / Soft |

#### AddChannel 重载

```csharp
// 单通道
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);
// 单通道 + 接线方式
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh, AITerminal terminal);
// chnId = -1 → 添加全部通道
// AITerminal: RSE（默认）/ Differential
```

#### 读取数据重载

```csharp
// Single 模式 — 单通道
aiTask.ReadSinglePoint(ref double readValue, int channel);
// Single 模式 — 所有通道
aiTask.ReadSinglePoint(ref double[] readValues);

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);
// Finite/Continuous — 多通道（列存储，每列一个通道）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
// timeout = -1 → 永久等待
```

#### AI 四种模式速查

| 模式 | 典型配置 | 读取方式 |
|------|----------|----------|
| `Single` | `Mode=AIMode.Single` | 轮询 `ReadSinglePoint` |
| `Finite` | `+SamplesToAcquire=N` | `WaitUntilDone(-1)` 后 `ReadData` |
| `Continuous` | `+SampleRate` | Timer 轮询 `AvailableSamples` → `ReadData` |
| `Record` | `+Record.FilePath/Mode/Length` | `GetRecordPreviewData` 预览，数据写入文件 |

#### 连续采集 Timer 模式（最常用）

```csharp
// Timer Tick 中：
timer.Enabled = false;
if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
{
    aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
    easyChartX1.Plot(readBuffer);   // 显示波形
}
timer.Enabled = true;
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.ExpectedRate = 10000; // 期望外部时钟频率
```

---

## 模拟输出（AO）

### 任务类：`JYUSB1601AOTask`

#### 关键属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Mode` | `AOMode` | Single / Finite / ContinuousWrapping / ContinuousNoWrapping |
| `UpdateRate` | `double` | 更新率（Sa/s） |
| `SamplesToUpdate` | `int` | Finite/Continuous 模式下每通道输出点数 |
| `CompleteState` | `OutputCompleteState` | Zero / Hold（默认，保持末值） |

#### AddChannel / 写入

```csharp
aoTask.AddChannel(int chnId);   // chnId=-1 添加全部，AO 仅 0~1 通道
aoTask.AddChannel(int[] chnsId);

// Single 模式
aoTask.WriteSinglePoint(double[] writeValues);   // 所有通道
aoTask.WriteSinglePoint(double writeValue, int channel);  // 指定通道

// 缓冲模式（Finite / Continuous）
aoTask.WriteData(double[] buf, int timeout);     // 单通道
aoTask.WriteData(double[,] buf, int timeout);    // 多通道（列存储）
```

#### AO 四种模式速查

| 模式 | 说明 |
|------|------|
| `Single` | 写寄存器，立即输出直流电压 |
| `Finite` | 一次性输出 N 点波形，完成后保持末值或归零 |
| `ContinuousWrapping` | 循环播放缓冲区中固定波形（不可实时更新） |
| `ContinuousNoWrapping` | 持续从缓冲区消耗数据，可实时追加（适合流式输出） |

#### 连续环形输出标准流程

```csharp
aoTask = new JYUSB1601AOTask("USBDev0");
aoTask.AddChannel(0);
aoTask.Mode = AOMode.ContinuousWrapping;
aoTask.UpdateRate = 100000;
aoTask.SamplesToUpdate = 1000;          // 缓冲区大小
aoTask.WriteData(waveBuffer, -1);       // 先写数据
aoTask.Start();                         // 再启动
```

---

## 数字 I/O（DI / DO）

### 任务类：`JYUSB1601DITask` / `JYUSB1601DOTask`

```csharp
// 数字输入（8 线，DIO_0~DIO_7）
var diTask = new JYUSB1601DITask("USBDev0");
diTask.AddChannel(0);       // 添加第 0 线
diTask.Start();
bool value;
diTask.ReadSinglePoint(ref value, 0);   // 读单线
diTask.Stop();

// 数字输出
var doTask = new JYUSB1601DOTask("USBDev0");
doTask.AddChannel(0);
doTask.Start();
doTask.WriteSinglePoint(true, 0);       // 写单线
doTask.Stop();
```

---

## 计数器输入（CI）

### 任务类：`JYUSB1601CITask`

**构造**：`new JYUSB1601CITask(string boardName, int channel)` — channel 0 或 1

#### CI 测量类型

| CIType | 读取方法 | 返回值 |
|--------|----------|--------|
| `EdgeCounting` | `ReadSinglePoint(ref uint count)` | 边沿计数值 |
| `Frequency` | `ReadSinglePoint(ref double meas, int timeout)` | 频率（Hz） |
| `Period` | `ReadSinglePoint(ref double meas, int timeout)` | 周期（秒） |
| `Pulse` | `ReadSinglePoint(ref double m1, ref double m2, int timeout)` | 低电平时长、高电平时长（秒） |
| `TwoEdgeSeparation` | `ReadSinglePoint(ref double m1, ref double m2, int timeout)` | 两边沿间隔（秒） |
| `QuadEncoder` | `ReadSinglePoint(ref uint count)` | 编码器计数 |

```csharp
// 频率测量示例
ciTask = new JYUSB1601CITask("USBDev0", 0);
ciTask.Type = CIType.Frequency;
ciTask.Start();
double freq;
ciTask.ReadSinglePoint(ref freq, -1);   // timeout=-1 永久等待
ciTask.Stop();
```

---

## 计数器输出（CO）

### 任务类：`JYUSB1601COTask`

```csharp
var coTask = new JYUSB1601COTask("USBDev0", 0);
// 按频率+占空比配置脉冲（1kHz，50% 占空比，输出 -1=无限个）
var pulse = new COPulse(COPulseType.DutyCycleFrequency, 1000.0, 0.5, -1);
coTask.WriteSinglePoint(pulse);
coTask.Start();
// ... 等待 ...
coTask.Stop();
```

`COPulseType` 三种构造方式：

| 类型 | param1 | param2 |
|------|--------|--------|
| `DutyCycleFrequency` | 频率（Hz） | 占空比（0~1） |
| `HighLowTime` | 高电平时长（秒） | 低电平时长（秒） |
| `HighLowTick` | 高电平 Tick 数 | 低电平 Tick 数 |

---

## 信号导出（Signal Export）

将内部信号输出到 DIO 引脚（如将 AI 采样时钟导出用于同步）：

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,
                        SignalExportDestination.DIO_0);
```

---

## 常见错误处理

| 异常代码 | 原因 | 处理建议 |
|----------|------|----------|
| `OpenDeviceFailed` | 板卡未连接或名称错误 | 检查设备管理器别名 |
| `NoChannelAdded` | 未调用 AddChannel 就 Start | 在 Start 前添加通道 |
| `BufferDataOverflow` | 读取速度慢于采集速度 | 增大读取频率或减小采样率 |
| `ReadDataTimeout` | timeout 内未读到足够数据 | 增大 timeout 或检查采样率 |
| `TaskHasStartedCannotPerformTheSetOperation` | Task 运行中修改参数 | Stop 后再修改 |
| `SampleRateParameterInvalid` | 采样率超过硬件上限 | 检查通道数和最大采样率 |

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)
