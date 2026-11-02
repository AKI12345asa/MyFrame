using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraEventInspector : SkillEventDataInspectorBase<CameraEventTrackItem, CameraEventTrack>
{
    private static Dictionary<Type, CameraEventInspectorItem> _inspectors;
    private static bool _hasUnsavedSceneHandleChanges;
    public new VisualElement Root
    {
        get => base.Root;
        set => base.Root = value;
    }

    public override void OnDraw()
    {
        DrawCommonFields();
        if (!GetInspectors().TryGetValue(trackItem.CameraEvent.GetType(), out var inspector))
        {
            Root.Add(new HelpBox($"未注册相机事件 Inspector：{trackItem.CameraEvent.GetType().Name}", HelpBoxMessageType.Error));
            return;
        }

        inspector.Draw(this, trackItem.CameraEvent);
    }

    public static void DrawSceneGUI(
        CameraEventBase cameraEvent,
        Transform model,
        Transform skillCamera,
        Quaternion modelRotation)
    {
        if (model == null) return;

        if (GetInspectors().TryGetValue(cameraEvent.GetType(), out var inspectorItem))
        {
            inspectorItem.DrawSceneGUI(cameraEvent, model, skillCamera, modelRotation);
        }

        var currentEvent = Event.current;
        if (!_hasUnsavedSceneHandleChanges || currentEvent == null ||
            (currentEvent.type != EventType.MouseUp && currentEvent.rawType != EventType.MouseUp))
        {
            return;
        }

        _hasUnsavedSceneHandleChanges = false;
        SkillEditorWindow.Instance.SaveConfig();
    }

    public static void ApplyFrameEvent(CameraEventBase cameraEvent, CameraEventContext context)
    {
        if (!GetInspectors().TryGetValue(cameraEvent.GetType(), out var inspectorItem)) return;

        var currentFrame = context.CurrentFrame;
        if (currentFrame < cameraEvent.FrameIndex ||
            (!inspectorItem.PersistsAfterEnd && currentFrame > cameraEvent.FrameIndex + cameraEvent.Duration))
        {
            return;
        }

        context.FrameProgress = Mathf.Clamp01((float)(currentFrame - cameraEvent.FrameIndex) / Mathf.Max(1, cameraEvent.Duration));
        inspectorItem.OnFrameEvent(cameraEvent, context);
    }

    public static void MarkSceneHandleChanged()
    {
        _hasUnsavedSceneHandleChanges = true;
        SkillEditorInspector.Instance.Show();
    }

    internal void DrawCommonFields()
    {
        var trackNameField = new TextField("轨道名称") { value = trackItem.CameraEvent.TrackName };
        var previousTrackName = trackNameField.value;
        trackNameField.RegisterCallback<FocusInEvent>(_ => previousTrackName = trackNameField.value);
        trackNameField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (previousTrackName == trackNameField.value) return;
            trackItem.CameraEvent.TrackName = trackNameField.value;
            trackItem.SetTrackName(trackNameField.value);
            SaveConfig();
        });
        Root.Add(trackNameField);

        var frameIndexField = new IntegerField("起始帧") { value = trackItem.CameraEvent.FrameIndex };
        var previousFrameIndex = frameIndexField.value;
        frameIndexField.RegisterCallback<FocusInEvent>(_ => previousFrameIndex = frameIndexField.value);
        frameIndexField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (previousFrameIndex == frameIndexField.value) return;
            trackItem.SetFrameIndex(frameIndexField.value);
            if (frameIndexField.value != trackItem.CameraEvent.FrameIndex)
            {
                frameIndexField.SetValueWithoutNotify(trackItem.CameraEvent.FrameIndex);
            }
            SaveConfig();
        });
        Root.Add(frameIndexField);

        var durationField = new IntegerField("持续帧数") { value = trackItem.CameraEvent.Duration };
        var previousValue = durationField.value;
        durationField.RegisterCallback<FocusInEvent>(_ => previousValue = durationField.value);
        durationField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (previousValue == durationField.value) return;
            trackItem.CameraEvent.Duration = Mathf.Max(1, durationField.value);
            if (durationField.value != trackItem.CameraEvent.Duration)
            {
                durationField.SetValueWithoutNotify(trackItem.CameraEvent.Duration);
            }
            trackItem.ResetView();
            SaveConfig();
        });
        Root.Add(durationField);
    }

    public void AddElement(VisualElement element)
    {
        Root.Add(element);
    }

    public void AddFloatField(string label, float currentValue, Action<float> setValue)
    {
        var field = new FloatField(label) { value = currentValue };
        var previousValue = currentValue;
        field.RegisterCallback<FocusInEvent>(_ => previousValue = field.value);
        field.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (Mathf.Approximately(previousValue, field.value)) return;
            setValue(field.value);
            SaveConfig();
        });
        Root.Add(field);
    }

    public void AddVector3Field(string label, Vector3 currentValue, Action<Vector3> setValue)
    {
        var field = new Vector3Field(label) { value = currentValue };
        var previousValue = currentValue;
        field.RegisterCallback<FocusInEvent>(_ => previousValue = field.value);
        field.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (previousValue == field.value) return;
            setValue(field.value);
            SaveConfig();
        });
        Root.Add(field);
    }

    public void AddCurveField(string label, AnimationCurve currentValue, Action<AnimationCurve> setValue)
    {
        var field = new CurveField(label) { value = currentValue };
        var previousValue = currentValue;
        field.RegisterCallback<FocusInEvent>(_ => previousValue = field.value);
        field.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (CurveEquals(previousValue, field.value)) return;
            setValue(field.value);
            SaveConfig();
        });
        Root.Add(field);
    }

    internal void SaveConfig()
    {
        SkillEditorWindow.Instance.SaveConfig();
    }

    private static Dictionary<Type, CameraEventInspectorItem> GetInspectors()
    {
        if (_inspectors != null) return _inspectors;

        _inspectors = new Dictionary<Type, CameraEventInspectorItem>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<CameraEventInspectorItem>())
        {
            if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null) continue;
            var attribute = type.GetCustomAttributes(typeof(CameraEventInspectorAttribute), false)
                .Cast<CameraEventInspectorAttribute>()
                .FirstOrDefault();
            if (attribute == null) continue;
            _inspectors.Add(attribute.EventType, (CameraEventInspectorItem)Activator.CreateInstance(type));
        }

        return _inspectors;
    }

    private static bool CurveEquals(AnimationCurve first, AnimationCurve second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first == null || second == null || first.length != second.length) return false;
        for (var i = 0; i < first.length; i++)
        {
            if (!first.keys[i].Equals(second.keys[i])) return false;
        }
        return true;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CameraEventInspectorAttribute : Attribute
{
    public Type EventType { get; }

    public CameraEventInspectorAttribute(Type eventType)
    {
        EventType = eventType;
    }
}

