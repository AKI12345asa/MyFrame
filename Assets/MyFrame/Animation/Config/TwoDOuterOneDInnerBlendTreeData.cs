using System;
using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>保存二维外层位置与每个外层组内的一维动画混合配置。</summary>
[CreateAssetMenu(fileName = "Animation NodeData", menuName = "Animation/TwoD Outer OneD Inner Blend Tree Data")]
[FrameConfig(typeof(TwoDOuterOneDInnerBlendTreeData), "AnimationConfig/TwoDOuterOneDInnerBlendTreeData")]
public class TwoDOuterOneDInnerBlendTreeData : FrameConfigBase
{
    /// <summary>外层二维采样点及其对应的内层一维动画集合。</summary>
    public List<TwoDOuterOneDInnerBlendTreePoint> Points;
}

/// <summary>外层二维采样点及其关联的内层动画片段组。</summary>
[Serializable]
public class TwoDOuterOneDInnerBlendTreePoint
{
    /// <summary>外层动画组名称。</summary>
    public string name;
    /// <summary>此动画组在外层二维混合空间中的位置。</summary>
    public Vector2 outerPoint;
    /// <summary>此动画组内部按一维参数混合的片段列表。</summary>
    public List<DirectionalLinearBlendTreePoint> innerPoints;
}
