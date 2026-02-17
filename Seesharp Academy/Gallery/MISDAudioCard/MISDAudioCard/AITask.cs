using System;
using System.Runtime.InteropServices;

namespace MISDAudioCard
{
    /// <summary>
    /// Audio Input Task for recording from sound card
    /// </summary>
    public class AITask : AudioTask
    {
        #region Win32 API Declarations

        private const int WOM_OPEN = 0x3BB;
        private const int WOM_CLOSE = 0x3BC;
        private const int WOM_DONE = 0x3BD;
        private const int WIM_OPEN = 0x3BE;
        private const int WIM_CLOSE = 0x3BF;
        private const int WIM_DATA = 0x3C0;
        private const int WHDR_DONE = 0x00000001;
        private const int WHDR_PREPARED = 0x00000002;
        private const int WHDR_INQUEUE = 0x00000010;

        [StructLayout(LayoutKind.Sequential)]
        private class WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public uint reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
        }

        private delegate void waveInCallback(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        private static extern int waveInGetNumDevs();

        [DllImport("winmm.dll")]
        private static extern int waveInGetDevCaps(IntPtr uDeviceID, ref WAVEINCAPS pwfx, int cbwfx);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInOpen(out IntPtr hWaveIn, IntPtr uDeviceID, WAVEFORMATEX pwfx, waveInCallback dwCallback, IntPtr dwInstance, uint fdwOpen);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInClose(IntPtr hWaveIn);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInPrepareHeader(IntPtr hWaveIn, WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInUnprepareHeader(IntPtr hWaveIn, WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInAddBuffer(IntPtr hWaveIn, WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInStart(IntPtr hWaveIn);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInStop(IntPtr hWaveIn);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveInReset(IntPtr hWaveIn);

        private const int CALLBACK_FUNCTION = 0x00030000;

        #endregion

        #region Private Fields

        private IntPtr _waveInHandle = IntPtr.Zero;
        private WAVEFORMATEX _waveFormat;
        private GCHandle _waveHandle;
        private WAVEHDR _waveHeader;
        private GCHandle _waveHeaderHandle;
        private IntPtr _dataBuffer;
        private GCHandle _dataBufferHandle;
        private bool _isRecording = false;
        private waveInCallback _callback;
        private double[] _recordedData;
        private uint _samplesToAcquire;
        private uint _samplesRecorded;
        private AIMode _mode;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for AITask
        /// </summary>
        /// <param name="deviceName">Device name (can be empty for default device)</param>
        public AITask(string deviceName) : base()
        {
            _taskName = deviceName;
            _mode = AIMode.Finite;
            _callback = new waveInCallback(WaveInCallback);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Acquisition mode
        /// </summary>
        public AIMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        /// <summary>
        /// Number of samples to acquire
        /// </summary>
        public uint SamplesToAcquire
        {
            get { return _samplesToAcquire; }
            set { _samplesToAcquire = value; }
        }

        /// <summary>
        /// Get number of available samples in buffer
        /// </summary>
        public ulong AvailableSamples
        {
            get { return (ulong)_samplesRecorded; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add channel for recording
        /// </summary>
        /// <param name="channelNumber">Channel number (0 for Left, 1 for Right)</param>
        /// <param name="lowRange">Low range (not used for audio)</param>
        /// <param name="highRange">High range (not used for audio)</param>
        public override void AddChannel(int channelNumber, double lowRange, double highRange)
        {
            if (channelNumber < 0 || channelNumber > 1)
            {
                throw new ArgumentException("Channel number must be 0 (Left) or 1 (Right)");
            }

            if (_channels.Count >= 2)
            {
                throw new InvalidOperationException("Maximum 2 channels supported");
            }

            _channels.Add(new AudioChannelConfig(channelNumber, lowRange, highRange));
        }

        /// <summary>
        /// Add channel with terminal configuration
        /// </summary>
        /// <param name="channelNumber">Channel number</param>
        /// <param name="lowRange">Low range</param>
        /// <param name="highRange">High range</param>
        /// <param name="terminal">Terminal configuration</param>
        public void AddChannel(int channelNumber, double lowRange, double highRange, AITerminal terminal)
        {
            AddChannel(channelNumber, lowRange, highRange);
        }

        /// <summary>
        /// Start recording
        /// </summary>
        public override void Start()
        {
            if (_channels.Count == 0)
            {
                throw new InvalidOperationException("No channels added. Add at least one channel before starting.");
            }

            if (_isRecording)
            {
                throw new InvalidOperationException("Already recording");
            }

            try
            {
                InitializeWaveIn();
                _isRecording = true;
            }
            catch (Exception ex)
            {
                Cleanup();
                throw new Exception("Failed to start recording: " + ex.Message);
            }
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public override void Stop()
        {
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;

            try
            {
                if (_waveInHandle != IntPtr.Zero)
                {
                    waveInStop(_waveInHandle);
                    waveInReset(_waveInHandle);
                    waveInClose(_waveInHandle);
                    _waveInHandle = IntPtr.Zero;
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Read data from buffer
        /// </summary>
        /// <param name="data">2D array to store data [samples, channels]</param>
        /// <param name="samplesPerChannel">Number of samples per channel</param>
        /// <param name="timeout">Timeout in milliseconds (-1 for infinite)</param>
        public void ReadData(ref double[,] data, int samplesPerChannel, int timeout)
        {
            if (_recordedData == null)
            {
                throw new InvalidOperationException("No data recorded yet");
            }

            int channels = _channels.Count;
            data = new double[samplesPerChannel, channels];

            // Copy recorded data to output array
            for (int i = 0; i < samplesPerChannel && i < _recordedData.Length / channels; i++)
            {
                for (int ch = 0; ch < channels; ch++)
                {
                    data[i, ch] = _recordedData[i * channels + ch];
                }
            }
        }

        #endregion

        #region Private Methods

        private void InitializeWaveIn()
        {
            int deviceCount = waveInGetNumDevs();
            if (deviceCount == 0)
            {
                throw new Exception("No audio input devices found");
            }

            _waveFormat = new WAVEFORMATEX
            {
                wFormatTag = 1, // PCM
                nChannels = (ushort)_channels.Count,
                nSamplesPerSec = (uint)_sampleRate,
                wBitsPerSample = 16,
                nBlockAlign = (ushort)(_channels.Count * 2),
                nAvgBytesPerSec = (uint)(_sampleRate * _channels.Count * 2),
                cbSize = 0
            };

            int result = waveInOpen(out _waveInHandle, IntPtr.Zero, _waveFormat, _callback, IntPtr.Zero, CALLBACK_FUNCTION);
            if (result != 0)
            {
                throw new Exception("Failed to open wave input device. Error code: " + result);
            }

            // Calculate buffer size (16-bit samples, 2 bytes per sample)
            uint samplesPerChannel = _samplesToAcquire;
            uint numChannels = (uint)_channels.Count;
            uint bytesPerSample = 2;
            uint bufferSize = samplesPerChannel * numChannels * bytesPerSample;

            // Allocate data buffer
            _dataBuffer = Marshal.AllocHGlobal((int)bufferSize);
            _dataBufferHandle = GCHandle.Alloc(_dataBuffer, GCHandleType.Pinned);

            // Initialize wave header
            _waveHeader = new WAVEHDR
            {
                lpData = _dataBuffer,
                dwBufferLength = bufferSize,
                dwBytesRecorded = 0,
                dwUser = IntPtr.Zero,
                dwFlags = 0,
                dwLoops = 0,
                lpNext = IntPtr.Zero,
                reserved = 0
            };

            _waveHeaderHandle = GCHandle.Alloc(_waveHeader, GCHandleType.Pinned);

            result = waveInPrepareHeader(_waveInHandle, _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
            if (result != 0)
            {
                throw new Exception("Failed to prepare wave header. Error code: " + result);
            }

            result = waveInAddBuffer(_waveInHandle, _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
            if (result != 0)
            {
                throw new Exception("Failed to add buffer. Error code: " + result);
            }

            _samplesRecorded = 0;
            result = waveInStart(_waveInHandle);
            if (result != 0)
            {
                throw new Exception("Failed to start recording. Error code: " + result);
            }
        }

        private void WaveInCallback(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            if (uMsg == WIM_DATA && _isRecording)
            {
                // Copy data from buffer to array
                WAVEHDR header = Marshal.PtrToStructure<WAVEHDR>(dwParam1);
                int samples = (int)(header.dwBytesRecorded / 2); // 16-bit samples
                short[] rawData = new short[samples];

                Marshal.Copy(header.lpData, rawData, 0, samples);

                // Convert to double and normalize
                _recordedData = new double[samples];
                for (int i = 0; i < samples; i++)
                {
                    _recordedData[i] = rawData[i] / 32768.0; // Normalize to -1.0 to 1.0
                }

                _samplesRecorded = (uint)_recordedData.Length;
                _isRecording = false;
            }
        }

        private void Cleanup()
        {
            if (_waveHeaderHandle.IsAllocated)
            {
                if (_waveInHandle != IntPtr.Zero && _waveHeader != null)
                {
                    try
                    {
                        waveInUnprepareHeader(_waveInHandle, _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
                    }
                    catch { }
                }
                _waveHeaderHandle.Free();
            }

            if (_dataBufferHandle.IsAllocated)
            {
                if (_dataBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_dataBuffer);
                    _dataBuffer = IntPtr.Zero;
                }
                _dataBufferHandle.Free();
            }

            if (_waveHandle.IsAllocated)
            {
                _waveHandle.Free();
            }
        }

        #endregion
    }
}
