---
name: jy6312-driver
description: 为 JYTEK JY-6312 PXIe 16 通道热电偶/低电压模拟输入模块编写 C# (.NET) 驱动代码。涵盖 AI 单点/有限/连续采集、热电偶测量（R/S/B/J/T/E/K/N/C/A/G/D）与内置冷端补偿 (TB68CJ)、低电压测量（±1.25V / ±625mV / ±312.5mV / ±156.2mV / ±78.125mV 五档量程）、数字触发 / 软件触发 / 重复触发、PFI 与 PXI_Trig 信号导出与多卡采样时钟同步、开路热电偶 (OTD) 检测、50/60 Hz 工频抑制、冷端温度自定义与原始数据 (EMF + 冷端温度) 读取。当用户提到 JY6312、JY-6312、6312 板卡、JY6312AITask、JY6312Device、热电偶采集、TB68CJ、冷端补偿 (CJC)、多通道温度采集、OTD、160 Sa/s 采样时自动应用本技能。
---

# JY6312 驱动开发技能

## 硬件概览

JY-6312 是 JYTEK PXIe 系列 **16 通道同步采样**热电偶/低电压模拟输入模块，每通道独立 ADC（支持通道级同步），专为低频高精度温度与微弱电压测量设计。

| 参数 | JY-6312 |
|------|---------|
| AI 通道数 | 16（每 ADC 1 通道） |
| AI 最大采样率（每通道） | **160 Sa/s** |
| AI 最小采样率（每通道） | **0.25 Sa/s** |
| 输入量程（电压模式） | ±1.25 V / ±625 mV / ±312.5 mV / ±156.2 mV / ±78.125 mV（五档） |
| 热电偶类型 | R / S / B / J / T / E / K / N / C / A / G / D（12 种） |
| 冷端补偿 (CJC) | 通过 **TB68CJ** 接线端子内置传感器自动补偿，可关闭后手动设 `CustomCJTemperature` |
| 工频抑制 | 50 Hz / 60 Hz（**仅在 SampleRate ≤ 8 Sa/s 时有效**） |
| 触发端子 | PFI0 / PFI1，PXI_Trig0..PXI_Trig7，PXI_Star |
| 信号导出源 | SampleClock / TriggerOut / IsConverting |
| 模块功能 | **仅 AI**（无独立 DI/DO 任务类） |
| 接口 | USB / PCIe / PXIe |

### 产品型号

- **USB-6312**：16-ch 24-bit USB 通道间隔离热电偶输入模块
- **PCIe-6312**：16-ch 24-bit PCIe 通道间隔离热电偶输入模块
- **PXIe-6312**：16-ch 24-bit PXIe 通道间隔离热电偶输入模块

## 驱动与依赖

| 文件 | 绝对路径 |
|------|----------|
| 主驱动 DLL（**必须引用**） | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll` |
| 驱动 XML 注释文档 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.XML` |
| 示例工程根目录 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\JY6312.Examples\` |

**工程引用配置**（.csproj）：

```xml
<Reference Include="JY6312">
  <HintPath>C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll</HintPath>
</Reference>
```

目标框架 `.NET Framework 4.0+`，平台选择 `x64`（**禁止 AnyCPU**）。

代码顶部统一：
```csharp
using JY6312;
```

## 核心 API 速查

| 类型 | 用途 |
|------|------|
| `JY6312AITask` | AI 任务主类（唯一任务类，电压/热电偶复用） |
| `JY6312Device` | 板卡实例（`Scan()`, `GetInstance(boardNum)`, `MaxSampleRate` 等） |
| `JYDriverException` | 驱动统一异常，必须 `try/catch` |
| `AIMode` | `Single` / `Finite` / `Continuous` |
| `MeasurementType` | `Thermocouple` / `GeneralVoltage` |
| `ThermocoupleType` | `TypeR/S/B/J/T/E/K/N/C/A/G/D` |
| `AIRange` | `_1p25V` / `_625mV` / `_312p5mV` / `_156p2mV` / `_78p125mV` |
| `AITriggerType` | **`Immediately`** / `Digital` / `Software` |
| `BuildInCJC` | 内置冷端补偿配置 |

## 标准工作流（AI 任务）

```
1. 构造 task        : new JY6312AITask(boardNum)
2. 添加通道         : AddChannel(chID, ThermocoupleType) 或 AddChannel(chID, rangeLow, rangeHigh)
3. 配置模式/采样率  : Mode / SampleRate / SamplesToAcquire
4. [可选] 触发/时钟/信号导出 : Trigger / SampleClock / SignalExport
5. 启动             : Start()
6. 读取 → 停止      : ReadSinglePoint / ReadData / ReadRawData → Stop()
```

**关键约束**：
- `AddChannel` **必须在 `Start()` 之前**，否则抛 `aiIsStart` 错误。
- `Start()` 前若启用内置 CJC (默认)，驱动会检查 TB68CJ 连接状态。
- `Finite` 模式必须设置 `SamplesToAcquire`。
- `ReadData` 连续模式下需先判断 `AvailableSamples >= 请求长度`。
- 多卡同步：**从卡必须先 `Start()`**，主卡后 `Start()`。
- 推荐启动前调用 `aiTask.CheckTerminalBlockConnectionStatusBeforeStart()`。

## AI 三种模式速查

| 模式 | 典型配置 | 读取方式 |
|------|----------|----------|
| `Single` | `Mode=AIMode.Single` | `ReadSinglePoint` |
| `Finite` | `+SamplesToAcquire=N` | `AITaskIsDone` 后 `ReadData` |
| `Continuous` | `+SampleRate` | Timer 轮询 `AvailableSamples` → `ReadData` |

## 典型代码模板

### 1. 单点热电偶测量（Single 模式）

```csharp
aiTask = new JY6312AITask(boardNum: 0);
aiTask.Mode = AIMode.Single;
aiTask.AddChannel(chID: 0, ThermocoupleType.TypeK);
aiTask.SampleRate = 1;
aiTask.Start();
Thread.Sleep((int)(1000.0 / aiTask.SampleRate));
double[] readValue = new double[1];
aiTask.ReadSinglePoint(ref readValue);
aiTask.Stop();
```

### 2. 连续采集（内部时钟 / 外部时钟）

```csharp
aiTask = new JY6312AITask(0);
aiTask.AddChannel(channelID, ThermocoupleType.TypeK);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleClock.Source = AISampleClockSource.Internal;
aiTask.SampleRate = 100;   // 0.25 ~ 160 Sa/s
// 外部时钟：
// aiTask.SampleClock.Source = AISampleClockSource.External;
// aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
// aiTask.SampleClock.External.ExpectedRate = 100;
aiTask.Start();
```

### 3. 数字触发（PFI/PXI_Trig 上升沿）

```csharp
aiTask.Trigger.Type           = AITriggerType.Digital;
aiTask.Trigger.Mode           = AITriggerMode.Start;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;
```

### 4. 软件触发 + Retrigger

```csharp
aiTask.Trigger.Type              = AITriggerType.Software;
aiTask.Trigger.Mode              = AITriggerMode.Reference;
aiTask.Trigger.ReTriggerCount    = 5;    // -1=无限
aiTask.Trigger.PreTriggerSamples = 1;
aiTask.Start();
aiTask.SendSoftwareTrigger();
```

### 5. 关闭内置 CJC + 自定义冷端温度

```csharp
aiTask.BuildInCJC.Enabled = false;
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.Start();
aiTask.SetCJTemperature(chID: 0, temperature: 25.0);
```

### 6. 读取原始 EMF + 冷端温度

```csharp
double[,] hjVoltage     = new double[samples, channels];
double[,] cjTemperature = new double[samples, channels];
aiTask.ReadRawData(ref hjVoltage, ref cjTemperature, timeout: 1000);
double tempC = Utility.Thermocouple.ConvertEMFToTemperature(
    ThermocoupleType.TypeK, hjVoltage[0, 0] * 1e6, cjTemperature[0, 0]);
