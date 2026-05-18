---
name: jy9819-driver
description: 提供 JYTEK JY9819（PXIe-9819）高速数字化仪（Digitizer）的完整 C# 驱动开发指引。涵盖高速模拟输入（AI）有限/连续/录制采集、多通道同步采集、触发配置（数字/模拟/软件触发、StartTrigger/ReferenceTrigger/ReTrigger）、带宽选择（全带宽/20 MHz）、外部参考时钟、多卡同步（<500 ps精度）、AC/DC耦合、阻抗匹配（50Ω/1MΩ）、数字输入/输出（DI/DO）、录制模式（Finite Streaming）。当用户使用 JY9819、PXIe-9819、JY9819AITask、JY9819DITask、JY9819DOTask、AIMode.Finite、AIMode.Continuous、AIMode.Record、AIBandwidth、RecordMode.Finite 开发高速数据采集、数字化仪、波形记录、射频信号分析应用时自动应用。
---

# JY9819 高速数字化仪驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9819\Bin\JY9819.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9819\Bin\JY9819.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9819;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查（基于简仪产品型录_20260331.pdf，文档页25）

| 功能                  | 规格                                    |
| --------------------- | --------------------------------------- |
| AI 分辨率             | 16-bit                                  |
| AI 通道数             | 4 通道同步采样                           |
| AI 最大采样率         | 250 MS/s（每通道）                       |
| AI 采样率范围         | 2 kS/s ~ 250 MS/s                       |
| AI 输入量程           | ±10V / ±5V / ±1V / ±0.5V               |
| AI 输入阻抗           | 50Ω / 1MΩ（软件可选）                   |
| AI 输入耦合           | AC / DC（软件可选）                      |
| AI 模拟带宽           | 110 MHz（全带宽模式）/ 20 MHz（低带宽可选）|
| AI DC 精度            | 0.3%（满量程）                           |
| 串扰                  | -103 dB（@1 MHz）                        |
| 板载内存              | 2 GB                                    |
| 多卡同步精度          | <500 ps                                  |

### 多卡同步

JY9819 支持多卡参考时钟同步，使用 `SyncTopology` 配置主从关系，实现高精度同步采集（<500 ps）：

```csharp
// ===== 主卡配置（Slot 4）=====
var masterTask = new JY9819AITask(4);
masterTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.Impedance50Ohm, AIBandwidth.Bandwidth_FULL);
masterTask.Mode = AIMode.Finite;
masterTask.SampleRate = 250e6;
masterTask.SamplesToAcquire = 10000;
masterTask.Trigger.Type = AITriggerType.Immediate;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 同步参数配置
masterTask.Sync.Topology = SyncTopology.Master;
masterTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;   // 路由采样时钟同步信号
masterTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;       // 路由触发同步信号

// 配置外部参考时钟（PXIe 100MHz 时钟）
masterTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
masterTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
masterTask.Device.ReferenceClock.Commit();

// ===== 从卡配置（Slot 6）=====
var slaveTask = new JY9819AITask(6);
slaveTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.Impedance50Ohm, AIBandwidth.Bandwidth_FULL);
slaveTask.Mode = AIMode.Finite;
slaveTask.SampleRate = 250e6;
slaveTask.SamplesToAcquire = 10000;

// 接收主卡的触发信号
slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig0;

// 同步参数配置
slaveTask.Sync.Topology = SyncTopology.Slave;
slaveTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
slaveTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;

// 配置外部参考时钟
slaveTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
slaveTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slaveTask.Device.ReferenceClock.Commit();

// ===== 提交同步配置（先从卡后主卡）=====
slaveTask.Sync.Commit();
masterTask.Sync.Commit();

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();   // 从卡先启动，等待触发
masterTask.Start();  // 主卡启动

// ===== 读取数据 =====
double[] masterData = new double[10000];
double[] slaveData = new double[10000];

masterTask.WaitUntilDone(-1);
slaveTask.WaitUntilDone(-1);

masterTask.ReadData(ref masterData, 10000, -1);
slaveTask.ReadData(ref slaveData, 10000, -1);

masterTask.Stop();
slaveTask.Stop();
masterTask.Channels.Clear();
slaveTask.Channels.Clear();
```

