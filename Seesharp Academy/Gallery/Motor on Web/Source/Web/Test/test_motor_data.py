"""
电机数据模拟测试程序
模拟电机噪声信号及其即时频率，发送到Redis数据库
"""

import redis
import time
import math
import numpy as np
from scipy.fft import fft, fftfreq

class MotorDataSimulator:
    def __init__(self, redis_host='localhost', redis_port=6379):
        """初始化Redis连接"""
        self.redis_client = redis.Redis(
            host=redis_host,
            port=redis_port,
            decode_responses=True
        )
        self.time_counter = 0
        
    def generate_motor_signal(self, duration=1.0, sample_rate=10000):
        """
        生成电机噪声信号
        包含主频和谐波成分
        """
        # 基础频率（模拟电机转速）
        base_frequency = 50 + 5 * math.sin(self.time_counter * 0.1)  # 50Hz附近波动
        
        # 生成时间序列
        t = np.linspace(0, duration, int(sample_rate * duration), endpoint=False)
        
        # 生成信号：主频 + 谐波 + 噪声
        signal = (
            1.0 * np.sin(2 * np.pi * base_frequency * t) +  # 主频
            0.5 * np.sin(2 * np.pi * base_frequency * 2 * t) +  # 二次谐波
            0.3 * np.sin(2 * np.pi * base_frequency * 3 * t) +  # 三次谐波
            0.2 * np.sin(2 * np.pi * base_frequency * 4 * t) +  # 四次谐波
            0.1 * np.sin(2 * np.pi * base_frequency * 5 * t)    # 五次谐波
        )
        
        # 添加高斯噪声
        noise = np.random.normal(0, 0.1, len(t))
        signal = signal + noise
        
        return signal, base_frequency
    
    def compute_spectrum(self, signal, sample_rate):
        """计算信号的频谱"""
        n = len(signal)
        yf = fft(signal)
        xf = fftfreq(n, 1 / sample_rate)
        
        # 只取正频率部分
        positive_freq_idx = xf >= 0
        xf = xf[positive_freq_idx]
        yf = 2.0 / n * np.abs(yf[positive_freq_idx])
        
        return xf, yf
    
    def find_peaks(self, frequencies, amplitudes, threshold=0.1):
        """找出频谱中的峰值点（即时频率）"""
        peaks_freq = []
        peaks_amp = []
        
        for i in range(1, len(amplitudes) - 1):
            if (amplitudes[i] > amplitudes[i-1] and 
                amplitudes[i] > amplitudes[i+1] and 
                amplitudes[i] > threshold):
                peaks_freq.append(frequencies[i])
                peaks_amp.append(amplitudes[i])
        
        # 按幅度排序，取前6个主要频率
        if len(peaks_freq) > 0:
            sorted_indices = np.argsort(peaks_amp)[::-1][:6]
            peaks_freq = [peaks_freq[i] for i in sorted_indices]
            peaks_amp = [peaks_amp[i] for i in sorted_indices]
        
        return peaks_freq, peaks_amp
    
    def send_to_redis(self, signal, waveform_dt, spectrum, spectrum_df, 
                     frequencies, amplitudes):
        """发送数据到Redis"""
        try:
            # 时域信号
            waveform_str = ','.join(map(str, signal))
            self.redis_client.set('SeesharpMotor:TimeWaveform', waveform_str)
            self.redis_client.set('SeesharpMotor:Waveform_dT', str(waveform_dt))
            
            # 频谱（降采样以减少数据量）
            spectrum_downsampled = spectrum[::10]  # 每10个点取1个
            spectrum_str = ','.join(map(str, spectrum_downsampled))
            self.redis_client.set('SeesharpMotor:Spectrum', spectrum_str)
            self.redis_client.set('SeesharpMotor:Spectrum_dF', str(spectrum_df * 10))
            
            # 即时频率和幅度
            freq_str = ','.join(map(str, frequencies))
            amp_str = ','.join(map(str, amplitudes))
            self.redis_client.set('SeesharpMotor:Frequencies', freq_str)
            self.redis_client.set('SeesharpMotor:Amplitudes', amp_str)
            
            print(f"数据已发送到Redis | 主频: {frequencies[0] if frequencies else 0:.2f}Hz | "
                  f"峰值数: {len(frequencies)}")
            
        except Exception as e:
            print(f"发送数据到Redis失败: {e}")
    
    def run(self, duration=1.0, sample_rate=10000, update_interval=0.5):
        """运行模拟器"""
        print(f"电机数据模拟器启动")
        print(f"采样率: {sample_rate}Hz | 更新间隔: {update_interval}s")
        print(f"按Ctrl+C停止...")
        
        try:
            while True:
                self.time_counter += 1
                
                # 生成信号
                signal, base_freq = self.generate_motor_signal(duration, sample_rate)
                waveform_dt = 1.0 / sample_rate
                
                # 计算频谱
                frequencies, spectrum = self.compute_spectrum(signal, sample_rate)
                spectrum_df = frequencies[1] - frequencies[0] if len(frequencies) > 1 else 1.0
                
                # 找出主要频率成分
                peak_freqs, peak_amps = self.find_peaks(frequencies.tolist(), 
                                                        spectrum.tolist())
                
                # 发送到Redis
                self.send_to_redis(
                    signal.tolist(),
                    waveform_dt,
                    spectrum.tolist(),
                    spectrum_df,
                    peak_freqs,
                    peak_amps
                )
                
                # 等待下一次更新
                time.sleep(update_interval)
                
        except KeyboardInterrupt:
            print("\n模拟器已停止")
        except Exception as e:
            print(f"运行错误: {e}")


def main():
    """主函数"""
    print("="*50)
    print("锐视公学电机状态监测 - 数据模拟器")
    print("="*50)
    
    # 检查Redis连接
    try:
        simulator = MotorDataSimulator()
        simulator.redis_client.ping()
        print("✓ 成功连接到Redis服务器")
    except Exception as e:
        print(f"✗ 无法连接到Redis服务器: {e}")
        print("请确保Redis服务正在运行")
        return
    
    # 运行模拟器
    simulator.run(
        duration=1.0,        # 信号持续时间（秒）
        sample_rate=10000,   # 采样率（Hz）
        update_interval=0.5  # 更新间隔（秒）
    )


if __name__ == "__main__":
    main()
