using HarmonyLib;
using MTM101BaldAPI;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MTM101BaldAPI.Registers.Buttons;

namespace BaldiLevelEditor
{
    public interface ITileVisual
    {
        public ByteVector2 position { get; }
        public Direction direction { get; }
        public TiledPrefab prefab { set; get; }
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public string highlight { get; set; }
        public void DestroyObject(EditorLevel level);
        public bool DoesExist(EditorLevel level);
    }

    // THIS IS SO FUCKING UGLY PLEASE END ME

    public abstract class TileBasedEditorVisual<T> : MonoBehaviour, ITileVisual where T : TiledPrefab
    {
        public ByteVector2 position => typedPrefab.position;
        public virtual Direction direction => typedPrefab.direction.ToStandard();
        public virtual TiledPrefab prefab { get => typedPrefab; set => typedPrefab = (T)value; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public T typedPrefab;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        protected string _highlight = "none";

        public abstract string highlight { get; set; }

        public abstract void DestroyObject(EditorLevel level);
        public abstract bool DoesExist(EditorLevel level);
    }

    public abstract class TileBasedVisualBase : TileBasedEditorVisual<TiledPrefab>
    {
        MeshRenderer[] meshRenderers = new MeshRenderer[0];
        SpriteRenderer[] spriteRenderers = new SpriteRenderer[0];
        public override string highlight
        {
            get => _highlight;
            set
            {
                if (value != _highlight)
                {
                    meshRenderers.Do(x =>
                    {
                        x.materials.Do(z =>
                        {
                            z.SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                        });
                    });
                    spriteRenderers.Do(x =>
                    {
                        x.materials.Do(z =>
                        {
                            z.SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                        });
                    });
                }
                _highlight = value;
            }
        }

        void Awake()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
        public override void DestroyObject(EditorLevel level)
        {
            level.tiledPrefabs.Remove(prefab);
        }
        public override bool DoesExist(EditorLevel level)
        {
            return level.tiledPrefabs.Contains(prefab);
        }
    }

    public interface IWallVisual
    {
        public abstract void SetupMaterials(MeshRenderer renderer, bool outside);
        public abstract string highlight { get; set; }
        public abstract GameObject gameObject { get; }
        public abstract bool ShouldBeDestroyed(EditorLevel level);
        public abstract TiledPrefab prefab { get; set; }
    }

    public abstract class WallEditorVisual<T> : TileBasedEditorVisual<T>, IWallVisual where T : TiledPrefab
    {
        protected MeshRenderer[] meshRenders = new MeshRenderer[2];
        public GameObject[] wallParts = new GameObject[2];
        public abstract string objectName { get; }

