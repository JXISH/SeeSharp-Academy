---
name: jy7811-driver
description: 提供 JYTEK PXIe-7811 高密度通用开关（继电器）模块的完整 C# 驱动开发指引。涵盖静态模式（Static）继电器置位、扫描模式（Scan）多步切换、数字触发与软件触发、扫描列表（ScanEntry/ScanAction/connect/sequence/entry terminator）语法、触发信号导入/导出、继电器状态查询与切换次数统计等功能。当用户使用 PXIe-7811、JY7811StaticTask、JY7811ScanTask、JY7811Device、RelayState.NormallyOpen、RelayState.NormallyClosed、TriggerType.Digital、TriggerType.Soft 开发继电器开关切换、多路复用、信号路由、自动化测试开关矩阵应用时自动应用。
---

# JY7811 驱动开发指引

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\SWITCH\JY7811\Bin\JY7811.dll`（引用到 .csproj）
- **XML 文档**：`C:\SeeSharp\JYTEK\Hardware\SWITCH\JY7811\Bin\JY7811.xml`（提供 IntelliSense 支持）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JY7811;`
- **板卡编号**：通过 JYTEK 设备管理器查看的槽位号（Slot Number），如 `0`、`1` 等

## 硬件规格速查

| 参数                          | 规格                                                                 |
| ----------------------------- | -------------------------------------------------------------------- |
| 通道数                        | 66 个单刀双掷（SPDT）继电器通道，可同时工作                          |
| 继电器类型                    | 机电式，自锁（latching）                                             |
| 最大切换电压                  | 通道间 100 V / 通道至地 100 V（CAT I）                               |
| 最大切换功率（每通道）        | 30 W / 31.25 VA（DC 至 60 Hz）                                       |
| 切换负载组合                  | 0.5 A@50 VDC；1 A@30 VDC；1 A@31.25 VAC                              |
| 最大切换/承载电流（每通道）   | 1 A（≤55 °C 时 66 通道可同时满载）                                   |
| 最小切换负载                  | 10 μA @ 10 mVDC                                                      |
| DC 路径电阻                   | 初始 < 0.5 Ω；寿命末期 ≥ 1.0 Ω                                       |
| 连接器接触电阻                | < 35 mΩ                                                              |
| 电缆电阻（ACL-1030200-2）     | 0.4 Ω/m                                                              |
| 带宽（-3 dB, 50 Ω 终端）      | 模块本体 ≥ 90 MHz；带电缆 ≥ 3 MHz                                    |
| 继电器操作时间                | 最大 4 ms                                                            |
| 预期寿命                      | 机械 5×10⁷ 次；电气 1×10⁵ 次（30 VDC, 1 A DC 阻性负载）              |
| 触发输入源                    | PXI trigger lines <0..7>，最小脉宽 150 ns                            |
| 触发输出目的                  | PXI trigger lines <0..7>，脉宽可编程 1 μs ~ 62 μs                    |
| 触发方式                      | 数字触发（Digital）/ 软件触发（Soft）                                |
| 接口类型                      | PXIe                                                                 |

### 产品型号

- **PXIe-7811**：66-CH, 100 Vdc / 1 A / 30 W, SPDT PXIe 通用继电器开关模块

### 配件

- **ACL-1030200-2**：2-meter, 200-pin LFH female C type to 4×50-pin DSUB cable
- **DIN-50-01**：50-pin DSUB terminal box with DIN rail

## 通用编程范式

JY7811 提供两种工作模式：

- **静态模式（`JY7811StaticTask`）**：直接设置某个或多个继电器到指定状态，立即生效。
- **扫描模式（`JY7811ScanTask`）**：按预先配置的扫描列表（Scan List），在触发信号驱动下依次执行多步继电器切换。

