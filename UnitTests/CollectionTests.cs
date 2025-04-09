using OutParsing;

namespace UnitTests;
internal class CollectionTests {
    [Test]
    public void IntArray() {
        OutParser.Parse(
            "1,2,4,8,16",
            "{array:,}",
            out int[] array
        );

        Assert.AreEqual(new int[] { 1, 2, 4, 8, 16 }, array);
    }

    [Test]
    public void StringArray() {
        OutParser.Parse(
            "i am cool",
            "{array: }",
            out string[] array
        );

        Assert.AreEqual(new string[] {"i", "am", "cool" }, array);
    }

    [Test]
    public void IntList() {
        OutParser.Parse(
            "1,2,4,8,16",
            "{list:,}",
            out List<int> list
        );

        Assert.AreEqual(new int[] { 1, 2, 4, 8, 16 }, list);
    }

    [Test]
    public void TryIntArray() {
        if (OutParser.TryParse(
            "1,2,4,8,16",
            "{array:,}",
            out int[]? array
        ) == false) {
            Assert.Fail("Failed to parse int array");
            return;
        }

        Assert.AreEqual(new int[] { 1, 2, 4, 8, 16 }, array);
    }
}
