using HarmonyLib;
using MTM101BaldAPI.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiLevelEditor.Patches
{
    [HarmonyPatch(typeof(MainMenu))]
    [HarmonyPatch("Start")]
    internal class MainMenuPatch
    {
        static void Postfix(MainMenu __instance)
        {
            Image image = UIHelpers.CreateImage(BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("EditorButton"), __instance.transform, Vector3.zero, false, 1f);
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = new Vector2(60, -88);
            CursorController.Instance.transform.SetAsLastSibling();
            __instance.transform.Find("Bottom").SetAsLastSibling();
            __instance.transform.Find("BlackCover").SetAsLastSibling();
            if (BaldiLevelEditorPlugin.isFucked)
            {
                image.sprite = BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("EditorButtonFail");
                return;
            }
            StandardMenuButton button = image.gameObject.ConvertToButton<StandardMenuButton>();
            button.highlightedSprite = BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("EditorButtonGlow");
            button.unhighlightedSprite = BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("EditorButton");
            button.swapOnHigh = true;
            button.OnPress.AddListener(() =>
            {
                BaldiLevelEditorPlugin.Instance.StartCoroutine(BaldiLevelEditorPlugin.Instance.GoToGame());
            });
        }
    }
}
