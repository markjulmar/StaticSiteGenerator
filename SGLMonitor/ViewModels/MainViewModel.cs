using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MDPGen.Core;
using MDPGen.Core.Data;
using MDPGen.Core.Services;
using XamarinUniversity.Infrastructure;
using MDPGen.Core.Infrastructure;

namespace SGLMonitor.ViewModels
{
    /// <summary>
    /// Main view model which drives the application logic.
    /// </summary>
    public class MainViewModel : SimpleViewModel
    {
        private const int ServerPort = 8080;
        public static readonly string UrlRoot = $"http://localhost:{ServerPort.ToString()}";

        private MarkdownFileViewModel selectedFile;
        private string url;
        private string siteConfigFile;
        private string destinationFolder;
        private FileSystemWatcher watcher;
        private StaticSiteGenerator staticSiteGenerator;
        private ContentPage rootNode;
        private LocalHostServer webServer;
        private readonly SynchronizationContext uiContext;
        private bool isRunningBuild;
        private double currentProgress, maxProgress = 1;
        private TraceType outputFilter;
        private CancellationTokenSource cancelTokenSource;
        private readonly ObservableCollection<Tuple<TraceType,string>> statusText = new ObservableCollection<Tuple<TraceType, string>>();

        /// <summary>
        /// Returns all the files we have or will process.
        /// </summary>
        private IEnumerable<MarkdownFileViewModel> AllFiles => Collapse(Files);

        /// <summary>
        /// Application title
        /// </summary>
        public string AppTitle { get; }

        /// <summary>
        /// Trace type
        /// </summary>
        public TraceType OutputFilter
        {
            get => outputFilter;
            set
            {
                if (SetPropertyValue(ref outputFilter, value) 
                    && statusText.Count > 0)
                {
                    Status.Clear();
                    AddMatchingStatusItems(value, statusText);
                }
            }
        }

        /// <summary>
        /// Delegate to display an error message.
        /// </summary>
        public Action<string> ShowError { get; set; }

        /// <summary>
        /// Delegate to invoke Refresh on WebBrowser control
        /// </summary>
        public Action RefreshBrowser { get; set; }

        /// <summary>
        /// Current progress during build
        /// </summary>
        public double CurrentProgress
        {
            get => currentProgress;
            private set => SetPropertyValue(ref currentProgress, value);
        }

        /// <summary>
        /// Max progress during build
        /// </summary>
        public double MaxProgress
        {
            get => maxProgress;
            private set => SetPropertyValue(ref maxProgress, value);
        }

        /// <summary>
        /// Site configuration file to describe the Markdown content
        /// </summary>
        public string SiteConfigFile
        {
            get => UserSettings.Default.SiteConfigFile;
            set
            {
                if (UserSettings.Default.SiteConfigFile != value)
                {
                    UserSettings.Default.SiteConfigFile = value;
                    RaisePropertyChanged();
                    uiContext.Post(_ => LoadSiteInfo.RaiseCanExecuteChanged(), null);
                }
            }
        }

        /// <summary>
        /// True if we are running a build
        /// </summary>
        public bool IsRunningBuild
        {
            get => isRunningBuild;
            set
            {
                if (SetPropertyValue(ref isRunningBuild, value))
                {
                    uiContext.Post(_ => {
                        LoadSiteInfo.RaiseCanExecuteChanged();
                        CancelBuild.RaiseCanExecuteChanged();
                    }, null);
                }
            }
        }

        /// <summary>
        /// Diagnostic log for app
        /// </summary>
        public ObservableCollection<Tuple<TraceType,string>> Status { get; }

        /// <summary>
        /// Root of the output folder for HTML content
        /// </summary>
        public string DestinationFolder
        {
            get => UserSettings.Default.OutputFolder;
            set
            {
                if (UserSettings.Default.OutputFolder != value)
                {
                    UserSettings.Default.OutputFolder = value;
                    RaisePropertyChanged();
                    uiContext.Post(_ => LoadSiteInfo.RaiseCanExecuteChanged(), null);
                }
            }
        }

