using System.Collections.Generic;

namespace OutParsing {
    partial class OutParser {
		public class Interceptors {

			//[System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "")]
			public static void ParseL1Implementation(string input, string template, out int value0) {
				var a = new OutParserInstance(input, [""]);
				value0 = a.GetSpanParsable<int>();
			}

			public static void ParseL2Implementation(string input, string template, out string value0) {
				var a = new OutParserInstance(input, ["My name is "]);
                value0 = a.GetSpanParsable<string>();
            }

			public static void ParseL3Implementation(string input, string template, out string value0) {
				var a = new OutParserInstance(input, ["I love ", " and I cannot lie"]);
                value0 = a.GetSpanParsable<string>();
            }

			public static void ParseL4Implementation(string input, string template, out List<int> value0) {
                var a = new OutParserInstance(input, [""]);
                value0 = a.GetSpanParsableList<int>(", ");
            }

        }
	}
}

namespace System.Runtime.CompilerServices {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
#pragma warning disable CS9113 // Parameter is unread.
    file sealed class InterceptsLocationAttribute(int version, string data) : Attribute {
#pragma warning restore CS9113 // Parameter is unread.
    }
}