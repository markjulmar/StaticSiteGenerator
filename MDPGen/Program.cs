using System;
using MDPGen.Core.Services;
using System.Threading;
#if !DEBUG
using System.Linq;
using System.Reflection;
#endif

namespace MDPGen
{
    public static class Program
    {
        public static bool VerboseLog = false;

        public static int Main(string[] args)
        {
            // Look for assemblies in our resources
            HandleAssemblyLoads();

            // Run our code.
            return Run(args);
        }

        /// <summary>
        /// Method to run the app. 
        /// This looks stupid, but it's in a different method
        /// so that it gets JIT compiled _after_ we hook the
        /// assembly load method .. otherwise our core assemblies
        /// won't be found.
        /// </summary>
        /// <param name="args"></param>
        static int Run(string[] args)
        {
            TraceLog.OutputHandler += WriteLog;
            return new SiteBuilder().Run(args);
        }

        /// <summary>
        /// Method to color our console output.
        /// </summary>
        /// <param name="traceType">TraceType</param>
        /// <param name="text">Text to output</param>
        static void WriteLog(TraceType traceType, string text)
        {
            if (traceType == TraceType.Diagnostic && !VerboseLog)
                return;

            ConsoleColor[] colors = { ConsoleColor.Blue, ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.Magenta, ConsoleColor.White };

            lock (Console.Out)
            {
                ConsoleColor startingFgColor = Console.ForegroundColor;
                //ConsoleColor startingBkColor = Console.BackgroundColor;
                try
                {
                    int tid = Thread.CurrentThread.ManagedThreadId;

                    ConsoleColor outputColor;
                    if (traceType == TraceType.Error || text.Contains("Error") || text.Contains("Exception") || text.Contains("Problem"))
                        outputColor = ConsoleColor.Red;
                    else if (traceType == TraceType.Warning || text.Contains("Warning"))
                        outputColor = ConsoleColor.Yellow;
                    else
                    {
                        outputColor = tid == 1 
                            ? ConsoleColor.Gray 
                            : colors[(tid-1) % colors.Length];
                    }

                    Console.ForegroundColor = outputColor;
                    if (VerboseLog)
                    {
                        Console.WriteLine($"[{tid}] " + text);
                    }
                    else
                    {
                        Console.WriteLine(text);
                    }
                }
                finally
                {
                    Console.ForegroundColor = startingFgColor;
                }
            }
        }

        /// <summary>
        /// This method connects up the Assembly.LoadAsync handler with DLLs
        /// stored as embedded resources. This allows a single .EXE to be 
        /// shipped with all the required DLLs as part of the one package.
        /// We only do this for the release build.
        /// </summary>
        static void HandleAssemblyLoads()
        {
#if !DEBUG
            // We cache off the DLLs which are stored as embedded resources.
            var assemblies = new System.Collections.Generic.Dictionary<string, Assembly>();
            var executingAssembly = Assembly.GetExecutingAssembly();
            var resources = executingAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".dll"));

            // Read each embedded resource and create an Assembly from it.
            foreach (string resource in resources)
            {
                using (var stream = executingAssembly.GetManifestResourceStream(resource))
                {
                    if (stream != null)
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        try
                        {
                            assemblies.Add(resource, Assembly.Load(bytes));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load: {resource}, Exception: {ex.Message}");
                        }
                    }
                }
            }

            // Hook the resolver and return our DLL loaded from resources when asked.
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => {
                var assemblyName = new AssemblyName(e.Name);
                var path = $"{assemblyName.Name}.dll";
                return assemblies.ContainsKey(path) ? assemblies[path] : null;
            };
#endif
        }
    }
}
