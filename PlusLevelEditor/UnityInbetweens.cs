using System;
using System.Collections.Generic;
using System.Text;

namespace PlusLevelFormat
{
    public struct UnityVector3
    {
        public float x;
        public float y;
        public float z;

        public UnityVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public struct UnityQuaternion 
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public UnityQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
}
