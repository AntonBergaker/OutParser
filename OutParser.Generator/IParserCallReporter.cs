using Microsoft.CodeAnalysis;
namespace OutParser.Generator;
internal interface IParserCallReporter {
    void ReportPatternMissingOut(Location location, string key);
    void ReportOutMissingPattern(Location location, string argumentName);
    void ReportPatternRepeats(Location location, string template);
    void ReportTemplateNotLiteral(Location location);
    void ReportTypeNotParsable(Location location, string name);
}
