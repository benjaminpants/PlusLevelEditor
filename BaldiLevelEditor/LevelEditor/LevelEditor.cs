using HarmonyLib;
using MTM101BaldAPI;
using PlusLevelFormat;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using MTM101BaldAPI.UI;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PlusLevelLoader;
using System.IO;
using MTM101BaldAPI.AssetTools;
using System.Xml.Linq;
using System.Collections;

namespace BaldiLevelEditor
{
    public enum LevelEditorState
    {
        Standard,
        InMenu
    }


    public partial class PlusLevelEditor : Singleton<PlusLevelEditor>
    {
        public struct EditorLine
        {
            public Vector3 start;
            public Vector3 end;
            public int requiredDots;
            public EditorLine(Vector3 start, Vector3 end)
            {
                this.start = start;
                this.end = end;
                requiredDots = Mathf.CeilToInt(Vector3.Distance(start, end) / 2.5f);
            }
        }

        static FieldInfo _useRawPosition = AccessTools.Field(typeof(CursorController), "useRawPosition");
        static FieldInfo _deltaThisFrame = AccessTools.Field(typeof(CursorController), "deltaThisFrame");
        public GameCamera? myCamera;
        public AudioManager audMan;
        public Canvas? canvas;
        public CursorController? cursor;
        public Transform myPawn; //the "pawn" that controls the camera and movement
        public Transform tileMap; // the transform used to store all the tiles
        public EnvironmentController? puppetEnvironmentController = null; // a puppet enviroment controller used to generate texture atlas' and tile meshess.
        public EditorLevel level = new EditorLevel(50, 50);
        public EditorTile[,] edTiles;
        public IntVector2 selectorLocation = new IntVector2(-1, 0); //the current location of the selector
        public IntVector2 highlightedLocation = new IntVector2(-1, 0); //the current location that the cursor is hovering over
        public EditorSelector selector; // the selector that has the arrow handles
        public AreaData? selectedArea;
        public List<IWallVisual> wallVisuals = new List<IWallVisual>();
        public List<IEditor3D> prefabVisuals = new List<IEditor3D>();
        public List<ITileVisual> tiledVisuals = new List<ITileVisual>();
        private Transform lineParent;
        private SpriteRenderer rendererTemplate;
        public List<SpriteRenderer> lineSprites = new List<SpriteRenderer>();
        public const int minLineSprites = 128;
        public List<EditorLine> lines = new List<EditorLine>();
        //public List<NPCSpawnLocation> npcVisuals = new List<NPCSpawnLocation>();
        public Transform dummyColliderTransform;
        public EditorTool? selectedTool;
        public Vector3 cameraRotation = new Vector3(0f, 0f, 0f);
        private Ray ray;
        private RaycastHit hit;
        private bool rayHit = false;
        private static FieldInfo cc_results = AccessTools.Field(typeof(CursorController), "results");
        bool initializedDummy = false;
        public float updateDelay = 0f;
        private int prefabHighlight = -1;
        private IEditor3D? selectedPrefab => (prefabHighlight == -1) ? null : prefabVisuals[prefabHighlight];
        public Vector3? prefabHandleStart = null;
        public Vector3? prefabHandleOffset = null;
        public Vector3? prefabHandleEnd = null;
        public Vector3? prefabHandleDirection = null;

        // for dragging handles
        public IntVector2? handleStart = null;
        public IntVector2? handleEnd = null;
        public Direction handleDirection = Direction.Null;

        public LevelEditorState state = LevelEditorState.Standard;

        public Texture2D GenerateTextureAtlas(TextureContainer container)
        {
            GameObject dummyObject = new GameObject();
            dummyObject.SetActive(false);
            RoomController dummyRC = dummyObject.AddComponent<RoomController>();
            dummyRC.baseMat = new Material(BaldiLevelEditorPlugin.Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard"));
            dummyRC.posterMat = new Material(BaldiLevelEditorPlugin.Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard"));
            dummyRC.florTex = PlusLevelLoaderPlugin.TextureFromAlias(container.floor);
            dummyRC.wallTex = PlusLevelLoaderPlugin.TextureFromAlias(container.wall);
            dummyRC.ceilTex = PlusLevelLoaderPlugin.TextureFromAlias(container.ceiling);
            dummyRC.ec = puppetEnvironmentController;
            dummyRC.GenerateTextureAtlas();
            Texture2D atlas = dummyRC.textureAtlas;
            Destroy(dummyRC.baseMat);
            Destroy(dummyRC.posterMat);
            Destroy(dummyObject);
            return atlas;
        }

        public EditorTile? selectedTile
        {
            get
            {
                if (selectorLocation.x == -1)
                {
                    return null;
                }
                return edTiles[selectorLocation.x, selectorLocation.z];
            }
        }
        Vector2 analogMove = Vector2.zero;
        public Vector2 cursorBounds = Vector2.zero;
        private AnalogInputData movementData = new AnalogInputData()
        {
            steamAnalogId="Movement",
            xAnalogId="MovementX",
            yAnalogId="MovementY",
            steamDeltaId="",
            xDeltaId="",
            yDeltaId=""
        };

        void Initialize()
        {
            initializedDummy = false;
            if (edTiles != null)
            {
                edTiles.ConvertTo1d(edTiles.GetLength(0), edTiles.GetLength(1)).Do(x =>
                {
                    Destroy(x.gameObject);
                });
            }
            edTiles = new EditorTile[level.width, level.height];
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    CreateTile(new ByteVector2(x, y));
                }
            }
            for (int i = 0; i < dummyColliderTransform.childCount; i++)
            {
                Destroy(dummyColliderTransform.GetChild(i).gameObject);
            }
        }

