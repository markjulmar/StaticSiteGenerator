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
    /// Base class for content page loaders with some common
    /// functionality that should be present for most, if not all of them.
    /// </summary>
    public abstract class BaseContentPageLoader : IContentPageLoader
    {
        /// <summary>
        /// The loader used to grab metadata for each page.
        /// Can be set to NullMetadataLoader to ignore metadata.
        /// </summary>
        public IPageMetadataLoader MetadataLoader { get; set; }

        /// <summary>
        /// Set the default page output filename - e.g. default.html
        /// </summary>
        public string DefaultOutputPageFilename { get; set; }

        /// <summary>
        /// Hook for doing pre-load validation
        /// </summary>
        /// <param name="contentFolder">Content folder</param>
        protected virtual void OnPreLoad(string contentFolder)
        {
        }

        /// <summary>
        /// This method adds the Prev/Next links 
        /// </summary>
        /// <param name="rootNode"></param>
        protected void ChainNodesTogether(ContentPage rootNode)
        {
            if (rootNode != null)
            {
                rootNode.NextPage = rootNode.Children.FirstOrDefault();

                ContentPage prevPage = null;
                foreach (var child in rootNode.Enumerate().Where(n => n != rootNode))
                {
                    child.PreviousPage = prevPage ?? rootNode;
                    if (prevPage != null)
                        prevPage.NextPage = child;

                    prevPage = child;
                }
            }
        }

        /// <summary>
        /// Hook for doing validation
        /// </summary>
        /// <param name="contentFolder">Content folder we loaded.</param>
        /// <param name="root">Root node created</param>
        protected virtual void OnPostLoad(string contentFolder, ContentPage root)
        {
            // Chain the nodes together.
            ChainNodesTogether(root);

            // Validate the nodes
            ValidateNodes(root);
        }

        /// <summary>
        /// Validate the nodes - check for valid IDs.
        /// </summary>
        /// <param name="root">Root node</param>
        protected virtual void ValidateNodes(ContentPage root)
        { 
            var uniqueIdCheck = new HashSet<string>();
            var ignore = new HashSet<string>();
            var nodes = root.Enumerate()
                .Where(p => p.Filename != null).ToList();

            // Check for unique ids on every node.
            foreach (var page in nodes.Where(n => !String.IsNullOrWhiteSpace(n.Id)))
            {
                string id = page.Id;
                if (!uniqueIdCheck.Add(id) && ignore.Add(id))
                {
                    var allPages = nodes.Where(n => n.Id == id)
                        .Select(n => n.RelativeFilename).ToList();
                    if (allPages.Distinct().Count() != allPages.Count)
                    {
                        // If it's the same page (repeated) then it's
                        // considered an error.
                        var dups = allPages.GroupBy(i => i)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key);
                        throw new Exception($"Page {String.Join(",", dups)} has multiple entries.");
                    }

                    TraceLog.Write(TraceType.Warning, $"Id {id} used in multiple pages: {String.Join(", ", allPages)}");
                }
            }
        }

        /// <summary>
        /// LoadAsync the content pages from the given folder.
        /// </summary>
        /// <param name="contentFolder">Folder to start at</param>
        /// <returns>Loaded content page with all children</returns>
        public abstract Task<ContentPage> LoadAsync(string contentFolder);

        /// <summary>
        /// Reloads a single content page.
        /// </summary>
        /// <param name="page">Page to reload</param>
        async Task IContentPageLoader.RefreshPageAsync(ContentPage page)
        {
            try
            {
                await LoadContentForNodeAsync(page);
            }
            catch (Exception ex)
            {
                TraceLog.Write(TraceType.Error, $"{ex.GetType().Name} caught refreshing {page.Filename} {ex.Message} {ex.InnerException?.Message}");
                ex.Data["caught"] = true;
                throw;
            }
        }

        /// <summary>
        /// Identify the source file and append an extension.
        /// </summary>
        /// <param name="filename">Filename with no extension.</param>
        /// <returns>File on disk with extension</returns>
        protected virtual string IdentifyFile(string filename)
        {
            string[] extensionsToLookFor =
            {
                FileExtensions.Markdown,
                ".markdown",
                FileExtensions.Html,
                ".htm",
                ".aspx",
            };

            filename = Path.GetFullPath(filename);
            if (!Path.HasExtension(filename))
            {
                foreach (var ext in extensionsToLookFor)
                {
                    var fn = Path.ChangeExtension(filename, ext);
                    if (File.Exists(fn)) return fn;
                }
            }

            return filename;
        }

        /// <summary>
        /// This method determines the filenames
        /// </summary>
        /// <param name="node">Node to fill in</param>
        /// <param name="rootFolder">Root content folder</param>
        /// <param name="contentFolder">Folder where this file is located</param>
        /// <param name="entry">Filename with or without extension</param>
        protected virtual void DetermineFilenamesAndContentType(ContentPage node, 
            string rootFolder, string contentFolder, string entry)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (rootFolder == null) throw new ArgumentNullException(nameof(rootFolder));
            if (contentFolder == null) throw new ArgumentNullException(nameof(contentFolder));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            string relativeFolder = contentFolder.Substring(rootFolder.Length);
            if (contentFolder != rootFolder 
                && relativeFolder.StartsWith("\\"))
                relativeFolder = relativeFolder.Substring(1);

            string fullPath = Path.Combine(contentFolder, entry);
            // This is the full path + filename + extension
            node.Filename = IdentifyFile(fullPath);
            // This is the relative path + filename + extension
            node.RelativeFilename = Utilities.CreateNormalizedFilename(relativeFolder, Path.GetFileName(node.Filename));

            // This is the relative path + filename + HTML
            string currentExtension = Path.GetExtension(node.RelativeFilename);

            // If we've overridden the default output filename, then apply it
            // now - by default the content page loader will use whatever is the first
            // filename in the folder metadata.
            if (node.IsDefaultPage
                && !string.IsNullOrWhiteSpace(DefaultOutputPageFilename))
            {
                string folder = Path.GetDirectoryName(node.RelativeFilename) ?? "";
                string fn = Path.Combine(folder, DefaultOutputPageFilename);
                if (!Path.HasExtension(fn))
                {
                    fn = Path.ChangeExtension(fn, String.Compare(currentExtension, FileExtensions.Markdown, StringComparison.OrdinalIgnoreCase) == 0
                        ? FileExtensions.Html
                        : currentExtension);
                }
                node.RelativeOutputFilename = fn;
            }
            else
            {
                node.RelativeOutputFilename = String.Compare(currentExtension, FileExtensions.Markdown, StringComparison.OrdinalIgnoreCase) == 0
                    ? Path.ChangeExtension(node.RelativeFilename, FileExtensions.Html)
                    : node.RelativeFilename;
            }

            // Content type is the extension
            string extension = Path.GetExtension(node.Filename)?.ToLower();
            if (extension == null)
                node.ContentType = ContentType.Unknown;
            else
            {
                switch (extension)
                {
                    case FileExtensions.Markdown:
                    case ".markdown":
                        node.ContentType = ContentType.Markdown;
                        break;
                    case FileExtensions.Html:
                    case ".htm":
                        node.ContentType = ContentType.Html;
                        break;
                    case ".aspx":
                        node.ContentType = ContentType.Aspx;
                        break;
                    default:
                        node.ContentType = ContentType.Unknown;
                        break;
                }
            }

            // Emit a warning if we didn't know the content type.
            if (!string.IsNullOrWhiteSpace(extension)
                && node.ContentType == ContentType.Unknown)
            {
                TraceLog.Write(TraceType.Warning, $"Unable to identify content type for entry \"{fullPath}; possible file/folder name mismatch.");
            }
        }

        /// <summary>
        /// This identifies the file and loads the content from the file.
        /// </summary>
        /// <param name="node">Node representing this content page</param>
        public virtual async Task LoadContentForNodeAsync(ContentPage node)
        {
            // Load the content if it's a file.
            if (!String.IsNullOrWhiteSpace(node.Filename)
                && File.Exists(node.Filename))
            {
                // Load the file.
                using (var reader = new StreamReader(node.Filename))
                {
                    node.Content = await reader.ReadToEndAsync();
                }
            }

            // Use the metadata loader.
            DocumentMetadata md = MetadataLoader?.Load(node);
            node.SetMetadata(md);

            // Generate unique id if not set.
            if (String.IsNullOrWhiteSpace(node.Id))
                node.Id = Path.GetFileName(node.Filename)?.ToLower();

            // Generate title if not present
            if (String.IsNullOrWhiteSpace(node.Title))
                node.Title = Utilities.TitleFromFilename(Path.GetFileNameWithoutExtension(node.Filename));
        }
    }
}
