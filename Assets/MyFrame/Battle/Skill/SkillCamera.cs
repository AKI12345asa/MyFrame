using System;
using System.Collections.Generic;
using Cinemachine;
using MyFrame;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class SkillCamera:MonoBehaviour
{
   [SerializeField]private int defaultPriority;
   [SerializeField]private int runningPriority;
   [SerializeField] private bool ifFollowing;
   [SerializeField] private CinemachineImpulseListener impulseListener;
   [SerializeField] private CinemachineImpulseSource impulseSource;
   public bool IfFollowing => ifFollowing;
   public CinemachineImpulseListener ImpulseListener => impulseListener;
   public CinemachineImpulseSource ImpulseSource => impulseSource;
   private CinemachineVirtualCameraBase _virtualCamera;
   private Vector3 _cameraPositionRelativeToPlayer;
   private Quaternion _initialModelRotation;
   private bool _asSkillCamera;
   private bool _holdPosition;
   private bool _holdRotation;
   private Vector3 _heldWorldPosition;
   private Quaternion _heldWorldRotation;
   private Skill_Player _skillPlayer;
   private ICharacter _owner;
   public event Action OnSkillCameraStart;
   public event Action OnSkillCameraEnd;
   public bool IsActive => _asSkillCamera;
   public Skill_Player SkillPlayer => _skillPlayer;
   public ICharacter Owner => _owner;
   public CinemachineVirtualCameraBase VirtualCamera => _virtualCamera;
   // 开启技能相机时记录的宿主模型朝向
   public Quaternion InitialModelRotation => _initialModelRotation;
   public void Init(Skill_Player skillPlayer, ICharacter owner)
   {
      _skillPlayer = skillPlayer;
      _owner = owner;
      _virtualCamera = GetComponent<CinemachineVirtualCameraBase>();
   }

   public void SynchronizeCameraPose(Vector3 position, Quaternion rotation)
   {
      transform.SetPositionAndRotation(position, rotation);
      if (_virtualCamera == null)
      {
         _virtualCamera = GetComponent<CinemachineVirtualCameraBase>();
      }

      if (_virtualCamera == null) return;
      _virtualCamera.ForceCameraPosition(position, rotation);
      _virtualCamera.PreviousStateIsValid = false;
   }
   
   /// <summary>
   /// 开启技能相机，由 Skill_Player 在开始播放技能时调用
   /// </summary>
   public void StartSkillCamera()
   {
      if (_asSkillCamera || _owner == null || _owner.ModelTransform == null) return;
      OnSkillCameraStart?.Invoke();
      SynchronizeCameraPose(transform.position, transform.rotation);
      _initialModelRotation = _owner.ModelTransform.rotation;
      _asSkillCamera = true;
      if (IfFollowing)
      {
         _cameraPositionRelativeToPlayer = _owner.ModelTransform.InverseTransformPoint(transform.position);
      }
      SetPriorityIfConfigured(runningPriority);
   }

   /// <summary>
   /// 记录当前世界位置并在之后保持不变（相机可能是宿主的子物体，避免被宿主带偏）
   /// </summary>
   public void HoldWorldPosition()
   {
      _holdPosition = true;
      _heldWorldPosition = transform.position;
   }

   public void HoldWorldPosition(Vector3 worldPosition)
   {
      transform.position = worldPosition;
      HoldWorldPosition();
   }

   public void ReleaseWorldPosition()
   {
      _holdPosition = false;
   }

   /// <summary>
   /// 记录当前世界旋转并在之后保持不变
   /// </summary>
   public void HoldWorldRotation()
   {
      _holdRotation = true;
      _heldWorldRotation = transform.rotation;
   }

   public void HoldWorldRotation(Quaternion worldRotation)
   {
      transform.rotation = worldRotation;
      HoldWorldRotation();
   }

   public void ReleaseWorldRotation()
   {
      _holdRotation = false;
   }

   public void LateUpdate()
   {
      if (!_asSkillCamera) return;
      if (IfFollowing)
      {
         transform.position = _owner.ModelTransform.TransformPoint(_cameraPositionRelativeToPlayer);
      }
      else if (_holdPosition)
      {
         transform.position = _heldWorldPosition;
      }

      if (_holdRotation)
      {
         transform.rotation = _heldWorldRotation;
      }
   }

   /// <summary>
   /// 关闭技能相机
   /// </summary>
   public void ReSetSkillCamera()
   {
      SetPriorityIfConfigured(defaultPriority);
      _asSkillCamera = false;
      _holdPosition = false;
      _holdRotation = false;
      OnSkillCameraEnd?.Invoke();
   }

   private void SetPriorityIfConfigured(int priority)
   {
      if (_virtualCamera != null && priority != -1)
      {
         _virtualCamera.Priority = priority;
      }
   }

   public float FieldOfView
   {
      get
      {
         switch (_virtualCamera)
         {
            case CinemachineVirtualCamera virtualCamera:
               return virtualCamera.m_Lens.FieldOfView;
            case CinemachineFreeLook freeLook:
               return freeLook.m_Lens.FieldOfView;
            default:
               return 0;
         }
      }
      set
      {
         switch (_virtualCamera)
         {
            case CinemachineVirtualCamera virtualCamera:
               var virtualCameraLens = virtualCamera.m_Lens;
               virtualCameraLens.FieldOfView = value;
               virtualCamera.m_Lens = virtualCameraLens;
               break;
            case CinemachineFreeLook freeLook:
               var freeLookLens = freeLook.m_Lens;
               freeLookLens.FieldOfView = value;
               freeLook.m_Lens = freeLookLens;
               break;
         }
      }
   }
   
}
