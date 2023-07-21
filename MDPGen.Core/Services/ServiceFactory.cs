using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MDPGen.Core.Data;

namespace MDPGen.Core.Services
{
    /// <summary>
    /// Factory which creates the services used by the system.
    /// </summary>
    public sealed class ServiceFactory : IEnumerable<KeyValuePair<Type,TypeDescription>>
    {
        private static readonly Lazy<ServiceFactory> instance = new Lazy<ServiceFactory>(() => new ServiceFactory(), true);
        private readonly ConcurrentDictionary<Type, TypeDescription> registeredServices = new ConcurrentDictionary<Type, TypeDescription>();

        /// <summary>
        /// Returns the implementation fo the service factory.
        /// </summary>
        public static ServiceFactory Instance => instance.Value;

        /// <summary>
        /// Shouldn't create one directly.
        /// </summary>
        private ServiceFactory()
        {
        }

        /// <summary>
        /// True if the service is registered.
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>True if we have an implementation for the service</returns>
        public bool IsRegistered<T>()
        {
            return registeredServices.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Register a service type with an implementation type
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <typeparam name="TImpl">Implementation type</typeparam>
        public void RegisterServiceType<T,TImpl>()
        {
            RegisterServiceType<T>(typeof(TImpl));
        }

        /// <summary>
        /// Register a service type from a string
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="typeName">FQ string of type</param>
        public void RegisterServiceType<T>(string typeName)
        {
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                Type type = LoadType(typeName);
                if (type == null)
                    throw new Exception($"Could not find ({typeof(T).Name}) {typeName}");
                RegisterServiceType<T>(type);
            }
        }

        /// <summary>
        /// Register a service type with an implementation type
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="type">Implementation type</param>
        public void RegisterServiceType<T>(Type type)
        {
            RegisterServiceType<T>(new TypeDescription(type));
        }

        /// <summary>
        /// Register a service type with an implementation type
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <param name="td">Type description with optional properties</param>
        public void RegisterServiceType<T>(TypeDescription td)
        {
            if (!td.ResolvedType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(T)))
                throw new Exception($"{td.Type} does not implement {typeof(T).Name}.");
            registeredServices[typeof(T)] = td;
        }

        /// <summary>
        /// Create the given service.
        /// </summary>
        /// <typeparam name="T">Service type to create</typeparam>
        /// <returns>Instance</returns>
        public T Create<T>()
        {
            TypeDescription type;
            if (registeredServices.TryGetValue(typeof(T), out type))
            {
                return type.Create<T>();
            }

            throw new Exception($"{typeof(T).Name} is not registered in the service factory.");
        }

        /// <summary>
        /// Enumerate the registered services
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<KeyValuePair<Type, TypeDescription>> GetEnumerator()
        {
            return registeredServices.GetEnumerator();
        }

        /// <summary>
        /// Enumerate the registered services
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return registeredServices.GetEnumerator();
        }

        /// <summary>
        /// Identify the Type object from an Assembly name/type.
        /// </summary>
        /// <param name="name">Typename, can be fully qualified.</param>
        /// <returns>Type</returns>
        public static Type LoadType(string name)
        {
            string typeName;
            Assembly asm = null;
            int pos = name.IndexOf(",", StringComparison.Ordinal); // Look for first comma.
            if (pos == -1)
            {
                // Assume it's just a type in the core assembly.
                typeName = name;
                asm = Assembly.GetExecutingAssembly();
            }
            else
            {
                // Includes assembly name.
                typeName = name.Substring(0, pos);
                var asmName = name.Substring(pos + 1);
                if (!string.IsNullOrWhiteSpace(asmName))
                {
                    // Make sure the assembly is loaded.
                    asm = Assembly.Load(asmName);
                }
            }

            if (asm == null)
                throw new Exception($"Could not locate assembly for page loader {name}.");

            return asm.GetType(typeName, true, true);
        }
    }
}
