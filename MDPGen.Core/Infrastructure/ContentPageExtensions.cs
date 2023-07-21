using System;
using System.Collections.Generic;
using System.Linq;

namespace MDPGen.Core.Infrastructure
{
    /// <summary>
    /// Extensions for the ContentPage type
    /// </summary>
    public static class ContentPageExtensions
    {
        /// <summary>
        /// Locate a ContentPage by the unique ID assigned to it.
        /// </summary>
        /// <param name="page">Starting point</param>
        /// <param name="id">ID to search for</param>
        /// <returns>Located page</returns>
        public static ContentPage FindById(this ContentPage page, string id)
        {
            id = id?.Trim();
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            return page.Enumerate()
                .FirstOrDefault(
                    p => String.Compare(p.Id, id, StringComparison.OrdinalIgnoreCase) == 0);
        }

        /// <summary>
        /// Enumerate the given root node and return all the children.
        /// </summary>
        /// <param name="contentPage">Node to start on</param>
        /// <returns>Flat list of nodes</returns>
        public static IEnumerable<TContentPage> Enumerate<TContentPage>(this TContentPage contentPage) where TContentPage : ContentPage
        {
            if (contentPage == null)
                throw new ArgumentNullException(nameof(contentPage));

            yield return contentPage;
            foreach (var child in contentPage.Children.Cast<TContentPage>())
            {
                foreach (var node in Enumerate(child))
                    yield return node;
            }
        }
    }
}