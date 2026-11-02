using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public interface ISkillOwner
{
   public GameObject SkillOwnerGameObject  { get; } 
   public List<SkillConfig> SkillOwnerSkillConfigs { get; }
   public CinemachineVirtualCameraBase GetSkillCamera(GameObject skillOwnerGameObject);

   public List<Type> GetRemainingComponentWhenPlay();
}
