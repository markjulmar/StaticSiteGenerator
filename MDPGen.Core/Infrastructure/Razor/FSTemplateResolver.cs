using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;

namespace MDPGen.Core.Infrastructure.Razor
{
    /// <summary>
    /// Template resolver for RazorEngine which loads files from the filesystem.
    /// </summary>
    internal class FileSystemTemplateResolver : ITemplateManager
    {
        readonly List<string> templatesFolder;
        readonly string[] validExtensions = new[] { ".cshtml", ".html", ".htm" };
        readonly IDictionary<ITemplateKey, ITemplateSource> loadedTemplates = new Dictionary<ITemplateKey, ITemplateSource>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="templatesFolder">Folder to load HTML templates from</param>
        public FileSystemTemplateResolver(List<string> templatesFolder)
        {
            this.templatesFolder = templatesFolder;
        }

        /// <summary>
        /// Resolves templates from a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ITemplateSource Resolve(ITemplateKey key)
        {
            // Already loaded?
            if (loadedTemplates.ContainsKey(key))
            {
                return loadedTemplates[key];
            }

            // Look up the template based on the path
            var templatePath = Utilities.FindFileAlongPath(templatesFolder, key.Name);
            if (templatePath == null)
            {
                foreach (var ext in validExtensions)
                {
                    var testPath = Utilities.FindFileAlongPath(templatesFolder, Path.ChangeExtension(key.Name, ext));
                    if (testPath != null)
                    {
                        templatePath = testPath;
                        break;
                    }
                }
            }

            var template = templatePath != null ? File.ReadAllText(templatePath) : String.Empty;
            return new LoadedTemplateSource(template);
        }

        /// <summary>
        /// Returns a key for the given file.
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="resolveType">Resolve type</param>
        /// <param name="context">Key</param>
        /// <returns></returns>
        public ITemplateKey GetKey(string name, ResolveType resolveType, ITemplateKey context)
        {
            return new NameOnlyTemplateKey(name, resolveType, context);
        }

        /// <summary>
        /// Add a new template to the collection
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="source">Template source</param>
        public void AddDynamic(ITemplateKey key, ITemplateSource source)
        {
            if (!loadedTemplates.ContainsKey(key))
            {
                loadedTemplates.Add(key, source);
            }
        }
    }
}
