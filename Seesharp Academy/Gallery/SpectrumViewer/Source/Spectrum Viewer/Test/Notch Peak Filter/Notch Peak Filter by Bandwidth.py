import numpy as np
from scipy.signal import freqz, lfilter, iirnotch, iirpeak
import matplotlib.pyplot as plt

# --------------------------
# 1. Define Filter Design Functions using Bandwidth
# --------------------------

def design_notch_filter_by_bandwidth(f0, bandwidth, fs, attenuation_db=40):
    """
    Design notch filter using bandwidth specification
    Uses scipy's iirnotch which uses Q = f0 / BW for -3dB bandwidth
    """
    # For scipy's iirnotch: Q = f0 / BW
    Q = f0 / bandwidth
    wo = f0 / (fs / 2)  # Normalized frequency
    
    # Use scipy's iirnotch function
    b, a = iirnotch(wo, Q)
    
    return b, a, Q


def design_bandpass_filter_by_bandwidth(f0, bandwidth, fs, attenuation_db=3):
    """
    Design bandpass filter using bandwidth specification
    Uses scipy's iirpeak which uses Q = f0 / BW for -3dB bandwidth
    """
    # For scipy's iirpeak: Q = f0 / BW
    Q = f0 / bandwidth
    wo = f0 / (fs / 2)  # Normalized frequency
    
    # Use scipy's iirpeak function
    b, a = iirpeak(wo, Q)
    
    return b, a, Q


def verify_bandwidth(b, a, f0, target_bandwidth, fs, filter_type='notch'):
    """Verify actual -3dB bandwidth"""
    w, H = freqz(b, a, worN=16384)
    freq = w * fs / (2 * np.pi)
    mag_db = 20 * np.log10(np.abs(H) + 1e-10)
    
    if filter_type == 'notch':
        # For notch filter: find -3dB points
        # The notch has minimum at center, we look for -3dB drop from passband
        target_level = -3
        
        # Find left -3dB point (where gain drops below -3dB going toward center)
        left_candidates = freq[(freq < f0) & (mag_db <= target_level)]
        right_candidates = freq[(freq > f0) & (mag_db <= target_level)]
        
        if len(left_candidates) > 0 and len(right_candidates) > 0:
            # Get the closest -3dB points to center
            left_point = left_candidates[-1]  # Last point below -3dB on left
            right_point = right_candidates[0]  # First point below -3dB on right
            actual_bandwidth = right_point - left_point
        else:
            actual_bandwidth = 0
    else:  # bandpass
        # For bandpass: -3dB points are half-power points
        target_level = -3
        left_idx = np.where((freq < f0) & (mag_db <= target_level))[0]
        right_idx = np.where((freq > f0) & (mag_db <= target_level))[0]
        
        if len(left_idx) > 0 and len(right_idx) > 0:
            actual_bandwidth = freq[right_idx[0]] - freq[left_idx[-1]]
        else:
            actual_bandwidth = 0
    
    return actual_bandwidth


# --------------------------
# 2. Design Examples
# --------------------------

fs = 1000  # Sampling rate 1kHz

# Notch filter: Remove 50Hz with 10Hz bandwidth
f0_notch = 50
bandwidth_notch = 10

# Bandpass filter: Extract 100Hz with 20Hz bandwidth
f0_bandpass = 100
bandwidth_bandpass = 20

# Design filters
print("=" * 70)
print("Notch Filter Design (using bandwidth specification)")
print("=" * 70)
b_notch, a_notch, Q_notch = design_notch_filter_by_bandwidth(
    f0_notch, bandwidth_notch, fs
)
print(f"Center frequency: {f0_notch} Hz")
print(f"Target -3dB bandwidth: {bandwidth_notch} Hz")
print(f"Calculated Q: {Q_notch:.2f} (Q = f0 / BW = {f0_notch}/{bandwidth_notch})")

