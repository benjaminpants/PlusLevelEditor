using HarmonyLib;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
    public class WallPlacement : TiledPrefab
    {
        public bool wall = true;
    }

    public class EditorButtonPlacement : TiledPrefab
    {
        public EditorButtonPlacement()
        {
            type = "button";
        }

        public List<PrefabLocation> connectedPrefabs = new List<PrefabLocation>();
        public List<TiledPrefab> connectedTiles = new List<TiledPrefab>();

        public ButtonLocation ToLocation(EditorLevel level)
        {
            ButtonLocation newLoca = new ButtonLocation();
            newLoca.type = type;
            newLoca.position = position;
            newLoca.direction = direction;
            for (int i = 0; i < connectedPrefabs.Count; i++)
            {
                ConnectionData? data = ConnectionData.FromPrefab(level, connectedPrefabs[i]);
                if (data == null) continue;
                newLoca.connections.Add(data);
            }
            for (int i = 0; i < connectedTiles.Count; i++)
            {
                ConnectionData? data = ConnectionData.FromTileBased(level, connectedTiles[i]);
                if (data == null) continue;
                newLoca.connections.Add(data);
            }
            return newLoca;
        }

        public void ValidateConnections(EditorLevel level)
        {
            for (int i = (connectedTiles.Count - 1); i >= 0; i--)
            {
                if (level.tiledPrefabs.IndexOf(connectedTiles[i]) == -1)
                {
                    connectedTiles.RemoveAt(i);
                }
            }
            for (int i = (connectedPrefabs.Count - 1); i >= 0; i--)
            {
                if (level.prefabs.IndexOf(connectedPrefabs[i]) == -1)
                {
                    connectedPrefabs.RemoveAt(i);
                }
            }
        }
    }

    public class EditorLevel : Level
    {
        public List<TiledArea> areas = new List<TiledArea>();
        public Dictionary<string, TextureContainer> defaultTextures = new Dictionary<string, TextureContainer>();
        public List<WallPlacement> manualWalls = new List<WallPlacement>();
        public List<EditorButtonPlacement> editorButtons = new List<EditorButtonPlacement>();
        public List<PrefabLocation> prefabs = new List<PrefabLocation>();
        public List<ItemLocation> items = new List<ItemLocation>();
        private RoomProperties _hallRoom;
        public Dictionary<ElevatorArea, ElevatorLocation> elevatorAreas = new Dictionary<ElevatorArea, ElevatorLocation>();
        public RoomProperties hallRoom => _hallRoom;
        public EditorLevel(byte width, byte height) : base(width, height)
        {
            InitializeDefaultTextures();
            _hallRoom = new RoomProperties("hall");
            _hallRoom.textures = new TextureContainer(defaultTextures["hall"]);
            AddRoom(_hallRoom);
        }

        void InitializeDefaultTextures()
        {
            defaultTextures.Add("hall", new TextureContainer("HallFloor", "Wall", "Ceiling"));
            defaultTextures.Add("class", new TextureContainer("BlueCarpet", "Wall", "Ceiling"));
            defaultTextures.Add("faculty", new TextureContainer("BlueCarpet", "FacultyWall", "Ceiling"));
            defaultTextures.Add("office", new TextureContainer("BlueCarpet", "FacultyWall", "Ceiling"));
            defaultTextures.Add("closet", new TextureContainer("Actual", "Wall", "Ceiling"));
            defaultTextures.Add("reflex", new TextureContainer("HallFloor", "FacultyWall", "ElevatorCeiling"));
            defaultTextures.Add("library", new TextureContainer("BlueCarpet", "FacultyWall", "Ceiling"));
            defaultTextures.Add("cafeteria", new TextureContainer("HallFloor", "Wall", "Ceiling"));
            defaultTextures.Add("outside", new TextureContainer("Grass", "Fence", "None"));
        }

        public static EditorLevel LoadFromStream(BinaryReader reader)
        {
            byte version = reader.ReadByte();
            EditorLevel level = new EditorLevel(reader.ReadByte(), reader.ReadByte());
            //level.defaultTextures.Clear();
            level.rooms.Clear();
            int defaultTextures = reader.ReadInt32();
            for (int i = 0; i < defaultTextures; i++)
            {
                string key = reader.ReadString();
                if (level.defaultTextures.ContainsKey(key))
                {
                    level.defaultTextures[key] = reader.ReadTextureContainer();
                }
                else
                {
                    level.defaultTextures.Add(key, reader.ReadTextureContainer());
                }
            }
            int roomCount = reader.ReadInt32();
            for (int i = 0; i < roomCount; i++)
            {
                level.rooms.Add(reader.ReadRoom());
            }
            level.RemoveRoomInternal(level._hallRoom);
            level._hallRoom = level.rooms.Find(x => x.type == "hall");
            int areaCount = reader.ReadInt32();
            for (int i = 0; i < areaCount; i++)
            {
                string type = reader.ReadString();
                switch (type)
                {
                    case "rect":
                        level.areas.Add(new AreaData(ByteVector2.one, ByteVector2.one, 0).ReadInto(reader));
                        break;
                    default:
                        throw new NotImplementedException("Unknown type " + type);
                }
            }
            int doorCount = reader.ReadInt32();
            for (int i = 0; i < doorCount; i++)
            {
                level.doors.Add(reader.ReadDoor());
            }
            int windowCount = reader.ReadInt32();
            for (int i = 0; i < windowCount; i++)
            {
                level.windows.Add(reader.ReadWindow());
            }
            int wallCount = reader.ReadInt32();
            for (int i = 0; i < wallCount; i++)
            {
                WallPlacement wall = new WallPlacement();
                wall.direction = (PlusDirection)reader.ReadByte();
                wall.position = reader.ReadByteVector2();
                wall.wall = reader.ReadBoolean();
                level.manualWalls.Add(wall);
            }
            int prefabCount = reader.ReadInt32();
            for (int i = 0; i < prefabCount; i++)
            {
                level.prefabs.Add(reader.ReadPrefab());
            }
            int elevatorCount = reader.ReadInt32();
            for (int i = 0; i < elevatorCount; i++)
            {
                ElevatorLocation location = reader.ReadElevator();
                ElevatorArea area = new ElevatorArea(location.position, 1, location.direction.ToStandard());
                level.areas.Add(area);
                level.elevators.Add(location);
                level.elevatorAreas.Add(area,location);
            }
            int npcCount = reader.ReadInt32();
            for (int i = 0; i < npcCount; i++)
            {
                level.npcSpawns.Add(reader.ReadNPC());
            }
            int itemCount = reader.ReadInt32();
            for (int i = 0; i < itemCount; i++)
            {
                level.items.Add(new ItemLocation()
                {
                    item = reader.ReadString(),
                    position = new UnityVector3(reader.ReadSingle(), 5f, reader.ReadSingle())
                });
            }
            if (version == 0) return level; //no buttons or tiledPrefabs
            int tiledPrefabCount = reader.ReadInt32();
            for (int i = 0; i < tiledPrefabCount; i++)
            {
                level.tiledPrefabs.Add(reader.ReadTiledPrefab());
            }
            reader.ReadInt32(); //placeholder for builders
            int buttonCount = reader.ReadInt32();
            for (int i = 0; i < buttonCount; i++)
            {
                EditorButtonPlacement placement = new EditorButtonPlacement();
                placement.type = reader.ReadString();
                placement.position = reader.ReadByteVector2();
                placement.direction = (PlusDirection)reader.ReadByte();
                int connectedPrefabs = reader.ReadInt32();
                for (int k = 0; k < connectedPrefabs; k++)
                {
                    placement.connectedPrefabs.Add(level.prefabs[reader.ReadInt32()]);
                }
                int connectedTiles = reader.ReadInt32();
                for (int k = 0; k < connectedTiles; k++)
                {
                    placement.connectedTiles.Add(level.tiledPrefabs[reader.ReadInt32()]);
                }
                level.editorButtons.Add(placement);
            }
            return level;
        }

        public void AddElevator(ElevatorArea area, bool isSpawn)
        {
            ElevatorLocation location = new ElevatorLocation() { direction = area.direction.ToData(), isSpawn = isSpawn, position = area.origin };
            elevators.Add(location);
            elevatorAreas.Add(area, location);
        }

        public const byte editorFormatVersion = 1;

        public void SaveIntoStream(BinaryWriter writer)
        {
            writer.Write(editorFormatVersion);
            writer.Write(width);
            writer.Write(height);
            writer.Write(defaultTextures.Count);
            foreach (KeyValuePair<string, TextureContainer> kvp in defaultTextures)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
            writer.Write(rooms.Count);
            for (int i = 0; i < rooms.Count; i++)
            {
                writer.Write(rooms[i], false);
            }
            List<TiledArea> saveAreas = areas.Where(x => x.shouldSave).ToList();
            writer.Write(saveAreas.Count);
            for (int i = 0; i < saveAreas.Count; i++)
            {
                saveAreas[i].Write(writer);
            }
            writer.Write(doors.Count);
            for (int i = 0; i < doors.Count; i++)
            {
                writer.Write(doors[i]);
            }
            writer.Write(windows.Count);
            for (int i = 0; i < windows.Count; i++)
            {
                writer.Write(windows[i]);
            }
            writer.Write(manualWalls.Count);
            for (int i = 0; i < manualWalls.Count; i++)
            {
                writer.Write((byte)((int)manualWalls[i].direction));
                writer.Write(manualWalls[i].position);
                writer.Write(manualWalls[i].wall);
            }
            writer.Write(prefabs.Count);
            for (int i = 0; i < prefabs.Count; i++)
            {
                writer.Write(prefabs[i]);
            }
            writer.Write(elevators.Count);
            for (int i = 0; i < elevators.Count; i++)
            {
                writer.Write(elevators[i]);
            }
            writer.Write(npcSpawns.Count);
            for (int i = 0; i < npcSpawns.Count; i++)
            {
                writer.Write(npcSpawns[i]);
            }
            writer.Write(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                writer.Write(items[i].item);
                writer.Write(items[i].position.x);
                writer.Write(items[i].position.z);
            }
            writer.Write(tiledPrefabs.Count);
            for (int i = 0; i < tiledPrefabs.Count; i++)
            {
                writer.Write(tiledPrefabs[i]);
            }
            writer.Write((int)0); //placeholder for builders
            writer.Write(editorButtons.Count);
            for (int i = 0; i < editorButtons.Count; i++)
            {
                writer.Write(editorButtons[i].type);
                writer.Write(editorButtons[i].position);
                writer.Write((byte)editorButtons[i].direction);
                writer.Write(editorButtons[i].connectedPrefabs.Count);
                for (int k = 0; k < editorButtons[i].connectedPrefabs.Count; k++)
                {
                    writer.Write(prefabs.IndexOf(editorButtons[i].connectedPrefabs[k]));
                }
                writer.Write(editorButtons[i].connectedTiles.Count);
                for (int k = 0; k < editorButtons[i].connectedTiles.Count; k++)
                {
                    writer.Write(tiledPrefabs.IndexOf(editorButtons[i].connectedTiles[k]));
                }
            }
        }

        public PlusLevelFormat.Tile? GetTileSafe(int x, int y)
        {
            if (x < 0) return null;
            if (x >= width) return null;
            if (y < 0) return null;
            if (y >= width) return null;
            return tiles[x,y];
        }

        public void RemoveRoomIfNoReferences(ushort id)
        {
            if (id == 1) return; //the first room must ALWAYS stay, as that is our hallroom
            if (areas.Where(x => x.roomId == id).Count() != 0) return;
            RemoveRoom(rooms[id - 1]);
        }

        public override ushort RemoveRoom(RoomProperties room)
        {
            if (room == hallRoom) return ushort.MaxValue; // 255x255 is less than MaxValue so this will never occur naturally
            return RemoveRoomInternal(room);
        }

        protected ushort RemoveRoomInternal(RoomProperties room)
        {
            ushort goneId = base.RemoveRoom(room);
            areas.RemoveAll(x => (x.roomId == goneId));
            areas.Do(x =>
            {
                x.roomId = _oldToNew[x.roomId];
            });
            return goneId;
        }

        public void SetWall(ByteVector2 position, Direction dir, bool state, bool oneSide = false)
        {
            IntVector2 forward = Directions.ToIntVector2(dir);
            if (!state)
            {
                this.tiles[position.x, position.y].walls &= (Nybble)~((1 << Directions.BitPosition(dir)));
                if ((GetTileSafe(position.x + forward.x, position.y + forward.z) == null) || oneSide) return;
                this.tiles[position.x + forward.x, position.y + forward.z].walls &= (Nybble)~((1 << Directions.BitPosition(dir.GetOpposite())));
            }
            else
            {
                this.tiles[position.x, position.y].walls |= (Nybble)((1 << Directions.BitPosition(dir)));
                if ((GetTileSafe(position.x + forward.x, position.y + forward.z) == null) || oneSide) return;
                this.tiles[position.x + forward.x, position.y + forward.z].walls |= (Nybble)((1 << Directions.BitPosition(dir.GetOpposite())));
            }
        }

        public void ValidateDoors()
        {
            List<DoorLocation> doorsToRemove = new List<DoorLocation>();
            for (int i = 0; i < doors.Count; i++)
            {
                PlusLevelFormat.Tile? t = GetTileSafe(doors[i].position.x, doors[i].position.y);
                if (t == null) throw new Exception("DOOR OOB!");
                if (t.roomId == 0) { doorsToRemove.Add(doors[i]); continue; }
                doors[i].roomId = t.roomId;
                IntVector2 vec2 = doors[i].direction.ToStandard().ToIntVector2();
                PlusLevelFormat.Tile? tAhead = GetTileSafe(doors[i].position.x + vec2.x, doors[i].position.y + vec2.z);
                if (tAhead == null) { doorsToRemove.Add(doors[i]); continue; }
                if (tAhead.roomId == 0) { doorsToRemove.Add(doors[i]); continue; }
                if ((rooms[tAhead.roomId - 1].type != "hall") && PlusLevelEditor.DoorShouldPrioritizeRooms(doors[i].type))
                {
                    doors[i].roomId = tAhead.roomId;
                }
            }
            doors.RemoveAll(x => doorsToRemove.Contains(x));
        }

        public void ValidateTiledPrefabs<T>(List<T> list, bool careAboutMissingWall = true) where T : TiledPrefab
        {
            List<T> toRemove = new List<T>();
            for (int i = 0; i < list.Count; i++)
            {
                PlusLevelFormat.Tile? t = GetTileSafe(list[i].position.x, list[i].position.y);
                if (t == null) throw new Exception("TILED OBJECT OOB!");
                if (t.roomId == 0) { toRemove.Add(list[i]); continue; }
                if (!careAboutMissingWall) continue; // we dont care about if the wall outside exists(aka for buttons) so it can go BYE BYE!
                IntVector2 vec2 = list[i].direction.ToStandard().ToIntVector2();
                PlusLevelFormat.Tile? tAhead = GetTileSafe(list[i].position.x + vec2.x, list[i].position.y + vec2.z);
                if (tAhead == null) { toRemove.Add(list[i]); continue; }
                if (tAhead.roomId == 0) { toRemove.Add(list[i]); continue; }
            }
            list.RemoveAll(x => toRemove.Contains(x));
        }

        public void UpdateTiles(bool forEditor)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y].roomId = GetRoomIDOfPos(tiles[x, y].position, forEditor);
                    tiles[x, y].walls = new Nybble(0);
                }
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    List<Direction> allDirections = Directions.All();
                    PlusLevelFormat.Tile t = tiles[x, y];
                    while (allDirections.Count > 0)
                    {
                        Direction dir = allDirections[0];
                        allDirections.RemoveAt(0);
                        IntVector2 vec2 = dir.ToIntVector2();
                        PlusLevelFormat.Tile? nearbyTile = GetTileSafe(t.position.x + vec2.x, t.position.y + vec2.z);
                        bool wallFacingThisWay = (nearbyTile == null) ? true : (nearbyTile.roomId != t.roomId);
                        if (wallFacingThisWay)
                        {
                            t.walls |= (Nybble)(1 << Directions.BitPosition(dir));
                        }
                    }
                }
            }
            ValidateDoors();
            ValidateTiledPrefabs<WindowLocation>(windows);
            ValidateTiledPrefabs<WallPlacement>(manualWalls);
            ValidateTiledPrefabs<EditorButtonPlacement>(editorButtons, false);
            for (int i = 0; i < doors.Count; i++)
            {
                SetWall(doors[i].position, doors[i].direction.ToStandard(), false);
            }
            if (!forEditor)
            {
                for (int i = 0; i < manualWalls.Count; i++)
                {
                    SetWall(manualWalls[i].position, manualWalls[i].direction.ToStandard(), manualWalls[i].wall);
                }
                for (int i = 0; i < windows.Count; i++)
                {
                    SetWall(windows[i].position, windows[i].direction.ToStandard(), true);
                }
                for (int i = 0; i < editorButtons.Count; i++)
                {
                    SetWall(editorButtons[i].position, editorButtons[i].direction.ToStandard(), true);
                }
            }
            else
            {
                for (int i = 0; i < manualWalls.Count; i++)
                {
                    SetWall(manualWalls[i].position, manualWalls[i].direction.ToStandard(), false);
                }
                for (int i = 0; i < windows.Count; i++)
                {
                    SetWall(windows[i].position, windows[i].direction.ToStandard(), false);
                }
                for (int i = 0; i < editorButtons.Count; i++)
                {
                    SetWall(editorButtons[i].position, editorButtons[i].direction.ToStandard(), false);
                    SetWall((editorButtons[i].position.ToInt() + editorButtons[i].direction.ToStandard().ToIntVector2()).ToByte(), editorButtons[i].direction.ToStandard().GetOpposite(), true, true);
                }
            }
            // update all room prefabs
            rooms.Do(x => x.prefabs.Clear());
            rooms.Do(x => x.items.Clear());
            List<object> toRemove = new List<object>();
            prefabs.Do(x =>
            {
                ushort roomId = GetRoomIDOfPos(new ByteVector2((byte)Mathf.RoundToInt((x.position.x - 5f) / 10f), (byte)Mathf.RoundToInt((x.position.z - 5f) / 10f)), true);
                if (roomId == 0)
                {
                    toRemove.Add(x);
                    return;
                }
                rooms[roomId - 1].prefabs.Add(x);
            });
            prefabs.RemoveAll(x => toRemove.Contains(x));
            toRemove.Clear();
            items.Do(x =>
            {
                ushort roomId = GetRoomIDOfPos(new ByteVector2((byte)Mathf.RoundToInt((x.position.x - 5f) / 10f), (byte)Mathf.RoundToInt((x.position.z - 5f) / 10f)), true);
                if (roomId == 0)
                {
                    toRemove.Add(x);
                    return;
                }
                rooms[roomId - 1].items.Add(x);
            });
            items.RemoveAll(x => toRemove.Contains(x));
            areas.Do(x =>
            {
                x.FinalizeWalls(this, forEditor);
            });
            // handle elevators
            List<ElevatorArea> elevatorsToRemove = new List<ElevatorArea>();
            elevatorAreas.Do(x =>
            {
                if (!areas.Contains(x.Key))
                {
                    toRemove.Add(x.Key);
                    elevators.Remove(x.Value);
                }
            });
            foreach (ElevatorArea rea in elevatorsToRemove)
            {
                elevatorAreas.Remove(rea);
            }
            for (int i = (npcSpawns.Count - 1); i >= 0; i--)
            {
                if (GetRoomIDOfPos(npcSpawns[i].position, true) == 0)
                {
                    npcSpawns.Remove(npcSpawns[i]);
                }
            }
            for (int i = (tiledPrefabs.Count - 1); i >= 0; i--)
            {
                if (GetRoomIDOfPos(tiledPrefabs[i].position, true) == 0)
                {
                    tiledPrefabs.RemoveAt(i);
                }
            }
            editorButtons.Do(x => x.ValidateConnections(this));
            buttons.Clear();
            if (!forEditor)
            {
                for (int i = 0; i < editorButtons.Count; i++)
                {
                    buttons.Add(editorButtons[i].ToLocation(this));
                }
            }
        }

        public ushort GetRoomIDOfPos(ByteVector2 vector, bool forEditor)
        {
            foreach (TiledArea area in areas)
            {
                if (area.editorOnly && !forEditor) continue;
                if (area.VectorIsInArea(vector))
                {
                    return area.roomId;
                }    
            }
            return 0;
        }

        public bool CollidesWithAreas(TiledArea area, TiledArea toIgnore)
        {
            foreach (TiledArea tarea in areas)
            {
                if (tarea == toIgnore) continue;
                if (tarea.CollidesWith(area))
                {
                    return true;
                }
            }
            return false;
        }

        public TiledArea? GetAreaOfPos(ByteVector2 vector)
        {
            foreach (TiledArea area in areas)
            {
                if (area.VectorIsInArea(vector))
                {
                    return area;
                }
            }
            return null;
        }

    }

    public abstract class TiledArea
    {
        public abstract string type { get; } 
        public ByteVector2 origin;
        public ushort roomId;
        public virtual bool shouldSave => true;
        public virtual bool editorOnly => false;
        public TiledArea(ByteVector2 origin, ushort roomId)
        {
            this.origin = origin;
            this.roomId = roomId;
        }

        // finalize our walls
        public virtual void FinalizeWalls(EditorLevel level, bool forEditor)
        {

        }

        public virtual bool VectorIsInArea(ByteVector2 vector)
        {
            return CalculateOwnedTiles().Contains(vector);
        }

        public virtual void Write(BinaryWriter writer)
        {
            writer.Write(type);
            writer.Write(origin);
            writer.Write(roomId);
        }

        public virtual TiledArea ReadInto(BinaryReader reader)
        {
            origin = reader.ReadByteVector2();
            roomId = reader.ReadUInt16();
            return this;
        }

        public bool CollidesWith(TiledArea area)
        {
            ByteVector2[] owned = CalculateOwnedTiles();
            for (int i = 0; i < owned.Length; i++)
            {
                if (area.VectorIsInArea(owned[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public abstract ByteVector2[] CalculateOwnedTiles();
    }

    public class ElevatorArea : TiledArea
    {
        public ElevatorArea(ByteVector2 origin, ushort roomId, Direction direction) : base(origin, roomId)
        {
            this.direction = direction;
        }

        protected PlusDirection plusDirection = PlusDirection.Null;

        public Direction direction
        {
            get
            {
                return (Direction)plusDirection;
            }
            set
            {
                plusDirection = (PlusDirection)value;
            }
        }
        public override string type => "elevator";
        public override bool editorOnly => true;
        public override bool shouldSave => false;

        public override void FinalizeWalls(EditorLevel level, bool forEditor)
        {
            ByteVector2[] owned = CalculateOwnedTiles();
            for (int i = 1; i < owned.Length; i++)
            {
                level.SetWall(owned[i], direction, false);
                level.SetWall(owned[i], direction.GetOpposite(), true);
            }
            level.SetWall(owned[2], ((Direction)(((int)direction + 1) % 4)), true);
            level.SetWall(owned[3], ((Direction)(((int)direction + 3) % 4)), true);
            if (!forEditor) return;
            for (int i = 0; i < 4; i++)
            {
                level.SetWall(owned[0],(Direction)i, true);
            }
        }

        public override ByteVector2[] CalculateOwnedTiles()
        {
            ByteVector2[] tiles = new ByteVector2[4];
            tiles[1] = new ByteVector2(origin.x, origin.y);
            tiles[0] = (origin.ToInt() - direction.ToIntVector2()).ToByte();
            tiles[2] = (origin.ToInt() + ((Direction)(((int)direction + 1) % 4)).ToIntVector2()).ToByte();
            tiles[3] = (origin.ToInt() + ((Direction)(((int)direction + 3) % 4)).ToIntVector2()).ToByte();
            return tiles;
        }
    }

    public class AreaData : TiledArea
    {
        public override string type => "rect";
        public ByteVector2 size;
        public ByteVector2 corner => origin + (size - ByteVector2.one);

        public AreaData(ByteVector2 origin, ByteVector2 size, ushort roomId) : base(origin, roomId)
        {
            this.size = size;
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(size);
        }

        public override TiledArea ReadInto(BinaryReader reader)
        {
            base.ReadInto(reader);
            size = reader.ReadByteVector2();
            return this;
        }

        public override bool VectorIsInArea(ByteVector2 vector) //this is quicker than the default VectorIsInArea implementation
        {
            if (!(vector.x >= origin.x)) return false;
            if (!(vector.x < (origin + size).x)) return false;
            if (!(vector.y >= origin.y)) return false;
            if (!(vector.y < (origin + size).y)) return false;
            return true;
        }

        public override ByteVector2[] CalculateOwnedTiles()
        {
            List<ByteVector2> vectors = new List<ByteVector2>();
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    vectors.Add(origin + new ByteVector2(x,y));
                }
            }
            return vectors.ToArray();
        }
    }
}
