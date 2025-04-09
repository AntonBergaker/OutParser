using OutParsing;

namespace UnitTests;
internal class ParameterNameTests {
    /*[Test]
    public void DiscardWithType() {
        OutParser.Parse("I eat cookies and drink milk!", "I eat {_} and drink {drink}!", out string _, out string drink);

        Assert.AreEqual("milk", drink);
    }*/

    [Test]
    public void DiscardWithoutType() {
        OutParser.Parse<string, string>("I eat cookies and drink milk!", "I eat {_} and drink {drink}!", out _, out string drink);

        Assert.AreEqual("milk", drink);
    }
}
