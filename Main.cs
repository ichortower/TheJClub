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
        public static IMutateMode Mode = null;
        public static ModConfig Config = null;

        public override void Entry(IModHelper helper)
        {
            Main.instance = this;
            Config = helper.ReadConfig<ModConfig>();
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Specialized.LoadStageChanged += OnLoadStageChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            OtherNames = helper.Data.ReadJsonFile<List<string>>
                    ("assets/OtherNames.json") ?? new List<string>();
            Bonchinate.Overrides = helper.Data.ReadJsonFile<Dictionary<string, string>>
                    ("assets/Boncher.json") ?? new Dictionary<string, string>();
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

            SelectMode();
        }

        public static void SelectMode()
        {
            instance.Monitor.Log($"{Config.Mode}", LogLevel.Warn);
            Mode = Config.Mode switch {
                AltMode.Boncher => new Bonchinate(),
                _ => new Jayify()
            };
        }

        public static void ForgetNames()
        {
            DisplayNameMap.Clear();
            NPCListReady = false;
            instance.Helper.GameContent.InvalidateCache("Strings/NPCNames");
            instance.Helper.GameContent.InvalidateCache("Data/Characters");
            instance.Helper.GameContent.InvalidateCache(asset => {
                return asset.NameWithoutLocale.StartsWith("Strings/") ||
                        StringStringDataAssets.Contains(asset.NameWithoutLocale.ToString());
            });
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
                                Mode.Mutate(entry.Value);
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
                                    Mode.Mutate(displayName);
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

        private void OnLoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage >= LoadStage.Preloaded && !NPCListReady &&
                    DisplayNameMap.Count > 0) {
                foreach (string name in OtherNames) {
                    DisplayNameMap[name] = Mode.Mutate(name);
                }
                NPCListReady = true;
                Helper.GameContent.InvalidateCache(asset => {
                    return asset.NameWithoutLocale.StartsWith("Strings/") ||
                            StringStringDataAssets.Contains(asset.NameWithoutLocale.ToString());
                });
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            JConf.Register();
        }

        private void StringStringDictNameReplace(IAssetData asset)
        {
            var dict = asset.AsDictionary<string, string>();
            foreach (var entry in dict.Data) {
                dict.Data[entry.Key] = ReplaceNames(entry.Value);
            }
        }

        private static void NPC_showTextAboveHead_Postfix(NPC __instance)
        {
            string s = (string)NPC_textAboveHead.GetValue(__instance);
            NPC_textAboveHead.SetValue(__instance, ReplaceNames(s));
        }

        private static FieldInfo NPC_textAboveHead = typeof(NPC).GetField(
                "textAboveHead", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void Utility_ParseGiftReveals_Postfix(
                ref string __result)
        {
            __result = ReplaceNames(__result);
        }

        private static string ReplaceNames(string s)
        {
            foreach (string key in DisplayNameMap.Keys) {
                s = s.Replace(key, DisplayNameMap[key]);
            }
            return s;
        }


        /*
         * When NPCs' names are Jayified for the first time, save them here.
         * This serves as the substitution dictionary for ordinary and event
         * dialogue.
         * The keys are sorted descending by length, then ascending by value.
         * Replacing longer keys first makes certain edge cases work better
         * ("Pierre's Prime" is replaced before "Pierre"; if Pierre went first,
         * Prime would be left unjayed).
         */
        private static SortedDictionary<string, string> DisplayNameMap =
                new(new comp());

        private class comp : Comparer<string>
        {
            public override int Compare(string a, string b) {
                if (a.Length == b.Length) {
                    return a.CompareTo(b);
                }
                return b.Length.CompareTo(a.Length);
            }
        }

        /*
         * Flag to prevent string edits before the NPC list is ready.
         * Set to true after reaching a load stage where the list is populated.
         */
        private static bool NPCListReady = false;

        /*
         * List of assets using string->string models that require NPC name
         * substitutions. This is the part where I bet I missed some.
         */
        private static List<string> StringStringDataAssets = new() {
            "Data/hats", "Data/mail", "Data/Quests",
        };

        /*
         * Loaded from assets/OtherNames.json in Entry
         */
        private static List<string> OtherNames = null;
    }
}
