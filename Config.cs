using StardewModdingAPI;
using System;

namespace ichortower.TheJClub;

internal sealed class ModConfig
{
    public AltMode Mode { get; set; } = AltMode.Default;
}

internal enum AltMode {
    Default,
    Boncher,
}

internal sealed class JConf
{
    public static bool InvalidateOnSave = false;

    public static void Register()
    {
        var cmapi = Main.instance.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
                "spacechase0.GenericModConfigMenu");
        if (cmapi is null) {
            return;
        }

        IManifest me = Main.instance.ModManifest;
        cmapi.Register(
            mod: me,
            reset: () => Main.Config = new ModConfig(),
            save: () => {
                if (InvalidateOnSave) {
                    Main.SelectMode();
                    Main.ForgetNames();
                }
                InvalidateOnSave = false;
            }
        );
        cmapi.AddTextOption(
            mod: me,
            name: () => TR.Get("gmcm.mode.name"),
            fieldId: "Mode",
            tooltip: () => TR.Get("gmcm.mode.tooltip"),
            allowedValues: Enum.GetNames<AltMode>(),
            getValue: () => Main.Config.Mode.ToString(),
            setValue: value => {
                var v = (AltMode)Enum.Parse(typeof(AltMode), value);
                if (v != Main.Config.Mode) {
                    InvalidateOnSave = true;
                }
                Main.Config.Mode = v;
            }
        );
        cmapi.AddParagraph(
            mod: me,
            text: () => TR.Get("gmcm.blurb.Default") + "\n" +
                    TR.Get("gmcm.blurb.Boncher")
        );
    }
}

internal sealed class TR
{
    public static string Get(string key) {
        return Main.instance.Helper.Translation.Get(key);
    }
}


public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);
    void AddParagraph(IManifest mod, Func<string> text);
}