```csharp
// 静态模式：创建 → Connect → （可选）WaitUntilDebounced
var staticTask = new JY7811StaticTask(0);
staticTask.Connect(0, RelayState.NormallyClosed);    // 通道 0 切换为常闭
staticTask.WaitUntilDebounced(-1);                   // 等待去抖完成

// 扫描模式：创建 → 添加扫描列表 → 配置触发 → Start → 触发/等待 → Stop
var scanTask = new JY7811ScanTask(0);
scanTask.AddScanEntry(new int[] { 0, 1 }, RelayState.NormallyClosed, true);
scanTask.AddScanEntry(new int[] { 2, 3 }, RelayState.NormallyOpen, true);
scanTask.Trigger.Type = TriggerType.Soft;
scanTask.ContinuousScan = false;
scanTask.Start();
scanTask.SendSoftwareTrigger();     // 推动到下一个 ScanEntry
scanTask.Stop();
```

**异常处理**：始终捕获 `JYDriverException`（驱动层）和 `Exception`（系统层）。

**通道编号**：`0 ~ 65`（共 66 个 SPDT 通道）。

**继电器状态**：`RelayState.NormallyOpen`（常开，触点断开）/ `RelayState.NormallyClosed`（常闭，触点闭合）。

---

## 静态模式（JY7811StaticTask）

### 任务类：`JY7811StaticTask`

用于立即设置一个或多个继电器到指定状态，不涉及扫描列表与触发。

#### 构造函数

```csharp
new JY7811StaticTask(int slotNumber)       // 按槽位号创建（推荐）
new JY7811StaticTask(string boardName)     // 按设备管理器中的别名创建
```

#### 关键属性

| 属性           | 类型             | 说明                                       |
| -------------- | ---------------- | ------------------------------------------ |
| `Device`       | `JY7811Device`   | 设备对象（用于查询状态、切换次数等）        |
| `IsDebounced`  | `bool`           | 指示所有继电器是否已去抖稳定                |

#### 方法

```csharp
// 连接单通道到指定状态
void Connect(int channelID, RelayState relayState)

// 连接多通道到同一状态
void Connect(int[] channelIDs, RelayState relayState)

// 连接多通道到各自对应的状态
void Connect(int[] channelIDs, RelayState[] relayStates)

// 将全部 66 个通道设置到指定状态
void ConnectAll(RelayState relayState)

// 等待所有继电器去抖完成
// timeout = -1 永久等待；>0 指定毫秒超时
void WaitUntilDebounced(int timeout)
```

#### 静态模式典型用法

```csharp
var staticTask = new JY7811StaticTask(0);

// 单通道：通道 5 切换为常闭
staticTask.Connect(5, RelayState.NormallyClosed);

// 多通道同状态：通道 0、2、4 全部置常闭
staticTask.Connect(new int[] { 0, 2, 4 }, RelayState.NormallyClosed);

// 多通道各自状态
staticTask.Connect(
    new int[] { 0, 1, 2 },
    new RelayState[] { RelayState.NormallyClosed, RelayState.NormallyOpen, RelayState.NormallyClosed }
);

// 将全部通道复位为常开
staticTask.ConnectAll(RelayState.NormallyOpen);

// 等待去抖完成
staticTask.WaitUntilDebounced(-1);
```

---

## 扫描模式（JY7811ScanTask）

### 任务类：`JY7811ScanTask`

用于在触发信号驱动下依次执行一张「扫描列表」。列表由多个 **ScanEntry** 组成，每个 Entry 之间需要一次触发信号来推进；同一 Entry 内可以包含多个 **ScanAction**。

#### 构造函数

```csharp
new JY7811ScanTask(int slotNumber)
new JY7811ScanTask(string boardName)
```

#### 关键属性

| 属性              | 类型                  | 说明                                                 |
| ----------------- | --------------------- | ---------------------------------------------------- |
| `Device`          | `JY7811Device`        | 设备对象                                             |
| `ScanEntries`     | `List<ScanEntry>`     | 已配置的扫描条目集合                                 |
| `Trigger`         | `ScanTrigger`         | 触发配置对象                                         |
| `ContinuousScan`  | `bool`                | true：循环扫描列表；false：扫描一次即停             |
| `ScanEntryDelay`  | `int`                 | 每个 Entry 切换完成到发出 advance 触发的最短延迟（ms）|
| `IsScanning`      | `bool`                | 是否正在扫描                                         |
| `ScanListString`  | `string`              | 由当前扫描列表生成的字符串表示                       |
| `SignalExport`    | `ScanSignalExport`    | 信号导出配置                                         |

