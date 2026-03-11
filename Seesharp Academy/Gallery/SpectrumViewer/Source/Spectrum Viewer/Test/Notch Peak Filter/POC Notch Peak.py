import numpy as np
from scipy.signal import freqz, lfilter
import matplotlib.pyplot as plt

# --------------------------
# 1. 定义滤波器设计函数
# --------------------------
def design_notch_peak_filter(f0, fs, Q, gain_dB=0):
    """
    Design second-order notch/bandpass filters
    :param f0: Center frequency (Hz)
    :param fs: Sampling rate (Hz)
    :param Q: Quality factor (higher = narrower bandwidth)
    :param gain_dB: Peak gain (dB), 0=notch, non-zero=bandpass
    :return: Filter coefficients b, a
    """
    # Step 1: Normalize parameters
    omega0 = 2 * np.pi * f0 / fs  # Normalized angular frequency
    alpha = np.sin(omega0 / 2) / (2 * Q)  # Bandwidth coefficient
    
    # Step 2: Calculate coefficients
    if gain_dB == 0:
        # Notch filter (band-stop) - removes specific frequency
        b0 = 1
        b1 = -2 * np.cos(omega0)
        b2 = 1
        a1 = -2 * np.cos(omega0) * (1 - alpha)
        a2 = 1 - 2 * alpha
        a0 = 1 + alpha
        b = np.array([b0, b1, b2]) / a0
        a = np.array([1, a1, a2]) / a0
    else:
        # Bandpass filter (narrow band-pass) - passes only specific frequency
        # Use resonator form for accurate peak frequency placement
        # This form ensures the peak is exactly at the target frequency
        b0 = 1 - alpha/2
        b1 = 0
        b2 = -(1 - alpha/2)
        a0 = 1
        a1 = -2 * np.cos(omega0) * (1 - alpha/2)
        a2 = 1 - alpha
        
        # Calculate gain at center frequency and normalize to 0dB
        w_center = np.array([omega0])
        _, H_center = freqz(np.array([b0, b1, b2]), np.array([a0, a1, a2]), worN=w_center)
        gain = np.abs(H_center[0])
        
        # Normalize coefficients to achieve exactly 0dB at center frequency
        b = np.array([b0, b1, b2]) / gain
        a = np.array([a0, a1, a2])
    
    return b, a

# --------------------------
# 2. Design Examples (Remove 50Hz power line interference + Extract 100Hz signal)
# --------------------------
fs = 1000  # Sampling rate 1kHz
f0_notch = 50  # Notch frequency 50Hz
f0_bandpass = 100  # Bandpass frequency 100Hz
Q = 10  # Quality factor (narrow bandwidth, precise filtering)

# Design filters
b_notch, a_notch = design_notch_peak_filter(f0_notch, fs, Q, gain_dB=0)
b_bandpass, a_bandpass = design_notch_peak_filter(f0_bandpass, fs, Q, gain_dB=10)  # 10dB gain

# --------------------------
# 3. Verify Frequency Response
# --------------------------
# Calculate frequency response
w_notch, H_notch = freqz(b_notch, a_notch, worN=2048)
w_bandpass, H_bandpass = freqz(b_bandpass, a_bandpass, worN=2048)

# Convert to frequency axis
freq_notch = w_notch * fs / (2 * np.pi)
freq_bandpass = w_bandpass * fs / (2 * np.pi)

# Magnitude response (dB)
epsilon = 1e-10  # Small value to avoid log10(0)
mag_notch = 20 * np.log10(np.abs(H_notch) + epsilon)
mag_bandpass = 20 * np.log10(np.abs(H_bandpass) + epsilon)

# Check gain at center frequency
omega0_test = 2 * np.pi * f0_bandpass / fs
w_center, H_center = freqz(b_bandpass, a_bandpass, worN=[omega0_test])
gain_at_center = 20 * np.log10(np.abs(H_center[0]))
print(f"Bandpass filter gain at {f0_bandpass}Hz: {gain_at_center:.4f} dB")

# Find actual peak frequency
peak_idx = np.argmax(mag_bandpass)
peak_freq = freq_bandpass[peak_idx]
peak_gain = mag_bandpass[peak_idx]
print(f"Actual peak frequency: {peak_freq:.2f} Hz with gain: {peak_gain:.4f} dB")

# --------------------------
# 4. 绘图展示
# --------------------------
plt.figure(figsize=(12, 8))

# Notch filter response
plt.subplot(2, 1, 1)
plt.plot(freq_notch, mag_notch, label='Notch Filter (50Hz)')
plt.axvline(x=f0_notch, color='r', linestyle='--', label=f'Target Frequency {f0_notch}Hz')
plt.title('Notch Filter Frequency Response (Remove 50Hz Power Line Interference)')
plt.ylabel('Magnitude (dB)')
plt.xlim(0, fs/2)
plt.ylim(-40, 5)
plt.grid(True)
plt.legend()

# Bandpass filter response
plt.subplot(2, 1, 2)
plt.plot(freq_bandpass, mag_bandpass, label='Bandpass Filter (100Hz)')
plt.axvline(x=f0_bandpass, color='r', linestyle='--', label=f'Target Frequency {f0_bandpass}Hz')
plt.title('Bandpass Filter Frequency Response (Extract 100Hz Signal)')
plt.xlabel('Frequency (Hz)')
plt.ylabel('Magnitude (dB)')
plt.xlim(0, fs/2)
plt.ylim(-60, 5)
plt.grid(True)
plt.legend()

plt.tight_layout()
plt.show()

# --------------------------
# 5. Signal Filtering Example
# --------------------------
# Generate test signal: 50Hz (interference) + 100Hz (useful signal) + noise
t = np.linspace(0, 1, fs, endpoint=False)
signal = np.sin(2*np.pi*50*t) + np.sin(2*np.pi*100*t) + 0.1*np.random.randn(len(t))

# First remove 50Hz interference, then extract 100Hz signal
signal_filtered = lfilter(b_notch, a_notch, signal)
signal_extracted = lfilter(b_bandpass, a_bandpass, signal_filtered)

# 绘制滤波前后的信号
plt.figure(figsize=(12, 6))
plt.subplot(3, 1, 1)
plt.plot(t, signal)
plt.title('Original Signal (50Hz Interference + 100Hz Signal + Noise)')
plt.ylabel('Magnitude')

plt.subplot(3, 1, 2)
plt.plot(t, signal_filtered)
plt.title('After Notch Filtering (50Hz Interference Removed)')
plt.ylabel('Magnitude')

plt.subplot(3, 1, 3)
plt.plot(t, signal_extracted)
plt.title('After Bandpass Filtering (100Hz Signal Extracted)')
plt.xlabel('Time (s)')
plt.ylabel('Magnitude')

plt.tight_layout()
plt.show()