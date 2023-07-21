namespace MDPGen.Core.Infrastructure.Metadata
{
    /// <summary>
    /// Metadata loader which does nothing.
    /// </summary>
    public class NullMetadataLoader : IPageMetadataLoader
    {
        /// <summary>
        /// Provide no metadata for page.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public DocumentMetadata Load(ContentPage page)
        {
            return null;
        }
    }
}
