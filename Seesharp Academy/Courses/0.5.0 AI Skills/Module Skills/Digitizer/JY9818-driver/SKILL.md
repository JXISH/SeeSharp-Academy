---
name: jy9818-driver
description: 提供 JYTEK JY9818 高速数字化仪（Digitizer）的完整 C# 驱动开发指引。涵盖高速模拟输入（AI）有限/连续/录制采集、多通道同步采集、触发配置（数字/模拟/软件触发、预触发采集）、外部参考时钟、多卡同步（Master/Slave拓扑）、AC/DC耦合、阻抗匹配（50Ω/1MΩ）、信号导出、录制模式（Finite/Infinite Streaming）。当用户使用 JY9818、PXIe-9818、PCIe-9818、JY9818AITask、AIMode.Finite、AIMode.Continuous、AIMode.Record、SyncTopology.Master、SyncTopology.Slave 开发高速数据采集、数字化仪、波形记录、射频信号分析应用时自动应用。
---

# JY9818 高速数字化仪驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9818\Bin\JY9818.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9818\Bin\JY9818.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY9818;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                  | 规格                                    |
| --------------------- | --------------------------------------- |
| AI 分辨率             | 16-bit                                  |
| AI 通道数             | 8 通道                                  |
| AI 最大采样率         | 125 MS/s（每通道）                       |
| AI 输入量程           | ±10V / ±5V / ±1V / ±0.5V               |
| AI 输入阻抗           | 50Ω / 1MΩ（可配置）                     |
| AI 输入耦合           | AC / DC（可配置）                        |
| 参考时钟              | 内部 / 外部（PXIe/PCIe 总线）            |
| 触发方式              | 数字 / 模拟 / 软件触发                   |
| 信号导出              | 支持将触发信号导出到 PXI 触发总线         |
| 接口                  | PXIe / PCIe                              |

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY9818AITask(0);          // 1. 创建（按槽位号）
task.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);  // 2. 添加通道
task.Mode = AIMode.Continuous;            // 3. 配置
task.SampleRate = 100000000;              // 100 MS/s
task.Start();                            // 4. 启动
// ... 读取数据 ...
task.Stop();                             // 5. 停止
task.Channels.Clear();                   // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY9818AITask`

#### 关键属性

| 属性                 | 类型                    | 说明                                    |
| -------------------- | ----------------------- | --------------------------------------- |
| `Mode`               | `AIMode`                | Finite / Continuous / Record            |
| `SampleRate`         | `double`                | 每通道采样率（Sa/s），最高 125M         |
| `SamplesToAcquire`   | `ulong`                 | Finite 模式下每通道采集点数             |
| `AvailableSamples`   | `ulong`                 | 缓冲区中可读取的点数（Finite/Continuous/Record 模式） |
| `Trigger.Type`       | `AITriggerType`         | Immediate / Digital / Analog / Soft     |
| `Device.ReferenceClock` | `ReferenceClock`     | 参考时钟配置（内部/外部）               |

#### AddChannel 重载

```csharp
// 单通道
void AddChannel(int chnId, double rangeLow, double rangeHigh, AICoupling coupling, AIImpedance impedance)

// 批量通道（统一参数）
void AddChannel(int[] chnIds, double rangeLow, double rangeHigh, AICoupling coupling, AIImpedance impedance)

// 批量通道（各自独立参数）
void AddChannel(int[] chnIds, double[] rangeLow, double[] rangeHigh, AICoupling[] coupling, AIImpedance[] impedance)

// chnId: 0~7（8 个通道）
// rangeLow: -10 / -5 / -1 / -0.5
// rangeHigh: 10 / 5 / 1 / 0.5
// coupling: AC / DC
// impedance: Impedance50Ohm / Impedance1M
```

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