#### 扫描列表配置方法

```csharp
// ───── AddScanEntry：新增一个独立 Entry（Entry 之间需要触发推进）─────
void AddScanEntry(int channelID,   RelayState relayState,    bool waitUntilDebounced)
void AddScanEntry(int[] channelIDs, RelayState relayState,    bool waitUntilDebounced)
void AddScanEntry(int[] channelIDs, RelayState[] relayStates, bool waitUntilDebounced)

// ───── AddScanAction：向"最后一个 Entry"追加一个动作（不需要额外触发）─────
void AddScanAction(int channelID,   RelayState relayState,    bool waitUntilDebounced)
void AddScanAction(int[] channelIDs, RelayState relayState,    bool waitUntilDebounced)
void AddScanAction(int[] channelIDs, RelayState[] relayStates, bool waitUntilDebounced)

// 通过字符串直接注入扫描列表
void AddScan(string scanList)

// 删除指定 Entry；scanEntryIndex = -1 表示删除全部
void RemoveScanEntry(int scanEntryIndex)

// 根据当前 ScanEntries 重新生成 ScanListString
string GenerateScanListString()
```

#### 控制方法

```csharp
void Start()                          // 启动扫描任务
void Stop()                           // 停止扫描任务
void SendSoftwareTrigger()            // 发送软件触发，推进到下一个 Entry
void WaitUntilDone(int timeout)       // 等待扫描完成；timeout=-1 永久等待
```

### 扫描列表语法

`ScanListString` 描述整张扫描列表，由 3 类分隔符组合：

| 分隔符 | 名称              | 语义                                                                                                     |
| ------ | ----------------- | -------------------------------------------------------------------------------------------------------- |
| `;`    | EntryTerminator   | 结束一个 ScanEntry；**需要一次触发信号**才能推进到下一个 Entry                                             |
| `&`    | connect           | 同一 Entry 内的多个动作**尽可能并行**执行（无去抖等待），延迟最小                                          |
| `&&`   | sequence          | 同一 Entry 内的动作**依次执行并等待去抖**，如 `CH0->COM0 && CH9->COM9`                                     |

示例：

```
CH0->COM0 & CH1->COM1; CH2->COM2 && CH3->COM3;
```

含义：第 1 个 Entry 并行切换 CH0/CH1；发送一次触发后进入第 2 个 Entry，先切换 CH2 并等待去抖，再切换 CH3。

### 扫描模式典型用法

```csharp
var scanTask = new JY7811ScanTask(0);

// 清空旧列表
scanTask.RemoveScanEntry(-1);

// Entry 1：通道 0、1 同时闭合（connect，&）
scanTask.AddScanEntry(new int[] { 0, 1 }, RelayState.NormallyClosed, false);

// Entry 2：通道 2 闭合后，等待去抖，再切通道 3（sequence，&&）
scanTask.AddScanEntry(2, RelayState.NormallyClosed, true);
scanTask.AddScanAction(3, RelayState.NormallyClosed, true);

// Entry 3：全部置回常开
scanTask.AddScanEntry(new int[] { 0, 1, 2, 3 }, RelayState.NormallyOpen, true);

// 基本参数
scanTask.ContinuousScan = false;       // 只扫描一轮
scanTask.ScanEntryDelay = 10;          // 每个 Entry 完成到发 advance 触发 ≥ 10ms
scanTask.Trigger.Type  = TriggerType.Soft;

scanTask.Start();

// 首个 Entry 进入后会自动执行；从第 2 个 Entry 起需发触发
scanTask.SendSoftwareTrigger();        // 推进到 Entry 2
scanTask.SendSoftwareTrigger();        // 推进到 Entry 3

scanTask.WaitUntilDone(-1);
scanTask.Stop();
```

