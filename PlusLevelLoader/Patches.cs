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

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.levelloader", "Bugfixes", "Supress Banana Null Object Reference")]
    [HarmonyPatch(typeof(ITM_NanaPeel))]
    [HarmonyPatch("Update")]
    static class SupressNanaError
    {
        static Exception Finalizer(Exception __exception)
        {
            if (__exception is NullReferenceException) return null;
            return __exception;
        }
    }

    [ConditionalPatchConfig("mtm101.rulerp.baldiplus.levelloader", "Bugfixes", "Supress Banana Null Object Reference")]
    [HarmonyPatch(typeof(Entity))]
    [HarmonyPatch("Update")]
    static class SupressEntityError
    {
        static Exception Finalizer(Exception __exception)
        {
            if (__exception is NullReferenceException) return null;
            return __exception;
        }
    }
}
