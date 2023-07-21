namespace MDPGen.Core.Data
{
    /// <summary>
    /// The known replaceable tokens in our page templates
    /// </summary>
    public static class KnownTokens
    {
        /// <summary>
        /// Page title
        /// </summary>
        public const string Title = "title";
        /// <summary>
        /// The last modified date for the file that generated this HTML.
        /// </summary>
        public const string LastModifedDate = "last-modified-date";
        /// <summary>
        /// Date/Time
        /// </summary>
        public const string Date = "Constant.date";
        /// <summary>
        /// SGL generator version
        /// </summary>
        public const string GeneratorVersion = "Constant.sglVersion";
        /// <summary>
        /// Source file which was used to generate the page
        /// </summary>
        public const string SourceFile = "Constant.SourceFile";
    }
}
