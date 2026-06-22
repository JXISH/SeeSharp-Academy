---
name: jy6311-driver
description: 提供 JYTEK JY6311 系列 16 通道、24-bit、通道间隔离 RTD 温度采集卡（DAQ）的完整 C# 驱动开发指引。涵盖模拟输入（AI）RTD 温度/电阻/电压三类测量模式（Single/Finite/Continuous）、PT100/PT1000 两线/三线/四线接法、多种 TCR 系数（TCR3851/3916/3920/3911/3928/3750）、数字输入/输出（DI/DO）单点模式、数字触发/软触发配置、内部/外部时钟配置、多卡采样时钟同步。当用户使用 USB/PCIe/PXIe-6311、JY6311AITask、JY6311DITask、JY6311DOTask、RTDType.PT100、RTDType.PT1000、RTDTerminal.TwoWire/ThreeWire/FourWire、RTDTCRType、MeasureDataType.Temperature/Resistance/Voltage、AIMode.Single、AIMode.Finite、AIMode.Continuous 开发温度采集、热电阻测量、工业测温、环境监测、自动化测试应用时自动应用。
---

# JY6311 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\Bin\JY6311.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY6311;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能                      | 规格                                                           |
| ------------------------- | -------------------------------------------------------------- |
| AI 分辨率                 | 24-bit                                                         |
| AI 通道数                 | 16 通道（通道间隔离）                                          |
| 采样方式                  | 同步采集（Simultaneous sampling）                              |
| AI 最大采样率             | 3 kS/s（每通道）                                               |
| 温度测量精度              | 0.37 ℃（PT100，4 线制）                                        |
| 支持传感器                | RTD PT100 / PT1000                                             |
| 接线方式                  | 2 线制 / 3 线制 / 4 线制                                       |
| 温度测量范围（PT100）     | -200 ℃ ~ +850 ℃                                                |
| 温度测量范围（PT1000）    | -200 ℃ ~ +150 ℃                                                |
| 电阻测量范围              | 0 Ω ~ 400 Ω（400 Ω / 1600 Ω 两档）                             |
| 电压测量量程              | ±1.25V / ±625mV / ±312.5mV / ±156.25mV / ±78.125mV / ±39.062mV |
| 激励电流                  | 1 mA（PT100）/ 750 μA（PT1000）                                |
| 通道间隔离                | 60 VDC                                                         |
| 通道对大地隔离            | 60 VDC                                                         |
| 电源工频抑制              | 50/60 Hz 软件可选                                              |
| 触发方式                  | 数字触发 / 软件触发 / 立即触发                                 |
| DI/DO                     | 3 通道（复用 PFI0~PFI2，TTL 电平）                             |
| 接口类型                  | USB / PCIe / PXIe                                              |

### 产品型号

- **USB-6311**：16-ch 24-bit USB 通道间隔离 RTD 温度输入模块
- **PCIe-6311**：16-ch 24-bit PCIe 通道间隔离 RTD 温度输入模块
- **PXIe-6311**：16-ch 24-bit PXIe 通道间隔离 RTD 温度输入模块

## 通用编程范式

所有 Task 均遵循：**创建 Task → 添加通道 → 配置参数 → Start() → 读/写数据 → Stop() → Channels.Clear()**

```csharp
var task = new JY6311AITask(0);                                  // 1. 创建（按槽位号）
task.AddChannel(0, RTDType.PT100, RTDTerminal.FourWire);         // 2. 添加通道（温度/电阻）
task.Mode = AIMode.Continuous;                                    // 3. 配置
task.SampleRate = 100;
task.Start();                                                     // 4. 启动
// ... 读取数据 ...
task.Stop();                                                      // 5. 停止
task.Channels.Clear();                                            // 6. 清除通道
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## 模拟输入（AI）

### 任务类：`JY6311AITask`

JY6311 支持三类测量，通过不同的 `AddChannel` 重载区分：
- **RTD 温度**：`AddChannel(chId, RTDType, RTDTerminal, RTDTCRType)` — 需指定 TCR 系数
- **RTD 电阻**：`AddChannel(chId, RTDType, RTDTerminal)` — 无需 TCR
- **电压**：`AddChannel(chId, rangeLow, rangeHigh)` — 量程配对详见 [reference.md](reference.md)

> 通道编号 0~15；所有重载均支持 `int[]` 多通道版本。完整签名与枚举定义见 [reference.md](reference.md)。

#### AI 三种模式速查

| 模式         | 典型配置                       | 读取方式                                     |
| ------------ | ------------------------------ | -------------------------------------------- |
| `Single`     | `Mode=AIMode.Single`           | 轮询 `ReadSinglePoint`                       |
| `Finite`     | `+SamplesToAcquire=N`          | `WaitUntilDone()` 后 `ReadData`              |
| `Continuous` | `+SampleRate`                  | Timer 轮询 `AvailableSamples` → `ReadData`   |

#### 数字触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.PFI0;   // PFI0~PFI2 / PXI_Trig0~7
aiTask.Trigger.Digital.Edge = AIDigitalTriggerEdge.Rising;     // Rising / Falling
```

#### 软触发配置

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
aiTask.SendSoftwareTrigger();  // 手动发送软触发
```

#### 外部时钟配置

```csharp
aiTask.SampleClock.Source = AISampleClockSource.External;
aiTask.SampleClock.External.Terminal = ClockTerminal.PFI0;
aiTask.SampleClock.External.ExpectedRate = 100;
```

#### 工频抑制配置

```csharp
aiTask.PowerLineFrequency = PowerLineFrequency._50Hz;   // 国内电网
// aiTask.PowerLineFrequency = PowerLineFrequency._60Hz;  // 部分海外电网
```

---

## 多卡同步

JY6311 支持通过 PXI 背板触发总线进行多卡采样时钟同步（主卡导出时钟/触发 → 从卡接收）。

**关键步骤**：主卡 `SignalExport.Add(...)` 导出时钟到 PXI_Trig，从卡设置 `SampleClock.Source = External` 并指定同一 Trig 线。启动顺序：**先从卡，后主卡**。

> 完整代码见 [examples.md](examples.md) 示例 15。

---

## 数字输入/输出（DI/DO）

- **`JY6311DITask`**：3 通道（PFI0~PFI2），仅单点读取 `ReadSinglePoint`
- **`JY6311DOTask`**：3 通道（PFI0~PFI2），仅单点写入 `WriteSinglePoint`

> **注意**：DI 与 DO 不能同时占用同一 PFI 管脚；PFI 同时也被 AI 触发/外部时钟复用，配置时需注意资源冲突。完整 API 见 [reference.md](reference.md)，代码示例见 [examples.md](examples.md) 示例 12~13。

---

## 更多详情

- 完整 API 参考：[reference.md](reference.md)
- 各功能代码示例：[examples.md](examples.md)
- 硬件手册：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\JY-6311 Specs and Manual_EN.pdf`
- 示例工程：`C:\SeeSharp\JYTEK\Hardware\DAQ\JY6311\JY6311.Examples\`
