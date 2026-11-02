using System;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>先按二维参数混合外层动画组，再按一维参数混合每组内部片段。</summary>
public class TwoDOuterOneDInnerBlendTreeNode : BlendAnimationNodeBase
{
    private AnimationMixerPlayable _totalMixer;
    private bool _isSync;
    public override bool IsSync => _isSync;
    private float _playSpeed;

    private List<AnimationMixerPlayable> _innerMixers;
    private List<List<AnimationClipPlayable>> _innerClipPlayableLists;
    private List<DirectionalLinearBlendTreeNode.OneDimensionalTransitionContainer> _innerContainers;
    private List<List<float>> _syncDeviations;
    private TwoDimensionalBlendTreeNode.TwoDimensionalTransitionContainer _outerContainer;

    private Vector2 _currentOuterPoint;
    private Vector2 _outerVelocity;
    private float _currentInnerPoint;
    private float _innerVelocity;

    public override float Speed
    {
        get => _playSpeed;
        set => _playSpeed = value;
    }

    protected override void ConnectToPlayableGraph<V>(Playable connector, PlayableGraph graph, V outputMixer, int sourceOutPutPort, bool isTransitioning)
    {
        base.ConnectToPlayableGraph(connector, graph, outputMixer, sourceOutPutPort, isTransitioning);
    }

