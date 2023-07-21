using MDPGen.Core.Infrastructure;

namespace MDPGen.Core
{
    /// <summary>
    /// Base class for processing blocks.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TR">Result type</typeparam>
    public abstract class BaseProcessingBlock<T, TR> : IProcessingBlock<T, TR>
    {
        /// <summary>
        /// Process the data
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">Data to run this processing block on</param>
        /// <returns>Output</returns>
        public abstract TR Process(PageVariables pageVars, T input);

        /// <summary>
        /// Called when a new page render sequence is about to begin.
        /// Multiple pages may be rendered up to the call to Shutdown
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Called when the page rendering sequence is complete. The block
        /// may release memory, objects, etc.
        /// </summary>
        public virtual void Shutdown()
        {
        }
    }

    /// <summary>
    /// This represents a single processing block in our chain.
    /// </summary>
    public interface IProcessingBlock
    {
        /// <summary>
        /// Called when a new page render sequence is about to begin.
        /// Multiple pages may be rendered up to the call to Shutdown
        /// </summary>
        void Initialize();
        /// <summary>
        /// Called when the page rendering sequence is complete. The block
        /// may release memory, objects, etc.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// This represents a single processing block which can be added to the
    /// static site generator chain.
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <typeparam name="TR">Output type</typeparam>
    public interface IProcessingBlock<in T, out TR> : IProcessingBlock
    {
        /// <summary>
        /// Process the data
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">Data to run this processing block on</param>
        /// <returns>Output</returns>
        TR Process(PageVariables pageVars, T input);
    }
}
