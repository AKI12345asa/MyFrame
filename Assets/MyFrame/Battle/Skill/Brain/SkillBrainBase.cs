using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using MyFrame;
[RequireComponent(typeof(Skill_Player))]
public abstract class SkillBrainBase : MonoBehaviour
{
    [SerializeField]protected Skill_Player skillPlayer;
    [ShowInInspector]protected Dictionary<SkillConfig,SkillBehaviourBase> SkillBehaviourDic = new();
    protected ICharacter Owner;
    [NonSerialized]public bool Exiting;
    public SkillConfig LastUsedSkillConfig { get;protected set; }
    public bool ReleaseAgain => LastUsedSkillConfig != null && SkillBehaviourDic[LastUsedSkillConfig].CanReleaseSelfInRelease;
    public virtual bool CanRelease { get; protected set; }
    public int SkillCount => SkillBehaviourDic.Count;

    public virtual void Init(ICharacter owner)
    {
        CanRelease = true;
        Owner = owner;
        skillPlayer.Init(owner.AnimationController,owner.ModelTransform,Owner);
    }
    
    public virtual bool CheckIfExitSkillState()
    {
        return Owner.AnimationController.CheckIsTransitionFinished();
    }
    
    public virtual void SetCanReleaseFlag(bool newValue)
    {
        CanRelease = newValue;
    }
    
    public abstract void ReleaseSkill(SkillConfig playerSkillConfig);
    protected virtual void Update()
    {
        if (SkillBehaviourDic != null)
        {
            foreach (var behaviour in SkillBehaviourDic.Values)
            {
                behaviour.OnUpdate();
            }
        }
    }
    
    public abstract bool CanBeInterruptedByHurt(AttackData attackData);

 
    #region 共享数据
    //通过对于一个泛型类进行接口继承的包装使包含其的字典可以存储不同泛型类型的数据，在出现结构体时可以避免频繁的装箱拆箱同时也能保证类型安全，并且通过对象池减少GC压力
    //这种可以存储泛型类的字典可以用作不同脚本之间的通信
    protected interface ISkillConfigData{}

    protected class SkillShareData<T> : ISkillConfigData
    {
        public T Value;
    }
    
    protected Dictionary<string,ISkillConfigData> ShareDataDic = new();
    protected SkillShareData<T> GetSkillShareData<T>()
    {
        return ResSystem.GetOrNew<SkillShareData<T>>();
    }

    protected void DestroySkillShareData(ISkillConfigData data)
    {
        data.ObjectPushPool();
    }

    public void AddShareData<T>(string key, T value)
    {
        var skillShareData = GetSkillShareData<T>();
        skillShareData.Value = value;
        ShareDataDic.Add(key, skillShareData);
        if (SharedDataEventDataDic.TryGetValue(key, out var shareEventData))
        {
            ((SharedDataEventData<T>)shareEventData).TriggerCreate(value);
            ((SharedDataEventData<T>)shareEventData).TriggerChanged(value);
        }
    }

    public void AddOrUpdateShareData<T>(string key, T value)
    {
        if (ShareDataDic.TryGetValue(key, out var shareData))
        {
            ((SkillShareData<T>)shareData).Value = value;
            if (SharedDataEventDataDic.TryGetValue(key, out var shareEventData))
            {
                ((SharedDataEventData<T>)shareEventData).TriggerChanged(value);
            }
        }
        else
        {
           AddShareData(key, value);
        }
    }

    public bool ContainsShareData(string key)
    {
        return ShareDataDic.ContainsKey(key);
    }

    public bool TryGetShareData<T>(string key, out T value)
    {
        var res = ShareDataDic.TryGetValue(key, out ISkillConfigData data);
        value = res ? ((SkillShareData<T>)data).Value : default;
        return res;
    }

    public void RemoveShareData(string key)
    {
        if(ShareDataDic.Remove(key, out ISkillConfigData data))
        {
            if (SharedDataEventDataDic.TryGetValue(key, out var shareEventData))
            {
                shareEventData.TriggerRemove();
            }
            DestroySkillShareData(data);
        }
    }
    
    public void ClearShareData()
    {
        foreach (var data in ShareDataDic)
        {
            DestroySkillShareData(data.Value);
            if (SharedDataEventDataDic.TryGetValue(data.Key, out var shareEventData))
            {
               shareEventData.TriggerRemove();
            }
        }
        ShareDataDic.Clear();
    }
    #endregion

    #region 共享数据相关事件

    protected interface ISharedDataEventData
    {
        public abstract void TriggerRemove();
    }

    protected Dictionary<string, ISharedDataEventData> SharedDataEventDataDic = new();
    private class SharedDataEventData<T> : ISharedDataEventData
    {
        public Action<T> OnCreate;
        public Action<T> OnChanged;
        public Action OnRemove;

        public void TriggerCreate(T value)
        {
            OnCreate?.Invoke(value);
        }

        public void TriggerChanged(T value)
        {
            OnChanged?.Invoke(value);
        }

        public void TriggerRemove()
        {
            OnRemove?.Invoke();
        }
    }
    
    public void AddSharedDataCreateEventListener<T>(string key, Action<T> action)
    {
        if (!SharedDataEventDataDic.TryGetValue(key, out var sharedDataEventData))
        {
            var eventData = new SharedDataEventData<T>();
            SharedDataEventDataDic.Add(key, eventData);
            eventData.OnCreate += action;
        }
        else
        {
            ((SharedDataEventData<T>)sharedDataEventData).OnCreate += action;
        }
    }  
    public void RemoveSharedDataCreateEventListener<T>(string key, Action<T> action)
    {
        if (SharedDataEventDataDic.TryGetValue(key, out var sharedDataEventData))
        {
            ((SharedDataEventData<T>)sharedDataEventData).OnCreate -= action;
        }
    }
    public void AddSharedDataChangedEventListener<T>(string key, Action<T> action)
    {
        if (!SharedDataEventDataDic.TryGetValue(key, out var sharedDataEventData))
        {
            var eventData = new SharedDataEventData<T>();
            SharedDataEventDataDic.Add(key, eventData);
            eventData.OnChanged += action;
        }
        else
        {
            ((SharedDataEventData<T>)sharedDataEventData).OnChanged += action;
        }
    }
    public void RemoveSharedDataChangedEventListener<T>(string key, Action<T> action)
    {
        if (SharedDataEventDataDic.TryGetValue(key, out var sharedDataEventData))
        {
            ((SharedDataEventData<T>)sharedDataEventData).OnChanged -= action;
        }
    }
    public void AddSharedDataRemoveEventListener<T>(string key, Action action)
    {
        if (!SharedDataEventDataDic.TryGetValue(key, out var sharedDataEventData))
        {
            var eventData = new SharedDataEventData<T>();
            SharedDataEventDataDic.Add(key, eventData);
            eventData.OnRemove += action;
        }
        else
        {
            ((SharedDataEventData<T>)sharedDataEventData).OnRemove += action;
        }
    }

    public void RemoveSharedDataRemoveEventListener<T>(string key, Action action)
    {
        if (SharedDataEventDataDic.TryGetValue(key, out var sharedDataEventData))
        {
            ((SharedDataEventData<T>)sharedDataEventData).OnRemove -= action;
        }
    }
    #endregion

    
    public virtual void EndSkillState()
    {
        Exiting = false;
    }


    public virtual void Recycle()
    {
        foreach (var behaviour in SkillBehaviourDic.Values)
        {
            behaviour.Destroy();
        }
    }

    public void SetLastUsedSkillConfigToNull()
    {
        LastUsedSkillConfig = null;
    }
}
    
    
    
    
    