**关键要点：**
- 使用 `SyncTopology.Master/Slave` 配置主从关系
- 使用 `SyncTriggerRouting` 和 `SyncPulseRouting` 路由同步信号
- 配置外部参考时钟 `PXIe_Clk100`
- 必须调用 `Device.ReferenceClock.Commit()` 提交参考时钟配置
- 必须调用 `Sync.Commit()` 提交同步配置（先从卡后主卡）
- 启动顺序：**先从卡后主卡**
- 同步精度 <500 ps
| 触发方式              | 数字 / 模拟 / 软件触发                   |
| 触发模式              | StartTrigger / ReferenceTrigger / ReTrigger |
| 参考时钟              | 内部 / 外部（软件可选）                  |
| 数字 I/O              | 支持（DI/DO）                            |
| 接口                  | PXIe                                     |

### 动态性能（@10 MHz 输入信号）

| 量程  | 阻抗   | THD（dBc）| SFDR（dBc）| ENOB（bit）|
|-------|--------|-----------|------------|------------|
| ±0.5V | 50Ω   | -80        | 85         | 10.9       |
| ±1V   | 50Ω   | -81        | 82         | 11.3       |
| ±5V   | 50Ω   | -77        | 80         | 10.9       |
| ±10V  | 50Ω   | -73        | 78         | 10.1       |
| ±0.5V | 1MΩ   | -80        | 82         | 10.3       |
| ±1V   | 1MΩ   | -79        | 80         | 10.9       |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY9819AITask(0);          // 1. 创建（按槽位号）
// 注意：AddChannel 需要额外的 AIBandwidth 参数
task.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);
task.Mode = AIMode.Continuous;           // 3. 配置
task.SampleRate = 250000000;             // 250 MS/s
task.Start();                           // 4. 启动
// ... 读取数据 ...
task.Stop();                            // 5. 停止
task.Channels.Clear();                  // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY9819AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Finite / Continuous / Record            |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s），最高 250M         |
| `SamplesToAcquire`   | `ulong`                 | Finite/Record 模式下每通道预览/采集点数 |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数                    |
| `Trigger.Type`       | `AITriggerType`         | Immediate / Digital / Analog / Soft / MultiChannelAnalog |
| `Record`             | `AIRecord`              | 录制配置（Record 模式）                 |

#### AddChannel 重载

> **注意**：JY9819 的 `AddChannel` 比 JY9818 多一个 `AIBandwidth` 参数

```csharp
// 单通道
void AddChannel(int chnId, double rangeLow, double rangeHigh,
                AICoupling coupling, AIImpedance impedance, AIBandwidth bandwidth)

// 批量通道（统一参数）
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh,
                AICoupling coupling, AIImpedance impedance, AIBandwidth bandwidth)

// 批量通道（各自独立参数）
void AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh,
                AICoupling[] coupling, AIImpedance[] impedance, AIBandwidth[] bandwidth)

// chnId: 0~3（4 个通道）
// rangeLow: -10 / -5 / -1 / -0.5
// rangeHigh: 10 / 5 / 1 / 0.5
// coupling: AC / DC
// impedance: Impedance50Ohm / ImpedanceHigh
// bandwidth: Bandwidth_FULL（110 MHz全带宽）/ Bandwidth_20M（20 MHz低通滤波）
```

#### AIBandwidth 枚举

| 值              | 说明                                    |
| --------------- | --------------------------------------- |
| `Bandwidth_FULL`| 全带宽（110 MHz），适合高频信号         |
| `Bandwidth_20M` | 20 MHz 带宽，软件低通滤波，减少高频噪声 |

#### 读取数据

```csharp
// Finite/Continuous/Record — 单通道（一维数组）
void ReadData(ref double[] buf, int samplesPerChannel, int timeout);
void ReadData(ref double[] buf, int timeout);  // 自动读取所有可用样本

// Finite/Continuous/Record — 多通道（二维数组，按列排列）
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
void ReadData(ref double[,] buf, int timeout);  // 自动读取所有可用样本

// timeout = -1 → 永久等待
```

**重要：多通道数据排列格式**

- **单通道**：`double[]` 一维数组，长度 = 每通道点数
- **多通道**：`double[每通道点数, 通道数]` 二维数组，**每列代表一个通道的数据**

