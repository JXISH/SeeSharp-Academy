---
name: jy8902-driver
description: 提供 JYTEK PXIe-8902 6½ 位高精度数字万用表（DMM）的完整 C# 驱动开发指引。涵盖直流电压（DCV）、交流电压（ACV）、直流电流（DCI）、交流电流（ACI）、二线制电阻（Resistance2Wire）、四线制电阻（Resistance4Wire）六种测量功能；单点（SinglePoint）/ 多点（MultiPoint）/ 连续多点（ContinuousMultiPoint）三种测量模式；Aperture / NPLC 两种积分时间配置；立即 / 软件 / 数字触发；信号导出与参考时钟配置。当用户使用 PXIe-8902、JY8902Task、MeasurementMode.SinglePoint、MeasurementFunction.DCVolt、DCVoltRange、ACVoltRange、DCCurrentRange、ACCurrentRange、Resistance2WireRange、Resistance4WireRange、SamplingType.Aperture、SamplingType.NPLC、PowerLineFrequency 开发数字万用表测量、电压/电流/电阻采集、自动化测试应用时自动应用。
---

# JY8902 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DMM\JY8902\Bin\JY8902.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\DMM\JY8902\Bin\JY8902.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本（**Platform Target: x86**）
- **命名空间**：`using JY8902;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 功能             | 规格                                                         |
| ---------------- | ------------------------------------------------------------ |
| 产品定位         | 6½ 位 PXIe 高精度数字万用表（DMM）                           |
| 测量功能         | DCV / ACV / DCI / ACI / 2-Wire Resistance / 4-Wire Resistance |
| 基本精度         | 80 ppm                                                       |
| 最大采样率       | 3.5 kHz（典型 2 kS/s）                                       |
| DC 电压量程      | 0.2 V / 2 V / 20 V / 240 V                                   |
| AC 电压量程      | 0.2 V / 2 V / 20 V / 240 V（15 Hz ~ 100 kHz）                |
| DC 电流量程      | 20 mA / 200 mA / 1 A                                         |
| AC 电流量程      | 20 mA / 200 mA / 1 A（15 Hz ~ 5 kHz）                        |
| 2-Wire 电阻量程  | 100 Ω / 1 kΩ / 10 kΩ / 100 kΩ / 1 MΩ / 10 MΩ / 100 MΩ        |
| 4-Wire 电阻量程  | 100 Ω / 1 kΩ / 10 kΩ / 100 kΩ / 1 MΩ                         |
| DC 输入电阻      | 0.2 V / 2 V 量程 > 10 GΩ；20 V / 240 V 量程 10 MΩ            |
| AC 输入阻抗      | 10 MΩ ∥ 220 pF                                               |
| 输入过压保护     | DC 300 V（全量程）；AC 300 V rms                             |
| 输入隔离         | 300 V from Earth ground（CAT II）                            |
| 最大共模电压     | 300 V AC rms 或 DC                                           |
| 保险丝           | F 1.15 A 250 V，用户可替换                                   |
| 触发方式         | 立即（Immediate）/ 软件（Software）/ 数字（Digital）         |
| 参考时钟         | Internal（板载 25 MHz）/ External（PXIe_Clk100）             |
| 接口             | PXIe                                                         |

### 产品型号

- **PXIe-8902**：6½ digit PXIe 高性能数字万用表（DCV/ACV/DCI/ACI/2W-R/4W-R 六功能一体）

### 配件

- **ACL-1008902-01**：Hirose 4 针到引线端电缆

## 通用编程范式

所有测量均遵循：**创建 Task → 配置 Mode/Function/Range → 配置 Sampling → （可选触发）→ Start() → 读数据 → Stop()**

```csharp
var dmm = new JY8902Task(0);                         // 1. 创建（按槽位号）
dmm.Mode = MeasurementMode.SinglePoint;              // 2. 测量模式
dmm.Function = MeasurementFunction.DCVolt;           // 3. 测量功能
dmm.DCVolt.Range = DCVoltRange._20V;                 // 4. 量程
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;   // 5. 市电频率
dmm.Sampling.Type = SamplingType.NPLC;               // 6. 积分时间方式
dmm.Sampling.NPLC = 10;
dmm.Start();                                         // 7. 启动
double value = 0;
dmm.ReadSinglePoint(ref value);                      // 8. 读取
dmm.Stop();                                          // 9. 停止
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

---

## DMM 测量任务