### 数字触发配置

```csharp
scanTask.Trigger.Type            = TriggerType.Digital;
scanTask.Trigger.Digital.Source  = DigitalTriggerSource.PXI_Trig0;   // PXI_Trig0 ~ PXI_Trig7
scanTask.Trigger.Digital.Edge    = DigitalTriggerEdge.Rising;        // Rising / Falling
```

### 扫描完成信号导出（告知外部"本条 Entry 已完成"）

```csharp
// 每完成一个 ScanEntry，将 ScanEntryComplete 信号导出到 PXI_Trig1
scanTask.SignalExport.Add(
    ScanSignalExportSource.ScanEntryComplete,
    ScanSignalExportDestination.PXI_Trig1
);

// 多个目的地
scanTask.SignalExport.Add(
    ScanSignalExportSource.ScanEntryComplete,
    new List<ScanSignalExportDestination>
    {
        ScanSignalExportDestination.PXI_Trig1,
        ScanSignalExportDestination.PXI_Trig2
    }
);

// 清除
scanTask.SignalExport.Clear(ScanSignalExportDestination.PXI_Trig1);
scanTask.SignalExport.ClearAll();
```

---

## 设备信息与继电器统计（JY7811Device）

通过 `task.Device`（`JY7811StaticTask.Device` 或 `JY7811ScanTask.Device`）访问。

#### 属性

| 属性                       | 类型       | 说明                         |
| -------------------------- | ---------- | ---------------------------- |
| `SerialNumber`             | `string`   | 板卡序列号                   |
| `DeviceID`                 | `int`      | 设备 ID                      |
| `RelayControlPulseWidth`   | `int`      | 继电器控制脉冲宽度（ms，≥1） |
| `ChannelCount_Max`         | `int`      | 最大通道数（66）             |

#### 方法

```csharp
// 获取单个继电器最近一次的开关状态（单点 / 扫描模式均适用）
void GetRelayState(int channelID, out RelayState relayState)

// 获取多个继电器的状态
void GetRelayState(int[] channelsID, out RelayState[] relayState)

// 获取继电器切换次数（用于评估寿命）
void GetRelayCount(int channelID, out uint switchingTimes)
void GetRelayCount(int[] channelsID, out uint[] switchingTimes)

// 设置/重置继电器切换次数（用于校准或维护记录）
void SetSwitchingTimes(int channelID, uint switchingTimes)
void SetSwitchingTimes(int[] channelsID, uint[] switchingTimes)
```

#### 查询全部继电器状态

```csharp
var task = new JY7811StaticTask(0);
for (int i = 0; i < 66; i++)
{
    RelayState state;
    task.Device.GetRelayState(i, out state);
    Console.WriteLine($"CH{i}: {state}");
}
```

---

## 枚举速查

### RelayState

| 值                | 说明                   |
| ----------------- | ---------------------- |
| `NormallyOpen`    | 常开（触点断开）       |
| `NormallyClosed`  | 常闭（触点闭合）       |

### TriggerType

| 值         | 说明           |
| ---------- | -------------- |
| `Soft`     | 软件触发（默认）|
| `Digital`  | 数字触发       |

### DigitalTriggerSource

| 值                             | 说明            |
| ------------------------------ | --------------- |
| `PXI_Trig0` ~ `PXI_Trig7`      | PXI 触发总线输入 |

### DigitalTriggerEdge

| 值        | 说明     |
| --------- | -------- |
| `Rising`  | 上升沿   |
| `Falling` | 下降沿   |

### ScanSignalExportSource

| 值                     | 说明                           |
| ---------------------- | ------------------------------ |
| `ScanEntryComplete`    | 每个 ScanEntry 切换完成事件    |

### ScanSignalExportDestination

| 值                             | 说明             |
| ------------------------------ | ---------------- |
| `PXI_Trig0` ~ `PXI_Trig7`      | PXI 触发总线输出 |

---

## 常见错误处理

