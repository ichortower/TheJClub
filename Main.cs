using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ichortower.TheJClub
{
    internal sealed class Main : Mod
    {
        public static Main instance = null;
        public static string ModId = null;

        public override void Entry(IModHelper helper)
        {
            Main.instance = this;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Specialized.LoadStageChanged += OnLoadStageChanged;
            OtherNames = helper.Data.ReadJsonFile<List<string>>("assets/OtherNames.json")
                    ?? new List<string>();
            var harmony = new Harmony(this.ModManifest.UniqueID);

            MethodInfo NPC_showTextAboveHead = typeof(NPC).GetMethod(
                    nameof(NPC.showTextAboveHead),
                    BindingFlags.Public | BindingFlags.Instance);
            harmony.Patch(NPC_showTextAboveHead,
                    postfix: new HarmonyMethod(typeof(Main),
                        nameof(this.NPC_showTextAboveHead_Postfix)));

            // using ParseGiftReveals is a bit weird, but it runs right before
            // the dialogue is displayed and we have to come after it or else
            // we break gift taste reveals
            MethodInfo Utility_ParseGiftReveals = typeof(Utility).GetMethod(
                    nameof(Utility.ParseGiftReveals),
                    BindingFlags.Public | BindingFlags.Static);
            harmony.Patch(Utility_ParseGiftReveals,
                    postfix: new HarmonyMethod(typeof(Main),
                        nameof(this.Utility_ParseGiftReveals_Postfix)));
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // extra lateness to add to the Late patch priorities
            int AfterEverybody = 250;

            // handle everyone who uses the standard location
            if (e.NameWithoutLocale.IsEquivalentTo("Strings/NPCNames")) {
                e.Edit(data => {
                    var dict = data.AsDictionary<string, string>();
                    foreach (var entry in dict.Data) {
                        dict.Data[entry.Key] = DisplayNameMap[entry.Value] =
                                Jayify(entry.Value);
                    }
                }, AssetEditPriority.Late + AfterEverybody);
            }
            // try to catch NPCs who throw it directly into the character entry
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Characters")) {
                e.Edit(data => {
                    var dict = data.AsDictionary<string, CharacterData>();
                    foreach (var entry in dict.Data) {
                        string displayName = entry.Value.DisplayName;
                        if (!displayName.StartsWith("[LocalizedText")) {
                            entry.Value.DisplayName = DisplayNameMap[displayName] =
                                    Jayify(displayName);
                        }
                    }
                }, AssetEditPriority.Late + AfterEverybody);
            }
            // replace names appearing in Strings/ assets, which may not be
            // run through Dialogue before displaying (e.g. weapon names)
            if (e.NameWithoutLocale.StartsWith("Strings/") && NPCListReady &&
                    !e.NameWithoutLocale.IsEquivalentTo("Strings/NPCNames")) {
                e.Edit(StringStringDictNameReplace,
                        AssetEditPriority.Late + AfterEverybody);
            }
            // a few Data/ stragglers that don't farm out their text to Strings
            foreach (string name in StringStringDataAssets) {
                if (e.NameWithoutLocale.IsEquivalentTo(name) && NPCListReady) {
                    e.Edit(StringStringDictNameReplace,
                            AssetEditPriority.Late + AfterEverybody);
                }
            }
        }

        private void StringStringDictNameReplace(IAssetData asset)
        {
            var dict = asset.AsDictionary<string, string>();
            foreach (var entry in dict.Data) {
                string s = entry.Value;
                foreach (var pair in DisplayNameMap) {
                    s = s.Replace(pair.Key, pair.Value);
                }
                dict.Data[entry.Key] = s;
            }
        }

        private void OnLoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage >= LoadStage.Preloaded && !NPCListReady &&
                    DisplayNameMap.Count > 0) {
                foreach (string name in OtherNames) {
                    DisplayNameMap[name] = Jayify(name);
                }
                NPCListReady = true;
                Helper.GameContent.InvalidateCache(asset => {
                    return asset.NameWithoutLocale.StartsWith("Strings/") ||
                            StringStringDataAssets.Contains(asset.NameWithoutLocale.ToString());
                });
            }
        }

        private static void NPC_showTextAboveHead_Postfix(NPC __instance)
        {
            string s = (string)NPC_textAboveHead.GetValue(__instance);
            foreach (var pair in DisplayNameMap) {
                s = s.Replace(pair.Key, pair.Value);
            }
            NPC_textAboveHead.SetValue(__instance, s);
        }

        private static FieldInfo NPC_textAboveHead = typeof(NPC).GetField(
                "textAboveHead", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void Utility_ParseGiftReveals_Postfix(
                ref string __result)
        {
            foreach (var pair in DisplayNameMap) {
                __result = __result.Replace(pair.Key, pair.Value);
            }
        }

        private string Jayify(string input)
        {
            if (input is null) {
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
            if ("aeiouAEIOU".IndexOf(word[0]) > 0) {
                return "J" + word.Substring(0,1).ToLower() + word.Substring(1);
            }
            return "J" + word.Substring(1);
        }

        /*
         * When NPCs' names are Jayified for the first time, save them here.
         * This serves as the substitution dictionary for ordinary and event
         * dialogue.
         */
        private static Dictionary<string, string> DisplayNameMap = new();

        /*
         * Flag to prevent string edits before the NPC list is ready.
         * Set to true after reaching a load stage where the list is populated.
         */
        private static bool NPCListReady = false;

        /*
         * Intended to prevent certain unfortunate words from occurring.
         * Currently: jew, jizz
         */
        private static List<string> scunthorpe = new() {
            "ew", "iz"
        };

        /*
         * List of assets using string->string models that require NPC name
         * substitutions. This is the part where I bet I missed some.
         */
        private static List<string> StringStringDataAssets = new() {
            "Data/hats", "Data/Quests",
        };

        /*
         * Loaded from assets/OtherNames.json in Entry
         */
        private static List<string> OtherNames = null;
    }
}