示例（3通道，每通道4个点）：
```csharp
// 缓冲区定义：[每通道点数, 通道数]
double[,] readValue = new double[4, 3];

// 数据结构：
// readValue[0,0]=CH0_P1  readValue[0,1]=CH1_P1  readValue[0,2]=CH2_P1
// readValue[1,0]=CH0_P2  readValue[1,1]=CH1_P2  readValue[1,2]=CH2_P2
// readValue[2,0]=CH0_P3  readValue[2,1]=CH1_P3  readValue[2,2]=CH2_P3
// readValue[3,0]=CH0_P4  readValue[3,1]=CH1_P4  readValue[3,2]=CH2_P4

// 提取单个通道数据
for (int i = 0; i < 4; i++)
{
    ch0Data[i] = readValue[i, 0];  // 第0列 = 通道0
    ch1Data[i] = readValue[i, 1];  // 第1列 = 通道1
    ch2Data[i] = readValue[i, 2];  // 第2列 = 通道2
}
```

#### AI 三种模式速查

| 模式         | 典型配置                      | 读取方式                                     |
| ------------ | ----------------------------- | -------------------------------------------- |
| `Finite`     | `+SamplesToAcquire=N`         | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                 | Timer 轮询 `AvailableSamples` → `ReadData`   |
| `Record`     | `Mode=AIMode.Record`          | 流式写入板载内存，通过 `ReadData` 读取       |

#### 连续采集 Timer 模式（最常用）

```csharp
// Timer Tick 中：
timer.Enabled = false;
if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
{
    aiTask.ReadData(ref readBuffer, (ulong)readBuffer.Length, -1);
    easyChartX1.Plot(readBuffer);
}
timer.Enabled = true;
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

#### 模拟触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
aiTask.SendSoftwareTrigger();
```

#### 参考时钟配置

```csharp
// 使用外部参考时钟
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Frequency = 10000000;  // 10 MHz
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
aiTask.Device.ReferenceClock.Commit();
```

#### 信号导出

```csharp
// 将起始触发信号导出到 PXI 触发总线
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);
```

---

## 录制模式（Record）

JY9818 高速数字化仪支持 Record 模式，数据流式写入板载内存：

```csharp
aiTask = new JY9818AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTask.Mode = AIMode.Record;
aiTask.SampleRate = 100000000;  // 100 MS/s

aiTask.Start();

// Record 模式下读取数据
double[] buffer = new double[10000];
aiTask.ReadData(ref buffer, 10000, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

### Record 模式特点

- 数据直接写入板载内存，支持高速采集
- 适用于需要长时间连续采集的场景
- 可与触发配置配合使用
- 通过 `ReadData` 方法读取已录制的数据

---

## 常见错误处理

| 异常代码                                         | 原因                     | 处理建议              |
| ------------------------------------------------ | ------------------------ | --------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误   | 检查设备管理器槽位号  |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start | 在 Start 前添加通道   |
| `BufferDataOverflow`                             | 读取速度慢于采集速度     | 增大读取频率或减小采样率 |
| `ReadDataTimeout`                                | timeout 内未读到足够数据 | 增大 timeout 或检查采样率 |
| `SampleRateParameterInvalid`                     | 采样率超过硬件上限       | 检查最大采样率 125M   |

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)


---

# 完整 API 参考

# JY9818 驱动 API 完整参考

## 命名空间与引用

```csharp
using JY9818;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\Digitizer\JY9818\Bin\JY9818.dll
```

---

## JY9818AITask — 模拟输入任务

### 构造函数

```csharp
new JY9818AITask(int slotNumber)      // 按槽位号创建（推荐）
new JY9818AITask(string boardName)    // 按板卡名称创建（设备别名）
```

### 属性

| 属性                  | 类型                | 默认值      | 说明                                    |
| --------------------- | ------------------- | ----------- | --------------------------------------- |
| `Mode`                | `AIMode`            | —           | Finite / Continuous / Record            |
| `SampleRate`          | `double`            | —           | 每通道采样率（Sa/s），最高 125M         |
| `SamplesToAcquire`    | `ulong`             | —           | Finite 模式采集点数/通道                |
| `AvailableSamples`    | `ulong`             | —           | 缓冲区可读点数                          |
| `TransferedSamples`   | `ulong`             | —           | 已传输点数                              |
| `BufLenInSamples`     | `int`               | —           | 缓冲区每通道最大容量                    |
| `Trigger`             | `AITrigger`         | —           | 触发配置对象                            |
| `SignalExport`        | `AISignalExport`    | —           | 信号导出配置                            |
| `Channels`            | `List<AIChannel>`   | —           | 已添加的通道列表                        |
| `Device`              | `JY9818Device`      | —           | 设备对象（含参考时钟配置）              |
| `Record`              | `AIRecord`          | —           | 录制配置对象                            |

### 方法

#### AddChannel

```csharp
void AddChannel(int chnId, double rangeLow, double rangeHigh, AICoupling coupling, AIImpedance impedance)
void AddChannel(int[] chnsId, double rangeLow, double rangeHigh, AICoupling coupling, AIImpedance impedance)
// chnId: 0~7
// rangeLow: -10 / -5 / -1 / -0.5
// rangeHigh: 10 / 5 / 1 / 0.5
// coupling: AC / DC
// impedance: Impedance50Ohm / ImpedanceHigh
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
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)      // 指定样本数
void ReadData(ref double[] buf, int timeout)                              // 读取所有可用样本