### 任务类：`JY8902Task`

#### 构造函数

```csharp
new JY8902Task(int slot)                                                    // 按槽位号创建（推荐）
new JY8902Task(int slot, MeasurementFunction function, double powerLineFrequency)
new JY8902Task(string deviceName)                                           // 按别名创建
new JY8902Task(string deviceName, MeasurementFunction function, PowerLineFrequency powerLineFrequency)
```

#### 关键属性

| 属性                  | 类型                   | 说明                                          |
| --------------------- | ---------------------- | --------------------------------------------- |
| `Mode`                | `MeasurementMode`      | SinglePoint / MultiPoint / ContinuousMultiPoint（默认 SinglePoint） |
| `Function`            | `MeasurementFunction`  | DCVolt / ACVolt / DCCurrent / ACCurrent / Resistance2Wire / Resistance4Wire |
| `PowerLineFrequency`  | `PowerLineFrequency`   | `_50Hz` / `_60Hz`                             |
| `DCVolt`              | `DCVolt`               | 直流电压配置（含 `Range`）                    |
| `ACVolt`              | `ACVolt`               | 交流电压配置（含 `Range`）                    |
| `DCCurrent`           | `DCCurrent`            | 直流电流配置（含 `Range`）                    |
| `ACCurrent`           | `ACCurrent`            | 交流电流配置（含 `Range`）                    |
| `Resistance2Wire`     | `Resistance2Wire`      | 二线制电阻配置（含 `Range`）                  |
| `Resistance4Wire`     | `Resistance4Wire`      | 四线制电阻配置（含 `Range`）                  |
| `Sampling`            | `Sampling`             | 积分时间配置（`Type` / `Aperture` / `NPLC`）  |
| `Trigger`             | `TriggerIn`            | 触发配置（`Type` / `Digital` / `Delay`）      |
| `MultiPoint`          | `MultiPoint`           | 多点配置（`SampleCount` / `SampleInterval` / `SampleTrigger`） |
| `SignalExport`        | `DMMSignalExport`      | 信号导出配置                                  |
| `TriggerOut`          | `TriggerOut`           | 测量完成信号输出路由                          |
| `AvailableSamples`    | `ulong`                | 缓冲区中可读取的点数（非 SinglePoint 模式有效） |
| `TransferedSamples`   | `ulong`                | 已传输点数                                    |
| `BufLenInSamples`     | `int`                  | 缓冲区样点数（`MultiPoint.SampleCount` 需 ≤ 该值） |
| `IsOverRange`         | `bool`                 | 测量是否超量程                                |
| `DisableCalibration`  | `bool`                 | 是否禁用校准（默认 false）                    |
| `Device`              | `JY8902Device`         | 设备信息（序列号、最大采样率等）              |
| `TempInformation`     | `TempInfor`            | 板卡温度信息                                  |

#### 读取数据方法

```csharp
// SinglePoint 模式（timeout 默认，可省略）
dmm.ReadSinglePoint(ref double measureValue, int timeout = -1);
dmm.ReadRawSinglePoint(ref double measureValue, int timeout = -1);   // 返回原始电压

// MultiPoint / ContinuousMultiPoint 模式
dmm.ReadMultiPoint(ref double[] measureValues, uint numToRead, int timeout = -1);
dmm.ReadRawMultiPoint(ref int[] measureValues, uint numToRead, int timeout = -1);
```

#### 控制方法

```csharp
void Start();                 // 启动测量
void Stop();                  // 停止测量
void SendSoftwareTrigger();   // 发送软件触发（需 Trigger.Type = Software）
```

---

## 测量模式

### MeasurementMode 枚举

| 值                     | 说明                                                         |
| ---------------------- | ------------------------------------------------------------ |
| `SinglePoint`          | 单点测量，循环调用 `ReadSinglePoint`                         |
| `MultiPoint`           | 多点测量，采集 `MultiPoint.SampleCount` 点后停止             |
| `ContinuousMultiPoint` | 连续多点测量，应用轮询 `AvailableSamples` 持续读取           |

### MeasurementFunction 枚举

| 值                  | 说明                 |
| ------------------- | -------------------- |
| `DCVolt`            | 直流电压测量         |
| `ACVolt`            | 交流电压测量         |
| `DCCurrent`         | 直流电流测量         |
| `ACCurrent`         | 交流电流测量         |
| `Resistance2Wire`   | 二线制电阻测量       |
| `Resistance4Wire`   | 四线制电阻测量       |

