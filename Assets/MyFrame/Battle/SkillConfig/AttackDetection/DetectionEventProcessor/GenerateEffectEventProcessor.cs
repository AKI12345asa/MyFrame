using MyFrame;
using UnityEngine;

public class GenerateEffectEventProcessor:DetectionEventProcessor<GenerateEffectEvent>
{
    public GenerateEffectEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void ProcessEvent(GenerateEffectEvent ev, Vector3 hitPoint, IHitTarget target,Skill_Player skillPlayer)
    {
        if (ev.EffectPrefab != null)
        {
            var effect = PoolSystem.GetGameObject(ev.EffectPrefab);
            effect.transform.position = hitPoint;
            effect.transform.LookAt(Camera.main?.transform);
            effect.GetComponent<EffectController>().Init();
        }
    }
}
