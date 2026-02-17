using System;

namespace MISDAudioCard
{
    /// <summary>
    /// Audio input mode enumeration
    /// </summary>
    public enum AIMode
    {
        /// <summary>
        /// Finite acquisition mode
        /// </summary>
        Finite
    }

    /// <summary>
    /// Audio output mode enumeration
    /// </summary>
    public enum AOMode
    {
        /// <summary>
        /// Finite output mode
        /// </summary>
        Finite,
        /// <summary>
        /// Continuous wrapping output mode
        /// </summary>
        ContinuousWrapping
    }

    /// <summary>
    /// Audio channel enumeration
    /// </summary>
    public enum AudioChannel
    {
        /// <summary>
        /// Left channel
        /// </summary>
        Left = 0,
        /// <summary>
        /// Right channel
        /// </summary>
        Right = 1
    }

    /// <summary>
    /// Audio terminal configuration
    /// </summary>
    public enum AITerminal
    {
        /// <summary>
        /// Differential mode
        /// </summary>
        Differential,
        /// <summary>
        /// Single-ended mode
        /// </summary>
        SingleEnded
    }
}
