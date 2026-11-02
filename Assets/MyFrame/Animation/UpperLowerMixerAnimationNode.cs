using System.Collections;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>使用 AvatarMask 将上身单片段动画与下身混合动画组合播放。</summary>
public class UpperLowerMixerAnimationNode : AnimationNodeBase, ICanSyncNode
{
    //Unity的根运动会自动采用第0层的动画
    private PlayableGraph _playableGraph;
    private AnimationLayerMixerPlayable _layerMixer;
    private AnimationMixerPlayable _upperMixer;
    private readonly AnimationClipPlayable[] _upperClips = new AnimationClipPlayable[2];
    private int _currentUpperPort;
    private Coroutine _upperTransitionCoroutine;
    private OneDimensionBlendAnimationNode _oneDimensionLowerNode;//下半身是一个混合动画(上下左右待机),索引为 0
    public OneDimensionBlendAnimationNode OneDimensionLowerNode => _oneDimensionLowerNode;
    private TwoDimensionalBlendTreeNode _twoDimensionalLowerNode;
    public TwoDimensionalBlendTreeNode TwoDimensionalLowerNode => _twoDimensionalLowerNode;
    private DirectionalLinearBlendTreeNode _directionalLinearLowerNode;
    public DirectionalLinearBlendTreeNode DirectionalLinearLowerNode => _directionalLinearLowerNode;
    /// <summary>下层混合动画是否启用相位同步。</summary>
    public bool IsSync { get; set; }
    

    /// <summary>初始化上下身动画节点，并使用多组二维混合树驱动下层动画。</summary>
    public void Init(PlayableGraph graph, AnimationMixerPlayable outputMixer,AvatarMask avatarMask, int inputPort, AnimationClip upperClip, List<List<AnimationClip>> clipsList, List<List<Vector2>> pointsList, List<float> blendMixersPoints, float speed, bool isSync = false, bool isTransitioning = false, List<List<float>> syncDeviations = null, bool isCached = false)
    {
        _layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
        InputPort = inputPort;
        _playableGraph = graph;
        IsSync = isSync;
        IsCached = isCached;
        ConnectToPlayableGraph(_layerMixer,graph,outputMixer,0,isTransitioning);
        InitializeUpperMixer(graph, avatarMask);
        _twoDimensionalLowerNode = new TwoDimensionalBlendTreeNode();
        _twoDimensionalLowerNode.Init(graph, _layerMixer,clipsList, pointsList, blendMixersPoints, speed, 0, isSync, syncDeviations:syncDeviations, isCached:isCached);
        SetUpperAnimationClipImmediate(upperClip);
        Speed = speed;
        SetClipSpeed(1f);
    }
    
    /// <summary>使用二维混合树配置初始化上下身动画节点。</summary>
    public void Init(PlayableGraph graph, AnimationMixerPlayable outputMixer,AvatarMask avatarMask, int inputPort, AnimationClip upperClip,TwoDimensionalBlendTreeData data, float speed, bool isSync = false, bool isTransitioning = false, bool isCached = false)
    {
        _layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
        InputPort = inputPort;
        _playableGraph = graph;
        IsSync = isSync;
        IsCached = isCached;
        ConnectToPlayableGraph(_layerMixer,graph,outputMixer,0,isTransitioning);
        InitializeUpperMixer(graph, avatarMask);
        _twoDimensionalLowerNode = new TwoDimensionalBlendTreeNode();
        _twoDimensionalLowerNode.Init(graph, _layerMixer,data, speed,0, isSync, isCached:isCached);
        SetUpperAnimationClipImmediate(upperClip);
        Speed = speed;
        SetClipSpeed(1f);
    }

    /// <summary>使用方向线性混合树配置初始化上下身动画节点。</summary>
    public void Init(PlayableGraph graph, AnimationMixerPlayable outputMixer, AvatarMask avatarMask, int inputPort, AnimationClip upperClip, DirectionalLinearBlendTreeData data, float speed, bool isSync = false, bool isTransitioning = false, bool isCached = false)
    {
        _layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
        InputPort = inputPort;
        _playableGraph = graph;
        IsSync = isSync;
        IsCached = isCached;
        ConnectToPlayableGraph(_layerMixer, graph, outputMixer, 0, isTransitioning);
        InitializeUpperMixer(graph, avatarMask);
        _directionalLinearLowerNode = new DirectionalLinearBlendTreeNode();
        _directionalLinearLowerNode.Init(graph, _layerMixer, data, speed, 0, isSync, isCached: isCached);
        SetUpperAnimationClipImmediate(upperClip);
        Speed = speed;
        SetClipSpeed(1f);
    }