---

## 量程配置

### DCVoltRange / ACVoltRange

| 值        | 说明              |
| --------- | ----------------- |
| `Auto`    | 自动量程          |
| `_200mV`  | 0.2 V 量程        |
| `_2V`     | 2 V 量程          |
| `_20V`    | 20 V 量程         |
| `_240V`   | 240 V 量程        |

### DCCurrentRange / ACCurrentRange

| 值         | 说明              |
| ---------- | ----------------- |
| `Auto`     | 自动量程          |
| `_20mA`    | 20 mA 量程        |
| `_200mA`   | 200 mA 量程       |
| `_1000mA`  | 1 A 量程          |

### Resistance2WireRange

| 值       | 说明         |
| -------- | ------------ |
| `Auto`   | 自动量程     |
| `_100`   | 100 Ω 量程   |
| `_1K`    | 1 kΩ 量程    |
| `_10K`   | 10 kΩ 量程   |
| `_100K`  | 100 kΩ 量程  |
| `_1M`    | 1 MΩ 量程    |
| `_10M`   | 10 MΩ 量程   |
| `_100M`  | 100 MΩ 量程  |

### Resistance4WireRange

| 值       | 说明         |
| -------- | ------------ |
| `Auto`   | 自动量程     |
| `_100`   | 100 Ω 量程   |
| `_1K`    | 1 kΩ 量程    |
| `_10K`   | 10 kΩ 量程   |
| `_100K`  | 100 kΩ 量程  |
| `_1M`    | 1 MΩ 量程    |

**使用方式：**

```csharp
dmm.DCVolt.Range         = DCVoltRange._20V;
dmm.ACVolt.Range         = ACVoltRange.Auto;
dmm.DCCurrent.Range      = DCCurrentRange._200mA;
dmm.ACCurrent.Range      = ACCurrentRange._1000mA;
dmm.Resistance2Wire.Range = Resistance2WireRange._10K;
dmm.Resistance4Wire.Range = Resistance4WireRange._1K;
```

---

## 积分时间配置（Sampling）

`Sampling` 控制 A/D 转换器对输入信号的**积分时间**，直接影响测量精度与速度。

| 属性         | 类型            | 说明                                          |
| ------------ | --------------- | --------------------------------------------- |
| `Type`       | `SamplingType`  | `Aperture`（按秒）/ `NPLC`（按电源线周期数）  |
| `Aperture`   | `double`        | 积分时间（秒），驱动会选择最接近的离散值      |
| `NPLC`       | `double`        | 电源线周期数（Number of Power Line Cycles）   |

### 典型 Aperture 值

| 市电 50Hz           | 对应 NPLC |
| ------------------- | --------- |
| 0.02 / 0.06 / 0.10 / 0.14 / 0.16 / 0.18 秒 | 1 / 3 / 5 / 7 / 8 / 9 |

| 市电 60Hz           | 对应 NPLC |
| ------------------- | --------- |
| 0.01667 / 0.05000 / 0.08333 / 0.11667 / 0.13333 / 0.15000 秒 | 1 / 3 / 5 / 7 / 8 / 9 |

```csharp
// 方式 1：Aperture（按秒）
dmm.Sampling.Type = SamplingType.Aperture;
dmm.Sampling.Aperture = 0.1;      // 100 ms 积分

// 方式 2：NPLC（推荐跨电网兼容）
dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;           // 10 个电源线周期
```

---

## 多点测量配置（MultiPoint）

仅当 `Mode = MultiPoint` 或 `ContinuousMultiPoint` 时有效：

| 属性              | 类型             | 说明                                               |
| ----------------- | ---------------- | -------------------------------------------------- |
| `SampleCount`     | `int`            | 测量点数（需 ≤ `BufLenInSamples`）                 |
| `SampleInterval`  | `double`         | 相邻测量之间的间隔（秒），通常 ≥ Aperture          |
| `SampleTrigger`   | `SampleTrigger`  | 采样触发源：`Immediate` / `PFI0` / `PFI1` / `PXI_Trig0` ~ `PXI_Trig7` |

```csharp
dmm.Mode = MeasurementMode.ContinuousMultiPoint;
dmm.MultiPoint.SampleCount    = 20;
dmm.MultiPoint.SampleInterval = 0.1;                   // 100 ms 间隔
dmm.MultiPoint.SampleTrigger  = SampleTrigger.Immediate;
```

