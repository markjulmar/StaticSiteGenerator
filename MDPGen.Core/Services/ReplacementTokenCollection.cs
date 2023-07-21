using System;
using System.Collections.Generic;
using System.Text;
using MDPGen.Core.Data;
using System.Text.RegularExpressions;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Dictionary with replacement tokens used to replace blocks in the HTML page template
    /// </summary>
    internal sealed class ReplacementTokenCollection : Dictionary<string, string>, ITokenCollection
    {
        /// <summary>
        /// Check a given replacement token name for validity extension point.
        /// </summary>
        public event Func<string, bool> IsValidToken;

        /// <summary>
        /// Constructor - ensures keys are case-insensitive
        /// </summary>
        public ReplacementTokenCollection()
            : base(new NoSpaceComparer())
        {
        }

        /// <summary>
        /// Method to replace all the tokens in the given string.
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>String with all recognized tokens replaced.</returns>
        public string Replace(string input)
        {
            var sb = new StringBuilder(input);

            // Go through all the tags in the input, backwards
            // so we can replace each tag.
            foreach (var token in Enumerate(input))
            {
                //int pos = token.Item1;
                string tag = token.Item2;
                string fullTag = token.Item3;

                string value = null;

                // Format tag is in the form: {{token:format}}
                int fpos = tag.IndexOf(":", StringComparison.Ordinal);
                if (fpos >= 0)
                {
                    string key = tag.Substring(0, fpos);
                    if (this.ContainsKey(key))
                        value = DoFormat(this[key], tag.Substring(fpos + 1));
                }
                else if (this.ContainsKey(tag))
                    value = this[tag];

                if (value != null)
                {
                    sb.Replace(fullTag, value);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Try to guess as to the value type by coercing it to
        /// numeric, double, and DateTime.
        /// </summary>
        /// <param name="value">Value to format</param>
        /// <param name="format">Format to use</param>
        /// <returns>Formatted string</returns>
        private string DoFormat(string value, string format)
        {
            const string open = "{0:";
            const string close = "}";

            long lv; double dv; DateTime dt;
            if (long.TryParse(value, out lv))
            {
                return string.Format(open + format + close, lv);
            }
            
            if (double.TryParse(value, out dv))
            {
                return string.Format(open + format + close, dv);
            }
            
            if (DateTime.TryParse(value, out dt))
            {
                return string.Format(open + format + close, dt.ToUniversalTime());
            }

            return string.Format(open + format + close, value);
        }

        /// <summary>
        /// This locates any token blocks in the string and returns them as a comma-delimited list.
        /// </summary>
        /// <param name="input">String to check</param>
        /// <returns>List of located tokens + position</returns>
        public IEnumerable<Tuple<int,string,string>> Enumerate(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var re = new Regex(Constants.BeginMarker + @"([\s+\w-.:/,]+)" + Constants.EndMarker);
            foreach (Match m in re.Matches(input))
            {
                string original = m.Groups[0].Value;
                string id = m.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(id) && (IsValidToken == null || IsValidToken.Invoke(id)))
                    yield return Tuple.Create(m.Groups[0].Index, id, original);
            }
        }

        /// <summary>
        /// Replace indexer to return null if key does not exist.
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <returns></returns>
        public new string this[string key]
        {
            get => !ContainsKey(key) ? null : base[key];
            set => base[key] = value;
        }

        /// <summary>
        /// Adds a set of items to our collection
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<KeyValuePair<string,string>> items)
        {
            foreach (var item in items)
                Add(item.Key, item.Value);
        }

        /// <summary>
        /// A simple StringComparer which ignores case and 
        /// strips spaces from the keys
        /// </summary>
        class NoSpaceComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x == null || y == null
                    ? x == y
                    : String.Compare(x.Trim(), y.Trim(), StringComparison.OrdinalIgnoreCase) == 0;
            }

            public int GetHashCode(string obj)
            {
                return obj == null ? 0 : obj.Trim().ToLower().GetHashCode();
            }
        }
    }
}