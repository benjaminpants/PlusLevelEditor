using MTM101BaldAPI;
using MTM101BaldAPI.UI;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiLevelEditor.UI
{

    [Flags]
    public enum NextDirection : byte
    {
        DontMove=0,
        Left=1,
        Up=2,
        Right=4,
        Down=8,
        ResetX=16,
        ResetY=32,
        ResetBoth=48
    }

    public abstract class UIComponent : MonoBehaviour
    {
        public RectTransform rectTransform => transform.parent.GetComponent<RectTransform>();

        public UIMenuMono parentBehavior;

        public static T CreateBase<T>() where T : UIComponent
        {
            GameObject obj = new GameObject();
            obj.SetActive(false);
            obj.name = "UIPrefab_" + typeof(T).Name;
            obj.layer = LayerMask.NameToLayer("UI");
            obj.ConvertToPrefab(false);
            return obj.AddComponent<T>();
        }

        void Awake()
        {
            VirtualAwake();
        }

        void Update()
        {
            VirtualUpdate();
        }

        protected virtual void VirtualAwake()
        {

        }

        protected virtual void VirtualUpdate()
        {

        }
    }

    public class TextureUIElement : UIElement
    {
        public Texture2D texture;
        public float scale;

        public float rawWidth => texture.width;

        public float rawHeight => texture.height;

        public override float width => rawWidth * scale;

        public override float height => rawHeight * scale;

        public override UIComponent componentPrefab => BaldiLevelEditorPlugin.Instance.assetMan.Get<UIComponent>("texture");

        public TextureUIElement(Texture2D texture, float scale)
        {
            this.texture = texture;
            this.scale = scale;
        }

        public override UIComponent ToComponent(RectTransform parent)
        {
            UITextureComponent component = (UITextureComponent)base.ToComponent(parent);
            component.texture = texture;
            component.gameObject.transform.localScale *= scale;
            return component;
        }
    }

    public class ImageUIElement : UIElement
    {
        public override float width => sprite.textureRect.width;

        public override float height => sprite.textureRect.height;

        public Sprite sprite;

        public override UIComponent componentPrefab => BaldiLevelEditorPlugin.Instance.assetMan.Get<UIComponent>("image");

        public ImageUIElement(Sprite sprite) 
        {
            this.sprite = sprite;
        }

        public override UIComponent ToComponent(RectTransform parent)
        {
            UIComponent component = base.ToComponent(parent);
            ((UIImageComponent)component).sprite = sprite;
            return component;
        }
    }

    public class ClipboardUIElement : ImageUIElement
    {
        public override float shiftX => 63;
        public override float shiftY => 55; //everywhere i go it haunts me
        public ClipboardUIElement() : base(BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("clipboard"))
        {
        }
    }

    public class LabelUIElement : UIElement
    {

        public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
        public BaldiFonts font;
        public string text;
        private Vector2 _size;
        public Color color;

        public LabelUIElement(Vector2 size, string text, Color? color = null, BaldiFonts font = BaldiFonts.ComicSans12, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            this.text = text;
            this.font = font;
            this.color = color == null ? Color.black : color.Value;
            this.alignment = alignment;
            _size = size;
        }

        public override UIComponent ToComponent(RectTransform parent)
        {
            UILabelComponent component = (UILabelComponent)base.ToComponent(parent);
            component.font = font;
            component.text = text;
            component.color = color;
            component.alignment = alignment;
            return component;
        }

        public override float width => _size.x;

        public override float height => _size.y;

        public override UIComponent componentPrefab => BaldiLevelEditorPlugin.Instance.assetMan.Get<UILabelComponent>("label");
    }

    public class ButtonUIElement : ImageUIElement
    {
        Sprite highlightSprite;
        Action<StandardMenuButton> onClick;
        public ButtonUIElement(Sprite sprite, Action<StandardMenuButton> onClick) : base(sprite)
        {
            highlightSprite = sprite;
            this.onClick = onClick;
        }

        public ButtonUIElement(Sprite sprite, Sprite highlightSprite, Action<StandardMenuButton> onClick) : base(sprite)
        {
            this.highlightSprite = highlightSprite;
            this.onClick = onClick;
        }

        public override UIComponent ToComponent(RectTransform parent)
        {
            UIButtonComponent b = (UIButtonComponent)base.ToComponent(parent);
            b.button.swapOnHigh = true;
            b.button.highlightedSprite = highlightSprite;
            b.button.unhighlightedSprite = sprite;
            (b).button.OnPress.AddListener(() =>
            {
                onClick.Invoke(b.GetComponent<StandardMenuButton>());
            });
            return b;
        }

        public override UIComponent componentPrefab => BaldiLevelEditorPlugin.Instance.assetMan.Get<UIComponent>("button");
    }

    public abstract class UIElement
    {
        public abstract float width { get; }
        public abstract float height { get; }
        public virtual float shiftX => width;
        public virtual float shiftY => height;

        public abstract UIComponent componentPrefab { get; }

        public virtual UIComponent ToComponent(RectTransform parent)
        {
            RectTransform rectTransform = new GameObject().AddComponent<RectTransform>();
            rectTransform.transform.SetParent(parent,false);
            rectTransform.gameObject.layer = LayerMask.NameToLayer("UI");
            rectTransform.pivot = Vector3.up;
            rectTransform.anchorMax = Vector3.up;
            rectTransform.anchorMin = Vector3.up;
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.name = componentPrefab.name + "_Rect";
            return GameObject.Instantiate<UIComponent>(componentPrefab, rectTransform);
        }
    }

    public struct MenuElement
    {
        public NextDirection direction;
        public UIElement element;
        public MenuElement(NextDirection direction, UIElement element)
        {
            this.element = element;
            this.direction = direction;
        }
    }

    public class UIMenu
    {
        public MenuElement[] elements = new MenuElement[0];

        public Type componentToAdd = typeof(UIMenuMono);

        public RectTransform ToComponents(Transform parent, Vector2 sizeDelta, object? targetObject = null)
        {
            Vector2 virtualPosition = Vector2.zero;
            RectTransform transform = new GameObject().AddComponent<RectTransform>();
            transform.pivot = Vector3.one / 2;
            transform.anchorMax = Vector3.one / 2;
            transform.anchorMin = Vector3.one / 2;
            transform.transform.localPosition = Vector2.zero;
            transform.sizeDelta = sizeDelta;
            transform.gameObject.layer = LayerMask.NameToLayer("UI");
            transform.SetParent(parent, false);
            transform.name = "UIMenuRect";
            UIMenuMono behavior = (UIMenuMono)transform.gameObject.AddComponent(componentToAdd);
            behavior.targetObject = targetObject;
            for (int i = 0; i < elements.Length; i++)
            {
                UIComponent component = elements[i].element.ToComponent(transform);
                component.rectTransform.anchoredPosition = virtualPosition;
                component.parentBehavior = behavior;
                component.gameObject.SetActive(true);
                if (elements[i].direction == NextDirection.DontMove) continue;
                if (elements[i].direction.HasFlag(NextDirection.Up))
                {
                    virtualPosition += Vector2.up * elements[i].element.shiftY;
                }
                if (elements[i].direction.HasFlag(NextDirection.Down))
                {
                    virtualPosition += Vector2.down * elements[i].element.shiftY;
                }
                if (elements[i].direction.HasFlag(NextDirection.Left))
                {
                    virtualPosition += Vector2.left * elements[i].element.shiftX;
                }
                if (elements[i].direction.HasFlag(NextDirection.Right))
                {
                    virtualPosition += Vector2.right * elements[i].element.shiftX;
                }
                if (elements[i].direction.HasFlag(NextDirection.ResetX))
                {
                    virtualPosition.Scale(new Vector2(0f,1f));
                }
                if (elements[i].direction.HasFlag(NextDirection.ResetY))
                {
                    virtualPosition.Scale(new Vector2(1f, 0f));
                }
            }
            return transform;
        }
    }
    public class UIMenuBuilder
    {
        List<MenuElement> menuElements = new List<MenuElement>();

        Type behavior = typeof(UIMenuMono);

        public UIMenuBuilder AddElement(UIElement element, NextDirection nextdirection)
        {
            menuElements.Add(new MenuElement(nextdirection, element));
            return this;
        }

        public UIMenuBuilder AddClipboard()
        {
            menuElements.Add(new MenuElement(NextDirection.Right | NextDirection.Down, new ClipboardUIElement()));
            return this;
        }

        public UIMenuBuilder AddImage(Sprite sprite, NextDirection nextdirection)
        {
            menuElements.Add(new MenuElement(nextdirection, new ImageUIElement(sprite)));
            return this;
        }

        public UIMenuBuilder AddLabel(float sizeX, float sizeY, string text, NextDirection nextdirection)
        {
            menuElements.Add(new MenuElement(nextdirection, new LabelUIElement(new Vector2(sizeX, sizeY), text)));
            return this;
        }

        public UIMenuBuilder AddTexture(Texture2D texture, float scale, NextDirection nextdirection)
        {
            menuElements.Add(new MenuElement(nextdirection, new TextureUIElement(texture, scale)));
            return this;
        }

        public UIMenuBuilder AddButton(Sprite sprite, Action<StandardMenuButton> action, NextDirection nextdirection)
        {
            menuElements.Add(new MenuElement(nextdirection, new ButtonUIElement(sprite, action)));
            return this;
        }

        public UIMenuBuilder AddButton(Sprite sprite, Sprite highlightSprite, Action<StandardMenuButton> action, NextDirection nextdirection)
        {
            menuElements.Add(new MenuElement(nextdirection, new ButtonUIElement(sprite, highlightSprite, action)));
            return this;
        }

        public UIMenuBuilder SetComponent<T>() where T : UIMenuMono
        {
            behavior = typeof(T);
            return this;
        }

        public UIMenu Build()
        {
            return new UIMenu()
            {
                elements = menuElements.ToArray(),
                componentToAdd = behavior
            };
        }
    }
}
