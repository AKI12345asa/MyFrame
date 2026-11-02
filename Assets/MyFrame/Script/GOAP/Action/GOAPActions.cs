using System.Collections.Generic;
using Sirenix.OdinInspector;

public class GOAPActions:IDeepCopy<GOAPActions>
{
    public List<GOAPActionBase> Actions = new();
    //该Dic中这些Value的行为效果可以满足GOAPStateType的要求
    public Dictionary<string,HashSet<GOAPActionBase>> ActionEffectDic = new();

    public void Init(GOAPAgent agent, IGOAPOwner owner)
    {
        ActionEffectDic.Clear();
        Actions.ForEach(action =>
        {
            action.Init(agent, owner);
            foreach (var effect in action.Effects)
            {
                AddActionEffect(effect.StateType,action);
            }
        });
        
    }

    private void AddActionEffect(GOAPStateType type, GOAPActionBase action)
    {
        if (!ActionEffectDic.TryGetValue(type, out var actions))
        {
            actions = new();
            ActionEffectDic.Add(type, actions);
        }
        actions.Add(action);
    }

#if UNITY_EDITOR
    [Button("检查所有状态类型")]
    public void CheckAllActionStateType()
    {
        foreach (GOAPActionBase item in Actions)
        {
            foreach (var pre in item.Precondition)
            {
                pre.CheckState();
            }

            foreach (var effect in item.Effects)
            {
                effect.CheckState();
            }
        }
    }
#endif
    public GOAPActions DeepCopy()
    {
        var copy = new GOAPActions();
        if (Actions == null) return copy;

        foreach (var action in Actions)
        {
            var clonedAction = action?.DeepCopy();
            if (clonedAction == null) continue;
            copy.Actions.Add(clonedAction);

            foreach (var effect in clonedAction.Effects)
            {
                if (!copy.ActionEffectDic.TryGetValue(effect.StateType, out var list))
                {
                    list = new HashSet<GOAPActionBase>();
                    copy.ActionEffectDic.Add(effect.StateType, list);
                }
                list.Add(clonedAction);
            }
        }

        return copy;
    }
}
