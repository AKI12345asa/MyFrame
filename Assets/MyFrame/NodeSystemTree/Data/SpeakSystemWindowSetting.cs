using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace SpeakSystemTree.Data
{
   [CreateAssetMenu(fileName = "WindowSetting",menuName = "SpeakSystemTree/设置数据")]
   public class SpeakSystemWindowSetting : SerializedScriptableObject
   {
      public  List<INodeViewer> CopyNodes = new List<INodeViewer>();
     [OdinSerialize] public Type type;
      #region 颜色设置
      [FoldoutGroup("节点背景颜色")]
      [OdinSerialize, FoldoutGroup("节点背景颜色/执行器节点颜色"), LabelText("选择执行器")]
      public Color ChoiceNodeColor;

      [OdinSerialize, FoldoutGroup("节点背景颜色/执行器节点颜色"), LabelText("顺序执行器")]
      public Color SequenceNodeColor;

      [OdinSerialize, FoldoutGroup("节点背景颜色/行为节点颜色"), LabelText("显示窗口")]
      public Color ShowDialogueNodeColor;

      [OdinSerialize, FoldoutGroup("节点背景颜色/行为节点颜色"), LabelText("关闭窗口")]
      public Color CloseDialogueNodeColor;
      #endregion
      public static SpeakSystemWindowSetting GetSetting()
      {
         return Resources.Load<SpeakSystemWindowSetting>("WindowSetting");
      }
      
      
   }
}
