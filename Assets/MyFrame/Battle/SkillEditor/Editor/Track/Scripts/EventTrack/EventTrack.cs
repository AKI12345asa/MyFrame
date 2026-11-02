using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

public class EventTrack : SkillTrackBase
{
    private SkillSingleLineTrackStyle _trackStyle;
    
    private Dictionary<int, EventTrackItem> _trackItemDic = new();

    public SkillCustomEventData EventData => SkillEditorWindow.Instance.SkillClip.SkillCustomEventData;
    
    public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
    {
        base.Init(menuParent, trackParent,frameWidth);
        _trackStyle = new SkillSingleLineTrackStyle();
        _trackStyle.Init(menuParent, trackParent, "事件配置");
        _trackStyle.RegisterContentCallback<MouseDownEvent>(ContentRootMouseDown);
        ResetView();
    }

    private void ContentRootMouseDown(MouseDownEvent evt)
    {
        var frameIndex = SkillEditorWindow.Instance.GetFrameIndexByMousePos(evt.localMousePosition.x,false);
        if (EventData.FrameData.ContainsKey(frameIndex)) return;
        if (EventTrackItem.CurrentSelectItem != null)//变化位置
        {
            EventTrackItem.CurrentSelectItem.ChangeFrameIndex(frameIndex);
        }
        else//未选中则是添加轨道
        {
            var customEvent = new SkillCustomEvent();
            EventData.FrameData.Add(frameIndex, customEvent);
            SkillEditorWindow.Instance.SaveConfig();
            CreateItem(frameIndex,customEvent);
        }
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        //清空原有的视图
        _trackItemDic.ForEach(item =>
        {
            _trackStyle.DeleteItem(item.Value.ItemStyle.Root);
        });
        _trackItemDic.Clear();
        if (SkillEditorWindow.Instance.SkillClip == null) return;
        //根据新数据绘制轨道
        SkillEditorWindow.Instance.SkillClip.SkillCustomEventData.FrameData.ForEach(item =>
        {
            CreateItem(item.Key,item.Value);
        });
    }
    
    private void CreateItem(int frameIndex, SkillCustomEvent skillAnimationEvent)
    {
        var trackItem = new EventTrackItem();
        trackItem.Init(this, _trackStyle, frameIndex, FrameUnitWidth, skillAnimationEvent);
        _trackItemDic.Add(frameIndex, trackItem);
    }
    
    public override void DeleteTrackItem(int frameIndex)
    {
        EventData.FrameData.Remove(frameIndex);//从保存数据中删除
        if (_trackItemDic.Remove(frameIndex, out var trackItem))
        {
            _trackStyle.DeleteItem(trackItem.ItemStyle.Root);
        }//从视图中删除
        SkillEditorWindow.Instance.SaveConfig();
    }
    
    /// <summary>
    /// 更新当前事件的索引
    /// </summary>
    /// <param name="oldIndex"></param>
    /// <param name="newIndex"></param>
    public void SetFrameIndex(int oldIndex, int newIndex)
    {
        if (EventData.FrameData.Remove(oldIndex,
                out var skillAnimationEvent))
        {
            if(!EventData.FrameData.TryGetValue(newIndex,out _))
            {
                EventData.FrameData.Add(newIndex, skillAnimationEvent);
                _trackItemDic.Remove(oldIndex,out var trackItem);
                _trackItemDic.Add(newIndex, trackItem);
                SkillEditorWindow.Instance.SaveConfig();
            }
        }
    }
    
    public override void Destroy()
    {
        _trackStyle.Destroy();
    }
}
