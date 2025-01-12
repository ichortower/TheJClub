using System;
using System.Collections.Generic;
using System.Linq;

namespace ichortower.TheJClub;

internal sealed class Jayify : IMutateMode
{
    public string Mutate(string input)
    {
        if (string.IsNullOrEmpty(input)) {
            return "J";
        }
        var items = input.Split(" ", StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries).Select(JayifyWord).ToArray();
        return String.Join(" ", items);
    }

    private string JayifyWord(string word)
    {
        if (word.ToUpper().StartsWith("J")) {
            return word;
        }
        foreach (string s in scunthorpe) {
            // the 1 here is hardcoded because we know the scunthorpes are
            // two letters (vowel-consonant). change if needed later
            if (word.ToLower().StartsWith(s)) {
                return "Ja" + word.Substring(1);
            }
            if (word.ToLower().Substring(1).StartsWith(s)) {
                return "J" + word.ToLower();
            }
        }
        if ("aeiouAEIOU".IndexOf(word[0]) >= 0) {
            return "J" + word.Substring(0,1).ToLower() + word.Substring(1);
        }
        return "J" + word.Substring(1);
    }

    /*
     * Intended to prevent certain unfortunate words from occurring.
     * Currently: jew, jizz
     */
    private static List<string> scunthorpe = new() {
        "ew", "iz"
    };
}
