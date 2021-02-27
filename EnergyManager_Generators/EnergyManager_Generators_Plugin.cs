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
using xiaoye97;
using BepInEx.Logging;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace EnergyManager_Generators
{
    enum generatorType : short
    {
        WindTurbine = 2203,
        Solar = 2205,
        Thermal = 2204,
        MiniFusion = 2211,
        ArtificialStar = 2210,
    }

    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    public class EnergyManager_Generators_Plugin : BaseUnityPlugin
    {
        public static ManualLogSource logger;

        public const string ModGuid = "com.Taki7o7.EnergyManager_Generators";
        public const string ModName = "EnergyManager_Generators";
        public const string ModVer = "1.0.3";

        public static bool EditExisting = false;
        public static bool AdvancedMode = false;

        
        //EASY MODE VANILLA

        //Wind 2203
        public static long modGenWindTurbine { get; set; } = 300;

        //Thermal 2204
        public static long modGenThermal { get; set; } = 2160;

        //Mini Fusion 2211
        public static long modGenMiniFusion { get; set; } = 9000;

        //Solar 2205
        public static long modGenSolar { get; set; } = 360;

        //Artificial Star 2210
        public static long modGenArtStar { get; set; } = 75000;



        // ADVANCED MODE
        public static string generatorValues { get; set; } = string.Empty;
        public static Dictionary<int, long> GenModDict;

        //public static bool loadedOnce = false;

        public static BepInEx.Configuration.ConfigFile myCfg;

        

        public void Awake()
        {
            logger = base.Logger;

            myCfg = Config;

            GenModDict = new Dictionary<int, long>();

            #region SettingsEasyMode_Generators

            modGenWindTurbine = Config.Bind<long>("-| 1 Easy Mode |1| Wind Turbine", "Max Power (kW)", 300, "Max kW a Wind Turbine can generate").Value;
            modGenSolar = Config.Bind<long>("-| 1 Easy Mode |2| Solar Panel", "Max Power (kW)", 360, "Max kW a Solar Panel can generate").Value;
            modGenThermal = Config.Bind<long>("-| 1 Easy Mode |3| Thermal Power Generator", "Max Power (kW)", 2160, "Max kW a Thermal Power Generator can generate").Value;
            modGenMiniFusion = Config.Bind<long>("-| 1 Easy Mode |4| Mini Fusion Power Generator", "Max Power (kW)", 9000, "Max kW a Mini Fusion Power Generator can generate").Value;
            modGenArtStar = Config.Bind<long>("-| 1 Easy Mode |5| Artificial Star", "Max Power (kW)", 75000, "Max kW a Artificial Star can generate").Value;
            #endregion


            AdvancedMode = Config.Bind<bool>("-| 2 Advanced Mode", "Activate Advanced Mode", false, "Advanced settings can be useful if you have additional modded Power Generators & want to specify different values for them.\nIn Advanced mode, Easy Mode settings get ignored!\nTo find out Modded ITEM IDs checkout \"\\BepInEx\\config\\LDBTool\\LDBTool.CustomID.cfg\" or ask the mod creator.").Value;
            generatorValues = Config.Bind<string>("-| 2 Advanced Mode Power Generators", "Power Generator ItemIDs Values", "2203:300|2205:360|2204:2160|2211:9000|2210:75000", "Here you can specify the Power generation values for Generators.\n\nFormat: {ItemID}:{PowerGen(kW)}\nSeperate multiple entries by |\nExample: 2203:300|2205:360|2204:2160|2211:9000|2210:75000\n\nVanilla IDs:\n2203 = Wind Turbine\n2205 = Solar Panel\n2204 = Thermal Power Generator\n2211 = Mini Fusion Generator\n2210 = Artificial Star").Value;

            EditExisting = Config.Bind<bool>("-| 3 Edit Existing", "Edit existing Buildings on Load", false, "If enabled, the Mod will NOT ONLY work with new set Power Generator Buildings, but will also try to edit all existing ones when loading a Savegame. You can keep it disabled if you did once.\nAlso you can use this setting to reset to defaults(just enter the default values in any mode and activate this setting)\nNote that any changes made in the Config, require the game to restart!\nThis setting will automatically deactivate once you load a game after activating it.").Value;

            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(EnergyManagerPatch));
        }

        public void OnDestroy()
        {
            //Debug.Log("DESTROY EnergyManager_Generators #######################################################");

            ////Reset EditExisting to false if user exits and already had loaded at least one game
            //if (loadedOnce && EditExisting)
            //{
            //    EnergyManager_Generators_Plugin.logger.LogMessage("Disabling \"Edit Existing\" Setting to false...");
            //    Config["-| 3 Edit Existing", "Edit existing Buildings on Load"].BoxedValue = (bool)false;
            //    Config.Save();
            //    Config.Reload();
            //}


            //LDB.items.Select(2203).prefabDesc.genEnergyPerTick = Convert.ToInt64(Math.Round(LDB.items.Select(2203).prefabDesc.genEnergyPerTick / multiplier);
        }

        bool IsInEnum(short id)
        {
            return Enum.IsDefined(typeof(generatorType), id);
        }
    }

    [HarmonyPatch]
    public class EnergyManagerPatch
    {
        public static double kwMultiplier = 16.67;
        public static double mjMultiplier = 1000000;
        static bool alreadyInitialized = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LDBTool), "VFPreloadPostPatch")]
        public static void LDBVFPreloadPostPatchPostfix()
        {
            if (!alreadyInitialized) // Don't do when loading back into main menu
            {
                EnergyManager_Generators_Plugin.GenModDict = new Dictionary<int, long>();

                //EASY MODE
                #region EasyModeHandle
                if (!EnergyManager_Generators_Plugin.AdvancedMode)
                {
                    EnergyManager_Generators_Plugin.logger.LogMessage("============ Easy Mode Begin ============");

                    #region EditPowerGeneratorsVanillaEasyMode
                    if (EnergyManager_Generators_Plugin.modGenWindTurbine != (long)EnergyManager_Generators_Plugin.myCfg["-| 1 Easy Mode |1| Wind Turbine", "Max Power (kW)"].DefaultValue || EnergyManager_Generators_Plugin.EditExisting)
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### WindTurbine not default --- Attempting Edit...");
                        try
                        {
                            LDB.items.Select(2203).prefabDesc.genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.modGenWindTurbine * kwMultiplier));
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### WindTurbine Edited successfully!");
                            EnergyManager_Generators_Plugin.GenModDict.Add(2203, EnergyManager_Generators_Plugin.modGenWindTurbine);
                        }
                        catch (Exception)
                        {
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### Error when attempting to edit WindTurbine. Skipping WindTurbine... #######");
                        }
                    }
                    else
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### WindTurbine is default --- No Edit needed!");
                    }

                    if (EnergyManager_Generators_Plugin.modGenSolar != (long)EnergyManager_Generators_Plugin.myCfg["-| 1 Easy Mode |2| Solar Panel", "Max Power (kW)"].DefaultValue || EnergyManager_Generators_Plugin.EditExisting)
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### Solar not default --- Attempting Edit...");

                        try
                        {
                            LDB.items.Select(2205).prefabDesc.genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.modGenSolar * kwMultiplier));
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### Solar Edited successfully!");
                            EnergyManager_Generators_Plugin.GenModDict.Add(2205, EnergyManager_Generators_Plugin.modGenSolar);
                        }
                        catch (Exception)
                        {
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### Error when attempting to edit Solar. Skipping Solar...");
                        }
                    }
                    else
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### Solar is default --- No Edit needed!");
                    }

                    if (EnergyManager_Generators_Plugin.modGenThermal != (long)EnergyManager_Generators_Plugin.myCfg["-| 1 Easy Mode |3| Thermal Power Generator", "Max Power (kW)"].DefaultValue || EnergyManager_Generators_Plugin.EditExisting)
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### ThermalPowerGen not default --- Attempting Edit...");

                        try
                        {
                            LDB.items.Select(2204).prefabDesc.genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.modGenThermal * kwMultiplier));
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### ThermalPowerGen Edited successfully!");
                            EnergyManager_Generators_Plugin.GenModDict.Add(2204, EnergyManager_Generators_Plugin.modGenThermal);
                        }
                        catch (Exception)
                        {
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### Error when attempting to edit ThermalPowerGen. Skipping ThermalPowerGen...");
                        }
                    }
                    else
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### ThermalPowerGen is default --- No Edit needed!");
                    }
                    //EnergyManager_Generators_Plugin.logger.LogMessage("useFuelPerTick: " + LDB.items.Select(2204).prefabDesc.useFuelPerTick);

                    if (EnergyManager_Generators_Plugin.modGenMiniFusion != (long)EnergyManager_Generators_Plugin.myCfg["-| 1 Easy Mode |4| Mini Fusion Power Generator", "Max Power (kW)"].DefaultValue || EnergyManager_Generators_Plugin.EditExisting)
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### MiniFusion not default --- Attempting Edit...");
                        try
                        {
                            LDB.items.Select(2211).prefabDesc.genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.modGenMiniFusion * kwMultiplier));
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### MiniFusion Edited successfully!");
                            EnergyManager_Generators_Plugin.GenModDict.Add(2211, EnergyManager_Generators_Plugin.modGenMiniFusion);
                        }
                        catch (Exception)
                        {
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### Error when attempting to edit MiniFusion. Skipping MiniFusion...");
                        }
                    }
                    else
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### Minifusion is default --- No Edit needed!");
                    }

                    if (EnergyManager_Generators_Plugin.modGenArtStar != (long)EnergyManager_Generators_Plugin.myCfg["-| 1 Easy Mode |5| Artificial Star", "Max Power (kW)"].DefaultValue || EnergyManager_Generators_Plugin.EditExisting)
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### ArtificialStar not default --- Attempting Edit...");
                        try
                        {
                            LDB.items.Select(2210).prefabDesc.genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.modGenArtStar * kwMultiplier));
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### ArtificialStar Edited successfully!");
                            EnergyManager_Generators_Plugin.GenModDict.Add(2210, EnergyManager_Generators_Plugin.modGenArtStar);
                        }
                        catch (Exception)
                        {
                            EnergyManager_Generators_Plugin.logger.LogMessage("###### Error when attempting to edit ArtificialStar. Skipping ArtificialStar...");
                        }
                    }
                    else
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("###### ArtificialStar is default --- No Edit needed!");
                    }
                    //EnergyManager_Generators_Plugin.logger.LogMessage("useFuelPerTick: " + LDB.items.Select(2211).prefabDesc.useFuelPerTick);
                    #endregion

                    string count = (EnergyManager_Generators_Plugin.GenModDict != null && EnergyManager_Generators_Plugin.GenModDict.Count > 0) ? EnergyManager_Generators_Plugin.GenModDict.Count.ToString() : "0";
                    EnergyManager_Generators_Plugin.logger.LogMessage($"============ Easy Mode End --- Edited {count} Item/s ============");
                }
                #endregion

                //ADVANCED MODE
                #region AdvancedModeHandle
                else
                {
                    EnergyManager_Generators_Plugin.logger.LogMessage("============ Advanced Mode Begin ============");
                    foreach (string item in EnergyManager_Generators_Plugin.generatorValues.Split('|'))
                    {
                        EnergyManager_Generators_Plugin.logger.LogMessage("Trying to make edit: " + item);
                        try
                        {
                            string[] setting = item.Split(':');
                            if (setting.Length != 2)
                            {
                                EnergyManager_Generators_Plugin.logger.LogMessage("Cannot make Edit: " + item + " --- Too much or too less parameters for specific setting! --- Skipping...");
                                continue;
                            }

                            bool intIDSuccess = Int32.TryParse(setting[0], out int idToEdit);
                            bool longValueSuccess = Int64.TryParse(setting[1], out long valueToEdit);

                            if (intIDSuccess && longValueSuccess)
                            {
                                PrefabDesc desc = LDB.items.Select(idToEdit).prefabDesc;

                                if (desc.isPowerGen)
                                {
                                    
                                    desc.genEnergyPerTick = Convert.ToInt64(Math.Round(valueToEdit * kwMultiplier));
                                    EnergyManager_Generators_Plugin.logger.LogMessage("Successfully Edited: " + item + " --- Continuing");
                                    EnergyManager_Generators_Plugin.GenModDict.Add(idToEdit, valueToEdit);
                                }
                                else
                                {
                                    EnergyManager_Generators_Plugin.logger.LogMessage("Cannot make Edit: " + item + " --- Item \"" + idToEdit + "\" is no Power-Generator --- Skipping...");
                                    continue;
                                }
                            }
                            else
                            {
                                EnergyManager_Generators_Plugin.logger.LogMessage("Cannot make Edit: " + item + " --- ID-Parse Success: " + intIDSuccess + " | Value-Parse Success: " + longValueSuccess + " --- Skipping...");
                                continue;
                            }

                        }
                        catch (Exception)
                        {
                            EnergyManager_Generators_Plugin.logger.LogMessage("Cannot make Edit: " + item + " --- Maybe the ID is not existing? --- Skipping...");
                            continue;
                        }
                    }
                    string count = (EnergyManager_Generators_Plugin.GenModDict != null && EnergyManager_Generators_Plugin.GenModDict.Count > 0) ? EnergyManager_Generators_Plugin.GenModDict.Count.ToString() : "0";
                    EnergyManager_Generators_Plugin.logger.LogMessage($"============ Advanced Mode End --- Edited {count} Item/s ============");
                }
                #endregion

                alreadyInitialized = true;
            }

        }

        // Maybe make this optional debug setting to debug modded items for the user to find out the id easier?
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(PowerSystem), "NewGeneratorComponent")]
        //public static bool NewGeneratorComponentPrefix(PowerSystem __instance, ref int entityId, ref PrefabDesc desc)
        //{
        //    //EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\nNAME: " + desc.prefab.name + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\nNAME: " + LDB.items.Select(__instance.factory.entityPool[entityId].protoId).Name.Translate(Language.enUS) + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\nBUILT " + __instance.factory.entityPool[entityId].protoId + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isPowerGen " + desc.isPowerGen + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n windForcedPower " + desc.windForcedPower + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isAccumulator " + desc.isAccumulator + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isPowerExchanger " + desc.isPowerExchanger + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isPowerConsumer " + desc.isPowerConsumer + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isCollectStation " + desc.isCollectStation + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n photovoltaic " + desc.photovoltaic + "\n----------------------------------------");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n gammaRayReceiver " + desc.gammaRayReceiver + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isMonster " + desc.isMonster + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n fuelMask " + desc.fuelMask + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n isSilo " + desc.isSilo + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n Art star? " + LDB.items.Select(__instance.factory.entityPool[entityId].protoId).Name.Translate(Language.enUS).ToLower().Contains("artificial star") + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n genEnergyPerTick " + desc.genEnergyPerTick + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n outputEnergyPerTick " + desc.outputEnergyPerTick + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n inputEnergyPerTick " + desc.inputEnergyPerTick + "\n----------------------------------------\n");
        //    EnergyManager_Generators_Plugin.logger.LogMessage("----------------------------------------\n maxAcuEnergy " + desc.maxAcuEnergy + "\n----------------------------------------\n");
        //    return true;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GameMain), "End")]
        //public static bool GameMainEndPrefix(GameMain __instance)
        //{
        //    //Reset EditExisting to false if user exits and already had loaded at least one game
        //    if (EnergyManager_Generators_Plugin.loadedOnce && EnergyManager_Generators_Plugin.EditExisting)
        //    {
        //        EnergyManager_Generators_Plugin.logger.LogMessage("Disabling \"Edit Existing\" Setting to false...");
        //        EnergyManager_Generators_Plugin.myCfg["-| 3 Edit Existing", "Edit existing Buildings on Load"].BoxedValue = (bool)false;
        //        EnergyManager_Generators_Plugin.myCfg.Save();
        //        EnergyManager_Generators_Plugin.myCfg.Reload();
        //    }
        //    return true;
        //    //
        //    //Debug.Log("%<><><><><><><><><><><><> LOOOOOOOOOOOOOOOOOOOOOOOOOOOOOADED!!!!!! <><><><><><><><><><><><>%");
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIRoot), "OnGameMainObjectCreated")]
        public static void OnGameMainObjectCreatedPostfix(UIRoot __instance)
        {
            if (EnergyManager_Generators_Plugin.EditExisting)
            {
                EnergyManager_Generators_Plugin.logger.LogMessage("Resetting \"Edit Existing\" Setting to FALSE");
                EnergyManager_Generators_Plugin.EditExisting = false;
                EnergyManager_Generators_Plugin.myCfg["-| 3 Edit Existing", "Edit existing Buildings on Load"].BoxedValue = (bool)false;
                EnergyManager_Generators_Plugin.myCfg.Save();
                EnergyManager_Generators_Plugin.myCfg.Reload();

            }
            //if (!EnergyManager_Generators_Plugin.loadedOnce)
            //{
            //    EnergyManager_Generators_Plugin.loadedOnce = true;
            //}
            //
            //Debug.Log("%<><><><><><><><><><><><> LOOOOOOOOOOOOOOOOOOOOOOOOOOOOOADED!!!!!! <><><><><><><><><><><><>%");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PowerSystem), "Import")]
        public static void ImportPostfix(PowerSystem __instance, ref BinaryReader r)
        {
            //Debug.Log("Planet --------- " + __instance.planet.displayName);
            if (__instance == null)
            {
                return;
            }
            if (__instance.genPool != null && 
                __instance.genPool.Length > 0 && 
                EnergyManager_Generators_Plugin.GenModDict != null && 
                EnergyManager_Generators_Plugin.GenModDict.Count > 0 && 
                EnergyManager_Generators_Plugin.EditExisting
                )
            {
                EnergyManager_Generators_Plugin.logger.LogMessage("Editing Existing Generator Pool in Powersystem of Planet \"" + __instance.planet.displayName + "\"");
                int tmpCount = 0;
                for (int i = 0; i < __instance.genPool.Length; i++)
                {
                    int ItemID = __instance.factory.entityPool[__instance.genPool[i].entityId].protoId;
                    if (EnergyManager_Generators_Plugin.GenModDict.ContainsKey(ItemID))
                    {
                        __instance.genPool[i].genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.GenModDict[ItemID] * kwMultiplier));
                        tmpCount++;
                    }
                }
                EnergyManager_Generators_Plugin.logger.LogMessage("Edited " + tmpCount + " Generator/s on Planet \"" + __instance.planet.displayName + "\"");
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PowerSystem), "Import")]
        //public static void ImportPostfix(PowerSystem __instance, ref BinaryReader r)
        //{
        //    if (__instance == null)
        //    {
        //        return;
        //    }
        //    if (__instance.genPool != null && __instance.genPool.Length > 0)
        //    {
        //        for (int i = 0; i < __instance.genPool.Length; i++)
        //        {
        //            if (__instance.factory.entityPool[__instance.genPool[i].entityId].protoId == (short)2205)
        //            {
        //                //__instance.factory.entityPool[__instance.excPool[1].entityId].protoId
        //                __instance.genPool[i].genEnergyPerTick = Convert.ToInt64(Math.Round(EnergyManager_Generators_Plugin.modGenSolar * kwMultiplier);
        //            }
        //        }
        //    }
        //    for (int i = 0; i < __instance.accPool.Length; i++)
        //    {
        //        //__instance.accPool[i].
        //    }
        //}

    }
}
