using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraEventTrackItemStyle : SkillTrackItemStyleBase
{
    private const string TrackItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/TrackItem/CameraEventTrackItem.uxml";

    private Label _titleLabel;
    private SkillMutilineTrackStyle.ChildTrack _childTrack;

    public bool IsInit { get; private set; }
    public VisualElement MainDragArea { get; private set; }

    public void Init(float frameUnitWidth, CameraEventBase cameraEvent, SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        if (IsInit) return;

        _titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackItemAssetPath).Instantiate().Q<Label>();
        Root = _titleLabel;
        MainDragArea = Root.Q<VisualElement>("Main");
        _childTrack = childTrack;
        _childTrack.InitContent(Root);
        IsInit = true;
        ResetView(frameUnitWidth, cameraEvent);
    }

    public void ResetView(float frameUnitWidth, CameraEventBase cameraEvent)
    {
        if (!IsInit) return;

        _titleLabel.text = cameraEvent.DisplayName;
        SetWidth(Mathf.Max(1, cameraEvent.Duration) * frameUnitWidth);
        SetPositionX(cameraEvent.FrameIndex * frameUnitWidth);
    }
}
