namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// A single content page entry in the meta.json file.
    /// </summary>
    public class ContentEntryInfo
    {
        /// <summary>
        /// Title for the page
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// File for this page (markdown)
        /// </summary>
        public string File { get; set; }
        /// <summary>
        /// Folder for child
        /// </summary>
        public string Folder { get; set; }
        /// <summary>
        /// Optional template for page render
        /// </summary>
        public string Template { get; set; }
    }
}