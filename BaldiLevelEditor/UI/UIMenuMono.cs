using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BaldiLevelEditor.UI
{
    public class UIMenuMono : MonoBehaviour
    {
        public object? targetObject;

        public virtual int SendInt(string id, int v)
        {
            return v;
        }
        
        public virtual string SendString(string id, string v)
        {
            return v;
        }

        public virtual bool SendBool(string id, bool v)
        {
            return v;
        }

        public virtual object? SendObject(string id, object v)
        {
            return v;
        }

        public virtual Texture2D SendTexture(string id, Texture2D v)
        {
            return v;
        }
    }
}
