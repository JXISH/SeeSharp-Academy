---
name: jy5412-driver
description: 简仪 PXIe-5412 系列 96 通道 3.3V/5V CMOS 静态数字 I/O 板卡 C# 驱动使用规范。当用户需要使用 PXIe-5412 进行静态 DI/DO 读写、边沿变化检测（Change Detection）、输出看门狗（Watchdog Timer）、可编程上电状态（Programmable Power-Up States）、数字滤波（Debounce Filter）时必须加载本技能。该板卡**无计数器 / 无模拟通道 / 无触发时钟体系**。
---

# PXIe-5412 驱动使用指南

> 本文档覆盖 PXIe-5412 板卡 C# 驱动（命名空间 `JY5412`，程序集 `JY5412.dll` v1.1.2）。该系列为**纯静态数字 I/O** 板卡，**无 AI/AO/计数器/触发时钟体系**。提供 4 个端口（Port0~Port3），每端口 24 条线，共 **96 路 I/O**。
>
> 所有 API、枚举、方法签名均来源于 `JY5412.dll` 反射与 `JY5412_V1.1.1_Examples` 范例；硬件规格来源于《简仪产品型录》PXIe-5412 章节（印刷页 91）。

## 1. 环境要求

| 项 | 说明 |
|---|---|
| 驱动 DLL | `C:\SeeSharp\JYTEK\Hardware\DAQ\JY5412\Bin\JY5412.dll` |
| 范例代码 | `D:\JYTEK_Work\Examples\C#\DAQ\JY5412_V1.1.1_Examples` |
| 安装器要求 | `JY5412 Installer_V1.0.0.msi` 或更高（驱动包自带） |
| .NET Framework | 4.0 及以上 |
| 命名空间 | `using JY5412;` |
| 配套 GUI 套件 | `SeeSharpTools.JY.GUI` 1.4.7 或更高（范例中用到 LED / IndustrySwitch 控件） |
| 异常类型 | `JY5412.JYDriverException`（所有驱动层错误以此抛出） |

## 2. 硬件规格速查（PDF 印刷页 91）

### 2.1 通道与总线

| 项 | 规格 |
|---|---|
| 总通道数 | **96 路 I/O**（4 Ports × 24 Lines，Port0~Port3，Line0~Line23） |
| 方向控制 | 通过建立 DI / DO Task 分别占用相应线，**不同 line 可独立做 DI 或 DO** |
| 上电默认状态 | CMOS Input（可通过 `JY5412PowerUpStates` 编程持久化） |
| 逻辑电平 | **3.3 V / 5 V** 软件可选（**以 Port 为单位**，通过 `Device.SetLogicLevel(portID, LogicLevel)`） |
| I/O 连接器 | SCSI-100 |
| 总线接口 | PXIe x4 / x1 兼容插槽（PXI Express peripheral V1.0） |
| 机械规格 | 3U PXIe，重量 213.6 g |
| 工作温度 | 0 ~ 50 ℃，20 % ~ 80 % 相对湿度，无冷凝 |

### 2.2 电气规格

| 项 | 规格 |
|---|---|
| 输入电压 V<sub>in</sub> | 0 V ~ V<sub>DD</sub>（V<sub>DD</sub>=3.3 V 或 5 V，按 Port 配置） |
| V<sub>IH</sub> | 0.7 × V<sub>DD</sub> ~ V<sub>DD</sub> |
| V<sub>IL</sub> | 0 V ~ 0.3 × V<sub>DD</sub> |
| 输入保护范围 | **-3 V ~ +20 V** |
| 输出高电压 V<sub>OH</sub> | V<sub>DD</sub> − 0.4 V ~ V<sub>DD</sub> |
| 输出低电压 V<sub>OL</sub> | 0 V ~ 0.3 V |
| 最大输出高电流 I<sub>OH</sub> | 10 mA |
| 最大输出低电流 I<sub>OL</sub> | 25 mA |
| 输出类型 | Push-Pull |
| 内部上拉 / 下拉电阻 | 上拉 100 kΩ typ. / 下拉 70 kΩ typ.（软件可选 Floating / PullUp / PullDown） |
| I/O 传输延迟 | 输入 0.03 ms / 输出 0.18 ms |
| 连接器 +5 V 辅助电源 | 引脚 49、99，最大 1 A |

### 2.3 功能特性

