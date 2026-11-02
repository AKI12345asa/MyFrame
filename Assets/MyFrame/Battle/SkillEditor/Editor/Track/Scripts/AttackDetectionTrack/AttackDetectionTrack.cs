using System.Collections.Generic;
using UnityEngine.UIElements;

public class AttackDetectionTrack:SkillTrackBase
{
    private SkillMutilineTrackStyle _trackStyle;
    
    public AttackDetectionData AttackData => SkillEditorWindow.Instance.SkillClip.SkillAttackDetectionData;

    private List<AttackDetectionTrackItem> _trackItems = new();
    
     public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
    {
        base.Init(menuParent, trackParent, frameWidth);
        _trackStyle = new  SkillMutilineTrackStyle();
        _trackStyle.Init(menuParent, trackParent, "攻击伤害检测",AddChildTrack,CheckDeleteChildTrack,SwapChildTrack,UpdateChildTrackName);
       AttackData.FrameData.ForEach(CreateItem);
    }

       
    
    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        _trackItems.ForEach(item => item.Destroy());
        _trackItems.Clear();
        if (SkillEditorWindow.Instance.SkillClip == null) return;
        AttackData.FrameData.ForEach(CreateItem);
    }

    private void CreateItem(AttackDetectionEvent attackEvent)
    {
        var item = new AttackDetectionTrackItem();
        item.Init(this,FrameUnitWidth,attackEvent,_trackStyle.AddChildTrack());
        item.SetTrackName(attackEvent.TrackName);
        _trackItems.Add(item);
    }

    private void SwapChildTrack(int index1, int index2)
    {
        var data1 = AttackData.FrameData[index1];
        var data2 = AttackData.FrameData[index2];
        
        AttackData.FrameData[index1] = data2;
        AttackData.FrameData[index2] = data1;
        
    }
    
    public override void Destroy()
    {
        _trackStyle.Destroy();
    }
    
    
    private void AddChildTrack()
    {
        var attackEvent = new AttackDetectionEvent();
        AttackData.FrameData.Add(attackEvent);
        CreateItem(attackEvent);
        SkillEditorWindow.Instance.SaveConfig();
    }

    private bool CheckDeleteChildTrack(int index)
    {
        if(index < 0 || index >= AttackData.FrameData.Count) return false;
        if (AttackData.FrameData[index] == null) return false;
        AttackData.FrameData.RemoveAt(index);
        _trackItems.RemoveAt(index);
        SkillEditorWindow.Instance.SaveConfig();
        return true;
    }
    
    private void UpdateChildTrackName(SkillMutilineTrackStyle.ChildTrack childTrack, string newName)
    {
        AttackData.FrameData[childTrack.GetIndex()].TrackName = newName;
        SkillEditorWindow.Instance.SaveConfig();
    }

    public override void DrawGizmos()
    {
        _trackItems.ForEach(item =>
        {
            var currentFrameIndex = SkillEditorWindow.Instance.CurrentSelectFrameIndex;
            var detectionEvent = item.AttackDetectionEvent;
            if (currentFrameIndex < detectionEvent.FrameIndex ||
                currentFrameIndex > detectionEvent.FrameIndex + detectionEvent.DurationFrame) return;
            
            
            item.DrawGizmos();
        });
    }

    public override void OnSceneGUI()
    {
        _trackItems.ForEach(item =>
        {
            var currentFrameIndex = SkillEditorWindow.Instance.CurrentSelectFrameIndex;
            var detectionEvent = item.AttackDetectionEvent;
            if (currentFrameIndex < detectionEvent.FrameIndex ||
                currentFrameIndex > detectionEvent.FrameIndex + detectionEvent.DurationFrame) return;
            if (SkillEditorInspector.CurrentTrackItem != item) return;//选中才绘制
            item.OnSceneGUI();
        });
    }
}
