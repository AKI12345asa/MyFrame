using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;


public class GOAPActionBase:IDeepCopy<GOAPActionBase>
{
    [LabelText("行为名称")]public string ActionName;
    [LabelText("前提")] public List<GOAPTypeAndComparer> Precondition = new();
    [LabelText("效果")] public List<GOAPTypeAndComparer> Effects = new();
    [LabelText("代价值")] public float CostValue;
    [LabelText("效果值")] public float EffectValue;
    public GOAPActionUpdateType GoapActionUpdateType = GOAPActionUpdateType.BeforePlanning;
    public float Priority => EffectValue - CostValue;
    protected GOAPAgent Agent;

    public virtual void Init(GOAPAgent agent,IGOAPOwner owner)
    {
        Agent = agent;
        Precondition.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public virtual void Initialize(){}
    
    public virtual bool CheckPrecondition()
    {
        foreach (var precondition in Precondition)
        {
            if (!Agent.CheckStateForPreCondition(precondition.StateType, precondition.StateComparer))
            {
                return false;
            }
        }
        return true;
    }

    public virtual bool CheckForEffect()
    {
        foreach (var effect in Effects)
        {
            if (!Agent.CheckStateForEffect(effect.StateType, effect.StateComparer))
            {
                return false;
            }
        }
        return true;
    }
    
    public virtual GOAPRunState StartRun()
    {
        if (CheckForEffect())
        {
            return GOAPRunState.Success;
        }
        if (CheckPrecondition())
        {
            OnStart();
            return GOAPRunState.Running;
        }
        else
        {
            return GOAPRunState.Failed;
        }
    }
    
    public virtual void OnStart(){}

    public virtual GOAPRunState OnUpdate()
    {
        if (CheckForEffect())
        {
            return GOAPRunState.Success;
        }
        return default;
    }
    
    public virtual void OnStop(){}
    
    public virtual void OnDestroy(){}
    
    public virtual void ApplyEffect()
    {
        Effects.ForEach(effect =>
        {
            
            if (GOAPGlobal.Instance != null && GOAPGlobal.Instance.TryGetGlobalState(effect.StateType, out var state))
            {
               state.ApplyEffect(effect.StateComparer);
            }
            else
            {
                Agent.ApplyEffect(effect);
            }
        });
    }
    
    public virtual void AlwaysUpdateEvent()
    {
        
    }

    public virtual void BeforePlanningUpdateEvent(GOAPStateType targetStateType,GOAPStateComparer comparer)
    {
        
    }

    public virtual void BeIntoPlanUpdateEvent()
    {
        
    }
    
    public virtual void AfterActionEndUpdateEvent()
    {
        
    }

    public GOAPActionBase DeepCopy()
    {
        var copy = (GOAPActionBase)MemberwiseClone();
        copy.Agent = null;
        copy.Precondition = new List<GOAPTypeAndComparer>(Precondition?.Count ?? 0);
        copy.Effects = new List<GOAPTypeAndComparer>(Effects?.Count ?? 0);

        if (Precondition != null)
        {
            foreach (var item in Precondition)
            {
                copy.Precondition.Add(item?.DeepCopy());
            }
        }

        if (Effects != null)
        {
            foreach (var item in Effects)
            {
                copy.Effects.Add(item?.DeepCopy());
            }
        }

        return copy;
    }
}
[Flags]
public enum GOAPActionUpdateType
{
    Always = 1 << 0,
    BeforePlanning = 1 << 1,
    BeIntoPlan = 1 << 2,
    AfterActionEnd = 1 << 3,
}

public class GOAPTypeAndComparer:IDeepCopy<GOAPTypeAndComparer>
{
    [OnValueChanged("CheckState")]public GOAPStateType StateType;
    public GOAPStateComparer StateComparer;
    public float Priority;
#if UNITY_EDITOR
    public void CheckState()
    {
        if(GOAPEditorUtility.Global != null && GOAPEditorUtility.Global.TryGetGlobalState(StateType, out var globalState) && StateComparer == null &&
            StateComparer?.GetType() != globalState.GetComparerType())
        {
            StateComparer = globalState.GetComparer();
        }
        else if (GOAPEditorUtility.Agent != null && GOAPEditorUtility.Agent.States != null &&
                 GOAPEditorUtility.Agent.States.TryGetState(StateType, out GOAPStateBase agentState) &&StateComparer?.GetType() != agentState.GetComparerType())
        {
            StateComparer = agentState.GetComparer();
        }

        if (GOAPEditorUtility.Agent == null)
        {
            Debug.Log("NUll");
        }
    }
#endif
    public GOAPTypeAndComparer DeepCopy()
    {
        return new GOAPTypeAndComparer
        {
            StateType = StateType,
            StateComparer = StateComparer?.DeepCopy(),
        };
    }
}
