using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ModInteropImportGenerator.Helpers;

internal static class MethodHelpers
{
    private static readonly Dictionary<string, int> MethodNameToIndex = [];
    private static readonly Dictionary<IMethodSymbol, string> MethodToImportName = [];

    internal static void ClearGeneratedNameCache()
    {
        MethodNameToIndex.Clear();
        MethodToImportName.Clear();
    }

    internal static string GetGeneratedImportFieldName(this IMethodSymbol method)
    {
        if (MethodToImportName.TryGetValue(method, out string name))
            return name;

        string methodName = method.Name;
        if (MethodNameToIndex.TryGetValue(methodName, out int index))
            methodName += index++;
        else
            index = 0;

        MethodNameToIndex[method.Name] = index;
        return MethodToImportName[method] = methodName;
    }

    internal static string GetGeneratedImportDelegateName(this IMethodSymbol method)
        => $"{GetGeneratedImportFieldName(method)}Delegate";

    internal static string GetReturnTypeName(this IMethodSymbol method)
        => method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    internal static IEnumerable<string> GetParameterReferences(this IMethodSymbol method)
        => method.Parameters
            .Select(p => p.Name);

    internal static IEnumerable<string> GetParameterDefinitions(this IMethodSymbol method)
        => method.Parameters
            .Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}");
}
