using System;
using System.Collections.Generic;
using Sirenix.Serialization;

public class SkillCustomEventData
{
    [NonSerialized,OdinSerialize]
    public Dictionary<int, SkillCustomEvent> FrameData = new();
}
