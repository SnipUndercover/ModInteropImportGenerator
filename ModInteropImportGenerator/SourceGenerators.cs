using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ModInteropImportGenerator.Helpers;

namespace ModInteropImportGenerator;

internal static class SourceGenerators
{
    private const string ImportStateNotImported
        = ModInteropImportSourceGenerator.ImportStateNotImportedEnumReference;
    private const string ImportStateOk
        = ModInteropImportSourceGenerator.ImportStateOkEnumReference;
    [Obsolete($"This state is currently unused. Use {nameof(ImportStateFailedImport)} instead.")]
    private const string ImportStateDependencyNotPresent
        = ModInteropImportSourceGenerator.ImportStateDependencyNotPresentEnumReference;
    private const string ImportStateFailedImport
        = ModInteropImportSourceGenerator.ImportStateFailedImportEnumReference;
    private const string ImportStatePartialImport
        = ModInteropImportSourceGenerator.ImportStatePartialImportEnumReference;
    private const string ImportStateUnknownFailure
        = ModInteropImportSourceGenerator.ImportStateUnknownFailureEnumReference;

    internal const string GeneratedModImportClassName = "GeneratedModImport";
    internal const string ImportsLoadedFieldName = "IsImported";
    internal const string ImportStateFieldName = "ImportState";
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

        const string ExpectedMethodCountLocalName = "expectedMethodCount";
        const string ActualMethodCountLocalName = "actualMethodCount";
        sourceGen.WriteLine($"const uint {ExpectedMethodCountLocalName} = {methods.Count};");
        sourceGen.WriteLine($"uint {ActualMethodCountLocalName} = 0;");
        sourceGen.WriteLine();

        foreach (IMethodSymbol method in methods)
        {
            string importReference = method.GetGeneratedImportFieldReference();

            sourceGen.WriteLine($"if ({importReference} is not null)");
            using (sourceGen.UseIndent())
                sourceGen.WriteLine($"{ActualMethodCountLocalName}++;");
        }
        sourceGen.WriteLine();

        sourceGen.WriteLine($"{ImportStateFieldName} = {ActualMethodCountLocalName} switch");
        using (sourceGen.UseCodeBlock(withSemicolon: true))
        {
            sourceGen.WriteLine($"{ExpectedMethodCountLocalName} => {ImportStateOk},");
            sourceGen.WriteLine($"0 => {ImportStateFailedImport},");
            sourceGen.WriteLine($"_ => {ImportStatePartialImport}");
        }
        sourceGen.WriteLine($"{ImportsLoadedFieldName} = {ImportStateFieldName} == {ImportStateOk};");
        sourceGen.WriteLine();

        if (!sourceGen.ImportMeta.RequiredDependency)
        {
            sourceGen.Write($"if ({ImportStateFieldName} is {ImportStateOk} or {ImportStateFailedImport})");
            using (sourceGen.UseIndent())
                sourceGen.WriteLine("return;");
        }
        else
        {
            sourceGen.Write($"if ({ImportStateFieldName} == {ImportStateOk})");
            using (sourceGen.UseIndent())
                sourceGen.WriteLine("return;");

            sourceGen.Write($"if ({ImportStateFieldName} == {ImportStateFailedImport})");
            using (sourceGen.UseIndent())
            {
                sourceGen.WriteLine("throw new InvalidOperationException(");
                using (sourceGen.UseIndent())
                {
                    sourceGen.WriteLine("$\"\"\"");
                    sourceGen.WriteLine(
                        $"Failed to import \"{sourceGen.ImportMeta.ImportName}\"; imported "
                        + $"{{{ActualMethodCountLocalName}}} out of "
                        + $"{{{ExpectedMethodCountLocalName}}} methods.");
                    sourceGen.WriteLine(
                        "Check that the dependency is present, the import name matches the export name, and that "
                        + "the import methods' names and signatures match with the export methods.");
                    sourceGen.WriteLine("\"\"\");");
                }
            }
        }

        sourceGen.WriteLine();

        foreach (IMethodSymbol method in methods)
        {
            GenerateLoadTimeImportValidatorGuard(sourceGen, method);
            sourceGen.WriteLine();
        }

