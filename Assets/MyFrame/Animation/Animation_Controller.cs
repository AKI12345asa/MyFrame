using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFrame;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

/// <summary>
/// 动画控制器
/// </summary>
public class Animation_Controller : MonoBehaviour
{
    /// <summary>接收 PlayableGraph 动画输出的 Animator。</summary>
    [SerializeField] public Animator animator;
    private PlayableGraph _graph;
    private AnimationMixerPlayable _mixer;//主混合器仅有两个输入端口，分别对应当前节点和下一个节点
    private bool _graphInitialized;
    private AnimationNodeBase _previousNode;
    private AnimationNodeBase _currentNode;
    /// <summary>当前正在播放或过渡至的动画节点。</summary>
    public AnimationNodeBase CurrentNode => _currentNode;
    private int _fromPort = 0;
    private int _toPort = 1;
    private bool _currentIsFirstPlayable1;
    private bool _isSyncing;
    private float _syncClampSpeed;
    private Vector2 _syncClamp;
    private Coroutine _transitionCoroutine;
    private float _speed = 1f;
    private AnimationNodeBase _nextNode;
    private float _nextTransitionTime;
    private bool _transitionBreaking;
    private bool _chainPlay;
    private AnimationChain _curAnimationChain;
    /// <summary>阻止新的动画节点进入过渡队列时为 true。</summary>
    public bool LockTransition { get; set; }
    
    /// <summary>当前动画的播放速度；设置时立即更新所有活动节点。</summary>
    public float Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            _currentNode?.SetClipSpeed(_speed);
            _previousNode?.SetClipSpeed(_speed);
            _nextNode?.SetClipSpeed(_speed);
        }
    }
    /// <summary>创建并启动动画 PlayableGraph。播放动画前必须先调用。</summary>
    public void Init()
    {
        //创建图
        _graph = PlayableGraph.Create(gameObject.name);
        _graphInitialized = true;
        
        //设置图的时间模式
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
        //创建混合器
         _mixer = AnimationMixerPlayable.Create(_graph, 3);
         
        //连接输出
        var output = AnimationPlayableOutput.Create(_graph,"Animation",animator);
        
        //进行连接 
        output.SetSourcePlayable(_mixer);
        
        _graph.Play();
        
    }

    /// <summary>
    /// 回收动画控制器，清理所有状态
    /// </summary>
    /// <summary>断开并销毁动画图，清空当前、前一个及待播放节点。</summary>
    public void Recycle()
    {
        DestroyGraph();
        _graphInitialized = false;
        _currentNode = null;
        _previousNode = null;
        _nextNode = null;
    }

    /// <summary>
    /// 重新启动动画控制器，清理所有状态并重新初始化
    /// </summary>
    /// <summary>停止现有过渡并重新创建动画图。</summary>
    public void ReStart()
    {
        // 确保完全清除之前的所有状态
        if (_graphInitialized)
        {
            DestroyGraph();
        }
        
        // 停止所有协程
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }
        
        // 重置所有节点引用
        _previousNode = null;
        _currentNode = null;
        _nextNode = null;
        Init();
    }

    /// <summary>断开指定节点并将其回收到对象池。</summary>
    /// <param name="node">需要销毁或回收的节点。</param>
    public void DestroyNode(AnimationNodeBase node)
    {
        if (node != null)
        {
            _graph.Disconnect(_mixer,node.InputPort);
            node.PushPool();
        }
    }

    /// <summary>更新待播放节点的过渡状态，并同步当前节点的播放速度。</summary>
    public void Update()
    {
        if (_nextNode != null)
        {
            UpdateTransitionAnimation(_nextTransitionTime);
        }
        if(_curAnimationChain != null) _curAnimationChain.UpdateChain();
    }

    /// <summary>
    /// 设置同步速度和同步速度范围这是因为如果要同步前后两个动画的播放进度有可能会因为骨骼出现位移不稳定的情况这里通过设置同步速度
    /// 来保证动画播放时角色移动速度与期望的移动速度一致，避免出现位移不稳定的情况
    /// </summary>
    /// <param name="syncClampSpeed">用于限制根运动位移的基准速度。</param>
    /// <param name="syncClamp">根运动位移倍率的最小值和最大值。</param>
    public void SetSyncClampSpeed(float syncClampSpeed, Vector2 syncClamp)
    {
        _syncClampSpeed = syncClampSpeed;
        _syncClamp = syncClamp;
    }


    /// <summary>
    /// 设置根运动是否按同步模式进行限幅。
    /// </summary>
    public void SetSyncing(bool isSyncing)
    {
        _isSyncing = isSyncing;
    }
    
    private void UpdateTransitionAnimation(float fixedTime)
    {
        if (_transitionCoroutine == null)
        {
            _transitionCoroutine = StartCoroutine(TransitionAnimation(fixedTime));
        }
        else if(_transitionBreaking)
        {
            // 防止当前或即将播放的 Blend 节点权重全零导致空输出
            if (_currentNode is OneDimensionBlendAnimationNode curBlend) curBlend.EnsureAnyWeight();
            if (_nextNode    is OneDimensionBlendAnimationNode nextBlend) nextBlend.EnsureAnyWeight();
            if (_previousNode != null) _mixer.SetInputWeight(_previousNode.InputPort, 0f);
            if (_currentNode  != null) _mixer.SetInputWeight(_currentNode.InputPort, 1f);
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionAnimation(fixedTime));
            _transitionBreaking = false;
        }
    }
    
    /// <summary>
    /// 动画过渡
    /// </summary>
    private IEnumerator TransitionAnimation(float fixedTime)
    {
        if(_previousNode != null)DestroyNode(_previousNode);
        _previousNode = _currentNode;
        _currentNode = _nextNode;
        _nextNode = null;
        if (_currentNode is ICanSyncNode node)
        {
            SetSyncing(node.IsSync);
        }
        else
        {
            SetSyncing(false);
        }
        // 确保进入过渡前当前节点的内部权重不是全零，避免 Mixer 首帧输出为空
        if (_currentNode is OneDimensionBlendAnimationNode blendNode)
        {
            blendNode.EnsureAnyWeight();
        }
        if(!_currentNode.IsLinked)_currentNode.ConnectToPlayableGraph(_mixer,_graph,false);
        if (fixedTime == 0)
        {
            if (_previousNode != null) _mixer.SetInputWeight(_previousNode.InputPort, 0);
            _mixer.SetInputWeight(_currentNode.InputPort, 1);
        } 
        if (_previousNode != null)
        {
            ResetWeight(_previousNode.InputPort,_currentNode.InputPort);
        }
        (_fromPort, _toPort) = (_toPort, _fromPort);//提前交换防止终止携程之后端口未交换
        float currentWeight = 1;
        var speed = 1 / fixedTime;
        while (currentWeight > 0)
        {
            yield return null;
            currentWeight = Mathf.Clamp01(currentWeight - Time.deltaTime * Speed * speed);

            if (_previousNode != null) _mixer.SetInputWeight(_previousNode.InputPort, currentWeight);
            _mixer.SetInputWeight(_currentNode.InputPort, 1 - currentWeight);
        }
        _transitionCoroutine = null;
    }

    /// <summary>
    /// 查询当前动画节点过渡是否已完成。
    /// </summary>
    /// <returns>没有待播放节点、过渡协程已结束且当前输入权重为 1 时返回 true。</returns>
    public bool CheckIsTransitionFinished()
    {
        return _nextNode == null && _transitionCoroutine == null && Mathf.Approximately(_mixer.GetInputWeight(_fromPort), 1);
    }

    // 是否正在执行过渡
    /// <summary>当前是否正在执行动画过渡。</summary>
    public bool IsTransitioning => _transitionCoroutine != null;

    /// <summary>检查当前节点是否正在播放指定的单个动画片段。</summary>
    public bool IsPlayingSingleClip(AnimationClip clip)
    {
        return _currentNode is SingleAnimationNode single && single.GetAnimationClip() == clip;
    }
    
