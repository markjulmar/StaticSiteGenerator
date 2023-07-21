namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// Represents a key/value token from the YAML header.
    /// These are loaded into the replacement tokens for the page.
    /// </summary>
    public class HeaderToken
    {
        /// <summary>
        /// Key (string)
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public HeaderToken()
        {
        }

        /// <summary>
        /// Parameterized Constructor 
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        public HeaderToken(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
