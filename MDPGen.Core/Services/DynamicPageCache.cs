using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Cache object which can be used with dynamic
    /// programming to act as an object with properties.
    /// </summary>
    public class DynamicPageCache : DynamicObject
    {
        /// <summary>
        /// Dictionary for the cache
        /// </summary>
        public ConcurrentDictionary<string, object> Properties { get; } = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Add a range of the values to the object.
        /// </summary>
        /// <param name="values">Values to add</param>
        public void AddRange(IEnumerable<KeyValuePair<string, object>> values)
        {
            foreach (KeyValuePair<string, object> pair in values)
            {
                Properties[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Add a single value to our dictionary
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public bool AddValue(string propertyName, object value)
        {
            return this.Properties.TryAdd(propertyName, value);
        }

        /// <summary>
        /// Return all the valid dynamic member names.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetDynamicMemberNames() => this.Properties.Keys;

        /// <summary>
        /// Reset the cache object
        /// </summary>
        public void Reset()
        {
            this.Properties.Clear();
        }

        /// <summary>
        /// Retrieve a value from the dictionary
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            // first check the properties collection for member
            if (Properties.Keys.Contains(binder.Name))
            {
                result = Properties[binder.Name];
            }

            // Always return valid value.
            return true;
        }

        /// <summary>
        /// Set a value into the dictionary
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Properties[binder.Name] = value;
            return true;
        }
    }
}