| 异常代码                                         | 原因                              | 处理建议                             |
| ------------------------------------------------ | --------------------------------- | ------------------------------------ |
| `InitializeFailed`                               | 板卡未连接或槽位号错误            | 检查设备管理器中的槽位号/别名         |
| `ErrorParam` / `InvalidParameter`                | 参数非法                          | 检查通道号、枚举范围                 |
| `IncorrectCallOrder`                             | 调用顺序不正确                    | 遵循 创建→配置→Start→操作→Stop      |
| `CannotCall`                                     | 当前配置下不允许调用该方法        | 检查工作模式是否匹配                 |
| `SwitchIsStart`                                  | Switch 已启动后仍尝试修改通道配置  | Stop 后再改                          |
| `HardwareResourceReserved`                       | 硬件资源已被占用                  | 确认板卡未被其他进程打开              |
| `AddChnIdError` / `AddChnIdsError`               | 通道号超出 0~65 范围              | 修正通道编号                         |
| `SignalExportDestinationInvalid`                 | 信号导出目的无效                  | 检查 ScanSignalExportDestination 值 |
| `TimeOut`                                        | 等待触发或去抖超时                | 增大 timeout 或检查触发信号          |

捕获示例：

```csharp
try
{
    scanTask.Start();
    scanTask.SendSoftwareTrigger();
}
catch (JYDriverException ex)
{
    // ex.Message      — 错误描述
    // ex.ErrorCode    — 错误码
    MessageBox.Show(ex.Message);
}
```

---

# 完整 API 参考

## 命名空间与引用

```csharp
using JY7811;
// DLL: C:\SeeSharp\JYTEK\Hardware\SWITCH\JY7811\Bin\JY7811.dll
```

## JY7811StaticTask — 静态任务

### 构造函数

```csharp
new JY7811StaticTask(int slotNumber)
new JY7811StaticTask(string boardName)
```

### 属性

| 属性          | 类型           | 说明                       |
| ------------- | -------------- | -------------------------- |
| `Device`      | `JY7811Device` | 设备对象                   |
| `IsDebounced` | `bool`         | 所有继电器是否已去抖稳定   |

### 方法

```csharp
void Connect(int channelID, RelayState relayState)
void Connect(int[] channelIDs, RelayState relayState)
void Connect(int[] channelIDs, RelayState[] relayStates)
void ConnectAll(RelayState relayState)
void WaitUntilDebounced(int timeout)    // timeout = -1 永久等待
```

## JY7811ScanTask — 扫描任务

### 构造函数

```csharp
new JY7811ScanTask(int slotNumber)
new JY7811ScanTask(string boardName)
```

### 属性

| 属性             | 类型                 | 说明                                                  |
| ---------------- | -------------------- | ----------------------------------------------------- |
| `Device`         | `JY7811Device`       | 设备对象                                              |
| `ScanEntries`    | `List<ScanEntry>`    | 扫描条目集合                                          |
| `Trigger`        | `ScanTrigger`        | 触发配置                                              |
| `ContinuousScan` | `bool`               | 是否循环扫描整个列表                                  |
| `ScanEntryDelay` | `int`                | Entry 切换完成到 advance 输出触发的最短延迟（ms）     |
| `IsScanning`     | `bool`               | 是否正在扫描                                          |
| `ScanListString` | `string`             | 扫描列表的字符串表示                                  |
| `SignalExport`   | `ScanSignalExport`   | 信号导出配置                                          |

### 方法

```csharp
// 扫描列表
void AddScanEntry(int channelID, RelayState relayState, bool waitUntilDebounced)
void AddScanEntry(int[] channelIDs, RelayState relayState, bool waitUntilDebounced)
void AddScanEntry(int[] channelIDs, RelayState[] relayStates, bool waitUntilDebounced)
void AddScanAction(int channelID, RelayState relayState, bool waitUntilDebounced)
void AddScanAction(int[] channelIDs, RelayState relayState, bool waitUntilDebounced)
void AddScanAction(int[] channelIDs, RelayState[] relayStates, bool waitUntilDebounced)
void AddScan(string scanList)
void RemoveScanEntry(int scanEntryIndex)   // -1 删除全部
string GenerateScanListString()

// 控制
void Start()
void Stop()
void SendSoftwareTrigger()
void WaitUntilDone(int timeout)
```

