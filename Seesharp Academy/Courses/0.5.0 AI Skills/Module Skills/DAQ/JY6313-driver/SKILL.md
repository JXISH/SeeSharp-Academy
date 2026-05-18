---
name: JY6313-driver
description: 简仪科技 JY6313 应变/桥路采集模块 C# 驱动使用指南。涵盖应变测量（Strain）、比率测量（Ratiometric）、电压测量（Voltage）三大测量模式，以及桥路配置、激励电压、归零校准、分流校准、TB6313 接线盒、多卡同步、触发与信号导出等完整用法。当用户需要开发 JY6313（USB/PCIe/PXIe-6313）相关应用时调用本指南。
---

# JY6313 应变/桥路采集模块驱动使用指南

## 一、板卡定位与适用场景

JY6313 是一款 **高密度应变 / 桥路专用采集模块**（16 通道、24-bit Delta-Sigma ADC、同步采样、最高 80 kSa/s/ch），专为精密应变 / 力 / 压力 / 扭矩 / 桥式传感器测量设计。

- 型号变体：`USB-6313` / `PCIe-6313` / `PXIe-6313`（本驱动统一入口）
- 主要测量模式：
  - **Strain（应变测量）**：输出单位 `微应变 με`（ue）
  - **Ratiometric（桥路比率测量）**：输出单位 `mV/V`
  - **Voltage（电压测量）**：输出单位 `V`（仅支持差分、共模电压 0.2V~3.6V）
- 本板卡 **仅提供模拟输入（AI）**，不含 AO / DI / DO / CI / CO。
- 采样模式：`Single` / `Finite` / `Continuous`（**不支持 Record 模式**）。

> 任意应变 / 桥路 / 称重 / 微小电压采集任务均应优先选用本驱动。

---

## 二、环境与依赖

| 项目 | 说明 |
| --- | --- |
| 驱动 DLL 路径 | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY6313\Bin\JY6313.dll` |
| 目标框架 | `.NET Framework 4.6.2` |
| 命名空间 | `using JY6313;` |
| 范例代码路径 | `D:\JYTEK_Work\Examples\C#\DAQ\JY6313_V1.6.4_Examples` |
| 主任务类 | `JY6313AITask` |
| 设备类 | `JY6313Device` |
| 专用接线盒 | `TB-6313`（补全 1/4 桥、1/2 桥桥臂；分流电阻 50 kΩ/100 kΩ） |

---

## 三、硬件规格速查

### 3.1 输入特性

| 项目 | 规格 |
| --- | --- |
| 通道数 | 16（Ch0~Ch15） |
| ADC 分辨率 | 24-bit |
| ADC 类型 | Delta-Sigma |
| 采样方式 | 同步（Simultaneous） |
| 采样率范围 | **7.8125 Sa/s ~ 80 kSa/s**（每通道） |
| 采样率分辨率 | ≤ 30.517 μSa/s |
| FIFO | 128 M Samples |
| 数据传输 | DMA |
| 共模电压范围（Vcm） | **0.2 V ~ 3.6 V** |
| CMRR（DC~60Hz, Gain=6.25） | 76 dB |
| 整体精度 | 0.27% |
| 故障保护 | 上电/断电均 30 V；EX+/- 短路保护 |

### 3.2 增益档位与 Ratiometric 量程

JY6313 AI 前端支持 5 种可编程增益（对应枚举 `AIGain`），决定比率/电压测量的量程：

| 增益档位 | 枚举值 | Ratiometric 量程 (±mV/V) | Ratiometric 绝对精度（Vex=5V） | Offset Error (μV/V) | Nulling 归零范围 |
| --- | --- | --- | --- | --- | --- |
| ×6.25 | `AIGain.x6p25` | ±400/Vex | 0.09% Reading | 480/Vex | ±200 mV |
| ×12.5 | `AIGain.x12p5` | ±200/Vex | 0.09% Reading | 380/Vex | ±150 mV |
| ×25 | `AIGain.x25`   | ±100/Vex | 0.09% Reading | 350/Vex | ±125 mV |
| ×50 | `AIGain.x50`   | ±50/Vex  | 0.09% Reading | 340/Vex | ±112.5 mV |
| ×100 | `AIGain.x100` | ±25/Vex  | 0.12% Reading | 340/Vex | ±106.25 mV |

> 驱动会根据用户传入的 `rangeLow / rangeHigh` 自动选择合适增益档位（Auto Range Gain）。

### 3.3 桥路完整性（Bridge Completion）

- 支持三种桥型：**Full / Half / Quarter**。
- 半桥 / 1/4 桥需要由 **TB-6313 接线盒** 补全剩余桥臂；不使用 TB-6313 时，JY6313 只接受 **全桥输入**。
- TB-6313 上半桥补全（Half-Bridge Completion）：
  - Offset 容差：±125 μV/V max
  - 稳定性：3.8 μV/V per °C max
- TB-6313 上 1/4 桥补全（Quarter-Bridge Completion）：
  - 可选阻值：**120 Ω / 350 Ω / 1000 Ω**（软件可选）
  - 容差：±0.1% max；稳定性：25 ppm/°C max
