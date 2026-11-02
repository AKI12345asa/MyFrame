using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class GOAPStates: IDeepCopy<GOAPStates>
{
    public Dictionary<string, GOAPStateBase> StateDic = new();
    
    public bool TryAddState(GOAPStateType type, GOAPStateBase state)
    {
        return StateDic.TryAdd(type, state);
    }

    public bool TryRemove(GOAPStateType type)
    {
        return StateDic.Remove(type);
    }

    public T GetState<T>(GOAPStateType type) where T : GOAPStateBase
    {
       return StateDic[type] as T;
    }

    public bool TryGetState(GOAPStateType type, out GOAPStateBase state)
    {
        state = default;
        if (StateDic == null) return false;
        return StateDic.TryGetValue(type, out state);
    }
    
    public bool TryGetState<T>(GOAPStateType type, out T state) where T : GOAPStateBase
    {
        state = default;
        if (StateDic == null) return false;
        
        if (StateDic.TryGetValue(type, out var baseState))
        {
            state = baseState as T;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 检查某个状态作为前提时是否满足要求
    /// </summary>
    public bool CheckStateForPreCondition(GOAPStateType type, GOAPStateComparer comparer)
    {
        if(TryGetState(type,out GOAPStateBase state))
        {
            return state.CompareWithPreCondition(comparer);
        }

        return false;
    }
    /// <summary>
    /// 检查某个状态作为效果时是否满足要求
    /// </summary>
    public bool CheckStateForEffect(GOAPStateType type, GOAPStateComparer comparer)
    {
        if(TryGetState(type,out GOAPStateBase state))
        {
            return state.CompareWithEffect(comparer);
        }

        return false;
    }
    public void ApplyEffect(GOAPTypeAndComparer effect)
    {
        if (StateDic.TryGetValue(effect.StateType, out var state))
        {
            state.ApplyEffect(effect.StateComparer);
        }
    }

    public GOAPStates DeepCopy()
    {
        var copy = new GOAPStates();
        if (StateDic == null) return copy;

        foreach (var kv in StateDic)
        {
            copy.StateDic[kv.Key] = kv.Value?.DeepCopy();
        }

        return copy;
    }
}
