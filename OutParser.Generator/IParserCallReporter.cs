using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace OutParser.Generator;
internal interface IParserCallReporter {
    void ReportMissingOut(TextSpan span, string key);
    void ReportMissingPattern(TextSpan span, string argumentName);
    void ReportPatternRepeats(TextSpan span, string template);
    void ReportTemplateNotLiteral(TextSpan span);
    void ReportTypeNotParsable(TextSpan span, string name);
}
