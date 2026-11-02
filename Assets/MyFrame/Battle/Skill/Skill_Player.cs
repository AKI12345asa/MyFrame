using System;
using System.Collections;
using System.Collections.Generic;
using MyFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;

/// <summary>
/// 技能播放器
/// </summary>
public class Skill_Player : SerializedMonoBehaviour
{
    [SerializeField]private Animation_Controller animationController;
    [SerializeField] private Dictionary<string, SkillChecker> _checkerDic = new();
    [SerializeField] private ParentConstraint mainWeaponParentConstrain;
    [SerializeField] private Dictionary<string, SkillCamera> _skillCameraDic = new();
    private Dictionary<Type,DetectionEventProcessor> _attackDetectionEventProcessorDic = new();
    private SkillCamera _curSkillCamera;
    public SkillCamera CurSkillCamera => _curSkillCamera;
    private ICharacter _owner;
    public ICharacter Owner => _owner;
    private bool _isPlaying = false;
    public bool IsPlaying
    {
        get => _isPlaying;
        set => _isPlaying = value;
    }
    public bool IsLoop { get;private set; }
    public int FrameRate => _frameRate;
    private SkillChecker _currentWeapon;
    public SkillChecker CurrentWeapon => _currentWeapon;
    private SkillClip _skillClip;
    private int _currentFrameIndex;
    private float _playTotalTime;
    private int _frameRate;
    private readonly HashSet<CameraEventBase> _activeCameraEvents = new();
    private readonly HashSet<CameraEventBase> _finishedCameraEvents = new();
    private readonly List<CameraEventBase> _cameraEventBuffer = new();
    private readonly HashSet<CameraEventProcessor> _triggeredCameraProcessors = new();
    private readonly Dictionary<Type, CameraEventProcessor> _cameraEventProcessorDic = new();
    private Transform _modelTransform;
    public Transform ModelTransform => _modelTransform;
    public AttackDetectionType AttackDetectionType;
    public event Action<HitDataBase> HitTargetEvent;

    public void Init(Animation_Controller animationController,Transform modelTransform,ICharacter owner)
    {
        this.animationController = animationController;
        _modelTransform = modelTransform;
        _owner = owner;
        foreach (var item in checkerDic.Values)
        {
            item.Init(OnWeaponDetection, this);
        }
        foreach(var skillCamera in _skillCameraDic.Values)
        {
            skillCamera.Init(this,_owner);
        }
        IsLoop = false;
    }

    #region 武器
    public Dictionary<string, SkillChecker> checkerDic => _checkerDic;

    public ParentConstraint MainWeaponParentConstrain => mainWeaponParentConstrain;

    public void SetMainWeaponHand(bool isLeft)
    {
        if (mainWeaponParentConstrain == null) return;
        var left = mainWeaponParentConstrain.GetSource(0);
        var right = mainWeaponParentConstrain.GetSource(1);
        left.weight = isLeft ? 1 : 0;
        right.weight = isLeft ? 0 : 1;
        mainWeaponParentConstrain.SetSource(0, left);
        mainWeaponParentConstrain.SetSource(1, right);
    }

    private void OnWeaponDetection(IHitTarget hitTarget, Vector3 hitPoint,AttackDetectionEvent detectionEvent,string weaponName)
    {
        if (_skillBehaviour == null || !_skillBehaviour.OnDetection(hitTarget, hitPoint, detectionEvent, weaponName)) return;
        foreach (var triggerEvent in detectionEvent.TriggerEvents)
        {
            if(_attackDetectionEventProcessorDic.TryGetValue(triggerEvent.GetType(),out var eventProcessor)) 
                eventProcessor.ProcessEvent(triggerEvent,hitPoint,hitTarget,this);
        }
    }
    #endregion
    
    
    private SkillBehaviourBase _skillBehaviour;