| 功能 | 说明 |
|---|---|
| **数字滤波（Debounce Filter）** | 3 档可选滤波基准（10 MHz / 100 kHz / 1 kHz），对应最小脉宽范围 0 ~ 255 之间（驱动 API 对应 `Device.SetMinPulseWidth(portID, int)`，单位为滤波基准的 tick 数；范围约 100 ns ~ 255 ms） |
| **变化检测（Change Detection）** | 上升沿 / 下降沿 / 双沿可配置，硬件触发中断，由事件 `ChangeDetected` 送达用户态 |
| **输出看门狗（Watchdog Timer）** | 软件 `ResetTimer()` 喂狗；超时后锁定输出到用户指定的 `WatchdogExpirationState`（High / Low / Tristate / NoChange） |
| **可编程上电状态（Programmable Power-Up States）** | 每通道独立配置为 Input（含上拉/下拉）或 Output（含高 / 低电平） |

### 2.4 产品型号与附件

| 型号 | 总线 |
|---|---|
| PXIe-5412 | PXI Express（x1 / x4 兼容插槽） |

> 当前驱动 DLL 暴露的 API 仅支持 PXIe 版本；构造器接受 **槽位号** 或 **板卡别名**（在 JYTEK MAX 中配置）。

| 附件 | 说明 |
|---|---|
| DIN-100-1 | 100 Pin SCSI 接线端子板 |
| ACL-1020100-1 | 1 m 100 pin SCSI 双绞线电缆 |
| ACL-1020100-2 | 2 m 100 pin SCSI 双绞线电缆 |

## 3. 通用编程范式

JY5412 驱动按功能划分为四种 Task：

| Task 类 | 功能 | Start/Stop |
|---|---|---|
| `JY5412DITask` | 静态数字输入 | **无**，构造后直接 Read |
| `JY5412DOTask` | 静态数字输出 | **无**，构造后直接 Write |
| `JY5412DIChangeDetectionTask` | 数字输入电平变化检测 | **有** `Start()/Stop()` + 事件 |
| `JY5412DOWatchdogTask` | 数字输出看门狗定时器 | **有** `Start()/Stop()` + `ResetTimer()` 喂狗 |

### 3.1 生命周期模板

```
new JY5412XxxTask(slot / boardName)
    → AddChannel(portID, lineID, …)          // 必须
    → Device.SetLogicLevel(portID, level)     // 可选，配置该 port 的电平
    → Device.SetMinPulseWidth(portID, tick)   // 可选，配置去抖滤波
    → Start()           // 仅 ChangeDetection / Watchdog Task 有
    → ReadSinglePoint / WriteSinglePoint      // 或订阅 ChangeDetected 事件
    → Stop()            // 仅 ChangeDetection / Watchdog Task 有
    → Channels.Clear()  // 解除通道占用
    → Device.Release()  // 关闭设备句柄（建议在窗体 FormClosing 统一调用）
```

### 3.2 Task 构造模板

```csharp
// 按槽位号
var diTask = new JY5412DITask(slotNum: 0);
var doTask = new JY5412DOTask(0);
var cdTask = new JY5412DIChangeDetectionTask(0);
var wdTask = new JY5412DOWatchdogTask(0);

// 按板卡别名（在 MAX 中配置）
var diTask2 = new JY5412DITask("JY5412_0");
```

### 3.3 端口 / 线数常量

```csharp
uint ports = JY5412Device.NumberOfPorts;       // 4（静态属性）
uint lines = JY5412Device.NumOfLinesPerPort;   // 24
// 典型遍历所有 96 路
for (int p = 0; p < JY5412Device.NumberOfPorts; p++)
    for (int l = 0; l < JY5412Device.NumOfLinesPerPort; l++) { ... }
```

## 4. 数字输入 DI — `JY5412DITask`

### 4.1 构造与添加通道

`AddChannel` 所有重载（按参数形式整理）：

```csharp
void AddChannel(int portID, int lineID, DITerminal terminal, bool enableFilter);
void AddChannel(int portID, int[] lineID, DITerminal terminal, bool enableFilter);
void AddChannel(int portID, int[] lineID, DITerminal[] terminal);                       // 每线独立 terminal，不开滤波
void AddChannel(int portID, int[] lineID, DITerminal[] terminal, bool[] enableFilter);  // 每线独立 terminal 与滤波
void AddChannel(int[] portID, int[] lineID, DITerminal terminal, bool enableFilter);    // 跨端口批量
void AddChannel(int[] portID, int[] lineID, DITerminal[] terminal, bool enableFilter);
void AddChannel(int[] portID, int[] lineID, DITerminal[] terminal, bool[] enableFilter);
```

