using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EventTrackItem : TrackItemBase<EventTrack>
{
    private SkillCustomEvent _customEvent;
    public SkillCustomEvent CustomEvent => _customEvent;
    public static EventTrackItem CurrentSelectItem;
   
    private SkillCustomEventTrackItemStyle TrackItemStyle => ItemStyle as SkillCustomEventTrackItemStyle;
    public void Init(EventTrack eventTrack,SkillTrackStyleBase parentTrackStyle,int startFrameIndex,float frameUnitWidth,SkillCustomEvent animationEvent)
    {
        Track = eventTrack;
        _frameIndex = startFrameIndex;
        FrameUnitWidth = frameUnitWidth;
        _customEvent = animationEvent;
        ItemStyle = new SkillCustomEventTrackItemStyle();
        TrackItemStyle.Init(parentTrackStyle);
        NormalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
        SelectColor = new Color(0.388f, 0.850f, 0.905f, 0.8f);
        TrackItemStyle.Root.RegisterCallback<MouseDownEvent>(MouseDown);
        ResetView(frameUnitWidth);
    }

    private void MouseDown(MouseDownEvent evt)
    {
        if (CurrentSelectItem != this)
        {
            if(CurrentSelectItem != null)
            {
                CurrentSelectItem.OnUnSelect();
            }
            Select();
        }
        else
        {
            OnUnSelect();
        }
    }

    public override void OnSelect()
    {
        CurrentSelectItem = this;
        base.OnSelect();
    }
    
    public override void OnUnSelect()
    {
        CurrentSelectItem = null;
        base.OnUnSelect();
    }



    public override void ResetView(float frameUnitWidth)
    {
        FrameUnitWidth = frameUnitWidth;
        //位置计算
        TrackItemStyle.SetPositionX(_frameIndex * frameUnitWidth - frameUnitWidth / 2);//transform.position是相对于父节点的位置
        TrackItemStyle.SetWidth(frameUnitWidth);
    }

    public void ChangeFrameIndex(int newIndex)
    {
        Track.SetFrameIndex(_frameIndex,newIndex);
        _frameIndex = newIndex;
        SkillEditorInspector.Instance.SetTrackItemFrameIndex(_frameIndex);
        ResetView();
    }
    
}
