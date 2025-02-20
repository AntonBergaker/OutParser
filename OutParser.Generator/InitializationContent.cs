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

            private delegate T ParsableConvertMethod<T>(global::System.ReadOnlySpan<char> span);

            private static ParsableConvertMethod<T> GetParsableConvertMethod<T>()
                where T : global::System.IParsable<T> => 
                static x => T.Parse(new string(x), System.Globalization.CultureInfo.InvariantCulture);

            private static ParsableConvertMethod<T> GetSpanParsableConvertMethod<T>()
                where T : global::System.ISpanParsable<T> =>
                static x => T.Parse(x, System.Globalization.CultureInfo.InvariantCulture);


            #region Single Parsable
            public T GetParsable<T>() where T : global::System.IParsable<T> {
                return GetSpanParsableFunction(GetParsableConvertMethod<T>());
            }

            public T GetSpanParsable<T>() where T : global::System.ISpanParsable<T> {
                return GetSpanParsableFunction(GetSpanParsableConvertMethod<T>());
            }

            private T GetSpanParsableFunction<T>(ParsableConvertMethod<T> parseFunction) {
                var nextString = GetNextPart();
                return parseFunction(nextString);
            }
            #endregion

            #region List Parsable
            public global::System.Collections.Generic.List<T> GetParsableList<T>(string separator) where T : global::System.IParsable<T> {
                return GetSpanParsableListFunction(GetParsableConvertMethod<T>(), separator);
            }

            public global::System.Collections.Generic.List<T> GetSpanParsableList<T>(string separator) where T : global::System.ISpanParsable<T> {
                return GetSpanParsableListFunction(GetSpanParsableConvertMethod<T>(), separator);
            }

            private global::System.Collections.Generic.List<T> GetSpanParsableListFunction<T>(ParsableConvertMethod<T> parseFunction, string separator) {
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
            #endregion

            private delegate (bool Success, T Result) TryParsableConvertMethod<T>(global::System.ReadOnlySpan<char> span);
            private static TryParsableConvertMethod<T> TryGetParsableConvertMethod<T>()
                where T : global::System.IParsable<T> => 
                static x => {
                    var result = T.TryParse(new string(x), System.Globalization.CultureInfo.InvariantCulture, out var list);
                    return (result, list!);
                };
            private static TryParsableConvertMethod<T> TryGetSpanParsableConvertMethod<T>()
                where T : global::System.ISpanParsable<T> => 
                static x => {
                    var result = T.TryParse(x, System.Globalization.CultureInfo.InvariantCulture, out var list);
                    return (result, list!);
                };

            #region Single TryParsable
            public bool TryGetParsable<T>([System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T result) where T : global::System.IParsable<T> {
                return TryGetSpanParsableFunction(TryGetParsableConvertMethod<T>(), out result);
            }

            public bool TryGetSpanParsable<T>([System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T result) where T : global::System.ISpanParsable<T> {
                return TryGetSpanParsableFunction(TryGetSpanParsableConvertMethod<T>(), out result);
            }

            private bool TryGetSpanParsableFunction<T>(TryParsableConvertMethod<T> parseFunction, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out T result) {
                if (TryGetNextPart(out var nextString) == false) {
                    result = default;
                    return false;
                }
                var (success, parseResult) = parseFunction(nextString);
                result = parseResult;
                return success;
            }
            #endregion

            #region List TryParsable
            public bool TryGetParsableList<T>(string separator, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out global::System.Collections.Generic.List<T> list) where T : global::System.IParsable<T> {
                return TryGetSpanParsableListFunction(TryGetParsableConvertMethod<T>(), separator, out list);
            }
            public bool TryGetSpanParsableList<T>(string separator, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out global::System.Collections.Generic.List<T> list) where T : global::System.ISpanParsable<T> {
                return TryGetSpanParsableListFunction(TryGetSpanParsableConvertMethod<T>(), separator, out list);
            }

            private bool TryGetSpanParsableListFunction<T>(TryParsableConvertMethod<T> parseFunction, string separator, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out global::System.Collections.Generic.List<T> list) {
                list = default;
                if (TryGetNextPart(out var chars) == false) {
                    return false;
                }
                var workingList = new global::System.Collections.Generic.List<T>();

                while (true) {
                    int index = System.MemoryExtensions.IndexOf(chars, separator);
                    if (index == -1) {
                        var (innerSuccess, innerResult) = parseFunction(chars);
                        if (innerSuccess == false) {
                            return false;
                        }
                        workingList.Add(innerResult);
                        break;
                    }

                    var (success, result) = parseFunction(chars[..index]);
                    if (success == false) {
                        return false;
                    }
                    workingList.Add(result);
                    chars = chars[(index + separator.Length)..];
                }

                list = workingList;
                return true;
            }

            #endregion

            private bool TryGetNextPart(out global::System.ReadOnlySpan<char> chars) {
                chars = default;

                if (_currentStringPosition > _inputString.Length) {
                    return false;
                }

                // Final bit
                if (_currentIndex >= _templateComponents.Length) {
                    chars = global::System.MemoryExtensions.AsSpan(_inputString, _currentStringPosition, _inputString.Length - _currentStringPosition);
                    return true;
                }

                string nextPart = _templateComponents[_currentIndex++];
                int index = _inputString.IndexOf(nextPart, _currentStringPosition);

                if (index <= -1) {
                    return false;
                }

                var startPos = _currentStringPosition;
                _currentStringPosition = index + nextPart.Length;
                chars = global::System.MemoryExtensions.AsSpan(_inputString, startPos, index - startPos);
                return true;
            }

            private global::System.ReadOnlySpan<char> GetNextPart() {
                if (_currentStringPosition > _inputString.Length) {
                    throw new global::System.Exception("Tried to read beyond the size of the input string. This usually means the provided string is missing parts of the template.");
                }

                // Final bit
                if (_currentIndex >= _templateComponents.Length) {
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
