using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlusLevelFormat
{
    public static class WriterExtensions
    {

        public static void Write(this BinaryWriter writer, TiledPrefab prefab)
        {
            writer.Write(prefab.type);
            writer.Write(prefab.position);
            writer.Write((byte)prefab.direction);
        }

        public static TiledPrefab ReadTiledPrefab(this BinaryReader reader)
        {
            TiledPrefab newPf = new TiledPrefab();
            newPf.type = reader.ReadString();
            newPf.position = reader.ReadByteVector2();
            newPf.direction = (PlusDirection)reader.ReadByte();
            return newPf;
        }

        public static void Write(this BinaryWriter writer, ButtonLocation button)
        {
            writer.Write(button.type);
            writer.Write(button.position);
            writer.Write((byte)button.direction);
            writer.Write(button.connections.Count);
            for (int i = 0; i < button.connections.Count; i++)
            {
                writer.Write(button.connections[i]);
            }
        }

        public static ButtonLocation ReadButton(this BinaryReader reader)
        {
            ButtonLocation button = new ButtonLocation();
            button.type = reader.ReadString();
            button.position = reader.ReadByteVector2();
            button.direction = (PlusDirection)reader.ReadByte();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                button.connections.Add(reader.ReadConnectionData());
            }
            return button;
        }

        public static void Write(this BinaryWriter writer, ConnectionData data)
        {
            writer.Write((byte)data.type);
            writer.Write(data.roomId);
            writer.Write(data.index);
        }

        public static ConnectionData ReadConnectionData(this BinaryReader reader)
        {
            PlusReceiverType type = (PlusReceiverType)reader.ReadByte();
            ushort roomId = reader.ReadUInt16();
            int index = reader.ReadInt32();
            return new ConnectionData()
            {
                type = type,
                roomId = roomId,
                index = index
            };
        }

        public static void Write(this BinaryWriter writer, RoomProperties room, bool writePrefabsAndItems = true)
        {
            writer.Write(room.type);
            writer.Write(room.textures);
            writer.WriteActivity(room.activity);
            if (!writePrefabsAndItems)
            {
                writer.Write(0);
                writer.Write(0);
                return;
            }
            writer.Write(room.prefabs.Count);
            for (int i = 0; i < room.prefabs.Count; i++)
            {
                writer.Write(room.prefabs[i]);
            }
            writer.Write(room.items.Count);
            for (int i = 0; i < room.items.Count; i++)
            {
                writer.Write(room.items[i].item);
                writer.Write(room.items[i].position.x);
                writer.Write(room.items[i].position.z);
            }
        }

        public static void WriteActivity(this BinaryWriter writer, RoomActivity? activity)
        {
            if (activity == null)
            {
                writer.Write("null");
                return;
            }
            if (activity.activity == "null")
            {
                writer.Write("null");
                return;
            }
            writer.Write(activity.activity);
            writer.Write(activity.position.x);
            writer.Write(activity.position.y);
            writer.Write(activity.position.z);
            writer.Write((byte)activity.direction);
        }

        public static RoomActivity? ReadActivity(this BinaryReader reader)
        {
            string type = reader.ReadString();
            if (type == "null") return null;
            RoomActivity activity = new RoomActivity();
            activity.activity = type;
            activity.position = new UnityVector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            activity.direction = (PlusDirection)reader.ReadByte();
            return activity;
        }

        public static void Write(this BinaryWriter writer, ElevatorLocation elevator)
        {
            writer.Write(elevator.position);
            writer.Write((byte)elevator.direction);
            writer.Write(elevator.isSpawn);
        }

        public static ElevatorLocation ReadElevator(this BinaryReader reader)
        {
            ElevatorLocation elevator = new ElevatorLocation();
            elevator.position = reader.ReadByteVector2();
            elevator.direction = (PlusDirection)reader.ReadByte();
            elevator.isSpawn = reader.ReadBoolean();
            return elevator;
        }

        public static RoomProperties ReadRoom(this BinaryReader reader)
        {
            RoomProperties room = new RoomProperties(reader.ReadString());
            room.textures = reader.ReadTextureContainer();
            room.activity = reader.ReadActivity();
            int prefabCount = reader.ReadInt32();
            for (int i = 0; i < prefabCount; i++)
            {
                room.prefabs.Add(reader.ReadPrefab());
            }
            int itemCount = reader.ReadInt32();
            for (int i = 0; i < itemCount; i++)
            {
                room.items.Add(new ItemLocation()
                {
                    item = reader.ReadString(),
                    position = new UnityVector3(reader.ReadSingle(), 5f, reader.ReadSingle())
                });
            }
            return room;
        }

        public static ByteVector2 ReadByteVector2(this BinaryReader reader)
        {
            return new ByteVector2(reader.ReadByte(), reader.ReadByte());
        }

        public static TextureContainer ReadTextureContainer(this BinaryReader reader)
        {
            return new TextureContainer(reader.ReadString(), reader.ReadString(), reader.ReadString());
        }

        public static void Write(this BinaryWriter writer, ByteVector2 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
        }

        public static void Write(this BinaryWriter writer, DoorLocation doorlocation)
        {
            writer.Write(doorlocation.roomId);
            writer.Write(doorlocation.type);
            writer.Write(doorlocation.position);
            writer.Write((byte)((int)doorlocation.direction));
        }

        public static void Write(this BinaryWriter writer, WindowLocation windowlocation)
        {
            writer.Write(windowlocation.type);
            writer.Write(windowlocation.position);
            writer.Write((byte)((int)windowlocation.direction));
        }

        public static DoorLocation ReadDoor(this BinaryReader reader)
        {
            DoorLocation door = new DoorLocation();
            door.roomId = reader.ReadUInt16();
            door.type = reader.ReadString();
            door.position = reader.ReadByteVector2();
            door.direction = (PlusDirection)reader.ReadByte();
            return door;
        }

        public static void Write(this BinaryWriter writer, NPCLocation npc)
        {
            writer.Write(npc.type);
            writer.Write(npc.position);
            if (npc.properties.Count > 0) throw new NotImplementedException();
            writer.Write(npc.properties.Count);
        }

        public static NPCLocation ReadNPC(this BinaryReader reader)
        {
            NPCLocation npc = new NPCLocation();
            npc.type = reader.ReadString();
            npc.position = reader.ReadByteVector2();
            if (reader.ReadInt32() > 0) throw new NotImplementedException();
            return npc;
        }
        public static WindowLocation ReadWindow(this BinaryReader reader)
        {
            WindowLocation window = new WindowLocation();
            window.type = reader.ReadString();
            window.position = reader.ReadByteVector2();
            window.direction = (PlusDirection)reader.ReadByte();
            return window;
        }

        public static void Write(this BinaryWriter writer, PrefabLocation location)
        {
            writer.Write(location.prefab);
            // position
            writer.Write(location.position.x); // x
            writer.Write(location.position.y); // y
            writer.Write(location.position.z); // z
            // quarternion
            writer.Write(location.rotation.x); // x
            writer.Write(location.rotation.y); // y
            writer.Write(location.rotation.z); // z
            writer.Write(location.rotation.w); // w
        }

        public static void Write(this BinaryWriter writer, TextureContainer texture)
        {
            writer.Write(texture.floor);
            writer.Write(texture.wall);
            writer.Write(texture.ceiling);
        }

        public static PrefabLocation ReadPrefab(this BinaryReader reader)
        {
            PrefabLocation location = new PrefabLocation();
            location.prefab = reader.ReadString();
            location.position = new UnityVector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            location.rotation = new UnityQuaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            return location;
        }

        // thank you stack overflow!
        private static byte ConvertBoolArrayToByte(bool[] source)
        {
            byte result = 0;
            // This assumes the array never contains more than 8 elements!
            int index = 8 - source.Length;

            // Loop through the array
            foreach (bool b in source)
            {
                // if the element is 'true' set the bit at that position
                if (b)
                    result |= (byte)(1 << (7 - index));

                index++;
            }

            return result;
        }

        private static bool[] ConvertByteToBoolArray(byte b)
        {
            // prepare the return result
            bool[] result = new bool[8];

            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) != 0;

            // reverse the array
            Array.Reverse(result);

            return result;
        }

        public static void Write(this BinaryWriter writer, bool[] flags)
        {
            writer.Write(flags.Length);
            for (int i = 0; i < flags.Length; i+=8)
            {
                bool[] bytes = new bool[8];
                int z = 0;
                for (int y = i; y < i+8; y++)
                {
                    if (y >= flags.Length)
                    {
                        bytes[z] = false;
                    }
                    else
                    {
                        bytes[z] = flags[y];
                    }
                    z++;
                }
                writer.Write(ConvertBoolArrayToByte(bytes));
            }
        }

        public static bool[] ReadBoolArray(this BinaryReader reader)
        {
            int actLength = reader.ReadInt32();
            int length = (int)Math.Ceiling(actLength / 8f);
            bool[] result = new bool[length * 8]; //this rounds us up to read the extra byte
            for (int i = 0; i < length; i++)
            {
                bool[] bools = ConvertByteToBoolArray(reader.ReadByte());
                for (int y = 0; y < bools.Length; y++)
                {
                    result[(i * 8) + y] = bools[y];
                }
            }
            bool[] actualResult = new bool[actLength];
            for (int i = 0; i < actualResult.Length; i++)
            {
                actualResult[i] = result[i];
            }
            return actualResult;
        }
    }
}