- 分流电阻（Shunt Calibration）：**50 kΩ / 100 kΩ**，由 TB-6313 提供；不使用 TB-6313 时需外接分流电阻并在驱动中填入精确阻值。

### 3.4 桥路激励（Excitation）

- 激励输出通道数：**2 组独立激励**（输出类型：平衡差分恒压）
  - **Bank0**（对应 Ch0~Ch7）共用一组激励
  - **Bank1**（对应 Ch8~Ch15）共用一组激励
- **同一 Bank 内所有启用通道的 Vex 必须相同**，否则报错。
- 激励电压（Vex）范围：
  - **内部激励源（`ExcitationSource.Internal`）**：硬件规格 **1 V ~ 5 V**，分辨率约 1.22 mV，精度 1 mV，容差 ±0.2 mV。
  - **外部激励源（`ExcitationSource.External`）**：驱动允许填写 **1 V ~ 10 V**（对应 `JY6313Device.MinExcitationVoltage = 1`，`MaxExtExcitationVoltage = 10`）。此模式下实际激励电压由用户外供，驱动仅据此值做工程单位换算。
- 单通道最大电流：软件限流约 20 mA（`JY6313Device.MaxExcitationCurrent = 20`；硬件上限 42 mA / 故障 60 mA）。
- 噪声：150 μVrms。

### 3.5 电压测量模式限制

- 仅支持 **差分输入**，共模电压须在 **0.2 V ~ 3.6 V** 范围内。
- 最大输入电压：±0.4 V（`JY6313Device.MaxInputVoltage = 0.4`）。
- 可选量程（`AddChannel(chID, rangeLow, rangeHigh)`）：**±0.4 V / ±0.2 V / ±0.1 V / ±0.05 V / ±0.025 V**（驱动按 range 自动选增益）。
- 即使不接 TB-6313，仍需在软件中指示 `TerminalBlock` 状态（置为 `General` 或 `TB6313`）。

### 3.6 订货件

- 模块本体：`USB-6313` / `PCIe-6313` / `PXIe-6313`
- 专用配件：
  - `TB-6313`：8 通道接线盒（每块 PCIe/PXIe-6313 需 2 块凑满 16 路）
  - `RM-6313`：32 通道加固接线盒（PCIe/PXIe-6313 用）
  - `ACL-1016868-1/2`：68pin VHDC-SCSI 屏蔽电缆，1 m / 2 m

---

## 四、通用编程范式

所有 JY6313 采集流程均遵循以下步骤：

```csharp
using JY6313;

// 1. 构造 AITask（传入槽号 SlotNumber）
JY6313AITask aiTask = new JY6313AITask(slotNumber);

// 2. 设置采样模式、采样率、采样点数
aiTask.Mode = AIMode.Continuous;        // Single / Finite / Continuous
aiTask.SampleRate = 10000;              // 7.8125 ~ 80000
aiTask.SamplesToAcquire = 5000;         // Finite 模式下为本次总点数；Continuous 下为读取批大小

// 3. 添加通道（三选一：Strain / Ratiometric / Voltage）
aiTask.Channels.Clear();
aiTask.AddChannel(...);                 // 见下方各测量模式章节

// 4. 指示每个 Bank 是否安装 TB-6313 接线盒
aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.TB6313;   // Ch0~Ch7
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.TB6313;   // Ch8~Ch15
// 若无 TB-6313：TerminalBlockType.General（电压模式与全桥输入时使用）

// 5.【可选】触发 / 信号导出 / 多卡同步 / 参考时钟配置
aiTask.Trigger.Type = AITriggerType.Immediate;   // 默认立即触发

// 6.【可选】归零校准（Start 前执行；Continuous 模式下运行中也可执行）
aiTask.PerformOffsetNulling(-1);

// 7.【可选】分流校准（必须 Start 前执行；须 Bank 已接 TB-6313 或外接分流电阻）
aiTask.PerformShuntCalibration(-1);

// 8. 启动采集
aiTask.Start();

// 9. 读取数据
double[,] data = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
aiTask.ReadData(ref data, 2000);

// 10. 停止
aiTask.Stop();
```

> 约定：二维数据缓冲区的形状为 `[采样点数, 通道数]`（行=时间，列=通道）。

---

## 五、模拟输入（AI）

### 5.1 测量类型（MeasurementType）

| 枚举值 | 含义 | 输出单位 | `AddChannel` 重载 |
| --- | --- | --- | --- |
| `MeasurementType.Strain` | 应变测量 | με (ue) | 形式 A（带桥型配置 + 应变片参数） |
| `MeasurementType.Ratiometric` | 比率测量 | mV/V | 形式 B（全桥，只需 Vex + 量程）或形式 C（带桥型 + 阻值） |
| `MeasurementType.Voltage` | 电压测量 | V | 形式 D（仅量程） |

测量类型由 `AddChannel` 重载自动决定，用户无需手动设置枚举。

### 5.2 通道添加 API 汇总