```csharp
// 示例（4通道，每通道1000点）
double[,] readValue = new double[1000, 4];   // [每通道点数, 通道数]
aiTask.ReadData(ref readValue, -1);

// 提取单个通道数据
for (int i = 0; i < 1000; i++)
{
    ch0Data[i] = readValue[i, 0];  // 第0列 = 通道0
    ch1Data[i] = readValue[i, 1];  // 第1列 = 通道1
}
```

#### AI 三种模式速查

| 模式         | 典型配置                      | 读取方式                                     |
| ------------ | ----------------------------- | -------------------------------------------- |
| `Finite`     | `+SamplesToAcquire=N`         | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                 | Timer 轮询 `AvailableSamples` → `ReadData`   |
| `Record`     | `Mode=AIMode.Record`          | 流式写入文件，通过 `GetRecordPreviewData` 预览 |

#### 连续采集 Timer 模式（最常用）

```csharp
// Timer Tick 中：
timer.Enabled = false;
if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
{
    aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
    easyChartX1.Plot(readBuffer);
}
timer.Enabled = true;
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

#### 模拟触发配置

#### Edge 比较器（边沿触发）

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;  // Rising / Falling
aiTask.Trigger.Analog.Edge.Threshold = 2.5;                       // 触发电平
```

#### Hysteresis 比较器（迟滞触发）

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Hysteresis;
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 3.0;  // 高阈值
aiTask.Trigger.Analog.Hysteresis.LowThreshold = 2.0;   // 低阈值
```

#### Window 比较器（窗口触发）

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Window;
aiTask.Trigger.Analog.Window.Condition = AIAnalogWindowCondition.Entering;  // Entering / Leaving
aiTask.Trigger.Analog.Window.HighThreshold = 3.0;
aiTask.Trigger.Analog.Window.LowThreshold = 2.0;
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
aiTask.SendSoftwareTrigger();
```

---

## 数字输入（DI）

### 任务类：`JY9819DITask`

```csharp
var diTask = new JY9819DITask(0);  // 按槽位号创建
diTask.AddChannel(0);              // 添加 DI 通道（0-based）
diTask.Start();

bool readValue = false;
diTask.ReadData(ref readValue);    // 单点读取

diTask.Stop();
diTask.Channels.Clear();
```

---

## 数字输出（DO）

### 任务类：`JY9819DOTask`

```csharp
var doTask = new JY9819DOTask(0);  // 按槽位号创建
doTask.AddChannel(0);              // 添加 DO 通道（0-based）
doTask.Start();

doTask.WriteData(true);            // 单点输出

doTask.Stop();
doTask.Channels.Clear();
```

---

## 录制模式（Record）

JY9819 支持 Record 模式，将数据流式写入文件（2 GB板载内存支持超长录制）：

```csharp
aiTask = new JY9819AITask(0);
aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);

aiTask.Mode = AIMode.Record;
aiTask.Record.Mode = RecordMode.Finite;   // 有限录制
aiTask.SampleRate = 250000000;            // 250 MS/s
aiTask.SamplesToAcquire = 100000;         // 预览缓冲点数/通道
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.Length = 5.0;              // 录制时长（秒）

aiTask.Start();
```

### Record 模式预览与状态查询

```csharp
// Timer Tick 中：
double recordedLength;
bool recordDone;
aiTask.GetRecordStatus(out recordedLength, out recordDone);

if (recordDone)
{
    aiTask.Stop();
    aiTask.Channels.Clear();
    MessageBox.Show("录制完成！");
}
else
{
    // 获取预览数据（多通道二维数组，按列排列）
    if (aiTask.AvailableSamples >= 10000)
    {
        double[,] previewData = new double[10000, aiTask.Channels.Count];
        aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
        // previewData[i, ch] = 第ch通道第i个预览点
    }
}
```

---

## 常见错误处理

| 异常代码                                         | 原因                     | 处理建议              |
| ------------------------------------------------ | ------------------------ | --------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误   | 检查设备管理器槽位号  |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start | 在 Start 前添加通道   |
| `BufferDataOverflow`                             | 读取速度慢于采集速度     | 增大读取频率或减小采样率 |
| `ReadDataTimeout`                                | timeout 内未读到足够数据 | 增大 timeout 或检查采样率 |
| `SampleRateParameterInvalid`                     | 采样率超过硬件上限       | 检查最大采样率 250M   |

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)


