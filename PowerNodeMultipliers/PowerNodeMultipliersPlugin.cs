using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;

using UnityEngine;
using System.Security;
using System.Security.Permissions;
using System.IO;
using System.Collections.Generic;
//using xiaoye97;
using BepInEx.Logging;
using System.Reflection.Emit;
using System.Linq;
using BepInEx.Configuration;

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
        public const string ModVer = "1.0.5";

        public static string generatorValues { get; set; } = string.Empty;

        //public static bool loadedOnce = false;

        public static BepInEx.Configuration.ConfigFile myCfg;

        public static Dictionary<int, long> GenModDict;


        // Tesla Tower
        public static float TeslaTowerConn { get; set; } = 3.0f;
        public static float TeslaTowerCover { get; set; } = 2.0f;


        // Wireless Power Station
        public static float WirelessConn { get; set; } = 3.0f;
        public static int WirelessCover { get; set; } = 2;
        public static float WirelessPowerMultiplier { get; set; } = 3.0f;


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
            WirelessCover = Config.Bind<int>("-| 2 Wireless Power Station", "Cover Radius Multiplier", 2, new ConfigDescription("Multiplies the Cover Radius of the Wireless Power Station", new AcceptableValueRange<int>(1, 4))).Value;
            WirelessPowerMultiplier = Config.Bind<float>("-| 2 Wireless Power Station", "Charging Power Multiplier", 3.0f, "Multiplies the Maximum Charging Power of Wireless Power Stations").Value;

            // Satellite Substation
            SatConn = Config.Bind<float>("-| 3 Satellite Substation", "Connection Distance Multiplier", 1.5f, "Multiplies the Connection Distance of the Satellite Substation").Value;
            SatCover = Config.Bind<float>("-| 3 Satellite Substation", "Cover Radius Multiplier", 1.5f, new ConfigDescription("Multiplies the Cover Radius of the Satellite Substation", new AcceptableValueRange<float>(1f, 150f))).Value;


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

        // [HarmonyTranspiler]
        // [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        // static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        // {
        //     CodeMatcher matcher = new CodeMatcher(instructions)
        //.MatchForward(true,
        //    new CodeMatch(OpCodes.Ldloc_S),
        //    new CodeMatch(OpCodes.Ldelema),
        //    new CodeMatch(i =>
        //        i.opcode == OpCodes.Ldflda && ((FieldInfo)i.operand).Name == nameof(PowerNodeComponent.powerPoint)),
        //    new CodeMatch(i =>
        //        i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == nameof(Vector3.x)),
        //    new CodeMatch(i =>
        //        i.opcode == OpCodes.Ldc_R4 && (Convert.ToSingle(i.operand)) == 0.988f),
        //    new CodeMatch(OpCodes.Mul),
        //    new CodeMatch(OpCodes.Ldloca_S),
        //    new CodeMatch(i =>
        //    i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == nameof(Vector3.x)),
        //    new CodeMatch(OpCodes.Sub),
        //    new CodeMatch(OpCodes.Stloc_S));

        //     float cur = Convert.ToSingle(matcher.Operand);

        //     matcher.SetOperandAndAdvance((float)(cur / (1.67f * PowerNodeMultipliersPlugin.WirelessCover)));

        //     return matcher.InstructionEnumeration();
        // }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PowerSystem), "GameTick")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {

                //if (code[i].operand is FieldInfo field && field.Name == nameof(PowerNodeComponent.coverRadius))
                //{
                //    curCover = (code[i].operand is Single ? Convert.ToSingle(code[i].operand) : 6.5f);
                //    PowerNodeMultipliersPlugin.logger.LogWarning(code[i].operand.ToString());
                //    PowerNodeMultipliersPlugin.logger.LogWarning("curCover saved");
                //}
                if (code[i].LoadsConstant(15f))
                {
                    code[i].operand = 26.5f;
                    PowerNodeMultipliersPlugin.logger.LogMessage("(Replaced 15f with 26.5f)");
                }
                if (code[i].LoadsConstant(64.05))
                {
                    code[i].operand = (PowerNodeMultipliersPlugin.WirelessCover == 4 ? 700.0 : PowerNodeMultipliersPlugin.WirelessCover == 3 ? 400.0 : PowerNodeMultipliersPlugin.WirelessCover == 2 ? 180.0 : 64.05);
                    //code[i].operand = 6.405 * PowerNodeMultipliersPlugin.WirelessCover * (PowerNodeMultipliersPlugin.WirelessCover > 1f ? PowerNodeMultipliersPlugin.WirelessCover - 1 : 1) * 9.85f;
                    PowerNodeMultipliersPlugin.logger.LogMessage($"(Replaced 64.05 with {(PowerNodeMultipliersPlugin.WirelessCover == 4 ? 760.0 : PowerNodeMultipliersPlugin.WirelessCover == 3 ? 400.0 : PowerNodeMultipliersPlugin.WirelessCover == 2 ? 150.0 : 64.05)})");
                }
            }
            return code.AsEnumerable();
        }

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
                LDB.items.Select(2202).prefabDesc.workEnergyPerTick = Convert.ToInt64(LDB.items.Select(2202).prefabDesc.workEnergyPerTick * PowerNodeMultipliersPlugin.WirelessPowerMultiplier);
                PowerNodeMultipliersPlugin.logger.LogInfo("### Editing Wireless Power Station successful!");
                PowerNodeMultipliersPlugin.logger.LogInfo("### Attemting to edit Satellite Substation Item");
                LDB.items.Select(2212).prefabDesc.powerConnectDistance *= PowerNodeMultipliersPlugin.SatConn;
                LDB.items.Select(2212).prefabDesc.powerCoverRadius *= PowerNodeMultipliersPlugin.SatCover;
                PowerNodeMultipliersPlugin.logger.LogInfo("### Editing Satellite Substation successful!");

                alreadyInitialized = true;
            }
        }



        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(PowerNodeComponent), "Import")]
        //public static bool ImportPre(PowerNodeComponent __instance, ref BinaryReader r)
        //{
        //    r.ReadInt32();
        //    __instance.id = r.ReadInt32();
        //    __instance.entityId = r.ReadInt32();
        //    __instance.networkId = r.ReadInt32();
        //    __instance.isCharger = r.ReadBoolean();
        //    __instance.workEnergyPerTick = r.ReadInt32();
        //    __instance.idleEnergyPerTick = r.ReadInt32();
        //    __instance.requiredEnergy = r.ReadInt32();
        //    __instance.powerPoint.x = r.ReadSingle();
        //    __instance.powerPoint.y = r.ReadSingle();
        //    __instance.powerPoint.z = r.ReadSingle();
        //    __instance.connectDistance = r.ReadSingle();
        //    __instance.coverRadius = r.ReadSingle();

        //    //if (__instance.isCharger && __instance.idleEnergyPerTick == 1500) // Wireless Charging Station
        //    //{
        //    //    __instance.coverRadius = 6.5f * PowerNodeMultipliersPlugin.WirelessCover;
        //    //    //__instance.connectDistance = 45.5f * PowerNodeMultipliersPlugin.WirelessConn;

        //    //    //__instance.coverRadius = 6.5f;
        //    //    //__instance.connectDistance = 45.5f;
        //    //}
        //    //if (__instance.isCharger && __instance.idleEnergyPerTick == 6000) // Orbital Substation
        //    //{
        //    //    __instance.coverRadius = 26.5f * PowerNodeMultipliersPlugin.SatCover;
        //    //    //__instance.connectDistance = 53.5f * PowerNodeMultipliersPlugin.SatConn;
        //    //}
        //    //if (!__instance.isCharger && __instance.idleEnergyPerTick == 0) // Tesla Tower
        //    //{
        //    //    __instance.coverRadius = 10.5f * PowerNodeMultipliersPlugin.TeslaTowerCover;
        //    //    //__instance.connectDistance = 22.5f * PowerNodeMultipliersPlugin.TeslaTowerConn;
        //    //}


        //    return false;
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PowerNodeComponent), "Import")]
        //public static void PowerNodeImportPostfix(PowerNodeComponent __instance, ref BinaryReader r)
        //{

        //    PowerNetwork x = new PowerNetwork();
            

        //    Debug.LogError(__instance.powerPoint.y);
        //    if (__instance.isCharger && __instance.idleEnergyPerTick == 1500) // Wireless Charging Station
        //    {
        //        __instance.coverRadius = 6.5f * PowerNodeMultipliersPlugin.WirelessCover;
        //        __instance.connectDistance = 45.5f * PowerNodeMultipliersPlugin.WirelessConn;
                
        //        //__instance.coverRadius = 6.5f;
        //        //__instance.connectDistance = 45.5f;
        //    }
        //    if (__instance.isCharger && __instance.idleEnergyPerTick == 6000) // Orbital Substation
        //    {
        //        __instance.coverRadius = 26.5f * PowerNodeMultipliersPlugin.SatCover;
        //        __instance.connectDistance = 53.5f * PowerNodeMultipliersPlugin.SatConn;
        //    }
        //    if (!__instance.isCharger && __instance.idleEnergyPerTick == 0) // Tesla Tower
        //    {
        //        __instance.coverRadius = 10.5f * PowerNodeMultipliersPlugin.TeslaTowerCover;
        //        __instance.connectDistance = 22.5f * PowerNodeMultipliersPlugin.TeslaTowerConn;
        //    }
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(PowerSystem), "NewNodeComponent")]
        //public static bool OnNodeAdded(PowerSystem __instance, ref int entityId, ref float conn, ref float cover)
        //{
        //    //PowerNodeMultipliersPlugin.logger.LogInfo($"### ORIGINAL Tesla Tower: Connection-Distance = {conn} | Cover-Radius = {cover}");
        //    //conn *= 2;
        //    //cover *= 2;
        //    return true;
        //}
    }
}
