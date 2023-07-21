using System;
using MDPGen.Core.Services;

namespace MDPGen.Core.MarkdownExtensions
{
    /// <summary>
    /// Base class used for our Markdown extensions
    /// </summary>
    public abstract class BaseMarkdownExtension : IMarkdownExtension
    {
        /// <summary>
        /// IServiceProvider which is used to retrieve services for the extension
        /// </summary>
        protected IServiceProvider ServiceProvider;

        /// <summary>
        /// The session cache - this is cleared after each page render.
        /// </summary>
        public dynamic PageState => ServiceProvider.GetService<DynamicPageCache>();

        /// <summary>
        /// Tokens used for replacement tags in the template HTML file.
        /// </summary>
        public ITokenCollection Tokens => ServiceProvider.GetService<ITokenCollection>();

        /// <summary>
        /// Unique ID/name generator for HTML tags.
        /// </summary>
        public IIdGenerator IdGen => ServiceProvider.GetService<IIdGenerator>();

        /// <summary>
        /// Method used to generate the HTML output.
        /// </summary>
        /// <returns>HTML output to add to the resulting page, or null if no output.</returns>
        protected abstract string Process();

        /// <summary>
        /// Method for IMarkdownExtension which generates the HTML output
        /// </summary>
        /// <param name="provider">Service provider with supported services usable by the extension</param>
        /// <returns>HTML output to add to the resulting page, or null if no output.</returns>
        string IMarkdownExtension.Process(IServiceProvider provider)
        {
            ServiceProvider = provider;
            return Process();
        }
    }
}