/// <summary>
/// 帧事件发生时提供给 InspectorItem 的相机上下文
/// </summary>
public class CameraEventContext
{
    public Transform CameraTransform;
    public CinemachineVirtualCameraBase VirtualCamera;
    public Transform ModelTransform;
    public Quaternion ModelRotation;
    public Vector3 InitialPosition;
    public Quaternion InitialRotation;
    public float InitialFieldOfView;
    public int CurrentFrame;
    public int LastFrame;
    public float FrameRate;
    public bool IsPlaying;
    // 帧进度 [0,1]
    public float FrameProgress;
    public Func<CameraEventBase, bool> TryMarkTriggered;

    public float FieldOfView
    {
        get
        {
            switch (VirtualCamera)
            {
                case CinemachineVirtualCamera virtualCamera:
                    return virtualCamera.m_Lens.FieldOfView;
                case CinemachineFreeLook freeLook:
                    return freeLook.m_Lens.FieldOfView;
                default:
                    return 0;
            }
        }
        set
        {
            switch (VirtualCamera)
            {
                case CinemachineVirtualCamera virtualCamera:
                    var virtualCameraLens = virtualCamera.m_Lens;
                    virtualCameraLens.FieldOfView = value;
                    virtualCamera.m_Lens = virtualCameraLens;
                    break;
                case CinemachineFreeLook freeLook:
                    var freeLookLens = freeLook.m_Lens;
                    freeLookLens.FieldOfView = value;
                    freeLook.m_Lens = freeLookLens;
                    break;
            }
        }
    }