        void Start()
        {
            lineParent = new GameObject().transform;
            lineParent.name = "Line Parent";
            myPawn = new GameObject().transform;
            myPawn.gameObject.name = "Level Editor Pawn";
            myPawn.transform.position += Vector3.up * 5f;
            dummyColliderTransform = new GameObject().transform;
            dummyColliderTransform.gameObject.name = "Dummy Transform";
            tileMap = new GameObject().transform;
            tileMap.gameObject.name = "Tilemap Transform";
            rendererTemplate = new GameObject().AddComponent<SpriteRenderer>();
            rendererTemplate.gameObject.SetActive(false);
            rendererTemplate.transform.name = "SpriteTemplate";
            rendererTemplate.material = new Material(BaldiLevelEditorPlugin.spriteMaterial);
            rendererTemplate.sprite = BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("LinkSprite");
            rendererTemplate.material.SetTexture("_LightMap", Texture2D.whiteTexture);
            lineStart = -1;
            for (int i = 0; i < minLineSprites; i++)
            {
                GetOrAllocateSprite();
            }
            Initialize();

            myCamera?.UpdateTargets(myPawn, 30);
            Singleton<MusicManager>.Instance.PlayMidi(BaldiLevelEditorPlugin.editorThemes[UnityEngine.Random.Range(0,4)], true);
            /*level.areas.Add(new AreaData(new ByteVector2(0, 0), new ByteVector2(3, 12), 1));
            level.areas.Add(new AreaData(new ByteVector2(3, 0), new ByteVector2(10, 3), 1));
            level.areas.Add(new AreaData(new ByteVector2(5, 18), new ByteVector2(4, 4), 1));
            level.UpdateTiles();*/

            selector = new GameObject().AddComponent<EditorSelector>();
            selector.gameObject.name = "Selector";
            SpawnUI();
        }

        public void CompileLevelAsPlayable(string path)
        {
            StartCoroutine(CompileLevel(path));
        }

        SpriteRenderer GetOrAllocateSprite()
        {
            lineStart++;
            if (lineStart < lineSprites.Count)
            {
                return lineSprites[lineStart];
            }
            SpriteRenderer newSprite = GameObject.Instantiate<SpriteRenderer>(rendererTemplate, lineParent);
            newSprite.name = "Segment " + lineStart;
            lineSprites.Add(newSprite);
            return newSprite;
        }
        int lineStart = -1;
        void RenderLines()
        {
            lineSprites.Do(x => x.gameObject.SetActive(false));
            lineStart = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                EditorLine line = lines[i];
                for (int k = 0; k < line.requiredDots; k++)
                {
                    SpriteRenderer renderer = GetOrAllocateSprite();
                    renderer.gameObject.SetActive(true);
                    renderer.gameObject.transform.position = Vector3.Lerp(line.start,line.end, ((float)(k + 1f) / line.requiredDots));
                }
            }
            while (lineSprites.Count > Mathf.Max(lineStart, minLineSprites))
            {
                Destroy(lineSprites[lineSprites.Count - 1].gameObject);
                lineSprites.RemoveAt(lineSprites.Count - 1);
            }
        }
        public void LoadTempPlay()
        {
            if (tempPlayLevel == null)
            {
                audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                throw new Exception("Level Loading failed!");
            }
            SceneObject obj = CustomLevelLoader.LoadLevel(tempPlayLevel);
            obj.manager = BaldiLevelEditorPlugin.mainGameManager;
            GameLoader loader = new GameObject("GameLoader").AddComponent<GameLoader>();
            ElevatorScreen screen = GameObject.Instantiate<ElevatorScreen>(BaldiLevelEditorPlugin.elevatorScreen);
            loader.cgmPre = BaldiLevelEditorPlugin.coreGamePrefab;
            loader.AssignElevatorScreen(screen);
            loader.Initialize(0);
            loader.LoadLevel(obj);
            screen.Initialize();
            loader.SetSave(false); //no saving!
            Destroy(myCamera.camCom.gameObject);
            Destroy(myCamera.canvasCam.gameObject);
            Destroy(myCamera.overlayCam.gameObject);
            Destroy(myCamera.gameObject);
            Destroy(canvas);
            Destroy(this.gameObject); // KILL MEEEEE
        }

