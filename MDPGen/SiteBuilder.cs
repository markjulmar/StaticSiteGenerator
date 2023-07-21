using CommandLine;
using MDPGen.Core;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MDPGen
{
    /// <summary>
    /// Simple wrapper to build a site.
    /// </summary>
    public class SiteBuilder
    {
        private string outputFolder;
        private string siteConfiguration;
        private StaticSiteGenerator siteGenerator;
        private ConsoleProgressBar progress;

        public int Run(string[] args)
        {
            // Parse the command line
            CommandLineOptions options = null;
            Parser.Default.ParseArguments<CommandLineOptions>(
                args.Length > 0 ? args : new[] { "--help" })
                  .WithParsed(clo => options = clo);
            if (options == null)
                return 1; // bad arguments or help.

            Console.WriteLine("MDPGen " + typeof(Program).GetTypeInfo()
                                .Assembly.GetName().Version);
            Console.WriteLine($"Copyright (C) {DateTime.Now.Year} Microsoft, Xamarin\r\n");

            // Set the verbose flag for diagnostic output.
            Program.VerboseLog = options.Verbose;

            // Step: Initialize creates a new site.
            if (options.Initialize)
            {
                CreateNewSite();
                return 0;
            }

            // Otherwise we are site building.
            Stopwatch sw = Stopwatch.StartNew();
            siteGenerator = new StaticSiteGenerator();
            siteGenerator.ProgressCallback += OnUpdateProgress;

            // Set max threads.
            if (options.MaxThreads > 0)
                siteGenerator.MaxThreads = options.MaxThreads;

            // Set build symbols
            siteGenerator.BuildSymbols.AddRange(options.BuildSymbols);

            // Step 2: Get the site configuration
            siteConfiguration = Path.GetFullPath(options.SiteConfigFile);
            if (siteConfiguration != null)
            {
                try
                {
                    siteGenerator.Initialize(siteConfiguration);
                }
                catch (Exception ex)
                {
                    TraceLog.Write(TraceType.Error, ex.FormatText());
                    return 3; // Failed to initialize
                }
            }

            // Assign an output folder.
            outputFolder = options.OutputFolder ?? "output";

            // Step 3: build the site.
            try
            {
                // Show a mini-progress bar when we are in minimal
                // text and we have an output window.
                if (!options.Verbose && !Console.IsOutputRedirected)
                    progress = new ConsoleProgressBar();

                var tokenSource = new CancellationTokenSource();
                var task = siteGenerator.BuildSite(outputFolder, tokenSource.Token);

                // Allow cancellation if we have a console.
                if (!Console.IsInputRedirected)
                {
                    while (!task.IsCompleted
                        && !task.IsCanceled
                        && !task.IsFaulted)
                    {
                        if (Console.KeyAvailable)
                            tokenSource.Cancel();
                        Thread.Sleep(1);
                    }
                }

                // Consume the result.
                task.Wait(tokenSource.Token);
            }
            catch (AggregateException aex)
            {
                var ex = aex.Flatten().InnerException;
                TraceLog.Write(TraceType.Error, "\r\n" + ex.FormatText());
                return 4; // Failed to build site.
            }
            catch (Exception ex)
            {
                TraceLog.Write(TraceType.Error, "\r\n" + ex.FormatText());
                return 4; // Failed to build site.
            }
            finally
            {
                Console.WriteLine();
            }

            sw.Stop();
            Console.WriteLine($"Finished building site in {sw.Elapsed}");

            return 0; // OK
        }

        /// <summary>
        /// Update the console-based progress bar.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
        private void OnUpdateProgress(int current, int max)
        {
            if (max > 0)
            {
                progress?.Update((int)Math.Round(100.0 * current / max));
            }
        }

        /// <summary>
        /// Method to generate a blank sample site
        /// </summary>
        static void CreateNewSite()
        {
            const string contentFolder = "content";
            const string template = ".Template";

            Console.WriteLine("Generating new site.");
            Directory.CreateDirectory(contentFolder);

            var asm = Assembly.GetExecutingAssembly();
            foreach (var item in asm.GetManifestResourceNames()
                .Where(n => n.Contains(template)))
            {
                string fn = item.Substring(item.IndexOf(template, StringComparison.Ordinal) + template.Length + 1);
                if (fn == "default.md")
                    fn = Path.Combine(contentFolder, fn);
                using (Stream input = asm.GetManifestResourceStream(item))
                using (Stream output = File.OpenWrite(fn))
                {
                    input?.CopyTo(output);
                }
            }
        }
    }
}