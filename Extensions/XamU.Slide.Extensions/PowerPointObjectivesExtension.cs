using System;
using System.Text;
using MDPGen.Core.MarkdownExtensions;
using ReadSlides;

namespace XamU.Slide.Extensions
{
    /// <summary>
    /// Retrieve the objectives for the set PowerPoint.
    /// </summary>
    public class PowerPointObjectivesExtension : BaseMarkdownExtension
    {
        /// <summary>
        /// Method used to generate the HTML output.
        /// </summary>
        /// <returns>HTML output to add to the resulting page, or null if no output.</returns>
        protected override string Process()
        {
            string filename = Tokens[PowerPointTitleExtension.PowerPointFilename];
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentException($"Missing {PowerPointTitleExtension.PowerPointFilename} (filename) for {GetType().Name}.");

            StringBuilder sb = new StringBuilder();
            SlideManager mgr = new SlideManager(filename);
            if (mgr.SlideCount > 1)
            {
                var text = mgr.GetAllTextInSlide(2);
                if (text?.Length > 0)
                {
                    sb.AppendLine("<ol class=\"objectives\">");
                    for (int i = 0; i < text.Length - 1; i++)
                        sb.AppendFormat("<li>{0}</li>\r\n", text[i]?.Trim());
                    sb.AppendLine("</ol>");
                }
            }

            return sb.ToString();
        }
    }
}