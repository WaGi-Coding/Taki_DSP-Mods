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
using NGPT;
using BepInEx.Configuration;
using BepInEx.Logging;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace FreeFoundationsFreeSoil
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
	//[BepInDependency("org.bepinex.plugins.buildanywhere", BepInDependency.DependencyFlags.SoftDependency)] // Delete again once he fixed reform size bug on his side!
	//[BepInIncompatibility("org.bepinex.plugins.buildanywhere")]
	public class FreeFoundationsFreeSoilPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Taki7o7.FreeFoundationsFreeSoil";
        public const string ModName = "FreeFoundationsFreeSoil";
        public const string ModVer = "1.2.3";

		public static bool freeFoundationEnabled { get; private set; } = true;
		public static bool freeSoilEnabled { get; private set; } = true;
		public static bool collectSoilEnabled { get; private set; } = true;

		public static int buildrange { get; private set; } = 160;
		public static bool buildrangeEnabled { get; private set; } = true;

		public static ManualLogSource logger;
		//public static bool BuildAnyWhereWarningShowed { get; set; } = false;


		public void Awake()
		{
			logger = base.Logger;
          
			//Debug.Log("Awake");
			//Config.TryGetEntry<double>(new BepInEx.Configuration.ConfigDefinition("Mod-Settings", "Energy use multiplier"), out BepInEx.Configuration.ConfigEntry<double> entry);
			//if (entry != null && entry.Value < 0.1)
			//{
			//    //Config.
			//    Debug.Log("Multiplier too small, forcing 0.1");
			//    entry.Value = 0.1;
			//    Config.Save();
			//}
			//var x = Config["Mod-Settings", "Enable Higher Buildrange"];
			freeFoundationEnabled = Config.Bind<bool>("Free Foundations", "Enable Free Foundations", true, "If enabled, you do not need Foundations to build them.").Value;
            freeSoilEnabled = Config.Bind<bool>("Free Soil", "Enable Free Soil", true, "If enabled, you do not need Soil to build Foundations.").Value;
			collectSoilEnabled = Config.Bind<bool>("Free Soil", "Enable Soil Collect", true, "If enabled, you will collect soil as usual. If disabled, it will not collect Soil.").Value;
            buildrangeEnabled = Config.Bind<bool>("Build Range", "Enable Higher Buildrange", true, "Enables/Disables the functionality to gain higher Build-Range").Value;
            buildrange = Config.Bind<int>("Build Range", "Build Range", 250, new ConfigDescription("Vanilla value is 80. Mod max & default is 250.", new AcceptableValueRange<int>(80, 250))).Value;
            ///////ConfigEntry<int> setting = Config.AddSetting("Section", "Key", 1, new ConfigDescription("Description", new AcceptableValueRange<int>(0, 100)));
            ///

    //        foreach (var item in Config.Keys)
    //        {
				//Debug.Log(item.Section + " --- " + item.Key);
    //        }

			var harmony = new Harmony(ModGuid);


			harmony.PatchAll(typeof(FreeFoundationsFreeSoilPatch));

			harmony.PatchAll(typeof(BuildingRangePatchImport));
			harmony.PatchAll(typeof(BuildingRangePatchExport));
            
		}

	}

	[HarmonyPatch]
	public class BuildingRangePatchImport
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlayerAction_Build), "Init")]
		public static void SizePatch(PlayerAction_Build __instance)
		{
			__instance.reformIndices = new int[900];
			__instance.reformPoints = new Vector3[900];
            
			//if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("org.bepinex.plugins.buildanywhere"))
   //         {
			//	FreeFoundationsFreeSoilPlugin.logger.LogWarning("You have installed \"BuildAnywhere\" Mod by Alejandro which unfortunately locks the Reform-Size to 10x10");
			//	//FreeFoundationsFreeSoilPlugin.BuildAnyWhereWarningShowed = true;

			//}
		}

		//static double customMultiplier = MechaEnergyUseMultiplierPlugin.multiplier;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Mecha), "Import")]
		public static void Postfix(Mecha __instance)
		{
            if (FreeFoundationsFreeSoilPlugin.buildrangeEnabled)
            {
				__instance.buildArea = (float)FreeFoundationsFreeSoilPlugin.buildrange;
            }
            else
            {
				__instance.buildArea = 80f;
            }
		}
	}
	public class BuildingRangePatchExport
	{
		//static double customMultiplier = MechaEnergyUseMultiplierPlugin.multiplier;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Mecha), "Export")]
		public static void Prefix(Mecha __instance)
		{
			__instance.buildArea = 80f;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Mecha), "Export")]
		public static void Postfix(Mecha __instance)
		{
			if (FreeFoundationsFreeSoilPlugin.buildrangeEnabled)
			{
				__instance.buildArea = (float)FreeFoundationsFreeSoilPlugin.buildrange;
			}
			else
			{
				__instance.buildArea = 80f;
			}
		}
	}

	[HarmonyPatch]
    public class FreeFoundationsFreeSoilPatch
	{
		[HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAction_Build), "DetermineBuildPreviews")]
        public static bool DetermineBuildPreviewsPrefix(ref PlayerAction_Build __instance)
        {

			CommandState cmd = __instance.controller.cmd;
			//__instance.waitConfirm = false;
			bool flag = false;
			bool flag2 = false;
			if (__instance.handPrefabDesc == null)
			{
				if (cmd.mode == 4)
				{
					int[] consumeRegister = GameMain.statistics.production.factoryStatPool[__instance.factory.index].consumeRegister;
					if (__instance.reformSize < 1)
					{
						__instance.reformSize = 1;
					}
					else if (__instance.reformSize > 30)
					{
						__instance.reformSize = 30;
					}
					if ((__instance.reformCenterPoint - __instance.player.position).sqrMagnitude > __instance.player.mecha.buildArea * __instance.player.mecha.buildArea)
					{
						if (!VFInput.onGUI)
						{
							__instance.cursorText = "目标超出范围".Translate();
							__instance.cursorWarning = true;
							UICursor.SetCursor(ECursor.Ban);
						}
					}
					else
					{
						if (!VFInput.onGUI)
						{
							UICursor.SetCursor(ECursor.Reform);
						}
						bool flag24 = false;
						if (VFInput._reformPlusKey.onDown)
						{
							if (__instance.reformSize < 30)
							{
								__instance.reformSize++;
								flag24 = true;
								for (int num61 = 0; num61 < __instance.reformSize * __instance.reformSize; num61++)
								{
									__instance.reformIndices[num61] = -1;
								}
							}
						}
						else if (VFInput._reformMinusKey.onDown && __instance.reformSize > 1)
						{
							__instance.reformSize--;
							flag24 = true;
						}
						float radius = 0.99094594f * (float)__instance.reformSize;
						int num62 = __instance.factory.ComputeFlattenTerrainReform(__instance.reformPoints, __instance.reformCenterPoint, radius, __instance.reformPointsCount, 3f, 1f);
						if (__instance.cursorValid && !VFInput.onGUI)
						{
							if (num62 > 0)
							{
								string soilCount = num62.ToString();

								if (FreeFoundationsFreeSoilPlugin.freeSoilEnabled)
								{
									soilCount = "0";
								}
								//Debug.Log("IF");
								__instance.cursorText = string.Concat(new string[]
								{
								"沙土消耗".Translate(),
								" ",
								soilCount,
								" ",
								"个沙土".Translate(),
								"\n",
								"改造大小".Translate(),
								__instance.reformSize.ToString(),
								"x",
								__instance.reformSize.ToString(),
								(FreeFoundationsFreeSoilPlugin.freeSoilEnabled) ?       "\n       (Free Soil is Enabled)" : string.Empty,
								(FreeFoundationsFreeSoilPlugin.freeFoundationEnabled) ? "\n(Free Foundations is Enabled)" : string.Empty
								});
							}
							else if (num62 == 0)
							{
								__instance.cursorText = "改造大小".Translate() + __instance.reformSize.ToString() + "x" + __instance.reformSize.ToString() + 
									((FreeFoundationsFreeSoilPlugin.freeSoilEnabled) ?       "\n       (Free Soil is Enabled)" : string.Empty) +
									((FreeFoundationsFreeSoilPlugin.freeFoundationEnabled) ? "\n(Free Foundations is Enabled)" : string.Empty);
							}
							else
							{
								int num63 = -num62;



								string soilCount = num63.ToString();

								if (!FreeFoundationsFreeSoilPlugin.collectSoilEnabled)
								{
									soilCount = "0";
								}
								//Debug.Log("ELSE");



								__instance.cursorText = string.Concat(new string[]
								{
								"沙土获得".Translate(),
								" ",
								soilCount,
								" ",
								"个沙土".Translate(),
								"\n",
								"改造大小".Translate(),
								__instance.reformSize.ToString(),
								"x",
								__instance.reformSize.ToString(),
								(FreeFoundationsFreeSoilPlugin.freeSoilEnabled) ?       "\n       (Free Soil is Enabled)" : string.Empty,
								(FreeFoundationsFreeSoilPlugin.freeFoundationEnabled) ? "\n(Free Foundations is Enabled)" : string.Empty
								});
							}
							if (VFInput._buildConfirm.pressing)
							{
								bool flag25 = false;
								if (VFInput._buildConfirm.onDown)
								{
									flag25 = true;
									__instance.reformMouseOnDown = true;
								}
								if (__instance.reformMouseOnDown)
								{
									__instance.inReformOperation = true;
									if (__instance.reformChangedPoint.x != __instance.reformCenterPoint.x || __instance.reformChangedPoint.y != __instance.reformCenterPoint.y || __instance.reformChangedPoint.z != __instance.reformCenterPoint.z || flag24)
									{
										bool doItFoundation = true;
										if (FreeFoundationsFreeSoilPlugin.freeFoundationEnabled)
										{
											doItFoundation = __instance.handItem.BuildMode == 4;
										}
										else
										{
											doItFoundation = __instance.handItem.BuildMode == 4 && __instance.player.package.GetItemCount(__instance.handItem.ID) + __instance.player.inhandItemCount >= __instance.reformPointsCount;

										}
										if (doItFoundation)
										{
											bool doItSoil = false;

											if (FreeFoundationsFreeSoilPlugin.freeSoilEnabled || !FreeFoundationsFreeSoilPlugin.collectSoilEnabled)
											{
												doItSoil = __instance.player.sandCount >= 0;
											}
											else
											{
												doItSoil = __instance.player.sandCount - num62 >= 0;
											}
											//num64 = 100000;
											if (doItSoil)
											{
												//Debug.Log(num62);
												__instance.factory.FlattenTerrainReform(__instance.reformCenterPoint, radius, __instance.reformSize, __instance.veinBuried, 3f);
												VFAudio.Create("reform-terrain", null, __instance.reformCenterPoint, true);

												if (num62 < 0)
												{
													if (FreeFoundationsFreeSoilPlugin.collectSoilEnabled)
													{
														__instance.player.SetSandCount(__instance.player.sandCount - num62);
													}
													else
													{
														__instance.player.SetSandCount(__instance.player.sandCount);
													}
												}
												else
												{
													if (FreeFoundationsFreeSoilPlugin.freeSoilEnabled)
													{
														__instance.player.SetSandCount(__instance.player.sandCount);
													}
													else
													{
														__instance.player.SetSandCount(__instance.player.sandCount - num62);
													}
												}

												int num65 = __instance.reformSize * __instance.reformSize;
												for (int num66 = 0; num66 < num65; num66++)
												{
													int num67 = __instance.reformIndices[num66];
													PlatformSystem platformSystem = __instance.factory.platformSystem;
													if (num67 >= 0)
													{
														int num68 = platformSystem.GetReformType(num67);
														int num69 = platformSystem.GetReformColor(num67);
														if (num68 != __instance.reformType || num69 != __instance.reformColor)
														{
															__instance.factory.platformSystem.SetReformType(num67, __instance.reformType);
															__instance.factory.platformSystem.SetReformColor(num67, __instance.reformColor);
														}
													}
												}
												int num70 = __instance.reformPointsCount;
												if (__instance.player.inhandItemCount > 0)
												{
													int num71 = (__instance.reformPointsCount >= __instance.player.inhandItemCount) ? __instance.player.inhandItemCount : __instance.reformPointsCount;
													__instance.player.UseHandItems(num71);
													num70 -= num71;
												}
												int id = __instance.handItem.ID;
												consumeRegister[id] += __instance.reformPointsCount;
												__instance.player.package.TakeTailItems(ref id, ref num70, false);
												GameMain.gameScenario.NotifyOnBuild(__instance.player.planetId, __instance.handItem.ID, 0);
											}
											else if (flag25)
											{
												//Debug.Log("No1");
												UIRealtimeTip.Popup("沙土不足".Translate(), true, 0);
											}
										}
										else if (flag25)
										{
											//Debug.Log("No2");
											UIRealtimeTip.Popup("物品不足".Translate(), true, 1);
										}
									}
								}
								else
								{
									__instance.inReformOperation = false;
								}
								__instance.reformChangedPoint = __instance.reformCenterPoint;
							}
							else
							{
								__instance.inReformOperation = false;
								__instance.reformChangedPoint = Vector3.zero;
								__instance.reformMouseOnDown = false;
							}
						}
					}
				}
                else
                {
					// Just do original code instead
					return true;
                }
				__instance.ClearBuildPreviews();
			}
			else
			{
				return true;
			}
			if (!flag)
			{
				UIRoot.instance.uiGame.CloseInserterBuildTip();
			}
			if (!flag2)
			{
				UIRoot.instance.uiGame.beltBuildTip.SetOutputEntity(0, -1);
				UIRoot.instance.uiGame.CloseBeltBuildTip();
			}


			// Don't execute original put original above instead?
			return false;
        }
    }
}
