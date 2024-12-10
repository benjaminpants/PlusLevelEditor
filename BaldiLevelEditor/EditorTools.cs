using HarmonyLib;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace BaldiLevelEditor
{
    public abstract class EditorTool
    {
        public abstract Sprite editorSprite { get; }
        public Transform hitTransform;
        public abstract void OnHover(IntVector2 vector);
        public abstract void OnDrop(IntVector2 vector);
        public virtual void Reset()
        {
            hitTransform = null;
        }
        public virtual void OnEscape()
        {
            Reset();
            Singleton<PlusLevelEditor>.Instance.SelectTool(null);
        }
        public virtual void OnDirectionSelect(Direction dir, bool clicked)
        {

        }

        protected bool IsOutOfBounds(IntVector2 pos)
        {
            if (pos.x < 0) return true;
            if (pos.z < 0) return true;
            if (pos.x >= Singleton<PlusLevelEditor>.Instance.level.width) return true;
            if (pos.z >= Singleton<PlusLevelEditor>.Instance.level.width) return true;
            return false;
        }

        protected TiledArea GetAreaAtPos(IntVector2 pos)
        {
            if (IsOutOfBounds(pos)) return null;
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            for (int i = 0; i < instance.level.areas.Count; i++)
            {
                if (instance.level.areas[i].VectorIsInArea(pos.ToByte()))
                {
                    return instance.level.areas[i];
                }
            }
            return null;
        }

        protected void ClearHighlights()
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            instance.edTiles.ConvertTo1d(instance.level.width, instance.level.height).Do(x =>
            {
                x.highlight = "none";
            });
            instance.wallVisuals.Do(x => x.highlight = "none");
            instance.prefabVisuals.Do(x => x.highlight = "none");
            instance.tiledVisuals.Do(x => x.highlight = "none");
        }

        protected void HighlightTiles(ByteVector2[] positions, string highlight)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                Singleton<PlusLevelEditor>.Instance.edTiles[positions[i].x, positions[i].y].highlight = highlight;
            }
        }

        protected bool HasArea(IntVector2 pos)
        {
            if (IsOutOfBounds(pos)) return false;
            return Singleton<PlusLevelEditor>.Instance.level.GetRoomIDOfPos(pos.ToByte(), true) != 0;
        }
    }

    public class DisabledEditorTool : EditorTool
    {
        public string _sprite;
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/" + _sprite);

        public DisabledEditorTool(string sprite)
        {
            _sprite = sprite;
        }

        public override void OnDrop(IntVector2 vector)
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }

        public override void OnHover(IntVector2 vector)
        {
            
        }
    }

    public class PrebuiltStructureTool : PlaceAndRotateToolBase
    {
        string _sprite;
        EditorPrebuiltStucture _structure;
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/helper_" + _sprite);
        public PrebuiltStructureTool(string sprite, EditorPrebuiltStucture structure)
        {
            _sprite = sprite;
            _structure = structure;
        }

        public override bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            return true;
        }

        IEnumerator SpawnObjects(PlusLevelEditor instance, Direction dir, ByteVector2 position)
        {
            for (int i = 0; i < _structure.prefabs.Count; i++)
            {
                PrefabLocation prefab = _structure.prefabs[i].GetNew();
                // NEW SPECIAL MOVE: UGLY HACK!
                Vector3 oldVec = prefab.position.ToUnity();
                prefab.position = instance.ByteVectorToWorld(position).ToData();
                EditorPrefab obj = instance.AddPrefab(prefab);
                obj.transform.position = oldVec;
                obj.transform.position += instance.ByteVectorToWorld(position);
                obj.transform.RotateAround(instance.ByteVectorToWorld(position) + _structure.origin, UnityEngine.Vector3.up, dir.ToDegrees());
                obj.UpdateObject();
                obj.DoneUpdateObject();
                yield return new WaitForSeconds(0.05f);
            }
            yield break;
        }

        public override void OnPlace(Direction dir)
        {
            if (!selectedPosition.HasValue) throw new InvalidOperationException();
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            instance.StartCoroutine(SpawnObjects(instance, dir, selectedPosition.Value));
            instance.updateDelay = _structure.prefabs.Count * 0.05f;
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("NotebookCollect"));
            Singleton<PlusLevelEditor>.Instance.SelectTool(null);
        }

        public override void OnPlacementFail()
        {
            
        }

        public override void OnSwitchToRotation()
        {
            
        }
    }
    public class DoorTool : PlaceAndRotateToolBase
    {
        protected string _type;

        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/DoorED");

        public DoorTool(string type)
        {
            _type = type;
        }

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardOpen"));
        }

        public override void OnPlace(Direction dir)
        {
            Singleton<PlusLevelEditor>.Instance.AddDoor(new DoorLocation()
            {
                type = _type,
                position = selectedPosition.Value,
                direction = dir.ToData(),
                roomId = Singleton<PlusLevelEditor>.Instance.level.GetRoomIDOfPos(selectedPosition.Value, true)
            }, BaldiLevelEditorPlugin.doorTypes[_type]);
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardShut"));
        }

        public override void OnPlacementFail()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }
    }
    public class SwingingDoorTool : DoorTool
    {
        public SwingingDoorTool(string type) : base(type)
        {
        }

        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/" + _type + "_SwingDoorED");

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_Swinging"));
        }
    }

    public abstract class PlaceAndRotateToolBase : EditorTool
    {
        public override Sprite editorSprite => throw new NotImplementedException();
        public ByteVector2? selectedPosition;

        public abstract void OnSwitchToRotation();
        public abstract void OnPlace(Direction dir);
        public virtual bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            List<TiledPrefab> tiledPrefab = new List<TiledPrefab>();
            tiledPrefab.AddRange(instance.level.doors);
            tiledPrefab.AddRange(instance.level.manualWalls);
            tiledPrefab.AddRange(instance.level.windows);
            tiledPrefab.AddRange(instance.level.editorButtons);

            for (int i = 0; i < tiledPrefab.Count; i++)
            {
                if ((tiledPrefab[i].position == position) && ((tiledPrefab[i].direction.ToStandard() == dir)))
                {
                    return false;
                }
                if (((tiledPrefab[i].position.ToInt() + tiledPrefab[i].direction.ToStandard().ToIntVector2()) == position.ToInt()) && ((tiledPrefab[i].direction.ToStandard().GetOpposite() == dir)))
                {
                    return false;
                }
            }
            return true;
        }
        public abstract void OnPlacementFail();

        public override void OnDirectionSelect(Direction dir, bool clicked)
        {
            if (selectedPosition == null) { OnPlacementFail(); return; }//throw new Exception("selectedPosition is null!");
            if (clicked)
            {
                if (IsValidPlacement(selectedPosition.Value, dir))
                {
                    OnPlace(dir);
                    Reset();
                    Singleton<PlusLevelEditor>.Instance.RefreshLevel(true);
                    Singleton<PlusLevelEditor>.Instance.SelectTool(null);
                }
                else
                {
                    OnPlacementFail();
                }
            }
        }

        public virtual bool IsVectorValidSpot(IntVector2 vector)
        {
            if (IsOutOfBounds(vector)) return false;
            if (!HasArea(vector)) return false;
            return true;
        }

        public override void OnDrop(IntVector2 vector)
        {
            if (!IsVectorValidSpot(vector)) return;
            if (selectedPosition == null)
            {
                selectedPosition = vector.ToByte();
                Singleton<PlusLevelEditor>.Instance.selector.type = SelectorType.ItemSelectDirection;
                OnSwitchToRotation();
            }
            else
            {
                Singleton<PlusLevelEditor>.Instance.SelectTool(null);
            }
        }

        public override void OnHover(IntVector2 vector)
        {
            ClearHighlights();
            if (selectedPosition != null)
            {
                HighlightTiles(new ByteVector2[] { selectedPosition.Value }, "yellow");
                return;
            }
            if (IsOutOfBounds(vector)) return;
            if (!HasArea(vector)) return;
            HighlightTiles(new ByteVector2[] { vector.ToByte() }, "yellow");
        }

        public override void Reset()
        {
            selectedPosition = null;
        }

        public override void OnEscape()
        {
            if (selectedPosition != null)
            {
                Reset();
                Singleton<PlusLevelEditor>.Instance.SelectTool(this);
                return;
            }
            base.OnEscape();
        }
    }

    public class WindowTool : PlaceAndRotateToolBase
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/WindowED");

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Scissors"));
        }

        public override void OnPlace(Direction dir)
        {
            Singleton<PlusLevelEditor>.Instance.AddWindow(new WindowLocation()
            {
                type = "standard",
                position = selectedPosition.Value,
                direction = dir.ToData(),
            });
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("DrR_Hammer"));
        }

        public override void OnPlacementFail()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }
    }

    public class WallTool : PlaceAndRotateToolBase
    {
        private bool _active;
        public WallTool(bool active)
        {
            _active = active;
        }

        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>(!_active ? "UI/WallEdNo" : "UI/WallEd");

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("ErrorMaybe"));
        }

        public override void OnPlace(Direction dir)
        {
            Singleton<PlusLevelEditor>.Instance.AddWall(new WallPlacement()
            {
                position = selectedPosition.Value,
                direction = dir.ToData(),
                wall= _active
            });
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Slap"));
        }

        public override void OnPlacementFail()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }
    }

    public class ButtonTool : PlaceAndRotateToolBase
    {
        private string _type;
        public ButtonTool(string type)
        {
            _type = type;
        }

        public override bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            List<TiledPrefab> tiledPrefab = new List<TiledPrefab>();
            tiledPrefab.AddRange(instance.level.doors);
            tiledPrefab.AddRange(instance.level.manualWalls);
            tiledPrefab.AddRange(instance.level.windows);

            for (int i = 0; i < tiledPrefab.Count; i++)
            {
                if ((tiledPrefab[i].position == position) && ((tiledPrefab[i].direction.ToStandard() == dir)))
                {
                    return false;
                }
                if (((tiledPrefab[i].position.ToInt() + tiledPrefab[i].direction.ToStandard().ToIntVector2()) == position.ToInt()) && ((tiledPrefab[i].direction.ToStandard().GetOpposite() == dir)))
                {
                    return false;
                }
            }
            for (int i = 0; i < instance.level.editorButtons.Count; i++)
            {
                if ((instance.level.editorButtons[i].position == position) && ((instance.level.editorButtons[i].direction.ToStandard() == dir)))
                {
                    return false;
                }
            }
            return true;
        }

        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Button_" + _type);

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("ErrorMaybe"));
        }

        public override void OnPlace(Direction dir)
        {
            Singleton<PlusLevelEditor>.Instance.AddButton(new EditorButtonPlacement()
            {
                position = selectedPosition.Value,
                direction = dir.ToData(),
                type = _type
            });
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Slap"));
        }

        public override void OnPlacementFail()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }
    }

    public class FloorTool : EditorTool
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Floor_" + roomType);

        private string roomType;

        public FloorTool(string room)
        {
            roomType = room;
        }

        public override void OnDrop(IntVector2 vector)
        {
            if (IsOutOfBounds(vector)) return;
            if (HasArea(vector)) return;
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GrappleClang"));
            RoomProperties room;
            if (roomType == "hall")
            {
                room = instance.level.hallRoom;
            }
            else
            {
                room = new RoomProperties(roomType);
                room.textures = new TextureContainer(instance.level.defaultTextures[roomType]);
            }
            ushort id = instance.level.AddRoom(room);
            instance.level.areas.Add(new AreaData(vector.ToByte(), ByteVector2.one, id));
            instance.RefreshLevel();
            instance.SelectTool(null);
        }

        public override void OnHover(IntVector2 vector)
        {
            
        }
    }

    public class NpcTool : EditorTool
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/NPC_" + _prefab);

        private string _prefab;

        public NpcTool(string prefab)
        {
            _prefab = prefab;
        }

        public override void OnDrop(IntVector2 vector)
        {
            if (IsOutOfBounds(vector)) return;
            if (!HasArea(vector)) return;
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("CashBell"));
            instance.AddNPC(new NPCLocation()
            {
                position = vector.ToByte(),
                type = _prefab,
            });
            instance.RefreshLevel(false);
            instance.SelectTool(null);
        }

        public override void OnHover(IntVector2 vector)
        {

        }
    }

    public class ActivityTool : PlaceAndRotateToolBase
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Activity_" + _activity);

        string _activity;

        public ActivityTool(string activity)
        {
            _activity = activity;
        }

        public override void OnPlace(Direction dir)
        {
            if (IsOutOfBounds(selectedPosition.Value.ToInt())) return;
            if (!HasArea(selectedPosition.Value.ToInt())) return;
            EditorObjectType type = BaldiLevelEditorPlugin.editorActivities.Find(x => x.name == _activity);
            Singleton<PlusLevelEditor>.Instance.AddActivity(new RoomActivity()
            {
                activity = _activity,
                position = (Singleton<PlusLevelEditor>.Instance.ByteVectorToWorld(selectedPosition.Value) + type.offset).ToData(),
                direction = dir.ToData()
            }, GetAreaAtPos(selectedPosition.Value.ToInt()).roomId);
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Slap"));
            Singleton<PlusLevelEditor>.Instance.SelectTool(null);
        }

        public override void OnPlacementFail()
        {
            //throw new NotImplementedException();
        }

        public override bool IsVectorValidSpot(IntVector2 vector)
        {
            TiledArea ta = GetAreaAtPos(vector);
            if (ta == null) return false;
            return base.IsVectorValidSpot(vector) && (Singleton<PlusLevelEditor>.Instance.level.rooms[ta.roomId - 1].activity == null);
        }

        public override bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            return true;
        }

        public override void OnSwitchToRotation()
        {
            //
        }
    }

    public class RotateAndPlacePrefab : PlaceAndRotateToolBase
    {
        private string _object;

        public string Object => _object;
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Object_" + _object);

        public RotateAndPlacePrefab(string obj)
        {
            _object = obj;
        }

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Scissors"));
            //throw new NotImplementedException();
        }

        public override bool IsVectorValidSpot(IntVector2 vector)
        {
            if (IsOutOfBounds(vector)) return false;
            if (!HasArea(vector)) return false;
            return true;
        }

        public override bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            return true;
        }

        public override void OnPlace(Direction dir)
        {
            if (IsOutOfBounds(selectedPosition.Value.ToInt())) return;
            if (!HasArea(selectedPosition.Value.ToInt())) return;
            EditorObjectType type = BaldiLevelEditorPlugin.editorObjects.Find(x => x.name == _object);
            Singleton<PlusLevelEditor>.Instance.AddPrefab(new PrefabLocation()
            {
                prefab = _object,
                position = (Singleton<PlusLevelEditor>.Instance.IntVectorToWorld(selectedPosition.Value.ToInt()) + type.offset).ToData(),
                rotation = dir.ToRotation().ToData()
            });
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Slap"));
            Singleton<PlusLevelEditor>.Instance.SelectTool(null);
        }

        public override void OnPlacementFail()
        {
            //throw new NotImplementedException();
        }
    }

    public class ObjectTool : EditorTool
    {
        private string _object;

        public string Object => _object;
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Object_" + _object);

        public ObjectTool(string obj)
        {
            _object = obj;
        }

        public override void OnDrop(IntVector2 vector)
        {
            if (IsOutOfBounds(vector)) return;
            if (!HasArea(vector)) return;
            EditorObjectType type = BaldiLevelEditorPlugin.editorObjects.Find(x => x.name == _object);
            Singleton<PlusLevelEditor>.Instance.AddPrefab(new PrefabLocation()
            {
                prefab = _object,
                position = (Singleton<PlusLevelEditor>.Instance.IntVectorToWorld(vector) + type.offset).ToData(),
                rotation = new UnityQuaternion()
            });
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Slap"));
            Singleton<PlusLevelEditor>.Instance.SelectTool(null);
        }

        public override void OnHover(IntVector2 vector)
        {
            //throw new NotImplementedException();
        }
    }

    public class ItemTool : EditorTool
    {
        private string _item;

        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/ITM_" + _item);

        public ItemTool(string obj)
        {
            _item = obj;
        }

        public override void OnDrop(IntVector2 vector)
        {
            if (IsOutOfBounds(vector)) return;
            if (!HasArea(vector)) return;
            Singleton<PlusLevelEditor>.Instance.AddItem(new ItemLocation()
            {
                item = _item,
                position = (Singleton<PlusLevelEditor>.Instance.IntVectorToWorld(vector) + (UnityEngine.Vector3.up * 5f)).ToData(),
            });
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Slap"));
            Singleton<PlusLevelEditor>.Instance.SelectTool(null);
        }

        public override void OnHover(IntVector2 vector)
        {
            //throw new NotImplementedException();
        }
    }

    public class ConnectTool : EditorTool
    {
        private bool inSecondPhase;
        private IEditorConnectable connectTo;
        private ButtonEditorVisual buttonFromVisual;
        private EditorButtonPlacement buttonFrom
        {
            get
            {
                if (buttonFromVisual == null) return null;
                return buttonFromVisual.typedPrefab;
            }
        }

        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Connect");

        public override void OnDrop(IntVector2 vector)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            if (!inSecondPhase)
            {
                if (hitTransform != null)
                {
                    if (hitTransform.name == "ButtonVisual")
                    {
                        inSecondPhase = true;
                        instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Scissors"));
                        buttonFromVisual = hitTransform.parent.gameObject.GetComponent<ButtonEditorVisual>();
                        return;
                    }
                }
                return;
            }
            else
            {
                if (buttonFromVisual == null) throw new InvalidOperationException("In second phase without Button!");
                if (hitTransform != null)
                {
                    IEditorConnectable connection = hitTransform.GetComponent<IEditorConnectable>();
                    if (connection != null)
                    {
                        if (connection.isTiled)
                        {
                            if (buttonFrom.connectedTiles.Contains(connection.tiledPrefab))
                            {
                                buttonFrom.connectedTiles.Remove(connection.tiledPrefab);
                                instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardUnlock"));
                            }
                            else
                            {
                                buttonFrom.connectedTiles.Add(connection.tiledPrefab);
                                instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardLock"));
                            }
                        }
                        else
                        {
                            if (buttonFrom.connectedPrefabs.Contains(connection.locationPrefab))
                            {
                                buttonFrom.connectedPrefabs.Remove(connection.locationPrefab);
                                instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardUnlock"));
                            }
                            else
                            {
                                buttonFrom.connectedPrefabs.Add(connection.locationPrefab);
                                instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardLock"));
                            }
                        }
                        Reset();
                        instance.SelectTool(null);
                        instance.RefreshLevel(false);
                        instance.UpdateLines();
                    }
                }
                return;
            }
        }

        public override void Reset()
        {
            base.Reset();
            inSecondPhase = false;
            connectTo = null;
            buttonFromVisual = null;
        }

        public override void OnEscape()
        {
            if (inSecondPhase)
            {
                Reset();
                return;
            }
            base.OnEscape();
        }

        public override void OnHover(IntVector2 vector)
        {
            ClearHighlights();
            if (!inSecondPhase)
            {
                if (hitTransform != null)
                {
                    if (hitTransform.name == "ButtonVisual")
                    {
                        hitTransform.parent.GetComponent<ButtonEditorVisual>().highlight = "yellow";
                        return;
                    }
                }
                return;
            }
            else
            {
                if (buttonFromVisual == null) throw new InvalidOperationException("In second phase without Button!");
                buttonFromVisual.highlight = "blue";
                if (hitTransform != null)
                {
                    IEditorConnectable connection = hitTransform.GetComponent<IEditorConnectable>();
                    if (connection != null)
                    {
                        bool hasConnection = false;
                        if (connection.isTiled)
                        {
                            hasConnection = buttonFrom.connectedTiles.Contains(connection.tiledPrefab);
                        }
                        else
                        {
                            hasConnection = buttonFrom.connectedPrefabs.Contains(connection.locationPrefab);
                        }
                        connection.highlight = hasConnection ? "red" : "green";
                        return;
                    }
                }
                return;
            }
        }
    }


    public class MergeTool : EditorTool
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Merge");
        private bool inSecondPhase;
        private TiledArea[] areas = new TiledArea[2];
        public override void OnDrop(IntVector2 vector)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            if (IsOutOfBounds(vector)) return;
            if (inSecondPhase)
            {
                TiledArea area = GetAreaAtPos(vector);
                if (area == null) return;
                areas[1] = area;
                if (instance.level.rooms[areas[0].roomId - 1].type != instance.level.rooms[areas[1].roomId - 1].type)
                {
                    areas[1] = null;
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    return;
                }
                // merge the two areas
                ushort oldId = areas[1].roomId;
                areas[1].roomId = areas[0].roomId;
                instance.level.RemoveRoomIfNoReferences(oldId);
                Reset();
                instance.RefreshLevel(true);
                instance.SelectTool(null);
                instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardUnlock"));
            }
            else
            {
                TiledArea area = GetAreaAtPos(vector);
                if (area == null) return;
                areas[0] = area;
                inSecondPhase = true;
                instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Doors_StandardLock"));
            }
        }

        public override void OnEscape()
        {
            if (inSecondPhase)
            {
                Reset();
                return;
            }
            base.OnEscape();
        }

        public override void OnHover(IntVector2 vector)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            if (IsOutOfBounds(vector)) return;
            ClearHighlights();
            if (areas[0] != null)
            {
                instance.level.areas.Where(x => x.roomId == areas[0].roomId).Do(x =>
                {
                    HighlightTiles(x.CalculateOwnedTiles(), "green");
                });
                //HighlightTiles(areas[0].CalculateOwnedTiles(), "green");
            }
            TiledArea area = GetAreaAtPos(vector);
            if (area == null) return;
            if (area == areas[0]) return;
            if (areas[0] != null)
            {
                HighlightTiles(area.CalculateOwnedTiles(), (instance.level.rooms[area.roomId - 1].type == instance.level.rooms[areas[0].roomId - 1].type) ? "yellow" : "red");
            }
            else
            {
                HighlightTiles(area.CalculateOwnedTiles(), "yellow");
            }
        }

        public override void Reset()
        {
            inSecondPhase = false;
            areas = new TiledArea[2];
        }
    }

    public class TileBasedTool : PlaceAndRotateToolBase
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Tile_" + _tile);

        private string _tile;

        public TileBasedTool(string tile)
        {
            _tile = tile;
        }

        public override void OnPlace(Direction dir)
        {
            TiledPrefab prefab = new TiledPrefab()
            {
                type = _tile,
                direction = dir.ToData(),
                position = selectedPosition.Value
            };
            Singleton<PlusLevelEditor>.Instance.AddTiledPrefab(prefab);
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("LockDoorStop"));
        }

        public override bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;

            for (int i = 0; i < instance.level.tiledPrefabs.Count; i++)
            {
                if ((instance.level.tiledPrefabs[i].position == position) && ((instance.level.tiledPrefabs[i].direction.ToStandard() == dir)))
                {
                    return false;
                }
            }
            return true;
        }

        public override void OnPlacementFail()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }

        public override void OnSwitchToRotation()
        {
            // do nothing
        }
    }

    public class ElevatorTool : PlaceAndRotateToolBase
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>(_isSpawn ? "UI/ElevatorEDSpawn" : "UI/ElevatorED");

        private bool _isSpawn;

        public ElevatorTool(bool isSpawn)
        {
            _isSpawn = isSpawn;
        }

        public override void OnPlace(Direction dir)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            ElevatorArea area = new ElevatorArea(selectedPosition.Value, 1, dir);
            instance.level.areas.Add(area);
            instance.level.AddElevator(area, _isSpawn);
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Close_Real"));
            instance.RefreshLevel();
            instance.SelectTool(null);
        }

        public override bool IsVectorValidSpot(IntVector2 vector)
        {
            if (_isSpawn)
            {
                if (Singleton<PlusLevelEditor>.Instance.level.exits.Where(x => x.isSpawn).Count() > 0)
                {
                    return false;
                }
            }
            if (IsOutOfBounds(vector)) return false;
            if (HasArea(vector)) return false;
            return true;
        }

        public override bool IsValidPlacement(ByteVector2 position, Direction dir)
        {
            ElevatorArea area = new ElevatorArea(selectedPosition.Value, 1, dir);
            ByteVector2[] owned = area.CalculateOwnedTiles();
            for (int i = 0; i < owned.Length; i++)
            {
                if (HasArea(owned[i].ToInt())) return false;
                if (IsOutOfBounds(owned[i].ToInt())) return false;
            }
            return true;
        }

        public override void OnPlacementFail()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
        }

        public override void OnSwitchToRotation()
        {
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Open_Real"));
        }
    }

    public class DeleteTool : EditorTool
    {
        public override Sprite editorSprite => BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/Delete");

        public override void OnDrop(IntVector2 vector)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            if (hitTransform != null)
            {
                if ((hitTransform.name == "Door"))
                {
                    instance.level.doors.Remove(hitTransform.parent.GetComponent<DoorEditorVisual>().typedPrefab);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
                else if ((hitTransform.name == "Window"))
                {
                    instance.level.windows.Remove(hitTransform.parent.GetComponent<WindowEditorVisual>().typedPrefab);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
                else if ((hitTransform.name == "ManualWall"))
                {
                    instance.level.manualWalls.Remove(hitTransform.parent.GetComponent<ManualWallEditorVisual>().typedPrefab);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
                else if ((hitTransform.name == "ButtonVisual"))
                {
                    instance.level.editorButtons.Remove(hitTransform.parent.GetComponent<ButtonEditorVisual>().typedPrefab);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
                else if ((hitTransform.name == "ObjectLocation"))
                {
                    hitTransform.GetComponent<IEditor3D>().DestroyObject(instance.level);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
                else if ((hitTransform.name == "NPCCollider"))
                {
                    instance.level.npcSpawns.Remove(hitTransform.parent.GetComponent<NPCSpawnLocation>().typedPrefab);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
                else if ((hitTransform.name == "TileLocation"))
                {
                    hitTransform.GetComponent<ITileVisual>().DestroyObject(instance.level);
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
            }
            if (IsOutOfBounds(vector)) return;
            if (!HasArea(vector)) return;
            for (int i = 0; i < instance.level.areas.Count; i++)
            {
                if (instance.level.areas[i].VectorIsInArea(vector.ToByte()))
                {
                    instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("GlassBreak"));
                    ushort id = instance.level.areas[i].roomId;
                    instance.level.areas.RemoveAt(i);
                    instance.level.RemoveRoomIfNoReferences(id);
                    instance.RefreshLevel();
                    instance.UpdateLines();
                    instance.SelectTool(null);
                    return;
                }
            }
        }

        public override void OnHover(IntVector2 vector)
        {
            PlusLevelEditor instance = Singleton<PlusLevelEditor>.Instance;
            ClearHighlights();
            if (IsOutOfBounds(vector)) return;
            if (hitTransform != null)
            {
                if (hitTransform.name == "Door")
                {
                    hitTransform.parent.GetComponent<DoorEditorVisual>().highlight = "red";
                    return;
                }
                else if (hitTransform.name == "Window")
                {
                    hitTransform.parent.GetComponent<WindowEditorVisual>().highlight = "red";
                    return;
                }
                else if (hitTransform.name == "ManualWall")
                {
                    hitTransform.parent.GetComponent<ManualWallEditorVisual>().highlight = "red";
                    return;
                }
                else if (hitTransform.name == "ButtonVisual")
                {
                    hitTransform.parent.GetComponent<ButtonEditorVisual>().highlight = "red";
                    return;
                }
                else if (hitTransform.name == "ObjectLocation")
                {
                    hitTransform.GetComponent<IEditor3D>().highlight = "red";
                    return;
                }
                else if (hitTransform.name == "NPCCollider")
                {
                    hitTransform.parent.GetComponent<NPCSpawnLocation>().highlight = "red";
                    return;
                }
                else if (hitTransform.name == "TileLocation")
                {
                    hitTransform.GetComponent<ITileVisual>().highlight = "red";
                    return;
                }
            }
            for (int i = 0; i < instance.level.areas.Count; i++)
            {
                if (instance.level.areas[i].VectorIsInArea(vector.ToByte()))
                {
                    ByteVector2[] ownedAreas = instance.level.areas[i].CalculateOwnedTiles();
                    HighlightTiles(ownedAreas, "red");
                    return;
                }
            }
        }
    }
}
