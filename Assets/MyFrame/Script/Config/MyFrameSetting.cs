using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;

namespace MyFrame
{
    public enum SaveSystemType
    {
        Binary,
        Json
    }
    [CreateAssetMenu(menuName = "MyFrame/Config/FrameSetting", fileName = "FrameSetting")]
    public class MyFrameSetting:FrameConfigBase
    {
        public float FramePerSecond = 30;

        #region 窗口相关

        [OdinSerialize]public Dictionary<string,UIWindowData> UIWindowDataDic = new ();
        public List<UILayoutData> UILayoutDataList = new ();

        #endregion

        #region 存档相关

        public SaveSystemType SaveSystemType = SaveSystemType.Binary;

        #endregion

        #region 配置相关

        public List<string> ConfigFolderPathList = new List<string>();

        #endregion
        
    }
    
    
}