```

### 7. 开路热电偶检测 (OTD)

```csharp
aiTask = new JY6312AITask(0);
aiTask.AddChannel(new[] {0, 1, 2, 3}, ThermocoupleType.TypeK);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 1;
ThermocoupleConnectionStatus[] results = aiTask.DetectOpenThermocouple();
// results[i]: Normal / OpenCircuit
```

### 8. TB68CJ 端子状态

```csharp
bool connected = aiTask.BuildInCJC.SensorStatus == CJSensorConnectionStatus.Normal;
```

### 9. 多卡采样时钟同步

```csharp
// 主卡：导出 SampleClock
master.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
// 从卡：接收外部时钟
slave.SampleClock.Source = AISampleClockSource.External;
slave.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slave.SampleClock.External.ExpectedRate = 100;
slave.Start();    // 从卡先启动
master.Start();   // 主卡后启动
```

### 10. 50/60 Hz 工频抑制

```csharp
aiTask.SampleRate = 2;   // 必须 ≤ 8 Sa/s 才生效
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
```

## 异常与错误处理

所有驱动调用**必须**包在 `try { ... } catch (JYDriverException ex) { ... }` 中。

| 场景 | 错误枚举 | 诊断 |
|------|----------|------|
| Start 后仍 AddChannel | `aiIsStart` | 在 Start() 之前完成所有通道 |
| 读数请求超时 | `Timeout` | 增大 `timeout`，或先判断 `AvailableSamples` |
| Finite 下读取多于 SamplesToAcquire | `BufferDownflow` | 限制请求长度 |
| 量程参数非法 | `ErrorParam1` | 仅允许 ±1.25/±0.625/±0.3125/±0.1562/±0.078125 V |
| 开启内置 CJC 但未接 TB68CJ | `InitializeFailed` | 关闭 `BuildInCJC.Enabled` 或检查端子 |
| 外部时钟 PLL 未锁定 | `PLLLockFailed` | 检查物理接线和 `ExpectedRate` |

**窗口/程序关闭时**：`FormClosing` / `finally` 中调用 `aiTask?.Stop()` 释放板卡资源。

## 代码风格与约定

- **板卡构造**：`new JY6312AITask(int boardNum)` 或 `new JY6312AITask(string boardName)`。
- **读数数组维度**：一维 `double[samples]` 单通道；二维 `double[samples, channels]` 多通道。
- **采样率取整**：`SampleRate` 写后再读会得到驱动实际生效值。
- **UI 线程更新**：Timer.Tick 中短暂禁用 `timer.Enabled`，防止重入。
- **平台**：`x64` 推荐。AnyCPU 会在 64 位系统上随机失败。

## 进阶参考

| 文档 | 何时查阅 |
|------|----------|
| [`reference.md`](reference.md) | 完整 API 定义（类/属性/方法签名）、枚举全表、错误码、PFI/SignalExport/BuildInCJC 细节 |
| [`examples.md`](examples.md) | 完整可运行示例代码（21 个场景 + 综合技巧） |
---
name: jy6312-driver
description: 提供 JYTEK JY6312 系列 16 通道、24-bit、通道间隔离热电偶温度采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）热电偶温度测量（12 种热电偶类型 R/S/B/J/T/E/K/N/C/A/G/D）、电压测量、电流测量（外接 10Ω 采样电阻）三类模式（Single/Finite/Continuous）、内置冷端补偿（BuildInCJC）、外置冷端温度（SetCJTemperature 自定义 RJ）、开路热电偶检测（DetectOpenThermocouple / OTD）、50/60 Hz 工频抑制、数字触发/软触发/外部时钟、多卡采样时钟同步。当用户使用 USB/PCIe/PXIe-6312、JY6312AITask、ThermocoupleType.TypeR/S/B/J/T/E/K/N/C/A/G/D、MeasurementType.Thermocouple/GeneralVoltage、AIRange._1p25V/_625mV/_312p5mV/_156p2mV/_78p125mV、AIMode.Single/Finite/Continuous、AITriggerType.Immediately/Digital/Software、BuildInCJC、SetCJTemperature、DetectOpenThermocouple、PowerLineRejection、TB-68CJ 接线端子开发热电偶测温、工业温度监测、多点温度扫描、冷端补偿、电流变送器采集应用时自动应用。
---

# JY6312 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.XML`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY6312;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等
- **配套端子**：TB-68CJ 68-Pin 屏蔽 I/O 接线端子（含冷端温度传感器）