    /// <summary>将二维外层、一维内层混合器接入父播放图。</summary>
    public override void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)
    {
        if (isTransitioning) return;
        ConnectToPlayableGraph(_totalMixer, graph, outputMixer, 0, false);
    }

    /// <summary>设置所有内层动画片段的播放速度。</summary>
    public override void SetClipSpeed(float speed)
    {
        if (_innerClipPlayableLists == null) return;
        foreach (var clips in _innerClipPlayableLists)
        {
            foreach (var clip in clips)
            {
                clip.SetSpeed(speed * Speed);
            }
        }
    }

    /// <summary>使用外层二维采样点和各层内部的一维动画片段初始化节点。</summary>
    public void Init<V>(
        PlayableGraph graph,
        V outputMixer,
        List<Vector2> outerPoints,
        List<List<AnimationClip>> innerClipsList,
        List<List<float>> innerPointsList,
        float speed,
        int inputPort,
        bool isSync = false,
        bool isTransitioning = false,
        List<List<float>> syncDeviations = null,
        bool isCached = false) where V : struct, IPlayable
    {
        ValidateInputLists(outerPoints, innerClipsList, innerPointsList, syncDeviations, isSync);
        _isSync = isSync;
        _playSpeed = speed;
        InputPort = inputPort;
        IsCached = isCached;
        _syncDeviations = syncDeviations;

        _totalMixer = AnimationMixerPlayable.Create(graph, outerPoints.Count);
        _innerMixers = new List<AnimationMixerPlayable>(outerPoints.Count);
        _innerClipPlayableLists = new List<List<AnimationClipPlayable>>(outerPoints.Count);
        _innerContainers = new List<DirectionalLinearBlendTreeNode.OneDimensionalTransitionContainer>(outerPoints.Count);
        _outerContainer = TwoDimensionalBlendTreeNode.TwoDimensionalTransitionContainer.Create(outerPoints.ToArray());

        for (var i = 0; i < outerPoints.Count; i++)
        {
            ConnectInnerMixer(graph, i, innerClipsList[i], innerPointsList[i]);
        }

        _currentOuterPoint = Vector2.zero;
        _outerVelocity = Vector2.zero;
        _currentInnerPoint = innerPointsList[0][0];
        _innerVelocity = 0f;

        if (_innerMixers.Count > 0)
        {
            SetOuterBlendPoint(_currentOuterPoint);
            SetInnerBlendPointForAllLayers(_currentInnerPoint);
        }

        ConnectToPlayableGraph(_totalMixer, graph, outputMixer, 0, isTransitioning);
    }

    /// <summary>使用二维外层、一维内层混合树配置初始化节点。</summary>
    public void Init<V>(PlayableGraph graph, V outputMixer, TwoDOuterOneDInnerBlendTreeData data, float speed, int inputPort, bool isSync = false, bool isTransitioning = false, bool isCached = false) where V : struct, IPlayable
    {
        var outerPoints = new List<Vector2>();
        var innerClipsList = new List<List<AnimationClip>>();
        var innerPointsList = new List<List<float>>();
        var syncDeviationsList = new List<List<float>>();
        foreach (var p in data.Points)
        {
            var clips = new List<AnimationClip>();
            var points = new List<float>();
            var deviations = new List<float>();
            foreach (var inner in p.innerPoints)
            {
                clips.Add(inner.clip);
                points.Add(inner.point);
                deviations.Add(inner.syncDeviation);
            }

            outerPoints.Add(p.outerPoint);
            innerClipsList.Add(clips);
            innerPointsList.Add(points);
            syncDeviationsList.Add(deviations);
        }

        Init(graph, outputMixer, outerPoints, innerClipsList, innerPointsList, speed, inputPort, isSync, isTransitioning, syncDeviationsList, isCached);
    }

    private static void ValidateInputLists(
        List<Vector2> outerPoints,
        List<List<AnimationClip>> innerClipsList,
        List<List<float>> innerPointsList,
        List<List<float>> syncDeviations,
        bool isSync)
    {
        if (outerPoints == null) throw new ArgumentNullException(nameof(outerPoints));
        if (innerClipsList == null) throw new ArgumentNullException(nameof(innerClipsList));
        if (innerPointsList == null) throw new ArgumentNullException(nameof(innerPointsList));
        if (outerPoints.Count == 0) throw new ArgumentException("外层二维采样点列表不能为空。", nameof(outerPoints));

        for (var i = 0; i < outerPoints.Count; i++)
        {
            if (innerClipsList[i] == null) throw new ArgumentException($"第 {i} 层的内部动画片段列表不能为空。");
            if (innerPointsList[i] == null) throw new ArgumentException($"第 {i} 层的内部一维采样点列表不能为空。");
            if (innerClipsList[i].Count != innerPointsList[i].Count)
            {
                throw new ArgumentException($"第 {i} 层的动画片段与一维采样点数量不一致：动画片段={innerClipsList[i].Count}，采样点={innerPointsList[i].Count}。");
            }

            if (syncDeviations == null) continue;
            if (syncDeviations[i] == null) throw new ArgumentException($"第 {i} 层的相位差列表不能为空。");
            if (syncDeviations[i].Count != innerClipsList[i].Count)
            {
                throw new ArgumentException($"第 {i} 层的动画片段与相位差数量不一致：动画片段={innerClipsList[i].Count}，相位差={syncDeviations[i].Count}。");
            }
        }

        TwoDimensionalBlendTreeNode.TwoDimensionalTransitionContainer.Create(outerPoints.ToArray());
        for (var i = 0; i < innerPointsList.Count; i++)
        {
            DirectionalLinearBlendTreeNode.OneDimensionalTransitionContainer.Create(innerPointsList[i].ToArray());
        }
    }

    private void ConnectInnerMixer(PlayableGraph graph, int index, List<AnimationClip> clips, List<float> points)
    {
        var mixer = AnimationMixerPlayable.Create(graph, clips.Count);
        graph.Connect(mixer, 0, _totalMixer, index);
        _innerMixers.Add(mixer);
        _innerClipPlayableLists.Add(new List<AnimationClipPlayable>(clips.Count));
        _innerContainers.Add(DirectionalLinearBlendTreeNode.OneDimensionalTransitionContainer.Create(points.ToArray()));
        for (var i = 0; i < clips.Count; i++)
        {
            var playable = AnimationClipPlayable.Create(graph, clips[i]);
            playable.SetSpeed(_playSpeed);
            _innerClipPlayableLists[index].Add(playable);
            graph.Connect(playable, 0, mixer, i);
            mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
        }
    }

    /// <summary>根据二维参数设置各外层动画组的权重。</summary>
    public void SetOuterBlendPoint(Vector2 outerPoint)
    {
        var outerWeights = TwoDimensionalBlendTreeNode.TwoDimensionalTransitionContainer.GetWeights(_outerContainer, outerPoint);
        for (var i = 0; i < outerWeights.Length; i++)
        {
            _totalMixer.SetInputWeight(i, outerWeights[i]);
        }
    }

    /// <summary>根据一维参数设置指定外层动画组内部片段的权重。</summary>
    public void SetInnerBlendPoint(int outerIndex, float innerPoint)
    {
        if (outerIndex < 0 || outerIndex >= _innerMixers.Count) throw new IndexOutOfRangeException("外层动画组索引越界。");
        var innerWeights = DirectionalLinearBlendTreeNode.OneDimensionalTransitionContainer.GetWeights(_innerContainers[outerIndex], innerPoint);
        var mixer = _innerMixers[outerIndex];
        for (var i = 0; i < innerWeights.Length; i++)
        {
            mixer.SetInputWeight(i, innerWeights[i]);
        }
    }

    /// <summary>将相同的一维混合参数应用到所有外层动画组。</summary>
    public void SetInnerBlendPointForAllLayers(float innerPoint)
    {
        for (var i = 0; i < _innerMixers.Count; i++)
        {
            SetInnerBlendPoint(i, innerPoint);
        }
    }

    /// <summary>立即设置外层二维参数和内层一维参数。</summary>
    public void SetBlendByOuterAndInner(Vector2 outerPoint, float innerPoint)
    {
        _currentOuterPoint = outerPoint; ;
        _outerVelocity = Vector2.zero;
        _currentInnerPoint = innerPoint;
        _innerVelocity = 0f;
        SetOuterBlendPoint(outerPoint);
        SetInnerBlendPointForAllLayers(innerPoint);
    }

    /// <summary>平滑地将外层二维参数和内层一维参数调整到目标值。</summary>
    public void SmoothSetBlendByOuterAndInner(Vector2 targetOuterPoint, float targetInnerPoint, float outerXDampTime, float outerYDampTime, float innerDampTime)
    {
        _currentOuterPoint.x = outerXDampTime <= 0f
            ? targetOuterPoint.x
            : Mathf.SmoothDamp(_currentOuterPoint.x, targetOuterPoint.x, ref _outerVelocity.x, outerXDampTime);
        _currentOuterPoint.y = outerYDampTime <= 0f
            ? targetOuterPoint.y
            : Mathf.SmoothDamp(_currentOuterPoint.y, targetOuterPoint.y, ref _outerVelocity.y, outerYDampTime);
        _currentInnerPoint = innerDampTime <= 0f
            ? targetInnerPoint
            : Mathf.SmoothDamp(_currentInnerPoint, targetInnerPoint, ref _innerVelocity, innerDampTime);
        SetOuterBlendPoint(_currentOuterPoint);
        SetInnerBlendPointForAllLayers(_currentInnerPoint);
    }

    /// <summary>回收混合节点及其内部混合器资源。</summary>
    /// <param name="isForced">为 true 时强制回收缓存节点。</param>
    public override void PushPool(bool isForced = false)
    {
        if (IsCached && !isForced)
        {
            IsLinked = false;
            return;
        }

        _innerMixers?.Clear();
        _innerClipPlayableLists?.Clear();
        _innerContainers?.Clear();
        _syncDeviations?.Clear();
        _innerMixers = null;
        _innerClipPlayableLists = null;
        _innerContainers = null;
        _syncDeviations = null;
        _currentOuterPoint = Vector2.zero;
        _outerVelocity = Vector2.zero;
        _currentInnerPoint = 0f;
        _innerVelocity = 0f;
        base.PushPool(isForced);
    }

    public float GetMasterClipTime()
    {
        if (_innerClipPlayableLists == null || _innerClipPlayableLists.Count == 0) return 0f;
        var outerWeights = TwoDimensionalBlendTreeNode.TwoDimensionalTransitionContainer.GetWeights(_outerContainer, _currentOuterPoint);
        float masterTime = 0f;
        for (var i = 0; i < _innerClipPlayableLists.Count; i++)
        {
            var innerClips = _innerClipPlayableLists[i];
            if (innerClips == null || innerClips.Count == 0) continue;
            var innerWeights = DirectionalLinearBlendTreeNode.OneDimensionalTransitionContainer.GetWeights(_innerContainers[i], _currentInnerPoint);
            for (var j = 0; j < innerClips.Count; j++)
            {
                var clipTime = innerClips[j].GetTime();
                masterTime += (float)(clipTime * outerWeights[i] * innerWeights[j]);
            }
        }
        return masterTime;
    }
}
