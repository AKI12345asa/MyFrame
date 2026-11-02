using System;
using MyFrame;
using UnityEngine;

public class EffectController : MonoBehaviour
{
        public float DestroyTime;
        private float DestroyTimer;
        
        public void Init()
        {
            var ps = gameObject.GetComponent<ParticleSystem>();
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play();
            
          DestroyTimer = DestroyTime;
        }

        private void Update()
        {
                DestroyTimer -= Time.deltaTime;
                if (DestroyTimer <= 0)
                {
                   PoolSystem.PushGameObject(gameObject);
                }
        }
}