## ScanTrigger — 触发配置

```csharp
scanTask.Trigger.Type            = TriggerType.Digital;   // Soft（默认）/ Digital
scanTask.Trigger.Digital.Source  = DigitalTriggerSource.PXI_Trig0;
scanTask.Trigger.Digital.Edge    = DigitalTriggerEdge.Rising;
```

`DigitalTrigger` 另提供构造函数：

```csharp
new DigitalTrigger(DigitalTriggerSource source, DigitalTriggerEdge edge)
```

## ScanSignalExport — 信号导出

```csharp
void Add(ScanSignalExportSource source, ScanSignalExportDestination destination)
void Add(ScanSignalExportSource source, List<ScanSignalExportDestination> destinations)
void Clear(ScanSignalExportDestination destination)
void ClearAll()
```

## ScanEntry / ScanAction

| 成员                          | 说明                                                   |
| ----------------------------- | ------------------------------------------------------ |
| `ScanEntry.ScanActions`       | 该 Entry 内的动作集合（List&lt;ScanAction&gt;）        |
| `ScanAction.ChannelIDs`       | 本动作的通道号数组                                     |
| `ScanAction.RelayStates`      | 本动作的继电器目标状态数组                             |
| `ScanAction.WaitUntilDebounced` | 是否等待所有继电器去抖稳定后再进行下一动作             |

## JY7811Device — 设备类

| 属性 / 方法                                                           | 说明                                        |
| --------------------------------------------------------------------- | ------------------------------------------- |
| `SerialNumber`                                                        | 序列号                                      |
| `DeviceID`                                                            | 设备 ID                                     |
| `RelayControlPulseWidth`                                              | 继电器控制脉冲宽度（ms，≥1）                |
| `ChannelCount_Max`                                                    | 最大通道数（66）                            |
| `GetRelayState(int, out RelayState)`                                  | 查询单通道状态                              |
| `GetRelayState(int[], out RelayState[])`                              | 查询多通道状态                              |
| `GetRelayCount(int, out uint)`                                        | 查询单通道切换次数                          |
| `GetRelayCount(int[], out uint[])`                                    | 查询多通道切换次数                          |
| `SetSwitchingTimes(int, uint)`                                        | 设置单通道切换次数（维护记录）              |
| `SetSwitchingTimes(int[], uint[])`                                    | 设置多通道切换次数                          |
| `GetInstance(int)` / `GetInstance(string)`                            | 按槽位号/别名获取设备单例                   |

## JYDriverException — 异常类

```csharp
try { ... }
catch (JYDriverException ex)
{
    // ex.Message       — 驱动错误描述
    // ex.ErrorCode     — 错误码
    // ex.FollowingException — 下一个异常（异常链）
    MessageBox.Show(ex.Message);
}
```

### 常见 `JYDriverExceptionPublic` 枚举值

| 枚举值                             | 含义                                             |
| ---------------------------------- | ------------------------------------------------ |
| `NoError`                          | 无错误                                           |
| `InitializeFailed`                 | 初始化失败（未连接/槽位号错误）                  |
| `TimeOut`                          | 超时                                             |
| `ErrorParam` / `InvalidParameter`  | 参数错误                                         |
| `IncorrectCallOrder`               | 调用顺序不正确                                   |
| `CannotCall`                       | 当前配置下不能调用该方法                          |
| `HardwareResourceReserved`         | 硬件资源已被占用                                 |
| `SwitchIsStart`                    | Switch 已启动，不允许修改配置                    |
| `AddChnIdError`                    | 通道号超出 0~65                                  |
| `AddChnIdsError`                   | 通道号数组越界                                   |
| `SignalExportDestinationInvalid`   | 无效的信号导出目的                               |

