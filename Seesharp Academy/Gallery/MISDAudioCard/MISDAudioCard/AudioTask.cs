using System;
using System.Collections.Generic;

namespace MISDAudioCard
{
    /// <summary>
    /// Base class for audio channel configuration
    /// </summary>
    public class AudioChannelConfig
    {
        /// <summary>
        /// Channel number (0 for Left, 1 for Right)
        /// </summary>
        public int ChannelNumber { get; set; }

        /// <summary>
        /// Low voltage range
        /// </summary>
        public double LowRange { get; set; }

        /// <summary>
        /// High voltage range
        /// </summary>
        public double HighRange { get; set; }

        public AudioChannelConfig(int channel, double lowRange, double highRange)
        {
            ChannelNumber = channel;
            LowRange = lowRange;
            HighRange = highRange;
        }
    }

    /// <summary>
    /// Collection of audio channels
    /// </summary>
    public class AudioChannelCollection : List<AudioChannelConfig>
    {
        /// <summary>
        /// Get channel count
        /// </summary>
        public new int Count
        {
            get { return base.Count; }
        }
    }

    /// <summary>
    /// Base class for Audio Task
    /// </summary>
    public abstract class AudioTask : IDisposable
    {
        /// <summary>
        /// Channels collection
        /// </summary>
        protected AudioChannelCollection _channels;

        /// <summary>
        /// Sample rate
        /// </summary>
        protected double _sampleRate;

        /// <summary>
        /// Task name
        /// </summary>
        protected string _taskName;

        /// <summary>
        /// Is disposed flag
        /// </summary>
        protected bool _disposed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        protected AudioTask()
        {
            _channels = new AudioChannelCollection();
            _sampleRate = 44100.0; // Default sample rate
        }

        /// <summary>
        /// Get channels collection
        /// </summary>
        public AudioChannelCollection Channels
        {
            get { return _channels; }
        }

        /// <summary>
        /// Sample rate (samples per second)
        /// </summary>
        public double SampleRate
        {
            get { return _sampleRate; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Sample rate must be positive");
                _sampleRate = value;
            }
        }

        /// <summary>
        /// Add channel to the task
        /// </summary>
        /// <param name="channelNumber">Channel number (0 or 1)</param>
        /// <param name="lowRange">Low voltage range</param>
        /// <param name="highRange">High voltage range</param>
        public abstract void AddChannel(int channelNumber, double lowRange, double highRange);

        /// <summary>
        /// Start the task
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stop the task
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Dispose method
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    Stop();
                    if (_channels != null)
                    {
                        _channels.Clear();
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~AudioTask()
        {
            Dispose(false);
        }
    }
}
