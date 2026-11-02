using System;
using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>保存基于移动速度分层、并按转向参数混合的动画数据。</summary>
[CreateAssetMenu(fileName = "Animation NodeData", menuName = "Animation/Directional Linear Blend Tree Data")]
[FrameConfig(typeof(DirectionalLinearBlendTreeData), "AnimationConfig/DirectionalLinearBlendTreeData")]
public class DirectionalLinearBlendTreeData : FrameConfigBase
{
    /// <summary>按移动速度排列的方向动画层。</summary>
    public List<DirectionalLinearBlendTreeLayer> Layers;
}

/// <summary>方向线性混合树中的一个移动速度层。</summary>
[Serializable]
public class DirectionalLinearBlendTreeLayer
{
    /// <summary>动画层名称。</summary>
    public string name;
    /// <summary>动画层在移动速度轴上的混合位置。</summary>
    public float mixerPoint;
    /// <summary>此层按转向参数混合的动画片段列表。</summary>
    public List<DirectionalLinearBlendTreePoint> points;
}

/// <summary>移动速度层中的单个转向采样点及其动画片段。</summary>
[Serializable]
public class DirectionalLinearBlendTreePoint
{
    /// <summary>此转向采样点对应的动画片段。</summary>
    [LabelText("动画片段")] public AnimationClip clip;
    /// <summary>片段对应的转向参数位置。</summary>
    [LabelText("转向参数")] public float point;
    /// <summary>片段间相位同步使用的归一化相位偏移。</summary>
    [LabelText("相位差")] public float syncDeviation;
}
