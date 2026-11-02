 using System;
 using UnityEngine;

 public interface IHitTarget 
 {
     public Transform ModelTransform { get; }
     public bool BeHit(Vector3 hitPoint,out HitDataBase hitData);
 }

 
 
