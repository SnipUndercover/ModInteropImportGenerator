using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ModInteropImportGenerator;

internal static class GenerationHelpers
{
    /// <summary>
    ///   Returns a string which matches the delegate type for a given <see cref="IMethodSymbol"/>,
    ///   like <c>Action&lt;string&gt;</c>.
    /// </summary>
    /// <param name="methodDeclaration">
    ///   The method declaration symbol.
    /// </param>
    internal static string BuildDelegateType(IMethodSymbol methodDeclaration)
    {
        bool returnsVoid = methodDeclaration.ReturnsVoid;

        if (methodDeclaration.Parameters.Length == 0 && returnsVoid)
            return "Action";

        List<string> delegateParameters = methodDeclaration.Parameters.Select(p => p.Type.ToDisplayString()).ToList();
        if (!returnsVoid)
            delegateParameters.Add(methodDeclaration.ReturnType.ToDisplayString());

        return $"{(returnsVoid ? "Action" : "Func")}<{string.Join(", ", delegateParameters)}>";
    }

    internal static void GenerateLoadMethod(SimpleSourceGenerator sourceGen, List<IMethodSymbol> methodsToGenerate)
    {
        sourceGen.WriteLine();
        sourceGen.WriteLine("internal static void Load()");
        sourceGen.WriteLine('{');
        using (sourceGen.UseIndent())
        {
            sourceGen.WriteLine($"typeof({sourceGen.ClassName}).ModInterop();");
            if (sourceGen.ImportMeta.RequiredDependency)
                foreach (var method in methodsToGenerate)
                {
                    string methodName = method.Name;
                    string delegateName = GetGeneratedDelegateFieldName(methodName);
                    sourceGen.WriteLine($"if ({delegateName} is null)");
                    using (sourceGen.UseIndent())
                        sourceGen.WriteLine(GenerateInvalidModInteropException(methodName));
                }
        }
        sourceGen.WriteLine('}');
    }

    /// <summary>
    ///   Generates the delegate field and method implementation for a given partial method definition.
    /// </summary>
    internal static void GenerateMethodImplementation(SimpleSourceGenerator sourceGen, IMethodSymbol methodDeclaration)
    {
        string methodName = methodDeclaration.Name;
        string delegateName = GetGeneratedDelegateFieldName(methodName);

        sourceGen.WriteLine("[DebuggerBrowsable(DebuggerBrowsableState.Never)]");
        sourceGen.WriteLine($$"""[ModImportName($"{ImportName}.{nameof({{methodName}})}")]""");
        sourceGen.WriteLine($"public static {BuildDelegateType(methodDeclaration)} {delegateName};");
        if (sourceGen.ImportMeta.RequiredDependency)
            GenerateRequiredDependencyMethodImplementation(sourceGen, methodDeclaration, delegateName);
        else
            GenerateOptionalDependencyMethodImplementation(sourceGen, methodDeclaration, delegateName);
    }

    internal static void GenerateRequiredDependencyMethodImplementation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol methodDeclaration,
        string delegateName)
    {
        string methodName = methodDeclaration.Name;

        string returnType = methodDeclaration.ReturnsVoid
            ? "void"
            : methodDeclaration.ReturnType.ToDisplayString();

        IEnumerable<string> parameterReferences
            = methodDeclaration.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}");

        string delegateInvocation = $"{delegateName}("
            + $"{string.Join(", ", methodDeclaration.Parameters.Select(p => p.Name))})";

        sourceGen.WriteLine($"public static partial {returnType} {methodName}({string.Join(", ", parameterReferences)})");
        sourceGen.WriteLine('{');
        using (sourceGen.UseIndent())
        {
            // checked in load instead
            // sourceGen.WriteLine($"if ({delegateName} is null)");
            // using (sourceGen.UseIndent())
            //     sourceGen.WriteLine(GenerateInvalidModInteropException(methodName));

            if (!methodDeclaration.ReturnsVoid)
                sourceGen.Write("return ");
            sourceGen.Write(delegateInvocation).WriteLine(';');
        }
        sourceGen.WriteLine('}');
    }

    internal static void GenerateOptionalDependencyMethodImplementation(
        SimpleSourceGenerator sourceGen,
        IMethodSymbol methodDeclaration,
        string delegateName)
    {
        string methodName = methodDeclaration.Name;

        string returnType = methodDeclaration.ReturnsVoid
            ? "void"
            : methodDeclaration.ReturnType.ToDisplayString();

        IEnumerable<string> parameterReferences
            = methodDeclaration.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}");

        string delegateInvocation = $"{delegateName}?.Invoke("
            + $"{string.Join(", ", methodDeclaration.Parameters.Select(p => p.Name))})";

        sourceGen.WriteLine($"public static partial {returnType} {methodName}({string.Join(", ", parameterReferences)})");
        using (sourceGen.UseIndent())
        {
            sourceGen.Write($"=> {delegateInvocation}");
            if (!methodDeclaration.ReturnsVoid)
                sourceGen.Write($" ?? default({returnType})");
            sourceGen.WriteLine(';');
        }
    }

    internal static string GenerateInvalidModInteropException(string methodName)
        => $$"""throw new InvalidOperationException($"Mod import for \"{ImportName}\" has not been loaded, or \"{ImportName}.{nameof({{methodName}})}\" does not exist.");""";

    internal static string GetGeneratedDelegateFieldName(string methodName)
        => $"__Generated_{methodName}";
}
