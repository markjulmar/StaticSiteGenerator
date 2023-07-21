using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MDPGen.Core.Data;
using MDPGen.Core.Services;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// Page loader which grabs all the files in a given location.
    /// </summary>
    public class FolderContentPageLoader : BaseContentPageLoader
    {
        // Files we look for as the "default" URL mapped file
        // E.g. What "/folder/" maps to in a GET request.
        private readonly List<string> defaultFiles = new List<string> { "default", "index" };

        /// <summary>
        /// True to recurse into sub folders.
        /// Defaults to false.
        /// </summary>
        public bool Recursive { get; set; }

        /// <summary>
        /// The files to look for - defaults to "*.md"
        /// </summary>
        public string Filespec { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public FolderContentPageLoader()
        {
            Filespec = "*.md";
        }

        /// <summary>
        /// LoadAsync the content pages from the given folder.
        /// </summary>
        /// <param name="contentFolder">Folder to start at</param>
        /// <returns>Loaded content page with all children</returns>
        public override async Task<ContentPage> LoadAsync(string contentFolder)
        {
            if (contentFolder == null)
                throw new ArgumentNullException(nameof(contentFolder));

            if (!Directory.Exists(contentFolder))
                throw new ArgumentException("Folder does not exist.", nameof(contentFolder));

            // Create a metadata loader if it's not assigned yet.
            if (MetadataLoader == null)
                MetadataLoader = ServiceFactory.Instance.Create<IPageMetadataLoader>();

            OnPreLoad(contentFolder);
            var root = await ScanFolderAsync(null, contentFolder, contentFolder);
            OnPostLoad(contentFolder, root);

            return root;
        }

        /// <summary>
        /// Scans a given folderand returns a ContentPage with all the 
        /// files loaded into it. If a "default" file is located, it will
        /// be assigned to the parent node.
        /// </summary>
        /// <param name="parent">Parent (if any)</param>
        /// <param name="rootFolder">Starting point for content</param>
        /// <param name="contentFolder">Current content location</param>
        /// <returns>ContentPage with children</returns>
        private async Task<ContentPage> ScanFolderAsync(ContentPage parent, string rootFolder, string contentFolder)
        {
            string relativeFolder = contentFolder.Substring(rootFolder.Length);

            var root = new ContentPage
            { 
                Url  = Utilities.GenerateRelativeUrlFromFolder(relativeFolder),
                IsDefaultPage = true,
                Parent = parent
            };

            foreach (var file in Directory.GetFiles(contentFolder, Filespec).OrderBy(n => n))
            {
                var entry = Path.GetFileName(file);
                ContentPage node;
                if (defaultFiles.Contains(Path.GetFileNameWithoutExtension(file)?.ToLowerInvariant()))
                {
                    node = root;
                }
                else
                {
                    node = new ContentPage
                    {
                        Parent = root,
                        Url = Constants.WebSeparator.UrlCombine(Utilities.GenerateRelativeUrlFromFolder(relativeFolder), Path.GetFileNameWithoutExtension(entry)),
                    };
                    root.Children.Add(node);
                }

                // Set the filenames
                node.Filename = file;
                DetermineFilenamesAndContentType(node, rootFolder, contentFolder, entry);

                // Load the content
                try
                {
                    await LoadContentForNodeAsync(node);
                }
                catch (Exception ex)
                {
                    TraceLog.Write(TraceType.Error, $"{ex.GetType().Name} caught loading content for {node.Filename} {ex.Message} {ex.InnerException?.Message}");
                    ex.Data["caught"] = true;
                    throw;
                }
            }

            if (Recursive)
            {
                foreach (var dir in Directory.GetDirectories(contentFolder))
                {
                    var node = await ScanFolderAsync(root, rootFolder, dir);
                    root.Children.Add(node);
                }
            }

            return root;
        }
    }
}
