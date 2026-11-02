using System.Collections.Generic;
using UnityEngine;

public struct AttackData
{
    public Vector3 HitPoint;
    public Vector3 HitStrength;
    public float HitTime;
    public ICharacter Source;
    public float AttackValue;
}

public struct ImpulseData
{
   public ImpulseDetectionEvent ImpulseDetectionEvent;
   public Vector3 ImpulseDirection;
   public float ImpulseForce;
   public float ImpulseDuration;
}
