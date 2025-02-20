using OutParsing;
using NUnit.Framework.Internal;

namespace UnitTests;

public class SimpleParsingTests {

    [Test]
    public void Single() {
        OutParser.Parse("69", "{x}", out int x);

        Assert.AreEqual(69, x);
    }

    [Test]
    public void SimpleInts() {
        OutParser.Parse(
            "x=512, y=123",
            "x={x}, y={y}",
            out int x, out int y
        );

        Assert.AreEqual(512, x);
        Assert.AreEqual(123, y);
    }

    [Test]
    public void NoPrestring() {
        OutParser.Parse(
            "1337 is the ticket!",
            "{x} is the ticket!",
            out int x
        );

        Assert.AreEqual(1337, x);
    }

    [Test]
    public void NoPostString() {
        OutParser.Parse(
            "Ticket is 1337",
            "Ticket is {x}",
            out int x
        );

        Assert.AreEqual(1337, x);
    }

    [Test]
    public void Stacked() {
        OutParser.Parse("123 woop", "{x} woop", out int x);
        OutParser.Parse("123 woop", "{y} woop", out int y);
        OutParser.Parse("123 woop", "{z} woop", out int z);

        Assert.AreEqual(123, x);
        Assert.AreEqual(123, y);
        Assert.AreEqual(123, z);
    }


    [Test]
    public void Multiline() {
        var input = """
            Today: 1,
            Tomorrow: 4,
            Tuesday: 9
            """;
        OutParser.Parse(input,
            """
            Today: {value0},
            Tomorrow: {value1},
            Tuesday: {value2}
            """,
            out int value0, out int value1, out int value2
        );

        Assert.AreEqual(1, value0);
        Assert.AreEqual(4, value1);
        Assert.AreEqual(9, value2);
    }
}