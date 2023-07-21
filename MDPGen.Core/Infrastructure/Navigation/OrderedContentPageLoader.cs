using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MDPGen.Core.Data;
using Newtonsoft.Json;
using MDPGen.Core.Services;
using System.Threading.Tasks;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// The OrderedContentPageLoader is responsible for identifying and loading
    /// each of the ContentPage objects which represent the pages in our 
    /// generated site.
    /// It can be replaced during the build by supplying an
    /// implementation of IContentPageLoader to the
    /// StaticSiteGenerator.
    /// </summary>
    public class OrderedContentPageLoader : BaseContentPageLoader
    {
        private string rootContentFolder;

        // Files we look for as the "default" URL mapped file
        // E.g. What "/folder/" maps to in a GET request.
        private readonly List<string> defaultFiles = new List<string> {"default", "index"};

        /// <summary>
        /// This is the file we use to identify the specific files
        /// we are going to load. It is a JSON file containing a string array
        /// of file and folder names. Change this value prior to calling
        /// LoadAsync to use a different filename.
        /// </summary>
        public string DirectoryInfoFilename { get; set; } = "meta.json";

        /// <summary>
        /// Method to load a tree from a set of folders.
        /// </summary>
        /// <param name="contentFolder">starting folder to scan for meta.json</param>
        public override async Task<ContentPage> LoadAsync(string contentFolder)
        {
            if (contentFolder == null)
                throw new ArgumentNullException(nameof(contentFolder));

            if (!Directory.Exists(contentFolder))
                throw new ArgumentException("Folder does not exist.", nameof(contentFolder));

            // Create a metadata loader if it's not assigned yet.
            if (MetadataLoader == null)
                MetadataLoader = ServiceFactory.Instance.Create<IPageMetadataLoader>();

            rootContentFolder = contentFolder;

            // Load the data.
            OnPreLoad(contentFolder);
            var rootNode = await ScanFolderAsync(contentFolder);
            OnPostLoad(contentFolder, rootNode);

            return rootNode;
        }

        /// <summary>
        /// Scans a folder for a meta.json file
        /// </summary>
        /// <param name="folder">Folder name</param>
        /// <returns>TreeNode with children</returns>
        private async Task<ContentPage> ScanFolderAsync(string folder)
        {
            string relativeFolder = folder.Substring(rootContentFolder.Length);
            ContentPage node = null;

            var filename = Path.Combine(folder, DirectoryInfoFilename);
            if (File.Exists(filename))
            {
                try
                {
                    // Get the list of files.
                    string[] folderInfo = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(filename));
                    Debug.Assert(folderInfo != null && folderInfo.Length > 0);

                    // Create our page encapsulating the folder.
                    node = new ContentPage
                    {
                        IsDefaultPage = true,
                        Url = Constants.WebSeparator.UrlCombine(Utilities.GenerateRelativeUrlFromFolder(relativeFolder)) + Constants.WebSeparator,
                    };

                    // Get all the nodes and then load any additional information.
                    await AddNodesAsync(folder, folderInfo, node);

                    // First entry in the list was a folder, not a file.
                    // .. and no default/index was found.
                    if (node.Filename == null)
                    {
                        TraceLog.Write(TraceType.Warning, $"{folder} does not contain a default file.");
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.Data.Contains("caught"))
                        TraceLog.Write(TraceType.Error, $"Error parsing {DirectoryInfoFilename} in {folder}: {ex.Message} {ex.InnerException?.Message}");
                    throw;
                }
            }

            return node;
        }

        /// <summary>
        /// Add child nodes to a folder based on a parent tree node.
        /// </summary>
        /// <param name="folder">Folder</param>
        /// <param name="entries">Generated info about folder</param>
        /// <param name="parentNode">Node representing parent folder</param>
        async Task AddNodesAsync(string folder, string[] entries, ContentPage parentNode)
        {
            // See if we have a default file first. This is always
            // taken as the content for the parent folder node.
            int defaultIndex = Array.FindIndex(entries,
                n => defaultFiles.Contains(Path.GetFileNameWithoutExtension(n)?.ToLowerInvariant()));
            if (defaultIndex >= 0)
            {
                await ProcessSingleNodeAsync(folder, parentNode, entries[defaultIndex], true);
            }

            // Process the remainder of the files.
            for (int index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                if (String.IsNullOrWhiteSpace(entry) || index == defaultIndex)
                    continue;

                await ProcessSingleNodeAsync(folder, parentNode, entry);
            }

            // Did we ever catch the root node?
            if (parentNode.Filename == null)
            {
                // See if the default file actually _does_ exist.
                string fn = defaultFiles.Select(f => IdentifyFile(Path.Combine(folder, f)))
                    .FirstOrDefault(File.Exists);

                if (fn != null)
                {
                    string filename = Path.GetFileName(fn);
                    TraceLog.Write(TraceType.Warning, $"{filename} entry is missing from {DirectoryInfoFilename} for {folder} even though file exists.");
                    await ProcessSingleNodeAsync(folder, parentNode, filename, true);
                }
            }
        }

        /// <summary>
        /// Called to process a single node.
        /// </summary>
        /// <param name="folder">Folder we are working on</param>
        /// <param name="parentNode">Parent node</param>
        /// <param name="entry">Specific entry</param>
        /// <param name="isDefault">True if this is the parent folder entry</param>
        protected virtual async Task ProcessSingleNodeAsync(string folder, ContentPage parentNode, string entry, bool isDefault = false)
        {
            string relativePath = folder.Substring(rootContentFolder.Length);
            string fullPath = Path.Combine(folder, entry);
            ContentPage node;

            // If the entry is not a folder, then assume it's a filename.
            // We will determine the exact file later.
            if (!Directory.Exists(fullPath))
            {
                if (isDefault)
                {
                    // Fill in the folder.
                    node = parentNode;
                    string fn = Path.GetFileNameWithoutExtension(fullPath);
                    if (defaultFiles.All(f => string.Compare(fn, f, StringComparison.OrdinalIgnoreCase)!=0))
                    {
                        // Filename isn't default.md.
                        node.Url = Constants.WebSeparator.UrlCombine(Utilities.GenerateRelativeUrlFromFolder(relativePath), entry);
                        TraceLog.Write(TraceType.Diagnostic, $"Folder '{folder}' root file is {Path.GetFileName(node.Filename)}.");
                    }
                }
                else
                {
                    // Create a single page
                    node = new ContentPage
                    {
                        Parent = parentNode,
                        Url = Constants.WebSeparator.UrlCombine(Utilities.GenerateRelativeUrlFromFolder(relativePath), entry),
                    };
                }

                // Set the filenames
                DetermineFilenamesAndContentType(node, rootContentFolder, folder, entry);

                // This page loader expects the file to exist.
                if (!string.IsNullOrWhiteSpace(node.Filename)
                    && !File.Exists(node.Filename))
                {
                    TraceLog.Write(TraceType.Error, $"File not found: {node.RelativeFilename}");
                    node.Filename = node.RelativeFilename = node.RelativeOutputFilename = string.Empty;
                }

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

            // Entry is a folder. Capture all the content within that folder.
            // We expect a child metajson file to be present.
            else
            {
                node = await ScanFolderAsync(fullPath);
                if (node != null)
                    node.Parent = parentNode;
            }

            // Add it to the parent list
            if (node != null && node != parentNode)
                parentNode.Children.Add(node);
        }
    }
}