## 硬件规格速查

| 功能                    | 规格                                                            |
| ----------------------- | --------------------------------------------------------------- |
| AI 分辨率               | 24-bit（280 ppm 精度）                                          |
| AI 通道数               | 16 通道（通道间隔离）                                           |
| 采样方式                | 同步采集（Simultaneous sampling）                               |
| AI 采样率范围           | 0.5 Sa/s ~ 160 Sa/s（每通道）                                   |
| 支持热电偶类型          | R / S / B / J / T / E / K / N / C / A / G / D（12 种）          |
| 电压测量量程            | ±1.25V / ±625mV / ±312.5mV / ±156.2mV / ±78.125mV               |
| 热电偶输入保护          | TC+ / TC- 间 ±20 V                                              |
| ESD 保护                | 4 kV                                                            |
| 差分输入阻抗            | 1 GΩ                                                            |
| 最大 DC 线性度          | ±15 ppm                                                         |
| 存储深度                | 64 M Samples                                                    |
| 通道间隔离              | 60 VDC                                                          |
| COM 对大地隔离          | 60 VDC                                                          |
| 冷端补偿                | 内置（TB-68CJ 冷端传感器）/ 外置软件补偿                        |
| 开路热电偶检测（OTD）   | 软件使能，逐通道检测                                            |
| 电源工频抑制            | 50 / 60 Hz，采样率 ≥ 2 Sa/s 时抑制 ≥ 65 dB                      |
| 触发方式                | 立即 / 数字 / 软件                                              |
| PFI                     | 2 通道 PFI<0..1>，仅输入，用于外部数字触发（5 V TTL）           |
| 数字触发源（PXIe）      | SSI<0..7>、PXI_TRIG<0..7>、PXI_STAR、PFI<0..1>                  |
| 数字触发源（PCIe/USB）  | PFI<0..1>                                                       |
| 接口类型                | USB / PCIe / PXIe                                               |

### 产品型号

- **USB-6312**：16-ch 24-bit USB 通道间隔离热电偶输入模块
- **PCIe-6312**：16-ch 24-bit PCIe 通道间隔离热电偶输入模块
- **PXIe-6312**：16-ch 24-bit PXIe 通道间隔离热电偶输入模块

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读取数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY6312AITask(0);                                   // 1. 创建（按槽位号）
task.AddChannel(0, ThermocoupleType.TypeK);                       // 2. 添加通道（热电偶/电压）
task.Mode = AIMode.Continuous;                                    // 3. 配置
task.SampleRate = 10;                                             // 热电偶采样率低（0.5~160 Sa/s）
task.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
task.BuildInCJC.Enabled = true;                                   // 启用内置冷端补偿
task.Start();                                                     // 4. 启动
// ... 读取数据 ...
task.Stop();                                                      // 5. 停止
task.Channels.Clear();                                            // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

**启动前检查接线端子**（推荐，确保 TB-68CJ 正确连接）：

```csharp
task.CheckTerminalBlockConnectionStatusBeforeStart();
```

---

## 模拟输入（AI）

### 任务类：`JY6312AITask`

#### 关键属性

| 属性                 | 类型                   | 说明                                                      |
| -------------------- | ---------------------- | --------------------------------------------------------- |
| `Mode`               | `AIMode`               | Single / Finite / Continuous                              |
| `SampleRate`         | `double`               | 每通道采样率（Sa/s），范围 0.5 ~ 160                      |
| `ConversionTime`     | `double`               | ADC 转换时间（与 SampleRate 互算）                        |
| `SamplesToAcquire`   | `int`                  | Finite 模式下每通道采集点数                               |
| `AvailableSamples`   | `ulong`                | 缓冲区中可读取的点数（非 Single 模式）                    |
| `TransferedSamples`  | `ulong`                | 已传输点数（非 Single 模式）                              |
| `BuildInCJC`         | `BuildInCJC`           | 内置冷端补偿配置对象                                      |
| `PowerLineRejection` | `PowerLineRejection`   | 工频抑制配置对象（含 50/60 Hz 频率属性）                  |
| `SampleClock`        | `AISampleClock`        | 时钟配置对象                                              |
| `Trigger`            | `AITrigger`            | 触发配置对象                                              |
| `SignalExport`       | `AISignalExport`       | 信号导出对象                                              |
| `Channels`           | `List<AIChannel>`      | 已添加的通道列表                                          |
| `Device`             | `JY6312Device`         | 设备对象                                                  |
| `DisableCalibration` | `bool`                 | 禁用校准（调试用，正常请置 false）                        |

#### AddChannel 重载

JY6312 支持两类测量：**热电偶温度**与**电压**，分别对应不同的 AddChannel 重载。
电流测量通过**电压模式 + 外接精密采样电阻**（通常 10 Ω）软件换算实现。

```csharp
// ===== 热电偶测量（返回温度，单位 ℃）=====
// 单通道
aiTask.AddChannel(int chnId, ThermocoupleType tcType);
// 多通道（统一类型）
aiTask.AddChannel(int[] chnsId, ThermocoupleType tcType);
// 多通道（各自类型）
aiTask.AddChannel(int[] chnsId, ThermocoupleType[] tcTypes);

// ===== 电压测量（返回伏特）=====
aiTask.AddChannel(int chnId, double rangeLow, double rangeHigh);
aiTask.AddChannel(int[] chnsId, double rangeLow, double rangeHigh);
aiTask.AddChannel(int[] chnsId, double[] rangeLow, double[] rangeHigh);

// 通道编号：0~15
// 电压 rangeLow/High 合法配对：
//   ±1.25 / ±0.625 / ±0.3125 / ±0.1562 / ±0.078125
```

#### 枚举说明

**MeasurementType 枚举**（AIChannel.MeasurementType 只读属性）

| 值               | 说明                        |
| ---------------- | --------------------------- |
| `Thermocouple`   | 热电偶温度测量（℃）         |
| `GeneralVoltage` | 通用电压测量（V）           |

