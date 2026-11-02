using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EffectTrack : SkillTrackBase
{
    private SkillMutilineTrackStyle _trackStyle;
    
    public SkillEffectData EffectData => SkillEditorWindow.Instance.SkillClip.SkillEffectData;
    
    private List<EffectTrackItem> _trackItems = new();

    public static Transform EffectParent{get;private set;}
    
    public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
    {
        base.Init(menuParent, trackParent, frameWidth);
        _trackStyle = new  SkillMutilineTrackStyle();
        _trackStyle.Init(menuParent, trackParent, "特效配置",AddChildTrack,CheckDeleteChildTrack,SwapChildTrack,UpdateChildTrackName);
        //TODO:根据音效数据初始化音效子轨道
        if (SkillEditorWindow.Instance.OnEditorScene)
        {
          ClearEffects();
        }
        
        EffectData.FrameData.ForEach(CreateItem);
        
    }


    public void ClearEffects()
    {
        EffectParent = GameObject.Find("Effects").transform;
        EffectParent.rotation = Quaternion.Euler(0, 0, 0);
        EffectParent.localPosition = Vector3.zero;
        EffectParent.localScale= Vector3.one;
      
        while (EffectParent.childCount > 0)
        {
            var gameObject = EffectParent.GetChild(0).gameObject;
            Object.DestroyImmediate(gameObject);
        }
    }
    
    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        _trackItems.ForEach(item => item.Destroy());
        _trackItems.Clear();
        if (SkillEditorWindow.Instance.SkillClip == null) return;
        EffectData.FrameData.ForEach(CreateItem);
    }

    private void CreateItem(SkillEffectEvent effectEvent)
    {
        var item = new EffectTrackItem();
        item.Init(this,FrameUnitWidth,effectEvent,_trackStyle.AddChildTrack());
        item.SetTrackName(effectEvent.TrackName);
        _trackItems.Add(item);
    }

    private void SwapChildTrack(int index1, int index2)
    {
        var data1 = EffectData.FrameData[index1];
        var data2 = EffectData.FrameData[index2];
        
        EffectData.FrameData[index1] = data2;
        EffectData.FrameData[index2] = data1;
        
    }
    
    public override void Destroy()
    {
        _trackItems.ForEach(item => item.CleanEffectPreviewObj());
        _trackStyle.Destroy();
    }
    
    
    private void AddChildTrack()
    {
        var skillEffectEvent = new SkillEffectEvent();
        EffectData.FrameData.Add(skillEffectEvent);
        CreateItem(skillEffectEvent);
        SkillEditorWindow.Instance.SaveConfig();
    }

    private bool CheckDeleteChildTrack(int index)
    {
        if(index < 0 || index >= EffectData.FrameData.Count) return false;
        if (EffectData.FrameData[index] == null) return false;
        EffectData.FrameData.RemoveAt(index);
        SkillEditorWindow.Instance.SaveConfig();
        _trackItems[index].CleanEffectPreviewObj();
        
        _trackItems.RemoveAt(index);
        return true;
    }
    
    private void UpdateChildTrackName(SkillMutilineTrackStyle.ChildTrack childTrack, string newName)
    {
        EffectData.FrameData[childTrack.GetIndex()].TrackName = newName;
        SkillEditorWindow.Instance.SaveConfig();
    }

    public override void TickView(int frameIndex)
    {
        _trackItems.ForEach(item => item.TickView(frameIndex));
    }
}
