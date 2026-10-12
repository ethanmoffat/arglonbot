using System.Globalization;

namespace arglonbot;

// Port of Client::MakeDrunkText from the Endless Online 0.2.28 client: the game garbles chat
// this way for a drunk player, with added letters, doubled letters, starred vowels and hiccups,
// cut to the game's chat length.
public static class DrunkText
{
    private const int MaxLength = 128;
    private const string Hiccup = "*hic* ";

    public static string Make(string text, Random random)
    {
        // The client works on single-byte characters. Text elements keep emoji and accented
        // letters whole when they are doubled.
        var chars = SplitTextElements(text);

        for (int i = 0; i < chars.Count; i++)
        {
            var handled = false;
            var isLast = i == chars.Count - 1;

            // A vowel run gets its added letter after the last vowel of the run.
            if (IsAOrE(chars[i]) && (isLast || !IsAOrE(chars[i + 1])))
            {
                handled = true;
                if (random.Next(10) < 7)
                    chars.Insert(i + 1, "j");
            }
            else if (IsUOrO(chars[i]) && (isLast || !IsUOrO(chars[i + 1])))
            {
                handled = true;
                if (random.Next(10) < 7)
                    chars.Insert(i + 1, "w");
            }
            else if (chars[i] == "i")
            {
                handled = true;
                if (random.Next(10) < 4)
                    chars.Insert(i + 1, "u");
            }

            // Inserted letters are visited next, so they can be doubled (or, for "u", get a "w") in turn.
            if (!handled && chars[i] != " " && random.Next(10) < 4)
                chars.Insert(i + 1, chars[i]);
        }

        for (int j = 0; j < chars.Count; j++)
        {
            if (IsVowel(chars[j]) && random.Next(24) < 2)
                chars[j] = "*";
        }

        for (int k = 0; k < chars.Count; k++)
        {
            if (chars[k] == " " && random.Next(10) < 3)
            {
                chars.InsertRange(k + 1, Hiccup.Select(c => c.ToString()));
                k += Hiccup.Length;
            }
        }

        if (chars.Count >= MaxLength)
        {
            chars.RemoveRange(MaxLength - 2, chars.Count - (MaxLength - 2));
            chars.AddRange([".", "."]);
        }

        return string.Concat(chars);
    }

    private static List<string> SplitTextElements(string text)
    {
        var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
            elements.Add(enumerator.GetTextElement());
        return elements;
    }

    private static bool IsAOrE(string c) => c is "a" or "e" or "A" or "E";

    private static bool IsUOrO(string c) => c is "u" or "o" or "U" or "O";

    // Capital I is starred like the others, though only lowercase i gets an added "u".
    private static bool IsVowel(string c) => IsAOrE(c) || IsUOrO(c) || c is "i" or "I";
}