> `AIChannel.Gain` 属性是 **只读**的，由驱动根据 `RangeLow/RangeHigh` 自动选定。用户不能手动赋值 `AIGain.xNN`，要换档请调整量程上下限。

#### A. 应变测量通道（Strain）

```csharp
aiTask.AddChannel(
    int chID,                               // 通道号 0~15
    StrainConfiguration strainConfig,       // QuarterBridgeI/II, HalfBridgeI/II, FullBridgeI/II/III
    double excitationVoltage,               // Vex，单位 V，范围 1~5 V（同 Bank 内需一致）
    double rangeHigh,                       // 应变量程上限，单位 με
    double rangeLow,                        // 应变量程下限，单位 με
    double gageNominalResistance,           // 应变片标称阻值 Ω（120/350/1000 等）
    double gageFactor = 2.0,                // 应变片灵敏系数（可选）
    double poissonFactor = 0.3,             // 材料泊松系数（可选，仅 HalfBridgeI/QuarterBridgeII 等桥型使用）
    double leadWireResistance = 0,          // 引线电阻 Ω（可选，用于三线制补偿）
    ExcitationSource excitationSource = ExcitationSource.Internal
);
```

> 读取得到的数据为 **微应变 (με)**。使用 `QuarterBridge*` / `HalfBridge*` 时请务必将对应 Bank 的接线盒置为 `TerminalBlockType.TB6313`。

#### B. 比率测量通道 — 全桥（Ratiometric，默认全桥）

```csharp
aiTask.AddChannel(
    int chID,
    double excitationVoltage,               // Vex，V
    double rangeLow,                        // 量程下限，单位 mV/V（如 -80）
    double rangeHigh,                       // 量程上限，单位 mV/V（如 80）
    ExcitationSource excitationSource = ExcitationSource.Internal
);
```

#### C. 比率测量通道 — 指定桥型（Ratiometric with BridgeType）

```csharp
aiTask.AddChannel(
    int chID,
    BridgeType bridgeType,                  // FullBridge / HalfBridge / QuarterBridge
    double excitationVoltage,
    double rangeLow,                        // mV/V
    double rangeHigh,                       // mV/V
    double gageNominalResistance,           // Ω
    double leadWireResistance = 0,          // Ω
    ExcitationSource excitationSource = ExcitationSource.Internal
);
```

#### D. 电压测量通道（Voltage）

```csharp
aiTask.AddChannel(
    int chID,
    double rangeLow,                        // V，-0.4 / -0.2 / -0.1 / -0.05 / -0.025
    double rangeHigh                        // V，+0.4 / +0.2 / +0.1 / +0.05 / +0.025
);
```

> 电压模式必须满足输入共模电压 0.2~3.6 V。

### 5.3 采样模式（AIMode）

| 模式 | 说明 | 典型用法 |
| --- | --- | --- |
| `AIMode.Single` | 单点软件查询式采集（调用一次 `ReadSinglePoint` 得一组当前值） | 静态监测、仪表式读数 |
| `AIMode.Finite` | 有限点采集，由 `SamplesToAcquire` 决定总点数，采完自动停止 | 事件捕获、波形片段 |
| `AIMode.Continuous` | 持续采集，硬件不停止，由用户定时 `ReadData` 分批读走数据 | 数据流记录、实时监测 |

> **JY6313 不支持 Record（录制）模式**，长时间记录请使用 Continuous 模式配合用户端写盘。

### 5.4 操作模式（OperationMode）

`aiTask.OperationMode` 决定数字滤波器特性，间接影响可用采样率：

| 枚举 | 说明 |
| --- | --- |
| `OperationMode.Auto` | 默认；驱动根据采样率自动选择 |
| `OperationMode.Normal` | 普通延迟滤波器；采样率范围通常 `500 Sa/s ~ 80 kSa/s` |
| `OperationMode.LowLatency` | 低延迟滤波器；采样率范围通常 `7.8125 Sa/s ~ 4000 Sa/s`（用于闭环实时控制） |

常量：`JY6313Device.MaxSampleRate` / `MinSampleRate` / `MaxLowLatencyModeSampleRate` / `MinNormalModeSampleRate`。

### 5.5 工频抑制

```csharp
aiTask.Advanced.PowerLineFrequency = PowerLineFrequency._50Hz;   // 或 _60Hz
```
用于内部数字滤波器抑制工频干扰（国内一般选 50 Hz）。

### 5.6 接线盒类型（TerminalBlockType）

```csharp
aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.TB6313;    // Ch0~Ch7
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.TB6313;    // Ch8~Ch15
// 可选值：General / TB6313
```

- `TB6313`：启用板外接线盒提供的桥路补全（半桥 / 1/4 桥）和内置分流电阻。
- `General`：不使用 TB-6313，仅支持全桥输入或电压测量。电压模式下也必须显式设置此属性。

### 5.7 归零校准（Offset Nulling）

```csharp
// 对所有通道执行
aiTask.PerformOffsetNulling(-1);

// 或仅对指定通道
aiTask.PerformOffsetNulling(new int[] { 0, 1, 4 });
```

