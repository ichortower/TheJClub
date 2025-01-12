using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ichortower.TheJClub;

internal sealed class Bonchinate : IMutateMode
{
    public string Mutate(string input)
    {
        if (string.IsNullOrEmpty(input)) {
            return "Boncher";
        }
        var items = input.Split(" ", StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries).Select(BonchWord).ToArray();
        return String.Join(" ", items);
    }

    public static Dictionary<string, string> Overrides = null;

    private string BonchWord(string word)
    {
        if (Overrides.TryGetValue(word, out string o)) {
            return o;
        }
        int leading = 0;
        while (isvowel(word[leading])) {
            ++leading;
        }
        // prefer to use an O if present
        int start = word.IndexOf("o", leading, StringComparison.OrdinalIgnoreCase);
        if (start == -1) {
            start = word.IndexOfAny(new char[]{'a','e','i','u','y','A','E','I','U','Y'}, leading);
        }
        // if no vowels, fuck
        // length <= 3 is for "M." and probably "Mr."
        if (start == -1) {
            if (word.Length <= 3) {
                return word;
            }
            return word[0] + "onch"; // FIXME not correct
        }
        int cstart = start;
        while (cstart < word.Length && isvowel(word[cstart])) {
            ++cstart;
        }
        int cend = cstart;
        while (cend < word.Length && !isvowel(word[cend])) {
            ++cend;
        }
        string first = word.Substring(0, start);
        string cons = word.Substring(cstart, cend - cstart);
        string last = word.Substring(cend);
        cons = GetBehavior(cons).Invoke(cons);
        return first + "onch" + cons + last;
    }

    private bool isvowel(char c)
    {
        return "aeiouyAEIOUY".IndexOf(c) >= 0;
    }

    private void Log(string t)
    {
        Main.instance.Monitor.Log(t, LogLevel.Info);
    }

    private Func<string, string> GetBehavior(string segment)
    {
        switch (segment) {
        case "b":
            return TakeNone;
        case "ll":
        case "tr":
        case "bb":
        case "rn":
        case "st":
        case "sp":
        case "ms":
            return TakeFirst;
        default:
            return TakeAll;
        }
    }

    private string TakeFirst(string segment)
    {
        return segment.Substring(1);
    }

    private string TakeNone(string segment)
    {
        return segment;
    }

    private string TakeAll(string segment)
    {
        return "";
    }
}
