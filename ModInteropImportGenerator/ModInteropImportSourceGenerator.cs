using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ModInteropImportGenerator;

[Generator]
public class ModInteropImportSourceGenerator : IIncrementalGenerator
{
    // see https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md

    private const string GenerateImportsAttributeFqn
        = $"{nameof(ModInteropImportGenerator)}.{nameof(GenerateImportsAttribute)}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // add the attribute so that IDEs can pick up on it and let the user use it
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "GenerateImportsAttribute.g.cs",
            SourceText.From(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("GenerateImportsAttribute.cs")!,
                Encoding.UTF8,
                throwIfBinaryDetected: true,
                canBeEmbedded: true)));

        // get our syntax provider, filtering only for classes annotated with the [GenerateImports] attribute.
        // only filtered syntax nodes can trigger code generation.
        // there's a convenient ForAttributeWithMetadataName which is
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenerateImportsAttributeFqn,
            static (node, _) => node is ClassDeclarationSyntax, // we can return `true` here but let's be sure
            static (syntaxContext, _) => GetClassDeclarationForSourceGen(syntaxContext));

        // register the source code generator
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    /// <summary>
    ///   Checks whether the Node is annotated with the [<see cref="GenerateImportsAttribute"/>] attribute
    ///   and maps syntax context to the specific node type (ClassDeclarationSyntax).
    /// </summary>
    /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
    /// <returns>The specific cast and whether the attribute was found.</returns>
    private static (ClassDeclarationSyntax classDeclaration, ModImportMetadata importMeta)
        GetClassDeclarationForSourceGen(GeneratorAttributeSyntaxContext context)
    {
        // we already know this is a ClassDeclarationSyntax from the filter in Initialize
        // so this cast is safe
        var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;

        Debug.WriteLine($"Checking declaration of class \"{classDeclaration.Identifier.Text}\"...");

        foreach (AttributeData attribute in context.Attributes)
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (attributeName != GenerateImportsAttributeFqn)
            {
                Debug.WriteLine($"Skipping attribute \"{attributeName}\".");
                continue;
            }
            Debug.WriteLine($"Found attribute \"{GenerateImportsAttributeFqn}\".");

            var ctorArgs = attribute.ConstructorArguments;
            if (ctorArgs.Length < 1)
            {
                Debug.WriteLine("Constructor argument count is less than 1, skipping.");
                continue;
            }

            var modImportNameArgument = ctorArgs[0];
            if (modImportNameArgument.Kind == TypedConstantKind.Error)
            {
                Debug.WriteLine("Import name argument is in error, skipping.");
                continue;
            }

            if (modImportNameArgument.Value is not string modImportName)
            {
                Debug.WriteLine("Import name is not a string, skipping.");
                continue;
            }
            Debug.WriteLine($"Found mod import name: \"{modImportName}\"");

            var requiredDependency = false;
            var namedArguments = attribute.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (namedArguments.TryGetValue("RequiredDependency", out var requiredDependencyArgument))
            {
                if (requiredDependencyArgument.Kind == TypedConstantKind.Error)
                {
                    Debug.WriteLine("Required dependency argument is in error, skipping.");
                    continue;
                }

                if (requiredDependencyArgument.Value is not bool requiredDependencyValue)
                {
                    Debug.WriteLine("Required dependency argument is not a bool, skipping.");
                    continue;
                }

                requiredDependency = requiredDependencyValue;
                Debug.WriteLine($"RequiredDependency defined as {requiredDependency}.");
            }
            else
                Debug.WriteLine("RequiredDependency not defined, treating as false.");

            return (classDeclaration, new ModImportMetadata(modImportName, requiredDependency));
        }

        throw new InvalidOperationException($"Cannot find {GenerateImportsAttributeFqn} attribute!");
    }

    private void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<(ClassDeclarationSyntax classDeclaration, ModImportMetadata importMeta)> modImportDeclarations)
    {
        // go through all the filtered class declarations
        foreach ((ClassDeclarationSyntax classDeclaration, ModImportMetadata importMeta) in modImportDeclarations)
        {
            // we need to get semantic model of the class to retrieve metadata
            SemanticModel model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            // symbols allow us to get the compile-time information
            if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                continue;

            SimpleSourceGenerator sourceGen = new(classDeclaration, compilation, importMeta);

            List<IMethodSymbol> methodsToImport = classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsPartialDefinition && m.PartialImplementationPart is null)
                .ToList();

            sourceGen.AddUsings("System", "System.Diagnostics", "MonoMod.ModInterop");

            sourceGen.WriteLine($"public static partial class {sourceGen.ClassName}");
            using (sourceGen.UseCodeBlock())
            {
                SourceGenerators.GenerateMethodImplementations(sourceGen, methodsToImport);
                sourceGen.WriteLine();
                SourceGenerators.GenerateLoadMethod(sourceGen, methodsToImport);
            }
            sourceGen.WriteLine();

            sourceGen.WriteLine($"[ModImportName({SourceGenerators.GeneratedModImportClassName}.ImportName)]");
            sourceGen.WriteLine($"file class {SourceGenerators.GeneratedModImportClassName}");
            using (sourceGen.UseCodeBlock())
            {
                sourceGen.WriteLine($"public const string ImportName = \"{importMeta.ImportName}\";");
                sourceGen.WriteLine();

                SourceGenerators.GenerateImportFields(sourceGen, methodsToImport);
            }

            // add the source code to the compilation
            context.AddSource($"{sourceGen.ClassName}.g.cs", SourceText.From(sourceGen.Generate(), Encoding.UTF8));
        }
    }
}
