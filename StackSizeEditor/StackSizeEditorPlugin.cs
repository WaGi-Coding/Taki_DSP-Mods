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
using xiaoye97;
using BepInEx.Logging;
using System.Reflection.Emit;
using System.Linq;
using BepInEx.Configuration;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace StackSizeEditor
{
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool")]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
    public class StackSizeEditorPlugin : BaseUnityPlugin
    {
        public static ManualLogSource logger;

        public const string ModGuid = "com.Taki7o7.StackSizeEditor";
        public const string ModName = "StackSizeEditor";
        public const string ModVer = "1.0.0";


        public static int generalMultiplier { get; set; } = 1;
        public static string stackSizeValues { get; set; } = string.Empty;
        public static Dictionary<int, int> StackModDict = new Dictionary<int, int>();
        public static Dictionary<int, int> ActualStackModDict = new Dictionary<int, int>();


        public void Awake()
        {
            logger = base.Logger;
            generalMultiplier = Config.Bind<int>("-| 1 Stack Size Edit", "|1| Stack-Size Multiplier", 1, "Here you can specify a general Stack Size Multiplier.\nAny Custom Edit Entries have priority!").Value;
            stackSizeValues = Config.Bind<string>("-| 1 Stack Size Edit", "|2| Stack-Size Custom Entries", string.Empty, "Here you can specify the Stack-Size of ItemIDs\nYou can find ItemIDs when hovering over Items, right after their Name.\n\nFormat: {ItemID}:{StackSize}\nSeperate multiple entries by |\nExample: 2210:100|2203:100\nThis would set Artificial Stars and Wind Turbines StackSize to 100\nThis will ignore the general Multiplier!").Value;




            //Parsing
            #region Parsing
            List<string> entries = StackSizeEditorPlugin.stackSizeValues.Split('|').ToList();
            if (string.IsNullOrEmpty(StackSizeEditorPlugin.stackSizeValues) || entries.Count < 1)
            {
                logger.LogMessage("Stack Size Configuration is empty! Nothing to Parse! --- Exiting...");
                return;
            }
            logger.LogMessage("============ Begin Parsing StackSizes ============");
            foreach (string entry in entries)
            {
                logger.LogMessage("Trying to make edit: " + entry);
                try
                {
                    string[] setting = entry.Split(':');
                    if (setting.Length != 2)
                    {
                        logger.LogMessage($"Cannot parse entry: {entry} --- Too {(setting.Length < 2 ? "less" : "much")} parameters for specific setting! --- Skipping...");
                        continue;
                    }

                    bool intIDSuccess = Int32.TryParse(setting[0], out int idToEdit);
                    bool longValueSuccess = Int32.TryParse(setting[1], out int valueToEdit);

                    if (intIDSuccess && longValueSuccess)
                    {
                        if (!StackModDict.ContainsKey(idToEdit))
                        {
                            logger.LogMessage($"Parsed entry: {entry} --- Continue!");
                            StackModDict.Add(idToEdit, valueToEdit);
                            continue;
                        }
                        else
                        {
                            logger.LogMessage($"Cannot parse entry: {entry} --- ID {idToEdit} existing in the List! --- Continue!");
                            continue;
                        }
                    }
                    else
                    {
                        logger.LogMessage("Cannot parse entry: " + entry + " --- ID-Parse Success: " + intIDSuccess + " | Value-Parse Success: " + longValueSuccess + " --- Skipping...");
                        continue;
                    }

                }
                catch (Exception)
                {
                    StackSizeEditorPlugin.logger.LogMessage("Cannot parse entry: " + entry + " --- Skipping...");
                    continue;
                }
            }
            string count = (StackSizeEditorPlugin.StackModDict != null && StackSizeEditorPlugin.StackModDict.Count > 0) ? StackSizeEditorPlugin.StackModDict.Count.ToString() : "0";
            StackSizeEditorPlugin.logger.LogMessage($"============ Parsing END --- Edited {count} Item/s ============");
            #endregion




            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(StackSizeEditorPatch));
        }
    }

    [HarmonyPatch]
    public class StackSizeEditorPatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StorageComponent), "LoadStatic")]
        public static bool MyLoadStatic(StorageComponent __instance)
        {
            bool flag = !StorageComponent.staticLoaded;
            if (flag)
            {
                StorageComponent.itemIsFuel = new bool[12000];
                StorageComponent.itemStackCount = new int[12000];
                for (int i = 0; i < 12000; i++)
                {
                    StorageComponent.itemStackCount[i] = 1000;
                }
                ItemProto[] dataArray = LDB.items.dataArray;
                for (int j = 0; j < dataArray.Length; j++)
                {
                    StorageComponent.itemIsFuel[dataArray[j].ID] = (dataArray[j].HeatValue > 0L);
                    if (!StackSizeEditorPlugin.StackModDict.ContainsKey(dataArray[j].ID))
                    {
                        StorageComponent.itemStackCount[dataArray[j].ID] = dataArray[j].StackSize * StackSizeEditorPlugin.generalMultiplier;
                    }
                    else
                    {
                        StorageComponent.itemStackCount[dataArray[j].ID] = StackSizeEditorPlugin.StackModDict[dataArray[j].ID];
                    }
                }
                StorageComponent.staticLoaded = true;
            }
            return false;
        }


        static bool alreadyInitialized = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LDBTool), "VFPreloadPostPatch")]
        public static void LDBVFPreloadPostPatchPostfix()
        {
            if (!alreadyInitialized) // Don't do when loading back into main menu
            {
                //StackSizeEditorPlugin.StackModDict = new Dictionary<int, int>();

                foreach (var item in StackSizeEditorPlugin.StackModDict)
                {
                    if (LDB.items.Select(item.Key) == null)
                    {
                        StackSizeEditorPlugin.logger.LogMessage($"Cannot find ID \"{item.Key}\" --- Skipping...");
                        continue;
                    }
                    else
                    {
                        if (LDB.items.Select(item.Key).StackSize != item.Value)
                        {
                            StackSizeEditorPlugin.logger.LogMessage($"Edited Stacksize of \"{LDB.items.Select(item.Key).Name.Translate()}\" ({item.Key}) to {item.Value} --- Continue!");
                            StackSizeEditorPlugin.ActualStackModDict.Add(item.Key, item.Value);
                            continue;
                        }
                        else
                        {
                            StackSizeEditorPlugin.logger.LogMessage($"Original Stack Size of \"{LDB.items.Select(item.Key).Name.Translate()}\" ({item.Key}) already is {item.Value} --- Skipping...");
                            continue;
                        }
                    }
                }
                

                alreadyInitialized = true;
            }

        }
    }
}
