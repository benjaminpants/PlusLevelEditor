using PlusLevelFormat;
using System;
using UnityEngine;

namespace PlusLevelLoader
{
    public static class Extensions
    {
        public static Direction ToStandard(this PlusDirection direction)
        {
            return (Direction)direction;
        }

        public static PlusDirection ToData(this Direction direction)
        {
            return (PlusDirection)direction;
        }

        public static IntVector2 ToInt(this ByteVector2 me)
        {
            return new IntVector2(me.x, me.y);
        }

        public static ByteVector2 ToByte(this IntVector2 me)
        {
            return new ByteVector2(me.x, me.z);
        }

        public static Vector3 ToUnity(this UnityVector3 me)
        {
            return new Vector3(me.x, me.y, me.z);
        }

        public static Quaternion ToUnity(this UnityQuaternion me)
        {
            return new Quaternion(me.x, me.y, me.z, me.w);
        }

        public static UnityVector3 ToData(this Vector3 me)
        {
            return new UnityVector3(me.x, me.y, me.z);
        }

        public static UnityQuaternion ToData(this Quaternion me)
        {
            return new UnityQuaternion(me.x, me.y, me.z, me.w);
        }
    }
}
