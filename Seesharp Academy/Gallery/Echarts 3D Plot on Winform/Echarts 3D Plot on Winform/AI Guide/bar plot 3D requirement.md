Echarts 3D Plot on Winform项目是winform,已经有一个瀑布图，要增加一个3D柱状图演示。

点击 buttonDisplay3DBar 完成以下功能
* 生成模拟中心对称的正态加噪声数据double[,] barData, 2维序号对应X,Y轴坐标，数值代表Z轴高度。模拟设置X,Y序号对应的起始值和步进，如x0, xStep，y0, yStep。
* 在webView23DBar加载threeDBarWeb.html
* 将barData传给threeDBarWeb.html
* threeDBarWeb.html用echarts展示这个3D bar plot，参考范例https://echarts.apache.org/examples/zh/editor.html?c=transparent-bar3d&gl=1
  * 透明度根据numericUpDownTransparency 百分比数值设定
  * 3轴坐标是数值，不要采用参考范例的日期时间体系
* 软件可以不联网运行，下载必要js文件。可参考项目已有waterfall做法