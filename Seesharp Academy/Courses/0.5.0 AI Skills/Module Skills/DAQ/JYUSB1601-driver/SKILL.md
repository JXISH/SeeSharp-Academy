---
name: jyusb1601-driver
description: 提供 JYTEK JYUSB-1601 多功能数据采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）单点/有限/连续/录制采集、模拟输出（AO）单点/有限/连续输出、数字输入/输出（DI/DO）单点/有限/连续模式、计数器输入/输出（CI/CO）边沿计数/频率/周期/脉冲测量/脉冲生成、触发配置（数字/模拟/软件触发）、时钟配置（内部/外部时钟）、录制模式（Finite/Infinite Streaming）。当用户使用 JYUSB1601、USB-1601、JYUSB1601AITask、JYUSB1601AOTask、JYUSB1601CITask、JYUSB1601COTask、JYUSB1601DITask、JYUSB1601DOTask、AIMode.Single、AIMode.Finite、AIMode.Continuous、AIMode.Record 开发数据采集、信号生成、计数器测量、自动化测试应用时自动应用。
---

# JYUSB-1601 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB1601\Bin\JYUSB1601.dll`（引用到 .csproj）
- **目标框架**：.NET Framework 4.0 或更高版本
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

- `AddChannel(chnId, rangeLow, rangeHigh)` — 量程 ±10/±5/±2.5V；`chnId=-1` 全部通道
- 可选 `AITerminal.RSE`（16ch 单端）/ `AITerminal.Differential`（8ch 差分）

#### AI 四种模式速查

| 模式 | 典型配置 | 读取方式 |
|------|----------|----------|
| `Single` | `Mode=AIMode.Single` | 轮询 `ReadSinglePoint` |
| `Finite` | `+SamplesToAcquire=N` | `WaitUntilDone(-1)` 后 `ReadData` |
| `Continuous` | `+SampleRate` | Timer 轮询 `AvailableSamples` → `ReadData` |
| `Record` | `+Record.FilePath/Mode/Length` | `GetRecordPreviewData` 预览，数据写入文件 |

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

- `AddChannel(chnId)` — 仅 0/1 两通道，`chnId=-1` 全部

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

- **`JYUSB1601DITask`**：8 线输入（DIO_0~DIO_7），`ReadSinglePoint` 读取
- **`JYUSB1601DOTask`**：8 线输出，`WriteSinglePoint` 写入

> 完整 API 见 [reference.md](reference.md)，代码示例见 [examples.md](examples.md) 示例 15。

---

## 计数器输入（CI）

### 任务类：`JYUSB1601CITask`

**构造**：`new JYUSB1601CITask(string boardName, int channel)` — channel 0 或 1

| CIType | 说明 | 返回值 |
|--------|------|--------|
| `EdgeCounting` | 边沿计数 | `uint` 计数值 |
| `Frequency` | 频率测量 | `double` Hz |
| `Period` | 周期测量 | `double` 秒 |
| `Pulse` | 脉冲宽度 | 高/低电平时长（秒） |
| `TwoEdgeSeparation` | 两边沿间隔 | `double` 秒 |
| `QuadEncoder` | 正交编码器 | `uint` 计数 |

---

## 计数器输出（CO）

### 任务类：`JYUSB1601COTask`

通过 `COPulse` 配置脉冲参数：

| COPulseType | param1 | param2 |
|------|--------|--------|
| `DutyCycleFrequency` | 频率（Hz） | 占空比（0~1） |
| `HighLowTime` | 高电平时长（秒） | 低电平时长（秒） |
| `HighLowTick` | 高电平 Tick 数 | 低电平 Tick 数 |

> `count = -1` 表示无限循环输出。

---

## 录制模式（Record）

`AIMode.Record` 将采集数据流式写入文件，支持 `GetRecordPreviewData` 预览。

| 配置属性 | 说明 |
|----------|------|
| `Record.FilePath` | 录制文件路径 |
| `Record.FileFormat` | 仅支持 `FileFormat.Bin` |
| `Record.Mode` | `RecordMode.Finite`（定长）/ `RecordMode.Infinite`（无限） |
| `Record.Length` | 录制时长（秒），Finite 有效 |

> 完整录制流程见 [examples.md](examples.md) 示例 7。

---

## 信号导出（Signal Export）

将内部信号输出到 DIO 引脚（如将 AI 采样时钟导出用于同步）：

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,
                        SignalExportDestination.DIO_0);
```

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)
