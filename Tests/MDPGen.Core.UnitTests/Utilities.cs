using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MDPGen.Core.UnitTests
{
    internal class TempFile : IDisposable
    {
        public static List<TempFile> Create(string folder, string[] files, string contents)
        {
            return files.Select(fn => new TempFile(Path.Combine(folder, fn), contents))
                .ToList();
        }

        public static TempFile Create(string contents)
        {
            return new TempFile(Path.GetTempFileName(), contents);
        }

        public string Name;
        public TempFile(string fn, string contents)
        {
            if (string.IsNullOrEmpty(fn))
                throw new ArgumentNullException(nameof(fn));

            Name = fn;
            File.WriteAllText(Name, contents ?? "");
        }

        public void Dispose()
        {
            if (Name != null)
                File.Delete(Name);
        }
    }

    internal class TempDir : IDisposable
    {
        public string Name;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TempDir Create()
        {
            return new TempDir(new StackTrace(1).GetFrame(0).GetMethod().Name);
        }

        public TempDir(string dirName)
        {
            if (string.IsNullOrEmpty(dirName))
                throw new ArgumentNullException(nameof(dirName));

            Name = Path.Combine(Path.GetTempPath(), "tests", dirName);
            if (Directory.Exists(Name))
                Directory.Delete(Name,true);
            Directory.CreateDirectory(Name);
        }

        public void Dispose()
        {
            Directory.Delete(Name, true);
        }
    }
}
