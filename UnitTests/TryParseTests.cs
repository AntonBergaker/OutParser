using OutParsing;

namespace UnitTests;

public class TryParseTests {

    [Test]
    public void SimpleTryParse() {
        var success = OutParser.TryParse("x = 5", "x = {x}", out int x);

        Assert.IsTrue(success);
        Assert.AreEqual(5, x);
    }

    [Test]
    public void FailingTryParse() {
        var success = OutParser.TryParse("x = greger", "x = {x}", out int x);

        Assert.IsFalse(success);
    }

    [Test]
    public void FailingRead() {
        var success = OutParser.TryParse("jeff!", "my name is {jeff}!", out string? jeff);

        Assert.IsFalse(success);
    }

}