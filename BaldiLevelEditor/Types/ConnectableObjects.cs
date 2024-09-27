using PlusLevelFormat;
using PlusLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
   
    public interface IEditorConnectable
    {
        Vector3 connectionPosition { get; }
        bool isTiled { get; }
        PrefabLocation locationPrefab { get; }
        TiledPrefab tiledPrefab { get; }
        string highlight { get; set; }
    }

    public class TiledEditorConnectable : TileBasedVisualBase, IEditorConnectable
    {
        public bool isTiled => true;

        public PrefabLocation locationPrefab => throw new NotImplementedException();

        public TiledPrefab tiledPrefab => prefab;

        public Vector3 positionOffset = Vector3.zero;

        public float directionAddition = 0f;

        public Vector3 connectionPosition => transform.position + positionOffset + (tiledPrefab.direction.ToStandard().ToVector3() * directionAddition);
    }
}