- 原理：硬件归零——在输入前端产生与当前输入等大反向的电压以抵消。**不损失动态范围**。
- 时机：必须在 `AddChannel` / `TerminalBlock` 等全部参数配置完成 **之后** 调用。`Continuous` 模式下运行中也可调用。
- 用途：消除桥路初始不平衡电压。

### 5.8 分流校准（Shunt Calibration）

```csharp
// 使用 TB-6313 上内置的分流电阻（默认两种阻值 50kΩ/100kΩ）
aiTask.PerformShuntCalibration(-1);

// 外接分流电阻时需显式传入阻值数组（单位 Ω）
aiTask.PerformShuntCalibration(
    new int[] { 0, 1 },                  // 通道号
    new double[] { 50000, 50000 }        // 与通道一一对应的外接分流电阻值
);
```

- 原理：通过接入已知分流电阻模拟已知输入变化，结合实测值反推增益系数，消除系统增益误差。
- 时机：**必须在 `Start()` 之前执行**；执行前需 `AddChannel` 并正确设置 `TerminalBlock`。
- 使用 TB-6313 时，可通过 `BridgeCalInfo` 选择 `ShuntResistance.R50kOhm` 或 `R100kOhm`。

### 5.9 输入故障检测

```csharp
var faults = aiTask.DetectInputFaults();    // 返回每通道故障信息，用于诊断 EX+/- 短路、开路等
```

### 5.10 读取数据

```csharp
// Continuous 模式常用
ulong available = aiTask.AvailableSamples;   // 当前缓冲区可读样本数
ulong read     = aiTask.TransferedSamples;   // 累计已读样本数
SamplingState st = aiTask.State;             // 状态机：Ready/Running/Done/...

// 方法一：读取二维 [samples, channels]
double[,] buf2d = new double[samples, aiTask.Channels.Count];
aiTask.ReadData(ref buf2d, timeoutMs);

// 方法二：单通道一维（当只启用 1 个通道时）
double[] buf1d = new double[samples];
aiTask.ReadData(ref buf1d, timeoutMs);

// 方法三：Single 模式使用
double[] point = new double[aiTask.Channels.Count];
aiTask.ReadSinglePoint(ref point);

// 方法四：读原始码值（无工程单位换算）
int[,] raw = new int[samples, aiTask.Channels.Count];
aiTask.ReadRawData(ref raw, timeoutMs);
```

---

## 六、触发配置（AITrigger）

支持 4 种触发类型：

```csharp
aiTask.Trigger.Type = AITriggerType.Immediate;  // Immediate / Digital / Soft / Analog
aiTask.Trigger.Mode = AITriggerMode.Start;      // Start（默认）/ Reference
aiTask.Trigger.PreTriggerSamples = 100;         // Reference 模式下的预触发点数
aiTask.Trigger.ReTriggerCount = 0;              // 重触发次数（0 表示仅一次）
```

### 6.1 立即触发（Immediate）

`Start()` 后立即开始采集。默认值，无需额外配置。

### 6.2 软件触发（Soft）

```csharp
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
// 满足条件时由代码发送触发
aiTask.Trigger.Soft.SendSoftwareTrigger();
```

### 6.3 数字触发（Digital）

```csharp
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.Ext_Trig;    // Ext_Trig / PXI_Trig0 ~ PXI_Trig7
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;        // Rising / Falling
```

> `AIDigitalTriggerSource` 完整取值：`Ext_Trig`、`PXI_Trig0` ~ `PXI_Trig7`（共 9 个）。

### 6.4 模拟触发（Analog）

支持 3 种比较器：**Edge（边沿）** / **Hysteresis（迟滞）** / **Window（窗口）**。

```csharp
aiTask.Trigger.Type = AITriggerType.Analog;
// 触发源仅支持 16 个 AI 通道：Channel_0 ~ Channel_15
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Edge;  // Edge / Hysteresis / Window

// Edge
aiTask.Trigger.Analog.Edge.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Edge.Threshold = 100;                         // 阈值，单位与该通道测量单位一致（ue/(mV/V)/V）

// Hysteresis
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 200;
aiTask.Trigger.Analog.Hysteresis.LowThreshold  = 50;

// Window
aiTask.Trigger.Analog.Window.Condition = AIAnalogWindowCondition.EnteringWindow;
aiTask.Trigger.Analog.Window.HighThreshold = 500;
aiTask.Trigger.Analog.Window.LowThreshold  = -500;
```

> `AIAnalogTriggerSource` 完整取值：`Channel_0` ~ `Channel_15`（共 16 个，没有 `APFI` 等外部触发源）。

### 6.5 触发模式（AITriggerMode）

- `Start`：触发后开始采集（默认）。
- `Reference`：触发作为参考点，结合 `PreTriggerSamples` 采集触发前+触发后数据，常用于瞬态捕获。

---

## 七、参考时钟（ReferenceClock）

