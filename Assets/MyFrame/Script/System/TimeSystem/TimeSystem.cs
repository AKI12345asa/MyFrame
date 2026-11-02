using System;
using System.Collections;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeSystem :SingletonMono<TimeSystem>
{
   private static Dictionary<string, Dictionary<ITimeOwner, TimeScaleContainer>> TimeDic;
   private static float WorldTimeScale { get;  set; }
   public static void Init()
   {
      TimeDic = new Dictionary<string, Dictionary<ITimeOwner, TimeScaleContainer>>();
      WorldTimeScale = 1;
   }

   public static void RegisterAsTimeOwner<T>(T owner,float localTimeScale = 1f,string scene = null) where T : ITimeOwner
   {
      if (string.IsNullOrEmpty(scene)) scene = SceneManager.GetActiveScene().name;
      if(!TimeDic.ContainsKey(scene)) TimeDic.Add(scene, new Dictionary<ITimeOwner, TimeScaleContainer>());
      if (TimeDic[scene].TryGetValue(owner, out var container))
      {
         container.UpdateTimeScale(localTimeScale);
         owner.WhenUpdateTimeScale(container.GetTimeScale());
         return;
      }
      container = new TimeScaleContainer(localTimeScale);
      TimeDic[scene].Add(owner, container);
      owner.timeScaleContainer = container;
      owner.WhenUpdateTimeScale(container.GetTimeScale());
   }

   public static void UnRegisterTimeOwner<T>(T owner, string scene = null) where T : ITimeOwner
   {
      if (string.IsNullOrEmpty(scene)) scene = SceneManager.GetActiveScene().name;
      if (!TimeDic.TryGetValue(scene,out var sceneDic)
          || !sceneDic.Remove(owner,out var container)) return;
      container.Abandon = true;
   }

   public static void SetWorldTimeScale(float scale)
   {
      if (scale < 0)
      {
         throw new Exception("当前设置的时间缩放小于0无法生效");
      }
      WorldTimeScale = scale;
      foreach (var sceneTimeOwners in TimeDic.Values)
      {
         foreach (var timeOwner in sceneTimeOwners)
         {
            timeOwner.Key.WhenUpdateTimeScale(timeOwner.Value.GetTimeScale());
         }
      }
   }

   public static void SetTimeScale<T>(T owner ,float localTimeScale,string scene = null) where T : ITimeOwner
   {
      if (owner == null) return;
      if (string.IsNullOrEmpty(scene)) scene = SceneManager.GetActiveScene().name;
      if (!TimeDic.TryGetValue(scene,out var sceneDic)
         || !sceneDic.TryGetValue(owner,out var container)) return;
      if (localTimeScale < 0)      
      {
         throw new Exception("当前设置的时间缩放小于0无法生效");
      }
      container.UpdateTimeScale(localTimeScale);
      owner.WhenUpdateTimeScale(container.GetTimeScale());
   }
   public class TimeScaleContainer
   {
      private float TimeScale {get; set;}
      public bool Abandon { get; set; }
      public bool ImpactByWorldScale { get;set; }
      public TimeScaleContainer(float timeScale)
      {
         TimeScale = timeScale;
         ImpactByWorldScale = true;
      }
      
      
      public void UpdateTimeScale(float timeScale)
      {
         TimeScale = timeScale;
      }

      public float GetTimeScale()
      {
         if (Abandon)
         {
            throw new Exception("当前时间缩放已被移除请停止使用");
         }
         return ImpactByWorldScale ? TimeScale * WorldTimeScale :  TimeScale;
      }

      public float GetDeltaTime()
      {
         return Time.deltaTime * GetTimeScale();
      }
   }
   
   #region 顿帧管理

   private static Dictionary<IFrameStutter,IEnumerator> ShutterSetterCoroutines = new();
   private static Dictionary<IFrameStutter,IEnumerator> ShutterGetterCoroutines = new();

   public static void ReceiveShutter(IFrameStutter stutter,StutterFrameEvent ev)
   {
      if (ShutterSetterCoroutines.ContainsKey(stutter))
      {
         Instance.StopCoroutine(ShutterSetterCoroutines[stutter]);
         ShutterSetterCoroutines.Remove(stutter);
      }
      var coroutine = stutter.GetShutter(ev.StutterFrameTimeScaleForTarget,ev.StutterFrameDurationForTarget);
      ShutterSetterCoroutines.Add(stutter,coroutine);
      Instance.StartCoroutine(coroutine);
   }

   public static void StutterSetterEnd(IFrameStutter stutter)
   {
      ShutterSetterCoroutines.Remove(stutter);
   }

   public static void StutterGetterEnd(IFrameStutter stutter)
   {
      ShutterGetterCoroutines.Remove(stutter);
   }
   public static void SetShutter(IFrameStutter stutter,StutterFrameEvent ev)
   {
      if (ShutterGetterCoroutines.ContainsKey(stutter))
      {
         Instance.StartCoroutine(ShutterGetterCoroutines[stutter]);
         ShutterGetterCoroutines.Remove(stutter);
      }
      var coroutine = stutter.SetShutter(ev.StutterFrameTimeScaleForSelf,ev.StutterFrameDurationForSelf);
      ShutterGetterCoroutines.Add(stutter,coroutine);
      Instance.StartCoroutine(coroutine);
   }

   #endregion
   
   
}
