namespace MDPGen.Core.Data
{
    /// <summary>
    /// Constant strings used in the parser
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Token begin marker
        /// </summary>
        public const string BeginMarker = "{{";
        /// <summary>
        /// Token end marker
        /// </summary>
        public const string EndMarker = "}}";
        /// <summary>
        /// Environment variable marker
        /// </summary>
        public const string EnvironmentVarMarker = "%";
        /// <summary>
        /// Web URL separator
        /// </summary>
        public const string WebSeparator = "/";
        /// <summary>
        /// Begin HTML comment
        /// </summary>
        public const string HtmlStartComment = "<!--";
        /// <summary>
        /// End HTML comment
        /// </summary>
        public const string HtmlEndComment = "-->";
    }
}