/// <summary>
/// 播放单个动画片段；若已有当前动画，则按指定时长过渡。
/// </summary>
/// <param name="animationClip">要播放的动画片段。</param>
/// <param name="refreshAnimation">即使目标与当前动画相同，也重新播放。</param>
/// <param name="transitionFixedTime">过渡时长（秒）。</param>
/// <param name="transitionBreaking">允许打断当前正在进行的过渡。</param>
/// <param name="defaultDeviation">新动画的初始归一化播放时间。</param>
/// <param name="speed">动画片段自身的播放速度，不与控制器速度相乘。</param>
/// <returns>创建的动画节点；重复请求被忽略时返回 null。</returns>
    public SingleAnimationNode PlaySingleAnimation(AnimationClip animationClip, bool refreshAnimation = false, float transitionFixedTime = 0.25f,bool transitionBreaking = false,float defaultDeviation = 0, float speed = 1f)
    {
        var playSpeed = speed;
        SingleAnimationNode singleAnimationNode = null;
        if(!_chainPlay) EndAnimationChainPlay();
        if (_currentNode == null)//首次播放
        {
            singleAnimationNode = ResSystem.GetOrNew<SingleAnimationNode>();
            singleAnimationNode.Init(_graph, _mixer,animationClip,playSpeed,_fromPort,defaultDeviation:defaultDeviation);
            singleAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort,1);
            _currentNode = singleAnimationNode;
        }
        else
        {
            // 避免重复入队同一动画（含等待队列）
            if (_nextNode is SingleAnimationNode pendingSingle && SameSingle(pendingSingle, animationClip, playSpeed)) return null;
            if (!refreshAnimation && _currentNode is SingleAnimationNode curSingle && SameSingle(curSingle, animationClip, playSpeed)) return null;
            //销毁掉原先占用端口的节点
            singleAnimationNode = ResSystem.GetOrNew<SingleAnimationNode>();
            singleAnimationNode.Init(_graph,_mixer,animationClip,playSpeed,_toPort,true,defaultDeviation);
            singleAnimationNode.Speed = playSpeed;
            RenewNextNode(singleAnimationNode, transitionFixedTime, transitionBreaking,refreshAnimation);
        }
        return singleAnimationNode;
    }
    
    /// <summary>播放由多个片段组成的一维线性混合动画。</summary>
    public OneDimensionBlendAnimationNode PlayBlendAnimation(List<AnimationClip> clips, float[] points, float transitionFixedTime = 0.25f,bool isSync = false,bool transitionBreaking = false,List<float> weights = null,bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var blendAnimationNode = ResSystem.GetOrNew<OneDimensionBlendAnimationNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        //如果是第一次播放则不过渡
        if (_currentNode == null)
        {
            blendAnimationNode.Init(_graph,_mixer,clips,points,playSpeed,_fromPort,isSync, weights:weights,isCached:isCached);
            blendAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = blendAnimationNode;
        }
        else
        {
            // 避免重复入队同一混合
            if (_nextNode is OneDimensionBlendAnimationNode pendingBlend && SameBlend(pendingBlend, clips, playSpeed, isSync)) return null;
            if (_currentNode is OneDimensionBlendAnimationNode curBlend && SameBlend(curBlend, clips, playSpeed, isSync)) return null;
            blendAnimationNode.Init(_graph,_mixer,clips,points,playSpeed,_toPort,isSync,true, weights:weights,isCached:isCached);
            blendAnimationNode.Speed = playSpeed;
            RenewNextNode(blendAnimationNode,transitionFixedTime,transitionBreaking);
        }
        return blendAnimationNode;
    } 
    
    /// <summary>播放两个片段组成的一维线性混合动画。</summary>
    public OneDimensionBlendAnimationNode PlayBlendAnimation(AnimationClip clip1 ,AnimationClip clip2, float[] points, float transitionFixedTime = 0.25f,bool isSync = false,bool transitionBreaking = false,List<float> weights = null,bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var blendAnimationNode = ResSystem.GetOrNew<OneDimensionBlendAnimationNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        //如果是第一次播放则不过渡
        if (_currentNode == null)
        {
            blendAnimationNode.Init(_graph,_mixer,clip1,clip2,points,playSpeed,_fromPort,isSync,isCached:isCached);
            blendAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = blendAnimationNode;
        }
        else
        {
            // 避免重复入队同一双剪辑混合
            if (_nextNode is OneDimensionBlendAnimationNode pendingBlend && SameBlend(pendingBlend, clip1, clip2, playSpeed, isSync)) return null;
            if (_currentNode is OneDimensionBlendAnimationNode curBlend && SameBlend(curBlend, clip1, clip2, playSpeed, isSync)) return null;
            blendAnimationNode.Init(_graph,_mixer,clip1,clip2,points,playSpeed,_toPort,isSync,true,isCached:isCached);
            blendAnimationNode.Speed = playSpeed;
            RenewNextNode(blendAnimationNode,transitionFixedTime,transitionBreaking);
        }
        return blendAnimationNode;
    } 


    /// <summary>播放上身单片段与下身多层二维混合动画组合。</summary>
    public UpperLowerMixerAnimationNode PlayUpperLowerMixerAnimationNode(AnimationClip upperClip, List<List<AnimationClip>> clipsList, List<List<Vector2>> pointsList, List<float> blendMixersPoints,AvatarMask avatarMask, float transitionFixedTime = 0.25f, bool isSync = false, List<List<float>> syncDeviations = null,bool transitionBreaking = false, bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var upperLowerMixerAnimationNode = ResSystem.GetOrNew<UpperLowerMixerAnimationNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        //如果是第一次播放则不过渡
        if (_currentNode == null)
        {
            upperLowerMixerAnimationNode.Init(_graph, _mixer, avatarMask, _fromPort,upperClip, clipsList, pointsList, blendMixersPoints, playSpeed, isSync, false, syncDeviations, isCached);
            upperLowerMixerAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = upperLowerMixerAnimationNode;
        }
        else
        {
            upperLowerMixerAnimationNode.Init(_graph, _mixer, avatarMask,_toPort,upperClip, clipsList, pointsList, blendMixersPoints, playSpeed, isSync, true, syncDeviations, isCached);
            upperLowerMixerAnimationNode.Speed = playSpeed;
            RenewNextNode(upperLowerMixerAnimationNode,transitionFixedTime,transitionBreaking);
        }
        return upperLowerMixerAnimationNode;
    }

    /// <summary>使用二维混合树配置播放上下身组合动画。</summary>
    public UpperLowerMixerAnimationNode PlayUpperLowerMixerAnimationNode(AnimationClip upperClip,TwoDimensionalBlendTreeData data,AvatarMask avatarMask, float transitionFixedTime = 0.25f, bool isSync = false, bool transitionBreaking = false, bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var upperLowerMixerAnimationNode = ResSystem.GetOrNew<UpperLowerMixerAnimationNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        //如果是第一次播放则不过渡
        if (_currentNode == null)
        {
            upperLowerMixerAnimationNode.Init(_graph, _mixer, avatarMask, _fromPort,upperClip, data, playSpeed, isSync, false, isCached);
            upperLowerMixerAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = upperLowerMixerAnimationNode;
        }
        else
        {
            upperLowerMixerAnimationNode.Init(_graph, _mixer, avatarMask,_toPort,upperClip, data, playSpeed, isSync, true, isCached);
            upperLowerMixerAnimationNode.Speed = playSpeed;
            RenewNextNode(upperLowerMixerAnimationNode,transitionFixedTime,transitionBreaking);
        }
        return upperLowerMixerAnimationNode;
    }
    /// <summary>播放多层二维混合树动画。</summary>
    public TwoDimensionalBlendTreeNode PlayTwoDimensionAnimationNode(List<List<AnimationClip>> clipsList, List<List<Vector2>> pointsList, List<float> blendMixersPoints, bool isSync = true, List<List<float>> syncDeviations = null, float transitionFixedTime = 0.25f,bool transitionBreaking = false,bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var twoDimensionAnimationNode = ResSystem.GetOrNew<TwoDimensionalBlendTreeNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        if (_currentNode == null)
        {
            twoDimensionAnimationNode.Init(_graph, _mixer, clipsList, pointsList, blendMixersPoints, playSpeed, _fromPort, isSync, false,syncDeviations,isCached);
            twoDimensionAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = twoDimensionAnimationNode;
        }
        else
        {
            twoDimensionAnimationNode.Init(_graph, _mixer, clipsList, pointsList, blendMixersPoints, playSpeed, _toPort, isSync, true,syncDeviations,isCached);
            twoDimensionAnimationNode.Speed = playSpeed;
            RenewNextNode(twoDimensionAnimationNode, transitionFixedTime,transitionBreaking);
        }
        return twoDimensionAnimationNode;
    }

    /// <summary>使用二维混合树配置播放动画。</summary>
    public TwoDimensionalBlendTreeNode PlayTwoDimensionAnimationNode(TwoDimensionalBlendTreeData data, bool isSync = true, float transitionFixedTime = 0.25f, bool transitionBreaking = false, bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var twoDimensionAnimationNode = ResSystem.GetOrNew<TwoDimensionalBlendTreeNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        if (_currentNode == null)
        {
            twoDimensionAnimationNode.Init(_graph, _mixer, data, playSpeed, _fromPort, isSync, false,isCached);
            twoDimensionAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = twoDimensionAnimationNode;
        }
        else
        {
            twoDimensionAnimationNode.Init(_graph, _mixer, data, playSpeed, _toPort, isSync, true,isCached);
            twoDimensionAnimationNode.Speed = playSpeed;
            RenewNextNode(twoDimensionAnimationNode, transitionFixedTime,transitionBreaking);
        }
        return twoDimensionAnimationNode;
    }

    /// <summary>使用方向线性混合树配置播放动画。</summary>
    public DirectionalLinearBlendTreeNode PlayDirectionalLinearAnimationNode(DirectionalLinearBlendTreeData data, bool isSync = true, float transitionFixedTime = 0.25f, bool transitionBreaking = false, bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var directionalLinearNode = ResSystem.GetOrNew<DirectionalLinearBlendTreeNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        if (_currentNode == null)
        {
            directionalLinearNode.Init(_graph, _mixer, data, playSpeed, _fromPort, isSync, false, isCached);
            directionalLinearNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = directionalLinearNode;
        }
        else
        {
            directionalLinearNode.Init(_graph, _mixer, data, playSpeed, _toPort, isSync, true, isCached);
            directionalLinearNode.Speed = playSpeed;
            RenewNextNode(directionalLinearNode, transitionFixedTime, transitionBreaking);
        }

        return directionalLinearNode;
    }

    /// <summary>
    /// 播放二维外层、一维内层混合树动画节点。
    /// </summary>
    /// <param name="data">二维外层、一维内层混合树配置。</param>
    /// <param name="isSync">是否同步混合片段的播放相位。</param>
    /// <param name="transitionFixedTime">过渡时长（秒）。</param>
    /// <param name="transitionBreaking">是否允许打断当前过渡。</param>
    /// <param name="isCached">是否将节点缓存以供复用。</param>
    /// <returns>创建的混合树节点。</returns>
    public TwoDOuterOneDInnerBlendTreeNode PlayTwoDOuterOneDInnerAnimationNode(TwoDOuterOneDInnerBlendTreeData data, bool isSync = true, float transitionFixedTime = 0.25f, bool transitionBreaking = false, bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var node = ResSystem.GetOrNew<TwoDOuterOneDInnerBlendTreeNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        if (_currentNode == null)
        {
            node.Init(_graph, _mixer, data, playSpeed, _fromPort, isSync, false, isCached);
            node.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = node;
        }
        else
        {
            node.Init(_graph, _mixer, data, playSpeed, _toPort, isSync, true, isCached);
            node.Speed = playSpeed;
            RenewNextNode(node, transitionFixedTime, transitionBreaking);
        }

        return node;
    }

    /// <summary>
    /// 使用方向线性混合树播放上下分层动画。
    /// </summary>
    /// <param name="upperClip">上层动画片段。</param>
    /// <param name="avatarMask">指定上层动画影响的骨骼范围。</param>
    /// <param name="data">下层方向线性混合树配置。</param>
    /// <param name="transitionFixedTime">过渡时长（秒）。</param>
    /// <param name="isSync">是否同步混合片段的播放相位。</param>
    /// <param name="transitionBreaking">是否允许打断当前过渡。</param>
    /// <param name="isCached">是否将节点缓存以供复用。</param>
    /// <returns>创建的上下分层动画节点。</returns>
    public UpperLowerMixerAnimationNode PlayUpperLowerMixerAnimationNode(AnimationClip upperClip, DirectionalLinearBlendTreeData data, AvatarMask avatarMask, float transitionFixedTime = 0.25f, bool isSync = false, bool transitionBreaking = false, bool isCached = false, float speed = 1f)
    {
        var playSpeed = speed;
        var upperLowerMixerAnimationNode = ResSystem.GetOrNew<UpperLowerMixerAnimationNode>();
        if(!_chainPlay) EndAnimationChainPlay();
        if (_currentNode == null)
        {
            upperLowerMixerAnimationNode.Init(_graph, _mixer, avatarMask, _fromPort, upperClip, data, playSpeed, isSync, false, isCached);
            upperLowerMixerAnimationNode.Speed = playSpeed;
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = upperLowerMixerAnimationNode;
        }
        else
        {
            upperLowerMixerAnimationNode.Init(_graph, _mixer, avatarMask, _toPort, upperClip, data, playSpeed, isSync, true, isCached);
            upperLowerMixerAnimationNode.Speed = playSpeed;
            RenewNextNode(upperLowerMixerAnimationNode, transitionFixedTime, transitionBreaking);
        }

        return upperLowerMixerAnimationNode;
    }
    /// <summary>播放已缓存的动画节点。</summary>
    /// <param name="speed">动画片段自身的播放速度，不与控制器速度相乘。</param>
    public void PlayCachedAnimationNode(AnimationNodeBase blendNode, float transitionFixedTime = 0.25f,bool transitionBreaking = false,float speed = 1f)
    {
        blendNode.Speed = speed;
        blendNode.SetClipSpeed(1f);
        if (_currentNode == null)
        {
            blendNode.ConnectToPlayableGraph(_mixer, _graph,false);
            _mixer.SetInputWeight(_fromPort, 1);
            _currentNode = blendNode;
        }
        else
        {
            blendNode.ConnectToPlayableGraph(_mixer, _graph,true);
            blendNode.SetInputPort(_toPort);
            RenewNextNode(blendNode, transitionFixedTime,transitionBreaking);
        }
    }
    
    /// <summary>将节点设为下一个播放节点，并配置过渡时长及打断策略。</summary>
    private void RenewNextNode(AnimationNodeBase node, float nextTransitionTime, bool transitionBreaking = false,bool isRefreshing = false)
    {
        if (LockTransition) return;
        // 若下一个节点与当前下一个或当前节点等价，则不重复入队
        if (_nextNode != null && IsSameNode(_nextNode, node) && !isRefreshing) return;
        if (_currentNode != null && IsSameNode(_currentNode, node) && !isRefreshing) return;
        DestroyNode(_nextNode);
        _nextNode = node;
        _nextTransitionTime = nextTransitionTime;
        _transitionBreaking = transitionBreaking;
    }

    #region 去重检验

    private bool IsSameNode(AnimationNodeBase a, AnimationNodeBase b)
    {
        if (a == null || b == null) return false;
        if (a.GetType() != b.GetType()) return false;

        switch (a)
        {
            case SingleAnimationNode sa when b is SingleAnimationNode sb:
                return SameSingle(sa, sb.GetAnimationClip(), sb.Speed);
            case OneDimensionBlendAnimationNode ba when b is OneDimensionBlendAnimationNode bb:
                return SameBlend(ba, bb.GetClips().ToList(), bb.Speed, bb.IsSync);
            case UpperLowerMixerAnimationNode:
                return false; // 暂不去重该类型，避免访问私有数据
            default:
                return false;
        }
    }

    // 判断单剪辑节点是否等价（用于去重播放请求）
    private bool SameSingle(SingleAnimationNode node, AnimationClip clip, float speed)
    {
        return node.GetAnimationClip() == clip && Mathf.Approximately(node.Speed, speed);
    }

    // 判断 Blend 节点（列表版）是否等价
    private bool SameBlend(OneDimensionBlendAnimationNode node, IList<AnimationClip> clips, float speed, bool isSync)
    {
        if (!Mathf.Approximately(node.Speed, speed)) return false;
        if (node.IsSync != isSync) return false;
        var nodeClips = node.GetClips();
        if (nodeClips.Count != clips.Count) return false;
        for (var i = 0; i < clips.Count; i++)
        {
            if (nodeClips[i] != clips[i]) return false;
        }
        return true;
    }

    // 判断 Blend 节点（双剪辑版）是否等价
    private bool SameBlend(OneDimensionBlendAnimationNode node, AnimationClip clip1, AnimationClip clip2, float speed, bool isSync)
    {
        var nodeClips = node.GetClips();
        if (nodeClips.Count != 2) return false;
        if (nodeClips[0] != clip1 || nodeClips[1] != clip2) return false;
        return Mathf.Approximately(node.Speed, speed) && node.IsSync == isSync;
    }

    #endregion
    
    
    private void ResetWeight(int fromIndex, int toIndex)
    {
        _mixer.SetInputWeight(fromIndex, 1);
        _mixer.SetInputWeight(toIndex,0);
    }
    
   
    
    
    private void DestroyGraph()
    {
        _previousNode?.PushPool(true);
        _currentNode?.PushPool(true);
        _nextNode?.PushPool(true);
        if (_graphInitialized && _graph.IsValid())
        {
            _graph.Destroy();
        }
        _graphInitialized = false;
        _transitionCoroutine = null;
        _fromPort = 0;
        _toPort = 1;
    }

    #region RootMotion

   
    private Action<Vector3, Quaternion> _rootMotionAction;
    private Vector3 _lastAnimationDeltaPosition;
    
    /// <summary>
    /// 统一接收 Animator 的根运动，并按同步设置限幅后转发给注册的回调。
    /// </summary>
    private void OnAnimatorMove()
    {
        var deltaPosition = animator.deltaPosition;
        if (_isSyncing)
        {
            var magnitude = Mathf.Clamp(deltaPosition.magnitude, (_syncClampSpeed * Time.deltaTime * _syncClamp.x),
                (_syncClampSpeed * Time.deltaTime *  _syncClamp.y));
            deltaPosition = deltaPosition.normalized * magnitude;
         
        }
        _rootMotionAction?.Invoke(deltaPosition, animator.deltaRotation);
    }

    /// <summary>注册根运动回调，用于接收每帧的位置增量和旋转增量。</summary>
    public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
    {
        _rootMotionAction = rootMotionAction;
    }

    /// <summary>清除已注册的根运动回调。</summary>
    public void ClearRootMotionAction()
    {
        _rootMotionAction = null;
    }
    #endregion

    #region 动画事件

    private enum AnimationEventType
    {
        None,
        Int,
        Float,
        Bool,
        String,
        Object
    }
    
    private class AnimationDelegate
    {
       public AnimationEventType EventType { get; private set; }
       public MulticastDelegate Callback { get; private set; }
       
       public AnimationDelegate(AnimationEventType eventType, MulticastDelegate callback)
       {
              EventType = eventType;
              Callback = callback;
       }

    }
    
    private Dictionary<string,List<AnimationDelegate>> _eventDic = new();

    #region 事件触发逻辑

    
    /// <summary>
    /// 根据动画事件携带的事件名和参数类型分发已注册的回调。
    /// </summary>
    /// <param name="animationEvent"></param>
    private void AnimationEvent(AnimationEvent animationEvent)
    {
        var strings = animationEvent.stringParameter.Split('/');
        if (_eventDic.TryGetValue(strings[0], out var delegates))
        {
            var delegatesCopy = new List<AnimationDelegate>(delegates);
            foreach (var animationDelegate in delegatesCopy)
            {
                switch (animationDelegate.EventType)
                {
                    case AnimationEventType.Int:
                        ((Action<int>)animationDelegate.Callback)?.Invoke(animationEvent.intParameter);
                        break;
                    case AnimationEventType.Float:
                        ((Action<float>)animationDelegate.Callback)?.Invoke(animationEvent.floatParameter);
                        break;
                    case AnimationEventType.Bool:
                        ((Action<bool>)animationDelegate.Callback)?.Invoke(animationEvent.intParameter != 0);
                        break;
                    case AnimationEventType.String:
                        if(strings.Length < 2) continue;
                        ((Action<string>)animationDelegate.Callback)?.Invoke(strings[1]);
                        break;
                    case AnimationEventType.Object:
                        ((Action<UnityEngine.Object>)animationDelegate.Callback)?.Invoke(animationEvent.objectReferenceParameter);
                        break;
                    case AnimationEventType.None:
                        ((Action)animationDelegate.Callback)?.Invoke();
                        break;
                }
            }
        }
    }

    #endregion

    #region 事件添加

    /// <summary>注册无参数动画事件回调。</summary>
    public void AddAnimationEvent(string eventName, Action addAction)
    {
        var newDelegate = new AnimationDelegate(AnimationEventType.None, addAction);
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
            delegates.Add(newDelegate);   
        }
        else
        {
            _eventDic.Add(eventName, new List<AnimationDelegate> { newDelegate });
        }
    }

    /// <summary>注册浮点参数动画事件回调。</summary>
    public void AddAnimationEvent(string eventName, Action<float> addAction)
    {
        var newDelegate = new AnimationDelegate(AnimationEventType.Float, addAction);
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
            delegates.Add(newDelegate);   
        }
        else
        {
            _eventDic.Add(eventName, new List<AnimationDelegate> { newDelegate });
        }
    }
    
    /// <summary>注册布尔参数动画事件回调。</summary>
    public void AddAnimationEvent(string eventName, Action<bool> addAction)
    {
        var newDelegate = new AnimationDelegate(AnimationEventType.Bool, addAction);
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
            delegates.Add(newDelegate);   
        }
        else
        {
            _eventDic.Add(eventName, new List<AnimationDelegate> { newDelegate });
        }
    }
    
    /// <summary>注册整数参数动画事件回调。</summary>
    public void AddAnimationEvent(string eventName, Action<int> addAction)
    {
        var newDelegate = new AnimationDelegate(AnimationEventType.Int, addAction);
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
            delegates.Add(newDelegate);   
        }
        else
        {
            _eventDic.Add(eventName, new List<AnimationDelegate> { newDelegate });
        }
    }
    
    /// <summary>注册字符串参数动画事件回调；事件字符串参数以斜杠分隔事件名和参数值。</summary>
    public void AddAnimationEvent(string eventName, Action<string> addAction)
    {
        var newDelegate = new AnimationDelegate(AnimationEventType.String, addAction);
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
            delegates.Add(newDelegate);   
        }
        else
        {
            _eventDic.Add(eventName, new List<AnimationDelegate> { newDelegate });
        }
    }

    #endregion


    #region 事件移除

    /// <summary>移除指定名称对应的所有动画事件回调。</summary>
    public void RemoveAnimationEvent(string eventName)
    {
        _eventDic.Remove(eventName);
    }

    /// <summary>移除指定名称下匹配的无参数回调。</summary>
    public void RemoveAnimationEvent(string eventName, Action removeAction)
    {
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
           var delegateToRemove = delegates.FirstOrDefault(d => d.EventType == AnimationEventType.None && d.Callback.Equals(removeAction));
            if (delegateToRemove != null)
            { 
                delegates.Remove(delegateToRemove);
            }
        }
    }
    
    /// <summary>移除指定名称下匹配的浮点参数回调。</summary>
    public void RemoveAnimationEvent(string eventName, Action<float> removeAction)
    {
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
           var delegateToRemove = delegates.FirstOrDefault(d => d.EventType == AnimationEventType.Float && d.Callback.Equals(removeAction));
            if (delegateToRemove != null)
            { 
                delegates.Remove(delegateToRemove);
            }
        }
    }
    
    /// <summary>移除指定名称下匹配的布尔参数回调。</summary>
    public void RemoveAnimationEvent(string eventName, Action<bool> removeAction)
    {
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
           var delegateToRemove = delegates.FirstOrDefault(d => d.EventType == AnimationEventType.Bool && d.Callback.Equals(removeAction));
            if (delegateToRemove != null)
            { 
                delegates.Remove(delegateToRemove);
            }
        }
    }
    
    /// <summary>移除指定名称下匹配的整数参数回调。</summary>
    public void RemoveAnimationEvent(string eventName, Action<int> removeAction)
    {
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
           var delegateToRemove = delegates.FirstOrDefault(d => d.EventType == AnimationEventType.Int && d.Callback.Equals(removeAction));
            if (delegateToRemove != null)
            { 
                delegates.Remove(delegateToRemove);
            }
        }
    }
    
    /// <summary>移除指定名称下匹配的字符串参数回调。</summary>
    public void RemoveAnimationEvent(string eventName, Action<string> removeAction)
    {
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
           var delegateToRemove = delegates.FirstOrDefault(d => d.EventType == AnimationEventType.String && d.Callback.Equals(removeAction));
            if (delegateToRemove != null)
            { 
                delegates.Remove(delegateToRemove);
            }
        }
    }
    
    /// <summary>移除指定名称下匹配的 Unity 对象参数回调。</summary>
    public void RemoveAnimationEvent(string eventName, Action<UnityEngine.Object> removeAction)
    {
        if (_eventDic.TryGetValue(eventName, out var delegates))
        {
           var delegateToRemove = delegates.FirstOrDefault(d => d.EventType == AnimationEventType.Object && d.Callback.Equals(removeAction));
            if (delegateToRemove != null)
            { 
                delegates.Remove(delegateToRemove);
            }
        }
    }
    

    #endregion

    /// <summary>清除所有动画事件回调。</summary>
    public void CleanAllActionEvent()
    {
        _eventDic.Clear();   
    }
    #endregion
    
    