| 参数 | 说明 |
|---|---|
| `portID` | 0 ~ 3 |
| `lineID` | 0 ~ 23 |
| `terminal` | `DITerminal.Floating` / `PullUp` / `PullDown` |
| `enableFilter` | 是否启用数字去抖滤波（最小脉宽由 `Device.SetMinPulseWidth` 配置） |

### 4.2 读取

```csharp
using JY5412;

var diTask = new JY5412DITask(slotNum: 0);

// 单线添加
diTask.AddChannel(portID: 0, lineID: 0, DITerminal.PullDown, enableFilter: false);
diTask.AddChannel(0, 1, DITerminal.PullUp,   enableFilter: true);

// 每端口独立设置逻辑电平（3.3 V 或 5 V）
diTask.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);

// 去抖滤波最小脉宽（单位为滤波基准 tick）
diTask.Device.SetMinPulseWidth(0, 50);

bool[] buf = new bool[diTask.Channels.Count];
diTask.ReadSinglePoint(ref buf);             // 按 AddChannel 顺序返回

// 指定线读取（不依赖 AddChannel 顺序，但该线必须已 AddChannel）
bool v;
diTask.ReadSinglePoint(ref v, portID: 0, lineID: 1);

diTask.Channels.Clear();
diTask.Device.Release();
```

> **注意**：`JY5412DITask` **没有** `Start()` / `Stop()` 方法，构造 + AddChannel 后即可直接 ReadSinglePoint。

### 4.3 通道元数据

`diTask.Channels` 是 `List<DIChannel>`，每项包含：

| 属性 | 说明 |
|---|---|
| `PortID` / `LineID` | 线坐标 |
| `Terminal` | 当前端接配置（Floating / PullUp / PullDown） |
| `EnableFilter` | 是否启用滤波 |

## 5. 数字输出 DO — `JY5412DOTask`

### 5.1 构造与写入

```csharp
using JY5412;

var doTask = new JY5412DOTask(0);

// 批量添加整个 Port0
int[] lines = Enumerable.Range(0, (int)JY5412Device.NumOfLinesPerPort).ToArray();
doTask.AddChannel(portID: 0, lineID: lines);

// 或单线添加
doTask.AddChannel(1, 0);

doTask.Device.SetLogicLevel(0, LogicLevel.CMOS3_3V);
doTask.Device.SetLogicLevel(1, LogicLevel.CMOS5_0V);

// 写入全部已添加通道（长度必须等于 Channels.Count，顺序与 AddChannel 一致）
bool[] outBuf = new bool[doTask.Channels.Count];
outBuf[0] = true;
doTask.WriteSinglePoint(outBuf);

// 指定单线写入
doTask.WriteSinglePoint(writeValue: true, portID: 1, lineID: 0);

doTask.Channels.Clear();
doTask.Device.Release();
```

`AddChannel` 重载：

```csharp
void AddChannel(int portID, int lineID);
void AddChannel(int portID, int[] lineID);
void AddChannel(int[] portID, int[] lineID);
```

`WriteSinglePoint` 重载：

```csharp
void WriteSinglePoint(bool[] writeValues);                                  // 按 AddChannel 顺序
void WriteSinglePoint(bool writeValue, int portID, int lineID);             // 指定线
```

## 6. 变化检测 — `JY5412DIChangeDetectionTask`

硬件可对选定的输入线做上升沿 / 下降沿 / 双沿检测，触发时通过 `ChangeDetected` 事件送出变化信息；**此 Task 具有 `Start()` / `Stop()`**，并且可调用 `ReadSinglePoint` 同步读取当前电平。

### 6.1 完整流程

```csharp
using JY5412;

var cdTask = new JY5412DIChangeDetectionTask(0);

// AddChannel 签名：(portID, lineID, ChangeDetectionMode, DITerminal, enableFilter)
cdTask.AddChannel(0, 0, ChangeDetectionMode.RisingEdge,  DITerminal.PullDown, enableFilter: true);
cdTask.AddChannel(0, 1, ChangeDetectionMode.FallingEdge, DITerminal.PullUp,   enableFilter: false);
cdTask.AddChannel(0, 2, ChangeDetectionMode.BothEdge,    DITerminal.Floating, enableFilter: false);

// 各 Port 电平
cdTask.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);
// 滤波最小脉宽
cdTask.Device.SetMinPulseWidth(0, 20);

// 订阅事件（必须在 Start 之前）
cdTask.ChangeDetected += args =>
{
    // ChangedDetectedEventArgs.Lines 是 List<ChangeDetectionEventChannel>
    foreach (var line in args.Lines)
    {
        Console.WriteLine($"P{line.PortID}.{line.LineID} => {line.Level}");
    }
};

cdTask.Start();

// 也可同步读取当前电平
bool[] now = new bool[cdTask.Channels.Count];
cdTask.ReadSinglePoint(ref now);

cdTask.Stop();
cdTask.ChangeDetected -= …;        // 解绑
cdTask.Channels.Clear();
cdTask.Device.Release();
```

