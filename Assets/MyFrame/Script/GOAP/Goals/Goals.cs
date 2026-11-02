using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class GOAPGoals:IDeepCopy<GOAPGoals>
{
    public class Goal
    {
        [LabelText("目标状态"),OnValueChanged("CheckState")] public GOAPStateType TargetState;
        [LabelText("预期效果")] public GOAPStateComparer TargetValue;
        [LabelText("优先级系数"),HorizontalGroup("1")] public float PriorityMultiply;
        [LabelText("实时优先级"),HorizontalGroup("1")] public float RuntimePriority;
        [LabelText("是否能打断其他目标")] public bool CanInterrupt;
        [LabelText("是否可被中断")] public bool CanBeInterrupted;
        [LabelText("最终优先级"),ShowInInspector,HorizontalGroup("1")]public float Priority => PriorityMultiply * RuntimePriority;
        [LabelText("目标检查器")] public IGOAPGoalChecker Checker;
#if UNITY_EDITOR
        public void CheckState()
        {
            if (GOAPEditorUtility.Global != null && GOAPEditorUtility.Global.TryGetGlobalState(TargetState, out var globalState) &&
                (TargetValue?.GetType() != globalState.GetComparer().GetType()))
            {
                TargetValue = globalState.GetComparer();
            }
            else if (GOAPEditorUtility.Agent != null && GOAPEditorUtility.Agent.States != null &&
                     GOAPEditorUtility.Agent.States.TryGetState(TargetState, out GOAPStateBase agentState) &&(TargetValue?.GetType() != agentState.GetComparer().GetType()))
            {
                TargetValue = agentState.GetComparer();
            }
        }    
#endif

        public Goal DeepCopy()
        {
            return new Goal
            {
                TargetState = TargetState,
                TargetValue = TargetValue?.DeepCopy(),
                PriorityMultiply = PriorityMultiply,
                RuntimePriority = RuntimePriority,
                CanInterrupt = CanInterrupt,
                CanBeInterrupted = CanBeInterrupted,
                Checker = CloneChecker(Checker),
            };
        }

        private static IGOAPGoalChecker CloneChecker(IGOAPGoalChecker checker)
        {
            if (checker == null) return null;
            var checkerType = checker.GetType();
            // Checker 不持久化运行态数据，这里仅按类型创建新实例
            if (checkerType.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(checkerType) as IGOAPGoalChecker;
            }

            Debug.LogWarning($"[GOAP] Checker {checkerType.Name} 缺少无参构造，无法按类型创建副本");
            return null;
        }
    }

    private class SortedGoalComparer : IComparer<string>
    {
        private Dictionary<string, Goal> _dic = new();
        public SortedGoalComparer(Dictionary<string, Goal> dic)
        {
            _dic = dic;
        }
        
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            var com = _dic[y].Priority.CompareTo(_dic[x].Priority);
            if (com == 0) return -1; //避免同优先级被去重
            return com;
        }
    }
    
    [OdinSerialize]public Dictionary<string, Goal> Dic = new();
    public SortedList<string,Goal> SortedList;

    private GOAPAgent _agent;
    private IGOAPOwner _owner;
    public void Init(GOAPAgent agent, IGOAPOwner owner)
    {
        _agent = agent;
        _owner = owner;
        SortedList = new SortedList<string, Goal>(Dic.Count,new SortedGoalComparer(Dic));
    }

    public SortedList<string,Goal> UpdateGoals()
    {
        if (Dic == null) return null;
        SortedList.Clear();
        foreach (var item in Dic)
        {
            if (item.Value.Checker != null) 
            {
                item.Value.Checker.Update(item.Value,_agent,_owner);
            }
            SortedList.Add(item.Key, item.Value);
            
        }
        return SortedList;
    }
    
#if UNITY_EDITOR
    [Button("目标状态校验")]
    public void CheckGoalsTargetValueType()
    {
        foreach (var item in Dic)
        {
            item.Value.CheckState();
        }
    }
#endif
    public GOAPGoals DeepCopy()
    {
        var copy = new GOAPGoals();
        if (Dic == null) return copy;

        foreach (var kv in Dic)
        {
            copy.Dic[kv.Key] = kv.Value?.DeepCopy();
        }

        return copy;
    }
}


