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
using System.Collections.Generic;
using System.Linq;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RemoveEarlyAccessAndSteamName
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
    public class RemoveEarlyAccessAndSteamName_Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Taki7o7.RemoveEarlyAccessAndSteamName";
        public const string ModName = "RemoveEarlyAccessAndSteamName";
        public const string ModVer = "1.0.0";

        public void Awake()
        {
            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(RemoveEarlyAccessAndSteamName_Patch));
        }
    }

    [HarmonyPatch]
    public class RemoveEarlyAccessAndSteamName_Patch
    {
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIVersionText), "OnLanguageChange")]
        //public static bool OnLanguageChangePrefix(UIVersionText __instance, Language language)
        //{
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIVersionText), "Update")]
        //public static bool UpdatePrefix(UIVersionText __instance)
        //{
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIVersionText), "OnEnable")]
        //public static bool OnEnablePrefix(UIVersionText __instance)
        //{
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UIVersionText), "Start")]
        //public static bool StartPrefix(UIVersionText __instance)
        //{
        //    return false;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIVersionText), "Refresh")]
        public static bool RefreshPrefix(UIVersionText __instance)
        {
            if (__instance.textComp != null)
            {
                if (GameMain.isRunning && !GameMain.instance.isMenuDemo)
                {
                    __instance.textComp.text = string.Empty;
                }
                else
                {
                    __instance.textComp.text = string.Empty;
                }
            }

            return false;
        }

    }
}
