using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AttackDetectionTrackItem : TrackItemBase<AttackDetectionTrack>
{
    private SkillMutilineTrackStyle.ChildTrack _childTrack;

    private SkillAttackDetectionTrackItemStyle AttackDetectionTrackItemStyle =>
        ItemStyle as SkillAttackDetectionTrackItemStyle;

    private AttackDetectionEvent _attackEvent;

    public AttackDetectionEvent AttackDetectionEvent => _attackEvent;

    public void Init(AttackDetectionTrack track, float frameUnitWidth, AttackDetectionEvent attackEvent,
        SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        Track = track;
        _frameIndex = attackEvent.FrameIndex;
        ItemStyle = new SkillAttackDetectionTrackItemStyle();
        _childTrack = childTrack;
        FrameUnitWidth = frameUnitWidth;
        if (attackEvent.TriggerEvents == null) attackEvent.TriggerEvents = new();
        NormalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
        SelectColor = new Color(0.388f, 0.850f, 0.905f, 0.8f);
        _attackEvent = attackEvent;
        ResetView(frameUnitWidth);
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        if (!AttackDetectionTrackItemStyle.IsInit)
        {
            //绑定事件
            AttackDetectionTrackItemStyle.Init(FrameUnitWidth, _attackEvent, _childTrack);
            AttackDetectionTrackItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(MouseDown);
            AttackDetectionTrackItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(MouseUp);
            AttackDetectionTrackItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(MouseOut);
            AttackDetectionTrackItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(MouseMove);
        }

        AttackDetectionTrackItemStyle.ResetView(frameWidth, _attackEvent);
    }

    public void Destroy()
    {
        _childTrack.Destroy();
    }

    public void SetTrackName(string name)
    {
        _childTrack.SetTrackName(name);
    }

    #region 鼠标交互

    private bool _mouseDrag = false;
    private float _startDragPosX;
    private int _startDragFrameIndex;
    private readonly object _mouseLock = new object();

    private void MouseDown(MouseDownEvent evt)
    {
        _startDragPosX = evt.mousePosition.x;
        _startDragFrameIndex = _frameIndex;
        _mouseDrag = true;
        Select();

    }

    private void MouseUp(MouseUpEvent evt)
    {
        if (_mouseDrag) ApplyDrag();
        _mouseDrag = false;

    }

    private void MouseOut(MouseOutEvent evt)
    {
        if (_mouseDrag) ApplyDrag();
        _mouseDrag = false;
    }

    private void MouseMove(MouseMoveEvent evt)
    {
        if (!_mouseDrag) return;
        var offsetPos = evt.mousePosition.x - _startDragPosX;
        var offsetFrame = Mathf.RoundToInt(offsetPos / FrameUnitWidth);
        var targetFrameIndex = _startDragFrameIndex + offsetFrame;
        if (targetFrameIndex < 0 || offsetFrame == 0) return; //不考虑拖动到负数帧的情况
        _frameIndex = targetFrameIndex;
        _attackEvent.FrameIndex = _frameIndex;
        //刷新视图
        ResetView(FrameUnitWidth);
    }

    private void ApplyDrag()
    {
        if (_startDragFrameIndex != _frameIndex)
        {
            _attackEvent.FrameIndex = _frameIndex;
        }

        SkillEditorWindow.Instance.SaveConfig();
    }

    public void CheckFrameCount()
    {
        var frameCount = (int)(_attackEvent.DurationFrame);
        //当前动画超过的技能总帧数，则拓展技能总帧数
        if (_frameIndex + frameCount > SkillEditorWindow.Instance.SkillClip.FrameCount)
        {
            SkillEditorWindow.Instance.CurrentFrameCount = _frameIndex + frameCount;
        }
    }

    #endregion

    public void DrawGizmos()
    {
        SkillGizmosTool.DrawDetection(AttackDetectionEvent, SkillEditorWindow.Instance.CurrentPreviewCharacterObj.GetComponentInChildren<Skill_Player>());
    }

    public void OnSceneGUI()
    {
        var previewCharacter = SkillEditorWindow.Instance.CurrentPreviewCharacterObj;
        if (previewCharacter == null) return;

        DrawRepelStrengthHandle(previewCharacter);
        switch (AttackDetectionEvent.AttackDetectionType)
        {
            case AttackDetectionType.Box:
                DrawBoxHandles(previewCharacter, AttackDetectionEvent.AttackDetectionData as AttackBoxDetectionData);
                break;
            case AttackDetectionType.Sphere:
                DrawSphereHandles(previewCharacter, AttackDetectionEvent.AttackDetectionData as AttackSphereDetectionData);
                break;
            case AttackDetectionType.Fan:
                DrawFanHandles(previewCharacter, AttackDetectionEvent.AttackDetectionData as AttackFanDetectionData);
                break;
        }
    }

    private void DrawBoxHandles(GameObject previewCharacter, AttackBoxDetectionData data)
    {
        if (data == null) return;

        var transform = previewCharacter.transform;
        var position = transform.TransformPoint(data.Position);
        var rotation = transform.rotation * Quaternion.Euler(data.Rotation);
        var handleSize = HandleUtility.GetHandleSize(position);
        var changed = false;

        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        var newPosition = Handles.PositionHandle(position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            data.Position = transform.InverseTransformPoint(newPosition);
            changed = true;
        }

        Handles.color = Color.yellow;
        var rotationHandlePosition = position + GetSceneViewRight() * handleSize * 0.7f;
        EditorGUI.BeginChangeCheck();
        var newRotation = Handles.RotationHandle(rotation, rotationHandlePosition);
        if (EditorGUI.EndChangeCheck())
        {
            data.Rotation = (Quaternion.Inverse(transform.rotation) * newRotation).eulerAngles;
            changed = true;
        }

        Handles.color = Color.green;
        var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
        for (var i = 0; i < axes.Length; i++)
        {
            var localAxis = axes[i];
            var worldAxis = rotation * localAxis;
            var size = GetVectorComponent(data.Scale, i);
            var sliderPosition = position + worldAxis * (size * 0.5f + handleSize * 0.15f);
            EditorGUI.BeginChangeCheck();
            var newSize = Handles.ScaleSlider(size, sliderPosition, worldAxis, rotation, handleSize * 0.8f, 0.05f);
            if (!EditorGUI.EndChangeCheck()) continue;

            SetVectorComponent(ref data.Scale, i, Mathf.Max(0.01f, newSize));
            changed = true;
        }

        if (changed) MarkDetectionHandleChanged();
    }

    private void DrawSphereHandles(GameObject previewCharacter, AttackSphereDetectionData data)
    {
        if (data == null) return;

        var transform = previewCharacter.transform;
        var position = transform.TransformPoint(data.Position);
        var handleSize = HandleUtility.GetHandleSize(position);
        var changed = false;

        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        var newPosition = Handles.PositionHandle(position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            data.Position = transform.InverseTransformPoint(newPosition);
            changed = true;
        }

        Handles.color = Color.yellow;
        var radiusHandlePosition = position + Vector3.up * (data.Radius + handleSize * 0.3f);
        EditorGUI.BeginChangeCheck();
        var newRadius = Handles.ScaleSlider(
            data.Radius, radiusHandlePosition, Vector3.up, Quaternion.identity, handleSize * 0.8f, 0.05f);
        if (EditorGUI.EndChangeCheck())
        {
            data.Radius = Mathf.Max(0.01f, newRadius);
            changed = true;
        }

        if (changed) MarkDetectionHandleChanged();
    }

    private void DrawFanHandles(GameObject previewCharacter, AttackFanDetectionData data)
    {
        if (data == null) return;

        var transform = previewCharacter.transform;
        var position = transform.TransformPoint(data.Position);
        var rotation = transform.rotation * Quaternion.Euler(data.Rotation);
        var handleSize = HandleUtility.GetHandleSize(position);
        var changed = false;

        Handles.color = Color.cyan;
        EditorGUI.BeginChangeCheck();
        var newPosition = Handles.PositionHandle(position, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            data.Position = transform.InverseTransformPoint(newPosition);
            changed = true;
        }

        Handles.color = Color.yellow;
        var rotationHandlePosition = position + GetSceneViewRight() * handleSize * 0.7f;
        EditorGUI.BeginChangeCheck();
        var newRotation = Handles.RotationHandle(rotation, rotationHandlePosition);
        if (EditorGUI.EndChangeCheck())
        {
            data.Rotation = (Quaternion.Inverse(transform.rotation) * newRotation).eulerAngles;
            changed = true;
        }

        Handles.color = Color.green;
        var radiusAxis = rotation * Vector3.forward;
        var radiusHandlePosition = position + radiusAxis * (data.Radius + handleSize * 0.3f);
        EditorGUI.BeginChangeCheck();
        var newRadius = Handles.ScaleSlider(
            data.Radius, radiusHandlePosition, radiusAxis, rotation, handleSize * 0.8f, 0.05f);
        if (EditorGUI.EndChangeCheck())
        {
            data.Radius = Mathf.Max(data.InsideRadius + 0.01f, newRadius);
            changed = true;
        }

        Handles.color = Color.blue;
        var heightAxis = rotation * Vector3.up;
        var heightHandlePosition = position + heightAxis * (data.Height * 0.5f + handleSize * 0.3f);
        EditorGUI.BeginChangeCheck();
        var newHeight = Handles.ScaleSlider(
            data.Height, heightHandlePosition, heightAxis, rotation, handleSize * 0.8f, 0.05f);
        if (EditorGUI.EndChangeCheck())
        {
            data.Height = Mathf.Max(0.01f, newHeight);
            changed = true;
        }

        Handles.color = Color.white;
        var insideRadiusHandlePosition = position - radiusAxis * (data.InsideRadius + handleSize * 0.3f);
        EditorGUI.BeginChangeCheck();
        var newInsideRadius = Handles.ScaleSlider(
            data.InsideRadius, insideRadiusHandlePosition, -radiusAxis, rotation, handleSize * 0.8f, 0.05f);
        if (EditorGUI.EndChangeCheck())
        {
            data.InsideRadius = Mathf.Clamp(newInsideRadius, 0, data.Radius - 0.01f);
            changed = true;
        }

        Handles.color = Color.magenta;
        var halfAngle = Mathf.Clamp(data.Angle, 0, 360) * 0.5f;
        var positiveAngleDirection = Quaternion.AngleAxis(halfAngle, heightAxis) * radiusAxis;
        var negativeAngleDirection = Quaternion.AngleAxis(-halfAngle, heightAxis) * radiusAxis;
        var angleHandlePosition = position + positiveAngleDirection * data.Radius;
        var angleTangent = Vector3.Cross(heightAxis, positiveAngleDirection).normalized;
        if (angleTangent.sqrMagnitude < 0.0001f) angleTangent = rotation * Vector3.right;
        Handles.DrawLine(position, position + positiveAngleDirection * data.Radius);
        Handles.DrawLine(position, position + negativeAngleDirection * data.Radius);
        EditorGUI.BeginChangeCheck();
        var newAngleHandlePosition = Handles.Slider(
            angleHandlePosition, angleTangent, handleSize * 0.12f, Handles.ArrowHandleCap, 0);
        if (EditorGUI.EndChangeCheck())
        {
            var newAngleDirection = Vector3.ProjectOnPlane(newAngleHandlePosition - position, heightAxis);
            if (newAngleDirection.sqrMagnitude > 0.0001f)
            {
                data.Angle = Mathf.Clamp(
                    Mathf.Abs(Vector3.SignedAngle(radiusAxis, newAngleDirection.normalized, heightAxis)) * 2f,
                    0, 360);
                changed = true;
            }
        }

        if (changed) MarkDetectionHandleChanged();
    }

    private static float GetVectorComponent(Vector3 value, int axis)
    {
        return axis switch
        {
            0 => value.x,
            1 => value.y,
            _ => value.z
        };
    }

    private static void SetVectorComponent(ref Vector3 value, int axis, float component)
    {
        switch (axis)
        {
            case 0: value.x = component; break;
            case 1: value.y = component; break;
            default: value.z = component; break;
        }
    }

    private static Vector3 GetSceneViewRight()
    {
        var sceneCamera = SceneView.currentDrawingSceneView?.camera;
        return sceneCamera == null ? Vector3.right : sceneCamera.transform.right;
    }

    private static void MarkDetectionHandleChanged()
    {
        SkillEditorWindow.Instance.RequestSaveConfig();
        SceneView.RepaintAll();
    }

    private void DrawRepelStrengthHandle(GameObject previewCharacter)
    {
        if (previewCharacter == null) return;
        var hitEvent = AttackDetectionEvent.TriggerEvents?.OfType<AttackHitEvent>().FirstOrDefault();
        if (hitEvent == null) return;

        var anchor = GetRepelStrengthAnchor(previewCharacter);
        var horizontalDirection = Vector3.right;
        var verticalDirection = Vector3.up;
        var horizontalEnd = anchor + horizontalDirection * hitEvent.HorizontalAttackStrength;
        var verticalEnd = anchor + verticalDirection * hitEvent.VerticalAttackStrength;
        var resultant = horizontalDirection * hitEvent.HorizontalAttackStrength +
                        verticalDirection * hitEvent.VerticalAttackStrength;
        var resultantEnd = anchor + resultant;
        var handleScale = HandleUtility.GetHandleSize(anchor);
        var direction = resultant.sqrMagnitude > 0.0001f ? resultant.normalized : Vector3.forward;

        Handles.color = Color.magenta;
        Handles.DrawAAPolyLine(5f, anchor, resultantEnd);
        Handles.ArrowHandleCap(0, resultantEnd, Quaternion.LookRotation(direction), handleScale * 0.3f,
            EventType.Repaint);

        var handleSize = handleScale * 0.1f;
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.red;
        var newHorizontalEnd = Handles.Slider(horizontalEnd, horizontalDirection, handleSize,
            Handles.ArrowHandleCap, 0);
        Handles.color = Color.green;
        var newVerticalEnd = Handles.Slider(verticalEnd, verticalDirection, handleSize, Handles.ArrowHandleCap, 0);
        if (EditorGUI.EndChangeCheck())
        {
            hitEvent.HorizontalAttackStrength =
                Vector3.Dot(newHorizontalEnd - anchor, horizontalDirection);
            hitEvent.VerticalAttackStrength =
                Vector3.Dot(newVerticalEnd - anchor, verticalDirection);
            SkillEditorWindow.Instance.RequestSaveConfig();
        }
    }

    private Vector3 GetRepelStrengthAnchor(GameObject previewCharacter)
    {
        var data = AttackDetectionEvent.AttackDetectionData;
        switch (AttackDetectionEvent.AttackDetectionType)
        {
            case AttackDetectionType.Weapon:
                var weaponData = data as AttackWeaponDetectionData;
                var skillPlayer = previewCharacter.GetComponentInChildren<Skill_Player>();
                if (weaponData == null || skillPlayer == null || string.IsNullOrEmpty(weaponData.WeaponName) ||
                    !skillPlayer.checkerDic.TryGetValue(weaponData.WeaponName, out var weaponController) ||
                    weaponController == null)
                {
                    return previewCharacter.transform.position;
                }
                var weaponCollider = weaponController.GetComponent<Collider>();
                return weaponCollider != null ? weaponCollider.bounds.center : weaponController.transform.position;
            case AttackDetectionType.Box:
                var boxData = data as AttackBoxDetectionData;
                return boxData == null
                    ? previewCharacter.transform.position
                    : previewCharacter.transform.TransformPoint(boxData.Position);
            case AttackDetectionType.Sphere:
                var sphereData = data as AttackSphereDetectionData;
                return sphereData == null
                    ? previewCharacter.transform.position
                    : previewCharacter.transform.TransformPoint(sphereData.Position);
            case AttackDetectionType.Fan:
                var fanData = data as AttackFanDetectionData;
                return fanData == null
                    ? previewCharacter.transform.position
                    : previewCharacter.transform.TransformPoint(fanData.Position);
            default:
                return previewCharacter.transform.position;
        }
    }
}