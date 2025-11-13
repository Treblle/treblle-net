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
                DebugLogger.LogError($"creating instance of {type.FullName}", ex);
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
            var assemblies = new List<Assembly>();

            // Always include the executing assembly (Treblle.Net.dll) which contains the maskers
            var executingAssembly = Assembly.GetExecutingAssembly();
            assemblies.Add(executingAssembly);

            // Also scan for any additional assemblies in the base directory
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string dll in Directory.GetFiles(baseDirectory, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dll);
                    // Don't add duplicates
                    if (!assemblies.Any(a => a.FullName == assembly.FullName))
                    {
                        assemblies.Add(assembly);
                    }
                }
                catch (Exception ex)
                {
                    // Silently ignore DLLs that can't be loaded (native DLLs, corrupted files, etc.)
                    // Log only in debug mode for troubleshooting masker loading issues
                    DebugLogger.LogWarning($"Could not load assembly {Path.GetFileName(dll)}: {ex.Message}");
                }
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
