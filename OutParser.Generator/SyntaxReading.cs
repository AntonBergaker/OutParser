using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OutParser.Generator;
internal static class SyntaxReading {
    private const string ParseMethodName = "OutParsing.OutParser.Parse";

    public static ParserCall? GetParserCall(SyntaxNode node, SemanticModel model, IParserCallReporter reporter, CancellationToken token) {
        if (model.GetOperation(node, token) is not IInvocationOperation operation) {
            return null;
        }

        // Starts with Parse<
        if (operation.TargetMethod.ToDisplayString().ToString().StartsWith($"{ParseMethodName}<") == false) {
            return null;
        }
        if (operation.Arguments.Length <= 1) {
            return null;
        }
        var templateArgument = operation.Arguments[1].Value;
        if (templateArgument is not ILiteralOperation literalOperation) {
            reporter.ReportTemplateNotLiteral(templateArgument);
            return null;
        }
        if (literalOperation.ConstantValue.HasValue == false) {
            reporter.ReportTemplateNotLiteral(templateArgument);
            return null;
        }
        var stringValue = literalOperation.ConstantValue.Value as string;
        if (stringValue == null) {
            return null;
        }

        var invocation = (InvocationExpressionSyntax)node;
#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var location = model.GetInterceptableLocation(invocation);
        if (location == null) {
            return null;
        }
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var (components, patterns) = GetTemplateComponents(stringValue);
        var patternSet = new Dictionary<string, (string Separator, int Count)>();

        foreach (var pattern in patterns) {
            var (reducedPattern, separator) = GetSeparatorFromPattern(pattern);
            if (patternSet.TryGetValue(reducedPattern, out var found) && found.Count == 1) {
                reporter.ReportPatternRepeats(literalOperation, reducedPattern);
            }
            patternSet[reducedPattern] = (separator, found.Count + 1);
        }

        List<TypeData> types = new();
        int index = 0;
        foreach (var argument in operation.Arguments.Skip(2)) {
            var argumentName = argument.Parameter?.Name ?? "";

            if (patternSet.TryGetValue(argumentName, out var pattern)) {
                // Mark as consumed, so we can find unconsumed later.
                patternSet[argumentName] = (pattern.Separator, -1);
            } else {
                reporter.ReportMissingPattern(argument.Syntax.Span, argumentName);
                return null;
            }

            var typeData = GetTypeData(argument.Parameter?.Type, argument.Syntax.Span, reporter, pattern.Separator);
            if (typeData == null) {
                return null;
            }
            types.Add(typeData);
        }

        foreach (var pair in patternSet) {
            if (pair.Value.Count != -1) {
                reporter.ReportMissingOut(literalOperation.Syntax.Span, pair.Key);
            }
        }

        return new(types.ToArray(), components, location.Data);
    }

    private static (string[] Components, string[] Templates) GetTemplateComponents(string value) {
        List<string> components = new();
        List<string> templates = new();

        int startIndex = 0;
        while (true) {
            int open = FindNextNonDupeChar(value, '{', startIndex);
            if (open == -1) {
                break;
            }
            var close = FindNextNonDupeChar(value, '}', open + 1);
            if (close == -1) {
                break;
            }

            templates.Add(value.Substring(open + 1, close - 1 - open));
            components.Add(value.Substring(startIndex, open - startIndex));

            startIndex = close + 1;
        }
        if (startIndex < value.Length) {
            components.Add(value.Substring(startIndex, value.Length - startIndex));
        }

        return (components.ToArray(), templates.ToArray());
    }

    private static int FindNextNonDupeChar(string @string, char @char, int startIndex) {
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
    private static TypeData? GetTypeData(ITypeSymbol? type, TextSpan span, IParserCallReporter reporter, string separator) {
        if (type == null) {
            return null;
        }
        var typeName = type.ToString();
        TypeData? innerType = null;

        TypeDataKind kind = TypeDataKind.Parsable;
        if (type is IArrayTypeSymbol array) {
            kind = TypeDataKind.Array;
            innerType = GetTypeData(array.ElementType, span, reporter, "");
        }
        else if (type.OriginalDefinition?.ToString() == "System.Collections.Generic.List<T>") {
            if (type is not INamedTypeSymbol namedType) {
                return null;
            }
            kind = TypeDataKind.List;
            innerType = GetTypeData(namedType.TypeArguments[0], span, reporter, "");
        }
        else {
            var spanParsableString = $"System.ISpanParsable<{typeName}>";
            bool isSpanParsable = type.AllInterfaces.Any(i => i.ToString() == spanParsableString);

            if (isSpanParsable == false) { // span parsable has priority, so only check for normal parsable as fallback
                var parsableString = $"System.IParsable<{typeName}>";
                if (type.AllInterfaces.Any(i => i.ToString() == parsableString) == false) {
                    // If neither type of parsable, skip the type
                    reporter.ReportTypeNotParsable(span, type.Name);
                    return null;
                }
            }
            kind = isSpanParsable ? TypeDataKind.SpanParsable : TypeDataKind.Parsable;
        }

        return new(typeName, separator, kind, innerType);
    }

    private static (string Pattern, string Separator) GetSeparatorFromPattern(string pattern) {
        var colonIndex = pattern.IndexOf(':');
        if (colonIndex == -1) {
            return (pattern, "");
        }
        return (pattern.Substring(0, colonIndex), pattern.Substring(colonIndex + 1));
    }

}
