using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ModInteropImportGenerator.Helpers;

internal static class MethodHelpers
{
    internal static string GetGeneratedDelegateName(this IMethodSymbol method)
        => $"{method.Name}Delegate";

    internal static string GetReturnTypeName(this IMethodSymbol method)
        => method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    internal static IEnumerable<string> GetParameterReferences(this IMethodSymbol method)
        => method.Parameters
            .Select(p => p.Name);

    internal static IEnumerable<string> GetParameterDefinitions(this IMethodSymbol method)
        => method.Parameters
            .Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}");
}
