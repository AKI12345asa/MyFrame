using System;

namespace MyFrame
{
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UIWindowDataAttribute : Attribute
    {
        public string windowKey;
        public bool ifCache;
        public bool ifPreInit;
        public string assetPath;
        public int layerNum;

        public UIWindowDataAttribute(string windowKey, bool ifCache, string assetPath, int layerNum,bool ifPreInit)
        {
            this.windowKey = windowKey;
            this.ifCache = ifCache;
            this.assetPath = assetPath;
            this.layerNum = layerNum;
            this.ifPreInit = ifPreInit;
        }
        public UIWindowDataAttribute(Type type, bool ifCache, string assetPath, int layerNum,bool ifPreInit)
        {
            this.windowKey = type.FullName;
            this.ifCache = ifCache;
            this.assetPath = assetPath;
            this.layerNum = layerNum;
            this.ifPreInit = ifPreInit;
        }
    }
}
