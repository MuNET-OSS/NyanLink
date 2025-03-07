using MelonLoader;
using Tomlet;

[assembly: MelonInfo(typeof(WorldLink.Core), "WorldLink", "1.0.0", "Azalea")]
[assembly: MelonGame("sega-interactive", "Sinmai")]

namespace WorldLink;

public class Config
{
    public string LobbyUrl { get; set; }
    public bool Debug { get; set; }
}

public class Core : MelonMod
{
    public static Config Config;
    
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("""
/=============================\
|  .  .      .  ..       .    |
|  |  | _ ._.| _||   *._ ;_/  |
|  |/\|(_)[  |(_]|___|[ )| \  |
\=============================/
  |      Version 1.0.0      |
  |   Made with <3 by Aza   |
  \=========================/
""");
        
        // Load config
        LoggerInstance.Msg("Loading config...");
        Config = TomletMain.To<Config>(File.ReadAllText("WorldLink.toml"));
     
        LoggerInstance.Msg("Patching...");
        Futari.OnBeforePatch();
        HarmonyLib.Harmony.CreateAndPatchAll(typeof(Futari));
        
        if (Config.Debug)
        {
            LoggerInstance.Msg("Patching debug...");
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Futari.FutariDebug));
        }
        
        LoggerInstance.Msg("Initialized.");
    }
}