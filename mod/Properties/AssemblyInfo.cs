using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(WorldLink.BuildInfo.Name)]
[assembly: AssemblyProduct(WorldLink.BuildInfo.Name)]
[assembly: AssemblyCopyright(WorldLink.BuildInfo.Author)]
[assembly: AssemblyVersion(WorldLink.BuildInfo.Version)]
[assembly: AssemblyFileVersion(WorldLink.BuildInfo.GitVersion)]
[assembly: MelonInfo(typeof(WorldLink.Core), WorldLink.BuildInfo.Name, WorldLink.BuildInfo.GitVersion, WorldLink.BuildInfo.Author)]
[assembly: MelonColor(0, 172, 181, 250)]
[assembly: HarmonyDontPatchAll]
[assembly: MelonGame("sega-interactive", "Sinmai")]