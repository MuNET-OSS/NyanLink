using MelonLoader;
using WorldLink;

static class AquaMai
{
    public static string ReadString(string key)
    {
        return key switch
        {
            "Mods.WorldLink.LobbyUrl" => Core.Config.LobbyUrl,
            "Mods.WorldLink.RelayUrl" => Core.Config.RelayUrl,
            _ => null
        };
    }
}

[AttributeUsage(AttributeTargets.Class)]
class EnableIf: Attribute
{
    public EnableIf(Type tpye, string condition) { }
}
    
[AttributeUsage(AttributeTargets.All)]
class ConfigEntry: Attribute
{
    public ConfigEntry(bool hideWhenDefault) { }

    public ConfigEntry(string name, string desc) { }
}

[AttributeUsage(AttributeTargets.All)]
public class ConfigSection: Attribute
{
    public ConfigSection(string zh, string en, bool defaultOn = false) { }
}