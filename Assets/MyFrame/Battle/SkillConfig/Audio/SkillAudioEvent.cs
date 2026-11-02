using UnityEngine;

public class SkillAudioEvent
{
#if UNITY_EDITOR
    public string TrackName = "音效轨道";
#endif
    public int FrameIndex = -1;
    public int PlayCount;
    public AudioClip AudioClip;
    public float Volume = 1;
}
