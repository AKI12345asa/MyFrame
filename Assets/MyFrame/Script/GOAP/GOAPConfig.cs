using System;
using MyFrame;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "GOAPConfig", menuName = "Config/GOAPConfig")]
public class GOAPConfig:FrameConfigBase
{
    [OdinSerialize]public GOAPGoals Goals = new GOAPGoals();
    [OdinSerialize] public GOAPStates States = new();
    [OdinSerialize] public GOAPActions Actions = new();
    [OdinSerialize] public GOAPPlan Plan = new();

#if UNITY_EDITOR
    private static Action _goapConfigValidate;

    public static void RegisterValidateAction(Action action)
    {
        _goapConfigValidate = action;
    }
    
    private void OnValidate()
    {
        _goapConfigValidate?.Invoke();
    }

#endif
}