# Verify notch filter bandwidth
actual_bw_notch = verify_bandwidth(b_notch, a_notch, f0_notch, bandwidth_notch, fs, 'notch')
print(f"Actual -3dB bandwidth: {actual_bw_notch:.2f} Hz")

# Check attenuation at center
w_center, H_center = freqz(b_notch, a_notch, worN=[2*np.pi*f0_notch/fs])
attenuation_at_center = 20 * np.log10(np.abs(H_center[0]))
print(f"Attenuation at center: {attenuation_at_center:.2f} dB")

print("\n" + "=" * 70)
print("Bandpass Filter Design (using bandwidth specification)")
print("=" * 70)
b_bandpass, a_bandpass, Q_bandpass = design_bandpass_filter_by_bandwidth(
    f0_bandpass, bandwidth_bandpass, fs
)
print(f"Center frequency: {f0_bandpass} Hz")
print(f"Target -3dB bandwidth: {bandwidth_bandpass} Hz")
print(f"Calculated Q: {Q_bandpass:.2f} (Q = f0 / BW = {f0_bandpass}/{bandwidth_bandpass})")

# Verify bandpass filter bandwidth
actual_bw_bandpass = verify_bandwidth(b_bandpass, a_bandpass, f0_bandpass, bandwidth_bandpass, fs, 'bandpass')
print(f"Actual -3dB bandwidth: {actual_bw_bandpass:.2f} Hz")

# Check gain at center
w_center_bp, H_center_bp = freqz(b_bandpass, a_bandpass, worN=[2*np.pi*f0_bandpass/fs])
gain_at_center = 20 * np.log10(np.abs(H_center_bp[0]))
print(f"Gain at center: {gain_at_center:.4f} dB")

# --------------------------
# 3. Plot Frequency Responses
# --------------------------

# Calculate frequency responses
w_notch, H_notch = freqz(b_notch, a_notch, worN=4096)
w_bandpass, H_bandpass = freqz(b_bandpass, a_bandpass, worN=4096)

freq_notch = w_notch * fs / (2 * np.pi)
freq_bandpass = w_bandpass * fs / (2 * np.pi)

epsilon = 1e-10
mag_notch = 20 * np.log10(np.abs(H_notch) + epsilon)
mag_bandpass = 20 * np.log10(np.abs(H_bandpass) + epsilon)

# Create figure with 2 subplots
plt.figure(figsize=(14, 10))

# Notch filter response
plt.subplot(2, 1, 1)
plt.plot(freq_notch, mag_notch, 'b-', linewidth=2, label='Notch Filter')
plt.axvline(x=f0_notch, color='r', linestyle='--', linewidth=1.5, label=f'Center: {f0_notch}Hz')
plt.axhline(y=-3, color='g', linestyle=':', linewidth=1.5, alpha=0.7, label='-3dB Level')
plt.axhline(y=0, color='k', linestyle='-', linewidth=0.5, alpha=0.5)
plt.title(f'Notch Filter: Center={f0_notch}Hz, BW={bandwidth_notch}Hz (target), BW={actual_bw_notch:.2f}Hz (actual), Q={Q_notch:.2f}', 
          fontsize=12, fontweight='bold')
plt.ylabel('Magnitude (dB)', fontsize=11)
plt.xlabel('Frequency (Hz)', fontsize=11)
plt.xlim(0, fs/2)
plt.ylim(-60, 5)
plt.grid(True, alpha=0.3)
plt.legend(fontsize=10)

# Bandpass filter response
plt.subplot(2, 1, 2)
plt.plot(freq_bandpass, mag_bandpass, 'b-', linewidth=2, label='Bandpass Filter')
plt.axvline(x=f0_bandpass, color='r', linestyle='--', linewidth=1.5, label=f'Center: {f0_bandpass}Hz')
plt.axhline(y=-3, color='g', linestyle=':', linewidth=1.5, alpha=0.7, label='-3dB Level')
plt.axhline(y=0, color='k', linestyle='-', linewidth=0.5, alpha=0.5)
plt.title(f'Bandpass Filter: Center={f0_bandpass}Hz, BW={bandwidth_bandpass}Hz (target), BW={actual_bw_bandpass:.2f}Hz (actual), Q={Q_bandpass:.2f}', 
          fontsize=12, fontweight='bold')
