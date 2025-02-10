using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using System.Linq;

namespace Treblle.Net.Helpers
{
    internal static class AssemblyHelper
    {
        internal static object CreateInstance(Type type)
        {
            try
            {
                return Activator.CreateInstance(type); // Works if there's a parameterless constructor
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating instance of {type.FullName}: {ex.Message}");
                return null;
            }
        }

        internal static List<Type> GetClassesDerivedFromType(Type baseType)
        {
            List<Assembly> assemblies = LoadAssemblies();
            List<Type> derivedClasses = assemblies
                .SelectMany(assembly => GetSafeTypes(assembly))
                .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type))
                .ToList();

            return derivedClasses;
        }

        static List<Assembly> LoadAssemblies()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var assemblies = new List<Assembly>();

            foreach (string dll in Directory.GetFiles(baseDirectory, "*.dll"))
            {
                try
                {
                    assemblies.Add(Assembly.LoadFrom(dll));
                }
                catch (Exception) { } // Ignore loading errors
            }

            return assemblies;
        }

        static IEnumerable<Type> GetSafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }
}
