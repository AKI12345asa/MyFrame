using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

public class GOAPAgent:SerializedMonoBehaviour
{
  public GOAPConfig GOAPConfig;
    public GOAPGoals Goals
    {
        get
        {
            if (Application.isPlaying)
            {
                return _goals;
            }
            return GOAPConfig != null ? GOAPConfig.Goals : _goals;
        }
    }

    public GOAPStates States
    {
        get
        {
            if (Application.isPlaying)
            {
                return _states;
            }
            return GOAPConfig != null ? GOAPConfig.States : _states;
        }
    }

    public GOAPActions Actions
    {
        get
        {
            if (Application.isPlaying)
            {
                return _actions;
            }
            return GOAPConfig != null ? GOAPConfig.Actions : _actions;
        }
    }
    public GOAPPlan Plan;
    private List<Action> _alwaysUpdateActions = new ();
    [SerializeField]private bool isStopped = false;
    private GOAPStates _states;
    private GOAPActions _actions;
    private GOAPGoals _goals;
    public IGOAPOwner Owner { get; private set; }
    public void Init(IGOAPOwner owner)
    {
        DeepCopyFromConfig();
        Owner = owner;
        _alwaysUpdateActions.Clear();
        /*Actions.Init(this,Owner);
        Goals.Init(this,owner);*/
        Plan = new GOAPPlan();
        foreach (var action in Actions.Actions)
        {
            if((action.GoapActionUpdateType & GOAPActionUpdateType.Always) != 0) _alwaysUpdateActions.Add(() => action.AlwaysUpdateEvent());
        }
        PoolSystem.InitObjectPool<GOAPPlanNode>(typeof(GOAPPlanNode).FullName,getAction:node => node.OnDestroy());
    }
    
