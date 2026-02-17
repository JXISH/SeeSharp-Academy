建立第二个范例"MISDAudioCard.Example.AI-AO"
仿照MISDAudioCard.Example，采用MISDAudioCard定义的MISDAudioCard类调用声卡AI和AO
范例用.NET winform 4.6.2和SeesharpTools.JY.GUI
窗体用numericUpdown控件配置采样率、采样长度
按钮"AI"点击启动一次双通道采集，并将采集得到数据显示在easyChartX控件
按钮"AO"点击，如果已经有AI采集的数据，则用AO方法输出。