```csharp
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.Internal;   // 默认内部晶振
// 或外部时钟（外部可选端子仅 PXIe_Clk100）：
aiTask.Device.ReferenceClock.Source = ReferenceClockSource.External;
aiTask.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
```

PXIe 平台强烈推荐使用机箱背板 `PXIe_Clk100` 作为公共参考时钟（尤其是多卡同步场景）。USB/PCIe 平台不提供外部参考时钟选项。

---

## 八、信号导出（AISignalExport）

AI 任务通过 `aiTask.SignalExport` 导出关键信号到机箱背板或外部端子。

```csharp
aiTask.SignalExport.ClearAll();

// 将采样时钟导出到 PXI_Trig0
aiTask.SignalExport.Add(
    AISignalExportSource.SampleClock,            // 信号源
    SignalExportDestination.PXI_Trig0            // 目标端子
);

// 单个源也可一次导出到多个目标
aiTask.SignalExport.Add(
    AISignalExportSource.Trig,
    new List<SignalExportDestination> {
        SignalExportDestination.Ext_Trig,
        SignalExportDestination.PXI_Trig1
    });

// 清除单个目标端子上的导出
aiTask.SignalExport.Clear(SignalExportDestination.PXI_Trig0);
```

**信号源 `AISignalExportSource`**：`SampleClock` / `Sync` / `ReadyForTrig` / `Trig` / `EndOfRecord` / `EndOfAcquisition`。

**目标端子 `SignalExportDestination`**：`Ext_Trig` / `PXI_Trig0` ~ `PXI_Trig7`（共 9 个）。

---

## 九、多卡同步（Sync）

### 9.1 拓扑（SyncTopology）

| 枚举 | 说明 |
| --- | --- |
| `SyncTopology.Independent` | 独立运行（默认） |
| `SyncTopology.Master` | 主卡，负责发出同步脉冲与触发 |
| `SyncTopology.Slave` | 从卡，接收主卡的同步与触发 |

### 9.2 同步路由

`aiTask.Sync.Terminal` 配置同步脉冲走线（`SyncTerminal.PXI_Trig0 ~ PXI_Trig7`）。

### 9.3 同步启动次序（关键）

所有卡必须共用同一参考时钟（推荐 `PXIe_Clk100`），且必须按以下顺序调用 `Commit()` / `Start()`：

1. 所有从卡先 `Commit()` → 主卡最后 `Commit()`
2. 所有从卡先 `Start()` → 主卡最后 `Start()`

示例：

```csharp
// 配置公共参考时钟
slave.Device.ReferenceClock.Source = ReferenceClockSource.External;
slave.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
master.Device.ReferenceClock.Source = ReferenceClockSource.External;
master.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;

// 拓扑与同步路由
slave.Sync.Topology  = SyncTopology.Slave;
master.Sync.Topology = SyncTopology.Master;
slave.Sync.Terminal  = SyncTerminal.PXI_Trig0;
master.Sync.Terminal = SyncTerminal.PXI_Trig0;

// 采样率必须完全相同
slave.SampleRate  = 10000;
master.SampleRate = 10000;

// 先 Slave，后 Master
slave.Commit();
master.Commit();

slave.Start();
master.Start();
```

> `AIMode.Single` / `Finite` / `Continuous` 三种模式均支持多卡同步。

---

## 十、完整 API 关键清单

### 10.1 类

| 类 | 作用 |
| --- | --- |
| `JY6313AITask` | AI 采集任务主入口，所有配置与读取均通过它进行 |
| `JY6313Device` | 设备抽象，含 `Banks[0/1]`、`ReferenceClock`、静态规格常量 |
| `AIChannel` | 单通道配置，由 `AddChannel` 添加后在 `aiTask.Channels` 中 |
| `AITrigger` | 触发配置（`Digital` / `Soft` / `Analog` 子对象） |
| `AISignalExport` | 信号导出 |
| `Sync` | 多卡同步配置 |
| `ReferenceClock` | 参考时钟 |
| `StrainMeasInfo` | 应变测量元信息 |
| `BridgeCalInfo` | 桥路校准信息（含分流电阻选择） |

### 10.2 重要枚举

