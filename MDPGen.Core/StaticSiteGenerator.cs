using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;
using MDPGen.Core.Services;
using System.Threading;
using MDPGen.Core.Blocks;
using Newtonsoft.Json.Linq;
using MDPGen.Core.Infrastructure.Metadata;

namespace MDPGen.Core
{
    /// <summary>
    /// The main site generator class which does all the work for the
    /// Command Line and GUI versions of our site creator.
    /// </summary>
    public class StaticSiteGenerator
    {
        bool initialized;

        /// <summary>
        /// Progress callback as we render files.
        /// </summary>
        public event Action<int, int> ProgressCallback;

        /// <summary>
        /// The # of threads to use to generate pages. Defaults
        /// to the # of logical cores.
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// Build symbols added to use with conditionals
        /// </summary>
        public List<string> BuildSymbols { get; } = new List<string>();

        /// <summary>
        /// HTML template file used to render each page
        /// </summary>
        public string DefaultPageTemplate { get; set; }

        /// <summary>
        /// List of folders where we look for include files and templates (e.g. Path)
        /// </summary>
        public List<string> SearchFolders { get; set; }

        /// <summary>
        /// The content folder to build the site from
        /// </summary>
        public string ContentFolder { get; set; }

        /// <summary>
        /// The specific folders to copy to the destination.
        /// </summary>
        public List<string> AssetFoldersToCopy { get; } = new List<string>();

        /// <summary>
        /// This function can be attached to add logic to
        /// the CopyAsset step. It is passed the full filename of
        /// the source and destination files and should return false if
        /// it handles/processes the file and does _not_ want it to be copied.
        /// </summary>
        public event Func<string, string, bool> TransformAsset;

        /// <summary>
        /// Script configuration to use for Roslyn
        /// </summary>
        public ScriptConfig ScriptConfiguration { get; set; }

        /// <summary>
        /// List of assemblies to load extensions from
        /// </summary>
        public List<string> Extensions { get; set; }

        /// <summary>
        /// Constants to load into the page replacement tokens
        /// </summary>
        public List<KeyValuePair<string, string>> Constants { get; set; }

        /// <summary>
        /// Processing chain
        /// </summary>
        public List<IProcessingBlock> ProcessingChain { get; set; }
        