**ThermocoupleType 枚举**（12 种国际标准热电偶）

| 值       | 典型测温范围       | 说明                               |
| -------- | ------------------ | ---------------------------------- |
| `TypeR`  | -50 ~ 1768 ℃       | 铂铑 13% - 铂                      |
| `TypeS`  | -50 ~ 1768 ℃       | 铂铑 10% - 铂                      |
| `TypeB`  | 0 ~ 1820 ℃         | 铂铑 30% - 铂铑 6%                 |
| `TypeJ`  | -210 ~ 1200 ℃      | 铁 - 铜镍                          |
| `TypeT`  | -270 ~ 400 ℃       | 铜 - 铜镍                          |
| `TypeE`  | -270 ~ 1000 ℃      | 镍铬 - 铜镍                        |
| `TypeK`  | -270 ~ 1372 ℃      | 镍铬 - 镍铝（工业最常用）          |
| `TypeN`  | -270 ~ 1300 ℃      | 镍铬硅 - 镍硅                      |
| `TypeC`  | 0 ~ 2320 ℃         | 钨铼 5% - 钨铼 26%                 |
| `TypeA`  | 0 ~ 2500 ℃         | 钨铼 5% - 钨铼 20%                 |
| `TypeG`  | 0 ~ 2315 ℃         | 钨 - 钨铼 26%                      |
| `TypeD`  | 0 ~ 2315 ℃         | 钨铼 3% - 钨铼 25%                 |

> 精确的允许电压/温度范围可通过 `Utility.Thermocouple.GetVmin/GetVmax/GetTmin/GetTmax(type)` 查询。

**AIRange 枚举**（电压测量量程，通过 AddChannel 的 rangeLow/High 自动匹配）

| 值           | 量程           |
| ------------ | -------------- |
| `_1p25V`     | ±1.25 V        |
| `_625mV`     | ±625 mV        |
| `_312p5mV`   | ±312.5 mV      |
| `_156p2mV`   | ±156.2 mV      |
| `_78p125mV`  | ±78.125 mV     |

#### 冷端补偿（CJC）

JY6312 支持两种冷端补偿方式：

**方式一：内置冷端补偿（推荐，配合 TB-68CJ 使用）**

```csharp
aiTask.BuildInCJC.Enabled = true;      // 启用内置冷端补偿，自动读取 TB-68CJ 温度
```

**方式二：自定义冷端温度（手动提供 RJ 温度）**

```csharp
aiTask.BuildInCJC.Enabled = false;
aiTask.SetCJTemperature(0, 25.0);              // 通道 0 冷端温度 25 ℃
aiTask.SetCJTemperature(new double[] { 25.0, 25.0, 25.0 });   // 按已添加通道顺序
```

**BuildInCJC 属性速览**

| 属性              | 类型                            | 说明                                   |
| ----------------- | ------------------------------- | -------------------------------------- |
| `Enabled`         | `bool`                          | 是否启用内置冷端补偿                   |
| `SensorConnected` | `bool`                          | 冷端传感器是否已连接（只读）           |
| `SensorStatus`    | `CJSensorConnectionStatus`      | 传感器状态（只读）                     |
| `Debouncing`      | `bool`                          | 去抖动                                 |
| `Advanced`        | `CJCAdvanced`                   | 高级配置（忽略超时、更新超时）         |

**CJSensorConnectionStatus 枚举**

| 值             | 说明         |
| -------------- | ------------ |
| `Normal`       | 正常         |
| `LostConnect`  | 断开         |
| `Closed`       | 关闭         |

#### 开路热电偶检测（OTD）

`DetectOpenThermocouple()` 方法会对所有已添加通道进行一次开路检测，返回每个通道的连接状态。

```csharp
ThermocoupleConnectionStatus[] status = aiTask.DetectOpenThermocouple();
for (int i = 0; i < status.Length; i++)
{
    if (status[i] == ThermocoupleConnectionStatus.OpenCircuit)
        Console.WriteLine($"通道 {aiTask.Channels[i].ID}：开路");
}
```

**ThermocoupleConnectionStatus 枚举**

| 值             | 说明       |
| -------------- | ---------- |
| `Normal`       | 连接正常   |
| `OpenCircuit`  | 开路       |

#### 读取数据重载

```csharp
// Single 模式（按已添加通道顺序返回）
aiTask.ReadSinglePoint(ref double[] readValue);

// 读取原始电压 + 温度（诊断用）
aiTask.ReadRawSinglePoint(ref double[] voltages, ref double[] temperatures);

// Finite/Continuous — 单通道
aiTask.ReadData(ref double[] buf, int samplesPerChannel, int timeout);
aiTask.ReadData(ref double[] buf, int samplesPerChannel);       // 默认 timeout=-1
// Finite/Continuous — 多通道（列存储，每列一个通道）
aiTask.ReadData(ref double[,] buf, int samplesPerChannel, int timeout);
aiTask.ReadData(ref double[,] buf, int samplesPerChannel);

// 多通道同时读取电压与温度
aiTask.ReadRawData(ref double[,] voltages, ref double[,] temperatures, int samplesPerChannel);

// timeout = -1 → 永久等待
```

#### AI 三种模式速查

| 模式         | 典型配置                       | 读取方式                                     |
| ------------ | ------------------------------ | -------------------------------------------- |
| `Single`     | `Mode=AIMode.Single`           | 轮询 `ReadSinglePoint`                       |
| `Finite`     | `+SamplesToAcquire=N`          | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                  | Timer 轮询 `AvailableSamples` → `ReadData`   |

#### 连续采集 Timer 模式（最常用）

```csharp
// Timer Tick 中：
timer.Enabled = false;
if (aiTask.AvailableSamples >= (ulong)readBuffer.GetLength(0))
{
    aiTask.ReadData(ref readBuffer, readBuffer.GetLength(0), -1);
    easyChartX1.Plot(readBuffer);        // 显示波形
}
timer.Enabled = true;
```

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // PFI0~PFI1 / PXI_Trig0~7 / PXI_Star
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;     // Rising / Falling
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Software;
aiTask.Start();
aiTask.SendSoftwareTrigger();   // 手动发送软触发
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10;
```

#### 工频抑制配置

根据现场工频选择对应抑制档位，可显著改善测温稳定性：

```csharp
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;   // 国内电网
// aiTask.PowerLineRejection.Frequency = PowerLineFrequency._60Hz;  // 部分海外电网