| 枚举 | 取值 |
| --- | --- |
| `AIMode` | `Single` / `Finite` / `Continuous` |
| `OperationMode` | `Auto` / `Normal` / `LowLatency` |
| `MeasurementType` | `Strain` / `Ratiometric` / `Voltage` |
| `BridgeType` | `FullBridge` / `HalfBridge` / `QuarterBridge` |
| `StrainConfiguration` | `QuarterBridgeI` / `QuarterBridgeII` / `HalfBridgeI` / `HalfBridgeII` / `FullBridgeI` / `FullBridgeII` / `FullBridgeIII` |
| `ExcitationSource` | `Internal` / `External` |
| `AIGain` | `x6p25` / `x12p5` / `x25` / `x50` / `x100` |
| `ShuntResistance` | `R50kOhm` / `R100kOhm` |
| `TerminalBlockType` | `General` / `TB6313` |
| `AITriggerType` | `Immediate` / `Digital` / `Soft` / `Analog` |
| `AITriggerMode` | `Start` / `Reference` |
| `AIDigitalTriggerSource` | `Ext_Trig` / `PXI_Trig0` ~ `PXI_Trig7` |
| `AIDigitalTriggerEdge` | `Rising` / `Falling` |
| `AIAnalogTriggerSource` | `Channel_0` ~ `Channel_15`（共 16 个，只能选 AI 通道作为模拟触发源） |
| `AIAnalogTriggerComparator` | `Edge` / `Hysteresis` / `Window` |
| `AIAnalogTriggerEdge` | `Rising` / `Falling` |
| `AIAnalogWindowCondition` | `EnteringWindow` / `LeavingWindow` |
| `AISignalExportSource` | `SampleClock` / `Sync` / `ReadyForTrig` / `Trig` / `EndOfRecord` / `EndOfAcquisition` |
| `SignalExportDestination` | `Ext_Trig` / `PXI_Trig0` ~ `PXI_Trig7` |
| `SyncTopology` | `Independent` / `Master` / `Slave` |
| `SyncTerminal` | `PXI_Trig0 ~ PXI_Trig7` |
| `ReferenceClockSource` | `Internal` / `External` |
| `ExternalReferenceClockTerminal` | `PXIe_Clk100` 等 |
| `PowerLineFrequency` | `_50Hz` / `_60Hz` |
| `SamplingState` | `Ready` / `Running` / `Done` / `Stopped` 等 |

### 10.3 常用属性 / 方法

```csharp
// JY6313Device 静态常量
JY6313Device.TotalNumberOfChannels;          // = 16
JY6313Device.MaxSampleRate;                  // = 80000
JY6313Device.MinSampleRate;                  // = 7.8125
JY6313Device.MaxLowLatencyModeSampleRate;
JY6313Device.MinNormalModeSampleRate;

// 实例属性
aiTask.SampleRate;
aiTask.SamplesToAcquire;
aiTask.Mode;
aiTask.OperationMode;
aiTask.Channels;                             // List<AIChannel>
aiTask.Trigger;
aiTask.SignalExport;
aiTask.Sync;
aiTask.Advanced;                             // 含 PowerLineFrequency 等
aiTask.AvailableSamples;
aiTask.TransferedSamples;
aiTask.State;

// 方法
aiTask.AddChannel(...);                      // 4 种重载：Strain / Ratiometric 全桥 / Ratiometric 带桥型 / Voltage
aiTask.PerformOffsetNulling(int chID);       // -1 表示所有已启用通道
aiTask.PerformOffsetNulling(int[] chIDs);
aiTask.PerformShuntCalibration(int chID);
aiTask.PerformShuntCalibration(int[] chIDs, double[] externalShuntResistances);
aiTask.DetectInputFaults();
aiTask.Commit();                             // 多卡同步场景下必需
aiTask.Start();
aiTask.Stop();
aiTask.ReadData(ref double[,] data, int timeoutMs);
aiTask.ReadData(ref double[]   data, int timeoutMs);
aiTask.ReadSinglePoint(ref double[] data);
aiTask.ReadRawData(ref int[,] rawData, int timeoutMs);
```

---

## 十一、代码示例集

### 11.1 应变测量 · 单点（1/4 桥 + TB-6313）

```csharp
using JY6313;

var aiTask = new JY6313AITask(slotNumber);

aiTask.Channels.Clear();
// 1/4 桥 I 型、Vex=5V、量程 ±40000 με、350Ω 应变片、GF=2、泊松=0.5
aiTask.AddChannel(
    chID: 0,
    strainConfig: StrainConfiguration.QuarterBridgeI,
    excitationVoltage: 5.0,
    rangeHigh: 40000,
    rangeLow: -40000,
    gageNominalResistance: 350,
    gageFactor: 2.0,
    poissonFactor: 0.5,
    leadWireResistance: 0);

aiTask.Mode = AIMode.Single;
aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.TB6313;
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.TB6313;

aiTask.Start();

double[] reading = new double[aiTask.Channels.Count];
aiTask.ReadSinglePoint(ref reading);
Console.WriteLine($"应变 = {reading[0]:F2} με");

aiTask.Stop();
```

### 11.2 应变测量 · 连续采集 + 归零 + 分流校准 + 多通道多 Bank

```csharp
using JY6313;

var aiTask = new JY6313AITask(0);

aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 2000;
aiTask.SamplesToAcquire = 1000;     // 每批读取 1000 点

aiTask.Channels.Clear();

// Bank0（Ch0~7）Vex = 2.5V；Bank1（Ch8~15）Vex = 5V —— 同 Bank 内 Vex 必须一致
double bank0Vex = 2.5, bank1Vex = 5.0;
for (int chID = 0; chID < 16; chID++)
{
    double vex = (chID <= 7) ? bank0Vex : bank1Vex;
    aiTask.AddChannel(
        chID, StrainConfiguration.FullBridgeI,
        vex, rangeHigh: 40000, rangeLow: -40000,
        gageNominalResistance: 350, gageFactor: 2.0, poissonFactor: 0.3);
}

aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.TB6313;
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.TB6313;

aiTask.PerformOffsetNulling(-1);                   // 归零
aiTask.PerformShuntCalibration(-1);                // 分流校准（使用 TB-6313 内置分流电阻；未装 TB-6313 的 Bank 必须改用外接电阻的重载 PerformShuntCalibration(chIDs[], resistances[])）

aiTask.Start();

double[,] data = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
while (running)
{
    if (aiTask.AvailableSamples >= (ulong)aiTask.SamplesToAcquire)
    {
        aiTask.ReadData(ref data, 2000);
        // 处理 data[sampleIdx, chIdx] 单位 με
    }
    Thread.Sleep(100);
}

aiTask.Stop();
```