    public void ResetCamera()
    {
        CameraTransform.SetPositionAndRotation(InitialPosition, InitialRotation);
        FieldOfView = InitialFieldOfView;
    }
}

public abstract class CameraEventInspectorItem
{
    public abstract void Draw(CameraEventInspector inspector, CameraEventBase cameraEvent);

    // 事件结束后是否仍保持其效果
    public virtual bool PersistsAfterEnd => false;

    public virtual void OnFrameEvent(CameraEventBase cameraEvent, CameraEventContext context)
    {
    }

    public virtual void DrawSceneGUI(
        CameraEventBase cameraEvent,
        Transform model,
        Transform skillCamera,
        Quaternion modelRotation)
    {
    }
}

public abstract class CameraEventInspectorItem<T> : CameraEventInspectorItem where T : CameraEventBase
{
    public sealed override void Draw(CameraEventInspector inspector, CameraEventBase cameraEvent)
    {
        if (cameraEvent is not T typedEvent)
        {
            throw new InvalidOperationException($"相机事件 Inspector 类型不匹配：预期 {typeof(T).Name}，实际 {cameraEvent.GetType().Name}");
        }

        Draw(inspector, typedEvent);
    }

    protected abstract void Draw(CameraEventInspector inspector, T cameraEvent);

    public sealed override void OnFrameEvent(CameraEventBase cameraEvent, CameraEventContext context)
    {
        if (cameraEvent is not T typedEvent)
        {
            throw new InvalidOperationException($"相机帧事件类型不匹配：预期 {typeof(T).Name}，实际 {cameraEvent.GetType().Name}");
        }

        OnFrameEvent(typedEvent, context);
    }

    protected virtual void OnFrameEvent(T cameraEvent, CameraEventContext context)
    {
    }

    public sealed override void DrawSceneGUI(
        CameraEventBase cameraEvent,
        Transform model,
        Transform skillCamera,
        Quaternion modelRotation)
    {
        if (cameraEvent is not T typedEvent)
        {
            throw new InvalidOperationException($"相机事件 SceneView 类型不匹配：预期 {typeof(T).Name}，实际 {cameraEvent.GetType().Name}");
        }

        DrawSceneGUI(typedEvent, model, skillCamera, modelRotation);
    }

    protected virtual void DrawSceneGUI(T cameraEvent, Transform model, Transform skillCamera, Quaternion modelRotation)
    {
    }
}

[CameraEventInspector(typeof(CameraImpulseEvent))]
public sealed class CameraImpulseEventInspectorItem : CameraEventInspectorItem<CameraImpulseEvent>
{
    protected override void Draw(CameraEventInspector inspector, CameraImpulseEvent cameraEvent)
    {
        var shapeField = new EnumField("震动波形", cameraEvent.ImpulseShapes);
        shapeField.RegisterValueChangedCallback(evt =>
        {
            cameraEvent.ImpulseShapes = (CinemachineImpulseDefinition.ImpulseShapes)evt.newValue;
            inspector.SaveConfig();
            SkillEditorInspector.Instance.Show();
        });
        inspector.AddElement(shapeField);

        if (cameraEvent.ImpulseShapes == CinemachineImpulseDefinition.ImpulseShapes.Custom)
        {
            inspector.AddCurveField("自定义震动曲线", cameraEvent.CustomImpulseCurve,
                value => cameraEvent.CustomImpulseCurve = value);
        }
        inspector.AddVector3Field("震动方向", cameraEvent.CustomImpulseDirection,
            value => cameraEvent.CustomImpulseDirection = value);
        inspector.AddFloatField("震动强度", cameraEvent.ImpulseForce,
            value => cameraEvent.ImpulseForce = value);
    }