        /// <summary>
        /// Command to cancel the build.
        /// </summary>
        public IDelegateCommand CancelBuild { get; }

        /// <summary>
        /// Command to do the processing/load of the SiteConfiguration.
        /// </summary>
        public IAsyncDelegateCommand LoadSiteInfo { get; }

        /// <summary>
        /// The currently selected file in the list of Markdown files.
        /// </summary>
        public MarkdownFileViewModel SelectedFile
        {
            get => selectedFile;
            set
            {
                var oldSelection = selectedFile;
                if (SetPropertyValue(ref selectedFile, value))
                {
                    oldSelection?.OnSelectionChanged();
                    if (selectedFile != null)
                    {
                        Url = UrlRoot + selectedFile.Node.Url;
                        selectedFile.OnSelectionChanged();
                    }
                    else
                        Url = string.Empty;
                }
            }
        }

        /// <summary>
        /// The URL for the currently selected file.
        /// </summary>
        public string Url
        {
            get => url;
            set
            {
                if (SetPropertyValue(ref url, value))
                {
                    if (string.IsNullOrEmpty(url))
                    {
                        SelectedFile = null;
                        return;
                    }

                    url = url.Substring(UrlRoot.Length);
                    SelectedFile = AllFiles.FirstOrDefault(mdf => mdf.Node.Url == url);
                }
            }
        }

