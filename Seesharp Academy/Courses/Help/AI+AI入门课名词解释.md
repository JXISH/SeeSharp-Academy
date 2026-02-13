# AI+AI入门课

# Visual Studio C# 软件编程常用名词对照表

| 中文             | 英语                                     | 极简解释                                                     |
| ---------------- | ---------------------------------------- | ------------------------------------------------------------ |
| 集成开发环境     | Integrated Development Environment (IDE) | 编写、编译、调试代码的统一工具（如 Visual Studio）           |
| 解决方案         | Solution                                 | 管理多个项目的容器，包含项目依赖和配置                       |
| 项目             | Project                                  | 具体功能模块的代码集合，有独立的配置文件（如.csproj）        |
| 引用             | Reference                                | 项目依赖的外部类库（如 DLL、NuGet 包），需手动添加以使用其功能 |
| 命名空间         | Namespace                                | 管理类的 “文件夹”，避免类名冲突（如 System.Windows.Forms）   |
| 类               | Class                                    | C# 中封装数据和方法的基本单元，是面向对象编程的核心          |
| 方法             | Method                                   | 类中实现特定功能的代码块（如 AddChannel ()、ReadData ()）    |
| 窗体             | Form                                     | Windows Forms 应用的可视化界面容器，承载控件（如按钮、图表） |
| 控件             | Control                                  | 窗体上的交互元素（如 Button 按钮、EasyChartX 图表控件）      |
| 调试             | Debug                                    | 开发阶段的代码测试模式，可设置断点、查看变量值以定位错误     |
| 启动新实例       | Start New Instance                       | 右键项目触发，启动当前项目的新运行进程（支持多窗口运行）     |
| 生成             | Build                                    | 编译项目代码，生成可执行文件或程序集（如.exe、DLL）          |
| 发布             | Release                                  | 项目最终交付的编译模式，优化代码、移除调试信息               |
| 动态链接库       | Dynamic Link Library (DLL)               | 可被多个程序共享的代码库（如 SeeSharpTools.JY.GUI.dll）      |
| 程序集           | Assembly                                 | 编译后的代码单元，包含 DLL 或 EXE，是.NET 程序的部署单位     |
| 任务             | Task                                     | 封装异步操作的对象，常用于数据采集（如 JY5320AITask 采集任务） |
| 事件             | Event                                    | 控件或对象触发的动作（如 Button 的 Click 点击事件）          |
| 属性             | Property                                 | 类或控件的特征（如 Form 的 Text 标题、EasyChartX 的 DataStorage 数据缓存属性） |
| 配置文件         | Configuration File                       | 存储项目配置的文件（如 app.config、packages.config）         |
| NuGet 包         | NuGet Package                            | 可复用的代码库（如 SeeSharpTools.JY.GUI），通过 NuGet 管理器安装 / 更新 |
| .NET 框架        | .NET Framework                           | 微软提供的开发框架，提供类库和运行环境（如项目依赖的 net462） |
| 公共语言运行时   | Common Language Runtime (CLR)            | .NET 框架的核心，负责代码执行、内存管理和安全检查            |
| 数据采集         | Data Acquisition (DAQ)                   | 从硬件（如 USB-1601 采集卡）获取数据的过程                   |
| 有限点采集       | Finite Point Acquisition                 | 数据采集的一种模式，采集指定数量的样本后停止（如采集 10k 个采样点） |
| 频谱分析         | Spectrum Analysis                        | 对采集的时域信号进行 FFT 转换，分析频率分布（需 SeeSharpTools 等工具包支持） |
| 序列化           | Serialize                                | 将对象转换为字节流，用于数据存储或传输                       |
| 反序列化         | Deserialize                              | 将字节流恢复为对象，还原序列化前的数据结构                   |
| 字符串           | String                                   | 文本数据类型，用于存储字符序列（如文件路径、提示信息）       |
| 整数类型         | Integer (int)                            | 存储整数的数据类型（如采样率、通道号）                       |
| 双精度浮点类型   | Double                                   | 存储高精度小数的数据类型（如采集的电压值、频率值）           |
| 文件流           | File Stream                              | 读写文件的字节流对象，用于数据存储（如将频谱数据写入 CSV 文件） |
| 对话框           | Dialog                                   | 与用户交互的弹窗（如 SaveFileDialog 选择文件保存路径）       |
| 命名空间引用指令 | Using Directive                          | 简化命名空间使用的指令（如 using System.Windows.Forms;）     |
| 静态的           | Static                                   | 修饰类 / 方法，无需实例化即可调用（如 Math.PI、File.WriteAllLines ()） |
| 空值             | Null                                     | 表示对象未引用任何实例，需避免空引用异常                     |
| 布尔类型         | Boolean (bool)                           | 只有 true（真）和 false（假）两种值，用于条件判断            |
| 数组             | Array                                    | 存储同类型数据的集合（如采集的时域数据数组、频谱数据数组）   |
| 集合             | Collection                               | 动态存储数据的容器（如 List<T>，支持添加 / 删除元素）        |
| 断点             | Breakpoint                               | 调试时设置的代码暂停点，用于查看程序运行状态                 |
| 异常             | Exception                                | 程序运行时的错误（如硬件连接失败、文件不存在），需 try-catch 捕获处理 |



* 以上表格主要由豆包AI生成