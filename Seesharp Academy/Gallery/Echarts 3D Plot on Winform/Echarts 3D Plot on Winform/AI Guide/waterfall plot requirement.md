Echarts 3D Plot on Winform项目是winform嵌入echarts 瀑布图的程序

点击buttonDisplay实现以下功能
* C#中生成演示double[,] waterfallData，2维序号对应X,Y轴坐标，数值代表Z轴高度。模拟设置X,Y序号对应的起始值和步进，如x0, xStep，y0, yStep。
* 在webView2Waterfall加载waterfallWeb.html
* 将waterfallData传给waterfallWeb.html
* waterfallWeb.html用echarts展示这个曲面，参考范例https://echarts.apache.org/examples/zh/editor.html?c=simple-surface&gl=1

创建必要文件和程序。