using UnityEngine;

/// <summary>
/// 提交给相机事件处理器的时间信息，全部为实际时间（秒）而非帧数
/// </summary>
public readonly struct CameraEventTime
{
    /// <summary>事件开始的时间点</summary>
    public readonly float StartTime;
    /// <summary>事件持续时间</summary>
    public readonly float Duration;
    /// <summary>技能当前播放到的时间点</summary>
    public readonly float PlaybackTime;

    public float EndTime => StartTime + Duration;
    /// <summary>事件已经进行的时间</summary>
    public float ElapsedTime => Mathf.Clamp(PlaybackTime - StartTime, 0, Duration);
    /// <summary>事件进度 [0,1]</summary>
    public float Progress => Duration <= 0 ? 1f : Mathf.Clamp01((PlaybackTime - StartTime) / Duration);

    public CameraEventTime(float startTime, float duration, float playbackTime)
    {
        StartTime = startTime;
        Duration = duration;
        PlaybackTime = playbackTime;
    }
}

public abstract class CameraEventProcessor
{
    /// <summary>事件第一次到达开始时间时调用</summary>
    public abstract void StartEvent(CameraEventBase ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer);

    /// <summary>事件进行期间每帧调用（包含开始与结束那一帧）</summary>
    public abstract void TickEvent(CameraEventBase ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer);

    /// <summary>事件到达结束时间，或被技能中断/循环重置时调用</summary>
    public abstract void EndEvent(CameraEventBase ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer);

    /// <summary>复位该处理器造成的相机影响，由复位事件处理器调用</summary>
    public virtual void ResetProcessor(SkillCamera camera, Skill_Player skillPlayer) { }
}

public abstract class CameraEventProcessor<T> : CameraEventProcessor where T : CameraEventBase
{
    protected readonly ICharacter Owner;

    protected CameraEventProcessor(ICharacter owner)
    {
        Owner = owner;
    }

    public override void StartEvent(CameraEventBase ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        if (ev is T t) StartEvent(t, time, camera, skillPlayer);
    }

    public override void TickEvent(CameraEventBase ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        if (ev is T t) TickEvent(t, time, camera, skillPlayer);
    }

    public override void EndEvent(CameraEventBase ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer)
    {
        if (ev is T t) EndEvent(t, time, camera, skillPlayer);
    }

    protected virtual void StartEvent(T ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer) { }

    protected virtual void TickEvent(T ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer) { }

    protected virtual void EndEvent(T ev, CameraEventTime time, SkillCamera camera, Skill_Player skillPlayer) { }
}