---

# 完整代码示例

## 示例 1：静态模式 — 单通道切换

```csharp
using System;
using JY7811;

// 创建静态任务（槽位号 0）
var staticTask = new JY7811StaticTask(0);

// 将通道 0 切换为常闭（触点闭合）
staticTask.Connect(0, RelayState.NormallyClosed);

// 等待去抖稳定
staticTask.WaitUntilDebounced(-1);

// 验证状态
RelayState state;
staticTask.Device.GetRelayState(0, out state);
Console.WriteLine($"CH0 state = {state}");
```

## 示例 2：静态模式 — 多通道批量切换

```csharp
var staticTask = new JY7811StaticTask(0);

// 多通道同状态：0、2、4 全部闭合
staticTask.Connect(new int[] { 0, 2, 4 }, RelayState.NormallyClosed);

// 多通道各自不同状态
int[] channels = { 10, 11, 12, 13 };
RelayState[] states =
{
    RelayState.NormallyClosed,
    RelayState.NormallyOpen,
    RelayState.NormallyClosed,
    RelayState.NormallyOpen
};
staticTask.Connect(channels, states);

// 等待所有继电器稳定
staticTask.WaitUntilDebounced(1000);

// 全部通道复位为常开
staticTask.ConnectAll(RelayState.NormallyOpen);
```

## 示例 3：静态模式 — 读取全部继电器状态

```csharp
var staticTask = new JY7811StaticTask(0);

const int channelCount = 66;
for (int i = 0; i < channelCount; i++)
{
    RelayState state;
    staticTask.Device.GetRelayState(i, out state);
    Console.WriteLine($"CH{i:D2} : {state}");
}

// 一次性批量读取
int[] ids = Enumerable.Range(0, channelCount).ToArray();
RelayState[] allStates;
staticTask.Device.GetRelayState(ids, out allStates);
```

## 示例 4：查询并重置继电器切换次数

```csharp
var staticTask = new JY7811StaticTask(0);

// 查询通道 0 的切换次数（用于寿命评估）
uint times;
staticTask.Device.GetRelayCount(0, out times);
Console.WriteLine($"CH0 switched {times} times.");

// 清零通道 0 的切换次数记录
staticTask.Device.SetSwitchingTimes(0, 0);
```

## 示例 5：扫描模式 — 软件触发

```csharp
var scanTask = new JY7811ScanTask(0);

// 清空旧列表，防止叠加
scanTask.RemoveScanEntry(-1);

// 构造三组 ScanEntry
scanTask.AddScanEntry(new int[] { 0, 1 },       RelayState.NormallyClosed, false);
scanTask.AddScanEntry(new int[] { 2, 3 },       RelayState.NormallyClosed, true);
scanTask.AddScanEntry(new int[] { 0, 1, 2, 3 }, RelayState.NormallyOpen,   true);

// 触发与扫描参数
scanTask.Trigger.Type  = TriggerType.Soft;
scanTask.ContinuousScan = false;
scanTask.ScanEntryDelay = 10;              // 每个 Entry 后至少等待 10 ms

scanTask.Start();

// 第 1 个 Entry 启动后自动执行；后续 Entry 需要触发推进
scanTask.SendSoftwareTrigger();            // → Entry 2
scanTask.SendSoftwareTrigger();            // → Entry 3

// 等待所有 Entry 完成
scanTask.WaitUntilDone(-1);
scanTask.Stop();
```

## 示例 6：扫描模式 — 数字触发 + 完成信号导出

