using System;
using System.Collections.Generic;
using Sirenix.Serialization;
    public class AttackDetectionData
    {
        [NonSerialized,OdinSerialize]
        public List<AttackDetectionEvent> FrameData = new ();
    }
