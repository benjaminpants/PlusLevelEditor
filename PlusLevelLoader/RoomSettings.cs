﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PlusLevelLoader
{
    public class RoomSettings
    {
        public RoomCategory category;
        public RoomType type;
        public UnityEngine.Color color = Color.white;
        public Material mapMaterial;
        public StandardDoorMats doorMat;
        public RoomFunctionContainer container;

        public RoomSettings(RoomCategory cat, RoomType type, Color color, StandardDoorMats doors, Material mapMaterial = null) 
        {
            category = cat;
            this.type = type;
            this.color = color;
            this.doorMat = doors;
            if (mapMaterial != null)
            {
                this.mapMaterial = mapMaterial;
            }
            else
            {
                this.mapMaterial = PlusLevelLoaderPlugin.Instance.assetMan.Get<Material>("MapTile_Standard");
            }
        }
    }
}