// 或分别控制（相对少用，Frequency 已足够）
aiTask.PowerLineRejection.RejectAt50Hz = true;
aiTask.PowerLineRejection.RejectAt60Hz = false;
```

#### 电流测量（外接采样电阻）

JY6312 不直接提供电流量程，可通过**外接精密采样电阻 + 电压测量**实现：

```csharp
const double SHUNT = 10.0;     // 10 Ω 采样电阻

// 电流范围 ±125 mA → 电压 ±1.25V
aiTask.AddChannel(0, -1.25, 1.25);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10;
aiTask.Start();

double[] voltage = new double[100];
aiTask.ReadData(ref voltage, 100, -1);

// 换算为电流
double[] current = new double[100];
for (int i = 0; i < 100; i++) current[i] = voltage[i] / SHUNT;   // 单位：A
```

| 电压量程    | 对应电流（R=10Ω） |
| ----------- | ----------------- |
| ±1.25 V     | ±125 mA           |
| ±625 mV     | ±62.5 mA          |
| ±312.5 mV   | ±31.25 mA         |
| ±156.2 mV   | ±15.62 mA         |
| ±78.125 mV  | ±7.8125 mA        |

---

## 多卡同步

JY6312 支持通过 PXI 背板触发总线进行多卡采样时钟同步，适用于大规模温度监测场景。

```csharp
// ===== 主卡配置（Slot 2）=====
var masterTask = new JY6312AITask(2);
masterTask.AddChannel(0, ThermocoupleType.TypeK);
masterTask.BuildInCJC.Enabled = true;
masterTask.Mode = AIMode.Finite;
masterTask.SamplesToAcquire = 1000;
masterTask.SampleRate = 10;
masterTask.Trigger.Type = AITriggerType.Immediately;
masterTask.Trigger.Mode = AITriggerMode.Start;

// 导出采样时钟和触发信号到 PXI 背板
masterTask.SignalExport.Add(AISignalExportSource.SampleClock, SignalExportDestination.PXI_Trig0);
masterTask.SignalExport.Add(AISignalExportSource.TriggerOut,  SignalExportDestination.PXI_Trig1);

// ===== 从卡配置（Slot 4）=====
var slaveTask = new JY6312AITask(4);
slaveTask.AddChannel(0, ThermocoupleType.TypeK);
slaveTask.BuildInCJC.Enabled = true;
slaveTask.Mode = AIMode.Finite;
slaveTask.SamplesToAcquire = 1000;

slaveTask.SampleClock.Source = AISampleClockSource.External;
slaveTask.SampleClock.External.Terminal = ClockTerminal.PXI_Trig0;
slaveTask.SampleClock.External.ExpectedRate = 10;

slaveTask.Trigger.Type = AITriggerType.Digital;
slaveTask.Trigger.Mode = AITriggerMode.Start;
slaveTask.Trigger.Digital.Source = AIDigitalTriggerSource.PXI_Trig1;

// ===== 启动顺序：先启动从卡，再启动主卡 =====
slaveTask.Start();
masterTask.Start();

// ===== 读取数据 =====
double[] masterData = new double[1000];
double[] slaveData  = new double[1000];

masterTask.ReadData(ref masterData, 1000, -1);
slaveTask.ReadData(ref slaveData,  1000, -1);

masterTask.Stop();
slaveTask.Stop();
masterTask.Channels.Clear();
slaveTask.Channels.Clear();
```

---

## 常见错误处理

| 异常代码                                         | 原因                     | 处理建议                           |
| ------------------------------------------------ | ------------------------ | ---------------------------------- |
| `OpenDeviceFailed`                               | 板卡未连接或槽位号错误   | 检查设备管理器槽位号               |
| `NoChannelAdded`                                 | 未调用 AddChannel 就 Start | 在 Start 前添加通道                |
| `BufferDataOverflow`                             | 读取速度慢于采集速度     | 增大读取频率或减小采样率           |
| `ReadDataTimeout`                                | timeout 内未读到足够数据 | 增大 timeout 或检查采样率          |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数      | Stop 后再修改                      |
| `SampleRateParameterInvalid`                     | 采样率超出 0.5~160 范围  | 使用合法采样率                     |
| `ChannelNumberParameterInvalid`                  | 通道号 < 0 或 > 15       | 使用合法通道号                     |
| `ChannelInputRangeParameterInvalid`              | 电压量程不合法           | 使用枚举表中的量程对               |
| `TriggerParameterInvalid`                        | 触发参数无效             | 检查触发源、边沿、模式组合         |
| 冷端传感器 `LostConnect`                         | TB-68CJ 未连接/连接不良  | 检查端子连接，或关闭内置 CJC 使用自定义 RJ |

---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY6312;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DAQ\JY6312\Bin\JY6312.dll
```

---

## JY6312AITask — 模拟输入任务

### 构造函数

```csharp
new JY6312AITask(int slotNumber)     // 按槽位号创建（推荐）
new JY6312AITask(string boardType)   // 按板卡类型字符串创建
```

### 属性