### 11.3 比率测量 · 单点（全桥）

```csharp
using JY6313;

var aiTask = new JY6313AITask(slotNumber);
aiTask.Channels.Clear();

// 全桥比率：Vex = 5V，量程 ±80 mV/V
aiTask.AddChannel(chID: 0, excitationVoltage: 5.0, rangeLow: -80, rangeHigh: 80);

aiTask.Mode = AIMode.Single;
aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.General;   // 全桥不需要 TB-6313
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.General;

aiTask.Start();
double[] point = new double[1];
aiTask.ReadSinglePoint(ref point);
Console.WriteLine($"Ratiometric = {point[0]:F4} mV/V");
aiTask.Stop();
```

### 11.4 电压测量 · 连续采集

```csharp
using JY6313;

var aiTask = new JY6313AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 20000;
aiTask.SamplesToAcquire = 2000;

aiTask.Channels.Clear();
for (int chID = 0; chID < 4; chID++)
{
    aiTask.AddChannel(chID, rangeLow: -0.4, rangeHigh: 0.4);     // ±0.4 V 档
}

aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.General;
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.General;

aiTask.Start();

double[,] buf = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
while (running)
{
    if (aiTask.AvailableSamples >= (ulong)aiTask.SamplesToAcquire)
        aiTask.ReadData(ref buf, 2000);
    Thread.Sleep(100);
}
aiTask.Stop();
```

### 11.5 应变测量 · 有限采集 + 数字触发 + 参考触发（Reference，含预触发）

```csharp
var aiTask = new JY6313AITask(0);
aiTask.Mode = AIMode.Finite;
aiTask.SampleRate = 5000;
aiTask.SamplesToAcquire = 2000;

aiTask.Channels.Clear();
aiTask.AddChannel(0, StrainConfiguration.FullBridgeI, 5.0,
                  40000, -40000, 350, 2.0, 0.3);
aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.TB6313;
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.TB6313;

// 数字触发 + 预触发 500 点
aiTask.Trigger.Type  = AITriggerType.Digital;
aiTask.Trigger.Mode  = AITriggerMode.Reference;
aiTask.Trigger.PreTriggerSamples = 500;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.Ext_Trig;
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;
aiTask.Trigger.ReTriggerCount = 0;

aiTask.PerformOffsetNulling(-1);
aiTask.Start();

double[,] data = new double[aiTask.SamplesToAcquire, aiTask.Channels.Count];
aiTask.ReadData(ref data, -1);    // -1 表示等待至数据就绪
aiTask.Stop();
```

### 11.6 应变测量 · 连续 + 模拟触发（Hysteresis 迟滞）

```csharp
var aiTask = new JY6313AITask(0);
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;
aiTask.SamplesToAcquire = 1000;

aiTask.AddChannel(0, StrainConfiguration.FullBridgeI, 5.0,
                  40000, -40000, 350, 2.0, 0.3);
aiTask.Device.Banks[0].TerminalBlock = TerminalBlockType.TB6313;
aiTask.Device.Banks[1].TerminalBlock = TerminalBlockType.TB6313;

aiTask.Trigger.Type = AITriggerType.Analog;
aiTask.Trigger.Analog.Source = AIAnalogTriggerSource.Channel_0;     // 只能选 AI 通道作为触发源
aiTask.Trigger.Analog.Comparator = AIAnalogTriggerComparator.Hysteresis;
aiTask.Trigger.Analog.Hysteresis.Slope = AIAnalogTriggerEdge.Rising;
aiTask.Trigger.Analog.Hysteresis.HighThreshold = 1000;              // 单位 με
aiTask.Trigger.Analog.Hysteresis.LowThreshold  = 200;

aiTask.Start();
// ... 循环 ReadData
```

### 11.7 两卡同步（PXIe 主从）

