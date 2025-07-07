using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ModInteropImportGenerator.Helpers;

namespace ModInteropImportGenerator;

internal static class SourceGenerators
{
    internal const string GeneratedModImportClassName = "GeneratedModImport";
    internal const string ImportsLoadedFieldName = "IsImported";
    internal const string LoadMethodName = "Load";

    private static readonly SymbolDisplayFormat PartialMethodImplementationFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat
            .RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)
            .RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeDefaultValue);

    internal static void GenerateLoadMethod(
        SimpleSourceGenerator sourceGen,
        List<IMethodSymbol> methods)
    {
        sourceGen.WriteLine($"internal static void {LoadMethodName}()");
        using var _ = sourceGen.UseCodeBlock();

        sourceGen.WriteLine($"typeof({GeneratedModImportClassName}).ModInterop();");
        sourceGen.WriteLine();

        if (sourceGen.ImportMeta.RequiredDependency)
        {
            foreach (IMethodSymbol method in methods)
            {
                GenerateLoadTimeImportValidatorGuard(sourceGen, method);
                sourceGen.WriteLine();
            }
            sourceGen.WriteLine($"{ImportsLoadedFieldName} = true;");
            return;
        }

        const string ExpectedMethodCountLocalName = "expectedMethodCount";
        const string ActualMethodCountLocalName = "actualMethodCount";
        sourceGen.WriteLine($"const int {ExpectedMethodCountLocalName} = {methods.Count};");
        sourceGen.WriteLine($"int {ActualMethodCountLocalName} = 0;");
        sourceGen.WriteLine();

        foreach (IMethodSymbol method in methods)
        {
            string importReference = method.GetGeneratedImportFieldReference();

            sourceGen.WriteLine($"if ({importReference} is not null)");
            using (sourceGen.UseIndent())
                sourceGen.WriteLine($"{ActualMethodCountLocalName}++;");
        }
        sourceGen.WriteLine();

        sourceGen.WriteLine($"{ImportsLoadedFieldName} = {ActualMethodCountLocalName} switch");
        using (sourceGen.UseCodeBlock(withSemicolon: true))
        {
            sourceGen.WriteLine($"{ExpectedMethodCountLocalName} => true,");
            sourceGen.WriteLine("0 => false,");
            sourceGen.WriteLine("_ => null");
        }
        sourceGen.WriteLine();

        sourceGen.WriteLine($"if ({ImportsLoadedFieldName} is not null)");
        using (sourceGen.UseIndent())
            sourceGen.WriteLine("return;");
        sourceGen.WriteLine();

        foreach (IMethodSymbol method in methods)
        {
            GenerateLoadTimeImportValidatorGuard(sourceGen, method);
            sourceGen.WriteLine();
        }

        sourceGen.WriteLine("throw new UnreachableException(");
        using (sourceGen.UseIndent())
        {
            sourceGen.WriteLine("$\"\"\"");
            sourceGen.WriteLine(
                $"Failed to import \"{sourceGen.ImportMeta.ImportName}\"; "
                + $"imported {{{ActualMethodCountLocalName}}} out of {{{ExpectedMethodCountLocalName}}} methods.");
            sourceGen.WriteLine(
                "This situation was thought to be unreachable; please report this to "
                + $"the {nameof(ModInteropImportGenerator)} repository.");
            sourceGen.WriteLine("\"\"\");");
        }
    }

    private static void GenerateLoadTimeImportValidatorGuard(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        string importReference = method.GetGeneratedImportFieldReference();

        sourceGen.WriteLine($"if ({importReference} is null)");
        using (sourceGen.UseIndent())
            GenerateLoadTimeFailedImportException(sourceGen, method);
    }

    private static void GenerateRuntimeImportValidatorGuard(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        if (!sourceGen.ImportMeta.RequiredDependency)
        {
            sourceGen.WriteLine($"if ({ImportsLoadedFieldName} is false)");
            using (sourceGen.UseIndent())
                GenerateRuntimeFailedImportException(sourceGen, method);
            sourceGen.WriteLine();
        }

        sourceGen.WriteLine($"if ({ImportsLoadedFieldName} is null)");
        using (sourceGen.UseIndent())
            GenerateUnloadedImportException(sourceGen, method);
    }

    private static void GenerateLoadTimeFailedImportException(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        sourceGen.WriteLine("throw new InvalidOperationException(");
        using (sourceGen.UseIndent())
        {
            sourceGen.WriteLine("\"\"\"");
            sourceGen.WriteLine(
                $"No suitable export found for import \"{sourceGen.ImportMeta.ImportName}.{method.Name}\".");
            sourceGen.WriteLine(
                "Check if the export mod is enabled, and that the import name and method definitions "
                + "match that of the export.");
            sourceGen.WriteLine(
                $"[failing method: {method.ToDisplayString(PartialMethodImplementationFormat)}]");
            sourceGen.WriteLine("\"\"\");");
        }
    }

    private static void GenerateRuntimeFailedImportException(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        sourceGen.WriteLine("throw new InvalidOperationException(");
        using (sourceGen.UseIndent())
        {
            sourceGen.WriteLine("\"\"\"");
            sourceGen.WriteLine(
                $"Attempted to call import \"{sourceGen.ImportMeta.ImportName}.{method.Name}\" "
                + $"while the dependency is disabled.");
            sourceGen.WriteLine(
                $"Ensure that \"{sourceGen.ClassName}.{ImportsLoadedFieldName}\" is true before calling this method.");
            sourceGen.WriteLine(
                "If it is, check if the export mod is enabled, and that the import name and method definitions "
                + "match that of the export.");
            sourceGen.WriteLine("\"\"\");");
        }
    }

    private static void GenerateUnloadedImportException(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        sourceGen.WriteLine("throw new NotImplementedException(");
        using (sourceGen.UseIndent())
        {
            sourceGen.WriteLine("\"\"\"");
            sourceGen.WriteLine(
                $"Attempted to call import \"{sourceGen.ImportMeta.ImportName}.{method.Name}\" before importing it.");
            sourceGen.WriteLine($"Ensure that \"{sourceGen.ClassName}.{LoadMethodName}()\" has been called first.");
            sourceGen.WriteLine("\"\"\");");
        }
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

        string methodSignature = method.ToDisplayString(PartialMethodImplementationFormat);
        string importInvocation = method.GetGeneratedImportFieldInvocation();

        sourceGen.WriteLine($"public static partial {methodSignature}");
        using (sourceGen.UseCodeBlock())
        {
            GenerateRuntimeImportValidatorGuard(sourceGen, method);

            sourceGen.WriteLine();
            sourceGen.WriteLine(method.ReturnsVoid
                ? $"{importInvocation};"
                : $"return {importInvocation};");
        }
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
}