        IEnumerator CompileLevel(string path)
        {
            level.UpdateTiles(false);
            selector.gameObject.SetActive(false);
            gearAnimator.SetDefaultAnimation("spin", 1f);
            updateDelay = float.MaxValue;
            CursorController.Instance.DisableClick(true);
            audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/MachineTHEQUEUESYSTEMISBROKEN"));
            //CapsuleCollider collider = GameObject.Instantiate<CapsuleCollider>(BaldiLevelEditorPlugin.playerColliderObject);
            CapsuleCollider collider = BaldiLevelEditorPlugin.playerColliderObject;
            //collider.gameObject.SetActive(true);
            List<Direction> test = new List<Direction>();
            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    level.entitySafeTiles[x, y] = true;
                    level.eventSafeTiles[x, y] = true;
                    if (level.tiles[x, y].type == 16)
                    {
                        level.entitySafeTiles[x, y] = false;
                        level.eventSafeTiles[x, y] = false;
                        continue;
                    }
                    Vector3 vect = IntVectorToWorld(new IntVector2(x, y)) + (Vector3.up * 5f);
                    Collider[] colliders = Physics.OverlapCapsule(vect + (Vector3.up * 3f), vect - (Vector3.up * 2.5f), collider.radius);
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        if (!colliders[i].GetComponent<EditorHasNoCollidersInBaseGame>()) //DUMB DUMB DUMB!
                        {
                            level.entitySafeTiles[x, y] = false;
                            level.eventSafeTiles[x, y] = false;
                            //level.blockedWalls[x, y] = true;
                        }
                    }
                    // handle blocked walls, check each direction to see if there is something blocking it, don't check the center, as this would make tables and chairs block the wall
                    for (int i = 0; i < 4; i++)
                    {
                        Direction dir = (Direction)i;
                        test.Clear();
                        Directions.FillClosedDirectionsFromBin(test, level.tiles[x, y].walls);
                        if (!test.Contains(dir)) continue;
                        //if ((level.tiles[x, y].walls &= (Nybble)(1 << dir.BitPosition())) != 0) continue;
                        Vector3 offset = dir.ToVector3() * (collider.radius * 1.4f);
                        Collider[] subcolliders = Physics.OverlapCapsule(vect + (Vector3.up * 5f) + offset, (vect - (Vector3.up * 2.5f)) + offset, collider.radius);
                        for (int k = 0; k < subcolliders.Length; k++)
                        {
                            if (!subcolliders[k].GetComponent<EditorHasNoCollidersInBaseGame>()) //DUMB DUMB DUMB!
                            {
                                //Debug.Log(subcolliders[k]);
                                level.blockedWalls[x, y] = true;
                                /*SpriteRenderer rend = GetOrAllocateSprite();
                                rend.transform.position = vect + offset;
                                rend.gameObject.SetActive(true);
                                yield return new WaitForSeconds(0.5f);*/
                            }
                        }
                    }
                }
                yield return null;
            }
            level.editorButtons.Do(x =>
            {
                level.blockedWalls[x.position.x, x.position.y] = true;
            });
            audMan.FlushQueue(true);
            selector.gameObject.SetActive(true);
            StartCoroutine(RunThreadAndSpinGear(() =>
            {
                SaveLevelAsPlayable(path);
                FileStream stream = File.OpenRead(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.cbld"));
                BinaryReader reader = new BinaryReader(stream);
                tempPlayLevel = reader.ReadLevel();
                reader.Close();
            }, () =>
            {
                LoadTempPlay();
            }));
            yield break;
        }

        private void SaveLevelAsPlayable(string path)
        {
            level.UpdateTiles(false);
            FileStream stream = File.OpenWrite(path);
            stream.SetLength(0);
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(level);
            writer.Close();
            level.UpdateTiles(true);
        }

        public void SaveLevelAsEditor(string path)
        {
            level.UpdateTiles(true);
            FileStream stream = File.OpenWrite(path);
            stream.SetLength(0);
            BinaryWriter writer = new BinaryWriter(stream);
            level.SaveIntoStream(writer); // saves the editor variant
            writer.Close();
        }

        private EditorLevel? tempLevel;
        private Level? tempPlayLevel;

        public void LoadLevelFromFile(string path)
        {
            FileStream stream = File.OpenRead(path);
            BinaryReader reader = new BinaryReader(stream);
            tempLevel = EditorLevel.LoadFromStream(reader);
            //LoadLevel(EditorLevel.LoadFromStream(reader));
            reader.Close();
        }

        public void LoadLevel(EditorLevel toLoad)
        {
            ClearPrefabSelectState();
            selectorLocation = new IntVector2(-1, -1);
            selector.type = SelectorType.None;
            wallVisuals.Do(x => Destroy(x.gameObject));
            prefabVisuals.Do(x => Destroy(x.gameObject));
            tiledVisuals.Do(x => Destroy(x.gameObject));
            wallVisuals.Clear();
            prefabVisuals.Clear();
            tiledVisuals.Clear();
            level = toLoad;
            Initialize();
            StartCoroutine(LoadSlowly());
            
        }

        IEnumerator LoadSlowly()
        {
            lines.Clear();
            lineSprites.Do(x => x.gameObject.SetActive(false));
            gearAnimator.SetDefaultAnimation("spin", 1f);
            updateDelay = float.MaxValue;
            CursorController.Instance.DisableClick(true);
            audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/MachineTHEQUEUESYSTEMISBROKEN"));
            /*audMan.FlushQueue(true);
            SoundObject loop = BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/MachineLoopLong");
            for (int i = 0; i < 10; i++)
            {
                audMan.QueueAudio(loop);
            }
            yield return null;
            audMan.QueueAudio(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/MachineStart"));
            audMan.PlayQueue();
            yield return null;*/
            int workDone = 0;
            while (!initializedDummy)
            {
                yield return null;
            }
            UpdateAllTextures();
            foreach (var x in level.doors)
            {
                AddDoor(x, BaldiLevelEditorPlugin.doorTypes[x.type]);
                workDone++;
                if (workDone > 4) { workDone = 0; yield return null; }
            }
            foreach (var x in level.windows)
            {
                AddWindow(x);
                workDone++;
                if (workDone > 4) { workDone = 0; yield return null; }
            }
            foreach (var x in level.manualWalls)
            {
                AddWall(x);
                workDone++;
                if (workDone > 4) { workDone = 0; yield return null; }
            }
            foreach (var x in level.prefabs)
            {
                AddPrefab(x);
                workDone++;
                if (workDone > 10) { workDone = 0; yield return null; }
            }
            foreach (var x in level.npcSpawns)
            {
                AddNPC(x);
                yield return null;
            }
            foreach (var x in level.items)
            {
                AddItem(x);
                workDone++;
                if (workDone > 5) { workDone = 0; yield return null; }
            }
            foreach (var x in level.tiledPrefabs)
            {
                AddTiledPrefab(x);
                workDone++;
                if (workDone > 5) { workDone = 0; yield return null; }
            }
            foreach (var x in level.editorButtons)
            {
                AddButton(x);
                yield return null;
            }
            //level.doors.Do(x => AddDoor(x, BaldiLevelEditorPlugin.doorTypes[x.type]));
            //level.windows.Do(x => AddWindow(x));
            //level.manualWalls.Do(x => AddWall(x));
            //level.prefabs.Do(x => AddPrefab(x));
            //level.npcSpawns.Do(x => AddNPC(x));
            //level.items.Do(x => AddItem(x));
            for (int i = 0; i < level.rooms.Count; i++)
            {
                RoomProperties rm = level.rooms[i];
                if (rm.activity == null) continue;
                AddActivity(rm.activity, (ushort)(i + 1));
                yield return null;
            }
            RefreshLevel(true);
            UpdateLines();
            gearAnimator.SetDefaultAnimation("", 0f);
            updateDelay = 0.1f;
            CursorController.Instance.DisableClick(false);
            audMan.FlushQueue(true);
            audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("CashBell"));
            yield break;
        }

        public void RefreshLevel(bool updateTiles = true)
        {
            level.UpdateTiles(true);
            if (updateTiles)
            {
                UpdateAllMeshes();
            }
            UpdateAllVisuals();
            if (updateTiles)
            {
                UpdateAllTextures();
            }
        }

        public void UpdateLines()
        {
            lines.Clear();
            foreach (EditorButtonPlacement placement in level.editorButtons)
            {
                placement.ValidateConnections(level); // JUST TO DOUBLE CHECK LOL!
                Vector3 placementOrigin = wallVisuals.Find(x => x.prefab == placement).gameObject.GetComponent<ButtonEditorVisual>().wallParts[0].transform.position;
                foreach (TiledPrefab tile in placement.connectedTiles)
                {
                    lines.Add(new EditorLine(placementOrigin, tiledVisuals.Find(x => x.prefab == tile).gameObject.GetComponent<IEditorConnectable>().connectionPosition));
                }
                foreach (PrefabLocation prefab in placement.connectedPrefabs)
                {
                    lines.Add(new EditorLine(placementOrigin, prefabVisuals.Find(x => x.location == prefab).gameObject.GetComponent<IEditorConnectable>().connectionPosition));
                }
            }
            RenderLines();
        }

        public void UpdatePrefabVisuals()
        {
            for (int i = prefabVisuals.Count - 1; i >= 0; i--)
            {
                IEditor3D prefabVis = prefabVisuals[i];
                if (!prefabVis.DoesExist(level))
                {
                    if (selectedPrefab == prefabVis)
                    {
                        prefabHighlight = -1;
                        UpdateHighlights();
                    }
                    prefabVisuals.Remove(prefabVis);
                    GameObject.Destroy(prefabVis.gameObject);
                }
            }
        }
        
        public void UpdateTiledVisuals()
        {
            for (int i = tiledVisuals.Count - 1; i >= 0; i--)
            {
                ITileVisual vis = tiledVisuals[i];
                if (!vis.DoesExist(level))
                {
                    tiledVisuals.Remove(vis);
                    GameObject.Destroy(vis.gameObject);
                    UpdateLines();
                }
            }
        }

        public void UpdateAllVisuals()
        {
            // remove deleted door visuals
            List<IWallVisual> toRemove = new List<IWallVisual>();
            wallVisuals.Do(x =>
            {
                if (x.ShouldBeDestroyed(level))
                {
                    toRemove.Add(x);
                }
            });
            for (int i = 0; i < toRemove.Count; i++)
            {
                wallVisuals.Remove(toRemove[i]);
                GameObject.Destroy(toRemove[i].gameObject);
            }
            UpdatePrefabVisuals();
            UpdateTiledVisuals();
        }

        public void UpdateAllMeshes()
        {
            edTiles.ConvertTo1d(level.width, level.height).Do(x =>
            {
                x.UpdateMesh();
            });
        }
        public void UpdateAllTextures()
        {
            edTiles.ConvertTo1d(level.width, level.height).Do(x =>
            {
                x.UpdateTexture();
            });
        }
        public void AddTiledPrefab(TiledPrefab loca)
        {
            if (!level.tiledPrefabs.Contains(loca))
            {
                level.tiledPrefabs.Add(loca);
            }
            ITileVisual spawn = GameObject.Instantiate(BaldiLevelEditorPlugin.tiledPrefabPrefabs[loca.type].gameObject).GetComponent<ITileVisual>();
            spawn.prefab = loca;
            spawn.transform.name = "TileLocation";
            spawn.transform.position = ByteVectorToWorld(loca.position);
            spawn.transform.rotation = loca.direction.ToStandard().ToRotation();
            spawn.gameObject.SetActive(true);
            tiledVisuals.Add(spawn);
        }

        public static bool DoorShouldPrioritizeRooms(string type)
        {
            return (PlusLevelLoaderPlugin.Instance.doorPrefabs[type] is SwingDoor);
        }

        public static bool DoorShouldOrientateTowardsRooms(string type)
        {
            return (!(PlusLevelLoaderPlugin.Instance.doorPrefabs[type] is SwingDoor));
        }

        public bool AddDoor(DoorLocation doorLoca, Type type)
        {
            // TODO: make this a bool of some kind, dont just bulk apply it to swing doors!
            if ((DoorShouldOrientateTowardsRooms(doorLoca.type)) && !level.allowOOBDoors)
            {
                // do some editing to doorLoca to fix weird oddities
                // only do this if we are in a hall room
                ByteVector2 newPos = (doorLoca.position.ToInt() + doorLoca.direction.ToStandard().ToIntVector2()).ToByte();
                ushort roomId = level.GetRoomIDOfPos(newPos, true);
                if (roomId == 0)
                {
                    return false; // door was not in valid location, dont even bother
                }
                if (level.rooms[doorLoca.roomId - 1].type == "hall")
                {
                    if (level.rooms[roomId - 1].type != "hall")
                    {
                        doorLoca.position = newPos;
                        doorLoca.direction = doorLoca.direction.ToStandard().GetOpposite().ToData();
                        //Debug.Log("rerouted door to:" + level.rooms[roomId - 1].type);
                    }
                }
                else
                {
                    if (level.rooms[roomId - 1].type == "library")
                    {
                        doorLoca.position = newPos;
                        doorLoca.direction = doorLoca.direction.ToStandard().GetOpposite().ToData();
                        //Debug.Log("rerouted door to:" + level.rooms[roomId - 1].type);
                    }
                }
            }
            if (!level.doors.Contains(doorLoca))
            {
                level.doors.Add(doorLoca);
            }
            GameObject doorV = new GameObject();
            doorV.name = "DoorRoot";
            DoorEditorVisual visual = (DoorEditorVisual)doorV.AddComponent(type);
            visual.Setup(doorLoca);
            visual.transform.position = ByteVectorToWorld(doorLoca.position);
            wallVisuals.Add(visual);
            return true;
        }

        public void AddNPC(NPCLocation loca)
        {
            if (!level.npcSpawns.Contains(loca))
            {
                level.npcSpawns.Add(loca);
            }
            GameObject npcBase = new GameObject();
            npcBase.name = "Location_" + loca.type;
            GameObject npcV = GameObject.Instantiate(BaldiLevelEditorPlugin.characterObjects[loca.type], npcBase.transform);
            npcV.SetActive(true);
            npcV.transform.localPosition = new Vector3(0f,5f,0f);
            NPCSpawnLocation spawn = npcBase.AddComponent<NPCSpawnLocation>();
            spawn.typedPrefab = loca;
            spawn.transform.position = ByteVectorToWorld(loca.position) + (Vector3.up * 0.01f);
            tiledVisuals.Add(spawn);
        }
        public EditorPrefab AddPrefab(PrefabLocation loca)
        {
            if (!level.prefabs.Contains(loca))
            {
                level.prefabs.Add(loca);
            }
            EditorObjectType type = BaldiLevelEditorPlugin.editorObjects.Find(x => x.name == loca.prefab);
            EditorPrefab edOb = GameObject.Instantiate<EditorPrefab>(type.prefab.gameObject.GetComponent<EditorPrefab>());
            edOb.gameObject.SetActive(true);
            edOb.name = "ObjectLocation";
            edOb.transform.position = loca.position.ToUnity();
            edOb.transform.rotation = loca.rotation.ToUnity();
            edOb.obj = loca;
            prefabVisuals.Add(edOb);
            RefreshLevel(false);
            return edOb;
        }

        public void AddItem(ItemLocation loca)
        {
            if (!level.items.Contains(loca))
            {
                level.items.Add(loca);
            }
            EditorObjectType type = BaldiLevelEditorPlugin.pickupPrefab;
            ItemPrefab edOb = GameObject.Instantiate<ItemPrefab>(type.prefab.gameObject.GetComponent<ItemPrefab>());
            edOb.gameObject.SetActive(true);
            edOb.name = "ObjectLocation";
            edOb.transform.position = loca.position.ToUnity();
            edOb.obj = loca;
            edOb.gameObject.GetComponentInChildren<SpriteRenderer>().sprite = edOb.itemObject.itemSpriteLarge;
            prefabVisuals.Add(edOb);
            RefreshLevel(false);
        }

        public void AddActivity(RoomActivity loca, ushort roomId)
        {
            if (level.rooms[roomId - 1].activity == null)
            {
                level.rooms[roomId - 1].activity = loca;
            }
            EditorObjectType type = BaldiLevelEditorPlugin.editorActivities.Find(x => x.name == loca.activity);
            ActivityPrefab edOb = GameObject.Instantiate<ActivityPrefab>(type.prefab.gameObject.GetComponent<ActivityPrefab>());
            edOb.gameObject.SetActive(true);
            edOb.name = "ObjectLocation";
            edOb.transform.position = loca.position.ToUnity();
            edOb.transform.rotation = loca.direction.ToStandard().ToRotation();
            edOb.obj = loca;
            prefabVisuals.Add(edOb);
            RefreshLevel(false);
        }

        public void AddWindow(WindowLocation windoLoca)
        {
            if (!level.windows.Contains(windoLoca))
            {
                level.windows.Add(windoLoca);
            }
            GameObject doorV = new GameObject();
            doorV.name = "WindowRoot";
            WindowEditorVisual visual = doorV.AddComponent<WindowEditorVisual>();
            visual.Setup(windoLoca);
            visual.transform.position = ByteVectorToWorld(windoLoca.position);
            wallVisuals.Add(visual);
        }

        public void AddWall(WallPlacement wallLoca)
        {
            if (!level.manualWalls.Contains(wallLoca))
            {
                level.manualWalls.Add(wallLoca);
            }
            GameObject doorV = new GameObject();
            doorV.name = "ManualWallRoot";
            ManualWallEditorVisual visual = doorV.AddComponent<ManualWallEditorVisual>();
            visual.isVisible = wallLoca.wall;

            visual.Setup(wallLoca);
            visual.transform.position = ByteVectorToWorld(wallLoca.position);
            wallVisuals.Add(visual);
        }

        public void AddButton(EditorButtonPlacement wallLoca)
        {
            if (!level.editorButtons.Contains(wallLoca))
            {
                level.editorButtons.Add(wallLoca);
            }
            GameObject doorV = new GameObject();
            doorV.name = "ButtonRoot";
            ButtonEditorVisual visual = doorV.AddComponent<ButtonEditorVisual>();
            visual.Setup(wallLoca);
            visual.transform.position = ByteVectorToWorld(wallLoca.position);
            wallVisuals.Add(visual);
        }

        public void UpdateHighlights()
        {
            selectedArea = null;
            selector.gameObject.SetActive(selectedTile != null);
            selector.transform.position = ByteVectorToWorld(new ByteVector2(selectorLocation.x,selectorLocation.z));
            TiledArea? data = level.GetAreaOfPos(new ByteVector2(selectorLocation.x, selectorLocation.z));
            // todo: why the fuck is this here??? shouldn't state switching handle this?
            selector.arrows.Do(x =>
            {
                x.SetActive(true);
            });
            prefabVisuals.Do(x =>
            {
                x.highlight = "none";
            });
            tiledVisuals.Do(x =>
            {
                x.highlight = "none";
            });
            if (selectedPrefab != null)
            {
                selectedPrefab.highlight = "yellow";
                selector.transform.position = selectedPrefab.transform.position;
                if (selector.type != SelectorType.PrefabRotate)
                {
                    selector.type = SelectorType.PrefabSelect;
                }
                // ugly hack
                selector.arrows.Do(x =>
                {
                    x.SetActive(false);
                });
                selector.prefabRotations[0].transform.localPosition = selectedPrefab.transform.forward * 10f;
                selector.gameObject.SetActive(true);
                edTiles.ConvertTo1d(level.width, level.height).Do(x =>
                {
                    x.highlight = "none";
                });
                wallVisuals.Do(x => x.highlight = "none");
                return;
            }
            if (data != null)
            {
                selector.type = SelectorType.Area;
                if (data is AreaData) //if we have highlighted an square area, move the arrows to the appropiate locations
                {
                    AreaData area = (AreaData)data;
                    selectedArea = area;
                    // North
                    selector.arrows[0].transform.position = IntVectorToWorld(new IntVector2(area.origin.x + (area.size.x / 2), area.origin.y + area.size.y)) + (Vector3.up * 0.25f);
                    // East
                    selector.arrows[1].transform.position = IntVectorToWorld(new IntVector2(area.origin.x + area.size.x, area.origin.y + (area.size.y / 2))) + (Vector3.up * 0.25f);
                    // South
                    selector.arrows[2].transform.position = IntVectorToWorld(new IntVector2(area.origin.x + (area.size.x / 2), (int)(area.origin.y) - 1)) + (Vector3.up * 0.25f);
                    // West
                    selector.arrows[3].transform.position = IntVectorToWorld(new IntVector2(area.origin.x - 1, area.origin.y + (area.size.y / 2))) + (Vector3.up * 0.25f);
                }
                else //we are in an area that can not be sized with the selector arrows (typically an elevator), so disable them completely
                {
                    selector.arrows.Do(x =>
                    {
                        x.SetActive(false);
                    });
                }
                edTiles.ConvertTo1d(level.width, level.height).Do(x =>
                {
                    x.highlight = data.VectorIsInArea(x.edTile.position) ? "yellow" : "none";
                });
                return;
            }
            selector.type = SelectorType.Tile;
            edTiles.ConvertTo1d(level.width, level.height).Do(x =>
            {
                x.highlight = x == selectedTile ? "yellow" : "none";
            });
            wallVisuals.Do(x => x.highlight = "none");
        }

        public void SelectTool(EditorTool? tool)
        {
            selector.gameObject.SetActive(true);
            selectedTool = tool;
            updateDelay = 0.1f;
            ClearPrefabSelectState();
            if (tool == null)
            {
                selector.type = SelectorType.None;
                selectorLocation = new IntVector2(-1, 0);
                UpdateHighlights();
                return;
            }
            edTiles.ConvertTo1d(level.width, level.height).Do(x =>
            {
                x.highlight = "none";
            });
            prefabVisuals.Do(x => x.highlight = "none");
            tiledVisuals.Do(x => x.highlight = "none");
            wallVisuals.Do(x => x.highlight = "none");
            selector.type = SelectorType.HoldingItem;
        }

        public Vector3 ByteVectorToWorld(ByteVector2 vector)
        {
            return new Vector3((vector.x * 10f) + 5f, 0f, (vector.y * 10f) + 5f);
        }

        public Vector3 IntVectorToWorld(IntVector2 vector)
        {
            return new Vector3((vector.x * 10f + 5f), 0f, (vector.z * 10f + 5f));
        }

        public IntVector2 WorldToIntVector(Vector3 position)
        {
            float x = (position.x - 5f) / 10f;
            float y = (position.z - 5f) / 10f;
            return new IntVector2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        public EditorTile CreateTile(ByteVector2 position) //create a tile object at the specified position
        {
            EditorTile tile = new GameObject().AddComponent<EditorTile>();
            tile.transform.SetParent(tileMap, false);
            tile.position = position;
            tile.transform.position = ByteVectorToWorld(position);
            tile.name = position.x + "," + position.y;
            edTiles[position.x, position.y] = tile;
            return tile;
        }

        public void CreateDummyFloor(IntVector2 position)
        {
            /*EditorTile template = edTiles[0, 0];
            GameObject clone = GameObject.Instantiate(template.transform.Find("FloorCollider").gameObject, dummyColliderTransform);*/
            GameObject clone = new GameObject();
            clone.transform.SetParent(dummyColliderTransform, true);
            clone.name = name;
            MeshFilter filter = clone.AddComponent<MeshFilter>();
            filter.mesh = BaldiLevelEditorPlugin.Instance.assetMan.Get<Mesh>("Quad");
            clone.AddComponent<MeshCollider>();
            clone.transform.localScale = new Vector3(10f, 10f, 1f);
            clone.transform.eulerAngles = new Vector3(90f, 0f, 0f);
            clone.name = "DummyCollider";
            clone.transform.position = IntVectorToWorld(position);
        }

        void MovePawn() //move the pawn/camera
        {
            if (Singleton<InputManager>.Instance.GetDigitalInput("UseItem", false))
            {
                Vector2 analog = (Vector2)_deltaThisFrame.GetValue(cursor);
                cameraRotation += (new Vector3(-analog.y, analog.x, 0f) * 1.15f);
                cameraRotation.x = Mathf.Clamp(cameraRotation.x, -89f, 89f);
                myPawn.eulerAngles = cameraRotation;
            }
            Singleton<InputManager>.Instance.GetAnalogInput(movementData, out analogMove, out _);
            float moveSpeed = Singleton<InputManager>.Instance.GetDigitalInput("Run", false) ? 125f : 50f;
            myPawn.position += myPawn.forward * analogMove.y * Time.deltaTime * moveSpeed;
            myPawn.position += myPawn.right * analogMove.x * Time.deltaTime * moveSpeed;
            //myPawn.position = new Vector3(myPawn.position.x, Mathf.Max(0.1f, myPawn.position.y), myPawn.position.z); //stop the player from going under the map
        }

        void ClearPrefabSelectState()
        {
            prefabHandleDirection = null;
            if (selectedPrefab != null)
            {
                selectedPrefab.collidersEnabled = true;
            }
            prefabHighlight = -1;
        }

        void HandleHoldingItemState()
        {
            if (selectedTool == null) throw new Exception("In HoldingItem selector state without a valid tool!");
            if (!rayHit)
            {
                selectedTool.hitTransform = null;
            }
            else
            {
                selectedTool.hitTransform = hit.transform;
            }
            selectedTool.OnHover(highlightedLocation);
            if (selector.type == SelectorType.HoldingItem)
            {
                selector.transform.position = IntVectorToWorld(highlightedLocation);
                if (Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", true))
                {
                    selectedTool.OnDrop(highlightedLocation);
                }
            }
            if (selector.type == SelectorType.ItemSelectDirection)
            {
                for (int i = 0; i < selector.arrows.Length; i++)
                {
                    if (selector.arrows[i].transform == hit.transform)
                    {
                        selectedTool.OnDirectionSelect((Direction)i, Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", true));
                        return;
                    }
                }
            }
        }

        void HandleSelect()
        {
            if (selector.type == SelectorType.Area)
            {
                for (int i = 0; i < selector.arrows.Length; i++)
                {
                    if (selector.arrows[i].transform == hit.transform)
                    {
                        selector.type = SelectorType.AreaDragging;
                        handleStart = new IntVector2(highlightedLocation.x, highlightedLocation.z);
                        handleDirection = (Direction)i;
                        return;
                    }
                }
            }
            if (hit.transform.name == "ObjectLocation")
            {
                ClearPrefabSelectState();
                prefabHighlight = prefabVisuals.FindIndex(x => (x == hit.transform.GetComponent<IEditor3D>()));
                UpdateHighlights();
                if (selectedPrefab != null)
                {
                    selectedPrefab.collidersEnabled = false;
                }
                return;
            }
            if ((selector.type == SelectorType.PrefabSelect)) //dont do this if we are currently rotating
            {
                if (selectedPrefab == null) throw new Exception("selectedPrefab is null!");
                for (int i = 0; i < selector.prefabArrows.Length; i++)
                {
                    if (selector.prefabArrows[i].transform == hit.transform)
                    {
                        prefabHandleStart = hit.transform.position;
                        prefabHandleOffset = selectedPrefab.transform.position - hit.transform.position;
                        prefabHandleDirection = EditorSelector.rotationVectors[i];
                    }
                }
            }
            EditorTile? foundTile = null;
            if (hit.transform.name.StartsWith("WallCollider"))
            {
                foundTile = hit.transform.parent.parent.GetComponent<EditorTile>();
            }
            else if (hit.transform.name == "FloorCollider")
            {
                foundTile = hit.transform.parent.GetComponent<EditorTile>();
            }
            if (foundTile == null) return;
            ClearPrefabSelectState();
            selectorLocation = foundTile.position.ToInt();
            UpdateHighlights();
        }

        void HandlePrefabSelect()
        {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            if (prefabHandleDirection != null)
            {
                selector.prefabRotations[0].SetActive(false);
                if (prefabHandleStart == null) throw new Exception("prefabHandleStart is NULL!");
                if (selectedPrefab == null) throw new Exception("selectedPrefab is NULL!");
                if (prefabHandleOffset == null) throw new Exception("prefabHandleOffset is NULL!");
                float snapToGrid = 0.25f; // TODO: make this an option
                prefabHandleEnd = Vector3.Scale(ray.origin + (ray.direction *= Vector3.Distance(prefabHandleStart.Value, ray.origin)), new Vector3(Mathf.Abs(prefabHandleDirection.Value.x), Mathf.Abs(prefabHandleDirection.Value.y), Mathf.Abs(prefabHandleDirection.Value.z)));
                prefabHandleEnd += Vector3.Scale(prefabHandleStart.Value, new Vector3(1 - Mathf.Abs(prefabHandleDirection.Value.x), 1 - Mathf.Abs(prefabHandleDirection.Value.y), 1 - Mathf.Abs(prefabHandleDirection.Value.z)));
                selectedPrefab.transform.position = prefabHandleEnd.Value + prefabHandleOffset.Value;
                selectedPrefab.transform.position = new Vector3(Mathf.Round(selectedPrefab.transform.position.x / snapToGrid) * snapToGrid, Mathf.Round(selectedPrefab.transform.position.y / snapToGrid) * snapToGrid, Mathf.Round(selectedPrefab.transform.position.z / snapToGrid) * snapToGrid);
                prefabHandleStart = prefabHandleEnd;
                selector.transform.position = selectedPrefab.transform.position;
                if (!Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", false))
                {
                    prefabHandleDirection = null;
                    selectedPrefab.UpdateObject();
                    selectedPrefab.DoneUpdateObject();
                    RefreshLevel(false);
                }
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            }
            else
            {
                if (selectedPrefab == null) throw new Exception("In PrefabSelect selector state without selected prefab!");
                selector.prefabRotations[0].SetActive(true);
                if (Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", false))
                {
                    if (hit.transform == null) return;
                    if (hit.transform.parent == selector.prefabRotations[0].transform)
                    {
                        selector.type = SelectorType.PrefabRotate;
                    }
                }
            }
        }

        void HandlePrefabRotate()
        {
            Vector3 baseVector = Vector3.Scale(ray.origin + (ray.direction *= Vector3.Distance(selector.prefabRotations[0].transform.position, ray.origin)), new Vector3(1f, 0f, 1f));
            Quaternion q = Quaternion.LookRotation(baseVector - new Vector3(selectedPrefab.transform.position.x, 0f, selectedPrefab.transform.position.z));
            float gridSnap = 5f;
            q.eulerAngles = new Vector3(Mathf.Round(q.eulerAngles.x / gridSnap) * gridSnap, Mathf.Round(q.eulerAngles.y / gridSnap) * gridSnap, Mathf.Round(q.eulerAngles.z / gridSnap) * gridSnap);
            selectedPrefab.transform.rotation = q;
            selectedPrefab.UpdateObject();
            UpdateHighlights();
            if (!Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", false))
            {
                selector.type = SelectorType.PrefabSelect;
                selectedPrefab.DoneUpdateObject();
                UpdateHighlights();
            }
        }

        void HandleAreaDrag()
        {
            if (selectedArea == null) throw new Exception("selectedArea NULL while trying to drag!");
            if (handleStart == null) throw new Exception("handleStart NULL while trying to drag!");
            handleEnd = new IntVector2(highlightedLocation.x, highlightedLocation.z);
            IntVector2 difference = handleEnd.Value - handleStart.Value;
            IntVector2 sizeDif = new IntVector2(0, 0);
            IntVector2 posDif = new IntVector2(0, 0);
            switch (handleDirection) // handle the movement of each arrow individually
            {
                case Direction.North:
                    sizeDif = new IntVector2(0, difference.z);
                    break;
                case Direction.South:
                    sizeDif = new IntVector2(0, -difference.z);
                    posDif = new IntVector2(0, difference.z);
                    break;
                case Direction.East:
                    sizeDif = new IntVector2(difference.x, 0);
                    break;
                case Direction.West:
                    sizeDif = new IntVector2(-difference.x, 0);
                    posDif = new IntVector2(difference.x, 0);
                    break;
            }
            if (!Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", false))
            {
                selector.type = SelectorType.Area;
                ByteVector2 targetSize = (selectedArea.size.ToInt() + sizeDif).Max(new IntVector2(1, 1)).ToByte();
                ByteVector2 targetPosition = (selectedArea.origin.ToInt() + posDif).ToByte();
                AreaData data = new AreaData(targetPosition, targetSize, 0);
                if (!level.CollidesWithAreas(data, selectedArea)) //make sure we won't be causing any areas to be ontop of eachother
                {
                    selectedArea.size = targetSize;
                    selectedArea.origin = targetPosition;
                    RefreshLevel(true);
                    UpdateHighlights();
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Sfx_Button_Press"));
                }
                else
                {
                    UpdateHighlights();
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                }
            }
            else
            {
                Vector3 oldPos = selector.arrows[(int)handleDirection].transform.position;
                IntVector2 vector = handleEnd.Value.LockAxis(handleStart.Value, handleDirection);
                selector.arrows[(int)handleDirection].transform.position = IntVectorToWorld(vector) + (Vector3.up * 0.25f);
                if (selector.arrows[(int)handleDirection].transform.position != oldPos)
                {
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Sfx_Button_Unpress"));
                }
            }
        }

        //public CanvasScaler scaler;
        void Update()
        {
            if (myCamera == null) return;
            //Singleton<GlobalCam>.Instance.transform.position = transform.position;
            if (cursor == null)
            {
                cursor = GameObject.FindObjectOfType<CursorController>();
                //scaler = canvas.GetComponent<CanvasScaler>();
                _useRawPosition.SetValue(cursor, true);
                return;
            }
            canvas.scaleFactor = originalScale; //what??
            if (!initializedDummy)
            {
                for (int x = -1; x < level.width + 1; x++)
                {
                    for (int y = -1; y < level.height + 1; y++)
                    {
                        if (level.GetTileSafe(x, y) == null)
                        {
                            CreateDummyFloor(new IntVector2(x, y));
                        }
                    }
                }
                initializedDummy = true;
            }
            if (state == LevelEditorState.InMenu) return;
            bool cursorOverUI = ((List<RaycastResult>)cc_results.GetValue(cursor)).Count != 0;
            MovePawn();
            if (updateDelay > 0f)
            {
                updateDelay -= Time.deltaTime;
                return;
            }
            if (Singleton<InputManager>.Instance.GetDigitalInput("Pause", true))
            {
                if (selectedTool != null)
                {
                    selectedTool.OnEscape();
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Boink"));
                }
            }
            Vector3 pos = new Vector3((cursor.LocalPosition.x / cursorBounds.x) * Screen.width, Screen.height + ((cursor.LocalPosition.y / cursorBounds.y) * Screen.height));
            ray = myCamera.camCom.ScreenPointToRay(pos);
            rayHit = (Physics.Raycast(ray, out hit, 1000f, 2363401));
            if (rayHit)
            {
                highlightedLocation = WorldToIntVector(hit.transform.position);
            }
            if (selector.inItemHoldingState)
            {
                HandleHoldingItemState();
                return;
            }
            // if we click and we are not actively dragging an area, try to select the tile we have clicked
            if ((Singleton<InputManager>.Instance.GetDigitalInput("MouseSubmit", true) && rayHit) && (selector.type != SelectorType.AreaDragging) && !cursorOverUI)
            {
                HandleSelect();
            }
            // if we are dragging an area, figure out where the cursor currently is in world and calculate the new bounds.
            if ((selector.type == SelectorType.PrefabSelect))
            {
                HandlePrefabSelect();
            }
            if (selector.type == SelectorType.PrefabRotate)
            {
                HandlePrefabRotate();
            }
            if (rayHit && (selector.type == SelectorType.AreaDragging))
            {
                HandleAreaDrag();
            }
        }
    }
}