---

## 触发配置（Trigger）

### TriggerIn 属性

| 属性       | 类型              | 说明                                               |
| ---------- | ----------------- | -------------------------------------------------- |
| `Type`     | `TriggerType`     | `Immediate` / `Software` / `Digital`               |
| `Digital`  | `DigitalTrigger`  | 数字触发子配置（`Source` + `Edge`）                |
| `Delay`    | `double`          | 触发延迟（秒）                                     |

### 数字触发

```csharp
dmm.Trigger.Type            = TriggerType.Digital;
dmm.Trigger.Digital.Source  = DigitalTriggerSource.PFI0;    // PFI0/PFI1/PXI_Trig0~7
dmm.Trigger.Digital.Edge    = MeasurementTriggerEdge.Rising; // Rising / Falling
```

### 软件触发

```csharp
dmm.Trigger.Type = TriggerType.Software;
dmm.Start();
// ...在需要时手动发送：
dmm.SendSoftwareTrigger();
```

### 立即触发（默认）

```csharp
dmm.Trigger.Type = TriggerType.Immediate;   // Start 后立即开始测量
```

### 枚举参考

| 枚举                       | 取值                                                        |
| -------------------------- | ----------------------------------------------------------- |
| `TriggerType`              | `Immediate` / `Software` / `Digital`                        |
| `DigitalTriggerSource`     | `PFI0` / `PFI1` / `PXI_Trig0` ~ `PXI_Trig7`                 |
| `SampleTrigger`            | `Immediate` / `PFI0` / `PFI1` / `PXI_Trig0` ~ `PXI_Trig7`   |
| `MeasurementTriggerEdge`   | `Rising` / `Falling`                                        |

---

## 信号导出（SignalExport）

将"测量完成"信号路由到 PFI 或 PXI 触发总线，供其他板卡同步使用。

### 方法

```csharp
dmm.SignalExport.Add(DMMSignalExportSource source, SignalExportDestination destination);
dmm.SignalExport.Add(DMMSignalExportSource source, List<SignalExportDestination> destinations);
dmm.SignalExport.Clear(SignalExportDestination destination);
dmm.SignalExport.ClearAll();
```

### 枚举参考

| 枚举                        | 取值                                                    |
| --------------------------- | ------------------------------------------------------- |
| `DMMSignalExportSource`     | `MeasureComplete`                                       |
| `SignalExportDestination`   | `None` / `PFI0` / `PFI1` / `PXI_Trig0` ~ `PXI_Trig7`    |
| `TriggerOut`                | `None` / `PFI0` / `PFI1` / `PXI_Trig0` ~ `PXI_Trig7`    |

```csharp
// 示例：将测量完成信号导出到 PXI_Trig0
dmm.SignalExport.ClearAll();
dmm.SignalExport.Add(DMMSignalExportSource.MeasureComplete, SignalExportDestination.PXI_Trig0);
```

---

## 参考时钟配置（ReferenceClock）

通过 `Device.ReferenceClock` 配置板卡的参考时钟源（需在 Task 启动前提交）。

| 属性          | 类型                        | 说明                                           |
| ------------- | --------------------------- | ---------------------------------------------- |
| `Source`      | `ReferenceClockSource`      | `Internal`（板载 25 MHz，默认）/ `External`    |
| `External.Terminal` | `ExternalReferenceClockTerminal` | `PXIe_Clk100`                      |

```csharp
dmm.Device.ReferenceClock.Source = ReferenceClockSource.External;
dmm.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
dmm.Device.ReferenceClock.Commit();
```

> ⚠️ 警告：错误的参数会导致 PLL 无法锁定参考时钟，必须关机重启才能恢复。

---

## 常见错误处理