// 多通道（二维数组，按列排列：[每通道点数, 通道数]）
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)      // 指定样本数
void ReadData(ref double[,] buf, int timeout)                              // 读取所有可用样本

// 原生指针接口（支持单/多通道）
void ReadData(IntPtr buf, int samplesPerChannel, int timeout)
```

#### 通道管理

```csharp
// 移除指定通道（chnId=-1 表示移除所有通道）
void RemoveChannel(int chnId)

// 清除所有通道（与 Channels.Clear() 等效）
void RemoveChannel(-1)
```

#### 录制相关

```csharp
// 预览录制数据
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
| `Record`     | 记录模式（流式采集到板载内存）               |

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

## AITrigger — 触发配置

### 属性

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;    // Digital / Analog / Soft / Immediate
```

### 数字触发

```csharp
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;       // Rising / Falling
```

### 模拟触发

#### 边沿比较器
```csharp
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;        // Channel_0~Channel_7
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;
```

#### 滞后比较器
```csharp
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Hysteresis;
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 3.0;
aiTask.Trigger.Analog.Hysteresis.LowThreshold = 1.0;
```

#### 窗口比较器
```csharp
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Window;
aiTask.Trigger.Analog.Window.Condition = AIAnalogWindowCondition.Entering;
aiTask.Trigger.Analog.Window.HighThreshold = 3.0;
aiTask.Trigger.Analog.Window.LowThreshold = 1.0;
```

### AITriggerType 枚举

| 值           | 说明                    |
| ------------ | ----------------------- |
| `Immediate`  | 立即触发（默认）        |
| `Digital`    | 数字边沿触发            |
| `Analog`     | 模拟边沿触发            |
| `Soft`       | 软件触发                |
| `MultichannelAnalog` | 多通道模拟触发  |

### AIDigitalTriggerSource 枚举

| 值                  | 说明                    |
| ------------------- | ----------------------- |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7 |
| `PFI_0`             | 前面板 PFI_0 数字触发输入 |
| `PXIe_DStarB`       | PXIe DStarB 触发输入    |
| `PXIe_DStarC`       | PXIe DStarC 触发输入    |

### AIDigitalTriggerEdge 枚举

| 值           | 说明         |
| ------------ | ------------ |
| `Rising`     | 上升沿触发   |
| `Falling`    | 下降沿触发   |

### AIAnalogTriggerComparator 枚举

| 值           | 说明         |
| ------------ | ------------ |
| `Edge`       | 边沿比较器   |
| `Hysteresis` | 滞后比较器   |
| `Window`     | 窗口比较器   |

### AIAnalogTriggerSource 枚举

| 值           | 说明         |
| ------------ | ------------ |
| `Channel_0` ~ `Channel_7` | AI 通道 0~7 作为触发源 |

### AIAnalogTriggerEdge 枚举

| 值           | 说明         |
| ------------ | ------------ |
| `Rising`     | 上升沿       |
| `Falling`    | 下降沿       |

### AIAnalogWindowCondition 枚举

| 值           | 说明         |
| ------------ | ------------ |
| `Entering`   | 进入窗口     |
| `Leaving`    | 离开窗口     |

---

## AITrigger 其他属性

### 触发模式

```csharp
aiTask.Trigger.Mode = AITriggerMode.Start;      // 起始触发（默认）
aiTask.Trigger.Mode = AITriggerMode.Reference;  // 参考触发（仅Finite模式有效）
```

### 重触发配置

```csharp
// 重触发次数：0或1表示仅触发一次，-1表示连续重触发直到停止
aiTask.Trigger.ReTriggerCount = -1;

