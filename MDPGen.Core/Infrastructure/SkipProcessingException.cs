using System;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// Processing blocks can throw this exception to stop
    /// processing of a chain item and skip it without 
    /// actually ending the overall task.
    /// </summary>
    [Serializable]
    public class SkipProcessingException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SkipProcessingException() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        public SkipProcessingException(string message) : base(message) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception caught</param>
        public SkipProcessingException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        protected SkipProcessingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
