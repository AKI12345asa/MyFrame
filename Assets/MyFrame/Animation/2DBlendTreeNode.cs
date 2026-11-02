using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TwoDimensionalBlendTreeNode:BlendAnimationNodeBase
{
    private  AnimationMixerPlayable _totalMixer;
    protected bool _isSync;
    public override bool IsSync => _isSync;
    private float _playSpeed;
    private List<List<AnimationClipPlayable>> _blendClipPlayableLists;
    public List<List<AnimationClipPlayable>> BlendClipPlayableLists => _blendClipPlayableLists;
    private List<AnimationMixerPlayable> _blendMixerPlayableList;
    private List<TwoDimensionalTransitionContainer> _dimensionalTransitionContainers;
    private List<float> _blendNodePoints;
    private List<List<float>> _syncDeviations;
    private readonly Dictionary<int, Vector2> _smoothCurrentPoints = new Dictionary<int, Vector2>();
    private readonly Dictionary<int, Vector2> _smoothVelocities = new Dictionary<int, Vector2>();
    private float _smoothLinearX;
    private float _smoothLinearVelocity;
    private bool _hasSmoothLinearX;
    private int _masterMixerIndex = -1;
    private int _masterClipIndex = -1;
    // 子混合器局部主片段稳定态，避免临界权重时频繁切换。
    private List<int> _localMasterClipIndices;
    private List<float> _localMasterSwitchTimes;
    // 主片段切换防抖：候选权重需要领先当前主片段一定差值，且满足最短保持时间。
    private float _masterSwitchMinWeightDelta = 0.08f;
    private float _masterMinHoldSeconds = 0.15f;
    private float _lastMasterSwitchTime = -999f;
    private float _localMasterSwitchMinWeightDelta = 0.08f;
    private float _localMasterMinHoldSeconds = 0.12f;
    public override float Speed
    {
        get => _playSpeed;
        set => _playSpeed = value;
    }

    protected override void ConnectToPlayableGraph<V>(Playable connector, PlayableGraph graph, V outputMixer,
        int sourceOutPutPort, bool isTransitioning)
    {
        base.ConnectToPlayableGraph(connector, graph, outputMixer, sourceOutPutPort, isTransitioning);
        if (isTransitioning) return;
        if (_isSync) this.AddUpdate(SyncPhase);
    }
    
    public override void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)
    {
        if (isTransitioning) return;
        ConnectToPlayableGraph(_totalMixer, graph, outputMixer, 0, false);
    }

    public override void SetClipSpeed(float speed)
    {
        foreach(var clips in _blendClipPlayableLists)
        {
            foreach (var clip in clips)
            {
                clip.SetSpeed(speed * Speed);
            }
        }
    }
    
    public  void Init<V>(PlayableGraph graph, V outputMixer, List<List<AnimationClip>> clipsList,List<List<Vector2>> pointsList, List<float> blendMixersPoints,float speed, int inputPort, bool isSync = false, bool isTransitioning = false,List<List<float>> syncDeviations = null,bool isCached = false) where V : struct, IPlayable
    {
        ValidateInputLists(clipsList, pointsList, blendMixersPoints, syncDeviations, isSync);
        _isSync = isSync;
        _playSpeed = speed;
        InputPort =  inputPort;
        IsCached = isCached;
        _totalMixer = AnimationMixerPlayable.Create(graph,clipsList.Count);
        _blendClipPlayableLists = new List<List<AnimationClipPlayable>>(clipsList.Count);
        _blendMixerPlayableList = new List<AnimationMixerPlayable>(clipsList.Count);
        _syncDeviations = syncDeviations;
        _blendNodePoints = blendMixersPoints;
        _dimensionalTransitionContainers = new List<TwoDimensionalTransitionContainer>(clipsList.Count);
        _localMasterClipIndices = new List<int>(clipsList.Count);
        _localMasterSwitchTimes = new List<float>(clipsList.Count);
        for (int i = 0; i < clipsList.Count; i++)
        {
            ConnectClipsToMixer(clipsList[i],pointsList[i],graph,i);
            _localMasterClipIndices.Add(-1);
            _localMasterSwitchTimes.Add(-999f);
        }
        if(_blendMixerPlayableList.Count > 1)
        {
            SetBlendAnimationAsLinerPoint(0);
        }
        else
        {
            _totalMixer.SetInputWeight(0, 1);
        }
        SyncWhenMainClipChange();
        ConnectToPlayableGraph(_totalMixer, graph, outputMixer, 0, isTransitioning);
    }

    public void Init<V>(PlayableGraph graph, V outputMixer, TwoDimensionalBlendTreeData data,float speed, int inputPort, bool isSync = false, bool isTransitioning = false, bool isCached = false) where V : struct, IPlayable
    {
        var clipsList =  new List<List<AnimationClip>>();
        var pointsList = new List<List<Vector2>>();
        var syncDeviationsList = new List<List<float>>();
        var mixerPoints = new List<float>();
        foreach (var dataPoints in data.PointsList)
        {
            var clips = new List<AnimationClip>();
            var points = new List<Vector2>();
            var syncDeviations = new List<float>();
            foreach (var dataPoint in dataPoints.points)
            {
                clips.Add(dataPoint.clip);
                points.Add(dataPoint.point);
                syncDeviations.Add(dataPoint.syncDeviation);
            }
            clipsList.Add(clips);
            pointsList.Add(points);
            syncDeviationsList.Add(syncDeviations);
            mixerPoints.Add(dataPoints.mixerPoint);
        }
        Init(graph, outputMixer, clipsList, pointsList,mixerPoints, speed, inputPort, isSync, isTransitioning, syncDeviationsList,isCached);
    }
    
    private static void ValidateInputLists(
        List<List<AnimationClip>> clipsList,
        List<List<Vector2>> pointsList,
        List<float> blendMixersPoints,
        List<List<float>> syncDeviations,
        bool isSync)
    {
        if (clipsList == null) throw new ArgumentNullException(nameof(clipsList));
        if (pointsList == null) throw new ArgumentNullException(nameof(pointsList));
        if (blendMixersPoints == null) throw new ArgumentNullException(nameof(blendMixersPoints));
        int outerCount = clipsList.Count;
        if (outerCount == 0) throw new ArgumentException("clipsList can not be empty.", nameof(clipsList));

        if (pointsList.Count != outerCount)
        {
            throw new ArgumentException($"Outer count mismatch: clipsList={outerCount}, pointsList={pointsList.Count}.");
        }

        if (blendMixersPoints.Count != outerCount)
        {
            throw new ArgumentException($"Outer count mismatch: clipsList={outerCount}, blendMixersPoints={blendMixersPoints.Count}.");
        }

        if (isSync && syncDeviations == null)
        {
            throw new ArgumentNullException(nameof(syncDeviations), "syncDeviations is required when isSync is true.");
        }

        if (syncDeviations != null && syncDeviations.Count != outerCount)
        {
            throw new ArgumentException($"Outer count mismatch: clipsList={outerCount}, syncDeviations={syncDeviations.Count}.");
        }

        for (int i = 0; i < outerCount; i++)
        {
            if (clipsList[i] == null)
            {
                throw new ArgumentException($"clipsList[{i}] is null.");
            }

            if (pointsList[i] == null)
            {
                throw new ArgumentException($"pointsList[{i}] is null.");
            }

            int clipCount = clipsList[i].Count;
            int pointCount = pointsList[i].Count;
            if (clipCount != pointCount)
            {
                throw new ArgumentException($"Inner count mismatch at index {i}: clips={clipCount}, points={pointCount}.");
            }

            if (syncDeviations == null) continue;

            if (syncDeviations[i] == null)
            {
                throw new ArgumentException($"syncDeviations[{i}] is null.");
            }

            int deviationCount = syncDeviations[i].Count;
            if (deviationCount != clipCount)
            {
                throw new ArgumentException($"Inner count mismatch at index {i}: clips={clipCount}, syncDeviations={deviationCount}.");
            }
        }
    }

    public override void SetBlendAnimationAsLinerPoint(float xValue)
    {
        if (_blendNodePoints == null || _blendNodePoints.Count < 2)
        {
            throw new InvalidOperationException("blendMixersPoints 至少需要 2 个点。");
        }
        
        xValue = Mathf.Clamp(xValue, _blendNodePoints.First(), _blendNodePoints.Last());

        var ceilIndex = -1;
        for (var i = 0; i < _blendNodePoints.Count; i++)
        {
            if (_blendNodePoints[i] >= xValue)
            {
                ceilIndex = i;
                break;
            }
        }

        if (ceilIndex < 0)
        {
            ceilIndex = _blendNodePoints.Count - 1;
        }

        var ceilPoint = _blendNodePoints[ceilIndex];
        if (ceilIndex == 0)
        {
            ceilIndex = 1;
            ceilPoint = _blendNodePoints[ceilIndex];
        }

        var floorPoint = _blendNodePoints[ceilIndex - 1];
        var floorIndex = ceilIndex - 1;
        if (Mathf.Approximately(floorIndex, ceilIndex))
        {
            floorIndex = ceilIndex - 1;
        }

        for (var i = 0; i < _blendNodePoints.Count; i++)
        {
            if (i == floorIndex || i == ceilIndex) continue;
            _totalMixer.SetInputWeight(i, 0);
        }

        var dis = ceilPoint - floorPoint;
        if (Mathf.Abs(dis) < 1e-6f)
        {
            _totalMixer.SetInputWeight(floorIndex, 1f);
            _totalMixer.SetInputWeight(ceilIndex, 0f);
            SyncWhenMainClipChange();
            return;
        }

        _totalMixer.SetInputWeight(floorIndex, (ceilPoint - xValue) / dis);
        _totalMixer.SetInputWeight(ceilIndex, (xValue - floorPoint) / dis);
        SyncWhenMainClipChange();
    }

    public void SmoothSetBlendAnimationAsLinerPoint(float targetX, float dampTime)
    {
        if (!_hasSmoothLinearX || dampTime <= 0f)
        {
            _smoothLinearX = targetX;
            _smoothLinearVelocity = 0f;
            _hasSmoothLinearX = true;
        }
        else
        {
            _smoothLinearX = Mathf.SmoothDamp(_smoothLinearX, targetX, ref _smoothLinearVelocity, dampTime);
        }
        SetBlendAnimationAsLinerPoint(_smoothLinearX);
    }

    public void SetClipsWeights(int index,Vector2 point)
    {
        if (index >= _blendNodePoints.Count)
        {
            throw new IndexOutOfRangeException("索引越界");
        }
        var weights = TwoDimensionalTransitionContainer.GetWeights(_dimensionalTransitionContainers[index], point);
        var mixer = _blendMixerPlayableList[index];
        for (var i = 0; i < weights.Length; i++)
        {
            mixer.SetInputWeight(i, weights[i]);
        }
        SyncWhenMainClipChange();
    }

    public void SmoothSetClipsWeights(int index, Vector2 targetPoint, float xDampTime, float yDampTime)
    {
        if (index >= _blendNodePoints.Count)
        {
            throw new IndexOutOfRangeException("索引越界");
        }

        if (!_smoothCurrentPoints.TryGetValue(index, out var currentPoint))
        {
            currentPoint = Vector2.zero;
            _smoothCurrentPoints[index] = currentPoint;
        }

        if (!_smoothVelocities.TryGetValue(index, out var velocity))
        {
            velocity = Vector2.zero;
            _smoothVelocities[index] = velocity;
        }

        if (xDampTime <= 0f)
        {
            currentPoint.x = targetPoint.x;
            velocity.x = 0f;
        }
        else
        {
            currentPoint.x = Mathf.SmoothDamp(currentPoint.x, targetPoint.x, ref velocity.x, xDampTime);
        }

        if (yDampTime <= 0f)
        {
            currentPoint.y = targetPoint.y;
            velocity.y = 0f;
        }
        else
        {
            currentPoint.y = Mathf.SmoothDamp(currentPoint.y, targetPoint.y, ref velocity.y, yDampTime);
        }

        _smoothCurrentPoints[index] = currentPoint;
        _smoothVelocities[index] = velocity;
        SetClipsWeights(index, currentPoint);
    }

    public void SmoothSetClipsWeightsForAllMixers(Vector2 targetPoint, float xDampTime, float yDampTime)
    {
        for (var i = 0; i < _blendMixerPlayableList.Count; i++)
        {
            SmoothSetClipsWeights(i, targetPoint, xDampTime, yDampTime);
        }
    }

    public double GetCurrentMasterClipTime(int mixerIndex, bool ifNormalized = false)
    {
        if (mixerIndex < 0 || mixerIndex >= _blendClipPlayableLists.Count)
        {
            throw new IndexOutOfRangeException("动画层索引越界。");
        }

        var localMasterClipIndex = _localMasterClipIndices[mixerIndex];
        if (!_isSync || localMasterClipIndex < 0 || localMasterClipIndex >= _blendClipPlayableLists[mixerIndex].Count)
        {
            localMasterClipIndex = GetDominantClipIndex(mixerIndex);
        }

        if (localMasterClipIndex < 0) return default;
        var playable = _blendClipPlayableLists[mixerIndex][localMasterClipIndex];
        if (!ifNormalized) return playable.GetTime();

        var clipLength = playable.GetAnimationClip().length;
        return clipLength <= 1e-6f ? default : playable.GetTime() / clipLength;
    }

    public void SetCurMasterClipTime(double time, bool isNormalizedTime = false)
    {
        var mixerIndex = _masterMixerIndex;
        var clipIndex = _masterClipIndex;
        if (mixerIndex < 0 || mixerIndex >= _blendClipPlayableLists.Count ||
            clipIndex < 0 || clipIndex >= _blendClipPlayableLists[mixerIndex].Count)
        {
            mixerIndex = GetDominantTotalMixerIndex();
            if (mixerIndex < 0) return;
            clipIndex = GetDominantClipIndex(mixerIndex);
            if (clipIndex < 0) return;
        }

        var playable = _blendClipPlayableLists[mixerIndex][clipIndex];
        if (isNormalizedTime)
        {
            var clipLength = playable.GetAnimationClip().length;
            if (clipLength <= 1e-6f) return;
            time = (time % 1d + 1d) % 1d;
            time *= clipLength;
        }

        playable.SetTime(time);
    }

    public double GetMasterClipTime(bool ifNormalized = false)
    {
        var mixerIndex = _masterMixerIndex;
        var clipIndex = _masterClipIndex;
        if (mixerIndex < 0 || mixerIndex >= _blendClipPlayableLists.Count ||
            clipIndex < 0 || clipIndex >= _blendClipPlayableLists[mixerIndex].Count)
        {
            mixerIndex = GetDominantTotalMixerIndex();
            if (mixerIndex < 0) return -1;
            clipIndex = GetDominantClipIndex(mixerIndex);
            if (clipIndex < 0) return -1;
        }

        var playable = _blendClipPlayableLists[mixerIndex][clipIndex];
        if (!ifNormalized) return playable.GetTime();

        var clipLength = playable.GetAnimationClip().length;
        return clipLength <= 1e-6f ? -1 : playable.GetTime() / clipLength;
    }
    
    public void ConnectClipsToMixer(List<AnimationClip> clips,List<Vector2> points,PlayableGraph graph,int index)
    {
        var mixer = AnimationMixerPlayable.Create(graph, clips.Count);
        // 子混合器输出接入总混合器的对应输入端口，避免占用子混合器input 0
        graph.Connect(mixer, 0, _totalMixer, index);
        _blendMixerPlayableList.Add(mixer);
        _blendClipPlayableLists.Add(new List<AnimationClipPlayable>(clips.Count));
        _dimensionalTransitionContainers.Add(TwoDimensionalTransitionContainer.Create(points.ToArray()));
        for(var i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetSpeed(_playSpeed);
            _blendClipPlayableLists[index].Add(playable);
            graph.Connect(playable, 0, mixer, i);
        }
        for (var i = 0; i < clips.Count; i++)
        {
            if (i == 0)
            {
                mixer.SetInputWeight(i, 1);
                continue;
            }
            mixer.SetInputWeight(i,0);
        }
    }
    

    protected override void SyncWhenMainClipChange()
    {
        if(!_isSync || _syncDeviations == null) return;
        for (var mixerIndex = 0; mixerIndex < _blendMixerPlayableList.Count; mixerIndex++)
        {
            UpdateStableLocalMasterClipIndex(mixerIndex);
        }

        var newMasterMixerIndex = GetDominantTotalMixerIndex();
        if (newMasterMixerIndex < 0) return;

        var newMasterClipIndex = GetDominantClipIndex(newMasterMixerIndex);
        if (newMasterClipIndex < 0) return;

        // 首次建立主片段
        if (_masterMixerIndex < 0 || _masterClipIndex < 0)
        {
            _masterMixerIndex = newMasterMixerIndex;
            _masterClipIndex = newMasterClipIndex;
            _lastMasterSwitchTime = Time.time;
        }
        else if (_masterMixerIndex != newMasterMixerIndex || _masterClipIndex != newMasterClipIndex)
        {
            var candidateWeight = GetEffectiveMasterWeight(newMasterMixerIndex, newMasterClipIndex);
            var currentWeight = GetEffectiveMasterWeight(_masterMixerIndex, _masterClipIndex);
            var canSwitchByWeight = (candidateWeight - currentWeight) >= _masterSwitchMinWeightDelta;
            var canSwitchByTime = (Time.time - _lastMasterSwitchTime) >= _masterMinHoldSeconds;
            if (canSwitchByWeight && canSwitchByTime)
            {
                _masterMixerIndex = newMasterMixerIndex;
                _masterClipIndex = newMasterClipIndex;
                _lastMasterSwitchTime = Time.time;
            }
        }

    }

    protected override void SyncPhase()
    {
        if (!_isSync || _syncDeviations == null || _masterMixerIndex < 0 || _masterClipIndex < 0) return;
        for (var mixerIndex = 0; mixerIndex < _blendMixerPlayableList.Count; mixerIndex++)
        {
            SyncMixerInternalByLocalMaster(mixerIndex);
        }

        SyncLocalMastersToGlobalMaster(_masterMixerIndex, _masterClipIndex); 
        for (var mixerIndex = 0; mixerIndex < _blendMixerPlayableList.Count; mixerIndex++)
        {
            SyncMixerInternalByLocalMaster(mixerIndex);
        }
    }

    private float GetEffectiveMasterWeight(int mixerIndex, int clipIndex)
    {
        if (mixerIndex < 0 || mixerIndex >= _blendMixerPlayableList.Count) return 0f;
        var mixerWeight = _totalMixer.GetInputWeight(mixerIndex);
        var clipWeight = _blendMixerPlayableList[mixerIndex].GetInputWeight(clipIndex);
        return mixerWeight * clipWeight;
    }

    private int GetDominantTotalMixerIndex()
    {
        var maxWeight = float.MinValue;
        var index = -1;
        for (var i = 0; i < _blendMixerPlayableList.Count; i++)
        {
            var w = _totalMixer.GetInputWeight(i);
            if (w <= maxWeight) continue;
            maxWeight = w;
            index = i;
        }

        return index;
    }

    private int GetDominantClipIndex(int mixerIndex)
    {
        var mixer = _blendMixerPlayableList[mixerIndex];
        var clips = _blendClipPlayableLists[mixerIndex];
        var maxWeight = float.MinValue;
        var index = -1;
        for (var i = 0; i < clips.Count; i++)
        {
            var w = mixer.GetInputWeight(i);
            if (w <= maxWeight) continue;
            maxWeight = w;
            index = i;
        }

        return index;
    }

    private int UpdateStableLocalMasterClipIndex(int mixerIndex)
    {
        if (mixerIndex < 0 || mixerIndex >= _blendMixerPlayableList.Count) return -1;
        var clips = _blendClipPlayableLists[mixerIndex];
        if (clips == null || clips.Count == 0) return -1;

        var candidateIndex = GetDominantClipIndex(mixerIndex);
        if (candidateIndex < 0) return -1;

        var currentIndex = _localMasterClipIndices[mixerIndex];
        if (currentIndex < 0 || currentIndex >= clips.Count)
        {
            _localMasterClipIndices[mixerIndex] = candidateIndex;
            _localMasterSwitchTimes[mixerIndex] = Time.time;
            return candidateIndex;
        }

        if (currentIndex == candidateIndex)
        {
            return currentIndex;
        }

        var mixer = _blendMixerPlayableList[mixerIndex];
        var candidateWeight = mixer.GetInputWeight(candidateIndex);
        var currentWeight = mixer.GetInputWeight(currentIndex);
        var canSwitchByWeight = (candidateWeight - currentWeight) >= _localMasterSwitchMinWeightDelta;
        var canSwitchByTime = (Time.time - _localMasterSwitchTimes[mixerIndex]) >= _localMasterMinHoldSeconds;

        if (canSwitchByWeight && canSwitchByTime)
        {
            _localMasterClipIndices[mixerIndex] = candidateIndex;
            _localMasterSwitchTimes[mixerIndex] = Time.time;
            return candidateIndex;
        }

        return currentIndex;
    }

    private void SyncMixerInternalByLocalMaster(int mixerIndex)
    {
        if (mixerIndex < 0 || mixerIndex >= _blendClipPlayableLists.Count) return;
        var localMasterClipIndex = _localMasterClipIndices[mixerIndex];
        if (localMasterClipIndex < 0) return;

        var clips = _blendClipPlayableLists[mixerIndex];
        var deviations = _syncDeviations[mixerIndex];
        var localMasterPlayable = clips[localMasterClipIndex];
        var localMasterClip = localMasterPlayable.GetAnimationClip();
        if (localMasterClip == null || localMasterClip.length <= 1e-6f) return;

        var localMasterNorm = (float)((localMasterPlayable.GetTime() % localMasterClip.length) / localMasterClip.length);
        var localPhaseAnchor = localMasterNorm - deviations[localMasterClipIndex];

        for (var clipIndex = 0; clipIndex < clips.Count; clipIndex++)
        {
            if (clipIndex == localMasterClipIndex) continue;
            var playable = clips[clipIndex];
            var clip = playable.GetAnimationClip();
            if (clip == null || clip.length <= 1e-6f) continue;

            var normalizedTime = (localPhaseAnchor + deviations[clipIndex] + 1f) % 1f;
            playable.SetTime(normalizedTime * clip.length);
        }
    }

    private void SyncLocalMastersToGlobalMaster(int globalMasterMixerIndex, int globalMasterClipIndex)
    {
        if (globalMasterMixerIndex < 0 || globalMasterMixerIndex >= _blendClipPlayableLists.Count) return;
        if (globalMasterClipIndex < 0 || globalMasterClipIndex >= _blendClipPlayableLists[globalMasterMixerIndex].Count) return;

        var globalMasterPlayable = _blendClipPlayableLists[globalMasterMixerIndex][globalMasterClipIndex];
        var globalMasterClip = globalMasterPlayable.GetAnimationClip();
        if (globalMasterClip == null || globalMasterClip.length <= 1e-6f) return;

        var globalMasterNorm = (float)((globalMasterPlayable.GetTime() % globalMasterClip.length) / globalMasterClip.length);
        var globalAnchor = globalMasterNorm - _syncDeviations[globalMasterMixerIndex][globalMasterClipIndex];

        for (var mixerIndex = 0; mixerIndex < _blendClipPlayableLists.Count; mixerIndex++)
        {
            if (mixerIndex == globalMasterMixerIndex) continue;
            var localMasterClipIndex = _localMasterClipIndices[mixerIndex];
            if (localMasterClipIndex < 0) continue;

            var playable = _blendClipPlayableLists[mixerIndex][localMasterClipIndex];
            var clip = playable.GetAnimationClip();
            if (clip == null || clip.length <= 1e-6f) continue;

            var normalizedTime = (globalAnchor + _syncDeviations[mixerIndex][localMasterClipIndex] + 1f) % 1f;
            playable.SetTime(normalizedTime * clip.length);
        }
    }

    public override void PushPool(bool isForced = false)
    {
        if (IsCached && !isForced)
        {
            IsLinked = false;
            if (_localMasterClipIndices != null)
            {
                for (var i = 0; i < _localMasterClipIndices.Count; i++) _localMasterClipIndices[i] = -1;
            }
            if (_localMasterSwitchTimes != null)
            {
                for (var i = 0; i < _localMasterSwitchTimes.Count; i++) _localMasterSwitchTimes[i] = -999f;
            }
            this.RemoveUpdate(SyncPhase);
            return;
        }
        _blendClipPlayableLists.Clear();
        _blendMixerPlayableList.Clear();
        _dimensionalTransitionContainers.Clear();
        _blendNodePoints.Clear();
        _syncDeviations?.Clear();
        _smoothCurrentPoints.Clear();
        _smoothVelocities.Clear();
        _smoothLinearX = 0f;
        _smoothLinearVelocity = 0f;
        _hasSmoothLinearX = false;
        _localMasterClipIndices?.Clear();
        _localMasterSwitchTimes?.Clear();
        _blendClipPlayableLists = null;
        _blendMixerPlayableList = null;
        _dimensionalTransitionContainers = null;
        _syncDeviations = null;
        _localMasterClipIndices = null;
        _localMasterSwitchTimes = null;
        _blendNodePoints = null;
        _masterMixerIndex = -1;
        _masterClipIndex = -1;
        _lastMasterSwitchTime = -999f;
        this.RemoveUpdate(SyncPhase);
        base.PushPool(isForced);
    }

    public struct TwoDimensionalTransitionContainer
    {
        public Vector2[] Points;

        /// <summary>
        /// 任意一个移动点向逆时针和顺时针方向查找相差角度最小的两个点再取用原点构成三角形计算movePoint的三点权重构成最终的结果
        /// </summary>
        /// <param name="container"></param>
        /// <param name="movePoint"></param>
        /// <returns></returns>
        public static float[] GetWeights(TwoDimensionalTransitionContainer container, Vector2 movePoint)
        {
            if (container.Points == null || container.Points.Length == 0)
            {
                return Array.Empty<float>();
            }

            var weights = new float[container.Points.Length];
            if (container.Points.Length < 3)
            {
                weights[0] = 1f;
                return weights;
            }

            if (movePoint.sqrMagnitude < 1e-8f)
            {
                weights[0] = 1f;
                return weights;
            }

            var index1 = container.GetNextestPoint(movePoint);
            var index2 = container.GetNextestPoint(movePoint, true);

            if (index1 < 1)
            {
                index1 = container.GetNearestPointByAbsoluteAngle(movePoint);
            }

            if (index2 < 1 || index2 == index1)
            {
                index2 = container.GetNearestPointByAbsoluteAngle(-movePoint);
                if (index2 < 1 || index2 == index1)
                {
                    index2 = index1 == 1 ? 2 : 1;
                }
            }

            var effectiveWeights = CalculateWeights(container.Points[0], container.Points[index1],
                container.Points[index2], movePoint);
            for (var i = 0; i < container.Points.Length; i++)
            {
                if (i == 0)
                {
                    weights[i] = effectiveWeights[0];
                }
                else if (i == index1)
                {
                    weights[i] = effectiveWeights[1];
                }
                else if (i == index2)
                {
                    weights[i] = effectiveWeights[2];
                }
            }

            return weights;
        }

        private int GetNextestPoint(Vector2 movePoint, bool isLeft = false)
        {
            var minAngle = float.MaxValue;
            var index = -1;
            for (int i = 1; i < Points.Length; i++)
            {
                var angle = Vector2.SignedAngle(movePoint, Points[i]);
                if (isLeft)
                {
                    if (angle < 0 && Mathf.Abs(angle) < minAngle)
                    {
                        minAngle = Mathf.Abs(angle);
                        index = i;
                    }
                }
                else
                {
                    if (angle >= 0 && angle < minAngle)
                    {
                        minAngle = angle;
                        index = i;
                    }
                }
            }

            return index;
        }

        private int GetNearestPointByAbsoluteAngle(Vector2 movePoint)
        {
            var minAngle = float.MaxValue;
            var index = -1;
            for (int i = 1; i < Points.Length; i++)
            {
                var angle = Mathf.Abs(Vector2.SignedAngle(movePoint, Points[i]));
                if (angle < minAngle)
                {
                    minAngle = angle;
                    index = i;
                }
            }

            return index;
        }

        public static TwoDimensionalTransitionContainer Create(Vector2[] points)
        {
            if (!CheckPoints(points))
            {
                throw new ArgumentException("当前输入的点集不合法，无法创建一个有效的二维过渡容器。请确保第一个点是(0,0)，并且至少有4个点,同时满足对应的散布要求");
            }

            var container = new TwoDimensionalTransitionContainer();
            container.Points = points;

            return container;
        }

        /// <summary>
        /// 校验当前二维混合节点是否均匀分布在二维坐标系中
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private static bool CheckPoints(Vector2[] points)
        {
            if (points == null || points.Length == 0)
            {
                return false;
            }

            if (points[0] != Vector2.zero) //第一个点必须在原点
            {
                return false;
            }

            if (points.Length <= 3)
            {
                return false;
            }

            var angles = new List<float>();
            for (int i = 1; i < points.Length; i++)
            {
                var vec = points[i];
                angles.Add(GetAngle(vec));
            }

            angles.Sort();
            for (int i = 0; i < angles.Count; i++)
            {
                var nextIndex = (i + 1) % angles.Count;
                if (nextIndex != 0)
                {
                    if (angles[nextIndex] - angles[i] > 180)
                    {
                        return false;
                    }
                }
                else
                {
                    if (angles[nextIndex] + 360 - angles[i] > 180)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static float GetAngle(Vector2 point)
        {
            var angle = Vector2.SignedAngle(Vector2.right, point);
            if (angle < 0)
            {
                angle = 360 + angle;
            }

            return angle;
        }

        private static float[] CalculateWeights(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 movePoint)
        {
            const float Epsilon = 1e-6f;
            float D = (p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y);
            var weights = new float[3];

            if (Mathf.Abs(D) < Epsilon)
            {
                FallbackNearestVertexWeights(p1, p2, p3, movePoint,weights);
                return weights;
            }

            float w1 = ((p2.y - p3.y) * (movePoint.x - p3.x) + (p3.x - p2.x) * (movePoint.y - p3.y)) / D;
            float w2 = ((p3.y - p1.y) * (movePoint.x - p3.x) + (p1.x - p3.x) * (movePoint.y - p3.y)) / D;
            float w3 = 1f - w1 - w2;


            w1 =Mathf.Clamp01(w1);
            w2 =Mathf.Clamp01(w2);
            w3 =Mathf.Clamp01(w3);
            
            float totalWeight = w1 + w2 + w3;
            if (totalWeight < Epsilon)
            {
                FallbackNearestVertexWeights(p1, p2, p3, movePoint,weights);
                return weights;
            }

            w1 /= totalWeight;
            w2 /= totalWeight;
            w3 /= totalWeight;
            weights[0] = w1;
            weights[1] = w2;
            weights[2] = w3;
            return weights;
        }

        /// <summary>
        /// 比重退化的兜底逻辑
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="movePoint"></param>
        /// <param name="weights"></param>
        private static void FallbackNearestVertexWeights(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 movePoint,float[] weights)
        {
            float d1 = (movePoint - p1).sqrMagnitude;
            float d2 = (movePoint - p2).sqrMagnitude;
            float d3 = (movePoint - p3).sqrMagnitude;

            if (d1 <= d2 && d1 <= d3)
            {
                weights[0] = 1;
                weights[1] = 0;
                weights[2] = 0;
            }
            else if (d2 <= d1 && d2 <= d3)
            {
                weights[0] = 0;
                weights[1] = 1;
                weights[2] = 0;
            }
            else
            {
                weights[0] = 0;
                weights[1] = 0;
                weights[2] = 1;
            }
        }
    }
}
