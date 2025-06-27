using System;
using Microsoft.CodeAnalysis;

namespace ModInteropImportGenerator.Helpers;

internal static class ParameterHelpers
{
    internal static string GetDefinition(this IParameterSymbol parameter)
    {
        string? refKind = parameter.GetRefKindForDefinition();
        string type = parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        string name = parameter.Name;

        return refKind is not null
            ? $"{refKind} {type} {name}"
            : $"{type} {name}";
    }

    internal static string GetReference(this IParameterSymbol parameter)
    {
        string? refKind = parameter.GetRefKindForReference();
        string name = parameter.Name;

        return refKind is not null
            ? $"{refKind} {name}"
            : name;
    }

    internal static string? GetRefKindForDefinition(this IParameterSymbol parameter)
        => parameter.RefKind switch {
            RefKind.None => null,
            RefKind.Ref => "ref",
            RefKind.Out => "out",
            RefKind.In => "in",
            RefKind.RefReadOnlyParameter => "ref readonly",
            _ => throw new NotSupportedException(
                $"Parameter \"{parameter.Name}\" uses an unsupported {nameof(RefKind)}: {parameter.RefKind}"),
        };

    internal static string? GetRefKindForReference(this IParameterSymbol parameter)
        => parameter.RefKind switch {
            RefKind.None => null,
            RefKind.Ref => "ref",
            RefKind.Out => "out",
            RefKind.In or RefKind.RefReadOnlyParameter => "in",
            _ => throw new NotSupportedException(
                $"Parameter \"{parameter.Name}\" uses an unsupported {nameof(RefKind)}: {parameter.RefKind}"),
        };
}
