using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>按一维标量参数在多个动画片段之间进行线性混合。</summary>
public class OneDimensionBlendAnimationNode : BlendAnimationNodeBase
{
    protected AnimationMixerPlayable _blendMixer;
    protected bool _isSync;
    public override bool IsSync => _isSync;
    protected bool _isSyncing;
    /// <summary>节点当前是否正在执行相位同步。</summary>
    public bool IsSyncing => _isSyncing;
    private List<AnimationClip> _animationClips;
    private float _playSpeed;
    protected List<AnimationClipPlayable> _blendClipPlayableList = new(10);
    protected List<float> _animationClipPoints;
    /// <summary>获取参与混合的动画片段，顺序与混合点顺序一致。</summary>
    public IReadOnlyList<AnimationClip> GetClips() => _animationClips;

    public override float Speed
    {
        get => _playSpeed;
        set => _playSpeed = value;
    }

    /// <summary>使用动画片段列表和对应的一维混合点初始化节点。</summary>
    public virtual void Init<V>(PlayableGraph graph, V outputMixer, List<AnimationClip> clips, float[] points,
        float speed, int inputPort, bool isSync = false, bool isTransitioning = false,List<float> weights = null,bool isCached = false) where V : struct, IPlayable
    {
        _isSync = isSync;
        _mainPlayableIndex = 0;
        _blendMixer = AnimationMixerPlayable.Create(graph, clips.Count);
        _blendClipPlayableList.Clear();
        _animationClips = clips;
        IsCached = isCached;
        InputPort = inputPort;
        _playSpeed = speed;
        if (points != null) _animationClipPoints = points.ToList();
        ConnectToPlayableGraph(_blendMixer, graph, outputMixer, 0, isTransitioning);
        for (var i = 0; i < _animationClips.Count; i++)
        {
            CreateAndConnectBlendPlayable(graph, _animationClips[i], i, _playSpeed);
        }
        if(weights != null)
        {
            SetBlendAnimationWeight(weights);
        }
    }

    /// <summary>使用两个动画片段初始化一维混合节点。</summary>
    public void Init<V>(PlayableGraph graph, V outputMixer, AnimationClip clip1, AnimationClip clip2, float[] points,
        float speed, int inputPort, bool isSync = false, bool isTransitioning = false,List<float> weights = null,bool isCached =false) where V : struct, IPlayable
    {
        _isSync = isSync;
        _mainPlayableIndex = 0;
        _blendMixer = AnimationMixerPlayable.Create(graph, 2);
        _blendClipPlayableList.Clear();
        IsCached = isCached;
        InputPort = inputPort;
        _playSpeed = speed;
        _animationClips = new List<AnimationClip> { clip1, clip2 };
        _animationClipPoints = points.ToList();
        ConnectToPlayableGraph(_blendMixer, graph, outputMixer, 0, isTransitioning);
        for (var i = 0; i < _animationClips.Count; i++)
        {
            CreateAndConnectBlendPlayable(graph, _animationClips[i], i, _playSpeed);
        }
        if (weights != null)
        {
            SetBlendAnimationWeight(weights);
        }
    }

    protected AnimationClipPlayable CreateAndConnectBlendPlayable(PlayableGraph graph, AnimationClip clip, int index,
        float speed)
    {
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetSpeed(speed);
        _blendClipPlayableList.Add(clipPlayable);
        graph.Connect(clipPlayable, 0, _blendMixer, index);
        return clipPlayable;

    }

    protected override void ConnectToPlayableGraph<V>(Playable connector, PlayableGraph graph, V outputMixer,
        int sourceOutPutPort, bool isTransitioning)
    {
        base.ConnectToPlayableGraph(connector, graph, outputMixer, sourceOutPutPort, isTransitioning);
        if (isTransitioning) return;
        if (_isSync)
        {
            this.AddUpdate(SyncPhase);
        }
    }

