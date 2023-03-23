using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Microsoft.Extensions.DependencyModel;

namespace RoslynAnalysis.Core;

public static class RuntimeHelper
{
    public static IEnumerable<Assembly> GetAllAssembliesEnumerable()
        => DependencyContext.Default.CompileLibraries.Where(lib => !lib.Serviceable && lib.Type != "package").Select(lib => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(lib.Name)));

    public static IEnumerable<Type> GetAllTypes() => GetAllAssembliesEnumerable().SelectMany(assembly => assembly.DefinedTypes.Select(typeinfo => typeinfo.AsType()));
}