```csharp
var master = new JY6313AITask(masterSlot);
var slave  = new JY6313AITask(slaveSlot);

// 相同通道与参数
master.AddChannel(0, 5.0, -80, 80);
slave .AddChannel(0, 5.0, -80, 80);

master.Device.Banks[0].TerminalBlock = TerminalBlockType.General;
master.Device.Banks[1].TerminalBlock = TerminalBlockType.General;
slave .Device.Banks[0].TerminalBlock = TerminalBlockType.General;
slave .Device.Banks[1].TerminalBlock = TerminalBlockType.General;

// 相同采样率
master.Mode = slave.Mode = AIMode.Finite;
master.SampleRate = slave.SampleRate = 20000;
master.SamplesToAcquire = slave.SamplesToAcquire = 10000;

// 公共参考时钟
master.Device.ReferenceClock.Source = ReferenceClockSource.External;
master.Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;
slave .Device.ReferenceClock.Source = ReferenceClockSource.External;
slave .Device.ReferenceClock.External.Terminal = ExternalReferenceClockTerminal.PXIe_Clk100;

// 拓扑与同步路由
master.Sync.Topology = SyncTopology.Master;
slave .Sync.Topology = SyncTopology.Slave;
master.Sync.Terminal = SyncTerminal.PXI_Trig0;
slave .Sync.Terminal = SyncTerminal.PXI_Trig0;

// 先 Slave，后 Master
slave.Commit();
master.Commit();
slave.Start();
master.Start();

double[] dMaster = new double[master.SamplesToAcquire];
double[] dSlave  = new double[slave .SamplesToAcquire];
master.ReadData(ref dMaster, -1);
slave .ReadData(ref dSlave , -1);

master.Stop();
slave .Stop();
```

### 11.8 信号导出（将采样时钟导到 PXI_Trig0）

```csharp
aiTask.SignalExport.ClearAll();
aiTask.SignalExport.Add(
    AISignalExportSource.SampleClock,
    SignalExportDestination.PXI_Trig0);
```

---

## 十二、错误处理与常见陷阱

1. **同 Bank 内 Vex 不一致会报错**：Ch0~Ch7 共用一组激励，Ch8~Ch15 共用一组；添加通道时请保证同 Bank 启用通道的 `excitationVoltage` 相同。
2. **1/4 桥、1/2 桥必须配 TB-6313**：否则无法补全桥路；不使用 TB-6313 时，仅支持全桥输入或电压测量。
3. **电压模式也要设置 `TerminalBlock`**：即便不使用 TB-6313，也必须显式赋值 `General` 或 `TB6313`。
4. **`PerformShuntCalibration` 必须在 `Start()` 之前调用**；而 `PerformOffsetNulling` 既可在 `Start()` 前，也可在 Continuous 运行期间执行。分流校准有两个重载：`PerformShuntCalibration(int)` / `PerformShuntCalibration(int[])` 依赖 TB-6313 内置分流电阻，所以所涉及的通道必须位于 **已将 TerminalBlock 设为 `TB6313` 的 Bank**；对于被 `General` 端子板所在的 Bank，必须改用 `PerformShuntCalibration(int[] chIDs, double[] externalShuntResistances)` 显式传入外接电阻值。
5. **采样率须落在 `MinSampleRate ~ MaxSampleRate` 之间**；若使用 `OperationMode.LowLatency`，实际上限会降到 `MaxLowLatencyModeSampleRate`（约 4 kSa/s）。
6. **多卡同步必须先 Slave 后 Master**：`Commit()` 与 `Start()` 调用顺序不可颠倒，且需共用同一参考时钟。
7. **`SamplesToAcquire` 含义**：
   - `Finite` 模式：本次采集总点数；
   - `Continuous` 模式：每次 `ReadData` 期望读取的批大小（通过 `AvailableSamples` 轮询）。
8. **读取超时单位为毫秒**；`-1` 代表无限等待，直到数据就绪或任务停止。
9. **JY6313 不支持 Record 模式**：如需长时间数据落盘，请在 `Continuous` 模式下由应用端负责写文件。
10. **电压模式共模电压限制 0.2 V~3.6 V**：输入信号共模超出此范围将测量异常。
11. **使用外部激励**：`ExcitationSource.External` 表示由用户提供激励电压，驱动不再输出；此时必须在 `AddChannel` 中填入与外部实际相同的 `excitationVoltage` 值，以便正确换算工程单位。
12. **结束后必须 `Stop()`**，长时间运行的 Continuous 任务不及时停止会持续占用 FIFO 与 DMA 资源。

---

## 十三、典型开发流程速记

1. 明确测量物理量 → 选测量类型：应变 / 比率 / 电压。
2. 选桥型（仅应变/比率） + 决定是否启用 TB-6313。
3. 按通道分配到 Bank0 / Bank1，**统一各 Bank 的 Vex**。
4. 选采样率与采样模式（Single / Finite / Continuous），必要时设 `OperationMode`。
5. 需要同步就配置 `ReferenceClock` + `Sync`（Slave 先于 Master）。
6. 需要触发就配置 `Trigger`（Immediate / Digital / Soft / Analog）。
7. 必要时执行 `PerformOffsetNulling` 与 `PerformShuntCalibration`（后者须在 Start 前）。
8. `Start` → 轮询 `AvailableSamples` → `ReadData` → `Stop`。

---

> 若遇到上述文档未覆盖的 API 或字段、或者应变花（Strain Rosette）等高阶应用（需结合用户自行实现的主应变/主角计算辅助类），请向用户确认具体需求后再实现，不要臆测驱动接口。
