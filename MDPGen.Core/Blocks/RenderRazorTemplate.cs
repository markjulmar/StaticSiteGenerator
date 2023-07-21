using MDPGen.Core.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// This processing block merges the created Markdown/HTML into a page template
    /// using the Razor engine.
    /// </summary>
    public class RenderRazorTemplate : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// We create a RazorEngine for each thread and hold them in this
        /// static dictionary.
        /// </summary>
        private static ConcurrentDictionary<int, Lazy<Infrastructure.Razor.RazorEngine>> engines = new ConcurrentDictionary<int, Lazy<Infrastructure.Razor.RazorEngine>>();

        /// <summary>
        /// Dispose the razor engine
        /// </summary>
        public override void Shutdown()
        {
            foreach (var entry in engines)
            {
                if (entry.Value.IsValueCreated)
                    entry.Value.Value.Dispose();
            }

            engines.Clear();
        }

        /// <summary>
        /// Process the merge template function
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">File to run processor on</param>
        /// <returns>Merged files</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            string pageTemplate = pageVars.Page.PageTemplate;
            if (string.IsNullOrWhiteSpace(pageTemplate))
                return input;

            // Create the Razor engine. We use this same instance 
            // throughout the render of the site.
            var razorEngine = GetOrCreateEngine(
                Thread.CurrentThread.ManagedThreadId,
                pageVars.SearchFolders, pageVars.RequiredNamespaces);
            if (razorEngine == null)
                throw new Exception("Failed to create or locate Razor Engine to transform page.");

            // Do the conversion.
            string source = $"@{{Layout=\"{pageTemplate}\";}}\r\n{input}";
            return razorEngine.Transform(source, pageVars);
        }

        /// <summary>
        /// Retrieve or create the Razor engine for this thread.
        /// </summary>
        /// <param name="key">Key to use</param>
        /// <param name="searchFolders">Search folders</param>
        /// <param name="requiredNamespaces">Namespaces</param>
        /// <returns></returns>
        private Infrastructure.Razor.RazorEngine GetOrCreateEngine(int key,
            List<string> searchFolders, List<string> requiredNamespaces)
        {
            // We use a Lazy<T> here to ensure that we only create the engine
            // _once_ per thread. Otherwise two threads hitting this could create
            // separate engines but only have one of them stored. That would be
            // acceptable except that the island engine would never be disposed.
            var lazyEngine = engines.GetOrAdd(key,
                _ => new Lazy<Infrastructure.Razor.RazorEngine>(
                    () => new Infrastructure.Razor.RazorEngine(searchFolders, requiredNamespaces)));
            return lazyEngine.Value;
        }
    }
}