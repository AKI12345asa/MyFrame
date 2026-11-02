using UnityEngine;
[GOAPAction("计时")]
public class TimerAction : GOAPActionBase
{
        public float Timer;
        public float Time;

        public override void OnStart()
        {
                Timer = 0;
        }

        public override GOAPRunState OnUpdate()
        {
               Timer += UnityEngine.Time.deltaTime;
               if (Timer >= Time)
               {
                   ApplyEffect();
                   return GOAPRunState.Success;
               }
               else
               {
                   return base.OnUpdate();
               }
        }
}
