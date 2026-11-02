using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>混合动画节点基类，扩展线性权重控制和相位同步能力。</summary>
public abstract class BlendAnimationNodeBase:AnimationNodeBase, ICanSyncNode
{
    /// <summary>节点是否启用动画片段相位同步。</summary>
    public virtual bool IsSync { get; }
    
    /// <summary>将混合节点回收到对象池；缓存节点默认保留以供复用。</summary>
    /// <param name="isForced">为 true 时忽略缓存设置并强制回收。</param>
    public override void PushPool(bool isForced = false)
    {
        if(IsCached) return;
        base.PushPool(isForced);
    }

    /// <summary>按一维参数在相邻混合点之间计算动画权重。</summary>
    /// <param name="xValue">用于计算混合权重的一维参数。</param>
    public virtual void SetBlendAnimationAsLinerPoint(float xValue){}
    
    protected virtual void SyncWhenMainClipChange(){}

    protected virtual void SyncPhase(){}
}
