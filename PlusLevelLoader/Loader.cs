using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using PlusLevelFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader
{
    public static class CustomLevelLoader
    {
        public static SceneObject CreateEmptySceneObject()
        {
            SceneObject scene = ScriptableObject.CreateInstance<SceneObject>();
            scene.levelNo = -1;
            scene.levelTitle = "WIP";
            scene.extraAsset = ScriptableObject.CreateInstance<ExtraLevelDataAsset>();
            scene.extraAsset.minLightColor = Color.white;
            scene.extraAsset.npcSpawnPoints = new List<IntVector2>(); //wow thank you unity!
            scene.name = "WIP";
            scene.skybox = PlusLevelLoaderPlugin.Instance.assetMan.Get<Cubemap>("Cubemap_DayStandard");

            return scene;
        }

        public static LevelAsset LoadLevelAsset(Level level)
        {
            LevelAsset asset = ScriptableObject.CreateInstance<LevelAsset>();
            asset.levelSize = new IntVector2(level.width, level.height);
            asset.tile = new CellData[level.width * level.height];
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    asset.tile[x * level.height + y] = new CellData()
                    {
                        pos = new IntVector2(x, y),
                        type = level.tiles[x, y].type,
                        roomId = Mathf.Max((int)level.tiles[x, y].roomId - 1, 0)
                    };
                }
            }
            for (int i = 0; i < level.rooms.Count; i++)
            {
                RoomProperties room = level.rooms[i];
                RoomData data = new RoomData();
                RoomSettings settings = PlusLevelLoaderPlugin.Instance.roomSettings[room.type];
                data.ceilTex = PlusLevelLoaderPlugin.TextureFromAlias(room.textures.ceiling);
                data.florTex = PlusLevelLoaderPlugin.TextureFromAlias(room.textures.floor);
                data.wallTex = PlusLevelLoaderPlugin.TextureFromAlias(room.textures.wall);
                data.mapMaterial = settings.mapMaterial;
                data.doorMats = settings.doorMat;
                data.category = settings.category;
                data.type = settings.type;
                data.color = settings.color;
                for (int y = 0; y < room.prefabs.Count; y++)
                {
                    data.basicObjects.Add(new BasicObjectData()
                    {
                        position = room.prefabs[y].position.ToUnity(),
                        rotation = room.prefabs[y].rotation.ToUnity(),
                        prefab = PlusLevelLoaderPlugin.Instance.prefabAliases[room.prefabs[y].prefab].transform
                    });
                }
                data.hasActivity = room.activity != null;
                if (data.hasActivity)
                {
                    data.activity = new ActivityData()
                    {
                        direction = room.activity.direction.ToStandard(),
                        position = room.activity.position.ToUnity(),
                        prefab = PlusLevelLoaderPlugin.Instance.activityAliases[room.activity.activity]
                    };
                }
                else
                {
                    data.activity = new ActivityData();
                }
                data.roomFunctionContainer = settings.container;
                for (int y = 0; y < room.items.Count; y++)
                {
                    Vector3 vec = room.items[y].position.ToUnity();
                    data.items.Add(new ItemData()
                    {
                        item = PlusLevelLoaderPlugin.Instance.itemObjects[room.items[y].item],
                        position = new Vector3(vec.x, vec.z)
                    });
                }
                // find all cells that belong to this room that aren't empties
                CellData[] cells = asset.tile.Where(x => x.roomId == i).Where(x => x.type != 16).ToArray();
                for (int y = 0; y < cells.Length; y++)
                {
                    if (level.entitySafeTiles[cells[y].pos.x, cells[y].pos.z])
                    {
                        data.entitySafeCells.Add(cells[y].pos);
                    }
                    if (level.eventSafeTiles[cells[y].pos.x, cells[y].pos.z])
                    {
                        data.eventSafeCells.Add(cells[y].pos);
                    }
                    if (level.blockedWalls[cells[y].pos.x, cells[y].pos.z])
                    {
                        data.blockedWallCells.Add(cells[y].pos);
                    }
                }
                asset.rooms.Add(data);
            }
            List<TileBasedObjectData> appendAtEnd = new List<TileBasedObjectData>();
            for (int i = 0; i < level.doors.Count; i++)
            {
                Door doorPrefab = PlusLevelLoaderPlugin.Instance.doorPrefabs[level.doors[i].type];
                // special case for swinging doors because they are WEIRD
                if ((doorPrefab is SwingDoor) && (level.rooms[level.doors[i].roomId - 1].type == "hall"))
                {
                    appendAtEnd.Add(new TileBasedObjectData()
                    {
                        prefab = doorPrefab,
                        direction = level.doors[i].direction.ToStandard(),
                        position = level.doors[i].position.ToInt()
                    });
                }
                else
                {
                    asset.doors.Add(new DoorData(
                        Mathf.Max(level.doors[i].roomId - 1, 0),
                        doorPrefab,
                        level.doors[i].position.ToInt(),
                        level.doors[i].direction.ToStandard()));
                }
            }
            for (int i = 0; i < level.windows.Count; i++)
            {
                asset.windows.Add(new WindowData()
                {
                    position = level.windows[i].position.ToInt(),
                    direction = level.windows[i].direction.ToStandard(),
                    window = PlusLevelLoaderPlugin.Instance.windowObjects[level.windows[i].type]
                });
            }
            for (int i = 0; i < level.tiledPrefabs.Count; i++)
            {
                asset.tbos.Add(new TileBasedObjectData()
                {
                    position = level.tiledPrefabs[i].position.ToInt(),
                    direction = level.tiledPrefabs[i].direction.ToStandard(),
                    prefab = PlusLevelLoaderPlugin.Instance.tileAliases[level.tiledPrefabs[i].type]
                });
            }
            for (int i = 0; i < level.posters.Count; i++)
            {
                PosterLocation poster = level.posters[i];
                asset.posters.Add(new PosterData()
                {
                    poster = PlusLevelLoaderPlugin.Instance.posters[poster.type],
                    direction = poster.direction.ToStandard(),
                    position = poster.position.ToInt()
                });
            }
            for (int i = 0; i < level.buttons.Count; i++)
            {
                ButtonData buttonData = new ButtonData()
                {
                    position = level.buttons[i].position.ToInt(),
                    direction = level.buttons[i].direction.ToStandard(),
                    prefab = PlusLevelLoaderPlugin.Instance.buttons[level.buttons[i].type]
                };
                for (int k = 0; k < level.buttons[i].connections.Count; k++)
                {
                    ConnectionData data = level.buttons[i].connections[k];
                    buttonData.receivers.Add(new ButtonReceiverData()
                    {
                        receiverIndex = data.index,
                        receiverRoom = data.roomId,
                        type = (ButtonReceiverType)data.type
                    });
                }
                asset.buttons.Add(buttonData);
            }

            asset.spawnDirection = Direction.North;
            asset.spawnPoint = new Vector3(0f, 5f, 0f);
            for (int i = 0; i < level.elevators.Count; i++)
            {
                asset.exits.Add(new ExitData()
                {
                    direction = level.elevators[i].direction.ToStandard(),
                    position = level.elevators[i].position.ToInt(),
                    spawn = level.elevators[i].isSpawn,
                    room = RoomAssetMetaStorage.Instance.Get("Room_Elevator").value,
                    prefab = PlusLevelLoaderPlugin.Instance.assetMan.Get<Elevator>("ElevatorPrefab")
                });
            }
            asset.tbos.AddRange(appendAtEnd);
            return asset;
        }

        public static SceneObject LoadLevel(Level level)
        {
            SceneObject scene = CreateEmptySceneObject();
            
            scene.levelAsset = LoadLevelAsset(level);
            for (int i = 0; i < level.npcSpawns.Count; i++)
            {
                scene.extraAsset.npcSpawnPoints.Add(level.npcSpawns[i].position.ToInt());
                scene.extraAsset.npcsToSpawn.Add(PlusLevelLoaderPlugin.Instance.npcAliases[level.npcSpawns[i].type]);
            }
            return scene;
        }
    }
}
