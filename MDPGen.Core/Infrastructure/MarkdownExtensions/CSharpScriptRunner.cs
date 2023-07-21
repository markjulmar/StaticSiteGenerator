using System;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Dynamic;
using System.Collections.Generic;
using System.Threading;
using MDPGen.Core.Infrastructure;
using System.Text.RegularExpressions;
using System.Net;
using MDPGen.Core.Services;
using Microsoft.CodeAnalysis;

namespace MDPGen.Core.MarkdownExtensions
{
    /// <summary>
    /// This class loads and manages C# script engines which are used to run
    /// local script-based extensions.
    /// </summary>
    public class CSharpScriptRunner
    {
        /// <summary>
        /// Internal engine data
        /// </summary>
        struct EngineData
        {
            public string Filename;
            public CSharpScriptEngine Engine;
        }

        /// <summary>
        /// All the loaded C# scripts
        /// </summary>
        readonly Dictionary<string, EngineData> loadedScripts = new Dictionary<string, EngineData>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This loads all the C# scripts contained within the specified folder.
        /// </summary>
        /// <param name="folder">Folder where scripts are located</param>
        /// <param name="requiredAssemblies">Assemblies to add</param>
        /// <param name="namespaces">required namespaces</param>
        public CSharpScriptRunner(string folder, 
            IEnumerable<Assembly> requiredAssemblies = null, 
            IEnumerable<string> namespaces = null)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("Folder cannot be null.", nameof(folder));

            if (requiredAssemblies == null)
                requiredAssemblies = Enumerable.Empty<Assembly>();
            if (namespaces == null)
                namespaces = Enumerable.Empty<string>();

            var coreAssemblies = new[] {
                                FindAssembly(Assembly.GetExecutingAssembly()),
                                FindAssembly(typeof(WebUtility).Assembly) }
                            .Union(requiredAssemblies.Select(FindAssembly))
                            .ToList();
            var coreImports = new[] {
                            "System",
                            "System.Collections.Generic",
                            "System.Linq",
                            "System.Text"
                        };

            var imports = namespaces.Union(coreImports).Distinct().ToList();

            foreach (var file in Directory.GetFiles(folder, "*.cs"))
            {
                try
                {
                    var assemblies = coreAssemblies.ToList(); // copy
                    string code = LoadAssemblies(File.ReadAllText(file), assemblies);
                    var engine = new CSharpScriptEngine()
                        .WithImports(imports.ToArray())
                        .WithReferences(assemblies.ToArray()).
                       CompileScript(code);

                    string key = Path.GetFileNameWithoutExtension(file);
                    loadedScripts.Add(key, new EngineData { Engine = engine, Filename = Path.GetFullPath(file) });
                }
                catch (Exception ex)
                {
                    TraceLog.Write(TraceType.Error, $"Failed to load {file} into C# script engine: {ex.GetType().Name} {ex.Message} {ex.InnerException?.Message}");
                }
            }
        }

        /// <summary>
        /// This locates a referenced assembly by name.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private MetadataReference FindAssembly(Assembly assembly)
        {
            // If the given assembly has a file-based location, we can load it from there.
            if (!string.IsNullOrEmpty(assembly.Location))
                return MetadataReference.CreateFromFile(assembly.Location);

            // See if it's in the root assembly resources. We use this trick to collapse DLLs into one file.
            Assembly rootAssembly = Assembly.GetEntryAssembly();
            string name = assembly.GetName().Name + ".dll";
            var resource = rootAssembly.GetManifestResourceStream(name);
            if (resource != null)
                return MetadataReference.CreateFromStream(resource);

            // Not found.
            return null;
        }

        /// <summary>
        /// Test for a specific script by name.
        /// </summary>
        /// <param name="name">Name of the script</param>
        /// <returns>True if script is loaded.</returns>
        public bool HasScript(string name)
        {
            return loadedScripts.ContainsKey(name);
        }

        /// <summary>
        /// Returns the loaded scripts.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> EnumerateLoadedScripts()
        {
            return loadedScripts.Select(s => s.Key);
        }

        /// <summary>
        /// Executes the specified script by name.
        /// </summary>
        /// <param name="name">Name of the script</param>
        /// <param name="pageVariables">Page variables to add</param>
        /// <param name="parameters">Parameters to pass</param>
        /// <returns></returns>
        public object RunScript(string name, PageVariables pageVariables, List<string> parameters)
        {
            if (loadedScripts.TryGetValue(name, out EngineData data))
            {
                var values = new Dictionary<string, object>();
                for (int i = 0; i < parameters.Count; i++)
                    values.Add("p" + (i+1), parameters[i]);
                values.Add("Count", parameters.Count);

                // If an HTML file is alongside the script, load it.
                string htmlFile = Path.ChangeExtension(data.Filename, ".html");
                if (htmlFile != null && File.Exists(htmlFile))
                    values.Add("HtmlTemplate", File.ReadAllText(htmlFile));

                try
                {
                    return data.Engine.Execute(pageVariables, values, CancellationToken.None).Result?.ReturnValue;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Problem running script {name}: {ex.Message} {ex.InnerException?.Message}", ex);
                }
            }
            else
            {
                throw new Exception($"No script found with the name {name}.");
            }
        }

