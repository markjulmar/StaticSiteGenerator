using System.Collections.Generic;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// A folder in our content files.
    /// </summary>
    public class ContentFolderInfo
    {
        /// <summary>
        /// Title used in the navigation for this folder
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Children
        /// </summary>
        public List<ContentEntryInfo> Entries { get; set; }
    }
}