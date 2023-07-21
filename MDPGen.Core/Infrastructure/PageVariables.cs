using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MDPGen.Core.Data;
using MDPGen.Core.MarkdownExtensions;
using MDPGen.Core.Services;
using System.Linq;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// The page render state - this is unique to each page we render and is passed
    /// down through the processing blocks.
    /// </summary>
    public class PageVariables : IServiceProvider
    {
        private readonly string defaultPageTemplate;
        private readonly IEnumerable<KeyValuePair<string, string>> constantTokens;
        private readonly DynamicPageCache pageCache = new DynamicPageCache();
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private readonly Func<string, string, ContentPage, bool> copyAssetCheck;

        /// <summary>
        /// Build symbols added to use with conditionals
        /// </summary>
        public List<string> BuildSymbols { get; }

        /// <summary>
        /// List of folders where we look for include files and templates (e.g. Path)
        /// </summary>
        public List<string> SearchFolders { get; }
        
        /// <summary>
        /// Base output folder we are writing to.
        /// </summary>
        public string OutputFolder { get; }

        /// <summary>
        /// Page we are working on
        /// </summary>
        public ContentPage Page { get; private set; }

        /// <summary>
        /// Return the ViewBag as the real type.
        /// </summary>
        public DynamicPageCache PageCache => pageCache;

        /// <summary>
        /// Page state (dynamic cache)
        /// </summary>
        public dynamic ViewBag => pageCache;

        /// <summary>
        /// Replacement tokens we can use
        /// </summary>
        public ITokenCollection Tokens { get; } = ServiceFactory.Instance.Create<ITokenCollection>();

        /// <summary>
        /// Namespaces for this page.
        /// </summary>
        public List<string> RequiredNamespaces { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PageVariables(
            string defaultPageTemplate = null, 
            IEnumerable<KeyValuePair<string, string>> constantTokens = null, 
            List<string> buildSymbols = null,
            List<string> searchFolders = null,
            string outputFolder = null,
            IEnumerable<string> requiredNamespaces = null,
            Func<string, string, ContentPage, bool> copyAssetCheck = null)
        {
            this.defaultPageTemplate = defaultPageTemplate;
            this.constantTokens = constantTokens;
            this.OutputFolder = outputFolder;
            this.BuildSymbols = buildSymbols ?? new List<string>();
            this.SearchFolders = searchFolders ?? new List<string>();

            RegisterInstance(ServiceFactory.Instance.Create<IMarkdownParser>());
            RegisterInstance(Tokens);
            RegisterInstance(ServiceFactory.Instance.Create<IIdGenerator>());
            RegisterInstance(pageCache);

            requiredNamespaces = requiredNamespaces?.Union(ScriptConfig.RequiredNamespaces) 
                ?? ScriptConfig.RequiredNamespaces;

            this.RequiredNamespaces = requiredNamespaces.Distinct().ToList();
            this.copyAssetCheck = copyAssetCheck;
        }

        /// <summary>
        /// Initialize the page state for the given page.
        /// </summary>
        /// <param name="page">Content page to render</param>
        /// <param name="initScriptFilename">C# script to initialize</param>
        public void InitializeFor(ContentPage page, string initScriptFilename = null)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            // Reset our state
            Tokens.Clear();
            pageCache.Reset();
            GetService<IIdGenerator>().Reset();

            // Set the page template if it's not supplied.
            // We need to check both possible locations since 
            // the metadata template can be set to "none" 
            // which results in a null template which we don't want
            // to override here.
            if (page.GetMetadata()?.PageTemplate == null
                && page.PageTemplate == null)
            {
                page.PageTemplate = defaultPageTemplate;
            }

            // Get the last modified date of the original Markdown file.
            if (!string.IsNullOrEmpty(page.Filename))
            {
                try
                {
                    var fi = new FileInfo(page.Filename);
                    if (fi.Exists)
                    {
                        this.Tokens[KnownTokens.LastModifedDate] = fi.LastWriteTimeUtc.ToString("O");
                    }
                }
                catch
                {
                    // Ignore exceptions.
                }
            }

            // Constants
            this.Tokens[KnownTokens.Title] = page.Title;
            this.Tokens[KnownTokens.Date] = DateTime.UtcNow.ToString("O");
            this.Tokens[KnownTokens.GeneratorVersion] = GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
            this.Tokens[KnownTokens.SourceFile] = Path.GetFileName(page.Filename);

            // Add constant tokens
            if (this.constantTokens != null)
            {
                foreach (var pair in this.constantTokens)
                    this.Tokens[pair.Key] = pair.Value;
            }

            // Set our current node
            ViewBag.CurrentPage = Page = page;

            // Add any tokens from the page header.
            if (page.GetMetadata()?.Tokens?.Count > 0)
            {
                foreach (var item in page.GetMetadata().Tokens)
                {
                    this.Tokens[item.Key] = item.Value;
                }
            }

            // Connect up the extensions.
            ExtensionProcessor.InitializeExtensions(this, initScriptFilename);
        }

        /// <summary>
        /// Copies a folder - uses the CopyAssetCheck to ensure we only copy
        /// the necessary bits.
        /// </summary>
        /// <param name="sourceFolder">Source folder</param>
        /// <param name="destFolder">Destination folder</param>
        /// <returns>Number of files copied</returns>
        public int CopyFolder(string sourceFolder, string destFolder)
        {
            return Utilities.RecursiveCopyFolder(
                new DirectoryInfo(sourceFolder),
                new DirectoryInfo(destFolder),
                (s,d) => copyAssetCheck(s,d,Page.Root));
        }

        /// <summary>
        /// Register a specific type as a service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Register<T>() where T : class
        {
            services[typeof(T)] = null;
        }

        /// <summary>
        /// Register a specific instance as a service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void RegisterInstance<T>(T value) where T : class
        {
            services[typeof(T)] = value;
        }

        /// <summary>
        /// Generic version of GetService()
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        /// <summary>
        /// Retrieves the service by type.
        /// </summary>
        /// <param name="serviceType">Type to find</param>
        /// <returns>Service instance or null</returns>
        public object GetService(Type serviceType)
        {
            object value = null;
            foreach (var entry in services.Keys)
            {
                if (serviceType.GetTypeInfo().IsAssignableFrom(entry.GetTypeInfo()))
                {
                    value = services[entry];
                    if (value == null)
                    {
                        try
                        {
                            value = Activator.CreateInstance(serviceType);
                            services[entry] = value;
                        }
                        catch
                        {
                            // Hmm..            
                        }
                    }
                }
            }

            return value;
        }
    }
}
