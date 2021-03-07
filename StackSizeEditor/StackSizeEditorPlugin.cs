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
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;

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

        public static BepInEx.Configuration.ConfigFile myCfg;
        //public static ConfigFile CustomStackSizeConfig = new ConfigFile(Paths.ConfigPath + "/StackSizeEditor/StackSizeEditor.cfg", true);

        public static BinaryFormatter formatter = null;
        public static SaveData saveData = null;

        public static ConfigEntry<KeyCode> KeyConfig;
        public static ConfigEntry<bool> SortWhenApply;

        public static Dictionary<int, int> originalStackSizes = new Dictionary<int, int>();

        public static bool gameLoaded = false;
        //public static ItemProtoSet originalItemProtos;
        //public static ItemProto[] originalItemProtoArray;

        public static GameObject ScrollViewCanvas;
        public static GameObject EntryPrefab;
        public static GameObject myCanvas;
        public static Canvas myCanvasMain;
        public static Canvas myCanvasHeader;
        public static Text myHeaderText;
        public static GameObject myScrollViewContent;

        public static Image thumb;
        public static InputField myInputField;

        public static int SortBy = 0;  // 0 = name, 1 = id, 2 = stacksize
        public static int Reverse = 0; // 0 = don't reverse, 1 = reverse

        void Awake()
        {
            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(StackSizeEditorPatch));
        }
        //ScriptableObject x = new ScriptableObject();

        void SaveSaveData()
        {
            Directory.CreateDirectory(Paths.ConfigPath + "/StackSizeEditor");
            if (saveData == null)
            {
                saveData = new SaveData();
            }

            var file = new FileStream(Paths.ConfigPath + "/StackSizeEditor/StackSizeEditor.7o7", FileMode.OpenOrCreate, FileAccess.ReadWrite);
            formatter.Serialize(file, saveData);
            file.Close();
        }

        void LoadSaveData()
        {
            
            Directory.CreateDirectory(Paths.ConfigPath + "/StackSizeEditor");
            try
            {
                var file = new FileStream(Paths.ConfigPath + "/StackSizeEditor/StackSizeEditor.7o7", FileMode.Open, FileAccess.Read);
                saveData = (SaveData)formatter.Deserialize(file);
                file.Close();
            }
            catch
            {
                SaveSaveData();
            }
        }

        void Start()
        {
            
            myCfg = Config;

            formatter = new BinaryFormatter();
            LoadSaveData();






            //CustomStackSizeConfig.Reload();
            //foreach (var item in CustomStackSizeConfig)
            //{
            //    Debug.LogError(item.Key.Key);
            //    Debug.LogError(item.Value.BoxedValue);
            //}
            SortWhenApply = base.Config.Bind<bool>("1 | Sort when Apply |", "SortWhenApply", false, "If enabled, it will automatically sort all Storages when you apply a new Stack-Size.");
            KeyConfig = base.Config.Bind<KeyCode>("2 | UI-Key |", "HotKey", KeyCode.K, "Binds a HotKey for opening/closing the StackSizeEditor UI");

            var ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("StackSizeEditor.takiui"));
            ScrollViewCanvas = ab.LoadAsset<GameObject>("myCanvas");
            EntryPrefab = ab.LoadAsset<GameObject>("myEntryCanvas");

            //if (ScrollViewCanvas == null)
            //{
            //    Debug.LogWarning("canvas is null");
            //}
            //else
            //{
            //    Debug.LogWarning("canvas is NOT null");
            //}

            myCanvas = Instantiate<GameObject>(ScrollViewCanvas);
            //myScrollView.renderMode = RenderMode.ScreenSpaceOverlay;
            //myScrollView.enabled = true;

            foreach (var dropdown in myCanvas.GetComponentsInChildren<Dropdown>())
            {
                //Debug.LogWarning($"Dropdown found: {dropdown.name}");
                switch (dropdown.name)
                {
                    case "myDropdownSortBy":
                        dropdown.onValueChanged.AddListener(delegate { DropDownSortByValueChanged(dropdown.value); });
                        break;
                    case "myDropdownSortReverse":
                        dropdown.onValueChanged.AddListener(delegate { DropDownReverseValueChanged(dropdown.value); });
                        break;
                    default:
                        break;
                }
            }

            foreach (var button in myCanvas.GetComponentsInChildren<Button>())
            {
                //Debug.LogWarning($"Button found: {button.name}");
                switch (button.name)
                {
                    case "ButtonLoadList":
                        button.onClick.AddListener(delegate { LoadList(SortBy, Reverse); });
                        break;
                    case "ButtonSortInvs":
                        button.onClick.AddListener(delegate { SortAllStorages(); });
                        break;
                    case "ButtonClose":
                        button.onClick.AddListener(delegate { CloseUI(); });
                        break;
                    default:
                        break;
                }
            }

            foreach (var toggle in myCanvas.GetComponentsInChildren<Toggle>())
            {
                switch (toggle.gameObject.name)
                {
                    case "ToggleSortWhenApply":
                        toggle.isOn = SortWhenApply.Value;
                        toggle.onValueChanged.AddListener(delegate { SortWhenApplyToggleValChanged(toggle.isOn); });
                        break;
                    default:
                        break;
                }
            }

            //foreach (var imageitem in myCanvas.GetComponentsInChildren<Image>())
            //{
            //    switch (imageitem.name)
            //    {
            //        case "ImageThumbnail":
            //            Debug.LogWarning($"{imageitem.name} found");
            //            thumb = imageitem;
            //            break;
            //        default:
            //            break;
            //    }
            //}

            foreach (var inputField in myCanvas.GetComponentsInChildren<InputField>())
            {
                switch (inputField.name)
                {
                    case "myInputField":
                        myInputField = inputField;
                        break;
                    default:
                        break;
                }
            }

            //foreach (var textItem in myCanvas.GetComponentsInChildren<Text>())
            //{
            //    switch (textItem.name)
            //    {
            //        case "HeaderText":
            //            myHeaderText = textItem;
            //            myHeaderText.enabled = false; // init hide error message
            //            break;
            //        default:
            //            break;
            //    }
            //}

            //Debug.LogError("Start Canvas Search");
            foreach (var canvas in myCanvas.GetComponentsInChildren<Canvas>())
            {
                //Debug.LogError(canvas.name);
                switch (canvas.name)
                {
                    case "CanvasMain":
                        //Debug.LogWarning("Found CanvasMain!!! ########");
                        myCanvasMain = canvas;
                        break;
                    case "CanvasHeader":
                        //Debug.LogWarning("Found CanvasHeader!!! ########");
                        myCanvasHeader = canvas;
                        canvas.gameObject.AddComponent<DragWindow>();
                        break;
                    default:
                        break;
                }
            }

            foreach (var item in myCanvasMain.GetComponentsInChildren<RectTransform>())
            {
                switch (item.gameObject.name)
                {
                    case "myScrollViewContent":

                        myScrollViewContent = item.gameObject;

                        //foreach (Transform child in item.transform)
                        //{
                        //    Destroy(child.gameObject);
                        //}
                        break;
                    default:
                        break;
                }
            }

            myCanvas.SetActive(false);

        }

        void SortWhenApplyToggleValChanged(bool isOn)
        {
            SortWhenApply.Value = isOn;
            Config.Save();
        }

        void SortAllStorages()
        {
            // SORT
            foreach (var factoryItem in GameMain.data.factories)
            {
                try
                {
                    foreach (StorageComponent item in factoryItem.factoryStorage.storagePool)
                    {
                        try
                        {

                            item.Sort(true);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }

            }
            GameMain.mainPlayer.package.Sort(true);
            //SORT END
        }

        void DropDownSortByValueChanged(int newVal)
        {
            if (newVal != SortBy)
            {
                SortBy = newVal;

                //RE-LOAD/SORT HERE !!!
                LoadList(SortBy, Reverse);
            }
        }

        void DropDownReverseValueChanged(int newVal)
        {
            if (newVal != Reverse)
            {
                Reverse = newVal;

                //RE-LOAD/SORT HERE !!!
                LoadList(SortBy, Reverse);
            }
        }

        void CloseUI()
        {
            myCanvas.SetActive(false);
            VFInput.UpdateGameStates();
        }
        //private static ConfigFile StackSizes = new ConfigFile(Paths.ConfigPath + "/StackSizeEditor/stackSizes.cfg", true);
        void LoadList(int sortBy, int reverse) // 0 = Name, 1 = ID
        {
            foreach (Transform child in myScrollViewContent.transform)
            {
                Destroy(child.gameObject);
            }

            List<ItemProto> myList = LDB.items.dataArray.ToList();
            if (sortBy == 1)
            {
                // Sort by ID
                myList.Sort((x, y) =>
                    x.ID.CompareTo(y.ID)
                );
            }
            else if (sortBy == 2)
            {
                // Sort by StackSize
                myList.Sort((x, y) =>
                    x.StackSize.CompareTo(y.StackSize)
                );
            }
            else
            {
                // Sort by Name
                myList.Sort((x, y) =>
                    x.name.Translate().CompareTo(y.name.Translate())
                );
            }

            if (reverse == 1)
            {
                myList.Reverse();
            }

            foreach (var itemProto in myList)
            {
                GameObject tmpGo = Instantiate(EntryPrefab);
                tmpGo.name = $"myEntryCanvas_{itemProto.ID}";
                InputField tmpInputField = tmpGo.transform.Find("EntryInputField").GetComponent<InputField>();
                tmpGo.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);

                foreach (Transform childTrans in tmpGo.transform)
                {
                    switch (childTrans.gameObject.name)
                    {
                        case "EntryImage":
                            childTrans.gameObject.GetComponent<Image>().sprite = itemProto._iconSprite;
                            break;
                        case "TextName":
                            childTrans.gameObject.GetComponent<Text>().text = itemProto.name.Translate();
                            break;
                        case "TextID":
                            childTrans.gameObject.GetComponent<Text>().text = itemProto.ID.ToString();
                            break;
                        case "TextDefault":
                            childTrans.gameObject.GetComponent<Text>().text = originalStackSizes[itemProto.ID].ToString();
                            break;
                        case "EntryInputField":
                            //tmpInputField = childTrans.gameObject.GetComponent<InputField>();
                            tmpInputField.text = itemProto.StackSize.ToString();
                            //childTrans.gameObject.GetComponent<InputField>().text = itemProto.StackSize.ToString();
                            break;
                        case "ButtonApply":
                            childTrans.gameObject.GetComponent<Button>().onClick.AddListener(delegate { EditStackSize(itemProto, Convert.ToInt32(tmpInputField.text)); });
                            break;
                        case "ButtonReset":
                            childTrans.gameObject.GetComponent<Button>().onClick.AddListener(delegate { ResetStackSize(itemProto, tmpInputField); });
                            break;
                        default:
                            break;
                    }
                }
                tmpGo.transform.SetParent(myScrollViewContent.transform, false);
            }
        }

        void ResetStackSize(ItemProto itemProto, InputField inputField)
        {
            try
            {
                //ItemProto tmpProto = originalItemProtos.Select(itemProto.ID);
                //var tmpProto = originalItemProtoArray.Where(x => x.ID == itemProto.ID).First();
                int oss = StackSizeEditorPlugin.originalStackSizes[itemProto.ID];

                inputField.text = oss.ToString();

                if (itemProto.StackSize != oss)
                {
                    LDB.items.Select(itemProto.ID).StackSize = oss;
                    StorageComponent.itemStackCount[itemProto.ID] = oss;
                }

                if (saveData == null || saveData.StackSizesDict == null || saveData.StackSizesDict.Count == 0)
                {
                    return;
                }

                if (saveData.StackSizesDict.ContainsKey(itemProto.ID))
                {
                    saveData.StackSizesDict.Remove(itemProto.ID);
                    SaveSaveData();
                }

            }
            catch
            {

            }
        }

        void EditStackSize(ItemProto itemProto, int newStackSize)
        {
            if (saveData.StackSizesDict == null)
            {
                saveData.StackSizesDict = new Dictionary<int, int>();
            }

            if (saveData.StackSizesDict.ContainsKey(itemProto.ID) && saveData.StackSizesDict[itemProto.ID] != newStackSize)
            {
                saveData.StackSizesDict[itemProto.ID] = newStackSize;
                LDB.items.Select(itemProto.ID).StackSize = newStackSize;
                StorageComponent.itemStackCount[itemProto.ID] = newStackSize;
                if (SortWhenApply.Value)
                {
                    SortAllStorages();
                }
                SaveSaveData();
            }
            else
            {
                if (newStackSize != itemProto.StackSize)
                {
                    saveData.StackSizesDict.Add(itemProto.ID, newStackSize);
                    LDB.items.Select(itemProto.ID).StackSize = newStackSize;
                    StorageComponent.itemStackCount[itemProto.ID] = newStackSize;
                    if (SortWhenApply.Value)
                    {
                        SortAllStorages();
                    }
                    SaveSaveData();
                }
            }


            //var ce = CustomStackSizeConfig.Bind<int>("Item-Stacksizes ONLY EDIT INGAME", itemProto.ID.ToString(), itemProto.StackSize, itemProto.name.Translate());
            //ce.BoxedValue = newStackSize;
            ////cStackSize = newStackSize;

            //CustomStackSizeConfig.Save();
        }

        //Coroutine ErrorMessageRoutine;
        //void Clicked(string BtnName)
        //{
        //    //foreach (var item in myCanvasMain.GetComponentsInChildren<RectTransform>())
        //    //{
        //    //    switch (item.gameObject.name)
        //    //    {
        //    //        case "myScrollViewContent":
        //    //            foreach (Transform child in item.transform)
        //    //            {
        //    //                Destroy(child.gameObject);
        //    //            }
        //    //            break;
        //    //        default:
        //    //            break;
        //    //    }
        //    //}


        //    //return;

        //    //foreach (ItemProto itemProto in LDB.items.dataArray)
        //    //{
        //    //    if (itemProto.ID == 2210)
        //    //    {

        //    //        itemProto.StackSize = 123;
        //    //        StorageComponent.itemStackCount[2210] = 123;
        //    //    }
        //    //}


        //    // SORT

        //    foreach (var factoryItem in GameMain.data.factories)
        //    {
        //        try
        //        {
        //            foreach (StorageComponent item in factoryItem.factoryStorage.storagePool)
        //            {
        //                try
        //                {

        //                    item.Sort(true);
        //                    Debug.LogError("SORTING");

        //                }
        //                catch (Exception)
        //                {
        //                    continue;
        //                }
        //            }
        //        }
        //        catch (Exception)
        //        {

        //            continue;
        //        }

        //    }

        //    Debug.LogError("SORTING PLAYER INVERNTORY");
        //    GameMain.mainPlayer.package.Sort(true);

        //    //UIRoot.instance.uiGame.inventory.OnSort();

        //    //SORT END

        //    //foreach (StorageComponent item in GameMain.mainPlayer.factory.factoryStorage.storagePool)
        //    //{
        //    //    try
        //    //    {

        //    //        item.Sort(true);
        //    //        Debug.LogError("SORTING");

        //    //    }
        //    //    catch (Exception)
        //    //    {
        //    //        //nothing
        //    //    }
        //    //}

        //    Debug.LogWarning($"Button \"{BtnName}\" pressed!");

        //    bool success = Int32.TryParse(myInputField.text, out int result);
        //    ItemProto selectedItem = null;
        //    if (success)
        //    {
        //        selectedItem = LDB.items.Select(result);
        //    }

        //    if (success && selectedItem != null)
        //    {
        //        //myHeaderText.enabled = false;
        //        //if (ErrorMessageRoutine != null)
        //        //{
        //        //    StopCoroutine(ErrorMessageRoutine);
        //        //}

        //        //thumb.sprite = selectedItem._iconSprite;
        //    }
        //    else
        //    {
        //        ErrorMessageRoutine = StartCoroutine(ShowError(2f));
        //    }

        //}

        //IEnumerator ShowError(float delay)
        //{
        //    if (ErrorMessageRoutine != null)
        //    {
        //        StopCoroutine(ErrorMessageRoutine);
        //    }
        //    myHeaderText.enabled = true;
        //    yield return new WaitForSeconds(delay);
        //    myHeaderText.enabled = false;
        //    ErrorMessageRoutine = null;
        //}

        private void Update()
        {
            bool keyDown = Input.GetKeyDown(KeyConfig.Value);
            //if (keyDown && GameMain.mainPlayer.factory != null && GameMain.mainPlayer.factory.factoryStorage != null && GameMain.mainPlayer.factory.factoryStorage.storagePool != null)
            if (keyDown && gameLoaded && GameMain.mainPlayer != null && (GameMain.mainPlayer.factory != null || (GameMain.data.mainPlayer.mecha != null && GameMain.data.mainPlayer.mecha.droneCount != 34)))
            {
                //Debug.LogError(GameMain.data.mainPlayer.mecha.droneCount);
                //Debug.LogWarning($"mainplayer is null? {(GameMain.mainPlayer.mecha == null)}");

                UIGame uiGame = UIRoot.instance.uiGame;
                if (VFInput.inFullscreenGUI && !myCanvas.activeSelf)
                {
                    return;
                }
                //if (!myCanvas.activeSelf)
                //{
                //    thumb.sprite = LDB.items.Select(2104)._iconSprite;
                //}
                myCanvas.SetActive(!myCanvas.activeSelf);
                VFInput.UpdateGameStates();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && myCanvas.activeSelf)
            {
                myCanvas.SetActive(false);
                //VFInput.UpdateGameStates();
            }

        }

    }

    [HarmonyPatch]
    public class StackSizeEditorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LDBTool), "VFPreloadPostPatch")]
        public static void LDBVFPreloadPostPatchPostfix()
        {
            
            StackSizeEditorPlugin.gameLoaded = true;

            //StackSizeEditorPlugin.originalItemProtos = LDB.items.Copy();

            //StackSizeEditorPlugin.originalItemProtoArray = LDB.items.dataArray.Copy();
            StackSizeEditorPlugin.originalStackSizes = new Dictionary<int, int>();

            foreach (var item in LDB.items.dataArray)
            {
                StackSizeEditorPlugin.originalStackSizes.Add(item.ID, item.StackSize);
            }


            //StackSizeEditorPlugin.originalItemProtos = LDB.items.Copy(); // for getting original default values
            if (StackSizeEditorPlugin.saveData == null || StackSizeEditorPlugin.saveData.StackSizesDict == null || StackSizeEditorPlugin.saveData.StackSizesDict.Count == 0)
            {
                return;
            }
            foreach (var item in StackSizeEditorPlugin.saveData.StackSizesDict)
            {

                try
                {
                    var ip = LDB.items.Select(Convert.ToInt32(item.Key));
                    if (ip != null)
                    {
                        if (item.Value == ip.StackSize)
                        {
                            continue;
                        }

                        ip.StackSize = item.Value;
                        StorageComponent.itemStackCount[ip.ID] = item.Value;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), "UpdateGameStates")]
        public static void UpdateGameStatesPostfix()
        {
            UIGame uiGame = UIRoot.instance.uiGame;

            if ((uiGame.active && (uiGame.techTree.active || uiGame.dysonmap.active || uiGame.starmap.active)))
            {
                StackSizeEditorPlugin.myCanvas.SetActive(false);
                return;
            }
            VFInput.inFullscreenGUI = VFInput.inFullscreenGUI || StackSizeEditorPlugin.myCanvas.activeSelf;
        }


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(FactoryStorage), "Import")]
        //public static void ImportPostfix()
        //{
        //    Debug.LogError("FACTORY STORAGE IMPORT DONE!");
        //    StackSizeEditorPlugin.gameLoaded = true;
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIRoot), "OnGameEnd")]
        //public static void OnGameEndPostfix(UIRoot __instance)
        //{
        //    Debug.LogError("OnGameEnd");
        //    StackSizeEditorPlugin.gameLoaded = false;
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIRoot), "OnGameMainObjectCreated")]
        //public static void OnGameMainObjectCreatedPostfix(UIRoot __instance)
        //{
        //    Debug.LogError("OnGameMainObjectCreated");
        //    StackSizeEditorPlugin.gameLoaded = true;
        //    GameMain.isRunning
        //}


    }

    [System.Serializable]
    public class SaveData
    {
        public Dictionary<int, int> StackSizesDict { get; set; }
    }

    public class DragWindow : MonoBehaviour, IDragHandler
    {
        private RectTransform dragRectTransform = StackSizeEditorPlugin.myCanvasMain.GetComponent<RectTransform>();
        public void OnDrag(PointerEventData eventData)
        {
            if (dragRectTransform != null)
            {
                dragRectTransform.anchoredPosition += eventData.delta;
            }
        }
    }
}
