using UnityEngine;

public class ImpulseEventProcessor:DetectionEventProcessor<ImpulseDetectionEvent>
{
    public ImpulseEventProcessor(ICharacter owner) : base(owner)
    {
        
    }

    protected override void ProcessEvent(ImpulseDetectionEvent ev,Vector3 hitPoint, IHitTarget target,Skill_Player skillPlayer)
    {
        if (target is IImpulseReciver impulseReceiver)
        {
            ImpulseData impulseData = impulseReceiver.GetImpulseData(ev);
            impulseReceiver.ReceiveImpulse(impulseData);
            if (Owner is IImpulseSetter impulseSetter)
            {
                impulseSetter.SetImpulse(impulseData);
            }
        }
    }
}