---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY9819;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9819\Bin\JY9819.dll
```

---

## JY9819AITask — 模拟输入任务

### 构造函数

```csharp
new JY9819AITask(int slotNumber)      // 按槽位号创建（推荐）
```

### 属性

| 属性                  | 类型                | 说明                                    |
| --------------------- | ------------------- | --------------------------------------- |
| `Mode`                | `AIMode`            | Finite / Continuous / Record            |
| `SampleRate`          | `double`            | 每通道采样率（Sa/s），最高 250M         |
| `SamplesToAcquire`    | `ulong`             | Finite/Record 预览点数/通道             |
| `BufLenInSamples`     | `ulong`             | 缓冲区可存储的样本数（每通道）          |
| `AvailableSamples`    | `ulong`             | 缓冲区可读点数                          |
| `TransferedSamples`   | `ulong`             | 已传输点数                              |
| `Trigger`             | `AITrigger`         | 触发配置对象                            |
| `SampleClock`         | `AISampleClock`     | 采样时钟配置对象                        |
| `SignalExport`        | `AISignalExport`    | 信号导出配置对象                        |
| `Osp`                 | `AIOsp`             | 板载信号处理配置对象（JY9819特有）      |
| `DisableTriggerDelay` | `bool`              | 禁用触发延迟（用于低精度同步触发对齐）  |
| `DisableMultiCardTriggerSync` | `bool`      | 禁用多卡触发同步（仅同步时钟）          |
| `Channels`            | `List<AIChannel>`   | 已添加的通道列表                        |
| `Device`              | `JY9819Device`      | 设备对象（含参考时钟配置）              |
| `Record`              | `AIRecord`          | 录制配置对象                            |

### 方法

#### AddChannel

```csharp
// JY9819 AddChannel 比 JY9818 多一个 AIBandwidth 参数
void AddChannel(int chnId, double rangeLow, double rangeHigh,
                AICoupling coupling, AIImpedance impedance, AIBandwidth bandwidth)
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh,
                AICoupling coupling, AIImpedance impedance, AIBandwidth bandwidth)
// chnId: 0~3
// rangeLow: -10 / -5 / -1 / -0.5
// rangeHigh: 10 / 5 / 1 / 0.5
// coupling: AC / DC
// impedance: Impedance50Ohm / ImpedanceHigh
// bandwidth: Bandwidth_FULL / Bandwidth_20M
```

#### 控制

```csharp
void Start()
void Stop()
void WaitUntilDone(int timeout)   // timeout=-1 永久等待，仅 Finite 有效
void SendSoftwareTrigger()        // 软触发
```

#### 读取数据

```csharp
// 单通道（一维数组）
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)
void ReadData(ref double[] buf, int timeout)

// 多通道（二维数组，按列排列：[每通道点数, 通道数]）
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)
void ReadData(ref double[,] buf, int timeout)
```

#### 录制相关

```csharp
// 预览录制数据（多通道二维数组：[预览点数, 通道数]）
void GetRecordPreviewData(ref double[,] buf, int samplesPerChannel, int timeout)
void GetRecordPreviewData(ref double[] buf, int samplesPerChannel, int timeout)

// 获取录制状态
void GetRecordStatus(out double recordedLength, out bool recordDone)  // 秒
void GetRecordStatus(out uint recordedSamples, out bool recordDone)   // 样本数
```

---

## AIMode 枚举

| 值           | 说明                                         |
| ------------ | -------------------------------------------- |
| `Finite`     | 采集固定点数后停止，通过 WaitUntilDone 等待完成 |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取     |
| `Record`     | 录制模式（流式采集到文件，板载 2 GB 内存缓存） |

---

## AICoupling 枚举

| 值     | 说明       |
| ------ | ---------- |
| `AC`   | 交流耦合   |
| `DC`   | 直流耦合   |

---

## AIImpedance 枚举

| 值              | 说明       |
| --------------- | ---------- |
| `Impedance50Ohm`| 50Ω 阻抗   |
| `ImpedanceHigh` | 1MΩ 高阻抗 |

---

## AIBandwidth 枚举

| 值              | 说明                              |
| --------------- | --------------------------------- |
| `Bandwidth_FULL`| 全带宽（110 MHz），适合宽频信号   |
| `Bandwidth_20M` | 20 MHz 带宽，软件滤波降低高频噪声 |

---

## AITrigger — 触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;    // Digital / Analog / Soft / Immediate
aiTask.Trigger.Mode = AITriggerMode.Start;      // Start / Reference
```

