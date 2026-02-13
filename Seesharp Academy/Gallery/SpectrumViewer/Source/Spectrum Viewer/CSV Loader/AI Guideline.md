在本目录建立.NET framework 4.6.2的项目
这个Form将被另一个主窗体作为对话框调用。
打开窗体同时弹窗选择一个*.csv文件
将文件前5行显示在Form
分析每行有几列，如果有2列或更多，则合理
对合理文件第一行的第二列开始的各文字，认为是数据系列名称
在Form提供数据系列选项，用户可以选择需要导入的系列
第一列是数据采集时间，将第一列第二行到最后一行的时间差除以间隔行数就是采样间隔。采样间隔转换成采样率numericupdown控件显示在Form
Form上有个Load按钮，点击后将选择的系列数据全部读取到公共属性double[] LoadedSeries. 采样率显示控件的值也赋给公共属性double SampleRate

### AI Summary
CSV Loader项目已成功创建并完成所有功能实现及改进！

**项目信息：**
- 平台：.NET Framework 4.6.2
- 类型：Windows Forms应用程序
- 编译状态：成功（0警告，0错误）

**已实现的核心功能（根据AI Guideline.md要求）：**

1. ✅ **文件选择** - 打开窗体时自动弹出OpenFileDialog选择*.csv文件
2. ✅ **数据预览** - 在DataGridView中显示CSV文件前5行，**第一行作为表格列标题**（已改进）
3. ✅ **格式验证** - 自动验证CSV文件至少有2列数据
4. ✅ **系列识别** - 从第一行第二列开始识别数据系列名称
5. ✅ **系列选择** - 使用**ListBox实现单选**模式，让用户选择需要导入的数据系列（已改进）
6. ✅ **采样率计算** - 基于第一列时间数据自动计算采样率并显示在NumericUpDown控件
7. ✅ **数据加载** - Load按钮将选中的**单个系列**数据读取到`LoadedSeries`属性
8. ✅ **采样率设置** - 采样率值赋给`SampleRate`公共属性

**根据用户反馈实现的改进：**
1. ✅ CSV第一行数据作为DataGridView列标题显示
2. ✅ 系列选择改为单选模式（ListBox替代CheckedListBox）
3. ✅ 默认自动选中第一个系列
4. ✅ Load按钮只加载单个选中的系列

**公共属性：**
- `double[] LoadedSeries` - 存储加载的单个系列数据
- `double SampleRate` - 存储采样率

**可执行文件位置：**
`bin\Debug\CSV Loader.exe`

项目可以作为对话框被主窗体调用，返回OK/Cancel结果。所有功能已按照AI Guideline.md要求完成实现。