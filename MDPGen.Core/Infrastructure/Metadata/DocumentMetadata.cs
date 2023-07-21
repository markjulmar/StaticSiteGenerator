using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// The YAML header block for a single Markdown page
    /// </summary>
    public class DocumentMetadata
    {
        /// <summary>
        /// Unique identifier for the page
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Page title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Optional page template
        /// </summary>
        [YamlMember(Alias = "template", ApplyNamingConventions = false)]
        public string PageTemplate { get; set; }

        /// <summary>
        /// List of replacement tokens for the page
        /// </summary>
        public List<HeaderToken> Tokens { get; set; } = new List<HeaderToken>();
    }
}
