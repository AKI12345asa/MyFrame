using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFrame{
    public class UIWindowData
    {
        [LabelText("是否需要缓存")] public bool ifCache;
        [LabelText("是否会预初始化")]public bool ifPreInit;
        [LabelText("预制体")] public string assetPath;
        [LabelText("UI层级")] public int layerNum;

        /// <summary>
        /// 这个元素的窗口对象
        /// </summary>
        public UIWindowData(bool ifCache, int layerNum,string assetPath,bool ifPreInit)
        {
            this.ifCache = ifCache;
            this.layerNum = layerNum;
            this.assetPath = assetPath;
            this.ifPreInit = ifPreInit;
        }
    }
}
