using System.Threading.Tasks;
using MDPGen.Core.Infrastructure;

namespace MDPGen.Core
{
    /// <summary>
    /// The interface to load a set of ContentPage objects from 
    /// a folder or db structure. Custom types can implement this
    /// to provide custom locations for loading content.
    /// </summary>
    public interface IContentPageLoader
    {
        /// <summary>
        /// The loader used to grab metadata for each page.
        /// Can be set to NullMetadataLoader to ignore metadata.
        /// </summary>
        IPageMetadataLoader MetadataLoader { get; set; }

        /// <summary>
        /// LoadAsync the content pages from the given folder.
        /// </summary>
        /// <param name="contentFolder">Folder to start at</param>
        /// <returns>Loaded content page with all children</returns>
        Task<ContentPage> LoadAsync(string contentFolder);

        /// <summary>
        /// Reloads a single content page.
        /// </summary>
        /// <param name="page">Page to reload</param>
        Task RefreshPageAsync(ContentPage page);
    }
}
