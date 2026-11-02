
using System.Collections.Generic;
using MyFrame;
using UnityEngine;

[FrameConfig(typeof(SkillConfig),"技能/技能配置")]
public class SkillConfig:FrameConfigBase
{
    public string SkillName;
    public SkillClip[] Clips;   //全部有可能使用到的技能片段
}