### 6.2 `AddChannel` 主要重载

```csharp
void AddChannel(int portID, int lineID, ChangeDetectionMode mode, DITerminal terminal, bool enableFilter);
void AddChannel(int portID, int[] lineID, ChangeDetectionMode mode, DITerminal terminal, bool enableFilter);
void AddChannel(int portID, int[] lineID, ChangeDetectionMode[] mode, DITerminal terminal, bool enableFilter);
void AddChannel(int portID, int[] lineID, ChangeDetectionMode mode, DITerminal[] terminal, bool enableFilter);
void AddChannel(int portID, int[] lineID, ChangeDetectionMode[] mode, DITerminal[] terminal, bool enableFilter);
void AddChannel(int[] portID, int[] lineID, ChangeDetectionMode mode, DITerminal terminal, bool enableFilter);
void AddChannel(int[] portID, int[] lineID, ChangeDetectionMode[] mode, DITerminal[] terminal, bool enableFilter);
void AddChannel(int[] portID, int[] lineID, ChangeDetectionMode[] mode, DITerminal[] terminal, bool[] enableFilter);
```

### 6.3 事件数据结构

```csharp
public class ChangedDetectedEventArgs : EventArgs
{
    public List<ChangeDetectionEventChannel> Lines { get; set; }
    public IntPtr UserData;
}

public class ChangeDetectionEventChannel
{
    public int PortID { get; set; }
    public int LineID { get; set; }
    public bool Level { get; set; }   // 变化后的电平
}
```

> **线程安全建议**：事件回调在驱动回调线程触发，**切勿直接操作 UI 控件**；推荐写入 `ConcurrentQueue<string>`，再由 UI 定时器消费（详见范例 `Winform DI Change Detection`）。

## 7. 输出看门狗 — `JY5412DOWatchdogTask`

硬件看门狗：如应用未按期 `ResetTimer()`（"喂狗"），则看门狗超时，锁定指定输出线到预设状态（`WatchdogExpirationState`），保护被控对象。

### 7.1 关键属性与方法

| 成员 | 说明 |
|---|---|
| `Timeout` (`double`) | 超时时间，**单位：秒**（驱动 API 为 double） |
| `Expired` (`bool`, 只读) | 是否已超时并锁定 |
| `AddChannel(portID, lineID, WatchdogExpirationState)` | 加入受看门狗保护的 DO 线 |
| `Start()` / `Stop()` | 启停看门狗倒计时 |
| `ResetTimer()` | 喂狗（重置定时器） |
| `ClearExpiration()` | 清除超时锁定状态（需先 `Stop()`，之后才能重新 `Start()`） |
| `Commit()` | 提交配置（驱动内部提交，属性变更后调用） |
| `Channels` | `List<WatchdogChannel>` |

### 7.2 完整示例

```csharp
using JY5412;

// DO 与 Watchdog 两者都要建（Watchdog 仅配置锁定状态，不直接写输出）
var doTask = new JY5412DOTask(0);
var wdTask = new JY5412DOWatchdogTask(0);

// 将 Port0 全部线纳入保护，超时后拉低
int[] allLines = Enumerable.Range(0, (int)JY5412Device.NumOfLinesPerPort).ToArray();
doTask.AddChannel(0, allLines);
wdTask.AddChannel(0, allLines, WatchdogExpirationState.Low);

doTask.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);

// 配置超时时间 1.5 s
wdTask.Timeout = 1.5;

// 清旧锁定
if (wdTask.Expired)
{
    wdTask.Stop();
    wdTask.ClearExpiration();
}

// 先初始化一次 DO 输出
bool[] outBuf = new bool[doTask.Channels.Count];
doTask.WriteSinglePoint(outBuf);

wdTask.Start();                 // 看门狗开始计时

// 业务循环：周期性喂狗
while (running)
{
    if (wdTask.Expired)
    {
        // 超时锁定：必须 Stop → ClearExpiration 才能恢复
        wdTask.Stop();
        wdTask.ClearExpiration();
        doTask.WriteSinglePoint(outBuf);
        wdTask.Start();
        continue;
    }
    wdTask.ResetTimer();
    Thread.Sleep(500);
}

wdTask.Stop();
wdTask.Channels.Clear();
doTask.Channels.Clear();
wdTask.Device.Release();
doTask.Device.Release();
```

