using System;
using System.Collections.Generic;
using Sirenix.Serialization;

public class SkillEffectData
{
    /// <summary>
    /// 特效事件
    /// </summary>
    [NonSerialized,OdinSerialize]
    public List<SkillEffectEvent> FrameData = new List<SkillEffectEvent>(); 
    
}
