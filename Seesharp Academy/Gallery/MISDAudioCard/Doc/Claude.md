参考C:\SeeSharp\JYTEK\Hardware\DAQ\JYUSB1601\Bin JYUSB1601  C#驱动，和
C:\SeeSharp\JYTEK\Hardware\DAQ\JY1601\JYUSB-1601.Examples JYUSB1601范例，
在当前打开目录 Library创建C#解决方案，用通用的计算机声卡底层驱动，封装命名空间为MISDAudioCard的驱动。驱动提供
1. AITask，支持有限长采集
2. AOTask，支持有限长输出，和连续输出
3. AITask和AOTask两者都支持添加1个或2个通道，都支持采样率属性配置
4. 参考1601范例，构建一个MISDAudioCard的Winform范例，一个Generate按钮生成一个正弦波，在EasyChartX显示；一个Start按钮，将这个波形从配置的一个或者两个通道输出，同时从选中的1个或2个通道采集，将采集到的波形显示在另一个EasyChartX。
5. 两个EasyChartX上下排列。
6. 使用.NET Framework 4.6.2