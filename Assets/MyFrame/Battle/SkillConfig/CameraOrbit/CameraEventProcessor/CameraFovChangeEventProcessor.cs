using System.Collections.Generic;
using UnityEngine;

public class CameraFovChangeEventProcessor : CameraEventProcessor<CameraFovChangeEvent>
{
    private readonly Dictionary<CameraFovChangeEvent, float> _startFovs = new();
    private bool _hasOriginalFov;
    private float _originalFov;

    public CameraFovChangeEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void StartEvent(CameraFovChangeEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        var startFov = camera.FieldOfView;
        _startFovs[ev] = startFov;
        if (_hasOriginalFov) return;
        _hasOriginalFov = true;
        _originalFov = startFov;
    }

    protected override void TickEvent(CameraFovChangeEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        if (!_startFovs.TryGetValue(ev, out var startFov)) return;

        camera.FieldOfView = Mathf.Lerp(
            startFov,
            ev.Fov,
            Mathf.Clamp01(ev.FovChangeCurve.Evaluate(time.Progress)));
    }

    protected override void EndEvent(CameraFovChangeEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        _startFovs.Remove(ev);
    }

    public override void ResetProcessor(SkillCamera camera, Skill_Player skillPlayer)
    {
        _startFovs.Clear();
        if (camera != null && _hasOriginalFov)
        {
            camera.FieldOfView = _originalFov;
        }
        _hasOriginalFov = false;
    }
}