    /// <summary>
    /// 对于当前的技能释放逻辑进行注册
    /// </summary>
    /// <param name="playerSkillBehaviour">要注册的逻辑</param>
    public void StartPlayerSkillBehaviour(SkillBehaviourBase playerSkillBehaviour)
    {
        if(_skillBehaviour != null) _skillBehaviour.ClearActiveAttackDetectionEvents();
        _skillBehaviour = playerSkillBehaviour;
    }

    /// <summary>
    /// 播放技能
    /// </summary>
    /// <param name="skillClip">技能配置</param>
    /// <param name="isLoop">是否循环播放</param>
    public void PlaySkillClip(SkillClip skillClip,bool isLoop = false)
    {
        EndActiveCameraEvents();
        _skillClip = skillClip;
        _currentFrameIndex = 0;
        _frameRate = _skillClip.FrameRate;
        IsLoop = isLoop;
        _playTotalTime = 0;
        _finishedCameraEvents.Clear();
        _isPlaying = true;
        _curSkillCamera?.StartSkillCamera();
       _currentWeapon?.StopDetection(); 
        TickSkill();//先播放第零帧的内容
        UpdateCameraEvents();
    }
    private void Clean()
    {
        EndActiveCameraEvents();
        _isPlaying = false;
        _skillClip = null;
        _effectDic.Clear();
    }
    private void Update()
    {
        if (!_isPlaying) return;

        _playTotalTime += _owner.timeScaleContainer.GetDeltaTime();
        var targetFrameIndex = (int)(_playTotalTime * _frameRate);

        while (_currentFrameIndex < targetFrameIndex && _isPlaying)
        {
            if (_currentFrameIndex >= _skillClip.FrameCount)
            {
                if (!IsLoop || _skillClip.FrameCount <= 0)
                {
                    break;
                }
                
                ResetLoopFrame(ref targetFrameIndex);
            }
            TickSkill();
        }
        if (IsLoop && _skillClip.FrameCount > 0)
        {
            if (_currentFrameIndex >= _skillClip.FrameCount)
            {
                ResetLoopFrame(ref targetFrameIndex);
            }

            UpdateCameraEvents();
            return;
        }

        UpdateCameraEvents();
        if (targetFrameIndex >= _skillClip.FrameCount)
        {
            Clean();
            _skillBehaviour.OnSkillClipEnd();
        }
    }

    private void ResetLoopFrame(ref int targetFrameIndex)
    {
        EndActiveCameraEvents();
        _currentFrameIndex = 0;
        targetFrameIndex -= _skillClip.FrameCount;
        _effectDic.Clear();
        _playTotalTime -= _skillClip.FrameCount / (float)_frameRate;
        _finishedCameraEvents.Clear();
        _currentWeapon?.StopDetection();
        _skillBehaviour?.ClearActiveAttackDetectionEvents();
    }


    private void TickSkill()
    {
        _skillBehaviour.OnTickSkill(_currentFrameIndex);
        TickSkillCustomEvent();
        TickSkillAnimationEvent();
        TickSkillAudioEvent();
        TickSkillEffectEvent();
        TickSkillAttackDetectionEvent();
        _currentFrameIndex += 1;
    
    }

    public void StopCurrentSkill()
    {
        EndActiveCameraEvents();
        _skillBehaviour?.OnSkillBehaviourStopped();
        _currentWeapon?.StopDetection();
        _skillClip = null;
        _skillBehaviour = null;
        _isPlaying = false; 
        _effectDic.Clear();
    }

    public void InvokeHitTargetEvent(HitDataBase hitData)
    {
        HitTargetEvent?.Invoke(hitData);
    }
    