```csharp
var scanTask = new JY7811ScanTask(0);
scanTask.RemoveScanEntry(-1);

// 用 AddScanAction 在同一 Entry 内追加动作（无需触发即可推进）
scanTask.AddScanEntry(0, RelayState.NormallyClosed, true);
scanTask.AddScanAction(1, RelayState.NormallyClosed, true);    // 追加到当前 Entry

scanTask.AddScanEntry(new int[] { 0, 1 }, RelayState.NormallyOpen, true);

// 外部数字触发（PXI_Trig0 上升沿）推进扫描
scanTask.Trigger.Type           = TriggerType.Digital;
scanTask.Trigger.Digital.Source = DigitalTriggerSource.PXI_Trig0;
scanTask.Trigger.Digital.Edge   = DigitalTriggerEdge.Rising;

// 每个 Entry 完成后，通过 PXI_Trig1 向下游发出"完成"信号
scanTask.SignalExport.Add(
    ScanSignalExportSource.ScanEntryComplete,
    ScanSignalExportDestination.PXI_Trig1
);

scanTask.ContinuousScan = false;
scanTask.ScanEntryDelay = 5;

scanTask.Start();
// … 等待外部触发依次推进 …
scanTask.WaitUntilDone(-1);
scanTask.Stop();
```

## 示例 7：扫描模式 — 直接使用扫描列表字符串

```csharp
var scanTask = new JY7811ScanTask(0);
scanTask.RemoveScanEntry(-1);

// 扫描列表语法：
//   &   → 同一 Entry 内并行切换（最小延迟）
//   &&  → 同一 Entry 内按顺序切换并等待去抖
//   ;   → 一个 Entry 结束，需要触发才进入下一个
string list = "CH0->COM0 & CH1->COM1; CH2->COM2 && CH3->COM3;";
scanTask.AddScan(list);

scanTask.Trigger.Type = TriggerType.Soft;
scanTask.Start();
scanTask.SendSoftwareTrigger();
scanTask.WaitUntilDone(-1);
scanTask.Stop();
```

## 示例 8：扫描模式 — 连续循环扫描

```csharp
var scanTask = new JY7811ScanTask(0);
scanTask.RemoveScanEntry(-1);

scanTask.AddScanEntry(0, RelayState.NormallyClosed, true);
scanTask.AddScanEntry(0, RelayState.NormallyOpen,   true);

scanTask.ContinuousScan = true;     // 列表走完后自动从头再来
scanTask.Trigger.Type   = TriggerType.Soft;
scanTask.ScanEntryDelay = 50;

scanTask.Start();

// 按某种业务节奏触发
for (int i = 0; i < 100; i++)
{
    scanTask.SendSoftwareTrigger();
    System.Threading.Thread.Sleep(100);
}

scanTask.Stop();
```

## 示例 9：WinForm 安全关闭模板

```csharp
private JY7811StaticTask staticTask;
private JY7811ScanTask   scanTask;

private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    try
    {
        if (scanTask != null && scanTask.IsScanning)
        {
            scanTask.RemoveScanEntry(-1);
            scanTask.Stop();
        }
        // StaticTask 无 Start/Stop，随对象析构自动释放
    }
    catch (JYDriverException ex)
    {
        MessageBox.Show(ex.Message);
    }
}
```

---

## 综合技巧

### 正确区分 AddScanEntry 与 AddScanAction

- `AddScanEntry(...)`：**新增** 一个独立 Entry，Entry 之间需要一次触发信号来推进。
- `AddScanAction(...)`：向 **最后一个已存在的 Entry** 追加一个动作；动作之间不需要外部触发。

典型搭配：先用 `AddScanEntry` 建立一个 Entry，再多次 `AddScanAction` 在该 Entry 内批量追加并行/顺序动作。

### waitUntilDebounced 参数的选择

- 对精度、信号完整性要求高（如测量链路切换）→ `true`，下一动作等待所有继电器触点稳定后再继续。
- 对切换速度敏感（如大规模并行切换）→ `false`，追求最小延迟，驱动将使用 `connect (&)` 语义。

### 清空扫描列表

重新配置前务必调用 `scanTask.RemoveScanEntry(-1)`，否则新 Entry 会叠加到旧列表之后。

### 继电器寿命评估

机械寿命 5×10⁷，电气寿命（30 VDC / 1 A 阻性）1×10⁵。定期通过 `GetRelayCount` 监控切换次数，接近电气寿命时建议更换模块以保证 DC 路径电阻。
