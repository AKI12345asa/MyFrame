using System;
using UnityEngine;

namespace MyFrame
{
    [Serializable]
    public class UILayoutData
    {
        public Sprite maskSprite;
        public Color maskColor = new Color(1f,1f,1f,0f);
        public bool enableMask = false;
        public bool ifBlockRayCast = false;
    }
}