| 属性                  | 类型                  | 默认值 | 说明                                      |
| --------------------- | --------------------- | ------ | ----------------------------------------- |
| `Mode`                | `AIMode`              | —      | Single / Finite / Continuous              |
| `SampleRate`          | `double`              | —      | 每通道采样率（Sa/s），0.5 ~ 160           |
| `ConversionTime`      | `double`              | —      | ADC 转换时间（与 SampleRate 互算）        |
| `SamplesToAcquire`    | `int`                 | —      | Finite 模式采集点数/通道                  |
| `AvailableSamples`    | `ulong`               | —      | 缓冲区可读点数                            |
| `TransferedSamples`   | `ulong`               | —      | 已传输点数                                |
| `BuildInCJC`          | `BuildInCJC`          | —      | 内置冷端补偿对象                          |
| `PowerLineRejection`  | `PowerLineRejection`  | —      | 工频抑制对象                              |
| `SampleClock`         | `AISampleClock`       | —      | 时钟配置对象                              |
| `Trigger`             | `AITrigger`           | —      | 触发配置对象                              |
| `SignalExport`        | `AISignalExport`      | —      | 信号导出对象                              |
| `Channels`            | `List<AIChannel>`     | —      | 已添加的通道列表                          |
| `Device`              | `JY6312Device`        | —      | 设备对象                                  |
| `DisableCalibration`  | `bool`                | false  | 禁用校准                                  |

### 方法

#### AddChannel — 见上文"AddChannel 重载"章节
#### RemoveChannel

```csharp
void RemoveChannel(int channelID)
void RemoveChannel(int[] channelIDs)
```

#### 控制

```csharp
void Start()
void Stop()
void SendSoftwareTrigger()                            // 软触发
void CheckTerminalBlockConnectionStatusBeforeStart()  // 启动前检查端子板连接
```

#### 冷端温度与开路检测

```csharp
void SetCJTemperature(int channelId, double temperatureInCelsius)
void SetCJTemperature(double[] temperatures)             // 按已添加通道顺序
void ReportBuildInCJException()                           // 主动上报冷端异常
ThermocoupleConnectionStatus[] DetectOpenThermocouple()   // 所有通道开路检测
```

#### 读取数据

```csharp
// Single 模式
void ReadSinglePoint(ref double[] readValue)
void ReadRawSinglePoint(ref double[] voltages, ref double[] temperatures)

// 缓冲模式（Finite / Continuous）
void ReadData(ref double[] buf, int samplesPerChannel)
void ReadData(ref double[] buf, int samplesPerChannel, int timeout)
void ReadData(ref double[,] buf, int samplesPerChannel)
void ReadData(ref double[,] buf, int samplesPerChannel, int timeout)

// 同时获取原始电压和温度
void ReadRawData(ref double[]  voltages, ref double[]  temperatures, int samplesPerChannel)
void ReadRawData(ref double[]  voltages, ref double[]  temperatures, int samplesPerChannel, int timeout)
void ReadRawData(ref double[,] voltages, ref double[,] temperatures, int samplesPerChannel)
void ReadRawData(ref double[,] voltages, ref double[,] temperatures, int samplesPerChannel, int timeout)
```

---

## AIChannel — 通道属性

| 属性                  | 类型                  | 说明                                           |
| --------------------- | --------------------- | ---------------------------------------------- |
| `ID`                  | `int`                 | 通道号（只读）                                 |
| `ThermocoupleType`    | `ThermocoupleType`    | 热电偶类型（热电偶模式有效）                   |
| `CustomCJTemperature` | `double`              | 自定义冷端温度（当 BuildInCJC.Enabled=false）  |
| `Range`               | `AIRange`             | 输入量程（只读，由 AddChannel 决定）           |
| `Gain`                | `double`              | 对应增益（只读）                               |
| `RangeHigh`           | `double`              | 量程上限（只读）                               |
| `RangeLow`            | `double`              | 量程下限（只读）                               |
| `MeasurementType`     | `MeasurementType`     | Thermocouple / GeneralVoltage（只读）          |
| `Advanced`            | `AIChannelAdvanced`   | 高级：VbiasEnabled / InputBufferEnabled        |

---

## AIMode 枚举

| 值           | 说明                                            |
| ------------ | ----------------------------------------------- |
| `Single`     | 软件触发单点读取，循环调用 ReadSinglePoint      |
| `Finite`     | 采集固定点数后停止                              |
| `Continuous` | 持续采集，应用轮询 AvailableSamples 读取        |

---

## AISampleClock — 时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.Internal;    // 默认
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 10.0;
```

### ClockTerminal 枚举

| 值                        | 说明             |
| ------------------------- | ---------------- |
| `PFI0` / `PFI1`           | 前面板 PFI 引脚  |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线     |
| `PXI_Star`                | PXI 星型触发     |

---

## AITrigger — 触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;     // Immediately / Digital / Software
aiTask.Trigger.Mode = AITriggerMode.Start;       // Start / Reference
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;
```

### AITriggerType 枚举

| 值             | 说明                |
| -------------- | ------------------- |
| `Immediately`  | 立即触发（默认）    |
| `Digital`      | 数字边沿触发        |
| `Software`     | 软件触发            |

### AITriggerMode 枚举

| 值          | 说明             |
| ----------- | ---------------- |
| `Start`     | 开始触发（默认） |
| `Reference` | 参考触发         |

### AITrigger 其他重要属性

| 属性                | 类型   | 说明                                                         |
| ------------------- | ------ | ------------------------------------------------------------ |
| `PreTriggerSamples` | `uint` | 预触发样本数，Reference 模式有效，必须 ≤ SamplesToAcquire    |
| `ReTriggerCount`    | `int`  | 重复触发次数，0/1 触发一次，-1 持续重复直到 Stop             |

### AIDigitalTriggerSource 枚举

| 值                        | 说明           |
| ------------------------- | -------------- |
| `PFI0` / `PFI1`           | 前面板 PFI     |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线   |
| `PXI_Star`                | PXI 星型触发   |

### AIDigitalTriggerEdge 枚举

| 值        | 说明   |
| --------- | ------ |
| `Rising`  | 上升沿 |
| `Falling` | 下降沿 |

---

## AISignalExport — 信号导出

```csharp
aiTask.SignalExport.Add(AISignalExportSource.SampleClock,  SignalExportDestination.PXI_Trig0);
aiTask.SignalExport.Add(AISignalExportSource.TriggerOut,   SignalExportDestination.PXI_Trig1);
```

### AISignalExportSource 枚举

| 值              | 说明            |
| --------------- | --------------- |
| `SampleClock`   | 采样时钟        |
| `TriggerOut`    | 触发输出        |
| `IsConverting`  | ADC 转换指示    |

### SignalExportDestination 枚举