    private void DeepCopyFromConfig()
    {
        if (GOAPConfig == null)
        {
            _states = new GOAPStates();
            _actions = new GOAPActions();
            _goals = new GOAPGoals();
            return;
        }

        _states = GOAPConfig.States?.DeepCopy() ?? new GOAPStates();
        _actions = GOAPConfig.Actions?.DeepCopy() ?? new GOAPActions();
        _goals = GOAPConfig.Goals?.DeepCopy() ?? new GOAPGoals();
    }

#if UNITY_EDITOR
    [Button("调试_校验深拷贝")]
    private void ValidateDeepCopy()
    {
        if (GOAPConfig == null)
        {
            Debug.LogWarning("[GOAP] 未设置 GOAPConfig，无法校验深拷贝");
            return;
        }

        DeepCopyFromConfig();
        bool isValid = true;

        if (ReferenceEquals(_states, GOAPConfig.States))
        {
            Debug.LogError("[GOAP] States 仍引用配置对象");
            isValid = false;
        }

        if (ReferenceEquals(_actions, GOAPConfig.Actions))
        {
            Debug.LogError("[GOAP] Actions 仍引用配置对象");
            isValid = false;
        }

        if (ReferenceEquals(_goals, GOAPConfig.Goals))
        {
            Debug.LogError("[GOAP] Goals 仍引用配置对象");
            isValid = false;
        }

        if (GOAPConfig.States?.StateDic != null)
        {
            foreach (var kv in GOAPConfig.States.StateDic)
            {
                if (!_states.StateDic.TryGetValue(kv.Key, out var runtimeState))
                {
                    Debug.LogError($"[GOAP] 运行时缺少状态: {kv.Key}");
                    isValid = false;
                    continue;
                }

                if (ReferenceEquals(runtimeState, kv.Value))
                {
                    Debug.LogError($"[GOAP] 状态未拷贝: {kv.Key}");
                    isValid = false;
                }
            }
        }

        if (GOAPConfig.Actions?.Actions != null)
        {
            for (int i = 0; i < GOAPConfig.Actions.Actions.Count; i++)
            {
                var configAction = GOAPConfig.Actions.Actions[i];
                if (i >= _actions.Actions.Count)
                {
                    Debug.LogError($"[GOAP] 运行时缺少行为索引: {i}");
                    isValid = false;
                    continue;
                }

                var runtimeAction = _actions.Actions[i];
                if (ReferenceEquals(runtimeAction, configAction))
                {
                    Debug.LogError($"[GOAP] 行为未拷贝: {configAction?.ActionName}");
                    isValid = false;
                }

                if (runtimeAction?.Precondition != null && configAction?.Precondition != null)
                {
                    int count = Mathf.Min(runtimeAction.Precondition.Count, configAction.Precondition.Count);
                    for (int j = 0; j < count; j++)
                    {
                        if (ReferenceEquals(runtimeAction.Precondition[j], configAction.Precondition[j]))
                        {
                            Debug.LogError($"[GOAP] 行为前提未拷贝: {configAction.ActionName} / {j}");
                            isValid = false;
                        }

                        if (ReferenceEquals(runtimeAction.Precondition[j]?.StateComparer, configAction.Precondition[j]?.StateComparer))
                        {
                            Debug.LogError($"[GOAP] 前提比较器未拷贝: {configAction.ActionName} / {j}");
                            isValid = false;
                        }
                    }
                }

                if (runtimeAction?.Effects != null && configAction?.Effects != null)
                {
                    int count = Mathf.Min(runtimeAction.Effects.Count, configAction.Effects.Count);
                    for (int j = 0; j < count; j++)
                    {
                        if (ReferenceEquals(runtimeAction.Effects[j], configAction.Effects[j]))
                        {
                            Debug.LogError($"[GOAP] 行为效果未拷贝: {configAction.ActionName} / {j}");
                            isValid = false;
                        }

                        if (ReferenceEquals(runtimeAction.Effects[j]?.StateComparer, configAction.Effects[j]?.StateComparer))
                        {
                            Debug.LogError($"[GOAP] 效果比较器未拷贝: {configAction.ActionName} / {j}");
                            isValid = false;
                        }
                    }
                }
            }
        }

        if (GOAPConfig.Goals?.Dic != null)
        {
            foreach (var kv in GOAPConfig.Goals.Dic)
            {
                if (!_goals.Dic.TryGetValue(kv.Key, out var runtimeGoal))
                {
                    Debug.LogError($"[GOAP] 运行时缺少目标: {kv.Key}");
                    isValid = false;
                    continue;
                }

                if (ReferenceEquals(runtimeGoal, kv.Value))
                {
                    Debug.LogError($"[GOAP] 目标未拷贝: {kv.Key}");
                    isValid = false;
                }

                if (ReferenceEquals(runtimeGoal.TargetValue, kv.Value.TargetValue))
                {
                    Debug.LogError($"[GOAP] 目标比较器未拷贝: {kv.Key}");
                    isValid = false;
                }

                if (kv.Value.Checker != null && ReferenceEquals(runtimeGoal.Checker, kv.Value.Checker))
                {
                    Debug.LogWarning($"[GOAP] 目标检查器仍为共享引用: {kv.Key}，建议为 Checker 提供无参构造以便按类型创建副本");
                }
            }
        }

        if (isValid)
        {
            Debug.Log("[GOAP] 深拷贝校验通过：运行时对象已与配置对象解耦");
        }
    }
#endif
    
    public void OnUpdate()
    {
        if(Owner == null || isStopped) return;
        var sortedGoals = Goals.UpdateGoals();//获取按照优先级排序后的目标列表
        if ( Plan == null || !Plan.Running)
        {
            foreach (var item in sortedGoals)
            {
                //优先级不是负数且可以基于这个目标生成一个计划
                if (item.Value.Priority > 0 && GeneratePlan(item.Key,out var targetNode))
                {   
                    /*Debug.Log("构建任务:" + item.Key);*/
                    RunPlan(item.Key,targetNode);
                    break;
                }
            }
        }
        else
        {
            //如果当前目标可以被中断那么检测以下优先级更高的目标是否具有打断功能
            var currentGoal = Goals.Dic[Plan.GoalName];
            if (currentGoal != null && currentGoal.CanBeInterrupted)
            {
                foreach (var item in sortedGoals)
                {
                    if (item.Key == Plan.GoalName || !item.Value.CanInterrupt || !(item.Value.Priority > currentGoal.Priority)) continue;
                    if(!GeneratePlan(item.Key, out var targetNode)) continue;
                    StopPlan();
                    RunPlan(item.Key, targetNode);
                    break;
                }
            }
            Plan.OnUpdate();
        }
        _alwaysUpdateActions.ForEach(a => a.Invoke());
    }