### 7.3 `WatchdogExpirationState` 枚举

| 值 | 含义 |
|---|---|
| `High` | 超时后输出拉高 |
| `Low` | 超时后输出拉低 |
| `Tristate` | 超时后三态（高阻） |
| `NoChange` | 超时后保持当前电平 |

## 8. 可编程上电状态 — `JY5412PowerUpStates`

每通道独立配置上电瞬间的默认行为（Input / Output），配置存入板卡 EEPROM 持久生效。

```csharp
using JY5412;

var diTask = new JY5412DITask(0);

var ups = new JY5412PowerUpStates();

// 按 Port 设置逻辑电平（影响整个 Port）
for (int p = 0; p < JY5412Device.NumberOfPorts; p++)
{
    ups.Ports[p].Logiclevel = LogicLevel.CMOS5_0V;

    // 按 Line 设置方向与默认状态
    for (int l = 0; l < JY5412Device.NumOfLinesPerPort; l++)
    {
        // 方式 A：作为 DI 上电
        ups.Channels[p, l].Mode = PowerUpStateMode.Input;
        ups.Channels[p, l].Input.Terminal = DITerminal.PullUp;

        // 方式 B：作为 DO 上电，指定默认电平
        // ups.Channels[p, l].Mode = PowerUpStateMode.Output;
        // ups.Channels[p, l].Output.Level = true;   // 上电即为高电平
    }
}

// 写入板卡
diTask.Device.SetPowerUpStates(ups);

// 回读校验
var readBack = new JY5412PowerUpStates();
diTask.Device.GetPowerUpStates(ref readBack);

diTask.Device.Release();
```

> `SetPowerUpStates` 可由任意 Task 的 `Device` 属性调用（DI / DO / ChangeDetection / Watchdog 的 `Device` 均指向同一张卡）。

## 9. 设备级配置 — `JY5412Device`

| 成员 | 说明 |
|---|---|
| `static NumberOfPorts` (`uint`) | 固定 **4** |
| `static NumOfLinesPerPort` (`uint`) | 固定 **24** |
| `Handle` (`IntPtr`) | 底层设备句柄（只读） |
| `SetLogicLevel(int portID, LogicLevel)` | 单 port 设置逻辑电平 |
| `SetLogicLevel(int[] portID, LogicLevel[])` | 多 port 批量设置 |
| `SetMinPulseWidth(int portID, int minPulseWidth)` | 数字去抖滤波最小脉宽（tick）；**需配合 AddChannel 时的 `enableFilter=true`** |
| `SetMinPulseWidth(int[] portID, int[] minPulseWidth)` | 多 port 批量 |
| `SetPowerUpStates(JY5412PowerUpStates)` | 持久化上电默认状态 |
| `GetPowerUpStates(ref JY5412PowerUpStates)` | 读取上电默认状态 |
| `Release()` | 关闭设备句柄；建议在程序退出前统一调用 |

> 滤波 tick 与时间关系（PDF page 91）：硬件提供 3 档滤波基准（10 MHz / 100 kHz / 1 kHz），对应 tick 粒度 100 ns / 10 µs / 1 ms，`0 ~ 255` 对应 **100 ns ~ 255 ms** 之间。当前驱动 API `SetMinPulseWidth(int portID, int minPulseWidth)` 仅接收一个整数 tick 计数；具体滤波基准档位映射请以驱动测试面板（`JY5412TestPanel.exe`）或驱动后续版本说明为准。

## 10. 关键枚举速查

| 枚举 | 取值（= 底层整数值） | 说明 |
|---|---|---|
| `LogicLevel` | `CMOS3_3V=0`, `CMOS5_0V=1` | Port 逻辑电平 |
| `DITerminal` | `Floating=0`, `PullUp=1`, `PullDown=2` | DI 端接方式 |
| `ChangeDetectionMode` | `BothEdge=0`, `RisingEdge=1`, `FallingEdge=2` | 变化检测沿 |
| `WatchdogExpirationState` | `High=0`, `Low=1`, `Tristate=2`, `NoChange=3` | 看门狗超时锁定状态 |
| `PowerUpStateMode` | `Output=0`, `Input=1` | 上电默认方向 |

