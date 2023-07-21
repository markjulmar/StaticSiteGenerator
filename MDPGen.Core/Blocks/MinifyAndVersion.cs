using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MDPGen.Core.Data;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;
using Microsoft.Ajax.Utilities;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// Run the Minify and Version process on all our link and script tags.
    /// </summary>
    public class MinifyAndVersion : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// True to minify CSS files
        /// </summary>
        public bool MinifyCss { get; set; } = true;
        /// <summary>
        /// True to minify JS files
        /// </summary>
        public bool MinifyJs { get; set; } = true;
        /// <summary>
        /// True to version CSS files
        /// </summary>
        public bool VersionCss { get; set; } = true;
        /// <summary>
        /// True to version JS files.
        /// </summary>
        public bool VersionJs { get; set; } = true;


        // These values are used _cross-thread_ to stop TDF from processing the same files.
        private static readonly object WriteGuardCss = new object();
        private static readonly object WriteGuardJs = new object();

        /// <summary>
        /// Process the minify/versioning logic
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">HTML file</param>
        /// <returns>HTML file</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            var reLinks = new Regex("<link[^>]* href=\"([^ \"]*)\"");
            var reScripts = new Regex("<script[^>]* src=\"([^ \"]*)\"");

            var builder = new StringBuilder();
            using (var reader = new LineReader(input))
            {
                while (!reader.IsEof)
                {
                    string line = reader.ReadLine();
                    if (line == null) continue;
                    Match match = reLinks.Match(line);
                    if (!match.Success)
                        match = reScripts.Match(line);

                    if (match.Success)
                    {
                        string url = match.Groups[1].ToString();
                        // Check if "https://..." external URL.
                        bool isRemoteUrl = url.StartsWith("https://", ignoreCase: true, culture: System.Globalization.CultureInfo.InvariantCulture);
                        // Check if "http://..." external URL.
                        isRemoteUrl = isRemoteUrl || url.StartsWith("http://", ignoreCase: true, culture: System.Globalization.CultureInfo.InvariantCulture);
                        // Check if protocol-less "//..." external URL.
                        isRemoteUrl = isRemoteUrl || url.StartsWith("//", ignoreCase: true, culture: System.Globalization.CultureInfo.InvariantCulture);
                        // Don't do any processing of external files.
                        if (!isRemoteUrl)
                        {
                            string newUrl = MinifyUrl(url, pageVars.OutputFolder);
                            if (!string.IsNullOrWhiteSpace(newUrl))
                                line = line.Replace(url, newUrl);
                        }
                    }
                    builder.AppendLine(line);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Minify the given URL
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="outputFolder">Folder to store minified file</param>
        /// <returns>New filename</returns>
        private string MinifyUrl(string url, string outputFolder)
        {
            string str = Utilities.LocateFilePortionFromUrl(url);
            if (str != null)
            {
                char[] trimChars = { '/' };
                string fn = Path.Combine(outputFolder, str.TrimStart(trimChars).Replace('/', Path.DirectorySeparatorChar));
                try
                {
                    if (url.EndsWith(FileExtensions.Css) && !url.EndsWith(FileExtensions.CssMin))
                    {
                        if (MinifyCss)
                        {
                            lock (WriteGuardCss)
                            {
                                RunMinifyOnFile(fn);
                            }
                            url = Path.ChangeExtension(url, FileExtensions.CssMin);
                        }
                    }
                    else if (url.EndsWith(FileExtensions.Js) && !url.EndsWith(FileExtensions.JsMin))
                    {
                        if (MinifyJs)
                        {
                            lock (WriteGuardJs)
                            {
                                RunMinifyOnFile(fn);
                            }
                            url = Path.ChangeExtension(url, FileExtensions.JsMin);
                        }
                    }
                }
                catch (Exception exception)
                {
                    TraceLog.Write(TraceType.Error, exception.Message);
                }

                // Version the file by adding a querystring parameter to it.
                // We use the MD5 hash of the file itself which changes when the
                // contents of the file is altered.
                bool isJs = url.EndsWith(FileExtensions.Js) || url.EndsWith(FileExtensions.JsMin);
                bool isCss = !isJs && (url.EndsWith(FileExtensions.Css) || url.EndsWith(FileExtensions.CssMin));
                if (isJs && VersionJs
                    || isCss && VersionCss)
                {
                    string fileVersion = Utilities.GetFileVersion(fn);
                    if (!string.IsNullOrEmpty(fileVersion))
                    {
                        url = url + "?" + fileVersion;
                    }
                }
            }
            return url;
        }

        /// <summary>
        /// Runs the minify process on the given file and compresses it.
        /// </summary>
        /// <param name="fn">Filename</param>
        private static void RunMinifyOnFile(string fn)
        {
            bool isCss = Path.GetExtension(fn) == FileExtensions.Css;
            string target = Path.ChangeExtension(fn, isCss ? FileExtensions.CssMin : FileExtensions.JsMin);

            if (Utilities.IsSourceFileNewerThanTarget(fn, target))
            {
                string sourceInput;
                using (StreamReader reader = new StreamReader(File.OpenRead(fn)))
                {
                    sourceInput = reader.ReadToEnd();
                }
                if (!string.IsNullOrEmpty(sourceInput))
                {
                    Minifier minifier = new Minifier();
                    string output = isCss ? minifier.MinifyStyleSheet(sourceInput) : minifier.MinifyJavaScript(sourceInput);
                    if (minifier.Errors.Count > 0)
                    {
                        throw new Exception($"Failed to minify {fn}: {minifier.Errors.First()}");
                    }

                    using (StreamWriter writer = new StreamWriter(File.OpenWrite(target)))
                    {
                        writer.Write(output);
                    }
                }
            }
        }
    }
}