// 重触发频率（Hz），用于计算缓冲区大小，Record模式下无效，默认0表示自动设置
aiTask.Trigger.ReTriggerFrequency = 1000;
```

### 预触发采集（仅Reference模式有效）

```csharp
// 预触发采样点数，必须小于等于 SamplesToAcquire
aiTask.Trigger.PreTriggerSamples = 1000;
```

### AITriggerMode 枚举

| 值           | 说明                    |
| ------------ | ----------------------- |
| `Start`      | 起始触发模式（默认）    |
| `Reference`  | 参考触发模式            |

### AIAnalogComparatorLogic 枚举

| 值           | 说明                    |
| ------------ | ----------------------- |
| `Or`         | 多通道触发逻辑或（默认）|
| `And`        | 多通道触发逻辑与        |

---

## 多通道模拟触发（高级）

```csharp
aiTask.Trigger.Type = AITriggerType.MultichannelAnalog;
aiTask.Trigger.Multichannel.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Multichannel.Logic = AIAnalogComparatorLogic.Or;

// 边沿比较器示例
aiTask.Trigger.Multichannel.Edge.Sources.Clear();
aiTask.Trigger.Multichannel.Edge.Sources.Add(new AIAnalogMultichannelEdgeComparatorSource()
{
    Source = AIAnalogTriggerSource.Channel_0,
    Slope = AIAnalogTriggerEdge.Rising,
    Threshold = 2.5
});
```

---

## AISync — 同步配置

```csharp
// 主卡配置
aiTask.Sync.Topology = SyncTopology.Master;
aiTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
aiTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;
aiTask.Sync.Commit();

// 从卡配置
aiTask.Sync.Topology = SyncTopology.Slave;
aiTask.Sync.TriggerRouting = SyncTriggerRouting.PXI_Trig0;
aiTask.Sync.PulseRouting = SyncPulseRouting.PXI_Trig1;
aiTask.Sync.Commit();
```

### SyncTopology 枚举

| 值            | 说明                    |
| ------------- | ----------------------- |
| `Independent` | 独立模式（不支持同步）  |
| `Master`      | 主卡                    |
| `Slave`       | 从卡                    |

### SyncTriggerRouting 枚举

| 值            | 说明                    |
| ------------- | ----------------------- |
| `NONE`        | 无路由                  |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7 |
| `PFI_0`       | 前面板 PFI_0 触发输入   |

### SyncPulseRouting 枚举

| 值            | 说明                    |
| ------------- | ----------------------- |
| `NONE`        | 无路由                  |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7 |
| `PFI_0`       | 前面板 PFI_0 同步信号   |

---

```csharp
// 内部参考时钟（默认）
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

