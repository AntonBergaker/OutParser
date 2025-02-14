using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace OutParser.Generator;

[Generator]
public class OutParserGenerator : IIncrementalGenerator {
    private const string ParseMethodName = "InterpolatedParsing.InterpolatedParser.Parse(string, InterpolatedParsing.InterpolatedParser.InterpolatedParseStringHandler)";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        //Debugger.Launch();
        context.RegisterPostInitializationOutput(InitializationOutput);

        var interpolationCalls = context.SyntaxProvider.CreateSyntaxProvider(FindParseCalls, FindParseCallsTransform).NotNull().Collect();

        context.RegisterSourceOutput(interpolationCalls, EmitParseCallsCode);

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
            for (int j = 0; j < i; j++) {
                codeBuilder.AddLine($"value{j} = default!;");
            } 
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

    private ParserCall? FindParseCallsTransform(GeneratorSyntaxContext context, CancellationToken token) {
        if (context.SemanticModel.GetOperation(context.Node, token) is not IInvocationOperation operation) {
            return null;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(context.Node, token);

        // Starts with Parse<
        if (symbolInfo.Symbol?.ToDisplayString().StartsWith($"{ParseMethodName}<") ?? false) {
            return null;
        }
        if (operation.Arguments.Length <= 1) {
            return null;
        }
        var templateArgument = operation.Arguments[1].Value;
        if (templateArgument is not ILiteralOperation literalOperation) {
            return null;
        }
        if (literalOperation.ConstantValue.HasValue == false) {
            return null;
        }
        var stringValue = literalOperation.ConstantValue.Value as string;
        if (stringValue == null) {
            return null;
        }

        var invocation = (InvocationExpressionSyntax)context.Node;
#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var location = context.SemanticModel.GetInterceptableLocation(invocation);
        if (location == null) {
            return null;
        }
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


        List<TypeData> types = new();
        foreach (var argument in operation.Arguments.Skip(2)) {
            var typeData = GetTypeData(argument.Parameter?.Type);
            if (typeData == null) {
                return null;
            }
            types.Add(typeData);
        }

        List<string> components = new();
        int startIndex = 0;
        while (true) {
            int open = FindNextNonDupeChar(stringValue, '{', startIndex);
            if (open == -1) {
                break;
            }
            var close = FindNextNonDupeChar(stringValue, '}', open+1);
            if (close == -1) {
                break;
            }

            components.Add(stringValue.Substring(startIndex, open - startIndex));

            startIndex = close + 1;
        }
        if (startIndex < stringValue.Length) {
            components.Add(stringValue.Substring(startIndex, stringValue.Length - startIndex));
        }

        return new(types.ToArray(), components.ToArray(), location.Data);
    }

    private int FindNextNonDupeChar(string @string, char @char, int startIndex) {
        while (true) {
            var next = @string.IndexOf(@char, startIndex);
            if (next == -1) {
                return -1;
            }
            if (Utils.GetCharSafe(@string, next + 1) == @char) {
                startIndex = next + 1;
                continue;
            }
            return next;
        }
    }

    /// <summary>
    /// Returns TypeData if it makes sense to generate it
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private TypeData? GetTypeData(ITypeSymbol? type) {
        if (type == null) {
            return null;
        }
        var typeName = type.ToString();

        TypeData? innerType = null;

        TypeDataKind kind = TypeDataKind.Parsable;
        if (type is IArrayTypeSymbol array) {
            kind = TypeDataKind.Array;

            innerType = GetTypeData(array.ElementType);
        }
        else if (type.OriginalDefinition?.ToString() == "System.Collections.Generic.List<T>") {
            if (type is not INamedTypeSymbol namedType) {
                return null;
            }
            kind = TypeDataKind.List;
            innerType = GetTypeData(namedType.TypeArguments[0]);
        }
        else {
            var spanParsableString = $"System.ISpanParsable<{typeName}>";
            bool isSpanParsable = type.AllInterfaces.Any(i => i.ToString() == spanParsableString);

            if (isSpanParsable == false) { // span parsable has priority, so only check for normal parsable as fallback
                var parsableString = $"System.IParsable<{typeName}>";
                if (type.AllInterfaces.Any(i => i.ToString() == parsableString) == false) {
                    // If neither type of parsable, skip the type
                    return null;
                }
            }
            kind = isSpanParsable ? TypeDataKind.SpanParsable : TypeDataKind.Parsable;
        }

        return new(typeName, kind, innerType);
    }

    private void EmitParseCallsCode(SourceProductionContext context, ImmutableArray<ParserCall> parserCalls) {
        var code = new CodeBuilder();

        code.StartBlock("namespace OutParsing");
        code.StartBlock("partial class OutParser");
        code.StartBlock("public class Interceptors");

        int index = 0;
        foreach (var call in parserCalls) {
            code.AddLine($"[System.Runtime.CompilerServices.InterceptsLocationAttribute(1, \"{call.InterceptLocation}\")]");
            var outCalls = string.Join(", ", call.Types.Select((x, i) => $"out {x.FullName} value{i}"));
            code.StartBlock($"public static void ParseIntercept{index++}(string input, string template, {outCalls})");

            code.AddLine("var instance = new OutParserInstance(input, [");
            code.AddLine(string.Join(",", call.Components.Select(x => EscapeString(x))));
            code.AddLine("]);");

            for (int typeIndex = 0; typeIndex < call.Types.Length; typeIndex++) {
                TypeData type = call.Types[typeIndex];
                var method = type.Kind == TypeDataKind.SpanParsable ? "GetSpanParsable" : "GetParsable";
                code.AddLine($"instance.{method}(out value{typeIndex});");
            }

            code.EndBlock();
        }

        code.EndBlock();
        code.EndBlock();
        code.EndBlock();

        code.AddLine("""
        namespace System.Runtime.CompilerServices {
        	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        	file sealed class InterceptsLocationAttribute(int version, string data) : Attribute {
        	}
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
