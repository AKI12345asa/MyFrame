using UnityEngine;

public abstract class DetectionEventProcessor
{
     public abstract void ProcessEvent(DetectionTriggerEventBase ev,Vector3 hitPoint,IHitTarget target,Skill_Player skillPlayer);   
}

public abstract class DetectionEventProcessor<T> : DetectionEventProcessor where T : DetectionTriggerEventBase
{
    protected readonly ICharacter Owner;

    protected DetectionEventProcessor(ICharacter owner)
    {
        Owner = owner;
    }
    
    
    public override void ProcessEvent(DetectionTriggerEventBase ev,Vector3 hitPoint,IHitTarget target,Skill_Player skillPlayer)
    {
        if (ev is T t)
        {
            ProcessEvent(t, hitPoint, target, skillPlayer);
        }
    }
    protected abstract void ProcessEvent(T ev, Vector3 hitPoint, IHitTarget target,Skill_Player skillPlayer);
}