    public void OnDestroy()
    {
        Plan?.OnDestroy();
        
    }
    
    
    #region 状态检测

    public void ApplyEffect(GOAPTypeAndComparer effect)
    {
        States.ApplyEffect(effect);
    }

    public bool CheckStateForPreCondition(GOAPStateType preconditionStateType, GOAPStateComparer preconditionStateComparer)
    {
        //先检查全局状态
        if (GOAPGlobal.Instance && GOAPGlobal.Instance.TryGetGlobalState(preconditionStateType,out var globalState))
        {
            return globalState.CompareWithPreCondition(preconditionStateComparer);
        }
        else
        {
            return States.CheckStateForPreCondition(preconditionStateType, preconditionStateComparer);
        }
    }

    public bool CheckStateForEffect(GOAPStateType preconditionStateType, GOAPStateComparer preconditionStateComparer)
    {
        //先检查全局状态
        if (GOAPGlobal.Instance && GOAPGlobal.Instance.TryGetGlobalState(preconditionStateType,out var globalState))
        {
            return globalState.CompareWithEffect(preconditionStateComparer);
        }
        else
        {
            return States.CheckStateForEffect(preconditionStateType, preconditionStateComparer);
        }
    }
    
    private bool CheckActionPrecondition(GOAPActionBase action,out List<GOAPTypeAndComparer> pending)
    {
        pending = new List<GOAPTypeAndComparer>();
        var result = true;
        foreach (var precondition in action.Precondition)
        {
            if (!CheckStateForPreCondition(precondition.StateType, precondition.StateComparer))
            {
                result = false;
                pending.Add(precondition);
            }
        }
        return result;
    }

    #endregion
    
    #region 生成计划


    private GOAPActionBase GetMinCostActionByTargetEffect(GOAPStateType targetStateType, GOAPStateComparer comparer)
    {
        GOAPActionBase result = null;
        if (Actions.ActionEffectDic.TryGetValue(targetStateType, out var actionList))
        {
            actionList.ForEach(action =>
            {
                foreach (var effect in action.Effects)
                {
                    if (effect.StateType == targetStateType)
                    {
                        if (effect.StateComparer.MatchWithComparer(comparer))
                        {
                            if(result == null || result.CostValue > action.CostValue) result = action;
                            break;
                        }
                    }
                }
            });
        }
        return result;
    }

