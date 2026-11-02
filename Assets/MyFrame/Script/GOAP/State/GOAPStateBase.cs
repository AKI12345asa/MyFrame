using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 用于标记某个AI实体的某个状态
/// </summary>
public abstract class GOAPStateBase:IDeepCopy<GOAPStateBase>
{
    public abstract bool EqualsValue(GOAPStateBase other);
    public abstract void SetValue(GOAPStateBase other);
    
    public abstract Type GetComparerType();
    public abstract GOAPStateBase Copy();
    public abstract GOAPStateComparer GetComparer();
    public abstract bool CompareWithPreCondition(GOAPStateComparer comparer);
    public abstract bool CompareWithEffect(GOAPStateComparer comparer);
    public abstract void ApplyEffect(GOAPStateComparer comparer);
    public GOAPStateBase DeepCopy()
    {
        return Copy();
    }
    
}

public enum GOAPStateBaseType
{
    Int,
    Float,
    Bool,
    Object,
}
public abstract class GOAPStateBase<T, V, C> : GOAPStateBase where T : GOAPStateBase<T, V, C>, new() where C : GOAPStateComparer<T, C>, new()
{
    public V Value;
    public abstract bool EqualsValue(T other);
    
    
    public override bool EqualsValue(GOAPStateBase other)
    {
       return EqualsValue((T)other);
    }

    public virtual void SetValue(T other)
    {
        Value = other.Value;
    }

    public override void SetValue(GOAPStateBase other)
    {
        SetValue((T)other);
    }
    
    public virtual void SetValue(V value)
    {
        Value = value;
    }

    public override GOAPStateBase Copy()
    {
        return new T(){Value = Value};
    }

    public virtual C GetStateComparer()
    {
        return new C();
    }
    public override GOAPStateComparer GetComparer()
    {
        return GetStateComparer();
    }

    public abstract bool CompareWithPreCondition(C comparer);
    
    public override bool CompareWithPreCondition(GOAPStateComparer comparer)
    {
        return CompareWithPreCondition((C)comparer);
    }
    public abstract bool CompareWithEffect(C comparer);
    
    public override bool CompareWithEffect(GOAPStateComparer comparer)
    {
        return CompareWithEffect((C)comparer);
    }
    
    public abstract void ApplyEffect(C comparer);

    public override void ApplyEffect(GOAPStateComparer comparer)
    {
       ApplyEffect((C)comparer);
    }

    public override Type GetComparerType()
    {
        return typeof(C);
    }
}