        sourceGen.WriteLine($"{ImportStateFieldName} = {ImportStateUnknownFailure};");
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
        using var _ = sourceGen.UseIndent();

        sourceGen.WriteLine("throw new InvalidOperationException(");
        using var __ = sourceGen.UseIndent();

        sourceGen.WriteLine("\"\"\"");
        sourceGen.WriteLine(
            $"No suitable export method found for import method \"{sourceGen.ImportMeta.ImportName}.{method.Name}\".");
        sourceGen.WriteLine(
            "Check that the dependency is present, the import name matches the export name, and that the "
            + "import methods' names and signatures match with the export methods.");
        sourceGen.WriteLine(
            $"[failing method: {method.ToDisplayString(PartialMethodImplementationFormat)}]");
        sourceGen.WriteLine("\"\"\");");
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

        sourceGen.WriteLine($"public static partial {methodSignature}");
        using (sourceGen.UseCodeBlock())
        {
            GenerateImportInvocation(sourceGen, method);
            sourceGen.WriteLine();
            GenerateFailedImportStateGuard(sourceGen, method);
            sourceGen.WriteLine();
            GenerateNotImportedStateGuard(sourceGen, method);
            sourceGen.WriteLine();
            GenerateCatchAllGuard(sourceGen, method);
        }
    }

    private static void GenerateImportInvocation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        string importInvocation = method.GetGeneratedImportFieldInvocation();

        sourceGen.WriteLine(
            $"if ({ImportStateFieldName} == {ImportStateOk})");

        if (!method.ReturnsVoid)
        {
            using (sourceGen.UseIndent())
                sourceGen.WriteLine($"return {importInvocation};");
            return;
        }

        using (sourceGen.UseCodeBlock())
        {
            sourceGen.WriteLine($"{importInvocation};");
            sourceGen.WriteLine("return;");
        }
    }

    private static void GenerateFailedImportStateGuard(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        sourceGen.WriteLine(
            $"if ({ImportStateFieldName} == {ImportStateFailedImport})");
        using var _ = sourceGen.UseIndent();

        sourceGen.WriteLine("throw new InvalidOperationException(");
        using var __ = sourceGen.UseIndent();

        sourceGen.WriteLine("\"\"\"");
        sourceGen.WriteLine(
            $"Attempted to call import method \"{sourceGen.ImportMeta.ImportName}.{method.Name}\" "
            + $"but the import was not successful.");
        sourceGen.WriteLine(
            $"Ensure that \"{sourceGen.ClassName}.{ImportsLoadedFieldName}\" is true before calling the import method.");
        sourceGen.WriteLine(
            "If it is, check that the dependency is present, the import name matches the export name, "
            + "and that the import methods' names and signatures match with the export methods.");
        sourceGen.WriteLine("\"\"\");");
    }

    private static void GenerateNotImportedStateGuard(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        sourceGen.WriteLine($"if ({ImportStateFieldName} == {ImportStateNotImported})");
        using var _ = sourceGen.UseIndent();

        sourceGen.WriteLine("throw new NotImplementedException(");
        using var __ = sourceGen.UseIndent();

        sourceGen.WriteLine("\"\"\"");
        sourceGen.WriteLine(
            $"Attempted to call import \"{sourceGen.ImportMeta.ImportName}.{method.Name}\" before importing it.");
        sourceGen.WriteLine($"Ensure that \"{sourceGen.ClassName}.{LoadMethodName}()\" has been called first.");
        sourceGen.WriteLine("\"\"\");");
    }

    private static void GenerateCatchAllGuard(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol method)
    {
        sourceGen.WriteLine("throw new InvalidOperationException(");
        using (sourceGen.UseIndent())
        {
            sourceGen.WriteLine("\"\"\"");
            sourceGen.WriteLine(
                $"Attempted to call import \"{sourceGen.ImportMeta.ImportName}.{method.Name}\", "
                + $"but the import class is in an invalid state.");
            sourceGen.WriteLine($"Check the value of the \"{ImportStateFieldName}\" field for the cause.");
            sourceGen.WriteLine("\"\"\");");
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