| 异常代码                                         | 原因                         | 处理建议                        |
| ------------------------------------------------ | ---------------------------- | ------------------------------- |
| `InitializeFailed`                               | 板卡未连接或槽位号错误       | 检查设备管理器槽位号            |
| `TimeOut`                                        | 读取超时                     | 增大 timeout 或检查触发源       |
| `ErrorParam` / `InvalidParameter`                | 参数错误（量程 / 模式等）    | 按枚举取值范围重新配置          |
| `IncorrectCallOrder`                             | 调用顺序错误                 | 按"配置 → Start → Read"顺序执行 |
| `CannotCall`                                     | 当前配置不允许调用该方法     | 例如 SinglePoint 下调用 ReadMultiPoint |
| `BufferOverflow`                                 | 缓冲区溢出                   | 提高轮询频率或降低采样率        |
| `BufferDownflow`                                 | 缓冲区数据不足               | 等待更多样点或检查采样间隔      |
| `HardwareResourceReserved`                       | 硬件资源被占用               | 关闭其他使用该板卡的程序        |
| `UserBufferError`                                | 用户缓冲区错误               | 确保数组长度 ≥ numToRead        |
| `SampleOver`                                     | 采样率超过最大值             | 最大 3.5 kHz，降低采样率        |
| `SignalExportDestinationInvalid`                 | 信号导出目的地无效           | 使用合法的 `SignalExportDestination` |

---

## 更多详情

- 完整 API 参考：见下方"完整 API 参考"章节
- 各功能代码示例：见下方"代码示例"章节

---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY8902;
// DLL 路径：C:\SeeSharp\JYTEK\Hardware\DMM\JY8902\Bin\JY8902.dll
```

---

## JY8902Task — DMM 测量任务

### 构造函数

```csharp
new JY8902Task(int slot);
new JY8902Task(int slot, MeasurementFunction function, double powerLineFrequency);
new JY8902Task(string deviceName);
new JY8902Task(string deviceName, MeasurementFunction function, PowerLineFrequency powerLineFrequency);
```

### 属性汇总

| 属性                  | 类型                  | 说明                                            |
| --------------------- | --------------------- | ----------------------------------------------- |
| `Mode`                | `MeasurementMode`     | 测量模式（默认 SinglePoint）                    |
| `Function`            | `MeasurementFunction` | 测量功能（默认 DCVolt）                         |
| `PowerLineFrequency`  | `PowerLineFrequency`  | 市电频率                                        |
| `Sampling`            | `Sampling`            | 积分时间配置                                    |
| `Trigger`             | `TriggerIn`           | 触发配置                                        |
| `MultiPoint`          | `MultiPoint`          | 多点配置（非 SinglePoint 模式有效）             |
| `DCVolt`              | `DCVolt`              | 直流电压子配置                                  |
| `ACVolt`              | `ACVolt`              | 交流电压子配置                                  |
| `DCCurrent`           | `DCCurrent`           | 直流电流子配置                                  |
| `ACCurrent`           | `ACCurrent`           | 交流电流子配置                                  |
| `Resistance2Wire`     | `Resistance2Wire`     | 二线制电阻子配置                                |
| `Resistance4Wire`     | `Resistance4Wire`     | 四线制电阻子配置                                |
| `SignalExport`        | `DMMSignalExport`     | 信号导出                                        |
| `TriggerOut`          | `TriggerOut`          | 测量完成信号输出终端                            |
| `Device`              | `JY8902Device`        | 设备对象                                        |
| `TempInformation`     | `TempInfor`           | 温度信息                                        |
| `BufLenInSamples`     | `int`                 | 缓冲区样点容量                                  |
| `AvailableSamples`    | `ulong`               | 缓冲区可读样点数                                |
| `TransferedSamples`   | `ulong`               | 已传输样点数                                    |
| `IsOverRange`         | `bool`                | 是否超量程                                      |
| `DisableCalibration`  | `bool`                | 是否禁用校准                                    |

### 方法

```csharp
void Start();
void Stop();
void SendSoftwareTrigger();

