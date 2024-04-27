using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MTM101BaldAPI.UI;
using System.Collections;

namespace BaldiLevelEditor.UI
{
    public class UITextureComponent : UIComponent
    {
        public Texture texture;
        public RawImage image;

        protected override void VirtualAwake()
        {
            image = GetComponent<RawImage>();
            image.texture = texture;
            image.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        }
    }

    public class UIImageComponent : UIComponent
    {
        public Sprite sprite;
        public Image image;

        protected override void VirtualAwake()
        {
            image = GetComponent<Image>();
            image.sprite = sprite;
            image.rectTransform.sizeDelta = new Vector2(sprite.textureRect.width, sprite.textureRect.height);
        }
    }

    public class UILabelComponent : UIComponent
    {
        public TextMeshProUGUI tmp;
        public string text;
        public BaldiFonts font;
        public TextAlignmentOptions alignment;
        public Color color;
        protected override void VirtualAwake()
        {
            //tmp = UIHelpers.CreateText<TextMeshProUGUI>(font, text, transform.parent, Vector3.zero, false);
            tmp = GetComponent<TextMeshProUGUI>();
            tmp.font = font.FontAsset();
            tmp.fontSize = font.FontSize();
            tmp.alignment = alignment;
            tmp.transform.localPosition = Vector3.zero;
            tmp.rectTransform.sizeDelta = rectTransform.sizeDelta;
            tmp.color = color;
            tmp.text = text;
        }
    }

    public class UIButtonComponent : UIImageComponent
    {
        public StandardMenuButton button;
    }
}