plt.ylabel('Magnitude (dB)', fontsize=11)
plt.xlabel('Frequency (Hz)', fontsize=11)
plt.xlim(0, fs/2)
plt.ylim(-60, 5)
plt.grid(True, alpha=0.3)
plt.legend(fontsize=10)

plt.tight_layout()
print("\nDisplaying frequency response plot...")
print("(Plots will close automatically after display)")
plt.show(block=True)
# plt.pause(2)  # Display for 2 seconds
# plt.close()

# --------------------------
# 4. Signal Filtering Example
# --------------------------

print("\nApplying filters to test signal...")

# Generate test signal
t = np.linspace(0, 1, fs, endpoint=False)
signal = (np.sin(2*np.pi*50*t) + 
          np.sin(2*np.pi*100*t) + 
          0.1*np.random.randn(len(t)))

# Apply filters
signal_notch_filtered = lfilter(b_notch, a_notch, signal)
signal_bandpass_filtered = lfilter(b_bandpass, a_bandpass, signal_notch_filtered)

# Plot filtering results
plt.figure(figsize=(14, 8))

plt.subplot(3, 1, 1)
plt.plot(t, signal, 'b-', linewidth=0.8, alpha=0.7)
plt.title('Original Signal (50Hz + 100Hz + Noise)', fontsize=12, fontweight='bold')
plt.ylabel('Amplitude', fontsize=11)
plt.xlim(0, 0.1)
plt.grid(True, alpha=0.3)

plt.subplot(3, 1, 2)
plt.plot(t, signal_notch_filtered, 'g-', linewidth=0.8, alpha=0.7)
plt.title('After Notch Filtering (50Hz Removed)', fontsize=12, fontweight='bold')
plt.ylabel('Amplitude', fontsize=11)
plt.xlim(0, 0.1)
plt.grid(True, alpha=0.3)

plt.subplot(3, 1, 3)
plt.plot(t, signal_bandpass_filtered, 'r-', linewidth=0.8, alpha=0.7)
plt.title('After Bandpass Filtering (100Hz Extracted)', fontsize=12, fontweight='bold')
plt.ylabel('Amplitude', fontsize=11)
plt.xlabel('Time (s)', fontsize=11)
plt.xlim(0, 0.1)
plt.grid(True, alpha=0.3)

plt.tight_layout()
print("Displaying signal filtering plot...")
print("(Plots will close automatically after display)")
plt.show(block=True)
# plt.pause(2)
# plt.close()

# --------------------------
# 5. Summary
# --------------------------

print("\n" + "=" * 70)
print("SUMMARY")
print("=" * 70)
print("Both filters are designed using the bandwidth specification method.")
print(f"\nRelationship used: Q = f0 / BW")
print(f"Where BW is the -3dB bandwidth (half-power bandwidth)")
print(f"\nNotch Filter:")
print(f"  - Center frequency: {f0_notch} Hz")
print(f"  - Target bandwidth: {bandwidth_notch} Hz")
print(f"  - Actual bandwidth: {actual_bw_notch:.2f} Hz")
print(f"  - Calculated Q: {Q_notch:.2f}")
print(f"\nBandpass Filter:")
print(f"  - Center frequency: {f0_bandpass} Hz")
print(f"  - Target bandwidth: {bandwidth_bandpass} Hz")
print(f"  - Actual bandwidth: {actual_bw_bandpass:.2f} Hz")
print(f"  - Calculated Q: {Q_bandpass:.2f}")
print(f"  - Gain at center: {gain_at_center:.4f} dB")
print("\n" + "=" * 70)
