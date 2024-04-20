using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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

    public class UIButtonComponent : UIImageComponent
    {
        public StandardMenuButton button;
    }
}