#region 动画播放链
public AnimationChain CreateAnimationChain()
{
    return ResSystem.GetOrNew<AnimationChain>().Init(this);
}

public void PlayAnimationChain(AnimationChain animationChain,int startIndex = 0)
{
    _curAnimationChain = animationChain;
    _curAnimationChain.Play(startIndex);
}

private void EndAnimationChainPlay()
{
    _curAnimationChain = null;
}

/// <summary>
/// 这段数据结构动画播放器不负责其维护逻辑需要自行处理销毁流程
/// </summary>
 public class AnimationChain
 {
     private AnimationChainNode _head;
     private AnimationChainNode _tail;
     private AnimationChainNode _curNode;
     private Animation_Controller _controller;
     internal AnimationChain Init(Animation_Controller controller)
     {
         _controller = controller;
         return this;
     }
     
     internal void Play(int startIndex)
     {
         _curNode = _head;
         for (int i = 0; i < startIndex; i++)
         {
             if(_curNode.NextNode == null) break;
             _curNode = _curNode.NextNode;
         }
         _curNode.Play(_controller);
     }

     internal void UpdateChain()
     {
         if(_curNode == null) return;
         _curNode.Update();
         if(_curNode.CheckEndCondition())
         {
             _curNode.End();
             var nextNode = _curNode.NextNode;
             _curNode = nextNode;
             if(_curNode == null)
             {
                 _controller.EndAnimationChainPlay();
                 return;
             }
             _curNode.Play(_controller);
         }
     }

     internal void Destroy()
     {
         var node = _head;
         while (node != null)
         {
             var nextNode = node.NextNode;
             node.Destroy();
             node = nextNode;
         }
         _head = null;
         _curNode = null;
         _controller = null;
         this.ObjectPushPool();
     }
     


     public void ConnectNextNode(AnimationClip clip, float transitionFixedTime = 0.25f, bool transitionBreaking = false, float speed = 1f,Func<float,bool> endCondition = null,float endNormalizedTime = 1f,Action<Vector3,Quaternion> rootMotion = null,Action<SingleAnimationNode> playAction = null,Action<SingleAnimationNode> updateAction = null,Action<SingleAnimationNode> endAction = null)
     {
         var nextNode = ResSystem.GetOrNew<SingleAnimationChainNode>();
         nextNode.PlayAction = playAction;
         nextNode.UpdateAction = updateAction;
         nextNode.EndAction = endAction;
         nextNode.Clip = clip;
         nextNode.TransitionFixedTime = transitionFixedTime;
         nextNode.TransitionBreaking = transitionBreaking;
         nextNode.EndCondition = endCondition;
         nextNode.Speed = speed;
         nextNode.EndNormalizedTime = endNormalizedTime;
         nextNode.RootMotion = rootMotion;
         if(_head == null)
         {
             _head = nextNode;
             _curNode = nextNode;
         }
         else if(_tail != null)
         {
             _tail.NextNode = nextNode;
         }
         _tail = nextNode;
     }
     
     public void ConnectNextNode(DirectionalLinearBlendTreeData data, bool isSync = true, float transitionFixedTime = 0.25f, bool transitionBreaking = false, float speed = 1f, Func<float,bool> endCondition = null, float endNormalizedTime = 1f,     Action<Vector3,Quaternion> rootMotion = null,Action<DirectionalLinearBlendTreeNode> playAction = null,Action<DirectionalLinearBlendTreeNode> updateAction = null,Action<DirectionalLinearBlendTreeNode> endAction = null)
     {
         var nextNode = ResSystem.GetOrNew<DirectionalLinearAnimationChainNode>();
         nextNode.PlayAction = playAction;
         nextNode.UpdateAction = updateAction;
         nextNode.EndAction = endAction;
         nextNode.Data = data;
         nextNode.IsSync = isSync;
         nextNode.TransitionFixedTime = transitionFixedTime;
         nextNode.TransitionBreaking = transitionBreaking;
         nextNode.EndCondition = endCondition;
         nextNode.Speed = speed;
         nextNode.EndNormalizedTime = endNormalizedTime;
         nextNode.RootMotion = rootMotion;
         if(_head == null)
         {
             _head = nextNode;
             _curNode = nextNode;
         }
         else if(_tail != null)
         {
             _tail.NextNode = nextNode;
         }
         _tail = nextNode;
     }
     
     public void ConnectNextNode(TwoDimensionalBlendTreeData data, bool isSync = true, float transitionFixedTime = 0.25f, bool transitionBreaking = false, float speed = 1f, Func<float,bool> endCondition = null, float endNormalizedTime = 1f,     Action<Vector3,Quaternion> rootMotion = null,Action<TwoDimensionalBlendTreeNode> playAction = null,Action<TwoDimensionalBlendTreeNode> updateAction = null,Action<TwoDimensionalBlendTreeNode> endAction = null)
     {
         var nextNode = ResSystem.GetOrNew<TwoDimensionalAnimationChainNode>();
         nextNode.PlayAction = playAction;
         nextNode.UpdateAction = updateAction;
         nextNode.EndAction = endAction;
         nextNode.Data = data;
         nextNode.IsSync = isSync;
         nextNode.TransitionFixedTime = transitionFixedTime;
         nextNode.TransitionBreaking = transitionBreaking;
         nextNode.EndCondition = endCondition;
         nextNode.Speed = speed;
         nextNode.EndNormalizedTime = endNormalizedTime;
         nextNode.RootMotion = rootMotion;
         if(_head == null)
         {
             _head = nextNode;
             _curNode = nextNode;
         }
         else if(_tail != null)
         {
             _tail.NextNode = nextNode;
         }
         _tail = nextNode;
     }
     
     public void ConnectNextNode(TwoDOuterOneDInnerBlendTreeData data, bool isSync = true, float transitionFixedTime = 0.25f, bool transitionBreaking = false, float speed = 1f, Func<float,bool> endCondition = null, float endNormalizedTime = 1f,     Action<Vector3,Quaternion> rootMotion = null,Action<TwoDOuterOneDInnerBlendTreeNode> playAction = null,Action<TwoDOuterOneDInnerBlendTreeNode> updateAction = null,Action<TwoDOuterOneDInnerBlendTreeNode> endAction = null)
     {
         var nextNode = ResSystem.GetOrNew<TowDOuterOneDInnerAnimationChainNode>();
         nextNode.PlayAction = playAction;
         nextNode.UpdateAction = updateAction;
         nextNode.EndAction = endAction;
         nextNode.Data = data;
         nextNode.IsSync = isSync;
         nextNode.TransitionFixedTime = transitionFixedTime;
         nextNode.TransitionBreaking = transitionBreaking;
         nextNode.EndCondition = endCondition;
         nextNode.Speed = speed;
         nextNode.EndNormalizedTime = endNormalizedTime;
         nextNode.RootMotion = rootMotion;
         if(_head == null)
         {
             _head = nextNode;
             _curNode = nextNode;
         }
         else if(_tail != null)
         {
             _tail.NextNode = nextNode;
         }
         _tail = nextNode;
     }
     
     private abstract class AnimationChainNode
     {
         public float TransitionFixedTime = 0.25f;
         public bool TransitionBreaking = false;
         public bool IsCached = false;
         public float Speed = 1f;
         public Func<float,bool> EndCondition;
         public AnimationChainNode NextNode;
         public float EndNormalizedTime;
         public Action<Vector3,Quaternion> RootMotion;
         public abstract void Play(Animation_Controller controller);
         public abstract void Destroy();
         public abstract bool CheckEndCondition();
         public abstract void Update();
         public abstract void End();
     }

     private abstract class AnimationChainNode<T> : AnimationChainNode where T : AnimationNodeBase
     {
         public T AnimationNode;
         public Action<T> PlayAction;
         public Action<T> UpdateAction;
         public Action<T> EndAction;

         public override void Update()
         {
             if (AnimationNode != null) UpdateAction?.Invoke(AnimationNode);
         }

         public override void End()
         {
             if (AnimationNode != null) EndAction?.Invoke(AnimationNode);
         }

         protected void InvokePlayAction()
         {
             if (AnimationNode != null) PlayAction?.Invoke(AnimationNode);
         }

         protected void ClearNode()
         {
             PlayAction = null;
             UpdateAction = null;
             EndAction = null;
             AnimationNode = null;
             NextNode = null;
             RootMotion = null;
         }
     }

     private class SingleAnimationChainNode : AnimationChainNode<SingleAnimationNode>
     {
         public AnimationClip Clip;
         public override void Play(Animation_Controller controller)
         {
             controller._chainPlay = true;
             controller.SetRootMotionAction(RootMotion);
             AnimationNode = controller.PlaySingleAnimation(Clip, false, TransitionFixedTime, TransitionBreaking, 0, Speed);
             controller._chainPlay = false;
             InvokePlayAction();
         }

         public override void Destroy()
         {
             PoolSystem.PushObject(this);
             ClearNode();
             Clip = null;
             EndCondition = null;
         }

         public override bool CheckEndCondition()
         {
             if (EndCondition == null)
             {
                 return AnimationNode.NormalizedTime >= EndNormalizedTime;
             }
             else
             {
                 return EndCondition.Invoke(AnimationNode.NormalizedTime);
             }
         }
     }

     private class DirectionalLinearAnimationChainNode : AnimationChainNode<DirectionalLinearBlendTreeNode>
     {
         public DirectionalLinearBlendTreeData Data;
         public bool IsSync;

         public override void Play(Animation_Controller controller)
         {
             controller._chainPlay = true;
             controller.SetRootMotionAction(RootMotion);
             AnimationNode = controller.PlayDirectionalLinearAnimationNode(Data, IsSync, TransitionFixedTime, TransitionBreaking, false, Speed);
             controller._chainPlay = false;
             InvokePlayAction();
         }

         public override void Destroy()
         {
             PoolSystem.PushObject(this);
             ClearNode();
             Data = null; 
             EndCondition = null;
         }

         public override bool CheckEndCondition()
         {
             if (EndCondition == null) 
             {
                 return AnimationNode.GetMasterClipTime() >= EndNormalizedTime;
             }
             else
             {
                 return EndCondition.Invoke((float)AnimationNode.GetMasterClipTime());
             }
         }
     }

     private class TwoDimensionalAnimationChainNode : AnimationChainNode<TwoDimensionalBlendTreeNode>
     {
         public TwoDimensionalBlendTreeData Data;
         public bool IsSync;

         public override void Play(Animation_Controller controller)
         {
             controller._chainPlay = true;
             controller.SetRootMotionAction(RootMotion);
             AnimationNode = controller.PlayTwoDimensionAnimationNode(Data, IsSync, TransitionFixedTime, TransitionBreaking, false, Speed);
             controller._chainPlay = false; 
             InvokePlayAction();
         }

         public override void Destroy()
         {
             PoolSystem.PushObject(this);
             ClearNode();
             Data = null;
             EndCondition = null;
         }

         public override bool CheckEndCondition()
         {
             if (EndCondition == null)
             {
                 return AnimationNode.GetMasterClipTime() >= EndNormalizedTime;
             }
             else
             {
                 return EndCondition.Invoke((float)AnimationNode.GetMasterClipTime());  
             }
         }
     }

     private class TowDOuterOneDInnerAnimationChainNode : AnimationChainNode<TwoDOuterOneDInnerBlendTreeNode>
     {
         public TwoDOuterOneDInnerBlendTreeData Data;
         public bool IsSync;

         public override void Play(Animation_Controller controller)
         {
             controller._chainPlay = true;
             controller.SetRootMotionAction(RootMotion);
             AnimationNode = controller.PlayTwoDOuterOneDInnerAnimationNode(Data, IsSync, TransitionFixedTime, TransitionBreaking, false, Speed);
             controller._chainPlay = false;
             InvokePlayAction();
         }

         public override void Destroy()
         {
             PoolSystem.PushObject(this);
             ClearNode();
             Data = null;
             EndCondition = null;
         }

         public override bool CheckEndCondition()
         {
             if (EndCondition == null)
             {
                 return AnimationNode.GetMasterClipTime() >= EndNormalizedTime;
             }
             else
             {
                 return EndCondition.Invoke((float)AnimationNode.GetMasterClipTime());
             }
         }
     }
 }

 #endregion
}


 



