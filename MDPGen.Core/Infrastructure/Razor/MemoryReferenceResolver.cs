using RazorEngine.Compilation;
using RazorEngine.Compilation.ReferenceResolver;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MDPGen.Core.Infrastructure.Razor
{
    /// <summary>
    /// This resolves our resource-based assemblies for the Razor engine.
    /// </summary>
    internal class MemoryReferenceResolver : IReferenceResolver
    {
        /// <summary>
        /// Returns all the references (DLLs) for the compiler to use.
        /// </summary>
        /// <param name="context">Provides context for the compilation (which templates, which namespaces and types)</param>
        /// <param name="includeAssemblies">Assemblies to include</param>
        /// <returns>Compiler references to use with Roslyn/Razor</returns>
        public IEnumerable<CompilerReference> GetReferences(TypeContext context, IEnumerable<CompilerReference> includeAssemblies)
        {
            // Use the built-in resolver first. This retrieves all the in-memory DLLs the host is using that
            // are _file-based_. This will exclude resource based DLLs in release builds.
            var loadedAssemblies = (new UseCurrentAssembliesReferenceResolver())
                .GetReferences(context, includeAssemblies)
                .ToArray();

            // Return all the loaded assemblies.
            foreach (var asm in loadedAssemblies)
                yield return asm;

            // Look for resource-based assemblies.
            var coreAssembly = Assembly.GetEntryAssembly();
            foreach (var res in coreAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".dll")))
            {
                yield return CompilerReference.From(
                    coreAssembly.GetManifestResourceStream(res)); // stream (roslyn only)
            }
        }
    }
}
