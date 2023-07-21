using MDPGen.Core.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XamU.SGL.Extensions
{
    /// <summary>
    /// Type to retrieve course information for a given page.
    /// </summary>
    public static class CourseExtensions
    {
        /// <summary>
        /// Retrieve the course title
        /// </summary>
        public static string GetCourseTitle(this ContentPage page)
        {
            if (!page.IsCourse())
                return null;

            var md = page.GetMetadata<XamUMetadata>();
            return (!string.IsNullOrEmpty(md.CourseTitle))
                ? md.CourseTitle
                : md.Title;
        }

        /// <summary>
        /// Returns whether this node represents a course.
        /// </summary>
        public static bool IsCourse(this ContentPage page)
        {
            return page.GetMetadata<XamUMetadata>()?.TopicId != null;
        }

        /// <summary>
        /// Returns the node representing the course this page belongs to.
        /// </summary>
        public static ContentPage GetCourseOwner(this ContentPage startingPage)
        {
            if (startingPage == null)
                throw new ArgumentNullException(nameof(startingPage));

            var page = startingPage;
            while (page?.IsCourse() == false)
            {
                page = page.Parent;
            }

            return page ?? startingPage.Root;
        }

        /// <summary>
        /// The next course in the list after the current course.
        /// </summary>
        public static ContentPage GetNextCourse(this ContentPage page)
        {
            var thisCourse = page.GetCourseOwner();
            if (thisCourse != null)
            {
                var allCourses = page.GetCourses().ToList();
                int pos = allCourses.IndexOf(thisCourse);
                if (pos >= 0 && pos < allCourses.Count - 1)
                {
                    return allCourses[pos + 1];
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve all the courses
        /// </summary>
        /// <param name="page">Page</param>
        /// <returns>All the courses</returns>
        public static IList<ContentPage> GetCourses(this ContentPage page)
        {
            var root = page.Root ?? page;
            return root.Enumerate()
                    .Where(p => p.IsCourse()
                        && p.GetMetadata<XamUMetadata>().CreditSlug != null)
                    .ToList();
        }
    }
}
