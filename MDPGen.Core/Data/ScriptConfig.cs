using System.Collections.Generic;

namespace MDPGen.Core.Data
{
    /// <summary>
    /// The script configuration used for both Razor and Roslyn.
    /// </summary>
    public class ScriptConfig
    {
        internal static readonly string[] RequiredNamespaces = new[] {
            "MDPGen.Core.Infrastructure",
            "MDPGen.Core",
            "MDPGen.Core.Services"
        };

        /// <summary>
        /// C# Scripts folder - all loose scripts must be placed here.
        /// </summary>
        public string ScriptsFolder { get; set; }

        /// <summary>
        /// Script hooks
        /// </summary>
        public ScriptHooks Hooks { get; set; }

        /// <summary>
        /// Required assemblys (names)
        /// </summary>
        public List<string> Assemblies { get; set; }

        /// <summary>
        /// Namespaces which should be referenced in all HTML Razor pages.
        /// </summary>
        public List<string> Namespaces { get; set; }
    }

    /// <summary>
    /// Script (C#) hooks which we call out to during page processing
    /// </summary>
    public class ScriptHooks
    {
        /// <summary>
        /// Script file to execute on PageInit.
        /// </summary>
        public string OnPageInit { get; set; }
    }
}