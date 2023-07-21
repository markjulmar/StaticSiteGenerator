using MDPGen.Core.Services;
using System;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MDPGen.Core.Infrastructure.Metadata
{
    /// <summary>
    /// This metadata loader pulls a YAML header from the top of 
    /// the content for the Markdown file we are loading. The valid
    /// elements in the header can be Id, Title, Tokens, and Template.
    /// </summary>
    public class YamlMetadataLoader : BaseYamlMetadataLoader<DocumentMetadata>
    {
        // No additional features
    }

    /// <summary>
    /// This metadata loader pulls a YAML header from the top of 
    /// the content for the Markdown file we are loading. This is a
    /// base class which should be derived from in order to provide 
    /// additional details in the header.
    /// </summary>
    /// <typeparam name="T">Type to load</typeparam>
    public abstract class BaseYamlMetadataLoader<T> : IPageMetadataLoader
        where T : DocumentMetadata
    {
        private const string YamlMarker = "---";

        /// <summary>
        /// LoadAsync the YAML header from the top of the Markdown file itself.
        /// </summary>
        /// <param name="page">Page we are loading</param>
        /// <returns>Header metadata</returns>
        public virtual DocumentMetadata Load(ContentPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            // No content?
            if (page.Content == null)
                return null;

            T header = default(T);

            // Pull the YAML header out of the file.
            using (var reader = new LineReader(page.Content))
            {
                while (string.IsNullOrWhiteSpace(reader.PeekLine()))
                    reader.SkipLine();

                // YAML header must be first non-space element.
                if (reader.PeekLine().Trim() == YamlMarker)
                {
                    reader.SkipLine();
                    StringBuilder sb = new StringBuilder();
                    string line = reader.ReadLine();
                    while (line != YamlMarker && !reader.IsEof)
                    {
                        sb.AppendLine(line);
                        line = reader.ReadLine();
                    }

                    if (line != YamlMarker)
                        throw new FormatException("Missing ending YAML header line.");

                    line = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var deserializer = new DeserializerBuilder()
                                .WithNamingConvention(new CamelCaseNamingConvention())
                                .Build();
                        header = deserializer.Deserialize<T>(line);
                    }
                    else
                    {
                        TraceLog.Write(TraceType.Warning, $"Missing YAML header on {page.Filename}.");
                    }
                }

                // Get rid of the YAML header
                page.Content = reader.ReadToEnd();
            }

            return header;
        }
    }
}