### PreTriggerSamples（预触发采样）

当 `Trigger.Mode = AITriggerMode.Reference` 时，可以配置预触发采样点数：

```csharp
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 10000;  // 总采集点数
aiTask.Trigger.Mode = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 2000;  // 触发前采样 2000 点
// 触发后采样 8000 点
```

### 数字触发

```csharp
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;       // Rising / Falling
```

### 模拟触发

```csharp
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;  // Channel_0~Channel_3
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
```

### AITriggerType 枚举

| 值           | 说明                    |
| ------------ | ----------------------- |
| `Immediate`  | 立即触发（默认）        |
| `Digital`    | 数字边沿触发            |
| `Analog`     | 模拟边沿触发            |
| `Soft`       | 软件触发                |
| `MultiChannelAnalog` | 多通道模拟触发（多通道联合触发） |

### AIAnalogTriggerComparator 枚举

| 值           | 说明                         |
| ------------ | ---------------------------- |
| `Edge`       | 边沿比较器，单阈值触发电平     |
| `Hysteresis` | 迟滞比较器，双阈值防止误触发   |
| `Window`     | 窗口比较器，信号进入/离开窗口时触发 |

### AIAnalogTriggerEdge 枚举

| 值       | 说明   |
| -------- | ------ |
| `Rising` | 上升沿 |
| `Falling`| 下降沿 |

### AIAnalogWindowCondition 枚举

| 值         | 说明                   |
| ---------- | ---------------------- |
| `Entering` | 信号进入窗口区域时触发   |
| `Leaving`  | 信号离开窗口区域时触发   |

### AITriggerMode 枚举

| 值           | 说明                                    |
| ------------ | --------------------------------------- |
| `Start`      | 启动触发模式，触发后开始采集              |
| `Reference`  | 参考触发模式，仅在 Finite 模式下有效，支持预触发采样 |

---

## AIRecord — 录制配置

```csharp
aiTask.Mode = AIMode.Record;
aiTask.Record.FilePath = @"C:\Data\record.bin";
aiTask.Record.FileFormat = FileFormat.Bin;  // 文件格式（目前仅支持 Bin）
aiTask.Record.Mode = RecordMode.Finite;  // Finite 有限录制
aiTask.Record.Length = 5.0;              // 录制时长（秒）

// 高级配置（可选）
aiTask.Record.Advanced.PreviewBufferSize = 1048576;       // 预览缓冲区大小（字节）
aiTask.Record.Advanced.SamplesToPreviewBuffer = 10000;    // 每次写入预览缓冲区的样本数
aiTask.Record.Advanced.BlockSize = 524288;                // 每个录制数据块大小（字节）
aiTask.Record.Advanced.BlockCount = 10;                   // 录制缓冲块数量
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `FilePath` | `string` | 录制文件路径 |
| `FileFormat` | `FileFormat` | 文件格式（Bin 二进制格式） |
| `Mode` | `RecordMode` | 录制模式（Finite/Infinite） |
| `Length` | `double` | 录制时长（秒），仅在 Finite 模式下有效 |
| `Advanced` | `AIRecordAdvanced` | 高级录制配置对象 |

---

## RecordMode 枚举

| 值 | 说明 |
|----|------|
| `Finite` | 有限录制，录制指定时长后自动停止 |
| `Infinite` | 无限录制，持续录制直到手动停止 |

---

## FileFormat 枚举

| 值 | 说明 |
|----|------|
| `Bin` | 二进制文件格式 |

---

## AIRecordAdvanced — 录制高级配置

| 属性 | 类型 | 说明 |
|------|------|------|
| `PreviewBufferSize` | `uint` | 预览缓冲区大小（字节） |
| `SamplesToPreviewBuffer` | `uint` | 每次写入预览缓冲区的样本数 |
| `BlockSize` | `uint` | 每个录制数据块大小（字节） |
| `BlockCount` | `uint` | 录制缓冲块数量 |

---

## SignalExport — 信号导出配置

JY9819 支持将内部信号导出到外部端子，用于多卡同步或触发其他设备：

```csharp
// 导出采样完成信号到 PXI_Trig0
aiTask.SignalExport.SampleComplete = SignalExportDestination.PXI_Trig0;

// 导出开始触发信号到 PXI_Trig1
aiTask.SignalExport.StartTrig = SignalExportDestination.PXI_Trig1;

