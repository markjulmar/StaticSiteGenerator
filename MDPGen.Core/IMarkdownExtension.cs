using System;

namespace MDPGen.Core
{
    /// <summary>
    /// Markdown parser compiled extension.
    /// </summary>
    public interface IMarkdownExtension
    {
        /// <summary>
        /// Process the extension
        /// </summary>
        /// <returns>HTML content to inject, null if none.</returns>
        string Process(IServiceProvider provider);
    }
}
