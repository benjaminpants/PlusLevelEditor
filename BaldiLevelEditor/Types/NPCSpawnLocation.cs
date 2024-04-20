using HarmonyLib;
using MTM101BaldAPI;
using PlusLevelFormat;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
    public class NPCSpawnLocation : TileBasedEditorVisual<NPCLocation>
    {
        SpriteRenderer[] renderers = new SpriteRenderer[0];

        public override string highlight
        {
            get
            {
                return _highlight;
            }
            set
            {
                if (value != _highlight)
                {
                    renderers.Do(x =>
                    {
                        x.materials[0].SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                    });
                }
                _highlight = value;
            }
        }

        public override void DestroyObject(EditorLevel level)
        {
            level.npcSpawns.Remove(typedPrefab);
        }

        public override bool DoesExist(EditorLevel level)
        {
            return level.npcSpawns.Contains(typedPrefab);
        }

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void Start()
        {
            GameObject obj = new GameObject();
            obj.name = "NPCCollider";
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = BaldiLevelEditorPlugin.Instance.assetMan.Get<Mesh>("Quad");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.up * 0.25f;
            obj.transform.localScale = new Vector3(10f, 10f, 1f);
            obj.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            obj.AddComponent<MeshCollider>();
        }
    }
}
