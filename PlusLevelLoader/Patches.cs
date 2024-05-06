using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PlusLevelLoader
{
    [HarmonyPatch(typeof(LevelBuilder))]
    [HarmonyPatch("BlockTilesToBlock")]
    class BlockTilesToBlockPatch
    {
        static void Prefix(LevelBuilder __instance, EnvironmentController ___ec)
        {
            if (!(__instance is LevelLoader))
            {
                return;
            }
            LevelLoader loader = (LevelLoader)__instance;
            loader.levelAsset.rooms.Do(x =>
            {
                x.blockedWallCells.Do(z =>
                {
                    Cell cell = ___ec.CellFromPosition(z.x, z.z);
                    if (!cell.Null)
                    {
                        cell.HardCoverEntirely();
                    }
                });
            });
        }
    }
}
