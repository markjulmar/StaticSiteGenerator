using MDPGen.Core.Infrastructure;
using System.Collections.Generic;
using MDPGen.Core.Infrastructure.Metadata;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// Page loader for Xamarin University SGL content which uses custom
    /// content page for navigation.
    /// </summary>
    public class XamUPageMetadataLoader : BaseYamlMetadataLoader<XamUMetadata>
    {
        /// <summary>
        /// LoadAsync the metadata from the file.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public override DocumentMetadata Load(ContentPage page)
        {
            var header = base.Load(page) as XamUMetadata;
            FixupLinks(header?.Links);
            return header;
        }

        /// <summary>
        /// This makes sure group names are added to sibling elements.
        /// </summary>
        /// <param name="links">Header links loaded from YAML header</param>
        private void FixupLinks(List<HeaderLink> links)
        {
            if (links == null) return;

            string groupName = string.Empty;
            foreach (var link in links)
            {
                if (link.Group != null)
                    groupName = link.Group;
                else
                    link.Group = groupName;
            }
        }
    }
}
