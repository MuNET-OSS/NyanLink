using MelonLoader;

namespace WorldLink;

public class Config
{
    public string LobbyUrl { get; set; }
    public string RelayUrl { get; set; }
    public bool Debug { get; set; }
}
public class Core : MelonMod
{
    public static Config Config;

    public override void OnInitializeMelon()
    {
        // Load config
        LoggerInstance.Msg("Loading config...");
        Config = TomletShim.To<Config>(File.ReadAllText("WorldLink.toml"));

        LoggerInstance.Msg("Patching...");
        Futari.OnBeforePatch();
        HarmonyLib.Harmony.CreateAndPatchAll(typeof(Futari));

        LoggerInstance.Msg("Initialized.");
    }
}