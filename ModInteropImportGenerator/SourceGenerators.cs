using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ModInteropImportGenerator.Helpers;

namespace ModInteropImportGenerator;

internal static class SourceGenerators
{
    internal const string GeneratedModImportClassName = "GeneratedModImport";

    internal static void GenerateLoadMethod(
        SimpleSourceGenerator sourceGen,
        List<IMethodSymbol> methods)
    {
        sourceGen.WriteLine("internal static void Load()");
        using var _ = sourceGen.UseCodeBlock();

        sourceGen.WriteLine($"typeof({GeneratedModImportClassName}).ModInterop();");
        if (!sourceGen.ImportMeta.RequiredDependency)
            return;

        foreach (IMethodSymbol method in methods)
        {
            sourceGen.WriteLine();
            GenerateImportValidatorGuard(sourceGen, method);
        }
    }

    private static void GenerateImportValidatorGuard(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        string fieldName = method.GetGeneratedImportFieldName();

        sourceGen.WriteLine($"if ({GeneratedModImportClassName}.{fieldName} is null)");
        using (sourceGen.UseIndent())
            sourceGen.WriteLine(GenerateInvalidModInteropException(method));
    }

    internal static void GenerateMethodImplementations(
        SimpleSourceGenerator sourceGen,
        List<IMethodSymbol> methods)
    {
        bool first = true;
        foreach (IMethodSymbol method in methods)
        {
            if (first)
                first = false;
            else
                sourceGen.WriteLine();

            GenerateMethodImplementation(sourceGen, method);
        }
    }

    private static void GenerateMethodImplementation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        if (!method.ReturnsVoid)
            sourceGen.TryAddUsingFor(method.ReturnType);

        foreach (IParameterSymbol parameter in method.Parameters)
            sourceGen.TryAddUsingFor(parameter.Type);

        string returnType = method.GetReturnType();
        IEnumerable<string> parameterDefinitions = method.GetParameterDefinitions();

        sourceGen.WriteLine(
            $"public static partial {returnType} {method.Name}({string.Join(", ", parameterDefinitions)})");
        if (sourceGen.ImportMeta.RequiredDependency)
            GenerateRequiredMethodImplementation(sourceGen, method);
        else
            GenerateOptionalMethodImplementation(sourceGen, method);
    }

    private static void GenerateRequiredMethodImplementation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        using var _ = sourceGen.UseIndent();
        string fieldName = method.GetGeneratedImportFieldName();
        IEnumerable<string> parameterReferences = method.GetParameterReferences();

        sourceGen.WriteLine($"=> {GeneratedModImportClassName}.{fieldName}({string.Join(", ", parameterReferences)});");
    }

    private static void GenerateOptionalMethodImplementation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        if (method.HasOutParameters())
            GenerateOptionalMethodImplementationWithOutParams(sourceGen, method);
        else
            GenerateOptionalMethodImplementationWithoutOutParams(sourceGen, method);
    }

    private static void GenerateOptionalMethodImplementationWithoutOutParams(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        using var _ = sourceGen.UseIndent();
        string fieldName = method.GetGeneratedImportFieldName();
        IEnumerable<string> parameterReferences = method.GetParameterReferences();

        sourceGen.Write(
            $"=> {GeneratedModImportClassName}.{fieldName}?.Invoke({string.Join(", ", parameterReferences)})");
        if (method.ReturnsVoid)
        {
            sourceGen.WriteLine(';');
            return;
        }
        sourceGen.WriteLine();

        using (sourceGen.UseIndent())
            sourceGen.WriteLine($"?? default({method.GetReturnType()});");
    }

    private static void GenerateOptionalMethodImplementationWithOutParams(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        using var _ = sourceGen.UseCodeBlock();
        string fieldName = method.GetGeneratedImportFieldName();
        IEnumerable<string> parameterReferences = method.GetParameterReferences();
        bool returnsVoid = method.ReturnsVoid;

        string importReference = $"{GeneratedModImportClassName}.{fieldName}";
        string invocation = $"{importReference}({string.Join(", ", parameterReferences)})";

        sourceGen.WriteLine($"if ({importReference} is not null)");
        if (!returnsVoid)
            using (sourceGen.UseIndent())
                sourceGen.WriteLine($"return {invocation};");
        else
            using (sourceGen.UseCodeBlock())
            {
                sourceGen.WriteLine($"{invocation};");
                sourceGen.WriteLine("return;");
            }

        sourceGen.WriteLine();
        foreach (IParameterSymbol outParameter in method.GetOutParameters())
            sourceGen.WriteLine($"{outParameter.Name} = default({
                outParameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            });");

        if (returnsVoid)
            return;

        sourceGen.WriteLine();
        sourceGen.WriteLine($"return default({method.GetReturnType()});");
    }

    internal static void GenerateImportFields(
        SimpleSourceGenerator sourceGen,
        List<IMethodSymbol> methods)
    {
        bool first = true;
        foreach (IMethodSymbol method in methods)
        {
            if (first)
                first = false;
            else
                sourceGen.WriteLine();

            GenerateImportField(sourceGen, method);
        }
    }

    private static void GenerateImportField(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        GenerateImportDelegateDefinition(sourceGen, method);
        GenerateImportFieldDefinition(sourceGen, method);
    }

    private static void GenerateImportDelegateDefinition(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        if (!method.ReturnsVoid)
            sourceGen.TryAddUsingFor(method.ReturnType);

        foreach (IParameterSymbol parameter in method.Parameters)
            sourceGen.TryAddUsingFor(parameter.Type);

        string delegateName = method.GetGeneratedImportDelegateName();
        string returnType = method.GetReturnType();
        IEnumerable<string> parameterDefinitions = method.GetParameterDefinitions();

        sourceGen.WriteLine($"public delegate {returnType} {delegateName}({string.Join(", ", parameterDefinitions)});");
    }

    private static void GenerateImportFieldDefinition(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        string delegateName = method.GetGeneratedImportDelegateName();
        string fieldName = method.GetGeneratedImportFieldName();

        sourceGen.WriteLine($"public static {delegateName} {fieldName};");
    }

    internal static string GenerateInvalidModInteropException(IMethodSymbol method)
    {
        const string ImportNameIdentifier = $"{GeneratedModImportClassName}.ImportName";

        return $$"""throw new InvalidOperationException($"One or more import definitions for \"{{{
            ImportNameIdentifier
        }}}.{nameof({{
            method.Name
        }})}\" did not load correctly. Check the import name and method definitions.");""";
    }
}