        /// <summary>
        /// List of available Markdown files loaded from SiteConfiguration.
        /// </summary>
        public IList<MarkdownFileViewModel> Files { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel()
        {
            uiContext = SynchronizationContext.Current;
            Debug.Assert(uiContext != null);

            Url = string.Empty;
            AppTitle = $"Xamarin University SGL Previewer ({GetType().Assembly.GetName().Version})";

            if (!Enum.TryParse(UserSettings.Default.LogType, out outputFilter))
                outputFilter = TraceType.Normal;

            Status = new ObservableCollection<Tuple<TraceType, string>>();
            Files = new ObservableCollection<MarkdownFileViewModel>();
            LoadSiteInfo = new AsyncDelegateCommand(LoadFiles, CanBuildSite);
            CancelBuild = new DelegateCommand(OnCancelBuild, () => IsRunningBuild);

            TraceLog.OutputHandler += this.OnAddToLog;

            statusText.CollectionChanged += StatusText_CollectionChanged;
        }

        /// <summary>
        /// This method is called when the Status collection has an item added or removed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatusText_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                AddMatchingStatusItems(outputFilter, 
                    e.NewItems.Cast<Tuple<TraceType,string>>());
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Status.Clear();
            }
        }

        /// <summary>
        /// This adds all the status items which match the given filter
        /// </summary>
        /// <param name="filter">Filter to add</param>
        /// <param name="newItems">Items to possibly add</param>
        private void AddMatchingStatusItems(TraceType filter, IEnumerable<Tuple<TraceType, string>> newItems)
        {
            foreach (var item in newItems.Where(t => t.Item1 <= filter))
            {
                Status.Add(item);
            }
        }

        /// <summary>
        /// Called to cancel the current build.
        /// </summary>
        private void OnCancelBuild()
        {
            cancelTokenSource?.Cancel();
        }

        /// <summary>
        /// Determine whether we can build the site.
        /// </summary>
        /// <returns></returns>
        private bool CanBuildSite()
        {
            return !IsRunningBuild
                   && !string.IsNullOrWhiteSpace(SiteConfigFile)
                   && !string.IsNullOrWhiteSpace(DestinationFolder);
        }

        /// <summary>
        /// Loads the SiteConfiguration and populates all our ViewModel data.
        /// </summary>
        /// <returns></returns>
        private async Task LoadFiles()
        {
            string selectedPage = SelectedFile?.Node.Id;

            Reset();

            if (string.IsNullOrEmpty(SiteConfigFile)
                || string.IsNullOrEmpty(DestinationFolder))
                return;

            IsRunningBuild = true;
            // keep a local copy separate from UI.
            siteConfigFile = SiteConfigFile;
            destinationFolder = DestinationFolder;

            try
            {
#if CHECK_OUTPUT
            string startingFolder = destinationFolder;
            // Try to build it several times and compare folders.
            for (int _ncount = 0; _ncount < 10; _ncount++) {
#endif
                staticSiteGenerator = new StaticSiteGenerator();
                staticSiteGenerator.ProgressCallback += OnUpdateProgress;

                cancelTokenSource = new CancellationTokenSource();
                staticSiteGenerator.Initialize(siteConfigFile);
                rootNode = await staticSiteGenerator.BuildSite(destinationFolder, cancelTokenSource.Token);
                if (rootNode == null)
                {
                    throw new TaskCanceledException();
                }

#if CHECK_OUTPUT
                if (_ncount > 0)
                {
                    CompareFolders(startingFolder, destinationFolder);
                }
                destinationFolder = startingFolder + "_" + _ncount.ToString();
            }
            throw new TaskCanceledException();
#endif
                Files.Add(new MarkdownFileViewModel(this, rootNode));

                TraceLog.Write("Starting Web Server");

                if (Files.Count > 0)
                {
                    watcher = new FileSystemWatcher(staticSiteGenerator.ContentFolder)
                    {
                        IncludeSubdirectories = true
                    };
                    watcher.Changed += OnMarkdownFileChanged;
                    watcher.EnableRaisingEvents = true;

                    webServer = new LocalHostServer(destinationFolder);

                    var pageLoader = staticSiteGenerator.PageLoader as BaseContentPageLoader;
                    if (!string.IsNullOrEmpty(pageLoader?.DefaultOutputPageFilename))
                    {
                        string fn = pageLoader.DefaultOutputPageFilename;
                        if (!Path.HasExtension(fn))
                            fn = Path.ChangeExtension(fn, FileExtensions.Html);
                        webServer.DefaultFile = fn;
                    }

                    webServer.Run(ServerPort).IgnoreResult();
                }

                // Restore selection.
                if (selectedPage != null)
                {
                    SelectedFile = Collapse(Files)
                        .FirstOrDefault(f => f.Node.Id == selectedPage);
                }
            }
            catch (TaskCanceledException)
            {
                Reset(false);
                ShowError?.Invoke("Site build cancelled.");
            }
            catch (AggregateException aex)
            {
                Reset(false);
                ShowError?.Invoke(aex.Flatten().InnerExceptions[0].FormatText());
            }
            catch (Exception ex)
            {
                Reset(false);
                ShowError?.Invoke(ex.FormatText());
            }
            finally
            {
                IsRunningBuild = false;
            }
        }

        /// <summary>
        /// This updates the progress bar state based on the currently
        /// processed Markdown files in our site.
        /// </summary>
        /// <param name="current">Last processed count</param>
        /// <param name="max">Max count</param>
        private void OnUpdateProgress(int current, int max)
        {
            if (current == max)
            {
                MaxProgress = 1;
                CurrentProgress = 0;
            }
            else
            {
                MaxProgress = max;
                CurrentProgress = current;
            }
        }

        /// <summary>
        /// Called when the MDPGen engine has a trace event.
        /// </summary>
        /// <param name="traceType">Trace Type</param>
        /// <param name="text">Text</param>
        private void OnAddToLog(TraceType traceType, string text)
        {
            uiContext.Post(s => 
                statusText.Add((Tuple<TraceType,string>)s), 
                Tuple.Create(traceType, text));
        }

        /// <summary>
        /// Collapse the given set of nodes
        /// </summary>
        /// <param name="nodes">Nodes</param>
        /// <returns></returns>
        private static IEnumerable<MarkdownFileViewModel> Collapse(IEnumerable<MarkdownFileViewModel> nodes)
        {
            foreach (var item in nodes)
            {
                yield return item;
                foreach (var child in Collapse(item.Children))
                    yield return child;
            }
        }

        /// <summary>
        /// Reset the UI, turn off the File watcher and Web browser.
        /// </summary>
        private void Reset(bool clearLog = true)
        {
            Url = string.Empty;
            Files.Clear();
            if (clearLog)
                statusText.Clear();
            webServer?.Stop();
            webServer = null;

            watcher?.Dispose();
            watcher = null;
        }

        /// <summary>
        /// Called when a Markdown file changes - this will recreate the HTML from the 
        /// Markdown, and then select it in the WebBrowser.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnMarkdownFileChanged(object sender, FileSystemEventArgs e)
        {
            IsRunningBuild = true;

            watcher.EnableRaisingEvents = false;

            // Wait 2s for the write to finish
            await Task.Delay(2000);

            try
            {
                string file = e.FullPath;
                if (e.ChangeType != WatcherChangeTypes.Changed
                    && e.ChangeType != WatcherChangeTypes.Created)
                {
                    OnAddToLog(TraceType.Normal, $"Ignoring {e.ChangeType} of {e.FullPath} - might require a rebuild.");
                    return;
                }

                // Not a file? ignore.
                if (!File.Exists(e.FullPath))
                    return;

                var node = rootNode.Enumerate().SingleOrDefault(tn => tn.Filename == file);
                if (node != null)
                {
                    OnAddToLog(TraceType.Normal, $"Rendering {file}");
                    try
                    {
                        await staticSiteGenerator.PageLoader.RefreshPageAsync(node);
                        await staticSiteGenerator.GeneratePage(node, destinationFolder);
                    }
                    catch (Exception ex)
                    {
                        OnAddToLog(TraceType.Error, $"{ex.GetType().Name} caught processing {file}: {ex.Message}");
                        return;
                    }
                }
                else if (Array.IndexOf(new[] { FileExtensions.Markdown, FileExtensions.Ignore }, Path.GetExtension(file)) == -1)
                {
                    string contentFolder = staticSiteGenerator.ContentFolder;
                    string partialPath = file.StartsWith(contentFolder)
                        ? file.Substring(contentFolder.Length + 1)
                        : "";

                    if (!string.IsNullOrEmpty(partialPath))
                    {
                        string outputFile = Path.Combine(destinationFolder, partialPath);
                        try
                        {
                            staticSiteGenerator.CopyAsset(file, outputFile, rootNode);
                        }
                        catch (Exception ex)
                        {
                            OnAddToLog(TraceType.Error, $"{ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }

                if (node == null)
                {
                    RefreshBrowser?.Invoke();
                }
                else
                {
                    var mdf = AllFiles.SingleOrDefault(md => md.Node == node);
                    if (mdf != null)
                    {
                        if (SelectedFile == mdf)
                        {
                            RefreshBrowser?.Invoke();
                        }
                        else
                        {
                            SelectedFile = mdf;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnAddToLog(TraceType.Error, $"{ex.GetType().Name} processing {e.FullPath}: ${ex.FormatText()}");
            }
            finally
            {
                watcher.EnableRaisingEvents = true;
                IsRunningBuild = false;
            }
        }


#if CHECK_OUTPUT
        void CompareFolders(string sourceFolder, string destFolder)
        {
            Console.WriteLine($"Comparing {sourceFolder} and {destFolder}");

            var source = new DirectoryInfo(sourceFolder).GetFiles("*.*", SearchOption.AllDirectories);
            var dest = new DirectoryInfo(destFolder).GetFiles("*.*", SearchOption.AllDirectories);
            var fc = new FileCompare();
            if (!source.SequenceEqual(dest, fc))
            {
                foreach (var file in source.Except(dest, fc))
                {
                    Console.WriteLine($"!! DIFFERENCE: {file.FullName}");
                }
                Console.WriteLine("Press any key to continue.");
                Console.ReadLine();
            }
        }

        // We only compare name and length; since the header changes for every file
        // due to the date/time encoding, and the timestamp will be different, we cannot
        // do a full byte-by-byte comparison.
        class FileCompare : IEqualityComparer<FileInfo>
        {
            public bool Equals(FileInfo fileInfo, FileInfo fileInfo2)
            {
                return fileInfo.Name == fileInfo2.Name &&
                        fileInfo.Length == fileInfo2.Length;
            }

            public int GetHashCode(FileInfo fileInfo)
            {
                string s = $"{fileInfo.Name}{fileInfo.Length.ToString()}";
                return s.GetHashCode();
            }
        }
#endif
    }
}
