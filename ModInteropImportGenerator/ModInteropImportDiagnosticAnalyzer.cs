using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using ModInteropImportGenerator.Helpers;

namespace ModInteropImportGenerator;

// see https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModInteropImportDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string PreparedForCodeFixID = "CLII0001";
    internal const string CheckTypeFqn =
        ModInteropImportSourceGenerator.GenerateImportsAttributeFqn;
    internal DiagnosticDescriptor PreparedForCodeFix =
        new(PreparedForCodeFixID, "Import Generator is not good", "Any method in the Import Generator should be partial and not implemented, and containing classes should also be partial", "Usage", DiagnosticSeverity.Warning, true);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [PreparedForCodeFix];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(cxt =>
        {
            if (cxt.Node is AttributeSyntax node
                && node.Parent is AttributeListSyntax list
                && list.Parent is ClassDeclarationSyntax clas
                && cxt.SemanticModel.GetTypeInfo(node).Type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) == CheckTypeFqn
                && (clas.AncestorsAndSelf().Any(ac => ac is ClassDeclarationSyntax c
                        && c.Modifiers.All(mod => !mod.IsKind(SyntaxKind.PartialKeyword)))
                    || clas.Members.Any(mem => mem is MethodDeclarationSyntax method
                        && (method.SemicolonToken == default
                            || method.Body is { }
                            || method.ExpressionBody is { })))
                )
            {
                cxt.ReportDiagnostic(Diagnostic.Create(PreparedForCodeFix, node.GetLocation()));
            }
        }, SyntaxKind.Attribute);
    }
}