    private void InitializeUpperMixer(PlayableGraph graph, AvatarMask avatarMask)
    {
        if (_upperTransitionCoroutine != null)
        {
            MonoSystem.Stop_Coroutine(this);
            _upperTransitionCoroutine = null;
        }
        _upperClips[0] = default;
        _upperClips[1] = default;
        _currentUpperPort = 0;
        _upperMixer = AnimationMixerPlayable.Create(graph, 2);
        _layerMixer.ConnectInput(1, _upperMixer, 0, 1f);
        _layerMixer.SetLayerMaskFromAvatarMask(1, avatarMask);
        _layerMixer.SetLayerAdditive(0, false);
        _layerMixer.SetLayerAdditive(1, false);
        _layerMixer.SetInputWeight(0, 1f);
        _layerMixer.SetInputWeight(1, 1f);
    }

    /// <summary>替换上层动画片段。</summary>
    /// <param name="clip">新的上层动画片段。</param>
    /// <param name="transitionFixedTime">上层动画过渡时长（秒），设为 0 时立即切换。</param>
    public void SetUpperAnimationClip(AnimationClip clip, float transitionFixedTime = 0.25f)
    {
        if (clip == null) return;

        if (_upperTransitionCoroutine != null)
        {
            MonoSystem.Stop_Coroutine(this);
            _upperTransitionCoroutine = null;
            CollapseUpperTransition();
        }

        var currentClip = _upperClips[_currentUpperPort];
        if (currentClip.IsValid() && currentClip.GetAnimationClip() == clip) return;

        var nextPort = 1 - _currentUpperPort;
        DestroyUpperClip(nextPort);
        _upperClips[nextPort] = AnimationClipPlayable.Create(_playableGraph, clip);
        _upperMixer.ConnectInput(nextPort, _upperClips[nextPort], 0, 0f);

        if (transitionFixedTime <= 0f)
        {
            _upperMixer.SetInputWeight(_currentUpperPort, 0f);
            _upperMixer.SetInputWeight(nextPort, 1f);
            DestroyUpperClip(_currentUpperPort);
            _currentUpperPort = nextPort;
            return;
        }

        _upperTransitionCoroutine = MonoSystem.Start_Coroutine(TransitionUpperAnimation(_currentUpperPort, nextPort, transitionFixedTime));
    }

    private void SetUpperAnimationClipImmediate(AnimationClip clip)
    {
        if (clip == null) return;
        DestroyUpperClip(_currentUpperPort);
        _upperClips[_currentUpperPort] = AnimationClipPlayable.Create(_playableGraph, clip);
        _upperMixer.ConnectInput(_currentUpperPort, _upperClips[_currentUpperPort], 0, 1f);
        _upperMixer.SetInputWeight(_currentUpperPort, 1f);
    }

    private IEnumerator TransitionUpperAnimation(int fromPort, int toPort, float fixedTime)
    {
        var currentWeight = 1f;
        var speed = 1f / fixedTime;
        while (currentWeight > 0f)
        {
            yield return null;
            currentWeight = Mathf.Clamp01(currentWeight - Time.deltaTime * Speed * speed);
            _upperMixer.SetInputWeight(fromPort, currentWeight);
            _upperMixer.SetInputWeight(toPort, 1f - currentWeight);
        }

        DestroyUpperClip(fromPort);
        _currentUpperPort = toPort;
        _upperTransitionCoroutine = null;
    }

    private void CollapseUpperTransition()
    {
        var fromPort = _currentUpperPort;
        var toPort = 1 - fromPort;
        var keepPort = _upperMixer.GetInputWeight(toPort) > _upperMixer.GetInputWeight(fromPort) ? toPort : fromPort;
        var discardPort = 1 - keepPort;
        _upperMixer.SetInputWeight(keepPort, 1f);
        _upperMixer.SetInputWeight(discardPort, 0f);
        DestroyUpperClip(discardPort);
        _currentUpperPort = keepPort;
    }

    private void DestroyUpperClip(int port)
    {
        if (_upperClips[port].IsValid())
        {
            _upperMixer.DisconnectInput(port);
            _playableGraph.DestroyPlayable(_upperClips[port]);
            _upperClips[port] = default;
        }
    }

