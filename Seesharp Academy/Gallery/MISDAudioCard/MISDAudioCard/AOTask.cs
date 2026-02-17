using System;
using System.Runtime.InteropServices;

namespace MISDAudioCard
{
    /// <summary>
    /// Audio Output Task for playback to sound card
    /// </summary>
    public class AOTask : AudioTask
    {
        #region Win32 API Declarations

        private const int WOM_OPEN = 0x3BB;
        private const int WOM_CLOSE = 0x3BC;
        private const int WOM_DONE = 0x3BD;

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
        private struct WAVEOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] szPname;
            public uint dwFormats;
            public ushort wChannels;
            public ushort wReserved1;
            public uint dwSupport;
        }

        private delegate void waveOutCallback(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        private static extern int waveOutGetNumDevs();

        [DllImport("winmm.dll")]
        private static extern int waveOutGetDevCaps(IntPtr uDeviceID, ref WAVEOUTCAPS pwfx, int cbwfx);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutOpen(out IntPtr hWaveOut, IntPtr uDeviceID, WAVEFORMATEX pwfx, waveOutCallback dwCallback, IntPtr dwInstance, uint fdwOpen);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutClose(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutWrite(IntPtr hWaveOut, WAVEHDR pwh, uint cbwh);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutPause(IntPtr hWaveOut);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern int waveOutRestart(IntPtr hWaveOut);

        private const int CALLBACK_FUNCTION = 0x00030000;

        #endregion

        #region Private Fields

        private IntPtr _waveOutHandle = IntPtr.Zero;
        private WAVEFORMATEX _waveFormat;
        private GCHandle _waveHandle;
        private WAVEHDR _waveHeader;
        private GCHandle _waveHeaderHandle;
        private IntPtr _dataBuffer;
        private GCHandle _dataBufferHandle;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private waveOutCallback _callback;
        private AOMode _mode;
        private uint _samplesToUpdate;
        private double[] _dataToPlay;
        private bool _playbackCompleted = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for AOTask
        /// </summary>
        /// <param name="deviceName">Device name (can be empty for default device)</param>
        public AOTask(string deviceName) : base()
        {
            _taskName = deviceName;
            _mode = AOMode.Finite;
            _callback = new waveOutCallback(WaveOutCallback);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Output mode
        /// </summary>
        public AOMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        /// <summary>
        /// Update rate (same as SampleRate)
        /// </summary>
        public double UpdateRate
        {
            get { return _sampleRate; }
            set { _sampleRate = value; }
        }

        /// <summary>
        /// Number of samples to output
        /// </summary>
        public uint SamplesToUpdate
        {
            get { return _samplesToUpdate; }
            set { _samplesToUpdate = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Add channel for playback
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
        /// Add channel (simplified version)
        /// </summary>
        /// <param name="channelNumber">Channel number</param>
        public void AddChannel(int channelNumber)
        {
            AddChannel(channelNumber, -1.0, 1.0);
        }

        /// <summary>
        /// Write data to buffer
        /// </summary>
        /// <param name="data">Data array to write</param>
        /// <param name="timeout">Timeout in milliseconds (-1 for infinite)</param>
        public void WriteData(double[] data, int timeout)
        {
            _dataToPlay = new double[data.Length];
            Array.Copy(data, _dataToPlay, data.Length);
        }

        /// <summary>
        /// Start playback
        /// </summary>
        public override void Start()
        {
            if (_channels.Count == 0)
            {
                throw new InvalidOperationException("No channels added. Add at least one channel before starting.");
            }

            if (_isPlaying)
            {
                throw new InvalidOperationException("Already playing");
            }

            if (_dataToPlay == null || _dataToPlay.Length == 0)
            {
                throw new InvalidOperationException("No data to play. Write data first.");
            }

            _playbackCompleted = false;

            try
            {
                InitializeWaveOut();
                _isPlaying = true;
                _isPaused = false;
            }
            catch (Exception ex)
            {
                Cleanup();
                throw new Exception("Failed to start playback: " + ex.Message);
            }
        }

        /// <summary>
        /// Stop playback
        /// </summary>
        public override void Stop()
        {
            if (!_isPlaying)
            {
                return;
            }

            _isPlaying = false;
            _isPaused = false;

            try
            {
                if (_waveOutHandle != IntPtr.Zero)
                {
                    waveOutReset(_waveOutHandle);
                    waveOutClose(_waveOutHandle);
                    _waveOutHandle = IntPtr.Zero;
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
        /// Wait until playback is complete
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>True if playback completed, false if timeout</returns>
        public bool WaitUntilDone(int timeout)
        {
            int elapsed = 0;
            while (_isPlaying && elapsed < timeout)
            {
                System.Threading.Thread.Sleep(50);
                elapsed += 50;

                if (_playbackCompleted)
                {
                    return true;
                }
            }

            return _playbackCompleted;
        }

        #endregion

        #region Private Methods

        private void InitializeWaveOut()
        {
            int deviceCount = waveOutGetNumDevs();
            if (deviceCount == 0)
            {
                throw new Exception("No audio output devices found");
            }

            int channels = _channels.Count;
            _waveFormat = new WAVEFORMATEX
            {
                wFormatTag = 1, // PCM
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)_sampleRate,
                wBitsPerSample = 16,
                nBlockAlign = (ushort)(channels * 2),
                nAvgBytesPerSec = (uint)(_sampleRate * channels * 2),
                cbSize = 0
            };

            int result = waveOutOpen(out _waveOutHandle, IntPtr.Zero, _waveFormat, _callback, IntPtr.Zero, CALLBACK_FUNCTION);
            if (result != 0)
            {
                throw new Exception("Failed to open wave output device. Error code: " + result);
            }

            // Convert double data to short array for wave output
            int dataLength = _dataToPlay.Length;
            short[] shortData = new short[dataLength];

            for (int i = 0; i < dataLength; i++)
            {
                // Clamp value between -1.0 and 1.0
                double value = Math.Max(-1.0, Math.Min(1.0, _dataToPlay[i]));
                shortData[i] = (short)(value * 32767.0);
            }

            // Allocate data buffer
            int bufferSizeInBytes = shortData.Length * 2;
            _dataBuffer = Marshal.AllocHGlobal(bufferSizeInBytes);
            Marshal.Copy(shortData, 0, _dataBuffer, shortData.Length);
            _dataBufferHandle = GCHandle.Alloc(_dataBuffer, GCHandleType.Pinned);

            // Initialize wave header
            _waveHeader = new WAVEHDR
            {
                lpData = _dataBuffer,
                dwBufferLength = (uint)bufferSizeInBytes,
                dwBytesRecorded = 0,
                dwUser = IntPtr.Zero,
                dwFlags = 0,
                dwLoops = 0,
                lpNext = IntPtr.Zero,
                reserved = 0
            };

            if (_mode == AOMode.ContinuousWrapping)
            {
                _waveHeader.dwLoops = 0xFFFF; // Infinite loop
            }

            _waveHeaderHandle = GCHandle.Alloc(_waveHeader, GCHandleType.Pinned);

            result = waveOutPrepareHeader(_waveOutHandle, _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
            if (result != 0)
            {
                throw new Exception("Failed to prepare wave header. Error code: " + result);
            }

            result = waveOutWrite(_waveOutHandle, _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
            if (result != 0)
            {
                throw new Exception("Failed to write data. Error code: " + result);
            }
        }

        private void WaveOutCallback(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            if (uMsg == WOM_DONE)
            {
                if (_mode == AOMode.Finite)
                {
                    _playbackCompleted = true;
                }
                // For ContinuousWrapping mode, the audio will loop automatically
            }
        }

        private void Cleanup()
        {
            if (_waveHeaderHandle.IsAllocated)
            {
                if (_waveOutHandle != IntPtr.Zero && _waveHeader != null)
                {
                    try
                    {
                        waveOutUnprepareHeader(_waveOutHandle, _waveHeader, (uint)Marshal.SizeOf(typeof(WAVEHDR)));
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