## 11. 异常处理

所有驱动错误均以 `JY5412.JYDriverException` 抛出；该类包含 `ErrorCode`（`int`）与 `ExceptionName`（`JYDriverExceptionPublic`）两个字段。

### 11.1 常见错误码（`JYDriverExceptionPublic`）

| 错误码 | 值 | 典型原因与对策 |
|---|---|---|
| `OpenDeviceFailed` | -10001 | 槽位号不对 / 驱动未装 / 板卡被其他进程占用 |
| `CloseDeviceFailed` | -10002 | 资源未正确释放 |
| `ResetDeviceFailed` | -10003 | 硬件异常，尝试重启板卡 |
| `GetDevicePropertyFailed` | -10004 | 驱动版本不匹配或板卡未就绪 |
| `SetDevicePropertyFailed` | -10005 | 参数越界（如 portID > 3） |
| `HardwareResourceIsReserved` | -10006 | 同一 line 被其他 Task 占用（DI 与 DO 不可同时占 → 需 Channels.Clear 先释放） |
| `MethodNotPermitedToCallWhenCurrentStatus` | -10007 | 方法调用时机错误（如 Watchdog 超时后直接 ResetTimer） |
| `NoChannelAdded` | -10009 | ReadSinglePoint / WriteSinglePoint 前未 AddChannel |
| `NotSupportOperationForCurrentDevice` | -10010 | 对当前板卡不支持的操作（不支持硬件触发/采样时钟） |
| `ReadDataFailed` / `ReadDataTimeout` | -10016 / -10017 | 读取异常，检查电缆与供电 |
| `WriteDataFailed` / `WriteDataTimeout` | -10018 / -10019 | 写入异常 |
| `WritePointInvalid` | -10030 | `WriteSinglePoint` 传入数组长度与 Channels.Count 不一致 |
| `TaskHasStartedCannotPerformTheSetOperation` | -10104 | 在 Start 后试图修改属性（仅对 ChangeDetection / Watchdog 有效） |
| `ArrayLengthsNotConsistent` | -10035 | 批量 AddChannel 的 portID/lineID/terminal/enableFilter 数组长度不一致 |

### 11.2 推荐写法

```csharp
try
{
    diTask = new JY5412DITask(slot);
    diTask.AddChannel(0, 0, DITerminal.PullDown, false);
    diTask.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);

    bool[] buf = new bool[diTask.Channels.Count];
    diTask.ReadSinglePoint(ref buf);
}
catch (JYDriverException jex)
{
    MessageBox.Show($"JY5412 驱动错误 [{jex.ExceptionName}/{jex.ErrorCode}]: {jex.Message}");
}
catch (Exception ex)
{
    MessageBox.Show(ex.Message);
}
finally
{
    diTask?.Channels.Clear();
    diTask?.Device.Release();
}
```

## 12. 完整 API 参考（来自 `JY5412.dll` 反射，v1.1.2）

### 12.1 Task 类汇总

| 类 | 构造 | 有 Start/Stop | 主要方法 |
|---|---|---|---|
| `JY5412DITask` | `(int slotNum)` / `(string boardName)` | **否** | `AddChannel` ×7, `RemoveChannel`, `ReadSinglePoint(ref bool[])`, `ReadSinglePoint(ref bool, int, int)` |
| `JY5412DOTask` | `(int slotNum)` / `(string boardName)` | **否** | `AddChannel` ×3, `RemoveChannel`, `WriteSinglePoint(bool[])`, `WriteSinglePoint(bool, int, int)` |
| `JY5412DIChangeDetectionTask` | `(int slotNumber)` / `(string boardName)` | **是** | `AddChannel` ×8, `RemoveChannel`, `ReadSinglePoint` ×2, `Start`, `Stop`, 事件 `ChangeDetected` |
| `JY5412DOWatchdogTask` | `(int slotNumber)` / `(string boardName)` | **是** | `AddChannel` ×5, `RemoveChannel`, `Start`, `Stop`, `ResetTimer`, `ClearExpiration`, `Commit` |

所有 Task 都具有：
- `Channels` (`List<T>`) —— 已添加通道集合
- `Device` (`JY5412Device`) —— 板卡级配置入口

### 12.2 数据 / 配置类

