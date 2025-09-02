using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

// see https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1038.md

namespace ModInteropImportGenerator.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeGenerator))]
    public class CodeGenerator : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [ModInteropImportDiagnosticAnalyzer.PreparedForCodeFixID];

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var act =
                CodeAction.Create("Strip Import Generator",
                    cancel => StripAsync(context, cancel),
                    nameof(CodeGenerator));
            context.RegisterCodeFix(act, context.Diagnostics);
            return Task.CompletedTask;
        }

        private static async Task<Document> StripAsync(CodeFixContext context, System.Threading.CancellationToken cancel)
        {
            var model = await context.Document.GetSemanticModelAsync(cancel);
            var root = await context.Document.GetSyntaxRootAsync(cancel);
            if (model == null || root == null)
            {
                return context.Document;
            }

            var _attrNode = root.FindNode(context.Span);
            if (_attrNode is not AttributeSyntax attrNode)
            {
                return context.Document;
            }
            // if it's a nested class, we should mark all containing classes as partial
            var list = attrNode.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().ToList();
            // perform the rest actions in the walker rewriter
            var newRoot = new StripWalker(list).Visit(root);
            return context.Document.WithSyntaxRoot(newRoot);
        }

        public sealed override FixAllProvider? GetFixAllProvider()
        {
            return null;
        }
    }
    class StripWalker(List<ClassDeclarationSyntax> clas)
        : CSharpSyntaxRewriter()
    {
        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // the classes we are interested in are all in the `clas`
            if (clas.Count == 0 || node != clas.Last())
            {
                return node;
            }
            clas.RemoveAt(clas.Count - 1);

            ClassDeclarationSyntax newNode;
            if (clas.Count == 0)
            {
                // walk through all the methods
                // add partial and remove body
                var newMembers = SyntaxFactory.List(node.Members.Select(mem =>
                {
                    if (mem is MethodDeclarationSyntax method)
                    {
                        if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                        {
                            method = method.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                        }
                        return method
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            .WithBody(null)
                            .WithExpressionBody(null);
                    }
                    return mem;
                }));
                newNode = node.WithMembers(newMembers);
            }
            else
            {
                newNode = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            }
            // mark all classes as partial
            if (!newNode.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword)))
            {
                newNode = newNode.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
            }
            return newNode;
        }
    }
}
