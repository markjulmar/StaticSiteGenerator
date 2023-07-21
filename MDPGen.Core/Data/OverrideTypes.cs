using Newtonsoft.Json.Linq;

namespace MDPGen.Core.Data
{
    /// <summary>
    /// The types which will be used to override defaults
    /// when loading and processing pages.
    /// </summary>
    public class OverrideTypes
    {
        /// <summary>
        /// The implementation of the IMarkdownParser
        /// </summary>
        public JToken MarkdownParserType { get; set; }

        /// <summary>
        /// The type which will be used to load pages
        /// </summary>
        public JToken PageLoaderType { get; set; }

        /// <summary>
        /// The type used to parse/provide metadata for a single page.
        /// </summary>
        public JToken MetadataLoaderType { get; set; }

        /// <summary>
        /// The implementation of the IIdGenerator
        /// Very rare to override
        /// </summary>
        public JToken IdGeneratorType { get; set; }

        /// <summary>
        /// The implementation of the ITokenCollection
        /// Very rare to override
        /// </summary>
        public JToken TokenCollectionType { get; set; }
    }
}