    private void TickSkillCustomEvent()
    {
        if (_skillClip.SkillCustomEventData.FrameData.TryGetValue(_currentFrameIndex, out var skillCustomEvent))
        {
            skillCustomEvent = _skillBehaviour.BeforeSkillCustomEvent(skillCustomEvent);
            if (skillCustomEvent == null) return;
            _skillBehaviour.AfterSkillCustomEvent(skillCustomEvent);
        }
    } 
    private void TickSkillAnimationEvent()
    {
        if (animationController != null&& _skillClip.SkillAnimationData.FrameData.TryGetValue(_currentFrameIndex, out var skillAnimationEvent))
        {
            skillAnimationEvent = _skillBehaviour.BeforeSkillAnimationEvent(skillAnimationEvent,out var autoPlayAnimation,out var setWeaponHand);
            if (skillAnimationEvent == null) return;
            if (setWeaponHand)
            {
                SetMainWeaponHand(skillAnimationEvent.MainWeaponOnLeftHand);
            }
            if(autoPlayAnimation)
            {
                var animationNode = animationController.PlaySingleAnimation(
                    skillAnimationEvent.AnimationClip,
                    transitionFixedTime: skillAnimationEvent.TransitionTime,
                    refreshAnimation: true,
                    transitionBreaking: true);
                animationNode.SetClipSpeed(skillAnimationEvent.GetAnimationSpeed());
            }
            if (skillAnimationEvent.ApplyRootMotion)
            {
                animationController.SetRootMotionAction(_skillBehaviour.OnRootMotion);
            }
            else
            {
                animationController.ClearRootMotionAction();
            }
            _skillBehaviour.AfterSkillAnimationEvent(skillAnimationEvent);
        }
    } 
    private void TickSkillAudioEvent()
    {
        //驱动音效
        _skillClip.SkillAudioData.FrameData.ForEach(skillAudioEvent =>
        {
            skillAudioEvent = _skillBehaviour.BeforeSkillAudioEvent(skillAudioEvent);
            if(skillAudioEvent == null) return;
            if (skillAudioEvent.AudioClip != null && skillAudioEvent.FrameIndex == _currentFrameIndex)
            {
                //播放音效 从头开始播放
                AudioSystem.PlayOneShot(skillAudioEvent.AudioClip,transform.position,false,skillAudioEvent.Volume);
            }
            _skillBehaviour.AfterSkillAudioEvent(skillAudioEvent);
        });
    }

