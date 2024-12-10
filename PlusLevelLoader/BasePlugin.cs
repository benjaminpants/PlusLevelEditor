using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using PlusLevelFormat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader
{

    [BepInPlugin("mtm101.rulerp.baldiplus.levelloader", "Baldi's Basics Plus Level Loader", "0.1.0.2")]
    public class PlusLevelLoaderPlugin : BaseUnityPlugin
    {
        public static PlusLevelLoaderPlugin Instance;
        public static Level level;

        public static Texture2D TextureFromAlias(string alias)
        {
            if (!Instance.textureAliases.ContainsKey(alias))
            {
                return Instance.assetMan.Get<Texture2D>("Placeholder_Wall"); //return placeholder
            }
            return Instance.textureAliases[alias];
        }

        public AssetManager assetMan = new AssetManager();

        public Dictionary<string, Texture2D> textureAliases = new Dictionary<string, Texture2D>();
        public Dictionary<string, RoomSettings> roomSettings = new Dictionary<string, RoomSettings>();
        public Dictionary<string, Door> doorPrefabs = new Dictionary<string, Door>();
        public Dictionary<string, WindowObject> windowObjects = new Dictionary<string, WindowObject>();
        public Dictionary<string, GameObject> prefabAliases = new Dictionary<string, GameObject>();
        public Dictionary<string, TileBasedObject> tileAliases = new Dictionary<string, TileBasedObject>();
        public Dictionary<string, Activity> activityAliases = new Dictionary<string, Activity>();
        public Dictionary<string, NPC> npcAliases = new Dictionary<string, NPC>();
        public Dictionary<string, ItemObject> itemObjects = new Dictionary<string, ItemObject>();
        public Dictionary<string, GameButtonBase> buttons = new Dictionary<string, GameButtonBase>(); //rest in pieces lever...
        public Dictionary<string, PosterObject> posters = new Dictionary<string, PosterObject>();
        public Dictionary<string, Elevator> elevators = new Dictionary<string, Elevator>();
        public Dictionary<string, Transform> lightAliases = new Dictionary<string, Transform>();

        /*void OptMenPlaceholder(OptionsMenu __instance)
        {
            GameObject obj = CustomOptionsCore.CreateNewCategory(__instance, "LOADER");
            StandardMenuButton but = CustomOptionsCore.CreateApplyButton(__instance, "LOAD THE MAP YEAH FUCK YOU!!", () =>
            {
                FileStream stream = File.OpenRead("C:\\Users\\User1\\OneDrive\\Desktop\\experimentalhellscape\\BALDI_Data\\StreamingAssets\\test.lvl");
                BinaryReader reader = new BinaryReader(stream);
                level = reader.ReadLevel();
                LevelAsset asset = CustomLevelLoader.LoadLevel(level);
                reader.Close();
                Resources.FindObjectsOfTypeAll<SceneObject>().Do(x =>
                {
                    x.levelAsset = asset;
                    x.MarkAsNeverUnload();
                    if (x.extraAsset == null) return;
                    x.extraAsset.minLightColor = Color.white;
                    x.extraAsset.npcsToSpawn = new List<NPC>();
                });
            });
            but.transform.SetParent(obj.transform, false);
        }*/

        IEnumerator OnAssetsLoaded()
        {
            yield return 4;
            yield return "Adding Texture Aliases...";
            assetMan.AddFromResources<Texture2D>();
            assetMan.AddFromResources<Material>();
            assetMan.AddFromResources<Door>();
            assetMan.AddFromResources<Cubemap>();
            assetMan.AddFromResources<GameButtonBase>();
            assetMan.Add<Elevator>("ElevatorPrefab", Resources.FindObjectsOfTypeAll<Elevator>().First());
            assetMan.Remove<Door>("Door_Swinging");
            assetMan.Add<Door>("Door_Swinging", Resources.FindObjectsOfTypeAll<Door>().Where(x => x.name == "Door_Swinging").Where(x => x.transform.parent == null).First());
            assetMan.AddFromResources<WindowObject>();
            assetMan.AddFromResources<StandardDoorMats>();
            textureAliases.Add("HallFloor", assetMan.Get<Texture2D>("TileFloor"));
            textureAliases.Add("Wall", assetMan.Get<Texture2D>("Wall"));
            textureAliases.Add("Ceiling", assetMan.Get<Texture2D>("CeilingNoLight"));
            textureAliases.Add("BlueCarpet", assetMan.Get<Texture2D>("Carpet"));
            textureAliases.Add("FacultyWall", assetMan.Get<Texture2D>("WallWithMolding"));
            textureAliases.Add("Actual", assetMan.Get<Texture2D>("ActualTileFloor"));
            textureAliases.Add("ElevatorCeiling", assetMan.Get<Texture2D>("ElCeiling"));
            textureAliases.Add("Grass", assetMan.Get<Texture2D>("Grass"));
            textureAliases.Add("Fence", assetMan.Get<Texture2D>("fence"));
            textureAliases.Add("JohnnyWall", assetMan.Get<Texture2D>("JohnnyWall"));
            textureAliases.Add("None", assetMan.Get<Texture2D>("Transparent"));

            yield return "Setting Up Room Settings...";
            List<RoomFunctionContainer> roomFunctions = Resources.FindObjectsOfTypeAll<RoomFunctionContainer>().ToList();
            roomSettings.Add("hall", new RoomSettings(RoomCategory.Hall, RoomType.Hall, Color.white, assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("class", new RoomSettings(RoomCategory.Class, RoomType.Room, Color.green, assetMan.Get<StandardDoorMats>("ClassDoorSet"), assetMan.Get<Material>("MapTile_Classroom")));
            roomSettings.Add("faculty", new RoomSettings(RoomCategory.Faculty, RoomType.Room, Color.red, assetMan.Get<StandardDoorMats>("FacultyDoorSet"), assetMan.Get<Material>("MapTile_Faculty")));
            roomSettings.Add("office", new RoomSettings(RoomCategory.Office, RoomType.Room, new Color(1f,1f,0f), assetMan.Get<StandardDoorMats>("PrincipalDoorSet"), assetMan.Get<Material>("MapTile_Office")));
            roomSettings.Add("closet", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(1f, 0.6214f, 0f), assetMan.Get<StandardDoorMats>("SuppliesDoorSet")));
            roomSettings.Add("reflex", new RoomSettings(RoomCategory.Null, RoomType.Room, new Color(1f, 1f, 1f), assetMan.Get<StandardDoorMats>("DoctorDoorSet")));
            roomSettings.Add("library", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("cafeteria", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("outside", new RoomSettings(RoomCategory.Special, RoomType.Room, new Color(0f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings.Add("shop", new RoomSettings(RoomCategory.Store, RoomType.Room, new Color(1f, 1f, 1f), assetMan.Get<StandardDoorMats>("ClassDoorSet")));
            roomSettings["faculty"].container = roomFunctions.Find(x => x.name == "FacultyRoomFunction");
            roomSettings["office"].container = roomFunctions.Find(x => x.name == "OfficeRoomFunction");
            roomSettings["class"].container = roomFunctions.Find(x => x.name == "ClassRoomFunction");
            roomSettings["library"].container = roomFunctions.Find(x => x.name == "LibraryRoomFunction");
            roomSettings["cafeteria"].container = roomFunctions.Find(x => x.name == "CafeteriaRoomFunction");
            roomSettings["outside"].container = roomFunctions.Find(x => x.name == "PlaygroundRoomFunction");
            roomSettings["shop"].container = roomFunctions.Find(x => x.name == "JohnnyStoreRoomFunction");

            yield return "Setting Up Prefabs...";
            windowObjects.Add("standard", assetMan.Get<WindowObject>("WoodWindow"));
            doorPrefabs.Add("standard", assetMan.Get<Door>("ClassDoor_Standard"));
            doorPrefabs.Add("swing", assetMan.Get<Door>("Door_Swinging"));
            doorPrefabs.Add("autodoor", assetMan.Get<Door>("Door_Auto"));
            doorPrefabs.Add("coin", assetMan.Get<Door>("Door_SwingingCoin"));
            doorPrefabs.Add("oneway", assetMan.Get<Door>("Door_SwingingOneWay")); //FilingCabinet_Tall
            doorPrefabs.Add("swingsilent", assetMan.Get<Door>("SilentDoor_Swinging"));
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            prefabAliases.Add("desk", objects.Where(x => x.name == "Table_Test").First());
            prefabAliases.Add("bigdesk", objects.Where(x => x.name == "BigDesk").First());
            prefabAliases.Add("cabinettall", objects.Where(x => x.name == "FilingCabinet_Tall").First());
            prefabAliases.Add("chair", objects.Where(x => x.name == "Chair_Test").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("computer", objects.Where(x => x.name == "MyComputer").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("computer_off", objects.Where(x => x.name == "MyComputer_Off").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("roundtable", objects.Where(x => x.name == "RoundTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("locker", objects.Where(x => x.name == "Locker").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bluelocker", objects.Where(x => x.name == "BlueLocker").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("greenlocker", objects.Where(x => x.name == "StorageLocker").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_pencilnotes", objects.Where(x => x.name == "Decor_PencilNotes").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_papers", objects.Where(x => x.name == "Decor_Papers").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_globe", objects.Where(x => x.name == "Decor_Globe").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_notebooks", objects.Where(x => x.name == "Decor_Notebooks").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_lunch", objects.Where(x => x.name == "Decor_Lunch").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bookshelf", objects.Where(x => x.name == "Bookshelf_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bookshelf_hole", objects.Where(x => x.name == "Bookshelf_Hole_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("rounddesk", objects.Where(x => x.name == "RoundDesk").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("cafeteriatable", objects.Where(x => x.name == "CafeteriaTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("dietbsodamachine", objects.Where(x => x.name == "DietSodaMachine").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bsodamachine", objects.Where(x => x.name == "SodaMachine").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("zestymachine", objects.Where(x => x.name == "ZestyMachine").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("crazymachine_bsoda", objects.Where(x => x.name == "CrazyVendingMachineBSODA").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("crazymachine_zesty", objects.Where(x => x.name == "CrazyVendingMachineZesty").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("waterfountain", objects.Where(x => x.name == "WaterFountain").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("counter", objects.Where(x => x.name == "Counter").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("examination", objects.Where(x => x.name == "ExaminationTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("ceilingfan", objects.Where(x => x.name == "CeilingFan").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("merrygoround", objects.Where(x => x.name == "MerryGoRound_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("tree", objects.Where(x => x.name == "TreeCG").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("pinetree", objects.Where(x => x.name == "PineTree").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("appletree", objects.Where(x => x.name == "AppleTree").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("bananatree", objects.Where(x => x.name == "BananaTree").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("hoop", objects.Where(x => x.name == "HoopBase").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("payphone", objects.Where(x => x.name == "PayPhone").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("tapeplayer", objects.Where(x => x.name == "TapePlayer").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("plant", objects.Where(x => x.name == "Plant").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_banana", objects.Where(x => x.name == "Decor_Banana").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("decor_zoneflag", objects.Where(x => x.name == "Decor_ZoningFlag").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("hopscotch", objects.Where(x => x.name == "PlaygroundPavement").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("chairsanddesk", objects.Where(x => x.name == "Chairs_Desk_Perfect").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("picnictable", objects.Where(x => x.name == "PicnicTable").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("tent", objects.Where(x => x.name == "Tent_Object").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("rock", objects.Where(x => x.name == "Rock").Where(x => x.transform.parent == null).First());
            prefabAliases.Add("picnicbasket", objects.Where(x => x.name == "PicnicBasket").Where(x => x.transform.parent == null).First());

            TileBasedObject[] tiledObjects = Resources.FindObjectsOfTypeAll<TileBasedObject>();

            tileAliases.Add("lockdowndoor", tiledObjects.Where(x => x.name == "LockdownDoor").Where(x => x.transform.parent == null).First());

            Activity[] activites = Resources.FindObjectsOfTypeAll<Activity>();
            activityAliases.Add("notebook", activites.Where(x => x.name == "NoActivity").First());
            activityAliases.Add("mathmachine", activites.Where(x => (x.name == "MathMachine" && (x.transform.parent == null))).First());
            activityAliases.Add("mathmachine_corner", activites.Where(x => (x.name == "MathMachine_Corner" && (x.transform.parent == null))).First());
            npcAliases.Add("baldi", MTM101BaldiDevAPI.npcMetadata.Get(Character.Baldi).value);
            npcAliases.Add("principal", MTM101BaldiDevAPI.npcMetadata.Get(Character.Principal).value);
            npcAliases.Add("sweep", MTM101BaldiDevAPI.npcMetadata.Get(Character.Sweep).value);
            npcAliases.Add("playtime", MTM101BaldiDevAPI.npcMetadata.Get(Character.Playtime).value);
            npcAliases.Add("chalkface", MTM101BaldiDevAPI.npcMetadata.Get(Character.Chalkles).value);
            npcAliases.Add("bully", MTM101BaldiDevAPI.npcMetadata.Get(Character.Bully).value);
            npcAliases.Add("beans", MTM101BaldiDevAPI.npcMetadata.Get(Character.Beans).value);
            npcAliases.Add("prize", MTM101BaldiDevAPI.npcMetadata.Get(Character.Prize).value);
            npcAliases.Add("crafters", MTM101BaldiDevAPI.npcMetadata.Get(Character.Crafters).value);
            npcAliases.Add("pomp", MTM101BaldiDevAPI.npcMetadata.Get(Character.Pomp).value);
            npcAliases.Add("test", MTM101BaldiDevAPI.npcMetadata.Get(Character.LookAt).value);
            npcAliases.Add("cloudy", MTM101BaldiDevAPI.npcMetadata.Get(Character.Cumulo).value);
            npcAliases.Add("reflex", MTM101BaldiDevAPI.npcMetadata.Get(Character.DrReflex).value);

            yield return "Setting Up Items...";
            itemObjects.Add("quarter", ItemMetaStorage.Instance.FindByEnum(Items.Quarter).value);
            itemObjects.Add("keys", ItemMetaStorage.Instance.FindByEnum(Items.DetentionKey).value);
            itemObjects.Add("zesty", ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value);
            itemObjects.Add("whistle", ItemMetaStorage.Instance.FindByEnum(Items.PrincipalWhistle).value);
            itemObjects.Add("teleporter", ItemMetaStorage.Instance.FindByEnum(Items.Teleporter).value);
            itemObjects.Add("dietbsoda", ItemMetaStorage.Instance.FindByEnum(Items.DietBsoda).value);
            itemObjects.Add("bsoda", ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value);
            itemObjects.Add("boots", ItemMetaStorage.Instance.FindByEnum(Items.Boots).value);
            itemObjects.Add("clock", ItemMetaStorage.Instance.FindByEnum(Items.AlarmClock).value);
            itemObjects.Add("dirtychalk", ItemMetaStorage.Instance.FindByEnum(Items.ChalkEraser).value);
            itemObjects.Add("grapple", ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).value);
            itemObjects.Add("nosquee", ItemMetaStorage.Instance.FindByEnum(Items.Wd40).value);
            itemObjects.Add("nametag", ItemMetaStorage.Instance.FindByEnum(Items.Nametag).value);
            itemObjects.Add("tape", ItemMetaStorage.Instance.FindByEnum(Items.Tape).value);
            itemObjects.Add("scissors", ItemMetaStorage.Instance.FindByEnum(Items.Scissors).value);
            itemObjects.Add("apple", ItemMetaStorage.Instance.FindByEnum(Items.Apple).value);
            itemObjects.Add("swinglock", ItemMetaStorage.Instance.FindByEnum(Items.DoorLock).value);
            itemObjects.Add("portalposter", ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value);
            itemObjects.Add("banana", ItemMetaStorage.Instance.FindByEnum(Items.NanaPeel).value);
            itemObjects.Add("points25", ItemMetaStorage.Instance.GetPointsObject(25, true));
            itemObjects.Add("points50", ItemMetaStorage.Instance.GetPointsObject(50, true));
            itemObjects.Add("points100", ItemMetaStorage.Instance.GetPointsObject(100, true));
            itemObjects.Add("buspass", ItemMetaStorage.Instance.FindByEnum(Items.BusPass).value);
            Resources.FindObjectsOfTypeAll<PosterObject>().Do(x =>
            {
                if (x.GetInstanceID() >= 0)
                {
                    posters.Add(x.name, x);
                }
            });
            buttons.Add("button", assetMan.Get<GameButtonBase>("GameButton"));
            elevators.Add("elevator", PlusLevelLoaderPlugin.Instance.assetMan.Get<Elevator>("ElevatorPrefab"));
            lightAliases.Add("fluorescent", objects.First(x => (x.name == "FluorescentLight" && (x.transform.parent == null))).transform);
            lightAliases.Add("none", null);
            yield break;
        }

        void Awake()
        {
            LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoaded(), false);
            Instance = this;
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.levelloader");

            harmony.PatchAllConditionals();
        }
    }
}
