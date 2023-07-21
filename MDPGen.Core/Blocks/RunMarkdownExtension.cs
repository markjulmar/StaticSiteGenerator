using System.Text;
using System.Text.RegularExpressions;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;

namespace MDPGen.Core.Blocks
{
    /// <summary>
    /// Process the Markdown extensions
    /// </summary>
    public class RunMarkdownExtensions : BaseProcessingBlock<string, string>
    {
        /// <summary>
        /// Run the markdown extensions processor on our file.
        /// </summary>
        /// <param name="pageVars">Page Variables</param>
        /// <param name="input">Input file text</param>
        /// <returns>Processed file</returns>
        public override string Process(PageVariables pageVars, string input)
        {
            if (input == null)
                return null;

            var regex = new Regex(@"@[a-xA-z]+[a-zA-z0-9_]\s*\(");
            var builder = new StringBuilder();
            var processor = new ExtensionProcessor();
            using (var reader = new LineReader(input))
            {
                while (!reader.IsEof)
                {
                    string line = reader.PeekLine();
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        // Shift to the match
                        reader.Skip(match.Index);
                        
                        // Skip "@@" - this is our escape marker.
                        if (!reader.IsBof && (reader[reader.CurrentPos - 1] == '@'))
                        {
                            builder.AppendLine(line);
                            reader.SkipLine();
                        }
                        // Run the extension.
                        else
                        {
                            string output = processor.Run(pageVars, reader);
                            if (output != null)
                            {
                                builder.Append(output);
                            }
                        }
                    }
                    else
                    {
                        builder.AppendLine(line);
                        reader.SkipLine();
                    }
                }
            }
            return builder.ToString();
        }
    }
}
