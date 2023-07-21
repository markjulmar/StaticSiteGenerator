using MDPGen.Core.Infrastructure;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// The YAML header block for the XamU SGL content.
    /// </summary>
    public class XamUMetadata : DocumentMetadata
    {
        string navigationTitle;

        /// <summary>
        /// Optional course title
        /// </summary>
        [YamlMember(Alias = "course-title", ApplyNamingConventions = false)]
        public string CourseTitle { get; set; }

        /// <summary>
        /// Topic identifier from the XamU database
        /// </summary>
        public int? TopicId { get; set; }

        /// <summary>
        /// Slug used to give credit for the course
        /// </summary>
        public string CreditSlug { get; set; }

        /// <summary>
        /// Optional navigation title to be used in the 
        /// navigation tree. If not supplied, we use Title.
        /// </summary>
        [YamlMember(Alias = "nav-title", ApplyNamingConventions = false)]
        public string NavigationTitle
        {
            get => navigationTitle ?? Title;
            set => navigationTitle = value;
        }

        /// <summary>
        /// List of the additional links/resources for this page.
        /// </summary>
        public List<HeaderLink> Links { get; set; } = new List<HeaderLink>();
    }
}
