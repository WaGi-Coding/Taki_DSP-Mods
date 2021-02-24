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
using BepInEx.Logging;
using System.Linq;
using System.Text;
using UnityEngine.EventSystems;
using BepInEx.Configuration;
using xiaoye97;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace GigaStations
{
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInProcess("DSPGAME.exe")]
    public class GigaStationsPlugin : BaseUnityPlugin
    {

        public const string ModGuid = "com.Taki7o7.GigaStations_v2";
        public const string ModName = "GigaStations_v2";
        public const string ModVer = "2.0.4";



        public static ManualLogSource logger;

        //ILS
        public static int ilsMaxStorage { get; set; } = 30000; //Vanilla 10000
        public static int ilsMaxWarps { get; set; } = 150; //Vanilla 50
        public static int ilsMaxVessels { get; set; } = 30; //Vanilla 10 (limit from 10-30)
        public static int ilsMaxDrones { get; set; } = 150; //Vanilla 50
        public static long ilsMaxAcuGJ { get; set; } = 50; //Vanilla 12 GJ = * 1 000 000 000
        public static int ilsMaxSlots { get; set; } = 12; //Vanilla 5 (limited to from 5-12)

        //PLS
        public static int plsMaxStorage { get; set; } = 15000; //Vanilla 5000
        public static int plsMaxDrones { get; set; } = 150; //Vanilla 50 (limit from 50-150)
        public static long plsMaxAcuMJ { get; set; } = 500; //Vanilla 180 MJ = * 1 000 000
        public static int plsMaxSlots { get; set; } = 12; //Vanilla 3 (limited to from 3-12)

        //Collector
        public static int colMaxStorage { get; set; } = 15000; //Vanilla 5000 
        public static int colSpeedMultiplier { get; set; } = 3; //Vanilla 1 (==8)


        //VesselCapacity
        public static int vesselCapacity { get; set; } = 3000; //Vanilla depends on upgrade-lvl, but max is 1000 (limit to 5000?)

        //DroneCapacity
        public static int droneCapacity { get; set; } = 300; //Vanilla depends on upgrade-lvl, but max is 100 (limit to 500?)



        private Sprite icon_ils;
        private Sprite icon_pls;
        private Sprite icon_collector;

        void Start()
        {
            logger = base.Logger;


            //ILS
            ilsMaxSlots = Config.Bind<int>("-|1|- ILS", "-| 1 Max. Item Slots", 12, new ConfigDescription("The maximum Item Slots the Station can have.\nVanilla: 5", new AcceptableValueRange<int>(5, 12))).Value;
            ilsMaxStorage = Config.Bind<int>("-|1|- ILS", "-| 2 Max. Storage", 30000, "The maximum Storage capacity per Item-Slot.\nVanilla: 10000").Value;
            ilsMaxVessels = Config.Bind<int>("-|1|- ILS", "-| 3 Max. Vessels", 30, new ConfigDescription("The maximum Logistic Vessels amount.\nVanilla: 10", new AcceptableValueRange<int>(10, 30))).Value;
            ilsMaxDrones = Config.Bind<int>("-|1|- ILS", "-| 4 Max. Drones", 150, new ConfigDescription("The maximum Logistic Drones amount.\nVanilla: 50", new AcceptableValueRange<int>(50, 150))).Value;
            ilsMaxAcuGJ = Config.Bind<int>("-|1|- ILS", "-| 5 Max. Accu Capacity (GJ)", 50, "The Stations maximum Accumulator Capacity in GJ.\nVanilla: 12 GJ").Value;
            ilsMaxWarps = Config.Bind<int>("-|1|- ILS", "-| 6 Max. Warps", 150, "The maximum Warp Cells amount.\nVanilla: 50").Value;

            //PLS
            plsMaxSlots = Config.Bind<int>("-|2|- PLS", "-| 1 Max. Item Slots", 12, new ConfigDescription("The maximum Item Slots the Station can have.\nVanilla: 3", new AcceptableValueRange<int>(3, 12))).Value;
            plsMaxStorage = Config.Bind<int>("-|2|- PLS", "-| 2 Max. Storage", 15000, "The maximum Storage capacity per Item-Slot.\nVanilla: 5000").Value;
            plsMaxDrones = Config.Bind<int>("-|2|- PLS", "-| 3 Max. Drones", 150, new ConfigDescription("The maximum Logistic Drones amount.\nVanilla: 50", new AcceptableValueRange<int>(50, 150))).Value;
            plsMaxAcuMJ = Config.Bind<int>("-|2|- PLS", "-| 4 Max. Accu Capacity (GJ)", 500, "The Stations maximum Accumulator Capacity in MJ.\nVanilla: 180 MJ").Value;

            //Collector
            colSpeedMultiplier = Config.Bind<int>("-|3|- Collector", "-| 1 Collect Speed Multiplier", 3, "Multiplier for the Orbital Collectors Collection-Speed.\nVanilla: 1").Value;
            colMaxStorage = Config.Bind<int>("-|3|- Collector", "-| 2 Max. Storage", 15000, "The maximum Storage capacity per Item-Slot.\nVanilla: 5000").Value;


            //VesselCapacity
            vesselCapacity = Config.Bind<int>("-|4|- Vessel", "-| 1 Max. Capacity", 3, "Vessel Capacity Multiplier\n1 == 1000 Vessel Capacity at max Level").Value;

            //DroneCapacity
            droneCapacity = Config.Bind<int>("-|5|- Drone", "-| 1 Max. Capacity", 3, "Drone Capacity Multiplier\n1 == 100 Drone Capacity at max Level").Value;




            var ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("GigaStations.gigastationsicons"));
            icon_ils = ab.LoadAsset<Sprite>("icon_ils");
            icon_pls = ab.LoadAsset<Sprite>("icon_pls");
            icon_collector = ab.LoadAsset<Sprite>("icon_collector");
            LDBTool.PostAddDataAction += AddGigaPLS;
            LDBTool.PostAddDataAction += AddGigaILS;
            LDBTool.PostAddDataAction += AddGigaCollector;


            
        }


        void AddGigaPLS()
        {
            var oriRecipe = LDB.recipes.Select(93); //pls recipe original
            var oriItem = LDB.items.Select(2103);
            var RecipeGiga_PLS = oriRecipe.Copy();
            var Giga_PLS = oriItem.Copy();
            // Recipe
            RecipeGiga_PLS.ID = 410;
            RecipeGiga_PLS.Name = "PGS";
            RecipeGiga_PLS.name = "Planetary Giga Station";
            RecipeGiga_PLS.Description = "Has more Slots, Capacity, etc. than a usual PLS.";
            RecipeGiga_PLS.description = "Has more Slots, Capacity, etc. than a usual PLS.";
            RecipeGiga_PLS.Items = new int[] { 2103, 1103, 1106, 1303, 1206 }; //normal pls, titIngot, processor, partCont
            RecipeGiga_PLS.ItemCounts = new int[] { 1, 40, 40, 40, 20 };
            RecipeGiga_PLS.Results = new int[] { 2110 };
            RecipeGiga_PLS.ResultCounts = new int[] { 1 };
            RecipeGiga_PLS.GridIndex = 2701;
            RecipeGiga_PLS.TimeSpend *= 2;
            RecipeGiga_PLS.preTech = oriRecipe.preTech;
            RecipeGiga_PLS.SID = RecipeGiga_PLS.GridIndex.ToString();
            RecipeGiga_PLS.sid = RecipeGiga_PLS.GridIndex.ToString();
            // Item
            Giga_PLS.ID = 2110;
            Giga_PLS.Name = "PGS";
            Giga_PLS.name = "Planetary Giga Station";
            Giga_PLS.Description = "Has more Slots, Capacity, etc. than a usual PLS.";
            Giga_PLS.description = "Has more Slots, Capacity, etc. than a usual PLS.";
            Giga_PLS.BuildIndex = 410;
            Giga_PLS.GridIndex = RecipeGiga_PLS.GridIndex;
            Giga_PLS.handcraft = RecipeGiga_PLS;
            Giga_PLS.maincraft = RecipeGiga_PLS;
            Giga_PLS.handcrafts = new List<RecipeProto> { RecipeGiga_PLS };
            Giga_PLS.recipes = new List<RecipeProto> { RecipeGiga_PLS };
            Giga_PLS.makes = new List<RecipeProto>();
            Giga_PLS.prefabDesc = oriItem.prefabDesc.Copy();
            
            Giga_PLS.prefabDesc.workEnergyPerTick = 3333334;
            Giga_PLS.prefabDesc.modelIndex = Giga_PLS.ModelIndex;
            Giga_PLS.prefabDesc.stationMaxItemCount = GigaStationsPlugin.plsMaxStorage;
            Giga_PLS.prefabDesc.stationMaxItemKinds = GigaStationsPlugin.plsMaxSlots;
            Giga_PLS.prefabDesc.stationMaxDroneCount = GigaStationsPlugin.plsMaxDrones;
            Giga_PLS.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.plsMaxAcuMJ * 1000000);
            // Set MaxWarpers in station init!!!!!





            Traverse.Create(RecipeGiga_PLS).Field("_iconSprite").SetValue(icon_pls);
            Traverse.Create(Giga_PLS).Field("_iconSprite").SetValue(icon_pls);

            LDBTool.PostAddProto(ProtoType.Recipe, RecipeGiga_PLS);
            LDBTool.PostAddProto(ProtoType.Item, Giga_PLS);

            LDBTool.SetBuildBar(6, 4, 2110);

        }

        void AddGigaILS()
        {
            var oriRecipe = LDB.recipes.Select(95); //ils recipe original
            var oriItem = LDB.items.Select(2104);
            var RecipeGiga_ILS = oriRecipe.Copy();
            var Giga_ILS = oriItem.Copy();
            // Recipe
            RecipeGiga_ILS.ID = 411;
            RecipeGiga_ILS.Name = "IGS";
            RecipeGiga_ILS.name = "Interstellar Giga Station";
            RecipeGiga_ILS.Description = "Has more Slots, Capacity, etc. than a usual ILS.";
            RecipeGiga_ILS.description = "Has more Slots, Capacity, etc. than a usual ILS.";
            RecipeGiga_ILS.Items = new int[] { 2110, 1107, 1206 }; //giga ils, titAlloy, partCont
            RecipeGiga_ILS.ItemCounts = new int[] { 1, 40, 20 };
            RecipeGiga_ILS.Results = new int[] { 2111 };
            RecipeGiga_ILS.ResultCounts = new int[] { 1 };
            RecipeGiga_ILS.GridIndex = 2702;
            RecipeGiga_ILS.TimeSpend *= 2;
            RecipeGiga_ILS.preTech = oriRecipe.preTech;
            RecipeGiga_ILS.SID = RecipeGiga_ILS.GridIndex.ToString();
            RecipeGiga_ILS.sid = RecipeGiga_ILS.GridIndex.ToString();
            // Item
            Giga_ILS.ID = 2111;
            Giga_ILS.Name = "IGS";
            Giga_ILS.name = "Interstellar Giga Station";
            Giga_ILS.Description = "Has more Slots, Capacity, etc. than a usual ILS.";
            Giga_ILS.description = "Has more Slots, Capacity, etc. than a usual ILS.";
            Giga_ILS.BuildIndex = 411;
            Giga_ILS.GridIndex = RecipeGiga_ILS.GridIndex;
            Giga_ILS.handcraft = RecipeGiga_ILS;
            Giga_ILS.maincraft = RecipeGiga_ILS;
            Giga_ILS.handcrafts = new List<RecipeProto> { RecipeGiga_ILS };
            Giga_ILS.recipes = new List<RecipeProto> { RecipeGiga_ILS };
            Giga_ILS.makes = new List<RecipeProto>();
            Giga_ILS.prefabDesc = oriItem.prefabDesc.Copy();
            Giga_ILS.prefabDesc.workEnergyPerTick = 3333334;
            Giga_ILS.prefabDesc.modelIndex = Giga_ILS.ModelIndex;
            Giga_ILS.prefabDesc.stationMaxItemCount = GigaStationsPlugin.ilsMaxStorage;
            Giga_ILS.prefabDesc.stationMaxItemKinds = GigaStationsPlugin.ilsMaxSlots;
            Giga_ILS.prefabDesc.stationMaxDroneCount = GigaStationsPlugin.ilsMaxDrones;
            Giga_ILS.prefabDesc.stationMaxShipCount = GigaStationsPlugin.ilsMaxVessels;
            Giga_ILS.prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.ilsMaxAcuGJ * 1000000000);
            // Set MaxWarpers in station init!!!!!




            Traverse.Create(RecipeGiga_ILS).Field("_iconSprite").SetValue(icon_ils);
            Traverse.Create(Giga_ILS).Field("_iconSprite").SetValue(icon_ils);

            LDBTool.PostAddProto(ProtoType.Recipe, RecipeGiga_ILS);
            LDBTool.PostAddProto(ProtoType.Item, Giga_ILS);

            LDBTool.SetBuildBar(6, 5, 2111);

        }

        void AddGigaCollector()
        {
            var oriRecipe = LDB.recipes.Select(111); //collector recipe original
            var oriItem = LDB.items.Select(2105);
            var RecipeGiga_Collector = oriRecipe.Copy();
            var Giga_Collector = oriItem.Copy();
            // Recipe
            RecipeGiga_Collector.ID = 412;
            RecipeGiga_Collector.Name = "OGC";
            RecipeGiga_Collector.name = "Orbital Giga Collector";
            RecipeGiga_Collector.Description = $"Has more Capacity and collects {GigaStationsPlugin.colSpeedMultiplier}x faster than a usual Collector.";
            RecipeGiga_Collector.description = $"Has more Capacity and collects {GigaStationsPlugin.colSpeedMultiplier}x faster than a usual Collector.";
            RecipeGiga_Collector.Items = new int[] { 2111, 1205, 1406, 2207 }; // giga ils, supMagRing, reinfThrust, AccuFull 
            RecipeGiga_Collector.ItemCounts = new int[] { 1, 50, 20, 20 };
            RecipeGiga_Collector.Results = new int[] { 2112 };
            RecipeGiga_Collector.ResultCounts = new int[] { 1 };
            RecipeGiga_Collector.GridIndex = 2703;
            RecipeGiga_Collector.TimeSpend *= 2;
            RecipeGiga_Collector.preTech = oriRecipe.preTech;
            RecipeGiga_Collector.SID = RecipeGiga_Collector.GridIndex.ToString();
            RecipeGiga_Collector.sid = RecipeGiga_Collector.GridIndex.ToString();
            // Item
            Giga_Collector.ID = 2112;
            Giga_Collector.Name = "OGC";
            Giga_Collector.name = "Orbital Giga Collector";
            Giga_Collector.Description = $"Has more Capacity and collects {GigaStationsPlugin.colSpeedMultiplier}x faster than a usual Collector.";
            Giga_Collector.description = $"Has more Capacity and collects {GigaStationsPlugin.colSpeedMultiplier}x faster than a usual Collector.";
            Giga_Collector.BuildIndex = 412;
            Giga_Collector.GridIndex = RecipeGiga_Collector.GridIndex;
            Giga_Collector.handcraft = RecipeGiga_Collector;
            Giga_Collector.maincraft = RecipeGiga_Collector;
            Giga_Collector.handcrafts = new List<RecipeProto> { RecipeGiga_Collector };
            Giga_Collector.recipes = new List<RecipeProto> { RecipeGiga_Collector };
            Giga_Collector.makes = new List<RecipeProto>();
            Giga_Collector.prefabDesc = oriItem.prefabDesc.Copy();
            Giga_Collector.prefabDesc.modelIndex = Giga_Collector.ModelIndex;
            Giga_Collector.prefabDesc.stationMaxItemCount = GigaStationsPlugin.ilsMaxStorage;
            Giga_Collector.prefabDesc.stationCollectSpeed = oriItem.prefabDesc.stationCollectSpeed * GigaStationsPlugin.colSpeedMultiplier;
            // Set MaxWarpers in station init!!!!!




            Traverse.Create(RecipeGiga_Collector).Field("_iconSprite").SetValue(icon_collector);
            Traverse.Create(Giga_Collector).Field("_iconSprite").SetValue(icon_collector);

            LDBTool.PostAddProto(ProtoType.Recipe, RecipeGiga_Collector);
            LDBTool.PostAddProto(ProtoType.Item, Giga_Collector);

            LDBTool.SetBuildBar(6, 6, 2112);

        }

        public void Awake()
        {
            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(GigaStationsPlugin));
            harmony.PatchAll(typeof(StationEditPatch));
        }
    }


    [HarmonyPatch]
    public class StationEditPatch
    {
        static bool alreadyInitialized = false;


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIRoot), "OnGameMainObjectCreated")]
        public static void OnGameMainObjectCreatedPostfix(UIRoot __instance)
        {
            //GameMain.history.logisticDroneCarries = GigaStationsPlugin.vesselCapacity;
            //GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity;
            //GigaStationsPlugin.logger.LogInfo($"########## Logistic Carry Tech Level 0 (Tech-ID 3500) unlocked?: {(GameMain.history.TechUnlocked(3500) ? "YES" : "NO")}");

            if (GameMain.history.TechUnlocked(3508))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 8\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 8\nSetting Vessels Capacity: 1000 * {GigaStationsPlugin.vesselCapacity} = {1000 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 100 * {GigaStationsPlugin.droneCapacity} = {100 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 1000;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 100;
            }
            else if (GameMain.history.TechUnlocked(3507))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 7\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 7\nSetting Vessels Capacity: 800 * {GigaStationsPlugin.vesselCapacity} = {800 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 80 * {GigaStationsPlugin.droneCapacity} = {80 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 800;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 80;
            }
            else if (GameMain.history.TechUnlocked(3506))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 6\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 6\nSetting Vessels Capacity: 600 * {GigaStationsPlugin.vesselCapacity} = {600 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 70 * {GigaStationsPlugin.droneCapacity} = {70 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 600;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 70;
            }
            else if (GameMain.history.TechUnlocked(3505))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 5\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 5\nSetting Vessels Capacity: 500 * {GigaStationsPlugin.vesselCapacity} = {500 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 60 * {GigaStationsPlugin.droneCapacity} = {60 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 500;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 60;
            }
            else if (GameMain.history.TechUnlocked(3504))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 4\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 4\nSetting Vessels Capacity: 400 * {GigaStationsPlugin.vesselCapacity} = {400 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 50 * {GigaStationsPlugin.droneCapacity} = {50 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 400;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 50;
            }
            else if (GameMain.history.TechUnlocked(3503))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 3\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 3\nSetting Vessels Capacity: 300 * {GigaStationsPlugin.vesselCapacity} = {300 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 40 * {GigaStationsPlugin.droneCapacity} = {40 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 300;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 40;
            }
            else if (GameMain.history.TechUnlocked(3502))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 2\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 2\nSetting Vessels Capacity: 200 * {GigaStationsPlugin.vesselCapacity} = {200 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 35 * {GigaStationsPlugin.droneCapacity} = {35 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 200;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 35;
            }
            else if (GameMain.history.TechUnlocked(3501))
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 1\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 1\nSetting Vessels Capacity: 200 * {GigaStationsPlugin.vesselCapacity} = {200 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 30 * {GigaStationsPlugin.droneCapacity} = {30 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 200;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 30;
            }
            else // still lvl 0
            {
                GigaStationsPlugin.logger.LogInfo($"\nLogistic carrier capacity Level 0\nSetting Carrier Capacity Multipliers from settings...");
                GigaStationsPlugin.logger.LogInfo($"\nLevel 0\nSetting Vessels Capacity: 200 * {GigaStationsPlugin.vesselCapacity} = {200 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 25 * {GigaStationsPlugin.droneCapacity} = {25 * GigaStationsPlugin.droneCapacity}");
                GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 200;
                GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 25;
            }


            GameMain.history.onTechUnlocked += History_onTechUnlocked;
        }

        private static void History_onTechUnlocked(int techitemID, int techitemLevel)
        {
            /*
                lvl 0	+d:  0	+v:   0	= d:  25 v:  200
                lvl 1	+d:  5	+v:   0	= d:  30 v:  200
                lvl 2	+d:  5	+v:   0	= d:  35 v:  200
                lvl 3	+d:  5	+v: 100	= d:  40 v:  300
                lvl 4	+d: 10	+v: 100 = d:  50 v:  400
                lvl 5	+d: 10	+v: 100 = d:  60 v:  500
                lvl 6	+d: 10	+v: 100 = d:  70 v:  600
                lvl 7	+d: 10	+v: 200 = d:  80 v:  800
                lvl 8	+d: 20	+v: 200 = d: 100 v: 1000
            */

            
            switch (techitemID)
            {
                case 3508:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 1000 * {GigaStationsPlugin.vesselCapacity} = {1000 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 100 * {GigaStationsPlugin.droneCapacity} = {100 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 1000;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 100;
                    break;
                case 3507:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 800 * {GigaStationsPlugin.vesselCapacity} = {800 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 80 * {GigaStationsPlugin.droneCapacity} = {80 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 800;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 80;
                    break;
                case 3506:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 600 * {GigaStationsPlugin.vesselCapacity} = {600 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 70 * {GigaStationsPlugin.droneCapacity} = {70 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 600;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 70;
                    break;
                case 3505:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 500 * {GigaStationsPlugin.vesselCapacity} = {500 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 60 * {GigaStationsPlugin.droneCapacity} = {60 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 500;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 60;
                    break;
                case 3504:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 400 * {GigaStationsPlugin.vesselCapacity} = {400 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 50 * {GigaStationsPlugin.droneCapacity} = {50 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 400;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 50;
                    break;
                case 3503:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 300 * {GigaStationsPlugin.vesselCapacity} = {300 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 40 * {GigaStationsPlugin.droneCapacity} = {40 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 300;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 40;
                    break;
                case 3502:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 200 * {GigaStationsPlugin.vesselCapacity} = {200 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 35 * {GigaStationsPlugin.droneCapacity} = {35 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 200;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 35;
                    break;
                case 3501:
                    GigaStationsPlugin.logger.LogInfo($"\nUnlocked Logistic carrier capacity Level {techitemLevel}\nSetting Carrier Capacity Multipliers from settings...");
                    GigaStationsPlugin.logger.LogInfo($"\nLevel {techitemLevel}\nSetting Vessels Capacity: 200 * {GigaStationsPlugin.vesselCapacity} = {200 * GigaStationsPlugin.vesselCapacity}\nSetting Drones Capacity: 30 * {GigaStationsPlugin.droneCapacity} = {30 * GigaStationsPlugin.droneCapacity}");
                    GameMain.history.logisticShipCarries = GigaStationsPlugin.vesselCapacity * 200;
                    GameMain.history.logisticDroneCarries = GigaStationsPlugin.droneCapacity * 30;
                    break;
                default:
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LDBTool), "VFPreloadPostPatch")] // maybe swap with normal VFPreload if not supporting modded tesla towers? or later preloadpostpatch LDBTool one again if already done
        public static void LDBVFPreloadPostPostfix() // Do when LDB is done
        {
            if (!alreadyInitialized) // Don't do when loading back into main menu
            {

                //LDB.items.Select(2110).prefabDesc.stationMaxItemCount = GigaStationsPlugin.plsMaxStorage;
                //LDB.items.Select(2110).prefabDesc.stationMaxDroneCount = GigaStationsPlugin.plsMaxDrones;
                //LDB.items.Select(2110).prefabDesc.stationMaxItemKinds = GigaStationsPlugin.plsMaxSlots;
                //LDB.items.Select(2110).prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.plsMaxAcuMJ * 1000000);
                ////LDB.items.Select(2110).name = "Planetary Giga Station";




                //LDB.items.Select(2111).prefabDesc.stationMaxItemCount = GigaStationsPlugin.ilsMaxStorage;
                //LDB.items.Select(2111).prefabDesc.stationMaxItemKinds = GigaStationsPlugin.ilsMaxSlots;
                //// Set MaxWarpers in station init!!!!!
                //LDB.items.Select(2111).prefabDesc.stationMaxDroneCount = GigaStationsPlugin.ilsMaxDrones;
                //LDB.items.Select(2111).prefabDesc.stationMaxShipCount = GigaStationsPlugin.ilsMaxVessels;
                //LDB.items.Select(2111).prefabDesc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.ilsMaxAcuGJ * 1000000000);
                ////LDB.items.Select(2111).name = "Interstellar Giga Station";



                //LDB.items.Select(2105).prefabDesc.stationMaxItemCount = GigaStationsPlugin.colMaxStorage;
                //LDB.items.Select(2105).prefabDesc.stationCollectSpeed *= GigaStationsPlugin.colSpeedMultiplier;
                ////LDB.items.Select(2105).name = "Orbital Giga Collector";

                alreadyInitialized = true;
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlanetTransport), "NewStationComponent")] // maybe swap with normal VFPreload if not supporting modded tesla towers? or later preloadpostpatch LDBTool one again if already done
        //public static void NewStationComponentPostfix(PlanetTransport __instance, ref int _entityId, ref int _pcId, ref PrefabDesc _desc, ref StationComponent __result) // Do when LDB is done
        //{


        //    //PowerConsumerComponent powerConsumerComponent = __instance.powerSystem.consumerPool[_pcId];
        //    //Debug.LogError("max accu = " + _desc.prefab.GetComponentInChildren<StationDesc>().maxEnergyAcc);
        //    //Debug.LogError("energyPerTick = " + __result.energyPerTick);
        //    //Debug.LogError("required energy = " + powerConsumerComponent.requiredEnergy);
        //    //Debug.LogError("power ratio = " + powerConsumerComponent.powerRatio);
        //    //Debug.LogError("workEnergyPerTick = " + powerConsumerComponent.workEnergyPerTick);

        //    //StationComponent tmpSC = __result;



        //    //tmpSC.collectionIds = new int[2];
        //    //for (int i = 0; i < length; i++)
        //    //{

        //    //}


        //    //StationComponent altered = __result;
        //    //altered.warperMaxCount = 500;
        //    //__result = altered;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(StationComponent), "SetPCState")]
        //public static bool SetPCStatePrefix(StationComponent __instance, ref PowerConsumerComponent[] pcPool)
        //{
        //    float percentage = (float)((double)__instance.energy / (double)__instance.energyMax);

        //    //if (percentage >= 0.98f) //prevent fast flickering? 
        //    if (__instance.energy == __instance.energyMax)
        //    {
        //        pcPool[__instance.pcId].SetRequiredEnergy(false);
        //    }
        //    else
        //    {
        //        double num = 1;
        //        //double num = 1.0;
        //        if (percentage >= 0.98f)
        //        {
        //            num = 1.05 - percentage;
        //        }
        //        if (num > 1.0)
        //        {
        //            num = 1.0;
        //        }
        //        pcPool[__instance.pcId].SetRequiredEnergy(num);
        //    }
        //    __instance.energyPerTick = pcPool[__instance.pcId].requiredEnergy;

        //    return false;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), "NewConsumerComponent")]
        public static bool NewConsumerComponentPrefix(PowerSystem __instance, ref int entityId, ref long work, ref long idle)
        {
            var x = LDB.items.Select((int)__instance.factory.entityPool[entityId].protoId).ID;
            if (x != 2110 && x !=2111)
            {
                return true;
            }

            work = 1000000;

            return true;

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StationComponent), "Init")] // maybe swap with normal VFPreload if not supporting modded tesla towers? or later preloadpostpatch LDBTool one again if already done
        public static void StationComponentInitPostfix(StationComponent __instance, ref int _id, ref int _entityId, ref int _pcId, ref PrefabDesc _desc, ref EntityData[] _entityPool) // Do when LDB is done
        {

            //GigaStationsPlugin.logger.LogInfo($"protoID: {_entityPool[_entityId].protoId}");

            if (_entityPool[_entityId].protoId != 2110 && _entityPool[_entityId].protoId != 2111 && _entityPool[_entityId].protoId != 2112) // not my gigastations
            {
                return;
            }

            //Debug.Log("ID: " + __instance.id);
            //string text = ((!__instance.isStellar) ? ("Planetary Giga Station #" + __instance.id.ToString()) : ((__instance.isCollector) ? ("Orbital Giga Collector #" + __instance.gid.ToString()) : ("Interstellar Giga Station #" + __instance.gid.ToString())));
            //__instance.name = text;

            if (!__instance.isStellar && !__instance.isCollector) //pls
            {
                //_desc.stationMaxItemCount = GigaStationsPlugin.plsMaxItems;
                //_desc.stationMaxDroneCount = GigaStationsPlugin.plsMaxDrones;
                
                
                _desc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.plsMaxAcuMJ * 1000000);
                __instance.energyMax = GigaStationsPlugin.plsMaxAcuMJ * 1000000;
                __instance.storage = new StationStore[GigaStationsPlugin.plsMaxSlots];
                __instance.needs = new int[13];
                __instance.energyPerTick = 1000000;
            }
            else if (__instance.isStellar && !__instance.isCollector)
            {
                
                
                _desc.stationMaxEnergyAcc = Convert.ToInt64(GigaStationsPlugin.ilsMaxAcuGJ * 1000000000);
                
                //var x = _entityPool[_entityId].
                __instance.energyMax = GigaStationsPlugin.ilsMaxAcuGJ * 1000000000;
                __instance.warperMaxCount = GigaStationsPlugin.ilsMaxWarps;
                __instance.storage = new StationStore[GigaStationsPlugin.ilsMaxSlots];
                __instance.needs = new int[13];
                __instance.energyPerTick = 1000000;
                //_desc.stationMaxItemCount = GigaStationsPlugin.ilsMaxItems;
                //_desc.stationMaxDroneCount = GigaStationsPlugin.ilsMaxDrones;
                //_desc.stationMaxShipCount = GigaStationsPlugin.ilsMaxVessels;
            }
            else if (__instance.isCollector)
            {
                //_desc.stationMaxItemCount = GigaStationsPlugin.colMaxItems;
                //__instance.collectSpeed *= GigaStationsPlugin.colSpeedMultiplier;
            }


        }



        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StationComponent), "Import")]
        public static IEnumerable<CodeInstruction> StationImportTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //do for all should not matter

            List<CodeInstruction> list = instructions.ToList<CodeInstruction>();
            if (list[326].opcode == System.Reflection.Emit.OpCodes.Ldc_I4_6)
            {
                list[326].opcode = System.Reflection.Emit.OpCodes.Ldc_I4;
                list[326].operand = 13;
            }

            return list.AsEnumerable<CodeInstruction>();
        }

        //public static int myId = 0;

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIStationWindow), "get_stationId")]
        //public static void postgetstationid(UIStationWindow __instance, ref int __result)
        //{
        //    myId = __result;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "UpdateNeeds")]
        public static bool UpdateNeeds(StationComponent __instance)
        {
            // Do for all, should not matter

            int num = __instance.storage.Length;
            if (num > 12)
            {
                num = 12;
            }
            for (int i = 0; i <= num; i++)
            {
                if (i == num && !__instance.isCollector)
                {
                    __instance.needs[num] = ((!__instance.isStellar || __instance.warperCount >= __instance.warperMaxCount) ? 0 : 1210); // HIDDEN SLOT?!?!
                }
                else if (i < __instance.needs.Length)
                {
                    __instance.needs[i] = ((i >= num || __instance.storage[i].count >= __instance.storage[i].max) ? 0 : __instance.storage[i].itemId);
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnMinDeliverVesselValueChange")]
        public static bool OnMinDeliverVesselValueChangePrefix(UIStationWindow __instance, ref float value)
        {
            if (__instance.event_lock)
            {
                return false;
            }
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null || stationComponent.id != __instance.stationId)
            {
                return false;
            }
            int num = (int)(value * 1f + 0.5f);
            if (num < 1)
            {
                num = 1;
            }
            stationComponent.deliveryShips = num;
            __instance.minDeliverVesselValue.text = num.ToString("0") + " %";
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnMinDeliverDroneValueChange")]
        public static bool OnMinDeliverDroneValueChangePrefix(UIStationWindow __instance, ref float value)
        {
            if (__instance.event_lock)
            {
                return false;
            }
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (stationComponent == null || stationComponent.id != __instance.stationId)
            {
                return false;
            }
            int num = (int)(value * 1f + 0.5f);
            if (num < 1)
            {
                num = 1;
            }
            stationComponent.deliveryDrones = num;
            __instance.minDeliverDroneValue.text = num.ToString("0") + " %";
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        public static void OnStationIdChangePostfix(UIStationWindow __instance)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return;
            }
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            __instance.minDeliverDroneSlider.value = ((stationComponent.deliveryDrones <= 1) ? 0f : (0.1f * (float)stationComponent.deliveryDrones)) * 10f;
            __instance.minDeliverVesselSlider.value = ((stationComponent.deliveryShips <= 1) ? 0f : (0.1f * (float)stationComponent.deliveryShips)) * 10f;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnStationIdChange")]
        public static bool OnStationIdChangePre(UIStationWindow __instance)
        {
            if (__instance.stationId == 0 || __instance.factory == null)
            {
                __instance._Close();
                return false;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            ItemProto itemProto = LDB.items.Select((int)__instance.factory.entityPool[stationComponent.entityId].protoId);

            if (itemProto.ID != 2110 && itemProto.ID != 2111 && itemProto.ID != 2112)
            {


                //back to vanilla positions/size
                __instance.nameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(__instance.nameInput.GetComponent<RectTransform>().position.x, 0f);
                RectTransform component = __instance.titleText.GetComponent<RectTransform>();
                component.anchoredPosition = new Vector2(component.anchoredPosition.x, 0f);
                __instance.panelDownTrans.anchoredPosition = new Vector2(__instance.panelDownTrans.anchoredPosition.x, stationComponent.isStellar ? 166f : 106f); //collector is also stellar!
                __instance.droneIconButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(__instance.droneIconButton.GetComponent<RectTransform>().position.x, 0f);
                __instance.shipIconButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(__instance.shipIconButton.GetComponent<RectTransform>().position.x, 0f);
                __instance.warperIconButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(__instance.warperIconButton.GetComponent<RectTransform>().position.x, 0f);
                __instance.powerGroupRect.anchoredPosition = new Vector3(__instance.powerGroupRect.anchoredPosition.x, 58f);
                __instance.nameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(40f, -56f);
                __instance.titleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(40f, -20f);
                for (int i = 0; i < __instance.storageUIs.Length; i++)
                {
                    //__instance.storageUIs[i] = UnityEngine.Object.Instantiate<UIStationStorage>(__instance.storageUIPrefab, __instance.storageUIPrefab.transform.parent);
                    __instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta = new Vector2(__instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta.x, 70f);
                    (__instance.storageUIs[i].transform as RectTransform).anchoredPosition = new Vector2(40f, (float)(-90 - 76 * i));
                    //__instance.storageUIs[i].stationWindow = __instance;
                    //__instance.storageUIs[i]._Create();
                }
                return true; // not my giga ILS, return to original code
            }




            if (__instance.active)
            {
                if (__instance.stationId == 0 || __instance.factory == null)
                {
                    __instance._Close();
                    return false;
                }
                if (stationComponent == null || stationComponent.id != __instance.stationId)
                {
                    __instance._Close();
                    return false;
                }
                EventSystem.current.SetSelectedGameObject(null);
                //ItemProto itemProto = LDB.items.Select((int)__instance.factory.entityPool[stationComponent.entityId].protoId);
                if (itemProto == null)
                {
                    __instance._Close();
                    return false;
                }
                __instance.titleText.text = itemProto.name;
                string text = (!string.IsNullOrEmpty(stationComponent.name)) ? stationComponent.name : ((!stationComponent.isStellar) ? ("Planetary Giga Station #" + stationComponent.id.ToString()) : ((stationComponent.isCollector) ? ("Orbital Giga Collector #" + stationComponent.gid.ToString()) : ("Interstellar Giga Station #" + stationComponent.gid.ToString())));
                __instance.nameInput.text = text;
                //int num = (!stationComponent.isCollector) ? stationComponent.storage.Length : stationComponent.collectionIds.Length;
                int num = (!stationComponent.isCollector) ? stationComponent.storage.Length : stationComponent.collectionIds.Length;
                //for (int i = 0; i < itemProto.prefabDesc.stationMaxItemKinds; i++)
                for (int i = 0; i < __instance.storageUIs.Length; i++)
                {
                    if (i < num)
                    {
                        __instance.storageUIs[i].station = stationComponent;
                        __instance.storageUIs[i].index = i;
                        __instance.storageUIs[i]._Open();
                    }
                    else
                    {
                        __instance.storageUIs[i].station = null;
                        __instance.storageUIs[i].index = 0;
                        __instance.storageUIs[i]._Close();
                    }
                    __instance.storageUIs[i].ClosePopMenu();
                }
                bool logisticShipWarpDrive = GameMain.history.logisticShipWarpDrive;
                __instance.panelDown.SetActive(!stationComponent.isCollector);
                __instance.shipIconButton.gameObject.SetActive(stationComponent.isStellar);
                __instance.warperIconButton.gameObject.SetActive(stationComponent.isStellar && logisticShipWarpDrive);
                __instance.powerGroupRect.sizeDelta = new Vector2((float)((!stationComponent.isStellar) ? 440 : ((!logisticShipWarpDrive) ? 380 : 320)), 40f);
                __instance.event_lock = true;
                long workEnergyPerTick = itemProto.prefabDesc.workEnergyPerTick;
                long num2 = workEnergyPerTick * 5L;
                long num3 = workEnergyPerTick / 2L;
                long workEnergyPerTick2 = __instance.factory.powerSystem.consumerPool[stationComponent.pcId].workEnergyPerTick;
                //__instance.maxChargePowerSlider.maxValue = (float)(num2 / 50000L);
                __instance.maxChargePowerSlider.maxValue = 333.83333f;
                //__instance.maxChargePowerSlider.minValue = (float)(num3 / 150000L);
                __instance.maxChargePowerSlider.minValue = 10.0149999f;
                __instance.maxChargePowerSlider.value = (float)(workEnergyPerTick2 / 50000L);
                StringBuilderUtility.WriteKMG(__instance.powerServedSB, 8, workEnergyPerTick2 * 60L, true);
                __instance.maxChargePowerValue.text = __instance.powerServedSB.ToString();
                double num4 = stationComponent.tripRangeDrones;
                if (num4 > 1.0)
                {
                    num4 = 1.0;
                }
                else if (num4 < -1.0)
                {
                    num4 = -1.0;
                }
                float value = (float)Math.Round(Math.Acos(num4) / 3.141592653589793 * 180.0);
                __instance.maxTripDroneSlider.value = value;
                __instance.maxTripDroneValue.text = value.ToString("0 °");
                double num5 = stationComponent.tripRangeShips / 2400000.0;
                if (num5 < 9999.0)
                {
                    __instance.maxTripVesselValue.text = num5.ToString("0 ly");
                }
                else
                {
                    __instance.maxTripVesselValue.text = "∞";
                }
                if (num5 < 20.5)
                {
                    __instance.maxTripVesselSlider.value = (float)num5;
                }
                else if (num5 < 60.5)
                {
                    __instance.maxTripVesselSlider.value = (float)(num5 + 10.0);
                }
                else
                {
                    __instance.maxTripVesselSlider.value = 41f;
                }
                __instance.includeOrbitCollectorCheck.enabled = stationComponent.includeOrbitCollector;
                double num6 = stationComponent.warpEnableDist / 40000.0;
                if (num6 < 10.0)
                {
                    __instance.warperDistanceValue.text = num6.ToString("0.0 AU");
                }
                else
                {
                    __instance.warperDistanceValue.text = num6.ToString("0 AU");
                }
                if (num6 < 0.49)
                {
                    __instance.warperDistanceSlider.value = 1f;
                }
                else if (num6 < 3.01)
                {
                    __instance.warperDistanceSlider.value = (float)(num6 * 2.0 + 1.0);
                }
                else if (num6 < 12.01)
                {
                    __instance.warperDistanceSlider.value = (float)(num6 + 4.0);
                }
                else if (num6 < 21.99)
                {
                    __instance.warperDistanceSlider.value = (float)(num6 * 0.5 + 10.0);
                }
                else
                {
                    __instance.warperDistanceSlider.value = 21f;
                }
                __instance.warperNecessaryCheck.enabled = stationComponent.warperNecessary;

                //__instance.minDeliverDroneSlider.maxValue = 100;
                int num7 = stationComponent.deliveryDrones;
                //__instance.minDeliverDroneSlider.value = ((num7 <= 1) ? 0f : (0.1f * (float)num7));
                //__instance.minDeliverDroneSlider.value = ((num7 <= 1) ? 0f : (0.1f * (float)num7)) * 10f;
                if (num7 < 1)
                {
                    num7 = 1;
                }
                __instance.minDeliverDroneValue.text = num7.ToString("0") + " %";


                //__instance.minDeliverVesselSlider.value = ((num7 <= 1) ? 0f : (0.1f * (float)num7));
                //__instance.minDeliverVesselSlider.value = ((num7 <= 1) ? 0f : (0.1f * (float)num7)) * 10f;
                num7 = stationComponent.deliveryShips;
                if (num7 < 1)
                {
                    num7 = 1;
                }
                __instance.minDeliverVesselValue.text = num7.ToString("0") + " %";
                if (stationComponent.isCollector)
                {
                    __instance.windowTrans.sizeDelta = new Vector2(600f, (float)(80 + 60 * (num)));
                    __instance.configGroup.gameObject.SetActive(false);
                    __instance.maxChargePowerGroup.gameObject.SetActive(false);
                    __instance.maxTripDroneGroup.gameObject.SetActive(false);
                    __instance.maxTripVesselGroup.gameObject.SetActive(false);
                    __instance.includeOrbitCollectorGroup.gameObject.SetActive(false);
                    __instance.warperDistanceGroup.gameObject.SetActive(false);
                    __instance.warperNecessaryGroup.gameObject.SetActive(false);
                    __instance.minDeliverDroneGroup.gameObject.SetActive(false);
                    __instance.minDeliverVesselGroup.gameObject.SetActive(false);
                }
                else if (stationComponent.isStellar)
                {
                    __instance.windowTrans.sizeDelta = new Vector2(600f, (float)(350 + 60 * (num)));
                    __instance.panelDownTrans.anchoredPosition = new Vector2(__instance.panelDownTrans.anchoredPosition.x, 166f);
                    __instance.maxChargePowerGroup.anchoredPosition = new Vector2(__instance.maxChargePowerGroup.anchoredPosition.x, -36f);
                    __instance.maxTripDroneGroup.anchoredPosition = new Vector2(__instance.maxTripDroneGroup.anchoredPosition.x, -56f);
                    __instance.maxTripVesselGroup.anchoredPosition = new Vector2(__instance.maxTripVesselGroup.anchoredPosition.x, -76f);
                    __instance.warperDistanceGroup.anchoredPosition = new Vector2(__instance.warperDistanceGroup.anchoredPosition.x, -96f);
                    __instance.minDeliverDroneGroup.anchoredPosition = new Vector2(__instance.minDeliverDroneGroup.anchoredPosition.x, -116f);
                    __instance.minDeliverVesselGroup.anchoredPosition = new Vector2(__instance.minDeliverVesselGroup.anchoredPosition.x, -136f);
                    __instance.configGroup.gameObject.SetActive(true);
                    __instance.maxChargePowerGroup.gameObject.SetActive(true);
                    __instance.maxTripDroneGroup.gameObject.SetActive(true);
                    __instance.maxTripVesselGroup.gameObject.SetActive(true);
                    __instance.includeOrbitCollectorGroup.gameObject.SetActive(true);
                    __instance.warperDistanceGroup.gameObject.SetActive(true);
                    __instance.warperNecessaryGroup.gameObject.SetActive(true);
                    __instance.minDeliverDroneGroup.gameObject.SetActive(true);
                    __instance.minDeliverVesselGroup.gameObject.SetActive(true);
                }
                else
                {
                    __instance.windowTrans.sizeDelta = new Vector2(600f, (float)(350 + 60 * (num)));
                    __instance.panelDownTrans.anchoredPosition = new Vector2(__instance.panelDownTrans.anchoredPosition.x, 106f);
                    __instance.maxChargePowerGroup.anchoredPosition = new Vector2(__instance.maxChargePowerGroup.anchoredPosition.x, -36f);
                    __instance.maxTripDroneGroup.anchoredPosition = new Vector2(__instance.maxTripDroneGroup.anchoredPosition.x, -56f);
                    __instance.minDeliverDroneGroup.anchoredPosition = new Vector2(__instance.minDeliverDroneGroup.anchoredPosition.x, -76f);
                    __instance.configGroup.gameObject.SetActive(true);
                    __instance.maxChargePowerGroup.gameObject.SetActive(true);
                    __instance.maxTripDroneGroup.gameObject.SetActive(true);
                    __instance.maxTripVesselGroup.gameObject.SetActive(false);
                    __instance.includeOrbitCollectorGroup.gameObject.SetActive(false);
                    __instance.warperDistanceGroup.gameObject.SetActive(false);
                    __instance.warperNecessaryGroup.gameObject.SetActive(false);
                    __instance.minDeliverDroneGroup.gameObject.SetActive(true);
                    __instance.minDeliverVesselGroup.gameObject.SetActive(false);
                }

                __instance.nameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(__instance.nameInput.GetComponent<RectTransform>().position.x + 215f, -15f);



                __instance.titleText.text = "Taki7o7's Giga Stations Mod";
                var tta = __instance.titleText.GetComponent<RectTransform>();
                tta.anchoredPosition = new Vector2(tta.anchoredPosition.x, -10f);
                __instance.nameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(__instance.nameInput.GetComponent<RectTransform>().position.x + 215f, -15f);

                for (int i = 0; i < __instance.storageUIs.Length; i++)
                {
                    __instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta = new Vector2(__instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta.x, 60);
                    (__instance.storageUIs[i].transform as RectTransform).anchoredPosition = new Vector2(40f, (float)(-40 - 66 * i));

                }

                if (!stationComponent.isCollector)
                {
                    //__instance.windowTrans.sizeDelta = new Vector2(600f, (float)(1070));
                    __instance.panelDownTrans.anchoredPosition = new Vector2(__instance.panelDownTrans.anchoredPosition.x, 156f);
                    __instance.maxChargePowerGroup.anchoredPosition = new Vector2(__instance.maxChargePowerGroup.anchoredPosition.x, -36f);
                    __instance.droneIconButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(__instance.droneIconButton.GetComponent<RectTransform>().position.x, -25f);
                    __instance.shipIconButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(__instance.shipIconButton.GetComponent<RectTransform>().position.x, -25f);
                    __instance.warperIconButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(__instance.warperIconButton.GetComponent<RectTransform>().position.x, -25f);
                    __instance.powerGroupRect.anchoredPosition = new Vector3(__instance.powerGroupRect.anchoredPosition.x, 30f);
                }



                __instance.event_lock = false;
            }
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "OnWarperIconClick")]
        public static bool OnWarperIconClickPrefix(UIStationWindow __instance, ref int obj)
        {
            if ((__instance.stationId == 0 || __instance.factory == null))
            {
                __instance._Close();
                return false;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];

            ItemProto gigaProto = LDB.items.Select((int)__instance.factory.entityPool[stationComponent.entityId].protoId);

            //GigaStationsPlugin.logger.LogInfo($"gigaProtoID: {gigaProto.ID} --- gigaProtoSID: {gigaProto.SID}");

            if (gigaProto.ID != 2110 && gigaProto.ID != 2111 && gigaProto.ID != 2112)
            {
                return true; // not my giga ILS, return to original code
            }


            if (__instance.stationId == 0 || __instance.factory == null)
            {
                return false;
            }

            if (stationComponent == null || stationComponent.id != __instance.stationId)
            {
                return false;
            }
            if (!stationComponent.isStellar)
            {
                return false;
            }
            if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount == 0)
            {
                __instance.player.SetHandItems(0, 0, 0);
            }
            else if (__instance.player.inhandItemId > 0 && __instance.player.inhandItemCount > 0)
            {
                int num = 1210;
                ItemProto itemProto = LDB.items.Select(num);
                if (__instance.player.inhandItemId != num)
                {
                    UIRealtimeTip.Popup("只能放入".Translate() + itemProto.name, true, 0);
                    return false;
                }
                int num2 = GigaStationsPlugin.ilsMaxWarps;
                int warperCount = stationComponent.warperCount;
                int num3 = num2 - warperCount;
                if (num3 < 0)
                {
                    num3 = 0;
                }
                int num4 = (__instance.player.inhandItemCount >= num3) ? num3 : __instance.player.inhandItemCount;
                if (num4 <= 0)
                {
                    UIRealtimeTip.Popup("栏位已满".Translate(), true, 0);
                    return false;
                }
                stationComponent.warperCount += num4;
                __instance.player.AddHandItemCount_Unsafe(-num4);
                if (__instance.player.inhandItemCount <= 0)
                {
                    __instance.player.SetHandItemId_Unsafe(0);
                    __instance.player.SetHandItemCount_Unsafe(0);
                }
            }
            else if (__instance.player.inhandItemId == 0 && __instance.player.inhandItemCount == 0)
            {
                int warperCount2 = stationComponent.warperCount;
                int num5 = warperCount2;
                if (num5 <= 0)
                {
                    return false;
                }
                if (VFInput.shift || VFInput.control)
                {
                    num5 = __instance.player.package.AddItemStacked(1210, num5);
                    if (warperCount2 != num5)
                    {
                        UIRealtimeTip.Popup("无法添加物品".Translate(), true, 0);
                    }
                    UIItemup.Up(1210, num5);
                }
                else
                {
                    __instance.player.SetHandItemId_Unsafe(1210);
                    __instance.player.SetHandItemCount_Unsafe(num5);
                }
                stationComponent.warperCount -= num5;
                if (stationComponent.warperCount < 0)
                {
                    Assert.CannotBeReached();
                    stationComponent.warperCount = 0;
                }
            }

            return false;
        }


        // Fixing Belt cannot input for item-slots 7-12
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoPath), "TryPickItemAtRear", new Type[] { typeof(int[]), typeof(int) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
        public static bool TryPickItemAtRear(CargoPath __instance, int[] needs, out int needIdx, ref int __result)
        {
            needIdx = -1;
            int num = __instance.bufferLength - 5 - 1;
            if (__instance.buffer[num] == 250)
            {
                int num2 = (int)(__instance.buffer[num + 1] - 1 + (__instance.buffer[num + 2] - 1) * 100) + (int)(__instance.buffer[num + 3] - 1) * 10000 + (int)(__instance.buffer[num + 4] - 1) * 1000000;
                int item = __instance.cargoContainer.cargoPool[num2].item;

                for (int i = 0; i < needs.Length; i++)
                {
                    if (item == needs[i])
                    {
                        Array.Clear(__instance.buffer, num - 4, 10);
                        int num3 = num + 5 + 1;
                        if (__instance.updateLen < num3)
                        {
                            __instance.updateLen = num3;
                        }
                        __instance.cargoContainer.RemoveCargo(num2);
                        needIdx = i;
                        __result = item;
                        return false;
                    }
                }
            }
            __result = 0;
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "TakeItem", new Type[] { typeof(int), typeof(int), typeof(int[]) }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal })]
        public static bool TakeItemPrefix(StationComponent __instance, ref int _itemId, ref int _count, ref int[] _needs)
        {


            bool flag = false;
            if (_needs == null)
            {
                flag = true;
            }
            else
            {
                foreach (var need in _needs)
                {
                    if (need == _itemId)
                    {
                        flag = true;
                    }
                }
            }

            if (_itemId > 0 && _count > 0 && (flag))
            {
                int num = __instance.storage.Length;
                for (int i = 0; i < num; i++)
                {
                    if (__instance.storage[i].itemId == _itemId && __instance.storage[i].count > 0)
                    {
                        _count = ((_count >= __instance.storage[i].count) ? __instance.storage[i].count : _count);
                        _itemId = __instance.storage[i].itemId;
                        StationStore[] array = __instance.storage;
                        int num2 = i;
                        array[num2].count = array[num2].count - _count;
                        return false;
                    }
                }
            }
            _itemId = 0;
            _count = 0;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationComponent), "AddItem")]
        public static bool AddItemPrefix(StationComponent __instance, ref int itemId, ref int count, ref int __result)
        {
            if (itemId <= 0)
            {
                __result = 0;
                return false;
            }
            int num = __instance.storage.Length;

            if (0 < num && __instance.storage[0].itemId == itemId)
            {
                if (true)
                {

                }
                StationStore[] array = __instance.storage;
                int num2 = 0;
                array[num2].count = array[num2].count + count;
                __result = count;
                return false;
            }
            if (1 < num && __instance.storage[1].itemId == itemId)
            {
                StationStore[] array2 = __instance.storage;
                int num3 = 1;
                array2[num3].count = array2[num3].count + count;
                __result = count;
                return false;
            }
            if (2 < num && __instance.storage[2].itemId == itemId)
            {
                StationStore[] array3 = __instance.storage;
                int num4 = 2;
                array3[num4].count = array3[num4].count + count;
                __result = count;
                return false;
            }
            if (3 < num && __instance.storage[3].itemId == itemId)
            {
                StationStore[] array4 = __instance.storage;
                int num5 = 3;
                array4[num5].count = array4[num5].count + count;
                __result = count;
                return false;
            }
            if (4 < num && __instance.storage[4].itemId == itemId)
            {
                StationStore[] array5 = __instance.storage;
                int num6 = 4;
                array5[num6].count = array5[num6].count + count;
                __result = count;
                return false;
            }
            if (5 < num && __instance.storage[5].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num7 = 5;
                array6[num7].count = array6[num7].count + count;
                __result = count;
                return false;
            }
            if (6 < num && __instance.storage[6].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num8 = 6;
                array6[num8].count = array6[num8].count + count;
                __result = count;
                return false;
            }
            if (7 < num && __instance.storage[7].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num9 = 7;
                array6[num9].count = array6[num9].count + count;
                __result = count;
                return false;
            }
            if (8 < num && __instance.storage[8].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num10 = 8;
                array6[num10].count = array6[num10].count + count;
                __result = count;
                return false;
            }
            if (9 < num && __instance.storage[9].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num11 = 9;
                array6[num11].count = array6[num11].count + count;
                __result = count;
                return false;
            }
            if (10 < num && __instance.storage[10].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num12 = 10;
                array6[num12].count = array6[num12].count + count;
                __result = count;
                return false;
            }
            if (11 < num && __instance.storage[11].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num13 = 11;
                array6[num13].count = array6[num13].count + count;
                __result = count;
                return false;
            }
            if (12 < num && __instance.storage[12].itemId == itemId)
            {
                StationStore[] array6 = __instance.storage;
                int num14 = 12;
                array6[num14].count = array6[num14].count + count;
                __result = count;
                return false;
            }
            __result = 0;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStationWindow), "_OnCreate")]
        public static bool _OnCreatePrefix(UIStationWindow __instance)
        {
            // do always

            //part of 1% sliderstep fix
            __instance.minDeliverDroneSlider.maxValue = 100;
            __instance.minDeliverVesselSlider.maxValue = 100;


            __instance.storageUIs = new UIStationStorage[12];
            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                __instance.storageUIs[i] = UnityEngine.Object.Instantiate<UIStationStorage>(__instance.storageUIPrefab, __instance.storageUIPrefab.transform.parent);
                __instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta = new Vector2(__instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta.x, __instance.storageUIs[i].GetComponent<RectTransform>().sizeDelta.y - 10f);
                (__instance.storageUIs[i].transform as RectTransform).anchoredPosition = new Vector2(40f, (float)(-40 - 66 * i));
                __instance.storageUIs[i].stationWindow = __instance;
                __instance.storageUIs[i]._Create();
            }
            return false;
        }
    }

}
