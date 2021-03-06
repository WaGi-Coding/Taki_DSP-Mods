﻿using System;
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

        GameObject ScrollViewCanvas;
        public static GameObject myCanvas;
        public static Canvas myCanvasMain;
        public static Canvas myCanvasHeader;
        public static Text myHeaderText;

        public static Image thumb;
        public static InputField myInputField;

        void Awake()
        {
            var harmony = new Harmony(ModGuid);

            harmony.PatchAll(typeof(StackSizeEditorPatch));
        }
        ScriptableObject x = new ScriptableObject();

        void Start()
        {
            
            var ab = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("StackSizeEditor.takiui"));
            ScrollViewCanvas = ab.LoadAsset<GameObject>("myCanvas");
            
            if (ScrollViewCanvas == null)
            {
                Debug.LogWarning("canvas is null");
            }
            else
            {
                Debug.LogWarning("canvas is NOT null");
            }
            
            myCanvas = Instantiate<GameObject>(ScrollViewCanvas);
            //myScrollView.renderMode = RenderMode.ScreenSpaceOverlay;
            //myScrollView.enabled = true;
            
            foreach (var button in myCanvas.GetComponentsInChildren<Button>())
            {
                Debug.LogWarning($"Button found: {button.name}");
                switch (button.name)
                {
                    case "Button1":
                        button.onClick.AddListener(delegate { Clicked(button.name); });
                        break;
                    case "Button2":
                        button.onClick.AddListener(delegate { Clicked(button.name); });
                        break;
                    case "ButtonClose":
                        button.onClick.AddListener(delegate { CloseUI(); });
                        break;
                    default:
                        break;
                }
            }
            foreach (var imageitem in myCanvas.GetComponentsInChildren<Image>())
            {
                switch (imageitem.name)
                {
                    case "ImageThumbnail":
                        Debug.LogWarning($"{imageitem.name} found");
                        thumb = imageitem;
                        break;
                    default:
                        break;
                }
            }

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

            foreach (var textItem in myCanvas.GetComponentsInChildren<Text>())
            {
                switch (textItem.name)
                {
                    case "HeaderText":
                        myHeaderText = textItem;
                        myHeaderText.enabled = false; // init hide error message
                        break;
                    default:
                        break;
                }
            }

            Debug.LogError("Start Canvas Search");
            foreach (var canvas in myCanvas.GetComponentsInChildren<Canvas>())
            {
                Debug.LogError(canvas.name);
                switch (canvas.name)
                {
                    case "CanvasMain":
                        Debug.LogWarning("Found CanvasMain!!! ########");
                        myCanvasMain = canvas;
                        break;
                    case "CanvasHeader":
                        Debug.LogWarning("Found CanvasHeader!!! ########");
                        myCanvasHeader = canvas;
                        canvas.gameObject.AddComponent<DragWindow>();
                        break;
                    default:
                        break;
                }
            }

            myCanvas.SetActive(false);

        }

        void CloseUI()
        {
            myCanvas.SetActive(false);
            VFInput.UpdateGameStates();
        }


        Coroutine ErrorMessageRoutine;
        void Clicked(string BtnName)
        {
            Debug.LogWarning($"Button \"{BtnName}\" pressed!");

            bool success = Int32.TryParse(myInputField.text, out int result);
            ItemProto selectedItem = null;
            if (success)
            {
                selectedItem = LDB.items.Select(result);
            }

            if (success && selectedItem != null)
            {
                thumb.sprite = selectedItem._iconSprite;
            }
            else
            {
                ErrorMessageRoutine = StartCoroutine(ShowError(2f));
            }

        }

        IEnumerator ShowError(float delay)
        {
            if (ErrorMessageRoutine != null)
            {
                StopCoroutine(ErrorMessageRoutine);
            }
            myHeaderText.enabled = true;
            yield return new WaitForSeconds(delay);
            myHeaderText.enabled = false;
            ErrorMessageRoutine = null;
        }

        private void Update()
        {
            bool keyDown = Input.GetKeyDown(KeyCode.K);
            if (keyDown)
            {
                Debug.LogWarning($"mainplayer is null? {(GameMain.mainPlayer.mecha == null)}");
                UIGame uiGame = UIRoot.instance.uiGame;
                if (VFInput.inFullscreenGUI && !myCanvas.activeSelf)
                {
                    return;
                }
                if (!myCanvas.activeSelf)
                {
                    thumb.sprite = LDB.items.Select(2104)._iconSprite;
                }
                myCanvas.SetActive(!myCanvas.activeSelf);
                VFInput.UpdateGameStates();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && myCanvas.activeSelf)
            {
                myCanvas.SetActive(false);
                VFInput.UpdateGameStates();
            }
            
        }

    }

    [HarmonyPatch]
    public class StackSizeEditorPatch
    {

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
