using System;
using MyFrame;
using UnityEngine;

public interface ICharacter : IHitTarget,IStateMachineOwner,ITimeOwner
{
    Animation_Controller AnimationController { get; }
    
    public float GetAttackValue(AttackHitEvent hitConfig);
    
    public void OnSkillRotate();

    public void BeHurt(AttackData attackData);
    
    public void OnSkillEnd();
    
    public void OnSkillMove(Vector3 deltaPosition);
    
    public void OnSkillRotate(Quaternion deltaRotation);
    /// <summary>
    /// 参数：角色自身
    /// </summary>
    public event Action<ICharacter> UpdateEvent;
    /// <summary>
    /// 参数1：攻击者即自身，参数2：受击者
    /// </summary>
    public event Action<ICharacter, IHitTarget> OnAttackEvent;
    /// <summary>
    /// 参数1：受击者即自身，参数2：攻击者
    /// </summary>
    public event Action<ICharacter,ICharacter> HurtEvent;

    public event Action<ICharacter> DieEvent;

    public void OnAttack(IHitTarget target);
}
