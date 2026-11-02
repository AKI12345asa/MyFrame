using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillChecker : MonoBehaviour,IHitTarget,IImpulseReciver
{
   [SerializeField] private Collider detectionCollider;
   public string checkerName;
   private Action<IHitTarget,Vector3,AttackDetectionEvent,string> _onDetection;
   private AttackDetectionEvent _currentAttackDetectionEvent;
   private Vector3 hitPoint;
   public Skill_Player Player { get; private set; }

   public void Init(Action<IHitTarget,Vector3,AttackDetectionEvent,string> onDetection,Skill_Player player)
   {
      detectionCollider.enabled = false;
      _onDetection = onDetection;
      Player = player;
   }

   public void StartDetection(AttackDetectionEvent attackDetectionEvent)
   {
      detectionCollider.enabled = true;
      _currentAttackDetectionEvent = attackDetectionEvent;
   }

   public void StopDetection()
   {
      detectionCollider.enabled = false;
      _currentAttackDetectionEvent = null;
   }

   private void OnTriggerStay(Collider other)
   {
      if (_currentAttackDetectionEvent == null) return;
      //判断是否在LayerMask范围内
      if (( _currentAttackDetectionEvent.CheckLayerMask & 1 << other.gameObject.layer) > 0)//<<或>>的移动运算符优先于& 和 | 运算符
      {
         var hitTarget = other.GetComponentInChildren<IHitTarget>();
         if (hitTarget != null)
         {
            hitPoint = other.ClosestPoint(transform.position);
            _onDetection?.Invoke(hitTarget, hitPoint,_currentAttackDetectionEvent,checkerName);
         }
      }  
   }
   public Transform ModelTransform => transform;
   public bool BeHit(Vector3 hitPoint, out HitDataBase hitData)
   {
      hitData = null;
      return false;
   }

   public void ReceiveImpulse(ImpulseData impulseData)
   {
      
   }

   public ImpulseData GetImpulseData(ImpulseDetectionEvent impulseEvent)
   {
      return new ImpulseData()
      {
         ImpulseDetectionEvent =  impulseEvent,
         ImpulseDirection = impulseEvent.CustomImpulseDirection,
         ImpulseForce = impulseEvent.ImpulseForce,
         ImpulseDuration = impulseEvent.ImpulseDuration
      };
   }
}
