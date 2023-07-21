using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.Services;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// Processing block to handle the [[if xxx]] directive.
    /// </summary>
    public class IfDirective : BaseProcessingBlock<string,string>
    {
        /// <summary>
        /// Run the IF directive
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">Text to check</param>
        /// <returns></returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (pageVars == null)
                throw new ArgumentNullException(nameof(pageVars));

            if (input == null)
                return null;

            var reStart = new Regex(@"^\s*\[\[\s*if\s*(!?[\w\-_\$\{\%\}]+)\s*]]\s*$");
            var reEnd = new Regex(@"^\s*\[\[\s*endif\s*]]\s*$");

            int depth = 0, skipLines = 0;
            var builder = new StringBuilder();

            using (var reader = new LineReader(input))
            {
                while (!reader.IsEof)
                {
                    string str = reader.ReadLine();
                    if (str == null) continue;
                    Match match = reStart.Match(str);
                    if (match.Success)
                    {
                        depth++;
                        if (skipLines > 0)
                            skipLines++;
                        else
                            skipLines = CheckIfDirective(pageVars, match.Groups[1].Value, pageVars.BuildSymbols) ? 0 : 1;
                    }
                    else
                    {
                        if (reEnd.Match(str).Success)
                        {
                            if (depth == 0)
                                TraceLog.Write(TraceType.Error, $"ENDIF directive found in {pageVars.Page?.Filename} with no matching IF directive!");
                            else
                                depth--;

                            if (skipLines > 0)
                                skipLines--;
                            continue;
                        }
                        if (skipLines == 0)
                            builder.AppendLine(str);
                    }
                }
            }

            if (depth > 0)
            {
                TraceLog.Write(TraceType.Error, $"IF directive not closed in {pageVars.Page?.Filename}!");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Check to see if the given symbol exists.
        /// </summary>
        /// <param name="vars">Page variables</param>
        /// <param name="value">Symbol</param>
        /// <param name="buildSymbols">Available build symbols</param>
        /// <returns>True if the symbol exists</returns>
        private static bool CheckIfDirective(PageVariables vars, string value, List<string> buildSymbols)
        {
            bool isNot = value.StartsWith("!");
            if (isNot)
                value = value.Substring(1);

            bool hasValue;
            if (value.StartsWith("{{") && value.EndsWith("}}"))
            {
                hasValue = vars.GetService<ITokenCollection>().ContainsKey(value.Substring(2, value.Length - 4));
            }
            else if (value.StartsWith("%") && value.EndsWith("%"))
            {
                hasValue = Environment.GetEnvironmentVariable(value.Substring(1, value.Length - 2)) != null;
            }
            else
            {
                hasValue = buildSymbols?.Any(s => string.Equals(s, value, StringComparison.OrdinalIgnoreCase)) == true;
            }

            return isNot ? !hasValue : hasValue;
        }
    }
}