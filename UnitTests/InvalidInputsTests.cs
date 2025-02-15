using OutParsing;

namespace UnitTests;
internal class InvalidInputsTests {

    [Test]
    public void InvalidStart() {
        Assert.Catch(() => {
            OutParser.Parse(
                "too different 12",
                "something {x}",
            out int x);
        });

        Assert.Catch(() => {
            OutParser.Parse(
                "12",
                "too much {x}",
                out int x);
        });

        Assert.Catch(() => {
            OutParser.Parse(
                "missing 12",
                "{x}",
                out int x);
        });
    }
}
