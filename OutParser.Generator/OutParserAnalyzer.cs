using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace OutParser.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OutParserAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        new DiagnosticDescriptor(
            id: "OUTP01",
            title: "Type does not implement IParsable or ISpanParsable.",
            messageFormat: "{0} does not implement System.IParsable or System.ISpawnParsable.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "OutParser can not parse the provided type because it does not implement the necessary interfaces."
        ),

        new DiagnosticDescriptor(
            id: "OUTP20",
            title: "String template is not directly provided to Parse call.",
            messageFormat: "String template is not directly provided to Parse call.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "To prepare for the call, the template string has to be provided directly to OutParser"
        ),

        new DiagnosticDescriptor(
            id: "OUTP21",
            title: "Pattern in template does not have a matching out parameter.",
            messageFormat: "{0} exists in the template string, but does not have a matching out parameter.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "OutParser can not link the template string to an out parameter because there is no matching one."
        ),
        new DiagnosticDescriptor(
            id: "OUTP22",
            title: "Out parameter does not have a matching pattern in the template.",
            messageFormat: "{0} exists as an out parameter, but is not present in the template.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "OutParser can not link the out parameter to a matching pattern because there is no matching one."
        ),
        new DiagnosticDescriptor(
            id: "OUTP23",
            title: "Pattern in template occurs multiple times.",
            messageFormat: "{0} occurs two or more times in the template.",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The same pattern is present multiple times in the template. OutParser will only read one of these."
        ),
    ];

    public override void Initialize(AnalysisContext context) {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetOperation(context.Node, context.CancellationToken) is not IInvocationOperation operation) {
            return;
        }

    }

    private void AnalyzeSymbol(SymbolAnalysisContext context) {

    }
}
