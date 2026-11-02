using System;
using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>保存多层二维动画混合所需的层位置、动画片段及采样点。</summary>
[CreateAssetMenu(fileName = "Animation NodeData", menuName = "Animation/TwoDimension Blend Tree Data")]
[FrameConfig(typeof(TwoDimensionalBlendTreeData), "AnimationConfig/TwoDimensionalBlendTreeData")]
public class TwoDimensionalBlendTreeData:FrameConfigBase
{
    /// <summary>二维混合层列表；每层由一个层间混合点及一组二维动画采样点构成。</summary>
    public List<TwoDimensionalBlendTreePoints> PointsList;
}
/// <summary>二维混合树中的一个动画层及其层间混合位置。</summary>
[Serializable]
public class TwoDimensionalBlendTreePoints
{
   /// <summary>该混合层的名称。</summary>
   public string name; 
   /// <summary>该层在层间一维混合轴上的位置。</summary>
   public float mixerPoint; 
   /// <summary>此层参与二维混合的动画采样点。</summary>
   public List<TwoDimensionalBlendTreePoint> points;
}

/// <summary>一个动画片段及其二维采样位置和相位偏移。</summary>
[Serializable]
public class TwoDimensionalBlendTreePoint
{
    /// <summary>此采样点对应的动画片段。</summary>
    [LabelText("动画片段")]public AnimationClip clip;
    /// <summary>动画片段对应的二维采样位置。</summary>
    [LabelText("片段位置")]public Vector2 point;
    /// <summary>片段间相位同步使用的归一化相位偏移。</summary>
    [LabelText("相位差")]public float syncDeviation;
}