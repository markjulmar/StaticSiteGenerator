namespace XamU.SGL.Extensions
{
    /// <summary>
    /// Represents a single link in a YAML header.
    /// links:
    ///    - title: Frequently Asked Questions
    ///      description: >
    ///         This is a test of wrapping lines
    ///         which can span multiple lines in the YAML document.
    ///      url: https://www.xamarin.com
    /// </summary>
    public class HeaderLink
    {
        /// <summary>
        /// Title to display
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Optional description for the link
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// URL for the link
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Optional group to place in a unique box.
        /// </summary>
        public string Group { get; set; }
    }
}
