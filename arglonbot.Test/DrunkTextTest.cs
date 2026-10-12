namespace arglonbot.Test;

[TestFixture]
public class DrunkTextTest
{
    [Test]
    public void TestTextIsUnchangedWhenNoRollSucceeds()
    {
        Assert.That(DrunkText.Make("Hello there, Arglon", new ScriptedRandom()), Is.EqualTo("Hello there, Arglon"));
    }

    [Test]
    public void TestEmptyTextStaysEmpty()
    {
        Assert.That(DrunkText.Make(string.Empty, new ScriptedRandom()), Is.Empty);
    }

    [Test]
    public void TestAOrEGetsAJ()
    {
        Assert.That(DrunkText.Make("a", new ScriptedRandom(0)), Is.EqualTo("aj"));
    }

    [Test]
    public void TestVowelFollowedByItsPairIsDoubledInsteadOfGettingALetter()
    {
        Assert.That(DrunkText.Make("ea", new ScriptedRandom(0)), Is.EqualTo("eea"));
    }

    [Test]
    public void TestLowercaseIGetsAUWhichThenGetsAW()
    {
        Assert.That(DrunkText.Make("i", new ScriptedRandom(0, 0)), Is.EqualTo("iuw"));
    }

    [Test]
    public void TestCapitalIIsDoubledRatherThanGettingAU()
    {
        Assert.That(DrunkText.Make("I", new ScriptedRandom(0)), Is.EqualTo("II"));
    }

    [Test]
    public void TestVowelCanBeStarred()
    {
        Assert.That(DrunkText.Make("a", new ScriptedRandom(9, 0)), Is.EqualTo("*"));
    }

    [Test]
    public void TestHiccupFollowsASpaceAndDoesNotRepeatOnItsOwnSpace()
    {
        // a: no "j"; b: not doubled; a: not starred; the space hiccups. The trailing 0 would
        // add a second hiccup if the hiccup's own space were visited.
        Assert.That(DrunkText.Make("a b", new ScriptedRandom(9, 9, 23, 0, 0)), Is.EqualTo("a *hic* b"));
    }

    [Test]
    public void TestEmojiIsDoubledWhole()
    {
        Assert.That(DrunkText.Make("😮", new ScriptedRandom(0)), Is.EqualTo("😮😮"));
    }

    [Test]
    public void TestTextUnderTheChatLengthIsNotCut()
    {
        var text = new string('x', 127);

        Assert.That(DrunkText.Make(text, new ScriptedRandom()), Is.EqualTo(text));
    }

    [Test]
    public void TestTextAtTheChatLengthIsCutWithDots()
    {
        var result = DrunkText.Make(new string('x', 200), new ScriptedRandom());

        Assert.That(result, Is.EqualTo(new string('x', 126) + ".."));
    }

    // Returns the given rolls in order, then the highest roll (which never succeeds).
    private sealed class ScriptedRandom(params int[] rolls) : Random
    {
        private readonly Queue<int> _rolls = new(rolls);

        public override int Next(int maxValue) => _rolls.TryDequeue(out var roll) ? roll : maxValue - 1;
    }
}
