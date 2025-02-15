using OutParsing;

namespace UnitTests;
internal class ManyParametersTests {
    [Test]
    public void FourParameters() {
        OutParser.Parse(
            "One 1 Two 2 Three 3 Four 4",
            "One {one} Two {two} Three {three} Four {four}",
            out int one, out int two, out int three, out int four
        );

        Assert.AreEqual(1, one);
        Assert.AreEqual(2, two);
        Assert.AreEqual(3, three);
        Assert.AreEqual(4, four);
    }

    [Test]
    public void TwelveParameters() {
        OutParser.Parse(
            "One 1 Two 2 Three 3 Four 4 Five 5 Six 6 Seven 7 Eight 8 Nine 9 Ten 10 Eleven 11 Twelve 12",
            "One {one} Two {two} Three {three} Four {four} Five {five} Six {six} Seven {seven} Eight {eight} Nine {nine} Ten {ten} Eleven {eleven} Twelve {twelve}",
            out int one, out int two, out int three, out int four, out int five, out int six, out int seven, out int eight, out int nine, out int ten, out int eleven, out int twelve
        );

        Assert.AreEqual(1, one);
        Assert.AreEqual(2, two);
        Assert.AreEqual(3, three);
        Assert.AreEqual(4, four);
        Assert.AreEqual(5, five);
        Assert.AreEqual(6, six);
        Assert.AreEqual(7, seven);
        Assert.AreEqual(8, eight);
        Assert.AreEqual(9, nine);
        Assert.AreEqual(10, ten);
        Assert.AreEqual(11, eleven);
        Assert.AreEqual(12, twelve);
    }
}
