using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// Handles the [[include=xxx]] directive to insert files
    /// </summary>
    public class IncludeDirective : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Run the INCLUDE directive
        /// </summary>
        /// <param name="pageVars">Page variables</param>
        /// <param name="input">File to run processor on</param>
        /// <returns>Merged files</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            var regex = new Regex(@"\[\[\s*include\s*=\s*([\w-.\\:/ \{\}]+)]]");
            var matches = regex.Matches(input);
            if (matches.Count > 0)
            {
                foreach (Match m in matches)
                {
                    string original = m.Groups[0].Value;
                    string fn = m.Groups[1].Value;
                    if (!string.IsNullOrEmpty(fn))
                    {
                        string filename = pageVars.GetService<ITokenCollection>().Replace(fn);
                        string finalFilename = Utilities.FindFileAlongPath(
                                pageVars.Page?.Filename == null ? pageVars.SearchFolders
                                : new[] { Path.GetDirectoryName(pageVars.Page.Filename) }.Concat(pageVars.SearchFolders), filename);
                        if (finalFilename != null)
                        {
                            string text = File.ReadAllText(finalFilename);
                            input = input.Replace(original, text);
                        }
                        else
                        {
                            throw new Exception($"Failed to find include file {filename} in source file {pageVars.Page?.Filename}.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Malformed [[include]] directive {original} in source file {pageVars.Page?.Filename}.");
                    }
                }
            }

            return input;
        }
    }
}