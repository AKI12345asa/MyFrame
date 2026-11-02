using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
/// <summary>
/// 多层线性动画过渡节点
/// </summary>
public class DirectionalLinearBlendTreeNode : BlendAnimationNodeBase
{
    private AnimationMixerPlayable _totalMixer;
    protected bool _isSync;
    public override bool IsSync => _isSync;
    private float _playSpeed;
    private List<List<AnimationClipPlayable>> _blendClipPlayableLists;
    private List<AnimationMixerPlayable> _playableList;
    private List<OneDimensionalTransitionContainer> _xBlendContainers;
    private List<float> _moveSpeedPoints;
    private OneDimensionalTransitionContainer _yBlendContainer;
    private List<List<float>> _syncDeviations;
    private float _currentBlendX;
    private float _currentBlendY;
    private float _blendXVelocity;
    private float _blendYVelocity;
    private int _masterMixerIndex = -1;
    private int _masterClipIndex = -1;
    private List<int> _localMasterClipIndices;
    private List<float> _localMasterSwitchTimes;
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

    protected override void ConnectToPlayableGraph<V>(Playable connector, PlayableGraph graph, V outputMixer, int sourceOutPutPort, bool isTransitioning)
    {
        base.ConnectToPlayableGraph(connector, graph, outputMixer, sourceOutPutPort, isTransitioning);
        if (isTransitioning) return;
        if (_isSync)
        {
            this.AddUpdate(SyncPhase);
        }
    }
    
