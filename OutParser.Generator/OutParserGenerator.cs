using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace OutParser.Generator;

[Generator]
public class OutParserGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(InitializationOutput);

        var interpolationCalls = context.SyntaxProvider.CreateSyntaxProvider(FindParseCalls, FindParseCallsTransform).NotNull().Collect();

        context.RegisterImplementationSourceOutput(interpolationCalls, EmitParseCallsCode);

    }

    private void InitializationOutput(IncrementalGeneratorPostInitializationContext context) {
        context.AddSource("OutParser.Core.g.cs", InitializationContent.GetContent());

        var codeBuilder = new CodeBuilder();
        codeBuilder.AddLine("#nullable enable");
        codeBuilder.StartBlock("namespace OutParsing");
        codeBuilder.StartBlock("partial class OutParser");

        // Generate 12 parse methods
        codeBuilder.AddLine("public static void Parse(string input, string template) { }");
        for (int i = 1; i <= 12; i++) {
            var typeParameters = string.Join(", ", Enumerable.Range(0, i).Select(j => $"T{j}"));
            var outParameters = string.Join(", ", Enumerable.Range(0, i).Select(j => $"out T{j} value{j}"));
            codeBuilder.StartBlock($"public static void Parse<{typeParameters}>(string input, string template, {outParameters})");
            codeBuilder.AddLine("throw new global::System.NotImplementedException(\"OutParser parse call is not implemented. This means the OutParser Generator was unable to emit any code. Please check your build output for errors.\");");
            codeBuilder.EndBlock();
        }

        codeBuilder.EndBlock();
        codeBuilder.EndBlock();

        context.AddSource("OutParser.Parse.g.cs", codeBuilder.ToString());
    }

    private bool FindParseCalls(SyntaxNode node, CancellationToken token) {
        if (node is not InvocationExpressionSyntax invocation) {
            return false;
        }
        return invocation.ArgumentList.Arguments.Count > 0;
    }

    private class ParseCallReporter : IParserCallReporter {
        public void ReportPatternMissingOut(Location location, string key) { }

        public void ReportOutMissingPattern(Location location, string argumentName) { }

        public void ReportPatternRepeats(Location location, string template) { }

        public void ReportTemplateNotLiteral(Location location) { }

        public void ReportTypeNotParsable(Location location, string name) { }
    }

    private ParserCall? FindParseCallsTransform(GeneratorSyntaxContext context, CancellationToken token) {
        return SyntaxReading.GetParserCall(context.Node, context.SemanticModel, new ParseCallReporter(), token);
    }

    private void EmitParseCallsCode(SourceProductionContext context, ImmutableArray<ParserCall> parserCalls) {
        var code = new CodeBuilder();

        code.StartBlock("namespace OutParsing");
        code.StartBlock("partial class OutParser");
        code.StartBlock("public class Interceptors");

        int index = 0;
        foreach (var call in parserCalls) {
            if (context.CancellationToken.IsCancellationRequested) {
                return;
            }
            code.AddLine($"[System.Runtime.CompilerServices.InterceptsLocationAttribute(1, \"{call.InterceptLocation}\")]");
            var outCalls = string.Join(", ", call.OutCalls.Select((x, i) => $"out {x.TypeData.FullName} value{i}"));
            code.StartBlock($"public static void ParseIntercept{index++}(string input, string template, {outCalls})");

            code.AddLine("var instance = new OutParserInstance(input, [");
            code.AddLine(string.Join(",", call.Components.Select(x => '\t' + EscapeString(x))));
            code.AddLine("]);");

            // Store reordering of calls, to allow templates in the wrong order
            string[] readLines = new string[call.OutCalls.Length];

            for (int typeIndex = 0; typeIndex < call.OutCalls.Length; typeIndex++) {
                var outCall = call.OutCalls[typeIndex];
                var type = outCall.TypeData; 
                var variableName = $"value{typeIndex}";
                if (type.Kind is TypeDataKind.Parsable or TypeDataKind.SpanParsable) {
                    var method = type.Kind == TypeDataKind.SpanParsable ? "GetSpanParsable" : "GetParsable";
                    readLines[outCall.ReadIndex] = $"{variableName} = instance.{method}<{type.FullName}>();";
                }
                else if (type.Kind is TypeDataKind.Array or TypeDataKind.List) {
                    var method = type.InnerType?.Kind == TypeDataKind.SpanParsable ? "GetSpanParsableList" : "GetParsableList";
                    var convertMethod = type.Kind == TypeDataKind.Array ? ".ToArray()" : "";
                    readLines[outCall.ReadIndex] = $"{variableName} = instance.{method}<{type.InnerType?.FullName}>({EscapeString(outCall.ListSeparator!)}){convertMethod};";
                }
            }

            foreach (var line in readLines) {
                code.AddLine(line);
            }

            code.EndBlock();
        }

        code.EndBlock();
        code.EndBlock();
        code.EndBlock();

        code.AddLine("""
        namespace System.Runtime.CompilerServices {
        #pragma warning disable CS9113
        	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        	file sealed class InterceptsLocationAttribute(int version, string data) : Attribute {
        	}
        #pragma warning restore CS9113
        }
        """);

        context.AddSource("OutParser.Interceptors.g.cs", code.ToString());
    }

    private string EscapeString(string str) {
        return $"\"{str
            .Replace("\\", "\\\\")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")}\"";
    }
}
