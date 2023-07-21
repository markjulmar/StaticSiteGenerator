using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MDPGen.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MDPGen.Core.Data
{
    /// <summary>
    /// This class can be used to describe a complex type which should 
    /// be created and setup with JSON. 
    /// </summary>
    public class TypeDescription
    {
        private Type resolvedType;

        /// <summary>
        /// The type name, can include assembly name.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The properties to set on the object.
        /// </summary>
        [JsonProperty("properties")]
        JToken Properties { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TypeDescription()
        {
        }

        /// <summary>
        /// Constructor from a type
        /// </summary>
        /// <param name="t"></param>
        public TypeDescription(Type t)
        {
            resolvedType = t;
            Type = t.FullName;
        }

        /// <summary>
        /// Parse out a type description, either as a string
        /// or as a full type + properties collection
        /// </summary>
        /// <param name="token">Token</param>
        /// <returns>Type description</returns>
        public static TypeDescription FromToken(JToken token)
        {
            // Just a string? or full object?
            return token.Type == JTokenType.String 
                ? new TypeDescription {Type = token.Value<string>()} 
                : token.ToObject<TypeDescription>();
        }

        /// <summary>
        /// The resolved Type
        /// </summary>
        public Type ResolvedType => resolvedType ?? (resolvedType = ServiceFactory.LoadType(this.Type));

        /// <summary>
        /// Creates an object from a TypeDescription
        /// </summary>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <returns>Created object</returns>
        public T Create<T>()
        {
            var o = (T) Activator.CreateInstance(ResolvedType);
            foreach (var p in GetProperties())
            {
                var pi = ResolvedType.GetProperty(p.Item1, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
                if (pi != null)
                {
                    if (pi.PropertyType == p.Item2.GetType())
                    {
                        pi.SetValue(o, p.Item2);
                    }
                    else
                    {
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(pi.PropertyType);
                        object value = typeConverter.ConvertFromString(p.Item2.ToString());
                        pi.SetValue(o, value);
                    }
                }
                else
                {
                    TraceLog.Write(TraceType.Error, $"Failed to find property {p.Item1} on {ResolvedType.Name}");
                }
            }

            return o;
        }


        /// <summary>
        /// This returns a collection of name/value objects
        /// which is deserialized from the Properties collection.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Tuple<string, object>> GetProperties()
        {
            // No values?
            if (Properties == null || !Properties.HasValues)
                return Enumerable.Empty < Tuple<string, object>>();

            return Properties
                .Where(item => item.Type == JTokenType.Property && item.HasValues)
                .Select(item => {
                    var jp = (JProperty)item;

                    object value;
                    // We always turn an array into a List of
                    // string values so we don't have to dig into the types
                    if (jp.Value.Type == JTokenType.Array)
                    {
                        var jv = (JArray)jp.Value;
                        value = jv.Children<JToken>()
                            .Select(t => t.Value<object>().ToString()).ToList();
                    }
                    else
                    {
                        var jv = (JValue)jp.Value;
                        switch (jv.Type)
                        {
                            case JTokenType.Boolean:
                                value = jv.Value<bool>();
                                break;
                            case JTokenType.Float:
                                value = jv.Value<float>();
                                break;
                            case JTokenType.Integer:
                                value = jv.Value<int>();
                                break;
                            default:
                                value = jv.ToString(CultureInfo.InvariantCulture);
                                break;
                        }
                    }

                    return Tuple.Create(jp.Name, value);
                });
        }
    }
}
