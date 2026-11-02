using UnityEngine;

public class SkillAnimationEvent : SkillFrameEventBase
{
    public AnimationClip AnimationClip;
    public float AnimationSpeed = 1f;
    public bool ApplyRootMotion;
    public bool MainWeaponOnLeftHand;
    public float TransitionTime;
    public float GetAnimationSpeed() => AnimationSpeed > 0f ? AnimationSpeed : 1f;
#if UNITY_EDITOR
    public int DurationFrame;
#endif
}