        public override void DestroyObject(EditorLevel level)
        {
            throw new NotImplementedException("Ugly hack so I don't have to rewrite the wall handling code! TODO: REMOVE");
        }
        public override bool DoesExist(EditorLevel level)
        {
            throw new NotImplementedException("Ugly hack so I don't have to rewrite the wall handling code! TODO: REMOVE");
        }

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
                    meshRenders.Do(x =>
                    {
                        x.materials[0].SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                        x.materials[1].SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                    });
                }
                _highlight = value;
            }
        }

        public virtual void Setup(T prefab)
        {
            this.typedPrefab = prefab;
            wallParts[0] = SpawnWallPiece(false);
            meshRenders[0] = wallParts[0].GetComponent<MeshRenderer>();
            wallParts[1] = SpawnWallPiece(true);
            meshRenders[1] = wallParts[1].GetComponent<MeshRenderer>();
        }

        public virtual void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            ByteVector2 positionToGet = position;
            if (outside)
            {
                positionToGet += direction.ToIntVector2().ToByte();
            }

            renderer.materials[0].SetMainTexture(PlusLevelLoaderPlugin.TextureFromAlias(Singleton<PlusLevelEditor>.Instance.level.rooms[Mathf.Max(Singleton<PlusLevelEditor>.Instance.level.GetRoomIDOfPos(positionToGet, true) - 1,0)].textures.wall));
        }

        public virtual GameObject SpawnWallPiece(bool invert)
        {
            GameObject obj = new GameObject();
            obj.name = objectName;
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = BaldiLevelEditorPlugin.Instance.assetMan.Get<Mesh>("Quad");
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.materials = new Material[]
            {
                new Material(BaldiLevelEditorPlugin.tileMaskedShader),
                new Material(BaldiLevelEditorPlugin.tileMaskedShader)
            };
            renderer.materials[0].SetTexture("_LightMap", BaldiLevelEditorPlugin.lightmapTexture);
            renderer.materials[1].SetTexture("_LightMap", BaldiLevelEditorPlugin.lightmapTexture);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = Vector3.up * 5f;
            obj.transform.localPosition += direction.ToVector3() * 5f;
            if (!invert)
            {
                obj.transform.rotation = direction.ToRotation();
            }
            else
            {
                obj.transform.rotation = direction.GetOpposite().ToRotation();
            }
            obj.transform.localScale = new Vector3(10f, 10f, 1f);
            obj.AddComponent<MeshCollider>();
            SetupMaterials(renderer, invert);
            return obj;
        }

        public abstract bool ShouldBeDestroyed(EditorLevel level);
    }

    public class ManualWallEditorVisual : WallEditorVisual<WallPlacement>
    {
        public override string highlight
        {
            get => base.highlight;
            set
            {
                base.highlight = value;
                if (_highlight == "none")
                {
                    meshRenders[0].materials[1].SetTexture("_LightMap", Texture2D.whiteTexture);
                    meshRenders[1].materials[1].SetTexture("_LightMap", Texture2D.whiteTexture);
                }
            }
        }
        public override string objectName => "ManualWall";

        public bool isVisible = true;

        public override bool ShouldBeDestroyed(EditorLevel level)
        {
            return !level.manualWalls.Contains(typedPrefab);
        }

        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            if (isVisible)
            {
                base.SetupMaterials(renderer, outside);
                renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("BorderMask"));
                renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Border"));
            }
            else
            {
                renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("CrossMask"));
                renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Cross"));
            }
        }
    }

    public class DoorEditorVisual : WallEditorVisual<DoorLocation>
    {
        public ushort roomId => typedPrefab.roomId;

        public override string objectName => "Door";

        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("DoorMask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("DoorTexture_Closed"));
        }

        public override bool ShouldBeDestroyed(EditorLevel level)
        {
            return !level.doors.Contains(typedPrefab);
        }
    }

    public class ButtonEditorVisual : WallEditorVisual<EditorButtonPlacement>
    {
        private MeshRenderer? hackRenderer;
        public override string objectName => "ButtonVisual";

        public override string highlight 
        { 
            get => base.highlight; 
            set
            {
                base.highlight = value;
                if (hackRenderer == null) return;
                hackRenderer.material.SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
            }
        }

        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            if (outside)
            {
                renderer.gameObject.SetActive(false);
                return;
            }
            base.SetupMaterials(renderer, outside);
            //renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("DoorMask"));
            //renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("DoorTexture_Closed"));
            MeshRenderer clone = GameObject.Instantiate<MeshRenderer>(renderer, renderer.transform.parent);
            clone.name = "ButtonOverlay";
            //clone.transform.position += (clone.transform.forward *= 0.025f);
            hackRenderer = clone;
            hackRenderer.materials = new Material[] { ButtonColorManager.buttonColors["Red"].buttonUnpressed };
            Destroy(clone.GetComponent<MeshCollider>());
        }

        public override bool ShouldBeDestroyed(EditorLevel level)
        {
            return !level.editorButtons.Contains(typedPrefab);
        }
    }

    public class SwingEditorVisual : DoorEditorVisual
    {
        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoorMask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoor0"));
        }
    }

    public class SilentSwingEditorVisual : DoorEditorVisual
    {
        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoorMask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoorSilent"));
        }
    }

    public class CoinSwingEditorVisual : DoorEditorVisual
    {
        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoorMask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("CoinDoor"));
        }
    }
    public class AutoDoorEditorVisual : DoorEditorVisual
    {
        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("AutoDoor_Mask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("AutoDoor_Unlocked_Closed"));
        }
    }
    public class OneWaySwingEditorVisual : DoorEditorVisual
    {
        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("SwingDoorMask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>((!outside) ? "SwingDoor0Right" : "SwingDoor0Wrong"));
        }
    }

    public class WindowEditorVisual : WallEditorVisual<WindowLocation>
    {
        public override string objectName => "Window";

        public override void SetupMaterials(MeshRenderer renderer, bool outside)
        {
            base.SetupMaterials(renderer, outside);
            renderer.materials[0].SetMaskTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("WindowMask"));
            renderer.materials[1].SetMainTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("WindowTexture"));
        }

        public override bool ShouldBeDestroyed(EditorLevel level)
        {
            return !level.windows.Contains(typedPrefab);
        }
    }
}
