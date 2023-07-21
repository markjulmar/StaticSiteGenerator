using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SGLMonitor
{
    /// <summary>
    /// Main entry point
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        [STAThread]
        public static void Main()
        {
#if !DEBUG
            // We cache off the DLLs which are stored as embedded resources.
            var assemblies = new Dictionary<string, Assembly>();
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
                            Debug.Print($"Failed to load: {resource}, Exception: {ex.Message}");
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

            // Launch the app!
            App.Main();
        }
    }
}
