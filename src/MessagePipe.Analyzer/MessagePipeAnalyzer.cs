#pragma warning disable RS2008

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace MessagePipe.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessagePipeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: "MPA001",
            title: "MessagePipeAnalyzer001: Don't ignore subscription",
            messageFormat: "Don't ignore subscription(IDisposable of Subscribe)",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Checks handle Subscribe result(IDisposable).");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var types = new[]
            {
                "MessagePipe.ISubscriber`1",
                "MessagePipe.ISubscriber`2",
                "MessagePipe.IAsyncSubscriber`1",
                "MessagePipe.IAsyncSubscriber`2",
                "MessagePipe.IBufferedSubscriber`1",
                "MessagePipe.IBufferedAsyncSubscriber`1",
            };
            var messagePipeSymbols = types.Select(x => context.SemanticModel.Compilation.GetTypeByMetadataName(x)).Where(x => x != null).ToArray();

            var invocationExpressions = context.Node
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var expr in invocationExpressions)
            {
                var memberAccess = (expr.Expression as MemberAccessExpressionSyntax);
                if (memberAccess == null) continue;
                if (memberAccess.Name.Identifier.ValueText != "Subscribe") continue;

                var callerType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
                if (callerType == null) continue;
                var originalType = callerType.OriginalDefinition;
                if (originalType == null) continue;

                if (!IsMessagePipeSymbol(messagePipeSymbols!, originalType)) continue;

                if (ValidateInvocation(expr)) continue;

                // Report Warning
                var diagnostic = Diagnostic.Create(Rule, expr.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        static bool ValidateInvocation(InvocationExpressionSyntax expr)
        {
            bool allAncestorsIsParenthes = true;
            foreach (var x in expr.Ancestors())
            {
                // scope is in lambda, method
                if (x.IsKind(SyntaxKind.SimpleLambdaExpression) || x.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || x.IsKind(SyntaxKind.ArrowExpressionClause))
                {
                    // () => M()
                    if (allAncestorsIsParenthes) return true;
                    break;
                }
                if (x.IsKind(SyntaxKind.MethodDeclaration)) break;
                if (x.IsKind(SyntaxKind.PropertyDeclaration)) break;
                if (x.IsKind(SyntaxKind.ConstructorDeclaration)) break;

                // x = M()
                if (x.IsKind(SyntaxKind.SimpleAssignmentExpression)) return true;
                // var x = M()
                if (x.IsKind(SyntaxKind.VariableDeclarator)) return true;
                // return M()
                if (x.IsKind(SyntaxKind.ReturnStatement)) return true;
                // from x in M()
                if (x.IsKind(SyntaxKind.FromClause)) return true;
                // (bool) ? M() : M()
                if (x.IsKind(SyntaxKind.ConditionalExpression)) return true;
                // M(M())
                if (x.IsKind(SyntaxKind.InvocationExpression)) return true;
                // new C(M())
                if (x.IsKind(SyntaxKind.ObjectCreationExpression)) return true;
                // using(M())
                if (x.IsKind(SyntaxKind.UsingStatement)) return true;

                // (((((M()))))
                if (!x.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    allAncestorsIsParenthes = false;
                }
            }

            return false;
        }

        static bool IsMessagePipeSymbol(INamedTypeSymbol[] messagePipeSymbols, ITypeSymbol typeSymbol)
        {
            foreach (var item in messagePipeSymbols)
            {
                if (item.Equals(typeSymbol, SymbolEqualityComparer.Default)) return true;
            }

            return false;
        }
    }
}
