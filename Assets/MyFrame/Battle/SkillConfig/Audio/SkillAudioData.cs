using System;
using System.Collections.Generic;
using Sirenix.Serialization;

public class SkillAudioData
{
    [NonSerialized,OdinSerialize]
    public List<SkillAudioEvent> FrameData = new List<SkillAudioEvent>(); 
    
}
