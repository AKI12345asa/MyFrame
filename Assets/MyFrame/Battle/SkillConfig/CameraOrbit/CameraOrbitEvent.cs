using System;
using Cinemachine;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CameraEventAttribute : Attribute
{
    public string MenuPath;

    public string EventName;

    public CameraEventAttribute(string menuPath)
    {
        MenuPath = menuPath;
    }

    public static string GetDisplayName(Type type)
    {
        var attribute = (CameraEventAttribute)GetCustomAttribute(type, typeof(CameraEventAttribute), false);
        if (attribute == null || string.IsNullOrEmpty(attribute.MenuPath)) return type.Name;
        return attribute.EventName;
    }
}

public abstract class CameraEventBase
{
    public string TrackName;
    public int FrameIndex = -1;
    public int Duration = 5;

    /// <summary>
    /// 由 CameraEvent 标签路径的最后一段得到的默认显示名称
    /// </summary>
    public string DisplayName => CameraEventAttribute.GetDisplayName(GetType());
}

[CameraEvent("镜头效果/震动",EventName = "Impulse")]
public class CameraImpulseEvent : CameraEventBase
{
    public CinemachineImpulseDefinition.ImpulseShapes ImpulseShapes;
    public AnimationCurve CustomImpulseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public Vector3 CustomImpulseDirection;
    public float ImpulseForce;
}

[CameraEvent("镜头表现/旋转",EventName = "Rotate")]
public class CameraRotateEvent : CameraEventBase
{
    public Quaternion Rotation;
    public AnimationCurve RotateCurve = AnimationCurve.Linear(0, 0, 1, 1);
}

[CameraEvent("镜头表现/FOV变化",EventName = "FovChange")]
public class CameraFovChangeEvent : CameraEventBase
{
    public float Fov;
    public AnimationCurve FovChangeCurve = AnimationCurve.Linear(0, 0, 1, 1);
}

[CameraEvent("镜头表现/复位",EventName = "Reset")]
public class CameraResetEvent : CameraEventBase
{
}
