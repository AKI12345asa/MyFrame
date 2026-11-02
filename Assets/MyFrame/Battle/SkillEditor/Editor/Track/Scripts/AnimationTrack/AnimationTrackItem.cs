using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AnimationTrackItem : TrackItemBase<AnimationTrack>
{
   
   
    private SkillAnimationEvent _animationEvent;
    public SkillAnimationEvent AnimationEvent => _animationEvent;
    
    private SkillAnimationTrackItemStyle TrackItemStyle => ItemStyle as SkillAnimationTrackItemStyle;
    public void Init(AnimationTrack animationTrack,SkillTrackStyleBase parentTrackStyle,int startFrameIndex,float frameUnitWidth,SkillAnimationEvent animationEvent)
    {
        Track = animationTrack;
        _frameIndex = startFrameIndex;
        FrameUnitWidth = frameUnitWidth;
        _animationEvent = animationEvent;
        ItemStyle = new SkillAnimationTrackItemStyle();
        TrackItemStyle.Init(parentTrackStyle, startFrameIndex, frameUnitWidth);
         NormalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
         SelectColor = new Color(0.388f, 0.850f, 0.905f, 0.8f);
        TrackItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(MouseDown);
        TrackItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(MouseUp);
        TrackItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(MouseOut);
        TrackItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(MouseMove);
        
        ResetView(frameUnitWidth);
    }
    
  
    public override void ResetView(float frameUnitWidth)
    {
        FrameUnitWidth = frameUnitWidth;
        var root = TrackItemStyle.Root;
        var animationOverLine = TrackItemStyle.AnimationOverLine;
        TrackItemStyle.SetTitle(_animationEvent.AnimationClip.name);
        //位置计算
        TrackItemStyle.SetPositionX(_frameIndex * frameUnitWidth);//transform.position是相对于父节点的位置
        TrackItemStyle.SetWidth(_animationEvent.DurationFrame * frameUnitWidth);  

        var animationClipFrameCount = _animationEvent.AnimationClip.length *
                                      SkillEditorWindow.Instance.SkillClip.FrameRate /
                                      _animationEvent.GetAnimationSpeed();
        //计算动画结束线的位置
        if (animationClipFrameCount > _animationEvent.DurationFrame)
        {
            animationOverLine.style.display = DisplayStyle.None;
        }
        else
        {
            animationOverLine.style.display = DisplayStyle.Flex;
            var overLinePos = animationOverLine.transform.position;
            overLinePos.x = animationClipFrameCount * frameUnitWidth - 1;
            animationOverLine.transform.position = overLinePos;
        }
        Track.TickView(SkillEditorWindow.Instance.CurrentSelectFrameIndex);
    }

    #region 鼠标交互
 
    private bool _mouseDrag = false;
    private float _startDragPosX;
    private int _startDragFrameIndex;
    private readonly object _mouseLock = new object();
    private void MouseDown(MouseDownEvent evt)
    {
        
        _startDragPosX = evt.mousePosition.x;
        _startDragFrameIndex = _frameIndex;
        _mouseDrag = true;
        Select();
        evt.StopPropagation();
    }

    private void MouseUp(MouseUpEvent evt)
    {
        if(_mouseDrag)ApplyDrag();
        _mouseDrag = false;
        evt.StopPropagation();
    }

    private void MouseOut(MouseOutEvent evt)
    {
        if(_mouseDrag)ApplyDrag();
        _mouseDrag = false;
        evt.StopPropagation();
    }

    private void MouseMove(MouseMoveEvent evt)
    {
        if (!_mouseDrag) return;
        var offsetPos = evt.mousePosition.x - _startDragPosX;
        var offsetFrame = Mathf.RoundToInt(offsetPos / FrameUnitWidth);
        var targetFrameIndex = _startDragFrameIndex + offsetFrame;
        if (targetFrameIndex < 0) return;//不考虑拖动到负数帧的情况
           _frameIndex = targetFrameIndex;
            CheckFrameCount();
           //刷新视图
           ResetView(FrameUnitWidth);
           evt.StopPropagation();
    }

    private void ApplyDrag()
    {
        lock (_mouseLock)
        {
            var dragCheck = Track.CheckFrameIndexOnDrag(_frameIndex,_startDragFrameIndex) && Track.CheckFrameIndexOnDrag(_frameIndex + _animationEvent.DurationFrame,_startDragFrameIndex) &&
                            Track.CheckFrameIndexOnDrag(_frameIndex,_frameIndex + _animationEvent.DurationFrame,_startDragFrameIndex);
            if(dragCheck)
            {
                if (_startDragFrameIndex != _frameIndex)
                {
                   Track.SetFrameIndex(_startDragFrameIndex, _frameIndex);
                }
                SkillEditorInspector.Instance.SetTrackItemFrameIndex(_frameIndex);
            }
            else
            {
                _frameIndex = _startDragFrameIndex;
            }
            ResetView(FrameUnitWidth);
        }
    }

    public void CheckFrameCount()
    {
        //当前动画超过的技能总帧数，则拓展技能总帧数
        if (_frameIndex + _animationEvent.DurationFrame > SkillEditorWindow.Instance.SkillClip.FrameCount)
        {
            SkillEditorWindow.Instance.CurrentFrameCount = _frameIndex + _animationEvent.DurationFrame;
        }
    }
    
    #endregion

    public override void OnConfigChanged()
    {
        _animationEvent = Track.AnimationData.FrameData[_frameIndex];
        ResetView(FrameUnitWidth);
    }
    
    
}