void ReadSinglePoint(ref double measureValue, int timeout = -1);
void ReadRawSinglePoint(ref double measureValue, int timeout = -1);
void ReadMultiPoint(ref double[] measureValues, uint numToRead, int timeout = -1);
void ReadRawMultiPoint(ref int[] measureValues, uint numToRead, int timeout = -1);
```

---

## MeasurementMode 枚举

| 值                     | 说明                                    |
| ---------------------- | --------------------------------------- |
| `SinglePoint`          | 单点测量                                |
| `MultiPoint`           | 固定点数多点测量                        |
| `ContinuousMultiPoint` | 连续多点测量                            |

## MeasurementFunction 枚举

| 值                  | 说明             |
| ------------------- | ---------------- |
| `DCVolt`            | 直流电压         |
| `ACVolt`            | 交流电压         |
| `DCCurrent`         | 直流电流         |
| `ACCurrent`         | 交流电流         |
| `Resistance2Wire`   | 二线制电阻       |
| `Resistance4Wire`   | 四线制电阻       |

## PowerLineFrequency 枚举

| 值      | 说明      |
| ------- | --------- |
| `_50Hz` | 50 Hz 市电 |
| `_60Hz` | 60 Hz 市电 |

## SamplingType 枚举

| 值         | 说明                             |
| ---------- | -------------------------------- |
| `Aperture` | 按秒设定积分时间                 |
| `NPLC`     | 按电源线周期数设定积分时间       |

## TriggerType 枚举

| 值          | 说明           |
| ----------- | -------------- |
| `Immediate` | 立即触发（默认） |
| `Software`  | 软件触发       |
| `Digital`   | 数字边沿触发   |

---

## Sampling 类

```csharp
dmm.Sampling.Type     = SamplingType.NPLC;  // 或 SamplingType.Aperture
dmm.Sampling.Aperture = 0.1;                // 秒
dmm.Sampling.NPLC     = 10;                 // NPLC 值
```

## TriggerIn 类

```csharp
dmm.Trigger.Type           = TriggerType.Digital;
dmm.Trigger.Digital.Source = DigitalTriggerSource.PFI0;
dmm.Trigger.Digital.Edge   = MeasurementTriggerEdge.Rising;
dmm.Trigger.Delay          = 0.0;           // 触发延迟（秒）
```

## MultiPoint 类

```csharp
dmm.MultiPoint.SampleCount    = 100;
dmm.MultiPoint.SampleInterval = 0.01;        // 秒
dmm.MultiPoint.SampleTrigger  = SampleTrigger.Immediate;
```

---

## JY8902Device — 设备信息

| 属性                | 类型                       | 说明                          |
| ------------------- | -------------------------- | ----------------------------- |
| `BoardClockRate`    | `double`                   | 板卡时钟速率                  |
| `SerialNumber`      | `string`                   | 序列号                        |
| `MaxSampleRate`     | `double`                   | 单通道最大采样率              |
| `BufferSize`        | `int`                      | AI 缓冲区大小                 |
| `IsAISync`          | `bool`                     | 是否处于 AI 同步状态          |
| `ReferenceClock`    | `ReferenceClock`           | 参考时钟配置对象              |
| `DeviceID`          | `int`                      | 设备 ID                       |

### 方法

```csharp
void UpdateCalibrationInfo();
void Release();
static JY8902Device GetInstance(int cardNum);
static JY8902Device GetInstance(string aliasName);
```

---

## ReferenceClock 配置

| 属性               | 类型                                | 说明                              |
| ------------------ | ----------------------------------- | --------------------------------- |
| `Source`           | `ReferenceClockSource`              | `Internal` / `External`           |
| `External.Terminal`| `ExternalReferenceClockTerminal`    | `PXIe_Clk100`                     |
| `IsCommitAllowed`  | `bool`                              | 当前是否允许提交配置              |

### 方法

```csharp
void Commit();  // 必须在 Task 未运行时调用
```

---

## 异常类 JYDriverException

```csharp
try { ... }
catch (JYDriverException ex)
{
    // ex.Message       — 驱动错误描述
    // ex.ErrorCode     — 错误码
    MessageBox.Show(ex.Message);
}
```

### JYDriverExceptionPublic 常见值

| 枚举值                              | 含义                                |
| ----------------------------------- | ----------------------------------- |
| `NoError`                           | 无错误                              |
| `UnKnown`                           | 未知异常                            |
| `InitializeFailed`                  | 初始化失败（板卡未连接 / 槽位错误） |
| `TimeOut`                           | 超时                                |
| `ErrorParam`                        | 参数错误                            |
| `InvalidParameter`                  | 无效参数                            |
| `IncorrectCallOrder`                | 调用顺序不正确                      |
| `CannotCall`                        | 当前配置不能调用该方法              |
| `UserBufferError`                   | 用户缓冲区错误                      |
| `InvalidUserDataBuffer`             | 创建的数组大小无效                  |
| `BufferOverflow`                    | 缓冲区溢出                          |
| `BufferDownflow`                    | 缓冲区下溢                          |
| `HardwareResourceReserved`          | 硬件资源已被占用                    |
| `SampleOver`                        | 采样率超过最大值                    |
| `SignalExportDestinationInvalid`    | 信号导出目的地无效                  |

---

# 完整代码示例

> 所有示例均来自 `JY8902_V1.0.4_Examples/` 范例工程，已提取核心逻辑并加注中文注释。

---

## 示例 1：DC 电压单点测量

```csharp
using System;
using JY8902;

