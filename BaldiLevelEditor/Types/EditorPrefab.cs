using HarmonyLib;
using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
    public interface IEditor3D
    {
        string highlight { get; set; }
        bool collidersEnabled { get; set; }
        GameObject gameObject { get; }
        Transform transform { get; }
        IEditorLocation location { get; set; }
        bool DoesExist(EditorLevel level);
        void UpdateObject();
        void DoneUpdateObject();
        void DestroyObject(EditorLevel level);
    }

    public abstract class PrefabBase<T> : MonoBehaviour, IEditor3D where T : IEditorLocation
    {
        MeshRenderer[] meshRenderers = new MeshRenderer[0];
        SpriteRenderer[] spriteRenderers = new SpriteRenderer[0];
        Collider[] colliders = new Collider[0];
        private string _highlight = "none";
        public bool collidersEnabled
        {
            get
            {
                return colliders.Where(x => x.enabled == true).Count() > 0;
            }
            set
            {
                colliders.Do(x => x.enabled = value);
            }
        }
        public string highlight
        {
            get => _highlight;
            set
            {
                if (value != _highlight)
                {
                    meshRenderers.Do(x =>
                    {
                        x.materials.Do(z =>
                        {
                            z.SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                        });
                    });
                    spriteRenderers.Do(x =>
                    {
                        x.materials.Do(z =>
                        {
                            z.SetTexture("_LightMap", value == "none" ? BaldiLevelEditorPlugin.lightmapTexture : BaldiLevelEditorPlugin.lightmaps[value]);
                        });
                    });
                }
                _highlight = value;
            }
        }

        void Awake()
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            colliders = GetComponentsInChildren<Collider>();
        }

        public T obj;

        public virtual IEditorLocation location
        {
            get
            {
                return obj;
            }
            set
            {
                obj = (T)value;
            }
        }

        public abstract void UpdateObject();

        public abstract bool DoesExist(EditorLevel level);

        public abstract void DestroyObject(EditorLevel level);

        public virtual void DoneUpdateObject()
        {
            
        }
    }

    public class EditorPrefab : PrefabBase<PrefabLocation>
    {
        public override void DestroyObject(EditorLevel level)
        {
            level.prefabs.Remove(obj);
        }

        public override bool DoesExist(EditorLevel level)
        {
            return (level.rooms.Where(z => z.prefabs.Contains(location)).Count() > 0);
        }

        public override void UpdateObject()
        {
            obj.position = transform.position.ToData();
            obj.rotation = transform.rotation.ToData();
        }
    }

    public class ItemPrefab : PrefabBase<ItemLocation>
    {

        public ItemObject itemObject => BaldiLevelEditorPlugin.itemObjects[obj.item];

        public override void UpdateObject()
        {
            obj.position = transform.position.ToData();
        }

        public override void DoneUpdateObject()
        {
            base.DoneUpdateObject();
        }

        public override bool DoesExist(EditorLevel level)
        {
            return (level.rooms.Where(z => z.items.Contains(obj)).Count() > 0);
        }

        public override void DestroyObject(EditorLevel level)
        {
            level.rooms.Where(x => x.items.Contains(obj)).Do(x => x.items.Remove(obj));
            level.items.Remove(obj);
        }
    }

    public class ActivityPrefab : PrefabBase<RoomActivity>
    {
        public override void UpdateObject()
        {
            obj.position = transform.position.ToData();
        }

        public override void DoneUpdateObject()
        {
            base.DoneUpdateObject();
            obj.direction = Directions.DirFromVector3(transform.forward, 45f).ToData();
            transform.rotation = obj.direction.ToStandard().ToRotation();
        }

        public override bool DoesExist(EditorLevel level)
        {
            return (level.rooms.Where(z => z.activity == obj).Count() > 0);
        }

        public override void DestroyObject(EditorLevel level)
        {
            level.rooms.Where(x => x.activity == obj).Do(x => x.activity = null);
        }
    }

    public class EditorHasNoCollidersInBaseGame : MonoBehaviour
    {

    }

    public class EditorObjectType
    {
        public string name = "null";
        public bool canRotate = true;
        public IEditor3D prefab;
        public Vector3 offset;

        public static EditorObjectType CreateFromGameObject<T, T3D>(string id, GameObject reference, Vector3 offset, bool useActual = false) where T : PrefabBase<T3D>
            where T3D : IEditorLocation
        {
            GameObject obj = useActual ? reference : BaldiLevelEditorPlugin.StripAllScripts(reference);
            Collider collider = obj.gameObject.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("Placeholder automatic hitboxes for: " + reference.name + "!");
                obj.gameObject.AddComponent<EditorHasNoCollidersInBaseGame>(); // dumb hack
                collider = obj.gameObject.AddComponent<BoxCollider>();
                ((BoxCollider)collider).size = Vector3.one * 2.5f;
            }
            T edObj = collider.gameObject.AddComponent<T>();
            collider.gameObject.layer = LayerMask.NameToLayer("Default");
            return new EditorObjectType()
            {
                prefab = edObj,
                name = id,
                offset = offset
            };
        }
    }
}
