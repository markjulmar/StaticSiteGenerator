using MDPGen.Core.Infrastructure;

namespace MDPGen.Core
{
    /// <summary>
    /// This interface used to load the metadata for the page
    /// </summary>
    public interface IPageMetadataLoader
    {
        /// <summary>
        /// Loads any metadata associated with the page.
        /// </summary>
        /// <param name="page">Page which has been loaded</param>
        /// <returns>Metadata for page</returns>
        DocumentMetadata Load(ContentPage page);
    }
}