JY8902Task dmm = new JY8902Task(0);                       // 槽位号 0
dmm.Mode = MeasurementMode.SinglePoint;                   // 单点模式
dmm.Function = MeasurementFunction.DCVolt;                // DC 电压
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;        // 50Hz 市电
dmm.DCVolt.Range = DCVoltRange._20V;                      // ±20V 量程

// 使用 NPLC 设定积分时间
dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;

dmm.Start();

double readValue = 0;
dmm.ReadSinglePoint(ref readValue);
Console.WriteLine($"DC Voltage: {readValue:F6} V");

dmm.Stop();
```

---

## 示例 2：AC 电压单点测量

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.SinglePoint;
dmm.Function = MeasurementFunction.ACVolt;
dmm.ACVolt.Range = ACVoltRange._240V;                     // ±240V 量程
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.Aperture;
dmm.Sampling.Aperture = 0.1;                              // 100 ms 积分

dmm.Start();

double acValue = 0;
dmm.ReadSinglePoint(ref acValue);
Console.WriteLine($"AC Voltage (RMS): {acValue:F6} V");

dmm.Stop();
```

---

## 示例 3：DC 电流单点测量

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.SinglePoint;
dmm.Function = MeasurementFunction.DCCurrent;
dmm.DCCurrent.Range = DCCurrentRange._200mA;              // 200 mA 量程
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;

dmm.Start();

double currentValue = 0;
dmm.ReadSinglePoint(ref currentValue);
Console.WriteLine($"DC Current: {currentValue * 1000:F6} mA");

dmm.Stop();
```

---

## 示例 4：二线制电阻测量

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.SinglePoint;
dmm.Function = MeasurementFunction.Resistance2Wire;
dmm.Resistance2Wire.Range = Resistance2WireRange._10K;    // 10 kΩ 量程
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;

dmm.Start();

double resistance = 0;
dmm.ReadSinglePoint(ref resistance);
Console.WriteLine($"2-Wire Resistance: {resistance:F3} Ω");

dmm.Stop();
```

---

## 示例 5：四线制电阻测量（高精度）

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.SinglePoint;
dmm.Function = MeasurementFunction.Resistance4Wire;       // 4W 消除引线电阻影响
dmm.Resistance4Wire.Range = Resistance4WireRange._1K;     // 1 kΩ 量程
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;

dmm.Start();

double resistance = 0;
dmm.ReadSinglePoint(ref resistance);
Console.WriteLine($"4-Wire Resistance: {resistance:F6} Ω");

dmm.Stop();
```

---

## 示例 6：DC 电压连续多点测量（WinForm + Timer）

```csharp
using System;
using System.Windows.Forms;
using JY8902;

public partial class MainForm : Form
{
    private JY8902Task dmm;
    private double[] dataBuffer = new double[20];

    private void buttonStart_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;

        // 1. 建立 Task
        dmm = new JY8902Task(0);

        // 2. 配置为连续多点模式
        dmm.Mode = MeasurementMode.ContinuousMultiPoint;
        dmm.Function = MeasurementFunction.DCVolt;
        dmm.DCVolt.Range = DCVoltRange._20V;
        dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

        // 3. 积分时间 + 采样间隔
        dmm.Sampling.Type = SamplingType.NPLC;
        dmm.Sampling.NPLC = 1;
        dmm.MultiPoint.SampleCount    = 20;
        dmm.MultiPoint.SampleInterval = 0.02;             // 20 ms 间隔

        // 4. 可选：清除所有信号导出
        dmm.SignalExport.ClearAll();

        // 5. 启动并轮询读取
        dmm.Start();
        timer1.Enabled = true;
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        try
        {
            if (dmm.AvailableSamples >= 20)
            {
                dmm.ReadMultiPoint(ref dataBuffer, 20);
                easyChart1.Plot(dataBuffer);
            }
        }
        catch (JYDriverException ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
        timer1.Enabled = true;
    }