// 导出参考触发信号到 DStarC 总线
aiTask.SignalExport.ReferenceTrig = SignalExportDestination.PXIe_DStarC;

// 提交信号导出配置
aiTask.SignalExport.Commit();
```

### SignalExportDestination 枚举

| 值 | 说明 |
|----|------|
| `SyncSignal` | 同步信号 |
| `StartTrig` | 开始触发信号 |
| `ReferenceTrig` | 参考触发信号 |
| `PXIe_DStarC` | PXIe DStarC 总线 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发线 0~7 |

---

## SampleClock — 采样时钟配置

JY9819 支持内部和外部采样时钟源：

```csharp
// 使用内部采样时钟（默认）
aiTask.SampleClock.Source = AISampleClockSource.Internal;
aiTask.SampleClock.Internal.Rate = 250e6;  // 250 MS/s

// 或使用外部采样时钟
aiTask.SampleClock.Source = AISampleClockSource.External;
// aiTask.SampleClock.External.Terminal = ...  // 配置外部时钟端子
```

### AISampleClockSource 枚举

| 值 | 说明 |
|----|------|
| `Internal` | 内部时钟源（默认） |
| `External` | 外部时钟源 |

---

## OSP — 板载信号处理（JY9819 特有）

JY9819 支持板载信号处理（Onboard Signal Processing, OSP），可在硬件层面进行数据降采样、移动平均滤波和外部时钟重采样：

### 基本配置

```csharp
// 设置数据格式为实数（ADC 原始数据）
aiTask.Osp.DataFormat = AIOspDataFormat.Real;

// 设置数据处理模式
aiTask.Osp.Real.Mode = AIOspRealDataProcessingMode.None;          // 无处理
// aiTask.Osp.Real.Mode = AIOspRealDataProcessingMode.Downsample; // 整数倍降采样
// aiTask.Osp.Real.Mode = AIOspRealDataProcessingMode.ExternalClockResample; // 外部时钟重采样
```

### 移动平均滤波

```csharp
// 启用移动平均滤波
aiTask.Osp.Real.MovingAverage.Enabled = true;
aiTask.Osp.Real.MovingAverage.SizeAuto = true;   // 自动计算平均窗口大小
// 或手动设置
aiTask.Osp.Real.MovingAverage.SizeAuto = false;
aiTask.Osp.Real.MovingAverage.Size = 10;         // 平均窗口大小
```

### 外部时钟重采样

```csharp
aiTask.Osp.Real.Mode = AIOspRealDataProcessingMode.ExternalClockResample;

// 配置外部重采样时钟源
aiTask.Osp.Real.ExtClkResample.Terminal = AIOspExtResampleClockSource.PFI0;
aiTask.Osp.Real.ExtClkResample.ExpectedRate = 10e6;  // 期望重采样率 10 MS/s
```

### AIOspDataFormat 枚举

| 值 | 说明 |
|----|------|
| `Real` | 实数格式，ADC 原始数据 |
| `Complex` | 复数格式，DDC（数字下变频）IQ 数据 |

### AIOspRealDataProcessingMode 枚举

| 值 | 说明 |
|----|------|
| `None` | 无处理，直接输出 ADC 数据 |
| `Downsample` | 整数倍降采样 |
| `ExternalClockResample` | 外部时钟重采样 |

### AIOspExtResampleClockSource 枚举

| 值 | 说明 |
|----|------|
| `PFI0` ~ `PFI7` | 前面板 PFI 端子 0~7 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发线 0~7 |

---

## JY9819DITask — 数字输入任务

```csharp
new JY9819DITask(int slotNumber)
void AddChannel(int chnId)         // 添加 DI 通道（0-based）
void Start()
void Stop()
void ReadData(ref bool buf)        // 单点读取
void Channels.Clear()
```

---

## JY9819DOTask — 数字输出任务

```csharp
new JY9819DOTask(int slotNumber)
void AddChannel(int chnId)         // 添加 DO 通道（0-based）
void Start()
void Stop()
void WriteData(bool value)         // 单点输出
void Channels.Clear()
```

---

## 异常类 JYDriverException

```csharp
try { ... }
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);
}
```

---

## 设备硬件规格

| 参数           | 值                                      |
| -------------- | --------------------------------------- |
| AI 分辨率      | 16-bit                                  |
| AI 通道数      | 4                                       |
| AI 最大采样率  | 250 MS/s（每通道）                      |
| AI 采样率范围  | 2 kS/s ~ 250 MS/s                       |
| AI 输入量程    | ±10V / ±5V / ±1V / ±0.5V               |
| AI 输入阻抗    | 50Ω / 1MΩ（软件可选）                  |
| AI 输入耦合    | AC / DC（软件可选）                     |
| AI 模拟带宽    | 110 MHz（全带宽）/ 20 MHz（低通可选）   |
| AI DC 精度     | 0.3%（满量程）                          |
| 串扰           | -103 dB（@1 MHz）                       |
| 板载内存       | 2 GB                                    |
| 多卡同步精度   | <500 ps                                 |
| 触发方式       | 数字 / 模拟 / 软件                      |
| 接口           | PXIe                                    |

---

# 完整代码示例

# JY9819 代码示例集

> 所有示例均来自 `JY9819_V1.0.0_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 连续采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY9819;

