using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MDPGen.Core.Data;
using MDPGen.Core.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Dynamic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// Utility functions
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Versions we have added to files.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> VersionRefs =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This converts a Dictionary of KVPs to an ExpandoObject.
        /// Note: this is a shallow copy of direct values (e.g. reference types)
        /// </summary>
        /// <param name="dictionary">Input Dictionary</param>
        /// <returns>ExpandoObject</returns>
        public static ExpandoObject ToExpandoObject(this IDictionary<string,object> dictionary)
        {
            var result = new ExpandoObject();
            if (dictionary != null)
            {
                var target = (IDictionary<string, object>)result;
                foreach (var item in dictionary)
                {
                    target[item.Key] = item.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// Calculate the MD5 hash for a given file.
        /// </summary>
        /// <param name="fn">File</param>
        /// <returns>MD5 hash for file</returns>
        public static string CalculateFileHash(string fn)
        {
            string str;
            using (MD5 md = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(fn))
                {
                    str = BitConverter.ToString(md.ComputeHash(stream)).Replace("-", "‌​").ToLower();
                }
            }
            return str;
        }

        /// <summary>
        /// Locate a folder along a given path
        /// </summary>
        /// <param name="searchPath">List of folders to search</param>
        /// <param name="folder">Folder to search for</param>
        /// <returns></returns>
        public static string FindFolderAlongPath(IEnumerable<string> searchPath, string folder)
        {
            var directoryName = Path.GetDirectoryName(folder);
            if (directoryName == null)
            {
                throw new ArgumentNullException(nameof(folder));
            }

            if (directoryName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new ArgumentException($"Invalid folder name detected: {folder}", nameof(folder));
            }

            if (Directory.Exists(folder))
                return folder;

            if (searchPath != null)
            {
                foreach (var dir in searchPath)
                {
                    // Skip invalid folders.
                    if (dir.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                    {
                        if (String.Compare(Path.GetFileName(dir), folder, StringComparison.OrdinalIgnoreCase) == 0
                            && Directory.Exists(dir))
                            return dir;

                        string dirName = Path.Combine(dir, folder);
                        if (Directory.Exists(dirName))
                            return dirName;
                    }
                    else
                    {
                        TraceLog.Write(TraceType.Error, $"Invalid path specified to FindFolderAlongPath: {dir}.");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Locate a file along a given path
        /// </summary>
        /// <param name="searchPath">List of folders to search</param>
        /// <param name="filename">Filename to search for</param>
        /// <returns></returns>
        public static string FindFileAlongPath(IEnumerable<string> searchPath, string filename)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            string directoryName = Path.GetDirectoryName(filename) ?? "";
            string file = Path.GetFileName(filename);

            if (directoryName.IndexOfAny(Path.GetInvalidPathChars()) >= 0
                || file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"Invalid filename detected: {filename}", nameof(filename));
            }

            if (File.Exists(filename))
                return filename;

            if (searchPath != null)
            {
                foreach (var folder in searchPath)
                {
                    // Skip invalid folders.
                    if (folder.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                    {
                        string fn = Path.Combine(folder, filename);
                        if (File.Exists(fn))
                            return fn;
                    }
                    else
                    {
                        TraceLog.Write(TraceType.Error, $"Invalid path specified to FindFileAlongPath: {folder}.");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Create a relative URL from a folder
        /// </summary>
        /// <param name="folder">Folder name</param>
        /// <returns>URL normalized folder name</returns>
        public static string GenerateRelativeUrlFromFolder(string folder)
        {
            folder = folder.Replace("\\", "/");
            if (folder.StartsWith("./", StringComparison.Ordinal))
                folder = folder.Substring(1);
            else if (folder.Length >= 2 && folder[1] == ':')
                folder = folder.Substring(2);
            if (!folder.StartsWith("/", StringComparison.Ordinal))
                folder = "/" + folder;
            if (!folder.EndsWith("/", StringComparison.Ordinal))
                folder += "/";

            return folder;
        }

        /// <summary>
        /// Combines two paths and normalizes the path _without_ a FQN
        /// </summary>
        /// <param name="folder">Folder</param>
        /// <param name="file">File</param>
        /// <returns>Combined path</returns>
        public static string CreateNormalizedFilename(string folder, string file)
        {
            // Combine the path and remove any prefix. We want relative paths.
            file = Path.Combine(folder, file);

            // Get the API to convert separators for us.
            file = Path.Combine(Path.GetDirectoryName(file) ?? "", Path.GetFileName(file));

            // Remove the relative path (./)
            if (file.StartsWith($".{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                file = file.Substring(2);

            return file;
        }

        /// <summary>
        /// Change a relative path into a complete rooted path
        /// </summary>
        /// <param name="path">Path to change</param>
        /// <param name="root">Root path to add</param>
        /// <returns>New rooted path</returns>
        public static string FixupRelativePaths(string path, string root)
        {
            // Root the path
            if (!Path.IsPathRooted(path))
                path = Path.Combine(root, path);
            // Fix any incorrect slashes
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Assign a file version through a URL MD5 hash addition.
        /// </summary>
        /// <param name="filename">Filename to version</param>
        /// <returns>Version assigned</returns>
        public static string GetFileVersion(string filename)
        {
            string str;
            if (!VersionRefs.TryGetValue(filename, out str) && File.Exists(filename)) {
                str = CalculateFileHash(filename);
                VersionRefs.TryAdd(filename, str);
            }
            return str;
        }

        /// <summary>
        /// Return whether the first file is newer in date/time than the second.
        /// </summary>
        /// <param name="src">Source file</param>
        /// <param name="target">Target file</param>
        /// <returns>True if the source file is newer than the target</returns>
        public static bool IsSourceFileNewerThanTarget(string src, string target)
        {
            var info = new FileInfo(src);
            var info2 = new FileInfo(target);
            return info.Exists && (!info2.Exists || info.LastWriteTimeUtc > info2.LastWriteTimeUtc);
        }

        /// <summary>
        /// Retrieve the filename from a URL
        /// </summary>
        /// <param name="url">URL</param>
        /// <returns>Filename portion split out from path</returns>
        public static string LocateFilePortionFromUrl(string url)
        {
            // Ignore CDNs
            if (url.StartsWith("//"))
                return null;

            int pos = url.IndexOf("://", StringComparison.Ordinal);
            if (pos >= 0)
                pos += 3;
            else
                pos = 0;

            pos = url.IndexOf("/", pos, StringComparison.Ordinal);
            return pos > 0 ? url.Substring(pos) : url;
        }

        /// <summary>
        /// Copy a folder and all files/subfolders.
        /// </summary>
        /// <param name="source">Source folder</param>
        /// <param name="target">Target folder</param>
        /// <param name="filter">Opional filter</param>
        public static int RecursiveCopyFolder(DirectoryInfo source, DirectoryInfo target,
            Func<string, string, bool> filter = null)
        {
            if (!source.Exists)
            {
                TraceLog.Write(TraceType.Warning, $"{source.Name} asset folder does not exist.");
                return 0;
            }

            // Use an array so this is a reference type
            // and therefore on the heap; that way we can access
            // it safely in the closure below.
            int[] count = {0};

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            Parallel.ForEach(source.GetFiles(), fi =>
            {
                string tgFilename = Path.Combine(target.FullName, fi.Name);

                // Skip the file if the filter says not to copy it.
                if ((filter == null || filter.Invoke(fi.FullName, tgFilename))
                    && IsSourceFileNewerThanTarget(fi.FullName, tgFilename))
                {
                    TraceLog.Write(TraceType.Diagnostic, $"Copying {target.FullName}/{fi.Name}");
                    fi.CopyTo(tgFilename, true);
                    Interlocked.Increment(ref count[0]);
                }
            });

            // Copy each subdirectory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = new DirectoryInfo(Path.Combine(target.FullName, diSourceSubDir.Name));
                int add = RecursiveCopyFolder(diSourceSubDir, nextTargetSubDir, filter);
                Interlocked.Add(ref count[0], add);
            }

            return count[0];
        }

        /// <summary>
        /// Replaces an environment variable with the actual value.
        /// </summary>
        /// <param name="tokenValue">Token value</param>
        /// <param name="value">Returned value</param>
        /// <returns></returns>
        public static bool ReplaceEnvironmentVar(string tokenValue, out string value)
        {
            value = tokenValue;

            var match = new Regex($"{Constants.EnvironmentVarMarker}(.*?){Constants.EnvironmentVarMarker}").Match(tokenValue);
            if (match.Success)
            {
                string str = match.Groups[1].Captures[0].Value;
                string newValue = Environment.GetEnvironmentVariable(str) ?? string.Empty;
                value = tokenValue.Replace(Constants.EnvironmentVarMarker + str + Constants.EnvironmentVarMarker, newValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create a normal cased title from a filename.
        /// Replaces underscores/dashes with spaces
        /// Capitalizes first letter.
        /// </summary>
        /// <param name="filename">Filename to work with</param>
        /// <returns>Title</returns>
        public static string TitleFromFilename(string filename)
        {
            filename = filename.Replace('_', ' ').Replace('-', ' ');
            return filename.Length > 1 ? filename[0] + filename.Substring(1) : filename;
        }

        /// <summary>
        /// Formats an exception into a stack trace
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="frames">Number of frames to stack walk</param>
        /// <returns>Text</returns>
        public static string FormatText(this Exception exception, int frames = 6)
        {
            var err = new StringBuilder();
            err.AppendLine(exception.Message);

            // Get the innermost exception for the stack track.
            Exception ex = exception;
            while (ex.InnerException != null)
                ex = ex.InnerException;

            if (ex != exception)
            {
                err.AppendLine();
                err.AppendLine($"{ex.GetType().Name}: {ex.Message}");
            }

            // Get a stack trace.
            var st = new StackTrace(ex);
            var stackFrames = st.GetFrames() ?? new StackFrame[0];
            foreach (var frame in stackFrames.Skip(1).Take(frames))
            {
                var mi = frame.GetMethod();
                err.AppendLine($"   {mi.DeclaringType}.{mi.Name}");
            }

            return err.ToString();
        }

    }
}