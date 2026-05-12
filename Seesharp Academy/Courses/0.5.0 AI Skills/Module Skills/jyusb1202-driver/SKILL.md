---
name: jyusb1202-ai
description: 使用 JYUSB1202 驱动库 (JYUSB1202.dll) 编写 C# 模拟输入 (AI) 采集程序。支持单/多通道、连续/有限采集、立即/软件/数字触发、多种量程与耦合配置。当用户提到 JYUSB1202、JYTEK 数据采集卡、模拟输入采集、AITask、连续采集、有限采集、软触发、数字触发时自动应用此技能。
---

# JYUSB-1202 模拟输入 (AI) 技能

## 快速上手

## 环境要求

- **驱动 DLL**：`C:\SeeSharp\JYTEK\Hardware\DSA\JYUSB1202\Bin\JYUSB1202.dll`（引用到 .csproj）
- **目标框架**：.NET Framework 4.0 或更高版本
- **命名空间**：`using JYUSB1202;`
- **设备名称**：在 JYTEK 设备管理器中设置的板卡别名（Board Name），如 `"Dev1"`

### 最小流程（单通道连续采集）

```csharp
using JYUSB1202;

// 1. 创建任务（设备别名或插槽号）
var aiTask = new JYUSB1202AITask("DevName");   // 也可传 int slotNumber

// 2. 添加通道
aiTask.AddChannel(0, -10.0, 10.0, AITerminal.Differential, AICoupling.DC, false);

// 3. 配置采集参数
aiTask.Mode = AIMode.Continuous;
aiTask.SampleRate = 10000;        // Sa/s，单通道最大 250kSa/s
aiTask.SamplesToAcquire = 1000;  // 每次读取点数（Continuous 模式可选）

// 4. 开启校准（推荐）
JYUSB1202Miscellaneous.SetAICalibrationState(aiTask.Device, true);

// 5. 启动
aiTask.Start();

// 6. 读数据（单通道）
double[] buf = new double[1000];
aiTask.ReadData(ref buf, buf.Length, -1);   // timeout=-1 表示一直等待

// 7. 停止并清理
aiTask.Stop();
aiTask.Channels.Clear();
```

---

## 采集模式

| 模式 | `AIMode` 枚举值 | 特点 |
|------|---------------|------|
| 连续 | `AIMode.Continuous` | 循环采集，需用 Timer 轮询 |
| 有限 | `AIMode.Finite` | 采集指定点数后自动完成 |
| 录制 | `AIMode.Record` | 流式写入磁盘文件 |

---

## 通道配置

```csharp
// 单通道
aiTask.AddChannel(chnId, rangeLow, rangeHigh, terminal, coupling, enableIEPE);

// 多通道（相同参数）
aiTask.AddChannel(new int[]{0,1,2,3}, -10.0, 10.0,
    AITerminal.Differential, AICoupling.DC, false);

// 多通道（各自参数）
aiTask.AddChannel(
    new int[]{0,1},
    new double[]{-10,-5}, new double[]{10,5},
    new AITerminal[]{AITerminal.Differential, AITerminal.PSEUDIFF},
    new AICoupling[]{AICoupling.DC, AICoupling.AC},
    new bool[]{false, false});
```

**量程选项** (rangeLow / rangeHigh)：±12V、±5V、±1.25V、±0.32V

**终端模式** (`AITerminal`)：`Differential`（差分，默认）、`PSEUDIFF`（伪差分）

**耦合模式** (`AICoupling`)：`DC`（直流，默认）、`AC`（交流）

**多通道采样率**：单通道最大 250kSa/s；N 通道时每通道最大 250k/N Sa/s

---

## 触发配置

```csharp
// 立即触发（默认，无需配置）
aiTask.Trigger.Type = AITriggerType.Immediate;

// 软件触发
aiTask.Trigger.Type = AITriggerType.Soft;
aiTask.Start();
// ... 等待条件 ...
aiTask.SendSoftwareTrigger();   // 发送软触发

// 数字触发
aiTask.Trigger.Type = AITriggerType.Digital;
aiTask.Trigger.Digital.Source = AIDigitalTriggerSource.DIO_0;  // DIO_0~DIO_3
aiTask.Trigger.Digital.Edge   = AIDigitalTriggerEdge.Rising;   // Rising/Falling/HighLevel/LowLevel
```

---

## 读取数据

| 场景 | 方法签名 | 备注 |
|------|---------|------|
| 单通道 | `ReadData(ref double[] buf, int samples, int timeout)` | |
| 多通道 | `ReadData(ref double[,] buf, int samples, int timeout)` | 列优先，每列一个通道 |
| 轮询 | 检查 `aiTask.AvailableSamples >= (ulong)读取点数` 后再调用 | |
| 有限模式等待 | `aiTask.WaitUntilDone(-1)` | -1 = 无限等待 |

```csharp
// 多通道读取示例
double[,] buf = new double[samples, channelCount]; // [采样点, 通道数]
if (aiTask.AvailableSamples >= (ulong)samples)
    aiTask.ReadData(ref buf, samples, -1);
```

---

## 连续采集 Timer 模式（推荐模板）

```csharp
// 启动时
timer.Interval = 10;   // 10ms 轮询
timer.Start();

// Timer Tick 回调
void OnTick(object s, EventArgs e)
{
    timer.Stop();
    if (aiTask.AvailableSamples >= (ulong)buf.Length)
    {
        aiTask.ReadData(ref buf, buf.Length, -1);
        // 更新 UI / 图表
    }
    timer.Start();
}
```

---

## 错误处理

```csharp
try
{
    aiTask.Start();
}
catch (JYDriverException ex)
{
    MessageBox.Show(ex.Message);   // ex.ErrorCode 可获取具体错误码
}
```

常见异常：`OpenDeviceFailed`、`StartTaskFailed`、`ReadDataTimeout`、`BufferDataOverflow`

---

## 窗体关闭时的清理

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    base.OnFormClosing(e);
    try { aiTask?.Stop(); } catch { }
}
```

---

## 工程设置

- 目标平台：**x86**（JYUSB1202.dll 为 32 位）
- 目标框架：.NET 4.0 或以上
- 引用程序集：`JYUSB1202.dll`（驱动）、`SeeSharpTools.JY.GUI.dll`（图表控件，可选）

---

## 详细参考

- 完整 API 说明：[ai-reference.md](ai-reference.md)
- 完整代码示例（连续、有限、触发、多通道）：[ai-examples.md](ai-examples.md)
