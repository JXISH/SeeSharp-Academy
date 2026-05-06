# WebView2Plots 类库使用说明

## 简介

WebView2Plots 是一个基于 WebView2 + ECharts GL 的 WinForms 3D 绘图类库，提供开箱即用的 3D 曲面图（Waterfall）和 3D 柱状图（Bar3D）渲染能力。无需联网，所有 JS 资源已本地化。

## 类库内容

| 文件 | 说明 |
|------|------|
| `WebView2Plots.dll` | 编译后的类库 DLL |
| `waterfallWeb.html` | 3D 曲面图 HTML 模板 |
| `threeDBarWeb.html` | 3D 柱状图 HTML 模板 |
| `echarts.min.js` | ECharts 核心库 (v5.5.1) |
| `echarts-gl.min.js` | ECharts GL 扩展库 (v2.0.9) |

## 导入步骤

### 方式一：项目引用（同一解决方案内）

1. 在 Visual Studio 中，右键解决方案 → **添加** → **现有项目** → 选择 `WebView2Plots\WebView2Plots.csproj`
2. 在你的 WinForms 项目上右键 → **添加** → **项目引用** → 勾选 `WebView2Plots`
3. 你的项目也需要安装 NuGet 包 `Microsoft.Web.WebView2`（版本 1.0.3912.50 或兼容版本）
4. 将以下 4 个辅助文件添加到你的项目中（可用链接方式），并设置 **复制到输出目录 = 如果较新则复制**：
   - `waterfallWeb.html`
   - `threeDBarWeb.html`
   - `echarts.min.js`
   - `echarts-gl.min.js`

   在 `.csproj` 中添加链接文件的写法：
   ```xml
   <Content Include="..\WebView2Plots\waterfallWeb.html">
     <Link>waterfallWeb.html</Link>
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </Content>
   <Content Include="..\WebView2Plots\threeDBarWeb.html">
     <Link>threeDBarWeb.html</Link>
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </Content>
   <Content Include="..\WebView2Plots\echarts.min.js">
     <Link>echarts.min.js</Link>
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </Content>
   <Content Include="..\WebView2Plots\echarts-gl.min.js">
     <Link>echarts-gl.min.js</Link>
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </Content>
   ```

### 方式二：DLL 引用（独立项目）

1. 编译 WebView2Plots 项目，从 `WebView2Plots\bin\Debug\`（或 Release）获取：
   - `WebView2Plots.dll`
2. 在你的项目中 **添加引用** → **浏览** → 选择 `WebView2Plots.dll`
3. 你的项目需要安装 NuGet 包 `Microsoft.Web.WebView2`
4. 将以下 4 个辅助文件复制到你的项目输出目录（exe 同级目录）：
   - `waterfallWeb.html`
   - `threeDBarWeb.html`
   - `echarts.min.js`
   - `echarts-gl.min.js`

## 使用示例

### 前置条件
- WinForms 窗体上放置 `WebView2` 控件
- 代码文件顶部添加：
```csharp
using WebView2Plots;
```

### 3D 曲面图（Waterfall）
```csharp
double[,] data = new double[40, 40]; // 填充你的数据
double x0 = 0, xStep = 0.5;
double y0 = 0, yStep = 0.5;

await WebView2Waterfall.RenderAsync(
    webView2Control,    // WebView2 控件
    data,               // double[,] 二维数组，值为 Z 轴高度
    x0, xStep,          // X 轴起始值和步进
    y0, yStep,          // Y 轴起始值和步进
    "频率(Hz)",          // X 轴名称（可选，默认 "X"）
    "时间(s)",           // Y 轴名称（可选，默认 "Y"）
    "幅值(dB)"           // Z 轴名称（可选，默认 "Z"）
);
```

### 3D 柱状图（Bar3D）
```csharp
double[,] data = new double[20, 20]; // 填充你的数据
double x0 = -5, xStep = 0.5;
double y0 = -5, yStep = 0.5;
int transparency = 30; // 透明度百分比：0=不透明，100=全透明

await WebView2Bar3D.RenderAsync(
    webView2Control,    // WebView2 控件
    data,               // double[,] 二维数组，值为 Z 轴高度
    x0, xStep,          // X 轴起始值和步进
    y0, yStep,          // Y 轴起始值和步进
    "X",                // X 轴名称（可选）
    "Y",                // Y 轴名称（可选）
    "Z",                // Z 轴名称（可选）
    transparency        // 透明度百分比（可选，默认 50）
);
```

## 注意事项

- 按钮点击事件处理方法必须声明为 `async void`，以支持 `await` 调用
- 首次调用会自动加载 HTML 页面，后续调用直接更新数据，无需重复加载
- 4 个辅助文件（2 个 HTML + 2 个 JS）必须位于 exe 同级目录，否则运行时会弹出文件未找到提示
- 本类库完全离线运行，不依赖网络
