using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace MechaEnergyUseMultiplier
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
    public class MechaEnergyUseMultiplierPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Taki7o7.MechaEnergyUseMultiplier";
        public const string ModName = "MechaEnergyUseMultiplier";
        public const string ModVer = "1.0.2";

        public static double multiplier { get; private set; } = 0.5;

        public void Awake()
        {
            
            multiplier = Config.Bind<double>("Mod-Settings", "Energy use multiplier", 0.5, "The actual Energy Use Value will get multiplied by this Value. Mod default is set to 0.5 which is 50% Energy Use.").Value;

            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(MechaEnergyUseMultiplierPatch));
        }
    }

    [HarmonyPatch]
    public class MechaEnergyUseMultiplierPatch
    {
        static double customMultiplier = MechaEnergyUseMultiplierPlugin.multiplier;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Mecha),"UseEnergy")]
        public static bool UseEnergyPrefix(ref double energyUse)
        {
            energyUse *= customMultiplier;
            return true;
        }
    }
}
