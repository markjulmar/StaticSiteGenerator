using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MDPGen.Core.Data
{
    /// <summary>
    /// Site configuration (JSON) used to configure the generator
    /// </summary>
    public class SiteConfigInfo
    {
        /// <summary>
        /// HTML template file used to render each page
        /// </summary>
        public string DefaultPageTemplate { get; set; }

        /// <summary>
        /// True to process the pages one at a time vs. using multiple thread.
        /// </summary>
        public bool ProcessPagesSequentially { get; set; }

        /// <summary>
        /// True to copy over content (source) folder for images, etc.
        /// This will still skip meta files and Markdown.
        /// </summary>
        public bool CopyContentFolder { get; set; } = true;

        /// <summary>
        /// Template folders to copy to the destination location.
        /// This is a deep copy.
        /// </summary>
        public List<string> AssetFoldersToCopy { get; set; }

        /// <summary>
        /// List of folders where we look for include files and templates (e.g. Path)
        /// </summary>
        public List<string> SearchFolders { get; set; }

        /// <summary>
        /// Script environment configuration
        /// </summary>
        public ScriptConfig ScriptConfig { get; set; }

        /// <summary>
        /// The content folder to build the site from
        /// </summary>
        public string ContentFolder { get; set; }

        /// <summary>
        /// List of assemblies to load extensions from
        /// </summary>
        public List<string> Extensions { get; set; }

        /// <summary>
        /// A list of types to be used when processing assets.
        /// </summary>
        public JToken AssetHandlers { get; set; }

        /// <summary>
        /// File with JSON site page replacement tokens
        /// </summary>
        public string SiteConstantsFilename { get; set; }

        /// <summary>
        /// Config-file based constants to load into the page replacement tokens
        /// </summary>
        public List<KeyValuePair<string, string>> Constants { get; set; }

        /// <summary>
        /// Defined build symbols
        /// </summary>
        public List<string> BuildSymbols { get; set; }

        /// <summary>
        /// The overridden types used by the site generator
        /// </summary>
        public OverrideTypes OverrideTypes { get; set; }

        /// <summary>
        /// Processing chain, can be either a list of types, or
        /// a complex object with property setters.
        /// </summary>
        public JToken ProcessingChain { get; set; }
    }
}