public partial class MainForm : Form
{
    private JY9819AITask aiTask;
    private double[] readBuffer;

    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            // 1. 创建 Task
            aiTask = new JY9819AITask(0);

            // 2. 添加通道（DC 耦合，1MΩ 阻抗，全带宽）
            // 注意：JY9819 AddChannel 有 AIBandwidth 额外参数
            aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);

            // 3. 配置连续模式
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 250000000;  // 250 MS/s

            // 4. 启动 Task
            aiTask.Start();

            // 5. 分配缓冲区
            readBuffer = new double[10000];

            // 6. 启动 Timer
            timer_FetchData.Enabled = true;
            button_start.Enabled = false;
            button_stop.Enabled = true;
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
            if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
            {
                aiTask.ReadData(ref readBuffer, readBuffer.Length, -1);
                easyChartX1.Plot(readBuffer);
            }
        }
        catch (JYDriverException ex) 
        { 
            MessageBox.Show(ex.Message);
            return; 
        }
        timer_FetchData.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
        button_start.Enabled = true;
        button_stop.Enabled = false;
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (aiTask != null) aiTask.Stop();
    }
}
```

---

## 示例 2：AI 有限采集（多通道）

```csharp
private JY9819AITask aiTask;
private double[,] readValue;  // [每通道点数, 通道数]

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JY9819AITask(0);

    // 添加多个通道（4路，DC 耦合，50Ω 阻抗，全带宽）
    int[] channels = { 0, 1, 2, 3 };
    aiTask.AddChannel(channels, -5, 5, AICoupling.DC, AIImpedance.Impedance50Ohm, AIBandwidth.Bandwidth_FULL);

    // 配置有限模式
    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 10000;  // 每通道采集点数
    aiTask.SampleRate = 250000000;    // 250 MS/s

    aiTask.Start();

    // 分配缓冲区：[每通道点数, 通道数]
    readValue = new double[10000, aiTask.Channels.Count];

    timer_FetchData.Enabled = true;
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    if (aiTask.AvailableSamples >= aiTask.SamplesToAcquire)
    {
        // 读取多通道数据到二维数组
        aiTask.ReadData(ref readValue, -1);
        
        // 提取单个通道数据示例
        double[] ch0Data = new double[10000];
        for (int i = 0; i < 10000; i++)
        {
            ch0Data[i] = readValue[i, 0];  // 第0列 = 通道0
        }
        
        easyChartX1.Plot(ch0Data);
        
        aiTask.Stop();
        aiTask.Channels.Clear();
        timer_FetchData.Enabled = false;
    }
}
```

---

## 示例 3：AI 数字触发采集

```csharp
aiTask = new JY9819AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 250000000;

// 配置数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发信号到来后自动开始采集
```

---

## 示例 4：AI 模拟触发采集

```csharp
aiTask = new JY9819AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 10000;
aiTask.SampleRate = 250000000;

// 配置模拟触发
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;

aiTask.Start();
```

---

## 示例 5：AI 软触发采集

```csharp
aiTask = new JY9819AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 250000000;

aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();

// 手动发送软触发
aiTask.SendSoftwareTrigger();
```

---

## 示例 6：20 MHz 带宽模式（低噪声采集）

```csharp
aiTask = new JY9819AITask(0);

// 使用 20 MHz 低通滤波，适合低频信号降噪
aiTask.AddChannel(0, -1, 1, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_20M);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 50000000;  // 50 MS/s

