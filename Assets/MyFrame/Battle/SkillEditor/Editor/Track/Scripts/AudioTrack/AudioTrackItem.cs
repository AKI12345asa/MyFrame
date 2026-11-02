using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioTrackItem : TrackItemBase<AudioTrack>
{
    private SkillMutilineTrackStyle.ChildTrack _childTrack;
    
    private SkillAudioTrackItemStyle AudioTrackItemStyle => ItemStyle as SkillAudioTrackItemStyle;

    private SkillAudioEvent _audioEvent;
    
    public SkillAudioEvent AudioEvent => _audioEvent;
    public void Init(AudioTrack track,float frameUnitWidth,SkillAudioEvent skillAudioEvent,SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        Track = track;
        _frameIndex = skillAudioEvent.FrameIndex;
        _childTrack = childTrack;
        FrameUnitWidth = frameUnitWidth; NormalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
        SelectColor = new Color(0.388f, 0.850f, 0.905f, 0.8f);
        ItemStyle = new SkillAudioTrackItemStyle();
        _audioEvent = skillAudioEvent;
        _childTrack.ContentRoot.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        _childTrack.ContentRoot.RegisterCallback<DragExitedEvent>(OnDragExited);
        ResetView(frameUnitWidth);
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        if (_audioEvent.AudioClip != null) 
        {
            if (!AudioTrackItemStyle.IsInit)
            {
                //绑定事件
            AudioTrackItemStyle.Init(FrameUnitWidth, _audioEvent, _childTrack);
            AudioTrackItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(MouseDown);
            AudioTrackItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(MouseUp);
            AudioTrackItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(MouseOut);
            AudioTrackItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(MouseMove);
            }
        }
        AudioTrackItemStyle.ResetView(frameWidth,_audioEvent);
    }

    public void Destroy()
    {
        _childTrack.Destroy();
    }

    public void SetTrackName(string name)
    {
        _childTrack.SetTrackName(name);
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
       
    }

    private void MouseUp(MouseUpEvent evt)
    {
        if(_mouseDrag)ApplyDrag();
        _mouseDrag = false;
       
    }

    private void MouseOut(MouseOutEvent evt)
    {
        if(_mouseDrag)ApplyDrag();
        _mouseDrag = false;
    }

    private void MouseMove(MouseMoveEvent evt)
    {
        if (!_mouseDrag) return;
        var offsetPos = evt.mousePosition.x - _startDragPosX;
        var offsetFrame = Mathf.RoundToInt(offsetPos / FrameUnitWidth);
        var targetFrameIndex = _startDragFrameIndex + offsetFrame;
        if (targetFrameIndex < 0 || offsetFrame == 0) return;//不考虑拖动到负数帧的情况
            _frameIndex = targetFrameIndex;
            _audioEvent.FrameIndex = _frameIndex;
           //刷新视图
           ResetView(FrameUnitWidth);
    }

    private void ApplyDrag()
    {
            if (_startDragFrameIndex != _frameIndex)
            {
               _audioEvent.FrameIndex = _frameIndex;
            }
            SkillEditorWindow.Instance.SaveConfig();
    }

    public void CheckFrameCount()
    {
        var frameCount = (int)(_audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate);
        //当前动画超过的技能总帧数，则拓展技能总帧数
        if (_frameIndex + frameCount > SkillEditorWindow.Instance.SkillClip.FrameCount)
        {
            SkillEditorWindow.Instance.CurrentFrameCount = _frameIndex + frameCount;
        }
    }
    
    #endregion
    
    #region 资源的拖拽

    private void OnDragUpdate(DragUpdatedEvent evt)
    {
        //监听当前用户拖拽的是否是动画片段
        var objs = DragAndDrop.objectReferences; 
            
        if (objs[0] is AudioClip)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
  
    }
    private void OnDragExited(DragExitedEvent evt)
    {
        var objs = DragAndDrop.objectReferences;
        if (objs[0] is AudioClip clip)
        {
            //放置音频资源
            var selectFrameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);//轨道的本地坐标有考虑偏移值的影响所以直接根据本地坐标计算即可;

            if (selectFrameIndex < 0) return;
                _audioEvent.FrameIndex = selectFrameIndex;
                _audioEvent.AudioClip = clip;
                _audioEvent.PlayCount = 1;
                _audioEvent.Volume = 1;
                _frameIndex = _audioEvent.FrameIndex;
                ResetView(FrameUnitWidth);
                SkillEditorWindow.Instance.SaveConfig();
        }
    }

    #endregion

}
