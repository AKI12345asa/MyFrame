using System;
using System.Collections;
using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Skill/SkillClip",fileName = "SkillClip")]
public class SkillClip : FrameConfigBase
{
    [LabelText("技能名称")]public string SkillName;
    [LabelText("帧数上限")] public int FrameCount = 100;
    [LabelText("帧率")] public int FrameRate = 30;
    
    
    [NonSerialized,OdinSerialize]
    public SkillCustomEventData SkillCustomEventData = new();
    
    [NonSerialized,OdinSerialize]
    public SkillAnimationData SkillAnimationData = new  SkillAnimationData(); 
    
    [NonSerialized,OdinSerialize]
    public SkillAudioData SkillAudioData = new (); 
    
    [NonSerialized,OdinSerialize]
    public SkillEffectData SkillEffectData = new ();  
    
    [NonSerialized,OdinSerialize]
    public AttackDetectionData SkillAttackDetectionData = new();
    
    [NonSerialized,OdinSerialize]
    public CameraOrbitData CameraOrbitData = new();
    
    
    

#if UNITY_EDITOR
    private static Action _skillConfigValidate;

    public static void SetValidateAction(Action action)
    {
        _skillConfigValidate = action;
    }
    
    private void OnValidate()
    {
        _skillConfigValidate?.Invoke();
    }
#endif
}

