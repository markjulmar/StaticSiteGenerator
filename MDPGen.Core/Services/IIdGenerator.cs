using System;
using System.Threading;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Interface for the ID generator
    /// </summary>
    public interface IIdGenerator
    {
        /// <summary>
        /// Generate a unique number to use on the page
        /// </summary>
        /// <returns>Integer</returns>
        int GenerateNumber();

        /// <summary>
        /// Generate a unique valid HTML ID/name
        /// </summary>
        /// <returns></returns>
        string GenerateText();

        /// <summary>
        /// Used to reset the counter.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Implementation of the ID generator.
    /// </summary>
    internal sealed class IdGenerator : IIdGenerator
    {
        private int nextId;

        /// <summary>
        /// Generate a unique number to use on the page
        /// </summary>
        /// <returns>Integer</returns>
        public int GenerateNumber()
        {
            return Interlocked.Increment(ref nextId);
        }

        /// <summary>
        /// Used to reset the counter.
        /// </summary>
        public void Reset()
        {
            nextId = 1;
        }

        /// <summary>
        /// Generate a unique valid HTML ID/name
        /// </summary>
        /// <returns></returns>
        public string GenerateText()
        {
            return "T" + Guid.NewGuid().ToString("N");
        }
    }


}