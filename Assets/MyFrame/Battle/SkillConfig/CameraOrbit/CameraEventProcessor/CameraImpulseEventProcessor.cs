using UnityEngine;

public class CameraImpulseEventProcessor : CameraEventProcessor<CameraImpulseEvent>
{
    public CameraImpulseEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void StartEvent(CameraImpulseEvent ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        var impulseSource = camera.ImpulseSource;
        if (impulseSource == null || camera.ImpulseListener == null)
        {
            Debug.LogWarning("SkillCamera requires both CinemachineImpulseSource and CinemachineImpulseListener to play camera impulse.");
            return;
        }

        impulseSource.m_ImpulseDefinition.m_ImpulseShape = ev.ImpulseShapes;
        impulseSource.m_ImpulseDefinition.m_ImpulseDuration = time.Duration;
        impulseSource.m_ImpulseDefinition.m_CustomImpulseShape = ev.CustomImpulseCurve;
        impulseSource.m_DefaultVelocity = ev.CustomImpulseDirection;
        impulseSource.GenerateImpulseWithForce(ev.ImpulseForce);
    }
}
