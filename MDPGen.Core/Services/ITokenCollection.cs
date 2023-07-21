using System;
using System.Collections.Generic;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Token collection object which tracks the replacement
    /// tokens that will be used in the page rendering.
    /// </summary>
    public interface ITokenCollection : IDictionary<string, string>
    {
        /// <summary>
        /// Check a given replacement token name for validity extension point.
        /// </summary>
        event Func<string, bool> IsValidToken;

        /// <summary>
        /// Indexer to get or set a value.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        new string this[string key] { get; set; }

        /// <summary>
        /// Add a range of values to the token collection
        /// </summary>
        /// <param name="items"></param>
        void AddRange(IEnumerable<KeyValuePair<string, string>> items);

        /// <summary>
        /// Enumerate all the missing tokens in the given template text.
        /// </summary>
        /// <param name="input">Text to scan</param>
        /// <returns>List of potential tokens to replace</returns>
        IEnumerable<Tuple<int,string,string>> Enumerate(string input);

        /// <summary>
        /// This method will go through an input text template and
        /// replace any identified tokens, returning a fully 
        /// replaced value.
        /// </summary>
        /// <param name="input">Text to scan</param>
        /// <returns>Text with updated values</returns>
        string Replace(string input);
    }
}