aiTask.Start();
```

---

## 示例 7：Record 有限录制模式（Finite Streaming）

```csharp
private JY9819AITask aiTask;
private double[,] previewData;  // 预览数据：[预览点数, 通道数]

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9819AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_FULL);
        
        aiTask.Mode = AIMode.Record;
        aiTask.Record.Mode = RecordMode.Finite;   // 有限录制模式
        aiTask.SampleRate = 250000000;             // 250 MS/s
        aiTask.SamplesToAcquire = 10000;           // 预览缓冲点数
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Length = 5.0;               // 录制 5 秒
        
        aiTask.Start();
        previewData = new double[10000, aiTask.Channels.Count];
        timer_Preview.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    double recordedLength;
    bool recordDone;
    aiTask.GetRecordStatus(out recordedLength, out recordDone);
    
    if (recordDone)
    {
        try { if (aiTask != null) aiTask.Stop(); }
        catch (JYDriverException ex) { MessageBox.Show(ex.Message); return; }
        
        aiTask.Channels.Clear();
        timer_Preview.Enabled = false;
        MessageBox.Show($"录制完成！已录制 {recordedLength:F1} 秒");
    }
    else
    {
        if (aiTask.AvailableSamples >= 10000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
            // previewData[i, ch] 按列排列
        }
        timer_Preview.Enabled = true;
    }
}
```

---

## 示例 8：数字输入（DI）单点采集

```csharp
private JY9819DITask diTask;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        diTask = new JY9819DITask(0);

        // 添加需要采集的 DI 通道
        diTask.AddChannel(0);  // 通道0
        diTask.AddChannel(1);  // 通道1

        diTask.Start();
        timer_FetchData.Enabled = true;
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void timer_FetchData_Tick(object sender, EventArgs e)
{
    bool readValue = false;
    diTask.ReadData(ref readValue);
    textBox_diValue.Text = readValue.ToString();
}

private void button_stop_Click(object sender, EventArgs e)
{
    timer_FetchData.Enabled = false;
    if (diTask != null)
    {
        diTask.Stop();
        diTask.Channels.Clear();
    }
}
```

---

## 示例 9：数字输出（DO）单点控制

```csharp
private JY9819DOTask doTask;

private void button_start_Click(object sender, EventArgs e)
{
    try
    {
        doTask = new JY9819DOTask(0);
        doTask.AddChannel(0);  // 添加 DO 通道0
        doTask.Start();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}

private void button_setHigh_Click(object sender, EventArgs e)
{
    doTask.WriteData(true);   // 输出高电平
}

private void button_setLow_Click(object sender, EventArgs e)
{
    doTask.WriteData(false);  // 输出低电平
}

private void button_stop_Click(object sender, EventArgs e)
{
    if (doTask != null)
    {
        doTask.Stop();
        doTask.Channels.Clear();
    }
}
```

---

## 综合技巧

### AIBandwidth 选择建议

```csharp
// 高频信号（RF、雷达、高速瞬态）
aiTask.AddChannel(0, -5, 5, AICoupling.AC, AIImpedance.Impedance50Ohm, AIBandwidth.Bandwidth_FULL);  // 110 MHz

// 低频信号（音频、振动，需降低高频噪声）
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh, AIBandwidth.Bandwidth_20M);  // 20 MHz
```

### 多通道数据解析

```csharp
// JY9819 多通道采集返回 double[每通道点数, 通道数]，每列为一个通道
int channelCount = aiTask.Channels.Count;  // 最多 4 通道
int samplesPerChannel = 10000;

double[,] readValue = new double[samplesPerChannel, channelCount];
aiTask.ReadData(ref readValue, -1);

// 提取各通道
double[] ch0 = new double[samplesPerChannel];
double[] ch1 = new double[samplesPerChannel];
for (int i = 0; i < samplesPerChannel; i++)
{
    ch0[i] = readValue[i, 0];
    ch1[i] = readValue[i, 1];
}

// 使用 SeeSharpTools 转置后直接绘制多通道图
double[,] displayValue = new double[channelCount, samplesPerChannel];
ArrayManipulation.Transpose(readValue, ref displayValue);
easyChartX_readData.Plot(displayValue);
```

### 窗体关闭资源释放

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer_FetchData != null) timer_FetchData.Enabled = false;
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
}
```

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)