    /// <summary>将上下身分层混合器接入父播放图。</summary>
    public override void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)
    {
        if (isTransitioning) return;
        ConnectToPlayableGraph(_layerMixer, graph, outputMixer, 0,false);
    }

    /// <summary>设置上下身动画混合器的播放速度倍率。</summary>
    public override void SetClipSpeed(float speed)
    {
        _layerMixer.SetSpeed(1f);
        _directionalLinearLowerNode?.SetClipSpeed(speed * Speed);
        _twoDimensionalLowerNode?.SetClipSpeed(speed * Speed);
        _oneDimensionLowerNode?.SetClipSpeed(speed * Speed );
        for (var i = 0; i < _upperClips.Length; i++)
        {
            if (_upperClips[i].IsValid()) _upperClips[i].SetSpeed(speed * Speed);
        }
    }

    /// <summary>设置下层一维混合树的各动画权重。</summary>
    public void SetLowerAsOneDimensionNode(List<float> weightList)
    {
        if (_oneDimensionLowerNode == null)
        {
            Debug.Log("当前下层节点不是一维混合动画节点，无法设置权重。");
            return;
        }
        _oneDimensionLowerNode.SetBlendAnimationWeight(weightList);
    }

    /// <summary>按一维层间参数设置下层二维混合树的动画组权重。</summary>
    public void SetLowerMixerAsTwoDimensionNode(float weight)
    {
        if (_twoDimensionalLowerNode == null)
        {
            Debug.Log("当前下层节点不是二维混合动画节点，无法设置权重。");
            return;
        }
        _twoDimensionalLowerNode.SetBlendAnimationAsLinerPoint(weight);
    }
    
    /// <summary>设置下层二维混合树指定动画组的二维混合位置。</summary>
    public void SetLowerMixerAsTwoDimensionNode(int index,Vector2 point)
    {
        if (_twoDimensionalLowerNode == null)
        {
            Debug.Log("当前下层节点不是二维混合动画节点，无法设置权重。");
            return;
        }
        _twoDimensionalLowerNode.SetClipsWeights(index,point);
    }

    /// <summary>同时设置下层方向线性混合树的移动速度和转向参数。</summary>
    public void SetLowerDirectionalLinearNode(float movePoint, float turnPoint)
    {
        if (_directionalLinearLowerNode == null)
        {
            Debug.Log("当前下层节点不是方向线性混合动画节点，无法设置权重。");
            return;
        }

        _directionalLinearLowerNode.SetBlendByXAndY(movePoint, turnPoint);
    }
    
    /// <summary>设置下层方向线性混合树的移动速度参数。</summary>
    public void SetLowerDirectionalLinearMovePoint(float movePoint)
    {
        if (_directionalLinearLowerNode == null)
        {
            Debug.Log("当前下层节点不是方向线性混合动画节点，无法设置移动权重。");
            return;
        }

        _directionalLinearLowerNode.SetBlendAnimationAsLinerPoint(movePoint);
    }

    /// <summary>设置下层方向线性混合树的转向参数。</summary>
    public void SetLowerDirectionalLinearTurnPoint(float turnPoint)
    {
        if (_directionalLinearLowerNode == null)
        {
            Debug.Log("当前下层节点不是方向线性混合动画节点，无法设置转向权重。");
            return;
        }

        _directionalLinearLowerNode.SetClipWeightForAllLayers(turnPoint);
    }

    /// <summary>回收上下身节点及其下层混合节点。</summary>
    /// <param name="isForced">为 true 时强制回收缓存节点。</param>
    public override void PushPool(bool isForced = false)
    {
        IsLinked = false;
        if (_upperTransitionCoroutine != null)
        {
            MonoSystem.Stop_Coroutine(this);
            _upperTransitionCoroutine = null;
        }
        _directionalLinearLowerNode?.PushPool();
        _twoDimensionalLowerNode?.PushPool();
        _oneDimensionLowerNode?.PushPool();
        if (IsCached && !isForced) return;
        _directionalLinearLowerNode = null;
        _twoDimensionalLowerNode = null;
        _oneDimensionLowerNode = null;
        _layerMixer = default;
        _upperMixer = default;
        _upperClips[0] = default;
        _upperClips[1] = default;
    }
}