    private Dictionary<SkillEffectEvent,GameObject> _effectDic = new();
    private void TickSkillEffectEvent()
    {
        //驱动特效
        _skillClip.SkillEffectData.FrameData.ForEach(skillEffectData =>
        {
            skillEffectData = _skillBehaviour.BeforeSkillEffectEvent(skillEffectData);
            if(skillEffectData == null) return;
            if (skillEffectData.Prefab != null && skillEffectData.FrameIndex == _currentFrameIndex)
            {
                //实例化特效
                var currentEffect = PoolSystem.GetGameObject(skillEffectData.Prefab);
                if (currentEffect == null)
                {
                    currentEffect = Instantiate(skillEffectData.Prefab);
                    currentEffect.name = skillEffectData.Prefab.name;
                }

                currentEffect.transform.position = _modelTransform.TransformPoint(skillEffectData.Position);
                currentEffect.transform.rotation =  Quaternion.Euler(_modelTransform.eulerAngles + skillEffectData.Rotation);
                currentEffect.transform.localScale = skillEffectData.Scale;
                _effectDic.TryAdd(skillEffectData, currentEffect);
                if (skillEffectData.AutoDestruct)
                {
                    StartCoroutine(AutoDestructEffectGameObject((float)skillEffectData.Duration / _skillClip.FrameRate, currentEffect));
                }
            }
            else if(_effectDic.TryGetValue(skillEffectData, out var currentEffect))
            {
                currentEffect.transform.position = _modelTransform.TransformPoint(skillEffectData.Position);
            }
            _skillBehaviour.AfterSkillEffectEvent(skillEffectData);
        });

    } 
    private void TickSkillAttackDetectionEvent()
    {
         //驱动伤害检测
        _skillClip.SkillAttackDetectionData.FrameData.ForEach(detectionEvent =>
        {
           detectionEvent = _skillBehaviour.BeforeAttackDetectionEvent(detectionEvent);
           if (detectionEvent == null) return;
            AttackDetectionType = detectionEvent.GetAttackDetectionType();
            //武器需要只关注第一帧和结束帧
            if (AttackDetectionType == AttackDetectionType.Weapon)
            {
                if (detectionEvent.FrameIndex == _currentFrameIndex)
                {
                    _skillBehaviour.AddActiveAttackDetectionEvent(detectionEvent);
                    //驱动武器开启
                    var weaponDetectionData = (AttackWeaponDetectionData)detectionEvent.AttackDetectionData;
                    if (_checkerDic.TryGetValue(weaponDetectionData.WeaponName,out _currentWeapon))
                    {
                        _currentWeapon.StartDetection(detectionEvent);
                    }
                    else
                    {
                        Debug.LogError("没有指定武器");
                    }
                }
                if (_currentFrameIndex == detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                {
                    //驱动武器关闭
                    var weaponDetectionData = (AttackWeaponDetectionData)detectionEvent.AttackDetectionData;
                    _skillBehaviour.RemoveActiveAttackDetectionEvent(detectionEvent);
                    if (_checkerDic.TryGetValue(weaponDetectionData.WeaponName, out _currentWeapon))
                    {
                        _currentWeapon.StopDetection();
                    }
                    else
                    {
                        Debug.LogError("没有指定武器");
                    }
                }
            }
            else
            {
                if (_currentFrameIndex == detectionEvent.FrameIndex)
                {
                    _skillBehaviour.AddActiveAttackDetectionEvent(detectionEvent);
                }
                //在当前帧范围内
                if (_currentFrameIndex >= detectionEvent.FrameIndex && _currentFrameIndex <= detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                {
                  var colliders =  SkillAttackDetectionTool.ShapeDetection(_modelTransform,detectionEvent.AttackDetectionData,AttackDetectionType,detectionEvent.CheckLayerMask);
                  if (colliders == null) return;
                  foreach(var collider in colliders)
                  {
                      if(collider == null) break;
                      var hitTarget = collider.GetComponentInChildren<IHitTarget>();
                      var shapeDetectionData = (AttackShapeDetectionDataBase)detectionEvent.AttackDetectionData;
                      var hitPoint = collider.ClosestPoint(_modelTransform.TransformPoint(shapeDetectionData.Position));
                      if(hitTarget == null  || _skillBehaviour == null || !_skillBehaviour.OnDetection(hitTarget,hitPoint,detectionEvent)) continue;
                      foreach (var triggerEvent in detectionEvent.TriggerEvents)
                      {
                          if(_attackDetectionEventProcessorDic.TryGetValue(triggerEvent.GetType(),out var eventProcessor)) eventProcessor.ProcessEvent(triggerEvent,hitPoint,hitTarget,this);
                      }
                  }
                }
                if(_currentFrameIndex == detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                {
                    _skillBehaviour.RemoveActiveAttackDetectionEvent(detectionEvent);
                }
            }
            _skillBehaviour.AfterAttackDetectionEvent(detectionEvent);
        });
    }

    private void UpdateCameraEvents()
    {
        if (!_curSkillCamera || _skillClip == null || _frameRate <= 0) return;

        var playbackTime = _playTotalTime;
        _cameraEventBuffer.Clear();
        _cameraEventBuffer.AddRange(_skillClip.CameraOrbitData.FrameData);
        foreach (var cameraEvent in _cameraEventBuffer)
        {
            if (_finishedCameraEvents.Contains(cameraEvent)) continue;
            if (!_cameraEventProcessorDic.TryGetValue(cameraEvent.GetType(), out var processor)) continue;
            var time = CreateCameraEventTime(cameraEvent, playbackTime);
            if (playbackTime < time.StartTime) continue;

            if (_activeCameraEvents.Add(cameraEvent))
            {
                _triggeredCameraProcessors.Add(processor);
                processor.StartEvent(cameraEvent, time, _curSkillCamera, this);
            }
            processor.TickEvent(cameraEvent, time, _curSkillCamera, this);
            if (playbackTime >= time.EndTime)
            {
                _activeCameraEvents.Remove(cameraEvent);
                _finishedCameraEvents.Add(cameraEvent);
                processor.EndEvent(cameraEvent, time, _curSkillCamera, this);
            }
        }
    }

    private void EndActiveCameraEvents()
    {
        if (_activeCameraEvents.Count == 0) return;

        var activeEvents = new List<CameraEventBase>(_activeCameraEvents);
        _activeCameraEvents.Clear();
        foreach (var cameraEvent in activeEvents)
        {
            _finishedCameraEvents.Add(cameraEvent);
            if (!_curSkillCamera || !_cameraEventProcessorDic.TryGetValue(cameraEvent.GetType(), out var processor)) continue;
            processor.EndEvent(cameraEvent, CreateCameraEventTime(cameraEvent, _playTotalTime), _curSkillCamera, this);
        }
    }

    // 帧数在这里统一换算为实际时间
    private CameraEventTime CreateCameraEventTime(CameraEventBase cameraEvent, float playbackTime)
    {
        var startTime = cameraEvent.FrameIndex / (float)_frameRate;
        var duration = Mathf.Max(1, cameraEvent.Duration) / (float)_frameRate;
        return new CameraEventTime(startTime, duration, playbackTime);
    }

    /// <summary>
    /// 复位所有发生过的相机事件处理器的影响，并清空缓存
    /// </summary>
    public void ResetTriggeredCameraProcessors()
    {
        if (_triggeredCameraProcessors.Count == 0) return;

        var processors = new List<CameraEventProcessor>(_triggeredCameraProcessors);
        _triggeredCameraProcessors.Clear();
        foreach (var processor in processors)
        {
            processor.ResetProcessor(_curSkillCamera, this);
        }
    }

    public void RegisterCameraEventProcessor<T>(CameraEventProcessor<T> processor) where T : CameraEventBase
    {
        _cameraEventProcessorDic.TryAdd(typeof(T), processor);
    }

    public void UnRegisterCameraEventProcessor<T>() where T : CameraEventBase
    {
        _cameraEventProcessorDic.Remove(typeof(T));
    }
    
    public void RegisterDetectionEventProcessor<T>(DetectionEventProcessor<T> processor) where T : DetectionTriggerEventBase
    {
        var type = typeof(T);
        _attackDetectionEventProcessorDic.TryAdd(type, processor);
    }
    
    public void UnRegisterDetectionEventProcessor<T>() where T : DetectionTriggerEventBase
    {
        var type = typeof(T);
        _attackDetectionEventProcessorDic.Remove(type);
    }
    

    private IEnumerator AutoDestructEffectGameObject(float duration,GameObject effectObj)
    {
        yield return new WaitForSeconds(duration);
        effectObj.GameObjectPushPool();
    }
    
    public void SetCurSkillCamera(string useCameraName)
    {
        if (_skillCameraDic.TryGetValue(useCameraName, out var skillCamera))
        {
            _curSkillCamera = skillCamera;
        }
        else
        {
            Debug.LogError($"没有找到技能摄像机:{useCameraName}");
        }
    }

    #region Editor

#if UNITY_EDITOR
    [SerializeField] private bool _drawAttackDetectionGizmos = true;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_drawAttackDetectionGizmos && _skillClip != null)
        {
            foreach (var detectionEvent in _skillClip.SkillAttackDetectionData.FrameData)
            {
                if (_currentFrameIndex >= detectionEvent.FrameIndex &&
                    _currentFrameIndex <= detectionEvent.FrameIndex + detectionEvent.DurationFrame)
                {
                    SkillGizmosTool.DrawDetection(detectionEvent, this);
                }
            }
        }
    }
#endif

#endif
    #endregion
    
}