| 值                        | 说明           |
| ------------------------- | -------------- |
| `PXI_Trig0` ~ `PXI_Trig7` | PXI 触发总线   |
| `PXI_Star`                | PXI 星型触发   |

---

## BuildInCJC — 内置冷端补偿

| 属性              | 类型                        | 说明                                         |
| ----------------- | --------------------------- | -------------------------------------------- |
| `Enabled`         | `bool`                      | 是否启用内置冷端补偿                         |
| `SensorConnected` | `bool`                      | 冷端传感器是否连接（只读）                   |
| `SensorStatus`    | `CJSensorConnectionStatus`  | 传感器状态（Normal / LostConnect / Closed）  |
| `Debouncing`      | `bool`                      | 去抖动                                       |
| `Advanced`        | `CJCAdvanced`               | 高级属性                                     |

**CJCAdvanced 属性**

| 属性                   | 类型    | 说明                      |
| ---------------------- | ------- | ------------------------- |
| `IgnoreTimeoutException` | `bool`  | 忽略冷端读取超时异常    |
| `UpdateTimeout`        | `int`   | 冷端温度更新超时（ms）    |

---

## PowerLineRejection — 工频抑制

| 属性            | 类型                  | 说明                              |
| --------------- | --------------------- | --------------------------------- |
| `Frequency`     | `PowerLineFrequency`  | 工频：_50Hz / _60Hz（推荐用此）   |
| `RejectAt50Hz`  | `bool`                | 单独开/关 50 Hz 抑制              |
| `RejectAt60Hz`  | `bool`                | 单独开/关 60 Hz 抑制              |

### PowerLineFrequency 枚举

| 值      | 说明          |
| ------- | ------------- |
| `_50Hz` | 50 Hz 工频抑制 |
| `_60Hz` | 60 Hz 工频抑制 |

---

## Utility.Thermocouple — 热电偶工具类

静态辅助方法，在离线数据处理或自定义算法中可用：

```csharp
double emf  = Utility.Thermocouple.ConvertTemperatureToEMF(ThermocoupleType.TypeK, 100.0);   // ℃ → V
double temp = Utility.Thermocouple.ConvertEMFToTemperature(ThermocoupleType.TypeK, emf, 25); // V + RJ → ℃

double vmin = Utility.Thermocouple.GetVmin(ThermocoupleType.TypeK);
double vmax = Utility.Thermocouple.GetVmax(ThermocoupleType.TypeK);
double tmin = Utility.Thermocouple.GetTmin(ThermocoupleType.TypeK);
double tmax = Utility.Thermocouple.GetTmax(ThermocoupleType.TypeK);
```

---

## 异常类 JYDriverException

```csharp
try { ... }
catch (JYDriverException ex)
{
    // ex.Message       — 驱动错误描述
    // ex.ExceptionName — 异常枚举名
    // ex.ErrorCode     — 错误码
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 枚举（常见值）

| 枚举值                                           | 含义                     |
| ------------------------------------------------ | ------------------------ |
| `OpenDeviceFailed`                               | 打开设备失败             |
| `CloseDeviceFailed`                              | 关闭设备失败             |
| `NoChannelAdded`                                 | 未添加通道               |
| `StartTaskFailed`                                | 启动 Task 失败           |
| `StopTaskFailed`                                 | 停止 Task 失败           |
| `TaskHasNotStarted`                              | Task 未启动即读写        |
| `TaskHasStartedCannotPerformTheSetOperation`     | Task 运行中修改参数      |
| `BufferDataOverflow`                             | 采集缓冲区溢出           |
| `ReadDataTimeout`                                | 读取超时                 |
| `SampleRateParameterInvalid`                     | 采样率超限               |
| `ChannelNumberParameterInvalid`                  | 无效通道号               |
| `ChannelInputRangeParameterInvalid`              | 无效输入量程             |
| `TriggerParameterInvalid`                        | 触发参数无效             |

---

# 完整代码示例

> 所有示例均来自 `JY6312_V1.3.8_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：AI 单点热电偶温度测量（Console）

```csharp
using System;
using System.Threading;
using JY6312;

Console.Write("请输入板卡槽位号：");
int boardNum = Convert.ToInt32(Console.ReadLine());

Console.Write("请输入通道号（0~15）：");
int channelID = Convert.ToInt32(Console.ReadLine());

JY6312AITask aiTask = new JY6312AITask(boardNum);

// 添加通道：K 型热电偶
aiTask.AddChannel(channelID, ThermocoupleType.TypeK);

aiTask.Mode = AIMode.Single;
aiTask.BuildInCJC.Enabled = true;                                  // 启用内置冷端补偿
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;

aiTask.Start();
Thread.Sleep(500);     // 等待首次转换完成

double[] temperature = new double[1];
aiTask.ReadSinglePoint(ref temperature);
Console.WriteLine($"通道 {channelID} 温度：{temperature[0]:F2} ℃");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 2：AI 单点电压测量

```csharp
var aiTask = new JY6312AITask(0);

// 电压模式：±1.25V 量程
aiTask.AddChannel(0, -1.25, 1.25);
aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double[] voltage = new double[1];
aiTask.ReadSinglePoint(ref voltage);
Console.WriteLine($"电压值：{voltage[0]:F6} V");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 3：AI 连续温度采集（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY6312;

public partial class MainForm : Form
{
    private JY6312AITask aiTask;
    private double[,] readValue;       // 多通道二维缓冲