// 外部参考时钟
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Frequency = 10000000;  // 10 MHz
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
aiTask.Device.ReferenceClock.Commit();  // 提交配置
```

### ReferenceClockSource 枚举

| 值           | 说明                    |
| ------------ | ----------------------- |
| `Internal`   | 内部参考时钟            |
| `External`   | 外部参考时钟            |

### ExternalReferenceClockTerminal 枚举

| 值                  | 说明                    |
| ------------------- | ----------------------- |
| `CLK_IN`            | 前面板 CLK_IN（10MHz）  |
| `PXIe_Clk100`       | PXIe 100MHz 背板时钟    |

---

## AISignalExport — 信号导出

```csharp
// 将起始触发信号导出到 PXI 触发总线
aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

// 清除导出
aiTask.SignalExport.Clear(SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.ClearAll();
```

### AISignalExportSource 枚举

| 值              | 说明                    |
| --------------- | ----------------------- |
| `StartTrig`     | AI 起始触发信号         |
| `ReferenceTrig` | AI 参考触发信号         |
| `SyncTriggerOut`| PLL 同步触发输出        |
| `ChannelComparatorOut` | AI 模拟多通道比较器输出 |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7 |

### SignalExportDestination 枚举

| 值                  | 说明                    |
| ------------------- | ----------------------- |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线 0~7  |
| `PFI_0`            | 前面板 PFI_0            |

### SignalExportOutputType 枚举

| 值           | 说明         |
| ------------ | ------------ |
| `Logic`      | 逻辑输出     |
| `OCGate`     | 开漏门输出   |

---

## 异常类 JYDriverException

```csharp
try { ... }
catch (JYDriverException ex)
{
    // ex.Message — 驱动错误描述
    // ex.ExceptionName — 异常枚举名
    // ex.ErrorCode — 错误码
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 枚举（常见值）

| 枚举值                                           | 含义                        |
| ------------------------------------------------ | --------------------------- |
| `OpenDeviceFailed`                               | 打开设备失败（未连接/槽位号错误） |
| `NoChannelAdded`                                 | 未添加通道                  |
| `StartTaskFailed`                                | 启动 Task 失败              |
| `StopTaskFailed`                                 | 停止 Task 失败              |
| `BufferDataOverflow`                             | 采集缓冲区溢出              |
| `ReadDataTimeout`                                | 读取超时                    |
| `SampleRateParameterInvalid`                     | 采样率超限                  |

---

## 设备硬件规格

| 参数           | 值                         |
| -------------- | -------------------------- |
| AI 分辨率      | 16-bit                     |
| AI 通道数      | 8                          |
| AI 最大采样率  | 125 MS/s（每通道）         |
| AI 输入量程    | ±10V / ±5V / ±1V / ±0.5V   |
| AI 输入阻抗    | 50Ω / 1MΩ                  |
| AI 输入耦合    | AC / DC                    |
| 参考时钟       | 内部 / 外部                |
| 触发方式       | 数字 / 模拟 / 软件         |
| 接口           | PXIe / PCIe                |

---

## AIRecord — 录制配置

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

## RecordMode 枚举

| 值 | 说明 |
|----|------|
| `Finite` | 有限录制，录制指定时长后自动停止 |
| `Infinite` | 无限录制，需手动调用 Stop 停止 |

---

## FileFormat 枚举

| 值 | 说明 |
|----|------|
| `Bin` | 二进制格式（short 类型，需缩放转换） |


---

# 完整代码示例

﻿# JY9818 代码示例集

> 所有示例均来自 `JY9818_V1.2.2_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 连续采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY9818;

public partial class MainForm : Form
{
    private JY9818AITask aiTask;
    private double[] readBuffer;

    // 启动按钮
    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            // 1. 创建 Task
            aiTask = new JY9818AITask(0);

            // 2. 添加通道（DC 耦合，1MΩ 阻抗）
            aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);

            // 3. 配置连续模式
            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 100000000;  // 100 MS/s

            // 4. 配置参考时钟
            aiTask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;

            // 5. 启动 Task
            aiTask.Start();

            // 6. 分配缓冲区
            readBuffer = new double[10000];

            // 7. 启动 Timer
            timer_FetchData.Enabled = true;
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    // Timer Tick
    private void timer_FetchData_Tick(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        try
        {
            if (aiTask.AvailableSamples >= (ulong)readBuffer.Length)
            {
                aiTask.ReadData(ref readBuffer, (ulong)readBuffer.Length, -1);
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

    // 停止按钮
    private void button_stop_Click(object sender, EventArgs e)
    {
        timer_FetchData.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }

    // 窗体关闭
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (aiTask != null) aiTask.Stop();
    }
}
```

---

## 示例 2：AI 有限采集（多通道）

```csharp
private JY9818AITask aiTask;
private double[,] readValue;  // 多通道使用二维数组 [每通道点数, 通道数]

private void button_start_Click(object sender, EventArgs e)
{
    aiTask = new JY9818AITask(0);

    // 添加多个通道（DC 耦合，50Ω 阻抗）
    int[] channels = { 0, 1, 2 };
    aiTask.AddChannel(channels, -5, 5, AICoupling.DC, AIImpedance.Impedance50Ohm);

    // 配置有限模式
    aiTask.Mode = AIMode.Finite;
    aiTask.SamplesToAcquire = 10000;  // 每通道采集点数
    aiTask.SampleRate = 125000000;  // 125 MS/s

    // 导出触发信号到 PXI 触发总线
    aiTask.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

    aiTask.Start();

    // 分配缓冲区：[每通道点数, 通道数]
    readValue = new double[10000, aiTask.Channels.Count];

    // 启用 Timer 轮询
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
        
        easyChartX1.Plot(ch0Data);  // 绘制通道0数据
        
        aiTask.Stop();
        aiTask.Channels.Clear();
        timer_FetchData.Enabled = false;
    }
}
```

---

## 示例 3：AI 数字触发采集

```csharp
aiTask = new JY9818AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100000000;

// 配置数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI_0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发信号到来后自动开始采集
```

---

## 示例 4：AI 模拟触发采集

```csharp
aiTask = new JY9818AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 10000;
aiTask.SampleRate = 100000000;

// 配置模拟触发
aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 2.5;

aiTask.Start();
// 等待触发信号...
```

---

## 示例 5：AI 软触发采集

```csharp
aiTask = new JY9818AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 100000000;

// 配置软触发
aiTask.Trigger.Type = AITriggerType.Soft;

aiTask.Start();

// 在需要时手动发送软触发
aiTask.SendSoftwareTrigger();
```

---

## 示例 6：外部参考时钟配置

```csharp
aiTask = new JY9818AITask(0);
aiTask.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTask.Mode = AIMode.Continuous;

// 使用外部 10MHz 参考时钟
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Frequency = 10000000;
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
aiTask.Device.ReferenceClock.Commit();  // 提交配置

aiTask.SampleRate = 100000000;
aiTask.Start();
```

---

## 示例 7：信号导出（多卡同步）

```csharp
// 主卡配置：导出起始触发信号
aiTaskMaster = new JY9818AITask(0);
aiTaskMaster.AddChannel(0, -5, 5, AICoupling.DC, AIImpedance.ImpedanceHigh);
aiTaskMaster.Mode = AIMode.Finite;
aiTaskMaster.SamplesToAcquire = 10000;
aiTaskMaster.SampleRate = 100000000;

// 将起始触发导出到 PXI_Trig0
aiTaskMaster.SignalExport.Add(AISignalExportSource.StartTrig, SignalExportDestination.PXI_Trig0);

aiTaskMaster.Start();

// 从卡配置：使用 PXI_Trig0 作为触发源
// （需要在从卡代码中配置 Trigger.Source 为 PXI_Trig0）
```

---

## 示例 8：AC 耦合采集（高频信号）

```csharp
aiTask = new JY9818AITask(0);

// AC 耦合，50Ω 阻抗（适合高频信号）
aiTask.AddChannel(0, -1, 1, AICoupling.AC, AIImpedance.Impedance50Ohm);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 125000000;  // 最高采样率

aiTask.Start();
```

---

## 综合技巧

### 高速采集注意事项

```csharp
// 1. 使用足够大的缓冲区
readBuffer = new double[100000];  // 根据采样率调整

// 2. Timer 间隔要足够短（高速采集时）
timer_FetchData.Interval = 1;  // 1ms

// 3. 检查实际采样率
aiTask.Start();
double actualRate = aiTask.SampleRate;  // 获取实际采样率
```

### 窗体关闭资源释放

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timerFetch != null) timerFetch.Enabled = false;
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

### 多通道数据解析

```csharp
// JY9818 多通道采集返回二维数组 double[每通道点数, 通道数]
// 每列代表一个通道的数据

int channelCount = aiTask.Channels.Count;
int samplesPerChannel = 10000;

// 创建二维缓冲区
double[,] readValue = new double[samplesPerChannel, channelCount];

// 读取数据
aiTask.ReadData(ref readValue, -1);

// 提取通道 0 的数据（第0列）
double[] ch0Data = new double[samplesPerChannel];
for (int i = 0; i < samplesPerChannel; i++)
{
    ch0Data[i] = readValue[i, 0];
}

// 提取通道 1 的数据（第1列）
double[] ch1Data = new double[samplesPerChannel];
for (int i = 0; i < samplesPerChannel; i++)
{
    ch1Data[i] = readValue[i, 1];
}

// 或使用 SeeSharpTools 转置后直接绘图
double[,] displayValue = new double[channelCount, samplesPerChannel];
ArrayManipulation.Transpose(readValue, ref displayValue);
easyChartX_readData.Plot(displayValue);
```

---

## 8. Record 有限录制模式（Finite Streaming）

JY9818 支持将数据流式录制到文件，录制完成后自动停止。定时器中通过`GetRecordStatus`检查录制状态：

```csharp
private JY9818AITask aiTask;
private double[,] previewData;  // 预览数据：[预览点数, 通道数]

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9818AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 10000000;  // 10 MS/s
        
        aiTask.Record.FilePath = @"C:\Data\record.bin";
        aiTask.Record.Mode = RecordMode.Finite;  // 有限录制模式
        aiTask.Record.Length = 5.0;               // 录制 5 秒
        aiTask.SamplesToAcquire = 100000;         // 预览数据点数
        
        aiTask.Start();
        previewData = new double[10000, aiTask.Channels.Count];  // [预览点数, 通道数]
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
        MessageBox.Show("录制完成！");
    }
    else
    {
        // 获取预览数据（二维数组，按列排列）
        if (aiTask.AvailableSamples >= 10000)
        {
            aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
            // previewData[i, ch] = 第ch通道第i个预览点
        }
    }
    timer_Preview.Enabled = true;
}
```

---

## 9. Record 无限录制模式

无限录制模式，需手动调用 Stop 停止：

```csharp
private JY9818AITask aiTask;
private double[,] previewData;  // 预览数据：[预览点数, 通道数]

private void button_Start_Click(object sender, EventArgs e)
{
    try
    {
        aiTask = new JY9818AITask(0);
        aiTask.AddChannel(0, -10, 10, AICoupling.DC, AIImpedance.ImpedanceHigh);
        
        aiTask.Mode = AIMode.Record;
        aiTask.SampleRate = 10000000;  // 10 MS/s
        
        aiTask.Record.FilePath = @"C:\Data\continuous_record.bin";
        aiTask.Record.Mode = RecordMode.Infinite;  // 无限录制模式
        
        aiTask.Start();
        previewData = new double[10000, aiTask.Channels.Count];  // [预览点数, 通道数]
        timer_Preview.Enabled = true;
        
        button_Start.Enabled = false;
        button_Stop.Enabled = true;
    }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
}

private void timer_Preview_Tick(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    
    // 获取预览数据（二维数组，按列排列）
    if (aiTask.AvailableSamples >= 10000)
    {
        aiTask.GetRecordPreviewData(ref previewData, 10000, 1000);
        // previewData[i, ch] = 第ch通道第i个预览点
        // 提取通道0数据示例：
        // double[] ch0Preview = new double[10000];
        // for (int i = 0; i < 10000; i++) ch0Preview[i] = previewData[i, 0];
    }
    
    timer_Preview.Enabled = true;
}

private void button_Stop_Click(object sender, EventArgs e)
{
    timer_Preview.Enabled = false;
    try { if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); } }
    catch (JYDriverException ex) { MessageBox.Show(ex.Message); }
    button_Start.Enabled = true;
    button_Stop.Enabled = false;
}
```

---

## 10. 录制数据回放

读取已录制的 bin 文件并回放显示（JY9818 数据格式为 short）：

```csharp
private FileStream playbackStream;
private BinaryReader playbackReader;
private short[,] rawData;  // 原始数据：[点数, 通道数]
private double[,] playbackData;  // 转换后的电压值：[点数, 通道数]
private double scaleValue = 21.0;  // 量程缩放系数

private void button_OpenFile_Click(object sender, EventArgs e)
{
    var fileBrowser = new OpenFileDialog { Filter = "(*.bin)|*.bin" };
    if (fileBrowser.ShowDialog() != DialogResult.OK) return;
    if (!File.Exists(fileBrowser.FileName)) { MessageBox.Show("文件不存在"); return; }
    
    var fileInfo = new FileInfo(fileBrowser.FileName);
    int channelCount = 1;  // 根据实际通道数调整
    
    playbackStream = new FileStream(fileBrowser.FileName, FileMode.Open);
    playbackReader = new BinaryReader(playbackStream);
    
    rawData = new short[10000, channelCount];  // [点数, 通道数]
    playbackData = new double[10000, channelCount];  // [点数, 通道数]
    
    button_OpenFile.Enabled = false;
    button_Playback.Enabled = true;
}

private void timer_Playback_Tick(object sender, EventArgs e)
{
    try
    {
        // 读取 short 原始数据并转换为电压值
        // 数据格式：[点数, 通道数]，按列排列
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

---

## 重要说明：多通道数据读取格式

### 数据结构

JY9818 的多通道数据读取使用**二维数组**，数据**按列排列**：

- **数组格式**：`double[每通道点数, 通道数]`
- **排列方式**：每一列代表一个通道的数据
- **与交错格式的区别**：不是 `[CH0_P1, CH1_P1, CH2_P1, CH0_P2, ...]` 的交错格式

### 示例对比

#### ✅ 正确：多通道读取（二维数组）

```csharp
// 3通道，每通道10000点
double[,] readValue = new double[10000, 3];  // [点数, 通道数]
aiTask.ReadData(ref readValue, -1);

// 数据结构：
// readValue[0,0] = 通道0第1点  readValue[0,1] = 通道1第1点  readValue[0,2] = 通道2第1点
// readValue[1,0] = 通道0第2点  readValue[1,1] = 通道1第2点  readValue[1,2] = 通道2第2点
// ...

// 提取通道0的所有数据
double[] ch0Data = new double[10000];
for (int i = 0; i < 10000; i++)
{
    ch0Data[i] = readValue[i, 0];  // 第0列 = 通道0
}
```

#### ✅ 正确：单通道读取（一维数组）

```csharp
// 单通道，10000点
double[] readValue = new double[10000];  // 一维数组
aiTask.ReadData(ref readValue, -1);

// 直接使用，无需解析
```

### XML 文档原文

根据 `JY9818.xml` 中的官方注释：

- **多通道**：`"Read the multi-channel data by columns and store it in a data buffer, Each channel is represented by one column of data."`
- **单通道**：`"Read the one channel data and store it in a data buffer."`

### 绘图提示

如果需要使用图表控件（如 EasyChartX）直接绘制多通道数据，需要转置数组：

```csharp
// readValue: [10000, 3] -> displayValue: [3, 10000]
double[,] displayValue = new double[aiTask.Channels.Count, 10000];
ArrayManipulation.Transpose(readValue, ref displayValue);
easyChartX_readData.Plot(displayValue);
```

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)