    private void buttonStop_Click(object sender, EventArgs e)
    {
        timer1.Enabled = false;
        if (dmm != null) dmm.Stop();
    }
}
```

---

## 示例 7：DC 电压连续多点测量 + 数字触发

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.ContinuousMultiPoint;
dmm.Function = MeasurementFunction.DCVolt;
dmm.DCVolt.Range = DCVoltRange._20V;
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 1;
dmm.MultiPoint.SampleCount    = 20;
dmm.MultiPoint.SampleInterval = 0.02;

// 配置数字触发：PFI0 上升沿启动测量
dmm.Trigger.Type            = TriggerType.Digital;
dmm.Trigger.Digital.Source  = DigitalTriggerSource.PFI0;
dmm.Trigger.Digital.Edge    = MeasurementTriggerEdge.Rising;

dmm.Start();
// 等待 PFI0 上升沿到来后自动启动测量
// 后续通过 Timer 轮询 AvailableSamples → ReadMultiPoint
```

---

## 示例 8：DC 电压连续多点测量 + 软件触发

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.ContinuousMultiPoint;
dmm.Function = MeasurementFunction.DCVolt;
dmm.DCVolt.Range = DCVoltRange._20V;
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 1;
dmm.MultiPoint.SampleCount    = 20;
dmm.MultiPoint.SampleInterval = 0.02;

// 软件触发
dmm.Trigger.Type = TriggerType.Software;
dmm.Start();

// ... 需要时手动发送 ...
dmm.SendSoftwareTrigger();

// 后续读取同连续模式
```

---

## 示例 9：导出测量完成信号到 PXI 总线（同步其他板卡）

```csharp
JY8902Task dmm = new JY8902Task(0);
dmm.Mode = MeasurementMode.ContinuousMultiPoint;
dmm.Function = MeasurementFunction.DCVolt;
dmm.DCVolt.Range = DCVoltRange._20V;
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;

dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 1;
dmm.MultiPoint.SampleCount    = 20;
dmm.MultiPoint.SampleInterval = 0.02;

// 将"测量完成"信号导出到 PXI_Trig0，供其他板卡作为触发源
dmm.SignalExport.ClearAll();
dmm.SignalExport.Add(
    DMMSignalExportSource.MeasureComplete,
    SignalExportDestination.PXI_Trig0);

dmm.Start();
```

---

## 示例 10：使用外部参考时钟

```csharp
JY8902Task dmm = new JY8902Task(0);

// 在 Start 之前配置并提交参考时钟
dmm.Device.ReferenceClock.Source = ReferenceClockSource.External;
dmm.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
dmm.Device.ReferenceClock.Commit();

dmm.Mode = MeasurementMode.SinglePoint;
dmm.Function = MeasurementFunction.DCVolt;
dmm.DCVolt.Range = DCVoltRange._20V;
dmm.PowerLineFrequency = PowerLineFrequency._50Hz;
dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;

dmm.Start();
double v = 0;
dmm.ReadSinglePoint(ref v);
dmm.Stop();
```

---

## 综合技巧

### 1. 量程选择原则

```csharp
// 原则：选择略大于待测值的量程，以获得最佳精度
// DCV 0.2V 量程：24h 精度 20 ppm Reading + 25 ppm Range（最高精度）
// DCV 240V 量程：24h 精度 25 ppm Reading + 25 ppm Range

dmm.DCVolt.Range = DCVoltRange._2V;     // 小信号（<2V），高精度
dmm.DCVolt.Range = DCVoltRange.Auto;    // 不确定范围时使用自动量程
```

### 2. NPLC vs Aperture 选择

```csharp
// NPLC：按电源线周期，对市电噪声抑制效果最佳（推荐）
dmm.Sampling.Type = SamplingType.NPLC;
dmm.Sampling.NPLC = 10;          // 10 个周期 = 200 ms @ 50Hz

// Aperture：按秒精确设置，适用于跨国应用
dmm.Sampling.Type = SamplingType.Aperture;
dmm.Sampling.Aperture = 0.1;     // 100 ms
```

### 3. 超量程判断

```csharp
double v = 0;
dmm.ReadSinglePoint(ref v);
if (dmm.IsOverRange)
{
    Console.WriteLine("警告：测量值超量程，请切换更大量程");
}
```

### 4. 窗体关闭资源释放模板

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (timer1 != null) timer1.Enabled = false;
        if (dmm != null) dmm.Stop();
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

### 5. 读取设备信息

```csharp
JY8902Task dmm = new JY8902Task(0);
Console.WriteLine($"SerialNumber: {dmm.Device.SerialNumber}");
Console.WriteLine($"MaxSampleRate: {dmm.Device.MaxSampleRate} S/s");
Console.WriteLine($"Local Temp: {dmm.TempInformation.localTemp} °C");
```