        /// <summary>
        /// LoadAsync the specified assemblies using the "#r" directive in the script.
        /// </summary>
        /// <param name="code">Script code</param>
        /// <param name="assemblies">List of assemblies.</param>
        /// <returns></returns>
        private string LoadAssemblies(string code, List<MetadataReference> assemblies)
        {
            Regex re = new Regex("#r \"([^ \"]*)\"");
            using (LineReader reader = new LineReader(code))
            {
                Match match = re.Match(reader.PeekLine());
                while (match.Success)
                {
                    reader.ReadLine();
                    string asmName = match.Groups[1].ToString();
                    if (!string.IsNullOrEmpty(asmName))
                    {
                        var asm = Assembly.Load(asmName);
                        if (asm != null)
                        {
                            // Add the reference if we don't already include it.
                            var mdr = FindAssembly(asm);
                            if (mdr != null && assemblies.All(m => m.Display != mdr.Display))
                                assemblies.Add(mdr);
                        }
                    }
                    match = re.Match(reader.PeekLine());
                }
                return reader.Remainder;
            }
        }
    }

    /// <summary>
    /// This holds all the parameters passed to a C# script.
    /// </summary>
    public class GlobalScriptInputs
    {
        /// <summary>
        /// Service Provider to get to additional services.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Current page we are rendering/executing
        /// </summary>
        public ContentPage Page { get; set; }

        /// <summary>
        /// Passed input parameters (p1,p2,p3..)
        /// </summary>
        public dynamic Args = new ExpandoObject();

        /// <summary>
        /// The session cache - this is cleared after each page render.
        /// </summary>
        public dynamic ViewBag => ServiceProvider.GetService<DynamicPageCache>();

        /// <summary>
        /// Tokens used for replacement tags in the template HTML file.
        /// </summary>
        public ITokenCollection Tokens => ServiceProvider.GetService<ITokenCollection>();

        /// <summary>
        /// Unique ID/name generator for HTML tags.
        /// </summary>
        public IIdGenerator IdGen => ServiceProvider.GetService<IIdGenerator>();
    }

    /// <summary>
    /// Wrapper around Roslyn C# scripting engine.
    /// </summary>
    public class CSharpScriptEngine
    {
        Script<object> baseScript;
        ScriptOptions options = ScriptOptions.Default.WithReferences(
                        typeof(string).Assembly,
                        typeof(Task).Assembly,
                        typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly,
                        typeof(Enumerable).Assembly)
            .AddImports("System.Dynamic");

        /// <summary>
        /// Base script to execute.
        /// </summary>
        public Script<object> BaseScript => baseScript;

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="asms"></param>
        /// <returns></returns>
        public CSharpScriptEngine WithReferences(params Assembly[] asms)
        {
            if (baseScript != null) throw new Exception("Cannot add references after script is compiled.");
            options = options.AddReferences(asms);
            return this;
        }

        /// <summary>
        /// Add references
        /// </summary>
        /// <param name="asms"></param>
        /// <returns></returns>
        public CSharpScriptEngine WithReferences(params MetadataReference[] asms)
        {
            if (baseScript != null) throw new Exception("Cannot add references after script is compiled.");
            options = options.AddReferences(asms);
            return this;
        }

        /// <summary>
        /// Add namespaces
        /// </summary>
        /// <param name="namespaces"></param>
        /// <returns></returns>
        public CSharpScriptEngine WithImports(params string[] namespaces)
        {
            if (baseScript != null) throw new Exception("Cannot add imports after script is compiled.");
            options = options.AddImports(namespaces);
            return this;
        }

        /// <summary>
        /// This compiles the given code and wraps it in an 
        /// executable engine object.
        /// </summary>
        /// <param name="code">C# code to compile.</param>
        /// <returns>Engine wrapper</returns>
        public CSharpScriptEngine CompileScript(string code)
        {
            baseScript = CSharpScript.Create(code, options, typeof(GlobalScriptInputs));
            baseScript.Compile();
            return this;
        }

        /// <summary>
        /// This executes the given engine script with the passed
        /// parameters and state.
        /// </summary>
        /// <param name="pageVariables">Page parameters</param>
        /// <param name="parameters">Parameters for the script</param>
        /// <param name="cancelToken">Cancellation token</param>
        /// <returns>Return result</returns>
        public Task<ScriptState<object>> Execute(PageVariables pageVariables, 
            IDictionary<string,object> parameters, CancellationToken cancelToken)
        {
            if (baseScript == null)
                throw new Exception("Must compile script before running.");

            var inputParameters = new GlobalScriptInputs {
                ServiceProvider = pageVariables,
                Page = pageVariables.Page,
                Args = parameters.ToExpandoObject()
            };

            return baseScript.RunAsync(inputParameters, cancelToken);
        }
    }
}
