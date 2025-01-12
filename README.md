# The J Club

Have you ever lamented the absolutely criminal lack of custom NPCs whose names
start with J?

I have too. There are just so few of them! That is what this mod is meant to
address. When you install it, every NPC's display name will be modified to
start with the letter J. This applies to each word in the name (so, for
example, Jister Ji and Jrofessor Jnail), which seems only fair and correct, as
it increases the number of Js.

This mod should automatically work with custom NPCs, but see below for details.

## And, Introducing... Alt Modes!

This mod now features the ability to switch between different mutators, or
"modes". At this time, there is only one alt mode, "Boncher", which inserts
"onch" into character names. More alt modes may be added in the future.

To switch modes, use Generic Mod Config Menu and choose the mode you want, or
edit your config.json by hand like an animal. The option is called `Mode`
and the allowed values are `Default` (default J behavior) and `Boncher`.

# Caveats (Technical)

This mod accounts for any custom NPC that either:

1. puts its display name into Strings/NPCNames (the standard location) and
  references it with [LocalizedText], or
2. puts its display name directly into the DisplayName field in its
  Data/Characters entry.

If an NPC puts its name somewhere else, this mod won't find it, and you will
have one fewer J-named person, which is of course a tragedy.

This mod also attempts to find and replace NPC names in other assets whose text
is displayed to the player. It is possible, or maybe even likely, that I did
not find all of the assets that contain NPC names: please let me know if you
see a non-J name. In addition, to do many of these edits, it is necessary to
invalidate the assets involved after the NPCs load in, so some assets must be
reloaded. If you have a lot of mods requesting edits on those assets, this may
slightly increase your load times.

Some strings are written to your save data (for example, active quests), and
those are not edited, so if you add this mod to a file with active quests, you
may see non-J names in the quest text. The author regrets this infelicity.

You may experience strange results in non-English languages. I do not mean to
exclude those languages, so please let me know if something bad or offensive
appears so I can try to address it.

This mod attempts to avoid generating unfortunate names. It probably isn't
perfect! Please report icky results if you find any.
