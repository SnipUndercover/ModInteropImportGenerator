using System;
using Microsoft.CodeAnalysis;

namespace ModInteropImportGenerator.Helpers;

internal static class ParameterHelpers
{
    private static readonly SymbolDisplayFormat ParameterReferenceDisplayFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat
            .WithParameterOptions(SymbolDisplayParameterOptions.IncludeName);

    internal static string GetDefinition(this IParameterSymbol parameter)
        => parameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    internal static string GetReference(this IParameterSymbol parameter)
    {
        string? refKind = parameter.GetRefKindForReference();
        string name = parameter.ToDisplayString(ParameterReferenceDisplayFormat);

        return refKind is not null
            ? $"{refKind} {name}"
            : name;
    }

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