    private void button_start_Click(object sender, EventArgs e)
    {
        try
        {
            int pointsPerRead = 10;
            int channelCount = 1;
            readValue = new double[pointsPerRead, channelCount];

            aiTask = new JY6312AITask(0);
            aiTask.AddChannel(0, ThermocoupleType.TypeK);
            aiTask.BuildInCJC.Enabled = true;
            aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;

            aiTask.Mode = AIMode.Continuous;
            aiTask.SampleRate = 10;                    // 10 Sa/s

            aiTask.CheckTerminalBlockConnectionStatusBeforeStart();
            aiTask.Start();
            timer1.Enabled = true;
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask.AvailableSamples >= (ulong)readValue.GetLength(0))
        {
            try
            {
                aiTask.ReadData(ref readValue, readValue.GetLength(0), -1);
                easyChartX_AI.Plot(readValue);
            }
            catch (JYDriverException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        timer1.Enabled = true;
    }

    private void button_stop_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (aiTask != null)
        {
            aiTask.Stop();
            aiTask.Channels.Clear();
        }
    }
}
```

---

## 示例 4：AI 多通道混合热电偶采集（不同类型）

```csharp
JY6312AITask aiTask = new JY6312AITask(0);

// 各通道独立配置热电偶类型
int[] channels = { 0, 1, 2, 3 };
ThermocoupleType[] types =
{
    ThermocoupleType.TypeK,
    ThermocoupleType.TypeJ,
    ThermocoupleType.TypeT,
    ThermocoupleType.TypeE,
};

aiTask.AddChannel(channels, types);
aiTask.BuildInCJC.Enabled = true;
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10;
aiTask.Start();

// 多通道读取：列存储 [采样点, 通道]
double[,] readValue = new double[100, 4];
while (aiTask.AvailableSamples < 100) Thread.Sleep(10);
aiTask.ReadData(ref readValue, 100, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 5：AI 有限采集热电偶温度

```csharp
JY6312AITask aiTask = new JY6312AITask(0);
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, ThermocoupleType.TypeK);

aiTask.BuildInCJC.Enabled = true;
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 200;
aiTask.SampleRate = 10;

aiTask.Start();

double[,] readValue = new double[200, 4];
aiTask.ReadData(ref readValue, 200, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 6：自定义冷端温度（不使用内置 CJC）

```csharp
JY6312AITask aiTask = new JY6312AITask(0);
aiTask.AddChannel(0, ThermocoupleType.TypeK);

// 关闭内置冷端补偿，手动设置 RJ 温度（如外接高精度 PT100 测得 25 ℃）
aiTask.BuildInCJC.Enabled = false;
aiTask.SetCJTemperature(0, 25.0);

aiTask.Mode = AIMode.Single;
aiTask.Start();
Thread.Sleep(500);

double[] temp = new double[1];
aiTask.ReadSinglePoint(ref temp);
Console.WriteLine($"温度：{temp[0]:F2} ℃（RJ=25℃）");

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 7：开路热电偶检测（OTD）

```csharp
JY6312AITask aiTask = new JY6312AITask(0);
int[] channels = { 0, 1, 2, 3 };
aiTask.AddChannel(channels, ThermocoupleType.TypeK);
aiTask.BuildInCJC.Enabled = true;
aiTask.Mode = AIMode.Single;

aiTask.Start();
Thread.Sleep(500);

// 执行开路检测
ThermocoupleConnectionStatus[] status = aiTask.DetectOpenThermocouple();
for (int i = 0; i < status.Length; i++)
{
    string state = status[i] == ThermocoupleConnectionStatus.OpenCircuit ? "开路" : "正常";
    Console.WriteLine($"通道 {aiTask.Channels[i].ID}：{state}");
}

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 8：电流测量（外接 10Ω 精密电阻）

```csharp
const double SHUNT_OHM = 10.0;

JY6312AITask aiTask = new JY6312AITask(0);
aiTask.AddChannel(0, -1.25, 1.25);          // ±1.25V → ±125mA

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10;
aiTask.PowerLineRejection.Frequency = PowerLineFrequency._50Hz;
aiTask.Start();

double[] voltage = new double[100];
while (aiTask.AvailableSamples < (ulong)voltage.Length) Thread.Sleep(10);
aiTask.ReadData(ref voltage, voltage.Length, -1);

// 换算为电流（A）
double[] current = new double[100];
for (int i = 0; i < 100; i++) current[i] = voltage[i] / SHUNT_OHM;

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 示例 9：AI 连续采集 + 数字触发

```csharp
var aiTask = new JY6312AITask(0);
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.BuildInCJC.Enabled = true;

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10;

// 数字触发：PFI0 上升沿
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Mode = AITriggerMode.Start;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;

aiTask.Start();
// 等待触发到来后自动开始采集，后续读取同连续模式...
```

---

## 示例 10：AI 软触发采集

```csharp
var aiTask = new JY6312AITask(0);
aiTask.AddChannel(0, ThermocoupleType.TypeK);
aiTask.BuildInCJC.Enabled = true;
aiTask.Mode = AIMode.Finite;
aiTask.SamplesToAcquire = 100;
aiTask.SampleRate = 10;

aiTask.Trigger.Type = AITriggerType.Software;
aiTask.Start();

// 需要时手动发软触发
aiTask.SendSoftwareTrigger();

double[] data = new double[100];
aiTask.ReadData(ref data, 100, -1);

aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 综合技巧

### 多通道数据解析

```csharp
// ReadData 多通道缓冲区为 [采样点, 通道] 列存储
double[,] buf = new double[100, 4];
aiTask.ReadData(ref buf, 100, -1);

// 提取各通道数据
double[] ch0 = new double[100];
for (int i = 0; i < 100; i++) ch0[i] = buf[i, 0];
```

### 同时读取电压和温度（诊断校准用）

```csharp
double[,] voltages     = new double[100, 4];
double[,] temperatures = new double[100, 4];
aiTask.ReadRawData(ref voltages, ref temperatures, 100, -1);
// voltages：热电偶原始电压（V）；temperatures：经 CJC 补偿后的温度（℃）
```

### 冷端补偿策略选择

```csharp
// 1. 推荐：使用 TB-68CJ 内置冷端传感器
aiTask.BuildInCJC.Enabled = true;

// 2. 无 TB-68CJ：外接高精度 RTD / 温度计手动提供 RJ
aiTask.BuildInCJC.Enabled = false;
aiTask.SetCJTemperature(0, readFromExternalRTD());

// 3. 恒温槽环境（RJ 固定已知）：直接写常数
aiTask.SetCJTemperature(0, 0.0);   // 冰浴
```

### 窗体关闭时的资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer1 != null) timer1.Enabled = false;
        if (aiTask != null) { aiTask.Stop(); aiTask.Channels.Clear(); }
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```