| 类 | 用途 |
|---|---|
| `DIChannel` | DI Task 的通道元数据 (`PortID`, `LineID`, `Terminal`, `EnableFilter`) |
| `DOChannel` | DO Task 的通道元数据 (`PortID`, `LineID`) |
| `ChangeDetectionChannel` | Change Detection Task 通道 (`PortID`, `LineID`, `Terminal`, `EnableFilter`, `ChangeDetectionMode`) |
| `ChangeDetectionEventChannel` | 事件回调中每条变化线 (`PortID`, `LineID`, `Level`) |
| `ChangedDetectedEventArgs` | 事件参数 (`List<ChangeDetectionEventChannel> Lines`) |
| `WatchdogChannel` | 看门狗通道 (`PortID`, `LineID`, `ExpirationState`) |
| `JY5412PowerUpStates` | 上电状态容器 (`Ports[]`, `Channels[,]`) |
| `PowerUpStatePortParam` | Port 粒度 (`Logiclevel`) |
| `ChannelPowerUpStates` | 通道粒度 (`Mode`, `Input`, `Output`) |
| `PowerUpStateInputParam` | `Terminal`（DITerminal） |
| `PowerUpStateOutputParam` | `Level`（bool） |

### 12.3 `JY5412Device`

```csharp
static uint NumberOfPorts        { get; }   // = 4
static uint NumOfLinesPerPort    { get; }   // = 24
IntPtr Handle                    { get; }

void  SetLogicLevel(int portID, LogicLevel logicLevel);
void  SetLogicLevel(int[] portID, LogicLevel[] logicLevel);
void  SetMinPulseWidth(int portID, int minPulseWidth);
void  SetMinPulseWidth(int[] portID, int[] minPulseWidth);
void  SetPowerUpStates(JY5412PowerUpStates powerUpStates);
void  GetPowerUpStates(ref JY5412PowerUpStates powerUpStates);
int   Release();
```

### 12.4 日志开关 `JYLog`

```csharp
JYLog.EnableLog = true;
JYLog.LogLevel  = JYLogLevel.DEBUG;   // DEBUG / INFO / WARN / ERROR / FATAL
JYLog.Print(JYLogLevel.INFO, "msg {0}", argObj);
```

## 13. 完整代码示例

### 13.1 示例：读取 Port0 全部 24 路 DI

```csharp
using System;
using System.Linq;
using JY5412;

class DemoDIAll
{
    static void Main()
    {
        var diTask = new JY5412DITask(slotNum: 0);
        try
        {
            int[] lines = Enumerable.Range(0, (int)JY5412Device.NumOfLinesPerPort).ToArray();
            diTask.AddChannel(0, lines, DITerminal.PullDown, enableFilter: false);
            diTask.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);

            bool[] buf = new bool[diTask.Channels.Count];
            diTask.ReadSinglePoint(ref buf);

            for (int i = 0; i < buf.Length; i++)
                Console.WriteLine($"P0.{i} = {buf[i]}");
        }
        catch (JYDriverException jex)
        {
            Console.WriteLine($"Err {jex.ExceptionName}/{jex.ErrorCode}: {jex.Message}");
        }
        finally
        {
            diTask.Channels.Clear();
            diTask.Device.Release();
        }
    }
}
```

### 13.2 示例：变化检测（上升沿）+ 事件回调

```csharp
using System;
using System.Collections.Concurrent;
using JY5412;

class DemoCD
{
    static readonly ConcurrentQueue<string> q = new ConcurrentQueue<string>();

    static void Main()
    {
        var task = new JY5412DIChangeDetectionTask(0);

        task.AddChannel(0, 0, ChangeDetectionMode.RisingEdge, DITerminal.PullDown, enableFilter: true);
        task.AddChannel(0, 1, ChangeDetectionMode.BothEdge,   DITerminal.PullUp,   enableFilter: false);

        task.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);
        task.Device.SetMinPulseWidth(0, 20);

        task.ChangeDetected += e =>
        {
            foreach (var l in e.Lines)
                q.Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] P{l.PortID}.{l.LineID}={l.Level}");
        };

        task.Start();
        Console.WriteLine("Press Enter to stop...");
        while (Console.KeyAvailable == false)
        {
            while (q.TryDequeue(out var s)) Console.WriteLine(s);
            System.Threading.Thread.Sleep(50);
        }
        Console.ReadKey();

        task.Stop();
        task.Channels.Clear();
        task.Device.Release();
    }
}
```

