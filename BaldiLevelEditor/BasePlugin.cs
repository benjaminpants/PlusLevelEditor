using BaldiLevelEditor.Types;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.UI;
using PlusLevelFormat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BaldiLevelEditor
{

    public class EditorPrebuiltStucture
    {
        public List<PrefabLocation> prefabs = new List<PrefabLocation>();
        public Vector3 origin = Vector3.zero;

        public EditorPrebuiltStucture(params PrefabLocation[] _prefabs)
        {
            prefabs = _prefabs.ToList();
        }
    }

    [BepInPlugin("mtm101.rulerp.baldiplus.leveleditor", "Baldi's Basics Plus Level Editor", "0.1.0.2")]
    public class BaldiLevelEditorPlugin : BaseUnityPlugin
    {

        public static Dictionary<string, Type> doorTypes = new Dictionary<string, Type>();
        public static Dictionary<string, ITileVisual> tiledPrefabPrefabs = new Dictionary<string, ITileVisual>();

        public static bool isFucked { get; private set; }

        public static List<EditorObjectType> editorObjects = new List<EditorObjectType>();

        public static List<EditorObjectType> editorActivities = new List<EditorObjectType>();

        public static Dictionary<string, GameObject> characterObjects = new Dictionary<string, GameObject>();

        public static Dictionary<string, ItemObject> itemObjects = new Dictionary<string, ItemObject>();

        public AssetManager assetMan = new AssetManager();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static BaldiLevelEditorPlugin Instance;
        public static EditorObjectType pickupPrefab;
        GameCamera camera;
        Cubemap cubemap;
        Canvas canvasTemplate;
        CursorController cursorOrigin;
        internal Tile tilePrefab;
        EnvironmentController environmentControllerPrefab;

        public static CapsuleCollider playerColliderObject;

        public static Dictionary<string, Texture2D> lightmaps = new Dictionary<string, Texture2D>();
        public static Shader tileStandardShader => Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard");
        public static Shader tilePosterShader => Instance.assetMan.Get<Shader>("Shader Graphs/TileStandardWPoster");

        public static Shader tileAlphaShader => Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard_AlphaClip");
        public static Shader tilePosterAlphaShader => Instance.assetMan.Get<Shader>("Shader Graphs/TileStandardWPoster_AlphaClip");


        public static Shader tileMaskedShader => Instance.assetMan.Get<Shader>("Shader Graphs/MaskedStandard");
        public static Material spriteMaterial;
        public static ElevatorScreen elevatorScreen;
        public static CoreGameManager coreGamePrefab;
        public static EndlessGameManager endlessGameManager;
        public static MainGameManager mainGameManager;
        public static Texture2D yellowTexture => lightmaps["yellow"];
        public static Texture2D lightmapTexture => lightmaps["lighting"];
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static string[] editorThemes = new string[4];

        public static GameObject StripAllScripts(GameObject reference, bool stripColliders = false)
        {
            bool active = reference.activeSelf;
            reference.SetActive(false);
            GameObject obj = GameObject.Instantiate(reference);
            obj.GetComponentsInChildren<MonoBehaviour>().Do(x => {
                if (x is BillboardUpdater) return;
                Destroy(x);
            });
            if (stripColliders)
            {
                obj.GetComponentsInChildren<Collider>().Do(x => Destroy(x));
            }
            obj.name = "REFERENCE_" + obj.name;
            obj.ConvertToPrefab(false);
            reference.SetActive(active);
            return obj;
        }

        public static T CreateTileVisualFromObject<T, TC>(GameObject reference) where T : TileBasedEditorVisual<TC>
            where TC : TiledPrefab
        {
            GameObject newRef = StripAllScripts(reference, false);
            T comp = newRef.GetComponentInChildren<Collider>().gameObject.AddComponent<T>();
            return comp;
        }

        public IEnumerator GoToGame()
        {
            AsyncOperation waitForSceneLoad = SceneManager.LoadSceneAsync("Game");
            while (!waitForSceneLoad.isDone)
            {
                yield return null;
            }
            // this is slow AF but who actually cares
            for (int x = 0; x < lightmapTexture.width; x++)
            {
                for (int y = 0; y < lightmapTexture.height; y++)
                {
                    lightmapTexture.SetPixel(x,y,Color.white);
                }
            }
            lightmapTexture.Apply();
            GameCamera cam = GameObject.Instantiate<GameCamera>(camera);
            Shader.SetGlobalTexture("_Skybox", cubemap);
            Shader.SetGlobalColor("_SkyboxColor", Color.white);
            Shader.SetGlobalColor("_FogColor", Color.white);
            Shader.SetGlobalFloat("_FogStartDistance", 5f);
            Shader.SetGlobalFloat("_FogMaxDistance", 100f);
            Shader.SetGlobalFloat("_FogStrength", 0f);
            Canvas canvas = GameObject.Instantiate<Canvas>(canvasTemplate);
            canvas.name = "MainCanvas";
            CursorInitiator cursorInit = canvas.gameObject.AddComponent<CursorInitiator>();
            if ((float)Singleton<PlayerFileManager>.Instance.resolutionX / (float)Singleton<PlayerFileManager>.Instance.resolutionY >= 1.3333f)
            {
                canvas.scaleFactor = (float)Mathf.RoundToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 360f);
            }
            else
            {
                canvas.scaleFactor = (float)Mathf.FloorToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 480f);
            }
            cursorInit.screenSize = new Vector2(Screen.width / canvas.scaleFactor, Screen.height / canvas.scaleFactor);
            cursorInit.cursorPre = cursorOrigin;
            cursorInit.graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            canvas.gameObject.SetActive(true);
            canvas.worldCamera = cam.canvasCam;
            GameObject dummyObject = new GameObject();
            dummyObject.SetActive(false);
            PlusLevelEditor editor = dummyObject.AddComponent<PlusLevelEditor>();
            editor.ReflectionSetVariable("destroyOnLoad", true);
            editor.gameObject.AddComponent<BillboardManager>();
            dummyObject.SetActive(true);
            editor.gameObject.name = "Level Editor";
            editor.cursorBounds = cursorInit.screenSize;
            editor.gameObject.AddComponent<AudioManager>().audioDevice = editor.gameObject.AddComponent<AudioSource>();
            editor.audMan = editor.gameObject.GetComponent<AudioManager>();
            editor.audMan.ReflectionSetVariable("disableSubtitles", true);
            Singleton<PlusLevelEditor>.Instance.myCamera = cam;
            Singleton<PlusLevelEditor>.Instance.canvas = canvas;
            EnvironmentController ec = GameObject.Instantiate<EnvironmentController>(environmentControllerPrefab);
            ec.gameObject.SetActive(false);
            Singleton<PlusLevelEditor>.Instance.puppetEnvironmentController = ec;

            //CursorController cc = GameObject.Instantiate<CursorController>(cursorOrigin);
            //cc.transform.SetParent(canvas.transform, false);
        }

        IEnumerator AssetsLoadedActual()
        {
            if ((new Version(MTM101BaldiDevAPI.VersionNumber)) < (new Version("4.0.0.0")))
            {
                MTM101BaldiDevAPI.CauseCrash(this.Info, new Exception("Invalid API version, please use 4.0 or greater!"));
            }
            yield return 5;
            yield return "Defining Variables...";
            assetMan.Add<Sprite>("clipboard", Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "OptionsClipboard").First());
            spriteMaterial = Resources.FindObjectsOfTypeAll<Material>().Where(x => x.name == "SpriteStandard_Billboard").First();
            camera = Resources.FindObjectsOfTypeAll<GameCamera>().First();
            cubemap = Resources.FindObjectsOfTypeAll<Cubemap>().Where(x => x.name == "Cubemap_DayStandard").First();
            cursorOrigin = Resources.FindObjectsOfTypeAll<CursorController>().Where(x => !x.name.Contains("Clone")).First();
            environmentControllerPrefab = Resources.FindObjectsOfTypeAll<EnvironmentController>().First();
            tilePrefab = Resources.FindObjectsOfTypeAll<Tile>().First();
            Canvas endingError = GameObject.Instantiate<Canvas>(Resources.FindObjectsOfTypeAll<Canvas>().Where(x => x.name == "EndingError").First());
            endingError.gameObject.SetActive(false);
            for (int i = 0; i < endingError.transform.childCount; i++)
            {
                GameObject.Destroy(endingError.transform.GetChild(i).gameObject);
            }
            endingError.name = "Canvas Template";
            canvasTemplate = endingError;
            //GameObject.Destroy(canvasTemplate.GetComponent<GlobalCamCanvasAssigner>());
            canvasTemplate.gameObject.ConvertToPrefab(false);
            canvasTemplate.planeDistance = 100f;
            canvasTemplate.sortingOrder = 10;
            editorThemes[0] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorA.mid"), "editorA");
            editorThemes[1] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorB.mid"), "editorB");
            editorThemes[2] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorC.mid"), "editorC");
            editorThemes[3] = AssetLoader.MidiFromFile(Path.Combine(AssetLoader.GetModPath(this), "EditorD.mid"), "editorD");
            lightmaps.Add("lighting", Resources.FindObjectsOfTypeAll<Texture2D>().Where(x => x.name == "LightMap").First());

            pickupPrefab = EditorObjectType.CreateFromGameObject<ItemPrefab, ItemLocation>("item", Resources.FindObjectsOfTypeAll<Pickup>().Where(x => x.transform.parent == null).First().gameObject, Vector3.up * 5f);
            pickupPrefab.prefab.gameObject.AddComponent<EditorHasNoCollidersInBaseGame>();


            assetMan.AddFromResources<Texture2D>();
            assetMan.AddFromResources<Mesh>();
            assetMan.AddFromResources<Shader>();
            assetMan.AddFromResources<SoundObject>();
            elevatorScreen = Resources.FindObjectsOfTypeAll<ElevatorScreen>().Where(x => x.gameObject.transform.parent == null).First();
            coreGamePrefab = Resources.FindObjectsOfTypeAll<CoreGameManager>().First();
            endlessGameManager = Resources.FindObjectsOfTypeAll<EndlessGameManager>().First();
            Activity[] activites = Resources.FindObjectsOfTypeAll<Activity>();
            assetMan.Add("Mixer_Sounds", Resources.FindObjectsOfTypeAll<UnityEngine.Audio.AudioMixer>().Where(x => x.name == "Sounds").First());

            yield return "Creating Editor Prefabs...";
            // prefabs
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("desk", objects.Where(x => x.name == "Table_Test").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bigdesk", objects.Where(x => x.name == "BigDesk").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("cabinettall", objects.Where(x => x.name == "FilingCabinet_Tall").First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("chair", objects.Where(x => x.name == "Chair_Test").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("computer", objects.Where(x => x.name == "MyComputer").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("computer_off", objects.Where(x => x.name == "MyComputer_Off").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("roundtable", objects.Where(x => x.name == "RoundTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("locker", objects.Where(x => x.name == "Locker").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bluelocker", objects.Where(x => x.name == "BlueLocker").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("greenlocker", objects.Where(x => x.name == "StorageLocker").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bookshelf", objects.Where(x => x.name == "Bookshelf_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bookshelf_hole", objects.Where(x => x.name == "Bookshelf_Hole_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("rounddesk", objects.Where(x => x.name == "RoundDesk").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("cafeteriatable", objects.Where(x => x.name == "CafeteriaTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("dietbsodamachine", objects.Where(x => x.name == "DietSodaMachine").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bsodamachine", objects.Where(x => x.name == "SodaMachine").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("zestymachine", objects.Where(x => x.name == "ZestyMachine").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("crazymachine_bsoda", objects.Where(x => x.name == "CrazyVendingMachineBSODA").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("crazymachine_zesty", objects.Where(x => x.name == "CrazyVendingMachineZesty").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("waterfountain", objects.Where(x => x.name == "WaterFountain").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("counter", objects.Where(x => x.name == "Counter").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("examination", objects.Where(x => x.name == "ExaminationTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("ceilingfan", objects.Where(x => x.name == "CeilingFan").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("merrygoround", objects.Where(x => x.name == "MerryGoRound_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("tree", objects.Where(x => x.name == "TreeCG").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("pinetree", objects.Where(x => x.name == "PineTree").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("appletree", objects.Where(x => x.name == "AppleTree").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("bananatree", objects.Where(x => x.name == "BananaTree").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hoop", objects.Where(x => x.name == "HoopBase").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("payphone", objects.Where(x => x.name == "PayPhone").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("tapeplayer", objects.Where(x => x.name == "TapePlayer").Where(x => x.transform.parent == null).First(), Vector3.up * 5f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("plant", objects.Where(x => x.name == "Plant").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("picnictable", objects.Where(x => x.name == "PicnicTable").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("tent", objects.Where(x => x.name == "Tent_Object").Where(x => x.transform.parent == null).First(), Vector3.zero));

            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_pencilnotes", objects.Where(x => x.name == "Decor_PencilNotes").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_papers", objects.Where(x => x.name == "Decor_Papers").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_globe", objects.Where(x => x.name == "Decor_Globe").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_notebooks", objects.Where(x => x.name == "Decor_Notebooks").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_lunch", objects.Where(x => x.name == "Decor_Lunch").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_banana", objects.Where(x => x.name == "Decor_Banana").Where(x => x.transform.parent == null).First(), Vector3.up * 3.75f));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("decor_zoneflag", objects.Where(x => x.name == "Decor_ZoningFlag").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("rock", objects.Where(x => x.name == "Rock").Where(x => x.transform.parent == null).First(), Vector3.zero));
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("picnicbasket", objects.Where(x => x.name == "PicnicBasket").Where(x => x.transform.parent == null).First(), Vector3.zero));
            //editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hopscotch", );

            //objects.Where(x => x.name == "PlaygroundPavement").Where(x => x.transform.parent == null).First().transform.GetChild(0);
            // ugly hopscotch hack
            GameObject hopActual = GameObject.Instantiate(objects.Where(x => x.name == "PlaygroundPavement").Where(x => x.transform.parent == null).First().transform.GetChild(0).gameObject);
            GameObject hopBase = new GameObject();
            hopBase.ConvertToPrefab(false);
            hopActual.transform.SetParent(hopBase.transform, true);
            hopBase.SetActive(false);
            hopActual.gameObject.SetActive(true);
            Destroy(hopActual.gameObject.GetComponent<Collider>());
            hopBase.transform.name = "EditorHopscotchBase";
            BoxCollider box = hopBase.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(20f, 0.1f, 20f);
            editorObjects.Add(EditorObjectType.CreateFromGameObject<EditorPrefab, PrefabLocation>("hopscotch", hopBase, Vector3.zero, true));


            // activities
            editorActivities.Add(EditorObjectType.CreateFromGameObject<ActivityPrefab, RoomActivity>("notebook", Resources.FindObjectsOfTypeAll<Notebook>().First().gameObject, Vector3.up * 5f));
            editorActivities.Add(EditorObjectType.CreateFromGameObject<ActivityPrefab, RoomActivity>("mathmachine", activites.Where(x => (x.name == "MathMachine" && (x.transform.parent == null))).First().gameObject, Vector3.zero));
            editorActivities.Add(EditorObjectType.CreateFromGameObject<ActivityPrefab, RoomActivity>("mathmachine_corner", activites.Where(x => (x.name == "MathMachine_Corner" && (x.transform.parent == null))).First().gameObject, Vector3.zero));
            // ugly corner math machine hack
            EditorObjectType cornerMathMachine = editorActivities.Last();
            GameObject baseObject = GameObject.Instantiate(cornerMathMachine.prefab.gameObject);
            baseObject.GetComponents<MonoBehaviour>().Do(x => Destroy(x));
            baseObject.GetComponentsInChildren<Collider>().Do(x => Destroy(x));
            baseObject.transform.SetParent(cornerMathMachine.prefab.transform, false);
            baseObject.transform.localPosition = Vector3.zero;
            cornerMathMachine.prefab.gameObject.transform.eulerAngles = Vector3.zero;
            baseObject.transform.eulerAngles = new Vector3(0f, 45f, 0f);
            Destroy(cornerMathMachine.prefab.gameObject.GetComponent<MeshRenderer>());
            baseObject.SetActive(true);
            baseObject.name = "CornerRenderer";

            // characters
            yield return "Creating NPC Prefabs...";
            characterObjects.Add("baldi", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Baldi).value.gameObject, true));
            characterObjects.Add("principal", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Principal).value.gameObject, true));
            characterObjects.Add("sweep", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Sweep).value.gameObject, true));
            characterObjects.Add("playtime", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Playtime).value.gameObject, true));
            GameObject chalklesReference = StripAllScripts(NPCMetaStorage.Instance.Get(Character.Principal).value.gameObject, true);
            chalklesReference.GetComponentInChildren<SpriteRenderer>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "ChalkFace").First();
            characterObjects.Add("chalkface", chalklesReference);
            characterObjects.Add("bully", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Bully).value.gameObject, true));
            characterObjects.Add("beans", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Beans).value.gameObject, true));
            characterObjects.Add("prize", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Prize).value.gameObject, true));
            characterObjects.Add("crafters", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Crafters).value.gameObject, true));
            characterObjects.Add("pomp", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Pomp).value.gameObject, true));
            characterObjects.Add("test", StripAllScripts(NPCMetaStorage.Instance.Get(Character.LookAt).value.gameObject, true));
            characterObjects.Add("cloudy", StripAllScripts(NPCMetaStorage.Instance.Get(Character.Cumulo).value.gameObject, true));
            characterObjects.Add("reflex", StripAllScripts(NPCMetaStorage.Instance.Get(Character.DrReflex).value.gameObject, true));

            // items
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

            yield return "Setting Up Tiled Editor Prefabs...";
            // tile based objects
            TiledEditorConnectable lockdownVisual = CreateTileVisualFromObject<TiledEditorConnectable, TiledPrefab>(objects.Where(x => x.name == "LockdownDoor").First());
            lockdownVisual.positionOffset = Vector3.up * 21f;
            lockdownVisual.directionAddition = 4f;
            tiledPrefabPrefabs.Add("lockdowndoor", lockdownVisual);

            playerColliderObject = StripAllScripts(Resources.FindObjectsOfTypeAll<PlayerManager>().First().gameObject).GetComponent<CapsuleCollider>();
            playerColliderObject.name = "Player Collider Reference";

            yield return "Setting Misc Prefabs...";
            MainGameManager toCopy = Resources.FindObjectsOfTypeAll<MainGameManager>().First();
            GameObject newObject = new GameObject();
            newObject.SetActive(false);
            mainGameManager = newObject.AddComponent<EditorLevelManager>();
            GameObject ambienceChild = GameObject.Instantiate(toCopy.transform.Find("Ambience").gameObject, mainGameManager.transform);
            mainGameManager.ReflectionSetVariable("ambience", ambienceChild.GetComponent<Ambience>());
            mainGameManager.spawnNpcsOnInit = false;
            mainGameManager.spawnImmediately = false;
            mainGameManager.ReflectionSetVariable("allNotebooksNotification", BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/BAL_CommunityNotebooks"));
            mainGameManager.ReflectionSetVariable("happyBaldiPre", Resources.FindObjectsOfTypeAll<HappyBaldi>().First());
            mainGameManager.ReflectionSetVariable("destroyOnLoad", true);
            mainGameManager.gameObject.name = "CustomEditorGameManager";
            mainGameManager.gameObject.ConvertToPrefab(true);
            yield break;
        }


        void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path)
        {
            string[] paths = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < paths.Length; i++)
            {
                assetMan.Add<Sprite>(prefix + Path.GetFileNameWithoutExtension(paths[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(paths[i]), pixelsPerUnit));
            }
        }

        void AddAudioFolderToAssetMan(params string[] path)
        {
            string[] paths = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < paths.Length; i++)
            {
                SoundObject obj = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(paths[i]), Path.GetFileNameWithoutExtension(paths[i]), SoundType.Effect, Color.white);
                obj.subtitle = false;
                assetMan.Add<SoundObject>("Audio/" + Path.GetFileNameWithoutExtension(paths[i]), obj);
            }
        }

        void AddSolidColorLightmap(string name, Color color)
        {
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] colors = new Color[256 * 256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            tex.SetPixels(0, 0, 256, 256, colors);
            tex.Apply();
            lightmaps.Add(name, tex);
        }

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.leveleditor");
            //CustomOptionsCore.OnMenuInitialize += OptMenPlaceholder;
            Instance = this;
            AddSolidColorLightmap("white", Color.white);
            AddSolidColorLightmap("yellow", Color.yellow);
            AddSolidColorLightmap("red", Color.red);
            AddSolidColorLightmap("green", Color.green);
            AddSolidColorLightmap("blue", Color.blue);
            assetMan.Add<Texture2D>("Selector", AssetLoader.TextureFromMod(this, "Selector.png"));
            assetMan.Add<Texture2D>("Grid", AssetLoader.TextureFromMod(this, "Grid.png"));
            assetMan.Add<Texture2D>("Cross", AssetLoader.TextureFromMod(this, "Cross.png"));
            assetMan.Add<Texture2D>("CrossMask", AssetLoader.TextureFromMod(this, "CrossMask.png"));
            assetMan.Add<Texture2D>("Border", AssetLoader.TextureFromMod(this, "Border.png"));
            assetMan.Add<Texture2D>("BorderMask", AssetLoader.TextureFromMod(this, "BorderMask.png"));
            assetMan.Add<Texture2D>("Arrow", AssetLoader.TextureFromMod(this, "Arrow.png"));
            assetMan.Add<Texture2D>("ArrowSmall", AssetLoader.TextureFromMod(this, "ArrowSmall.png"));
            assetMan.Add<Texture2D>("Circle", AssetLoader.TextureFromMod(this, "Circle.png"));
            assetMan.Add<Texture2D>("ArrowSmall", AssetLoader.TextureFromMod(this, "ArrowSmall.png"));
            assetMan.Add<Texture2D>("SwingDoorSilent", AssetLoader.TextureFromMod(this, "SwingDoorSilent.png"));
            assetMan.Add<Sprite>("EditorButton", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "EditorButton.png"), 1f));
            assetMan.Add<Sprite>("EditorButtonGlow", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "EditorButton_Glow.png"), 1f));
            assetMan.Add<Sprite>("EditorButtonFail", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "EditorButtonFail.png"), 1f));
            assetMan.Add<Sprite>("LinkSprite", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "LinkSprite.png"), 40f));
            doorTypes.Add("standard", typeof(StandardDoorEditorVisual));
            doorTypes.Add("swing", typeof(SwingEditorVisual));
            doorTypes.Add("autodoor", typeof(AutoDoorEditorVisual));
            doorTypes.Add("swingsilent", typeof(SilentSwingEditorVisual));
            doorTypes.Add("coin", typeof(CoinSwingEditorVisual));
            doorTypes.Add("oneway", typeof(OneWaySwingEditorVisual));
            AddSpriteFolderToAssetMan("UI/", 40, AssetLoader.GetModPath(this), "UI");
            AddAudioFolderToAssetMan(AssetLoader.GetModPath(this), "Audio");
            assetMan.Get<SoundObject>("Audio/BAL_CommunityNotebooks").soundType = SoundType.Voice;
            assetMan.Get<SoundObject>("Audio/IncompatibleResolution").soundType = SoundType.Voice;
            assetMan.Get<SoundObject>("Audio/IncompatibleResolution").subtitle = true;
            assetMan.Get<SoundObject>("Audio/IncompatibleResolution").soundKey = "Please change your resolution in the options menu!";
            assetMan.Get<Sprite>("UI/DitherPattern").texture.wrapMode = TextureWrapMode.Repeat;
            LoadingEvents.RegisterOnAssetsLoaded(Info, AssetsLoadedActual(), false);
            harmony.PatchAllConditionals();
        }
    }
}
