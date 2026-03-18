MotorOnWeb 项目用USB1202采集，现在我要加一个硬件选项，USB1601。
我添加了comboBox选项，并设定选择集合包括两种硬件。还添加了对1601的dll引用。
请参考D:\Git\Sandbox\AI plus AI\Lab28 Motor on Web add 1601\cs\Motor on Web\External\JYUSB1601.xml 取得1601可能的方法和属性，参考C:\SeeSharp\JYTEK\Hardware\DAQ\JY1601\JYUSB-1601.Examples\Analog Input\Winform AI Finite\Winform AI Finite项目的编程流程。
修改MotorOnWeb 项目，对所有AI task都按照comboBox选项，增加对1601的支持。