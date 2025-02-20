using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
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

        // Generate 12 Parse methods
        codeBuilder.AddLine("public static void Parse(string input, string template) { }");
        for (int i = 1; i <= 12; i++) {
            var typeParameters = string.Join(", ", Enumerable.Range(0, i).Select(j => $"T{j}"));
            var outParameters = string.Join(", ", Enumerable.Range(0, i).Select(j => $"out T{j} value{j}"));
            codeBuilder.StartBlock($"public static void Parse<{typeParameters}>(string input, string template, {outParameters})");
            codeBuilder.AddLine("throw new global::System.NotImplementedException(\"OutParser Parse call is not implemented. This means the OutParser Generator was unable to emit any code. Please check your build output for errors.\");");
            codeBuilder.EndBlock();
        }

        // Generate 12 TryParse methods
        codeBuilder.AddLine("public static bool TryParse(string input, string template) { return true; }");
        for (int i = 1; i <= 12; i++) {
            var typeParameters = string.Join(", ", Enumerable.Range(0, i).Select(j => $"T{j}"));
            var outParameters = string.Join(", ", Enumerable.Range(0, i).Select(j => $"[System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T{j} value{j}"));
            codeBuilder.StartBlock($"public static bool TryParse<{typeParameters}>(string input, string template, {outParameters})");
            codeBuilder.AddLine("throw new global::System.NotImplementedException(\"OutParser TryParse call is not implemented. This means the OutParser Generator was unable to emit any code. Please check your build output for errors.\");");
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
            var methodSignature = call.IsTryParse ? "bool TryParseIntercept" : "void ParseIntercept";
            var maybeNull = call.IsTryParse ? "[System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] " : "";
            var outCalls = string.Join(", ", call.OutCalls.Select((x, i) => $"{maybeNull}out {x.TypeData.FullName} value{i}"));
            code.StartBlock($"public static {methodSignature}{index++}(string input, string template, {outCalls})");

            code.AddLine("var instance = new OutParserInstance(input, [");
            code.AddLine(string.Join(",", call.Components.Select(x => '\t' + EscapeString(x))));
            code.AddLine("]);");

            // Store reordering of calls, to allow templates in the wrong order
            string[] readLines = new string[call.OutCalls.Length];

            for (int typeIndex = 0; typeIndex < call.OutCalls.Length; typeIndex++) {
                var outCall = call.OutCalls[typeIndex];
                if (call.IsTryParse) {
                    readLines[outCall.ReadIndex] = GetTryParseMethodLine($"value{typeIndex}", outCall);
                } else {
                    readLines[outCall.ReadIndex] = GetParseMethodLine($"value{typeIndex}", outCall);
                }
            }

            foreach (var line in readLines) {
                code.AddLine(line);
            }

            if (call.IsTryParse) {
                code.AddLine("return true;");
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

    private string GetParseMethodLine(string variableName, OutCallData outCall) {
        var type = outCall.TypeData;
        if (type.Kind is TypeDataKind.Parsable or TypeDataKind.SpanParsable) {
            var method = type.Kind == TypeDataKind.SpanParsable ? "GetSpanParsable" : "GetParsable";
            return $"{variableName} = instance.{method}<{type.FullName}>();";
        } 
        else if (type.Kind is TypeDataKind.Array or TypeDataKind.List) {
            var method = type.InnerType?.Kind == TypeDataKind.SpanParsable ? "GetSpanParsableList" : "GetParsableList";
            var convertMethod = type.Kind == TypeDataKind.Array ? ".ToArray()" : "";
            return $"{variableName} = instance.{method}<{type.InnerType?.FullName}>({EscapeString(outCall.ListSeparator!)}){convertMethod};";
        }
        throw new Exception("Unimplemented type kind");
    }


    private string GetTryParseMethodLine(string variableName, OutCallData outCall) {
        var type = outCall.TypeData;
        if (type.Kind is TypeDataKind.Parsable or TypeDataKind.SpanParsable) {
            var parseMethod = type.Kind == TypeDataKind.SpanParsable ? "TryGetSpanParsable" : "TryGetParsable";
            return $"if (instance.{parseMethod}<{type.FullName}>(out {variableName}) == false) return false;";
        }

        var method = type.InnerType?.Kind == TypeDataKind.SpanParsable ? "TryGetSpanParsableList" : "TryGetParsableList";
        if (type.Kind is TypeDataKind.List) {
            return $"if (instance.{method}<{type.InnerType?.FullName}>({EscapeString(outCall.ListSeparator!)}, out {variableName}) == false) return false;";
        }
        if (type.Kind is TypeDataKind.Array) {
            return $"if (instance.{method}<{type.InnerType?.FullName}>({EscapeString(outCall.ListSeparator!)}, out var {variableName}List)) {variableName} = {variableName}.ToArray(); else return false;";
        }
        throw new Exception("Unimplemented type kind");
    }

    private string EscapeString(string str) {
        return $"\"{str
            .Replace("\\", "\\\\")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")}\"";
    }
}