    /// <summary>将一维混合器接入父播放图。</summary>
    public override void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)
    {
        if (isTransitioning) return;
        ConnectToPlayableGraph(_blendMixer, graph, outputMixer, 0, false);
    }

    /// <summary>直接设置各动画片段的混合权重。</summary>
    /// <param name="weightList">权重列表，顺序和数量需与动画片段一致。</param>
    public void SetBlendAnimationWeight(List<float> weightList)
    {
        for (int i = 0; i < _blendClipPlayableList.Count; i++)
        {
            _blendMixer.SetInputWeight(i, weightList[i]);
        }
        SyncWhenMainClipChange();
    }

    /// <summary>根据一维混合参数调整相邻片段的权重，其余片段权重归零。</summary>
    /// <param name="xValue">当前的一维混合参数。</param>
    public override void SetBlendAnimationAsLinerPoint(float xValue)
    {
        if (xValue > _animationClipPoints.Last()) throw new Exception("混合参数超过最大值。");
        xValue = Mathf.Clamp(xValue, _animationClipPoints.First(), _animationClipPoints.Last());
        var ceilPoint = _animationClipPoints.FirstOrDefault(point => point >= xValue);
        var ceilIndex = _animationClipPoints.IndexOf(ceilPoint);
        if (ceilIndex == 0)
        {
            ceilIndex = 1;
            ceilPoint = _animationClipPoints[ceilIndex];
        }

        var floorPoint = _animationClipPoints[ceilIndex - 1];
        var floorIndex = ceilIndex - 1;
        if (Mathf.Approximately(floorIndex, ceilIndex))
        {
            floorIndex = ceilIndex - 1;
        }

        for (var i = 0; i < _blendClipPlayableList.Count; i++)
        {
            if (i == floorIndex || i == ceilIndex) continue;
            _blendMixer.SetInputWeight(i, 0);
        }

        var dis = ceilPoint - floorPoint;
        _blendMixer.SetInputWeight(floorIndex, (ceilPoint - xValue) / dis);
        _blendMixer.SetInputWeight(ceilIndex, (xValue - floorPoint) / dis);
        SyncWhenMainClipChange();
    }
    

        /// <summary>设置此节点中所有动画片段的播放速度。</summary>
        public override void SetClipSpeed(float speed)
        {
            for (var i = 0; i < _blendClipPlayableList.Count; i++)
            {
                _blendClipPlayableList[i].SetSpeed(speed * Speed);
            }
        }

        /// <summary>回收节点；缓存节点在非强制回收时保留其 Playable 以便复用。</summary>
        public override void PushPool(bool isForced = false)
        {   
            if (IsCached && !isForced)
            {
                IsLinked = false;
                this.RemoveUpdate(SyncPhase);
                return;
            }
            _blendClipPlayableList.Clear();
            this.RemoveUpdate(SyncPhase);
            base.PushPool();
            _mainPlayableIndex = 0;
        }

        // 确保至少有一个输入权重为非零，避免外层过渡首帧输出为空
        /// <summary>若所有输入权重均为零，则将第一个输入设为满权重以避免无动画输出。</summary>
        public void EnsureAnyWeight()
        {
            var hasWeight = false;
            for (var i = 0; i < _blendMixer.GetInputCount(); i++)
            {
                if (_blendMixer.GetInputWeight(i) > 0f)
                {
                    hasWeight = true;
                    break;
                }
            }

            if (!hasWeight && _blendMixer.GetInputCount() > 0)
            {
                _blendMixer.SetInputWeight(0, 1f);
                SyncWhenMainClipChange();
            }
        }
        
        /// <summary>
        /// 同步相位
        /// </summary>
        protected override void SyncPhase()
        {
            if (!_isSync || _blendClipPlayableList.Count == 0) return;
            _isSyncing = true;
            var currentClipInPlayIndex = _mainPlayableIndex;
            if (currentClipInPlayIndex < 0 || currentClipInPlayIndex >= _blendClipPlayableList.Count) return;
            var currentTime = _blendClipPlayableList[currentClipInPlayIndex].GetTime();
            var currentClip = _blendClipPlayableList[currentClipInPlayIndex].GetAnimationClip();
            if (currentClip == null || currentClip.length <= 1e-6f) return;
            var norm = (currentTime % currentClip.length) / currentClip.length;
            for (var i = 0; i < _blendClipPlayableList.Count; i++)
            {
                if(i == currentClipInPlayIndex) continue;
                var playable = _blendClipPlayableList[i];
                var clip = playable.GetAnimationClip();
                if (clip == null || clip.length <= 1e-6f) continue;
                var mappedTime = norm * clip.length;
                playable.SetTime(mappedTime);
            }
        }
        private int _mainPlayableIndex;
        protected override void SyncWhenMainClipChange()
        {
            if (!_isSync || _blendClipPlayableList.Count == 0) return;
            var dominantIndex = 0;
            for (var i = 1; i < _blendClipPlayableList.Count; i++)
            {
                if (_blendMixer.GetInputWeight(i) > _blendMixer.GetInputWeight(dominantIndex))
                    dominantIndex = i;
            }
            _mainPlayableIndex = dominantIndex;
        }
    }
