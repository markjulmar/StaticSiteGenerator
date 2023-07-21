using System;
using System.Collections.Generic;
using System.Text;
using MDPGen.Core.Infrastructure;
using MDPGen.Core.MarkdownExtensions;
using MDPGen.Core.Services;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// This extension generates the current course tree topic list and navigation tree
    /// which is used as the navigation for the site.
    /// </summary>
    public class TopicListTreeExtension : BaseMarkdownExtension
    {
        /// <summary>
        /// This method inserts the topic list dropdown into the Template set so it 
        /// gets inserted into our page.
        /// </summary>
        /// <param name="provider">Service Provider</param>
        [ExtensionInit]
        public static void AddTopicsAndClasses(IServiceProvider provider)
        {
            dynamic pageCache = provider.GetService<DynamicPageCache>();
            ContentPage current = pageCache.CurrentPage;

            var tokens = provider.GetService<ITokenCollection>();
            tokens[nameof(TopicListTreeExtension)] = BuildTreeMap(current);
        }

        /// <summary>
        /// Method used to generate the HTML output.
        /// </summary>
        /// <returns>HTML output to add to the resulting page, or null if no output.</returns>
        protected override string Process()
        {
            throw new Exception($"{GetType().Name} should not be called in a Markdown file.");
        }

        /// <summary>
        /// Returns the HTML for the Navigation Tree sidebar used on the page.
        /// </summary>
        /// <returns>The tree map.</returns>
        /// <param name="current">Current node</param>
        /// <param name="prefix">Prefix to put on URLs.</param>
        public static string BuildTreeMap(ContentPage current, string prefix = "")
        {
            var expandedNodes = new List<string>();
            var sb = new StringBuilder("<ul class=\"tree-sidebar\">");
            AddNode(prefix, current, current.GetCourseOwner(), sb, expandedNodes, true);
            sb.Append("</ul>");

            return sb.ToString();
        }


        /// <summary>
        /// Adds a single tree node to the HTML along with all it's children.
        /// </summary>
        /// <param name="prefix">Prefix to put on URLs</param>
        /// <param name="currentPage">Current node.</param>
        /// <param name="nodeOrCourse">Node to add</param>
        /// <param name="sb">Current HTML contents</param>
        /// <param name = "expandedNodes"></param>
        /// <param name = "firstNode"></param>
        private static void AddNode(string prefix, ContentPage currentPage, ContentPage nodeOrCourse, StringBuilder sb, List<string> expandedNodes, bool firstNode)
        {
            if (nodeOrCourse == null)
                return;

            bool expandNode = nodeOrCourse.Children.Count > 0 && !firstNode
                              && (!nodeOrCourse.IsCourse() || TreeNodeIsExpanded(nodeOrCourse, currentPage));

            if (nodeOrCourse.Url != null)
            {
                string id = GenerateId(nodeOrCourse.Url);
                if (expandNode)
                {
                    //sb.Append("<span class=\"arrow\"></span>");
                    sb.AppendFormat(
                        "<li id=\"{0}\" class=\"expandable {1}{2}\"><a style=\"width:100%;\" href=\"{3}{4}\">{5}</a>",
                        id,
                        expandedNodes.Contains(id) || TreeNodeIsExpanded(nodeOrCourse, currentPage) ? "expanded " : "",
                        nodeOrCourse == currentPage ? "current-page active" : "",
                        prefix, nodeOrCourse.Url,
                        nodeOrCourse.GetMetadata<XamUMetadata>()?.NavigationTitle);
                }
                else
                {
                    string classes = nodeOrCourse == currentPage ? " class=\"current-page active" : "";
                    if (!firstNode && nodeOrCourse.Children.Count > 0)
                    {
                        if (classes != "")
                            classes += " expandable\"";
                        else
                            classes = " class=\"expandable\"";
                    }
                    else
                    {
                        if (classes != "")
                            classes += "\"";
                    }

                    sb.AppendFormat("<li id=\"{0}\"{1}><a style=\"width:100%;\" href=\"{2}{3}\">{4}</a>",
                        id, classes,
                        prefix, nodeOrCourse.Url,
                        nodeOrCourse.GetMetadata<XamUMetadata>()?.NavigationTitle);
                }
            }
            else
            {
                if (expandNode)
                {
                    sb.AppendFormat("<li class=\"expandable expanded\"><label>{0}</label>",
                        nodeOrCourse.GetMetadata<XamUMetadata>().NavigationTitle);
                }
                else
                {
                    string classes = firstNode ? " class=\"active" : "";
                    if (nodeOrCourse.Children.Count > 0)
                    {
                        if (classes != "")
                            classes += " expandable\"";
                        else
                            classes = " class=\"expandable\"";
                    }
                    else
                    {
                        if (classes != "")
                            classes += "\"";
                    }
                    sb.AppendFormat("<li{0}><label>{1}</label>",
                        classes,
                        nodeOrCourse.GetMetadata<XamUMetadata>().NavigationTitle);
                }
            }

            if (firstNode)
            {
                foreach (ContentPage child in nodeOrCourse.Children)
                {
                    AddNode(prefix, currentPage, child, sb, expandedNodes, false);
                }
            }

            else if (nodeOrCourse.Children.Count > 0
                && (!nodeOrCourse.IsCourse() || TreeNodeIsExpanded(nodeOrCourse, currentPage)))
            {
                sb.Append("<ul style=\"margin-top:0px;\">");
                foreach (ContentPage child in nodeOrCourse.Children)
                {
                    AddNode(prefix, currentPage, child, sb, expandedNodes, false);
                }
                sb.Append("</ul>");
            }

            sb.Append("</li>");
        }

        /// <summary>
        /// Generate a unique ID for our tree node.
        /// </summary>
        /// <param name="pageId"></param>
        /// <returns></returns>
        static string GenerateId(string pageId)
        {
            return pageId.Replace("/", "-").Trim('-').ToLower();
        }

        /// <summary>
        /// Returns whether the passed parent node should be expanded; this
        /// is false unless it is in the path for the active page.
        /// </summary>
        static bool TreeNodeIsExpanded(ContentPage node, ContentPage currentPage)
        {
            // Root nodes are always expanded.
            if (node.Parent == null)
                return true;

            do
            {
                if (currentPage == node)
                    return true;
                currentPage = currentPage.Parent;

            } while (currentPage != null);

            return false;
        }
    }
}