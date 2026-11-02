using System.Collections.Generic;
using MyFrame;
using Sirenix.Serialization;
using UnityEngine;
/// <summary>按名称索引单片段动画、方向线性混合树和二维混合树的配置集合。</summary>
[CreateAssetMenu(fileName = "CharacterAnimationConfig", menuName = "Animation/CharacterAnimationConfig")]
[FrameConfig(typeof(CharacterAnimationConfig), "AnimationConfig/CharacterAnimationConfig")]
public class CharacterAnimationConfig:FrameConfigBase
{
   [OdinSerialize]private Dictionary<string,AnimationClip> SingleAnimationClipDic = new Dictionary<string, AnimationClip>(); 
   [OdinSerialize]private Dictionary<string,DirectionalLinearBlendTreeData> DirectionalBlendTreeDic = new Dictionary<string, DirectionalLinearBlendTreeData>();
   [OdinSerialize] private Dictionary<string,TwoDimensionalBlendTreeData> TwoDimensionalBlendTreeDic = new Dictionary<string, TwoDimensionalBlendTreeData>();

   /// <summary>按名称查找单个动画片段。</summary>
   public bool TryGetAnimationClip(string clipName,out AnimationClip animationClip)
   {
      return SingleAnimationClipDic.TryGetValue(clipName, out animationClip);
   }
   
   /// <summary>按名称查找方向线性混合树配置。</summary>
   public bool TryGetDirectionalBlendTreeData(string blendTreeName,out DirectionalLinearBlendTreeData directionalLinearBlendTreeData)
   {
      return DirectionalBlendTreeDic.TryGetValue(blendTreeName, out directionalLinearBlendTreeData);
   }
   
   /// <summary>按名称查找二维混合树配置。</summary>
   public bool TryGetTwoDimensionalBlendTreeData(string blendTreeName,out TwoDimensionalBlendTreeData twoDimensionalBlendTreeData)
   {
      return TwoDimensionalBlendTreeDic.TryGetValue(blendTreeName, out twoDimensionalBlendTreeData);
   }
}
