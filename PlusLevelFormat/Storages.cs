using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlusLevelFormat
{
    public enum PlusDirection
    {
        North,
        East,
        South,
        West,
        Null
    }

    public class TextureContainer
    {
        public string floor = "null";
        public string wall = "null";
        public string ceiling = "null";

        public TextureContainer()
        {
        }

        public TextureContainer(TextureContainer container)
        {
            this.floor = container.floor;
            this.wall = container.wall;
            this.ceiling = container.ceiling;
        }

        public TextureContainer(string floor, string wall, string ceiling)
        {
            this.floor = floor;
            this.wall = wall;
            this.ceiling = ceiling;
        }
    }

    public class NPCLocation : TiledPrefab
    {
        public Dictionary<string, string> properties = new Dictionary<string, string>(); // for extra properties for editor levels specifically, included here because other mods might want to use it.
    }

    public class PrefabLocation : IEditorLocation
    {
        public UnityVector3 position { get; set; }
        public UnityQuaternion rotation;
        public string prefab = "null";

        public PrefabLocation()
        {

        }

        public PrefabLocation(string prefab, UnityVector3 vector, UnityQuaternion rotation = new UnityQuaternion())
        {
            this.prefab = prefab;
            position = vector;
            this.rotation = rotation;
        }

        public PrefabLocation GetNew()
        {
            return new PrefabLocation()
            {
                position = position,
                rotation = rotation,
                prefab = prefab
            };
        }
    }

    public class ItemLocation : IEditorLocation
    {
        public string item = "null";
        public UnityVector3 position { get; set; }
    }

    public class ElevatorLocation
    {
        public ByteVector2 position;
        public PlusDirection direction;
        public bool isSpawn = false;
    }

    public class RoomActivity : IEditorLocation
    {
        public string activity = "null";
        public UnityVector3 position { get; set; }
        public PlusDirection direction;
    }

    public class TiledPrefab
    {
        public string type = "null";
        public ByteVector2 position;
        public PlusDirection direction = PlusDirection.Null;
    }

    public class WindowLocation : TiledPrefab
    {
        //public string type = "null";
    }

    public class DoorLocation : TiledPrefab
    {
        //public string type = "null";
        public ushort roomId = 0;
    }

    public class ButtonLocation : TiledPrefab
    {
        public ButtonLocation()
        {
            type = "button";
        }
        public List<ConnectionData> connections = new List<ConnectionData>();
    }

    public enum PlusReceiverType
    {
        Basic,
        TileBased
    }

    public class ConnectionData
    {
        public ushort roomId;
        public int index;
        public PlusReceiverType type;

        public static ConnectionData? FromPrefab(Level level, PrefabLocation location)
        {
            int roomId = level.rooms.FindIndex(x => x.prefabs.Contains(location));
            if (roomId == -1) return null;
            int index = level.rooms[roomId].prefabs.IndexOf(location);
            if (index == -1) return null;
            return new ConnectionData()
            {
                type = PlusReceiverType.Basic,
                roomId = (ushort)roomId,
                index = index,
            };
        }

        public static ConnectionData? FromTileBased(Level level, TiledPrefab location)
        {
            int index = level.tiledPrefabs.IndexOf(location);
            if (index == -1) return null;
            return new ConnectionData()
            {
                type = PlusReceiverType.TileBased,
                roomId = 0,
                index = index
            };
        }
    }

    public class RoomType
    {
        public string name = "null";
        public bool offLimits = false;
        public List<string> scripts = new List<string>();
    }

    public interface IEditorLocation
    {
        public UnityVector3 position { get; set; }
    }

    public class RoomProperties
    {
        public RoomActivity? activity;
        public string type = "null";
        public TextureContainer textures = new TextureContainer();
        public List<PrefabLocation> prefabs = new List<PrefabLocation>();
        public List<ItemLocation> items = new List<ItemLocation>();

        public RoomProperties(string type)
        {
            this.type = type;
        }

        public RoomProperties()
        {

        }

    }
}
