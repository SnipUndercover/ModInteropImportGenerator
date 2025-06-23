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
        sourceGen.WriteLine($"if ({GeneratedModImportClassName}.{method.Name} is null)");
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

        string returnType = method.GetReturnTypeName();
        IEnumerable<string> parameterDefinitions = method.GetParameterDefinitions();

        sourceGen.WriteLine($"public static partial {returnType} {method.Name}({string.Join(", ", parameterDefinitions)})");
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
        IEnumerable<string> parameterReferences = method.GetParameterReferences();

        sourceGen.WriteLine($"=> {GeneratedModImportClassName}.{method.Name}({string.Join(", ", parameterReferences)});");
    }

    private static void GenerateOptionalMethodImplementation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        using var _ = sourceGen.UseIndent();
        IEnumerable<string> parameterReferences = method.GetParameterReferences();

        sourceGen.Write($"=> {GeneratedModImportClassName}.{method.Name}?.Invoke({string.Join(", ", parameterReferences)})");
        if (method.ReturnsVoid)
        {
            sourceGen.WriteLine(';');
            return;
        }
        sourceGen.WriteLine();

        using var __ = sourceGen.UseIndent();
        sourceGen.WriteLine($"?? default({method.GetReturnTypeName()});");
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

        string delegateName = method.GetGeneratedDelegateName();
        string returnType = method.GetReturnTypeName();
        IEnumerable<string> parameterDefinitions = method.GetParameterDefinitions();

        sourceGen.WriteLine($"public delegate {returnType} {delegateName}({string.Join(", ", parameterDefinitions)});");
    }

    private static void GenerateImportFieldDefinition(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        string delegateName = method.GetGeneratedDelegateName();

        sourceGen.WriteLine($"public static {delegateName} {method.Name};");
    }

    internal static string GenerateInvalidModInteropException(IMethodSymbol method)
    {
        const string ImportNameIdentifier = $"{GeneratedModImportClassName}.ImportName";

        return $$"""throw new InvalidOperationException($"Mod import for \"{{{ImportNameIdentifier}}}\" has not been loaded, or \"{{{ImportNameIdentifier}}}.{nameof({{method.Name}})}\" does not exist.");""";
    }
}
