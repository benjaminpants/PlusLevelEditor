using BaldiLevelEditor.UI;
using MTM101BaldAPI.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiLevelEditor
{
    public partial class PlusLevelEditor : Singleton<PlusLevelEditor>
    {

        public GameObject currentMenuBackground;

        void InitializeMenuBackground()
        {
            currentMenuBackground = new GameObject();
            currentMenuBackground.name = "MenuBackground";
            currentMenuBackground.transform.SetParent(canvas.transform, false);
            RectTransform transform = currentMenuBackground.AddComponent<RectTransform>();
            //transform.localScale *= 0.5f;
            transform.pivot = Vector3.up;
            transform.anchorMin = Vector3.up;
            transform.anchorMax = Vector3.up;
            transform.anchoredPosition = Vector3.zero;
            transform.sizeDelta = canvas.GetComponent<RectTransform>().sizeDelta;
            Image image = currentMenuBackground.AddComponent<Image>();
            image.sprite = GetUISprite("DitherPattern");
            image.type = Image.Type.Tiled;
            /*Image image = UIHelpers.CreateImage(GetUISprite("DitherPattern"), transform, Vector3.zero, false);
            image.gameObject.transform.SetParent(transform,false);
            image.rectTransform.anchorMin = Vector2.up;
            image.rectTransform.anchorMax = Vector2.up;
            image.rectTransform.pivot = Vector2.up;
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.gameObject.transform.localRotation = Quaternion.identity;
            image.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
            image.type = Image.Type.Tiled;*/
            currentMenuBackground.SetActive(false);
        }

        public void SwitchToMenu(UIMenu menu, object? targetObject = null)
        {
            state = LevelEditorState.InMenu;
            currentMenuBackground.SetActive(true);
            menu.ToComponents(currentMenuBackground.transform, new Vector2(480,360)/*canvas.gameObject.GetComponent<RectTransform>().sizeDelta*/);
        }
    }
}
