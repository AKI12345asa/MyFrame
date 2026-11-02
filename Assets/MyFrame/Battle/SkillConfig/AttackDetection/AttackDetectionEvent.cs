using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using Object = UnityEngine.Object;

public class AttackDetectionEvent
{
#if UNITY_EDITOR
public string TrackName = "检测轨道";
#endif
 public int FrameIndex = 0;
 public int DurationFrame = 10;
 public AttackDetectionDataBase AttackDetectionData;
 public List<DetectionTriggerEventBase> TriggerEvents = new();
 public LayerMask CheckLayerMask;
 public int DetectionCount = 0;
 public float ComboCheckDeltaTime = 0;
#if UNITY_EDITOR
    public AttackDetectionType AttackDetectionType
    {
        get => GetAttackDetectionType();
        set
        {
            if (value != AttackDetectionType)//类型发生了变化
            {
                switch (value)
                {
                    case AttackDetectionType.None:
                        AttackDetectionData = null;  
                        break;
                    case AttackDetectionType.Weapon:
                        AttackDetectionData = new AttackWeaponDetectionData();
                        break;
                    case AttackDetectionType.Box:
                        AttackDetectionData = new AttackBoxDetectionData();
                        break;
                    case AttackDetectionType.Sphere:
                        AttackDetectionData = new AttackSphereDetectionData();
                        break;
                    case AttackDetectionType.Fan:
                        AttackDetectionData = new AttackFanDetectionData();
                        break;
                }

                AttackDetectionType = value;
            }
        }
    }
#endif

    public AttackDetectionType GetAttackDetectionType()
    {
        switch (AttackDetectionData)
        {
            case null:
                return AttackDetectionType.None;
            case AttackWeaponDetectionData:
                return AttackDetectionType.Weapon;
            case AttackBoxDetectionData:
                return AttackDetectionType.Box;
            case AttackSphereDetectionData:
                return AttackDetectionType.Sphere;
            case AttackFanDetectionData:
                return AttackDetectionType.Fan;
        }
        return AttackDetectionType.None;
    }
    
}

#region 检测

//攻击检测类型
public enum AttackDetectionType
{
    None,
    Weapon,
    Box,
    Sphere,
    Fan,
}
//攻击检测基类
public abstract class AttackDetectionDataBase
{
    
}
//武器检测
public class AttackWeaponDetectionData : AttackDetectionDataBase
{
    public string WeaponName;
}
//形状检测基类
public abstract class AttackShapeDetectionDataBase : AttackDetectionDataBase
{
    public Vector3 Position;
}


public interface IDetectionWithRotation
{
    public Quaternion GetRotation();
}
//盒型检测
public class AttackBoxDetectionData : AttackShapeDetectionDataBase,IDetectionWithRotation
{
    public Vector3 Rotation;
    public Vector3 Scale = Vector3.one;
    public Quaternion GetRotation()
    {
        return Quaternion.Euler(Rotation);
    }
}

//球形检测
public class AttackSphereDetectionData : AttackShapeDetectionDataBase
{
    public float Radius;
}
//扇形检测
public class AttackFanDetectionData : AttackShapeDetectionDataBase,IDetectionWithRotation
{
    public Vector3 Rotation;
    public float InsideRadius = 1;
    public float Radius = 3;
    public float Height = 0.5f;
    public float Angle = 90;
    public Quaternion GetRotation()
    {
        return Quaternion.Euler(Rotation);
    }
}

#endregion

#region 命中事件
/// <summary>
/// 一种检测事件的基类，所有的检测事件都应该继承自这个类
/// </summary>
public abstract class DetectionTriggerEventBase
{
   
}

public class AttackHitEvent:DetectionTriggerEventBase
{
    public float AttackMultiply;
    public float HorizontalAttackStrength;
    public float VerticalAttackStrength;
    public bool IfRelatedToWeaponDirection = false;
    public float HitTime = 0.1f;
}

public class PlayAudioEvent:DetectionTriggerEventBase
{
    public AudioClip HitAudioClip;
    public float Volume = 1f;
}

public class GenerateEffectEvent:DetectionTriggerEventBase
{
    public GameObject EffectPrefab;
    public float Duration = 1f;
}

public class ImpulseDetectionEvent:DetectionTriggerEventBase
{
    public CinemachineImpulseDefinition.ImpulseShapes ImpulseShape;
    public float ImpulseDuration;
    public AnimationCurve CustomImpulseCurve;
    public Vector3 CustomImpulseDirection;
    public float ImpulseForce;
}

public class StutterFrameEvent:DetectionTriggerEventBase
{
    public float StutterFrameTimeScaleForTarget;
    public float StutterFrameTimeScaleForSelf;
    public float StutterFrameDurationForTarget;
    public float StutterFrameDurationForSelf;
}
#endregion
