MotorOnWebForm是一个.NET Winform 4.8框架上的数据采集软件。点击start按钮开始，点击stop按钮结束。
start以后启动采集和timerMain，在 timerMain 的处理里边实现定时功能的逻辑。
启动采集
* 采集模块为JY USB-1202模块
* textBoxDeviceName、numericUpDownChannelIndex、numericUpDownSampleRate、numericUpDownBlockLength规定了采集的参数
* 1202方法和属性参考C:\SeeSharp\JYTEK\Hardware\DSA\JYUSB1202\Bin\JYUSB1202.xml
* 数据采集编程步骤参考1601范例C:\SeeSharp\JYTEK\Hardware\DAQ\JY1601\JYUSB-1601.Examples\Analog Input\Winform AI Finite做有限长采样
定时逻辑中
* numericUpDownMonitorInterval确定了每隔多少秒采集一次数据分析并上传
* 每次定时采集，单独做一次有限长采样。采样结果显示在easyChartXWaveform；采样信号做频谱分析显示在easyChartXSpectrum。显示方法是 easyChartX.Plot(x, 0, dt),其中0是时间起点常数，dt是时间间隔。
* 频谱用SeeSharpTools.JY.DSP.Fundamental，参考C:\SeeSharp\JYTEK\SeeSharpExamples\Signal Processing and Analysis\Spectrum Calculation Example范例完成。输出汉宁窗 dBV为单位的频谱。
结果上报
* 每次分析完，把波形和平谱的数据上报到Redis数据库
* Redis服务由外部软件启动
* Redis变量：  
  * 时域信号： "SeesharpMotor:TimeWaveform": 逗号分割的浮点数
  * 时域信号采样间隔时间(s)："SeesharpMotor:Waveform_dT": 浮点数
  * 信号频谱："SeesharpMotor:Spectrum": 逗号分割的浮点数
  * 频谱采样间隔频率(Hz)："SeesharpMotor:Spectrum_dF": 浮点数
  * 即时频率："SeesharpMotor:Frequencies": 逗号分割的浮点数
  * 即时频率幅度："SeesharpMotor:Amplitudes": 逗号分割的浮点数,与即时频率一一对应，需容忍变量刷新竞争带来的瞬时不匹配
终止采集
* 停止现有的采集，并且停止timerMain
* 使能start按钮以及数据采集的配置