    public override void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)
    {
        if (isTransitioning) return;
        ConnectToPlayableGraph(_totalMixer, graph, outputMixer, 0, false);
    }
    
    public override void SetClipSpeed(float speed)
    {
        foreach (var clips in _blendClipPlayableLists)
        {
            foreach (var clip in clips)
            {
                clip.SetSpeed(speed * Speed);
            }
        }
    } 
    private void Init<V>(PlayableGraph graph, V outputMixer, List<List<AnimationClip>> clipsList, List<List<float>> turnPointsList, List<float> moveSpeedPoints, float speed, int inputPort, bool isSync = false, bool isTransitioning = false, List<List<float>> syncDeviations = null, bool isCached = false) where V : struct, IPlayable
    {
        ValidateInputLists(clipsList, turnPointsList, moveSpeedPoints, syncDeviations, isSync);
        _isSync = isSync;
        _playSpeed = speed;
        InputPort = inputPort;
        IsCached = isCached;
        _totalMixer = AnimationMixerPlayable.Create(graph, clipsList.Count);
        _blendClipPlayableLists = new List<List<AnimationClipPlayable>>(clipsList.Count);
        _playableList = new List<AnimationMixerPlayable>(clipsList.Count);
        _xBlendContainers = new List<OneDimensionalTransitionContainer>(clipsList.Count);
        _syncDeviations = syncDeviations;
        _moveSpeedPoints = moveSpeedPoints;
        _yBlendContainer = OneDimensionalTransitionContainer.Create(moveSpeedPoints.ToArray());
        _currentBlendY = moveSpeedPoints[0];
        _blendYVelocity = 0f;
        _localMasterClipIndices = new List<int>(clipsList.Count);
        _localMasterSwitchTimes = new List<float>(clipsList.Count);

        for (int i = 0; i < clipsList.Count; i++)
        {
            ConnectClipsToMixer(clipsList[i], turnPointsList[i], graph, i);
            _localMasterClipIndices.Add(-1);
            _localMasterSwitchTimes.Add(-999f);
        }

        if (turnPointsList.Count > 0 && turnPointsList[0].Count > 0)
        {
            _currentBlendX = turnPointsList[0][0];
        }
        _blendXVelocity = 0f;

        if (_playableList.Count > 1)
        {
            SetBlendAnimationAsLinerPoint(_moveSpeedPoints[0]);
        }
        else
        {
            _totalMixer.SetInputWeight(0, 1);
        }

        SyncWhenMainClipChange();
        ConnectToPlayableGraph(_totalMixer, graph, outputMixer, 0, isTransitioning);
    }

    /// <summary>使用方向线性混合树配置初始化节点。</summary>
    public void Init<V>(PlayableGraph graph, V outputMixer, DirectionalLinearBlendTreeData data, float speed, int inputPort, bool isSync = false, bool isTransitioning = false, bool isCached = false) where V : struct, IPlayable
    {
        var clipsList = new List<List<AnimationClip>>();
        var turnPointsList = new List<List<float>>();
        var syncDeviationsList = new List<List<float>>();
        var movePoints = new List<float>();
        foreach (var layer in data.Layers)
        {
            var clips = new List<AnimationClip>();
            var turnPoints = new List<float>();
            var deviations = new List<float>();
            foreach (var point in layer.points)
            {
                clips.Add(point.clip);
                turnPoints.Add(point.point);
                deviations.Add(point.syncDeviation);
            }

            clipsList.Add(clips);
            turnPointsList.Add(turnPoints);
            syncDeviationsList.Add(deviations);
            movePoints.Add(layer.mixerPoint);
        }

        Init(graph, outputMixer, clipsList, turnPointsList, movePoints, speed, inputPort, isSync, isTransitioning, syncDeviationsList, isCached);
    }

    private static void ValidateInputLists(
        List<List<AnimationClip>> clipsList,
        List<List<float>> turnPointsList,
        List<float> moveSpeedPoints,
        List<List<float>> syncDeviations,
        bool isSync)
    {
        if (clipsList == null) throw new ArgumentNullException(nameof(clipsList));
        if (turnPointsList == null) throw new ArgumentNullException(nameof(turnPointsList));
        if (moveSpeedPoints == null) throw new ArgumentNullException(nameof(moveSpeedPoints));

        int outerCount = clipsList.Count;
        if (outerCount == 0) throw new ArgumentException("动画混合层列表不能为空。", nameof(clipsList));
        if (turnPointsList.Count != outerCount)
        {
            throw new ArgumentException($"动画混合层数量与转向采样点组数量不一致：动画层={outerCount}，采样点组={turnPointsList.Count}。");
        }

        if (moveSpeedPoints.Count != outerCount)
        {
            throw new ArgumentException($"动画混合层数量与移动速度点数量不一致：动画层={outerCount}，移动速度点={moveSpeedPoints.Count}。");
        }

        if (isSync && syncDeviations == null)
        {
            throw new ArgumentNullException(nameof(syncDeviations), "启用相位同步时必须提供各动画片段的相位差列表。");
        }

        if (syncDeviations != null && syncDeviations.Count != outerCount)
        {
            throw new ArgumentException($"动画混合层数量与相位差组数量不一致：动画层={outerCount}，相位差组={syncDeviations.Count}。");
        }

        for (int i = 0; i < outerCount; i++)
        {
            if (clipsList[i] == null) throw new ArgumentException($"第 {i} 组动画片段列表不能为空。");
            if (turnPointsList[i] == null) throw new ArgumentException($"第 {i} 组转向采样点列表不能为空。");
            if (clipsList[i].Count != turnPointsList[i].Count)
            {
                throw new ArgumentException($"第 {i} 组动画片段与转向采样点数量不一致：动画片段={clipsList[i].Count}，采样点={turnPointsList[i].Count}。");
            }

            if (syncDeviations == null) continue;
            if (syncDeviations[i] == null)
            {
                throw new ArgumentException($"第 {i} 组相位差列表不能为空。");
            }

            if (syncDeviations[i].Count != clipsList[i].Count)
            {
                throw new ArgumentException($"第 {i} 组动画片段与相位差数量不一致：动画片段={clipsList[i].Count}，相位差={syncDeviations[i].Count}。");
            }
        }

        OneDimensionalTransitionContainer.Create(moveSpeedPoints.ToArray());
        for (int i = 0; i < turnPointsList.Count; i++)
        {
            OneDimensionalTransitionContainer.Create(turnPointsList[i].ToArray());
        }
    }

    /// <summary>根据X参数调整不同动画层的混合权重。</summary>
    public override void SetBlendAnimationAsLinerPoint(float xValue)
    {
        if (_moveSpeedPoints == null || _moveSpeedPoints.Count < 1)
        {
            throw new InvalidOperationException("X参数点至少需要 1 个。");
        }
        if (_moveSpeedPoints.Count == 1)
        {
            _totalMixer.SetInputWeight(0, 1f);
            SyncWhenMainClipChange();
            return;
        }
        // 参数超出范围时由一维过渡容器自动限制在首尾混合点之间。
        var weights = OneDimensionalTransitionContainer.GetWeights(_yBlendContainer, xValue);
        for (var i = 0; i < weights.Length; i++)
        {
            _totalMixer.SetInputWeight(i, weights[i]);
        }
        SyncWhenMainClipChange();
    }

    /// <summary>获取指定动画片段在总混合结果中的有效权重。</summary>
    public float GetClipWeight(int mixerWeight,int clipIndex)
    {
        return GetEffectiveMasterWeight(mixerWeight, clipIndex);
    }
    
    /// <summary>获取指定方向动画层在总混合器中的权重。</summary>
    public float GetMixerWeight(int mixerIndex)
    {
        if (mixerIndex < 0 || mixerIndex >= _playableList.Count)
        {
            throw new IndexOutOfRangeException("动画层索引越界。");
        }
        return _totalMixer.GetInputWeight(mixerIndex);
    }

    /// <summary>获取指定片段的播放时间，可选择返回归一化时间。</summary>
    public double GetClipPlayableTime(int mixerIndex, int clipIndex,bool ifNormalized = false)
    {
        if (mixerIndex < 0 || mixerIndex >= _blendClipPlayableLists.Count)
        {
            throw new IndexOutOfRangeException("动画层索引越界。");
        }

        var clips = _blendClipPlayableLists[mixerIndex];
        if (clipIndex < 0 || clipIndex >= clips.Count)
        {
            throw new IndexOutOfRangeException("动画片段索引越界。");
        }

        if (ifNormalized)
        {
            var clip = clips[clipIndex];
            return clip.GetTime() / clip.GetAnimationClip().length;
        }
        return clips[clipIndex].GetTime();
    }
    
    /// <summary>获取指定动画层当前主片段的播放时间，可选择返回归一化时间。</summary>
    public double GetCurrentMasterClipTime(int mixerIndex,bool ifNormalized = false)
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
        if (ifNormalized)
        {
            var clip = _blendClipPlayableLists[mixerIndex][localMasterClipIndex];
            var clipLength = clip.GetAnimationClip().length;
            if (clipLength <= 1e-6f) return default;
            return clip.GetTime() / clipLength;
        }
        return _blendClipPlayableLists[mixerIndex][localMasterClipIndex].GetTime();
    }
    
    /// <summary>根据X参数设置指定动画层中各方向片段的权重。</summary>
    public void SetClipWeightAsLinerPoint(int index, float xValue)
    {
        if (index < 0 || index >= _playableList.Count)
        {
            throw new IndexOutOfRangeException("动画层索引越界。");
        }

        var weights = OneDimensionalTransitionContainer.GetWeights(_xBlendContainers[index], xValue);
        var mixer = _playableList[index];
        for (var i = 0; i < weights.Length; i++)
        {
            mixer.SetInputWeight(i, weights[i]);
        }
        SyncWhenMainClipChange();
    }

    /// <summary>根据同一X设置所有动画层的片段权重。</summary>
    public void SetClipWeightForAllLayers(float xValue)
    {
        for (var i = 0; i < _playableList.Count; i++)
        {
            SetClipWeightAsLinerPointInternal(i, xValue);
        }
        SyncWhenMainClipChange();
    }

    private void SetClipWeightAsLinerPointInternal(int index, float xValue)
    {
        if (index < 0 || index >= _playableList.Count)
        {
            throw new IndexOutOfRangeException("动画层索引越界。");
        }

        var weights = OneDimensionalTransitionContainer.GetWeights(_xBlendContainers[index], xValue);
        var mixer = _playableList[index];
        for (var i = 0; i < weights.Length; i++)
        {
            mixer.SetInputWeight(i, weights[i]);
        }
    }

    /// <summary>立即设置X参数。</summary>
    public void SetBlendByX(float xValue)
    {
        _currentBlendX = xValue;
        _blendXVelocity = 0f;
        ApplyBlendByXAndY(_currentBlendX, _currentBlendY);
    }

    /// <summary>立即设置Y参数。</summary>
    public void SetBlendByY(float yValue)
    {
        _currentBlendY = yValue;
        _blendYVelocity = 0f;
        ApplyBlendByXAndY(_currentBlendX, _currentBlendY);
    }

    /// <summary>立即同时设置X与Y参数。</summary>
    public void SetBlendByXAndY(float xValue, float yValue)
    {
        _currentBlendX = xValue;
        _blendXVelocity = 0f;
        _currentBlendY = yValue;
        _blendYVelocity = 0f;
        ApplyBlendByXAndY(_currentBlendX, _currentBlendY);
    }

    /// <summary>平滑地将X参数调整到目标值。</summary>
    public void SmoothSetBlendByX(float targetXValue, float xDampTime)
    {
        if (xDampTime <= 0f)
        {
            _currentBlendX = targetXValue;
            _blendXVelocity = 0f;
        }
        else
        {
            _currentBlendX = Mathf.SmoothDamp(_currentBlendX, targetXValue, ref _blendXVelocity, xDampTime);
        }

        ApplyBlendByXAndY(_currentBlendX, _currentBlendY);
    }

    /// <summary>平滑地将Y参数调整到目标值。</summary>
    public void SmoothSetBlendByY(float targetYValue, float yDampTime)
    {
        if (yDampTime <= 0f)
        {
            _currentBlendY = targetYValue;
            _blendYVelocity = 0f;
        }
        else
        {
            _currentBlendY = Mathf.SmoothDamp(_currentBlendY, targetYValue, ref _blendYVelocity, yDampTime);
        }

        ApplyBlendByXAndY(_currentBlendX, _currentBlendY);
    }

    /// <summary>平滑地同时调整XY速度参数。</summary>
    public void SmoothSetBlendByXAndY(float targetXValue, float targetYValue, float xDampTime, float yDampTime)
    {
        _currentBlendX = xDampTime <= 0f
            ? targetXValue
            : Mathf.SmoothDamp(_currentBlendX, targetXValue, ref _blendXVelocity, xDampTime);
        _currentBlendY = yDampTime <= 0f
            ? targetYValue
            : Mathf.SmoothDamp(_currentBlendY, targetYValue, ref _blendYVelocity, yDampTime);
        ApplyBlendByXAndY(_currentBlendX, _currentBlendY);
    }

    public void SetCurMasterClipTime(double time, bool isNormalizedTime = false)
    {
        if (_masterMixerIndex < 0 || _masterMixerIndex >= _blendClipPlayableLists.Count || _masterClipIndex < 0 || _masterClipIndex >= _blendClipPlayableLists[_masterMixerIndex].Count)
        {
            return;
        }
        var playable = _blendClipPlayableLists[_masterMixerIndex][_masterClipIndex];
        if (isNormalizedTime)
        {
            var clip = playable.GetAnimationClip();
            if (clip == null || clip.length <= 1e-6f) return;
            time = (time % 1d + 1d) % 1d;
            time *= clip.length;
        }
        playable.SetTime(time);
    }

    public double GetMasterClipTime(bool ifNormalized = false)
    {
        if (_masterClipIndex < 0 || _masterMixerIndex < 0) return -1;
        var playable = _blendClipPlayableLists[_masterMixerIndex][_masterClipIndex];
        if(!ifNormalized) return playable.GetTime(); 
        return playable.GetTime() / playable.GetAnimationClip().length;
    }

    private void ApplyBlendByXAndY(float xValue, float yValue)
    {
        for (var i = 0; i < _playableList.Count; i++)
        {
            SetClipWeightAsLinerPointInternal(i, xValue);
        }
        SetBlendAnimationAsLinerPointWithoutSync(yValue);
        SyncWhenMainClipChange();
    }

    private void SetBlendAnimationAsLinerPointWithoutSync(float xValue)
    {
        if (_moveSpeedPoints.Count == 1)
        {
            _totalMixer.SetInputWeight(0, 1f);
            return;
        }

        var weights = OneDimensionalTransitionContainer.GetWeights(_yBlendContainer, xValue);
        for (var i = 0; i < weights.Length; i++)
        {
            _totalMixer.SetInputWeight(i, weights[i]);
        }
    }

    /// <summary>创建一个方向动画层的混合器，并连接该层的动画片段。</summary>
    private void ConnectClipsToMixer(List<AnimationClip> clips, List<float> points, PlayableGraph graph, int index)
    {
        var mixer = AnimationMixerPlayable.Create(graph, clips.Count);
        graph.Connect(mixer, 0, _totalMixer, index);
        _playableList.Add(mixer);
        _blendClipPlayableLists.Add(new List<AnimationClipPlayable>(clips.Count));
        _xBlendContainers.Add(OneDimensionalTransitionContainer.Create(points.ToArray()));
        for (var i = 0; i < clips.Count; i++)
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

            mixer.SetInputWeight(i, 0);
        }
    }

    protected override void SyncWhenMainClipChange()
    {
        if (!_isSync || _syncDeviations == null) return;
        for (var mixerIndex = 0; mixerIndex < _playableList.Count; mixerIndex++)
        {
            UpdateStableLocalMasterClipIndex(mixerIndex);
        }

        var newMasterMixerIndex = GetDominantTotalMixerIndex();
        if (newMasterMixerIndex < 0) return;

        var newMasterClipIndex = GetDominantClipIndex(newMasterMixerIndex);
        if (newMasterClipIndex < 0) return;

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
        for (var mixerIndex = 0; mixerIndex < _playableList.Count; mixerIndex++)
        {
            SyncMixerInternalByLocalMaster(mixerIndex);
        }

        SyncLocalMastersToGlobalMaster(_masterMixerIndex, _masterClipIndex);
        for (var mixerIndex = 0; mixerIndex < _playableList.Count; mixerIndex++)
        {
            SyncMixerInternalByLocalMaster(mixerIndex);
        }
    }

    private float GetEffectiveMasterWeight(int mixerIndex, int clipIndex)
    {
        if (mixerIndex < 0 || mixerIndex >= _playableList.Count) return 0f;
        var mixerWeight = _totalMixer.GetInputWeight(mixerIndex);
        var clipWeight = _playableList[mixerIndex].GetInputWeight(clipIndex);
        return mixerWeight * clipWeight;
    }

    private int GetDominantTotalMixerIndex()
    {
        var maxWeight = float.MinValue;
        var index = -1;
        for (var i = 0; i < _playableList.Count; i++)
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
        var mixer = _playableList[mixerIndex];
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

    /// <summary>
    /// 获取该mixer的本地权重最高的节点用于后续比较
    /// </summary>
    /// <param name="mixerIndex"></param>
    /// <returns></returns>
    private int UpdateStableLocalMasterClipIndex(int mixerIndex)
    {
        if (mixerIndex < 0 || mixerIndex >= _playableList.Count) return -1;
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

        var mixer = _playableList[mixerIndex];
        var candidateWeight = mixer.GetInputWeight(candidateIndex);
        var currentWeight = mixer.GetInputWeight(currentIndex);
        
        //最小权重差和最小时间差避免动画因为权重频繁抖动
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

    /// <summary>
    /// 内部片段相位同步
    /// </summary>
    /// <param name="mixerIndex"></param>
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

    /// <summary>
    /// 统一整体最大权重以及相位
    /// </summary>
    /// <param name="globalMasterMixerIndex"></param>
    /// <param name="globalMasterClipIndex"></param>
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
        _playableList.Clear();
        _xBlendContainers.Clear();
        _moveSpeedPoints.Clear();
        _syncDeviations?.Clear();
        _localMasterClipIndices?.Clear();
        _localMasterSwitchTimes?.Clear();
        _blendClipPlayableLists = null;
        _playableList = null;
        _xBlendContainers = null;
        _moveSpeedPoints = null;
        _syncDeviations = null;
        _currentBlendX = 0f;
        _currentBlendY = 0f;
        _blendXVelocity = 0f;
        _blendYVelocity = 0f;
        _localMasterClipIndices = null;
        _localMasterSwitchTimes = null;
        _masterMixerIndex = -1;
        _masterClipIndex = -1;
        _lastMasterSwitchTime = -999f;
        this.RemoveUpdate(SyncPhase);
        base.PushPool(isForced);
    }

    public struct OneDimensionalTransitionContainer
    {
        /// <summary>用于一维线性混合的有序参数点集合。</summary>
        public float[] Points;

        /// <summary>根据一维参数计算混合点权重，参数超出范围时使用首尾端点权重。</summary>
        public static float[] GetWeights(OneDimensionalTransitionContainer container, float value)
        {
            if (container.Points == null || container.Points.Length == 0)
            {
                return Array.Empty<float>();
            }

            var points = container.Points;
            var weights = new float[points.Length];
            if (points.Length == 1)
            {
                weights[0] = 1f;
                return weights;
            }
            
            value = Mathf.Clamp(value, points[0], points[points.Length - 1]);

            var ceilIndex = -1;
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i] >= value)
                {
                    ceilIndex = i;
                    break;
                }
            }

            if (ceilIndex < 0)
            {
                ceilIndex = points.Length - 1;
            }

            if (ceilIndex == 0)
            {
                ceilIndex = 1;
            }

            var floorIndex = ceilIndex - 1;
            var floorPoint = points[floorIndex];
            var ceilPoint = points[ceilIndex];
            var dis = ceilPoint - floorPoint;
            if (Mathf.Abs(dis) < 1e-6f)
            {
                weights[floorIndex] = 1f;
                return weights;
            }

            weights[floorIndex] = (ceilPoint - value) / dis;
            weights[ceilIndex] = (value - floorPoint) / dis;
            return weights;
        }

        /// <summary>创建一维线性过渡容器，并验证混合点集合有效。</summary>
        public static OneDimensionalTransitionContainer Create(float[] points)
        {
            if (!CheckPoints(points))
            {
                throw new ArgumentException("一维混合点集无效，无法创建线性过渡容器。请确保点集非空且按升序排列。");
            }

            var container = new OneDimensionalTransitionContainer { Points = points };
            return container;
        }

        private static bool CheckPoints(float[] points)
        {
            if (points == null || points.Length < 1)
            {
                return false;
            }

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i] < points[i - 1])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
