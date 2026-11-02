using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

/// <summary>
/// 动画技能数据
/// </summary>
[Serializable]
public class SkillAnimationData
{
    /// <summary>
    /// 动画帧事件数据
    /// Key:帧数
    /// Value:帧事件数据
    /// </summary>
    [NonSerialized,OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel ="帧数",ValueLabel ="帧事件数据" )]
    public Dictionary<int,SkillAnimationEvent> FrameData = new ();
}