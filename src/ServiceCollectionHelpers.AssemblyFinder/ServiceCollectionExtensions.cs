﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Services.Common.CommandLine;
using Microsoft.VisualStudio.Services.TestManagement.TestPlanning.WebApi;
using System;
using System.Collections.Generic; 
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ServiceCollectionHelpers.AssemblyFinder
{
    public static class ServiceCollectionExtensions
    {
        #region Methods
        public static IServiceCollection RegisterTypes<T>(this IServiceCollection serviceCollection)
        {
            return serviceCollection.RegisterClassesOfType(typeof(T), new RegisterAsOptions()
            {
                RegisterAs = RegisterAs.Scoped,
                //RegisterOnlyConcreteClass = true
            });
        }

        public static IServiceCollection RegisterClassesOfType<T>(this IServiceCollection serviceCollection, RegisterAsOptions options = null)
        {
            return serviceCollection.RegisterClassesOfType(typeof(T), options ?? new RegisterAsOptions()
            {
                RegisterAs = RegisterAs.Scoped,
                //RegisterOnlyConcreteClass = true
            });
        }

        public static IServiceCollection RegisterClassesOfType(this IServiceCollection serviceCollection, Type assignTypeFrom, RegisterAsOptions options = null)
        {
            // Add default assemblies :
            if(!options.Assemblies.Any())
                options.Assemblies.AddRange(GetAppDomainAssemblies());

            if(assignTypeFrom.IsInterface)
            {
                // We have to find all classes that implements this one
                return serviceCollection.RegisterClassesThatInheritsInterface(assignTypeFrom, options);
            }
            else
            {
                // We have to find all classes that inherits from this one
                return serviceCollection.RegisterClassesThatInheritsFrom(assignTypeFrom, options);
            }
        }

        private static IServiceCollection RegisterClassesThatInheritsInterface(this IServiceCollection serviceCollection, Type interfaceType, RegisterAsOptions options = null)
        {
            var result = new List<Type>();
            try
            {
                foreach (var a in options.Assemblies)
                {
                    foreach (var t in a.GetTypes())
                    {
                        if (!t.IsInterface && t.GetInterfaces().Contains(interfaceType))
                        {
                            if (t.IsClass && !t.IsAbstract)
                            {
                                result.Add(t);
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var msg = string.Join(Environment.NewLine, ex.LoaderExceptions.Select(e => e.Message));

                var fail = new Exception(msg, ex);

                throw fail;
            }

            RegisterTypes(serviceCollection, result, interfaceType, options);

            return serviceCollection;
        }

        private static IServiceCollection RegisterClassesThatInheritsFrom(this IServiceCollection serviceCollection, Type inheritsType, RegisterAsOptions options = null)
        {
            var result = new List<Type>();
            try
            {
                foreach (var a in options.Assemblies)
                {
                    foreach (var t in a.GetTypes())
                    {
                        if (!t.IsInterface && t.GetNestedTypes().Contains(inheritsType))
                        {
                            if (t.IsClass && !t.IsAbstract)
                            {
                                result.Add(t);
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var msg = string.Join(Environment.NewLine, ex.LoaderExceptions.Select(e => e.Message));

                var fail = new Exception(msg, ex);
                Debug.WriteLine(fail.Message, fail);

                throw fail;
            }

            RegisterTypes(serviceCollection, result, inheritsType, options);

            return serviceCollection;
        }

        private static void RegisterTypes(IServiceCollection serviceCollection, List<Type> types, Type serviceType, RegisterAsOptions options)
        {
            foreach (var type in types)
            {
                if (options.RegisterAs == RegisterAs.Transient)
                    serviceCollection.AddScoped(serviceType, type);
                else if (options.RegisterAs == RegisterAs.Scoped)
                    serviceCollection.AddScoped(serviceType, type);
                else if (options.RegisterAs == RegisterAs.Singleton)
                    serviceCollection.AddSingleton(serviceType, type);
                else
                    throw new InvalidOperationException("The RegisterAs option is not recongnised as a valid type. Supported :Transient, Scoped, Singleton.");
            }

        }

        /// <summary>
        /// Gets tne assemblies related to the current implementation.
        /// </summary>
        /// <returns>A list of assemblies that should be loaded by the Nop factory.</returns>
        internal static IList<Assembly> GetAppDomainAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Does type implement generic?
        /// </summary>
        /// <param name="type"></param>
        /// <param name="openGeneric"></param>
        /// <returns></returns>
        internal static bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
        {
            try
            {
                var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
                foreach (var implementedInterface in type.FindInterfaces((objType, objCriteria) => true, null))
                {
                    if (!implementedInterface.IsGenericType)
                        continue;

                    var isMatch = genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition());
                    return isMatch;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
