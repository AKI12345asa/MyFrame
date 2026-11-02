
using UnityEngine;

public class AttackEventProcessor:DetectionEventProcessor<AttackHitEvent>
{
    public AttackEventProcessor(ICharacter owner) : base(owner)
    {
    }

    protected override void ProcessEvent(AttackHitEvent ev,Vector3 hitPoint,IHitTarget target,Skill_Player skillPlayer)
    {
        var attackData = new AttackData
        {
            HitPoint = hitPoint,
            HitStrength = GetHitStrength(ev,target,skillPlayer),
            AttackValue = Owner.GetAttackValue(ev),
            Source = Owner,
            HitTime = ev.HitTime,
        };
        if (target is ICharacter character)
        {
            character.BeHurt(attackData);
        }
    }
    
    private Vector3 GetHitStrength(AttackHitEvent ev,IHitTarget target,Skill_Player skillPlayer)
    {
        var deltaDir = ev.IfRelatedToWeaponDirection ? target.ModelTransform.position - skillPlayer.CurrentWeapon.transform.position : target.ModelTransform.position - Owner.ModelTransform.position;
        var horizontalStrength = ev.HorizontalAttackStrength * new Vector3(deltaDir.x, 0, deltaDir.z).normalized;
        var verticalStrength = ev.VerticalAttackStrength * Vector3.up;
        return horizontalStrength + verticalStrength;
        
    }
}
