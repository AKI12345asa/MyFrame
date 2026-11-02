using UnityEngine;

public class StutterFrameProcessor:DetectionEventProcessor<StutterFrameEvent>
{
    public StutterFrameProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void ProcessEvent(StutterFrameEvent ev, Vector3 hitPoint, IHitTarget target,Skill_Player skillPlayer)
    {
        if (target is IFrameStutter receive)
        {
            TimeSystem.ReceiveShutter(receive,ev);
        }

        if (Owner is IFrameStutter setter)
        {
            TimeSystem.SetShutter(setter,ev);
        }
    }
}
