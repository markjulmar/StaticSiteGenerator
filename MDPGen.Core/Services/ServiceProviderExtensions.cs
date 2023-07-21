using System;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Extensions class for IServiceProvider
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Typed version of GetService to retrieve a specific 
        /// service extension by type.
        /// </summary>
        /// <typeparam name="T">Type to retrieve</typeparam>
        /// <param name="sp">IServiceProvider</param>
        /// <returns>Extension or null</returns>
        public static T GetService<T>(this IServiceProvider sp)
        {
            return (T) sp.GetService(typeof(T));
        }
    }
}
