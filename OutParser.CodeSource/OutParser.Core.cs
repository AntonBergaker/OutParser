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

            public void GetParsable<T>(out T value) where T : global::System.IParsable<T> {
                GetSpanParsableFunction(out value, static x => T.Parse(new string(x), null));
            }

            public void GetSpanParsable<T>(out T value) where T : global::System.ISpanParsable<T> {
                GetSpanParsableFunction(out value, static x => T.Parse(x, null));
            }

            private void GetSpanParsableFunction<T>(out T value, global::System.Func<global::System.ReadOnlySpan<char>, T> parseFunction) {
                var nextString = GetNextPart();
                value = parseFunction(nextString);
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
