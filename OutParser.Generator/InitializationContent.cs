namespace OutParser;

public static class InitializationContent {
    public static string GetContent() =>
"""
#nullable enable
namespace OutParsing {
	internal static partial class OutParser {
        private struct OutParserInstance {
            private readonly string _inputString;
            private readonly string[] _templateComponents;
            private int _currentIndex;
            private int _currentStringPosition;

            public OutParserInstance(string input, string[] templateComponents) {
                _inputString = input;
                _currentIndex = 1;
                _currentStringPosition = templateComponents[0].Length;
                _templateComponents = templateComponents;
            }

            private static global::System.Func<global::System.ReadOnlySpan<char>, T> GetParsableConvertMethod<T>()
                where T : global::System.IParsable<T> => 
                static x => T.Parse(new string(x), System.Globalization.CultureInfo.InvariantCulture);

            private static global::System.Func<global::System.ReadOnlySpan<char>, T> GetSpanParsableConvertMethod<T>()
                where T : global::System.ISpanParsable<T> =>
                static x => T.Parse(x, System.Globalization.CultureInfo.InvariantCulture);

            public T GetParsable<T>() where T : global::System.IParsable<T> {
                return GetSpanParsableFunction(GetParsableConvertMethod<T>());
            }

            public T GetSpanParsable<T>() where T : global::System.ISpanParsable<T> {
                return GetSpanParsableFunction(GetSpanParsableConvertMethod<T>());
            }

            private T GetSpanParsableFunction<T>(global::System.Func<global::System.ReadOnlySpan<char>, T> parseFunction) {
                var nextString = GetNextPart();
                return parseFunction(nextString);
            }

            public global::System.Collections.Generic.List<T> GetParsableList<T>(string separator) where T : global::System.IParsable<T> {
                return GetSpanParsableListFunction(GetParsableConvertMethod<T>(), separator);
            }

            public global::System.Collections.Generic.List<T> GetSpanParsableList<T>(string separator) where T : global::System.ISpanParsable<T> {
                return GetSpanParsableListFunction(GetSpanParsableConvertMethod<T>(), separator);
            }

            private global::System.Collections.Generic.List<T> GetSpanParsableListFunction<T>(global::System.Func<global::System.ReadOnlySpan<char>, T> parseFunction, string separator) {
                var chars = GetNextPart();
                var list = new global::System.Collections.Generic.List<T>();

                while (true) {
                    int index = System.MemoryExtensions.IndexOf(chars, separator);
                    if (index == -1) {
                        list.Add(parseFunction(chars));
                        break;
                    }

                    list.Add(parseFunction(chars[..index]));
                    chars = chars[(index + separator.Length)..];

                }

                return list;
            }

            private global::System.ReadOnlySpan<char> GetNextPart() {
                // Final bit
                if (_currentIndex >= _templateComponents.Length) {
                    if (_currentStringPosition > _inputString.Length) {
                        throw new global::System.Exception("Tried to read beyond the size of the input string. This usually means the provided string is missing parts of the template.");
                    }
                    return global::System.MemoryExtensions.AsSpan(_inputString, _currentStringPosition, _inputString.Length - _currentStringPosition);
                }

                string nextPart = _templateComponents[_currentIndex++];
                int index = _inputString.IndexOf(nextPart, _currentStringPosition);

                if (index <= -1) {
                    throw new global::System.Exception(
                        $"Failed to find the next part of the template: \"{nextPart}\" in the remainder of the input string. " +
                        $"Make sure the template matches the input string, and that any previous parsed part does not contain the substring: \"{nextPart}\""
                    );
                }

                var startPos = _currentStringPosition;
                _currentStringPosition = index + nextPart.Length;
                return global::System.MemoryExtensions.AsSpan(_inputString, startPos, index - startPos);
            }
        }
    }
}
""";}
