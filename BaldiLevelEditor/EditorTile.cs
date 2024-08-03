using MTM101BaldAPI;
using PlusLevelFormat;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
    public class EditorTile : MonoBehaviour
    {
        public ByteVector2 position;
        public PlusLevelFormat.Tile edTile => Singleton<PlusLevelEditor>.Instance.level.tiles[position.x,position.y];

        string _highlight = "none";

        public string highlight
        {
            get
            {
                return _highlight;
            }
            set
            {
                if (value != _highlight)
                {
                    _tile.MeshRenderer.material.SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                }
                _highlight = value;
            }
        }

        private Tile _tile;

        void Start()
        {
            _tile = GameObject.Instantiate<Tile>(BaldiLevelEditorPlugin.Instance.tilePrefab, this.transform);
            GameObject clone = GameObject.Instantiate(_tile.Collider(Direction.North), transform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            clone.name = "FloorCollider";
            clone.SetActive(true);
            UpdateMesh();
            UpdateTexture();
            // create the bottom thingy for displaying the grid
            GameObject obj = new GameObject();
            obj.name = name;
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = BaldiLevelEditorPlugin.Instance.assetMan.Get<Mesh>("Quad");
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = new Material(BaldiLevelEditorPlugin.tileStandardShader);
            renderer.material.SetTexture("_LightMap", Texture2D.whiteTexture);
            renderer.material.SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Grid"));
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.down * 0.1f;
            obj.transform.localScale = new Vector3(10f, 10f, 1f);
            obj.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        }

        public void UpdateTexture()
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            ushort id = instance.level.GetRoomIDOfPos(position, true);
            if (id == 0) return;
            _tile.MeshRenderer.material.SetMainTexture(instance.GenerateTextureAtlas(instance.level.rooms[id - 1].textures));
        }

        public void UpdateMesh()
        {
            if (edTile.type == 16)
            {
                for (int i = 0; i < 4; i++)
                {
                    _tile.Collider((Direction)i).SetActive(false);
                }
                _tile.gameObject.SetActive(false);
                return;
            }
            _tile.gameObject.SetActive(true);
            _tile.MeshFilter.sharedMesh = Singleton<PlusLevelEditor>.Instance.puppetEnvironmentController.TileMesh(edTile.type);
            for (int i = 0; i < 4; i++)
            {
                _tile.Collider((Direction)i).SetActive(edTile.type.ContainsDirection((Direction)i));
            }
        }
    }
}