        /// <summary>
        /// Content loader - this is responsible for identifying the 
        /// content pages to process.
        /// </summary>
        public IContentPageLoader PageLoader { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public StaticSiteGenerator()
        {
            RegisterDefaultServices();
        }

        /// <summary>
        /// Called to initialize the static site builder; all the properties
        /// must be set prior to calling this method.
        /// </summary>
        public void Initialize()
        {
            initialized = true;

            TraceLog.Write(TraceType.Diagnostic, $"Using default page template: {DefaultPageTemplate}");
            if (SearchFolders != null)
            {
                TraceLog.Write(TraceType.Diagnostic, $"Include search folders: {string.Join(",", SearchFolders)}");
            }
            else
            {
                TraceLog.Write(TraceType.Warning, "No search folders set.");
            }

            // Replace environment variables in the constants.
            if (Constants?.Count > 0)
            {
                for (int index = 0; index < Constants.Count; index++)
                {
                    var token = Constants[index];

                    // Replace any environment variables.
                    if (Utilities.ReplaceEnvironmentVar(token.Value, out string value))
                    {
                        token = new KeyValuePair<string, string>(token.Key, value);
                        Constants[index] = token;
                    }
                    TraceLog.Write(TraceType.Diagnostic, $"SET CONST {token.Key} = \"{token.Value}\"");
                }
            }

            // Initialize C# scripts
            if (!string.IsNullOrWhiteSpace(ScriptConfiguration?.ScriptsFolder))
            {
                // Get any custom types we use and grab the assembly
                List<Assembly> assemblies = LoadAssembliesByName(ScriptConfiguration.Assemblies) ?? new List<Assembly>();

                // Add any assemblies from override types and services.
                assemblies.AddRange(ServiceFactory.Instance.Select(kvp => kvp.Value.ResolvedType.Assembly));

                // If we have custom extensions, we need to load these too.
                if (Extensions?.Count > 0)
                    assemblies.AddRange(LoadAssembliesByName(Extensions));

                // Add Json.net
                assemblies.Add(typeof(JObject).Assembly);

                var scripts = ExtensionProcessor.InitializeScripts(
                    ScriptConfiguration.ScriptsFolder, 
                    assemblies.Distinct(),
                    ScriptConfiguration.Namespaces);
                TraceLog.Write(TraceType.Diagnostic, $"Located C# scripts in {ScriptConfiguration.ScriptsFolder}: {String.Join(",", scripts)}");
            }

            // Clear loaded extensions and services.
            ExtensionProcessor.Reset();

            // LoadAsync any extensions
            if (Extensions?.Count > 0)
            {
                var extensionAssemblies = Extensions.Select(name => Assembly.Load(new AssemblyName(name)))
                    .Where(a => a != null).ToArray();
                foreach (var asm in extensionAssemblies)
                    TraceLog.Write(TraceType.Diagnostic, $"Loading extension assembly {asm.FullName}");

                // Find any IMarkdownExtension implementations and add them to our
                // collection - these are compiled C# types which can inject features
                // into our Markdown.
                ExtensionProcessor.Init(extensionAssemblies);
            }

            // Create the page loader; we check to see if it's created already since
            // a direct user of the library could instantiate one prior to calling Init.
            if (PageLoader == null)
                PageLoader = ServiceFactory.Instance.Create<IContentPageLoader>();

            // Set MaxThreads if necessary.
            if (MaxThreads < 1)
                MaxThreads = Environment.ProcessorCount;

            TraceLog.Write(TraceType.Diagnostic, $"Configured to use {MaxThreads} threads.");
        }

        /// <summary>
        /// Register all the built-in services. Later code
        /// can replace services as necessary.
        /// </summary>
        internal static void RegisterDefaultServices()
        {
            ServiceFactory.Instance.RegisterServiceType<IIdGenerator, IdGenerator>();
            ServiceFactory.Instance.RegisterServiceType<IMarkdownParser, MarkdownParser>();
            ServiceFactory.Instance.RegisterServiceType<ITokenCollection, ReplacementTokenCollection>();
            ServiceFactory.Instance.RegisterServiceType<IContentPageLoader, FolderContentPageLoader>();
            ServiceFactory.Instance.RegisterServiceType<IPageMetadataLoader, YamlMetadataLoader>();
        }

        /// <summary>
        /// This loads a set of assemblies by name.
        /// </summary>
        /// <param name="assemblies">Name to load</param>
        /// <returns></returns>
        private List<Assembly> LoadAssembliesByName(IEnumerable<string> assemblies) => assemblies?.Select(Assembly.Load).ToList();

        /// <summary>
        /// Build the entire site described by the SiteInfo configuration into the specified output folder.
        /// </summary>
        /// <param name="outFolder">Output folder</param>
        /// <param name="cancelToken">Cancellation support</param>
        /// <returns>Task</returns>
        public async Task<ContentPage> BuildSite(string outFolder, CancellationToken cancelToken)
        {
            // Force a GC up front since we are about to allocate a bunch of memory
            // generating C# syntax trees.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Reset the progress.
            ProgressCallback?.Invoke(0, 0);

            if (string.IsNullOrWhiteSpace(outFolder))
                throw new ArgumentNullException(nameof(outFolder));

            if (!initialized)
                throw new Exception($"Must call {nameof(Initialize)} before {nameof(BuildSite)}.");

            // Get the output folder.
            string outputFolder = Path.GetFullPath(outFolder);
            if (string.Equals(outputFolder, ContentFolder, StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentException("Output folder cannot be the same as the content folder.");

            TraceLog.Write($"Building site. Source={ContentFolder} Destination={outputFolder}");

            // Step1: LoadAsync the pages from the source folder.
            TraceLog.Write("Identifying pages to render");
            var rootNode = await PageLoader.LoadAsync(ContentFolder);
            if (rootNode == null)
            {
                TraceLog.Write(TraceType.Error, "No content pages found / loaded.");
                return null;
            }

            // Step2: Copy over any images, scripts, CSS, etc.
            // This must be complete before we render pages if any
            // script/CSS minify is used since the files must exist.
            if (AssetFoldersToCopy.Count > 0)
            {
                TraceLog.Write("Copying assets");
                await Task.WhenAll(AssetFoldersToCopy.Select(f =>
                {
                    string sourceFolder = Path.GetFullPath(f);
                    return Task.Run(() =>
                    {
                        // Copy contents from the raw curriculum
                        int count = Utilities.RecursiveCopyFolder(
                                            new DirectoryInfo(sourceFolder),
                                            new DirectoryInfo(outputFolder),
                                            (s,d) => ShouldCopyAssetToOutputFolder(s,d,rootNode));
                        TraceLog.Write(TraceType.Diagnostic, $"Copied or replaced {count} asset files");
                    }, cancelToken);

                })).ConfigureAwait(false);
            }

            // Build the processing chain for our content pages.
            var itemsToProcess = rootNode.Enumerate().ToList();
            int totalItems = itemsToProcess.Count;
            int completedItems = 0;

            var block = this.BuildProcessingBlockChain(
                ProcessingChain, cancelToken,
                out Action cleanup,
                ProgressCallback == null
                    ? null : new Action(() =>
                    {
                        int newCount = Interlocked.Increment(ref completedItems);
                        ProgressCallback?.Invoke(newCount, totalItems);
                    }));

            // Step3: Run the processing chain on each ContentPage
            TraceLog.Write($"Rendering {totalItems} pages");
            Task renderTask = Task.Run(() => {
                foreach (var page in itemsToProcess)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    var pageVars = new PageVariables(
                        DefaultPageTemplate,
                        Constants,
                        BuildSymbols,
                        SearchFolders,
                        outputFolder,
                        ScriptConfiguration?.Namespaces,
                        ShouldCopyAssetToOutputFolder);

                    pageVars.InitializeFor(page, ScriptConfiguration?.Hooks?.OnPageInit);
                    block.Post(Tuple.Create(pageVars, (object)page.Content));
                }

                // No more pages.
                block.Complete();
            }, cancelToken);

            // Wait for all the work to be finished.
            try
            {
                await Task.WhenAll(block.Completion, renderTask);
            }
            finally
            {
                cleanup?.Invoke();

                // Force a GC once we are done to drop all the loaded elements
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                ProgressCallback?.Invoke(0, 0);
            }

            return rootNode;
        }

        /// <summary>
        /// Method to copy a single asset file from source to destination
        /// </summary>
        /// <param name="sourceFile">Source file</param>
        /// <param name="destFile">Destination file</param>
        /// <param name="rootNode">Root node to check for source file</param>
        public void CopyAsset(string sourceFile, string destFile, ContentPage rootNode)
        {
            if (ShouldCopyAssetToOutputFolder(sourceFile, destFile, rootNode))
            {
                TraceLog.Write(TraceType.Diagnostic, $"Copying {sourceFile} to {destFile}");
                File.Copy(sourceFile, destFile, true);
            }
        }

        /// <summary>
        /// This is called for each source file to determine
        /// whether it should be copied to the output folder.
        /// </summary>
        /// <param name="sourceFile">Source filename</param>
        /// <param name="destFile">Destination filename</param>
        /// <param name="rootNode">Content page of root to check for file</param>
        /// <returns>True/False to copy file</returns>
        bool ShouldCopyAssetToOutputFolder(string sourceFile, string destFile, ContentPage rootNode)
        {
            // No filename?
            if (sourceFile == null)
                return false;

            // Marked as ".ignore"?
            if (Path.GetExtension(sourceFile).ToLower() == FileExtensions.Ignore)
                return false;

            // Folder metadata file?
            if (PageLoader?.GetType() == typeof(OrderedContentPageLoader)
                && String.CompareOrdinal(Path.GetFileName(sourceFile), 
                    ((OrderedContentPageLoader)PageLoader).DirectoryInfoFilename) == 0)
            {
                return false;
            }

            // Scan our content. If this file is a ContentPage source
            // file then we'll skip it.
            if (rootNode?.Enumerate()
                        .Any(p => String.Compare(p.Filename, sourceFile, StringComparison.OrdinalIgnoreCase) == 0) == true)
                return false;

            // External hook to apply transforms or stop copy.
            return TransformAsset?.Invoke(sourceFile, destFile) != false;
        }

        /// <summary>
        /// Generate a single content page and place it into an output folder.
        /// </summary>
        /// <param name="page">Page</param>
        /// <param name="outputFolder">Folder to put file into</param>
        /// <returns>Task</returns>
        public async Task GeneratePage(ContentPage page, string outputFolder)
        {
            var pageVars = new PageVariables(
                DefaultPageTemplate, 
                Constants, 
                BuildSymbols, 
                SearchFolders,
                outputFolder,
                ScriptConfiguration?.Namespaces);

            var block = this.BuildProcessingBlockChain(
                ProcessingChain, CancellationToken.None, out Action cleanup);

            pageVars.InitializeFor(page, 
                ScriptConfiguration?.Hooks?.OnPageInit);
            block.Post(Tuple.Create(pageVars, (object) page.Content));
            block.Complete();

            await block.Completion;
            cleanup?.Invoke();
        }

        /// <summary>
        /// Create the processing block chain used to generate our files.
        /// </summary>
        /// <param name="blocks">Blocks to create</param>
        /// <param name="cancelToken">Cancellation token</param>
        /// <param name="cleanup">Cleanup callback when we are finished.</param>
        /// <param name="itemFinished">Callback for when a single item has been processed.</param>
        /// <returns>ActionBlock to execute</returns>
        private ActionBlock<Tuple<PageVariables, object>> BuildProcessingBlockChain(
            List<IProcessingBlock> blocks, 
            CancellationToken cancelToken,
            out Action cleanup,
            Action itemFinished = null)
        {
            cleanup = null;

            // Set the default to be minimum.
            if (blocks == null || blocks.Count == 0)
            {
                blocks = new List<IProcessingBlock>
                {
                    new ConvertMarkdownToHtml(),
                    new RenderMustachePageTemplate(),
                    new ReplaceTokens(),
                    new WriteFile()
                };
            }
            else
            {
                blocks = blocks.ToList(); // make a copy
            }

            // Make sure the inputs match to the outputs.
            ValidateActions(blocks);

            // Setup the cleanup.
            cleanup += () => blocks.ForEach(b => b.Shutdown());

            // Initialize the blocks.
            blocks.ForEach(b => b.Initialize());

            // Build the TDF processor.
            return new ActionBlock<Tuple<PageVariables , object>>(t =>
            {
                var pageVars = t.Item1;
                object input = t.Item2;
                foreach (var block in blocks)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    // Note: below nameof() provides string types, but it's just to get the
                    // method name; it doesn't restrict the input/output types.
                    var mi = block.GetType().GetMethod(nameof(IProcessingBlock<string,string>.Process));
                    if (mi != null)
                    {
                        try
                        {
                            input = mi.Invoke(block, new[] { pageVars, input });
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException != null)
                                ex = ex.InnerException;
                            if (ex.GetType() == typeof(SkipProcessingException))
                                break;

                            throw new Exception($"{block.GetType().Name} failed processing {pageVars.Page.RelativeFilename}", ex);
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to find IProcessingBlock.Process method in block {block.GetType().Name}");
                    }
                }

                itemFinished?.Invoke();

            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MaxThreads, CancellationToken = cancelToken });
        }

        /// <summary>
        /// Sanity check to make sure that the created chain can pass parameters down.
        /// </summary>
        /// <param name="actions">List of actions</param>
        private void ValidateActions(IReadOnlyList<object> actions)
        {
            var firstBlock = actions.FirstOrDefault();
            if (firstBlock != null)
            {
                Type blockType = firstBlock.GetType();
                MethodInfo mi = blockType.GetMethod(nameof(IProcessingBlock<string, string>.Process));
                if (mi != null)
                {
                    //Type inputType = mi.GetParameters()[1].ParameterType;
                    Type outputType = mi.ReturnType;

                    for (int i = 1; i < actions.Count; i++)
                    {
                        // Implements our generic interface
                        if (!blockType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IProcessingBlock<,>)))
                        {
                            throw new Exception($"{blockType.Name} does not implement IProcessingBlock.");
                        }

                        Type checkType = actions[i].GetType();
                        mi = checkType.GetMethod(nameof(IProcessingBlock<string, string>.Process));
                        if (mi != null)
                        {
                            Type checkInputType = mi.GetParameters()[1].ParameterType;
                            if (outputType != checkInputType
                                && !checkInputType.IsAssignableFrom(outputType))
                            {
                                throw new Exception($"Cannot cast output from {blockType.Name} - {outputType.Name} to {checkType.Name} parameter {checkInputType.Name}");
                            }

                            blockType = checkType;
                            outputType = mi.ReturnType;
                        }
                        else
                        {
                            throw new Exception($"Failed to find IProcessingBlock.Process method in block {checkType.Name}");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Failed to find IProcessingBlock.Process method in block {blockType.Name}");
                }
            }
        }
    }
}
