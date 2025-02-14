namespace OutParsing {
    partial class OutParser {
		public class Interceptors {

			//[System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "")]
			public static void ParseL1Implementation(string input, string template, out int value0) {
				var a = new OutParserInstance(input, [""]);
				a.GetSpanParsable(out value0);
			}

			public static void ParseL2Implementation(string input, string template, out string value0) {
				var a = new OutParserInstance(input, ["My name is "]);
				a.GetSpanParsable(out value0);
			}

			public static void ParseL3Implementation(string input, string template, out string value0) {
				var a = new OutParserInstance(input, ["I love ", " and I cannot lie"]);
				a.GetSpanParsable(out value0);
			}
		}
	}
}

namespace System.Runtime.CompilerServices {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	file sealed class InterceptsLocationAttribute(int version, string data) : Attribute {
	}
}