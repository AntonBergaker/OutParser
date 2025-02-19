using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace OutParser.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class OutParserAnalyzer : DiagnosticAnalyzer {
    private static DiagnosticDescriptor _diagnosticTypeDoesNotImplement = new(
        id: "OUTP01",
        title: "Type does not implement IParsable or ISpanParsable",
        messageFormat: "{0} does not implement System.IParsable or System.ISpawnParsable",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "OutParser can not parse the provided type because it does not implement the necessary interfaces."
    );

    private static readonly DiagnosticDescriptor _diagnosticTemplateNotDirectLiteral = new(
        id: "OUTP20",
        title: "String template is not directly provided to Parse call as a literal",
        messageFormat: "String template is not directly provided to Parse call",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "To prepare for the call, the template string has to be provided directly to OutParser."
    );

    private static readonly DiagnosticDescriptor _diagnosticPatternMissingOut = new(
        id: "OUTP21",
        title: "Pattern in template does not have a matching out parameter",
        messageFormat: "{0} exists in the template string, but does not have a matching out parameter",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "OutParser can not link the template string to an out parameter because there is no matching one."
    );

    private static readonly DiagnosticDescriptor _diagnosticOutMissingPattern = new(
        id: "OUTP22",
        title: "Out parameter does not have a matching pattern in the template",
        messageFormat: "{0} exists as an out parameter, but is not present in the template",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "OutParser can not link the out parameter to a matching pattern because there is no matching one."
    );

    private static readonly DiagnosticDescriptor _diagnosticPatternRepeats = new(
        id: "OUTP23",
        title: "Pattern in template occurs multiple times",
        messageFormat: "{0} occurs two or more times in the template",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The same pattern is present multiple times in the template. OutParser will only read one of these."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        _diagnosticTypeDoesNotImplement,
        _diagnosticTemplateNotDirectLiteral,
        _diagnosticPatternMissingOut,
        _diagnosticOutMissingPattern,
        _diagnosticPatternRepeats
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    private class Reporter : IParserCallReporter {
        private readonly SyntaxNodeAnalysisContext _context;

        public Reporter(SyntaxNodeAnalysisContext context) {
            _context = context;
        }

        public void ReportPatternMissingOut(Location location, string pattern) {
            _context.ReportDiagnostic(Diagnostic.Create(_diagnosticPatternMissingOut, location, [pattern]));
        }

        public void ReportOutMissingPattern(Location location, string outName){
            _context.ReportDiagnostic(Diagnostic.Create(_diagnosticOutMissingPattern, location, [outName]));
        }

        public void ReportPatternRepeats(Location location, string pattern){
            _context.ReportDiagnostic(Diagnostic.Create(_diagnosticPatternRepeats, location, [pattern]));
        }

        public void ReportTemplateNotLiteral(Location location) {
            _context.ReportDiagnostic(Diagnostic.Create(_diagnosticTemplateNotDirectLiteral, location));
        }

        public void ReportTypeNotParsable(Location location, string name) {
            _context.ReportDiagnostic(Diagnostic.Create(_diagnosticTypeDoesNotImplement, location, [name]));
        }
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context) {
        var reporter = new Reporter(context);
        SyntaxReading.GetParserCall(context.Node, context.SemanticModel, reporter, context.CancellationToken);
    }

}
