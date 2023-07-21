using System;
using System.Text;
using MDPGen.Core.Data;
using System.Linq;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// Extension methods for string types
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Used to normalize a URL to a web address.
        /// </summary>
        /// <param name="url">URL string</param>
        /// <returns>Normalized URL</returns>
        public static string NormalizeUrl(this string url)
        {
            return url.Replace("\\", Constants.WebSeparator).Trim(Constants.WebSeparator[0]);
        }

        /// <summary>
        /// This uppercases letters and replaces dashes/underscores with spaces.
        /// </summary>
        /// <param name="title">Title - typically filename</param>
        /// <returns></returns>
        public static string SanitizeTitle(this string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            char[] letters = title.ToCharArray();
            for (int i = 0; i < letters.Length; i++)
            {
                if (i == 0)
                    letters[i] = Char.ToUpper(letters[i]);
                if (letters[i] == '-' || letters[i] == '_')
                    letters[i] = ' ';
            }

            return new string(letters);
        }

        /// <summary>
        /// Method to search end of StringBuilder so we don't have
        /// to convert it to a string all the time to find terminators.
        /// </summary>
        /// <param name="sb">StringBuilder</param>
        /// <param name="value">Value we are looking for</param>
        /// <returns>True/False</returns>
        public static bool EndsWith(this StringBuilder sb, string value)
        {
            int length = value.Length;
            int sbLength = sb.Length;
            if (length > sbLength)
                return false;

            char[] ch = new char[length];
            for (int i = 0; i < length; i++)
            {
                ch[i] = sb[sbLength - (length - i)];
            }

            return new string(ch) == value;
        }

        /// <summary>
        /// Combines a root with a URL Path using a separator
        /// </summary>
        /// <param name="root">Root address</param>
        /// <param name="uris">Parts</param>
        /// <returns>Full combined URL</returns>
        public static string UrlCombine(this string root, params string[] uris)
        {
            StringBuilder sb = new StringBuilder(root.NormalizeUrl());
            foreach (var uri in uris)
            {
                var link = uri;
                if (link.StartsWith(Constants.WebSeparator))
                    link = link.Substring(1);
                if (!string.IsNullOrEmpty(link))
                    sb.AppendFormat("/{0}", link.NormalizeUrl());
            }

            return sb.ToString();
        }

        /// <summary>
        /// IndexOf implementation that can skip embedded blocks with quotes.
        /// </summary>
        /// <param name="content">String owner</param>
        /// <param name="startIndex">Where to start</param>
        /// <param name="lookFor">Character to find</param>
        /// <param name="embedBlocks">Blocks to ignore.</param>
        /// <returns>Index position of located character</returns>
        public static int SmartIndexOf(this string content, int startIndex, char lookFor, Tuple<char, char>[] embedBlocks = null)
        {
            if (embedBlocks == null)
                return content.IndexOf(lookFor, startIndex);

            while (content.Length > startIndex)
            {
                char ch = content[startIndex];

                if (ch == lookFor
                    && startIndex > 0
                    && content[startIndex - 1] != '\\')
                    return startIndex;

                var eb = embedBlocks.SingleOrDefault(e => e.Item1 == ch);
                if (eb != null)
                {
                    if (content[startIndex - 1] != '\\') // escaped?
                    {
                        startIndex++;
                        int moveTo = content.SmartIndexOf(startIndex, eb.Item2, embedBlocks);
                        if (moveTo != -1)
                            startIndex = moveTo;
                    }
                }
                startIndex++;
            }

            return -1;
        }

        /// <summary>
        /// Captures a string from the input by locating the termination point.
        /// </summary>
        /// <param name="content">String owner</param>
        /// <param name="startIndex">Where to start</param>
        /// <param name="lookFor">Termination character</param>
        /// <param name="includeChar">True to include terminator in return value</param>
        /// <param name="embedBlocks">Blocks to ignore in the stream - used for embedded quotes, JSON characters.</param>
        /// <returns>String located, or null if terminator was never located.</returns>
        public static string GrabTo(this string content, int startIndex, char lookFor, bool includeChar = false, Tuple<char, char>[] embedBlocks = null)
        {
            int pos = content.SmartIndexOf(startIndex, lookFor, embedBlocks);
            if (pos == -1)
                return null;
            if (includeChar)
                pos++;

            return content.Substring(startIndex, pos - startIndex);
        }
    }
}