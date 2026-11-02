using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(fileName = "KeyConfigs", menuName = "Config/Player/KeyConfigs")]
public class KeyConfigs:FrameConfigBase
{
   [ListDrawerSettings(CustomAddFunction = nameof(AddFunc))]public List<KeyConfig>  keyConfigs = new List<KeyConfig>();

   public KeyConfig AddFunc()
   {
       return new KeyConfig()
       {
           keyGuid = GUID.Generate().ToString()
       };
   }
}