    public bool CheckEffectForAction(GOAPActionBase action,GOAPStateType targetStateType, GOAPStateComparer comparer)
    {
        foreach (var effect in action.Effects)
        {
            if (effect.StateType == targetStateType)
            {
                if (effect.StateComparer.MatchWithComparer(comparer))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// 构造计划节点树
    /// 失败的可能性:某个环节中无法达成某个前置条件
    /// </summary>
    /// <returns></returns>
    private bool TryBuiltPlanPath(GOAPActionBase startAction,out GOAPPlanNode targetNode)
    {
        targetNode = ResSystem.GetOrNew<GOAPPlanNode>(); 
        targetNode.Action = startAction;
        CheckActionPrecondition(startAction, out targetNode.Pending);
        //遍历所有条件,全部满足才能构建成功
        foreach(var pend in targetNode.Pending)
        {
           var childNode = GetPlanNode(pend, 0);
           if (childNode == null)
           {
               RecycleNode(targetNode);
               targetNode = null;
               return false;
           }
           childNode.Parent = targetNode;
           childNode.Priority = pend.Priority;
           targetNode.Preconditions.Add(childNode);
        }

        return true;
    }
    
    private const int MaxDepth = 10;

    private GOAPPlanNode GetPlanNode(GOAPTypeAndComparer tc, int depth)
    {
        if (depth > MaxDepth) return null;
        var actions = Actions.ActionEffectDic.GetValueOrDefault(tc.StateType);
        if(actions == null || actions.Count == 0) return null;
        var candidateNodes = ResSystem.GetOrNew<List<GOAPPlanNode>>();
        candidateNodes.Clear();
        foreach (var action in actions)
        {
            if (!CheckEffectForAction(action, tc.StateType, tc.StateComparer)) continue;
            var node = ResSystem.GetOrNew<GOAPPlanNode>();
            node.Action = action;
            node.H = GetActionMinCost(action);
            CheckActionPrecondition(action, out node.Pending);
            candidateNodes.Add(node);
        }
        candidateNodes.Sort((a,b)=> a.H.CompareTo(b.H));
        GOAPPlanNode result = null;
        var i = 0;
        for(;i <= candidateNodes.Count - 1; i++)
        {
            var canUse = true;
            var node = candidateNodes[i];
            foreach (var pending in node.Pending)
            {
                var childNode = GetPlanNode(pending, depth + 1);
                if (childNode == null) 
                {
                    canUse = false;
                    break;
                }
                childNode.Parent = node;
                childNode.Priority = pending.Priority;
                node.Preconditions.Add(childNode);
            }

            if (canUse)
            {
                node.G = node.Action.CostValue;
                foreach(var child in node.Preconditions)
                {
                    node.G += child.G;
                }
                result = node;
                candidateNodes.Remove(node);
                break;
            }
            RecycleNode(node);
        }
        for(;i <= candidateNodes.Count - 1; i++)
        {
            RecycleNode(candidateNodes[i]);
        }
        candidateNodes.ObjectPushPool();
        return result;
    }

    private void RecycleNode(GOAPPlanNode node)
    {
        node.OnDestroy();
        node.ObjectPushPool();
    }
    
    private bool GeneratePlan(string goalName,out GOAPPlanNode targetNode)
    {
        /*Debug.Log("正在尝试构建计划" + goalName);*/
        var success = false;
        var goal = Goals.Dic[goalName];
        var targetType = goal.TargetState;
        targetNode = null;
        //遍历所有效果,全部满足则不再执行
        if (CheckStateForEffect(targetType, goal.TargetValue))
        {
            return false;
        }
        Actions.Actions.ForEach(action =>
        {
            if ((action.GoapActionUpdateType & GOAPActionUpdateType.BeforePlanning) != 0)
            {
                action.BeforePlanningUpdateEvent(targetType, goal.TargetValue);
            }
        });
        Actions.ActionEffectDic.TryGetValue(targetType, out var actions);
        if (actions == null || actions.Count == 0)
        {
            return false;
        }
        foreach (var action in actions)
        {
            if(TryBuiltPlanPath(action,out targetNode))
            {
                targetNode.Parent = null;
                success = true;
                TriggerBeIntoPlanUpdateEvent(targetNode);
                break;
            }
        }
        if(targetNode == null)
        {
            Debug.Log($"构建{goalName}计划失败");
        }
        return success;
    }

    private void TriggerBeIntoPlanUpdateEvent(GOAPPlanNode planNode)
    {
        if ((planNode.Action.GoapActionUpdateType & GOAPActionUpdateType.BeIntoPlan) != 0)
        {
            planNode.Action.BeIntoPlanUpdateEvent();
        }
        foreach (var child in planNode.Preconditions)
        {
            TriggerBeIntoPlanUpdateEvent(child);
        }
    }

    /// <summary>
    /// 这里的MinCost是A*算法中启发式函数的最小值,是预估从该节点开始往后走的最小代价
    /// </summary>
    /// <returns></returns>
    private float GetActionMinCost(GOAPActionBase action)
    {
        float h = 0f;
        foreach (var p in action.Precondition)
        {
            if (CheckStateForPreCondition(p.StateType, p.StateComparer)) continue;
            var best = GetMinCostActionByTargetEffect(p.StateType, p.StateComparer);
            if (best == null) return float.MaxValue;   // 无解 → 这个候选排最后
            h += best.CostValue;
        }
        return h;
    }
    
    #endregion

    #region 执行任务

    private void RunPlan(string gaolName, GOAPPlanNode goalNode)
    {
        Plan.StartRun(gaolName,goalNode);
    }

    private void StopPlan()
    {
        if(!isStopped)Plan.Stop();
    }
    #endregion

    public void Stop()
    {
        StopPlan();
        isStopped = true;
    }

    public void Start()
    {
        isStopped = false;
        Debug.Log("Start");
    }

    public void Recycle()
    {
        Stop();
    }

    public void ReStart()
    {
        Start();
    }
}

