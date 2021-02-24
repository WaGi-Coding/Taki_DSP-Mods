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
using System.IO;
using System.Collections.Generic;
//using xiaoye97;
using BepInEx.Logging;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace PowerNodeMultipliers
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
    public class PowerNodeMultipliersPlugin : BaseUnityPlugin
    {
        public static ManualLogSource logger;

        public const string ModGuid = "com.Taki7o7.PowerNodeMultipliers";
        public const string ModName = "PowerNodeMultipliers";
        public const string ModVer = "1.0.1";

        public static string generatorValues { get; set; } = string.Empty;

        //public static bool loadedOnce = false;

        public static BepInEx.Configuration.ConfigFile myCfg;

        public static Dictionary<int, long> GenModDict;


        // Tesla Tower
        public static float TeslaTowerConn { get; set; } = 3.0f;
        public static float TeslaTowerCover { get; set; } = 2.0f;


        // Wireless Power Station
        public static float WirelessConn { get; set; } = 3.0f;
        public static float WirelessCover { get; set; } = 2.0f;


        // Satellite Substation
        public static float SatConn { get; set; } = 1.5f;
        public static float SatCover { get; set; } = 1.5f;


        public void Awake()
        {
            logger = base.Logger;

            // Tesla Tower
            TeslaTowerConn = Config.Bind<float>("-| 1 Tesla Tower", "Connection Distance Multiplier", 3.0f, "Multiplies the Connection Distance of the Tesla Tower").Value;
            TeslaTowerCover = Config.Bind<float>("-| 1 Tesla Tower", "Cover Radius Multiplier", 2.0f, "Multiplies the Cover Radius of the Tesla Tower").Value;

            // Wireless Power Station
            WirelessConn = Config.Bind<float>("-| 2 Wireless Power Station", "Connection Distance Multiplier", 3.0f, "Multiplies the Connection Distance of the Wireless Power Station").Value;
            WirelessCover = Config.Bind<float>("-| 2 Wireless Power Station", "Cover Radius Multiplier", 2.0f, "Multiplies the Cover Radius of the Wireless Power Station").Value;

            // Satellite Substation
            SatConn = Config.Bind<float>("-| 3 Satellite Substation", "Connection Distance Multiplier", 1.5f, "Multiplies the Connection Distance of the Satellite Substation").Value;
            SatCover = Config.Bind<float>("-| 3 Satellite Substation", "Cover Radius Multiplier", 1.5f, "Multiplies the Cover Radius of the Satellite Substation").Value;


            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(PowerNodeMultipliersPatch));
        }

        public void OnDestroy()
        {

        }
    }

    [HarmonyPatch]
    public class PowerNodeMultipliersPatch
    {
        static bool alreadyInitialized = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), "InvokeOnLoadWorkEnded")] // maybe swap with normal VFPreload if not supporting modded tesla towers? or later preloadpostpatch LDBTool one again if already done
        public static void LDBVFPreloadPostPatchPostfix() // Do when LDB is done
        {
            if (!alreadyInitialized) // Don't do when loading back into main menu
            {
                PowerNodeMultipliersPlugin.logger.LogInfo("### Attemting to edit Tesla Tower Item");
                LDB.items.Select(2201).prefabDesc.powerConnectDistance *= PowerNodeMultipliersPlugin.TeslaTowerConn;
                LDB.items.Select(2201).prefabDesc.powerCoverRadius *= PowerNodeMultipliersPlugin.TeslaTowerCover;
                PowerNodeMultipliersPlugin.logger.LogInfo("### Editing Tesla Tower successful!");
                PowerNodeMultipliersPlugin.logger.LogInfo("### Attemting to edit Wireless Power Station Item");
                LDB.items.Select(2202).prefabDesc.powerConnectDistance *= PowerNodeMultipliersPlugin.WirelessConn;
                LDB.items.Select(2202).prefabDesc.powerCoverRadius *= PowerNodeMultipliersPlugin.WirelessCover;
                PowerNodeMultipliersPlugin.logger.LogInfo("### Editing Wireless Power Station successful!");
                PowerNodeMultipliersPlugin.logger.LogInfo("### Attemting to edit Satellite Substation Item");
                LDB.items.Select(2212).prefabDesc.powerConnectDistance *= PowerNodeMultipliersPlugin.SatConn;
                LDB.items.Select(2212).prefabDesc.powerCoverRadius *= PowerNodeMultipliersPlugin.SatCover;
                PowerNodeMultipliersPlugin.logger.LogInfo("### Editing Satellite Substation successful!");

                alreadyInitialized = true;
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "NewNodeComponent")]
        public static bool OnNodeAdded(PowerSystem __instance, ref int entityId, ref float conn, ref float cover)
        {
            PowerNodeMultipliersPlugin.logger.LogInfo($"### ORIGINAL Tesla Tower: Connection-Distance = {conn} | Cover-Radius = {cover}");
            //conn *= 2;
            //cover *= 2;
            return true;
        }
    }
}
