using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
    public enum SelectorType
    {
        None,
        Tile,
        Area,
        AreaDragging,
        HoldingItem,
        ItemSelectDirection,
        PrefabSelect,
        PrefabRotate
    }

    public class EditorSelector : MonoBehaviour
    {
        public bool inItemHoldingState => (type == SelectorType.ItemSelectDirection) || (type == SelectorType.HoldingItem);
        public GameObject main;
        public GameObject[] arrows = new GameObject[4];
        public GameObject[] prefabArrows = new GameObject[6];
        public GameObject[] prefabRotations = new GameObject[1];
        private SelectorType _type = SelectorType.Tile;

        public static Vector3[] rotationVectors = new Vector3[]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back,
        };
        public SelectorType type { 
            get
            {
                return _type;
            }
            set 
            {
                /*if (value != _type)
                {
                    Debug.Log("Switching to: " + value.ToString());
                }*/
                _type = value;
                main.SetActive(true);
                prefabRotations[0].SetActive(false);
                for (int i = 0; i < arrows.Length; i++)
                {
                    arrows[i].SetActive(true);
                }
                for (int i = 0; i < prefabArrows.Length; i++)
                {
                    prefabArrows[i].SetActive(false);
                }
                switch (_type)
                {
                    case SelectorType.PrefabRotate:
                        main.SetActive(false);
                        for (int i = 0; i < arrows.Length; i++)
                        {
                            arrows[i].SetActive(false);
                        }
                        for (int i = 0; i < prefabArrows.Length; i++)
                        {
                            prefabArrows[i].SetActive(false);
                        }
                        prefabRotations[0].SetActive(true);
                        break;
                    case SelectorType.ItemSelectDirection:
                    case SelectorType.Tile:
                        List<Direction> directions = Directions.All();
                        for (int i = 0; i < directions.Count; i++)
                        {
                            arrows[i].transform.localPosition = Vector3.up * 0.25f;
                            arrows[i].transform.localPosition += directions[i].ToVector3() * 10f;
                        }
                        break;
                    case SelectorType.PrefabSelect:
                        main.SetActive(false);
                        for (int i = 0; i < arrows.Length; i++)
                        {
                            arrows[i].SetActive(false);
                        }
                        for (int i = 0; i < prefabArrows.Length; i++)
                        {
                            prefabArrows[i].SetActive(true);
                        }
                        prefabRotations[0].SetActive(true);
                        //prefabRotations[0].transform.localPosition = Vector3.forward * 9f;
                        break;
                    case SelectorType.None:
                        main.SetActive(false);
                        for (int i = 0; i < arrows.Length; i++)
                        {
                            arrows[i].SetActive(false);
                        }
                        break;
                    case SelectorType.HoldingItem:
                        main.SetActive(true);
                        for (int i = 0; i < arrows.Length; i++)
                        {
                            arrows[i].SetActive(false);
                        }
                        break;
                }
            } 
        }
        void Start()
        {
            Initialize();
        }

        GameObject CreateGraphic(Texture2D texture, string name)
        {
            GameObject obj = new GameObject();
            obj.name = name;
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = BaldiLevelEditorPlugin.Instance.assetMan.Get<Mesh>("Quad");
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = new Material(BaldiLevelEditorPlugin.Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard_AlphaClip"));
            renderer.material.SetTexture("_LightMap", Texture2D.whiteTexture);
            renderer.material.SetMainTexture(texture);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.up * 0.25f;
            obj.transform.localScale = new Vector3(10f, 10f, 1f);
            obj.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            return obj;
        }

        GameObject CreateHandle()
        {
            Texture2D arrowTex = BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("ArrowSmall");
            GameObject handle = new GameObject();
            for (int i = 0; i < 4; i++)
            {
                GameObject subHandle = CreateGraphic(arrowTex, i.ToString());
                subHandle.transform.eulerAngles = new Vector3(0f, i * 90f, 0f);
                subHandle.transform.SetParent(handle.transform, false);
            }
            handle.transform.localScale *= 0.4f;
            return handle;
        }

        void Initialize()
        {
            main = CreateGraphic(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Selector"), "Selector");
            List<Direction> directions = Directions.All();
            for (int i = 0; i < directions.Count; i++)
            {
                arrows[i] = CreateGraphic(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Arrow"), "Handle_" + directions[i].ToString());
                arrows[i].transform.localPosition += directions[i].ToVector3()*10f;
                arrows[i].transform.eulerAngles += new Vector3(0f, directions[i].ToDegrees(), 0f);
                arrows[i].AddComponent<MeshCollider>();
            }
            // this code is hacky
            Vector3[] rotationRots = new Vector3[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.left,
            };
            string[] rotationNames = new string[]
            {
                "Up",
                "Down",
                "Left",
                "Right",
                "Forward",
                "Backward"
            };
            for (int i = 0; i < 6; i++)
            {
                prefabArrows[i] = CreateHandle();
                prefabArrows[i].name = "FullHandle_" + rotationNames[i];
                prefabArrows[i].transform.SetParent(transform, false);
                prefabArrows[i].transform.localPosition += rotationVectors[i] * 5f;
                prefabArrows[i].transform.Rotate(rotationRots[i],90f);
                BoxCollider bc = prefabArrows[i].AddComponent<BoxCollider>();
                bc.size = new Vector3(2.5f,6f,2.5f);
            }
            prefabArrows[1].transform.Rotate(Vector3.forward, 180f);
            prefabRotations[0] = new GameObject();
            prefabRotations[0].transform.SetParent(transform,false);
            GameObject circle = CreateGraphic(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Circle"), "Circle");
            circle.transform.SetParent(prefabRotations[0].transform);
            circle.AddComponent<MeshCollider>();
            prefabRotations[0].transform.localScale *= 0.1f;
            type = SelectorType.None;
        }
    }
}
