using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SkillCustomEvent
{
    public SkillEventType EventType;
    [ShowIf("@EventType == SkillEventType.Custom")]public string CustomEventName;
    public int IntArg;
    public float FloatArg;
    public string StringArg;
    public UnityEngine.Object ObjectArg;
}
/// <summary>
/// 自定义事件类型,自行增删
/// </summary>
public enum SkillEventType
{
    Custom,
    UnFreezeSkillRelease,
    CanRotate,
    CantRotate,
    EnemyFreezeSkillRelease,
}
