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

    [Test]
    public void MultipleVariables() {
        var success = OutParser.TryParse("I eat potato and meatballs", "I eat {food0} and {food1}", out string? food0, out string? food1);

        Assert.IsTrue(success);
        Assert.AreEqual("potato", food0);
        Assert.AreEqual("meatballs", food1);
    }
}