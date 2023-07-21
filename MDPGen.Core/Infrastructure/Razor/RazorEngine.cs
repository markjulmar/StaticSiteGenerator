using System;
using RazorEngine;
using RazorEngine.Templating; // For extension methods.
using RazorEngine.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RazorEngine.Roslyn;
using System.Dynamic;
using MDPGen.Core.Services;

namespace MDPGen.Core.Infrastructure.Razor
{
    /// <summary>
    /// Wrapper around the ASP.NET MVC Razor engine.
    /// </summary>
    public sealed class  RazorEngine : IDisposable
    {
        IRazorEngineService razorService;

        /// <summary>
        /// Constructor
        /// </summary>
        public RazorEngine(List<string> templatesFolders)
            : this(templatesFolders, Enumerable.Empty<string>()) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="templatesFolders">Folder with template files</param>
        /// <param name="namespaces">Default namespaces to add</param>
        public RazorEngine(List<string> templatesFolders, IEnumerable<string> namespaces)
        {
            if (templatesFolders == null || templatesFolders.Count == 0)
                throw new ArgumentNullException(nameof(templatesFolders));

            var config = new TemplateServiceConfiguration
            {
                Language = Language.CSharp,
                DisableTempFileLocking = true, // loads the files in-memory (gives the templates full-trust permissions)
                TemplateManager = new FileSystemTemplateResolver(templatesFolders),
                CompilerServiceFactory = new RoslynCompilerServiceFactory(),
                ReferenceResolver = new MemoryReferenceResolver(),
                CachingProvider = new DefaultCachingProvider(t => { }), //disables the warnings
                BaseTemplateType = typeof(RazorSupportTemplateBase)
            };

            // Add the namespaces.
            if (namespaces != null)
            {
                foreach (var n in namespaces.Distinct())
                    config.Namespaces.Add(n);
            }

            // Create the Razor engine.
            razorService = RazorEngineService.Create(config);
        }

        public void Dispose()
        {
            razorService?.Dispose();
            razorService = null;
        }

        /// <summary>
        /// Run Razor transformation on a page.
        /// </summary>
        /// <param name="content">Content</param>
        /// <param name="model">Model to supply</param>
        /// <returns></returns>
        public string Transform(string content, PageVariables model)
        {
            if (razorService == null)
                throw new ObjectDisposedException(nameof(RazorEngine));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            string key = GenerateKey(content).ToString();

            try
            {
                // Run the script.
                return razorService.RunCompile(content, 
                    key, typeof(PageVariables), model, new DynamicViewBagWrapper(model.PageCache));
            }
            catch (TemplateCompilationException ex)
            {
                var sb = new StringBuilder();
                foreach (var err in ex.CompilerErrors)
                    sb.AppendLine($"{err.ErrorNumber}: ({err.Line.ToString()}:{err.Column.ToString()}) {err.ErrorText}");
                throw new Exception(sb.ToString(), ex);
            }
        }

        /// <summary>
        /// Generate a unique hash key for the given file.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GenerateKey(string body)
        {
            return string.IsNullOrEmpty(body) ? 0: EqualityComparer<string>.Default.GetHashCode(body) & 0x7FFFFFFF;
        }

        /// <summary>
        /// This allows us to use our DynamicPageCache as a view bag
        /// in the Razor engine - so we don't have to copy values in and out.
        /// </summary>
        class DynamicViewBagWrapper : DynamicViewBag
        {
            private readonly DynamicPageCache pageCache;

            /// <summary>
            /// Constructor to initialize the view bag.
            /// </summary>
            /// <param name="pageCache"></param>
            public DynamicViewBagWrapper(DynamicPageCache pageCache)
            {
                this.pageCache = pageCache;
            }

            /// <summary>
            /// Gets the set of dynamic member names.
            /// </summary>
            /// <returns>An instance of <see cref="T:System.Collections.Generic.IEnumerable`1" />.</returns>
            public override IEnumerable<string> GetDynamicMemberNames() => pageCache.GetDynamicMemberNames();

            /// <summary>
            /// Attempts to read a dynamic member from the object.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="result">The result instance.</param>
            /// <returns>True, always.</returns>
            public override bool TryGetMember(GetMemberBinder binder, out object result) => pageCache.TryGetMember(binder, out result);

            /// <summary>
            /// Attempts to set a value on the object.
            /// </summary>
            /// <param name="binder">The binder.</param>
            /// <param name="value">The value to set.</param>
            /// <returns>True, always.</returns>
            public override bool TrySetMember(SetMemberBinder binder, object value) => pageCache.TrySetMember(binder, value);
        }
    }
}
