using System;
using System.Collections.Generic;
using System.IO;

namespace PlusLevelFormat
{
    public static class LevelExtensions
    {
        // Write the level into the BinaryWriter
        public static void Write(this BinaryWriter writer, Level level)
        {
            writer.Write(Level.version);
            writer.Write(level.width);
            writer.Write(level.height);
            // write the wall nybbles
            List<Nybble> nybbles = new List<Nybble>();
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    nybbles.Add(level.tiles[x, y].walls);
                }
            }
            writer.Write(nybbles.ToArray()); //write the nybbles in a nicely condensed format
            // length of room ids can be calculated with width * heigh
            // write the room ids
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    writer.Write(level.tiles[x, y].roomId);
                }
            }
            writer.Write(level.rooms.Count);
            for (int i = 0; i < level.rooms.Count; i++)
            {
                writer.Write(level.rooms[i]);
            }
            writer.Write(level.doors.Count);
            for (int i = 0; i < level.doors.Count; i++)
            {
                writer.Write(level.doors[i]);
            }
            writer.Write(level.windows.Count);
            for (int i = 0; i < level.windows.Count; i++)
            {
                writer.Write(level.windows[i]);
            }
            writer.Write(level.elevators.Count);
            for (int i = 0; i < level.elevators.Count; i++)
            {
                writer.Write(level.elevators[i]);
            }
            writer.Write(level.npcSpawns.Count);
            for (int i = 0; i < level.npcSpawns.Count; i++)
            {
                writer.Write(level.npcSpawns[i]);
            }
            writer.Write(level.buttons.Count);
            for (int i = 0; i < level.buttons.Count; i++)
            {
                writer.Write(level.buttons[i]);
            }
            // write the entity safe tiles
            List<bool> bools = new List<bool>();
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    bools.Add(level.entitySafeTiles[x, y]);
                }
            }
            writer.Write(bools.ToArray());
            // write the event safe tiles
            bools.Clear();
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    bools.Add(level.eventSafeTiles[x, y]);
                }
            }
            writer.Write(bools.ToArray());
            // write the blocked wall tiles
            bools.Clear();
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    bools.Add(level.blockedWalls[x, y]);
                }
            }
            writer.Write(bools.ToArray());
            writer.Write(level.tiledPrefabs.Count);
            for (int i = 0; i < level.tiledPrefabs.Count; i++)
            {
                writer.Write(level.tiledPrefabs[i]);
            }
        }

        public static Level ReadLevel(this BinaryReader reader)
        {
            byte version = reader.ReadByte();
            Level newLevel = new Level(reader.ReadByte(), reader.ReadByte());
            Nybble[] nybbles = reader.ReadNybbles();
            for (int x = 0; x < newLevel.width; x++)
            {
                for (int y = 0; y < newLevel.height; y++)
                {
                    newLevel.tiles[x, y].walls = nybbles[(x * newLevel.height) + y];
                    newLevel.tiles[x, y].roomId = reader.ReadUInt16();
                }
            }
            int roomCount = reader.ReadInt32();
            for (int i = 0; i < roomCount; i++)
            {
                newLevel.AddRoom(reader.ReadRoom());
            }
            int doorCount = reader.ReadInt32();
            for (int i = 0; i < doorCount; i++)
            {
                newLevel.doors.Add(reader.ReadDoor());
            }
            int windowCount = reader.ReadInt32();
            for (int i = 0; i < windowCount; i++)
            {
                newLevel.windows.Add(reader.ReadWindow());
            }
            int elevatorCount = reader.ReadInt32();
            for (int i = 0; i < elevatorCount; i++)
            {
                newLevel.elevators.Add(reader.ReadElevator());
            }
            int npcCount = reader.ReadInt32();
            for (int i = 0; i < npcCount; i++)
            {
                newLevel.npcSpawns.Add(reader.ReadNPC());
            }
            if (version == 0) return newLevel; //stop here because version 0 doesn't have connection data
            int buttonCount = reader.ReadInt32();
            for (int i = 0; i < buttonCount; i++)
            {
                newLevel.buttons.Add(reader.ReadButton());
            }
            if (version <= 1) return newLevel;
            bool[] entitySafe = reader.ReadBoolArray();
            for (int x = 0; x < newLevel.width; x++)
            {
                for (int y = 0; y < newLevel.height; y++)
                {
                    newLevel.entitySafeTiles[x, y] = entitySafe[(x * newLevel.height) + y];
                }
            }
            bool[] eventSafe = reader.ReadBoolArray();
            for (int x = 0; x < newLevel.width; x++)
            {
                for (int y = 0; y < newLevel.height; y++)
                {
                    newLevel.eventSafeTiles[x, y] = eventSafe[(x * newLevel.height) + y];
                }
            }
            if (version <= 2) return newLevel;
            bool[] blockedWalls = reader.ReadBoolArray();
            for (int x = 0; x < newLevel.width; x++)
            {
                for (int y = 0; y < newLevel.height; y++)
                {
                    newLevel.blockedWalls[x, y] = blockedWalls[(x * newLevel.height) + y];
                }
            }
            if (version <= 3) return newLevel;
            int tiledPrefabCount = reader.ReadInt32();
            for (int i = 0; i < tiledPrefabCount; i++)
            {
                newLevel.tiledPrefabs.Add(reader.ReadTiledPrefab());
            }
            return newLevel;
        }
    }

    public class Level
    {
        public const byte version = 4;
        public Tile[,] tiles = new Tile[1,1];
        public bool[,] entitySafeTiles = new bool[1, 1];
        public bool[,] eventSafeTiles = new bool[1, 1];
        public bool[,] blockedWalls = new bool[1, 1];
        public byte width;
        public byte height;
        public List<RoomProperties> rooms = new List<RoomProperties>();
        public List<TiledPrefab> tiledPrefabs = new List<TiledPrefab>();
        public List<DoorLocation> doors = new List<DoorLocation>();
        public List<WindowLocation> windows = new List<WindowLocation>();
        public List<ElevatorLocation> elevators = new List<ElevatorLocation>();
        public List<NPCLocation> npcSpawns = new List<NPCLocation>();
        public List<ButtonLocation> buttons = new List<ButtonLocation>();

        protected Dictionary<ushort, ushort> _oldToNew = new Dictionary<ushort, ushort>(); //only here so RemoveRoom's local variable can be passed to whatever overrides it

        public ushort AddRoom(RoomProperties room)
        {
            if (rooms.Contains(room))
            {
                return (ushort)(rooms.IndexOf(room) + 1);
            }
            rooms.Add(room);
            return (ushort)rooms.Count;
        }

        /// <summary>
        /// Returns the roomID of the room that was removed.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public virtual ushort RemoveRoom(RoomProperties room)
        {
            Dictionary<RoomProperties, ushort> oldRoomIds = new Dictionary<RoomProperties, ushort>();
            Dictionary<RoomProperties, ushort> newRoomIds = new Dictionary<RoomProperties, ushort>();
            _oldToNew.Clear();
            rooms.ForEach(room =>
            {
                oldRoomIds.Add(room, (ushort)(rooms.IndexOf(room) + 1)); //roomIds start at 1, as 0 is reserved for no room/empty tile
            });
            ushort roomId = (ushort)(rooms.IndexOf(room) + 1);
            rooms.Remove(room);
            rooms.ForEach(room =>
            {
                newRoomIds.Add(room, (ushort)(rooms.IndexOf(room) + 1));
            });
            foreach (KeyValuePair<RoomProperties, ushort> kvp in oldRoomIds)
            {
                if (kvp.Key == room)
                {
                    _oldToNew.Add(kvp.Value, 0);
                    continue;
                }
                _oldToNew.Add(kvp.Value, newRoomIds[kvp.Key]);
            }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tiles[x, y].roomId == 0) continue;
                    tiles[x, y].roomId = _oldToNew[tiles[x, y].roomId]; //convert all the room ids to the new ones
                }
            }
            doors.ForEach(door =>
            {
                door.roomId = _oldToNew[door.roomId];
            });
            List<WindowLocation> windowsToBreak = new List<WindowLocation>();
            windows.ForEach(window =>
            {
                if (tiles[window.position.x, window.position.y].roomId == 0)
                {
                    windowsToBreak.Add(window);
                }
            });
            windows.RemoveAll(x => windowsToBreak.Contains(x));

            return roomId;
        }

        public Level(byte width, byte height)
        {
            this.width = width;
            this.height = height;
            tiles = new Tile[width, height];
            entitySafeTiles = new bool[width, height];
            eventSafeTiles = new bool[width, height];
            blockedWalls = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = new Tile(new ByteVector2(x,y));
                    entitySafeTiles[x, y] = false;
                    eventSafeTiles[x, y] = false;
                    blockedWalls[x, y] = false;
                }
            }
        }
    }

    public class Tile
    {

        private ByteVector2 _position;
        public ByteVector2 position => _position;
        public ushort roomId = 0;
        public int type => (roomId == 0) ? 16 : walls;
        public Nybble walls = new Nybble(0);

        public Tile(ByteVector2 pos)
        {
            _position = pos;
        }
    }

    public struct ByteVector2
    {
        private byte _x;
        private byte _y;
        public byte x => _x;
        public byte y => _y;

        public ByteVector2(byte x, byte y)
        {
            _x = x;
            _y = y;
        }

        public ByteVector2(int x, int y)
        {
            _x = (byte)x;
            _y = (byte)y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_x, _y);
        }

        public static ByteVector2 one => new ByteVector2(1, 1);

        public static ByteVector2 operator +(ByteVector2 a, ByteVector2 b) => new ByteVector2(a.x + b.x, a.y + b.y);

        public static ByteVector2 operator -(ByteVector2 a, ByteVector2 b) => new ByteVector2(a.x - b.x, a.y - b.y);

        public static ByteVector2 operator *(ByteVector2 a, int b) => new ByteVector2(a.x * b, a.y * b);

        public static ByteVector2 operator /(ByteVector2 a, int b) => new ByteVector2(a.x / b, a.y / b);

        public static bool operator ==(ByteVector2 a, ByteVector2 b) => ((a.x == b.x) && (a.y == b.y));
        public static bool operator !=(ByteVector2 a, ByteVector2 b) => !(a == b);
    }
}
