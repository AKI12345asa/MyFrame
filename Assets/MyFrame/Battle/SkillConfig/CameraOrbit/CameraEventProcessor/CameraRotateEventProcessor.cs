using System.Collections.Generic;
using UnityEngine;

public class CameraRotateEventProcessor : CameraEventProcessor<CameraRotateEvent>
{
    // 相对于技能开始时宿主朝向的起始旋转
    private readonly Dictionary<CameraRotateEvent, Quaternion> _startLocalRotations = new();
    private bool _hasOriginalRotation;
    private Quaternion _originalLocalRotation;

    public CameraRotateEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void StartEvent(CameraRotateEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        var startRotation = Quaternion.Inverse(camera.InitialModelRotation) * camera.transform.rotation;
        _startLocalRotations[ev] = startRotation;
        if (_hasOriginalRotation) return;
        _hasOriginalRotation = true;
        _originalLocalRotation = startRotation;
    }

    protected override void TickEvent(CameraRotateEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        if (!camera.IsActive || !_startLocalRotations.TryGetValue(ev, out var startRotation)) return;
        var localRotation = Quaternion.Slerp(startRotation, ev.Rotation, Mathf.Clamp01(ev.RotateCurve.Evaluate(time.Progress)));
        camera.HoldWorldRotation(camera.InitialModelRotation * localRotation);
    }

    protected override void EndEvent(CameraRotateEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        _startLocalRotations.Remove(ev);
    }

    public override void ResetProcessor(SkillCamera camera, Skill_Player skillPlayer)
    {
        _startLocalRotations.Clear();
        if (camera == null) return;

        camera.ReleaseWorldRotation();
        if (_hasOriginalRotation)
        {
            camera.transform.rotation = camera.Owner.ModelTransform.rotation * _originalLocalRotation;
        }
        _hasOriginalRotation = false;
    }
}