### 13.3 示例：看门狗保护 DO 输出

```csharp
using System.Linq;
using System.Threading;
using JY5412;

class DemoWatchdog
{
    static void Main()
    {
        var doTask = new JY5412DOTask(0);
        var wd     = new JY5412DOWatchdogTask(0);

        int[] lines = Enumerable.Range(0, (int)JY5412Device.NumOfLinesPerPort).ToArray();
        doTask.AddChannel(0, lines);
        wd.AddChannel(0, lines, WatchdogExpirationState.Low);

        doTask.Device.SetLogicLevel(0, LogicLevel.CMOS5_0V);

        // 初始全高
        bool[] init = Enumerable.Repeat(true, lines.Length).ToArray();
        doTask.WriteSinglePoint(init);

        wd.Timeout = 1.0;                // 1 秒未喂狗即锁定为 Low
        if (wd.Expired) { wd.Stop(); wd.ClearExpiration(); }
        wd.Start();

        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(500);
            wd.ResetTimer();             // 正常喂狗
        }

        // 模拟业务卡死：不喂狗 2 秒
        Thread.Sleep(2000);
        System.Console.WriteLine($"Expired = {wd.Expired}");   // 应为 true

        // 恢复
        wd.Stop();
        wd.ClearExpiration();
        doTask.WriteSinglePoint(init);
        wd.Start();
        // ...

        wd.Stop();
        wd.Channels.Clear();
        doTask.Channels.Clear();
        wd.Device.Release();
        doTask.Device.Release();
    }
}
```

### 13.4 示例：设置所有 96 路上电默认状态为 Input + PullUp

```csharp
using JY5412;

var diTask = new JY5412DITask(0);
try
{
    var ups = new JY5412PowerUpStates();
    for (int p = 0; p < JY5412Device.NumberOfPorts; p++)
    {
        ups.Ports[p].Logiclevel = LogicLevel.CMOS5_0V;
        for (int l = 0; l < JY5412Device.NumOfLinesPerPort; l++)
        {
            ups.Channels[p, l].Mode = PowerUpStateMode.Input;
            ups.Channels[p, l].Input.Terminal = DITerminal.PullUp;
        }
    }
    diTask.Device.SetPowerUpStates(ups);

    // 回读验证
    var readBack = new JY5412PowerUpStates();
    diTask.Device.GetPowerUpStates(ref readBack);
}
finally
{
    diTask.Device.Release();
}
```

## 14. 注意事项速记

1. **DITask / DOTask 没有 `Start` / `Stop`**：构造后 `AddChannel` 即可直接 `ReadSinglePoint` / `WriteSinglePoint`；结束时 `Channels.Clear()` + `Device.Release()`。
2. **仅 `JY5412DIChangeDetectionTask` 与 `JY5412DOWatchdogTask` 具有 `Start`/`Stop`** 且**属性必须在 `Start` 前完成配置**，Start 后修改会抛 `TaskHasStartedCannotPerformTheSetOperation`。
3. **通道寻址为 `(portID, lineID)` 二维索引**：Port 0~3、Line 0~23。
4. **`LogicLevel` 按 Port 生效**：同 Port 内所有 24 根线共用 3.3 V 或 5 V；按线设置是非法的。
5. **`SetMinPulseWidth` 必须配合 `AddChannel(..., enableFilter: true)`** 才会启用滤波；单独调用不会生效。
6. **看门狗喂狗节奏** 必须 < `Timeout`，否则 `Expired=true`，需 `Stop` → `ClearExpiration` → `Start` 才能恢复。
7. **`WriteSinglePoint(bool[])` 的数组长度必须等于 `Channels.Count`**，顺序同 `AddChannel`；否则抛 `WritePointInvalid`。
8. **同一线不可被多个 Task 同时占用**（如 DITask 与 DOTask 同时 AddChannel 相同线会抛 `HardwareResourceIsReserved`）；释放顺序：`Channels.Clear()` → `Device.Release()`。
9. **ChangeDetected 事件在驱动回调线程触发**：UI 更新务必 marshaling（`Control.BeginInvoke` 或消息队列 + UI Timer）。
10. **输入保护范围 -3 V ~ +20 V**；**输出最大 I<sub>OH</sub>=10 mA、I<sub>OL</sub>=25 mA**，驱动电感/电容负载需外加保护。
11. **连接器 +5 V 辅助电源** 仅通过引脚 49/99 输出，最大 1 A，总和不超过板卡供电裕量。
