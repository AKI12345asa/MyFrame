using System;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;

public abstract class SkillBehaviourBase:IDeepCopy<SkillBehaviourBase>
{
    protected SkillConfig SkillConfig;
    protected bool CanRotate = false;
    protected ICharacter Owner;
    protected SkillBrainBase SkillBrain;
    protected bool IsPlaying;
    protected Skill_Player SkillPlayer;
    protected Dictionary<AttackDetectionEvent,CheckData> DetectionEventHitTargets;
    [NonSerialized]public bool CanReleaseSelfInRelease = false;
    public abstract SkillBehaviourBase DeepCopy();
    public virtual void Init(ICharacter owner, SkillBrainBase skillBrain, Skill_Player skillPlayer)
    {
        Owner = owner;
        SkillBrain = skillBrain;
        SkillPlayer = skillPlayer;
        DetectionEventHitTargets = new();
    }

    public virtual void OnUpdate()
    {
        if (IsPlaying)
        {
            PlayingOnUpdate();
        }
    }

    public virtual void PlayingOnUpdate()
    {
        foreach (var checkData in DetectionEventHitTargets.Values)
        {
            checkData.Update();
        }
    }
    public virtual void Release()
    {
        DetectionEventHitTargets.Clear();
        IsPlaying = true;
        SkillBrain.SetCanReleaseFlag(false);
    }
    
    public virtual void OnClipEndOrReleaseNew()
    {
        IsPlaying = false;
        DetectionEventHitTargets.Clear();
    }
    
    public virtual void OnNextReleaseSkill(SkillConfig skillConfig)
    {
        if(skillConfig != SkillConfig)OnClipEndOrReleaseNew();
    }
    
    public virtual void OnSkillClipEnd()
    {
        OnClipEndOrReleaseNew();
        if (!IsPlaying)
        {
            Owner.OnSkillEnd();
        }
        SkillBrain.Exiting = true;
    }
    
    

    public virtual bool CanBeInterruptedByHurt(AttackData attackData)
    {
        return true;
    }
    public virtual void OnSkillBehaviourStopped()
    {
        IsPlaying = false;
        SkillBrain.SetCanReleaseFlag(true);
    }
    
    #region 技能驱动时的事件

    public virtual void OnTickSkill(int frameIndex) {}
    
    public virtual SkillCustomEvent BeforeSkillCustomEvent(SkillCustomEvent customEvent)
    {
        return customEvent;
    }
    
    public virtual SkillAnimationEvent BeforeSkillAnimationEvent(SkillAnimationEvent animationEvent,out bool autoPlayAnimation,out bool setWeaponHand)
    {
        autoPlayAnimation = true;
        setWeaponHand = true;
        return animationEvent;
    }

    public virtual SkillAudioEvent BeforeSkillAudioEvent(SkillAudioEvent audioEvent)
    {
        return audioEvent;
    }

    public virtual SkillEffectEvent BeforeSkillEffectEvent(SkillEffectEvent effectEvent)
    {
        return effectEvent;
    }
    
    public virtual AttackDetectionEvent BeforeAttackDetectionEvent(AttackDetectionEvent attackDetectionEvent)
    {
        return attackDetectionEvent;
    }
    
    public virtual void AfterSkillCustomEvent(SkillCustomEvent customEvent)
    {
        switch (customEvent.EventType)
        {
            case SkillEventType.UnFreezeSkillRelease:
                SkillBrain.SetCanReleaseFlag(true);
                break;
            case SkillEventType.CanRotate:
                break;
            case SkillEventType.CantRotate:
                break;
            case SkillEventType.EnemyFreezeSkillRelease:
                SkillBrain.SetCanReleaseFlag(false);
                break;
        }
    }

    public virtual void AfterSkillAnimationEvent(SkillAnimationEvent animationEvent) {}

    public virtual void AfterSkillAudioEvent(SkillAudioEvent audioEvent) {}

    public virtual void AfterSkillEffectEvent(SkillEffectEvent effectEvent) {}
    
    public virtual void AfterAttackDetectionEvent(AttackDetectionEvent attackDetectionEvent) {}
    
    public virtual bool OnDetection(IHitTarget hitTarget,Vector3 hitPoint,AttackDetectionEvent detectionEvent,string weaponName = null)
    {
        if (detectionEvent.AttackDetectionData is AttackWeaponDetectionData attackWeaponDetectionData)
        {
            if (attackWeaponDetectionData.WeaponName == weaponName)
            {
                if (DetectionEventHitTargets[detectionEvent].HitTargets.Add(hitTarget))
                {
                    Owner.OnAttack(hitTarget);
                    return OnHitTarget(hitTarget,hitPoint);
                }
            }
        }
        else
        { 
            if (DetectionEventHitTargets[detectionEvent].HitTargets.Add(hitTarget))
            { 
                Owner.OnAttack(hitTarget); 
                return OnHitTarget(hitTarget,hitPoint);
            }
        }
        return false;
    }
    
    public virtual bool OnHitTarget(IHitTarget hitTarget,Vector3 hitPoint)
    {
        if (!hitTarget.BeHit(hitPoint, out var data)) return false;
        SkillPlayer.InvokeHitTargetEvent(data);
        return true;
    }
    
    public virtual void OnRootMotion(Vector3 deltaPosition,Quaternion deltaRotation) {}
    
    #endregion

    #region 攻击检测事件管理

    public void AddActiveAttackDetectionEvent(AttackDetectionEvent attackDetectionEvent)
    {
        if (!DetectionEventHitTargets.ContainsKey(attackDetectionEvent))
        {
            DetectionEventHitTargets.Add(attackDetectionEvent,new CheckData(attackDetectionEvent.ComboCheckDeltaTime,attackDetectionEvent.DetectionCount));
        }
    }
    
    public void RemoveActiveAttackDetectionEvent(AttackDetectionEvent attackDetectionEvent)
    {
        DetectionEventHitTargets.Remove(attackDetectionEvent);
    }
    
    public void ClearActiveAttackDetectionEvents()
    {
        DetectionEventHitTargets.Clear();
    }

    #endregion

    protected class CheckData
    {
        private const float DefaultCheckDeltaTime = 0.1f;
        private const int DefaultCheckCount = 1;
        
        public HashSet<IHitTarget> HitTargets;
        public float CheckDeltaTime;
        public float CheckTimer;
        public int CheckCount;

        public CheckData(float checkDeltaTime,int checkCount)
        {
            CheckDeltaTime = checkDeltaTime <= 0 ? DefaultCheckDeltaTime : checkDeltaTime;
            CheckCount = checkCount <= 0? DefaultCheckCount : checkCount;
            CheckTimer = CheckDeltaTime;
            HitTargets = new HashSet<IHitTarget>();
            CheckCount -= 1;
        }

        public void Update()
        {
            CheckTimer -= Time.deltaTime;
            if (CheckTimer <= 0 && CheckCount >= 1)
            {
                HitTargets.Clear();
                CheckCount -= 1;
                CheckTimer = CheckDeltaTime;
            }
        }
    }
    public abstract void Destroy();
}
