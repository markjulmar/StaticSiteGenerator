using System;
using System.Diagnostics;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Log types
    /// </summary>
    public enum TraceType
    {
        /// <summary>
        /// Error
        /// </summary>
        Error,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Normal
        /// </summary>
        Normal,
        /// <summary>
        /// Debug
        /// </summary>
        Diagnostic
    }

    /// <summary>
    /// Global tracing class
    /// </summary>
    public static class TraceLog
    {
        /// <summary>
        /// Handler to output trace events
        /// </summary>
        public static event Action<TraceType, string> OutputHandler;

        /// <summary>
        /// Write a normal trace event
        /// </summary>
        /// <param name="output"></param>
        public static void Write(string output)
        {
            Write(TraceType.Normal, output);
        }

        /// <summary>
        /// Write a specific trace event
        /// </summary>
        /// <param name="traceType">Trace type</param>
        /// <param name="output">String</param>
        public static void Write(TraceType traceType, string output)
        {
            Debug.WriteLine(output);
            OutputHandler?.Invoke(traceType, output);
        }
    }
}