    protected override void OnFrameEvent(CameraImpulseEvent cameraEvent, CameraEventContext context)
    {
        if (!context.IsPlaying || context.LastFrame >= cameraEvent.FrameIndex ||
            !context.TryMarkTriggered(cameraEvent))
        {
            return;
        }

        var impulseSource = context.CameraTransform.GetComponent<CinemachineImpulseSource>();
        var impulseListener = context.CameraTransform.GetComponent<CinemachineImpulseListener>();
        if (impulseSource == null || impulseListener == null)
        {
            Debug.LogWarning("SkillCamera requires both CinemachineImpulseSource and CinemachineImpulseListener to preview camera impulse.");
            return;
        }

        impulseSource.m_ImpulseDefinition.m_ImpulseShape = cameraEvent.ImpulseShapes;
        impulseSource.m_ImpulseDefinition.m_ImpulseDuration = (float)cameraEvent.Duration / context.FrameRate;
        impulseSource.m_ImpulseDefinition.m_CustomImpulseShape = cameraEvent.CustomImpulseCurve;
        impulseSource.m_DefaultVelocity = cameraEvent.CustomImpulseDirection;
        impulseSource.GenerateImpulseWithForce(cameraEvent.ImpulseForce);
    }
}

[CameraEventInspector(typeof(CameraRotateEvent))]
public sealed class CameraRotateEventInspectorItem : CameraEventInspectorItem<CameraRotateEvent>
{
    protected override void Draw(CameraEventInspector inspector, CameraRotateEvent cameraEvent)
    {
        inspector.AddVector3Field("相对模型旋转", cameraEvent.Rotation.eulerAngles,
            value => cameraEvent.Rotation = Quaternion.Euler(value));
        inspector.AddCurveField("旋转曲线", cameraEvent.RotateCurve, value => cameraEvent.RotateCurve = value);
    }

    public override bool PersistsAfterEnd => true;

    protected override void OnFrameEvent(CameraRotateEvent cameraEvent, CameraEventContext context)
    {
        context.CameraTransform.rotation = Quaternion.Slerp(
            context.CameraTransform.rotation,
            context.ModelRotation * cameraEvent.Rotation,
            Mathf.Clamp01(cameraEvent.RotateCurve.Evaluate(context.FrameProgress)));
    }

    protected override void DrawSceneGUI(
        CameraRotateEvent cameraEvent,
        Transform model,
        Transform skillCamera,
        Quaternion modelRotation)
    {
        var cameraPosition = skillCamera == null ? model.position : skillCamera.position;
        EditorGUI.BeginChangeCheck();
        var worldRotation = Handles.RotationHandle(modelRotation * cameraEvent.Rotation, cameraPosition);
        if (!EditorGUI.EndChangeCheck()) return;

        cameraEvent.Rotation = Quaternion.Inverse(modelRotation) * worldRotation;
        CameraEventInspector.MarkSceneHandleChanged();
    }
}

[CameraEventInspector(typeof(CameraFovChangeEvent))]
public sealed class CameraFovChangeEventInspectorItem : CameraEventInspectorItem<CameraFovChangeEvent>
{
    protected override void Draw(CameraEventInspector inspector, CameraFovChangeEvent cameraEvent)
    {
        inspector.AddFloatField("目标 FOV", cameraEvent.Fov, value => cameraEvent.Fov = value);
        inspector.AddCurveField("FOV 变化曲线", cameraEvent.FovChangeCurve,
            value => cameraEvent.FovChangeCurve = value);
    }

    protected override void OnFrameEvent(CameraFovChangeEvent cameraEvent, CameraEventContext context)
    {
        context.FieldOfView = Mathf.Lerp(
            context.FieldOfView,
            cameraEvent.Fov,
            Mathf.Clamp01(cameraEvent.FovChangeCurve.Evaluate(context.FrameProgress)));
    }
}

[CameraEventInspector(typeof(CameraResetEvent))]
public sealed class CameraResetEventInspectorItem : CameraEventInspectorItem<CameraResetEvent>
{
    protected override void Draw(CameraEventInspector inspector, CameraResetEvent cameraEvent)
    {
        inspector.AddElement(new Label("复位相机位置、旋转与视野"));
    }

    protected override void OnFrameEvent(CameraResetEvent cameraEvent, CameraEventContext context)
    {
        context.ResetCamera();
    }
}
