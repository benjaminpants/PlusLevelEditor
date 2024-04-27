using BaldiLevelEditor.UI;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.UI;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Rewired.Controller;

namespace BaldiLevelEditor
{

    public class EditorTrackCursor : MonoBehaviour
    {
        Vector3 initLocal;
        void Start()
        {
            initLocal = transform.localPosition;
        }

        void Update()
        {
            if (Singleton<PlusLevelEditor>.Instance.cursor == null) return;
            if (Singleton<PlusLevelEditor>.Instance.selectedTool != null)
            {
                transform.position = Singleton<PlusLevelEditor>.Instance.cursor.cursorTransform.position;
                return;
            }
            transform.localPosition = initLocal;
            Destroy(this);
        }
    }

    public class ToolIconManager : MonoBehaviour
    {
        private bool _active;

        public bool active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;
                gameObject.tag = _active ? "Button" : "Untagged";
            }
        }
    }

    public class CategoryManager : MonoBehaviour
    {
        public List<ToolIconManager> toAnimate = new List<ToolIconManager>();
        public List<ToolIconManager> allTools = new List<ToolIconManager>();
        public ToolIconManager pageButton;
        public int page;

        public IEnumerator AnimateIcon(ToolIconManager manager, Vector3 start, Vector3 end, float time, bool activeState)
        {
            manager.active = false;
            Transform transform = manager.transform.parent;
            if (transform == this.transform.parent)
            {
                transform = manager.transform;
            }
            float currentTime = 0f;
            while (currentTime < 1f)
            {
                currentTime += (Time.deltaTime / time);
                Vector3 lerped = Vector3.Lerp(start, end, currentTime);
                transform.localPosition = new Vector3(Mathf.Round(lerped.x), Mathf.Round(lerped.y), Mathf.Round(lerped.z));
                yield return null;
            }
            Singleton<PlusLevelEditor>.Instance.audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/UIClunk"));
            manager.active = activeState;
            yield break;
        }

        public void Initialize()
        {
            allTools.Do(x => x.active = false);
            toAnimate.Clear();
            for (int i = page * 7; i < Mathf.Min((page + 1) * 7, allTools.Count); i++)
            {
                toAnimate.Add(allTools[i]);
            }
        }

        bool doingSwitchAnimation = false;

        public void SwitchPage()
        {
            doingSwitchAnimation = true;
            StartCoroutine(CollapseInAndOut());
        }

        IEnumerator CollapseInAndOut()
        {
            float speed = 0.5f;
            ExpandViaPage(false, speed);
            yield return new WaitForSeconds((0.20f + (toAnimate.Count * 0.05f)) * speed);
            Initialize();
            ExpandViaPage(true, speed);
            yield return new WaitForSeconds((0.20f + (toAnimate.Count * 0.05f)) * speed);
            doingSwitchAnimation = false;
            yield break;
        }

        public bool isOut = false;
        public void OnClick()
        {
            if (doingSwitchAnimation) return;
            isOut = !isOut;
            StopAllCoroutines();
            ExpandViaPage(isOut);
        }

        public void ExpandViaPage(bool expand, float speed = 1f)
        {
            int i;
            Vector3 toMoveTo;
            for (i = 0; i < toAnimate.Count; i++)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 1), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(toAnimate[i], toAnimate[i].transform.parent.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
            if (pageButton != null)
            {
                toMoveTo = (expand) ? new Vector3(42f * (i + 1), 0f, 0f) : Vector3.zero;
                StartCoroutine(AnimateIcon(pageButton, pageButton.transform.localPosition, toMoveTo, (0.20f + (i * 0.05f)) * speed, expand));
            }
        }
    }

    public struct ToolCategory
    {
        public string name;
        public Sprite sprite;
        public List<EditorTool> tools;

        public ToolCategory(string name, Sprite sprite, params EditorTool[] tools)
        {
            this.name = name;
            this.sprite = sprite;
            this.tools = tools.ToList();
        }
    }

    public partial class PlusLevelEditor : Singleton<PlusLevelEditor>
    {

        public CustomImageAnimator gearAnimator;

        /*public List<EditorTool> tools = new List<EditorTool>()
        {
            new FloorTool("hall"),
            new FloorTool("class"),
            new FloorTool("faculty"),
            new FloorTool("office"),
            new DoorTool(),
            new WindowTool(),
            new MergeTool(),
            new DeleteTool()
        };*/

        public List<ToolCategory> toolCats = new List<ToolCategory>()
        {
            new ToolCategory("halls", GetUISprite("Floor"),
            new FloorTool("hall"),
            new FloorTool("class"),
            new FloorTool("faculty"),
            new FloorTool("office"),
            new FloorTool("closet"),
            new FloorTool("reflex"),
            new FloorTool("library"),
            new FloorTool("cafeteria"),
            new FloorTool("outside")),
            new ToolCategory("doors", GetUISprite("DoorED"),
            new DoorTool("standard"),
            new SwingingDoorTool("swing"),
            new SwingingDoorTool("swingsilent"),
            new SwingingDoorTool("coin"),
            new SwingingDoorTool("oneway"),
            new WindowTool(),
            new WallTool(true),
            new WallTool(false)),
            new ToolCategory("objects", GetUISprite("Object_desk"),
            new RotateAndPlacePrefab("waterfountain"),
            new RotateAndPlacePrefab("bsodamachine"),
            new RotateAndPlacePrefab("zestymachine"),
            new RotateAndPlacePrefab("crazymachine_bsoda"),
            new RotateAndPlacePrefab("crazymachine_zesty"),
            new ObjectTool("payphone"),
            new ObjectTool("tapeplayer"),
            new RotateAndPlacePrefab("locker"),
            new RotateAndPlacePrefab("bluelocker"),
            new PrebuiltStructureTool("bulklocker", new EditorPrebuiltStucture(
                new PrefabLocation("locker", new UnityVector3(-4f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(-2f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(0f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(2f,0f,4f)),
                new PrefabLocation("locker", new UnityVector3(4f,0f,4f)))),
            new PrebuiltStructureTool("chairdesk", new EditorPrebuiltStucture(
                new PrefabLocation("chair", new UnityVector3(0f,0f,-2f)),
                new PrefabLocation("desk", new UnityVector3(0f,0f,0f)))),
            new RotateAndPlacePrefab("desk"),
            new RotateAndPlacePrefab("chair"),
            new RotateAndPlacePrefab("bigdesk"),
            new ObjectTool("roundtable"),
            new ObjectTool("cafeteriatable"),
            new ObjectTool("plant"),
            new ObjectTool("decor_pencilnotes"),
            new ObjectTool("decor_papers"),
            new ObjectTool("decor_globe"),
            new ObjectTool("decor_notebooks"),
            new ObjectTool("decor_banana"),
            new ObjectTool("decor_lunch"),
            new ObjectTool("decor_zoneflag"),
            new ObjectTool("cabinettall"),
            new ObjectTool("computer"),
            new ObjectTool("computer_off"),
            new ObjectTool("ceilingfan"),
            new RotateAndPlacePrefab("bookshelf"),
            new RotateAndPlacePrefab("bookshelf_hole"),
            new ObjectTool("rounddesk"),
            new ObjectTool("counter"),
            new ObjectTool("examination"),
            new ObjectTool("merrygoround"),
            new ObjectTool("tree"),
            new ObjectTool("appletree"),
            new ObjectTool("hopscotch"),
            new ObjectTool("hoop")),
            new ToolCategory("activities", GetUISprite("Activity_notebook"),
            new ActivityTool("notebook"),
            new ActivityTool("mathmachine"),
            new ActivityTool("mathmachine_corner")),
            new ToolCategory("characters", GetUISprite("NPC_baldi"),
            new NpcTool("baldi"),
            new NpcTool("principal"),
            new NpcTool("sweep"),
            new NpcTool("playtime"),
            new NpcTool("bully"),
            new NpcTool("crafters"),
            new NpcTool("prize"),
            new NpcTool("cloudy"),
            new NpcTool("chalkface"),
            new NpcTool("beans"),
            new NpcTool("pomp"),
            new NpcTool("test"),
            new NpcTool("reflex")),
            new ToolCategory("items", GetUISprite("cat_item"),
            new ItemTool("quarter"),
            new ItemTool("bsoda"),
            new ItemTool("zesty"),
            new ItemTool("scissors"),
            new ItemTool("boots"),
            new ItemTool("nosquee"),
            new ItemTool("keys"),
            new ItemTool("tape"),
            new ItemTool("clock"),
            new ItemTool("swinglock"),
            new ItemTool("whistle"),
            new ItemTool("dirtychalk"),
            new ItemTool("nametag"),
            new ItemTool("teleporter"),
            new ItemTool("portalposter"),
            new ItemTool("grapple"),
            new ItemTool("apple")),
            new ToolCategory("connectables", GetUISprite("Button_button"),
            new ButtonTool("button"),
            new TileBasedTool("lockdowndoor")),
            new ToolCategory("utilities", GetUISprite("Gear"),
            new ElevatorTool(true),
            new ElevatorTool(false),
            new ConnectTool(),
            new MergeTool(),
            new DeleteTool()),
        };


        // UI
        public Transform UIWires;

        private static Sprite GetUISprite(string name)
        {
            return BaldiLevelEditorPlugin.Instance.assetMan.Get<Sprite>("UI/" + name);
        }

        public void CreateDirectoryIfNoExist()
        {
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "CustomLevels")))
            {
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "CustomLevels"));
            }
        }

        IEnumerator WaitForAudMan()
        {
            yield return null;
            while (audMan.AnyAudioIsPlaying)
            {
                yield return null;
            }
            SceneManager.LoadScene("MainMenu");
        }

        float originalScale = 1f;
        public void SpawnUI()
        {
            //Debug.Log(Mathf.Floor(canvas.scaleFactor));
            //Debug.Log(canvas.scaleFactor);
            /*if (Mathf.Floor(canvas.scaleFactor) != canvas.scaleFactor)
            {
                audMan.ReflectionSetVariable("disableSubtitles", false);
                audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Audio/IncompatibleResolution"));
                StartCoroutine(WaitForAudMan());
                return;
            }*/
            Image anchor = UIHelpers.CreateImage(GetUISprite("Wires"), canvas.transform, Vector3.zero, false);
            originalScale = canvas.scaleFactor;
            anchor.rectTransform.pivot = new Vector2(0f, 1f);
            anchor.rectTransform.anchorMin = new Vector2(0f, 1f);
            anchor.rectTransform.anchorMax = new Vector2(0f, 1f);
            anchor.rectTransform.sizeDelta = new Vector2(anchor.rectTransform.sizeDelta.x, canvas.renderingDisplaySize.y / canvas.scaleFactor);
            anchor.rectTransform.anchoredPosition = Vector3.zero;
            //anchor.rectTransform.offsetMax = new Vector2(379f, 0f);
            //anchor.rectTransform.offsetMin = new Vector2(320f, -360f);
            UIWires = anchor.transform;
            UIWires.name = "Wires";
            for (int i = 0; i < toolCats.Count; i++)
            {
                CreateSlot(UIWires, -(32f + (40f * i)), toolCats[i]);
            }
            Sprite sprite = GetUISprite("CogsMoveScreen0");
            Image gears = UIHelpers.CreateImage(sprite, canvas.transform, Vector3.zero, false);
            gears.rectTransform.anchorMin = new Vector2(1f, 0f);
            gears.rectTransform.anchorMax = new Vector2(1f, 0f);
            gears.rectTransform.pivot = new Vector2(1f, 0f);
            gears.rectTransform.anchoredPosition = new Vector2(6f,-30f);
            gearAnimator = gears.gameObject.AddComponent<CustomImageAnimator>();
            gearAnimator.animations.Add("spin", new CustomAnimation<Sprite>(new Sprite[]
            {
                GetUISprite("CogsMoveScreen0"),
                GetUISprite("CogsMoveScreen1"),
                GetUISprite("CogsMoveScreen2"),
                GetUISprite("CogsMoveScreen3")
            },0.2f));
            gearAnimator.affectedObject = gears;

            StandardMenuButton testButton = UIHelpers.CreateImage(GetUISprite("UITestButton"), canvas.transform, Vector3.zero, false).gameObject.ConvertToButton<StandardMenuButton>();
            testButton.OnPress.AddListener(() =>
            {
                SwitchToMenu(new UIMenuBuilder()
                    .AddClipboard()
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Down)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .AddButton(GetUISprite("PlayButton"), (StandardMenuButton b) =>
                    {
                        Debug.Log(b.name);
                    },
                    NextDirection.Down)
                    .AddLabel(100f,100f,"I LOVE BURGER!", NextDirection.Down)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Left)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Down)
                    .AddTexture(BaldiLevelEditorPlugin.Instance.assetMan.Get<Texture2D>("Wall"), 0.25f, NextDirection.Right)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Up)
                    .AddImage(GetUISprite("UITestButton"), NextDirection.Right)
                    .Build());
            });

            //gearAnimator.SetDefaultAnimation("spin", 1f);
            //TextMeshProUGUI text = UIHelpers.CreateText<TextMeshProUGUI>(BaldiFonts.ComicSans12, "EVERYTHING SEEN HERE IS SUBJECT TO CHANGE!", canvas.transform,new Vector3(-165f, 150f, 0f), false);
            CreateGearButton(GetUISprite("SaveButton"), GetUISprite("SaveButtonHover"), new Vector2(5f,10f), () =>
            {
                if (level.areas.Count == 0)
                {
                    return;
                }
                CreateDirectoryIfNoExist();
                SaveLevelAsEditor(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.bld")); // placeholder path
            }, true, () =>
            {
                if (level.areas.Count == 0)
                {
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    return;
                }
            });
            CreateGearButton(GetUISprite("LoadButton"), GetUISprite("LoadButtonHover"), new Vector2(53f,10f), () =>
            {
                CreateDirectoryIfNoExist();
                SaveLevelAsEditor(Path.Combine(Application.persistentDataPath, "CustomLevels", "level_previous.bld")); // placeholder path
                LoadLevelFromFile(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.bld")); // placeholder path
            }, false,
            () =>
            {
                if (tempLevel == null)
                {
                    audMan.PlaySingle(BaldiLevelEditorPlugin.Instance.assetMan.Get<SoundObject>("Elv_Buzz"));
                    throw new Exception("Level Loading failed!");
                }
                LoadLevel(tempLevel);
                tempLevel = null;
            });
            CreateGearButton(GetUISprite("PlayButton"), GetUISprite("PlayButtonHover"), new Vector2(100f, 10f), () =>
            {
                CompileLevelAsPlayable(Path.Combine(Application.persistentDataPath, "CustomLevels", "level.cbld"));
            }, false);
            InitializeMenuBackground();
            UpdateCursor();
            UpdateCursor();
        }

        // do this so if saving/loading on a slow machine the game doesn't look like its crashing
        IEnumerator RunThreadAndSpinGear(Action toPerform, Action? toPerformPostThread)
        {
            gearAnimator.SetDefaultAnimation("spin", 1f);
            Thread myThread = new Thread(new ThreadStart(toPerform));
            myThread.Start();
            updateDelay = float.MaxValue;
            while (myThread.IsAlive)
            {
                yield return null;
            }
            updateDelay = 0.1f;
            gearAnimator.SetDefaultAnimation("", 0f);
            CursorController.Instance.DisableClick(false);
            if (toPerformPostThread != null)
            {
                toPerformPostThread();
            }
            yield break;
        }

        void CreateGearButton(Sprite sprite, Sprite highlightSprite, Vector2 position, Action toDo, bool thread, Action? postThread = null)
        {
            Image but = UIHelpers.CreateImage(sprite, gearAnimator.transform, Vector3.zero, false);
            but.rectTransform.anchorMin = new Vector2(0f, 0f);
            but.rectTransform.anchorMax = new Vector2(0f, 0f);
            but.rectTransform.pivot = new Vector2(0f, 0f);
            but.rectTransform.anchoredPosition = position - new Vector2(-6f, -30f);
            StandardMenuButton stanMen = but.gameObject.ConvertToButton<StandardMenuButton>();
            stanMen.highlightedSprite = highlightSprite;
            stanMen.unhighlightedSprite = sprite;
            stanMen.swapOnHigh = true;
            if (thread)
            {
                stanMen.OnPress.AddListener(() =>
                {
                    if (updateDelay > 0f) return;
                    CursorController.Instance.DisableClick(true);
                    StartCoroutine(RunThreadAndSpinGear(toDo, postThread));
                });
            }
            else
            {
                stanMen.OnPress.AddListener(() =>
                {
                    if (updateDelay > 0f) return;
                    toDo();
                    if (postThread != null)
                    {
                        postThread();
                    }
                });
            }
        }

        public void CreateSlot(Transform parent, float y, ToolCategory cat)
        {
            GameObject empty = new GameObject();
            empty.transform.SetParent(parent, false);
            RectTransform transform = empty.AddComponent<RectTransform>();
            transform.pivot = new Vector2(0.5f,1f);
            transform.anchorMin = new Vector2(0.5f, 1f);
            transform.anchorMax = new Vector2(0.5f, 1f);
            transform.anchoredPosition = new Vector2(-0.5f, y);
            empty.name = cat.name;
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotCategory"), empty.transform, Vector2.zero, false);
            slot.transform.SetParent(empty.transform, false);
            slot.name = "Category Slot";
            Image icon = UIHelpers.CreateImage(cat.sprite, slot.transform, new Vector2(0f, 0f), false);
            slot.transform.SetParent(slot.transform, false);
            icon.name = "Icon";
            CategoryManager catMan = slot.gameObject.AddComponent<CategoryManager>();
            StandardMenuButton button = icon.gameObject.ConvertToButton<StandardMenuButton>(true);
            for (int i = 0; i < cat.tools.Count; i++)
            {
                catMan.allTools.Add(CreateToolButton(empty.transform, cat.tools[i]));
            }
            if (catMan.allTools.Count > 7)
            {
                CreateNextPageButton(empty.transform, catMan);
            }
            catMan.Initialize();
            slot.transform.SetAsLastSibling();
            button.OnPress.AddListener(() =>
            {
                catMan.OnClick();
            });
        }

        public ToolIconManager CreateToolButton(Transform parent, EditorTool tool)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotStandard"), parent, new Vector2(0f,0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Slot (" + tool.GetType().Name + ")";
            Image icon = UIHelpers.CreateImage(tool.editorSprite, slot.transform, new Vector2(0f, 0f), false);
            slot.transform.SetParent(slot.transform, false);
            icon.name = "Icon";
            StandardMenuButton button = icon.gameObject.ConvertToButton<StandardMenuButton>(true);
            button.OnPress.AddListener(() =>
            {
                if (button.GetComponent<EditorTrackCursor>()) return;
                if (selectedTool == null)
                {
                    SelectTool(tool);
                    button.gameObject.AddComponent<EditorTrackCursor>();
                }
            });
            return icon.gameObject.AddComponent<ToolIconManager>();
        }

        public ToolIconManager CreateNextPageButton(Transform parent, CategoryManager category)
        {
            Image slot = UIHelpers.CreateImage(GetUISprite("SlotScroll"), parent, new Vector2(0f, 0f), false);
            slot.transform.SetParent(parent, false);
            slot.name = "Tool Category Next page Button";
            slot.transform.SetParent(slot.transform, false);
            StandardMenuButton button = slot.gameObject.ConvertToButton<StandardMenuButton>(true);
            category.pageButton = slot.gameObject.AddComponent<ToolIconManager>();
            button.OnPress.AddListener(() =>
            {
                category.page = (category.page + 1) % (1 + Mathf.FloorToInt(category.allTools.Count / 7));
                category.SwitchPage();
            });
            return slot.gameObject.GetComponent<ToolIconManager>();
        }

        void UpdateCursor()
        {
            StartCoroutine(UpdateCursorDelay());
        }

        private IEnumerator UpdateCursorDelay()
        {
            yield return null;
            cursor.transform.SetAsLastSibling();
            yield break;
        }
    }
}
