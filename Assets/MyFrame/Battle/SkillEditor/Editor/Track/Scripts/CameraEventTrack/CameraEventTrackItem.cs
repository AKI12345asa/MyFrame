using UnityEngine;
using UnityEngine.UIElements;

public class CameraEventTrackItem : TrackItemBase<CameraEventTrack>
{
    private SkillMutilineTrackStyle.ChildTrack _childTrack;
    private CameraEventBase _cameraEvent;

    private CameraEventTrackItemStyle CameraEventTrackItemStyle => ItemStyle as CameraEventTrackItemStyle;
    public CameraEventBase CameraEvent => _cameraEvent;

    public void Init(CameraEventTrack track, float frameUnitWidth, CameraEventBase cameraEvent,
        SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        Track = track;
        FrameUnitWidth = frameUnitWidth;
        _frameIndex = cameraEvent.FrameIndex;
        _cameraEvent = cameraEvent;
        _childTrack = childTrack;
        NormalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
        SelectColor = new Color(0.388f, 0.850f, 0.905f, 0.8f);
        ItemStyle = new CameraEventTrackItemStyle();
        ResetView(frameUnitWidth);
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        if (!CameraEventTrackItemStyle.IsInit)
        {
            CameraEventTrackItemStyle.Init(FrameUnitWidth, _cameraEvent, _childTrack);
            CameraEventTrackItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(MouseDown);
            CameraEventTrackItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(MouseMove);
            CameraEventTrackItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(MouseUp);
            CameraEventTrackItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(MouseOut);
        }

        CameraEventTrackItemStyle.ResetView(frameWidth, _cameraEvent);
    }

    public void Destroy()
    {
        _childTrack.Destroy();
    }

    public void SetTrackName(string name)
    {
        _childTrack.SetTrackName(name);
    }

    public void SetFrameIndex(int frameIndex)
    {
        _frameIndex = Mathf.Max(0, frameIndex);
        _cameraEvent.FrameIndex = _frameIndex;
        ResetView(FrameUnitWidth);
    }

    private bool _isDragging;
    private float _dragStartPositionX;
    private int _dragStartFrameIndex;

    private void MouseDown(MouseDownEvent evt)
    {
        _dragStartPositionX = evt.mousePosition.x;
        _dragStartFrameIndex = _frameIndex;
        _isDragging = true;
        Select();
    }

    private void MouseMove(MouseMoveEvent evt)
    {
        if (!_isDragging) return;

        var offsetFrame = Mathf.RoundToInt((evt.mousePosition.x - _dragStartPositionX) / FrameUnitWidth);
        var targetFrameIndex = _dragStartFrameIndex + offsetFrame;
        if (targetFrameIndex < 0) return;

        _frameIndex = targetFrameIndex;
        _cameraEvent.FrameIndex = targetFrameIndex;
        ResetView(FrameUnitWidth);
    }

    private void MouseUp(MouseUpEvent evt)
    {
        SaveDrag();
    }

    private void MouseOut(MouseOutEvent evt)
    {
        SaveDrag();
    }

    private void SaveDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;
        SkillEditorWindow.Instance.SaveConfig();
    }
}
