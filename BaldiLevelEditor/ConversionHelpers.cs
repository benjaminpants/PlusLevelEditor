using PlusLevelFormat;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor
{
    public static class ConversionHelpers
    {
        public static Vector3 ToAxisMultipliers(this Direction me)
        {
            Vector3 resultVector = me.ToVector3();
            return new Vector3(Mathf.Abs(resultVector.x), Mathf.Abs(resultVector.y), Mathf.Abs(resultVector.z));
        }

        public static IntVector2 LockAxis(this IntVector2 me, IntVector2 origin, Direction toLock)
        {
            Vector3 locked = toLock.ToAxisMultipliers();
            return new IntVector2(locked.x == 1f ? me.x : origin.x, locked.z == 1f ? me.z : origin.z);
        }

        public static IntVector2 Max(this IntVector2 me, IntVector2 max)
        {
            return new IntVector2(Mathf.Max(me.x, max.x), Mathf.Max(me.z, max.z));
        }
    }
}
